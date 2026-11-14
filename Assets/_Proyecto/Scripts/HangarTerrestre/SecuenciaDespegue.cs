using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// SECUENCIA BREVE DE DESPEGUE (solo representación; no es pilotable ni una simulación atmosférica).
    /// Lanzadera L-4 PROVISIONAL en una plataforma terrestre: acceso y embarque → asiento y cierre de compuerta →
    /// comprobación de la preparación → cuenta atrás → encendido → ascenso → transición a la llegada a la EM-4X.
    /// La lanzadera es terrestre y distinta de la EM-4X, que espera en órbita.
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

        static readonly Vector3 Lugar = new Vector3(600f, 0f, 0f);     // lejos del hangar: plataforma de lanzamiento

        Camera cam;
        Transform lanzadera, cabina, compuerta, llama;
        Renderer cuerpo, morro;
        Action alTerminar;
        List<string> lineas = new List<string>();
        string titulo = "", grande = "";
        int lineasVisibles;
        float fundido;
        bool terminada;
        Vector3 ojosInicio;
        AudioSource audioFuente;
        AudioClip pitido, motor;
        GUIStyle sTitulo, sTexto, sGrande, sPista;
        Color cieloTierra = new Color(0.55f, 0.72f, 0.9f), cieloEspacio = new Color(0.01f, 0.01f, 0.03f);

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

        void Construir()
        {
            Transform raiz = new GameObject("Plataforma de lanzamiento (provisional)").transform;
            raiz.position = Lugar;
            Material suelo = Mat(new Color(0.45f, 0.5f, 0.38f), false), hormigon = Mat(new Color(0.62f, 0.62f, 0.6f), false),
                     casco = Mat(new Color(0.88f, 0.89f, 0.92f), false), oscuro = Mat(new Color(0.15f, 0.17f, 0.2f), false),
                     naranja = Mat(new Color(1f, 0.55f, 0.15f), false), fuego = Mat(new Color(1f, 0.7f, 0.25f), true),
                     interior = Mat(new Color(0.3f, 0.33f, 0.38f), false), pantalla = Mat(new Color(0.3f, 0.85f, 1f), true);
            Caja("Terreno", raiz, new Vector3(0f, -0.5f, 0f), new Vector3(600f, 1f, 600f), suelo);
            Caja("Plataforma", raiz, new Vector3(0f, 0.5f, 0f), new Vector3(16f, 1f, 16f), hormigon);
            Caja("Torre de servicio", raiz, new Vector3(-9f, 12f, 0f), new Vector3(2f, 24f, 2f), naranja);
            Caja("Pasarela de la torre", raiz, new Vector3(-6.5f, 18f, 0f), new Vector3(3f, 0.3f, 1.5f), naranja);

            // Lanzadera L-4 de pie: 9,2 × 12 × 4,9 m (las medidas del bloque provisional de la lanzadera, en vertical)
            lanzadera = new GameObject("Lanzadera L-4 (provisional)").transform;
            lanzadera.SetParent(raiz, false);
            lanzadera.localPosition = new Vector3(0f, 1f, 0f);
            cuerpo = Caja("Cuerpo", lanzadera, new Vector3(0f, 6f, 0f), new Vector3(9.2f, 12f, 4.9f), casco).GetComponent<Renderer>();
            morro = Caja("Morro", lanzadera, new Vector3(0f, 12.8f, 0f), new Vector3(6f, 1.6f, 3.6f), casco).GetComponent<Renderer>();
            Caja("Ventana", lanzadera, new Vector3(0f, 10.6f, 2.46f), new Vector3(4f, 1.2f, 0.05f), oscuro);
            Caja("Tobera", lanzadera, new Vector3(0f, -0.4f, 0f), new Vector3(3f, 0.8f, 3f), oscuro);
            llama = Caja("Llama", lanzadera, new Vector3(0f, -3f, 0f), new Vector3(2.6f, 4.5f, 2.6f), fuego);
            llama.gameObject.SetActive(false);

            // Cabina interior (se ve desde el asiento): paneles finos con ventana en el techo (la tripulación mira al cielo)
            cabina = new GameObject("Cabina (interior)").transform;
            cabina.SetParent(lanzadera, false);
            cabina.localPosition = new Vector3(0f, 9.5f, 0f);
            Caja("Suelo de la cabina", cabina, new Vector3(0f, -1.5f, 0f), new Vector3(4f, 0.1f, 3f), interior);
            Caja("Pared trasera", cabina, new Vector3(0f, 0f, -1.5f), new Vector3(4f, 3f, 0.1f), interior);
            Caja("Pared delantera", cabina, new Vector3(0f, 0f, 1.5f), new Vector3(4f, 3f, 0.1f), interior);
            Caja("Pared derecha", cabina, new Vector3(2f, 0f, 0f), new Vector3(0.1f, 3f, 3f), interior);
            Caja("Pared izquierda (marco de la compuerta)", cabina, new Vector3(-2f, 0f, -1.05f), new Vector3(0.1f, 3f, 0.9f), interior);
            Caja("Pared izquierda (marco de la compuerta)", cabina, new Vector3(-2f, 0f, 1.05f), new Vector3(0.1f, 3f, 0.9f), interior);
            Caja("Techo (marco)", cabina, new Vector3(0f, 1.5f, -1.1f), new Vector3(4f, 0.1f, 0.8f), interior);
            Caja("Techo (marco)", cabina, new Vector3(0f, 1.5f, 1.1f), new Vector3(4f, 0.1f, 0.8f), interior);
            Caja("Consola", cabina, new Vector3(0f, 0.9f, 1.2f), new Vector3(2.4f, 0.5f, 0.4f), pantalla);
            Caja("Asiento", cabina, new Vector3(0f, -1f, -0.3f), new Vector3(0.7f, 0.2f, 1.6f), naranja);
            compuerta = Caja("Compuerta", cabina, new Vector3(-2.05f, 0f, -1.3f), new Vector3(0.08f, 2.2f, 1.2f), naranja);

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

        /// <summary>Empieza la secuencia. «comprobacion» son las líneas de la comprobación (requisitos ya cumplidos).</summary>
        public void Iniciar(Vector3 ojos, List<string> comprobacion, Action fin)
        {
            if (cam == null) Construir();
            ojosInicio = ojos;
            lineas = comprobacion;
            alTerminar = fin;
            fundido = 0f; lineasVisibles = 0; terminada = false;
            lanzadera.localPosition = new Vector3(0f, 1f, 0f);
            compuerta.localPosition = new Vector3(-2.05f, 0f, -1.3f);
            llama.gameObject.SetActive(false);
            cam.transform.SetParent(null, true);
            cam.backgroundColor = cieloTierra;
            cuerpo.enabled = true; morro.enabled = true;
            cam.enabled = true;
            cam.tag = "MainCamera";
            Activa = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StartCoroutine(Ejecutar());
        }

        IEnumerator Ejecutar()
        {
            // 1 · Acceso y embarque: por la pasarela del hangar hasta la escotilla
            titulo = "ACCESO Y EMBARQUE · lanzadera L-4";
            Vector3 fin = new Vector3(0f, 1.6f, 69f);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 2.5f)
            {
                cam.transform.SetPositionAndRotation(Vector3.Lerp(ojosInicio, fin, Mathf.SmoothStep(0f, 1f, s)), Quaternion.LookRotation(Vector3.forward));
                yield return null;
            }

            // 2 · Asiento y cierre de compuerta
            titulo = "EN EL ASIENTO · CIERRE DE COMPUERTA";
            cuerpo.enabled = false; morro.enabled = false;                       // desde dentro: se ve la cabina y, por la ventana, el cielo
            cam.transform.SetParent(cabina, false);
            cam.transform.localPosition = new Vector3(0.3f, -0.5f, -0.3f);
            cam.transform.localRotation = Quaternion.LookRotation(Vector3.left + Vector3.up * 0.1f, Vector3.up);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 1.8f)
            {
                compuerta.localPosition = new Vector3(-2.05f, 0f, Mathf.Lerp(-1.3f, 0f, Mathf.SmoothStep(0f, 1f, s)));
                yield return null;
            }
            audioFuente.PlayOneShot(pitido);
            grande = "COMPUERTA CERRADA";
            yield return Esperar(0.8f);
            grande = "";

            // 3 · Comprobación de la preparación (la de verdad: ya se verificó antes de empezar)
            titulo = "COMPROBACIÓN DE LA PREPARACIÓN";
            Quaternion a = cam.transform.localRotation, b = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 1f)
            {
                cam.transform.localRotation = Quaternion.Slerp(a, b, Mathf.SmoothStep(0f, 1f, s));
                yield return null;
            }
            for (lineasVisibles = 0; lineasVisibles < lineas.Count; lineasVisibles++) yield return Esperar(0.45f);
            yield return Esperar(0.6f);

            // 4 · Cuenta atrás
            titulo = "CUENTA ATRÁS";
            for (int n = 5; n >= 1; n--)
            {
                grande = n.ToString();
                audioFuente.PlayOneShot(pitido);
                yield return Esperar(1f);
            }

            // 5 · Encendido
            titulo = "ENCENDIDO";
            grande = "IGNICIÓN";
            llama.gameObject.SetActive(true);
            audioFuente.PlayOneShot(motor);
            Vector3 base0 = cam.transform.localPosition;
            for (float s = 0f; s < 1.8f; s += Time.deltaTime)
            {
                cam.transform.localPosition = base0 + UnityEngine.Random.insideUnitSphere * 0.03f;
                llama.localScale = new Vector3(2.6f, 4.5f + Mathf.Sin(Time.time * 40f) * 0.6f, 2.6f);
                yield return null;
            }
            grande = "";

            // 6 · Ascenso: vista desde fuera y vuelta al asiento
            titulo = "ASCENSO";
            cuerpo.enabled = true; morro.enabled = true;
            cam.transform.SetParent(null, true);
            Vector3 punto = Lugar + new Vector3(45f, 3f, -70f);
            float subida = 0f;
            for (float s = 0f; s < 4.5f; s += Time.deltaTime)
            {
                subida = 0.5f * 9f * s * s;
                lanzadera.localPosition = new Vector3(0f, 1f + subida, 0f);
                llama.localScale = new Vector3(2.6f, 4.5f + Mathf.Sin(Time.time * 40f) * 0.6f, 2.6f);
                cam.transform.position = punto;
                cam.transform.LookAt(lanzadera.position + Vector3.up * 6f);
                cam.backgroundColor = Color.Lerp(cieloTierra, cieloEspacio, s / 4.5f * 0.4f);
                yield return null;
            }
            cuerpo.enabled = false; morro.enabled = false;
            cam.transform.SetParent(cabina, false);
            cam.transform.localPosition = base0;
            cam.transform.localRotation = b;
            titulo = "ASCENSO · rumbo a la órbita de la base y la EM-4X";
            float v = 9f * 4.5f;
            for (float s = 0f; s < 3f; s += Time.deltaTime)
            {
                subida += v * Time.deltaTime;
                lanzadera.localPosition = new Vector3(0f, 1f + subida, 0f);
                cam.backgroundColor = Color.Lerp(cieloTierra, cieloEspacio, 0.4f + s / 3f * 0.6f);
                cam.transform.localPosition = base0 + UnityEngine.Random.insideUnitSphere * 0.015f;
                yield return null;
            }

            // 7 · Fundido y transición a la llegada (mensajes existentes de ExpedicionMVP)
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / 1f) yield return null;
            fundido = 1f;
            Terminar();
        }

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
