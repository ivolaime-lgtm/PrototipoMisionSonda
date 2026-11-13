using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// GEOMETRÍA PROVISIONAL de la nave EM-4X (solo visual, bloques y materiales simples). Lee DefinicionNaveModular y
    /// construye: cascos octogonales con huecos en las escotillas, tapas, adaptadores, equipos, lanzadera (bloque),
    /// etapa de propulsión exterior, antena, paneles, rótulos, esquema de conexiones y figuras de referencia de 1,75 m.
    /// No tiene colisiones, física, masas ni controles (los controles están en InspeccionExpedicion).
    /// </summary>
    [DefaultExecutionOrder(100)]   // después de la cámara: los rótulos miran a la posición ya actualizada
    public class VistaExpedicion : MonoBehaviour
    {
        DefinicionNaveModular d;
        RevisionDistribucion.Resultado rev;
        Transform nave, cascos, adaptadores, exterior, equipos, etiquetas, esquema, referencias;
        Font fuente;
        readonly List<TextMesh> textos = new List<TextMesh>();
        readonly List<(Renderer r, Vector3 centro)> piezas = new List<(Renderer r, Vector3 centro)>();
        Material matCasco, matCascoNodo, matInterior, matNaranja, matVidrio, matOscuro, matPropulsion, matPanel, matLanzadera,
                 matBastidor, matConsola, matConsolaV0, matAsiento, matCamarote, matCarga, matMesa, matPantalla, matOtro,
                 matReservaVol, matLineaConexion, matLineaEstructural, matLineaRuta, matPiel;

        static readonly float Tan225 = Mathf.Tan(22.5f * Mathf.Deg2Rad);
        static readonly float Raiz2 = Mathf.Sqrt(2f);

        public Transform Nave => nave;
        public int Corte { get; private set; }
        public static readonly string[] NombresCorte =
        {
            "sin corte", "corte horizontal (oculta la mitad superior)", "corte longitudinal (oculta el lado de estribor)",
            "corte transversal por el nodo (oculta la mitad de proa)"
        };

        public void Construir(DefinicionNaveModular definicion, RevisionDistribucion.Resultado revision)
        {
            d = definicion;
            rev = revision;
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Materiales();
            Luces();

            nave = new GameObject("Nave · " + d.nombre).transform;
            cascos = Hijo("Cascos de los módulos", nave);
            adaptadores = Hijo("Adaptadores y escotillas", nave);   // también los pasadizos permanentes
            exterior = Hijo("Exterior (propulsión, antena, paneles, reservas)", nave);
            equipos = Hijo("Equipos interiores (bloques)", nave);
            etiquetas = Hijo("Rótulos", nave);
            esquema = Hijo("Esquema de conexiones y recorridos", nave);
            referencias = Hijo("Referencias de tamaño 1,75 m (NO son bots ni jugadores)", nave);

            foreach (ModuloNave m in d.modulos)
            {
                if (m.EsCaja) Lanzadera(m);
                else Modulo(m);
            }
            Adaptadores();
            Exteriores();
            Referencias();
            Esquema();

            // Piezas que el corte puede ocultar (no los rótulos, ni el esquema, ni las referencias)
            foreach (Transform raiz in new[] { cascos, adaptadores, exterior, equipos })
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                    piezas.Add((r, nave.InverseTransformPoint(r.bounds.center)));
            MostrarEsquema(false);
        }

        // ------------------------------------------------------------------ Utilidades

        static Material Mat(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material m = new Material(sh);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static Material MatSinLuz(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            Material m = new Material(sh);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static Material MatTransparente(Color c)
        {
            Material m = MatSinLuz(c);
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return m;
        }

        void Materiales()
        {
            matCasco = Mat(new Color(0.86f, 0.87f, 0.9f));
            matCascoNodo = Mat(new Color(0.78f, 0.84f, 0.92f));
            matInterior = Mat(new Color(0.8f, 0.82f, 0.86f));
            matNaranja = Mat(new Color(0.95f, 0.55f, 0.16f));
            matVidrio = Mat(new Color(0.3f, 0.7f, 0.85f));
            matOscuro = Mat(new Color(0.2f, 0.22f, 0.26f));
            matPropulsion = Mat(new Color(0.55f, 0.56f, 0.58f));
            matPanel = Mat(new Color(0.16f, 0.25f, 0.5f));
            matLanzadera = Mat(new Color(0.84f, 0.88f, 0.8f));
            matBastidor = Mat(new Color(0.6f, 0.65f, 0.7f));
            matConsola = Mat(new Color(0.23f, 0.49f, 0.85f));
            matConsolaV0 = Mat(new Color(0.15f, 0.35f, 0.75f));
            matAsiento = Mat(new Color(0.3f, 0.3f, 0.32f));
            matCamarote = Mat(new Color(0.71f, 0.54f, 0.85f));
            matCarga = Mat(new Color(0.97f, 0.79f, 0.6f));
            matMesa = Mat(new Color(0.54f, 0.43f, 0.23f));
            matPantalla = Mat(new Color(0.1f, 0.25f, 0.35f));
            matOtro = Mat(new Color(0.5f, 0.5f, 0.5f));
            matPiel = Mat(new Color(0.85f, 0.85f, 0.88f));
            matReservaVol = MatTransparente(new Color(1f, 0.5f, 0.15f, 0.25f));
            matLineaConexion = MatSinLuz(new Color(1f, 0.6f, 0.1f));
            matLineaEstructural = MatSinLuz(new Color(0.6f, 0.6f, 0.6f));
            matLineaRuta = MatSinLuz(new Color(0.95f, 0.25f, 0.2f));
        }

        void Luces()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.52f, 0.56f);
            Light sol = new GameObject("Luz principal").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 1.0f;
            sol.shadows = LightShadows.None;   // sin sombras: el interior se ve sin luces propias
            sol.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
            Light relleno = new GameObject("Luz de relleno").AddComponent<Light>();
            relleno.type = LightType.Directional;
            relleno.intensity = 0.45f;
            relleno.shadows = LightShadows.None;
            relleno.transform.rotation = Quaternion.Euler(-30f, 140f, 0f);
        }

        static Transform Hijo(string nombre, Transform padre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        static Transform Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Quaternion rot, Vector3 escala, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            g.name = nombre;
            Collider c = g.GetComponent<Collider>();
            if (c != null) Destroy(c);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        static Transform Caja(string nombre, Transform padre, Vector3 centro, Vector3 tamano, Material m) =>
            Pieza(PrimitiveType.Cube, nombre, padre, centro, Quaternion.identity, tamano, m);

        static Transform CajaMinMax(string nombre, Transform padre, Vector3 min, Vector3 max, Material m) =>
            Caja(nombre, padre, (min + max) * 0.5f, max - min, m);

        TextMesh Texto(string texto, Transform padre, Vector3 pos, float tamano, Color color)
        {
            GameObject g = new GameObject("Rótulo");
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            TextMesh tm = g.AddComponent<TextMesh>();
            tm.font = fuente;
            tm.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            tm.text = texto;
            tm.fontSize = 48;
            tm.characterSize = tamano;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            textos.Add(tm);
            return tm;
        }

        static string M(float v) => v.ToString("0.0#");

        Transform MarcoModulo(ModuloNave m, Transform padre)
        {
            Transform t = Hijo(m.nombre, padre);
            t.localPosition = m.origen;
            t.localRotation = m.Rotacion;
            return t;
        }

        /// <summary>Marco naranja de una escotilla cuadrada (lado) en el plano perpendicular a «normal», en el espacio de «padre».</summary>
        void MarcoEscotilla(Transform padre, Vector3 centro, Vector3 normal, float lado, float fondo)
        {
            Vector3 arriba = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.9f ? Vector3.up : Vector3.forward;
            Quaternion q = Quaternion.LookRotation(normal, arriba);
            float b = 0.1f, h = lado / 2f;
            Pieza(PrimitiveType.Cube, "Marco escotilla", padre, centro + q * new Vector3(0f, h + b / 2f, 0f), q, new Vector3(lado + 2f * b, b, fondo), matNaranja);
            Pieza(PrimitiveType.Cube, "Marco escotilla", padre, centro + q * new Vector3(0f, -h - b / 2f, 0f), q, new Vector3(lado + 2f * b, b, fondo), matNaranja);
            Pieza(PrimitiveType.Cube, "Marco escotilla", padre, centro + q * new Vector3(h + b / 2f, 0f, 0f), q, new Vector3(b, lado, fondo), matNaranja);
            Pieza(PrimitiveType.Cube, "Marco escotilla", padre, centro + q * new Vector3(-h - b / 2f, 0f, 0f), q, new Vector3(b, lado, fondo), matNaranja);
        }

        bool PuertoConectado(ModuloNave m, PuertoModulo p)
        {
            foreach (ConexionModulos c in d.conexiones)
                if ((c.moduloA == m.id && c.puertoA == p.id) || (c.moduloB == m.id && c.puertoB == p.id)) return true;
            return false;
        }

        // ------------------------------------------------------------------ Módulos octogonales

        void Modulo(ModuloNave m)
        {
            Transform t = MarcoModulo(m, cascos);
            bool nodo = m.tipo == TipoModuloNave.Nodo;
            bool prop = m.tipo == TipoModuloNave.Propulsion;
            Material mat = prop ? matPropulsion : nodo ? matCascoNodo : matCasco;
            float R = m.diametroExterior / 2f;
            float e = prop ? 0.3f : m.Grosor;
            float rm = R - e / 2f;
            float w = m.diametroExterior * Tan225 + 0.02f;

            // Ocho caras, partidas a lo largo (máx. 2,5 m) para que el corte pueda abrirlas
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f * Mathf.Deg2Rad;
                Vector3 n = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 tg = new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f);
                var huecos = new List<(float z0, float z1, float lado)>();
                foreach (PuertoModulo p in m.puertos)
                    if (!p.EnTapa && p.escotilla && Vector3.Dot(p.normal.normalized, n) > 0.9f)
                        huecos.Add((p.posicion.z - p.lado / 2f, p.posicion.z + p.lado / 2f, p.lado));
                huecos.Sort((x, y) => x.z0.CompareTo(y.z0));
                float cursor = 0f;
                foreach (var h in huecos)
                {
                    Cara(t, n, tg, rm, e, 0f, w, cursor, h.z0, mat);
                    float lat = (w - h.lado) / 2f;
                    Cara(t, n, tg, rm, e, -(h.lado / 2f + lat / 2f), lat, h.z0, h.z1, mat);
                    Cara(t, n, tg, rm, e, h.lado / 2f + lat / 2f, lat, h.z0, h.z1, mat);
                    MarcoEscotilla(t, n * (R - e / 2f) + Vector3.forward * (h.z0 + h.z1) / 2f, n, h.lado, e + 0.06f);
                    cursor = h.z1;
                }
                Cara(t, n, tg, rm, e, 0f, w, cursor, m.largo, mat);
            }

            // Tapas
            PuertoModulo pIni = null, pFin = null;
            foreach (PuertoModulo p in m.puertos)
                if (p.EnTapa) { if (p.normal.z < 0f) pIni = p; else pFin = p; }
            bool ventanaProa = m.id == "MA";
            Tapa(t, m, e, false, pIni, false, mat);
            Tapa(t, m, e, true, pFin, ventanaProa, mat);

            // Escotillas sin módulo al otro lado: tapa cerrada (escotilla EVA, puerto de suministro)
            foreach (PuertoModulo p in m.puertos)
            {
                if (!p.escotilla || PuertoConectado(m, p)) continue;
                Vector3 n = p.normal.normalized;
                Quaternion q = Quaternion.LookRotation(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.forward);
                Pieza(PrimitiveType.Cube, "Escotilla cerrada (" + p.id + ")", t, p.posicion - n * (e / 2f), q, new Vector3(p.lado, p.lado, 0.08f), matOscuro);
            }

            if (!prop) Equipos(m);
            Asideros(m, t);

            // Rótulo exterior
            if (!prop)
            {
                string paso = m.pasoLibre.x > 0f ? $" · paso libre {M(m.pasoLibre.x)} × {M(m.pasoLibre.y)}" : " · sin bastidores";
                Vector3 fuera = m.Centro + m.Arriba * (R + 1.4f);
                if (m.id == "BO" || m.id == "ES") fuera = m.AMundo(new Vector3(0f, 0f, m.largo + 1.6f));
                Texto($"{m.nombre.ToUpper()}\n{m.funcion}\next. {M(m.diametroExterior)} · int. {M(m.diametroInterior)}{paso} · largo {M(m.largo)} m",
                      etiquetas, fuera, 0.05f, new Color(0.9f, 0.95f, 1f));
            }
            else
                Texto("ETAPA DE PROPULSIÓN (solo exterior)\nsin balance de masa, motor ni combustible: PENDIENTE",
                      etiquetas, m.Centro + Vector3.up * (R + 1.4f), 0.05f, new Color(1f, 0.75f, 0.4f));
        }

        /// <summary>Un tramo de cara: centro tangencial «tc», ancho «tw», de z0 a z1 (partido en piezas de 2,5 m como máximo).</summary>
        static void Cara(Transform t, Vector3 n, Vector3 tg, float rm, float e, float tc, float tw, float z0, float z1, Material mat)
        {
            if (z1 - z0 < 0.01f || tw < 0.01f) return;
            int partes = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / 2.5f));
            float paso = (z1 - z0) / partes;
            Quaternion q = Quaternion.LookRotation(Vector3.forward, n);
            for (int i = 0; i < partes; i++)
            {
                float za = z0 + i * paso, zb = za + paso;
                Pieza(PrimitiveType.Cube, "Casco", t, n * rm + tg * tc + Vector3.forward * (za + zb) / 2f, q, new Vector3(tw, e, zb - za), mat);
            }
        }

        /// <summary>Tapa octogonal hecha de franjas horizontales, con hueco de escotilla o ventana.</summary>
        void Tapa(Transform t, ModuloNave m, float e, bool fin, PuertoModulo puerto, bool ventana, Material mat)
        {
            float R = m.diametroExterior / 2f;
            float zc = fin ? m.largo - e / 2f : e / 2f;
            bool hueco = puerto != null && puerto.escotilla;
            float hx0 = 0f, hx1 = 0f, hy0 = 0f, hy1 = 0f;
            if (ventana) { hx0 = -1.8f; hx1 = 1.8f; hy0 = -0.4f; hy1 = 1.4f; }
            else if (hueco) { hx0 = -puerto.lado / 2f; hx1 = puerto.lado / 2f; hy0 = hx0; hy1 = hx1; }
            bool conHueco = ventana || hueco;

            List<float> cortes = new List<float> { -R, R };
            if (conHueco) { cortes.Add(hy0); cortes.Add(hy1); }
            cortes.Sort();
            for (int i = 0; i + 1 < cortes.Count; i++)
            {
                float a = cortes[i], b = cortes[i + 1];
                int partes = Mathf.Max(1, Mathf.CeilToInt((b - a) / 0.35f));
                for (int j = 0; j < partes; j++)
                {
                    float y0 = a + (b - a) * j / partes, y1 = a + (b - a) * (j + 1) / partes, yc = (y0 + y1) / 2f;
                    float xh = Mathf.Min(R, R * Raiz2 - Mathf.Max(Mathf.Abs(y0), Mathf.Abs(y1)));
                    if (xh <= 0.01f) continue;
                    if (conHueco && yc > hy0 && yc < hy1)
                    {
                        if (hx0 > -xh) CajaMinMax("Tapa", t, new Vector3(-xh, y0, zc - e / 2f), new Vector3(hx0, y1, zc + e / 2f), mat);
                        if (hx1 < xh) CajaMinMax("Tapa", t, new Vector3(hx1, y0, zc - e / 2f), new Vector3(xh, y1, zc + e / 2f), mat);
                    }
                    else CajaMinMax("Tapa", t, new Vector3(-xh, y0, zc - e / 2f), new Vector3(xh, y1, zc + e / 2f), mat);
                }
            }
            if (ventana)
                CajaMinMax("Ventana de proa", t, new Vector3(hx0, hy0, zc - 0.03f), new Vector3(hx1, hy1, zc + 0.03f), matVidrio);
            else if (hueco)
                MarcoEscotilla(t, new Vector3(0f, 0f, zc), fin ? Vector3.forward : Vector3.back, puerto.lado, e + 0.06f);
        }

        Material MatEquipo(EquipoModulo e)
        {
            if (e.nombre.Contains("v0")) return matConsolaV0;
            switch (e.tipo)
            {
                case TipoEquipo.Bastidor: return matBastidor;
                case TipoEquipo.Consola: return matConsola;
                case TipoEquipo.Asiento: return matAsiento;
                case TipoEquipo.Camarote: return matCamarote;
                case TipoEquipo.Carga: return matCarga;
                case TipoEquipo.Mesa: return matMesa;
                case TipoEquipo.Pantalla: return matPantalla;
                default: return matOtro;
            }
        }

        void Equipos(ModuloNave m)
        {
            Transform t = MarcoModulo(m, equipos);
            foreach (EquipoModulo e in m.equipos)
            {
                Caja(e.nombre, t, e.centro, e.tamano, MatEquipo(e));
                if (e.tipo == TipoEquipo.Consola || e.tipo == TipoEquipo.Camarote || e.tipo == TipoEquipo.Mesa && e.tamano.y < 0.5f || e.tipo == TipoEquipo.Pantalla)
                    Texto(e.nombre, etiquetas, m.AMundo(e.centro) + m.Arriba * (e.tamano.y / 2f + 0.25f), 0.02f, new Color(0.85f, 0.9f, 1f));
            }
            // Indicaciones junto a cada escotilla: a qué módulo lleva
            foreach (ConexionModulos c in d.conexiones)
            {
                string otro = c.moduloA == m.id ? c.moduloB : c.moduloB == m.id ? c.moduloA : null;
                if (otro == null) continue;
                PuertoModulo p = m.Puerto(c.moduloA == m.id ? c.puertoA : c.puertoB);
                if (p == null || !p.escotilla) continue;
                Vector3 dentro = m.AMundo(p.posicion - p.normal.normalized * 0.7f);
                Vector3 sobre = p.EnTapa ? m.Arriba * (p.lado / 2f + 0.35f) : m.Adelante * (p.lado / 2f + 0.35f);
                Texto("→ " + d.Modulo(otro).nombre, etiquetas, dentro + sobre, 0.025f, new Color(1f, 0.8f, 0.45f));
            }
        }

        /// <summary>Asideros a lo largo del módulo (visuales; marcan la circulación flotando).</summary>
        void Asideros(ModuloNave m, Transform t)
        {
            if (m.tipo == TipoModuloNave.Propulsion) return;
            float r = m.diametroInterior / 2f - 0.12f;
            float d45 = r / Raiz2;
            for (int k = 0; k < 4; k++)
            {
                float sx = (k & 1) == 0 ? 1f : -1f, sy = (k & 2) == 0 ? 1f : -1f;
                Pieza(PrimitiveType.Cube, "Asidero", t, new Vector3(sx * d45, sy * d45, m.largo / 2f), Quaternion.identity,
                      new Vector3(0.05f, 0.05f, Mathf.Max(0.5f, m.largo - 1.6f)), matNaranja);
            }
        }

        // ------------------------------------------------------------------ Lanzadera (bloque)

        void Lanzadera(ModuloNave m)
        {
            Transform t = MarcoModulo(m, cascos);
            float hx = m.caja.x / 2f, hy = m.caja.y / 2f, L = m.caja.z, e = 0.3f;
            float suelo = ModuloNave.LanzaderaInteriorMin.y, techo = ModuloNave.LanzaderaInteriorMax.y;
            PuertoModulo p = m.Puerto("atraque");
            float zp = p != null ? p.posicion.z : L / 2f, h = p != null ? p.lado / 2f : 0.5f;

            CajaMinMax("Suelo y sistemas", t, new Vector3(-hx, -hy, 0f), new Vector3(hx, suelo, L), matLanzadera);
            // Techo con el hueco del atraque
            CajaMinMax("Techo", t, new Vector3(-hx, techo, 0f), new Vector3(hx, hy, zp - h), matLanzadera);
            CajaMinMax("Techo", t, new Vector3(-hx, techo, zp + h), new Vector3(hx, hy, L), matLanzadera);
            CajaMinMax("Techo", t, new Vector3(-hx, techo, zp - h), new Vector3(-h, hy, zp + h), matLanzadera);
            CajaMinMax("Techo", t, new Vector3(h, techo, zp - h), new Vector3(hx, hy, zp + h), matLanzadera);
            MarcoEscotilla(t, new Vector3(0f, (techo + hy) / 2f, zp), Vector3.up, 2f * h, hy - techo + 0.06f);
            // Costados (estribor con la escotilla de tierra 1,4 × 2,0)
            CajaMinMax("Costado babor", t, new Vector3(-hx, suelo, 0f), new Vector3(-hx + e, techo, L), matLanzadera);
            float za = 1.0f, zb = 2.4f, alto = suelo + 2.0f;
            CajaMinMax("Costado estribor", t, new Vector3(hx - e, suelo, 0f), new Vector3(hx, techo, za), matLanzadera);
            CajaMinMax("Costado estribor", t, new Vector3(hx - e, suelo, zb), new Vector3(hx, techo, L), matLanzadera);
            CajaMinMax("Costado estribor", t, new Vector3(hx - e, alto, za), new Vector3(hx, techo, zb), matLanzadera);
            CajaMinMax("Escotilla de tierra (cerrada)", t, new Vector3(hx - 0.08f, suelo, za), new Vector3(hx, alto, zb), matOscuro);
            // Popa y proa con ventana
            CajaMinMax("Popa", t, new Vector3(-hx, suelo, 0f), new Vector3(hx, techo, e), matLanzadera);
            CajaMinMax("Proa", t, new Vector3(-hx, suelo, L - e), new Vector3(hx, 0f, L), matLanzadera);
            CajaMinMax("Proa", t, new Vector3(-hx, 1.2f, L - e), new Vector3(hx, techo, L), matLanzadera);
            CajaMinMax("Proa", t, new Vector3(-hx, 0f, L - e), new Vector3(-3f, 1.2f, L), matLanzadera);
            CajaMinMax("Proa", t, new Vector3(3f, 0f, L - e), new Vector3(hx, 1.2f, L), matLanzadera);
            CajaMinMax("Ventana", t, new Vector3(-3f, 0f, L - 0.18f), new Vector3(3f, 1.2f, L - 0.12f), matVidrio);

            Equipos(m);
            Texto("LANZADERA L-4 (bloque provisional)\ndespega desde tierra con los cuatro y atraca bajo el mando\natraque 1,0 × 1,0 · sin animación de atraque",
                  etiquetas, m.AMundo(new Vector3(0f, -hy - 1.2f, L / 2f)), 0.05f, new Color(0.8f, 1f, 0.8f));
        }

        // ------------------------------------------------------------------ Adaptadores entre módulos

        void Adaptadores()
        {
            foreach (ConexionModulos c in d.conexiones)
            {
                ModuloNave a = d.Modulo(c.moduloA), b = d.Modulo(c.moduloB);
                PuertoModulo pa = a?.Puerto(c.puertoA), pb = b?.Puerto(c.puertoB);
                if (pa == null || pb == null) continue;
                Vector3 wa = a.AMundo(pa.posicion), wb = b.AMundo(pb.posicion);
                Vector3 dir = (wb - wa).normalized;
                float largo = Vector3.Distance(wa, wb);
                if (largo < 0.01f) continue;
                bool paso = pa.escotilla && pb.escotilla;
                Transform t = Hijo($"Adaptador {a.id}.{pa.id} – {b.id}.{pb.id}", adaptadores);
                t.localPosition = (wa + wb) / 2f;
                t.localRotation = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.forward);
                if (c.pasadizo && paso)
                {
                    // Pasadizo permanente: sección cuadrada del tamaño del hueco (paso libre), paredes de 0,2 m
                    float L = pa.lado, gp = DefinicionNaveModular.GrosorPasadizo;
                    Pieza(PrimitiveType.Cube, "Pasadizo", t, new Vector3(0f, L / 2f + gp / 2f, 0f), Quaternion.identity, new Vector3(L + 2f * gp, gp, largo), matCasco);
                    Pieza(PrimitiveType.Cube, "Pasadizo", t, new Vector3(0f, -L / 2f - gp / 2f, 0f), Quaternion.identity, new Vector3(L + 2f * gp, gp, largo), matCasco);
                    Pieza(PrimitiveType.Cube, "Pasadizo", t, new Vector3(L / 2f + gp / 2f, 0f, 0f), Quaternion.identity, new Vector3(gp, L, largo), matCasco);
                    Pieza(PrimitiveType.Cube, "Pasadizo", t, new Vector3(-L / 2f - gp / 2f, 0f, 0f), Quaternion.identity, new Vector3(gp, L, largo), matCasco);
                    float ra = L / 2f - 0.08f;
                    for (int q = 0; q < 4; q++)
                        Pieza(PrimitiveType.Cube, "Asidero", t, new Vector3((q & 1) == 0 ? ra : -ra, (q & 2) == 0 ? ra : -ra, 0f), Quaternion.identity,
                              new Vector3(0.05f, 0.05f, largo), matNaranja);
                    Texto($"pasadizo {M(L)} × {M(L)} · {M(largo)} m", etiquetas,
                          (wa + wb) / 2f + (Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.forward) * (L / 2f + gp + 0.4f), 0.022f, new Color(0.6f, 0.85f, 1f));
                    continue;
                }
                float ext = DefinicionNaveModular.ExteriorAdaptador(pa.lado), inte = DefinicionNaveModular.InteriorAdaptador(pa.lado);
                float g = (ext - inte) / 2f, rm = (ext + inte) / 4f, w = ext * Tan225 + 0.02f;
                Material mat = paso ? matCasco : matPropulsion;
                for (int k = 0; k < 8; k++)
                {
                    float an = k * 45f * Mathf.Deg2Rad;
                    Vector3 n = new Vector3(Mathf.Cos(an), Mathf.Sin(an), 0f);
                    Pieza(PrimitiveType.Cube, paso ? "Adaptador" : "Unión estructural", t, n * rm, Quaternion.LookRotation(Vector3.forward, n), new Vector3(w, g, largo), mat);
                }
                if (!paso)
                {
                    Caja("Cierre estructural", t, Vector3.zero, new Vector3(inte, inte, 0.1f), matPropulsion);
                    continue;
                }
                float r = inte / 2f - 0.1f;
                Pieza(PrimitiveType.Cube, "Asidero", t, new Vector3(r, 0f, 0f), Quaternion.identity, new Vector3(0.05f, 0.05f, largo), matNaranja);
                Pieza(PrimitiveType.Cube, "Asidero", t, new Vector3(-r, 0f, 0f), Quaternion.identity, new Vector3(0.05f, 0.05f, largo), matNaranja);
                Texto($"escotilla {M(pa.lado)} × {M(pa.lado)}", etiquetas, (wa + wb) / 2f + (Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.forward) * (ext / 2f + 0.35f),
                      0.022f, new Color(1f, 0.75f, 0.35f));
            }
        }

        // ------------------------------------------------------------------ Exterior

        void Exteriores()
        {
            foreach (ElementoExterior e in d.exteriores)
            {
                Vector3 c = (e.min + e.max) / 2f, s = e.max - e.min;
                if (e.nombre.StartsWith("Antena"))
                {
                    Caja("Mástil de la antena", exterior, new Vector3(e.min.x + 0.65f, c.y, c.z), new Vector3(1.3f, 0.25f, 0.25f), matOscuro);
                    Pieza(PrimitiveType.Cylinder, "Antena de alta ganancia", exterior, new Vector3(e.min.x + 1.3f, c.y, c.z), Quaternion.Euler(0f, 0f, 90f),
                          new Vector3(Mathf.Min(s.y, s.z), 0.05f, Mathf.Min(s.y, s.z)), matCasco);
                }
                else if (e.nombre.StartsWith("Motor"))
                {
                    Pieza(PrimitiveType.Cylinder, "Garganta del motor", exterior, new Vector3(c.x, c.y, e.max.z - 0.6f), Quaternion.Euler(90f, 0f, 0f),
                          new Vector3(s.x * 0.45f, 0.6f, s.y * 0.45f), matOscuro);
                    Pieza(PrimitiveType.Cylinder, "Tobera", exterior, new Vector3(c.x, c.y, e.min.z + (s.z - 1.2f) / 2f), Quaternion.Euler(90f, 0f, 0f),
                          new Vector3(s.x, (s.z - 1.2f) / 2f, s.y), matOscuro);
                }
                else if (e.nombre.StartsWith("Reserva"))
                {
                    Caja(e.nombre, exterior, c, s, matReservaVol);
                    Texto("Reserva de la sonda\n(exterior · PENDIENTE)", etiquetas, c + Vector3.left * (s.x / 2f + 1.2f), 0.035f, new Color(1f, 0.7f, 0.35f));
                }
                else
                {
                    Caja(e.nombre, exterior, c, s, matNaranja);
                    Texto("Puerto de suministro\n(cerrado · sin vehículos)", etiquetas, c + Vector3.right * (s.x / 2f + 1.6f), 0.035f, new Color(1f, 0.8f, 0.45f));
                }
            }

            // Paneles solares y radiadores desplegados a ambos lados de la etapa de propulsión (fuera del ancho medido)
            float zc = (d.panelesZ.x + d.panelesZ.y) / 2f;
            foreach (float lado in new[] { 1f, -1f })
            {
                float x0 = lado * d.panelesX.x, x1 = lado * d.panelesX.y;
                float xb = lado * 3f;   // el brazo arranca en la cara de la etapa de propulsión
                Caja("Brazo de paneles", exterior, new Vector3((xb + x1) / 2f, 0f, zc), new Vector3(Mathf.Abs(x1 - xb), 0.3f, 0.3f), matOscuro);
                Caja("Paneles solares", exterior, new Vector3((x0 + x1) / 2f, 0f, (zc + d.panelesZ.y) / 2f + 0.2f),
                     new Vector3(Mathf.Abs(x1 - x0), 0.06f, d.panelesZ.y - zc - 0.4f), matPanel);
                Caja("Radiadores", exterior, new Vector3((x0 + x1) / 2f, 0f, (zc + d.panelesZ.x) / 2f - 0.2f),
                     new Vector3(Mathf.Abs(x1 - x0), 0.06f, zc - d.panelesZ.x - 0.4f), matCasco);
            }
            Texto("Paneles solares y radiadores (desplegados; fuera del ancho medido)\nenergía y control térmico: PENDIENTES",
                  etiquetas, new Vector3(0f, 4.5f, zc), 0.05f, new Color(0.7f, 0.8f, 1f));
        }

        // ------------------------------------------------------------------ Referencias humanas

        Transform Figura(string nombre, ModuloNave m, Vector3 local, Quaternion giroLocal, Material mat)
        {
            Transform f = Hijo(nombre, referencias);
            f.localPosition = m.AMundo(local);
            f.localRotation = m.Rotacion * giroLocal;
            Pieza(PrimitiveType.Capsule, "Cuerpo 1,75 m", f, Vector3.zero, Quaternion.identity, new Vector3(0.5f, 0.875f, 0.5f), mat);
            Pieza(PrimitiveType.Cube, "Cara (hacia dónde mira)", f, new Vector3(0f, 0.65f, 0.22f), Quaternion.identity, new Vector3(0.22f, 0.12f, 0.06f), matOscuro);
            return f;
        }

        void Referencias()
        {
            foreach (PuestoExpedicion p in d.puestos)
            {
                ModuloNave m = d.Modulo(p.modulo);
                if (m == null) continue;
                Vector3 mira = new Vector3(p.mirada.x, 0f, p.mirada.z);
                if (mira.sqrMagnitude < 1e-4f) mira = Vector3.forward;
                Figura("Referencia " + p.rol, m, p.posicion, Quaternion.LookRotation(mira.normalized, Vector3.up), Mat(p.color));
                Texto($"{p.rol}\n(referencia 1,75 m · no es bot ni jugador)", etiquetas, m.AMundo(p.posicion) + m.Arriba * 1.3f, 0.022f, p.color);
            }
            // Maniquíes de medida en la zona común, un módulo de trabajo, el hábitat y la esclusa
            ModuloNave nc = d.Modulo("NC"), ci = d.Modulo("CI"), ha = d.Modulo("HA"), es = d.Modulo("ES");
            if (nc != null)
            {
                Figura("Maniquí junto a la mesa", nc, new Vector3(-1.7f, -2.1f, 2.8f), Quaternion.LookRotation(Vector3.right), matPiel);
                Figura("Maniquí flotando sobre la mesa", nc, new Vector3(1.2f, -1.9f, 2.2f), Quaternion.Euler(0f, -90f, 75f), matPiel);
                Texto("Zona común: maniquíes de medida (no son tripulantes)", etiquetas, nc.AMundo(new Vector3(0f, -0.4f, 2.8f)), 0.025f, Color.white);
            }
            if (ci != null) Figura("Maniquí cruzándose en el paso de Ciencia", ci, new Vector3(-0.9f, 0.5f, 6.5f), Quaternion.Euler(80f, 0f, 0f), matPiel);
            if (ha != null) Figura("Maniquí entre los camarotes", ha, new Vector3(0f, 0f, 5.8f), Quaternion.Euler(90f, 0f, 0f), matPiel);
            if (es != null) Figura("Maniquí en la esclusa", es, new Vector3(0.4f, 0f, 2.6f), Quaternion.identity, matPiel);
        }

        // ------------------------------------------------------------------ Esquema de conexiones (tecla X)

        void Linea(string nombre, List<Vector3> puntos, Material m, float ancho)
        {
            LineRenderer lr = new GameObject(nombre).AddComponent<LineRenderer>();
            lr.transform.SetParent(esquema, false);
            lr.useWorldSpace = false;
            lr.positionCount = puntos.Count;
            lr.SetPositions(puntos.ToArray());
            lr.startWidth = lr.endWidth = ancho;
            lr.sharedMaterial = m;
            lr.numCapVertices = 2;
        }

        void Esquema()
        {
            foreach (ConexionModulos c in d.conexiones)
            {
                ModuloNave a = d.Modulo(c.moduloA), b = d.Modulo(c.moduloB);
                PuertoModulo pa = a?.Puerto(c.puertoA), pb = b?.Puerto(c.puertoB);
                if (pa == null || pb == null) continue;
                Linea($"{a.id} – {b.id}", new List<Vector3> { a.Centro, a.AMundo(pa.posicion), b.AMundo(pb.posicion), b.Centro },
                      pa.escotilla && pb.escotilla ? matLineaConexion : matLineaEstructural, 0.18f);
            }
            if (rev != null)
                foreach (RevisionDistribucion.Ruta r in rev.rutas)
                    if (r.puntos.Count > 1) Linea($"Recorrido {r.desde} – {r.hasta}", r.puntos, matLineaRuta, 0.08f);
        }

        // ------------------------------------------------------------------ Conmutadores

        public void AplicarCorte(int modo)
        {
            Corte = ((modo % NombresCorte.Length) + NombresCorte.Length) % NombresCorte.Length;
            foreach (var p in piezas)
            {
                if (p.r == null) continue;
                bool oculta = Corte == 1 ? p.centro.y > 0.25f : Corte == 2 ? p.centro.x > 0.25f : Corte == 3 ? p.centro.z > 1.75f : false;
                p.r.enabled = !oculta;
            }
        }

        public bool EtiquetasVisibles { get; private set; } = true;
        public bool EsquemaVisible { get; private set; }

        public void MostrarEtiquetas(bool v)
        {
            EtiquetasVisibles = v;
            etiquetas.gameObject.SetActive(v);
        }

        public void MostrarEsquema(bool v)
        {
            EsquemaVisible = v;
            esquema.gameObject.SetActive(v);
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null || !EtiquetasVisibles) return;
            foreach (TextMesh t in textos)
                if (t != null) t.transform.rotation = Quaternion.LookRotation(t.transform.position - cam.transform.position, cam.transform.up);
        }
    }
}
