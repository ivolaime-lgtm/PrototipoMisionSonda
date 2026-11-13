using System;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Corrección de trayectoria antes del frenado: SOLO LÓGICA (sin dibujo). La usa ControlNave.
    ///
    ///   · Al final del crucero, con la avería reparada y los escombros terminados, se abre una ventana de duración fija.
    ///   · Aparece un corredor de entrada desplazado de la ruta actual. El jugador lleva la nave hasta él con los
    ///     propulsores de siempre (WASD en Pilotaje): el objetivo se compara con el MISMO desplazamiento lateral.
    ///   · Completar = permanecer dentro de la tolerancia los segundos indicados SEGUIDOS (si sale, se reinicia).
    ///   · Al completarla o agotarse el tiempo, la ventana termina y el viaje sigue. Opcional: ignorarla no penaliza.
    ///   · Efecto único: el periodo del marcador de la inserción orbital (más lento = más fácil frenar en el centro).
    ///
    /// La vista (VistaCorredor) y los textos solo LEEN este estado. Valores de DEMOSTRACIÓN.
    /// </summary>
    public class CorredorAproximacion
    {
        [Serializable]
        public struct Ajustes
        {
            [Tooltip("Desactivado: sin anillo, mensajes, consumos ni demora de llegada; el frenado queda como antes.")]
            public bool activa;
            [Tooltip("Progreso del viaje a partir del cual puede abrirse la ventana (además: avería reparada y escombros terminados).")]
            [Range(0f, 1f)] public float inicioProgreso;
            [Tooltip("Duración de la ventana con los tiempos normales (s). La llegada espera a que termine.")]
            public float duracionS;
            [Tooltip("Duración de la ventana en Modo Prueba (s). No se acelera: hay que caminar y maniobrar igual.")]
            public float duracionPruebaS;
            [Tooltip("Distancia mínima y máxima (m) entre la posición de la nave al abrirse y el corredor.")]
            public float distanciaMinimaM, distanciaMaximaM;
            [Tooltip("Distancia (m) al centro del corredor que cuenta como «dentro».")]
            public float toleranciaM;
            [Tooltip("Segundos SEGUIDOS dentro de la tolerancia para completar la corrección.")]
            public float permanenciaS;
            [Tooltip("Completada antes de esta fracción de la ventana = «a tiempo»; después = «tarde».")]
            [Range(0.05f, 1f)] public float fraccionATiempo;
            [Tooltip("Periodo (s) del marcador de inserción sin corregir: el de siempre.")]
            public float periodoSinCorregirS;
            [Tooltip("Periodo (s) del marcador de inserción si se corrigió a tiempo.")]
            public float periodoATiempoS;
            [Tooltip("Periodo (s) del marcador de inserción si se corrigió tarde.")]
            public float periodoTardeS;

            public static Ajustes PorDefecto => new Ajustes
            {
                activa = true,
                inicioProgreso = 0.85f,
                duracionS = 22f,
                duracionPruebaS = 18f,
                distanciaMinimaM = 5f,
                distanciaMaximaM = 9f,
                toleranciaM = 1.5f,
                permanenciaS = 2f,
                fraccionATiempo = 0.5f,
                periodoSinCorregirS = 2.2f,
                periodoATiempoS = 3.3f,
                periodoTardeS = 2.75f
            };
        }

        public enum Estado { Pendiente, Activa, Terminada }
        public enum Resultado { SinCorregir, ATiempo, Tarde }

        readonly Ajustes a;
        public CorredorAproximacion(Ajustes ajustes) { a = ajustes; }

        public Ajustes Configuracion => a;
        public bool Activa => a.activa;
        public Estado EstadoActual { get; private set; } = Estado.Pendiente;
        public Resultado ResultadoFinal { get; private set; } = Resultado.SinCorregir;

        /// <summary>Centro del corredor, en el mismo sistema que el desplazamiento lateral de la nave (m).</summary>
        public Vector2 Objetivo { get; private set; }
        public Vector2 DesplazamientoInicial { get; private set; }
        public float DuracionVentanaS { get; private set; }
        public float TiempoVentanaS { get; private set; }
        public float PermanenciaActualS { get; private set; }
        /// <summary>Segundos desde la apertura hasta completar los 2 s seguidos (−1 si no se completó).</summary>
        public float SegundosAlCompletar { get; private set; } = -1f;
        /// <summary>Distancia (m) al corredor en el último fotograma.</summary>
        public float Error { get; private set; }
        public bool Dentro => Error <= a.toleranciaM;
        /// <summary>Combustible de maniobra gastado mientras la ventana estaba abierta (kg). Lo suma ControlNave.</summary>
        public float CombustibleKg { get; set; }

        public float SegundosRestantes => Mathf.Max(0f, DuracionVentanaS - TiempoVentanaS);
        public bool Completada => SegundosAlCompletar >= 0f;

        public bool PuedeAbrir(float progreso, bool eventosResueltos) =>
            a.activa && EstadoActual == Estado.Pendiente && eventosResueltos && progreso >= a.inicioProgreso;

        /// <summary>
        /// Abre la ventana. El corredor se coloca a una distancia entre la mínima y la máxima de la posición actual,
        /// dentro del límite de desplazamiento y en dirección que NO sigue la deriva actual (la inercia no lo completa sola).
        /// </summary>
        public void Abrir(Vector2 desplazamiento, Vector2 velocidad, float limiteM, bool modoPrueba)
        {
            if (!a.activa || EstadoActual != Estado.Pendiente) return;
            float lim = Mathf.Max(0f, limiteM - a.toleranciaM);
            float dist = UnityEngine.Random.Range(a.distanciaMinimaM, Mathf.Max(a.distanciaMinimaM, a.distanciaMaximaM));
            Vector2 elegido = Vector2.zero;
            bool ok = false;
            for (int i = 0; i < 24 && !ok; i++)
            {
                float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                if (velocidad.sqrMagnitude > 0.01f && Vector2.Dot(dir, velocidad) > 0f) continue;   // no a favor de la deriva
                Vector2 c = desplazamiento + dir * dist;
                if (Mathf.Abs(c.x) <= lim && Mathf.Abs(c.y) <= lim) { elegido = c; ok = true; }
            }
            if (!ok)
            {
                // Cerca del borde: hacia el centro de la ruta (sigue estando a «dist» o lo más lejos posible dentro del límite).
                Vector2 haciaCentro = desplazamiento.sqrMagnitude > 0.0001f ? -desplazamiento.normalized : Vector2.right;
                elegido = desplazamiento + haciaCentro * dist;
                elegido.x = Mathf.Clamp(elegido.x, -lim, lim);
                elegido.y = Mathf.Clamp(elegido.y, -lim, lim);
            }
            Objetivo = elegido;
            DesplazamientoInicial = desplazamiento;
            DuracionVentanaS = Mathf.Max(1f, modoPrueba ? a.duracionPruebaS : a.duracionS);
            TiempoVentanaS = 0f;
            PermanenciaActualS = 0f;
            Error = (Objetivo - desplazamiento).magnitude;
            EstadoActual = Estado.Activa;
        }

        /// <summary>Avanza la ventana. Devuelve true en el fotograma en que termina (completada o agotada).</summary>
        public bool Actualizar(float dt, Vector2 desplazamiento)
        {
            if (EstadoActual != Estado.Activa) return false;
            TiempoVentanaS += dt;
            Error = (Objetivo - desplazamiento).magnitude;
            PermanenciaActualS = Dentro ? PermanenciaActualS + dt : 0f;   // salir reinicia la permanencia

            if (PermanenciaActualS >= a.permanenciaS)
            {
                SegundosAlCompletar = TiempoVentanaS;
                ResultadoFinal = SegundosAlCompletar <= a.fraccionATiempo * DuracionVentanaS ? Resultado.ATiempo : Resultado.Tarde;
                EstadoActual = Estado.Terminada;
                return true;
            }
            if (TiempoVentanaS >= DuracionVentanaS)
            {
                ResultadoFinal = Resultado.SinCorregir;
                EstadoActual = Estado.Terminada;
                return true;
            }
            return false;
        }

        /// <summary>Periodo del marcador de inserción según el resultado (registrado una sola vez).</summary>
        public float PeriodoInsercionS
        {
            get
            {
                switch (ResultadoFinal)
                {
                    case Resultado.ATiempo: return a.periodoATiempoS;
                    case Resultado.Tarde: return a.periodoTardeS;
                    default: return a.periodoSinCorregirS;
                }
            }
        }

        public string NombreResultado()
        {
            switch (ResultadoFinal)
            {
                case Resultado.ATiempo: return "corregida a tiempo";
                case Resultado.Tarde: return "corregida tarde";
                default: return "sin corregir";
            }
        }
    }
}
