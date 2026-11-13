using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Marca la raíz de una muestra visual (VISUAL_MUESTRA_…) y guarda lo que la muestra cambió
    /// fuera de su propia jerarquía, para poder restaurarlo al quitarla:
    /// - dibujados (Renderer) de las formas provisionales que se ocultaron (no se borran);
    /// - luz ambiental de la escena;
    /// - posprocesado de la cámara.
    /// No tiene lógica de juego. Lo usan los menús de Editor/MuestraVisual.cs.
    /// </summary>
    public class CapaVisual : MonoBehaviour
    {
        [Tooltip("Formas provisionales cuyo dibujado se apagó (sus colisionadores siguen activos).")]
        public List<Renderer> ocultados = new List<Renderer>();

        [Header("Valores originales (para restaurar)")]
        public bool ambienteGuardado;
        public UnityEngine.Rendering.AmbientMode modoAmbienteOriginal;
        public Color ambienteOriginal;

        public Camera camara;
        public bool postprocesadoGuardado;
        public bool postprocesadoOriginal;
        [Tooltip("True si la cámara no tenía datos de URP y la muestra los añadió.")]
        public bool datosCamaraAnadidos;
    }
}
