using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Jugador en primera persona dentro de la nave.
    /// WASD: caminar · Ratón: mirar · E: usar el puesto que miras · Q: salir del puesto.
    /// Clic izquierdo: volver a capturar el ratón si se ha liberado (por ejemplo con Esc en el editor).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class JugadorNave : MonoBehaviour
    {
        [SerializeField] Transform ojos;
        [SerializeField] Camera camara;
        [SerializeField] float velocidadCaminar = 3f;
        [SerializeField] float sensibilidadRaton = 0.12f;
        [SerializeField] float distanciaInteraccion = 2.6f;

        public PuestoNave PuestoMirado { get; private set; }
        public PuestoNave PuestoActual { get; private set; }

        /// <summary>Si es true, no se puede salir del puesto (por ejemplo, durante el despegue).</summary>
        public bool AsientoBloqueado { get; set; }

        /// <summary>Intensidad de la vibración de la cámara (0 = nada).</summary>
        public float Sacudida { get; set; }

        public Camera Camara => camara;

        CharacterController cc;
        float yaw, pitch, velocidadVertical;
        Vector3 posicionCamara;
        Quaternion rotacionCamara;
        PuestoNave puestoAnterior;
        float transicion;

        void Start()
        {
            cc = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
            if (camara == null) camara = Camera.main;
            if (ojos == null) ojos = transform;
            posicionCamara = ojos.position;
            rotacionCamara = ojos.rotation;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            Keyboard teclado = Keyboard.current;
            Mouse raton = Mouse.current;

            if (raton != null && raton.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (PuestoActual != null)
            {
                PuestoMirado = null;
                if (teclado != null && teclado.qKey.wasPressedThisFrame && !AsientoBloqueado)
                    PuestoActual = null;
                return;
            }

            // Mirar
            if (raton != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = raton.delta.ReadValue();
                yaw += d.x * sensibilidadRaton;
                pitch = Mathf.Clamp(pitch - d.y * sensibilidadRaton, -80f, 80f);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            ojos.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // Caminar
            Vector2 entrada = Vector2.zero;
            if (teclado != null)
            {
                if (teclado.wKey.isPressed) entrada.y += 1f;
                if (teclado.sKey.isPressed) entrada.y -= 1f;
                if (teclado.dKey.isPressed) entrada.x += 1f;
                if (teclado.aKey.isPressed) entrada.x -= 1f;
            }
            Vector3 mover = (transform.forward * entrada.y + transform.right * entrada.x);
            if (mover.sqrMagnitude > 1f) mover.Normalize();
            velocidadVertical = cc.isGrounded ? -1f : velocidadVertical - 9.8f * Time.deltaTime;
            cc.Move((mover * velocidadCaminar + Vector3.up * velocidadVertical) * Time.deltaTime);

            // Qué puesto se está mirando
            PuestoMirado = null;
            if (Physics.Raycast(ojos.position, ojos.forward, out RaycastHit hit, distanciaInteraccion, ~0, QueryTriggerInteraction.Ignore))
                PuestoMirado = hit.collider.GetComponentInParent<PuestoNave>();

            if (PuestoMirado != null && teclado != null && teclado.eKey.wasPressedThisFrame)
                PuestoActual = PuestoMirado;
        }

        void LateUpdate()
        {
            if (camara == null) return;
            Transform destino = PuestoActual != null ? PuestoActual.Vista : ojos;

            // Al entrar o salir de un puesto, la cámara se desliza durante un momento;
            // caminando, sigue a los ojos sin retraso.
            if (PuestoActual != puestoAnterior)
            {
                puestoAnterior = PuestoActual;
                transicion = 0.5f;
            }
            if (transicion > 0f)
            {
                transicion -= Time.deltaTime;
                float t = 1f - Mathf.Exp(-12f * Mathf.Min(Time.deltaTime, 0.1f));
                posicionCamara = Vector3.Lerp(posicionCamara, destino.position, t);
                rotacionCamara = Quaternion.Slerp(rotacionCamara, destino.rotation, t);
            }
            else
            {
                posicionCamara = destino.position;
                rotacionCamara = destino.rotation;
            }

            Vector3 vibracion = Sacudida > 0f ? Random.insideUnitSphere * Sacudida * 0.04f : Vector3.zero;
            camara.transform.SetPositionAndRotation(posicionCamara + vibracion, rotacionCamara);
        }
    }
}
