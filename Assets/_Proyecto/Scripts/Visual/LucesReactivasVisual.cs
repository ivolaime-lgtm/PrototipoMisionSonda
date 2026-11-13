using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Tiras luminosas decorativas que siguen el color y la intensidad de una luz de la cabina.
    /// Así, cuando ControlNave pone las luces en rojo (avería) o ámbar (escombros), las tiras
    /// también lo muestran y la alerta se ve más, no menos. Solo LEE la luz; no la cambia.
    /// Usa MaterialPropertyBlock: no crea copias de materiales.
    /// </summary>
    public class LucesReactivasVisual : MonoBehaviour
    {
        [SerializeField] Light luzReferencia;
        [SerializeField] Renderer[] tiras;
        [Tooltip("Brillo de las tiras con la luz de la cabina en su intensidad normal.")]
        [SerializeField] float brillo = 2.2f;
        [Tooltip("Intensidad normal de la luz de referencia (la que pone ControlNave sin alertas).")]
        [SerializeField] float intensidadNormal = 2.5f;
        [Tooltip("Color base de las tiras cuando la luz de la cabina es blanca.")]
        [SerializeField] Color colorBase = new Color(0.25f, 0.85f, 1f);

        MaterialPropertyBlock bloque;
        static readonly int Emision = Shader.PropertyToID("_EmissionColor");

        void Update()
        {
            if (luzReferencia == null || tiras == null) return;
            if (bloque == null) bloque = new MaterialPropertyBlock();

            Color luz = luzReferencia.color;
            // Luz blanca/azulada → color base; luz roja o ámbar → ese color.
            float saturacion = Mathf.Max(luz.r, luz.g, luz.b) - Mathf.Min(luz.r, luz.g, luz.b);
            Color c = Color.Lerp(colorBase, luz, Mathf.Clamp01(saturacion * 2f));
            float factor = brillo * Mathf.Clamp(luzReferencia.intensity / Mathf.Max(0.01f, intensidadNormal), 0.2f, 1.5f);

            bloque.SetColor(Emision, c * factor);
            foreach (Renderer r in tiras)
                if (r != null) r.SetPropertyBlock(bloque);
        }
    }
}
