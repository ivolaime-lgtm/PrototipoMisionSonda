using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Dibujo de la prueba de hangar (SOLO VISUAL; lee PruebaHangar y DefinicionNaveBase). Bloques y materiales simples.
    ///
    /// Interior y exterior salen de LA MISMA definición y cuelgan del mismo objeto «Nave», así que coinciden por
    /// construcción. Motor y tanque se montan en sus anclajes (ANC_I2_Motor y el módulo de servicio entre I3 y
    /// ANC_I2_Motor); al cambiar de variante se destruye la anterior antes de crear la nueva (nunca hay dos).
    /// La cámara de inspección es PROVISIONAL: sin colisiones, solo para mirar el bloque.
    /// </summary>
    public class VistaHangar : MonoBehaviour
    {
        static readonly Vector3 PosicionNave = new Vector3(0f, 2.2f, 0f);   // suelo interior a 2,2 m sobre el hangar

        PruebaHangar prueba;
        DefinicionNaveBase b;
        Camera camara;
        Font fuente;
        Transform nave, interior, exterior, techos, cubierta, etiquetas, montajes, referencias;
        Transform anclajeMotor, anclajeModulo, motorActual, moduloActual;
        readonly List<(Renderer r, EstadoMedida e, Material normal)> estados = new List<(Renderer, EstadoMedida, Material)>();
        readonly List<TextMesh> textos = new List<TextMesh>();
        Material matCasco, matMuro, matSuelo, matTecho, matOscuro, matNaranja, matVidrio, matHangar, matSoporte;
        Material matConservada, matPropuesta, matPendiente, matReserva, matReservaVol;
        bool estadosMostrados;
        OpcionMotorNave motorMostrado;
        OpcionTanqueNave tanqueMostrado;

        // Cámara
        float yaw = 35f, pitch = 22f, distancia = 42f;
        Vector3 ojo;                      // inspección: posición en el sistema de la nave
        float miraYaw, miraPitch;

        public Camera Camara => camara;
        /// <summary>Piezas montadas ahora mismo en cada anclaje (para la prueba de duplicados).</summary>
        public int PiezasEnAnclajeMotor => anclajeMotor != null ? anclajeMotor.childCount : 0;
        public int PiezasEnModuloServicio => anclajeModulo != null ? anclajeModulo.childCount : 0;
        public float ZAnclajeMotorActual { get; private set; }

        public void Construir(PruebaHangar p)
        {
            prueba = p;
            b = p.Base;
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject camGo = new GameObject("CamaraHangar");
            camGo.tag = "MainCamera";
            camara = camGo.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.09f, 0.1f, 0.12f);
            camara.nearClipPlane = 0.05f;
            camara.farClipPlane = 400f;
            camara.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();

            GameObject sol = new GameObject("LuzHangar");
            Light luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            sol.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.48f);

            matCasco = Mat(new Color(0.88f, 0.89f, 0.91f));
            matMuro = Mat(new Color(0.72f, 0.74f, 0.78f));
            matSuelo = Mat(new Color(0.36f, 0.38f, 0.42f));
            matTecho = Mat(new Color(0.8f, 0.81f, 0.84f));
            matOscuro = Mat(new Color(0.2f, 0.22f, 0.26f));
            matNaranja = Mat(new Color(0.95f, 0.55f, 0.16f));
            matVidrio = Mat(new Color(0.3f, 0.7f, 0.85f));
            matHangar = Mat(new Color(0.24f, 0.25f, 0.27f));
            matSoporte = Mat(new Color(0.85f, 0.7f, 0.2f));
            matConservada = Mat(new Color(0.35f, 0.8f, 0.45f));
            matPropuesta = Mat(new Color(0.35f, 0.6f, 0.95f));
            matPendiente = Mat(new Color(0.95f, 0.72f, 0.25f));
            matReserva = Mat(new Color(1f, 0.5f, 0.15f));
            matReservaVol = MatTransparente(new Color(1f, 0.5f, 0.15f, 0.22f));

            ConstruirHangar();
            nave = new GameObject("Nave · " + b.nombre).transform;
            nave.position = PosicionNave;
            interior = Hijo("Interior (bloque provisional)", nave);
            exterior = Hijo("Exterior (casco provisional)", nave);
            techos = Hijo("Techos interiores", interior);
            cubierta = Hijo("Cubierta del casco", exterior);
            etiquetas = Hijo("Etiquetas", nave);
            montajes = Hijo("Puntos de montaje", nave);
            referencias = Hijo("Referencias de tripulantes (NO son bots ni jugadores)", nave);

            ConstruirInterior();
            ConstruirCasco();
            ConstruirPila();
            ConstruirReferencias();
            ActualizarModulos();
            CambiarModo(PruebaHangar.Modo.Exterior);
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

        static Material MatTransparente(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            Material m = new Material(sh);
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);   // transparente
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static Transform Hijo(string nombre, Transform padre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        static Transform Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            g.name = nombre;
            Collider c = g.GetComponent<Collider>();
            if (c != null) Destroy(c);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        Transform Caja(string nombre, Transform padre, Vector3 min, Vector3 max, Material m) =>
            Pieza(PrimitiveType.Cube, nombre, padre, (min + max) / 2f, max - min, m);

        void Registrar(Transform t, EstadoMedida e)
        {
            Renderer r = t.GetComponent<Renderer>();
            if (r != null) estados.Add((r, e, r.sharedMaterial));
        }

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

        // ------------------------------------------------------------------ Hangar

        void ConstruirHangar()
        {
            Transform h = Hijo("Hangar de prueba", null);
            Caja("Suelo", h, new Vector3(-40f, -0.1f, -50f), new Vector3(40f, 0f, 30f), matHangar);
            Caja("Pared fondo", h, new Vector3(-40f, 0f, 30f), new Vector3(40f, 18f, 30.4f), Mat(new Color(0.3f, 0.32f, 0.35f)));
            // Marcas de suelo cada 5 m para estimar tamaños
            Material linea = Mat(new Color(0.85f, 0.75f, 0.2f));
            for (int i = -6; i <= 6; i++)
                Caja("MarcaSuelo", h, new Vector3(i * 5f - 0.04f, 0f, -45f), new Vector3(i * 5f + 0.04f, 0.01f, 25f), linea);
            // Pasarela provisional hasta la escotilla (el embarque NO está implementado)
            float xEscotilla = 4.4f;
            Caja("Torre de acceso", h, new Vector3(8f, 0f, -7.5f), new Vector3(10f, 2.2f, -5.2f), matSoporte);
            Caja("Pasarela", h, new Vector3(xEscotilla, 2.1f, -7.0f), new Vector3(8f, 2.2f, -5.7f), matSoporte);
            Texto("Pasarela provisional\n(embarque no implementado)", h, new Vector3(9f, 3.4f, -6.35f), 0.06f, new Color(1f, 0.85f, 0.4f));
        }

        // ------------------------------------------------------------------ Interior

        void ConstruirInterior()
        {
            float h = b.alturaLibreM, g = b.grosorMuroM;
            foreach (ZonaInterior z in b.zonas)
            {
                Transform suelo = Caja("Suelo " + z.nombre, interior, new Vector3(z.xMin, -0.2f, z.zMin), new Vector3(z.xMax, 0f, z.zMax),
                                       z.tipo == TipoZona.ReservaCarga ? matReserva : matSuelo);
                Registrar(suelo, z.estado);
                Caja("Techo " + z.nombre, techos, new Vector3(z.xMin, h, z.zMin), new Vector3(z.xMax, h + 0.2f, z.zMax), matTecho);
                Texto($"{z.nombre.ToUpper()}\n{M(z.Ancho)} × {M(z.Largo)} m", etiquetas, z.Centro(h - 0.5f), 0.045f, Color.white);
                if (z.tipo == TipoZona.ReservaCarga)
                {
                    Transform vol = Caja("Volumen reservado de carga", interior, new Vector3(z.xMin + 0.1f, 0f, z.zMin + 0.1f),
                                         new Vector3(z.xMax - 0.1f, 2.0f, z.zMax - 0.1f), matReservaVol);
                    vol.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            foreach (MuroInterior m in b.muros) ConstruirMuro(m, h, g);

            // Asideros provisionales a lo largo del pasillo (dos alturas, cada 1,5 m)
            Material asidero = matNaranja;
            for (float z = -8.5f; z <= 1.0f; z += 1.5f)
                for (int lado = -1; lado <= 1; lado += 2)
                    foreach (float y in new[] { 1.0f, 2.3f })
                        Pieza(PrimitiveType.Cube, "Asidero", interior, new Vector3(lado * 1.45f, y, z), new Vector3(0.05f, 0.05f, 0.6f), asidero);

            // Puestos: consola (sustituto) y asiento con sujeción
            foreach (PuestoBase p in b.puestos)
            {
                Transform puesto = Hijo("Puesto " + p.rol, interior);
                Transform consola = Pieza(PrimitiveType.Cube, p.rol == "Pilotaje" ? "Consola v0 (sustituto: medidas acordadas)" : "Consola (sustituto provisional)",
                                          puesto, p.centroConsola, p.tamanoConsola, matOscuro);
                Registrar(consola, p.estado);
                Transform asiento = Hijo("Asiento con sujeción", puesto);
                asiento.localPosition = p.asiento;
                asiento.localRotation = Quaternion.Euler(0f, p.orientacionGrados, 0f);
                Pieza(PrimitiveType.Cube, "Base", asiento, new Vector3(0f, 0.22f, 0f), new Vector3(0.5f, 0.44f, 0.5f), matOscuro);
                Pieza(PrimitiveType.Cube, "Cojín", asiento, new Vector3(0f, 0.48f, 0f), new Vector3(0.56f, 0.08f, 0.56f), matMuro);
                Transform resp = Pieza(PrimitiveType.Cube, "Respaldo", asiento, new Vector3(0f, 0.95f, -0.3f), new Vector3(0.56f, 0.9f, 0.08f), matMuro);
                resp.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                Pieza(PrimitiveType.Cube, "Arnés", asiento, new Vector3(0f, 0.95f, -0.24f), new Vector3(0.36f, 0.7f, 0.03f), matNaranja);
                Texto($"{p.rol.ToUpper()}\nasiento con sujeción", etiquetas, p.asiento + new Vector3(0f, 2.15f, 0f), 0.035f, p.color);
            }
        }

        void ConstruirMuro(MuroInterior m, float h, float g)
        {
            Vector3 a = new Vector3(m.a.x, 0f, m.a.y), c = new Vector3(m.b.x, 0f, m.b.y);
            float largo = Vector3.Distance(a, c);
            if (largo < 0.01f) return;
            Vector3 dir = (c - a) / largo;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            Transform muro = Hijo("Muro " + m.nombre, interior);
            muro.localPosition = a;
            muro.localRotation = rot;

            // Tramos macizos entre huecos y dinteles sobre cada hueco
            List<HuecoMuro> huecos = new List<HuecoMuro>(m.huecos);
            huecos.Sort((x, y) => x.desde.CompareTo(y.desde));
            float cursor = 0f;
            foreach (HuecoMuro hu in huecos)
            {
                if (hu.desde > cursor)
                    Caja("Tramo", muro, new Vector3(-g / 2f, 0f, cursor), new Vector3(g / 2f, h, hu.desde), matMuro);
                if (hu.alto < h)
                    Caja("Dintel", muro, new Vector3(-g / 2f, hu.alto, hu.desde), new Vector3(g / 2f, h, hu.hasta), matMuro);
                Transform marco = Caja("Marco " + hu.nombre, muro, new Vector3(-g / 2f - 0.02f, 0f, hu.desde), new Vector3(g / 2f + 0.02f, 0.03f, hu.hasta), matNaranja);
                Registrar(marco, hu.estado);
                Vector3 centro = muro.TransformPoint(new Vector3(0f, Mathf.Min(hu.alto, h) + 0.15f, (hu.desde + hu.hasta) / 2f));
                Texto($"{hu.nombre}\n{M(hu.Ancho)} × {M(hu.alto)} m", etiquetas, etiquetas.InverseTransformPoint(centro), 0.025f, new Color(1f, 0.85f, 0.5f));
                cursor = Mathf.Max(cursor, hu.hasta);
            }
            if (cursor < largo)
                Caja("Tramo", muro, new Vector3(-g / 2f, 0f, cursor), new Vector3(g / 2f, h, largo), matMuro);
        }

        // ------------------------------------------------------------------ Casco exterior

        void ConstruirCasco()
        {
            float y0 = b.cascoInferiorY, y1 = b.cascoSuperiorY, e = 0.08f;
            List<TramoCasco> tramos = new List<TramoCasco>(b.casco);
            tramos.Sort((x, y) => y.zMax.CompareTo(x.zMax));   // de proa a popa
            for (int i = 0; i < tramos.Count; i++)
            {
                TramoCasco t = tramos[i];
                float w = t.semiAncho;
                Transform tr = Hijo("Tramo " + t.nombre, exterior);
                Caja("Fondo", tr, new Vector3(-w, y0, t.zMin), new Vector3(w, y0 + e, t.zMax), matCasco);
                Transform cub = Caja("Cubierta " + t.nombre, cubierta, new Vector3(-w, y1 - e, t.zMin), new Vector3(w, y1, t.zMax), matCasco);
                Registrar(cub, t.estado);
                // Costado izquierdo completo; el derecho, con hueco de escotilla si este tramo la contiene
                Caja("Costado izquierdo", tr, new Vector3(-w, y0, t.zMin), new Vector3(-w + e, y1, t.zMax), matCasco);
                float zA = b.escotillaZ - b.escotillaAncho / 2f, zB = b.escotillaZ + b.escotillaAncho / 2f;
                if (zA > t.zMin && zB < t.zMax)
                {
                    Caja("Costado derecho (popa)", tr, new Vector3(w - e, y0, t.zMin), new Vector3(w, y1, zA), matCasco);
                    Caja("Costado derecho (proa)", tr, new Vector3(w - e, y0, zB), new Vector3(w, y1, t.zMax), matCasco);
                    Caja("Costado derecho (bajo escotilla)", tr, new Vector3(w - e, y0, zA), new Vector3(w, 0f, zB), matCasco);
                    Caja("Costado derecho (sobre escotilla)", tr, new Vector3(w - e, b.escotillaAlto, zA), new Vector3(w, y1, zB), matCasco);
                    Caja("Marco escotilla", tr, new Vector3(w - 0.02f, 0f, zA - 0.1f), new Vector3(w + 0.04f, b.escotillaAlto + 0.1f, zA), matNaranja);
                    Caja("Marco escotilla", tr, new Vector3(w - 0.02f, 0f, zB), new Vector3(w + 0.04f, b.escotillaAlto + 0.1f, zB + 0.1f), matNaranja);
                    Caja("Marco escotilla", tr, new Vector3(w - 0.02f, b.escotillaAlto, zA), new Vector3(w + 0.04f, b.escotillaAlto + 0.1f, zB), matNaranja);
                    // Túnel entre la esclusa (x 4,1) y el casco
                    Caja("Túnel escotilla (suelo)", tr, new Vector3(4.2f, -0.2f, zA), new Vector3(w, 0f, zB), matSuelo);
                }
                else Caja("Costado derecho", tr, new Vector3(w - e, y0, t.zMin), new Vector3(w, y1, t.zMax), matCasco);

                // Escalón transversal hacia el tramo siguiente (si cambia el ancho)
                if (i + 1 < tramos.Count)
                {
                    float w2 = tramos[i + 1].semiAncho;
                    if (!Mathf.Approximately(w, w2))
                    {
                        float a = Mathf.Min(w, w2), c = Mathf.Max(w, w2);
                        for (int s = -1; s <= 1; s += 2)
                            Caja("Escalón", tr, new Vector3(s > 0 ? a : -c, y0, t.zMin - e / 2f), new Vector3(s > 0 ? c : -a, y1, t.zMin + e / 2f), matCasco);
                    }
                }
                else
                {
                    // Mamparo de popa (anillo I4)
                    Caja("Mamparo de popa", tr, new Vector3(-w, y0, t.zMin - e), new Vector3(w, y1, t.zMin), matCasco);
                }
                if (i == 0)
                {
                    Caja("Proa", tr, new Vector3(-w, y0, t.zMax), new Vector3(w, y1, t.zMax + e), matCasco);
                    Caja("Ventana", tr, new Vector3(-3.0f, 1.2f, t.zMax + e), new Vector3(3.0f, 2.4f, t.zMax + e + 0.02f), matVidrio);
                }
                Caja("Franja", tr, new Vector3(-0.6f, y1, t.zMin), new Vector3(0.6f, y1 + 0.02f, t.zMax), matNaranja);
            }
            // Soportes del hangar bajo el casco
            foreach (float z in new[] { 2f, -6f, -13f })
                Caja("Soporte", exterior, new Vector3(-1.5f, -PosicionNave.y, z - 0.4f), new Vector3(1.5f, y0, z + 0.4f), matSoporte);
        }

        // ------------------------------------------------------------------ Pila de popa y módulos

        void ConstruirPila()
        {
            float r = b.diametroAnilloM;
            Vector3 eje = new Vector3(0f, b.ejeEmpujeY, 0f);
            Anillo("Anillo I4 (mamparo)", eje + new Vector3(0f, 0f, b.zMamparoI4), r, EstadoMedida.Conservada);
            Marcador("ANC_I4", eje + new Vector3(0f, 0f, b.zMamparoI4), "ANC_I4 · casco");

            // Bodega: solo reserva de volumen (pendiente)
            Transform bod = Pieza(PrimitiveType.Cylinder, "RESERVA bodega (pendiente)", montajes,
                                  eje + new Vector3(0f, 0f, b.zMamparoI4 - b.largoBodegaM / 2f), new Vector3(r, b.largoBodegaM / 2f, r), matReservaVol);
            bod.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bod.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Texto("RESERVA BODEGA Ø4,0 × 2,5 m\n(pendiente: no es un diseño)", etiquetas, eje + new Vector3(0f, 2.6f, b.zMamparoI4 - b.largoBodegaM / 2f), 0.04f, new Color(1f, 0.65f, 0.3f));
            Anillo("Anillo I3", eje + new Vector3(0f, 0f, b.ZAnclajeI3), r, EstadoMedida.Conservada);
            Marcador("ANC_I3_Superior", eje + new Vector3(0f, 0f, b.ZAnclajeI3), "ANC_I3_Superior");

            anclajeModulo = Hijo("Módulo de servicio (desde ANC_I3 hacia popa)", montajes);
            anclajeModulo.localPosition = eje + new Vector3(0f, 0f, b.ZAnclajeI3);
            anclajeMotor = Hijo("ANC_I2_Motor", montajes);
        }

        void Anillo(string nombre, Vector3 centro, float diametro, EstadoMedida e)
        {
            Transform a = Pieza(PrimitiveType.Cylinder, nombre, montajes, centro, new Vector3(diametro, 0.06f, diametro), matOscuro);
            a.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Registrar(a, e);
        }

        void Marcador(string nombre, Vector3 pos, string rotulo)
        {
            Pieza(PrimitiveType.Sphere, "Marca " + nombre, montajes, pos + new Vector3(0f, 2.15f, 0f), Vector3.one * 0.3f, matNaranja);
            Texto(rotulo, etiquetas, pos + new Vector3(0f, 2.6f, 0f), 0.035f, new Color(1f, 0.75f, 0.35f));
        }

        /// <summary>
        /// Monta el motor y el tanque elegidos. Si cambian, DESTRUYE la variante anterior antes de crear la nueva,
        /// así que en cada anclaje hay siempre una sola pieza.
        /// </summary>
        public void ActualizarModulos()
        {
            if (prueba == null || prueba.ResumenNave == null) return;
            OpcionTanqueNave tanque = prueba.ResumenNave.tanque;
            OpcionMotorNave motor = prueba.ResumenNave.motor;
            Vector3 eje = new Vector3(0f, b.ejeEmpujeY, 0f);
            ZAnclajeMotorActual = b.ZAnclajeMotor(tanque);

            if (moduloActual == null || tanque != tanqueMostrado)
            {
                if (moduloActual != null) { moduloActual.SetParent(null); Destroy(moduloActual.gameObject); }
                moduloActual = ConstruirModuloServicio(tanque);
                tanqueMostrado = tanque;
            }
            anclajeMotor.localPosition = eje + new Vector3(0f, 0f, ZAnclajeMotorActual);
            if (motorActual == null || motor != motorMostrado)
            {
                if (motorActual != null) { motorActual.SetParent(null); Destroy(motorActual.gameObject); }
                motorActual = ConstruirMotor(motor);
                motorMostrado = motor;
            }
            if (rotuloMotor != null) rotuloMotor.transform.localPosition = eje + new Vector3(0f, 2.6f, ZAnclajeMotorActual);
        }

        TextMesh rotuloMotor;

        Transform ConstruirModuloServicio(OpcionTanqueNave t)
        {
            float largoCarcasa = b.LargoModuloServicio(t);
            float r = b.diametroAnilloM;
            Transform v = Hijo("Módulo de servicio: " + (t != null ? t.nombre : "tanque"), anclajeModulo);
            // Carcasa abierta (anillos y largueros) para que se vea el tanque de dentro
            for (int i = 0; i <= 2; i++)
            {
                Transform a = Pieza(PrimitiveType.Cylinder, "Cuaderna", v, new Vector3(0f, 0f, -largoCarcasa * i / 2f), new Vector3(r, 0.05f, r), matOscuro);
                a.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = Quaternion.Euler(0f, 0f, 45f + 90f * i) * new Vector3(r / 2f - 0.1f, 0f, 0f);
                Pieza(PrimitiveType.Cube, "Larguero", v, p + new Vector3(0f, 0f, -largoCarcasa / 2f), new Vector3(0.15f, 0.15f, largoCarcasa), matOscuro);
            }
            // Tanque del catálogo, sin escalar: largo y diámetro reales de la opción
            float largo = t != null ? Mathf.Max(1f, t.largo) : 3.2f;
            float diam = t != null ? Mathf.Max(0.5f, t.diametro) : 2.0f;
            Material mt = Mat(t != null ? t.color : new Color(0.5f, 0.55f, 0.62f));
            Transform tq = Pieza(PrimitiveType.Cylinder, "Tanque", v, new Vector3(0f, 0f, -largoCarcasa / 2f), new Vector3(diam, largo / 2f, diam), mt);
            tq.localRotation = Quaternion.Euler(90f, 0f, 0f);
            int bandas = Mathf.Max(1, Mathf.RoundToInt(largo / 2f));
            for (int i = 0; i < bandas; i++)
            {
                float z = -largoCarcasa / 2f - largo / 2f + largo * (i + 1) / (bandas + 1);
                Transform ba = Pieza(PrimitiveType.Cylinder, "Banda", v, new Vector3(0f, 0f, z), new Vector3(diam + 0.06f, 0.06f, diam + 0.06f), matNaranja);
                ba.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Texto($"{(t != null ? t.nombre : "Tanque")}\n{largo:0.0} × Ø{diam:0.0} m · carcasa {largoCarcasa:0.0} m", v,
                  new Vector3(0f, 2.3f, -largoCarcasa / 2f), 0.035f, Color.white);
            return v;
        }

        Transform ConstruirMotor(OpcionMotorNave m)
        {
            Transform v = Hijo("Motor: " + (m != null ? m.nombre : "motor"), anclajeMotor);
            float t = m != null ? Mathf.Clamp(m.tamanoTobera, 0.5f, 1.6f) : 1f;
            Material mat = Mat(m != null ? m.color : new Color(0.35f, 0.37f, 0.4f));
            float dMax = b.diametroMaximoMotorM;
            Transform placa = Pieza(PrimitiveType.Cylinder, "Placa de montaje", v, new Vector3(0f, 0f, -0.1f), new Vector3(2.2f, 0.1f, 2.2f), matOscuro);
            placa.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float[] z = { -0.4f, -0.9f, -1.5f, -2.1f };
            float[] d = { 1.0f, 1.5f, 2.0f, 2.35f };
            for (int i = 0; i < z.Length; i++)
            {
                Transform c = Pieza(PrimitiveType.Cylinder, "Tobera", v, new Vector3(0f, 0f, z[i]),
                                    new Vector3(Mathf.Min(dMax, d[i] * t), 0.25f, Mathf.Min(dMax, d[i] * t)), mat);
                c.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            int aux = m != null ? Mathf.Clamp(m.toberasAuxiliares, 0, 8) : 0;
            for (int i = 0; i < aux; i++)
            {
                Vector3 p = Quaternion.Euler(0f, 0f, 45f + i * 360f / aux) * new Vector3(1.35f, 0f, 0f);
                Transform c = Pieza(PrimitiveType.Cylinder, "Tobera auxiliar", v, p + new Vector3(0f, 0f, -0.8f), new Vector3(0.45f, 0.35f, 0.45f), mat);
                c.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            // Reserva para la unión al lanzador (pendiente), detrás del motor
            Transform res = Pieza(PrimitiveType.Cylinder, "RESERVA unión al lanzador (pendiente)", v,
                                  new Vector3(0f, 0f, -b.largoMaximoMotorM - b.largoReservaLanzadorM / 2f),
                                  new Vector3(b.diametroAnilloM, b.largoReservaLanzadorM / 2f, b.diametroAnilloM), matReservaVol);
            res.localRotation = Quaternion.Euler(90f, 0f, 0f);
            res.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Texto($"{(m != null ? m.nombre : "Motor")} · en ANC_I2_Motor\nRESERVA unión al lanzador (pendiente)", v,
                  new Vector3(0f, 2.4f, -1.5f), 0.035f, Color.white);
            return v;
        }

        // ------------------------------------------------------------------ Referencias de tripulantes

        void ConstruirReferencias()
        {
            Material piel = Mat(new Color(0.85f, 0.85f, 0.88f));
            foreach (PuestoBase p in b.puestos)
            {
                Vector3 lado = Quaternion.Euler(0f, p.orientacionGrados, 0f) * new Vector3(0.75f, 0f, -0.2f);
                Vector3 pie = p.asiento + lado;
                Transform r = Hijo("Referencia " + p.rol, referencias);
                r.localPosition = pie;
                Pieza(PrimitiveType.Capsule, "Cuerpo 1,75 m", r, new Vector3(0f, 0.875f, 0f), new Vector3(0.5f, 0.875f, 0.5f), Mat(p.color));
                Texto($"{p.rol}\n(referencia 1,75 m · no es bot ni jugador)", etiquetas, pie + new Vector3(0f, 2.0f, 0f), 0.022f, p.color);
            }
            // Maniquí de medida flotando en el pasillo: comprueba que otro tripulante puede pasar al lado
            Transform f = Hijo("Maniquí de medida flotando (no es tripulante)", referencias);
            f.localPosition = new Vector3(-0.7f, 1.7f, -3.0f);
            Transform cuerpo = Pieza(PrimitiveType.Capsule, "Cuerpo tumbado", f, Vector3.zero, new Vector3(0.5f, 0.875f, 0.5f), piel);
            cuerpo.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Pieza(PrimitiveType.Cube, "Brazos extendidos (2,1 m)", f, new Vector3(0f, 0f, 0.7f), new Vector3(0.12f, 0.12f, 0.6f), piel);
            Transform paso = Hijo("Maniquí de medida de pie (no es tripulante)", referencias);
            paso.localPosition = new Vector3(0.75f, 0f, -3.0f);
            Pieza(PrimitiveType.Capsule, "Cuerpo 1,75 m", paso, new Vector3(0f, 0.875f, 0f), new Vector3(0.5f, 0.875f, 0.5f), piel);
            Texto("Maniquíes de medida: uno flotando y otro\npasando al lado en el pasillo de 3,0 m", etiquetas, new Vector3(0f, 3.0f, -3.0f), 0.022f, Color.white);
        }

        // ------------------------------------------------------------------ Modos y cámara

        public void CambiarModo(PruebaHangar.Modo modo)
        {
            if (modo == PruebaHangar.Modo.Inspeccion)
            {
                ojo = new Vector3(0f, 1.65f, -7.5f);
                miraYaw = 0f; miraPitch = 0f;
            }
        }

        void LateUpdate()
        {
            if (prueba == null) return;
            bool inspeccion = prueba.ModoActual == PruebaHangar.Modo.Inspeccion;
            bool verInterior = inspeccion || prueba.Corte;
            if (cubierta.gameObject.activeSelf == verInterior) cubierta.gameObject.SetActive(!verInterior);
            bool verTechos = inspeccion;
            if (techos.gameObject.activeSelf != verTechos) techos.gameObject.SetActive(verTechos);
            if (rotuloMotor == null) rotuloMotor = Texto("ANC_I2_Motor", etiquetas, Vector3.zero, 0.035f, new Color(1f, 0.75f, 0.35f));
            rotuloMotor.transform.localPosition = new Vector3(0f, b.ejeEmpujeY + 2.6f, ZAnclajeMotorActual);

            if (prueba.EstadosVisibles != estadosMostrados)
            {
                estadosMostrados = prueba.EstadosVisibles;
                foreach ((Renderer r, EstadoMedida e, Material normal) in estados)
                    if (r != null) r.sharedMaterial = estadosMostrados ? MatEstado(e) : normal;
            }

            if (inspeccion) CamaraInspeccion(); else CamaraExterior();

            // Rótulos de cara a la cámara
            foreach (TextMesh t in textos)
                if (t != null) t.transform.rotation = Quaternion.LookRotation(t.transform.position - camara.transform.position, camara.transform.up);
        }

        Material MatEstado(EstadoMedida e)
        {
            switch (e)
            {
                case EstadoMedida.Conservada: return matConservada;
                case EstadoMedida.Propuesta: return matPropuesta;
                case EstadoMedida.PendientePrueba: return matPendiente;
                default: return matReserva;
            }
        }

        void CamaraExterior()
        {
            Mouse raton = Mouse.current;
            if (raton != null)
            {
                if (raton.rightButton.isPressed)
                {
                    Vector2 d = raton.delta.ReadValue();
                    yaw += d.x * 0.2f;
                    pitch = Mathf.Clamp(pitch - d.y * 0.2f, -5f, 85f);
                }
                float rueda = raton.scroll.ReadValue().y;
                if (Mathf.Abs(rueda) > 0.01f) distancia = Mathf.Clamp(distancia * (rueda > 0f ? 0.9f : 1.1f), 8f, 120f);
            }
            Vector3 foco = nave.position + new Vector3(0f, 1.5f, -8f);
            camara.transform.position = foco + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distancia);
            camara.transform.LookAt(foco);
        }

        /// <summary>Cámara de inspección PROVISIONAL (sin colisiones): WASD, Espacio/Ctrl, ratón derecho, 1–4 puestos, 0 esclusa.</summary>
        void CamaraInspeccion()
        {
            Keyboard k = Keyboard.current;
            Mouse raton = Mouse.current;
            if (raton != null && raton.rightButton.isPressed)
            {
                Vector2 d = raton.delta.ReadValue();
                miraYaw += d.x * 0.15f;
                miraPitch = Mathf.Clamp(miraPitch - d.y * 0.15f, -85f, 85f);
            }
            if (k != null)
            {
                for (int i = 0; i < b.puestos.Count && i < 4; i++)
                {
                    bool pulsada = i == 0 ? k.digit1Key.wasPressedThisFrame : i == 1 ? k.digit2Key.wasPressedThisFrame
                                 : i == 2 ? k.digit3Key.wasPressedThisFrame : k.digit4Key.wasPressedThisFrame;
                    if (pulsada)
                    {
                        PuestoBase p = b.puestos[i];
                        ojo = p.asiento + new Vector3(0f, 1.2f, 0f);   // ojos de alguien sentado
                        miraYaw = p.orientacionGrados;
                        miraPitch = 10f;
                    }
                }
                if (k.digit0Key.wasPressedThisFrame)
                {
                    ojo = new Vector3(2.85f, 1.65f, -6.35f);
                    miraYaw = -90f; miraPitch = 0f;
                }
                Quaternion giro = Quaternion.Euler(0f, miraYaw, 0f);
                Vector3 mov = Vector3.zero;
                if (k.wKey.isPressed) mov += Vector3.forward;
                if (k.sKey.isPressed) mov += Vector3.back;
                if (k.dKey.isPressed) mov += Vector3.right;
                if (k.aKey.isPressed) mov += Vector3.left;
                mov = giro * mov;
                if (k.spaceKey.isPressed) mov += Vector3.up;
                if (k.leftCtrlKey.isPressed) mov += Vector3.down;
                float vel = k.leftShiftKey.isPressed ? 5f : 2.2f;
                ojo += mov * vel * Time.deltaTime;
                ojo.y = Mathf.Clamp(ojo.y, 0.3f, b.alturaLibreM - 0.1f);
            }
            camara.transform.position = nave.TransformPoint(ojo);
            camara.transform.rotation = nave.rotation * Quaternion.Euler(miraPitch, miraYaw, 0f);
        }
    }
}
