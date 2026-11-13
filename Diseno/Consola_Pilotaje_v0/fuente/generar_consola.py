# Genera el paquete de la consola de pilotaje v0 (modelo base de bloques).
# Fuente única: la geometría se define aquí en el sistema de Unity (x derecha del piloto, y arriba,
# z hacia la ventana, metros). Desde ella se escriben el OBJ, el script de Blender, los anclajes y las referencias.
import json, math, os, sys
import numpy as np
from scipy.spatial import ConvexHull

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)

# ----------------------------------------------------------------------------- utilidades geométricas
def V(*a): return np.array(a, dtype=float)
X, Y, Z = V(1, 0, 0), V(0, 1, 0), V(0, 0, 1)

def caja(c, ejes, medidas):
    """Caja con centro c, ejes (ux, uy, uz) unitarios y medidas totales (a, b, h)."""
    ux, uy, uz = ejes; a, b, h = (m / 2 for m in medidas)
    return [c + sx * a * ux + sy * b * uy + sz * h * uz for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]

def cilindro(base, eje, radio, alto, ref=X, n=16):
    eje = eje / np.linalg.norm(eje)
    u = ref - np.dot(ref, eje) * eje; u /= np.linalg.norm(u); w = np.cross(eje, u)
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n
        r = radio * (math.cos(a) * u + math.sin(a) * w)
        pts += [base + r, base + r + eje * alto]
    return pts

def rotY(t):   # convención de Unity: +t gira +z hacia +x
    c, s = math.cos(t), math.sin(t)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])

# ----------------------------------------------------------------------------- medidas (metros)
Z_FRENTE, Z_MESA, Z_ATRIL, Z_FONDO = 0.02, 0.36, 0.60, 0.85
Y_MESA_INF, Y_MESA_DEL, Y_MESA_TRAS = 0.64, 0.70, 0.76     # mesa inclinada 10°
Y_TOPE = 1.05                                               # nada por encima de 1,05 m
MEDIO = 0.50                                                # semiancho del tramo central
ALA = 0.45                                                  # largo de cada ala
ANG_ALA = math.radians(20)
ASIENTO_Z = -0.35
VISTA = V(0, 1.20, -0.50)

def perfil(x0, x1):
    """Tres volúmenes convexos del perfil (mesa, atril, lomo) entre x0 y x1, en coordenadas locales del tramo."""
    mesa = [V(x, y, z) for x in (x0, x1) for (z, y) in
            ((Z_FRENTE, Y_MESA_INF), (Z_FRENTE, Y_MESA_DEL), (Z_MESA, Y_MESA_INF), (Z_MESA, Y_MESA_TRAS))]
    atril = [V(x, y, z) for x in (x0, x1) for (z, y) in
             ((Z_MESA, 0.0), (Z_MESA, Y_MESA_TRAS), (Z_ATRIL, 0.0), (Z_ATRIL, Y_TOPE))]
    lomo = [V(x, y, z) for x in (x0, x1) for (z, y) in
            ((Z_ATRIL, 0.0), (Z_ATRIL, Y_TOPE), (Z_FONDO, 0.0), (Z_FONDO, Y_TOPE))]
    return {"mesa": mesa, "atril": atril, "lomo": lomo}

class Tramo:
    """Transformación local→consola. Local: x a lo largo del tramo, z desde el borde del piloto."""
    def __init__(self, origen, giro):
        self.o, self.R = origen, rotY(giro)
    def p(self, q): return self.o + self.R @ q
    def d(self, q): return self.R @ q

CENTRAL = Tramo(V(0, 0, 0), 0.0)
ALA_DER = Tramo(V(MEDIO, 0, 0), ANG_ALA)       # x local 0..ALA
ALA_IZQ = Tramo(V(-MEDIO, 0, 0), -ANG_ALA)     # x local −ALA..0

# Pantalla: superficie del atril. Dirección de subida s y normal hacia el piloto n.
s_loc = V(0, Y_TOPE - Y_MESA_TRAS, Z_ATRIL - Z_MESA); s_loc /= np.linalg.norm(s_loc)
n_loc = V(0, s_loc[2], -s_loc[1])
c_atril = V(0, (Y_TOPE + Y_MESA_TRAS) / 2, (Z_ATRIL + Z_MESA) / 2)
ang_mesa = math.atan2(Y_MESA_TRAS - Y_MESA_DEL, Z_MESA - Z_FRENTE)
t_mesa = V(0, math.sin(ang_mesa), math.cos(ang_mesa))      # a lo largo de la mesa, hacia el fondo
n_mesa = V(0, math.cos(ang_mesa), -math.sin(ang_mesa))     # normal de la mesa
def sobre_mesa(z):  # punto de la superficie de la mesa (x=0) a profundidad z
    return V(0, Y_MESA_DEL + (Y_MESA_TRAS - Y_MESA_DEL) * (z - Z_FRENTE) / (Z_MESA - Z_FRENTE), z)

# ----------------------------------------------------------------------------- piezas
# Cada objeto: nombre, raíz, pivote (consola), lista de (material, puntos de un sólido convexo)
objetos = []
def objeto(nombre, raiz, pivote, solidos):
    objetos.append({"nombre": nombre, "raiz": raiz, "pivote": pivote, "solidos": solidos})

R_CON, R_ASI = "NAV_Pilotaje_Consola_A", "NAV_Pilotaje_Asiento_A"
MAT_CAPA = {"mesa": "M_Grafito", "atril": "M_PinturaBlanca", "lomo": "M_PinturaBlanca"}

estructura = []
tramos = [(CENTRAL, -MEDIO, MEDIO), (ALA_DER, 0.0, ALA), (ALA_IZQ, -ALA, 0.0)]
for tr, x0, x1 in tramos:
    for capa, pts in perfil(x0, x1).items():
        estructura.append((MAT_CAPA[capa], [tr.p(q) for q in pts]))
# Juntas en cuña entre el tramo central y cada ala (cierran el hueco que deja el giro).
for tr, xc, xa in ((ALA_DER, MEDIO, 0.0), (ALA_IZQ, -MEDIO, 0.0)):
    pc, pa = perfil(xc, xc), perfil(xa, xa)
    for capa in pc:
        cara_central = [CENTRAL.p(q) for q in pc[capa]]
        cara_ala = [tr.p(q) for q in pa[capa]]
        estructura.append((MAT_CAPA[capa], cara_central + cara_ala))
objeto(R_CON + "_Estructura", R_CON, V(0, 0, 0), estructura)

# Pantallas y biseles: (nombre, tramo, centro x local, ancho, alto)
PANTALLAS = [("Estado", CENTRAL, 0.0, 0.60, 0.30),
             ("Propulsion", ALA_IZQ, -ALA / 2, 0.38, 0.26),
             ("Casco", ALA_DER, ALA / 2, 0.38, 0.26)]
biseles, anclas = [], {}
for nom, tr, xc, w, h in PANTALLAS:
    c = c_atril + V(xc, 0, 0)
    ejes = (tr.d(X), tr.d(s_loc), tr.d(n_loc))
    biseles.append(("M_Grafito", caja(tr.p(c + n_loc * 0.010), ejes, (w + 0.05, h + 0.05, 0.02))))
    centro_sup = tr.p(c + n_loc * 0.022)
    objeto(f"{R_CON}_Pantalla_{nom}", R_CON, centro_sup, [("M_Pantalla", caja(centro_sup, ejes, (w, h, 0.004)))])
    anclas[f"ANC_Pantalla_{nom}"] = {"pos": centro_sup, "normal": tr.d(n_loc), "arriba": tr.d(s_loc),
                                     "nota": f"centro de la superficie; area util {w:.2f} x {h:.2f} m; normal hacia el piloto"}
objeto(R_CON + "_Bisel", R_CON, V(0, 0, 0), biseles)

# Borde naranja de la mesa y tira de luz del zócalo, en los tres tramos
borde, tira = [], []
for tr, x0, x1 in tramos:
    L, xm = x1 - x0, (x0 + x1) / 2
    borde.append(("M_AcentoNaranja", caja(tr.p(V(xm, Y_MESA_DEL + 0.006, Z_FRENTE + 0.01)), (tr.d(X), Y, tr.d(Z)), (L, 0.012, 0.02))))
    borde.append(("M_AcentoNaranja", caja(tr.p(V(xm, Y_MESA_DEL - 0.03, Z_FRENTE - 0.004)), (tr.d(X), Y, tr.d(Z)), (L, 0.06, 0.008))))
    tira.append(("M_TiraLuz", caja(tr.p(V(xm, 0.055, Z_MESA - 0.006)), (tr.d(X), Y, tr.d(Z)), (L - 0.02, 0.03, 0.012))))
objeto(R_CON + "_BordeAcento", R_CON, V(0, 0, 0), borde)
objeto(R_CON + "_TiraLuz", R_CON, V(0, 0, 0), tira)

# Luz de estado (tramo central, a la derecha, junto al atril)
p_ind = sobre_mesa(0.30) + V(0.36, 0, 0)
objeto(R_CON + "_Indicador", R_CON, p_ind, [("M_Indicador", cilindro(p_ind, n_mesa, 0.03, 0.02))])
anclas["ANC_Indicador"] = {"pos": p_ind, "nota": "base de la luz de estado"}

# Palanca de empuje (ala derecha). Pivote en el eje de giro, sobre la base.
def en_mesa(tr, x, z, alto=0.0):
    return tr.p(sobre_mesa(z) + V(x, 0, 0) + n_mesa * alto)
ejes_mesa_der = (ALA_DER.d(X), ALA_DER.d(n_mesa), ALA_DER.d(t_mesa))
base_pal = en_mesa(ALA_DER, 0.27, 0.18, 0.015)
piv_pal = en_mesa(ALA_DER, 0.27, 0.18, 0.03)
inclin = ALA_DER.d(n_mesa * math.cos(math.radians(15)) + t_mesa * math.sin(math.radians(15)))
brazo_c = piv_pal + inclin * 0.08
objeto(R_CON + "_Palanca", R_CON, piv_pal, [
    ("M_Grafito", caja(base_pal, ejes_mesa_der, (0.10, 0.03, 0.20))),
    ("M_MetalDesnudo", caja(brazo_c, (ALA_DER.d(X), inclin, np.cross(ALA_DER.d(X), inclin) * -1), (0.025, 0.16, 0.025))),
    ("M_AcentoNaranja", caja(piv_pal + inclin * 0.175, (ALA_DER.d(X), inclin, np.cross(ALA_DER.d(X), inclin) * -1), (0.07, 0.035, 0.04))),
])
anclas["ANC_Palanca"] = {"pos": piv_pal, "nota": "eje de giro de la palanca (gira sobre el eje x del ala derecha)"}

# Selector de etapa con tapa de seguridad (tramo central, izquierda)
ejes_mesa_c = (X, n_mesa, t_mesa)
base_sel = sobre_mesa(0.20) + V(-0.30, 0, 0) + n_mesa * 0.01
objeto(R_CON + "_SelectorEtapa", R_CON, base_sel, [
    ("M_Grafito", caja(base_sel, ejes_mesa_c, (0.11, 0.02, 0.11))),
    ("M_Alerta", cilindro(base_sel + n_mesa * 0.01, n_mesa, 0.03, 0.018)),
])
bisagra = sobre_mesa(0.20) + V(-0.30, 0, 0) + n_mesa * 0.042 + t_mesa * 0.055
ABRE = math.radians(70)    # tapa abierta 70° para que se vea el botón (en el juego podría animarse)
d_tapa = -t_mesa * math.cos(ABRE) + n_mesa * math.sin(ABRE)
n_tapa = n_mesa * math.cos(ABRE) + t_mesa * math.sin(ABRE)
objeto(R_CON + "_TapaEtapa", R_CON, bisagra, [
    ("M_AcentoNaranja", caja(bisagra + d_tapa * 0.055, (X, n_tapa, d_tapa), (0.115, 0.008, 0.11))),
])
anclas["ANC_TapaEtapa"] = {"pos": bisagra, "nota": "bisagra de la tapa (gira sobre el eje x)"}

# Fila de interruptores (ala izquierda)
ejes_mesa_izq = (ALA_IZQ.d(X), ALA_IZQ.d(n_mesa), ALA_IZQ.d(t_mesa))
inter = []
for i in range(6):
    p = en_mesa(ALA_IZQ, -0.40 + i * 0.055, 0.16, 0.015)
    inter.append(("M_MetalDesnudo", caja(p, ejes_mesa_izq, (0.02, 0.03, 0.02))))
inter.append(("M_Grafito", caja(en_mesa(ALA_IZQ, -0.2625, 0.16, 0.004), ejes_mesa_izq, (0.35, 0.008, 0.06))))
objeto(R_CON + "_Interruptores", R_CON, en_mesa(ALA_IZQ, -0.2625, 0.16), inter)

# Asiento (raíz propia; pivote en el suelo, en el centro)
A = V(0, 0, ASIENTO_Z)
arriba_r = V(0, math.cos(math.radians(12)), -math.sin(math.radians(12)))
fondo_r = V(0, math.sin(math.radians(12)), math.cos(math.radians(12)))
pie_resp = A + V(0, 0.48, -0.30)
objeto(R_ASI + "_Base", R_ASI, A, [
    ("M_MetalDesnudo", cilindro(A, Y, 0.24, 0.04)),
    ("M_MetalDesnudo", cilindro(A + V(0, 0.04, 0), Y, 0.06, 0.32)),
])
objeto(R_ASI + "_Cojin", R_ASI, A + V(0, 0.38, 0), [("M_TelaAsiento", caja(A + V(0, 0.43, 0), (X, Y, Z), (0.52, 0.10, 0.50)))])
objeto(R_ASI + "_Respaldo", R_ASI, pie_resp, [
    ("M_TelaAsiento", caja(pie_resp + arriba_r * 0.36, (X, arriba_r, fondo_r), (0.52, 0.72, 0.10))),
    ("M_TelaAsiento", caja(pie_resp + arriba_r * 0.83, (X, arriba_r, fondo_r), (0.30, 0.18, 0.10))),
])
objeto(R_ASI + "_Reposabrazos", R_ASI, A + V(0, 0.64, -0.05), [
    ("M_Grafito", caja(A + V(sx * 0.30, 0.64, -0.05), (X, Y, Z), (0.07, 0.05, 0.40))) for sx in (-1, 1)
])
anclas["ANC_Asiento"] = {"pos": A, "nota": "pivote del asiento en el suelo"}
anclas["ANC_Vista"] = {"pos": VISTA, "nota": "ojos del piloto sentado; orientar hacia +Z (ventana) con unos 8 grados hacia abajo"}

# Colisionador simple (caja que envuelve la consola) y flecha de ayuda hacia la proa
todos = np.array([p for o in objetos if o["raiz"] == R_CON for _, pts in o["solidos"] for p in pts])
mn, mx = todos.min(0), todos.max(0)
objeto(R_CON + "_COL", R_CON, V(0, 0, 0), [("M_Colision", caja((mn + mx) / 2, (X, Y, Z), mx - mn))])
objeto("AYUDA_FlechaProa", "AYUDA", V(0, 0, 1.1), [
    ("M_Ayuda", caja(V(0, 0.005, 1.25), (X, Y, Z), (0.06, 0.01, 0.30))),
    ("M_Ayuda", [V(-0.12, 0, 1.40), V(0.12, 0, 1.40), V(0, 0, 1.60), V(-0.12, 0.01, 1.40), V(0.12, 0.01, 1.40), V(0, 0.01, 1.60)]),
    ("M_AyudaDerecha", caja(V(0.35, 0.005, 1.25), (X, Y, Z), (0.10, 0.01, 0.10))),
])

MATERIALES = {
    "M_PinturaBlanca": ((0.902, 0.894, 0.875), 0.0, 0.45, None),
    "M_Grafito": ((0.227, 0.247, 0.275), 0.6, 0.45, None),
    "M_MetalDesnudo": ((0.62, 0.64, 0.66), 0.9, 0.35, None),
    "M_AcentoNaranja": ((0.949, 0.549, 0.157), 0.1, 0.5, None),
    "M_Pantalla": ((0.02, 0.05, 0.06), 0.0, 0.9, (0.361, 0.784, 0.878)),
    "M_TiraLuz": ((0.1, 0.1, 0.1), 0.0, 0.5, (0.361, 0.784, 0.878)),
    "M_Indicador": ((0.2, 0.2, 0.2), 0.0, 0.6, (0.42, 0.816, 0.541)),
    "M_Alerta": ((0.898, 0.282, 0.302), 0.0, 0.5, None),
    "M_TelaAsiento": ((0.18, 0.21, 0.26), 0.0, 0.2, None),
    "M_Colision": ((0.9, 0.2, 0.9), 0.0, 0.5, None),
    "M_Ayuda": ((1.0, 0.9, 0.0), 0.0, 0.5, None),
    "M_AyudaDerecha": ((0.0, 0.6, 1.0), 0.0, 0.5, None),
}

# ----------------------------------------------------------------------------- caras de cada sólido convexo
def caras(pts):
    """Polígonos convexos (índices) con normal hacia fuera en un sistema DIESTRO."""
    P = np.array(pts)
    h = ConvexHull(P)
    grupos = {}
    for simp, eq in zip(h.simplices, h.equations):
        k = tuple(np.round(eq, 4))
        grupos.setdefault(k, set()).update(simp.tolist())
    res = []
    for eq, idx in grupos.items():
        n = np.array(eq[:3]); idx = list(idx)
        c = P[idx].mean(0)
        u = P[idx[0]] - c; u /= np.linalg.norm(u); w = np.cross(n, u)
        idx.sort(key=lambda i: math.atan2(np.dot(P[i] - c, w), np.dot(P[i] - c, u)))   # antihorario visto desde fuera
        res.append(idx)
    return P, res

# Sistemas de destino (todos diestros):
#   OBJ:     (−x, y, z)  → al importarlo, Unity invierte x y queda la ventana en +Z y la palanca a la derecha.
#   Blender: (x, z, y)   → x derecha, +Y hacia la ventana, Z arriba.
def a_obj(p): return V(-p[0], p[1], p[2])
def a_blender(p): return V(p[0], p[2], p[1])

def malla(obj, conv, relativo):
    verts, polys, mats = [], [], []
    piv = conv(obj["pivote"]) if relativo else V(0, 0, 0)
    for mat, pts in obj["solidos"]:
        P, cs = caras([conv(p) for p in pts])
        base = len(verts)
        verts += [tuple(np.round(p - piv, 5)) for p in P]
        for c in cs:
            polys.append([base + i for i in c]); mats.append(mat)
    return verts, polys, mats

# ----------------------------------------------------------------------------- OBJ + MTL
with open(os.path.join(OUT, "consola_pilotaje_v0.mtl"), "w") as f:
    for m, (col, met, rug, em) in MATERIALES.items():
        f.write(f"newmtl {m}\nKd {col[0]:.3f} {col[1]:.3f} {col[2]:.3f}\n")
        if em: f.write(f"Ke {em[0]:.3f} {em[1]:.3f} {em[2]:.3f}\n")
        f.write("\n")
with open(os.path.join(OUT, "consola_pilotaje_v0.obj"), "w") as f:
    f.write("# Consola de pilotaje v0 - MODELO BASE DE BLOQUES (no es el modelo final)\n")
    f.write("# Metros. Y arriba. Coordenadas absolutas respecto al pivote de la consola.\n")
    f.write("# El OBJ NO guarda pivotes de las piezas ni objetos vacios: ver anclajes.json y README.\n")
    f.write("mtllib consola_pilotaje_v0.mtl\n")
    vi = 1
    for o in objetos:
        v, p, m = malla(o, a_obj, False)
        f.write(f"o {o['nombre']}\n")
        for x in v: f.write(f"v {x[0]:.5f} {x[1]:.5f} {x[2]:.5f}\n")
        actual = None
        for poly, mat in zip(p, m):
            if mat != actual: f.write(f"usemtl {mat}\n"); actual = mat
            f.write("f " + " ".join(str(vi + i) for i in poly) + "\n")
        vi += len(v)

# ----------------------------------------------------------------------------- anclajes.json (coordenadas de Unity)
def r(v): return [round(float(a), 4) for a in v]
datos_anc = {"sistema": "Unity: x derecha del piloto, y arriba, z hacia la ventana; metros; origen = pivote de NAV_Pilotaje_Consola_A",
             "anclajes": {k: {kk: (r(vv) if isinstance(vv, np.ndarray) else vv) for kk, vv in a.items()} for k, a in anclas.items()},
             "pivotes": {o["nombre"]: r(o["pivote"]) for o in objetos}}
with open(os.path.join(OUT, "anclajes.json"), "w", encoding="utf-8") as f:
    json.dump(datos_anc, f, ensure_ascii=False, indent=2)

# ----------------------------------------------------------------------------- script de Blender
raices = {R_CON: V(0, 0, 0), R_ASI: A, "AYUDA": V(0, 0, 0)}
bl_obj = []
for o in objetos:
    v, p, m = malla(o, a_blender, True)
    usados = sorted(set(m))
    bl_obj.append({"nombre": o["nombre"], "raiz": o["raiz"],
                   "loc": r(a_blender(o["pivote"] - raices[o["raiz"]])),
                   "v": v, "f": p, "mats": usados, "fm": [usados.index(x) for x in m]})
bl_anc = []
for k, a in anclas.items():
    raiz = R_ASI if k == "ANC_Vista" and False else R_CON
    bl_anc.append({"nombre": k, "raiz": raiz, "loc": r(a_blender(a["pos"]))})
plantilla = open(os.path.join(os.path.dirname(__file__), "plantilla_blender.py"), encoding="utf-8").read()
codigo = (plantilla
          .replace("__OBJETOS__", json.dumps(bl_obj, separators=(",", ":")))
          .replace("__ANCLAJES__", json.dumps(bl_anc, separators=(",", ":")))
          .replace("__RAICES__", json.dumps({k: r(a_blender(v)) for k, v in raices.items()}))
          .replace("__MATERIALES__", json.dumps(MATERIALES)))
with open(os.path.join(OUT, "crear_consola_pilotaje_v0.py"), "w", encoding="utf-8") as f:
    f.write(codigo)

# ----------------------------------------------------------------------------- resumen para el README y las referencias
resumen = {
    "ancho_total": float(mx[0] - mn[0]), "fondo_total": float(mx[2] - mn[2]), "alto_total": float(mx[1] - mn[1]),
    "min": r(mn), "max": r(mx),
    "objetos": [o["nombre"] for o in objetos],
    "tris": {o["nombre"]: sum(len(c) - 2 for _, pts in o["solidos"] for c in caras(pts)[1]) for o in objetos},
    "anclas": {k: r(a["pos"]) for k, a in anclas.items()},
    "ang_mesa": math.degrees(ang_mesa), "ang_atril": math.degrees(math.atan2(Y_TOPE - Y_MESA_TRAS, Z_ATRIL - Z_MESA)),
}
with open(os.path.join(OUT, "_resumen.json"), "w") as f: json.dump(resumen, f, indent=1)
import pickle
pickle.dump({"objetos": objetos, "MATERIALES": MATERIALES, "anclas": anclas, "VISTA": VISTA}, open(os.path.join(OUT, "_geo.pkl"), "wb"))
print(json.dumps({k: resumen[k] for k in ("ancho_total", "fondo_total", "alto_total", "min", "max", "ang_mesa", "ang_atril")}))
print(sum(resumen["tris"].values()), "triangulos")
