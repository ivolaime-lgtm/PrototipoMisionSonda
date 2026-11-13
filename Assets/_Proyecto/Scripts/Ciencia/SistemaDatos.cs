using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Lleva la cuenta de los datos científicos:
    /// - Recogidos: guardados en la memoria de la sonda tras escanear.
    /// - Transmitidos: enviados desde la estación al satélite de relevo.
    /// Solo cuentan para el objetivo los escaneos TRANSMITIDOS.
    ///
    /// La transmisión ocurre cuando la sonda está en la estación
    /// (la misma zona que recarga la batería) y hay contacto con el relevo.
    /// Memoria, velocidad, contacto y consumo vienen de la configuración elegida.
    /// </summary>
    public class SistemaDatos : MonoBehaviour
    {
        [SerializeField] SistemaEnergia energia;
        [SerializeField] AplicadorConfiguracion aplicador;

        [Tooltip("Duración de un ciclo de paso del satélite de relevo, en segundos reales. " +
                 "Con contacto del 50 %, hay enlace la primera mitad del ciclo. DEMOSTRACIÓN.")]
        [SerializeField] float periodoVentanaS = 12f;

        class Paquete
        {
            public float mbRestantes;
            public int valor;
        }

        readonly List<Paquete> pendientes = new List<Paquete>();

        bool configurado;
        bool avisado;
        float velocidadMBs;
        float fraccionContacto = 1f;
        float consumoTransmisionW;

        public event Action AlCumplirObjetivo;

        public bool Configurado => configurado;
        public float MemoriaMB { get; private set; }
        public float DatosEnMemoriaMB { get; private set; }
        public float DatosRecogidosMB { get; private set; }
        public float DatosTransmitidosMB { get; private set; }
        public int EscaneosRealizados { get; private set; }
        public int EscaneosTransmitidos { get; private set; }
        public int EscaneosRequeridos { get; private set; } = 3;
        public int ValorTransmitido { get; private set; }
        public float VelocidadEnlaceMBs => velocidadMBs;

        /// <summary>Puntos científicos que había en la escena al empezar.</summary>
        public int PuntosEnEscena { get; private set; }

        public bool EnEstacion => energia != null && energia.Cargando;
        public bool HayContacto { get; private set; }
        public bool Transmitiendo { get; private set; }
        public float SegundosHastaContacto { get; private set; }
        public float ConsumoTransmisionActualW => Transmitiendo ? consumoTransmisionW : 0f;
        public bool ObjetivoCumplido => configurado && EscaneosTransmitidos >= EscaneosRequeridos;

        /// <summary>Energía total gastada transmitiendo, en Wh (para el informe).</summary>
        public float EnergiaTransmisionWh { get; private set; }

        /// <summary>Segundos en la estación con datos pendientes pero sin contacto (para el informe).</summary>
        public float SegundosSinContacto { get; private set; }

        void Awake()
        {
            if (energia == null) energia = FindAnyObjectByType<SistemaEnergia>();
            if (aplicador == null) aplicador = FindAnyObjectByType<AplicadorConfiguracion>();
        }

        void Update()
        {
            if (!configurado) IntentarConfigurar();
            if (!configurado) return;

            ActualizarContacto();

            Transmitiendo = EnEstacion && HayContacto && DatosEnMemoriaMB > 0f && !energia.Agotada;
            if (Transmitiendo)
            {
                Transmitir(velocidadMBs * Time.deltaTime);
                EnergiaTransmisionWh += consumoTransmisionW * Time.deltaTime * energia.MinutosMisionPorSegundo / 60f;
            }
            else if (EnEstacion && !HayContacto && DatosEnMemoriaMB > 0f && !energia.Agotada)
            {
                SegundosSinContacto += Time.deltaTime;
            }

            if (!avisado && ObjetivoCumplido)
            {
                avisado = true;
                AlCumplirObjetivo?.Invoke();
            }
        }

        void IntentarConfigurar()
        {
            if (aplicador == null) return;
            ResumenMision r = aplicador.Resumen;
            if (r == null || r.objetivo == null || r.orbita == null) return;

            MemoriaMB = r.memoriaMB;
            EscaneosRequeridos = Mathf.Max(1, r.objetivo.escaneosRequeridos);
            velocidadMBs = r.orbita.velocidadEnlaceMBs;
            fraccionContacto = Mathf.Clamp01(r.orbita.fraccionContacto);
            consumoTransmisionW = r.consumoTransmisionW;
            configurado = true;

            PuntosEnEscena = PuntoCientifico.Todos.Count;
            if (PuntosEnEscena < EscaneosRequeridos)
                Debug.LogWarning($"[Prototipo] El objetivo pide {EscaneosRequeridos} escaneos, pero la escena solo tiene " +
                                 $"{PuntosEnEscena} puntos científicos: el objetivo no se puede cumplir. " +
                                 "Añade puntos (prefab PuntoCientifico) o baja «Escaneos requeridos» en el catálogo.", this);
        }

        void ActualizarContacto()
        {
            if (fraccionContacto >= 0.999f)
            {
                HayContacto = true;
                SegundosHastaContacto = 0f;
                return;
            }
            if (fraccionContacto <= 0f || periodoVentanaS <= 0f)
            {
                HayContacto = false;
                SegundosHastaContacto = float.PositiveInfinity;
                return;
            }

            float t = Time.time % periodoVentanaS;
            float ventana = fraccionContacto * periodoVentanaS;
            HayContacto = t < ventana;
            SegundosHastaContacto = HayContacto ? 0f : periodoVentanaS - t;
        }

        public bool HayEspacio(float mb)
        {
            return DatosEnMemoriaMB + mb <= MemoriaMB + 0.001f;
        }

        public void RegistrarEscaneo(float mb, int valor)
        {
            pendientes.Add(new Paquete { mbRestantes = mb, valor = valor });
            DatosEnMemoriaMB += mb;
            DatosRecogidosMB += mb;
            EscaneosRealizados++;
        }

        void Transmitir(float mb)
        {
            while (mb > 0f && pendientes.Count > 0)
            {
                Paquete p = pendientes[0];
                float envio = Mathf.Min(mb, p.mbRestantes);
                p.mbRestantes -= envio;
                mb -= envio;
                DatosEnMemoriaMB = Mathf.Max(0f, DatosEnMemoriaMB - envio);
                DatosTransmitidosMB += envio;

                if (p.mbRestantes <= 0.0001f)
                {
                    pendientes.RemoveAt(0);
                    EscaneosTransmitidos++;
                    ValorTransmitido += p.valor;
                }
            }
        }
    }
}
