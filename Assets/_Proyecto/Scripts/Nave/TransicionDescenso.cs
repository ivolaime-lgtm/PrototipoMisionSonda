using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Transición corta entre la nave y Exploracion (IMGUI, sin escena nueva).
    /// Tres tarjetas: llegada al destino → despliegue del sistema de acceso → descenso al océano.
    /// Espacio: siguiente tarjeta · Intro: omitir todo.
    /// El acceso a través del hielo está SIMPLIFICADO: no se simula la perforación.
    /// Solo es visual: no cambia ningún dato de la misión. La crea ControlNave al desplegar la sonda.
    /// </summary>
    public class TransicionDescenso : MonoBehaviour
    {
        float segundosPorTarjeta = 4.5f;
        Action alTerminar;
        int tarjeta;
        float t;            // segundos en la tarjeta actual
        bool terminada;
        GUIStyle sTitulo, sTexto, sPequeno, sPie;

        static readonly string[] Titulos =
        {
            "1/3 · LLEGADA AL DESTINO",
            "2/3 · DESPLIEGUE DEL SISTEMA DE ACCESO",
            "3/3 · DESCENSO AL OCÉANO"
        };

        static readonly string[] Textos =
        {
            "La nave mantiene una órbita estable sobre la luna helada. La sonda se separa y desciende hacia la superficie.",
            "Un módulo de acceso se posa sobre el hielo y abre un paso hasta el agua líquida.",
            "La sonda atraviesa el paso y llega al océano bajo el hielo. Desde aquí la controlas tú, con su propia batería."
        };

        const string Aviso =
            "Simplificación del juego: el paso a través del hielo no se simula. En una misión real, atravesar una capa " +
            "gruesa de hielo sería uno de los mayores retos técnicos. Luna y misión ficticias (DEMOSTRACIÓN).";

        public static TransicionDescenso Iniciar(GameObject anfitrion, float segundosPorTarjeta, Action alTerminar)
        {
            TransicionDescenso tr = anfitrion.AddComponent<TransicionDescenso>();
            tr.segundosPorTarjeta = Mathf.Max(0.5f, segundosPorTarjeta);
            tr.alTerminar = alTerminar;
            return tr;
        }

        void Update()
        {
            if (terminada) return;
            t += Time.deltaTime;

            Keyboard k = Keyboard.current;
            if (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
            {
                Terminar();
                return;
            }
            if ((k != null && k.spaceKey.wasPressedThisFrame) || t >= segundosPorTarjeta)
            {
                tarjeta++;
                t = 0f;
                if (tarjeta >= Titulos.Length) Terminar();
            }
        }

        void Terminar()
        {
            if (terminada) return;
            terminada = true;
            alTerminar?.Invoke();
        }

        void OnGUI()
        {
            if (terminada) return;
            Preparar();
            GUI.depth = -100;   // por encima del HUD de la nave

            float escala = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            float ancho = Screen.width / escala;
            float f = Mathf.Clamp01(t / segundosPorTarjeta);
            int n = Mathf.Min(tarjeta, Titulos.Length - 1);

            if (n == 0)
            {
                // Tarjeta 1: deja ver la luna por la ventana y oscurece solo la franja inferior.
                Rellenar(new Rect(0f, 0f, ancho, 1080f), new Color(0f, 0.02f, 0.05f, 0.25f));
            }
            else
            {
                Corte(ancho, n, f);
            }

            // Texto
            Rect caja = new Rect(ancho / 2f - 620f, 760f, 1240f, 280f);
            Rellenar(caja, new Color(0f, 0.04f, 0.07f, 0.88f));
            GUILayout.BeginArea(new Rect(caja.x + 30f, caja.y + 18f, caja.width - 60f, caja.height - 36f));
            GUILayout.Label(Titulos[n], sTitulo);
            GUILayout.Label(Textos[n], sTexto);
            GUILayout.FlexibleSpace();
            GUILayout.Label(Aviso, sPequeno);
            GUILayout.EndArea();

            // Progreso de la tarjeta y controles
            Rellenar(new Rect(caja.x, caja.yMax - 4f, caja.width * f, 4f), new Color(0.3f, 0.9f, 1f, 0.9f));
            GUI.Label(new Rect(caja.x, caja.yMax + 6f, caja.width, 30f), "Espacio: siguiente · Intro: omitir", sPie);

            GUI.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// Corte esquemático (no a escala): espacio, capa de hielo y océano.
        /// Tarjeta 2: el módulo baja y se abre el paso. Tarjeta 3: la sonda baja por el paso.
        /// </summary>
        void Corte(float ancho, int n, float f)
        {
            float cx = ancho / 2f;
            float yHielo = 300f, gruesoHielo = 170f, yOceano = yHielo + gruesoHielo;

            Rellenar(new Rect(0f, 0f, ancho, yHielo), new Color(0.01f, 0.015f, 0.03f));                 // espacio
            for (int i = 0; i < 40; i++)                                                                  // estrellas fijas
            {
                float sx = Mathf.Repeat(i * 173.3f, ancho), sy = Mathf.Repeat(i * 61.7f, yHielo - 20f);
                Rellenar(new Rect(sx, sy, 2f, 2f), new Color(1f, 1f, 1f, 0.6f));
            }
            Rellenar(new Rect(0f, yHielo, ancho, gruesoHielo), new Color(0.72f, 0.86f, 0.95f));          // hielo
            Rellenar(new Rect(0f, yHielo, ancho, 6f), new Color(0.92f, 0.97f, 1f));
            Rellenar(new Rect(0f, yOceano, ancho, 1080f - yOceano), new Color(0.02f, 0.12f, 0.22f));      // océano
            GUI.Label(new Rect(40f, yHielo + 10f, 400f, 30f), "Capa de hielo (esquema, no a escala)", sPequeno);
            GUI.Label(new Rect(40f, yOceano + 10f, 400f, 30f), "Océano bajo el hielo", sPequeno);

            Color modulo = new Color(0.85f, 0.65f, 0.3f);
            Color paso = new Color(0.25f, 0.6f, 0.85f, 0.85f);

            if (n == 1)
            {
                // El módulo desciende hasta la superficie (primera mitad) y abre el paso (segunda mitad).
                float baja = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(f * 2f));
                float yModulo = Mathf.Lerp(40f, yHielo - 46f, baja);
                Rellenar(new Rect(cx - 30f, yModulo, 60f, 46f), modulo);
                float abre = Mathf.Clamp01(f * 2f - 1f);
                if (abre > 0f)
                    Rellenar(new Rect(cx - 9f, yHielo, 18f, gruesoHielo * abre), paso);
            }
            else
            {
                Rellenar(new Rect(cx - 30f, yHielo - 46f, 60f, 46f), modulo);
                Rellenar(new Rect(cx - 9f, yHielo, 18f, gruesoHielo), paso);
                // La sonda baja por el paso y entra en el océano.
                float ySonda = Mathf.Lerp(yHielo - 20f, yOceano + 150f, Mathf.SmoothStep(0f, 1f, f));
                Rellenar(new Rect(cx - 7f, ySonda, 14f, 22f), new Color(0.95f, 0.8f, 0.4f));
                Rellenar(new Rect(cx - 2f, ySonda + 22f, 4f, 30f), new Color(0.6f, 0.95f, 1f, 0.5f));   // luz de la sonda
                // Burbujas al entrar en el agua
                if (ySonda > yOceano)
                    for (int i = 0; i < 6; i++)
                    {
                        float by = ySonda - Mathf.Repeat(t * 60f + i * 25f, 120f);
                        if (by > yOceano)
                            Rellenar(new Rect(cx - 20f + i * 8f, by, 4f, 4f), new Color(0.8f, 0.95f, 1f, 0.6f));
                    }
            }
        }

        static void Rellenar(Rect r, Color c)
        {
            Color previo = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previo;
        }

        void Preparar()
        {
            if (sTitulo != null) return;
            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, richText = true };
            sTitulo.normal.textColor = new Color(0.6f, 0.9f, 1f);
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true, richText = true };
            sTexto.normal.textColor = Color.white;
            sPequeno = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true };
            sPequeno.normal.textColor = new Color(0.8f, 0.9f, 0.95f);
            sPie = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            sPie.normal.textColor = new Color(0.85f, 0.95f, 1f);
        }
    }
}
