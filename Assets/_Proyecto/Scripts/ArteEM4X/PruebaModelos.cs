using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PrototipoMision
{
    /// <summary>
    /// PRUEBA DE IMPORTACIÓN DE MODELOS (escena ModeloPrueba). Reutiliza SIN cambios la prueba de circulación
    /// (EM-4X, colisiones, puertas, personaje y sus tres revisiones) y encima aplica la tabla RecursosVisualesEM4X.
    ///   F2: mostrar u ocultar el informe · F3: alternar modelos / geometría provisional
    ///   F4: ir a la entrada del módulo de Ciencia · F12: capturar vistas (planta, perfil, alzado, perspectiva) en Capturas/
    /// No toca ExpedicionMVP ni CirculacionPrueba, ni guarda nada en la escena.
    /// </summary>
    public class PruebaModelos : MonoBehaviour
    {
        [SerializeField] RecursosVisualesEM4X recursos;

        PruebaCirculacion circ;
        SustitucionVisualEM4X sustitucion;
        bool listo, panel = true, capturando;
        string aviso = "";
        float avisoHasta;
        Vector2 desp;
        GUIStyle sTexto, sTitulo, sBien, sMal;

        void Awake() { circ = gameObject.AddComponent<PruebaCirculacion>(); }

        IEnumerator Start()
        {
            while (!circ.Listo) yield return null;
            sustitucion = new GameObject("Sustitución visual (modelos)").AddComponent<SustitucionVisualEM4X>();
            sustitucion.Aplicar(circ.Expedicion.Vista.Nave, circ.Expedicion.Definicion, circ.Uniones, null, recursos);
            listo = true;
            Avisar($"Modelos aplicados: {sustitucion.Aplicadas}. F3 compara con la geometría provisional · F4 lleva a Ciencia · F12 captura vistas.", 8f);
        }

        void Avisar(string t, float s) { aviso = t; avisoHasta = Time.time + s; }

        void Update()
        {
            if (!listo || capturando) return;
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.f2Key.wasPressedThisFrame) panel = !panel;
            if (k.f3Key.wasPressedThisFrame)
            {
                sustitucion.MostrarModelos(!sustitucion.ModelosVisibles);
                Avisar(sustitucion.ModelosVisibles ? "MODELOS importados (colisiones provisionales)." : "GEOMETRÍA PROVISIONAL.", 3f);
            }
            if (k.f4Key.wasPressedThisFrame && circ.Personaje != null)
            {
                ModuloNave nc = circ.Expedicion.Definicion.Modulo("NC");
                ModuloNave ci = circ.Expedicion.Definicion.Modulo("CI");
                circ.Personaje.Recolocar(nc.AMundo(new Vector3(-3.4f, 0f, 6.5f)), Quaternion.LookRotation(ci.Adelante, ci.Arriba));
                Avisar("Frente a la entrada de Ciencia (puerta de aislamiento telescópica).", 3f);
            }
            if (k.f12Key.wasPressedThisFrame) StartCoroutine(Capturar());
        }

        // ------------------------------------------------------------------ Capturas ortográficas

        IEnumerator Capturar()
        {
            capturando = true;
            yield return new WaitForEndOfFrame();
            Transform nave = circ.Expedicion.Vista.Nave;
            bool etiquetas = circ.Expedicion.Vista.EtiquetasVisibles;
            circ.Expedicion.Vista.MostrarEtiquetas(false);
            Renderer[] cuerpo = circ.Personaje != null ? circ.Personaje.GetComponentsInChildren<Renderer>() : new Renderer[0];
            foreach (Renderer r in cuerpo) r.forceRenderingOff = true;

            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer r in nave.GetComponentsInChildren<Renderer>())
                if (r.enabled && !(r is LineRenderer) && r.GetComponent<TextMesh>() == null) b.Encapsulate(r.bounds);

            string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas"));
            Directory.CreateDirectory(carpeta);
            string sello = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Camera cam = new GameObject("Cámara de captura").AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.82f, 0.85f, 0.88f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            const int ancho = 2400, alto = 1400;
            var vistas = new (string nombre, Vector3 dir, Vector3 arriba, bool orto)[]
            {
                ("planta_desde_arriba", Vector3.down, Vector3.forward, true),
                ("perfil_desde_estribor", Vector3.left, Vector3.up, true),
                ("alzado_desde_proa", Vector3.back, Vector3.up, true),
                ("perspectiva", new Vector3(-0.55f, -0.45f, -0.7f).normalized, Vector3.up, false)
            };
            int hechas = 0;
            foreach (var v in vistas)
            {
                float dist = b.extents.magnitude * 3f;
                cam.transform.position = b.center - v.dir * dist;
                cam.transform.rotation = Quaternion.LookRotation(v.dir, v.arriba);
                cam.orthographic = v.orto;
                cam.aspect = (float)ancho / alto;
                if (v.orto)
                {
                    Vector3 e = b.extents;
                    Vector3 r = cam.transform.right, u = cam.transform.up;
                    float w = Mathf.Abs(r.x) * e.x + Mathf.Abs(r.y) * e.y + Mathf.Abs(r.z) * e.z;
                    float h = Mathf.Abs(u.x) * e.x + Mathf.Abs(u.y) * e.y + Mathf.Abs(u.z) * e.z;
                    cam.orthographicSize = Mathf.Max(h, w / cam.aspect) * 1.08f;
                }
                else cam.fieldOfView = 40f;
                cam.farClipPlane = dist * 3f;
                RenderTexture rt = RenderTexture.GetTemporary(ancho, alto, 24);
                RenderPipeline.StandardRequest pedido = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, pedido)) RenderPipeline.SubmitRenderRequest(cam, pedido);
                else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
                RenderTexture previa = RenderTexture.active;
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
                tex.Apply();
                RenderTexture.active = previa;
                RenderTexture.ReleaseTemporary(rt);
                File.WriteAllBytes(Path.Combine(carpeta, $"EM4X_{v.nombre}_{sello}.png"), tex.EncodeToPNG());
                Destroy(tex);
                hechas++;
                if (v.orto)
                    Debug.Log($"[Arte EM-4X] Vista {v.nombre}: {2f * cam.orthographicSize * cam.aspect:0.0} × {2f * cam.orthographicSize:0.0} m de imagen ({ancho} × {alto} px).", this);
                yield return null;
            }
            Destroy(cam.gameObject);
            foreach (Renderer r in cuerpo) if (r != null) r.forceRenderingOff = false;
            circ.Expedicion.Vista.MostrarEtiquetas(etiquetas);
            Avisar($"{hechas} capturas guardadas en {carpeta}", 8f);
            Debug.Log("[Arte EM-4X] Capturas en " + carpeta, this);
            capturando = false;
        }

        // ------------------------------------------------------------------ Informe

        void OnGUI()
        {
            if (sTexto == null)
            {
                sTexto = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = true };
                sTexto.normal.textColor = new Color(0.92f, 0.95f, 1f);
                sTitulo = new GUIStyle(sTexto) { fontSize = 17, fontStyle = FontStyle.Bold };
                sBien = new GUIStyle(sTexto); sBien.normal.textColor = new Color(0.5f, 0.9f, 0.55f);
                sMal = new GUIStyle(sTexto); sMal.normal.textColor = new Color(1f, 0.5f, 0.4f);
            }
            float esc = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(esc, esc, 1f));
            if (Time.time < avisoHasta)
            {
                Rect ra = new Rect(20f, 940f, 1100f, 50f);
                GUI.color = new Color(0f, 0f, 0f, 0.75f); GUI.DrawTexture(ra, Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.Label(new Rect(ra.x + 10f, ra.y + 12f, ra.width - 20f, 30f), aviso, sTexto);
            }
            if (!listo || !panel || capturando) return;
            Rect r = new Rect(14f, 140f, 560f, 820f);
            GUI.color = new Color(0f, 0.04f, 0.08f, 0.8f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(r.x + 10f, r.y + 8f, r.width - 20f, r.height - 16f));
            desp = GUILayout.BeginScrollView(desp);
            GUILayout.Label("PRUEBA DE MODELOS · EM-4X", sTitulo);
            GUILayout.Label(recursos == null ? "<color=#FF8060>Sin tabla RecursosVisualesEM4X asignada.</color>"
                : $"Tabla: {recursos.name} · {(sustitucion.ModelosVisibles ? "viendo MODELOS" : "viendo PROVISIONAL")}", sTexto);
            GUILayout.Label("F2 informe · F3 modelos/provisional · F4 ir a Ciencia · F12 capturar vistas · Tab cámara de inspección", sTexto);
            GUILayout.Label("Las colisiones, puertas y paneles son SIEMPRE los provisionales: el modelo solo se ve.", sTexto);
            foreach (SustitucionVisualEM4X.Linea l in sustitucion.Informe)
            {
                GUILayout.Label($"<b>{l.clave}</b> · {l.estado}", l.correcto ? sBien : sMal);
                foreach (string d in l.detalles) GUILayout.Label("   · " + d, sMal);
            }
            GUILayout.Space(6f);
            GUILayout.Label("Comprobar a mano: cruzar la puerta de Ciencia abierta y cerrada, usar su panel desde los dos lados, " +
                            "acercarse a la consola y mirar que nada visible invade el paso ni deja huecos donde hay pared.", sTexto);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
