using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Guarda la configuración elegida mientras el juego cambia de escena.
    /// Se reinicia al entrar en Play. Si se pulsa Play directamente en
    /// Exploracion, Configurada es false y se usan las primeras opciones.
    /// </summary>
    public static class MisionActual
    {
        public static bool Configurada { get; private set; }
        public static ConfiguracionElegida Eleccion { get; private set; }

        public static void Guardar(ConfiguracionElegida eleccion)
        {
            Eleccion = eleccion;
            Configurada = true;
        }

        // Garantiza un estado limpio al pulsar Play aunque el proyecto
        // tenga desactivada la recarga de dominio (Enter Play Mode Options).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar()
        {
            Eleccion = default;
            Configurada = false;
        }
    }
}
