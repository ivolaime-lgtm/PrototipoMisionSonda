using System;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Navegación longitudinal (opcional): SOLO LÓGICA, sin dibujo. La usa ControlNave.
    ///
    ///   · La ruta es una recta de «distanciaRutaM» metros entre la salida (0) y el punto de aproximación (L).
    ///     Sin gravedad, sin rozamiento: la nave conserva su velocidad (vuelo por inercia).
    ///   · El motor principal empuja en el sentido al que apunta la proa: a = empuje / masa (los mismos valores del motor
    ///     elegido; no se cambian masas, empujes ni consumos).
    ///   · Girar 180° tarda «duracionGiroS» y no cambia la velocidad. Durante el giro el motor principal está bloqueado.
    ///   · Al frenar hasta velocidad 0, la velocidad se queda en 0 exactamente y el motor se apaga hasta soltar Espacio;
    ///     la orientación NO cambia sola. Si se vuelve a encender, la nave se mueve hacia donde apunta la proa.
    ///   · Zona de aproximación: L ± «semiZonaM». La inserción se autoriza dentro de la zona a ≤ «velocidadCapturaMS».
    ///     Pasarse de largo = salir de la zona por el lado del destino (posición > L + semiZona), no atravesar el planeta.
    ///
    /// Unidades: metros, segundos, m/s y kg. Tiempo real (sin compresión temporal); la distancia de la ruta está
    /// COMPRIMIDA: es una ruta corta de juego, no la distancia real a una luna. Valores de DEMOSTRACIÓN.
    /// </summary>
    public class NavegacionLongitudinal
    {
        [Serializable]
        public struct Ajustes
        {
            [Tooltip("Desactivado (por defecto): el viaje es el de siempre, con progreso guionizado. Activado: el jugador acelera, " +
                     "vuela por inercia, gira y frena con el motor principal desde Pilotaje.")]
            public bool activa;
            [Tooltip("Longitud de la ruta hasta el centro de la zona de aproximación con los tiempos normales (m). Ruta de juego comprimida.")]
            public float distanciaRutaM;
            [Tooltip("Longitud de la ruta en Modo Prueba (m).")]
            public float distanciaRutaPruebaM;
            [Tooltip("Velocidad hacia el destino al empezar el crucero (m/s): la deja el lanzador. No gasta combustible de la nave.")]
            public float velocidadInicialMS;
            [Tooltip("Media longitud de la zona de aproximación (m): la zona va de L − este valor a L + este valor.")]
            public float semiZonaM;
            [Tooltip("Velocidad máxima (m/s), en cualquier sentido, para que se autorice la inserción dentro de la zona.")]
            public float velocidadCapturaMS;
            [Tooltip("Segundos que tarda el giro de 180°. El motor principal queda bloqueado mientras dura.")]
            public float duracionGiroS;
            [Tooltip("Velocidad mínima de regreso (m/s) que usa la regla de «llegada imposible» para calcular el combustible necesario.")]
            public float velocidadRegresoMinimaMS;
            [Tooltip("Velocidad visual de las estrellas por cada m/s de la nave (solo dibujo).")]
            public float escalaVisualEstrellas;

            [Header("Destino y gravedad")]
            [Tooltip("Activado (por defecto): la ruta, la zona y la velocidad de captura salen del destino elegido en la selección de misión " +
                     "(CatalogoMision → Destinos). Desactivado: se usan los valores de arriba, como en la versión anterior.")]
            public bool usarDestinoElegido;
            [Tooltip("EXPERIMENTAL, desactivado por defecto. Atracción simplificada del destino en UN EJE (a lo largo de la ruta). " +
                     "No hay órbitas ni asistencias gravitatorias: ver LEEME_EQUIPO.")]
            public bool gravedadExperimental;

            [Header("Solo para pruebas")]
            [Tooltip("SOLO PRUEBAS. Activado: la nave empieza con «combustiblePruebaKg» en lugar del calculado. No cambia el catálogo " +
                     "ni los tanques; solo actúa con la navegación activada. Desactivado por defecto.")]
            public bool combustiblePruebaActivo;
            [Tooltip("SOLO PRUEBAS. Combustible inicial (kg). Se limita a la capacidad del tanque elegido.")]
            public float combustiblePruebaKg;

            public static Ajustes PorDefecto => new Ajustes
            {
                activa = false,
                distanciaRutaM = 8000f,
                distanciaRutaPruebaM = 2500f,
                velocidadInicialMS = 20f,
                semiZonaM = 250f,
                velocidadCapturaMS = 15f,
                duracionGiroS = 3f,
                velocidadRegresoMinimaMS = 1f,
                escalaVisualEstrellas = 1.75f,
                usarDestinoElegido = true,
                gravedadExperimental = false,
                combustiblePruebaActivo = false,
                combustiblePruebaKg = 240f
            };
        }

        readonly Ajustes a;
        public NavegacionLongitudinal(Ajustes ajustes) { a = ajustes; }

        public Ajustes Configuracion => a;
        public bool Activa => a.activa;

        /// <summary>Longitud de la ruta (m) hasta el centro de la zona de aproximación.</summary>
        public float L { get; private set; }
        /// <summary>Posición a lo largo de la ruta (m). 0 = salida; L = centro de la zona de aproximación.</summary>
        public float PosicionM { get; private set; }
        /// <summary>Velocidad con signo (m/s): positiva = hacia el destino por la ruta.</summary>
        public float VelocidadMS { get; private set; }
        /// <summary>+1: la proa apunta en el sentido de la ruta (hacia el destino si no lo has pasado). −1: al contrario.</summary>
        public int Orientacion { get; private set; } = 1;
        public bool Girando { get; private set; }
        public float TiempoGiroS { get; private set; }
        int orientacionInicioGiro = 1;
        /// <summary>Fracción máxima de la ruta alcanzada (0..1). Nunca baja: retroceder no repite eventos.</summary>
        public float ProgresoMaximo { get; private set; }
        public float VelocidadMaximaMS { get; private set; }
        /// <summary>True en el fotograma en que el motor se cortó al llegar a velocidad 0.</summary>
        public bool CortePorCero { get; private set; }
        /// <summary>Fracción (0..1) del último paso con el motor encendido.</summary>
        public float FraccionMotor { get; private set; }

        // ------------------------------------------------------------------ Gravedad experimental (un eje)
        //
        // Representa: la componente A LO LARGO DE LA RUTA de la atracción de un cuerpo cuyo centro está a «DistanciaPasoM» del
        // punto L (la ruta pasa a su lado). a(x) = −μ·x / (x² + b²)^(3/2), con x = posición − L. Como b > 0, no hay
        // singularidad: la atracción es máxima en |x| = b/√2 y vale 0 en el punto de paso.
        // NO representa: la componente lateral (se supone compensada), órbitas, asistencias gravitatorias ni varios cuerpos.
        // Límite de seguridad: la nave nunca se acerca al centro a menos de b, y b nunca es menor que el radio de seguridad
        // del cuerpo (DefinicionDestino lo corrige), así que no puede alcanzar la superficie.

        public bool GravedadActiva { get; private set; }
        public float Mu { get; private set; }
        public float DistanciaPasoM { get; private set; } = 1f;

        /// <summary>Activa la atracción en un eje. mu = G·M (m³/s²); b = distancia de paso (m, mayor que 0).</summary>
        public void ConfigurarGravedad(float mu, float b)
        {
            Mu = Mathf.Max(0f, mu);
            DistanciaPasoM = Mathf.Max(1f, b);
            GravedadActiva = Mu > 0f;
        }

        float X(float pos) => pos - L;
        /// <summary>Aceleración de la gravedad a lo largo de la ruta en una posición (m/s², positiva = sentido de la ruta).</summary>
        public float AceleracionGravedadEn(float pos)
        {
            if (!GravedadActiva) return 0f;
            float x = X(pos), r2 = x * x + DistanciaPasoM * DistanciaPasoM;
            return -Mu * x / (r2 * Mathf.Sqrt(r2));
        }
        /// <summary>Energía potencial por unidad de masa (J/kg). 0 sin gravedad.</summary>
        public float PotencialEn(float pos)
        {
            if (!GravedadActiva) return 0f;
            float x = X(pos);
            return -Mu / Mathf.Sqrt(x * x + DistanciaPasoM * DistanciaPasoM);
        }
        float PotencialBordeZona => PotencialEn(L + a.semiZonaM);

        /// <summary>Aceleración actual de la gravedad (m/s², con signo de la ruta).</summary>
        public float AceleracionGravedadMS2 => AceleracionGravedadEn(PosicionM);
        /// <summary>Aceleración del motor en el último paso (m/s², con signo de la ruta; 0 apagado).</summary>
        public float AceleracionMotorMS2 { get; private set; }
        /// <summary>Cambio de velocidad total que ha dado el motor principal (m/s, siempre positivo).</summary>
        public float DeltaVMotorTotalMS { get; private set; }
        /// <summary>Cambio de velocidad total que ha dado la gravedad (m/s, siempre positivo).</summary>
        public float DeltaVGravedadTotalMS { get; private set; }

        public void Iniciar(bool modoPrueba)
        {
            L = Mathf.Max(100f, modoPrueba ? a.distanciaRutaPruebaM : a.distanciaRutaM);
            PosicionM = 0f;
            VelocidadMS = a.velocidadInicialMS;
            VelocidadMaximaMS = Mathf.Abs(VelocidadMS);
            Orientacion = 1;
            Girando = false;
            TiempoGiroS = 0f;
            ProgresoMaximo = 0f;
            AceleracionMotorMS2 = 0f;
            DeltaVMotorTotalMS = DeltaVGravedadTotalMS = 0f;
        }

        // ------------------------------------------------------------------ Lecturas

        /// <summary>Distancia con signo al centro de la zona (m): positiva = el destino está por delante en la ruta.</summary>
        public float DistanciaAlDestinoM => L - PosicionM;
        public float DistanciaAbsolutaM => Mathf.Abs(DistanciaAlDestinoM);
        /// <summary>Distancia (m) hasta el borde más cercano de la zona; 0 dentro de la zona.</summary>
        public float DistanciaAZonaM => Mathf.Max(0f, DistanciaAbsolutaM - a.semiZonaM);
        public bool EnZona => DistanciaAbsolutaM <= a.semiZonaM;
        public bool Sobrepasada => PosicionM > L + a.semiZonaM;
        /// <summary>
        /// Velocidad que cuenta para la captura (m/s). Sin gravedad, la velocidad actual. Con gravedad, la que tendría la nave en el
        /// borde de la zona con la misma energía (dentro de la zona la gravedad la acelera, pero esta referencia no cambia sin motor,
        /// así que la autorización no parpadea al acercarse al centro).
        /// </summary>
        public float VelocidadReferenciaCapturaMS
        {
            get
            {
                if (!GravedadActiva) return Mathf.Abs(VelocidadMS);
                float v2 = VelocidadMS * VelocidadMS + 2f * (PotencialEn(PosicionM) - PotencialBordeZona);
                return Mathf.Sqrt(Mathf.Max(0f, v2));
            }
        }
        public bool VelocidadDeCaptura => VelocidadReferenciaCapturaMS <= a.velocidadCapturaMS;
        public bool PuedeCapturar => EnZona && VelocidadDeCaptura;

        const float Quieta = 0.05f;
        /// <summary>Velocidad hacia el destino (m/s): positiva si se acerca, negativa si se aleja.</summary>
        public float VelocidadDeAcercamientoMS => DistanciaAlDestinoM >= 0f ? VelocidadMS : -VelocidadMS;
        public bool SeAcerca => VelocidadDeAcercamientoMS > Quieta;
        public bool SeAleja => VelocidadDeAcercamientoMS < -Quieta;
        public bool Detenida => !SeAcerca && !SeAleja;
        /// <summary>True si la proa apunta hacia el destino (el motor principal acercaría la nave).</summary>
        public bool ProaHaciaDestino => (DistanciaAlDestinoM >= 0f ? 1 : -1) == Orientacion;
        /// <summary>True si el motor principal, encendido ahora, reduciría la velocidad actual.</summary>
        public bool MotorFrena => Mathf.Abs(VelocidadMS) > Quieta && Mathf.Sign(VelocidadMS) != Orientacion;

        /// <summary>Ángulo visual de la nave respecto a la ruta (grados, 0..360). 0 = proa en el sentido de la ruta.</summary>
        public float AnguloGrados
        {
            get
            {
                float desde = orientacionInicioGiro > 0 ? 0f : 180f;
                if (!Girando) return Orientacion > 0 ? 0f : 180f;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(TiempoGiroS / Mathf.Max(0.01f, a.duracionGiroS)));
                return Mathf.Repeat(desde + 180f * t, 360f);
            }
        }

        public float SegundosGiroRestantes => Girando ? Mathf.Max(0f, a.duracionGiroS - TiempoGiroS) : 0f;

        /// <summary>Segundos hasta el borde de la zona a la velocidad actual (infinito si no se acerca).</summary>
        public float SegundosAZona => EnZona ? 0f : SeAcerca ? DistanciaAZonaM / VelocidadDeAcercamientoMS : float.PositiveInfinity;

        /// <summary>Segundos hasta salir de la zona por cualquiera de sus bordes (infinito si está quieta o fuera de la zona).</summary>
        public float SegundosHastaSalirZona
        {
            get
            {
                if (!EnZona || Mathf.Abs(VelocidadMS) <= Quieta) return float.PositiveInfinity;
                float borde = VelocidadMS > 0f ? L + a.semiZonaM : L - a.semiZonaM;
                return Mathf.Abs(borde - PosicionM) / Mathf.Abs(VelocidadMS);
            }
        }

        /// <summary>Distancia (m) que recorre la nave frenando de la velocidad actual a la de captura con la aceleración dada.</summary>
        public float DistanciaFrenadoM(float acel)
        {
            float v = VelocidadReferenciaCapturaMS, vc = a.velocidadCapturaMS;
            if (v <= vc || acel <= 0f) return 0f;
            // Con gravedad: se frena contra la atracción del borde de la zona (estimación prudente).
            float g = GravedadActiva ? Mathf.Abs(AceleracionGravedadEn(L - Mathf.Sign(DistanciaAlDestinoM) * a.semiZonaM)) : 0f;
            float neta = acel - g;
            if (neta <= 0.05f) return float.PositiveInfinity;   // el motor no puede con la gravedad: avisar siempre
            return (v * v - vc * vc) / (2f * neta);
        }

        /// <summary>
        /// Distancia (m) que se necesita antes del borde de la zona para entrar a velocidad de captura:
        /// el giro (si la proa no está ya preparada para frenar) más la distancia de frenado.
        /// </summary>
        public float DistanciaNecesariaParaEntrarM(float acel)
        {
            if (!SeAcerca || VelocidadDeCaptura) return 0f;
            float giro = MotorFrena ? 0f : (Girando ? SegundosGiroRestantes : a.duracionGiroS) * Mathf.Abs(VelocidadMS);
            return giro + DistanciaFrenadoM(acel);
        }

        /// <summary>
        /// Cambio de velocidad (m/s) que el motor principal tiene que dar, como mínimo, para quedar dentro de la zona a
        /// velocidad de captura. Regla de esta versión (sin gravedad): si se acerca, frenar hasta la velocidad de captura;
        /// si se aleja, frenar hasta 0 y volver a «velocidadRegresoMinimaMS»; si está quieta fuera, arrancar a esa velocidad.
        /// No cuenta el tiempo: llegar despacio también vale. Los giros no gastan combustible (simplificación).
        /// </summary>
        public float DeltaVNecesariaMS
        {
            get
            {
                if (GravedadActiva) return DeltaVNecesariaConGravedad();
                float v = Mathf.Abs(VelocidadMS), vc = a.velocidadCapturaMS;
                if (EnZona && v <= vc) return 0f;
                if (SeAcerca) return Mathf.Max(0f, v - vc);
                float regreso = Mathf.Min(a.velocidadRegresoMinimaMS, vc);
                if (SeAleja) return v + regreso;
                return EnZona ? 0f : regreso;   // quieta
            }
        }

        /// <summary>
        /// Con gravedad, MÍNIMO teórico por energía (puede quedarse corto, nunca se pasa, así que no aborta antes de tiempo):
        ///   · Si se aleja con energía para escapar, frenar hasta la velocidad de escape del punto actual.
        ///   · Si después la energía sigue por encima de la que permite entrar en la zona a velocidad de captura, quitar el resto
        ///     frenando donde la nave va más rápido (en el punto de paso), que es lo más barato.
        /// Si la energía ya es suficientemente baja, la gravedad trae la nave a la zona sin motor.
        /// </summary>
        float DeltaVNecesariaConGravedad()
        {
            if (EnZona && VelocidadDeCaptura) return 0f;
            float v = Mathf.Abs(VelocidadMS), vc = a.velocidadCapturaMS;
            float fi = PotencialEn(PosicionM);
            float e = 0.5f * v * v + fi;
            float eMax = 0.5f * vc * vc + PotencialBordeZona;
            float fiCentro = -Mu / DistanciaPasoM;
            float dv = 0f;
            if (SeAleja && e >= 0f)
            {
                float vEscape = Mathf.Sqrt(Mathf.Max(0f, -2f * fi));
                dv += Mathf.Max(0f, v - vEscape);
                e = 0f;
            }
            if (e > eMax)
                dv += Mathf.Sqrt(2f * (e - fiCentro)) - Mathf.Sqrt(Mathf.Max(0f, 2f * (eMax - fiCentro)));
            return dv;
        }

        // ------------------------------------------------------------------ Acciones

        /// <summary>Empieza un giro de 180°. Devuelve false si ya estaba girando.</summary>
        public bool IniciarGiro()
        {
            if (Girando) return false;
            Girando = true;
            TiempoGiroS = 0f;
            orientacionInicioGiro = Orientacion;
            return true;
        }

        /// <summary>
        /// Avanza dt segundos. motor = Espacio mantenido y permitido. Devuelve la fracción del paso con el motor encendido
        /// (0..1) para cobrar el combustible. Si el motor lleva la velocidad a 0, la deja en 0 y se corta (CortePorCero).
        /// </summary>
        public float Paso(float dt, bool motor, float acel)
        {
            CortePorCero = false;
            FraccionMotor = 0f;
            if (dt <= 0f) return 0f;

            if (Girando)
            {
                TiempoGiroS += dt;
                if (TiempoGiroS >= a.duracionGiroS)
                {
                    Girando = false;
                    Orientacion = -orientacionInicioGiro;
                    orientacionInicioGiro = Orientacion;
                }
                motor = false;                         // bloqueado durante el giro
            }

            float v0 = VelocidadMS;
            AceleracionMotorMS2 = 0f;
            if (motor && acel > 0f)
            {
                float dv = Orientacion * acel * dt;
                float v1 = v0 + dv;
                bool frenando = Mathf.Abs(v0) > 0f && Mathf.Sign(v0) != Orientacion;
                if (frenando && Mathf.Sign(v1) != Mathf.Sign(v0))
                {
                    // Cruza el cero: se queda en 0 y el motor se apaga (sin giros ni inversiones automáticas).
                    FraccionMotor = Mathf.Clamp01(Mathf.Abs(v0) / (acel * dt));
                    float tMotor = FraccionMotor * dt;
                    PosicionM += v0 * 0.5f * tMotor;   // frenando hasta 0
                    VelocidadMS = 0f;
                    CortePorCero = true;
                }
                else
                {
                    FraccionMotor = 1f;
                    VelocidadMS = v1;
                    PosicionM += (v0 + v1) * 0.5f * dt;
                }
            }
            else PosicionM += v0 * dt;
            AceleracionMotorMS2 = Orientacion * acel * FraccionMotor;
            DeltaVMotorTotalMS += acel * FraccionMotor * dt;

            // Gravedad experimental: se suma aparte (el corte del motor en velocidad 0 no la detiene).
            if (GravedadActiva)
            {
                float dvg = AceleracionGravedadEn(PosicionM) * dt;
                VelocidadMS += dvg;
                PosicionM += 0.5f * dvg * dt;
                DeltaVGravedadTotalMS += Mathf.Abs(dvg);
            }

            VelocidadMaximaMS = Mathf.Max(VelocidadMaximaMS, Mathf.Abs(VelocidadMS));
            ProgresoMaximo = Mathf.Max(ProgresoMaximo, Mathf.Clamp01(PosicionM / L));
            return FraccionMotor;
        }

        /// <summary>Combustible (kg) para dar un cambio de velocidad dv con la masa actual y la velocidad de escape del motor (Tsiolkovsky).</summary>
        public static float CombustibleParaDeltaV(float dv, float masaKg, float empujeN, float consumoKgS)
        {
            if (dv <= 0f || masaKg <= 0f) return 0f;
            if (consumoKgS <= 0f || empujeN <= 0f) return float.PositiveInfinity;
            float ve = empujeN / consumoKgS;
            return masaKg * (1f - Mathf.Exp(-dv / ve));
        }
    }
}
