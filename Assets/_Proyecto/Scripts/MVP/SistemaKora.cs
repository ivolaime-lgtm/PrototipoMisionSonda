using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>Operaciones con Vector3d que el vuelo con gravedad necesita (solo lógica).</summary>
    public static class Vd
    {
        public static double Punto(Vector3d a, Vector3d b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3d Cruz(Vector3d a, Vector3d b) =>
            new Vector3d(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3d Unitario(Vector3d a)
        {
            double m = a.Magnitud;
            return m > 1e-12 ? a * (1.0 / m) : Vector3d.Cero;
        }
        public static double Distancia(Vector3d a, Vector3d b) => (a - b).Magnitud;
    }

    public enum TipoCuerpo { Planeta, Luna, Base, Satelite, Cumulo, Asteroide }

    /// <summary>Cuerpo del sistema ficticio. Los que no son el planeta se mueven en órbita circular en el plano XZ.</summary>
    public class CuerpoKora
    {
        public string id, nombre;
        public TipoCuerpo tipo;
        /// <summary>Parámetro gravitatorio G·M (m³/s²). 0 = no atrae (estación, satélite, asteroides).</summary>
        public double mu;
        public float radioM;
        /// <summary>Radio de su órbita circular alrededor del planeta (m) y fase inicial (rad). 0 = quieto en el origen.</summary>
        public double radioOrbitaM, faseInicialRad;
        /// <summary>Si no es null: sigue al cuerpo «padre» con este desplazamiento fijo (asteroides del cúmulo).</summary>
        public string padre;
        public Vector3 desplazamientoM;
        public bool navegable;
        public Color color;
    }

    public class PrediccionTrayectoria
    {
        public readonly List<Vector3d> puntos = new List<Vector3d>();
        public bool impacto;
        public string cuerpoImpacto = "";
        public double segundosImpacto;
        public double altitudMinimaM = double.MaxValue;
    }

    /// <summary>
    /// SISTEMA FICTICIO «KORA» (solo datos y lógica). DEMOSTRACIÓN: escala reducida, NO son distancias ni masas reales.
    ///
    /// Unidades: metros, segundos, kilogramos. Marco inercial con el planeta quieto en el origen; plano orbital = XZ.
    /// Atracción: a = −mu·r/|r|³ hacia el centro del planeta y hacia el de la luna (depende de la distancia). No hay
    /// gravedad «vertical» global. La estación, el satélite artificial y el cúmulo de asteroides siguen órbitas circulares
    /// alrededor del planeta con la velocidad que les corresponde (v = √(mu/r)); son «sobre raíles» (no notan la luna) y no
    /// atraen a la nave. La luna desvía la nave unos 300 m por órbita respecto a la base: es un efecto real del modelo.
    ///
    /// Tratamiento de la escala: los cuerpos son pequeños (planeta de 10 km de radio) y sus mu se eligen para que la órbita
    /// de la base dure unos 22 minutos y las velocidades orbitales (≈ 140 m/s) sean comparables con lo que la EM-4X puede
    /// cambiar con su combustible. Distancias, radios y mu son coherentes ENTRE SÍ: no se han acortado distancias dejando
    /// velocidades de otra escala. Paso de simulación: 0,02 s fijo (salto de rana); la aceleración del tiempo repite pasos.
    /// </summary>
    public class SistemaKora
    {
        public const double MuPlaneta = 6.0e8;
        public const float RadioPlanetaM = 10000f;
        public const double MuLuna = 8.0e6;
        public const float RadioLunaM = 3000f;
        public const double RadioOrbitaLunaM = 200000.0;
        public const double RadioOrbitaBaseM = 30000.0;
        public const double RadioOrbitaSateliteM = 31500.0;
        public const double RadioOrbitaCumuloM = 34000.0;
        public const float BandaObservacionMinM = 24000f;
        public const float BandaObservacionMaxM = 28000f;
        public const float RadioCumuloM = 2200f;
        public const double PasoSimulacionS = 0.02;

        public readonly List<CuerpoKora> cuerpos = new List<CuerpoKora>();
        readonly Dictionary<string, CuerpoKora> porId = new Dictionary<string, CuerpoKora>();
        CuerpoKora planeta, luna;

        public static SistemaKora Crear()
        {
            SistemaKora s = new SistemaKora();
            s.Agregar(new CuerpoKora { id = "kora", nombre = "Kora (planeta)", tipo = TipoCuerpo.Planeta, mu = MuPlaneta, radioM = RadioPlanetaM,
                                       navegable = true, color = new Color(0.32f, 0.5f, 0.78f) });
            s.Agregar(new CuerpoKora { id = "nix", nombre = "Nix (satélite natural)", tipo = TipoCuerpo.Luna, mu = MuLuna, radioM = RadioLunaM,
                                       radioOrbitaM = RadioOrbitaLunaM, faseInicialRad = 140.0 * Math.PI / 180.0, navegable = true,
                                       color = new Color(0.72f, 0.7f, 0.66f) });
            s.Agregar(new CuerpoKora { id = "base", nombre = "Base orbital", tipo = TipoCuerpo.Base, radioM = 40f,
                                       radioOrbitaM = RadioOrbitaBaseM, faseInicialRad = 0.0, navegable = true, color = new Color(0.45f, 0.95f, 0.6f) });
            s.Agregar(new CuerpoKora { id = "k2", nombre = "Satélite K-2", tipo = TipoCuerpo.Satelite, radioM = 12f,
                                       radioOrbitaM = RadioOrbitaSateliteM, faseInicialRad = 12.0 * Math.PI / 180.0, navegable = true,
                                       color = new Color(1f, 0.8f, 0.3f) });
            s.Agregar(new CuerpoKora { id = "tesel", nombre = "Cúmulo Tesel (centro)", tipo = TipoCuerpo.Cumulo, radioM = 0f,
                                       radioOrbitaM = RadioOrbitaCumuloM, faseInicialRad = 25.0 * Math.PI / 180.0, navegable = true,
                                       color = new Color(0.8f, 0.6f, 1f) });
            // Asteroide objetivo y resto del cúmulo (posiciones fijas respecto al centro; sin gravedad propia).
            s.Agregar(new CuerpoKora { id = "t7", nombre = "Asteroide T-7", tipo = TipoCuerpo.Asteroide, radioM = 130f, padre = "tesel",
                                       desplazamientoM = new Vector3(350f, 60f, -200f), navegable = true, color = new Color(0.62f, 0.5f, 0.42f) });
            System.Random azar = new System.Random(7);
            int creados = 0, intentos = 0;
            List<CuerpoKora> rocas = new List<CuerpoKora>();
            while (creados < 34 && intentos < 2000)
            {
                intentos++;
                Vector3 p = new Vector3((float)(azar.NextDouble() * 2.0 - 1.0) * RadioCumuloM, (float)(azar.NextDouble() * 2.0 - 1.0) * 500f,
                                        (float)(azar.NextDouble() * 2.0 - 1.0) * RadioCumuloM);
                float radio = 30f + (float)azar.NextDouble() * 110f;
                if (new Vector2(p.x, p.z).magnitude > RadioCumuloM) continue;
                bool libre = (p - new Vector3(350f, 60f, -200f)).magnitude > 130f + radio + 450f;   // hueco alrededor de T-7
                foreach (CuerpoKora o in rocas)
                    if ((o.desplazamientoM - p).magnitude < o.radioM + radio + 150f) { libre = false; break; }
                if (!libre) continue;
                CuerpoKora roca = new CuerpoKora { id = "roca" + creados, nombre = "Asteroide " + (creados + 1), tipo = TipoCuerpo.Asteroide, radioM = radio,
                                                   padre = "tesel", desplazamientoM = p, color = new Color(0.45f, 0.42f, 0.4f) };
                rocas.Add(roca);
                s.Agregar(roca);
                creados++;
            }
            return s;
        }

        void Agregar(CuerpoKora c)
        {
            cuerpos.Add(c);
            porId[c.id] = c;
            if (c.tipo == TipoCuerpo.Planeta) planeta = c;
            if (c.tipo == TipoCuerpo.Luna) luna = c;
        }

        public CuerpoKora Cuerpo(string id)
        {
            CuerpoKora c;
            return id != null && porId.TryGetValue(id, out c) ? c : null;
        }

        public static double VelocidadCircular(double radioM) => Math.Sqrt(MuPlaneta / radioM);
        public static double PeriodoCircular(double radioM) => 2.0 * Math.PI * Math.Sqrt(radioM * radioM * radioM / MuPlaneta);

        /// <summary>Posición y velocidad de un cuerpo en el instante t (s).</summary>
        public void Estado(CuerpoKora c, double t, out Vector3d pos, out Vector3d vel)
        {
            if (c.padre != null)
            {
                Estado(porId[c.padre], t, out pos, out vel);
                pos = pos + Vector3d.De(c.desplazamientoM);
                return;
            }
            if (c.radioOrbitaM <= 0.0)
            {
                pos = Vector3d.Cero;
                vel = Vector3d.Cero;
                return;
            }
            double r = c.radioOrbitaM;
            double w = Math.Sqrt(MuPlaneta / (r * r * r));
            double a = c.faseInicialRad + w * t;
            pos = new Vector3d(r * Math.Cos(a), 0.0, r * Math.Sin(a));
            vel = new Vector3d(-r * w * Math.Sin(a), 0.0, r * w * Math.Cos(a));
        }

        public Vector3d Posicion(CuerpoKora c, double t)
        {
            Estado(c, t, out Vector3d p, out Vector3d v);
            return p;
        }

        /// <summary>Aceleración de la gravedad (m/s²) en un punto: planeta + luna, hacia sus centros.</summary>
        public Vector3d Gravedad(Vector3d p, double t)
        {
            double r = p.Magnitud;
            if (r < 1.0) r = 1.0;
            Vector3d g = p * (-MuPlaneta / (r * r * r));
            Vector3d d = p - Posicion(luna, t);
            double dl = d.Magnitud;
            if (dl < 1.0) dl = 1.0;
            return g + d * (-MuLuna / (dl * dl * dl));
        }

        /// <summary>Radio del periapsis (m) de la órbita instantánea alrededor del planeta (sin contar la luna).</summary>
        public static double Periapsis(Vector3d p, Vector3d v)
        {
            double r = p.Magnitud;
            double energia = Vd.Punto(v, v) * 0.5 - MuPlaneta / r;
            double h = Vd.Cruz(p, v).Magnitud;
            double e2 = 1.0 + 2.0 * energia * h * h / (MuPlaneta * MuPlaneta);
            double e = e2 > 0.0 ? Math.Sqrt(e2) : 0.0;
            return h * h / MuPlaneta / (1.0 + e);
        }

        /// <summary>Radio del apoapsis (m), o infinito si la trayectoria es de escape.</summary>
        public static double Apoapsis(Vector3d p, Vector3d v)
        {
            double r = p.Magnitud;
            double energia = Vd.Punto(v, v) * 0.5 - MuPlaneta / r;
            if (energia >= 0.0) return double.PositiveInfinity;
            double a = -MuPlaneta / (2.0 * energia);
            return 2.0 * a - Periapsis(p, v);
        }

        /// <summary>
        /// Trayectoria SIN MOTOR desde un estado: integra la misma gravedad con paso «paso» y guarda un punto cada «cada»
        /// pasos. Se detiene si choca con el planeta o la luna (trayectoria de impacto).
        /// </summary>
        public PrediccionTrayectoria Predecir(Vector3d p, Vector3d v, double t0, double duracionS, double paso, int cada, float margenM)
        {
            PrediccionTrayectoria r = new PrediccionTrayectoria();
            double t = t0;
            int n = 0;
            Vector3d g = Gravedad(p, t);
            r.puntos.Add(p);
            while (t - t0 < duracionS)
            {
                v = v + g * (paso * 0.5);
                p = p + v * paso;
                t += paso;
                g = Gravedad(p, t);
                v = v + g * (paso * 0.5);
                n++;
                if (n % cada == 0) r.puntos.Add(p);
                double altitud = p.Magnitud - RadioPlanetaM;
                if (altitud < r.altitudMinimaM) r.altitudMinimaM = altitud;
                if (altitud < margenM)
                {
                    r.impacto = true; r.cuerpoImpacto = planeta.nombre; r.segundosImpacto = t - t0;
                    r.puntos.Add(p);
                    break;
                }
                if (Vd.Distancia(p, Posicion(luna, t)) < RadioLunaM + margenM)
                {
                    r.impacto = true; r.cuerpoImpacto = luna.nombre; r.segundosImpacto = t - t0;
                    r.puntos.Add(p);
                    break;
                }
            }
            return r;
        }

        /// <summary>¿El planeta tapa la línea recta entre dos puntos? (enlace de comunicaciones).</summary>
        public static bool PlanetaTapa(Vector3d a, Vector3d b)
        {
            Vector3d ab = b - a;
            double largo2 = Vd.Punto(ab, ab);
            if (largo2 < 1e-6) return false;
            double k = -Vd.Punto(a, ab) / largo2;
            if (k < 0.0) k = 0.0;
            if (k > 1.0) k = 1.0;
            Vector3d cerca = a + ab * k;
            return cerca.Magnitud < RadioPlanetaM;
        }
    }
}
