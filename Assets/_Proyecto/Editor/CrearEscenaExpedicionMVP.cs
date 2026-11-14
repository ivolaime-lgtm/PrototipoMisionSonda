using System.Collections.Generic;
using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTAS DE EDITOR (no forman parte del juego) — MVP de la expedición.
///
/// "Prototipo → Juego → 2. Crear escena ExpedicionMVP" (antes 16):
///   - Crea la escena NUEVA Assets/_Proyecto/Scenes/ExpedicionMVP.unity con un único objeto «JuegoMVP» (todo lo demás se
///     construye al pulsar Play). Si ya existe, NO la sustituye. No abre ni modifica ninguna otra escena.
///   - Crea tres materiales en Materials/MVP para que el ejecutable incluya los sombreadores que se usan al construir.
///   - NO toca la lista de escenas del build ni los paquetes.
///
/// "Prototipo → Juego → 5. Preparar la lista de escenas del build (Hangar + MVP)" (antes 17):
///   - Paso EXPLÍCITO: pone HangarExpedicion (entrada) y ExpedicionMVP como primeras escenas del build y conserva las
///     demás detrás, tal como estaban. Guarda la lista anterior en CopiasSeguridad/ por si hay que restaurarla.
///
/// "Prototipo → Juego → 6. Generar el ejecutable completo (Windows)" (antes 18):
///   - Compila con SU PROPIA lista (EscenasJuego): HangarExpedicion (entrada) y ExpedicionMVP, las únicas que se cargan
///     durante la partida (CargadorEscenas.Hangar y CargadorEscenas.Expedicion). Ninguna escena de pruebas.
///     No cambia la lista de escenas del editor. Salida: Ejecutable/ExpedicionEM4X_Completo/.
///   - La configuración anterior (solo ExpedicionMVP → Ejecutable/ExpedicionEM4X/) se conserva en el menú
///     «Juego → 7. Ejecutable anterior: solo ExpedicionMVP» y se describe en CopiasSeguridad/ConfiguracionEjecutable_anterior.txt.
/// </summary>
public static class CrearEscenaExpedicionMVP
{
    const string Ruta = "Assets/_Proyecto/Scenes/ExpedicionMVP.unity";
    const string CarpetaMateriales = "Assets/_Proyecto/Materials/MVP";
    const string RutaCatalogo = "Assets/_Proyecto/Data/CatalogoMision.asset";

    static Material CrearMaterial(string nombre, string sombreador, bool transparente)
    {
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;
        Shader sh = Shader.Find(sombreador);
        if (sh == null) { Debug.LogWarning("[MVP] No se encontró el sombreador " + sombreador + "."); return null; }
        m = new Material(sh);
        if (transparente && m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    [MenuItem("Prototipo/Juego/2. Crear escena ExpedicionMVP (antes 16)", false, 2)]
    public static void Crear()
    {
        if (File.Exists(Ruta))
        {
            EditorUtility.DisplayDialog("ExpedicionMVP ya existe",
                "La escena " + Ruta + " ya existe. Este menú no la sustituye.\nÁbrela desde la carpeta Scenes.", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Crear ExpedicionMVP",
                "Se creará la escena NUEVA " + Ruta + " con un objeto «JuegoMVP» y tres materiales en " + CarpetaMateriales + ".\n\n" +
                "No se abre ni se modifica ninguna otra escena y no se toca la lista de compilación ni los paquetes. " +
                "La escena que tengas abierta se cerrará (se te preguntará si quieres guardarla).", "Crear", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
        Directory.CreateDirectory(CarpetaMateriales);
        AssetDatabase.Refresh();
        Material conLuz = CrearMaterial("MVP_ConLuz", "Universal Render Pipeline/Lit", false);
        Material sinLuz = CrearMaterial("MVP_SinLuz", "Universal Render Pipeline/Unlit", false);
        Material transparente = CrearMaterial("MVP_Transparente", "Universal Render Pipeline/Unlit", true);
        AssetDatabase.SaveAssets();

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        JuegoMVP juego = new GameObject("JuegoMVP").AddComponent<JuegoMVP>();
        SerializedObject so = new SerializedObject(juego);
        CatalogoMision catalogo = AssetDatabase.LoadAssetAtPath<CatalogoMision>(RutaCatalogo);
        if (catalogo == null) Debug.LogWarning("[MVP] No se encontró " + RutaCatalogo + ": el juego usará los valores por defecto del catálogo.");
        so.FindProperty("catalogoMision").objectReferenceValue = catalogo;
        so.FindProperty("materialConLuz").objectReferenceValue = conLuz;
        so.FindProperty("materialSinLuz").objectReferenceValue = sinLuz;
        so.FindProperty("materialTransparente").objectReferenceValue = transparente;
        // Tabla de modelos (opcional): si existe se asigna; si no, todo provisional (la crea «Arte y pruebas → 1»).
        so.FindProperty("recursosVisuales").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecursosVisualesEM4X>("Assets/_Proyecto/Data/RecursosVisualesEM4X.asset");
        so.ApplyModifiedPropertiesWithoutUndo();

        if (EditorSceneManager.SaveScene(escena, Ruta))
            Debug.Log("[MVP] Escena creada en " + Ruta + ". Pulsa Play para probar el juego completo.");
        else
            Debug.LogError("[MVP] No se pudo guardar la escena en " + Ruta + ".");
    }

    /// <summary>Escenas que se cargan durante la partida, en orden (la primera es la entrada del ejecutable).</summary>
    public static readonly string[] EscenasJuego = { CargadorEscenas.Hangar, CargadorEscenas.Expedicion };
    const string SalidaCompleta = "Ejecutable/ExpedicionEM4X_Completo/ExpedicionEM4X.exe";
    const string SalidaAnterior = "Ejecutable/ExpedicionEM4X/ExpedicionEM4X.exe";

    static bool FaltanEscenas()
    {
        string faltan = "";
        foreach (string e in EscenasJuego) if (!File.Exists(e)) faltan += "\n· " + e;
        if (faltan.Length == 0) return false;
        EditorUtility.DisplayDialog("Faltan escenas del juego", "No existen:" + faltan +
            "\n\nCréalas con «Prototipo → Juego → 1» (HangarExpedicion) y «Prototipo → Juego → 2» (ExpedicionMVP).", "Aceptar");
        return true;
    }

    /// <summary>Guarda (una sola vez) la descripción de la configuración anterior del ejecutable.</summary>
    static void GuardarConfiguracionAnterior()
    {
        const string ruta = "CopiasSeguridad/ConfiguracionEjecutable_anterior.txt";
        if (File.Exists(ruta)) return;
        Directory.CreateDirectory("CopiasSeguridad");
        File.WriteAllText(ruta,
            "Configuración del ejecutable ANTERIOR al hangar terrestre (menú «18. Generar el ejecutable del MVP»):\n" +
            "  escenas: " + Ruta + " (solo esa; entrada directa al MVP)\n" +
            "  salida:  " + SalidaAnterior + "\n" +
            "  destino: StandaloneWindows64, BuildOptions.None\n" +
            "Se puede volver a generar con «Prototipo → Juego → 7. Ejecutable anterior: solo ExpedicionMVP».\n" +
            "Guardado el " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ".\n");
    }

    [MenuItem("Prototipo/Juego/5. Preparar la lista de escenas del build (Hangar + MVP)", false, 20)]
    public static void PrepararLista()
    {
        if (FaltanEscenas()) return;
        List<EditorBuildSettingsScene> actual = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string listado = "";
        foreach (EditorBuildSettingsScene s in actual) listado += (s.enabled ? "[x] " : "[ ] ") + s.path + "\n";
        bool yaEsta = actual.Count >= EscenasJuego.Length;
        for (int i = 0; yaEsta && i < EscenasJuego.Length; i++) yaEsta = actual[i].path == EscenasJuego[i] && actual[i].enabled;
        if (yaEsta)
        {
            EditorUtility.DisplayDialog("Ya está preparada", "HangarExpedicion (entrada) y ExpedicionMVP ya son las primeras escenas del build:\n\n" + listado, "Aceptar");
            return;
        }
        string copia = $"CopiasSeguridad/ListaEscenasBuild_antes_hangar_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        if (!EditorUtility.DisplayDialog("Preparar la lista de escenas del build",
                "Se pondrán HangarExpedicion (ENTRADA) y ExpedicionMVP como primeras escenas, activadas. Las demás se conservan detrás, igual que ahora:\n\n" +
                listado + "\nLa lista actual se guarda en " + copia + ".\n(El menú 6 usa su propia lista y no depende de esta.)", "Cambiar la lista", "Cancelar"))
            return;

        Directory.CreateDirectory("CopiasSeguridad");
        File.WriteAllText(copia, listado);
        List<EditorBuildSettingsScene> nueva = new List<EditorBuildSettingsScene>();
        foreach (string e in EscenasJuego) nueva.Add(new EditorBuildSettingsScene(e, true));
        foreach (EditorBuildSettingsScene s in actual) if (System.Array.IndexOf(EscenasJuego, s.path) < 0) nueva.Add(s);
        EditorBuildSettings.scenes = nueva.ToArray();
        Debug.Log("[Juego] Lista de escenas del build actualizada: HangarExpedicion es la entrada, después ExpedicionMVP. Lista anterior en " + copia + ".");
    }

    [MenuItem("Prototipo/Juego/6. Generar el ejecutable completo (Windows)", false, 21)]
    public static void Generar()
    {
        if (FaltanEscenas()) return;
        if (!EditorUtility.DisplayDialog("Generar el ejecutable completo",
                "Se compilarán, con su propia lista, las escenas que se cargan durante la partida:\n" +
                "  1. HangarExpedicion (entrada)\n  2. ExpedicionMVP\n" +
                "para Windows en " + SalidaCompleta + ". Ninguna escena de pruebas.\n" +
                "No se cambia la lista de escenas del editor ni el ejecutable anterior (Ejecutable/ExpedicionEM4X/). Puede tardar varios minutos.", "Generar", "Cancelar"))
            return;
        GuardarConfiguracionAnterior();
        Compilar(EscenasJuego, SalidaCompleta);
    }

    [MenuItem("Prototipo/Juego/7. Ejecutable anterior: solo ExpedicionMVP (sin hangar)", false, 22)]
    public static void GenerarAnterior()
    {
        if (!File.Exists(Ruta))
        {
            EditorUtility.DisplayDialog("Falta la escena", "Crea antes la escena con «Prototipo → Juego → 2. Crear escena ExpedicionMVP».", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Generar el ejecutable anterior",
                "Configuración ANTERIOR: se compilará SOLO ExpedicionMVP (sin hangar) en " + SalidaAnterior + ".\n" +
                "Sirve para probar el MVP por separado. Puede tardar varios minutos.", "Generar", "Cancelar"))
            return;
        GuardarConfiguracionAnterior();
        Compilar(new[] { Ruta }, SalidaAnterior);
    }

    static void Compilar(string[] escenas, string salida)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = salida,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        UnityEditor.Build.Reporting.BuildReport informe = BuildPipeline.BuildPlayer(opciones);
        if (informe.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[Juego] Ejecutable generado en " + salida + " (" + informe.summary.totalSize / (1024 * 1024) + " MB) con: " + string.Join(", ", escenas));
            EditorUtility.RevealInFinder(salida);
        }
        else
            Debug.LogError("[Juego] No se pudo generar el ejecutable: " + informe.summary.result + " (" + informe.summary.totalErrors + " errores). Mira la consola.");
    }
}
