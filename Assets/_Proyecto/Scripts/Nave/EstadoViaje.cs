using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Resultado del viaje en la nave. Lo rellena ControlNave y lo copia el informe final.
    /// Se reinicia al pulsar Play, al iniciar una misión y al entrar en la escena Nave.
    /// Si se juega Exploracion sin pasar por la nave, Realizado es false.
    /// </summary>
    public static class EstadoViaje
    {
        public static bool Realizado;
        public static bool Abortado;
        public static string MotivoAborto;

        public static float MasaInicialKg;
        public static float CombustibleInicialKg, CombustibleExtraKg, CombustibleFinalKg;
        public static float CombustibleManiobrasKg, CombustibleInsercionKg;
        /// <summary>Combustible justo antes de la inserción orbital (kg); −1 si no se llegó a frenar.</summary>
        public static float CombustibleAntesInsercionKg = -1f;
        public static float IntegridadFinal;
        public static int Impactos, EscombrosEsquivados;
        public static float EnergiaInicialWh, EnergiaFinalWh;
        public static float SegundosAveria;
        public static int ErroresAveria;
        public static float ErrorInsercion;        // 0 = perfecto, 0,5 = peor; -1 si no hubo inserción
        public static float SegundosViaje;
        public static float ZonaVerdeInsercion = 0.08f;

        // Evaluación del viaje (se calcula al entrar en órbita, antes de desplegar la sonda)
        public static bool Evaluado;
        public static float PerdidaEnergiaNave;      // fracción 0..1
        public static int PuntosReparacion, PuntosEscombros;
        public static int PuntosEtapa = -1;          // -1 = no evaluada
        public static string ResultadoEtapa = "";
        public static string Lanzador = "";
        public static int PuntosViaje => Evaluado ? PuntosReparacion + PuntosEscombros + Mathf.Max(0, PuntosEtapa) : 0;

        // Estado de la sonda al desplegarla (regla simplificada)
        public static float ReduccionCargaSonda;     // fracción 0..0,1
        public static bool CargaSondaAplicada;       // true cuando Exploracion ya la aplicó (una sola vez)
        public static float CargaInicialSondaWh = -1f;

        // Observación de aproximación (bonus aparte; no suma a la puntuación principal)
        public static bool ObservacionActiva;
        public static float ObservacionCapturadaUD, ObservacionEnviadaUD, ObservacionMaximaUD, EnergiaObservacionWh;
        public static int BonoObservacion, BonoObservacionMax;

        // Corrección de trayectoria antes del frenado (no da puntos; solo cambia el periodo del marcador de inserción)
        public static bool CorreccionActiva, CorreccionAbierta;
        public static string CorreccionResultado = "";
        public static float CorreccionSegundos = -1f, CorreccionDuracionS, CorreccionCombustibleKg;
        public static float PeriodoInsercionS = 2.2f, PeriodoInsercionBaseS = 2.2f;

        // Navegación longitudinal (opcional). El combustible del motor principal es una categoría propia del libro.
        public static bool NavegacionActiva;
        public static float CombustibleMotorPrincipalKg;
        /// <summary>Combustible inicial de PRUEBA (kg); −1 si la partida usó el combustible calculado.</summary>
        public static float CombustiblePruebaKg = -1f;
        public static float RutaM, VelocidadMaximaMS;
        public static int Sobrepasos;
        // Gravedad experimental (un eje): efecto del motor y de la gravedad por separado.
        public static bool GravedadActiva;
        public static float DeltaVMotorMS, DeltaVGravedadMS;

        /// <summary>
        /// Escena con la que empieza una misión: la Nave si está activada y en la lista de compilación;
        /// si no, Exploracion directamente (así el juego sigue funcionando sin la escena Nave).
        /// </summary>
        public static string EscenaDeInicio(bool pasarPorNave, string escenaNave, string escenaExploracion)
        {
            if (pasarPorNave)
            {
                if (Application.CanStreamedLevelBeLoaded(escenaNave)) return escenaNave;
                Debug.LogWarning($"[Prototipo] La escena {escenaNave} no está en la lista de compilación " +
                                 "(menú Prototipo/8). Se va directamente a " + escenaExploracion + ".");
            }
            return escenaExploracion;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar()
        {
            Realizado = false;
            Abortado = false;
            MotivoAborto = "";
            MasaInicialKg = 0f;
            CombustibleInicialKg = CombustibleExtraKg = CombustibleFinalKg = 0f;
            CombustibleManiobrasKg = CombustibleInsercionKg = 0f;
            CombustibleAntesInsercionKg = -1f;
            IntegridadFinal = 0f;
            Impactos = EscombrosEsquivados = 0;
            EnergiaInicialWh = EnergiaFinalWh = 0f;
            SegundosAveria = 0f;
            ErroresAveria = 0;
            ErrorInsercion = -1f;
            SegundosViaje = 0f;
            ZonaVerdeInsercion = 0.08f;
            Evaluado = false;
            PerdidaEnergiaNave = 0f;
            PuntosReparacion = PuntosEscombros = 0;
            PuntosEtapa = -1;
            ResultadoEtapa = "";
            Lanzador = "";
            ReduccionCargaSonda = 0f;
            CargaSondaAplicada = false;
            CargaInicialSondaWh = -1f;
            ObservacionActiva = false;
            ObservacionCapturadaUD = ObservacionEnviadaUD = ObservacionMaximaUD = EnergiaObservacionWh = 0f;
            BonoObservacion = BonoObservacionMax = 0;
            CorreccionActiva = CorreccionAbierta = false;
            CorreccionResultado = "";
            CorreccionSegundos = -1f;
            CorreccionDuracionS = CorreccionCombustibleKg = 0f;
            PeriodoInsercionS = PeriodoInsercionBaseS = 2.2f;
            NavegacionActiva = false;
            CombustibleMotorPrincipalKg = 0f;
            CombustiblePruebaKg = -1f;
            RutaM = VelocidadMaximaMS = 0f;
            Sobrepasos = 0;
            GravedadActiva = false;
            DeltaVMotorMS = DeltaVGravedadMS = 0f;
        }
    }
}
