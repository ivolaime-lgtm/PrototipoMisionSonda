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
        so.ApplyModifiedPropertiesWithoutUndo();
        if (EditorSceneManager.SaveScene(escena, Ruta))
            Debug.Log("[Hangar] Escena creada en " + Ruta + ". Pulsa Play para recorrer el hangar.");
        else
            Debug.LogError("[Hangar] No se pudo guardar la escena en " + Ruta + ".");
    }
}
