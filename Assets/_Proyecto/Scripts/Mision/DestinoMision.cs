using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Definición reutilizable de un destino (SOLO DATOS, sin dibujo). La lista vive en CatalogoMision → Destinos y el
    /// jugador elige uno en la pantalla de selección de misión. ControlNave lo lee al empezar el viaje.
    ///
    /// Qué cambia cada destino:
    ///   · Nombre y descripción, y una representación provisional (color y tamaño del cuerpo que se ve por la ventana).
    ///   · Con la navegación longitudinal activada: longitud de la ruta, zona de aproximación y velocidad de captura.
    ///   · Con la gravedad experimental activada: intensidad y distancia de paso de la atracción simplificada.
    /// Sin navegación, el viaje clásico solo cambia el nombre y el aspecto (la diferencia jugable necesita la navegación).
    /// Valores de DEMOSTRACIÓN: cuerpos ficticios, distancias de juego comprimidas.
    /// </summary>
    [Serializable]
    public class DefinicionDestino
    {
        [Header("Identidad")]
        public string nombre = "Destino";
        [Tooltip("Qué tiene de distinto este destino al jugarlo. Se muestra en la selección de misión.")]
        [TextArea(2, 4)] public string diferenciaJugable = "";

        [Header("Representación provisional (solo dibujo)")]
        [Tooltip("Tinte del cuerpo de destino en la ventana (multiplica el material actual).")]
        public Color colorVisual = Color.white;
        [Tooltip("Tamaño del cuerpo respecto al modelo actual (1 = igual). Máximo recomendado 1,4 para que la ruta pase a su lado.")]
        [Range(0.5f, 1.4f)] public float escalaVisual = 1f;

        [Header("Navegación longitudinal (si está activada)")]
        [Tooltip("Longitud de la ruta hasta el centro de la zona de aproximación (m), tiempos normales. Ruta de juego comprimida.")]
        public float distanciaRutaM = 8000f;
        [Tooltip("Longitud de la ruta en Modo Prueba (m).")]
        public float distanciaRutaPruebaM = 2500f;

        [Header("Condiciones de llegada (si la navegación está activada)")]
        [Tooltip("Media longitud de la zona de aproximación (m).")]
        public float semiZonaM = 250f;
        [Tooltip("Velocidad máxima para autorizar la inserción dentro de la zona (m/s).")]
        public float velocidadCapturaMS = 15f;

        [Header("Gravedad experimental (si está activada) — UN EJE")]
        [Tooltip("Distancia mínima entre la ruta y el centro del cuerpo (m). La ruta pasa a su lado: la nave nunca se acerca más.")]
        public float distanciaPasoM = 150f;
        [Tooltip("Velocidad que ganaría la nave cayendo desde muy lejos hasta el punto de paso, sin motor (m/s). Mide la intensidad.")]
        public float velocidadCaidaMS = 18f;
        [Tooltip("Radio de seguridad del cuerpo (m): superficie + margen. La distancia de paso nunca puede ser menor.")]
        public float radioSeguridadM = 100f;

        /// <summary>Distancia de paso efectiva: nunca por debajo del radio de seguridad (si se configura menor, se corrige).</summary>
        public float DistanciaPasoEfectivaM => Mathf.Max(distanciaPasoM, radioSeguridadM, 1f);

        /// <summary>Parámetro gravitatorio μ = G·M (m³/s²) deducido de la velocidad de caída: v² = 2μ / b.</summary>
        public float Mu => 0.5f * velocidadCaidaMS * velocidadCaidaMS * DistanciaPasoEfectivaM;

        public bool PasoCorregido => distanciaPasoM < radioSeguridadM;

        /// <summary>Destino actual (el de siempre) y un segundo destino de prueba con reglas de llegada distintas.</summary>
        public static List<DefinicionDestino> PorDefecto() => new List<DefinicionDestino>
        {
            new DefinicionDestino
            {
                nombre = "Luna helada (ficticia)",
                diferenciaJugable = "El destino de siempre. Ruta de 8 km, zona de aproximación amplia (±250 m) y captura a 15 m/s o menos. " +
                                    "Gravedad experimental suave.",
                colorVisual = Color.white, escalaVisual = 1f,
                distanciaRutaM = 8000f, distanciaRutaPruebaM = 2500f,
                semiZonaM = 250f, velocidadCapturaMS = 15f,
                distanciaPasoM = 150f, velocidadCaidaMS = 18f, radioSeguridadM = 100f
            },
            new DefinicionDestino
            {
                nombre = "Planetoide denso (ficticio)",
                diferenciaJugable = "Más lejos (11 km) y con una llegada más exigente: zona estrecha (±150 m) y captura a 8 m/s o menos, " +
                                    "así que hay que frenar antes y con más precisión. Con la gravedad experimental, atrae con más fuerza.",
                colorVisual = new Color(0.95f, 0.62f, 0.45f), escalaVisual = 1.3f,
                distanciaRutaM = 11000f, distanciaRutaPruebaM = 3200f,
                semiZonaM = 150f, velocidadCapturaMS = 8f,
                distanciaPasoM = 220f, velocidadCaidaMS = 28f, radioSeguridadM = 160f
            }
        };
    }
}
