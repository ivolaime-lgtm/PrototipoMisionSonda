using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Movimiento submarino en primera persona.
    /// La sonda avanza hacia donde mira la cámara, con inercia y freno del agua
    /// (el freno lo da el "Linear Damping" del Rigidbody).
    /// Controles: WASD mover · Espacio subir · Ctrl o C bajar · Shift turbo ·
    /// ratón mirar · Esc liberar el cursor · clic para recuperarlo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SistemaEnergia))]
    public class ControladorSonda : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Cámara en primera persona. Si se deja vacío, se busca entre los hijos.")]
        [SerializeField] Transform camara;

        [Header("Movimiento (valores de juego, no físicos)")]
        [Tooltip("Aceleración de los propulsores, en m/s².")]
        [SerializeField] float aceleracion = 10f;

        [Tooltip("Velocidad máxima normal, en m/s.")]
        [SerializeField] float velocidadMaxima = 5f;

        [Tooltip("Multiplicador de aceleración y velocidad con el turbo (Shift).")]
        [SerializeField] float multiplicadorTurbo = 1.8f;

        [Header("Masa — DEMOSTRACIÓN")]
        [Tooltip("Masa (kg) para la que se ajustaron la aceleración y la velocidad. " +
                 "Con más masa la sonda acelera menos; con menos, más. Modelo simplificado. DEMOSTRACIÓN.")]
        [SerializeField] float masaReferenciaKg = 170f;

        [Header("Cámara")]
        [Tooltip("Grados que gira la cámara por cada píxel que se mueve el ratón.")]
        [SerializeField] float sensibilidadRaton = 0.12f;

        [Tooltip("Ángulo máximo para mirar arriba o abajo, en grados.")]
        [SerializeField] float limiteInclinacion = 85f;

        Rigidbody rb;
        SistemaEnergia energia;
        float giroHorizontal;
        float giroVertical;
        Vector3 direccionDeseada;
        bool turbo;

        public float VelocidadActual => rb != null ? rb.linearVelocity.magnitude : 0f;

        /// <summary>Multiplicador de aceleración por la masa (1 = masa de referencia).</summary>
        public float FactorMasa { get; private set; } = 1f;
        public float MasaKg { get; private set; }

        /// <summary>
        /// Lo llama AplicadorConfiguracion con la masa de la sonda elegida.
        /// Modelo simplificado: factor = (masa de referencia / masa)², limitado entre 0,4 y 1,5.
        /// </summary>
        public void EstablecerMasa(float kg)
        {
            MasaKg = Mathf.Max(1f, kg);
            float relacion = masaReferenciaKg / MasaKg;
            FactorMasa = Mathf.Clamp(relacion * relacion, 0.4f, 1.5f);
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            energia = GetComponent<SistemaEnergia>();

            if (camara == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) camara = cam.transform;
            }

            // El cuerpo no gira: solo gira la cámara. Así el Rigidbody es estable.
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = false;

            if (camara != null)
            {
                Vector3 angulos = camara.localEulerAngles;
                giroHorizontal = angulos.y;
                giroVertical = 0f;
            }
        }

        void Start()
        {
            BloquearCursor(true);
        }

        void Update()
        {
            Keyboard teclado = Keyboard.current;
            Mouse raton = Mouse.current;

            if (teclado == null)
            {
                direccionDeseada = Vector3.zero;
                return;
            }

            if (teclado.escapeKey.wasPressedThisFrame)
                BloquearCursor(false);

            if (raton != null && raton.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                BloquearCursor(true);

            // Mirar con el ratón solo si el cursor está capturado.
            if (raton != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = raton.delta.ReadValue();
                giroHorizontal += delta.x * sensibilidadRaton;
                giroVertical -= delta.y * sensibilidadRaton;
                giroVertical = Mathf.Clamp(giroVertical, -limiteInclinacion, limiteInclinacion);
            }

            if (camara != null)
                camara.localRotation = Quaternion.Euler(giroVertical, giroHorizontal, 0f);

            Vector3 entrada = Vector3.zero;
            if (teclado.wKey.isPressed) entrada.z += 1f;
            if (teclado.sKey.isPressed) entrada.z -= 1f;
            if (teclado.dKey.isPressed) entrada.x += 1f;
            if (teclado.aKey.isPressed) entrada.x -= 1f;
            if (teclado.spaceKey.isPressed) entrada.y += 1f;
            if (teclado.leftCtrlKey.isPressed || teclado.cKey.isPressed) entrada.y -= 1f;

            Transform referencia = camara != null ? camara : transform;
            Vector3 direccion = referencia.forward * entrada.z
                              + referencia.right * entrada.x
                              + Vector3.up * entrada.y;
            direccionDeseada = Vector3.ClampMagnitude(direccion, 1f);

            // Sin energía, los propulsores no funcionan.
            if (energia.Agotada)
                direccionDeseada = Vector3.zero;

            turbo = teclado.leftShiftKey.isPressed && direccionDeseada.sqrMagnitude > 0.01f;

            energia.EstablecerPropulsion(direccionDeseada.magnitude, turbo);
        }

        void FixedUpdate()
        {
            float mult = turbo ? multiplicadorTurbo : 1f;
            rb.AddForce(direccionDeseada * aceleracion * mult * FactorMasa, ForceMode.Acceleration);

            float maxima = velocidadMaxima * mult;
            Vector3 v = rb.linearVelocity;
            if (v.sqrMagnitude > maxima * maxima)
                rb.linearVelocity = v.normalized * maxima;
        }

        static void BloquearCursor(bool bloquear)
        {
            Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloquear;
        }
    }
}
