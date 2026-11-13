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
/// "Prototipo → 16. Crear escena ExpedicionMVP (juego completo)":
///   - Crea la escena NUEVA Assets/_Proyecto/Scenes/ExpedicionMVP.unity con un único objeto «JuegoMVP» (todo lo demás se
///     construye al pulsar Play). Si ya existe, NO la sustituye. No abre ni modifica ninguna otra escena.
///   - Crea tres materiales en Materials/MVP para que el ejecutable incluya los sombreadores que se usan al construir.
///   - NO toca la lista de escenas del build ni los paquetes.
///
/// "Prototipo → 17. Preparar el ejecutable del MVP (lista de escenas)":
///   - Paso EXPLÍCITO: pone ExpedicionMVP como PRIMERA escena del build (escena de entrada) y conserva las demás detrás,
///     tal como estaban. Guarda la lista anterior en CopiasSeguridad/ por si hay que restaurarla.
///
/// "Prototipo → 18. Generar el ejecutable del MVP (Windows)":
///   - Compila SOLO la escena ExpedicionMVP en la carpeta Ejecutable/ del proyecto. No cambia la lista de escenas.
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

    [MenuItem("Prototipo/16. Crear escena ExpedicionMVP (juego completo)")]
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
        // Tabla de modelos (opcional): si existe se asigna; si no, todo provisional (menú 19 la crea).
        so.FindProperty("recursosVisuales").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecursosVisualesEM4X>("Assets/_Proyecto/Data/RecursosVisualesEM4X.asset");
        so.ApplyModifiedPropertiesWithoutUndo();

        if (EditorSceneManager.SaveScene(escena, Ruta))
            Debug.Log("[MVP] Escena creada en " + Ruta + ". Pulsa Play para probar el juego completo.");
        else
            Debug.LogError("[MVP] No se pudo guardar la escena en " + Ruta + ".");
    }

    [MenuItem("Prototipo/17. Preparar el ejecutable del MVP (lista de escenas)")]
    public static void PrepararLista()
    {
        if (!File.Exists(Ruta))
        {
            EditorUtility.DisplayDialog("Falta la escena", "Crea antes la escena con el menú 16.", "Aceptar");
            return;
        }
        List<EditorBuildSettingsScene> actual = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string listado = "";
        foreach (EditorBuildSettingsScene s in actual) listado += (s.enabled ? "[x] " : "[ ] ") + s.path + "\n";
        if (actual.Count > 0 && actual[0].path == Ruta && actual[0].enabled)
        {
            EditorUtility.DisplayDialog("Ya está preparada", "ExpedicionMVP ya es la primera escena del build:\n\n" + listado, "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Preparar la lista de escenas del build",
                "Se pondrá ExpedicionMVP como PRIMERA escena (escena de entrada del ejecutable). Las demás se conservan detrás, igual que ahora:\n\n" +
                listado + "\nLa lista actual se guarda en CopiasSeguridad/ListaEscenasBuild_antes_MVP.txt.", "Cambiar la lista", "Cancelar"))
            return;

        Directory.CreateDirectory("CopiasSeguridad");
        File.WriteAllText("CopiasSeguridad/ListaEscenasBuild_antes_MVP.txt", listado);
        List<EditorBuildSettingsScene> nueva = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(Ruta, true) };
        foreach (EditorBuildSettingsScene s in actual) if (s.path != Ruta) nueva.Add(s);
        EditorBuildSettings.scenes = nueva.ToArray();
        Debug.Log("[MVP] Lista de escenas del build actualizada: ExpedicionMVP es la escena de entrada. Lista anterior guardada en CopiasSeguridad/.");
    }

    [MenuItem("Prototipo/18. Generar el ejecutable del MVP (Windows)")]
    public static void Generar()
    {
        if (!File.Exists(Ruta))
        {
            EditorUtility.DisplayDialog("Falta la escena", "Crea antes la escena con el menú 16.", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Generar el ejecutable",
                "Se compilará SOLO la escena ExpedicionMVP para Windows en la carpeta Ejecutable/ExpedicionEM4X/ del proyecto.\n" +
                "No se cambia la lista de escenas del build. Puede tardar varios minutos.", "Generar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = new[] { Ruta },
            locationPathName = "Ejecutable/ExpedicionEM4X/ExpedicionEM4X.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        UnityEditor.Build.Reporting.BuildReport informe = BuildPipeline.BuildPlayer(opciones);
        if (informe.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[MVP] Ejecutable generado en Ejecutable/ExpedicionEM4X/ (" + informe.summary.totalSize / (1024 * 1024) + " MB).");
            EditorUtility.RevealInFinder("Ejecutable/ExpedicionEM4X/ExpedicionEM4X.exe");
        }
        else
            Debug.LogError("[MVP] No se pudo generar el ejecutable: " + informe.summary.result + " (" + informe.summary.totalErrors + " errores). Mira la consola.");
    }
}
