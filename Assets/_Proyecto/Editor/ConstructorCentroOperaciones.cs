using System.Collections.Generic;
using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego).
///
/// Menú "Prototipo → 6. Crear centro de operaciones":
/// - Crea la escena CentroOperaciones (pregunta antes de sustituirla).
/// - Crea, si no existe, el prefab EstacionOrbital con geometría propia.
/// - Usa la textura de Europa y el modelo TDRS de Externos/NASA.
///   Si el TDRS no se ha importado (falta glTFast), pone un marcador y avisa.
/// - Pone CentroOperaciones la primera en la lista de compilación.
/// No modifica ninguna otra escena ni sistema.
/// </summary>
public static class ConstructorCentroOperaciones
{
    const string Raiz = "Assets/_Proyecto";
    const string RutaEscena = Raiz + "/Scenes/CentroOperaciones.unity";
    const string CarpetaNasa = Raiz + "/Externos/NASA";
    const string RutaTextura = CarpetaNasa + "/Jupiter - Europa.jpg";
    const string RutaTdrs = CarpetaNasa + "/Tracking and Data Relay Satellites (TDRS) (A).glb";
    const string RutaCreditos = Raiz + "/CREDITOS.md";
    const string CarpetaPresentacion = Raiz + "/Presentacion";
    const string RutaPrefabEstacion = Raiz + "/Prefabs/EstacionOrbital.prefab";
    const string RutaMallaEstrellas = CarpetaPresentacion + "/Estrellas.asset";

    // Disposición (editable después en la escena). Calculada para que el satélite no quede
    // detrás de la estación; es la misma que aplica el menú 7.
    static readonly Vector3 PosicionLuna = new Vector3(139f, 46.6f, 154.6f);
    const float DiametroLuna = 150f;
    static readonly Vector3 PosicionCamara = new Vector3(-7f, 3.5f, -26f);
    static readonly Vector3 PuntoMira = new Vector3(5f, 0f, 20f);

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/6. Crear centro de operaciones", false, 1005)]
    public static void Crear()
    {
        if (File.Exists(RutaEscena) &&
            !EditorUtility.DisplayDialog("La escena ya existe",
                "Ya existe " + RutaEscena + ".\n\n¿Sustituirla por una nueva?\nEl prefab EstacionOrbital y los materiales existentes NO se modifican.",
                "Sustituir escena", "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        CrearCarpeta(Raiz, "Presentacion");
        CrearCarpeta(Raiz, "Materials");
        CrearCarpeta(Raiz, "Prefabs");
        CrearCarpeta(Raiz, "Scenes");

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Ambiente: espacio oscuro, sin niebla
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.07f, 0.09f);

        GameObject sol = new GameObject("Sol");
        Light luz = sol.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = new Color(1f, 0.96f, 0.9f);
        luz.intensity = 1.3f;
        luz.shadows = LightShadows.Soft;
        sol.transform.rotation = Quaternion.Euler(26f, -34f, 0f);

        // --- Estación (pivote de la escena)
        GameObject estacion = (GameObject)PrefabUtility.InstantiatePrefab(ObtenerPrefabEstacion());
        estacion.transform.position = new Vector3(12.6f, -10.1f, 26.1f);
        estacion.transform.rotation = Quaternion.Euler(0f, 25f, 8f);
        AnadirMovimiento(estacion, MovimientoVisual.Modo.RotarSobreSiMismo, Vector3.up, 2f, null, null, 0f);

        // --- Luna ficticia con textura de Europa
        GameObject luna = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        luna.name = "LunaFicticia (textura: Europa)";
        Object.DestroyImmediate(luna.GetComponent<Collider>());
        luna.transform.position = PosicionLuna;
        luna.transform.localScale = Vector3.one * DiametroLuna;
        luna.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
        luna.GetComponent<Renderer>().sharedMaterial = MaterialLuna();
        AnadirMovimiento(luna, MovimientoVisual.Modo.RotarSobreSiMismo, Vector3.up, 0.6f, null, null, 0f);

        // --- Satélite de relevo (TDRS): órbita visual entre la estación y la luna,
        //     siempre delante de la cámara. Solo decorativo: no cambia las reglas de comunicación.
        GameObject centroOrbita = new GameObject("CentroOrbitaRelevo");
        centroOrbita.transform.position = new Vector3(4.3f, 15.1f, 78.3f);
        GameObject satelite = ObtenerSatelite();
        satelite.transform.position = new Vector3(4.3f, 15.1f, 60.2f);
        Normalizar(satelite, 7f);
        AnadirMovimiento(satelite, MovimientoVisual.Modo.OrbitarAlrededor, new Vector3(0.1f, 1f, 0.3f), 6f, centroOrbita.transform, null, 0f);

        // --- Estrellas (malla propia)
        GameObject estrellas = new GameObject("Estrellas");
        estrellas.AddComponent<MeshFilter>().sharedMesh = MallaEstrellas();
        estrellas.AddComponent<MeshRenderer>().sharedMaterial = ObtenerMaterial("Estrella", Color.white, null, 0f, 0f, true);

        // --- Cámara con vaivén suave
        GameObject mira = new GameObject("PuntoMiraCamara");
        mira.transform.position = PuntoMira;
        GameObject camGo = new GameObject("Camara");
        camGo.tag = "MainCamera";
        camGo.transform.position = PosicionCamara;
        camGo.transform.LookAt(PuntoMira);
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.008f, 0.01f, 0.018f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1200f;
        camGo.AddComponent<AudioListener>();
        AnadirMovimiento(camGo, MovimientoVisual.Modo.OscilarAlrededor, Vector3.up, 0.6f, mira.transform, mira.transform, 8f);

        // --- Interfaz
        GameObject ui = new GameObject("CentroOperaciones");
        CentroOperaciones centro = ui.AddComponent<CentroOperaciones>();
        TextAsset creditos = AssetDatabase.LoadAssetAtPath<TextAsset>(RutaCreditos);
        if (creditos != null)
        {
            SerializedObject so = new SerializedObject(centro);
            so.FindProperty("creditos").objectReferenceValue = creditos;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else Debug.LogWarning("[Prototipo] No se encontró " + RutaCreditos + "; el panel de créditos quedará vacío.");

        EditorSceneManager.SaveScene(escena, RutaEscena);
        PonerPrimeraEnCompilacion();
        Debug.Log("[Prototipo] Escena CentroOperaciones creada y puesta la primera en la lista de compilación.");
    }

    // ------------------------------------------------------------------ Recursos externos

    static Material MaterialLuna()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTextura);
        if (tex != null)
        {
            TextureImporter imp = AssetImporter.GetAtPath(RutaTextura) as TextureImporter;
            if (imp != null && (imp.wrapModeV != TextureWrapMode.Clamp || imp.maxTextureSize != 2048))
            {
                imp.wrapModeU = TextureWrapMode.Repeat;
                imp.wrapModeV = TextureWrapMode.Clamp;
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTextura);
            }
        }
        else Debug.LogWarning("[Prototipo] No se encontró la textura " + RutaTextura + "; la luna será gris.");

        string ruta = Raiz + "/Materials/LunaFicticia.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(ShaderLit());
            m.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.92f));
            m.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(m, ruta);
        }
        if (tex != null && m.GetTexture("_BaseMap") == null)
        {
            m.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(m);
        }
        return m;
    }

    static GameObject ObtenerSatelite()
    {
        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTdrs);
        if (modelo != null)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            inst.name = "SateliteRelevo (modelo: TDRS)";
            return inst;
        }

        Debug.LogWarning("[Prototipo] El modelo TDRS no está importado como modelo (¿falta instalar glTFast o reimportar?). " +
                         "Se usa un marcador provisional; vuelve a ejecutar el menú 6 cuando se importe.");
        GameObject marcador = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marcador.name = "SateliteRelevo (MARCADOR PROVISIONAL)";
        Object.DestroyImmediate(marcador.GetComponent<Collider>());
        marcador.GetComponent<Renderer>().sharedMaterial = ObtenerMaterial("MarcadorPendiente", new Color(1f, 0.3f, 0.8f), new Color(1f, 0.2f, 0.7f), 0f, 0.3f, false);
        return marcador;
    }

    /// <summary>Escala el objeto para que su lado mayor mida 'tamano' metros.</summary>
    static void Normalizar(GameObject go, float tamano)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (mayor > 0.0001f) go.transform.localScale *= tamano / mayor;
    }

    // ------------------------------------------------------------------ Estación propia

    static GameObject ObtenerPrefabEstacion()
    {
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEstacion);
        if (existente != null) return existente;

        Material casco = ObtenerMaterial("CascoOrbital", new Color(0.82f, 0.84f, 0.86f), null, 0.4f, 0.55f, false);
        Material oro = ObtenerMaterial("AislanteDorado", new Color(0.78f, 0.58f, 0.24f), null, 0.85f, 0.6f, false);
        Material panel = ObtenerMaterial("PanelSolar", new Color(0.08f, 0.12f, 0.3f), new Color(0.01f, 0.02f, 0.06f), 0.3f, 0.85f, false);
        Material estructura = ObtenerMaterial("Estructura", new Color(0.35f, 0.36f, 0.38f), null, 0.6f, 0.4f, false);
        Material roja = ObtenerMaterial("LuzPosicionRoja", Color.red, new Color(3f, 0.2f, 0.2f), 0f, 0.5f, false);
        Material verde = ObtenerMaterial("LuzPosicionVerde", Color.green, new Color(0.2f, 3f, 0.3f), 0f, 0.5f, false);

        GameObject raiz = new GameObject("EstacionOrbital");
        Transform t = raiz.transform;

        // Módulo principal (eje X) y nodo central
        Pieza(PrimitiveType.Cylinder, "ModuloPrincipal", t, Vector3.zero, new Vector3(0, 0, 90), new Vector3(2.6f, 5f, 2.6f), casco);
        Pieza(PrimitiveType.Sphere, "Nodo", t, Vector3.zero, Vector3.zero, Vector3.one * 3.4f, casco);
        // Módulos secundarios (eje Z y eje Y)
        Pieza(PrimitiveType.Cylinder, "ModuloHabitable", t, new Vector3(0, 0, 3.6f), new Vector3(90, 0, 0), new Vector3(2.2f, 2.6f, 2.2f), casco);
        Pieza(PrimitiveType.Cylinder, "ModuloLogistico", t, new Vector3(0, 0, -3f), new Vector3(90, 0, 0), new Vector3(2f, 2f, 2f), oro);
        Pieza(PrimitiveType.Cylinder, "Esclusa", t, new Vector3(0, 2.6f, 0), Vector3.zero, new Vector3(1.4f, 1.2f, 1.4f), casco);

        // Estructura y paneles solares en ambos extremos
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Pieza(PrimitiveType.Cube, "Viga_" + lado, t, new Vector3(lado * 6.5f, 0, 0), Vector3.zero, new Vector3(3f, 0.25f, 0.25f), estructura);
            Pieza(PrimitiveType.Cube, "PanelSolarA_" + lado, t, new Vector3(lado * 10f, 0, 1.6f), Vector3.zero, new Vector3(6f, 0.06f, 2.6f), panel);
            Pieza(PrimitiveType.Cube, "PanelSolarB_" + lado, t, new Vector3(lado * 10f, 0, -1.6f), Vector3.zero, new Vector3(6f, 0.06f, 2.6f), panel);
            Pieza(PrimitiveType.Cube, "Mastil_" + lado, t, new Vector3(lado * 10f, 0, 0), Vector3.zero, new Vector3(6.2f, 0.12f, 0.12f), estructura);
        }

        // Radiador y antena
        Pieza(PrimitiveType.Cube, "Radiador", t, new Vector3(-1.5f, -2.4f, 0), Vector3.zero, new Vector3(3.5f, 0.06f, 2.2f), casco);
        Pieza(PrimitiveType.Cylinder, "MastilAntena", t, new Vector3(1.8f, 2.2f, 0), Vector3.zero, new Vector3(0.12f, 0.9f, 0.12f), estructura);
        Pieza(PrimitiveType.Sphere, "Antena", t, new Vector3(1.8f, 3.2f, 0), new Vector3(20, 0, 0), new Vector3(1.8f, 0.25f, 1.8f), casco);

        // Luces de posición
        Pieza(PrimitiveType.Sphere, "LuzRoja", t, new Vector3(-13.2f, 0, 0), Vector3.zero, Vector3.one * 0.3f, roja);
        Pieza(PrimitiveType.Sphere, "LuzVerde", t, new Vector3(13.2f, 0, 0), Vector3.zero, Vector3.one * 0.3f, verde);

        GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabEstacion);
        Object.DestroyImmediate(raiz);
        return guardado;
    }

    static void Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 rot, Vector3 escala, Material m)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = rot;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    // ------------------------------------------------------------------ Estrellas

    static Mesh MallaEstrellas()
    {
        Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(RutaMallaEstrellas);
        if (existente != null) return existente;

        Random.State previo = Random.state;
        Random.InitState(7);

        const int cantidad = 700;
        const float radio = 900f;
        List<Vector3> v = new List<Vector3>(cantidad * 4);
        List<int> tri = new List<int>(cantidad * 6);

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            Vector3 c = dir * radio;
            float s = Random.Range(0.8f, 2.4f);
            Vector3 a = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right).normalized * s;
            Vector3 b = Vector3.Cross(dir, a).normalized * s;
            int k = v.Count;
            v.Add(c - a - b); v.Add(c - a + b); v.Add(c + a + b); v.Add(c + a - b);
            // Doble cara para que se vea sin importar la orientación.
            tri.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3, k, k + 2, k + 1, k, k + 3, k + 2 });
        }
        Random.state = previo;

        Mesh m = new Mesh { name = "Estrellas" };
        m.SetVertices(v);
        m.SetTriangles(tri, 0);
        m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, RutaMallaEstrellas);
        return m;
    }

    // ------------------------------------------------------------------ Utilidades

    static void AnadirMovimiento(GameObject go, MovimientoVisual.Modo modo, Vector3 eje, float velocidad,
                                 Transform centro, Transform mirarA, float amplitud)
    {
        MovimientoVisual mv = go.AddComponent<MovimientoVisual>();
        SerializedObject so = new SerializedObject(mv);
        so.FindProperty("modo").enumValueIndex = (int)modo;
        so.FindProperty("eje").vector3Value = eje;
        so.FindProperty("velocidad").floatValue = velocidad;
        so.FindProperty("centro").objectReferenceValue = centro;
        so.FindProperty("mirarA").objectReferenceValue = mirarA;
        so.FindProperty("amplitudGrados").floatValue = amplitud;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Shader ShaderLit()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        return s != null ? s : Shader.Find("Standard");
    }

    static Material ObtenerMaterial(string nombre, Color color, Color? emision, float metalico, float suavidad, bool sinLuz)
    {
        string ruta = Raiz + "/Materials/" + nombre + ".mat";
        Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;

        Shader shader = sinLuz ? Shader.Find("Universal Render Pipeline/Unlit") : ShaderLit();
        if (shader == null) shader = ShaderLit();

        Material m = new Material(shader);
        m.SetColor("_BaseColor", color);
        if (!sinLuz)
        {
            m.SetFloat("_Metallic", metalico);
            m.SetFloat("_Smoothness", suavidad);
        }
        if (emision.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static void CrearCarpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + nombre))
            AssetDatabase.CreateFolder(padre, nombre);
    }

    static void PonerPrimeraEnCompilacion()
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == RutaEscena);
        lista.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
