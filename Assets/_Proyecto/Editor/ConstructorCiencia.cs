using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego) — incremento 3.
///
/// Menú "Prototipo → 4. Añadir ciencia a Exploracion":
/// - Crea 3 fichas de DEMOSTRACIÓN en Data/Fichas (si no existen).
/// - Crea el prefab PuntoCientifico (si no existe).
/// - Abre Exploracion y AÑADE:
///     · un objeto "SistemaCiencia" (Escaner + SistemaDatos + HudCiencia),
///     · 3 puntos científicos, solo si la escena aún no tiene ninguno.
/// No borra ni modifica ningún objeto existente. Pregunta antes de empezar.
/// </summary>
public static class ConstructorCiencia
{
    const string Raiz = "Assets/_Proyecto";
    const string CarpetaFichas = Raiz + "/Data/Fichas";
    const string RutaPrefabPunto = Raiz + "/Prefabs/PuntoCientifico.prefab";
    const string RutaExploracion = Raiz + "/Scenes/Exploracion.unity";

    static readonly Vector3[] PosicionesDeseadas =
    {
        new Vector3(28f, 0f, 32f),
        new Vector3(-42f, 0f, 18f),
        new Vector3(12f, 0f, -48f)
    };

    static readonly string[] Titulos =
    {
        "Columna de burbujas",
        "Pradera luminosa",
        "Chimenea mineral"
    };

    static readonly string[] Textos =
    {
        "Una corriente continua de burbujas sale de una grieta del fondo. En este escenario ficticio sirve " +
        "para practicar: cada escaneo ocupa memoria y no cuenta hasta que lo transmites desde la estación.",

        "Filamentos que emiten una luz tenue cubren varios metros del fondo. Escenario ficticio: los " +
        "instrumentos que generan más datos llenan la memoria antes, y hay que planificar los regresos.",

        "Una estructura alta formada por minerales acumulados alrededor de un respiradero. Escenario " +
        "ficticio: un escaneo lento consume más energía, así que conviene vigilar la batería antes de empezar."
    };

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/4. Añadir ciencia a Exploracion", false, 1003)]
    public static void AnadirCiencia()
    {
        if (!File.Exists(RutaExploracion))
        {
            EditorUtility.DisplayDialog("Falta la escena",
                "No existe " + RutaExploracion + ".\nCréala antes con el menú 1.", "Aceptar");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Añadir ciencia a Exploracion",
                "Se abrirá la escena Exploracion y se AÑADIRÁN:\n" +
                "• un objeto «SistemaCiencia» (escáner, datos e interfaz),\n" +
                "• 3 puntos científicos (solo si la escena no tiene ninguno).\n\n" +
                "También se crean, si no existen, 3 fichas en Data/Fichas y el prefab PuntoCientifico.\n" +
                "No se borra ni se modifica ningún objeto existente. Después se guardará la escena.",
                "Continuar", "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AsegurarCarpeta(Raiz, "Data");
        AsegurarCarpeta(Raiz + "/Data", "Fichas");
        AsegurarCarpeta(Raiz, "Prefabs");
        AsegurarCarpeta(Raiz, "Materials");

        FichaCientifica[] fichas = new FichaCientifica[Titulos.Length];
        for (int i = 0; i < Titulos.Length; i++)
            fichas[i] = ObtenerFicha(i);

        GameObject prefab = ObtenerPrefabPunto();

        Scene escena = EditorSceneManager.OpenScene(RutaExploracion, OpenSceneMode.Single);

        int puntosAnadidos = 0;
        if (Object.FindAnyObjectByType<PuntoCientifico>() == null)
        {
            Physics.SyncTransforms();
            GameObject contenedor = new GameObject("PuntosCientificos");
            for (int i = 0; i < PosicionesDeseadas.Length; i++)
            {
                GameObject punto = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                punto.name = "Punto_" + (i + 1) + "_" + Titulos[i].Replace(" ", "");
                punto.transform.SetParent(contenedor.transform, true);
                punto.transform.position = PosicionLibre(PosicionesDeseadas[i]);

                SerializedObject so = new SerializedObject(punto.GetComponent<PuntoCientifico>());
                so.FindProperty("ficha").objectReferenceValue = fichas[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                puntosAnadidos++;
            }
        }

        bool sistemaNuevo = false;
        Escaner escaner = Object.FindAnyObjectByType<Escaner>();
        if (escaner == null)
        {
            GameObject go = new GameObject("SistemaCiencia");
            go.AddComponent<SistemaDatos>();
            escaner = go.AddComponent<Escaner>();
            go.AddComponent<HudCiencia>();
            sistemaNuevo = true;
        }

        CompletarReferencias(escaner.gameObject);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Selection.activeGameObject = escaner.gameObject;
        Debug.Log($"[Prototipo] Ciencia añadida a Exploracion. Puntos nuevos: {puntosAnadidos}. " +
                  (sistemaNuevo ? "SistemaCiencia creado." : "SistemaCiencia ya existía; solo se completaron referencias vacías."));
    }

    // ------------------------------------------------------------------ Referencias

    static void CompletarReferencias(GameObject sistema)
    {
        SistemaEnergia energia = Object.FindAnyObjectByType<SistemaEnergia>();
        AplicadorConfiguracion aplicador = Object.FindAnyObjectByType<AplicadorConfiguracion>();
        Transform camara = null;
        if (energia != null)
        {
            Camera cam = energia.GetComponentInChildren<Camera>();
            if (cam != null) camara = cam.transform;
        }

        SistemaDatos datos = sistema.GetComponent<SistemaDatos>();
        Escaner escaner = sistema.GetComponent<Escaner>();
        HudCiencia hud = sistema.GetComponent<HudCiencia>();

        if (aplicador == null)
            Debug.LogWarning("[Prototipo] Exploracion no tiene AplicadorConfiguracion. Ejecuta antes el menú 3.");

        if (datos != null)
        {
            SerializedObject so = new SerializedObject(datos);
            AsignarSiVacio(so, "energia", energia);
            AsignarSiVacio(so, "aplicador", aplicador);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (escaner != null)
        {
            SerializedObject so = new SerializedObject(escaner);
            AsignarSiVacio(so, "camara", camara);
            AsignarSiVacio(so, "energia", energia);
            AsignarSiVacio(so, "datos", datos);
            AsignarSiVacio(so, "aplicador", aplicador);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (hud != null)
        {
            SerializedObject so = new SerializedObject(hud);
            AsignarSiVacio(so, "escaner", escaner);
            AsignarSiVacio(so, "datos", datos);
            AsignarSiVacio(so, "sonda", energia != null ? energia.transform : null);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void AsignarSiVacio(SerializedObject so, string propiedad, Object valor)
    {
        SerializedProperty p = so.FindProperty(propiedad);
        if (p != null && p.objectReferenceValue == null && valor != null)
            p.objectReferenceValue = valor;
    }

    // ------------------------------------------------------------------ Fichas y prefab

    static FichaCientifica ObtenerFicha(int i)
    {
        string ruta = $"{CarpetaFichas}/Ficha_{i + 1}.asset";
        FichaCientifica existente = AssetDatabase.LoadAssetAtPath<FichaCientifica>(ruta);
        if (existente != null) return existente;

        FichaCientifica f = ScriptableObject.CreateInstance<FichaCientifica>();
        f.titulo = Titulos[i];
        f.texto = Textos[i];
        f.esDemostracion = true;
        AssetDatabase.CreateAsset(f, ruta);
        return f;
    }

    static GameObject ObtenerPrefabPunto()
    {
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPunto);
        if (existente != null) return existente;

        Material activo = ObtenerMaterial("PuntoSinEscanear", new Color(1f, 0.55f, 0.85f), new Color(2.2f, 0.9f, 1.8f));
        Material escaneado = ObtenerMaterial("PuntoEscaneado", new Color(0.35f, 0.5f, 0.55f), new Color(0.1f, 0.25f, 0.3f));
        Material base_ = ObtenerMaterial("Roca", new Color(0.24f, 0.26f, 0.29f));

        GameObject raiz = new GameObject("PuntoCientifico");
        SphereCollider col = raiz.AddComponent<SphereCollider>();
        col.center = new Vector3(0f, 1.6f, 0f);
        col.radius = 1.3f;
        PuntoCientifico punto = raiz.AddComponent<PuntoCientifico>();

        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "Pedestal";
        pedestal.transform.SetParent(raiz.transform, false);
        pedestal.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        pedestal.transform.localScale = new Vector3(1.6f, 0.3f, 1.6f);
        Object.DestroyImmediate(pedestal.GetComponent<Collider>());
        pedestal.GetComponent<Renderer>().sharedMaterial = base_;

        GameObject nucleo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nucleo.name = "Nucleo";
        nucleo.transform.SetParent(raiz.transform, false);
        nucleo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        nucleo.transform.localScale = Vector3.one * 1.4f;
        Object.DestroyImmediate(nucleo.GetComponent<Collider>());
        Renderer indicador = nucleo.GetComponent<Renderer>();
        indicador.sharedMaterial = activo;

        GameObject luzGo = new GameObject("Luz");
        luzGo.transform.SetParent(raiz.transform, false);
        luzGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        Light luz = luzGo.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.55f, 0.85f);
        luz.range = 14f;
        luz.intensity = 3f;

        SerializedObject so = new SerializedObject(punto);
        so.FindProperty("indicador").objectReferenceValue = indicador;
        so.FindProperty("materialEscaneado").objectReferenceValue = escaneado;
        so.FindProperty("luz").objectReferenceValue = luz;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabPunto);
        Object.DestroyImmediate(raiz);
        return guardado;
    }

    // ------------------------------------------------------------------ Utilidades

    /// <summary>Busca cerca de la posición deseada un sitio sin rocas encima.</summary>
    static Vector3 PosicionLibre(Vector3 deseada)
    {
        for (int intento = 0; intento < 40; intento++)
        {
            Vector3 p = deseada;
            if (intento > 0)
            {
                float angulo = intento * 137.5f * Mathf.Deg2Rad;
                float radio = 3f + intento * 1.5f;
                p += new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radio;
            }
            // Caja que empieza a 0,2 m sobre el fondo (y = 0) para no detectar el propio suelo.
            bool ocupado = Physics.CheckBox(p + new Vector3(0f, 2.2f, 0f), new Vector3(2.2f, 2f, 2.2f),
                                            Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (!ocupado) return p;
        }
        Debug.LogWarning("[Prototipo] No se encontró un sitio libre cerca de " + deseada + "; colócalo a mano.");
        return deseada;
    }

    static void AsegurarCarpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + nombre))
            AssetDatabase.CreateFolder(padre, nombre);
    }

    static Material ObtenerMaterial(string nombre, Color color, Color? emision = null)
    {
        string ruta = Raiz + "/Materials/" + nombre + ".mat";
        Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material m = new Material(shader);
        m.SetColor("_BaseColor", color);
        if (emision.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }
}
