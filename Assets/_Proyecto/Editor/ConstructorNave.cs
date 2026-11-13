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
/// Menú "Prototipo → 8. Crear escena Nave":
/// - Crea la escena Nave con formas simples (pregunta antes de sustituirla si ya existe).
/// - Crea Data/CatalogoNave si no existe (no lo sobrescribe).
/// - Crea solo los materiales que falten; reutiliza los existentes (Roca, LunaFicticia, Estrella…).
/// - Añade Nave a la lista de compilación justo después de Configuracion.
/// No modifica ninguna otra escena.
/// </summary>
public static class ConstructorNave
{
    const string Raiz = "Assets/_Proyecto";
    const string RutaEscena = Raiz + "/Scenes/Nave.unity";
    const string RutaCatalogoNave = Raiz + "/Data/CatalogoNave.asset";
    const string RutaCatalogoMision = Raiz + "/Data/CatalogoMision.asset";

    [MenuItem("Prototipo/8. Crear escena Nave")]
    public static void Crear()
    {
        if (File.Exists(RutaEscena) &&
            !EditorUtility.DisplayDialog("La escena ya existe",
                "Ya existe " + RutaEscena + ".\n\n¿Sustituirla por una nueva?\nEl CatalogoNave y los materiales existentes NO se modifican.",
                "Sustituir escena", "Cancelar"))
            return;

        CatalogoMision catalogoMision = AssetDatabase.LoadAssetAtPath<CatalogoMision>(RutaCatalogoMision);
        if (catalogoMision == null)
        {
            EditorUtility.DisplayDialog("Falta el catálogo de misión",
                "No se encontró " + RutaCatalogoMision + ".\nCréalo primero con el menú de configuración.", "Aceptar");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        CrearCarpeta(Raiz, "Data");
        CrearCarpeta(Raiz, "Materials");
        CrearCarpeta(Raiz, "Scenes");

        CatalogoNave catalogoNave = ObtenerCatalogoNave();

        // --- Materiales (solo se crean si faltan)
        Material paredes = ObtenerMaterial("CabinaInterior", new Color(0.55f, 0.58f, 0.62f), null, 0.1f, 0.35f, false);
        Material suelo = ObtenerMaterial("SueloCabina", new Color(0.22f, 0.24f, 0.27f), null, 0.2f, 0.3f, false);
        Material consola = ObtenerMaterial("ConsolaPuesto", new Color(0.14f, 0.16f, 0.19f), null, 0.4f, 0.5f, false);
        Material pantalla = ObtenerMaterial("PantallaPuesto", new Color(0.1f, 0.35f, 0.45f), null, 0f, 0f, true);
        Material indicador = ObtenerMaterial("IndicadorPuesto", Color.white, null, 0f, 0f, true);
        Material marco = ObtenerMaterial("Estructura", new Color(0.35f, 0.36f, 0.38f), null, 0.6f, 0.4f, false);
        Material plataformaMat = ObtenerMaterial("PlataformaLanzamiento", new Color(0.42f, 0.43f, 0.42f), null, 0f, 0.2f, false);
        Material roca = ObtenerMaterial("Roca", new Color(0.35f, 0.33f, 0.31f), null, 0f, 0.15f, false);
        Material lunaMat = ObtenerMaterial("LunaFicticia", new Color(0.9f, 0.9f, 0.92f), null, 0f, 0.15f, false);
        Material estrella = ObtenerMaterial("Estrella", Color.white, null, 0f, 0f, true);
        Material sondaMat = ObtenerMaterial("AislanteDorado", new Color(0.78f, 0.58f, 0.24f), null, 0.85f, 0.6f, false);

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.3f, 0.32f, 0.36f);

        GameObject sol = new GameObject("Sol");
        Light luzSol = sol.AddComponent<Light>();
        luzSol.type = LightType.Directional;
        luzSol.color = new Color(1f, 0.96f, 0.9f);
        luzSol.intensity = 1.1f;
        luzSol.shadows = LightShadows.None;
        sol.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

        // ================================================================ Cabina
        // Interior de 8 m (x) × 10 m (z) × 3 m de alto. La ventana está en la pared delantera (+z).
        Transform cabina = new GameObject("Cabina").transform;
        Bloque("Suelo", cabina, new Vector3(0f, -0.1f, 0f), new Vector3(8.4f, 0.2f, 10.4f), suelo);
        Bloque("Techo", cabina, new Vector3(0f, 3.1f, 0f), new Vector3(8.4f, 0.2f, 10.4f), paredes);
        Bloque("ParedIzquierda", cabina, new Vector3(-4.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 10.4f), paredes);
        Bloque("ParedDerecha", cabina, new Vector3(4.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 10.4f), paredes);
        Bloque("ParedTrasera", cabina, new Vector3(0f, 1.5f, -5.1f), new Vector3(8.4f, 3f, 0.2f), paredes);
        // Pared delantera con hueco de ventana (x −2,2 · y 1,0 a 2,6)
        Bloque("FrenteBajo", cabina, new Vector3(0f, 0.5f, 5.1f), new Vector3(8.4f, 1f, 0.2f), paredes);
        Bloque("FrenteAlto", cabina, new Vector3(0f, 2.8f, 5.1f), new Vector3(8.4f, 0.4f, 0.2f), paredes);
        Bloque("FrenteIzquierda", cabina, new Vector3(-3.1f, 1.8f, 5.1f), new Vector3(2.2f, 1.6f, 0.2f), paredes);
        Bloque("FrenteDerecha", cabina, new Vector3(3.1f, 1.8f, 5.1f), new Vector3(2.2f, 1.6f, 0.2f), paredes);
        Decoracion(PrimitiveType.Cube, "MarcoVentanaArriba", cabina, new Vector3(0f, 2.62f, 5.0f), new Vector3(4.1f, 0.08f, 0.1f), marco);
        Decoracion(PrimitiveType.Cube, "MarcoVentanaAbajo", cabina, new Vector3(0f, 0.98f, 5.0f), new Vector3(4.1f, 0.08f, 0.1f), marco);
        Decoracion(PrimitiveType.Cube, "MarcoVentanaCentro", cabina, new Vector3(0f, 1.8f, 5.0f), new Vector3(0.06f, 1.6f, 0.06f), marco);

        Light luz1 = LuzCabina("LuzCabinaDelantera", cabina, new Vector3(0f, 2.8f, 2f));
        Light luz2 = LuzCabina("LuzCabinaTrasera", cabina, new Vector3(0f, 2.8f, -2.5f));

        // ================================================================ Puestos
        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        PuestoNave pilotaje = CrearPuesto(TipoPuesto.Pilotaje, "Pilotaje", cabina,
            new Vector3(0f, 0.5f, 3.9f), new Vector3(2.4f, 1f, 0.8f), 0f,
            new Vector3(0f, 1.6f, 3.0f), new Vector3(0f, 0f, 0f), consola, pantalla, indicador, fuente, -0.95f, 0.3f);

        PuestoNave ingenieria = CrearPuesto(TipoPuesto.Ingenieria, "Ingeniería", cabina,
            new Vector3(-3.55f, 0.5f, 0.5f), new Vector3(0.9f, 1f, 2.2f), -90f,
            new Vector3(-2.5f, 1.55f, 0.5f), new Vector3(28f, -90f, 0f), consola, pantalla, indicador, fuente, 0f, 0.5f);

        PuestoNave comunicaciones = CrearPuesto(TipoPuesto.Comunicaciones, "Comunicaciones", cabina,
            new Vector3(3.55f, 0.5f, 0.5f), new Vector3(0.9f, 1f, 2.2f), 90f,
            new Vector3(2.5f, 1.55f, 0.5f), new Vector3(28f, 90f, 0f), consola, pantalla, indicador, fuente, 0f, 0.5f);

        PuestoNave ciencia = CrearPuesto(TipoPuesto.Ciencia, "Ciencia", cabina,
            new Vector3(0f, 0.5f, -4.5f), new Vector3(2.4f, 1f, 0.8f), 180f,
            new Vector3(0f, 1.55f, -3.5f), new Vector3(28f, 180f, 0f), consola, pantalla, indicador, fuente, 0f, 0.5f);

        // Palancas de los tres circuitos (ingeniería)
        Renderer[] palancas = new Renderer[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject p = Decoracion(PrimitiveType.Cube, "PalancaCircuito" + (i + 1), ingenieria.transform,
                new Vector3(-3.3f, 1.12f, -0.1f + i * 0.6f), new Vector3(0.12f, 0.25f, 0.12f), indicador);
            palancas[i] = p.GetComponent<Renderer>();
            Etiqueta((i + 1).ToString(), ingenieria.transform, new Vector3(-3.15f, 1.12f, -0.1f + i * 0.6f), -90f, 0.04f, fuente);
        }

        // Sonda en la bahía (solo visual: no lleva scripts de control)
        GameObject sondaBahia = Decoracion(PrimitiveType.Capsule, "SondaEnBahia", ciencia.transform,
            new Vector3(1.05f, 1.25f, -4.6f), new Vector3(0.5f, 0.35f, 0.5f), sondaMat);
        sondaBahia.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // ================================================================ Jugador y cámara
        GameObject jugadorGo = new GameObject("Jugador");
        jugadorGo.transform.position = new Vector3(0f, 0.05f, -1.5f);
        CharacterController cc = jugadorGo.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.3f;
        GameObject ojos = new GameObject("Ojos");
        ojos.transform.SetParent(jugadorGo.transform, false);
        ojos.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        GameObject camGo = new GameObject("Camara");
        camGo.tag = "MainCamera";
        camGo.transform.position = ojos.transform.position;
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.62f, 0.85f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 2500f;
        cam.fieldOfView = 65f;
        camGo.AddComponent<AudioListener>();

        JugadorNave jugador = jugadorGo.AddComponent<JugadorNave>();
        Asignar(jugador, "ojos", ojos.transform);
        Asignar(jugador, "camara", cam);

        // ================================================================ Exterior
        GameObject exteriorGo = new GameObject("Exterior");
        // Plataforma de lanzamiento: suelo y torre (caen durante el ascenso)
        Transform plataforma = new GameObject("PlataformaLanzamiento").transform;
        plataforma.SetParent(exteriorGo.transform, false);
        Decoracion(PrimitiveType.Cube, "Suelo", plataforma, new Vector3(0f, -4f, 40f), new Vector3(500f, 1f, 500f), plataformaMat);
        Decoracion(PrimitiveType.Cube, "Torre", plataforma, new Vector3(6f, 10f, 16f), new Vector3(2f, 28f, 2f), marco);
        Decoracion(PrimitiveType.Cube, "BrazoTorre", plataforma, new Vector3(3.5f, 3f, 14f), new Vector3(5f, 0.4f, 0.6f), marco);
        Decoracion(PrimitiveType.Cube, "EdificioControl", plataforma, new Vector3(-40f, 2f, 120f), new Vector3(20f, 12f, 14f), paredes);

        GameObject luna = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        luna.name = "LunaDestino (ficticia)";
        Object.DestroyImmediate(luna.GetComponent<Collider>());
        luna.transform.SetParent(exteriorGo.transform, false);
        luna.transform.position = new Vector3(30f, 25f, 1800f);
        luna.transform.localScale = Vector3.one * 150f;
        luna.GetComponent<Renderer>().sharedMaterial = lunaMat;

        GameObject estrellasGo = new GameObject("EstrellasViaje");
        estrellasGo.transform.SetParent(exteriorGo.transform, false);
        MeshFilter mfEstrellas = estrellasGo.AddComponent<MeshFilter>();
        MeshRenderer mrEstrellas = estrellasGo.AddComponent<MeshRenderer>();
        mrEstrellas.sharedMaterial = estrella;
        mrEstrellas.shadowCastingMode = ShadowCastingMode.Off;
        mrEstrellas.receiveShadows = false;

        ExteriorNave exterior = exteriorGo.AddComponent<ExteriorNave>();
        Asignar(exterior, "camara", cam);
        Asignar(exterior, "plataforma", plataforma);
        Asignar(exterior, "luna", luna.transform);
        Asignar(exterior, "estrellas", mfEstrellas);
        Asignar(exterior, "materialEscombros", roca);
        Asignar(exterior, "materialSonda", sondaMat);

        // ================================================================ Control y HUD
        GameObject controlGo = new GameObject("ControlNave");
        ControlNave control = controlGo.AddComponent<ControlNave>();
        Asignar(control, "catalogoMision", catalogoMision);
        Asignar(control, "catalogoNave", catalogoNave);
        Asignar(control, "jugador", jugador);
        Asignar(control, "exterior", exterior);
        AsignarLista(control, "puestos", new Object[] { pilotaje, ingenieria, comunicaciones, ciencia });
        AsignarLista(control, "lucesCabina", new Object[] { luz1, luz2 });
        AsignarLista(control, "palancasIngenieria", palancas);

        HudNave hud = controlGo.AddComponent<HudNave>();
        Asignar(hud, "control", control);
        Asignar(hud, "jugador", jugador);

        EditorSceneManager.SaveScene(escena, RutaEscena);
        AnadirACompilacion();
        Selection.activeGameObject = controlGo;
        Debug.Log("[Prototipo] Escena Nave creada y añadida a la lista de compilación después de Configuracion. " +
                  "El modo de prueba se activa en ControlNave (Inspector).");
    }

    // ------------------------------------------------------------------ Piezas

    static PuestoNave CrearPuesto(TipoPuesto tipo, string nombre, Transform padre,
                                  Vector3 posConsola, Vector3 tamConsola, float giroY,
                                  Vector3 posVista, Vector3 rotVista,
                                  Material consola, Material pantalla, Material indicador, Font fuente,
                                  float lateralIndicador, float altoPantalla)
    {
        GameObject raiz = new GameObject("Puesto" + nombre.Replace("í", "i"));
        raiz.transform.SetParent(padre, false);

        // La consola tiene colisionador: es lo que el jugador mira para usar el puesto.
        Bloque("Consola", raiz.transform, posConsola, tamConsola, consola);

        // Pantalla inclinada sobre la consola, mirando hacia el jugador. Tiene colisionador
        // para que mirarla también active el puesto. En pilotaje es baja para no tapar la ventana.
        Quaternion giro = Quaternion.Euler(0f, giroY, 0f);
        Vector3 haciaJugador = giro * Vector3.back;
        Vector3 lateral = giro * Vector3.right * lateralIndicador;
        Vector3 posPantalla = posConsola + Vector3.up * (0.5f + altoPantalla * 0.5f) + haciaJugador * 0.05f;
        GameObject p = Bloque("Pantalla", raiz.transform, posPantalla,
                              new Vector3(Mathf.Min(1.4f, Mathf.Max(tamConsola.x, tamConsola.z) * 0.6f), altoPantalla, 0.05f), pantalla);
        p.transform.localRotation = giro * Quaternion.Euler(-15f, 0f, 0f);

        // Indicador y rótulo (en pilotaje, desplazados a un lado para no tapar la vista).
        float altoIndicador = 0.5f + altoPantalla + 0.12f;
        GameObject luz = Decoracion(PrimitiveType.Sphere, "Indicador", raiz.transform,
                                    posConsola + Vector3.up * altoIndicador + haciaJugador * 0.05f + lateral, Vector3.one * 0.16f, indicador);

        Etiqueta(nombre.ToUpperInvariant(), raiz.transform,
                 posConsola + Vector3.up * (altoIndicador + 0.2f) + haciaJugador * 0.05f + lateral, giroY, 0.05f, fuente);

        GameObject vista = new GameObject("Vista");
        vista.transform.SetParent(raiz.transform, false);
        vista.transform.localPosition = posVista;
        vista.transform.localEulerAngles = rotVista;

        PuestoNave puesto = raiz.AddComponent<PuestoNave>();
        SerializedObject so = new SerializedObject(puesto);
        so.FindProperty("tipo").enumValueIndex = (int)tipo;
        so.FindProperty("nombreVisible").stringValue = nombre;
        so.FindProperty("vista").objectReferenceValue = vista.transform;
        so.FindProperty("indicador").objectReferenceValue = luz.GetComponent<Renderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return puesto;
    }

    /// <summary>Pieza sólida con colisionador (paredes, suelo, consolas).</summary>
    static GameObject Bloque(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material m)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    /// <summary>Pieza decorativa sin colisionador.</summary>
    static GameObject Decoracion(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material m)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static void Etiqueta(string texto, Transform padre, Vector3 pos, float giroY, float tam, Font fuente)
    {
        if (fuente == null) return;
        GameObject go = new GameObject("Etiqueta_" + texto);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        // TextMesh se lee mirando en la dirección +z de su objeto: se gira como el jugador que lo mira.
        go.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = texto;
        tm.font = fuente;
        tm.fontSize = 48;
        tm.characterSize = tam;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.75f, 0.95f, 1f);
        go.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
    }

    static Light LuzCabina(string nombre, Transform padre, Vector3 pos)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 10f;
        l.intensity = 2.5f;
        l.color = new Color(0.92f, 0.96f, 1f);
        l.shadows = LightShadows.None;
        return l;
    }

    // ------------------------------------------------------------------ Datos y referencias

    static CatalogoNave ObtenerCatalogoNave()
    {
        CatalogoNave c = AssetDatabase.LoadAssetAtPath<CatalogoNave>(RutaCatalogoNave);
        if (c != null) return c;
        c = ScriptableObject.CreateInstance<CatalogoNave>();
        AssetDatabase.CreateAsset(c, RutaCatalogoNave);
        AssetDatabase.SaveAssets();
        return c;
    }

    static void Asignar(Object destino, string campo, Object valor)
    {
        SerializedObject so = new SerializedObject(destino);
        SerializedProperty p = so.FindProperty(campo);
        if (p == null) { Debug.LogError($"[Prototipo] No existe el campo {campo} en {destino.GetType().Name}"); return; }
        p.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AsignarLista(Object destino, string campo, Object[] valores)
    {
        SerializedObject so = new SerializedObject(destino);
        SerializedProperty p = so.FindProperty(campo);
        if (p == null) { Debug.LogError($"[Prototipo] No existe el campo {campo} en {destino.GetType().Name}"); return; }
        p.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
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
        }
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static void CrearCarpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + nombre))
            AssetDatabase.CreateFolder(padre, nombre);
    }

    static void AnadirACompilacion()
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == RutaEscena);
        int indice = lista.FindIndex(s => s.path.EndsWith("/Configuracion.unity"));
        lista.Insert(indice >= 0 ? indice + 1 : lista.Count, new EditorBuildSettingsScene(RutaEscena, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
