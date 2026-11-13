using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    /// <summary>
    /// Medidor de rendimiento para la prueba 6. Oculto por defecto: F9 lo muestra u oculta.
    /// Funciona en el editor y en el ejecutable, en todas las escenas, sin añadir nada
    /// a ninguna escena (se crea solo al arrancar el juego).
    /// Muestra FPS medios del último segundo, milisegundos por fotograma y el peor fotograma.
    /// </summary>
    public class MedidorRendimiento : MonoBehaviour
    {
        static MedidorRendimiento instancia;

        bool visible;
        int fotogramas;
        float acumulado, peorTemporal;
        float fps, msMedio, msPeor;
        GUIStyle estilo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (instancia != null) return;
            GameObject go = new GameObject("MedidorRendimiento (F9)");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<MedidorRendimiento>();
        }

        void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.f9Key.wasPressedThisFrame)
                visible = !visible;

            float dt = Time.unscaledDeltaTime;
            fotogramas++;
            acumulado += dt;
            if (dt > peorTemporal) peorTemporal = dt;

            if (acumulado >= 1f)
            {
                fps = fotogramas / acumulado;
                msMedio = acumulado / fotogramas * 1000f;
                msPeor = peorTemporal * 1000f;
                fotogramas = 0;
                acumulado = 0f;
                peorTemporal = 0f;
            }
        }

        void OnGUI()
        {
            if (!visible) return;
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
                estilo.normal.textColor = Color.white;
            }
            string texto = $"{fps:0} FPS · {msMedio:0.0} ms/fotograma · peor {msPeor:0.0} ms\n" +
                           $"Escena: {SceneManager.GetActiveScene().name} · {Screen.width}×{Screen.height}";
            GUI.Box(new Rect(Screen.width / 2f - 260f, 6f, 520f, 54f), texto, estilo);
        }
    }
}
