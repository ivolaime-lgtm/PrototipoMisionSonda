using System.Collections.Generic;
using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego) — incremento 4.
///
/// Menú "Prototipo → 5. Crear resultados y conectar Exploracion":
/// - Crea la escena Resultados (pregunta antes de sustituirla si existe).
/// - Abre Exploracion y AÑADE un objeto "ControlMision" (si no existe).
/// - Añade Resultados a la lista de compilación, después de Exploracion.
/// No borra ni modifica ningún otro objeto.
/// </summary>
public static class ConstructorResultados
{
    const string Raiz = "Assets/_Proyecto";
    const string RutaResultados = Raiz + "/Scenes/Resultados.unity";
    const string RutaExploracion = Raiz + "/Scenes/Exploracion.unity";
    const string RutaCatalogo = Raiz + "/Data/CatalogoMision.asset";

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/5. Crear resultados y conectar Exploracion", false, 1004)]
    public static void Crear()
    {
        if (!File.Exists(RutaExploracion))
        {
            EditorUtility.DisplayDialog("Falta la escena",
                "No existe " + RutaExploracion + ".\nCréala antes con el menú 1.", "Aceptar");
            return;
        }

        bool existeResultados = File.Exists(RutaResultados);
        string aviso = existeResultados
            ? "La escena Resultados ya existe y se SUSTITUIRÁ por una nueva.\n\n"
            : "Se creará la escena Resultados.\n\n";

        if (!EditorUtility.DisplayDialog(
                "Crear resultados",
                aviso +
                "Después se abrirá Exploracion y se AÑADIRÁ un objeto «ControlMision» (si no existe).\n" +
                "No se borra ni se modifica ningún otro objeto. Las escenas se guardarán.",
                "Continuar", "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // --- Escena Resultados
        Scene resultados = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject camGo = new GameObject("Camara");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.08f, 0.11f);
        camGo.AddComponent<AudioListener>();
        new GameObject("PantallaResultados").AddComponent<PantallaResultados>();
        EditorSceneManager.SaveScene(resultados, RutaResultados);

        // --- ControlMision en Exploracion
        Scene exploracion = EditorSceneManager.OpenScene(RutaExploracion, OpenSceneMode.Single);
        ControlMision control = Object.FindAnyObjectByType<ControlMision>();
        bool nuevo = control == null;
        if (nuevo)
            control = new GameObject("ControlMision").AddComponent<ControlMision>();

        SistemaEnergia energia = Object.FindAnyObjectByType<SistemaEnergia>();
        EstacionSegura estacion = Object.FindAnyObjectByType<EstacionSegura>();

        SerializedObject so = new SerializedObject(control);
        AsignarSiVacio(so, "catalogo", AssetDatabase.LoadAssetAtPath<CatalogoMision>(RutaCatalogo));
        AsignarSiVacio(so, "energia", energia);
        AsignarSiVacio(so, "datos", Object.FindAnyObjectByType<SistemaDatos>());
        AsignarSiVacio(so, "escaner", Object.FindAnyObjectByType<Escaner>());
        AsignarSiVacio(so, "aplicador", Object.FindAnyObjectByType<AplicadorConfiguracion>());
        AsignarSiVacio(so, "controlador", Object.FindAnyObjectByType<ControladorSonda>());
        AsignarSiVacio(so, "estacion", estacion != null ? estacion.transform : null);
        so.ApplyModifiedPropertiesWithoutUndo();

        if (Object.FindAnyObjectByType<SistemaDatos>() == null)
            Debug.LogWarning("[Prototipo] Exploracion no tiene SistemaCiencia. Ejecuta antes el menú 4.");

        EditorSceneManager.MarkSceneDirty(exploracion);
        EditorSceneManager.SaveScene(exploracion);

        AgregarAEscenasDeCompilacion();

        Selection.activeGameObject = control.gameObject;
        Debug.Log("[Prototipo] Escena Resultados creada y " +
                  (nuevo ? "ControlMision añadido a Exploracion." : "ControlMision ya existía; solo se completaron referencias vacías."));
    }

    static void AsignarSiVacio(SerializedObject so, string propiedad, Object valor)
    {
        SerializedProperty p = so.FindProperty(propiedad);
        if (p != null && p.objectReferenceValue == null && valor != null)
            p.objectReferenceValue = valor;
    }

    /// <summary>Añade Resultados justo después de Exploracion, conservando el resto.</summary>
    static void AgregarAEscenasDeCompilacion()
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == RutaResultados);
        int i = lista.FindIndex(s => s.path == RutaExploracion);
        lista.Insert(i >= 0 ? i + 1 : lista.Count, new EditorBuildSettingsScene(RutaResultados, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
