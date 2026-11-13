using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// COMPROBACIÓN AUTOMÁTICA con las colisiones reales de la escena, al iniciar (SOLO LÓGICA). Usa consultas de física
    /// con el tamaño del personaje (radio 0,25 m), con las puertas colocadas al instante, y al final deja el estado inicial (aislamiento abierto, el resto cerrado):
    ///   · Cada pasadizo abierto se atraviesa sin chocar; los pasadizos amplios, también pegado a paredes y esquinas.
    ///   · Cada paso con puertas (también las de aislamiento): cerrado bloquea; abierto se atraviesa (también entre las
    ///     dos puertas de una unión y, en los pasadizos amplios, pegado a paredes y esquinas).
    ///   · Las escotillas cerradas hacia el exterior y la unión estructural bloquean.
    ///   · Los recorridos entre los cuatro puestos y hacia esclusa, hábitat, bodega, sistemas y lanzadera están libres.
    ///   · Cada panel se ve y se alcanza con la mirada desde delante.
    /// Es una comprobación automática, NO una prueba del equipo: no garantiza que el control resulte cómodo ni que no haya
    /// enganches al girar el cuerpo.
    /// </summary>
    public static class ComprobacionPasos
    {
        public class Resultado
        {
            public int correctas;
            public List<string> problemas = new List<string>();
        }

        const float R = PersonajeFlotante.Radio;

        static bool Libre(Vector3 a, Vector3 b, Collider ignorar, out string choque)
        {
            choque = "";
            foreach (Collider c in Physics.OverlapCapsule(a, b, R, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c == ignorar || (ignorar != null && c.attachedRigidbody != null && c.attachedRigidbody == ignorar.attachedRigidbody)) continue;
                choque = c.name + (c.transform.parent != null ? " · " + c.transform.parent.name : "");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Añade, antes y después de cada escotilla, un punto 1 m dentro del módulo en el eje de la escotilla: así el
        /// recorrido entra de frente por la escotilla, como lo haría una persona, y no en diagonal a través de la pared.
        /// Los puntos de RevisionDistribucion.Recorrido son: inicio, (puerto de salida, puerto de llegada)…, final.
        /// </summary>
        static List<Vector3> DeFrente(List<Vector3> pts)
        {
            if (pts.Count < 4) return pts;
            List<Vector3> r = new List<Vector3> { pts[0] };
            for (int i = 1; i + 1 < pts.Count - 1; i += 2)
            {
                Vector3 sale = pts[i], entra = pts[i + 1], u = (entra - sale).normalized;
                r.Add(sale - u * 1.0f); r.Add(sale); r.Add(entra); r.Add(entra + u * 1.0f);
            }
            r.Add(pts[pts.Count - 1]);
            return r;
        }

        /// <summary>Barrido de una esfera del radio del personaje a lo largo de un recorrido (el cuerpo sigue a la cabeza).</summary>
        static bool RecorridoLibre(List<Vector3> pts, Collider ignorar, out string choque)
        {
            pts = DeFrente(pts);
            choque = "";
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1];
                float dist = Vector3.Distance(a, b);
                if (dist < 1e-3f) continue;
                foreach (RaycastHit h in Physics.SphereCastAll(a, R, (b - a) / dist, dist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider == ignorar || (ignorar != null && h.collider.attachedRigidbody != null && h.collider.attachedRigidbody == ignorar.attachedRigidbody)) continue;
                    choque = h.collider.name + (h.collider.transform.parent != null ? " · " + h.collider.transform.parent.name : "") + $" (tramo {i + 1})";
                    return false;
                }
                if (!Libre(a, a, ignorar, out choque)) { choque += $" (punto {i + 1})"; return false; }
            }
            return true;
        }

        static void Paso(DefinicionNaveModular d, string ma, string pa, string mb, string pb, out Vector3 a, out Vector3 b)
        {
            ModuloNave A = d.Modulo(ma), B = mb != null ? d.Modulo(mb) : null;
            PuertoModulo PA = A.Puerto(pa);
            Vector3 n = A.DireccionAMundo(PA.normal).normalized;
            Vector3 wa = A.AMundo(PA.posicion);
            Vector3 wb = B != null ? B.AMundo(B.Puerto(pb).posicion) : wa + n * d.largoAdaptador;
            // Desde 0,7 m dentro de A hasta 0,7 m dentro de B, por el centro de la escotilla
            a = wa - n * 0.7f;
            b = wb + n * 0.7f;
        }

        public static Resultado Ejecutar(DefinicionNaveModular d, DefinicionCirculacion c, List<PuertasUnion> uniones, Collider personaje)
        {
            Resultado r = new Resultado();
            void Ok() => r.correctas++;

            // Todas cerradas
            foreach (PuertasUnion u in uniones) u.FijarParaComprobacion(false);
            Physics.SyncTransforms();
            foreach (PasoNave p in c.pasos)
            {
                ModuloNave A = d.Modulo(p.moduloA);
                if (A == null || A.Puerto(p.puertoA) == null) continue;
                Paso(d, p.moduloA, p.puertoA, p.moduloB, p.puertoB, out Vector3 a, out Vector3 b);
                bool libre = Libre(a, b, personaje, out string choque);
                switch (p.tipo)
                {
                    case TipoPaso.Abierto:
                        if (libre) Ok(); else r.problemas.Add($"{p.nota}: el pasadizo abierto choca con {choque}");
                        // Pasadizos amplios: también se pasa pegado a cada pared y esquina (sin bordes invisibles)
                        ConexionModulos con = d.ConexionDe(p.moduloA, p.puertoA);
                        PuertoModulo pp = A.Puerto(p.puertoA);
                        if (con != null && con.pasadizo)
                        {
                            Vector3 dir = (b - a).normalized;
                            int k = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) && Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? 0 : Mathf.Abs(dir.y) >= Mathf.Abs(dir.z) ? 1 : 2;
                            Vector3 e1 = k == 0 ? Vector3.up : Vector3.right, e2 = k == 2 ? Vector3.up : Vector3.forward;
                            float off = pp.lado / 2f - R - 0.1f;
                            foreach (Vector2 o in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1) })
                            {
                                Vector3 desp = (e1 * o.x + e2 * o.y) * off;
                                if (Libre(a + desp, b + desp, personaje, out string ch2)) Ok();
                                else r.problemas.Add($"{p.nota}: pegado a la pared ({o.x:+0;-0;0}, {o.y:+0;-0;0}) choca con {ch2}");
                            }
                        }
                        break;
                    case TipoPaso.Aislamiento:
                    case TipoPaso.UnionSeparable:
                    case TipoPaso.PuertaEsclusa:
                    case TipoPaso.CerradoExterior:
                    case TipoPaso.Estructural:
                        if (!libre) Ok(); else r.problemas.Add($"{p.nota}: cerrado debería bloquear el paso y no lo bloquea");
                        break;
                }
            }

            // Todas abiertas
            foreach (PuertasUnion u in uniones) u.FijarParaComprobacion(true);
            Physics.SyncTransforms();
            foreach (PasoNave p in c.pasos)
            {
                if (p.tipo != TipoPaso.UnionSeparable && p.tipo != TipoPaso.PuertaEsclusa && p.tipo != TipoPaso.Aislamiento) continue;
                Paso(d, p.moduloA, p.puertoA, p.moduloB, p.puertoB, out Vector3 a, out Vector3 b);
                if (Libre(a, b, personaje, out string choque)) Ok();
                else r.problemas.Add($"{p.nota}: abierta debería dejar pasar y choca con {choque}");
                // Pasadizos amplios con la puerta de aislamiento abierta: también pegado a paredes y esquinas
                ConexionModulos con2 = d.ConexionDe(p.moduloA, p.puertoA);
                PuertoModulo pp2 = d.Modulo(p.moduloA).Puerto(p.puertoA);
                if (con2 != null && con2.pasadizo)
                {
                    Vector3 dir = (b - a).normalized;
                    int k = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) && Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? 0 : Mathf.Abs(dir.y) >= Mathf.Abs(dir.z) ? 1 : 2;
                    Vector3 e1 = k == 0 ? Vector3.up : Vector3.right, e2 = k == 2 ? Vector3.up : Vector3.forward;
                    float off = pp2.lado / 2f - R - 0.1f;
                    foreach (Vector2 o in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1) })
                    {
                        Vector3 desp = (e1 * o.x + e2 * o.y) * off;
                        if (Libre(a + desp, b + desp, personaje, out string ch2)) Ok();
                        else r.problemas.Add($"{p.nota}: abierta, pegado a la pared ({o.x:+0;-0;0}, {o.y:+0;-0;0}) choca con {ch2}");
                    }
                }
            }

            // Recorridos (con las puertas abiertas)
            var destinos = new List<(string nombre, string modulo, Vector3 punto)>();
            foreach (PuestoExpedicion p in d.puestos) destinos.Add(("Puesto de " + p.rol, p.modulo, p.posicion));
            foreach (VistaInterior v in d.vistas) if (v.nombre.StartsWith("Esclusa")) destinos.Add(("Esclusa", v.modulo, v.posicion));
            destinos.Add(("Hábitat (camarotes)", "HA", new Vector3(0f, 0f, 5.8f)));
            destinos.Add(("Bodega", "BO", new Vector3(0f, 0f, 3.0f)));
            destinos.Add(("Sistemas", "SI", new Vector3(0f, 0f, 3.0f)));
            destinos.Add(("Lanzadera (asientos)", "LZ", new Vector3(0f, 0.6f, 8.2f)));
            Vector3 mesa = new Vector3(0f, 0f, 2.8f);
            foreach (var de in destinos)
            {
                RevisionDistribucion.Ruta ru = RevisionDistribucion.Recorrido(d, "NC", mesa, de.modulo, de.punto);
                if (ru.puntos.Count == 0) { r.problemas.Add($"Sin recorrido hasta {de.nombre}"); continue; }
                if (RecorridoLibre(ru.puntos, personaje, out string choque)) Ok();
                else r.problemas.Add($"Recorrido zona común → {de.nombre}: choca con {choque}");
            }
            for (int i = 0; i < d.puestos.Count; i++)
                for (int j = i + 1; j < d.puestos.Count; j++)
                {
                    PuestoExpedicion a = d.puestos[i], b = d.puestos[j];
                    RevisionDistribucion.Ruta ru = RevisionDistribucion.Recorrido(d, a.modulo, a.posicion, b.modulo, b.posicion);
                    if (ru.puntos.Count > 0 && RecorridoLibre(ru.puntos, personaje, out string choque)) Ok();
                    else r.problemas.Add($"Recorrido {a.rol} → {b.rol}: bloqueado");
                }

            // Paneles: se alcanzan con la mirada desde su punto de uso, con las puertas abiertas y cerradas
            void Paneles(string estado)
            {
                foreach (UnionConPuertas u in c.uniones)
                    foreach (PanelNave pn in u.paneles)
                    {
                        ModuloNave m = d.Modulo(pn.modulo);
                        if (m == null) continue;
                        Vector3 centro = m.AMundo(pn.posicion), ojo = m.AMundo(pn.puntoUso);
                        Vector3 dir = centro - ojo;
                        bool ve = Physics.Raycast(ojo, dir.normalized, out RaycastHit h, dir.magnitude + 0.2f, ~0, QueryTriggerInteraction.Ignore) && h.collider.GetComponent<PanelPuerta>() != null;
                        bool sitio = Libre(ojo, ojo, personaje, out string choque);
                        if (ve && sitio) Ok();
                        else r.problemas.Add($"Panel de {u.nombre} en {m.nombre} ({estado}): " + (!sitio ? "no hay sitio delante (" + choque + ")" : "no se alcanza con la mirada"));
                    }
            }
            Paneles("puertas abiertas");

            foreach (PuertasUnion u in uniones) u.FijarParaComprobacion(false);
            Physics.SyncTransforms();
            Paneles("puertas cerradas");
            // Estado inicial: aislamiento abierto, el resto cerrado
            foreach (PuertasUnion u in uniones) u.FijarParaComprobacion(u.Datos.abiertaAlInicio);
            Physics.SyncTransforms();
            return r;
        }
    }
}
