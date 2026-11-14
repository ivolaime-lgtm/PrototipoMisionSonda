using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego) — prueba V1 de vuelo 3D.
///
/// Menú "Prototipo → 12. Crear escena VueloPrueba (vuelo 3D V1)":
///   - Crea la escena NUEVA Assets/_Proyecto/Scenes/VueloPrueba.unity con un único objeto «PruebaVuelo3D».
///     Todo lo demás (cámara, luz, mundo, nave, HUD) se crea al pulsar Play.
///   - Asigna los catálogos de Data/ para leer masa, combustible y motor principal.
///   - Si la escena ya existe, NO la sustituye: solo avisa.
///   - No abre ni modifica ninguna otra escena y no toca la lista de compilación.
/// </summary>
public static class CrearEscenaVueloPrueba
{
    const string Raiz = "Assets/_Proyecto";
    const string Ruta = Raiz + "/Scenes/VueloPrueba.unity";

    [MenuItem("Prototipo/Arte y pruebas/Crear escenas de prueba/VueloPrueba · vuelo 3D V1 (antes 12)", false, 140)]
    public static void Crear()
    {
        if (File.Exists(Ruta))
        {
            EditorUtility.DisplayDialog("VueloPrueba ya existe",
                "La escena " + Ruta + " ya existe. Este menú no la sustituye.\nÁbrela desde la carpeta Scenes.", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Crear VueloPrueba",
                "Se creará la escena NUEVA " + Ruta + " con un objeto «PruebaVuelo3D».\n\n" +
                "No se abre ni se modifica ninguna otra escena y no se toca la lista de compilación. " +
                "La escena que tengas abierta se cerrará (se te preguntará si quieres guardarla).",
                "Crear", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject go = new GameObject("PruebaVuelo3D");
        PruebaVuelo3D prueba = go.AddComponent<PruebaVuelo3D>();

        SerializedObject so = new SerializedObject(prueba);
        so.FindProperty("catalogoMision").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<CatalogoMision>(Raiz + "/Data/CatalogoMision.asset");
        so.FindProperty("catalogoNave").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<CatalogoNave>(Raiz + "/Data/CatalogoNave.asset");
        so.ApplyModifiedPropertiesWithoutUndo();

        if (EditorSceneManager.SaveScene(escena, Ruta))
            Debug.Log("[VueloPrueba] Escena creada en " + Ruta + ". Pulsa Play para probar el vuelo 3D V1.");
        else
            Debug.LogError("[VueloPrueba] No se pudo guardar la escena en " + Ruta + ".");
    }
}
