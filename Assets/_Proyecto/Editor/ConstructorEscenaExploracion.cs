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
/// Crea la escena de exploración con figuras simples:
/// fondo marino, rocas, plantas luminosas, estación, sonda e interfaz.
///
/// Uso: menú "Prototipo → 1. Crear escena de exploración".
///
/// Seguridad:
/// - Si la escena ya existe, pregunta antes de sustituirla.
/// - Si los prefabs o materiales ya existen, los reutiliza y NO los sobrescribe,
///   para respetar los cambios del equipo.
/// - No toca SampleScene ni ningún otro archivo.
/// </summary>
public static class ConstructorEscenaExploracion
{
    const string Raiz = "Assets/_Proyecto";
    const string RutaEscena = Raiz + "/Scenes/Exploracion.unity";
    const string RutaPrefabSonda = Raiz + "/Prefabs/Sonda.prefab";
    const string RutaPrefabEstacion = Raiz + "/Prefabs/Estacion.prefab";

    // Tamaño del área jugable (medio lado, en metros) y altura del techo invisible.
    const float MedioLado = 95f;
    const float AlturaTecho = 45f;

    static readonly Color ColorAgua = new Color(0.02f, 0.16f, 0.22f);

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/1. Crear escena de exploración", false, 1000)]
    public static void Crear()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (File.Exists(RutaEscena) &&
            !EditorUtility.DisplayDialog(
                "La escena ya existe",
                "Ya existe " + RutaEscena + ".\n\n¿Quieres sustituirla por una nueva?\n" +
                "Los prefabs y materiales existentes NO se modifican.",
                "Sustituir escena", "Cancelar"))
            return;

        AsegurarCarpetas();

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigurarAmbiente();
        CrearLuzSuperficie();
        CrearFondoYLimites();
        CrearDecorado();

        GameObject estacion = ObtenerEstacion();
        GameObject sonda = ObtenerSonda(new Vector3(0f, 2f, 0f));
        CrearInterfaz(sonda, estacion);

        EditorSceneManager.SaveScene(escena, RutaEscena);
        AgregarAEscenasDeCompilacion(RutaEscena);

        Selection.activeGameObject = sonda;
        Debug.Log("[Prototipo] Escena creada en " + RutaEscena + ". Pulsa Play para probarla.");
    }

    // ------------------------------------------------------------------ Carpetas

    static void AsegurarCarpetas()
    {
        CrearCarpeta("Assets", "_Proyecto");
        CrearCarpeta(Raiz, "Scenes");
        CrearCarpeta(Raiz, "Materials");
        CrearCarpeta(Raiz, "Prefabs");
    }

    static void CrearCarpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + nombre))
            AssetDatabase.CreateFolder(padre, nombre);
    }

    // ------------------------------------------------------------------ Ambiente

    static void ConfigurarAmbiente()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.10f, 0.22f, 0.28f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = ColorAgua;
        RenderSettings.fogDensity = 0.03f;
    }

    static void CrearLuzSuperficie()
    {
        GameObject go = new GameObject("LuzSuperficie");
        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = new Color(0.55f, 0.8f, 0.95f);
        luz.intensity = 0.7f;
        luz.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(70f, 30f, 0f);
    }

    // ------------------------------------------------------------------ Escenario

    static void CrearFondoYLimites()
    {
        Material arena = ObtenerMaterial("Arena", new Color(0.55f, 0.5f, 0.38f));

        GameObject fondo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        fondo.name = "FondoMarino";
        fondo.transform.localScale = new Vector3(MedioLado * 2f / 10f, 1f, MedioLado * 2f / 10f);
        fondo.GetComponent<Renderer>().sharedMaterial = arena;

        // Paredes y techo invisibles para que la sonda no salga del área.
        GameObject limites = new GameObject("Limites");
        CrearMuro(limites.transform, "MuroNorte", new Vector3(0f, AlturaTecho / 2f, MedioLado), new Vector3(MedioLado * 2f, AlturaTecho, 1f));
        CrearMuro(limites.transform, "MuroSur", new Vector3(0f, AlturaTecho / 2f, -MedioLado), new Vector3(MedioLado * 2f, AlturaTecho, 1f));
        CrearMuro(limites.transform, "MuroEste", new Vector3(MedioLado, AlturaTecho / 2f, 0f), new Vector3(1f, AlturaTecho, MedioLado * 2f));
        CrearMuro(limites.transform, "MuroOeste", new Vector3(-MedioLado, AlturaTecho / 2f, 0f), new Vector3(1f, AlturaTecho, MedioLado * 2f));
        CrearMuro(limites.transform, "Techo", new Vector3(0f, AlturaTecho, 0f), new Vector3(MedioLado * 2f, 1f, MedioLado * 2f));
    }

    static void CrearMuro(Transform padre, string nombre, Vector3 posicion, Vector3 tamano)
    {
        GameObject muro = new GameObject(nombre);
        muro.transform.SetParent(padre, false);
        muro.transform.localPosition = posicion;
        BoxCollider caja = muro.AddComponent<BoxCollider>();
        caja.size = tamano;
    }

    static void CrearDecorado()
    {
        Material roca = ObtenerMaterial("Roca", new Color(0.24f, 0.26f, 0.29f));
        Material tallo = ObtenerMaterial("TalloPlanta", new Color(0.08f, 0.22f, 0.16f));
        Material brillo = ObtenerMaterial("BrilloPlanta", new Color(0.2f, 0.9f, 0.8f), new Color(0.1f, 1.6f, 1.4f));

        GameObject rocas = new GameObject("Rocas");
        GameObject plantas = new GameObject("PlantasLuminosas");

        // Semilla fija: la escena sale igual cada vez que se genera.
        Random.State estadoPrevio = Random.state;
        Random.InitState(2026);

        for (int i = 0; i < 32; i++)
        {
            Vector3 p = PosicionLejosDeLaEstacion(16f);
            Vector3 escala = new Vector3(Random.Range(2f, 7f), Random.Range(1.5f, 5f), Random.Range(2f, 7f));
            p.y = escala.y * 0.3f;
            PrimitiveType tipo = i % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube;
            GameObject r = Primitiva(tipo, "Roca_" + i, rocas.transform, p, escala, roca, true);
            r.transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
        }

        for (int i = 0; i < 45; i++)
        {
            Vector3 p = PosicionLejosDeLaEstacion(9f);
            float alto = Random.Range(1f, 3f);
            GameObject planta = new GameObject("Planta_" + i);
            planta.transform.SetParent(plantas.transform, false);
            planta.transform.position = new Vector3(p.x, 0f, p.z);
            Primitiva(PrimitiveType.Cylinder, "Tallo", planta.transform, new Vector3(0f, alto / 2f, 0f), new Vector3(0.12f, alto / 2f, 0.12f), tallo, false);
            Primitiva(PrimitiveType.Sphere, "Brillo", planta.transform, new Vector3(0f, alto, 0f), Vector3.one * 0.4f, brillo, false);
        }

        Random.state = estadoPrevio;
    }

    static Vector3 PosicionLejosDeLaEstacion(float radioLibre)
    {
        float limite = MedioLado - 8f;
        for (int intento = 0; intento < 50; intento++)
        {
            Vector3 p = new Vector3(Random.Range(-limite, limite), 0f, Random.Range(-limite, limite));
            if (p.magnitude > radioLibre) return p;
        }
        return new Vector3(limite, 0f, limite);
    }

    // ------------------------------------------------------------------ Estación

    static GameObject ObtenerEstacion()
    {
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEstacion);
        if (existente != null)
            return (GameObject)PrefabUtility.InstantiatePrefab(existente);

        Material casco = ObtenerMaterial("CascoEstacion", new Color(0.75f, 0.78f, 0.8f));
        Material baliza = ObtenerMaterial("BalizaEstacion", new Color(1f, 0.6f, 0.2f), new Color(2.5f, 1.3f, 0.3f));

        GameObject raiz = new GameObject("Estacion");

        // Zona de recarga: esfera invisible marcada como trigger.
        SphereCollider zona = raiz.AddComponent<SphereCollider>();
        zona.isTrigger = true;
        zona.center = new Vector3(0f, 3.3f, 0f);
        zona.radius = 6f;
        raiz.AddComponent<EstacionSegura>();

        Primitiva(PrimitiveType.Cylinder, "Plataforma", raiz.transform, new Vector3(0f, 0.15f, 0f), new Vector3(10f, 0.15f, 10f), casco, true);
        Primitiva(PrimitiveType.Cylinder, "Techo", raiz.transform, new Vector3(0f, 6.45f, 0f), new Vector3(10f, 0.15f, 10f), casco, true);

        float d = 3.6f;
        Vector3[] esquinas =
        {
            new Vector3(d, 3.3f, d), new Vector3(-d, 3.3f, d),
            new Vector3(d, 3.3f, -d), new Vector3(-d, 3.3f, -d)
        };
        for (int i = 0; i < esquinas.Length; i++)
            Primitiva(PrimitiveType.Cylinder, "Pilar_" + i, raiz.transform, esquinas[i], new Vector3(0.5f, 3f, 0.5f), casco, true);

        Primitiva(PrimitiveType.Sphere, "Baliza", raiz.transform, new Vector3(0f, 7.2f, 0f), Vector3.one * 0.9f, baliza, false);

        GameObject luzGo = new GameObject("LuzEstacion");
        luzGo.transform.SetParent(raiz.transform, false);
        luzGo.transform.localPosition = new Vector3(0f, 5f, 0f);
        Light luz = luzGo.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.8f, 0.55f);
        luz.range = 25f;
        luz.intensity = 4f;

        // Guarda el prefab y devuelve el objeto de la ESCENA (no el asset del disco).
        PrefabUtility.SaveAsPrefabAssetAndConnect(raiz, RutaPrefabEstacion, InteractionMode.AutomatedAction);
        return raiz;
    }

    // ------------------------------------------------------------------ Sonda

    static GameObject ObtenerSonda(Vector3 posicion)
    {
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSonda);
        if (existente != null)
        {
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(existente);
            instancia.transform.position = posicion;
            return instancia;
        }

        GameObject sonda = new GameObject("Sonda");
        sonda.transform.position = posicion;

        Rigidbody rb = sonda.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        SphereCollider cuerpo = sonda.AddComponent<SphereCollider>();
        cuerpo.radius = 0.5f;

        sonda.AddComponent<SistemaEnergia>();
        sonda.AddComponent<ControladorSonda>();

        GameObject camGo = new GameObject("CamaraSonda");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(sonda.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColorAgua;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 160f;
        camGo.AddComponent<AudioListener>();

        GameObject focoGo = new GameObject("FocoFrontal");
        focoGo.transform.SetParent(camGo.transform, false);
        Light foco = focoGo.AddComponent<Light>();
        foco.type = LightType.Spot;
        foco.color = new Color(0.85f, 0.95f, 1f);
        foco.range = 35f;
        foco.spotAngle = 70f;
        foco.intensity = 3f;

        // Guarda el prefab y devuelve el objeto de la ESCENA (no el asset del disco).
        PrefabUtility.SaveAsPrefabAssetAndConnect(sonda, RutaPrefabSonda, InteractionMode.AutomatedAction);
        return sonda;
    }

    // ------------------------------------------------------------------ Interfaz

    static void CrearInterfaz(GameObject sonda, GameObject estacion)
    {
        GameObject go = new GameObject("Interfaz");
        HudMinimo hud = go.AddComponent<HudMinimo>();

        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("energia").objectReferenceValue = sonda.GetComponent<SistemaEnergia>();
        so.FindProperty("sonda").objectReferenceValue = sonda.transform;
        so.FindProperty("estacion").objectReferenceValue = estacion.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ Utilidades

    static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre,
                                Vector3 posicionLocal, Vector3 escala, Material material, bool conColision)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicionLocal;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = material;

        Collider colision = go.GetComponent<Collider>();
        if (!conColision)
        {
            Object.DestroyImmediate(colision);
        }
        else if (tipo == PrimitiveType.Cylinder)
        {
            // El cilindro trae un CapsuleCollider, que falla con formas planas.
            Object.DestroyImmediate(colision);
            go.AddComponent<MeshCollider>();
        }
        return go;
    }

    static Material ObtenerMaterial(string nombre, Color color, Color? emision = null)
    {
        string ruta = Raiz + "/Materials/" + nombre + ".mat";
        Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[Prototipo] No se encontró el shader URP/Lit. ¿El proyecto usa la plantilla Universal 3D?");
            shader = Shader.Find("Standard");
        }

        Material m = new Material(shader);
        m.SetColor("_BaseColor", color);
        if (emision.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static void AgregarAEscenasDeCompilacion(string ruta)
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == ruta);
        lista.Insert(0, new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
