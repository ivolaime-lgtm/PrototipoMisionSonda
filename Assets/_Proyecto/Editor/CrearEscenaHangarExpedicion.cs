using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR — hangar terrestre jugable.
///
/// "Prototipo → Juego → 1. Hangar terrestre: crear o abrir HangarExpedicion (ENTRADA del juego)" (antes menú 22)
///   - Si la escena NO existe: crea Assets/_Proyecto/Scenes/HangarExpedicion.unity con un objeto «HangarTerrestre»
///     (el hangar se construye al pulsar Play) y le asigna el catálogo y los materiales del MVP si existen.
///   - Si YA existe: solo la abre (nunca la sustituye).
///   - No toca HangarPrueba, ExpedicionMVP, otras escenas, paquetes ni la lista del build.
/// </summary>
public static class CrearEscenaHangarExpedicion
{
    const string Ruta = CargadorEscenas.Hangar;
    const string RutaTabla = "Assets/_Proyecto/Data/RecursosVisualesEM4X.asset";

    /// <summary>
    /// Conecta la tabla de sustitución visual (la de la EM-4X, con entradas «Escenario») al HangarTerrestre de la escena
    /// existente. Solo asigna esa referencia y guarda la escena (con confirmación); no cambia nada más.
    /// </summary>
    [MenuItem("Prototipo/Arte y pruebas/5. Conectar la tabla de sustitución al hangar (HangarExpedicion)", false, 111)]
    public static void ConectarTabla()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Sal del modo Play", "Usa este menú fuera del modo Play.", "Aceptar"); return; }
        RecursosVisualesEM4X tabla = AssetDatabase.LoadAssetAtPath<RecursosVisualesEM4X>(RutaTabla);
        if (tabla == null) { EditorUtility.DisplayDialog("Falta la tabla", "No existe " + RutaTabla + ".\nCréala con «Arte y pruebas → 1».", "Aceptar"); return; }
        if (!File.Exists(Ruta)) { EditorUtility.DisplayDialog("Falta la escena", "No existe " + Ruta + ".\nCréala con «Juego → 1».", "Aceptar"); return; }
        if (!EditorUtility.DisplayDialog("Conectar la tabla de sustitución",
                "Se abrirá " + Ruta + ", se asignará la tabla " + RutaTabla + " al objeto HangarTerrestre y se guardará la escena.\n" +
                "No se cambia nada más. (Copia de la escena en CopiasSeguridad/Copia_validada_AE_AF_2026-11-13.zip.)", "Conectar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene escena = EditorSceneManager.OpenScene(Ruta, OpenSceneMode.Single);
        HangarTerrestre hangar = Object.FindFirstObjectByType<HangarTerrestre>();
        if (hangar == null) { EditorUtility.DisplayDialog("Sin HangarTerrestre", "La escena no tiene el objeto HangarTerrestre.", "Aceptar"); return; }
        SerializedObject so = new SerializedObject(hangar);
        SerializedProperty prop = so.FindProperty("recursosVisuales");
        if (prop.objectReferenceValue == tabla) { Debug.Log("[Hangar] La tabla ya estaba conectada; no se ha modificado la escena."); return; }
        prop.objectReferenceValue = tabla;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(escena);
        if (EditorSceneManager.SaveScene(escena)) Debug.Log("[Hangar] Tabla de sustitución conectada a HangarExpedicion.");
    }

    [MenuItem("Prototipo/Juego/1. Hangar terrestre: crear o abrir HangarExpedicion (ENTRADA del juego)", false, 1)]
    public static void CrearOAbrir()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sal del modo Play", "Usa este menú fuera del modo Play.", "Aceptar");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(Ruta))
        {
            EditorSceneManager.OpenScene(Ruta, OpenSceneMode.Single);
            Debug.Log("[Hangar] Escena abierta: " + Ruta + " (ya existía; no se ha modificado).");
            return;
        }
        if (!EditorUtility.DisplayDialog("Crear HangarExpedicion",
                "Se creará la escena NUEVA " + Ruta + " con un objeto «HangarTerrestre».\n\n" +
                "No se modifica HangarPrueba, ExpedicionMVP ni ninguna otra escena, ni la lista de compilación.", "Crear", "Cancelar"))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        HangarTerrestre hangar = new GameObject("HangarTerrestre").AddComponent<HangarTerrestre>();
        SerializedObject so = new SerializedObject(hangar);
        so.FindProperty("catalogoMision").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CatalogoMision>("Assets/_Proyecto/Data/CatalogoMision.asset");
        so.FindProperty("materialConLuz").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Materials/MVP/MVP_ConLuz.mat");
        so.FindProperty("materialSinLuz").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Materials/MVP/MVP_SinLuz.mat");
        so.FindProperty("materialTransparente").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Materials/MVP/MVP_Transparente.mat");
        so.FindProperty("recursosVisuales").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecursosVisualesEM4X>(RutaTabla);
        so.ApplyModifiedPropertiesWithoutUndo();
        if (EditorSceneManager.SaveScene(escena, Ruta))
            Debug.Log("[Hangar] Escena creada en " + Ruta + ". Pulsa Play para recorrer el hangar.");
        else
            Debug.LogError("[Hangar] No se pudo guardar la escena en " + Ruta + ".");
    }
}
