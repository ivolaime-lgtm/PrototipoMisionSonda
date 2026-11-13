using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Señalización de los cuatro sectores de la nave (solo visual, creada en tiempo de ejecución por ControlNave).
    /// Numeración fija: 1 Pilotaje · 2 Ingeniería · 3 Comunicaciones y navegación · 4 Ciencia / control de misión.
    ///
    /// Dos disposiciones:
    /// - Nave AMPLIADA (existe RegistroAmpliacion, menú 9): rótulo sobre la entrada de cada sala, borde luminoso
    ///   en el perímetro de cada sala e indicador del sector actual. Sin tabiques ni luces propias: las salas ya
    ///   tienen paredes y luces.
    /// - Cabina ORIGINAL: rótulos en las paredes, bordes, tabiques bajos sin colisionador y una luz por sector
    ///   (versión reducida anterior).
    /// No mueve puestos, vistas ni colisionadores y no añade colisionadores.
    /// Un jugador controla la nave; la nave tiene puestos para cuatro tripulantes.
    /// </summary>
    public class SectoresNave : MonoBehaviour
    {
        struct Sector
        {
            public string nombre;
            public Color color;
            public Vector3 centro;      // centro de la zona en el suelo
            public Vector2 tamano;      // x, z
            public Vector3 rotulo;      // posición del rótulo
            public float giroRotulo;    // el texto se lee mirando hacia +z local del rótulo
        }

        static readonly Color CPilotaje = new Color(0.3f, 0.9f, 1f);
        static readonly Color CIngenieria = new Color(1f, 0.6f, 0.2f);
        static readonly Color CComunicaciones = new Color(0.4f, 1f, 0.5f);
        static readonly Color CCiencia = new Color(0.75f, 0.5f, 1f);

        // Cabina original (interior x ±4, z ±5; ventana al frente, +z).
        static readonly Sector[] SectoresCabina =
        {
            new Sector { nombre = "PILOTAJE", color = CPilotaje, centro = new Vector3(0f, 0f, 3.85f), tamano = new Vector2(4.6f, 2.1f),
                         rotulo = new Vector3(-3.1f, 2.25f, 4.93f), giroRotulo = 0f },
            new Sector { nombre = "INGENIERÍA", color = CIngenieria, centro = new Vector3(-3.05f, 0f, 0.5f), tamano = new Vector2(1.8f, 3.0f),
                         rotulo = new Vector3(-3.93f, 2.45f, 0.5f), giroRotulo = -90f },
            new Sector { nombre = "COMUNICACIONES Y NAVEGACIÓN", color = CComunicaciones, centro = new Vector3(3.05f, 0f, 0.5f), tamano = new Vector2(1.8f, 3.0f),
                         rotulo = new Vector3(3.93f, 2.45f, 0.5f), giroRotulo = 90f },
            new Sector { nombre = "CIENCIA / CONTROL DE MISIÓN", color = CCiencia, centro = new Vector3(0f, 0f, -4.05f), tamano = new Vector2(4.0f, 1.8f),
                         rotulo = new Vector3(0f, 2.45f, -4.93f), giroRotulo = 180f },
        };

        // Nave ampliada (menú 9): cabina de pilotaje, pasillo, salas laterales e Ingeniería a popa.
        // Rótulos en el lado del pasillo, sobre cada entrada.
        static readonly Sector[] SectoresAmpliada =
        {
            new Sector { nombre = "PILOTAJE", color = CPilotaje, centro = new Vector3(0f, 0f, 3.25f), tamano = new Vector2(7.8f, 3.4f),
                         rotulo = new Vector3(0f, 2.6f, 1.22f), giroRotulo = 0f },
            new Sector { nombre = "INGENIERÍA", color = CIngenieria, centro = new Vector3(0f, 0f, -11.6f), tamano = new Vector2(6.9f, 4.7f),
                         rotulo = new Vector3(0f, 2.6f, -8.92f), giroRotulo = 180f },
            new Sector { nombre = "COMUNICACIONES Y NAVEGACIÓN", color = CComunicaciones, centro = new Vector3(3.3f, 0f, -1.5f), tamano = new Vector2(3.7f, 3.9f),
                         rotulo = new Vector3(1.17f, 2.6f, -1.5f), giroRotulo = 90f },
            new Sector { nombre = "CIENCIA / CONTROL DE MISIÓN", color = CCiencia, centro = new Vector3(-3.3f, 0f, -1.5f), tamano = new Vector2(3.7f, 3.9f),
                         rotulo = new Vector3(-1.17f, 2.6f, -1.5f), giroRotulo = -90f },
        };

        Sector[] sectores;
        bool ampliada;
        Transform raiz;
        JugadorNave jugador;
        GUIStyle estilo;
        string sectorActual = "";

        void Start()
        {
            ampliada = FindFirstObjectByType<RegistroAmpliacion>() != null;
            sectores = ampliada ? SectoresAmpliada : SectoresCabina;
            jugador = FindFirstObjectByType<JugadorNave>();
            raiz = new GameObject("SECTORES (solo visual)").transform;
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < sectores.Length; i++)
            {
                Sector s = sectores[i];
                Transform grupo = new GameObject("Sector " + (i + 1) + " · " + s.nombre).transform;
                grupo.SetParent(raiz, false);
                Material borde = Mat(Color.black, s.color * 1.6f);

                BordeSuelo(grupo, s, borde);
                Rotulo(grupo, i, s, fuente, borde);
                if (!ampliada)
                {
                    Luz(grupo, s, i);
                    Tabiques(grupo, i, Mat(new Color(0.28f, 0.31f, 0.36f), s.color * 0.15f), borde);
                }
            }
        }

        void BordeSuelo(Transform g, Sector s, Material m)
        {
            float x = s.tamano.x / 2f, z = s.tamano.y / 2f, y = 0.012f, a = 0.05f;
            Pieza(g, s.centro + new Vector3(0f, y, z), new Vector3(s.tamano.x, 0.01f, a), m);
            Pieza(g, s.centro + new Vector3(0f, y, -z), new Vector3(s.tamano.x, 0.01f, a), m);
            Pieza(g, s.centro + new Vector3(x, y, 0f), new Vector3(a, 0.01f, s.tamano.y), m);
            Pieza(g, s.centro + new Vector3(-x, y, 0f), new Vector3(a, 0.01f, s.tamano.y), m);
        }

        void Luz(Transform g, Sector s, int i)
        {
            GameObject go = new GameObject("Luz del sector");
            go.transform.SetParent(g, false);
            go.transform.position = s.centro + new Vector3(0f, 2.4f, 0f) + (i == 0 ? new Vector3(0f, 0f, -0.6f) : Vector3.zero);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = s.color;
            l.intensity = i == 0 ? 0.7f : 1.0f;
            l.range = 3.5f;
            l.shadows = LightShadows.None;
        }

        void Rotulo(Transform g, int i, Sector s, Font fuente, Material franja)
        {
            Transform r = new GameObject("Rótulo").transform;
            r.SetParent(g, false);
            r.position = s.rotulo;
            r.rotation = Quaternion.Euler(0f, s.giroRotulo, 0f);

            // En la cabina original, el de pilotaje es más estrecho para no llegar al borde de la ventana (x = −2).
            float ancho = !ampliada && i == 0 ? 1.7f : 2.2f;
            Pieza(r, Vector3.zero, new Vector3(ancho, 0.55f, 0.03f), Mat(new Color(0.05f, 0.07f, 0.09f), Color.black), true);
            Pieza(r, new Vector3(0f, -0.3f, -0.02f), new Vector3(ancho, 0.05f, 0.03f), franja, true);

            if (fuente == null) return;
            GameObject t = new GameObject("Texto");
            t.transform.SetParent(r, false);
            t.transform.localPosition = new Vector3(0f, 0.02f, -0.025f);
            TextMesh tm = t.AddComponent<TextMesh>();
            tm.font = fuente;
            tm.fontSize = 80;
            tm.characterSize = 0.011f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = s.color;
            tm.text = $"SECTOR {i + 1}\n{s.nombre}";
            t.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        }

        // Solo en la cabina original: tabiques bajos (1,1 m) pegados a las paredes, sin colisionador.
        void Tabiques(Transform g, int i, Material m, Material borde)
        {
            switch (i)
            {
                case 0:
                    Tabique(g, new Vector3(-2.75f, 0.55f, 4.4f), new Vector3(0.08f, 1.1f, 1.2f), m, borde);
                    Tabique(g, new Vector3(2.75f, 0.55f, 4.4f), new Vector3(0.08f, 1.1f, 1.2f), m, borde);
                    break;
                case 1:
                    Tabique(g, new Vector3(-3.45f, 0.55f, -1.1f), new Vector3(1.1f, 1.1f, 0.08f), m, borde);
                    Tabique(g, new Vector3(-3.45f, 0.55f, 2.1f), new Vector3(1.1f, 1.1f, 0.08f), m, borde);
                    break;
                case 2:
                    Tabique(g, new Vector3(3.45f, 0.55f, -1.1f), new Vector3(1.1f, 1.1f, 0.08f), m, borde);
                    Tabique(g, new Vector3(3.45f, 0.55f, 2.1f), new Vector3(1.1f, 1.1f, 0.08f), m, borde);
                    break;
                default:
                    Tabique(g, new Vector3(-2.2f, 0.55f, -4.45f), new Vector3(0.08f, 1.1f, 1.1f), m, borde);
                    Tabique(g, new Vector3(2.2f, 0.55f, -4.45f), new Vector3(0.08f, 1.1f, 1.1f), m, borde);
                    break;
            }
        }

        void Tabique(Transform g, Vector3 pos, Vector3 tam, Material m, Material borde)
        {
            Pieza(g, pos, tam, m);
            Pieza(g, pos + new Vector3(0f, tam.y / 2f + 0.015f, 0f), new Vector3(tam.x + 0.02f, 0.03f, tam.z + 0.02f), borde);
        }

        void Update()
        {
            if (jugador == null || sectores == null) return;
            Vector3 p = jugador.transform.position;
            float margen = ampliada ? 0.1f : 0.6f;
            sectorActual = "";
            for (int i = 0; i < sectores.Length; i++)
            {
                Sector s = sectores[i];
                if (Mathf.Abs(p.x - s.centro.x) <= s.tamano.x / 2f + margen && Mathf.Abs(p.z - s.centro.z) <= s.tamano.y / 2f + margen)
                {
                    sectorActual = $"Sector {i + 1} · {s.nombre}";
                    break;
                }
            }
            if (sectorActual.Length == 0) sectorActual = ampliada ? "Pasillo" : "Pasillo central";
        }

        void OnGUI()
        {
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true };
                estilo.normal.textColor = new Color(0.85f, 0.95f, 1f);
            }
            float escala = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            GUI.Label(new Rect(24f, 1080f - 70f, 470f, 50f),
                      $"<b>{sectorActual}</b>\n<size=14>Nave con puestos para 4 tripulantes · controla 1 jugador</size>", estilo);
            GUI.matrix = Matrix4x4.identity;
        }

        GameObject Pieza(Transform padre, Vector3 pos, Vector3 escala, Material m, bool local = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());          // sin colisionador: no cambia el movimiento
            go.transform.SetParent(padre, false);
            if (local) go.transform.localPosition = pos; else go.transform.position = pos;
            go.transform.localScale = escala;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static Material Mat(Color baseColor, Color emision)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material m = new Material(sh);
            m.SetColor("_BaseColor", baseColor);
            m.color = baseColor;
            if (emision.maxColorComponent > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emision);
            }
            return m;
        }
    }
}
