using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    /// <summary>
    /// Interfaz mínima provisional dibujada por código (IMGUI).
    /// Muestra energía, consumo, estado de recarga y distancia a la estación.
    /// Se sustituirá por una interfaz editable (uGUI) en un incremento posterior.
    /// Los textos se pueden cambiar desde el Inspector.
    /// </summary>
    public class HudMinimo : MonoBehaviour
    {
        [Header("Referencias (las asigna el constructor de escena)")]
        [SerializeField] SistemaEnergia energia;
        [SerializeField] Transform sonda;
        [SerializeField] Transform estacion;

        [Header("Textos editables")]
        [SerializeField] string titulo = "ENERGÍA DE LA SONDA";
        [SerializeField] string etiquetaDemo = "VALORES DE DEMOSTRACIÓN · no son datos científicos";
        [SerializeField] string textoCargando = "En la estación: recargando";
        [SerializeField] string textoFuera = "Fuera de la estación";
        [TextArea(2, 4)]
        [SerializeField] string textoUnidades = "W (vatio) = consumo en cada instante · Wh (vatio-hora) = energía guardada.\n1 s real = {0:0.#} min de misión.";
        [TextArea(1, 3)]
        // Campo renombrado (antes textoControles) para que la escena use la lista completa de teclas.
        [SerializeField] string textoTeclas =
            "WASD mover · Espacio subir · Ctrl/C bajar · Shift turbo · Ratón mirar · E escanear · F cerrar ficha · " +
            "T terminar (en la estación) · M configuración · F9 rendimiento · Esc liberar cursor";
        [SerializeField] string textoAgotada = "ENERGÍA AGOTADA\nLa sonda no puede moverse ni volver a la estación.\nPulsa R para reiniciar.";
        [SerializeField] string textoAgotadaConInforme = "ENERGÍA AGOTADA\nLa sonda no puede moverse ni volver a la estación.\nGenerando el informe de la misión…";

        // Si la escena tiene ControlMision, él decide el final de la misión (informe)
        // y la tecla R deja de reiniciar.
        ControlMision control;

        GUIStyle estiloTitulo;
        GUIStyle estiloTexto;
        GUIStyle estiloPequeno;
        GUIStyle estiloAviso;

        const float AltoReferencia = 1080f;

        void Awake()
        {
            RepararReferencias();
            control = FindAnyObjectByType<ControlMision>();
        }

        /// <summary>
        /// Si una referencia está vacía o apunta a un prefab guardado en disco
        /// (en lugar del objeto de la escena), busca el objeto real en la escena.
        /// </summary>
        void RepararReferencias()
        {
            if (energia == null || !energia.gameObject.scene.IsValid())
            {
                SistemaEnergia enEscena = FindAnyObjectByType<SistemaEnergia>();
                if (enEscena != null)
                {
                    Debug.Log("[Prototipo] HudMinimo: la referencia de energía no era de la escena; se ha corregido.", this);
                    energia = enEscena;
                }
            }

            if (energia != null && (sonda == null || !sonda.gameObject.scene.IsValid()))
                sonda = energia.transform;

            if (estacion == null || !estacion.gameObject.scene.IsValid())
            {
                EstacionSegura enEscena = FindAnyObjectByType<EstacionSegura>();
                if (enEscena != null) estacion = enEscena.transform;
            }

            if (energia == null)
                Debug.LogWarning("[Prototipo] HudMinimo: no hay ninguna sonda con SistemaEnergia en la escena.", this);
        }

        void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (control == null && energia != null && energia.Agotada && teclado != null && teclado.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnGUI()
        {
            if (energia == null) return;

            PrepararEstilos();

            // Escala la interfaz según la altura de la pantalla.
            float escala = Screen.height / AltoReferencia;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            float anchoVirtual = Screen.width / escala;

            // Panel de energía
            Rect panel = new Rect(30f, 30f, 620f, 290f);
            Rellenar(panel, new Color(0f, 0.05f, 0.08f, 0.6f));

            float x = panel.x + 20f;
            float ancho = panel.width - 40f;

            GUI.Label(new Rect(x, panel.y + 12f, ancho, 30f), titulo, estiloTitulo);

            Rect barra = new Rect(x, panel.y + 52f, ancho, 26f);
            Rellenar(barra, new Color(1f, 1f, 1f, 0.12f));
            float f = energia.Fraccion;
            Color colorBarra = f > 0.5f ? new Color(0.3f, 0.9f, 1f)
                             : f > 0.25f ? new Color(1f, 0.8f, 0.2f)
                             : new Color(1f, 0.3f, 0.25f);
            Rellenar(new Rect(barra.x, barra.y, barra.width * f, barra.height), colorBarra);

            GUI.Label(new Rect(x, panel.y + 86f, ancho, 28f),
                $"{energia.ActualWh:0.0} / {energia.CapacidadWh:0} Wh   ·   consumo {energia.ConsumoActualW:0} W",
                estiloTexto);

            string estado = energia.Cargando
                ? $"{textoCargando} (+{energia.PotenciaRecargaW:0} W)"
                : textoFuera;
            GUI.Label(new Rect(x, panel.y + 116f, ancho, 28f), estado, estiloTexto);

            string autonomia = energia.Cargando
                ? "Autonomía: recargando en la estación"
                : float.IsInfinity(energia.AutonomiaSegundosReales)
                    ? "Autonomía: —"
                    : $"Autonomía al consumo actual: {energia.AutonomiaSegundosReales:0} s reales";
            if (sonda != null && estacion != null)
            {
                float distancia = Vector3.Distance(sonda.position, estacion.position);
                autonomia += $"   ·   estación a {distancia:0} m";
            }
            GUI.Label(new Rect(x, panel.y + 146f, ancho, 28f), autonomia, estiloTexto);

            GUI.Label(new Rect(x, panel.y + 182f, ancho, 100f),
                string.Format(textoUnidades, energia.MinutosMisionPorSegundo), estiloPequeno);

            // Etiqueta de demostración y controles
            GUI.Label(new Rect(30f, panel.yMax + 8f, 700f, 24f), etiquetaDemo, estiloPequeno);
            GUI.Label(new Rect(30f, AltoReferencia - 50f, anchoVirtual - 60f, 30f), textoTeclas, estiloPequeno);

            // Aviso de energía agotada
            if (energia.Agotada)
            {
                Rect aviso = new Rect(anchoVirtual / 2f - 380f, AltoReferencia / 2f - 90f, 760f, 180f);
                Rellenar(aviso, new Color(0.25f, 0f, 0f, 0.8f));
                GUI.Label(aviso, control != null ? textoAgotadaConInforme : textoAgotada, estiloAviso);
            }

            GUI.matrix = Matrix4x4.identity;
        }

        static void Rellenar(Rect r, Color c)
        {
            Color previo = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previo;
        }

        void PrepararEstilos()
        {
            if (estiloTitulo != null) return;

            estiloTitulo = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            estiloTitulo.normal.textColor = new Color(0.75f, 0.95f, 1f);

            estiloTexto = new GUIStyle(GUI.skin.label) { fontSize = 21 };
            estiloTexto.normal.textColor = Color.white;

            estiloPequeno = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            estiloPequeno.normal.textColor = new Color(0.8f, 0.9f, 0.95f);

            estiloAviso = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            estiloAviso.normal.textColor = Color.white;
        }
    }
}
