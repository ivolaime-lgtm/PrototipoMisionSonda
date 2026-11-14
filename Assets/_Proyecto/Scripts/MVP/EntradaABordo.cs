using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// ENTRADA A BORDO ANIMADA (solo representación; ExpedicionMVP). Continúa el paso que termina la secuencia de la L-4:
    /// la cámara empieza justo encima del puerto de atraque del Mando (la misma posición en la que acaba el plano de
    /// la secuencia, PoseInicial), sube al módulo, se cierra el acceso de la lanzadera, avanza por el pasadizo hasta el
    /// nodo central y se gira hasta el PUNTO DE INICIO de siempre. Entonces se entrega el control al personaje.
    ///
    /// No es un personaje nuevo: es una cámara en primera persona. Mientras dura, la misión no ha empezado (la simulación
    /// está parada) y el personaje ya está colocado en el punto de inicio sin control. Verla entera u omitirla
    /// (Espacio/Intro, después de haberla visto una vez o si la configuración lo permite) deja exactamente el mismo
    /// estado: puertas como al inicio, personaje en el punto de inicio, misión creada desde la preparación confirmada.
    /// </summary>
    public class EntradaABordo : MonoBehaviour
    {
        static bool vista;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { vista = false; }

        public bool Activa { get; private set; }

        JuegoMVP juego;
        Action alTerminar;
        Camera cam;
        PuertasUnion acceso;
        Vector3 pFin;
        Quaternion qFin;
        float fundido = 1f, t;
        bool cerrada, terminada;
        GUIStyle sTitulo, sPista;

        /// <summary>
        /// Posición y giro (coordenadas de la nave) donde termina el paso de la secuencia de la L-4 y empieza esta entrada:
        /// 2 m por encima del puerto de atraque del Mando, mirando hacia arriba con la popa en lo alto de la imagen.
        /// </summary>
        public static void PoseInicial(DefinicionNaveModular d, out Vector3 pos, out Quaternion rot)
        {
            ModuloNave ma = d != null ? d.Modulo("MA") : null;
            PuertoModulo p = ma != null ? ma.Puerto("atraque") : null;
            Vector3 puerto = p != null ? ma.AMundo(p.posicion) : new Vector3(0f, -3f, 11.7f);
            Vector3 arriba = ma != null ? ma.Arriba : Vector3.up, atras = ma != null ? -ma.Adelante : Vector3.back;
            pos = puerto + arriba * 2f;
            rot = Quaternion.LookRotation(arriba, atras);
        }

        /// <summary>Empieza la entrada. «posFin»/«rotFin» = punto de inicio a bordo (el del personaje).</summary>
        public void Iniciar(JuegoMVP j, Vector3 posFin, Quaternion rotFin, Action fin)
        {
            juego = j;
            alTerminar = fin;
            pFin = posFin;
            qFin = rotFin;
            Activa = true;
            terminada = false;
            t = 0f;
            if (juego.VistaVuelo != null) juego.VistaVuelo.Activar(false);
            juego.Personaje.ActivarControl(false);
            foreach (Renderer r in juego.Personaje.GetComponentsInChildren<Renderer>()) r.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            foreach (PuertasUnion u in juego.Uniones)
                if (u != null && u.Datos.id == "lanzadera") { acceso = u; u.FijarParaComprobacion(true); }

            GameObject g = new GameObject("Cámara de la entrada a bordo");
            cam = g.AddComponent<Camera>();
            Camera c = juego.Personaje.Camara;
            if (c != null)
            {
                cam.clearFlags = c.clearFlags;
                cam.backgroundColor = c.backgroundColor;
                cam.nearClipPlane = c.nearClipPlane;
                cam.farClipPlane = c.farClipPlane;
                cam.fieldOfView = c.fieldOfView;
            }
            cam.depth = 20f;
            g.AddComponent<AudioListener>();
            Vector3 p0; Quaternion q0;
            PoseInicial(juego.Expedicion.Definicion, out p0, out q0);
            cam.transform.SetPositionAndRotation(p0, q0);
        }

        void Update()
        {
            if (!Activa || terminada) return;
            Keyboard k = Keyboard.current;
            bool omitible = vista || ConfiguracionJuego.Actual.secuencias.omitirDesdeLaPrimeraVez;
            if (omitible && k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) { Terminar(); return; }

            DefinicionNaveModular d = juego.Expedicion.Definicion;
            ModuloNave ma = d.Modulo("MA");
            Vector3 arriba = ma != null ? ma.Arriba : Vector3.up, adelante = ma != null ? ma.Adelante : Vector3.forward;
            Vector3 p0; Quaternion q0;
            PoseInicial(d, out p0, out q0);
            // Puntos del paso: sobre el puerto → centro del Mando a la altura de los ojos → entrada del Mando → punto de inicio
            Vector3 p1 = new Vector3(p0.x, pFin.y, p0.z) - adelante * 0.5f;
            Vector3 p2 = ma != null ? ma.AMundo(new Vector3(0f, pFin.y - ma.origen.y, -0.2f)) : new Vector3(0f, pFin.y, 6f);
            Quaternion haciaPopa = Quaternion.LookRotation(-adelante, arriba);
            Quaternion deLado = Quaternion.LookRotation(Vector3.Cross(arriba, adelante), arriba);

            float dur = ConfiguracionJuego.D(ConfiguracionJuego.Actual.secuencias.entradaABordo);
            t += Time.deltaTime / Mathf.Max(0.5f, dur);
            fundido = Mathf.Clamp01(1f - t / 0.08f);
            Vector3 pos; Quaternion rot;
            if (t < 0.22f)
            {
                float k1 = Mathf.SmoothStep(0f, 1f, t / 0.22f);
                pos = Vector3.Lerp(p0, p1, k1);
                rot = Quaternion.Slerp(q0, haciaPopa, k1);
            }
            else if (t < 0.75f)
            {
                if (!cerrada && acceso != null) { cerrada = true; if (acceso.Abierta) acceso.Alternar(); }   // se cierra el acceso, abajo
                float k2 = Mathf.SmoothStep(0f, 1f, (t - 0.22f) / 0.53f);
                float a = Vector3.Distance(p1, p2), b = Vector3.Distance(p2, pFin), x = k2 * (a + b);
                pos = x < a ? Vector3.Lerp(p1, p2, x / Mathf.Max(0.01f, a)) : Vector3.Lerp(p2, pFin, (x - a) / Mathf.Max(0.01f, b));
                rot = haciaPopa;
            }
            else
            {
                // Media vuelta en el sitio, pasando por un lado (sin volteretas) hasta mirar como al empezar la misión
                float k3 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.75f) / 0.25f));
                pos = pFin;
                rot = k3 < 0.5f ? Quaternion.Slerp(haciaPopa, deLado, k3 * 2f) : Quaternion.Slerp(deLado, qFin, (k3 - 0.5f) * 2f);
            }
            cam.transform.SetPositionAndRotation(pos, rot);
            if (t >= 1f) Terminar();
        }

        void Terminar()
        {
            if (terminada) return;
            terminada = true;
            vista = true;
            Activa = false;
            if (cam != null) Destroy(cam.gameObject);
            Action fin = alTerminar;
            alTerminar = null;
            fin?.Invoke();           // JuegoMVP coloca puertas y personaje como siempre y empieza la misión
            Destroy(this);
        }

        void OnGUI()
        {
            if (!Activa) return;
            if (sTitulo == null)
            {
                sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                sTitulo.normal.textColor = new Color(0.55f, 0.9f, 1f);
                sPista = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
                sPista.normal.textColor = new Color(0.9f, 0.97f, 1f);
            }
            float esc = Screen.height / 1080f, ancho = Screen.width / esc;
            GUI.matrix = Matrix4x4.Scale(new Vector3(esc, esc, 1f));
            Color c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, ancho, 70f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, 1010f, ancho, 70f), Texture2D.whiteTexture);
            GUI.color = c;
            GUI.Label(new Rect(0f, 8f, ancho, 54f), ConfiguracionJuego.Texto("abordo.paso"), sTitulo);
            bool omitible = vista || ConfiguracionJuego.Actual.secuencias.omitirDesdeLaPrimeraVez;
            GUI.Label(new Rect(0f, 1022f, ancho, 46f), "Entrada a bordo (representación) · " +
                      ConfiguracionJuego.Texto(omitible ? "omitir" : "omitir.luego"), sPista);
            if (fundido > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, fundido);
                GUI.DrawTexture(new Rect(0f, 0f, ancho, 1080f), Texture2D.whiteTexture);
                GUI.color = c;
            }
        }
    }
}
