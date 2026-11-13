using System;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>Vector en doble precisión para posiciones y velocidades del vuelo 3D (solo lógica).</summary>
    [Serializable]
    public struct Vector3d
    {
        public double x, y, z;

        public Vector3d(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }

        public static Vector3d Cero => new Vector3d(0.0, 0.0, 0.0);
        public static Vector3d De(Vector3 v) => new Vector3d(v.x, v.y, v.z);
        public Vector3 AVector3() => new Vector3((float)x, (float)y, (float)z);
        public double Magnitud => Math.Sqrt(x * x + y * y + z * z);

        public static Vector3d operator +(Vector3d a, Vector3d b) => new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3d operator -(Vector3d a, Vector3d b) => new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3d operator *(Vector3d a, double k) => new Vector3d(a.x * k, a.y * k, a.z * k);
    }

    /// <summary>Mandos de un fotograma. Valores −1..1 en los ejes de la NAVE (x derecha, y arriba, z proa).</summary>
    public struct EntradaVuelo
    {
        /// <summary>x = cabeceo (+ morro abajo), y = guiñada (+ a la derecha), z = alabeo (+ a la izquierda).</summary>
        public Vector3 giro;
        /// <summary>x = derecha, y = arriba, z = adelante.</summary>
        public Vector3 traslacion;
        public bool motorPrincipal;
    }

    /// <summary>
    /// Datos que vienen del CATÁLOGO (CatalogoNave + configuración elegida). No se inventan aquí:
    /// masa, combustible, empuje y consumo del motor principal.
    /// </summary>
    public struct DatosCatalogoVuelo
    {
        public float masaSecaKg;
        public float combustibleKg;
        public float empujePrincipalN;
        public float consumoPrincipalKgS;
        public string origen;

        /// <summary>Valores estándar del catálogo actual, para abrir la escena sin catálogo asignado.</summary>
        public static DatosCatalogoVuelo Estandar => new DatosCatalogoVuelo
        {
            masaSecaKg = 6950f, combustibleKg = 350f, empujePrincipalN = 26000f, consumoPrincipalKgS = 2.5f,
            origen = "valores estándar (sin catálogo asignado)"
        };
    }

    /// <summary>
    /// Propulsores de MANIOBRA (traslación y orientación): valores PROVISIONALES de DEMOSTRACIÓN.
    /// No salen del catálogo ni copian el motor principal. Se ajustan en el inspector de PruebaVuelo3D.
    /// </summary>
    [Serializable]
    public struct AjustesManiobra
    {
        [Tooltip("Empuje de los propulsores de traslación en cada sentido de cada eje (N). Provisional.")]
        public float empujeTraslacionN;
        [Tooltip("Consumo de un eje de traslación a pleno empuje (kg/s). Provisional.")]
        public float consumoTraslacionKgS;
        [Tooltip("Aceleración angular a pleno mando (grados/s²): x cabeceo, y guiñada, z alabeo. Provisional; " +
                 "simplifica par motor e inercia en un solo número por eje.")]
        public Vector3 aceleracionAngularGradS2;
        [Tooltip("Consumo de un eje de orientación a pleno mando (kg/s). La estabilización asistida también lo gasta.")]
        public float consumoOrientacionKgS;
        [Tooltip("Límite de comodidad de la velocidad de giro (grados/s). 0 = sin límite. El mando no acelera más allá; " +
                 "no frena la nave por sí solo.")]
        public float velocidadAngularMaximaGradS;
        [Tooltip("Estabilización asistida al empezar: frena el giro de los ejes que no se están mandando.")]
        public bool estabilizacionInicial;

        public static AjustesManiobra PorDefecto => new AjustesManiobra
        {
            empujeTraslacionN = 1500f,
            consumoTraslacionKgS = 0.6f,
            aceleracionAngularGradS2 = new Vector3(8f, 8f, 12f),
            consumoOrientacionKgS = 0.08f,
            velocidadAngularMaximaGradS = 45f,
            estabilizacionInicial = true
        };
    }

    /// <summary>
    /// Estado del vuelo 3D sin gravedad (SOLO LÓGICA): posición y velocidad en el marco del mundo (doble precisión),
    /// orientación y velocidad angular en el marco de la nave. La orientación y la velocidad son independientes:
    /// girar no cambia el movimiento, y nada endereza la nave (no hay un «arriba» global).
    /// </summary>
    public class EstadoVuelo3D
    {
        public Vector3d Posicion;
        public Vector3d Velocidad;
        public Quaternion Orientacion = Quaternion.identity;
        /// <summary>Velocidad angular en los ejes de la nave (rad/s): x cabeceo, y guiñada, z alabeo.</summary>
        public Vector3 VelocidadAngular;

        /// <summary>
        /// Velocidad de la REFERENCIA respecto a la que se mide la velocidad relativa (el objetivo seleccionado, una
        /// estación…). Cero = el marco de la simulación, como en VueloPrueba. Solo la usa la asistencia de frenado.
        /// </summary>
        public Vector3d VelocidadReferencia;
        public Vector3d VelocidadRelativa => Velocidad - VelocidadReferencia;
        public float RapidezRelativa => (float)VelocidadRelativa.Magnitud;
        public Vector3 VelocidadRelativaEnNave => Quaternion.Inverse(Orientacion) * VelocidadRelativa.AVector3();

        public float Combustible;
        public bool Estabilizacion;
        public float GastoMotorKg, GastoTraslacionKg, GastoOrientacionKg;

        // Actividad del último paso (la leen la vista y el HUD).
        public bool MotorEncendido { get; private set; }
        public Vector3 TraslacionActiva { get; private set; }
        public Vector3 GiroActivo { get; private set; }

        readonly DatosCatalogoVuelo datos;
        readonly AjustesManiobra aj;

        public EstadoVuelo3D(DatosCatalogoVuelo datos, AjustesManiobra ajustes)
        {
            this.datos = datos;
            aj = ajustes;
            Reiniciar();
        }

        public DatosCatalogoVuelo Datos => datos;
        public AjustesManiobra Maniobra => aj;

        public void Reiniciar()
        {
            Posicion = Vector3d.Cero;
            Velocidad = Vector3d.Cero;
            VelocidadReferencia = Vector3d.Cero;
            Orientacion = Quaternion.identity;
            VelocidadAngular = Vector3.zero;
            Combustible = datos.combustibleKg;
            Estabilizacion = aj.estabilizacionInicial;
            GastoMotorKg = GastoTraslacionKg = GastoOrientacionKg = 0f;
            MotorEncendido = false;
            TraslacionActiva = GiroActivo = Vector3.zero;
        }

        public float MasaKg => datos.masaSecaKg + Combustible;
        public Vector3 Proa => Orientacion * Vector3.forward;
        public Vector3 Arriba => Orientacion * Vector3.up;
        public Vector3 Derecha => Orientacion * Vector3.right;
        public float Rapidez => (float)Velocidad.Magnitud;
        /// <summary>Velocidad expresada en los ejes de la nave (x derecha, y arriba, z adelante).</summary>
        public Vector3 VelocidadEnNave => Quaternion.Inverse(Orientacion) * Velocidad.AVector3();
        public Vector3 VelocidadAngularGradS => VelocidadAngular * Mathf.Rad2Deg;
        /// <summary>Cambio de velocidad que aún puede dar el motor principal con todo el combustible (m/s).</summary>
        public float DeltaVMotorRestanteMS
        {
            get
            {
                if (datos.consumoPrincipalKgS <= 0f || datos.masaSecaKg <= 0f) return 0f;
                float ve = datos.empujePrincipalN / datos.consumoPrincipalKgS;
                return ve * Mathf.Log(MasaKg / datos.masaSecaKg);
            }
        }
        public float AceleracionMotorMS2 => MasaKg > 0f ? datos.empujePrincipalN / MasaKg : 0f;
        public float AceleracionTraslacionMS2 => MasaKg > 0f ? aj.empujeTraslacionN / MasaKg : 0f;

        /// <summary>Avanza dt segundos con los mandos dados.</summary>
        public void Paso(EntradaVuelo e, float dt)
        {
            MotorEncendido = false;
            TraslacionActiva = Vector3.zero;
            GiroActivo = Vector3.zero;
            if (dt <= 0f) return;

            float m = MasaKg;
            Vector3 fuerzaNave = Vector3.zero;

            // Motor principal: empuja hacia la proa con los valores del catálogo.
            if (e.motorPrincipal && Combustible > 0f && datos.empujePrincipalN > 0f && datos.consumoPrincipalKgS > 0f)
            {
                float pedido = datos.consumoPrincipalKgS * dt;
                float gasto = Mathf.Min(Combustible, pedido);
                fuerzaNave.z += datos.empujePrincipalN * (gasto / pedido);
                Combustible -= gasto;
                GastoMotorKg += gasto;
                MotorEncendido = true;
            }

            // Traslación: propulsores de maniobra (provisionales).
            Vector3 t = new Vector3(Mathf.Clamp(e.traslacion.x, -1f, 1f), Mathf.Clamp(e.traslacion.y, -1f, 1f),
                                    Mathf.Clamp(e.traslacion.z, -1f, 1f));
            float ejesT = Mathf.Abs(t.x) + Mathf.Abs(t.y) + Mathf.Abs(t.z);
            if (ejesT > 0f && Combustible > 0f && aj.consumoTraslacionKgS > 0f)
            {
                float pedido = aj.consumoTraslacionKgS * ejesT * dt;
                float gasto = Mathf.Min(Combustible, pedido);
                float f = gasto / pedido;
                fuerzaNave += t * (aj.empujeTraslacionN * f);
                Combustible -= gasto;
                GastoTraslacionKg += gasto;
                TraslacionActiva = t * f;
            }

            // Movimiento: la fuerza se aplica en los ejes de la nave; la velocidad vive en el marco del mundo.
            Vector3 aMundo = Orientacion * (fuerzaNave / Mathf.Max(1f, m));
            Vector3d v0 = Velocidad;
            Velocidad = Velocidad + Vector3d.De(aMundo) * dt;
            Posicion = Posicion + (v0 + Velocidad) * (0.5 * dt);

            // Orientación: aceleración angular por eje; nada endereza la nave por sí solo.
            Vector3 alfa = aj.aceleracionAngularGradS2 * Mathf.Deg2Rad;
            float maxW = aj.velocidadAngularMaximaGradS * Mathf.Deg2Rad;
            Vector3 w = VelocidadAngular;
            Vector3 dw = Vector3.zero;
            Vector3 fraccion = Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                float paso = alfa[i] * dt;
                if (paso <= 0f) continue;
                float u = Mathf.Clamp(e.giro[i], -1f, 1f);
                float d = 0f;
                if (Mathf.Abs(u) > 0.01f)
                {
                    bool limitado = maxW > 0f && u * w[i] > 0f && Mathf.Abs(w[i]) >= maxW;
                    if (!limitado) d = u * paso;
                }
                else if (Estabilizacion && w[i] != 0f)
                    d = -Mathf.Sign(w[i]) * Mathf.Min(Mathf.Abs(w[i]), paso);
                dw[i] = d;
                fraccion[i] = d / paso;
            }
            float uso = Mathf.Abs(fraccion.x) + Mathf.Abs(fraccion.y) + Mathf.Abs(fraccion.z);
            if (uso > 0f && aj.consumoOrientacionKgS > 0f)
            {
                float pedido = aj.consumoOrientacionKgS * uso * dt;
                float gasto = Mathf.Min(Combustible, pedido);
                float f = pedido > 0f ? gasto / pedido : 0f;
                dw *= f;
                fraccion *= f;
                Combustible -= gasto;
                GastoOrientacionKg += gasto;
            }
            w += dw;
            if (maxW > 0f)
                for (int i = 0; i < 3; i++) w[i] = Mathf.Clamp(w[i], -maxW * 1.001f, maxW * 1.001f);
            VelocidadAngular = w;
            GiroActivo = fraccion;

            float angulo = w.magnitude * dt;
            if (angulo > 0f)
                Orientacion = Quaternion.Normalize(Orientacion * Quaternion.AngleAxis(angulo * Mathf.Rad2Deg, w.normalized));
        }
    }
}
