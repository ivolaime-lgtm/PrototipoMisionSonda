using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Personaje que CAMINA sobre el suelo del hangar terrestre (con gravedad; distinto del personaje flotante de la EM-4X).
    /// WASD caminar · ratón mirar · Mayús correr. El movimiento y la mirada se bloquean mientras hay una terminal abierta o
    /// el cursor está libre. CharacterController: el cuerpo queda siempre vertical, que aquí es lo correcto.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CaminanteHangar : MonoBehaviour
    {
        [SerializeField] float velocidad = 3.2f;
        [SerializeField] float velocidadCarrera = 6f;
        [SerializeField] float sensibilidad = 0.12f;
        [SerializeField] float alturaOjos = 1.65f;

        public Camera Camara { get; private set; }
        /// <summary>Lo decide el hangar: falso con una terminal abierta o con el cursor libre.</summary>
        public bool Control { get; set; } = true;

        CharacterController cc;
        float pitch, vy;
        int ignorarRaton;
        bool controlAntes;

        public void Iniciar(Vector3 pos, float yaw)
        {
            name = "Personaje (camina por el hangar)";
            cc = GetComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            cc.enabled = true;

            GameObject cam = new GameObject("Cámara del personaje");
            cam.transform.SetParent(transform, false);
            cam.transform.localPosition = new Vector3(0f, alturaOjos, 0f);
            cam.tag = "MainCamera";
            Camara = cam.AddComponent<Camera>();
            Camara.nearClipPlane = 0.05f;
            Camara.farClipPlane = 300f;
            Camara.fieldOfView = 70f;
            Camara.clearFlags = CameraClearFlags.SolidColor;
            Camara.backgroundColor = new Color(0.55f, 0.62f, 0.7f);
            cam.AddComponent<AudioListener>();
        }

        void Update()
        {
            if (cc == null) return;
            Keyboard k = Keyboard.current;
            Mouse raton = Mouse.current;
            Vector3 mov = Vector3.zero;
            if (Control && !controlAntes) ignorarRaton = 2;   // tras bloquear el cursor el primer movimiento puede venir de golpe
            controlAntes = Control;
            if (Control && k != null)
            {
                if (ignorarRaton > 0) ignorarRaton--;
                else if (raton != null)
                {
                    Vector2 d = raton.delta.ReadValue() * sensibilidad;
                    transform.Rotate(0f, d.x, 0f, Space.World);
                    pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
                    Camara.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
                Vector3 entrada = Vector3.zero;
                if (k.wKey.isPressed) entrada.z += 1f;
                if (k.sKey.isPressed) entrada.z -= 1f;
                if (k.dKey.isPressed) entrada.x += 1f;
                if (k.aKey.isPressed) entrada.x -= 1f;
                if (entrada.sqrMagnitude > 1f) entrada.Normalize();
                float v = k.leftShiftKey.isPressed ? velocidadCarrera : velocidad;
                mov = transform.TransformDirection(entrada) * v;
            }
            // Gravedad terrestre: pegado al suelo.
            if (cc.isGrounded && vy < 0f) vy = -1f;
            vy += Physics.gravity.y * Time.deltaTime;
            mov.y = vy;
            cc.Move(mov * Time.deltaTime);
        }

        public Vector3 Ojos => Camara != null ? Camara.transform.position : transform.position;
        public Vector3 Mirada => Camara != null ? Camara.transform.forward : transform.forward;
    }
}
