using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// SECUENCIA BREVE DE DESPEGUE (solo representación; no es pilotable ni una simulación atmosférica).
    /// Traslado desde el edificio de integración a la plataforma → acceso y embarque → asiento y cierre de compuerta →
    /// comprobación de la preparación → cuenta atrás → encendido → ascenso → transición a la llegada a la EM-4X.
    /// La lanzadera L-4 se monta con ModeloLanzadera y la configuración CONFIRMADA (la misma que se vio en la integración).
    /// Es terrestre y distinta de la EM-4X, que espera en órbita.
    ///
    /// Se puede omitir (Espacio) solo después de haberla visto una vez en esta sesión. Omitirla no cambia nada: los
    /// requisitos se comprueban ANTES de empezar y al final se llama a la misma acción que al terminarla.
    /// </summary>
    public class SecuenciaDespegue : MonoBehaviour
    {
        static bool vista;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { vista = false; }

        public bool Activa { get; private set; }
        public static bool PuedeOmitirse => vista;

        static readonly Vector3 Lugar = new Vector3(5000f, 0f, 0f);     // lejos del hangar: plataforma de lanzamiento
        const float AlturaPlataformaMovil = 2f;
        static readonly Vector3 SalidaEdificio = new Vector3(0f, 0f, 95f);   // donde empieza el traslado (local a Lugar)

        Camera cam;
        Transform raiz, conjunto, cabina, compuerta, brazo;
        ModeloLanzadera modelo;
        Action alTerminar;
        List<string> lineas = new List<string>();
        string titulo = "", grande = "";
        int lineasVisibles;
        float fundido;
        bool terminada;
        AudioSource audioFuente;
        AudioClip pitido, motor;
        GUIStyle sTitulo, sTexto, sGrande, sPista;
        Color cieloTierra = new Color(0.55f, 0.72f, 0.9f), cieloEspacio = new Color(0.01f, 0.01f, 0.03f);
        Material mInterior, mNaranja, mPantalla;

        static Material Mat(Color c, bool sinLuz)
        {
            Shader sh = Shader.Find(sinLuz ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find(sinLuz ? "Unlit/Color" : "Standard");
            Material m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        Transform Caja(string n, Transform padre, Vector3 pos, Vector3 esc, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            Collider c = g.GetComponent<Collider>(); c.enabled = false; Destroy(c);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = esc;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        /// <summary>Escenario fijo: terreno, plataforma de lanzamiento, torre con brazo de acceso y edificio de integración.</summary>
        void Construir()
        {
            raiz = new GameObject("Plataforma de lanzamiento (provisional)").transform;
            raiz.position = Lugar;
            Material suelo = Mat(new Color(0.45f, 0.5f, 0.38f), false), hormigon = Mat(new Color(0.62f, 0.62f, 0.6f), false),
                     pared = Mat(new Color(0.55f, 0.57f, 0.6f), false), oscuro = Mat(new Color(0.2f, 0.21f, 0.24f), false);
            mNaranja = Mat(new Color(1f, 0.55f, 0.15f), false);
            mInterior = Mat(new Color(0.3f, 0.33f, 0.38f), false);
            mPantalla = Mat(new Color(0.3f, 0.85f, 1f), true);
            Caja("Terreno", raiz, new Vector3(0f, -0.5f, 0f), new Vector3(800f, 1f, 800f), suelo);
            Caja("Plataforma de lanzamiento", raiz, new Vector3(0f, 0.5f, 0f), new Vector3(18f, 1f, 18f), hormigon);
            Caja("Camino de traslado", raiz, new Vector3(0f, 0.02f, SalidaEdificio.z / 2f), new Vector3(10f, 0.05f, SalidaEdificio.z), oscuro);
            Caja("Torre de servicio", raiz, new Vector3(0f, 22f, -10f), new Vector3(3f, 44f, 3f), mNaranja);
            brazo = Caja("Brazo de acceso", raiz, new Vector3(0f, 20f, -5.4f), new Vector3(1.6f, 0.3f, 5.6f), mNaranja);

            // Edificio de integración visto desde fuera (de aquí sale la lanzadera montada)
            Vector3 e = SalidaEdificio + new Vector3(0f, 0f, 18f);
            Caja("Edificio de integración (izquierda)", raiz, e + new Vector3(-13f, 23f, 0f), new Vector3(10f, 46f, 36f), pared);
            Caja("Edificio de integración (derecha)", raiz, e + new Vector3(13f, 23f, 0f), new Vector3(10f, 46f, 36f), pared);
            Caja("Edificio de integración (dintel)", raiz, e + new Vector3(0f, 44f, 0f), new Vector3(16f, 4f, 36f), pared);
            Caja("Edificio de integración (fondo)", raiz, e + new Vector3(0f, 23f, 17.5f), new Vector3(16f, 46f, 1f), oscuro);

            GameObject cg = new GameObject("Cámara del despegue");
            cam = cg.AddComponent<Camera>();
            cam.enabled = false;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cieloTierra;

            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            audioFuente.volume = 0.4f;
            pitido = Tono("pitido", 990f, 0.15f, 0f);
            motor = Tono("motor", 70f, 3f, 0.5f);
        }

        /// <summary>Monta la lanzadera con la configuración confirmada, sobre su plataforma móvil, y la cabina interior.</summary>
        void MontarLanzadera(ConfiguracionLanzadera config, Func<Color, bool, Material> fabrica)
        {
            if (modelo != null) modelo.Destruir();        // tras el ascenso cuelga de «raiz», no del conjunto
            if (conjunto != null) Destroy(conjunto.gameObject);
            conjunto = new GameObject("Lanzadera L-4 y plataforma móvil").transform;
            conjunto.SetParent(raiz, false);
            Caja("Plataforma móvil", conjunto, new Vector3(0f, AlturaPlataformaMovil / 2f, 0f), new Vector3(12f, AlturaPlataformaMovil, 12f), mInterior);
            ConfiguracionLanzadera c = config != null ? config.Copia() : new ConfiguracionLanzadera();
            DatosLanzadera.Limitar(c);
            modelo = ModeloLanzadera.Construir(conjunto, new Vector3(0f, AlturaPlataformaMovil + 1f, 0f), c, fabrica ?? new Func<Color, bool, Material>(Mat));
            configActual = c;

            // Cabina interior (se ve desde el asiento): paneles con ventana en el techo. Girada para que la compuerta
            // quede en el lado de la escotilla (−z).
            cabina = new GameObject("Cabina (interior)").transform;
            cabina.SetParent(modelo.Capsula, false);
            cabina.localPosition = new Vector3(0f, 1.5f, 0f);
            cabina.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Caja("Suelo de la cabina", cabina, new Vector3(0f, -1.4f, 0f), new Vector3(3.6f, 0.1f, 2.8f), mInterior);
            Caja("Pared trasera", cabina, new Vector3(0f, 0f, -1.4f), new Vector3(3.6f, 2.8f, 0.1f), mInterior);
            Caja("Pared delantera", cabina, new Vector3(0f, 0f, 1.4f), new Vector3(3.6f, 2.8f, 0.1f), mInterior);
            Caja("Pared derecha", cabina, new Vector3(1.8f, 0f, 0f), new Vector3(0.1f, 2.8f, 2.8f), mInterior);
            Caja("Pared izquierda (marco de la compuerta)", cabina, new Vector3(-1.8f, 0f, -0.95f), new Vector3(0.1f, 2.8f, 0.9f), mInterior);
            Caja("Pared izquierda (marco de la compuerta)", cabina, new Vector3(-1.8f, 0f, 0.95f), new Vector3(0.1f, 2.8f, 0.9f), mInterior);
            Caja("Techo (marco)", cabina, new Vector3(0f, 1.4f, -1f), new Vector3(3.6f, 0.1f, 0.8f), mInterior);
            Caja("Techo (marco)", cabina, new Vector3(0f, 1.4f, 1f), new Vector3(3.6f, 0.1f, 0.8f), mInterior);
            Caja("Consola", cabina, new Vector3(0f, 0.8f, 1.1f), new Vector3(2.2f, 0.5f, 0.4f), mPantalla);
            Caja("Asiento", cabina, new Vector3(0f, -0.9f, -0.3f), new Vector3(0.7f, 0.2f, 1.5f), mNaranja);
            compuerta = Caja("Compuerta", cabina, new Vector3(-1.85f, 0f, -1.2f), new Vector3(0.08f, 2f, 1.1f), mNaranja);
            cabina.gameObject.SetActive(false);
        }

        static AudioClip Tono(string nombre, float hz, float s, float ruido)
        {
            const int f = 22050;
            int n = Mathf.CeilToInt(f * s);
            float[] d = new float[n];
            System.Random r = new System.Random(3);
            for (int i = 0; i < n; i++)
                d[i] = (Mathf.Sin(2f * Mathf.PI * hz * i / f) * (1f - ruido) + ((float)r.NextDouble() * 2f - 1f) * ruido)
                       * Mathf.Min(1f, Mathf.Min(i, n - i) / (f * 0.02f)) * 0.6f;
            AudioClip c = AudioClip.Create(nombre, n, 1, f, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>Vista desde dentro (cabina visible, exterior oculto) o desde fuera.</summary>
        void Dentro(bool dentro)
        {
            modelo.MostrarExterior(!dentro);
            cabina.gameObject.SetActive(dentro);
        }

        /// <summary>
        /// Empieza la secuencia. «comprobacion» son las líneas de la comprobación (requisitos ya cumplidos); «config» es la
        /// configuración CONFIRMADA de la lanzadera; «fabrica» da los materiales (null = propios).
        /// </summary>
        public void Iniciar(Vector3 ojos, List<string> comprobacion, ConfiguracionLanzadera config, Func<Color, bool, Material> fabrica, Action fin)
        {
            if (cam == null) Construir();
            cam.transform.SetParent(null, true);          // antes de rehacer la lanzadera (la cámara pudo quedar en la cabina)
            MontarLanzadera(config, fabrica);
            lineas = comprobacion;
            alTerminar = fin;
            fundido = 0f; lineasVisibles = 0; terminada = false;
            titulo = ""; grande = "";
            conjunto.localPosition = SalidaEdificio;
            cam.transform.SetParent(null, true);
            cam.backgroundColor = cieloTierra;
            Dentro(false);
            cam.transform.position = Lugar + new Vector3(55f, 14f, 40f);
            cam.transform.LookAt(conjunto.position + Vector3.up * 16f);
            cam.enabled = true;
            cam.tag = "MainCamera";
            Activa = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StartCoroutine(Ejecutar());
        }

        IEnumerator Ejecutar()
        {
            // 0 · Del hangar a la salida (breve)
            titulo = "EMBARQUE CONFIRMADO";
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / 0.5f) yield return null;

            // 1 · Traslado: la lanzadera montada sale del edificio de integración hacia la plataforma (no es la plataforma)
            titulo = "TRASLADO A LA PLATAFORMA · L-4 montada: " + DatosLanzadera.Resumen(ConfigActual());
            Vector3 punto = Lugar + new Vector3(55f, 14f, 40f);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 4f)
            {
                fundido = Mathf.Max(0f, 1f - s * 4f);
                conjunto.localPosition = Vector3.Lerp(SalidaEdificio, Vector3.zero, Mathf.SmoothStep(0f, 1f, s));
                cam.transform.position = punto;
                cam.transform.LookAt(conjunto.position + Vector3.up * 16f);
                yield return null;
            }
            conjunto.localPosition = Vector3.zero;
            fundido = 0f;

            // 2 · Acceso y embarque por el brazo de la torre hasta la escotilla (lado −z de la cápsula)
            titulo = "ACCESO Y EMBARQUE · lanzadera L-4";
            float hc = modelo.Capsula.position.y + 1.6f;
            brazo.position = new Vector3(Lugar.x, hc - 1.7f, Lugar.z - 5.4f);
            Vector3 a0 = new Vector3(Lugar.x, hc, Lugar.z - 8.2f), a1 = new Vector3(Lugar.x, hc, Lugar.z - 3.2f);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 2.2f)
            {
                cam.transform.SetPositionAndRotation(Vector3.Lerp(a0, a1, Mathf.SmoothStep(0f, 1f, s)), Quaternion.LookRotation(Vector3.forward));
                yield return null;
            }

            // 3 · Asiento y cierre de compuerta
            titulo = "EN EL ASIENTO · CIERRE DE COMPUERTA";
            Dentro(true);                                                          // desde dentro: cabina y, por la ventana, el cielo
            cam.transform.SetParent(cabina, false);
            cam.transform.localPosition = new Vector3(0.3f, -0.4f, -0.3f);
            cam.transform.localRotation = Quaternion.LookRotation(Vector3.left + Vector3.up * 0.1f, Vector3.up);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 1.8f)
            {
                compuerta.localPosition = new Vector3(-1.85f, 0f, Mathf.Lerp(-1.2f, 0f, Mathf.SmoothStep(0f, 1f, s)));
                yield return null;
            }
            audioFuente.PlayOneShot(pitido);
            grande = "COMPUERTA CERRADA";
            yield return Esperar(0.8f);
            grande = "";

            // 4 · Comprobación de la preparación (la de verdad: ya se verificó antes de empezar)
            titulo = "COMPROBACIÓN DE LA PREPARACIÓN";
            Quaternion qa = cam.transform.localRotation, qb = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 1f)
            {
                cam.transform.localRotation = Quaternion.Slerp(qa, qb, Mathf.SmoothStep(0f, 1f, s));
                yield return null;
            }
            for (lineasVisibles = 0; lineasVisibles < lineas.Count; lineasVisibles++) yield return Esperar(0.45f);
            yield return Esperar(0.6f);

            // 5 · Cuenta atrás
            titulo = "CUENTA ATRÁS";
            for (int n = 5; n >= 1; n--)
            {
                grande = n.ToString();
                audioFuente.PlayOneShot(pitido);
                yield return Esperar(1f);
            }

            // 6 · Encendido
            titulo = "ENCENDIDO";
            grande = "IGNICIÓN";
            Transform llama = modelo.Llama;
            llama.gameObject.SetActive(true);
            audioFuente.PlayOneShot(motor);
            Vector3 base0 = cam.transform.localPosition;
            for (float s = 0f; s < 1.8f; s += Time.deltaTime)
            {
                cam.transform.localPosition = base0 + UnityEngine.Random.insideUnitSphere * 0.03f;
                llama.localScale = new Vector3(3f, 5f + Mathf.Sin(Time.time * 40f) * 0.7f, 3f);
                yield return null;
            }
            grande = "";

            // 7 · Ascenso: vista desde fuera y vuelta al asiento (la plataforma móvil se queda en tierra)
            titulo = "ASCENSO";
            Dentro(false);
            cam.transform.SetParent(null, true);
            modelo.Raiz.SetParent(raiz, true);
            Vector3 inicio = modelo.Raiz.localPosition;
            Vector3 vista0 = Lugar + new Vector3(60f, 4f, -90f);
            float subida = 0f;
            for (float s = 0f; s < 4.5f; s += Time.deltaTime)
            {
                subida = 0.5f * 9f * s * s;
                modelo.Raiz.localPosition = inicio + Vector3.up * subida;
                llama.localScale = new Vector3(3f, 5f + Mathf.Sin(Time.time * 40f) * 0.7f, 3f);
                cam.transform.position = vista0;
                cam.transform.LookAt(modelo.Raiz.position + Vector3.up * 14f);
                cam.backgroundColor = Color.Lerp(cieloTierra, cieloEspacio, s / 4.5f * 0.4f);
                yield return null;
            }
            Dentro(true);
            cam.transform.SetParent(cabina, false);
            cam.transform.localPosition = base0;
            cam.transform.localRotation = qb;
            titulo = "ASCENSO · rumbo a la órbita de la base y la EM-4X";
            float v = 9f * 4.5f;
            for (float s = 0f; s < 3f; s += Time.deltaTime)
            {
                subida += v * Time.deltaTime;
                modelo.Raiz.localPosition = inicio + Vector3.up * subida;
                cam.backgroundColor = Color.Lerp(cieloTierra, cieloEspacio, 0.4f + s / 3f * 0.6f);
                cam.transform.localPosition = base0 + UnityEngine.Random.insideUnitSphere * 0.015f;
                yield return null;
            }

            // 8 · Fundido y transición a la llegada (mensajes existentes de ExpedicionMVP)
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / 1f) yield return null;
            fundido = 1f;
            Terminar();
        }

        ConfiguracionLanzadera configActual;
        ConfiguracionLanzadera ConfigActual() => configActual ?? new ConfiguracionLanzadera();

        IEnumerator Esperar(float s)
        {
            for (float x = 0f; x < s; x += Time.deltaTime) yield return null;
        }

        void Terminar()
        {
            if (terminada) return;
            terminada = true;
            vista = true;
            StopAllCoroutines();
            fundido = 1f;
            alTerminar?.Invoke();
        }

        /// <summary>Cierra la secuencia sin cargar nada (por ejemplo, si la escena de la expedición no se pudo abrir).</summary>
        public void Detener()
        {
            StopAllCoroutines();
            Activa = false;
            terminada = true;
            fundido = 0f;
            if (cam != null) { cam.enabled = false; cam.tag = "Untagged"; }
        }

        void Update()
        {
            if (!Activa || terminada) return;
            Keyboard k = Keyboard.current;
            if (vista && k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) Terminar();
        }

        void OnGUI()
        {
            if (!Activa) return;
            if (sTexto == null)
            {
                sTexto = new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true, alignment = TextAnchor.UpperLeft };
                sTexto.normal.textColor = new Color(0.9f, 0.97f, 1f);
                sTitulo = new GUIStyle(sTexto) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                sTitulo.normal.textColor = new Color(0.55f, 0.9f, 1f);
                sGrande = new GUIStyle(sTexto) { fontSize = 90, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                sGrande.normal.textColor = new Color(1f, 0.85f, 0.4f);
                sPista = new GUIStyle(sTexto) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            }
            float esc = Screen.height / 1080f, ancho = Screen.width / esc;
            GUI.matrix = Matrix4x4.Scale(new Vector3(esc, esc, 1f));
            Color c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, ancho, 70f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, 1010f, ancho, 70f), Texture2D.whiteTexture);
            GUI.color = c;
            GUI.Label(new Rect(0f, 8f, ancho, 54f), titulo, sTitulo);
            if (grande.Length > 0) GUI.Label(new Rect(0f, 380f, ancho, 160f), grande, sGrande);
            if (lineasVisibles > 0)
            {
                Rect r = new Rect(60f, 120f, 900f, 40f + 34f * lineasVisibles);
                GUI.color = new Color(0f, 0.04f, 0.08f, 0.7f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = c;
                for (int i = 0; i < lineasVisibles && i < lineas.Count; i++)
                    GUI.Label(new Rect(r.x + 16f, r.y + 14f + i * 34f, r.width - 32f, 34f), "<color=#59CC73>[OK]</color> " + lineas[i], sTexto);
            }
            GUI.Label(new Rect(0f, 1022f, ancho, 46f), vista
                ? "Representación del despegue (lanzadera terrestre provisional, no pilotable) · ESPACIO o INTRO: omitir"
                : "Representación del despegue (lanzadera terrestre provisional, no pilotable) · se podrá omitir las próximas veces", sPista);
            if (fundido > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(fundido));
                GUI.DrawTexture(new Rect(0f, 0f, ancho, 1080f), Texture2D.whiteTexture);
                GUI.color = c;
            }
        }
    }
}
