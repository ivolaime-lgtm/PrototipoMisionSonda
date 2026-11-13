using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Movimiento decorativo para la escena de presentación. No afecta a
    /// ninguna mecánica de la misión.
    /// - RotarSobreSiMismo: gira el objeto (p. ej. la luna).
    /// - OrbitarAlrededor: da vueltas alrededor de un centro (p. ej. el satélite).
    /// - OscilarAlrededor: se balancea a un lado y otro de un centro (p. ej. la cámara).
    /// </summary>
    public class MovimientoVisual : MonoBehaviour
    {
        public enum Modo { RotarSobreSiMismo, OrbitarAlrededor, OscilarAlrededor }

        [SerializeField] Modo modo = Modo.RotarSobreSiMismo;
        [SerializeField] Vector3 eje = Vector3.up;

        [Tooltip("Grados por segundo (rotar y orbitar) o velocidad del vaivén (oscilar).")]
        [SerializeField] float velocidad = 5f;

        [Tooltip("Centro para orbitar u oscilar.")]
        [SerializeField] Transform centro;

        [Tooltip("Punto al que mira el objeto (opcional; si está vacío y hay centro, mira al centro).")]
        [SerializeField] Transform mirarA;

        [Tooltip("Solo para oscilar: grados máximos a cada lado.")]
        [SerializeField] float amplitudGrados = 10f;

        Vector3 desplazamientoInicial;

        void Start()
        {
            if (centro != null)
                desplazamientoInicial = transform.position - centro.position;
        }

        void Update()
        {
            switch (modo)
            {
                case Modo.RotarSobreSiMismo:
                    transform.Rotate(eje, velocidad * Time.deltaTime, Space.Self);
                    break;

                case Modo.OrbitarAlrededor:
                    if (centro == null) return;
                    transform.RotateAround(centro.position, eje.normalized, velocidad * Time.deltaTime);
                    break;

                case Modo.OscilarAlrededor:
                    if (centro == null) return;
                    float angulo = Mathf.Sin(Time.time * velocidad * Mathf.Deg2Rad * 10f) * amplitudGrados;
                    transform.position = centro.position + Quaternion.AngleAxis(angulo, eje.normalized) * desplazamientoInicial;
                    break;
            }

            if (mirarA != null)
                transform.LookAt(mirarA);
            else if (modo == Modo.OscilarAlrededor && centro != null)
                transform.LookAt(centro);
        }
    }
}
