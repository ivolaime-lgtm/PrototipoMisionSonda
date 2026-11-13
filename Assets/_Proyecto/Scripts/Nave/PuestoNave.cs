using UnityEngine;

namespace PrototipoMision
{
    public enum TipoPuesto { Pilotaje = 0, Ingenieria = 1, Comunicaciones = 2, Ciencia = 3 }

    public enum EstadoPuesto { Inactivo, Pendiente, Correcto, Alarma }

    /// <summary>
    /// Un puesto de la cabina. El jugador lo mira, pulsa E y la cámara pasa a la vista del puesto.
    /// La lógica de cada puesto está en ControlNave; aquí solo hay identidad, vista e indicador.
    /// Cada puesto corresponde a un rol del modo equipo para poder repartirlos más adelante.
    /// </summary>
    public class PuestoNave : MonoBehaviour
    {
        [SerializeField] TipoPuesto tipo;
        [SerializeField] string nombreVisible = "Puesto";
        [Tooltip("Punto y orientación de la cámara al usar el puesto.")]
        [SerializeField] Transform vista;
        [Tooltip("Luz de estado: ámbar = tarea pendiente, roja = alarma, verde = correcto.")]
        [SerializeField] Renderer indicador;

        public TipoPuesto Tipo => tipo;
        public string Nombre => nombreVisible;
        public Transform Vista => vista != null ? vista : transform;
        public EstadoPuesto Estado { get; set; }

        /// <summary>Rol del modo equipo responsable de este puesto (para repartir funciones más adelante).</summary>
        public Rol RolEquipo
        {
            get
            {
                switch (tipo)
                {
                    case TipoPuesto.Pilotaje: return Rol.Lanzamiento;
                    case TipoPuesto.Ingenieria: return Rol.Ingenieria;
                    case TipoPuesto.Comunicaciones: return Rol.Comunicaciones;
                    default: return Rol.Ciencia;
                }
            }
        }

        Material material;

        void Awake()
        {
            if (indicador != null) material = indicador.material;   // copia propia para cambiar el color
        }

        void Update()
        {
            if (material == null) return;
            bool parpadeo = Mathf.Repeat(Time.time * 2f, 1f) < 0.5f;
            Color c;
            switch (Estado)
            {
                case EstadoPuesto.Pendiente: c = parpadeo ? new Color(1f, 0.7f, 0.1f) : new Color(0.35f, 0.22f, 0.02f); break;
                case EstadoPuesto.Alarma: c = parpadeo ? new Color(1f, 0.1f, 0.08f) : new Color(0.3f, 0.02f, 0.02f); break;
                case EstadoPuesto.Correcto: c = new Color(0.2f, 1f, 0.35f); break;
                default: c = new Color(0.25f, 0.28f, 0.3f); break;
            }
            material.SetColor("_BaseColor", c);
        }
    }
}
