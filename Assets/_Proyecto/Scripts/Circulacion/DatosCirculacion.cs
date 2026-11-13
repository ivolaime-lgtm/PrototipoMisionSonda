using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    // =====================================================================================================================
    // DATOS de circulación interior de la EM-4X (solo datos). Lista de pasos y puertas ACORDADA con el equipo el
    // 12 de noviembre de 2026. No cambia la distribución aceptada (DefinicionNaveModular): solo clasifica sus conexiones y
    // añade puertas provisionales y paneles. Coordenadas de puertas y paneles: locales del módulo (ver NaveModular.cs).
    // =====================================================================================================================

    public enum TipoPaso
    {
        /// <summary>Pasadizo permanente sin puertas (ya no se usa en la EM-4X: ver Aislamiento).</summary>
        Abierto,
        /// <summary>Pasadizo permanente con una puerta de aislamiento NORMALMENTE ABIERTA (cierre manual).</summary>
        Aislamiento,
        /// <summary>Unión de un módulo separable en el futuro: una puerta a cada lado de la unión.</summary>
        UnionSeparable,
        /// <summary>Puerta interior de la esclusa (por función, no por ser separable).</summary>
        PuertaEsclusa,
        /// <summary>Escotilla cerrada hacia el exterior, sin interacción.</summary>
        CerradoExterior,
        /// <summary>Volumen exterior, no es pasadizo.</summary>
        SinPaso,
        /// <summary>Unión estructural sin paso interior.</summary>
        Estructural
    }

    /// <summary>Clasificación de una conexión (moduloB vacío = puerto sin módulo al otro lado).</summary>
    [Serializable]
    public class PasoNave
    {
        public string moduloA, puertoA, moduloB, puertoB;
        public TipoPaso tipo;
        public string nota;
    }

    /// <summary>Una puerta deslizante provisional: va dentro del módulo, junto a la escotilla del puerto indicado.</summary>
    [Serializable]
    public class PuertaNave
    {
        public string modulo, puerto;
        [Tooltip("1 = una hoja a cada lado; 2 = telescópica (dos hojas por lado) cuando no hay sitio para una sola.")]
        public int etapas = 1;
    }

    /// <summary>Panel de control (tecla E). «normal» = hacia dónde mira el panel (hacia el interior del módulo).</summary>
    [Serializable]
    public class PanelNave
    {
        public string modulo;
        public Vector3 posicion;
        public Vector3 normal;
        [Tooltip("Punto libre desde el que una persona usa el panel (local del módulo).")]
        public Vector3 puntoUso;
    }

    /// <summary>Paso con puertas: todas sus puertas se abren y cierran juntas desde cualquiera de sus paneles.</summary>
    [Serializable]
    public class UnionConPuertas
    {
        public string id, nombre;
        public TipoPaso tipo;
        public string moduloA, moduloB;
        [Tooltip("Estado al empezar: las puertas de aislamiento empiezan abiertas.")]
        public bool abiertaAlInicio;
        public List<PuertaNave> puertas = new List<PuertaNave>();
        public List<PanelNave> paneles = new List<PanelNave>();
    }

    public class DefinicionCirculacion
    {
        // Medidas provisionales de las puertas (m) y tiempo de apertura (s)
        public const float GrosorHoja = 0.04f;
        public const float SeparacionHoja = 0.08f;      // de la superficie interior a la primera hoja
        public const float SeparacionEtapas = 0.06f;    // entre las dos hojas de una puerta telescópica
        public const float MargenCorredor = 0.15f;      // la hoja abierta queda fuera del corredor de la escotilla
        public const float Solape = 0.02f;
        public const float AnchoPanel = 0.25f;
        public const float GrosorPanel = 0.05f;
        public const float SegundosPuerta = 1.2f;
        public const float DistanciaMaximaPanel = 2.5f;

        public List<PasoNave> pasos = new List<PasoNave>();
        public List<UnionConPuertas> uniones = new List<UnionConPuertas>();
        /// <summary>Intenciones futuras registradas (sin función en este incremento).</summary>
        public List<string> intencionesFuturas = new List<string>();

        static PasoNave P(string a, string pa, string b, string pb, TipoPaso t, string nota) =>
            new PasoNave { moduloA = a, puertoA = pa, moduloB = b, puertoB = pb, tipo = t, nota = nota };

        static PanelNave Pn(string m, Vector3 pos, Vector3 normal, Vector3 uso) =>
            new PanelNave { modulo = m, posicion = pos, normal = normal, puntoUso = uso };

        /// <summary>Lista acordada el 12-11-2026; puertas de aislamiento añadidas el mismo día (ajuste posterior).</summary>
        public static DefinicionCirculacion AcordadaEM4X()
        {
            DefinicionCirculacion c = new DefinicionCirculacion();
            c.pasos.Add(P("NC", "proa", "MA", "inicio", TipoPaso.Aislamiento, "Nodo central ↔ Mando"));
            c.pasos.Add(P("NC", "popa", "IN", "inicio", TipoPaso.Aislamiento, "Nodo central ↔ Ingeniería"));
            c.pasos.Add(P("NC", "babor", "CI", "inicio", TipoPaso.Aislamiento, "Nodo central ↔ Ciencia"));
            c.pasos.Add(P("NC", "estribor", "CO", "inicio", TipoPaso.Aislamiento, "Nodo central ↔ Comunicaciones"));
            c.pasos.Add(P("NC", "dorsal", "HA", "inicio", TipoPaso.Aislamiento, "Nodo central ↔ Hábitat"));
            c.pasos.Add(P("IN", "fin", "N2", "inicio", TipoPaso.Aislamiento, "Ingeniería ↔ Nodo de popa"));
            c.pasos.Add(P("N2", "dorsal", "SI", "inicio", TipoPaso.Aislamiento, "Nodo de popa ↔ Sistemas"));
            c.pasos.Add(P("MA", "atraque", "LZ", "atraque", TipoPaso.UnionSeparable, "Mando ↔ Lanzadera: una puerta a cada lado"));
            c.pasos.Add(P("NC", "ventral", "BO", "inicio", TipoPaso.UnionSeparable, "Nodo central ↔ Bodega: una puerta a cada lado"));
            c.pasos.Add(P("N2", "ventral", "ES", "inicio", TipoPaso.PuertaEsclusa, "Nodo de popa ↔ Esclusa: puerta interior"));
            c.pasos.Add(P("N2", "fin", "PR", "inicio", TipoPaso.Estructural, "Etapa de propulsión: unión estructural, sin paso"));
            c.pasos.Add(P("ES", "exterior", null, null, TipoPaso.CerradoExterior, "Escotilla exterior de la esclusa: cerrada, sin interacción"));
            c.pasos.Add(P("N2", "suministro", null, null, TipoPaso.CerradoExterior, "Puerto de suministro: cerrado hacia el exterior"));
            c.pasos.Add(P("N2", "sonda", null, null, TipoPaso.SinPaso, "Reserva de la sonda: volumen exterior, no pasadizo"));

            UnionConPuertas lz = new UnionConPuertas { id = "lanzadera", nombre = "Mando ↔ Lanzadera", tipo = TipoPaso.UnionSeparable, moduloA = "MA", moduloB = "LZ" };
            // En el mando la cara del suelo es estrecha (2,24 m): una hoja por lado no cabe abierta; se usa telescópica.
            lz.puertas.Add(new PuertaNave { modulo = "MA", puerto = "atraque", etapas = 2 });
            lz.puertas.Add(new PuertaNave { modulo = "LZ", puerto = "atraque", etapas = 1 });
            lz.paneles.Add(new PanelNave { modulo = "MA", posicion = new Vector3(0f, -2.65f, 4.3f), normal = Vector3.up, puntoUso = new Vector3(0f, -1.7f, 4.3f) });
            lz.paneles.Add(new PanelNave { modulo = "LZ", posicion = new Vector3(0f, 1.9f, 3.9f), normal = Vector3.down, puntoUso = new Vector3(0f, 0.9f, 3.9f) });
            c.uniones.Add(lz);

            UnionConPuertas bo = new UnionConPuertas { id = "bodega", nombre = "Nodo central ↔ Bodega", tipo = TipoPaso.UnionSeparable, moduloA = "NC", moduloB = "BO" };
            bo.puertas.Add(new PuertaNave { modulo = "NC", puerto = "ventral", etapas = 1 });
            bo.puertas.Add(new PuertaNave { modulo = "BO", puerto = "inicio", etapas = 1 });
            bo.paneles.Add(new PanelNave { modulo = "NC", posicion = new Vector3(0f, -4.65f, 7.8f), normal = Vector3.up, puntoUso = new Vector3(0f, -3.6f, 7.8f) });
            bo.paneles.Add(new PanelNave { modulo = "BO", posicion = new Vector3(2.05f, 0f, 0.33f), normal = Vector3.forward, puntoUso = new Vector3(1.0f, 0f, 1.1f) });
            c.uniones.Add(bo);

            UnionConPuertas es = new UnionConPuertas { id = "esclusa", nombre = "Nodo de popa ↔ Esclusa", tipo = TipoPaso.PuertaEsclusa, moduloA = "N2", moduloB = "ES" };
            es.puertas.Add(new PuertaNave { modulo = "ES", puerto = "inicio", etapas = 1 });
            es.paneles.Add(new PanelNave { modulo = "N2", posicion = new Vector3(0f, -2.95f, 3.6f), normal = Vector3.up, puntoUso = new Vector3(0f, -1.9f, 3.6f) });
            es.paneles.Add(new PanelNave { modulo = "ES", posicion = new Vector3(1.74f, 0f, 0.33f), normal = Vector3.forward, puntoUso = new Vector3(0.9f, 0f, 1.0f) });
            c.uniones.Add(es);

            // Puertas de aislamiento de los pasadizos permanentes: una por conexión, NORMALMENTE ABIERTAS, cierre manual.
            // Van dentro del módulo con sitio para que las hojas abiertas queden fuera del paso (2,4 o 2,2 m) sin estrecharlo.
            // Telescópicas (dos hojas por lado) en Ciencia, Comunicaciones y Hábitat: una hoja por lado no cabe abierta en sus
            // tapas, y en las caras del nodo chocaría con la pantalla y las taquillas.
            void Aislar(string id, string nombre, string ma, string mb, string moduloPuerta, string puerto, int etapas, PanelNave a, PanelNave b)
            {
                UnionConPuertas u = new UnionConPuertas { id = id, nombre = nombre, tipo = TipoPaso.Aislamiento, moduloA = ma, moduloB = mb, abiertaAlInicio = true };
                u.puertas.Add(new PuertaNave { modulo = moduloPuerta, puerto = puerto, etapas = etapas });
                u.paneles.Add(a);
                u.paneles.Add(b);
                c.uniones.Add(u);
            }
            Vector3 capa = new Vector3(0f, 2.08f, 0.33f), usoCapa = new Vector3(0f, 1.3f, 1.25f);
            Aislar("ais_mando", "Nodo central ↔ Mando", "NC", "MA", "NC", "proa", 1,
                   Pn("NC", new Vector3(0f, 2.08f, 9.67f), Vector3.back, new Vector3(0f, 1.3f, 8.75f)), Pn("MA", capa, Vector3.forward, usoCapa));
            Aislar("ais_ingenieria", "Nodo central ↔ Ingeniería", "NC", "IN", "NC", "popa", 1,
                   Pn("NC", capa, Vector3.forward, usoCapa), Pn("IN", capa, Vector3.forward, usoCapa));
            Aislar("ais_ciencia", "Nodo central ↔ Ciencia", "NC", "CI", "CI", "inicio", 2,
                   Pn("CI", capa, Vector3.forward, usoCapa), Pn("NC", new Vector3(-4.65f, 0f, 8.2f), Vector3.right, new Vector3(-3.65f, 0f, 8.2f)));
            Aislar("ais_comunicaciones", "Nodo central ↔ Comunicaciones", "NC", "CO", "CO", "inicio", 2,
                   Pn("CO", capa, Vector3.forward, usoCapa), Pn("NC", new Vector3(4.65f, 0f, 8.2f), Vector3.left, new Vector3(3.65f, 0f, 8.2f)));
            Aislar("ais_habitat", "Nodo central ↔ Hábitat", "NC", "HA", "HA", "inicio", 2,
                   Pn("HA", capa, Vector3.forward, usoCapa), Pn("NC", new Vector3(0f, 4.65f, 8.2f), Vector3.down, new Vector3(0f, 3.65f, 8.2f)));
            Aislar("ais_popa", "Ingeniería ↔ Nodo de popa", "IN", "N2", "N2", "inicio", 1,
                   Pn("N2", capa, Vector3.forward, usoCapa), Pn("IN", new Vector3(0f, 2.08f, 8.67f), Vector3.back, new Vector3(0f, 1.3f, 7.75f)));
            Aislar("ais_sistemas", "Nodo de popa ↔ Sistemas", "N2", "SI", "SI", "inicio", 1,
                   Pn("SI", capa, Vector3.forward, usoCapa), Pn("N2", new Vector3(0f, 2.95f, 4.2f), Vector3.down, new Vector3(0f, 1.95f, 4.2f)));

            c.intencionesFuturas.Add("Puertas de aislamiento: más adelante, impacto o avería → alarma → salida de la tripulación → aislamiento del módulo → reparación o regreso. Aislar no significa separar ni reparar.");
            c.intencionesFuturas.Add("Lanzadera separable (sin separación ni atraque en este incremento).");
            c.intencionesFuturas.Add("Bodega intercambiable en la base orbital (sin sustitución ni recarga en este incremento).");
            c.intencionesFuturas.Add("Etapa de propulsión sustituible (sin función en este incremento).");
            return c;
        }

        public PasoNave PasoDe(string modulo, string puerto)
        {
            foreach (PasoNave p in pasos)
                if ((p.moduloA == modulo && p.puertoA == puerto) || (p.moduloB == modulo && p.puertoB == puerto)) return p;
            return null;
        }
    }

    /// <summary>Caja alineada con los ejes del módulo (coordenadas locales del módulo).</summary>
    public struct CajaLocal
    {
        public Vector3 centro, tamano;
        public Vector3 Min => centro - tamano * 0.5f;
        public Vector3 Max => centro + tamano * 0.5f;
        public CajaLocal(Vector3 c, Vector3 t) { centro = c; tamano = t; }
    }

    /// <summary>
    /// Geometría de puertas y paneles (SOLO CÁLCULO, compartido por la revisión, la vista y la protección de cierre).
    /// Las hojas van dentro del módulo, paralelas a la pared, a «SeparacionHoja» de la superficie interior, y se deslizan
    /// a los lados hasta quedar fuera del corredor de la escotilla. La misma fórmula se comprobó antes en el modelo Python.
    /// </summary>
    public static class GeometriaPuertas
    {
        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        /// <summary>Grosor de la pared donde está el puerto (el techo de la lanzadera mide 0,5 m).</summary>
        public static float Grosor(ModuloNave m) => m.EsCaja ? m.caja.y / 2f - ModuloNave.LanzaderaInteriorMax.y : m.Grosor;

        /// <summary>n = normal del puerto (hacia fuera), s = dirección de deslizamiento, h = alto de la hoja.</summary>
        public static void Ejes(PuertoModulo p, out Vector3 n, out Vector3 s, out Vector3 h)
        {
            n = p.normal.normalized;
            s = (Mathf.Abs(n.z) > 0.5f || Mathf.Abs(n.y) > 0.5f) ? Vector3.right : Vector3.forward;
            h = Vector3.Cross(n, s);
        }

        public static Vector3 SuperficieInterior(ModuloNave m, PuertoModulo p) => p.posicion - p.normal.normalized * Grosor(m);

        static float MedioCierre(PuertoModulo p) => p.lado / 2f + DefinicionCirculacion.Solape;
        public static float AnchoHoja(PuertoModulo p, int etapas) => MedioCierre(p) / etapas + (etapas > 1 ? DefinicionCirculacion.Solape : 0f);
        static float AltoHoja(PuertoModulo p) => p.lado + 0.1f;

        /// <summary>Hojas con la apertura indicada (0 cerrada, 1 abierta). Orden: lado +, etapas…; lado −, etapas…</summary>
        public static List<CajaLocal> Hojas(ModuloNave m, PuertoModulo p, int etapas, float apertura)
        {
            Ejes(p, out Vector3 n, out Vector3 s, out Vector3 h);
            Vector3 sup = SuperficieInterior(m, p);
            float medio = MedioCierre(p), w = AnchoHoja(p, etapas), alto = AltoHoja(p);
            float abierta = DefinicionCirculacion.MargenCorredor + p.lado / 2f + w / 2f + 0.01f;
            Vector3 tam = Abs(s) * w + Abs(h) * alto + Abs(n) * DefinicionCirculacion.GrosorHoja;
            List<CajaLocal> lista = new List<CajaLocal>();
            foreach (float lado in new[] { 1f, -1f })
                for (int k = 0; k < etapas; k++)
                {
                    float d = DefinicionCirculacion.SeparacionHoja + k * DefinicionCirculacion.SeparacionEtapas;
                    float cerrada = etapas > 1 ? k * (medio / etapas) + w / 2f : w / 2f;
                    float c = Mathf.Lerp(cerrada, abierta, Mathf.Clamp01(apertura));
                    lista.Add(new CajaLocal(sup - n * d + s * (lado * c), tam));
                }
            return lista;
        }

        /// <summary>Guías de arriba y abajo de las hojas (fijas), a lo largo de todo el recorrido.</summary>
        public static List<CajaLocal> Guias(ModuloNave m, PuertoModulo p, int etapas)
        {
            Ejes(p, out Vector3 n, out Vector3 s, out Vector3 h);
            Vector3 sup = SuperficieInterior(m, p);
            float w = AnchoHoja(p, etapas), alto = AltoHoja(p);
            float recorrido = DefinicionCirculacion.MargenCorredor + p.lado / 2f + w + 0.02f;
            float dn = DefinicionCirculacion.SeparacionHoja + (etapas - 1) * DefinicionCirculacion.SeparacionEtapas / 2f;
            float gn = DefinicionCirculacion.GrosorHoja + (etapas - 1) * DefinicionCirculacion.SeparacionEtapas;
            Vector3 tam = Abs(s) * (2f * recorrido) + Abs(h) * 0.04f + Abs(n) * gn;
            return new List<CajaLocal>
            {
                new CajaLocal(sup - n * dn + h * (alto / 2f + 0.03f), tam),
                new CajaLocal(sup - n * dn - h * (alto / 2f + 0.03f), tam)
            };
        }

        /// <summary>Zona que debe estar libre para cerrar: el plano de las hojas sobre la escotilla, con margen.</summary>
        public static CajaLocal ZonaCierre(ModuloNave m, PuertoModulo p, int etapas)
        {
            Ejes(p, out Vector3 n, out Vector3 s, out Vector3 h);
            Vector3 sup = SuperficieInterior(m, p);
            float dn = DefinicionCirculacion.SeparacionHoja + (etapas - 1) * DefinicionCirculacion.SeparacionEtapas / 2f;
            Vector3 tam = Abs(s) * (p.lado + 0.2f) + Abs(h) * (p.lado + 0.2f) + Abs(n) * 0.5f;
            return new CajaLocal(sup - n * dn, tam);
        }

        public static CajaLocal Panel(PanelNave pn)
        {
            Vector3 n = Abs(pn.normal.normalized);
            Vector3 t = Vector3.one * DefinicionCirculacion.AnchoPanel - n * (DefinicionCirculacion.AnchoPanel - DefinicionCirculacion.GrosorPanel);
            return new CajaLocal(pn.posicion, t);
        }

        /// <summary>Caja local → caja del mundo de la nave (los ejes de los módulos son ±X, ±Y, ±Z).</summary>
        public static Bounds AMundo(ModuloNave m, CajaLocal c)
        {
            Vector3 t = Abs(m.DireccionAMundo(c.tamano));
            return new Bounds(m.AMundo(c.centro), t);
        }
    }
}
