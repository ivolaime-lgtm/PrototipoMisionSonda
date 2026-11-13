using System.Collections.Generic;
using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTAS DE EDITOR (no forman parte del juego) — incremento 2.
///
/// Menú "Prototipo → 2. Crear escena de configuración":
///   crea el catálogo (si no existe) y la escena Configuracion.
///
/// Menú "Prototipo → 3. Conectar configuración a Exploracion":
///   abre Exploracion y AÑADE un objeto AplicadorConfiguracion.
///   No borra ni modifica ningún otro objeto.
/// </summary>
public static class ConstructorConfiguracion
{
    const string Raiz = "Assets/_Proyecto";
    const string RutaCatalogo = Raiz + "/Data/CatalogoMision.asset";
    const string RutaConfiguracion = Raiz + "/Scenes/Configuracion.unity";
    const string RutaExploracion = Raiz + "/Scenes/Exploracion.unity";

    [MenuItem("Prototipo/2. Crear escena de configuración")]
    public static void CrearEscenaConfiguracion()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (File.Exists(RutaConfiguracion) &&
            !EditorUtility.DisplayDialog(
                "La escena ya existe",
                "Ya existe " + RutaConfiguracion + ".\n\n¿Quieres sustituirla por una nueva?\n" +
                "El catálogo de misión NO se modifica.",
                "Sustituir escena", "Cancelar"))
            return;

        CatalogoMision catalogo = ObtenerCatalogo();

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGo = new GameObject("Camara");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.08f, 0.11f);
        camGo.AddComponent<AudioListener>();

        GameObject pantallaGo = new GameObject("PantallaConfiguracion");
        PantallaConfiguracion pantalla = pantallaGo.AddComponent<PantallaConfiguracion>();
        SerializedObject so = new SerializedObject(pantalla);
        so.FindProperty("catalogo").objectReferenceValue = catalogo;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(escena, RutaConfiguracion);
        OrdenarEscenasDeCompilacion();

        Selection.activeObject = catalogo;
        Debug.Log("[Prototipo] Escena de configuración creada en " + RutaConfiguracion +
                  ". Siguiente paso: menú Prototipo → 3. Conectar configuración a Exploracion.");
    }

    [MenuItem("Prototipo/3. Conectar configuración a Exploracion")]
    public static void ConectarExploracion()
    {
        if (!File.Exists(RutaExploracion))
        {
            EditorUtility.DisplayDialog("Falta la escena",
                "No existe " + RutaExploracion + ".\nCréala antes con el menú 1.", "Aceptar");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Conectar configuración",
                "Se abrirá la escena Exploracion y se AÑADIRÁ un objeto llamado «AplicadorConfiguracion».\n\n" +
                "No se borra ni se modifica ningún otro objeto. Después se guardará la escena.",
                "Continuar", "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        CatalogoMision catalogo = ObtenerCatalogo();
        Scene escena = EditorSceneManager.OpenScene(RutaExploracion, OpenSceneMode.Single);

        AplicadorConfiguracion aplicador = Object.FindAnyObjectByType<AplicadorConfiguracion>();
        bool nuevo = aplicador == null;
        if (nuevo)
            aplicador = new GameObject("AplicadorConfiguracion").AddComponent<AplicadorConfiguracion>();

        SerializedObject so = new SerializedObject(aplicador);
        AsignarSiVacio(so, "catalogo", catalogo);
        AsignarSiVacio(so, "energia", Object.FindAnyObjectByType<SistemaEnergia>());
        AsignarSiVacio(so, "controlador", Object.FindAnyObjectByType<ControladorSonda>());
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        OrdenarEscenasDeCompilacion();

        Selection.activeGameObject = aplicador.gameObject;
        Debug.Log(nuevo
            ? "[Prototipo] Añadido AplicadorConfiguracion a Exploracion y escena guardada."
            : "[Prototipo] Exploracion ya tenía AplicadorConfiguracion; solo se completaron referencias vacías.");
    }

    // ------------------------------------------------------------------ Utilidades

    static CatalogoMision ObtenerCatalogo()
    {
        CatalogoMision existente = AssetDatabase.LoadAssetAtPath<CatalogoMision>(RutaCatalogo);
        if (existente != null) return existente;

        if (!AssetDatabase.IsValidFolder(Raiz + "/Data"))
            AssetDatabase.CreateFolder(Raiz, "Data");
        if (!AssetDatabase.IsValidFolder(Raiz + "/Scenes"))
            AssetDatabase.CreateFolder(Raiz, "Scenes");

        CatalogoMision nuevo = ScriptableObject.CreateInstance<CatalogoMision>();
        AssetDatabase.CreateAsset(nuevo, RutaCatalogo);
        AssetDatabase.SaveAssets();
        Debug.Log("[Prototipo] Catálogo de misión creado en " + RutaCatalogo + " con valores de DEMOSTRACIÓN.");
        return nuevo;
    }

    static void AsignarSiVacio(SerializedObject so, string propiedad, Object valor)
    {
        SerializedProperty p = so.FindProperty(propiedad);
        if (p != null && p.objectReferenceValue == null && valor != null)
            p.objectReferenceValue = valor;
    }

    /// <summary>Configuracion primero, Exploracion después; conserva el resto de escenas.</summary>
    static void OrdenarEscenasDeCompilacion()
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == RutaConfiguracion || s.path == RutaExploracion);

        int posicion = 0;
        if (File.Exists(RutaConfiguracion))
            lista.Insert(posicion++, new EditorBuildSettingsScene(RutaConfiguracion, true));
        if (File.Exists(RutaExploracion))
            lista.Insert(posicion, new EditorBuildSettingsScene(RutaExploracion, true));

        EditorBuildSettings.scenes = lista.ToArray();
    }
}
