using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Revisión de la distribución modular (SOLO LÓGICA). Comprueba con los datos de DefinicionNaveModular:
    ///   1. Que ningún módulo ni elemento exterior se solapa con otro (cajas exactas: todos los ejes son ±X, ±Y, ±Z).
    ///   2. Que cada conexión une dos puertos enfrentados, a la distancia del adaptador y con la misma escotilla.
    ///   3. Que cada adaptador o pasadizo cabe en la cara del módulo y cada escotilla en su tapa.
    ///   3b. Pasadizos permanentes: hueco igual al paso libre, pared suficiente a los lados del hueco, el pasadizo cabe
    ///       en la tapa, y ninguna conexión invade otro módulo, un elemento exterior u otra conexión.
    ///   4. Que cada equipo está dentro del casco, fuera del paso libre, sin tapar escotillas y sin chocar con otro.
    ///   5. Que el punto de cada puesto queda libre.
    /// También calcula envolvente, volúmenes y recorridos entre puestos. Es la misma revisión que se hizo antes de
    /// construir el bloque; NO comprueba colisiones de personajes ni el movimiento real (no los hay en esta prueba).
    /// </summary>
    public static class RevisionDistribucion
    {
        public class VolumenModulo
        {
            public ModuloNave modulo;
            public float presurizadoM3, equiposM3, libreM3, circulacionM3;
        }

        public class Ruta
        {
            public string desde, hasta;
            public float metros;
            public List<Vector3> puntos = new List<Vector3>();
        }

        public class Resultado
        {
            public int comprobaciones;
            public List<string> problemas = new List<string>();
            public List<string> holguras = new List<string>();
            public Bounds envolventeModulos, envolventeTotal;
            public List<VolumenModulo> volumenes = new List<VolumenModulo>();
            public float presurizadoM3, libreM3, circulacionM3, adaptadoresM3;
            public float presurizadoSinLanzaderaM3, libreSinLanzaderaM3;
            public List<Ruta> rutas = new List<Ruta>();
        }

        const float Tol = 1e-4f;
        static readonly float Raiz2 = Mathf.Sqrt(2f);
        static readonly float Tan225 = Mathf.Tan(22.5f * Mathf.Deg2Rad);

        public static float AreaOctogono(float entreCaras) => 2f * (Raiz2 - 1f) * entreCaras * entreCaras;

        public static Resultado Revisar(DefinicionNaveModular d)
        {
            Resultado r = new Resultado();
            Solapamientos(d, r);
            Conexiones(d, r);
            Puertos(d, r);
            Pasadizos(d, r);
            Equipos(d, r);
            Puestos(d, r);
            Volumenes(d, r);
            Rutas(d, r);
            return r;
        }

        // ------------------------------------------------------------------ 1. Solapamientos

        struct Caja { public string nombre; public Bounds b; }

        static float Separacion(Bounds a, Bounds b)
        {
            Vector3 s = Vector3.Max(b.min - a.max, a.min - b.max);
            return Mathf.Max(s.x, Mathf.Max(s.y, s.z));   // > 0: separadas
        }

        static void Solapamientos(DefinicionNaveModular d, Resultado r)
        {
            var cajas = new List<Caja>();
            foreach (ModuloNave m in d.modulos) cajas.Add(new Caja { nombre = m.nombre, b = m.Limites });
            foreach (ElementoExterior e in d.exteriores)
            {
                Bounds b = new Bounds(); b.SetMinMax(e.min, e.max);
                cajas.Add(new Caja { nombre = e.nombre, b = b });
            }

            HashSet<string> unidos = new HashSet<string>();
            foreach (ElementoExterior e in d.exteriores)
            {
                ModuloNave m = d.Modulo(e.unidoA);
                if (m != null) unidos.Add(Par(m.nombre, e.nombre));
            }
            HashSet<string> conectados = new HashSet<string>();
            foreach (ConexionModulos c in d.conexiones)
            {
                ModuloNave a = d.Modulo(c.moduloA), b = d.Modulo(c.moduloB);
                if (a != null && b != null) conectados.Add(Par(a.nombre, b.nombre));
            }

            var holg = new List<(float s, string t)>();
            for (int i = 0; i < cajas.Count; i++)
                for (int j = i + 1; j < cajas.Count; j++)
                {
                    float s = Separacion(cajas[i].b, cajas[j].b);
                    string par = Par(cajas[i].nombre, cajas[j].nombre);
                    if (unidos.Contains(par))
                    {
                        if (s < -0.001f) r.problemas.Add($"Solape de pieza unida: {cajas[i].nombre} / {cajas[j].nombre}");
                        else r.comprobaciones++;
                        continue;
                    }
                    if (s <= 0f) r.problemas.Add($"Solape: {cajas[i].nombre} / {cajas[j].nombre}");
                    else
                    {
                        r.comprobaciones++;
                        if (!conectados.Contains(par)) holg.Add((s, $"{s:0.00} m entre {cajas[i].nombre} y {cajas[j].nombre}"));
                    }
                }
            holg.Sort((a, b) => a.s.CompareTo(b.s));
            for (int i = 0; i < holg.Count && i < 4; i++) r.holguras.Add(holg[i].t);

            Bounds mods = new Bounds(); bool primero = true;
            foreach (ModuloNave m in d.modulos)
            {
                if (primero) { mods = m.Limites; primero = false; } else mods.Encapsulate(m.Limites);
            }
            r.envolventeModulos = mods;
            Bounds tot = mods;
            foreach (ElementoExterior e in d.exteriores) { tot.Encapsulate(e.min); tot.Encapsulate(e.max); }
            r.envolventeTotal = tot;
        }

        static string Par(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;

        // ------------------------------------------------------------------ 2. Conexiones

        static void Conexiones(DefinicionNaveModular d, Resultado r)
        {
            foreach (ConexionModulos c in d.conexiones)
            {
                ModuloNave a = d.Modulo(c.moduloA), b = d.Modulo(c.moduloB);
                PuertoModulo pa = a?.Puerto(c.puertoA), pb = b?.Puerto(c.puertoB);
                string n = $"{c.moduloA}.{c.puertoA} – {c.moduloB}.{c.puertoB}";
                if (pa == null || pb == null) { r.problemas.Add("Conexión con un módulo o puerto que no existe: " + n); continue; }
                Vector3 wa = a.AMundo(pa.posicion), wb = b.AMundo(pb.posicion);
                Vector3 na = a.DireccionAMundo(pa.normal), nb = b.DireccionAMundo(pb.normal);
                if ((wa + na * d.LargoConexion(c) - wb).magnitude > 0.01f) r.problemas.Add("Conexión desalineada: " + n);
                else if (Vector3.Dot(na, nb) > -0.999f) r.problemas.Add("Puertos no enfrentados: " + n);
                else if (Mathf.Abs(pa.lado - pb.lado) > Tol) r.problemas.Add("Escotillas de distinto tamaño: " + n);
                else r.comprobaciones++;
            }
        }

        // ------------------------------------------------------------------ 3. Puertos

        static void Puertos(DefinicionNaveModular d, Resultado r)
        {
            foreach (ModuloNave m in d.modulos)
                foreach (PuertoModulo p in m.puertos)
                {
                    if (m.EsCaja) { r.comprobaciones++; continue; }
                    float adaptador = DefinicionNaveModular.ExteriorConexion(d.ConexionDe(m.id, p.id), p.lado);
                    if (!p.EnTapa)
                    {
                        float cara = m.diametroExterior * Tan225;
                        if (adaptador > cara + Tol)
                            r.problemas.Add($"Puerto {m.id}.{p.id}: el adaptador ({adaptador:0.00} m) no cabe en la cara ({cara:0.00} m)");
                        else if (p.posicion.z - adaptador / 2f < 0.3f || p.posicion.z + adaptador / 2f > m.largo - 0.3f)
                            r.problemas.Add($"Puerto {m.id}.{p.id}: demasiado cerca del extremo del módulo");
                        else r.comprobaciones++;
                    }
                    else
                    {
                        float dInt = m.diametroInterior > 0f ? m.diametroInterior : m.diametroExterior;
                        if (p.lado / 2f * Raiz2 > dInt / 2f) r.problemas.Add($"Escotilla {m.id}.{p.id}: no cabe en la tapa");
                        else if (adaptador > m.diametroExterior) r.problemas.Add($"Adaptador {m.id}.{p.id}: mayor que el módulo");
                        else r.comprobaciones++;
                    }
                }
        }

        // ------------------------------------------------------------------ 3b. Pasadizos y volumen de las conexiones

        /// <summary>Caja exterior de una conexión (pasadizo o adaptador) en coordenadas de la nave.</summary>
        public static Bounds CajaConexion(DefinicionNaveModular d, ConexionModulos c)
        {
            ModuloNave a = d.Modulo(c.moduloA), b = d.Modulo(c.moduloB);
            PuertoModulo pa = a?.Puerto(c.puertoA), pb = b?.Puerto(c.puertoB);
            if (pa == null || pb == null) return new Bounds();
            Vector3 wa = a.AMundo(pa.posicion), wb = b.AMundo(pb.posicion);
            float ext = DefinicionNaveModular.ExteriorConexion(c, pa.lado);
            Vector3 n = wb - wa;
            int k = Mathf.Abs(n.x) >= Mathf.Abs(n.y) && Mathf.Abs(n.x) >= Mathf.Abs(n.z) ? 0 : Mathf.Abs(n.y) >= Mathf.Abs(n.z) ? 1 : 2;
            Vector3 lo = Vector3.Min(wa, wb), hi = Vector3.Max(wa, wb);
            for (int q = 0; q < 3; q++) if (q != k) { lo[q] -= ext / 2f; hi[q] += ext / 2f; }
            Bounds bb = new Bounds();
            bb.SetMinMax(lo, hi);
            return bb;
        }

        static bool Cruzan(Bounds a, Bounds b) =>
            a.min.x < b.max.x - Tol && b.min.x < a.max.x - Tol && a.min.y < b.max.y - Tol && b.min.y < a.max.y - Tol && a.min.z < b.max.z - Tol && b.min.z < a.max.z - Tol;

        static void Pasadizos(DefinicionNaveModular d, Resultado r)
        {
            var validas = new List<ConexionModulos>();
            foreach (ConexionModulos c in d.conexiones)
                if (d.Modulo(c.moduloA)?.Puerto(c.puertoA) != null && d.Modulo(c.moduloB)?.Puerto(c.puertoB) != null) validas.Add(c);

            foreach (ConexionModulos c in validas)
            {
                string nom = $"{c.moduloA}.{c.puertoA} – {c.moduloB}.{c.puertoB}";
                if (c.pasadizo)
                {
                    foreach ((string mid, string pid) in new[] { (c.moduloA, c.puertoA), (c.moduloB, c.puertoB) })
                    {
                        ModuloNave m = d.Modulo(mid);
                        PuertoModulo p = m.Puerto(pid);
                        if (!p.escotilla) { r.problemas.Add($"Pasadizo {nom}: el puerto {mid}.{pid} no tiene hueco"); continue; }
                        if (!p.EnTapa)
                        {
                            float caraInterior = m.diametroInterior * Tan225;
                            if (caraInterior - p.lado < 0.2f - Tol)
                                r.problemas.Add($"Pasadizo {nom}: el hueco de {p.lado:0.0} m en {m.nombre} deja menos de 0,1 m de pared a cada lado");
                            else r.comprobaciones++;
                        }
                        else
                        {
                            float h = p.lado / 2f + DefinicionNaveModular.GrosorPasadizo, R = m.diametroExterior / 2f;
                            if (h > R + Tol || 2f * h / Raiz2 > R + Tol) r.problemas.Add($"Pasadizo {nom}: no cabe en la tapa de {m.nombre}");
                            else r.comprobaciones++;
                        }
                    }
                }
                Bounds caja = CajaConexion(d, c);
                foreach (ModuloNave m in d.modulos)
                {
                    if (m.id == c.moduloA || m.id == c.moduloB) continue;
                    if (Cruzan(caja, m.Limites)) r.problemas.Add($"La conexión {nom} invade {m.nombre}");
                    else r.comprobaciones++;
                }
                foreach (ElementoExterior e in d.exteriores)
                {
                    Bounds b = new Bounds(); b.SetMinMax(e.min, e.max);
                    if (Cruzan(caja, b)) r.problemas.Add($"La conexión {nom} invade {e.nombre}");
                    else r.comprobaciones++;
                }
            }
            for (int i = 0; i < validas.Count; i++)
                for (int j = i + 1; j < validas.Count; j++)
                {
                    if (Cruzan(CajaConexion(d, validas[i]), CajaConexion(d, validas[j])))
                        r.problemas.Add($"Conexiones solapadas: {validas[i].moduloA}–{validas[i].moduloB} / {validas[j].moduloA}–{validas[j].moduloB}");
                    else r.comprobaciones++;
                }
        }

        // ------------------------------------------------------------------ 4. Equipos

        static bool Interseca(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
        {
            const float t = 1e-4f;
            return a0.x < b1.x - t && b0.x < a1.x - t && a0.y < b1.y - t && b0.y < a1.y - t && a0.z < b1.z - t && b0.z < a1.z - t;
        }

        /// <summary>Zonas que deben quedar libres delante de cada escotilla (coordenadas locales del módulo).</summary>
        public static List<(string id, Vector3 min, Vector3 max)> Corredores(ModuloNave m)
        {
            var lista = new List<(string id, Vector3 min, Vector3 max)>();
            foreach (PuertoModulo p in m.puertos)
            {
                if (!p.escotilla) continue;
                float h = p.lado / 2f + 0.15f;
                if (p.EnTapa)
                {
                    float z0 = p.normal.z < 0f ? 0f : m.largo - 1.2f;
                    float z1 = p.normal.z < 0f ? 1.2f : m.largo;
                    lista.Add((p.id, new Vector3(-h, -h, z0), new Vector3(h, h, z1)));
                    continue;
                }
                float z = p.posicion.z;
                if (m.EsCaja)
                {
                    lista.Add((p.id, new Vector3(-h, 0f, z - h), new Vector3(h, m.caja.y / 2f, z + h)));
                    continue;
                }
                float rInt = (m.diametroInterior > 0f ? m.diametroInterior : m.diametroExterior) / 2f;
                Vector3 lo = new Vector3(-h, -h, z - h), hi = new Vector3(h, h, z + h);
                Vector3 n = p.normal;
                int k = Mathf.Abs(n.x) >= Mathf.Abs(n.y) ? 0 : 1;
                if (n[k] > 0f) { hi[k] = rInt; lo[k] = 0f; } else { lo[k] = -rInt; hi[k] = 0f; }
                lista.Add((p.id, lo, hi));
            }
            return lista;
        }

        static bool DentroDelCasco(ModuloNave m, EquipoModulo e)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 q = new Vector3((i & 1) == 0 ? e.Min.x : e.Max.x, (i & 2) == 0 ? e.Min.y : e.Max.y, (i & 4) == 0 ? e.Min.z : e.Max.z);
                if (m.EsCaja)
                {
                    if (q.x < ModuloNave.LanzaderaInteriorMin.x - Tol || q.x > ModuloNave.LanzaderaInteriorMax.x + Tol ||
                        q.y < ModuloNave.LanzaderaInteriorMin.y - Tol || q.y > ModuloNave.LanzaderaInteriorMax.y + Tol ||
                        q.z < ModuloNave.LanzaderaInteriorMin.z - Tol || q.z > ModuloNave.LanzaderaInteriorMax.z + Tol) return false;
                    continue;
                }
                float rr = m.diametroInterior / 2f;
                if (Mathf.Abs(q.x) > rr + Tol || Mathf.Abs(q.y) > rr + Tol || (Mathf.Abs(q.x) + Mathf.Abs(q.y)) / Raiz2 > rr + Tol) return false;
                if (q.z < 0.05f - Tol || q.z > m.largo - 0.05f + Tol) return false;
            }
            return true;
        }

        static void Equipos(DefinicionNaveModular d, Resultado r)
        {
            foreach (ModuloNave m in d.modulos)
            {
                if (!m.Presurizado) continue;
                var corredores = Corredores(m);
                for (int i = 0; i < m.equipos.Count; i++)
                {
                    EquipoModulo e = m.equipos[i];
                    if (!DentroDelCasco(m, e)) r.problemas.Add($"Equipo fuera del casco: {m.nombre} · {e.nombre}");
                    else r.comprobaciones++;
                    if (m.pasoLibre.x > 0f)
                    {
                        Vector3 p0 = new Vector3(-m.pasoLibre.x / 2f, -m.pasoLibre.y / 2f, m.tramoPaso.x);
                        Vector3 p1 = new Vector3(m.pasoLibre.x / 2f, m.pasoLibre.y / 2f, m.tramoPaso.y);
                        if (Interseca(e.Min, e.Max, p0, p1)) r.problemas.Add($"Equipo dentro del paso libre: {m.nombre} · {e.nombre}");
                        else r.comprobaciones++;
                    }
                    foreach (var c in corredores)
                    {
                        if (Interseca(e.Min, e.Max, c.min, c.max)) r.problemas.Add($"Equipo delante de la escotilla «{c.id}»: {m.nombre} · {e.nombre}");
                        else r.comprobaciones++;
                    }
                    for (int j = i + 1; j < m.equipos.Count; j++)
                    {
                        if (Interseca(e.Min, e.Max, m.equipos[j].Min, m.equipos[j].Max))
                            r.problemas.Add($"Equipos solapados: {m.nombre} · {e.nombre} / {m.equipos[j].nombre}");
                        else r.comprobaciones++;
                    }
                }
                if (m.pasoLibre.x > 0f && !m.EsCaja)
                {
                    float rr = m.diametroInterior / 2f;
                    if (m.pasoLibre.x / 2f > rr || (m.pasoLibre.x / 2f + m.pasoLibre.y / 2f) / Raiz2 > rr + Tol)
                        r.problemas.Add($"El paso libre de {m.nombre} no cabe en su interior");
                    else r.comprobaciones++;
                }
            }
        }

        static void Puestos(DefinicionNaveModular d, Resultado r)
        {
            foreach (PuestoExpedicion p in d.puestos)
            {
                ModuloNave m = d.Modulo(p.modulo);
                if (m == null) { r.problemas.Add($"Puesto {p.rol}: el módulo {p.modulo} no existe"); continue; }
                Vector3 a = p.posicion - Vector3.one * 0.25f, b = p.posicion + Vector3.one * 0.25f;
                bool libre = true;
                foreach (EquipoModulo e in m.equipos)
                    if (Interseca(a, b, e.Min, e.Max)) { r.problemas.Add($"Puesto {p.rol}: el punto queda dentro de «{e.nombre}»"); libre = false; }
                if (libre) r.comprobaciones++;
            }
        }

        // ------------------------------------------------------------------ Volúmenes

        static void Volumenes(DefinicionNaveModular d, Resultado r)
        {
            foreach (ModuloNave m in d.modulos)
            {
                if (!m.Presurizado) continue;
                VolumenModulo v = new VolumenModulo { modulo = m };
                if (m.EsCaja)
                {
                    Vector3 s = ModuloNave.LanzaderaInteriorMax - ModuloNave.LanzaderaInteriorMin;
                    v.presurizadoM3 = s.x * s.y * s.z;
                }
                else v.presurizadoM3 = AreaOctogono(m.diametroInterior) * m.largo;
                foreach (EquipoModulo e in m.equipos) v.equiposM3 += e.tamano.x * e.tamano.y * e.tamano.z;
                v.libreM3 = v.presurizadoM3 - v.equiposM3;
                v.circulacionM3 = m.pasoLibre.x > 0f ? m.pasoLibre.x * m.pasoLibre.y * (m.tramoPaso.y - m.tramoPaso.x) : v.libreM3;
                r.volumenes.Add(v);
                r.presurizadoM3 += v.presurizadoM3; r.libreM3 += v.libreM3; r.circulacionM3 += v.circulacionM3;
                if (!m.EsCaja) { r.presurizadoSinLanzaderaM3 += v.presurizadoM3; r.libreSinLanzaderaM3 += v.libreM3; }
            }
            foreach (ConexionModulos c in d.conexiones)
            {
                PuertoModulo p = d.Modulo(c.moduloA)?.Puerto(c.puertoA);
                if (p == null || !p.escotilla) continue;
                r.adaptadoresM3 += c.pasadizo ? p.lado * p.lado * d.LargoConexion(c)
                                              : AreaOctogono(DefinicionNaveModular.InteriorAdaptador(p.lado)) * d.LargoConexion(c);
            }
            r.presurizadoM3 += r.adaptadoresM3; r.libreM3 += r.adaptadoresM3; r.circulacionM3 += r.adaptadoresM3;
            r.presurizadoSinLanzaderaM3 += r.adaptadoresM3; r.libreSinLanzaderaM3 += r.adaptadoresM3;
        }

        // ------------------------------------------------------------------ Recorridos

        /// <summary>Recorrido flotando de un punto (local de un módulo) a otro, pasando por escotillas.</summary>
        public static Ruta Recorrido(DefinicionNaveModular d, string moduloA, Vector3 puntoA, string moduloB, Vector3 puntoB)
        {
            // Grafo: módulo -> (vecino, puerto propio, puerto del vecino); solo uniones con escotilla
            var vecinos = new Dictionary<string, List<(string m, string pPropio, string pVecino)>>();
            foreach (ConexionModulos c in d.conexiones)
            {
                PuertoModulo pa = d.Modulo(c.moduloA)?.Puerto(c.puertoA), pb = d.Modulo(c.moduloB)?.Puerto(c.puertoB);
                if (pa == null || pb == null || !pa.escotilla || !pb.escotilla) continue;
                if (!vecinos.ContainsKey(c.moduloA)) vecinos[c.moduloA] = new List<(string m, string pPropio, string pVecino)>();
                if (!vecinos.ContainsKey(c.moduloB)) vecinos[c.moduloB] = new List<(string m, string pPropio, string pVecino)>();
                vecinos[c.moduloA].Add((c.moduloB, c.puertoA, c.puertoB));
                vecinos[c.moduloB].Add((c.moduloA, c.puertoB, c.puertoA));
            }
            var previo = new Dictionary<string, (string desde, string pDesde, string pHasta)> { [moduloA] = (null, null, null) };
            Queue<string> cola = new Queue<string>();
            cola.Enqueue(moduloA);
            while (cola.Count > 0)
            {
                string x = cola.Dequeue();
                if (!vecinos.ContainsKey(x)) continue;
                foreach (var v in vecinos[x])
                    if (!previo.ContainsKey(v.m)) { previo[v.m] = (x, v.pPropio, v.pVecino); cola.Enqueue(v.m); }
            }
            Ruta ruta = new Ruta();
            if (!previo.ContainsKey(moduloB)) return ruta;
            var tramos = new List<(string x, string px, string y, string py)>();
            string actual = moduloB;
            while (previo[actual].desde != null)
            {
                var pr = previo[actual];
                tramos.Insert(0, (pr.desde, pr.pDesde, actual, pr.pHasta));
                actual = pr.desde;
            }
            ruta.puntos.Add(d.Modulo(moduloA).AMundo(puntoA));
            foreach (var t in tramos)
            {
                ModuloNave mx = d.Modulo(t.x), my = d.Modulo(t.y);
                ruta.puntos.Add(mx.AMundo(mx.Puerto(t.px).posicion));
                ruta.puntos.Add(my.AMundo(my.Puerto(t.py).posicion));
            }
            ruta.puntos.Add(d.Modulo(moduloB).AMundo(puntoB));
            for (int i = 0; i + 1 < ruta.puntos.Count; i++) ruta.metros += Vector3.Distance(ruta.puntos[i], ruta.puntos[i + 1]);
            return ruta;
        }

        static void Rutas(DefinicionNaveModular d, Resultado r)
        {
            for (int i = 0; i < d.puestos.Count; i++)
                for (int j = i + 1; j < d.puestos.Count; j++)
                {
                    PuestoExpedicion a = d.puestos[i], b = d.puestos[j];
                    Ruta ru = Recorrido(d, a.modulo, a.posicion, b.modulo, b.posicion);
                    ru.desde = a.rol; ru.hasta = b.rol;
                    if (ru.puntos.Count == 0) r.problemas.Add($"Sin recorrido entre {a.rol} y {b.rol}");
                    r.rutas.Add(ru);
                }
        }
    }
}
