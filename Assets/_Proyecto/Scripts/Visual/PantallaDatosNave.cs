using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Pantalla de la consola (solo visual): muestra en un TextMesh datos que ya existen en
    /// ControlNave. Solo LEE el estado; no cambia nada de la misión. Se actualiza 4 veces por segundo.
    /// </summary>
    public class PantallaDatosNave : MonoBehaviour
    {
        public enum Contenido { Estado, Propulsion, Casco }

        [SerializeField] Contenido contenido = Contenido.Estado;
        [SerializeField] TextMesh texto;

        ControlNave control;
        float siguiente;

        void Start()
        {
            control = FindFirstObjectByType<ControlNave>();
        }

        void Update()
        {
            if (texto == null || Time.time < siguiente) return;
            siguiente = Time.time + 0.25f;
            if (control == null || control.Nave == null) { texto.text = "SIN DATOS"; return; }

            switch (contenido)
            {
                case Contenido.Estado:
                    texto.text = $"{control.NombreFase().ToUpperInvariant()}\nVIAJE {control.Progreso * 100f:0} %";
                    break;
                case Contenido.Propulsion:
                    texto.text = $"COMBUSTIBLE\n{control.Combustible:0.0} kg" +
                                 (control.ConsumoActualKgS > 0.05f ? $"  -{control.ConsumoActualKgS:0.0}/s" : "") +
                                 $"\nACEL {control.AceleracionMS2:0.00} m/s²";
                    break;
                case Contenido.Casco:
                    texto.text = $"CASCO {control.Integridad:0} %\nENERGÍA\n{control.EnergiaNave:0} Wh";
                    break;
            }
        }
    }
}
