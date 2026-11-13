using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    /// <summary>
    /// Pantalla inicial (interfaz provisional IMGUI) de la escena CentroOperaciones.
    /// Muestra la estación y el destino de fondo, y da paso a la configuración.
    /// No modifica ninguna mecánica de la misión.
    /// </summary>
    public class CentroOperaciones : MonoBehaviour
    {
        [SerializeField] string escenaConfiguracion = "Configuracion";

        [Tooltip("Archivo de créditos que se muestra en el panel CRÉDITOS.")]
        [SerializeField] TextAsset creditos;

        [Header("Textos editables")]
        [SerializeField] string titulo = "CENTRO DE OPERACIONES";
        [TextArea(2, 4)]
        [SerializeField] string subtitulo =
            "Misión de DEMOSTRACIÓN a una luna helada ficticia con un océano bajo el hielo.";
        [TextArea(3, 6)]
        [SerializeField] string leyenda =
            "Estación: geometría propia del equipo.\n" +
            "Satélite de relevo: modelo TDRS de NASA, solo como ilustración.\n" +
            "Luna: textura de Europa (USGS, JPL y Caltech) usada como referencia visual.";
        [TextArea(3, 6)]
        [SerializeField] string avisoFiccion =
            "Escenario ficticio: no representa una misión real a Europa ni a ningún otro cuerpo, " +
            "y no está respaldado por NASA.";

        bool verCreditos;
        Vector2 desplazamiento;
        GUIStyle sTitulo, sTexto, sPequeno, sBoton, sCreditos;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnGUI()
        {
            PrepararEstilos();

            float escala = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            float ancho = Screen.width / escala;

            bool configurar = false;
            bool salir = false;

            // Panel principal
            Rect panel = new Rect(40f, 40f, 620f, 470f);
            Rellenar(panel, new Color(0f, 0.04f, 0.07f, 0.72f));
            GUILayout.BeginArea(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, panel.height - 40f));
            GUILayout.Label(titulo, sTitulo);
            GUILayout.Label(subtitulo, sTexto);
            GUILayout.Space(18);
            if (GUILayout.Button("CONFIGURAR MISIÓN", sBoton, GUILayout.Height(64))) configurar = true;
            GUILayout.Space(8);
            if (GUILayout.Button(verCreditos ? "OCULTAR CRÉDITOS" : "CRÉDITOS", sBoton, GUILayout.Height(48))) verCreditos = !verCreditos;
            GUILayout.Space(8);
            if (GUILayout.Button("SALIR", sBoton, GUILayout.Height(48))) salir = true;
            GUILayout.EndArea();

            // Leyenda y aviso
            Rect pie = new Rect(40f, 1080f - 190f, Mathf.Min(900f, ancho - 80f), 150f);
            Rellenar(pie, new Color(0f, 0.04f, 0.07f, 0.6f));
            GUI.Label(new Rect(pie.x + 16f, pie.y + 10f, pie.width - 32f, 80f), leyenda, sPequeno);
            GUI.Label(new Rect(pie.x + 16f, pie.y + 90f, pie.width - 32f, 56f), avisoFiccion, sPequeno);

            // Créditos
            if (verCreditos)
            {
                float w = Mathf.Min(860f, ancho - 740f);
                if (w < 420f) w = ancho - 80f;
                Rect r = new Rect(ancho - w - 40f, 40f, w, 1080f - 260f);
                Rellenar(r, new Color(0.01f, 0.05f, 0.08f, 0.92f));
                GUILayout.BeginArea(new Rect(r.x + 16f, r.y + 12f, r.width - 32f, r.height - 24f));
                desplazamiento = GUILayout.BeginScrollView(desplazamiento);
                GUILayout.Label(creditos != null ? creditos.text : "Falta asignar el archivo CREDITOS en CentroOperaciones.", sCreditos);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }

            GUI.matrix = Matrix4x4.identity;

            if (configurar) SceneManager.LoadScene(escenaConfiguracion);
            if (salir)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
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
            if (sTitulo != null) return;

            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, wordWrap = true };
            sTitulo.normal.textColor = new Color(0.75f, 0.95f, 1f);

            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
            sTexto.normal.textColor = Color.white;

            sPequeno = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            sPequeno.normal.textColor = new Color(0.8f, 0.9f, 0.95f);

            sBoton = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };

            sCreditos = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            sCreditos.normal.textColor = new Color(0.88f, 0.94f, 0.97f);
        }
    }
}
