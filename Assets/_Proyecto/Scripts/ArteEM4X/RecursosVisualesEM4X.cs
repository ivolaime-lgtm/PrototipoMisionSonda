using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>Qué pieza provisional sustituye un modelo.</summary>
    public enum TipoSustitucion
    {
        /// <summary>Casco completo de un módulo (caras, tapas, marcos de escotilla, asideros). Pivote: centro de la cara de entrada.</summary>
        CascoModulo,
        /// <summary>Un equipo interior por su nombre exacto (consola, bastidor, asiento…). Pivote: centro de su caja.</summary>
        Equipo,
        /// <summary>Todas las hojas de puerta (o solo las de una unión). Pivote: centro de la hoja. X = deslizamiento, Y = alto, Z = grosor.</summary>
        HojaPuerta,
        /// <summary>Todos los paneles de puerta (o solo los de una unión). Pivote: centro. +Z = cara que mira al jugador.</summary>
        PanelPuerta,
        /// <summary>Utilería provisional del MVP por nombre exacto: «Kit», «Baliza de reparación», «Marca de puesto».</summary>
        Utileria
    }

    public enum AjusteSustitucion
    {
        /// <summary>El modelo se coloca a su escala real (1 unidad = 1 m). Lo normal para casco y equipos.</summary>
        TalCual,
        /// <summary>El modelo se estira para ocupar exactamente la caja provisional (útil para hojas de distinto ancho).</summary>
        EscalarALaCaja
    }

    [Serializable]
    public class EntradaVisual
    {
        public bool activa = true;
        public TipoSustitucion tipo;
        [Tooltip("Id del módulo (NC, MA, CI, CO, IN, HA, BO, N2, SI, ES, PR, LZ). Para Casco y Equipo.")]
        public string modulo = "CI";
        [Tooltip("Equipo o utilería: nombre EXACTO de la pieza provisional. Hoja o panel: id de la unión (ais_ciencia…) o vacío = todas.")]
        public string nombre = "";
        [Tooltip("Prefab o modelo importado. Si falta, se queda la geometría provisional.")]
        public GameObject prefab;
        public AjusteSustitucion ajuste = AjusteSustitucion.TalCual;
        [TextArea(1, 3)] public string notas = "";

        public string Clave =>
            tipo == TipoSustitucion.CascoModulo ? "casco/" + modulo
            : tipo == TipoSustitucion.Equipo ? "equipo/" + modulo + "/" + nombre
            : tipo == TipoSustitucion.HojaPuerta ? "hoja/" + (string.IsNullOrEmpty(nombre) ? "todas" : nombre)
            : tipo == TipoSustitucion.PanelPuerta ? "panel/" + (string.IsNullOrEmpty(nombre) ? "todos" : nombre)
            : "utileria/" + nombre;
    }

    /// <summary>
    /// TABLA DE SUSTITUCIÓN VISUAL de la EM-4X (solo datos). La edita SOLO el integrador.
    ///
    /// Cada entrada dice qué pieza provisional se sustituye por qué modelo. No cambia medidas, colisiones, puertas,
    /// puntos de interacción, cámaras ni balance: el modelo se añade como VISUAL y la pieza provisional deja de dibujarse
    /// pero conserva su colisión. Si una entrada no tiene modelo, o está desactivada, se ve la geometría provisional.
    /// Los modelos viven en Assets/_Arte; recrear una escena no los toca (la nave se genera al pulsar Play).
    /// </summary>
    [CreateAssetMenu(fileName = "RecursosVisualesEM4X", menuName = "Prototipo/Recursos visuales EM-4X")]
    public class RecursosVisualesEM4X : ScriptableObject
    {
        [Tooltip("Interruptor general: desactivado = todo provisional (para comparar).")]
        public bool usarModelos = true;
        public List<EntradaVisual> entradas = new List<EntradaVisual>
        {
            new EntradaVisual { tipo = TipoSustitucion.CascoModulo, modulo = "CI", notas = "PRUEBA 1: casco del módulo de Ciencia (EM4X_CI_Casco)." },
            new EntradaVisual { tipo = TipoSustitucion.Equipo, modulo = "CI", nombre = "Consola de Ciencia", notas = "PRUEBA 1: caja X 0,7 × Y 1,2 × Z 2,0 m en ejes del módulo; la cara útil mira a −X (hacia el pasillo)." },
            new EntradaVisual { tipo = TipoSustitucion.HojaPuerta, nombre = "ais_ciencia", ajuste = AjusteSustitucion.EscalarALaCaja, notas = "PRUEBA 1: hoja telescópica de Ciencia. Modelo: X = deslizamiento, Y = alto, Z = grosor (+Z hacia el interior)." },
            new EntradaVisual { tipo = TipoSustitucion.PanelPuerta, nombre = "ais_ciencia", notas = "PRUEBA 1: panel 0,25 × 0,25 × 0,05 m; +Z = cara hacia el jugador." }
        };
    }
}
