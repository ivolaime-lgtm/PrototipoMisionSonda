using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego) — prueba de circulación interior en la EM-4X aceptada.
///
/// Menú "Prototipo → 15. Crear escena CirculacionPrueba (personaje, pasadizos y puertas)":
///   - Crea la escena NUEVA Assets/_Proyecto/Scenes/CirculacionPrueba.unity con un único objeto «PruebaCirculacion».
///     La nave, las colisiones, las puertas, el personaje y las cámaras se crean al pulsar Play.
///   - Si la escena ya existe, NO la sustituye: solo avisa.
///   - No abre ni modifica ninguna otra escena (ExpedicionPrueba, HangarPrueba, Nave, VueloPrueba, Configuración) y no
///     toca la lista de escenas del build ni los paquetes.
/// </summary>
public static class CrearEscenaCirculacionPrueba
{
    const string Ruta = "Assets/_Proyecto/Scenes/CirculacionPrueba.unity";

    [MenuItem("Prototipo/15. Crear escena CirculacionPrueba (personaje, pasadizos y puertas)")]
    public static void Crear()
    {
        if (File.Exists(Ruta))
        {
            EditorUtility.DisplayDialog("CirculacionPrueba ya existe",
                "La escena " + Ruta + " ya existe. Este menú no la sustituye.\nÁbrela desde la carpeta Scenes.", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Crear CirculacionPrueba",
                "Se creará la escena NUEVA " + Ruta + " con un objeto «PruebaCirculacion».\n\n" +
                "No se abre ni se modifica ninguna otra escena (ExpedicionPrueba, HangarPrueba, Nave, VueloPrueba y Configuración " +
                "quedan igual) y no se toca la lista de compilación. La escena que tengas abierta se cerrará (se te preguntará si " +
                "quieres guardarla).", "Crear", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("PruebaCirculacion").AddComponent<PruebaCirculacion>();

        if (EditorSceneManager.SaveScene(escena, Ruta))
            Debug.Log("[Circulación] Escena creada en " + Ruta + ". Pulsa Play para probar el personaje, los pasadizos y las puertas.");
        else
            Debug.LogError("[Circulación] No se pudo guardar la escena en " + Ruta + ".");
    }
}
