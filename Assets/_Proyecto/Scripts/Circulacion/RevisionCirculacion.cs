using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Revisión de los pasos, puertas y paneles acordados (SOLO LÓGICA, sobre los datos). Complementa a
    /// RevisionDistribucion, que no cambia: las puertas y los paneles son elementos nuevos y se comprueban aquí.
    ///   1. Cada conexión de la EM-4X está clasificada una sola vez y con el tipo acordado; cada puerto sin módulo al otro
    ///      lado está cerrado o marcado como sin paso.
    ///   2. Cada unión separable tiene una puerta en cada módulo; la esclusa tiene su puerta interior.
    ///   3. Hojas (cerradas y abiertas) y guías dentro del casco, sin chocar con equipos; abiertas, fuera del corredor de
    ///      la escotilla (no estorban el paso).
    ///   4. Paneles dentro del casco, sin chocar con equipos, hojas, guías, corredores ni pasos libres; uno en cada lado
    ///      de la unión y a menos de 2,5 m de su escotilla (se alcanzan sin atravesar una puerta cerrada).
    /// No sustituye a la prueba con el personaje (eso lo hace ComprobacionPasos al iniciar y, sobre todo, el equipo).
    /// </summary>
    public static class RevisionCirculacion
    {
        public class Resultado
        {
            public int comprobaciones;
            public List<string> problemas = new List<string>();
            public List<string> notas = new List<string>();
        }

        const float Tol = 1e-4f;
        static readonly float Raiz2 = Mathf.Sqrt(2f);

        static bool Interseca(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1) =>
            a0.x < b1.x - Tol && b0.x < a1.x - Tol && a0.y < b1.y - Tol && b0.y < a1.y - Tol && a0.z < b1.z - Tol && b0.z < a1.z - Tol;

        static bool Dentro(ModuloNave m, Vector3 b0, Vector3 b1)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 q = new Vector3((i & 1) == 0 ? b0.x : b1.x, (i & 2) == 0 ? b0.y : b1.y, (i & 4) == 0 ? b0.z : b1.z);
                if (m.EsCaja)
                {
                    Vector3 mn = ModuloNave.LanzaderaInteriorMin, mx = ModuloNave.LanzaderaInteriorMax;
                    if (q.x < mn.x - Tol || q.x > mx.x + Tol || q.y < mn.y - Tol || q.y > mx.y + Tol || q.z < mn.z - Tol || q.z > mx.z + Tol) return false;
                    continue;
                }
                float r = m.diametroInterior / 2f;
                if (Mathf.Abs(q.x) > r + Tol || Mathf.Abs(q.y) > r + Tol || (Mathf.Abs(q.x) + Mathf.Abs(q.y)) / Raiz2 > r + Tol) return false;
                if (q.z < -Tol || q.z > m.largo + Tol) return false;
            }
            return true;
        }

        public static Resultado Revisar(DefinicionNaveModular d, DefinicionCirculacion c)
        {
            Resultado r = new Resultado();
            Clasificacion(d, c, r);
            Uniones(d, c, r);
            Puertas(d, c, r);
            PuertasEntreSi(d, c, r);
            Paneles(d, c, r);
            return r;
        }

        // ------------------------------------------------------------------ 1. Clasificación

        static void Clasificacion(DefinicionNaveModular d, DefinicionCirculacion c, Resultado r)
        {
            foreach (ConexionModulos k in d.conexiones)
            {
                int n = 0; PasoNave paso = null;
                foreach (PasoNave p in c.pasos)
                    if ((p.moduloA == k.moduloA && p.puertoA == k.puertoA && p.moduloB == k.moduloB && p.puertoB == k.puertoB) ||
                        (p.moduloA == k.moduloB && p.puertoA == k.puertoB && p.moduloB == k.moduloA && p.puertoB == k.puertoA)) { n++; paso = p; }
                string nom = $"{k.moduloA}.{k.puertoA} – {k.moduloB}.{k.puertoB}";
                if (n != 1) { r.problemas.Add($"Conexión {nom}: clasificada {n} veces (debe ser 1)"); continue; }
                PuertoModulo pa = d.Modulo(k.moduloA)?.Puerto(k.puertoA);
                bool conPaso = pa != null && pa.escotilla;
                bool tipoConPaso = paso.tipo == TipoPaso.Abierto || paso.tipo == TipoPaso.Aislamiento || paso.tipo == TipoPaso.UnionSeparable || paso.tipo == TipoPaso.PuertaEsclusa;
                if (conPaso != tipoConPaso) r.problemas.Add($"Conexión {nom}: tipo «{paso.tipo}» no coincide con su escotilla");
                else r.comprobaciones++;
            }
            foreach (ModuloNave m in d.modulos)
                foreach (PuertoModulo p in m.puertos)
                {
                    bool conectado = false;
                    foreach (ConexionModulos k in d.conexiones)
                        if ((k.moduloA == m.id && k.puertoA == p.id) || (k.moduloB == m.id && k.puertoB == p.id)) conectado = true;
                    if (conectado) continue;
                    PasoNave paso = c.PasoDe(m.id, p.id);
                    TipoPaso esperado = p.escotilla ? TipoPaso.CerradoExterior : TipoPaso.SinPaso;
                    if (paso == null || paso.tipo != esperado)
                        r.problemas.Add($"Puerto {m.id}.{p.id} sin módulo al otro lado: debe quedar como «{esperado}»");
                    else r.comprobaciones++;
                }
            foreach (PasoNave p in c.pasos)
            {
                ModuloNave a = d.Modulo(p.moduloA);
                if (a == null || a.Puerto(p.puertoA) == null) r.problemas.Add($"Paso «{p.nota}»: el puerto {p.moduloA}.{p.puertoA} no existe");
                else r.comprobaciones++;
            }
        }

        // ------------------------------------------------------------------ 2. Uniones

        static void Uniones(DefinicionNaveModular d, DefinicionCirculacion c, Resultado r)
        {
            foreach (PasoNave p in c.pasos)
            {
                if (p.tipo != TipoPaso.UnionSeparable && p.tipo != TipoPaso.PuertaEsclusa && p.tipo != TipoPaso.Aislamiento) continue;
                UnionConPuertas u = null;
                foreach (UnionConPuertas x in c.uniones)
                    if ((x.moduloA == p.moduloA && x.moduloB == p.moduloB) || (x.moduloA == p.moduloB && x.moduloB == p.moduloA)) u = x;
                if (u == null) { r.problemas.Add($"Paso «{p.nota}»: le faltan las puertas"); continue; }
                bool enA = false, enB = false, panelA = false, panelB = false;
                foreach (PuertaNave pu in u.puertas)
                {
                    bool coincide = (pu.modulo == p.moduloA && pu.puerto == p.puertoA) || (pu.modulo == p.moduloB && pu.puerto == p.puertoB);
                    if (!coincide) r.problemas.Add($"{u.nombre}: la puerta {pu.modulo}.{pu.puerto} no está en esta unión");
                    if (pu.modulo == p.moduloA) enA = true;
                    if (pu.modulo == p.moduloB) enB = true;
                }
                foreach (PanelNave pn in u.paneles)
                {
                    if (pn.modulo == p.moduloA) panelA = true;
                    if (pn.modulo == p.moduloB) panelB = true;
                }
                if (p.tipo == TipoPaso.UnionSeparable && !(enA && enB && u.puertas.Count == 2))
                    r.problemas.Add($"{u.nombre}: una unión separable lleva una puerta a cada lado");
                else if (p.tipo == TipoPaso.PuertaEsclusa && u.puertas.Count < 1)
                    r.problemas.Add($"{u.nombre}: falta la puerta interior");
                else if (p.tipo == TipoPaso.Aislamiento && (u.puertas.Count != 1 || !u.abiertaAlInicio))
                    r.problemas.Add($"{u.nombre}: el aislamiento lleva una sola puerta, abierta al empezar");
                else r.comprobaciones++;
                if (!panelA || !panelB) r.problemas.Add($"{u.nombre}: falta un panel en uno de los dos lados");
                else r.comprobaciones++;
            }
        }

        // ------------------------------------------------------------------ 3. Puertas

        static void Puertas(DefinicionNaveModular d, DefinicionCirculacion c, Resultado r)
        {
            foreach (UnionConPuertas u in c.uniones)
                foreach (PuertaNave pu in u.puertas)
                {
                    ModuloNave m = d.Modulo(pu.modulo);
                    PuertoModulo p = m?.Puerto(pu.puerto);
                    if (p == null) { r.problemas.Add($"{u.nombre}: la puerta {pu.modulo}.{pu.puerto} no existe"); continue; }
                    var corredores = RevisionDistribucion.Corredores(m);
                    var piezas = new List<(string nombre, CajaLocal caja, bool abierta)>();
                    foreach (CajaLocal h in GeometriaPuertas.Hojas(m, p, pu.etapas, 0f)) piezas.Add(("hoja cerrada", h, false));
                    foreach (CajaLocal h in GeometriaPuertas.Hojas(m, p, pu.etapas, 1f)) piezas.Add(("hoja abierta", h, true));
                    foreach (CajaLocal g in GeometriaPuertas.Guias(m, p, pu.etapas)) piezas.Add(("guía", g, false));
                    foreach (var pz in piezas)
                    {
                        string nom = $"{u.nombre} · puerta en {m.nombre} · {pz.nombre}";
                        if (!Dentro(m, pz.caja.Min, pz.caja.Max)) r.problemas.Add(nom + ": no cabe dentro del casco");
                        else r.comprobaciones++;
                        foreach (EquipoModulo e in m.equipos)
                        {
                            if (Interseca(pz.caja.Min, pz.caja.Max, e.Min, e.Max)) r.problemas.Add($"{nom}: choca con «{e.nombre}»");
                            else r.comprobaciones++;
                        }
                        if (pz.abierta)
                            foreach (var co in corredores)
                            {
                                if (Interseca(pz.caja.Min, pz.caja.Max, co.min, co.max)) r.problemas.Add($"{nom}: abierta estorba la escotilla «{co.id}»");
                                else r.comprobaciones++;
                            }
                    }
                    // Holgura lateral (informativa)
                    GeometriaPuertas.Ejes(p, out Vector3 n, out Vector3 s, out Vector3 h2);
                    List<CajaLocal> guias = GeometriaPuertas.Guias(m, p, pu.etapas);
                    float extremo = Mathf.Abs(Vector3.Dot(guias[0].tamano, s)) / 2f;
                    if (!m.EsCaja && !p.EnTapa)
                    {
                        float media = m.diametroInterior * Mathf.Tan(22.5f * Mathf.Deg2Rad) / 2f;
                        r.notas.Add($"{u.nombre} · {m.nombre}: guías hasta ±{extremo:0.00} m en una cara de ±{media:0.00} m (margen {media - extremo:0.00} m)" +
                                    (pu.etapas > 1 ? " · puerta telescópica (una hoja por lado no cabe)" : ""));
                    }
                }
        }

        /// <summary>Hojas y guías de puertas distintas del mismo módulo no se solapan (cerradas ni abiertas).</summary>
        static void PuertasEntreSi(DefinicionNaveModular d, DefinicionCirculacion c, Resultado r)
        {
            var piezas = new List<(string modulo, string puerta, CajaLocal caja)>();
            foreach (UnionConPuertas u in c.uniones)
                foreach (PuertaNave pu in u.puertas)
                {
                    ModuloNave m = d.Modulo(pu.modulo);
                    PuertoModulo p = m?.Puerto(pu.puerto);
                    if (p == null) continue;
                    string id = pu.modulo + "." + pu.puerto;
                    for (int a = 0; a <= 1; a++)
                        foreach (CajaLocal h in GeometriaPuertas.Hojas(m, p, pu.etapas, a)) piezas.Add((pu.modulo, id, h));
                    foreach (CajaLocal g in GeometriaPuertas.Guias(m, p, pu.etapas)) piezas.Add((pu.modulo, id, g));
                }
            for (int i = 0; i < piezas.Count; i++)
                for (int j = i + 1; j < piezas.Count; j++)
                {
                    if (piezas[i].modulo != piezas[j].modulo || piezas[i].puerta == piezas[j].puerta) continue;
                    if (Interseca(piezas[i].caja.Min, piezas[i].caja.Max, piezas[j].caja.Min, piezas[j].caja.Max))
                        r.problemas.Add($"Puertas solapadas en {piezas[i].modulo}: {piezas[i].puerta} / {piezas[j].puerta}");
                    else r.comprobaciones++;
                }
        }

        // ------------------------------------------------------------------ 4. Paneles

        static void Paneles(DefinicionNaveModular d, DefinicionCirculacion c, Resultado r)
        {
            foreach (UnionConPuertas u in c.uniones)
                foreach (PanelNave pn in u.paneles)
                {
                    ModuloNave m = d.Modulo(pn.modulo);
                    if (m == null) { r.problemas.Add($"{u.nombre}: panel en un módulo que no existe ({pn.modulo})"); continue; }
                    CajaLocal caja = GeometriaPuertas.Panel(pn);
                    string nom = $"{u.nombre} · panel en {m.nombre}";
                    List<string> mal = new List<string>();
                    if (!Dentro(m, caja.Min, caja.Max)) mal.Add("fuera del casco");
                    foreach (EquipoModulo e in m.equipos) if (Interseca(caja.Min, caja.Max, e.Min, e.Max)) mal.Add("choca con " + e.nombre);
                    foreach (var co in RevisionDistribucion.Corredores(m)) if (Interseca(caja.Min, caja.Max, co.min, co.max)) mal.Add("en el corredor " + co.id);
                    if (m.pasoLibre.x > 0f)
                    {
                        Vector3 p0 = new Vector3(-m.pasoLibre.x / 2f, -m.pasoLibre.y / 2f, m.tramoPaso.x);
                        Vector3 p1 = new Vector3(m.pasoLibre.x / 2f, m.pasoLibre.y / 2f, m.tramoPaso.y);
                        if (Interseca(caja.Min, caja.Max, p0, p1)) mal.Add("dentro del paso libre");
                    }
                    float distancia = float.MaxValue;
                    foreach (UnionConPuertas u2 in c.uniones)
                        foreach (PuertaNave pu in u2.puertas)
                        {
                            if (pu.modulo != m.id) continue;
                            PuertoModulo p = m.Puerto(pu.puerto);
                            if (p == null) continue;
                            for (int a = 0; a <= 1; a++)
                                foreach (CajaLocal h in GeometriaPuertas.Hojas(m, p, pu.etapas, a))
                                    if (Interseca(caja.Min, caja.Max, h.Min, h.Max)) mal.Add("choca con una hoja");
                            foreach (CajaLocal g in GeometriaPuertas.Guias(m, p, pu.etapas))
                                if (Interseca(caja.Min, caja.Max, g.Min, g.Max)) mal.Add("choca con una guía");
                        }
                    // Distancia a la escotilla de esta unión en este módulo (puerta propia o, si no tiene, el puerto de la unión)
                    foreach (PasoNave ps in c.pasos)
                    {
                        string puerto = ps.moduloA == m.id ? ps.puertoA : ps.moduloB == m.id ? ps.puertoB : null;
                        bool deEstaUnion = (ps.moduloA == u.moduloA && ps.moduloB == u.moduloB) || (ps.moduloA == u.moduloB && ps.moduloB == u.moduloA);
                        if (puerto == null || !deEstaUnion) continue;
                        PuertoModulo p = m.Puerto(puerto);
                        if (p != null) distancia = Mathf.Min(distancia, Vector3.Distance(pn.posicion, p.posicion));
                    }
                    if (distancia > DefinicionCirculacion.DistanciaMaximaPanel) mal.Add($"a {distancia:0.0} m de su escotilla");
                    // Punto de uso: libre de equipos, dentro del casco, delante del panel y a menos de 2,4 m (alcance de E)
                    Vector3 u0 = pn.puntoUso - Vector3.one * 0.25f, u1 = pn.puntoUso + Vector3.one * 0.25f;
                    if (!Dentro(m, u0, u1)) mal.Add("su punto de uso queda fuera del casco");
                    foreach (EquipoModulo e in m.equipos) if (Interseca(u0, u1, e.Min, e.Max)) mal.Add("su punto de uso choca con " + e.nombre);
                    if (Vector3.Dot(pn.puntoUso - pn.posicion, pn.normal) <= 0f) mal.Add("su punto de uso está detrás del panel");
                    if (Vector3.Distance(pn.puntoUso, pn.posicion) > 2.4f) mal.Add("su punto de uso está demasiado lejos");
                    if (mal.Count > 0) r.problemas.Add(nom + ": " + string.Join(", ", mal));
                    else r.comprobaciones++;
                }
        }
    }
}
