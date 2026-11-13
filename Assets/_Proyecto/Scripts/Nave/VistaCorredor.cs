using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Representación del corredor de aproximación (solo visual): un anillo de balizas delante de la nave.
    /// Se coloca con el MISMO criterio que los escombros (ExteriorNave): posición del corredor menos el
    /// desplazamiento lateral de la nave. Así, al maniobrar con WASD, el anillo se desplaza igual que se
    /// desplazaban las rocas, y queda centrado en la mira cuando la nave está en el corredor.
    /// Lee ControlNave.Corredor; no cambia nada del juego. Sustituible por un modelo.
    /// </summary>
    public class VistaCorredor : MonoBehaviour
    {
        [Tooltip("Distancia del anillo por delante del centro de la nave (m).")]
        [SerializeField] float distanciaM = 40f;
        [Tooltip("Radio visual del anillo (m). La tolerancia real está en CorredorAproximacion.")]
        [SerializeField] float radioM = 3f;
        [SerializeField] int balizas = 16;

        ControlNave control;
        ExteriorNave exterior;
        Transform anillo;
        Material mat;
        float verdeHasta = -1f;

        static readonly Color Cian = new Color(0.36f, 0.78f, 0.88f);
        static readonly Color Verde = new Color(0.42f, 0.82f, 0.54f);

        void Start()
        {
            control = GetComponent<ControlNave>();
            exterior = FindFirstObjectByType<ExteriorNave>();

            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            mat = new Material(sh);
            mat.SetColor("_BaseColor", Color.black);
            mat.color = Color.black;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Cian * 2f);

            anillo = new GameObject("CorredorAproximacion (solo visual)").transform;
            for (int i = 0; i < balizas; i++)
            {
                float a = i * Mathf.PI * 2f / balizas;
                GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(b.GetComponent<Collider>());
                b.transform.SetParent(anillo, false);
                b.transform.localPosition = new Vector3(Mathf.Cos(a) * radioM, Mathf.Sin(a) * radioM, 0f);
                b.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
                b.transform.localScale = new Vector3(0.25f, 0.6f, 0.25f);
                Renderer r = b.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            anillo.gameObject.SetActive(false);
        }

        void Update()
        {
            if (control == null || exterior == null || anillo == null) return;
            CorredorAproximacion c = control.Corredor;
            bool activa = c != null && c.Activa && c.EstadoActual == CorredorAproximacion.Estado.Activa &&
                          control.Fase == FaseNave.Crucero;

            // Al completarse, el anillo queda verde un momento como confirmación.
            if (c != null && c.Completada && verdeHasta < 0f) verdeHasta = Time.time + 1.5f;
            bool confirmacion = verdeHasta > 0f && Time.time < verdeHasta && control.Fase == FaseNave.Crucero;

            bool visible = activa || confirmacion;
            if (anillo.gameObject.activeSelf != visible) anillo.gameObject.SetActive(visible);
            if (!visible) return;

            Vector2 rel = c.Objetivo - exterior.Desplazamiento;
            anillo.position = exterior.CentroNave + new Vector3(rel.x, rel.y, distanciaM);

            bool verde = confirmacion || c.Dentro;
            float pulso = verde ? 1f : 0.75f + 0.25f * Mathf.Sin(Time.time * 5f);
            mat.SetColor("_EmissionColor", (verde ? Verde : Cian) * (2.2f * pulso));
            float esc = confirmacion ? 1f + 0.15f * Mathf.Sin(Time.time * 12f) : 1f;
            anillo.localScale = Vector3.one * esc;
        }

        void OnDestroy()
        {
            if (anillo != null) Destroy(anillo.gameObject);
            if (mat != null) Destroy(mat);
        }
    }
}
