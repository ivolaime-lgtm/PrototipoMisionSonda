using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Escáner de la sonda. Mantén E mientras apuntas a un punto científico
    /// cercano. Usa el instrumento elegido en la configuración:
    /// - Duración del escaneo (s).
    /// - Consumo mientras escanea (W), que se suma a la batería.
    /// - Datos que genera (MB), que se guardan en memoria.
    /// Si la memoria no tiene espacio, no deja escanear hasta transmitir.
    /// F cierra la ficha abierta.
    ///
    /// Este componente es el único que fija el consumo extra de la batería:
    /// suma el del escaneo y el de la transmisión.
    /// </summary>
    public class Escaner : MonoBehaviour
    {
        [SerializeField] Transform camara;
        [SerializeField] SistemaEnergia energia;
        [SerializeField] SistemaDatos datos;
        [SerializeField] AplicadorConfiguracion aplicador;

        [Tooltip("Distancia máxima para escanear, en metros.")]
        [SerializeField] float alcanceM = 8f;

        [Tooltip("Grosor del rayo de detección, en metros. Más grande = más fácil apuntar.")]
        [SerializeField] float radioDeteccionM = 0.4f;

        [Tooltip("Segundos que se muestra cada mensaje.")]
        [SerializeField] float duracionMensajeS = 4f;

        float tiempoMensaje;
        bool bloqueadoPorMemoria;

        public PuntoCientifico PuntoApuntado { get; private set; }
        public float DistanciaApuntado { get; private set; }
        public float Progreso { get; private set; }
        public bool Escaneando { get; private set; }
        public string Mensaje { get; private set; }
        public FichaCientifica FichaAbierta { get; private set; }

        /// <summary>Energía total gastada escaneando, en Wh (para el informe).</summary>
        public float EnergiaEscaneoWh { get; private set; }

        /// <summary>Veces que se intentó escanear con la memoria llena (para el informe).</summary>
        public int VecesMemoriaLlena { get; private set; }

        public OpcionInstrumento Instrumento =>
            aplicador != null && aplicador.Resumen != null ? aplicador.Resumen.instrumento : null;

        void Awake()
        {
            if (energia == null) energia = FindAnyObjectByType<SistemaEnergia>();
            if (datos == null) datos = FindAnyObjectByType<SistemaDatos>();
            if (aplicador == null) aplicador = FindAnyObjectByType<AplicadorConfiguracion>();
            if (camara == null && energia != null)
            {
                Camera cam = energia.GetComponentInChildren<Camera>();
                if (cam != null) camara = cam.transform;
            }
        }

        void Update()
        {
            Keyboard teclado = Keyboard.current;

            if (teclado != null && FichaAbierta != null && teclado.fKey.wasPressedThisFrame)
                FichaAbierta = null;

            if (tiempoMensaje > 0f)
            {
                tiempoMensaje -= Time.deltaTime;
                if (tiempoMensaje <= 0f) Mensaje = null;
            }

            BuscarPunto();

            bool pulsado = teclado != null && teclado.eKey.isPressed;
            OpcionInstrumento ins = Instrumento;
            float consumoEscaneoW = 0f;
            Escaneando = false;

            bool puede = pulsado && PuntoApuntado != null && !PuntoApuntado.Escaneado
                         && ins != null && datos != null && datos.Configurado
                         && energia != null && !energia.Agotada;

            if (puede)
            {
                if (!datos.HayEspacio(ins.datosPorEscaneoMB))
                {
                    if (!bloqueadoPorMemoria)
                    {
                        VecesMemoriaLlena++;
                        bloqueadoPorMemoria = true;
                    }
                    MostrarMensaje($"Memoria llena: este escaneo necesita {ins.datosPorEscaneoMB:0} MB. " +
                                   "Vuelve a la estación para transmitir.");
                    Progreso = 0f;
                }
                else
                {
                    bloqueadoPorMemoria = false;
                    Escaneando = true;
                    consumoEscaneoW = ins.consumoEscaneoW;
                    EnergiaEscaneoWh += consumoEscaneoW * Time.deltaTime * energia.MinutosMisionPorSegundo / 60f;
                    Progreso += Time.deltaTime / Mathf.Max(0.1f, ins.segundosEscaneo);

                    if (Progreso >= 1f)
                        CompletarEscaneo(ins);
                }
            }
            else
            {
                Progreso = 0f;
                if (!pulsado) bloqueadoPorMemoria = false;
            }

            if (energia != null)
            {
                float transmisionW = datos != null ? datos.ConsumoTransmisionActualW : 0f;
                energia.EstablecerConsumoExtra(consumoEscaneoW + transmisionW);
            }
        }

        void CompletarEscaneo(OpcionInstrumento ins)
        {
            Progreso = 0f;
            int valor = aplicador.Resumen.valorPorEscaneo;
            datos.RegistrarEscaneo(ins.datosPorEscaneoMB, valor);
            PuntoApuntado.MarcarEscaneado();
            FichaAbierta = PuntoApuntado.Ficha;
            MostrarMensaje($"Escaneo completado: +{ins.datosPorEscaneoMB:0} MB en memoria. " +
                           "Solo cuentan cuando los transmitas desde la estación.");
        }

        void BuscarPunto()
        {
            PuntoApuntado = null;
            if (camara == null) return;

            if (Physics.SphereCast(camara.position, radioDeteccionM, camara.forward, out RaycastHit hit,
                                   alcanceM, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                PuntoCientifico p = hit.collider.GetComponentInParent<PuntoCientifico>();
                if (p != null)
                {
                    PuntoApuntado = p;
                    DistanciaApuntado = hit.distance;
                }
            }
        }

        void MostrarMensaje(string texto)
        {
            Mensaje = texto;
            tiempoMensaje = duracionMensajeS;
        }
    }
}
