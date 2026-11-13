using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego) — bloque provisional de la nave de expedición modular EM-4X.
///
/// Menú "Prototipo → 14. Crear escena ExpedicionPrueba (nave modular EM-4X)":
///   - Crea la escena NUEVA Assets/_Proyecto/Scenes/ExpedicionPrueba.unity con un único objeto «PruebaExpedicion».
///     La nave, la cámara y los paneles se crean al pulsar Play.
///   - Si la escena ya existe, NO la sustituye: solo avisa.
///   - No abre ni modifica ninguna otra escena (HangarPrueba, Nave, VueloPrueba, Configuración) y no toca la lista de
///     escenas del build ni los paquetes.
/// </summary>
public static class CrearEscenaExpedicionPrueba
{
    const string Ruta = "Assets/_Proyecto/Scenes/ExpedicionPrueba.unity";

    [MenuItem("Prototipo/14. Crear escena ExpedicionPrueba (nave modular EM-4X)")]
    public static void Crear()
    {
        if (File.Exists(Ruta))
        {
            EditorUtility.DisplayDialog("ExpedicionPrueba ya existe",
                "La escena " + Ruta + " ya existe. Este menú no la sustituye.\nÁbrela desde la carpeta Scenes.", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Crear ExpedicionPrueba",
                "Se creará la escena NUEVA " + Ruta + " con un objeto «PruebaExpedicion».\n\n" +
                "No se abre ni se modifica ninguna otra escena (HangarPrueba, Nave, VueloPrueba y Configuración quedan igual) " +
                "y no se toca la lista de compilación. La escena que tengas abierta se cerrará (se te preguntará si quieres guardarla).",
                "Crear", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("PruebaExpedicion").AddComponent<PruebaExpedicion>();

        if (EditorSceneManager.SaveScene(escena, Ruta))
            Debug.Log("[Expedición] Escena creada en " + Ruta + ". Pulsa Play para recorrer el bloque provisional EM-4X.");
        else
            Debug.LogError("[Expedición] No se pudo guardar la escena en " + Ruta + ".");
    }
}
