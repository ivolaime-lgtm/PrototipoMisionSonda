using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTAS DE EDITOR para el arte de la EM-4X (no forman parte del juego). Ninguna sustituye escenas ni archivos.
///
/// 19. Preparar carpetas de arte y tabla de sustitución
///     Crea Assets/_Arte/{Nave,Interior,Utileria,Hangar}/{Modelos,Materiales,Texturas,Prefabs} y, si no existe,
///     Assets/_Proyecto/Data/RecursosVisualesEM4X.asset. Si la escena abierta tiene JuegoMVP o PruebaModelos sin tabla,
///     se la asigna (la escena queda modificada; guárdala tú).
/// 20. Crear escena ModeloPrueba (importar una pieza)
///     Escena NUEVA con un objeto «PruebaModelos». Si ya existe, no la sustituye.
/// 21. Comprobar recursos de arte (_Arte)
///     Revisa nombres, escala de importación, tamaño, colisiones, cámaras/luces y materiales. Escribe Informes/ComprobacionArte_<fecha>.txt.
/// </summary>
public static class HerramientasArteEM4X
{
    const string RutaTabla = "Assets/_Proyecto/Data/RecursosVisualesEM4X.asset";
    const string RutaEscena = "Assets/_Proyecto/Scenes/ModeloPrueba.unity";
    static readonly string[] Bloques = { "Nave", "Interior", "Utileria", "Hangar" };
    static readonly string[] Subcarpetas = { "Modelos", "Materiales", "Texturas", "Prefabs" };

    static RecursosVisualesEM4X Tabla(bool crear)
    {
        RecursosVisualesEM4X t = AssetDatabase.LoadAssetAtPath<RecursosVisualesEM4X>(RutaTabla);
        if (t != null || !crear) return t;
        if (File.Exists(RutaTabla))
        {
            // Existe pero no se carga como tabla (script con errores u otro tipo): no se sustituye.
            EditorUtility.DisplayDialog("No se toca la tabla",
                RutaTabla + " existe pero no se puede cargar como tabla de recursos. No se sustituye: revisa la consola.", "Aceptar");
            return null;
        }
        t = ScriptableObject.CreateInstance<RecursosVisualesEM4X>();
        AssetDatabase.CreateAsset(t, RutaTabla);
        AssetDatabase.SaveAssets();
        Debug.Log("[Arte EM-4X] Tabla creada en " + RutaTabla + " con las cuatro entradas de la PRUEBA 1 (sin modelos: todo provisional).");
        return t;
    }

    [MenuItem("Prototipo/Arte y pruebas/1. Preparar carpetas de arte y tabla de sustitución (antes 19)", false, 100)]
    public static void Preparar()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sal del modo Play", "Usa este menú fuera del modo Play.", "Aceptar");
            return;
        }
        foreach (string b in Bloques)
            foreach (string s in Subcarpetas)
                Directory.CreateDirectory(Path.Combine("Assets/_Arte", b, s));
        AssetDatabase.Refresh();
        RecursosVisualesEM4X tabla = Tabla(true);
        if (tabla == null) return;

        int asignadas = 0;
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (!(mb is JuegoMVP) && !(mb is PruebaModelos)) continue;
            SerializedObject so = new SerializedObject(mb);
            SerializedProperty p = so.FindProperty(mb is JuegoMVP ? "recursosVisuales" : "recursos");
            if (p == null || p.objectReferenceValue != null) continue;
            p.objectReferenceValue = tabla;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(mb.gameObject.scene);
            asignadas++;
        }
        Selection.activeObject = tabla;
        EditorUtility.DisplayDialog("Carpetas y tabla preparadas",
            "Carpetas: Assets/_Arte/{Nave, Interior, Utileria, Hangar}/{Modelos, Materiales, Texturas, Prefabs}.\n" +
            "Tabla: " + RutaTabla + " (seleccionada).\n" +
            (asignadas > 0 ? $"Asignada a {asignadas} componente(s) de la escena abierta: guarda la escena si quieres conservarlo.\n" : "") +
            "\nNo se ha modificado ningún script, escena guardada ni paquete.", "Aceptar");
    }

    [MenuItem("Prototipo/Arte y pruebas/2. Crear escena ModeloPrueba (antes 20)", false, 101)]
    public static void CrearEscena()
    {
        if (File.Exists(RutaEscena))
        {
            EditorUtility.DisplayDialog("ModeloPrueba ya existe", "La escena " + RutaEscena + " ya existe. Este menú no la sustituye.", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Crear ModeloPrueba",
                "Se creará la escena NUEVA " + RutaEscena + " con un objeto «PruebaModelos» que reutiliza la prueba de circulación " +
                "y aplica la tabla de modelos.\n\nNo se modifica ninguna otra escena ni la lista del build.", "Crear", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        RecursosVisualesEM4X tabla = Tabla(true);
        if (tabla == null) return;
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PruebaModelos pm = new GameObject("PruebaModelos").AddComponent<PruebaModelos>();
        SerializedObject so = new SerializedObject(pm);
        so.FindProperty("recursos").objectReferenceValue = tabla;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (EditorSceneManager.SaveScene(escena, RutaEscena))
            Debug.Log("[Arte EM-4X] Escena creada en " + RutaEscena + ". Asigna modelos en la tabla y pulsa Play.");
    }

    [MenuItem("Prototipo/Arte y pruebas/3. Comprobar recursos de arte (antes 21)", false, 102)]
    public static void Comprobar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Arte"))
        {
            EditorUtility.DisplayDialog("Sin carpeta de arte", "Usa antes «Prototipo → Arte y pruebas → 1. Preparar carpetas de arte».", "Aceptar");
            return;
        }
        Regex convencion = new Regex(@"^(EM4X|HGR)_[A-Za-z0-9]+_[A-Za-z0-9]+(_[A-Za-z0-9]+)*_v\d{2}$");
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("COMPROBACIÓN DE RECURSOS DE ARTE · " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        sb.AppendLine("Revisión automática: no sustituye la prueba en ModeloPrueba.\n");
        int avisos = 0, revisados = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/_Arte" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (go == null) continue;
            revisados++;
            List<string> n = new List<string>();
            string nombre = Path.GetFileNameWithoutExtension(ruta);
            if (!convencion.IsMatch(nombre)) n.Add("nombre fuera de la convención (EM4X_<Bloque>_<Pieza>_vNN o HGR_<Zona>_<Pieza>_vNN)");

            ModelImporter mi = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (mi != null)
            {
                if (Mathf.Abs(mi.globalScale - 1f) > 1e-4f) n.Add($"escala de importación {mi.globalScale} (debe ser 1)");
                if (!mi.useFileScale) n.Add("«Convert Units» desactivado: revisa que 1 unidad = 1 m");
                if (mi.addCollider) n.Add("«Generate Colliders» activado: desactívalo (las colisiones son las provisionales)");
            }

            Bounds b = new Bounds(); bool hay = false;
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 a = go.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = a.MultiplyPoint3x4(c);
                    if (!hay) { b = new Bounds(p, Vector3.zero); hay = true; } else b.Encapsulate(p);
                }
            }
            if (!hay) n.Add("no tiene mallas");
            else if (b.size.magnitude > 200f) n.Add($"mide {b.size} m: ¿exportado en centímetros?");

            int colisiones = go.GetComponentsInChildren<Collider>(true).Length + go.GetComponentsInChildren<Rigidbody>(true).Length;
            int extras = go.GetComponentsInChildren<Camera>(true).Length + go.GetComponentsInChildren<Light>(true).Length;
            if (extras > 0) n.Add($"{extras} cámaras o luces dentro del modelo: expórtalo sin ellas");
            if (colisiones > 0) n.Add($"{colisiones} colisiones o cuerpos físicos: se quitarán al integrar; mejor entregarlo sin ellos");

            HashSet<string> shaders = new HashSet<string>();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                {
                    string s = m == null ? "(vacío)" : m.shader != null ? m.shader.name : "(sin shader)";
                    if (!s.StartsWith("Universal Render Pipeline/") && !s.StartsWith("Shader Graphs/")) shaders.Add(s);
                }
            if (shaders.Count > 0) n.Add("materiales no URP: " + string.Join(", ", shaders));

            sb.AppendLine((n.Count == 0 ? "[OK] " : "[!]  ") + ruta + (hay ? $"  · tamaño {b.size.x:0.00} × {b.size.y:0.00} × {b.size.z:0.00} m · centro {b.center}" : ""));
            foreach (string s in n) sb.AppendLine("       - " + s);
            avisos += n.Count;
        }
        Directory.CreateDirectory("Informes");
        string archivo = "Informes/ComprobacionArte_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        File.WriteAllText(archivo, sb.ToString());
        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Comprobación de arte",
            $"{revisados} recursos revisados, {avisos} avisos.\nInforme completo: {archivo} (también en la consola).", "Aceptar");
    }
}
