using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego) — prueba de hangar con nave base modular.
///
/// Menú "Prototipo → 13. Crear escena HangarPrueba (nave base y bloque interior)":
///   - Crea la escena NUEVA Assets/_Proyecto/Scenes/HangarPrueba.unity con un único objeto «PruebaHangar».
///     El hangar, la nave, la cámara y los paneles se crean al pulsar Play.
///   - Asigna el CatalogoMision de Data/ (que ya enlaza el CatalogoNave).
///   - Si la escena ya existe, NO la sustituye: solo avisa.
///   - No abre ni modifica ninguna otra escena (ni Nave ni VueloPrueba) y no toca la lista de compilación.
/// </summary>
public static class CrearEscenaHangarPrueba
{
    const string Raiz = "Assets/_Proyecto";
    const string Ruta = Raiz + "/Scenes/HangarPrueba.unity";

    [MenuItem("Prototipo/13. Crear escena HangarPrueba (nave base y bloque interior)")]
    public static void Crear()
    {
        if (File.Exists(Ruta))
        {
            EditorUtility.DisplayDialog("HangarPrueba ya existe",
                "La escena " + Ruta + " ya existe. Este menú no la sustituye.\nÁbrela desde la carpeta Scenes.", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Crear HangarPrueba",
                "Se creará la escena NUEVA " + Ruta + " con un objeto «PruebaHangar».\n\n" +
                "No se abre ni se modifica ninguna otra escena (Nave y VueloPrueba quedan igual) y no se toca la lista de compilación. " +
                "La escena que tengas abierta se cerrará (se te preguntará si quieres guardarla).",
                "Crear", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject go = new GameObject("PruebaHangar");
        PruebaHangar prueba = go.AddComponent<PruebaHangar>();
        SerializedObject so = new SerializedObject(prueba);
        so.FindProperty("catalogo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<CatalogoMision>(Raiz + "/Data/CatalogoMision.asset");
        so.ApplyModifiedPropertiesWithoutUndo();

        if (EditorSceneManager.SaveScene(escena, Ruta))
            Debug.Log("[Hangar] Escena creada en " + Ruta + ". Pulsa Play para probar la nave base y el bloque interior.");
        else
            Debug.LogError("[Hangar] No se pudo guardar la escena en " + Ruta + ".");
    }
}
