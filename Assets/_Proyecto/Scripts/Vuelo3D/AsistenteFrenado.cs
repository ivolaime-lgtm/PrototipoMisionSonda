using System;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Asistencia de FRENADO del desplazamiento (SOLO LÓGICA, opcional). No pilota hacia ningún sitio: solo intenta dejar
    /// la nave quieta. Genera los mismos mandos que daría el jugador (−1..1 por eje), así que respeta los límites de fuerza,
    /// de giro y el consumo de EstadoVuelo3D; nada cambia de golpe.
    ///
    ///   1. Si va rápida: gira la nave PROGRESIVAMENTE hasta que la proa apunte contra el movimiento (solo alinea la proa;
    ///      no endereza el alabeo respecto a ningún «arriba») y frena con el motor principal.
    ///   2. Por debajo de «umbralMotorMS»: termina con los propulsores de traslación, sin girar, empujando contra la
    ///      velocidad de forma proporcional (cada vez más suave): no oscila ni se pasa.
    ///   3. Por debajo de «velocidadFinalMS»: corta el empuje, frena el giro que quede y se desactiva sola.
    /// Se cancela con la misma tecla o con cualquier mando manual. Valores PROVISIONALES de DEMOSTRACIÓN.
    ///
    /// REFERENCIA EXPLÍCITA: frena la velocidad RELATIVA a EstadoVuelo3D.VelocidadReferencia (cero en VueloPrueba; el objetivo
    /// seleccionado en el vuelo con gravedad). No intenta parar la nave «respecto al mundo» ni anula la gravedad: al terminar
    /// se desactiva y la nave sigue su trayectoria.
    /// </summary>
    public class AsistenteFrenado
    {
        [Serializable]
        public struct Ajustes
        {
            [Tooltip("Por encima de esta velocidad (m/s) frena con el motor principal; por debajo, solo con traslación.")]
            public float umbralMotorMS;
            [Tooltip("Velocidad (m/s) a la que se considera detenida y deja de empujar.")]
            public float velocidadFinalMS;
            [Tooltip("Velocidad de giro (°/s) que se admite al terminar.")]
            public float giroFinalGradS;
            [Tooltip("Desvío máximo (°) entre la proa y la dirección de frenado para encender el motor.")]
            public float alineacionGrados;
            [Tooltip("Velocidad de giro máxima (°/s) que usa la asistencia al orientar (nunca más que el límite de la nave).")]
            public float velocidadGiroMaximaGradS;
            [Tooltip("Suavidad del frenado con traslación (s): más alto, más suave al final.")]
            public float constanteTraslacionS;
            [Tooltip("Suavidad del control de giro (s).")]
            public float constanteGiroS;
            [Tooltip("Tiempo máximo (s); si no ha terminado, se desactiva y lo explica.")]
            public float tiempoMaximoS;

            public static Ajustes PorDefecto => new Ajustes
            {
                umbralMotorMS = 2f,
                velocidadFinalMS = 0.05f,
                giroFinalGradS = 0.3f,
                alineacionGrados = 3f,
                velocidadGiroMaximaGradS = 20f,
                constanteTraslacionS = 0.6f,
                constanteGiroS = 0.3f,
                tiempoMaximoS = 240f
            };
        }

        public enum Fase { Inactiva, Orientando, Motor, Traslacion, Estabilizando }

        readonly Ajustes a;
        public AsistenteFrenado(Ajustes ajustes) { a = ajustes; }

        public Ajustes Configuracion => a;
        public Fase FaseActual { get; private set; } = Fase.Inactiva;
        public bool Activa => FaseActual != Fase.Inactiva;
        /// <summary>Combustible gastado desde que se activó (kg). Ya está incluido en motor, traslación y orientación.</summary>
        public float GastoKg { get; set; }
        public float TiempoS { get; private set; }
        /// <summary>Por qué terminó la última vez (para el aviso).</summary>
        public string Motivo { get; private set; } = "";
        /// <summary>Ángulo actual (°) entre la proa y la dirección de frenado.</summary>
        public float DesvioGrados { get; private set; }

        public void Activar()
        {
            FaseActual = Fase.Orientando;
            GastoKg = 0f;
            TiempoS = 0f;
            Motivo = "";
        }

        public void Cancelar(string motivo)
        {
            if (!Activa) return;
            FaseActual = Fase.Inactiva;
            Motivo = motivo;
        }

        public static string Nombre(Fase f)
        {
            switch (f)
            {
                case Fase.Orientando: return "ORIENTANDO la proa contra el movimiento";
                case Fase.Motor: return "FRENANDO con el motor principal";
                case Fase.Traslacion: return "FRENANDO con traslación";
                case Fase.Estabilizando: return "PARANDO el giro";
                default: return "inactiva";
            }
        }

        /// <summary>Mandos de este fotograma. Si termina, pasa a Inactiva y deja el motivo.</summary>
        public EntradaVuelo Calcular(EstadoVuelo3D e, float dt)
        {
            EntradaVuelo s = default;
            if (!Activa) return s;
            TiempoS += dt;
            float v = e.RapidezRelativa;

            if (e.Combustible <= 0f)
            {
                Cancelar($"Asistencia detenida: sin combustible. Velocidad restante {v:0.00} m/s.");
                return s;
            }
            if (TiempoS > a.tiempoMaximoS)
            {
                Cancelar($"Asistencia detenida tras {a.tiempoMaximoS:0} s sin terminar. Velocidad restante {v:0.00} m/s.");
                return s;
            }

            if (v <= a.velocidadFinalMS)
            {
                // Ya no empuja: solo frena el giro que quede y termina.
                if (e.VelocidadAngularGradS.magnitude <= a.giroFinalGradS)
                {
                    Cancelar($"Frenado asistido terminado: {v:0.00} m/s. Gastó {GastoKg:0.0} kg.");
                    return s;
                }
                FaseActual = Fase.Estabilizando;
                s.giro = Seguir(e, Vector3.zero);
                return s;
            }

            bool usarMotor = FaseActual != Fase.Traslacion && v > a.umbralMotorMS && e.Datos.empujePrincipalN > 0f;
            if (usarMotor)
            {
                Vector3 frenadoMundo = -e.VelocidadRelativa.AVector3().normalized;
                Vector3 frenadoNave = Quaternion.Inverse(e.Orientacion) * frenadoMundo;
                DesvioGrados = Vector3.Angle(Vector3.forward, frenadoNave);

                // Giro progresivo hacia la dirección de frenado: velocidad de giro deseada con margen para frenar el giro.
                Quaternion.FromToRotation(Vector3.forward, frenadoNave).ToAngleAxis(out float angulo, out Vector3 eje);
                Vector3 alfa = e.Maniobra.aceleracionAngularGradS2 * Mathf.Deg2Rad;
                float alfaMin = Mathf.Max(0.001f, Mathf.Min(alfa.x, alfa.y));
                float limite = e.Maniobra.velocidadAngularMaximaGradS > 0f
                    ? Mathf.Min(a.velocidadGiroMaximaGradS, e.Maniobra.velocidadAngularMaximaGradS) : a.velocidadGiroMaximaGradS;
                float wMax = limite * Mathf.Deg2Rad;
                float rad = angulo * Mathf.Deg2Rad;
                float wDeseada = Mathf.Min(Mathf.Sqrt(2f * alfaMin * rad * 0.7f), wMax);
                Vector3 objetivo = rad > 1e-4f ? eje.normalized * wDeseada : Vector3.zero;
                s.giro = Seguir(e, objetivo);

                // Encender con desvío pequeño; seguir encendido con un poco más (evita encender y apagar sin parar).
                float tolerancia = FaseActual == Fase.Motor ? a.alineacionGrados * 2f : a.alineacionGrados;
                if (DesvioGrados <= tolerancia)
                {
                    FaseActual = Fase.Motor;
                    s.motorPrincipal = true;
                }
                else FaseActual = Fase.Orientando;
                return s;
            }

            // Velocidad baja: propulsores de traslación contra la velocidad, proporcional (decae sin pasarse).
            FaseActual = Fase.Traslacion;
            DesvioGrados = 0f;
            Vector3 vn = e.VelocidadRelativaEnNave;
            float aT = Mathf.Max(1e-4f, e.AceleracionTraslacionMS2);
            float k = 1f / (aT * Mathf.Max(0.05f, a.constanteTraslacionS));
            s.traslacion = new Vector3(Mathf.Clamp(-vn.x * k, -1f, 1f), Mathf.Clamp(-vn.y * k, -1f, 1f), Mathf.Clamp(-vn.z * k, -1f, 1f));
            s.giro = Seguir(e, Vector3.zero);   // mantiene la orientación quieta mientras frena
            return s;
        }

        /// <summary>Mando de giro por eje para acercar la velocidad angular a la deseada (rad/s, ejes de la nave).</summary>
        Vector3 Seguir(EstadoVuelo3D e, Vector3 deseada)
        {
            Vector3 alfa = e.Maniobra.aceleracionAngularGradS2 * Mathf.Deg2Rad;
            Vector3 w = e.VelocidadAngular;
            Vector3 u = Vector3.zero;
            float tau = Mathf.Max(0.05f, a.constanteGiroS);
            for (int i = 0; i < 3; i++)
                if (alfa[i] > 0f) u[i] = Mathf.Clamp((deseada[i] - w[i]) / (alfa[i] * tau), -1f, 1f);
            return u;
        }

        // ------------------------------------------------------------------ Estimaciones (para el HUD)

        public struct Estimacion
        {
            public float giroS, tiempoS, distanciaM, combustibleKg;
        }

        /// <summary>
        /// Frenar con el motor principal: girar la proa contra el movimiento (desde parado, a la aceleración angular máxima)
        /// y después aceleración constante. No cuenta el giro que ya lleve la nave ni las correcciones finales.
        /// </summary>
        public static Estimacion ConMotor(EstadoVuelo3D e)
        {
            Estimacion r = default;
            float v = e.RapidezRelativa;
            if (v <= 0.01f) return r;
            float angulo = Vector3.Angle(e.Proa, -e.VelocidadRelativa.AVector3()) * Mathf.Deg2Rad;
            Vector3 alfa = e.Maniobra.aceleracionAngularGradS2 * Mathf.Deg2Rad;
            float al = Mathf.Max(0.001f, Mathf.Min(alfa.x, alfa.y));
            float wMax = (e.Maniobra.velocidadAngularMaximaGradS > 0f ? e.Maniobra.velocidadAngularMaximaGradS : 1000f) * Mathf.Deg2Rad;
            r.giroS = angulo < wMax * wMax / al ? 2f * Mathf.Sqrt(angulo / al) : angulo / wMax + wMax / al;
            float am = Mathf.Max(0.001f, e.AceleracionMotorMS2);
            r.tiempoS = r.giroS + v / am;
            r.distanciaM = v * r.giroS + v * v / (2f * am);
            r.combustibleKg = EstadoCombustible(e, v) + e.Maniobra.consumoOrientacionKgS * r.giroS;
            return r;
        }

        static float EstadoCombustible(EstadoVuelo3D e, float dv)
        {
            DatosCatalogoVuelo d = e.Datos;
            if (d.consumoPrincipalKgS <= 0f || d.empujePrincipalN <= 0f) return float.PositiveInfinity;
            float ve = d.empujePrincipalN / d.consumoPrincipalKgS;
            return e.MasaKg * (1f - Mathf.Exp(-dv / ve));
        }

        /// <summary>Frenar solo con traslación, sin girar: cada eje frena su parte a la vez a plena fuerza.</summary>
        public static Estimacion SoloTraslacion(EstadoVuelo3D e)
        {
            Estimacion r = default;
            Vector3 vn = e.VelocidadRelativaEnNave;
            float aT = Mathf.Max(1e-4f, e.AceleracionTraslacionMS2);
            Vector3 d = new Vector3(vn.x * vn.x, vn.y * vn.y, vn.z * vn.z) / (2f * aT);
            r.tiempoS = Mathf.Max(Mathf.Abs(vn.x), Mathf.Abs(vn.y), Mathf.Abs(vn.z)) / aT;
            r.distanciaM = d.magnitude;
            r.combustibleKg = (Mathf.Abs(vn.x) + Mathf.Abs(vn.y) + Mathf.Abs(vn.z)) / aT * e.Maniobra.consumoTraslacionKgS;
            return r;
        }
    }
}
