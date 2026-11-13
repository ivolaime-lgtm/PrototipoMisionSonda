using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// PERSONAJE PROVISIONAL que flota dentro de la EM-4X, CON COLISIONES. Un solo personaje, sin animaciones, bots ni red.
    ///
    /// Cuerpo: cápsula de 1,75 m y 0,5 m de ancho, tumbada a lo largo de la mirada (en ingravidez se pasa por las
    /// escotillas de cabeza). Los ojos están en la punta delantera. Rigidbody sin gravedad: la física resuelve los choques
    /// con paredes, equipos y puertas cerradas.
    /// No hay «arriba» global: la orientación es libre en tres ejes; R alinea el «arriba» con el del módulo en que estás.
    ///
    /// Controles: ratón = mirar (girar el cuerpo) · W/S/A/D = moverse · Espacio/Ctrl = subir/bajar según la vista ·
    /// Z/X = girar sobre sí mismo · Mayús = rápido · R = nivelar con el módulo · E = usar el panel que miras.
    /// Al soltar las teclas se frena solo (ayuda PROVISIONAL de la prueba, no es la física definitiva).
    ///
    /// Reutiliza de JugadorNave la forma de mirar y usar con E (rayo desde los ojos) y la captura del ratón; no usa su
    /// CharacterController porque su cápsula siempre queda vertical respecto al mundo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PersonajeFlotante : MonoBehaviour
    {
        public const float Largo = 1.75f;
        public const float Radio = 0.25f;

        [SerializeField] float velocidad = 1.5f;
        [SerializeField] float velocidadRapida = 3f;
        [SerializeField] float aceleracion = 3f;
        [SerializeField] float sensibilidadRaton = 0.12f;
        [SerializeField] float giroAlabeo = 70f;
        [SerializeField] float distanciaUso = 2.4f;

        public Camera Camara { get; private set; }
        public bool ControlActivo { get; set; } = true;
        public PanelPuerta PanelMirado { get; private set; }
        public string ModuloActual { get; private set; } = "";
        public float Rapidez => rb != null ? rb.linearVelocity.magnitude : 0f;
        public string Bloqueo { get; private set; } = "";
        public string Contactos { get; private set; } = "";
        public Rigidbody Cuerpo => rb;
        public CapsuleCollider Capsula { get; private set; }

        DefinicionNaveModular d;
        Rigidbody rb;
        Quaternion giro;
        Vector3 entrada;
        float tiempoAtascado;
        readonly HashSet<string> tocando = new HashSet<string>();
        Renderer visual;

        public void Iniciar(DefinicionNaveModular def, Vector3 posicion, Quaternion orientacion)
        {
            d = def;
            name = "Personaje provisional (con colisiones)";
            transform.SetPositionAndRotation(posicion, orientacion);
            giro = orientacion;

            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 80f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.freezeRotation = true;          // la orientación la decide el jugador (MoveRotation)
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Capsula = gameObject.AddComponent<CapsuleCollider>();
            Capsula.direction = 2;                                   // a lo largo de la mirada (z)
            Capsula.height = Largo;
            Capsula.radius = Radio;
            Capsula.center = new Vector3(0f, 0f, -(Largo / 2f - Radio));   // ojos en la punta delantera

            // Visual sencillo (se ve desde la cámara de inspección; se oculta en primera persona)
            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Collider cc = cuerpo.GetComponent<Collider>();
            cc.enabled = false;            // Destroy actúa al final del fotograma: se desactiva ya
            Destroy(cc);
            cuerpo.name = "Cuerpo (visual)";
            cuerpo.transform.SetParent(transform, false);
            cuerpo.transform.localPosition = Capsula.center;
            cuerpo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cuerpo.transform.localScale = new Vector3(Radio * 2f, Largo / 2f, Radio * 2f);
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = new Material(sh != null ? sh : Shader.Find("Standard")) { color = new Color(1f, 0.85f, 0.2f) };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.color);
            visual = cuerpo.GetComponent<Renderer>();
            visual.sharedMaterial = mat;

            GameObject cam = new GameObject("Cámara del personaje");
            cam.transform.SetParent(transform, false);
            cam.tag = "MainCamera";
            Camara = cam.AddComponent<Camera>();
            Camara.nearClipPlane = 0.05f;
            Camara.farClipPlane = 400f;
            Camara.fieldOfView = 70f;
            Camara.clearFlags = CameraClearFlags.SolidColor;
            Camara.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            cam.AddComponent<AudioListener>();
        }

        /// <summary>Activa o desactiva el control en primera persona (la cámara de inspección es otra herramienta).</summary>
        public void ActivarControl(bool activo)
        {
            ControlActivo = activo;
            if (Camara != null)
            {
                Camara.enabled = activo;
                AudioListener al = Camara.GetComponent<AudioListener>();
                if (al != null) al.enabled = activo;
            }
            if (visual != null) visual.enabled = !activo;
            entrada = Vector3.zero;
            Cursor.lockState = activo ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !activo;
        }

        public void Recolocar(Vector3 posicion, Quaternion orientacion)
        {
            giro = orientacion;
            rb.position = posicion;
            rb.rotation = orientacion;
            transform.SetPositionAndRotation(posicion, orientacion);
            rb.linearVelocity = Vector3.zero;
        }

        void Update()
        {
            if (rb == null) return;
            ModuloActual = BuscarModulo(rb.position);
            if (!ControlActivo) return;
            Keyboard k = Keyboard.current;
            Mouse raton = Mouse.current;

            if (raton != null && raton.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Mirar: gira todo el cuerpo alrededor de los ojos, en sus propios ejes (sin arriba global)
            if (raton != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 dl = raton.delta.ReadValue() * sensibilidadRaton;
                giro = giro * Quaternion.Euler(-dl.y, dl.x, 0f);
            }
            entrada = Vector3.zero;
            if (k != null)
            {
                if (k.zKey.isPressed) giro = giro * Quaternion.Euler(0f, 0f, giroAlabeo * Time.deltaTime);
                if (k.xKey.isPressed) giro = giro * Quaternion.Euler(0f, 0f, -giroAlabeo * Time.deltaTime);
                if (k.rKey.wasPressedThisFrame) Nivelar();
                if (k.wKey.isPressed) entrada += Vector3.forward;
                if (k.sKey.isPressed) entrada += Vector3.back;
                if (k.dKey.isPressed) entrada += Vector3.right;
                if (k.aKey.isPressed) entrada += Vector3.left;
                if (k.spaceKey.isPressed) entrada += Vector3.up;
                if (k.leftCtrlKey.isPressed) entrada += Vector3.down;
                if (entrada.sqrMagnitude > 1f) entrada.Normalize();
                if (k.leftShiftKey.isPressed) entrada *= velocidadRapida / velocidad;
            }
            giro = Quaternion.Normalize(giro);

            // Panel que se mira (rayo desde los ojos; el rayo no detecta la propia cápsula porque empieza dentro)
            PanelMirado = null;
            if (Camara != null && Physics.Raycast(Camara.transform.position, Camara.transform.forward, out RaycastHit hit, distanciaUso, ~0, QueryTriggerInteraction.Ignore))
                PanelMirado = hit.collider.GetComponent<PanelPuerta>();
            if (PanelMirado != null && k != null && k.eKey.wasPressedThisFrame && PanelMirado.Union != null)
                PanelMirado.Union.Alternar();
        }

        void FixedUpdate()
        {
            if (rb == null) return;
            rb.MoveRotation(giro);
            float rapidezReal = rb.linearVelocity.magnitude;   // la que dejó la física tras los choques
            Vector3 deseada = ControlActivo ? giro * entrada * velocidad : Vector3.zero;
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, deseada, aceleracion * Time.fixedDeltaTime);

            // Bloqueos y enganches: se empuja pero no se avanza y hay contacto
            bool empuja = ControlActivo && entrada.sqrMagnitude > 0.01f;
            if (empuja && rapidezReal < 0.12f && tocando.Count > 0) tiempoAtascado += Time.fixedDeltaTime;
            else tiempoAtascado = 0f;
            Bloqueo = tiempoAtascado > 0.6f ? "Bloqueado contra: " + string.Join(", ", tocando) : "";
            Contactos = string.Join(", ", tocando);
            tocando.Clear();   // se rellena en OnCollisionStay de este paso
        }

        void OnCollisionStay(Collision c)
        {
            string n = c.collider.name;
            Transform t = c.collider.transform.parent;
            if (t != null && (n == "Casco" || n == "Tapa" || n.StartsWith("Hoja") || n == "Guía" || n == "Adaptador" || n == "Pasadizo")) n = n + " · " + t.name;
            tocando.Add(n);
        }

        void LateUpdate()
        {
            if (Camara != null && ControlActivo) Camara.transform.localRotation = Quaternion.identity;
        }

        /// <summary>Alinea el «arriba» de la vista con el «arriba» local del módulo actual (sin cambiar hacia dónde mira, si se puede).</summary>
        void Nivelar()
        {
            ModuloNave m = ModuloEn(rb.position);
            Vector3 arriba = m != null ? m.Arriba : Vector3.up;
            Vector3 adelante = giro * Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(adelante, arriba)) > 0.98f) adelante = m != null ? m.Adelante : Vector3.forward;
            giro = Quaternion.LookRotation(adelante, arriba);
        }

        public ModuloNave ModuloEn(Vector3 p)
        {
            if (d == null) return null;
            foreach (ModuloNave m in d.modulos)
            {
                if (!m.Presurizado) continue;
                Vector3 rel = p - m.origen;
                Vector3 l = new Vector3(Vector3.Dot(rel, m.Derecha), Vector3.Dot(rel, m.Arriba), Vector3.Dot(rel, m.Adelante));
                if (m.EsCaja)
                {
                    if (l.x >= -m.caja.x / 2f && l.x <= m.caja.x / 2f && l.y >= -m.caja.y / 2f && l.y <= m.caja.y / 2f && l.z >= 0f && l.z <= m.largo) return m;
                    continue;
                }
                float r = m.diametroExterior / 2f;
                if (Mathf.Abs(l.x) <= r && Mathf.Abs(l.y) <= r && (Mathf.Abs(l.x) + Mathf.Abs(l.y)) / Mathf.Sqrt(2f) <= r && l.z >= 0f && l.z <= m.largo) return m;
            }
            return null;
        }

        string BuscarModulo(Vector3 p)
        {
            ModuloNave m = ModuloEn(p);
            return m != null ? m.nombre : "pasadizo o adaptador entre módulos";
        }
    }
}
