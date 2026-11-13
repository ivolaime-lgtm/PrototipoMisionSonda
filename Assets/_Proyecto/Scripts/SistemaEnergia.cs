using System;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Batería de la sonda. Calcula el consumo en vatios (W) y descuenta
    /// la energía en vatios-hora (Wh). Todos los valores por defecto son
    /// de DEMOSTRACIÓN: no proceden de ninguna misión real.
    /// </summary>
    public class SistemaEnergia : MonoBehaviour
    {
        [Header("Batería — valores de DEMOSTRACIÓN")]
        [Tooltip("Energía máxima que guarda la batería, en vatios-hora (Wh). " +
                 "1 Wh = la energía que usa un aparato de 1 W durante 1 hora. DEMOSTRACIÓN.")]
        [SerializeField] float capacidadWh = 100f;

        [Header("Consumo — valores de DEMOSTRACIÓN")]
        [Tooltip("Consumo fijo de los sistemas de a bordo (ordenador, sensores, luces), en vatios (W). DEMOSTRACIÓN.")]
        [SerializeField] float consumoBaseW = 20f;

        [Tooltip("Consumo extra de los propulsores a máxima potencia, en vatios (W). DEMOSTRACIÓN.")]
        [SerializeField] float consumoPropulsionW = 60f;

        [Tooltip("Cuánto se multiplica el consumo de los propulsores con el turbo (Shift).")]
        [SerializeField] float multiplicadorConsumoTurbo = 2f;

        [Header("Recarga — valores de DEMOSTRACIÓN")]
        [Tooltip("Potencia que entrega la estación mientras la sonda está dentro, en vatios (W). DEMOSTRACIÓN.")]
        [SerializeField] float potenciaRecargaW = 300f;

        [Header("Escala de tiempo")]
        [Tooltip("Minutos de misión simulados por cada segundo real. Acelera el tiempo " +
                 "para que una partida dure pocos minutos. DEMOSTRACIÓN.")]
        [SerializeField] float minutosMisionPorSegundo = 1f;

        float propulsion01;
        bool turbo;
        float consumoExtraW;
        bool agotada;

        /// <summary>Se dispara una sola vez cuando la batería llega a 0.</summary>
        public event Action AlAgotarse;

        public float CapacidadWh => capacidadWh;
        public float ActualWh { get; private set; }
        public float Fraccion => capacidadWh > 0f ? ActualWh / capacidadWh : 0f;
        public float ConsumoActualW { get; private set; }
        public float PotenciaRecargaW => potenciaRecargaW;
        public float ConsumidaTotalWh { get; private set; }
        public bool Cargando { get; private set; }
        public bool Agotada => agotada;
        public float MinutosMisionPorSegundo => minutosMisionPorSegundo;

        /// <summary>Segundos reales que quedan al consumo actual (sin contar recarga).</summary>
        public float AutonomiaSegundosReales
        {
            get
            {
                float whPorSegundo = ConsumoActualW * minutosMisionPorSegundo / 60f;
                return whPorSegundo > 0f ? ActualWh / whPorSegundo : float.PositiveInfinity;
            }
        }

        void Awake()
        {
            ActualWh = capacidadWh;
        }

        /// <summary>Lo llama el controlador de la sonda cada fotograma.</summary>
        public void EstablecerPropulsion(float intensidad01, bool conTurbo)
        {
            propulsion01 = Mathf.Clamp01(intensidad01);
            turbo = conTurbo;
        }

        /// <summary>Consumo adicional (por ejemplo, el escáner en el próximo incremento).</summary>
        public void EstablecerConsumoExtra(float vatios)
        {
            consumoExtraW = Mathf.Max(0f, vatios);
        }

        /// <summary>Lo llama la estación al entrar o salir la sonda.</summary>
        public void EstablecerCarga(bool cargando)
        {
            Cargando = cargando;
        }

        /// <summary>Para la pantalla de configuración (próximo incremento).</summary>
        public void ConfigurarBateria(float nuevaCapacidadWh)
        {
            capacidadWh = Mathf.Max(1f, nuevaCapacidadWh);
            ActualWh = capacidadWh;
            ConsumidaTotalWh = 0f;
            agotada = false;
        }

        /// <summary>
        /// Fija la carga actual sin cambiar la capacidad máxima (por ejemplo, si la sonda llega
        /// con menos carga tras un descenso prolongado). La recarga en la estación funciona igual.
        /// </summary>
        public void EstablecerCargaActual(float wh)
        {
            ActualWh = Mathf.Clamp(wh, 0f, capacidadWh);
        }

        void Update()
        {
            if (agotada)
            {
                ConsumoActualW = 0f;
                return;
            }

            float multTurbo = turbo ? multiplicadorConsumoTurbo : 1f;
            ConsumoActualW = consumoBaseW + consumoPropulsionW * propulsion01 * multTurbo + consumoExtraW;

            // Horas de misión transcurridas en este fotograma.
            float horas = Time.deltaTime * minutosMisionPorSegundo / 60f;

            float gastoWh = ConsumoActualW * horas;
            float recargaWh = Cargando ? potenciaRecargaW * horas : 0f;

            ConsumidaTotalWh += gastoWh;
            ActualWh = Mathf.Clamp(ActualWh - gastoWh + recargaWh, 0f, capacidadWh);

            if (ActualWh <= 0f)
            {
                agotada = true;
                ConsumoActualW = 0f;
                AlAgotarse?.Invoke();
            }
        }
    }
}
