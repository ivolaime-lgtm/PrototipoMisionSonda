using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Ficha informativa que se muestra al escanear un punto científico.
    /// Se crea y edita como asset: clic derecho en Project →
    /// Create → Prototipo → Ficha científica.
    /// </summary>
    [CreateAssetMenu(fileName = "Ficha", menuName = "Prototipo/Ficha científica")]
    public class FichaCientifica : ScriptableObject
    {
        [Tooltip("Título que aparece al apuntar y en la ficha.")]
        public string titulo = "Punto de interés";

        [TextArea(4, 12)]
        [Tooltip("Texto de la ficha. Si usa información real, indica la fuente abajo.")]
        public string texto = "";

        [Tooltip("Imagen opcional. Usa solo imágenes con licencia identificada y anótala en Fuente.")]
        public Texture2D imagen;

        [Tooltip("Marca la ficha como DEMOSTRACIÓN (texto inventado, sin datos científicos reales).")]
        public bool esDemostracion = true;

        [Tooltip("Fuente del texto o de la imagen si proceden de un recurso real (p. ej. una página de NASA).")]
        public string fuente = "";
    }
}
