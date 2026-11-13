# Referencias del paquete: vista general, vista del piloto, planta y perfil con la línea de visión.
# Dibujos de los BLOQUES generados (proyección propia con matplotlib), no capturas de Unity.
import math, os, pickle, sys
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Polygon, Rectangle, FancyArrowPatch
sys.path.insert(0, os.path.dirname(__file__))

OUT = sys.argv[1]
g = pickle.load(open(os.path.join(OUT, "_geo.pkl"), "rb"))
from scipy.spatial import ConvexHull

def caras(pts):
    P = np.array(pts); h = ConvexHull(P); grupos = {}
    for simp, eq in zip(h.simplices, h.equations):
        grupos.setdefault(tuple(np.round(eq, 4)), set()).update(simp.tolist())
    res = []
    for eq, idx in grupos.items():
        n = np.array(eq[:3]); idx = list(idx); c = P[idx].mean(0)
        u = P[idx[0]] - c; u /= np.linalg.norm(u); w = np.cross(n, u)
        idx.sort(key=lambda i: math.atan2(np.dot(P[i] - c, w), np.dot(P[i] - c, u)))
        res.append((P[idx], n))
    return res

# Polígonos en coordenadas de Unity (la envolvente convexa no depende de la orientación)
COLOR = {k: v[3] if v[3] else v[0] for k, v in g["MATERIALES"].items()}
polis = []
for o in g["objetos"]:
    if o["nombre"].endswith("_COL") or o["nombre"].startswith("AYUDA"): continue
    for mat, pts in o["solidos"]:
        # Para el sentido de la normal basta un sistema diestro: (x, z, y) y volver.
        for P, n in caras([np.array([p[0], p[2], p[1]]) for p in pts]):
            polis.append((P[:, [0, 2, 1]], np.array([n[0], n[2], n[1]]), mat))

# Sala de la cabina, en coordenadas de la consola: pivote en z_mundo = 3,3
Z0 = 3.3
SALA = {"x": (-4.0, 4.0), "y": (0.0, 3.0), "z": (1.5 - Z0, 5.0 - Z0)}
EMIS = {"M_Pantalla", "M_TiraLuz", "M_Indicador"}

def render(nombre, ojo, objetivo, vfov, ancho_px, alto_px, sala=True, titulo=""):
    f = objetivo - ojo; f /= np.linalg.norm(f)
    r = np.cross(np.array([0, 1, 0]), f); r /= np.linalg.norm(r)      # sistema de Unity (zurdo): derecha = up × forward
    u = np.cross(f, r)
    t = math.tan(math.radians(vfov) / 2); asp = ancho_px / alto_px
    def proy(p):
        d = p - ojo; zc = d @ f
        return np.array([(d @ r) / (zc * t * asp), (d @ u) / (zc * t)]), zc
    fig = plt.figure(figsize=(ancho_px / 100, alto_px / 100), dpi=100)
    ax = fig.add_axes([0, 0, 1, 1]); ax.set_xlim(-1, 1); ax.set_ylim(-1, 1); ax.axis("off")
    fig.patch.set_facecolor("#14181d")
    luz = np.array([0.3, 0.8, -0.5]); luz /= np.linalg.norm(luz)
    items = []
    if sala:
        (x0, x1), (y0, y1), (z0, z1) = SALA["x"], SALA["y"], SALA["z"]
        caras_sala = [([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], "#2b3138"),   # pared de proa (ventana)
                      ([(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], "#3a4048"),   # suelo
                      ([(x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1)], "#22272d"),   # techo
                      ([(x0, y0, z0), (x0, y1, z0), (x0, y1, z1), (x0, y0, z1)], "#272c33"),
                      ([(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], "#272c33")]
        for pts, col in caras_sala:
            P = np.array(pts, float)
            items.append((1e6, P, col, "sala"))
    for P, n, mat in polis:
        c = P.mean(0)
        if n @ (ojo - c) <= 0: continue
        base = np.array(COLOR[mat])
        k = 1.0 if mat in EMIS else 0.35 + 0.65 * max(0.0, n @ luz)
        items.append(((c - ojo) @ f, P, tuple(np.clip(base * k, 0, 1)), mat))
    items.sort(key=lambda it: -it[0])
    for prof, P, col, mat in items:
        Q = [proy(p) for p in P]
        if any(zc <= 0.05 for _, zc in Q):
            continue
        ax.add_patch(Polygon([q for q, _ in Q], closed=True, facecolor=col, edgecolor="#0b0d10" if mat != "sala" else "#4a525c", linewidth=0.4))
    if sala:
        # Línea de altura 0,95 m en la pared de proa: por encima, visible sobre la consola
        a, _ = proy(np.array([SALA["x"][0], 0.95, SALA["z"][1]])); b, _ = proy(np.array([SALA["x"][1], 0.95, SALA["z"][1]]))
        ax.plot([a[0], b[0]], [a[1], b[1]], color="#F28C28", lw=1.2, ls="--")
        ax.text(b[0] - 0.02, b[1] + 0.02, "pared de proa a 0,95 m: por encima se ve sin obstáculos", color="#F28C28", fontsize=10, ha="right")
    ax.text(-0.98, 0.94, titulo, color="white", fontsize=12, va="top")
    ax.text(-0.98, -0.97, "Modelo base de bloques v0 · dibujo de referencia, no captura de Unity", color="#9aa4ae", fontsize=9)
    fig.savefig(os.path.join(OUT, nombre), dpi=100, facecolor=fig.get_facecolor()); plt.close(fig)

ojo = g["VISTA"]
pitch = math.radians(8)
render("vista_piloto_sentado.png", ojo, ojo + np.array([0, -math.sin(pitch), math.cos(pitch)]), 60, 1600, 900,
       titulo="Vista del piloto sentado (ojos a 1,20 m, mirando a la ventana, 8° hacia abajo, campo vertical 60°)")
render("vista_general.png", np.array([2.6, 2.1, -2.4]), np.array([0, 0.55, 0.2]), 40, 1600, 1000, sala=False,
       titulo="Vista general: consola (piezas separadas por color de material) y asiento")

# ----------------------------------------------------------------------------- planta y perfil acotados
def contorno_planta():
    pts = np.array([p for o in g["objetos"] if o["raiz"] == "NAV_Pilotaje_Consola_A" and not o["nombre"].endswith("_COL")
                    for _, s in o["solidos"] for p in s])
    return pts

fig, (a1, a2) = plt.subplots(1, 2, figsize=(16, 7.5), gridspec_kw={"width_ratios": [1.15, 1]})
for a in (a1, a2):
    a.set_aspect("equal"); a.grid(color="#dddddd", lw=0.5)
# Planta en coordenadas del MUNDO de la escena Nave (x, z)
a1.set_title("Planta de la cabina de pilotaje (coordenadas de la escena Nave, m)", fontsize=11)
a1.add_patch(Rectangle((-4.0, 1.5), 8.0, 3.5, fill=False, lw=2, ec="#3A3F46"))
a1.plot([-1.2, 1.2], [1.5, 1.5], color="white", lw=3)
a1.text(0, 1.3, "acceso desde el pasillo (x ±1,2)", ha="center", fontsize=9)
a1.plot([-4, 4], [5, 5], color="#5CC8E0", lw=4); a1.text(0, 5.12, "pared de proa / ventana (z = 5)", ha="center", fontsize=9, color="#2a8aa0")
for o in g["objetos"]:
    if o["nombre"].endswith("_COL") or o["nombre"].startswith("AYUDA"): continue
    for mat, s in o["solidos"]:
        P = np.array(s)[:, [0, 2]] + np.array([0, Z0])
        h = ConvexHull(P)
        a1.add_patch(Polygon(P[h.vertices], closed=True, fc=COLOR[mat] if mat not in ("M_PinturaBlanca",) else "#e6e4df", ec="#333", lw=0.4, alpha=0.95))
e = ojo + np.array([0, 0, Z0])
a1.plot(e[0], e[2], "o", color="#E5484D"); a1.text(e[0] + 0.12, e[2] - 0.05, "ojos (ANC_Vista)", fontsize=9, color="#E5484D")
a1.annotate("", xy=(-1.214, 4.35), xytext=(1.214, 4.35), arrowprops=dict(arrowstyle="<->", color="black"))
a1.text(0, 4.42, "2,43 m", ha="center", fontsize=9)
a1.annotate("", xy=(2.2, 3.3), xytext=(2.2, 4.15), arrowprops=dict(arrowstyle="<->"))
a1.text(2.3, 3.7, "0,85 m", fontsize=9)
a1.annotate("", xy=(2.2, 4.15), xytext=(2.2, 5.0), arrowprops=dict(arrowstyle="<->"))
a1.text(2.3, 4.55, "0,85 m libres\nhasta la ventana", fontsize=9)
a1.annotate("", xy=(1.214, 3.0), xytext=(4.0, 3.0), arrowprops=dict(arrowstyle="<->"))
a1.text(2.6, 2.82, "2,79 m libres", ha="center", fontsize=9)
a1.text(-3.9, 1.65, "borde del piloto de la consola: z = 3,30\nasiento: z = 2,95 · ojos: z = 2,80", fontsize=9)
a1.annotate("proa (+Z)", xy=(-3.4, 4.8), xytext=(-3.4, 3.9), arrowprops=dict(arrowstyle="->"), ha="center", fontsize=9)
a1.set_xlim(-4.4, 4.4); a1.set_ylim(1.0, 5.5); a1.set_xlabel("x (m)"); a1.set_ylabel("z (m)")

# Perfil (corte por x = 0): z del mundo en horizontal, y en vertical
a2.set_title("Perfil por el eje (x = 0) y línea de visión", fontsize=11)
a2.add_patch(Rectangle((1.5, 0), 3.5, 3.0, fill=False, lw=2, ec="#3A3F46"))
a2.plot([5, 5], [0, 3], color="#5CC8E0", lw=4)
for o in g["objetos"]:
    if o["nombre"].endswith("_COL") or o["nombre"].startswith("AYUDA"): continue
    for mat, s in o["solidos"]:
        P = np.array(s)
        if P[:, 0].min() > 0.05 or P[:, 0].max() < -0.05: continue   # solo lo que corta x = 0
        Q = P[:, [2, 1]] + np.array([Z0, 0]); h = ConvexHull(Q)
        a2.add_patch(Polygon(Q[h.vertices], closed=True, fc=COLOR[mat] if mat != "M_PinturaBlanca" else "#e6e4df", ec="#333", lw=0.5))
ez, ey = e[2], e[1]
a2.plot(ez, ey, "o", color="#E5484D")
borde = (0.85 + Z0, 1.05)
pend = (borde[1] - ey) / (borde[0] - ez); y5 = ey + pend * (5.0 - ez)
a2.plot([ez, 5.0], [ey, y5], color="#E5484D", lw=1.2, ls="--")
a2.text(4.95, y5 - 0.17, f"{y5:.2f} m", ha="right", fontsize=9, color="#E5484D")
sc = np.array([0.4631 + Z0, 0.919])
a2.plot([ez, sc[0]], [ey, sc[1]], color="#2a8aa0", lw=1)
ang = math.degrees(math.atan2(ey - sc[1], sc[0] - ez))
a2.text(3.0, 1.33, f"a la pantalla central: {ang:.0f}° hacia abajo", fontsize=9, color="#2a8aa0")
a2.text(1.6, 2.75, "techo a 3,0 m", fontsize=9)
a2.text(1.6, 1.25, "ojos a 1,20 m", fontsize=9, color="#E5484D")
a2.annotate("", xy=(4.3, 0), xytext=(4.3, 1.05), arrowprops=dict(arrowstyle="<->")); a2.text(4.35, 0.5, "1,05 m\n(máximo)", fontsize=9)
a2.annotate("", xy=(3.42, 0), xytext=(3.42, 0.70), arrowprops=dict(arrowstyle="<->")); a2.text(3.46, 0.3, "0,70", fontsize=9)
a2.text(1.6, 0.12, "suelo", fontsize=9)
a2.set_xlim(1.4, 5.3); a2.set_ylim(-0.1, 3.2); a2.set_xlabel("z (m)"); a2.set_ylabel("y (m)")
fig.suptitle("Consola de pilotaje v0 · espacio y vista del jugador (propuesta: comprobar en la escena)", fontsize=12)
fig.tight_layout()
fig.savefig(os.path.join(OUT, "planta_y_perfil.png"), dpi=110); plt.close(fig)
print("ok", y5, ang)
