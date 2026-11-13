using System;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Observación de aproximación: SOLO LÓGICA (sin dibujo ni escena). La usa ControlNave.
    ///
    ///   · En una ventana del crucero (fracciones del progreso del viaje) se capturan datos desde Ciencia.
    ///   · Capturar gasta energía de la NAVE; enviar desde Comunicaciones no gasta.
    ///   · Solo cuentan los datos ENVIADOS antes de llegar (fase Llegada). Lo no enviado se pierde.
    ///   · Da un bonus APARTE (0..puntosMaximos) que no suma a la puntuación principal.
    ///
    /// Las pantallas, el HUD y los modelos futuros solo LEEN estas propiedades. Valores de DEMOSTRACIÓN.
    /// </summary>
    public class ObservacionAproximacion
    {
        [Serializable]
        public struct Ajustes
        {
            [Tooltip("Desactivado: no hay ventana, mensajes, consumos ni interacción; el juego queda como antes.")]
            public bool activa;
            [Tooltip("Progreso del viaje (0..1) en que se abre la ventana de observación.")]
            [Range(0f, 1f)] public float inicioVentana;
            [Tooltip("Duración de la ventana, como fracción del viaje (0,16 = 24 s con un crucero de 150 s).")]
            [Range(0.01f, 1f)] public float duracionVentana;
            [Tooltip("Unidades de datos (UD) que se pueden capturar como máximo.")]
            public float datosMaximos;
            [Tooltip("UD capturadas por segundo manteniendo Espacio en Ciencia (con los tiempos normales).")]
            public float capturaPorSegundo;
            [Tooltip("UD enviadas por segundo manteniendo Espacio en Comunicaciones (con los tiempos normales).")]
            public float transmisionPorSegundo;
            [Tooltip("Energía de la nave (Wh) que cuesta capturar 1 UD. Valor provisional de prueba.")]
            public float costeWhPorUnidad;
            [Tooltip("Bonus máximo, aparte de la puntuación principal, si se envían todas las UD posibles.")]
            public int puntosMaximos;

            /// <summary>
            /// Valores provisionales: 12 UD × 21 Wh = 252 Wh ≈ una cuarta parte de la energía inicial de la nave
            /// (CatalogoNave.energiaNaveWh = 1000 Wh). Ventana entre la avería (0,30) y los escombros (0,60).
            /// </summary>
            public static Ajustes PorDefecto => new Ajustes
            {
                activa = true,
                inicioVentana = 0.40f,
                duracionVentana = 0.16f,
                datosMaximos = 12f,
                capturaPorSegundo = 1f,
                transmisionPorSegundo = 2f,
                costeWhPorUnidad = 21f,
                puntosMaximos = 6
            };
        }

        public enum EstadoVentana { Pendiente, Abierta, Cerrada }

        readonly Ajustes a;

        public ObservacionAproximacion(Ajustes ajustes) { a = ajustes; }

        public Ajustes Configuracion => a;
        public bool Activa => a.activa;

        public float Capturados { get; private set; }
        public float Enviados { get; private set; }
        public float Pendientes => Mathf.Max(0f, Capturados - Enviados);
        public float EnergiaUsadaWh { get; private set; }
        /// <summary>True cuando ya no se puede enviar (se llegó al destino o se abortó).</summary>
        public bool Finalizada { get; private set; }
        public float Perdidos => Finalizada ? Pendientes : 0f;
        public bool Usada => Capturados > 0f;

        public float FinVentana => Mathf.Clamp01(a.inicioVentana + a.duracionVentana);

        public EstadoVentana Ventana(float progreso)
        {
            if (!a.activa) return EstadoVentana.Pendiente;
            if (progreso < a.inicioVentana) return EstadoVentana.Pendiente;
            return progreso < FinVentana ? EstadoVentana.Abierta : EstadoVentana.Cerrada;
        }

        /// <summary>Segundos que faltan para que se abra (Pendiente) o se cierre (Abierta) la ventana.</summary>
        public float SegundosVentana(float progreso, float segundosCrucero)
        {
            float objetivo = progreso < a.inicioVentana ? a.inicioVentana : FinVentana;
            return Mathf.Max(0f, (objetivo - progreso) * segundosCrucero);
        }

        public bool PuedeCapturar(float progreso) =>
            a.activa && !Finalizada && Ventana(progreso) == EstadoVentana.Abierta && Capturados < a.datosMaximos;

        public bool PuedeEnviar => a.activa && !Finalizada && Pendientes > 0f;

        /// <summary>
        /// Captura durante dt segundos. escala = aceleración del modo de prueba (1 con los tiempos normales).
        /// Nunca deja la energía de la nave a cero por sí sola. Devuelve la energía gastada (Wh).
        /// </summary>
        public float Capturar(float dt, float escala, float progreso, float energiaNaveWh)
        {
            if (!PuedeCapturar(progreso) || a.costeWhPorUnidad < 0f) return 0f;
            float ud = Mathf.Min(a.capturaPorSegundo * escala * dt, a.datosMaximos - Capturados);
            if (a.costeWhPorUnidad > 0f)
                ud = Mathf.Min(ud, Mathf.Max(0f, energiaNaveWh - 1f) / a.costeWhPorUnidad);   // deja al menos 1 Wh
            if (ud <= 0f) return 0f;
            Capturados += ud;
            float gasto = ud * a.costeWhPorUnidad;
            EnergiaUsadaWh += gasto;
            return gasto;
        }

        /// <summary>Envía durante dt segundos. Devuelve las UD enviadas.</summary>
        public float Enviar(float dt, float escala)
        {
            if (!PuedeEnviar) return 0f;
            float ud = Mathf.Min(a.transmisionPorSegundo * escala * dt, Pendientes);
            Enviados += ud;
            return ud;
        }

        /// <summary>Al llegar al destino (o abortar): lo que quede pendiente ya no se puede enviar.</summary>
        public void Finalizar() { if (a.activa) Finalizada = true; }

        /// <summary>Bonus aparte: proporcional a las UD enviadas respecto al máximo posible.</summary>
        public int Bonus => a.activa && a.datosMaximos > 0f
            ? Mathf.Clamp(Mathf.RoundToInt(a.puntosMaximos * Enviados / a.datosMaximos), 0, a.puntosMaximos)
            : 0;
    }
}
