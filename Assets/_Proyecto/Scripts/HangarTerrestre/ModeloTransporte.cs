using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// REPRESENTACIÓN PROVISIONAL del VC-1, el transporte orbital NO TRIPULADO (solo visual, sin colisiones). Es un vehículo
    /// distinto de la L-4: sin cápsula ni asientos, con bodega de carga abierta por arriba. Se usa tumbado en la zona de
    /// carga del hangar, de pie sobre su lanzador provisional en la plataforma exterior y solo (sin lanzador) en órbita.
    ///
    /// Ejes: largo en +Z desde la popa (z = 0), eje del casco en x = 0, y = 0; las compuertas de la bodega se abren hacia +Y.
    /// Tramos: módulo de servicio 0–4 · bodega principal 4–27 (Ø 7,2 × 23 m) · anillo de carga menor 27–31 · proa 31–32,5.
    /// </summary>
    public class ModeloTransporte
    {
        public const float DiametroCasco = 7.6f, ZBodega = 4f, ZAnillo = 27f, Largo = 32.5f;

        public Transform Raiz { get; private set; }
        /// <summary>Hueco (centro del eje, cara de montaje) de cada componente encargado en la bodega.</summary>
        public readonly Dictionary<TipoComponente, Transform> Huecos = new Dictionary<TipoComponente, Transform>();
        /// <summary>Huecos de los módulos de combustible (hasta 12) y de los contenedores pequeños.</summary>
        public readonly List<Transform> HuecosCombustible = new List<Transform>();
        public readonly Dictionary<string, Transform> HuecosContenedor = new Dictionary<string, Transform>();
        public Transform Llamas { get; private set; }
        readonly List<Transform> bisagras = new List<Transform>();
        readonly Func<Color, bool, Material> mat;

        ModeloTransporte(Func<Color, bool, Material> m) { mat = m; }

        Transform Caja(string n, Transform p, Vector3 c, Vector3 t, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            Collider col = g.GetComponent<Collider>(); col.enabled = false; UnityEngine.Object.Destroy(col);
            g.transform.SetParent(p, false);
            g.transform.localPosition = c;
            g.transform.localScale = t;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        Transform Cil(string n, Transform p, float d, float z0, float z1, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = n;
            Collider col = g.GetComponent<Collider>(); col.enabled = false; UnityEngine.Object.Destroy(col);
            g.transform.SetParent(p, false);
            g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            g.transform.localPosition = new Vector3(0f, 0f, (z0 + z1) / 2f);
            g.transform.localScale = new Vector3(d, (z1 - z0) / 2f, d);
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        /// <summary>
        /// Construye el VC-1. «t» decide los huecos de la bodega (orden: tanque, motor, instrumento, relevo, sin solaparse).
        /// </summary>
        public static ModeloTransporte Construir(Transform padre, Vector3 pos, Quaternion rot, ResumenTransporte t, CatalogoMision cat,
                                                 Func<Color, bool, Material> mat)
        {
            ModeloTransporte m = new ModeloTransporte(mat);
            m.Montar(padre, pos, rot, t, cat);
            return m;
        }

        void Montar(Transform padre, Vector3 pos, Quaternion rot, ResumenTransporte t, CatalogoMision cat)
        {
            Raiz = new GameObject(DatosTransporteOrbital.Nombre + " (provisional)").transform;
            Raiz.SetParent(padre, false);
            Raiz.localPosition = pos;
            Raiz.localRotation = rot;
            Material blanco = mat(new Color(0.86f, 0.87f, 0.88f), false), gris = mat(new Color(0.45f, 0.47f, 0.5f), false),
                     oscuro = mat(new Color(0.18f, 0.19f, 0.22f), false), azul = mat(new Color(0.15f, 0.25f, 0.55f), false),
                     amarillo = mat(new Color(0.95f, 0.78f, 0.15f), false), fuego = mat(new Color(1f, 0.72f, 0.25f), true);
            float r = DiametroCasco / 2f;

            // Módulo de servicio (popa): motores de maniobra y paneles solares plegados
            Transform servicio = SustitucionEscenario.Punto(new GameObject("Módulo de servicio").transform, "VC1/ModuloServicio");
            servicio.SetParent(Raiz, false);
            Cil("Casco del módulo de servicio", servicio, DiametroCasco, 0f, ZBodega, gris);
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + 90f * i) * Mathf.Deg2Rad;
                Transform tob = Cil("Tobera de maniobra", servicio, 1f, -0.9f, 0f, oscuro);
                tob.localPosition += new Vector3(Mathf.Cos(a) * 2.2f, Mathf.Sin(a) * 2.2f, 0f);
            }
            Caja("Panel solar plegado (babor)", servicio, new Vector3(-r - 0.15f, 0f, 2f), new Vector3(0.15f, 3f, 3.6f), azul);
            Caja("Panel solar plegado (estribor)", servicio, new Vector3(r + 0.15f, 0f, 2f), new Vector3(0.15f, 3f, 3.6f), azul);
            Llamas = Caja("Llamas de maniobra", Raiz, new Vector3(0f, 0f, -2.5f), new Vector3(3f, 3f, 3f), fuego);
            Llamas.gameObject.SetActive(false);

            // Bodega principal (abierta por arriba) y anillo de carga menor
            Transform bodega = SustitucionEscenario.Punto(new GameObject("Bodega principal").transform, "VC1/Bodega");
            bodega.SetParent(Raiz, false);
            float lb = ZAnillo - ZBodega, zc = (ZBodega + ZAnillo) / 2f;
            Caja("Fondo de la bodega", bodega, new Vector3(0f, -r + 0.15f, zc), new Vector3(DiametroCasco - 0.6f, 0.3f, lb), blanco);
            Caja("Costado (babor)", bodega, new Vector3(-r + 0.15f, 0f, zc), new Vector3(0.3f, DiametroCasco - 0.6f, lb), blanco);
            Caja("Costado (estribor)", bodega, new Vector3(r - 0.15f, 0f, zc), new Vector3(0.3f, DiametroCasco - 0.6f, lb), blanco);
            for (float z = ZBodega; z <= ZAnillo + 0.01f; z += 3f)
                Caja("Cuaderna", bodega, new Vector3(0f, -r + 0.35f, z), new Vector3(DiametroCasco - 0.4f, 0.3f, 0.25f), amarillo);
            foreach (float lado in new[] { -1f, 1f })
            {
                Transform b = new GameObject(lado < 0 ? "Bisagra de la compuerta (babor)" : "Bisagra de la compuerta (estribor)").transform;
                b.SetParent(bodega, false);
                b.localPosition = new Vector3(lado * (r - 0.15f), r - 0.15f, zc);
                Caja("Compuerta de la bodega", b, new Vector3(-lado * (r - 0.3f) / 2f, 0f, 0f), new Vector3(r - 0.3f, 0.25f, lb), blanco);
                bisagras.Add(b);
            }
            Transform anillo = SustitucionEscenario.Punto(new GameObject("Anillo de carga menor").transform, "VC1/Anillo");
            anillo.SetParent(Raiz, false);
            Caja("Fondo del anillo", anillo, new Vector3(0f, -r + 0.15f, ZAnillo + 2f), new Vector3(DiametroCasco - 0.6f, 0.3f, 4f), gris);
            Caja("Costado del anillo (babor)", anillo, new Vector3(-r + 0.15f, 0f, ZAnillo + 2f), new Vector3(0.3f, DiametroCasco - 0.6f, 4f), gris);
            Caja("Costado del anillo (estribor)", anillo, new Vector3(r - 0.15f, 0f, ZAnillo + 2f), new Vector3(0.3f, DiametroCasco - 0.6f, 4f), gris);
            Caja("Mamparo de proa", anillo, new Vector3(0f, 0f, ZAnillo + 4f), new Vector3(DiametroCasco - 0.6f, DiametroCasco - 0.6f, 0.2f), gris);
            Transform proa = SustitucionEscenario.Punto(new GameObject("Proa y sistema de atraque").transform, "VC1/Proa");
            proa.SetParent(Raiz, false);
            Cil("Proa", proa, 2.2f, ZAnillo + 4f, Largo, oscuro);

            // Huecos de los componentes: a lo largo del eje, en orden, sin solaparse (el largo ya se comprobó en los datos)
            float zl = ZBodega + DatosTransporteOrbital.SeparacionM;
            foreach (TipoComponente tc in new[] { TipoComponente.Tanque, TipoComponente.Motor, TipoComponente.Instrumento, TipoComponente.Relevo })
            {
                ElementoTransporte e = t != null ? t.componentes.Find(x => x.tipo == tc) : null;
                if (e == null) continue;
                Transform h = new GameObject("Hueco · " + tc).transform;
                h.SetParent(Raiz, false);
                h.localPosition = new Vector3(0f, -r + 0.3f + e.medidas.diametro / 2f, zl);
                Huecos[tc] = h;
                zl += e.medidas.largo + DatosTransporteOrbital.SeparacionM;
            }
            // Anillo: 12 módulos de combustible (3 × 2 × 2) y tres contenedores
            for (int i = 0; i < DatosTransporteOrbital.ModulosCombustibleMax; i++)
            {
                Transform h = new GameObject("Hueco de combustible " + (i + 1)).transform;
                h.SetParent(Raiz, false);
                int col = i % 3, fila = (i / 3) % 2, capa = i / 6;
                h.localPosition = new Vector3(-2.45f + col * 2.45f, -1.9f + fila * 2.45f, ZAnillo + 0.3f + capa * 1.6f);
                HuecosCombustible.Add(h);
            }
            string[] lotes = { "suministros", "kits", "sonda" };
            for (int i = 0; i < lotes.Length; i++)
            {
                Transform h = new GameObject("Hueco del contenedor · " + lotes[i]).transform;
                h.SetParent(Raiz, false);
                h.localPosition = new Vector3(-2.4f + i * 2.4f, 1.2f, ZAnillo + 3.2f);
                HuecosContenedor[lotes[i]] = h;
            }
            SustitucionEscenario.Aplicar(Raiz);
        }

        /// <summary>0 = compuertas cerradas · 1 = abiertas (giran hacia fuera).</summary>
        public void Puertas(float abierto)
        {
            for (int i = 0; i < bisagras.Count; i++)
            {
                float lado = i == 0 ? -1f : 1f;
                bisagras[i].localRotation = Quaternion.Euler(0f, 0f, -lado * 110f * Mathf.Clamp01(abierto));
            }
        }

        /// <summary>Coloca en la bodega (como hijos) lo que corresponda. Devuelve la pieza puesta en el hueco.</summary>
        public Transform Poner(Transform pieza, Transform hueco)
        {
            if (pieza == null || hueco == null) return null;
            pieza.SetParent(hueco, false);
            pieza.localPosition = Vector3.zero;
            pieza.localRotation = Quaternion.identity;
            return pieza;
        }

        public void Destruir() { if (Raiz != null) UnityEngine.Object.Destroy(Raiz.gameObject); }

        /// <summary>Altura (local al lanzador) donde se apoya la popa del VC-1: la cara superior de la etapa central.</summary>
        public static float ApoyoVC1 => 0.5f + ConfiguracionJuego.Actual.lanzadorVC1.alturaEtapa;

        /// <summary>
        /// Lanzador provisional LC-2 para la plataforma exterior: etapa central y dos aceleradores, con el VC-1 encima dentro
        /// de una cofia. Devuelve la raíz del conjunto (de pie, base en y = 0). El VC-1 queda en «vc1» (hijo del conjunto).
        /// </summary>

        public static Transform Lanzador(Transform padre, Vector3 pos, Func<Color, bool, Material> mat, out Transform cofia, out Transform llama)
        {
            Transform l = new GameObject(DatosTransporteOrbital.NombreLanzador + " (provisional)").transform;
            SustitucionEscenario.Punto(l, "LC2/Lanzador");
            l.SetParent(padre, false);
            l.localPosition = pos;
            Material gris = mat(new Color(0.78f, 0.79f, 0.8f), false), oscuro = mat(new Color(0.18f, 0.19f, 0.22f), false),
                     rojo = mat(new Color(0.7f, 0.2f, 0.15f), false), fuego = mat(new Color(1f, 0.72f, 0.25f), true);
            GameObject Cilv(string n, Vector3 c, float d, float h, Material m)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                g.name = n;
                Collider col = g.GetComponent<Collider>(); col.enabled = false; UnityEngine.Object.Destroy(col);
                g.transform.SetParent(l, false);
                g.transform.localPosition = c;
                g.transform.localScale = new Vector3(d, h / 2f, d);
                g.GetComponent<Renderer>().sharedMaterial = m;
                return g;
            }
            // Proporciones VISUALES (ConfiguracionJuego → lanzadorVC1). Por defecto: etapa Ø 8 × 40, aceleradores Ø 3,2 × 24, cofia Ø 8,6 × 32.
            ProporcionesLC2 c = ConfiguracionJuego.Actual.lanzadorVC1;
            float he = c.alturaEtapa, top = 1f + he;
            Cilv("Etapa central", new Vector3(0f, 1f + he / 2f, 0f), c.diametroEtapa, he, gris);
            Cilv("Franja", new Vector3(0f, top - 3f, 0f), c.diametroEtapa + 0.05f, 1f, rojo);
            Cilv("Toberas", new Vector3(0f, 0.5f, 0f), c.diametroEtapa * 0.625f, 1f, oscuro);
            Transform aceleradores = new GameObject("Aceleradores").transform;
            aceleradores.SetParent(l, false);
            foreach (float x in new[] { -c.separacionAcelerador, c.separacionAcelerador })
            {
                Cilv("Acelerador", new Vector3(x, 1f + c.alturaAcelerador / 2f, 0f), c.diametroAcelerador, c.alturaAcelerador, gris).transform.SetParent(aceleradores, true);
                Cilv("Morro del acelerador", new Vector3(x, 1.6f + c.alturaAcelerador, 0f), c.diametroAcelerador * (2.2f / 3.2f), 1.2f, rojo).transform.SetParent(aceleradores, true);
            }
            cofia = Cilv("Cofia (cubre el VC-1)", new Vector3(0f, top + c.alturaCofia / 2f, 0f), c.diametroCofia, c.alturaCofia, gris).transform;
            Cilv("Punta de la cofia", new Vector3(0f, top + c.alturaCofia + 1f, 0f), c.diametroCofia * (5f / 8.6f), 2f, gris).transform.SetParent(cofia, true);
            GameObject ll = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ll.name = "Llama del lanzador";
            Collider cl = ll.GetComponent<Collider>(); cl.enabled = false; UnityEngine.Object.Destroy(cl);
            ll.transform.SetParent(l, false);
            ll.transform.localPosition = new Vector3(0f, -4f, 0f);
            ll.transform.localScale = new Vector3(6f, 8f, 6f);
            ll.GetComponent<Renderer>().sharedMaterial = fuego;
            ll.SetActive(false);
            llama = ll.transform;
            SustitucionEscenario.Aplicar(l);
            return l;
        }
    }
}
