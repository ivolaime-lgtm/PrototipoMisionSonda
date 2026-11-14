using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// REPRESENTACIÓN PROVISIONAL de la lanzadera L-4 en vertical (solo visual, sin colisiones), construida a partir de
    /// su ConfiguracionLanzadera. La usan el edificio de integración y la secuencia de despegue, así que el vehículo que
    /// despega es el mismo que se preparó. Geometría de bloques: no es el modelo final ni copia ningún vehículo real.
    ///
    /// Origen = base de la etapa principal (y = 0). De abajo arriba: toberas · etapa principal · [tanque de transferencia]
    /// · [bodega] · adaptador · cápsula · torre de escape. Los refuerzos van alrededor de la etapa principal.
    /// </summary>
    public class ModeloLanzadera
    {
        public const float Diametro = 5f, AlturaEtapa = 18f;

        public Transform Raiz { get; private set; }
        /// <summary>Punto de anclaje en el suelo de la cápsula (para la cabina interior de la secuencia de despegue).</summary>
        public Transform Capsula { get; private set; }
        /// <summary>Pieza de cada tipo (null si la opción elegida es «sin»).</summary>
        public readonly Dictionary<PiezaLanzadera, Transform> Piezas = new Dictionary<PiezaLanzadera, Transform>();
        /// <summary>Altura media de cada tipo de pieza (aunque falte), para enfocar la cámara.</summary>
        public readonly Dictionary<PiezaLanzadera, float> AlturaPieza = new Dictionary<PiezaLanzadera, float>();
        /// <summary>Renderers del exterior (se ocultan cuando la cámara está dentro de la cápsula).</summary>
        public readonly List<Renderer> Exterior = new List<Renderer>();
        public Transform Llama { get; private set; }
        public float AlturaTotal { get; private set; }

        readonly Func<Color, bool, Material> mat;
        Material mCasco, mOscuro, mNaranja, mGris, mFuego, mVentana;

        ModeloLanzadera(Func<Color, bool, Material> fabrica) { mat = fabrica; }

        /// <summary>Construye la lanzadera. «resaltar» y «mResaltado» pintan una pieza; «nueva» y «mNueva», la previsualizada.</summary>
        public static ModeloLanzadera Construir(Transform padre, Vector3 posLocal, ConfiguracionLanzadera c, Func<Color, bool, Material> fabrica,
                                                PiezaLanzadera? resaltar = null, Material mResaltado = null, bool orbital = false)
        {
            ModeloLanzadera m = new ModeloLanzadera(fabrica);
            m.Materiales();
            m.Montar(padre, posLocal, c, resaltar, mResaltado, orbital);
            SustitucionEscenario.Aplicar(m.Raiz);
            return m;
        }

        /// <summary>Lo que no se oculta con MostrarExterior (la cabina interior, que cuelga de la cápsula).</summary>
        public Transform Interior { get; set; }
        /// <summary>Huecos de la bodega para los contenedores que viajan en la L-4 (vacío si no hay bodega).</summary>
        public readonly List<Transform> HuecosBodega = new List<Transform>();

        public void Destruir() { if (Raiz != null) UnityEngine.Object.Destroy(Raiz.gameObject); }

        void Materiales()
        {
            mCasco = mat(new Color(0.9f, 0.9f, 0.92f), false);
            mOscuro = mat(new Color(0.16f, 0.17f, 0.2f), false);
            mNaranja = mat(new Color(0.95f, 0.5f, 0.12f), false);
            mGris = mat(new Color(0.55f, 0.58f, 0.62f), false);
            mFuego = mat(new Color(1f, 0.72f, 0.25f), true);
            mVentana = mat(new Color(0.1f, 0.25f, 0.35f), false);
        }

        Transform Cilindro(string n, Transform padre, Vector3 pos, float d, float h, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = n;
            Collider col = g.GetComponent<Collider>(); col.enabled = false; UnityEngine.Object.Destroy(col);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = new Vector3(d, h / 2f, d);
            Renderer r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            Exterior.Add(r);
            return g.transform;
        }

        Transform Caja(string n, Transform padre, Vector3 pos, Vector3 esc, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            Collider col = g.GetComponent<Collider>(); col.enabled = false; UnityEngine.Object.Destroy(col);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = esc;
            Renderer r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            Exterior.Add(r);
            return g.transform;
        }

        static void Pintar(Transform t, Material m)
        {
            if (t == null || m == null) return;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>()) r.sharedMaterial = m;
        }

        public static float AlturaTanque(int i) => i == 0 ? 0f : i == 1 ? 3f : 5f;
        public static float AlturaBodega(int i) => i == 0 ? 0f : i == 1 ? 2.5f : 4f;

        void Montar(Transform padre, Vector3 posLocal, ConfiguracionLanzadera c, PiezaLanzadera? resaltar, Material mResaltado, bool orbital)
        {
            Raiz = new GameObject($"{DatosLanzadera.Nombre} (provisional) · {DatosLanzadera.Resumen(c)}" + (orbital ? " · en órbita" : "")).transform;
            Raiz.SetParent(padre, false);
            Raiz.localPosition = posLocal;

            // Toberas y etapa principal (en órbita ya se separaron: solo queda la parte superior, con su bodega y su tanque)
            if (!orbital)
            {
                Transform etapa = SustitucionEscenario.Punto(new GameObject("Etapa principal (pieza)").transform, "L4/EtapaPrincipal");
                etapa.SetParent(Raiz, false);
                Cilindro("Toberas", etapa, new Vector3(0f, -0.5f, 0f), 3.2f, 1f, mOscuro);
                Cilindro("Etapa principal", etapa, new Vector3(0f, AlturaEtapa / 2f, 0f), Diametro, AlturaEtapa, mCasco);
                Cilindro("Franja", etapa, new Vector3(0f, AlturaEtapa - 1f, 0f), Diametro + 0.05f, 0.6f, mOscuro);
            }
            Llama = Caja("Llama", Raiz, new Vector3(0f, -3.5f, 0f), new Vector3(3f, 5f, 3f), mFuego);
            Exterior.Remove(Llama.GetComponent<Renderer>());
            Llama.gameObject.SetActive(false);

            // Refuerzos alrededor de la etapa
            int n = orbital ? 0 : DatosLanzadera.Refuerzos()[c.refuerzos].unidades;
            AlturaPieza[PiezaLanzadera.Refuerzos] = 7f;
            if (n > 0)
            {
                Transform g = new GameObject("Propulsores de refuerzo × " + n).transform;
                g.SetParent(Raiz, false);
                for (int i = 0; i < n; i++)
                {
                    float ang = (n == 2 ? 90f + 180f * i : 45f + 90f * i) * Mathf.Deg2Rad;
                    Vector3 p = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (Diametro / 2f + 1.05f);
                    Cilindro("Refuerzo", g, p + Vector3.up * 7f, 1.8f, 14f, mCasco);
                    Cilindro("Morro del refuerzo", g, p + Vector3.up * 14.6f, 1.3f, 1.2f, mNaranja);
                    Cilindro("Tobera del refuerzo", g, p + Vector3.up * -0.4f, 1.2f, 0.8f, mOscuro);
                }
                Piezas[PiezaLanzadera.Refuerzos] = g;
                SustitucionEscenario.Punto(g, "L4/Refuerzos/" + n);
            }

            float y = orbital ? 0f : AlturaEtapa;
            // Tanque de transferencia
            float ht = AlturaTanque(c.tanque);
            AlturaPieza[PiezaLanzadera.Tanque] = y + Mathf.Max(ht, 2f) / 2f;
            if (ht > 0f)
            {
                Transform t = new GameObject("Tanque de transferencia").transform;
                t.SetParent(Raiz, false);
                t.localPosition = new Vector3(0f, y, 0f);
                Cilindro("Tanque", t, new Vector3(0f, ht / 2f, 0f), Diametro, ht, mNaranja);
                Cilindro("Aro", t, new Vector3(0f, ht - 0.15f, 0f), Diametro + 0.05f, 0.3f, mGris);
                Piezas[PiezaLanzadera.Tanque] = t;
                SustitucionEscenario.Punto(t, "L4/TanqueTransferencia/" + c.tanque);
                y += ht;
            }
            // Bodega de carga
            float hb = AlturaBodega(c.bodega);
            AlturaPieza[PiezaLanzadera.Bodega] = y + Mathf.Max(hb, 2f) / 2f;
            if (hb > 0f)
            {
                Transform b = new GameObject("Bodega de carga").transform;
                b.SetParent(Raiz, false);
                b.localPosition = new Vector3(0f, y, 0f);
                Cilindro("Bodega", b, new Vector3(0f, hb / 2f, 0f), Diametro, hb, mGris);
                Caja("Compuerta de la bodega", b, new Vector3(0f, hb / 2f, Diametro / 2f - 0.05f), new Vector3(2.4f, hb * 0.7f, 0.2f), mOscuro);
                Piezas[PiezaLanzadera.Bodega] = b;
                SustitucionEscenario.Punto(b, "L4/Bodega/" + c.bodega);
                for (int i = 0; i < 3; i++)
                {
                    Transform hb2 = new GameObject("Hueco de la bodega " + (i + 1)).transform;
                    hb2.SetParent(b, false);
                    hb2.localPosition = new Vector3(-1.3f + i * 1.3f, 0.15f, 0.6f);
                    HuecosBodega.Add(hb2);
                }
                y += hb;
            }
            // Adaptador, cápsula y torre de escape
            Transform capPieza = SustitucionEscenario.Punto(new GameObject("Cápsula (pieza)").transform, "L4/Capsula");
            capPieza.SetParent(Raiz, false);
            Cilindro("Adaptador", capPieza, new Vector3(0f, y + 0.5f, 0f), 4.4f, 1f, mOscuro);
            y += 1f;
            Capsula = new GameObject("Cápsula (anclaje)").transform;
            Capsula.SetParent(Raiz, false);
            Capsula.localPosition = new Vector3(0f, y, 0f);
            Cilindro("Cápsula", capPieza, new Vector3(0f, y + 1.6f, 0f), 4.4f, 3.2f, mCasco);
            Cilindro("Cápsula (hombro)", capPieza, new Vector3(0f, y + 3.6f, 0f), 3.2f, 0.8f, mCasco);
            Cilindro("Cápsula (morro)", capPieza, new Vector3(0f, y + 4.25f, 0f), 2f, 0.5f, mCasco);
            Caja("Escotilla", capPieza, new Vector3(0f, y + 1.5f, -2.15f), new Vector3(1.2f, 1.8f, 0.15f), mOscuro);
            Caja("Ventanas", capPieza, new Vector3(0f, y + 2.6f, 2.15f), new Vector3(1.8f, 0.5f, 0.12f), mVentana);
            if (!orbital) Cilindro("Torre de escape", capPieza, new Vector3(0f, y + 6f, 0f), 0.5f, 3f, mNaranja);
            AlturaTotal = orbital ? y + 4.5f : y + 7.5f;

            if (resaltar.HasValue)
            {
                Transform pr;
                if (Piezas.TryGetValue(resaltar.Value, out pr)) Pintar(pr, mResaltado);
            }
        }

        /// <summary>Pinta una pieza (para resaltar la seleccionada o marcar la previsualizada).</summary>
        public void PintarPieza(PiezaLanzadera t, Material m)
        {
            Transform p;
            if (Piezas.TryGetValue(t, out p)) Pintar(p, m);
        }

        /// <summary>Muestra u oculta el exterior (también los modelos que lo sustituyan), salvo la llama y el interior.</summary>
        public void MostrarExterior(bool ver)
        {
            if (Raiz == null) return;
            foreach (Renderer r in Raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || (Llama != null && r.transform.IsChildOf(Llama)) || (Interior != null && r.transform.IsChildOf(Interior))) continue;
                r.enabled = ver;
            }
        }
    }
}
