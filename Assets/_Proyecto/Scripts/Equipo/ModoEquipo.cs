using UnityEngine;

namespace PrototipoMision
{
    /// <summary>Roles del equipo. Cada uno decide una parte de la configuración.</summary>
    public enum Rol { Ciencia = 0, Ingenieria = 1, Lanzamiento = 2, Comunicaciones = 3 }

    /// <summary>Si la configuración cumple la prioridad de un rol, y por qué.</summary>
    public struct EstadoPrioridad
    {
        public bool cumple;
        public string detalle;
    }

    /// <summary>
    /// Modo equipo en el mismo PC (1 a 4 jugadores). Guarda cuántos jugadores hay y
    /// quién es responsable de cada rol mientras se cambia de escena. Se reinicia al pulsar Play.
    ///
    /// Roles y decisiones:
    /// - Ciencia: objetivo e instrumento.  Prioridad: usar el instrumento recomendado.
    /// - Ingeniería: batería.              Prioridad: margen de energía tras escanear.
    /// - Lanzamiento: lanzador.            Prioridad: reserva de presupuesto.
    /// - Comunicaciones: órbita de relevo. Prioridad: transmisión rápida.
    /// Los umbrales están en el CatalogoMision (DEMOSTRACIÓN).
    /// </summary>
    public static class ModoEquipo
    {
        public const int MaxJugadores = 4;

        public static readonly string[] NombresRol = { "Ciencia", "Ingeniería", "Lanzamiento", "Comunicaciones" };

        public static readonly string[] PrioridadesRol =
        {
            "usar el instrumento recomendado para el objetivo",
            "dejar margen de energía después de escanear",
            "conservar una reserva de presupuesto",
            "transmitir los datos rápido"
        };

        static int[] responsable = new int[MaxJugadores];

        public static int Jugadores { get; private set; } = 1;
        public static bool EnEquipo => Jugadores > 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar()
        {
            Jugadores = 1;
            responsable = new int[MaxJugadores];
        }

        public static void EstablecerJugadores(int cantidad)
        {
            Jugadores = Mathf.Clamp(cantidad, 1, MaxJugadores);
            RepartoPorDefecto();
        }

        /// <summary>Reparto inicial de roles según el número de jugadores.</summary>
        public static void RepartoPorDefecto()
        {
            switch (Jugadores)
            {
                case 2: responsable = new[] { 0, 1, 1, 0 }; break;   // J1: Ciencia + Comunicaciones · J2: Ingeniería + Lanzamiento
                case 3: responsable = new[] { 0, 1, 1, 2 }; break;   // J2 lleva Ingeniería y Lanzamiento
                case 4: responsable = new[] { 0, 1, 2, 3 }; break;
                default: responsable = new[] { 0, 0, 0, 0 }; break;
            }
        }

        public static int Responsable(Rol rol) => Mathf.Clamp(responsable[(int)rol], 0, Jugadores - 1);

        public static void AsignarResponsable(Rol rol, int jugador)
        {
            responsable[(int)rol] = Mathf.Clamp(jugador, 0, Jugadores - 1);
        }

        public static int[] CopiaReparto()
        {
            int[] copia = new int[MaxJugadores];
            for (int i = 0; i < MaxJugadores; i++) copia[i] = Responsable((Rol)i);
            return copia;
        }

        public static string NombreJugador(int jugador) => "J" + (jugador + 1);

        public static string NombreRol(Rol rol) => NombresRol[(int)rol];

        /// <summary>Roles de un jugador, separados por comas (vacío si no tiene ninguno).</summary>
        public static string RolesDe(int jugador)
        {
            string texto = "";
            for (int i = 0; i < MaxJugadores; i++)
                if (Responsable((Rol)i) == jugador)
                    texto += (texto.Length > 0 ? ", " : "") + NombresRol[i];
            return texto;
        }

        /// <summary>Comprueba si una configuración cumple la prioridad de un rol.</summary>
        public static EstadoPrioridad Evaluar(Rol rol, ResumenMision r, CatalogoMision c)
        {
            EstadoPrioridad e = new EstadoPrioridad();
            if (r == null || c == null || r.instrumento == null) { e.detalle = "Sin datos"; return e; }

            switch (rol)
            {
                case Rol.Ciencia:
                    e.cumple = r.instrumentoRecomendado;
                    e.detalle = (e.cumple ? "Instrumento recomendado" : "Instrumento no recomendado") +
                                $": {r.valorPorEscaneo} puntos por escaneo";
                    break;

                case Rol.Ingenieria:
                    float margen = r.energiaWh - r.energiaEscaneosWh;
                    e.cumple = margen >= c.margenEnergiaMinimoWh;
                    e.detalle = $"Margen tras escanear {margen:0} Wh (objetivo ≥ {c.margenEnergiaMinimoWh:0} Wh)";
                    break;

                case Rol.Lanzamiento:
                    float reserva = r.presupuestoMUSD - r.costeTotalMUSD;
                    e.cumple = reserva >= c.reservaPresupuestoMinimaMUSD;
                    e.detalle = $"Reserva {reserva:0} M USD (objetivo ≥ {c.reservaPresupuestoMinimaMUSD:0} M USD)";
                    break;

                case Rol.Comunicaciones:
                    bool hayEnlace = !float.IsInfinity(r.segundosTransmision);
                    e.cumple = hayEnlace && r.segundosTransmision <= c.tiempoTransmisionMaximoS;
                    e.detalle = hayEnlace
                        ? $"Transmitir todo ≈ {r.segundosTransmision:0} s reales (objetivo ≤ {c.tiempoTransmisionMaximoS:0} s)"
                        : "Sin enlace";
                    break;
            }
            return e;
        }
    }
}
