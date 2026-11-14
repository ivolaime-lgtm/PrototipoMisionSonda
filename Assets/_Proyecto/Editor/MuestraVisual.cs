using System.Collections.Generic;
using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego). Muestra visual del sector delantero de la nave.
///
/// "Prototipo → Muestra visual → 1. Aplicar: sector delantero de la nave"
///   Con la escena Nave abierta y fuera de Play, añade la raíz VISUAL_MUESTRA_NAVE con paneles,
///   nervaduras, suelo de rejilla, techo con tiras de luz, marco biselado de la ventana, consola de
///   pilotaje con pantallas que muestran datos reales (solo lectura), luces de acento y un perfil de
///   posprocesado moderado.
///   - NO regenera la escena ni borra nada. NO añade colisionadores ni mueve puestos ni vistas.
///   - Oculta (no borra) el dibujado de: la consola y la pantalla de pilotaje (sus colisionadores siguen)
///     y los tres listones antiguos de la ventana (incluido el central, que tapaba el centro de la vista).
///   - Cambia la luz ambiental y activa el posprocesado de la cámara; guarda los valores originales.
///   - Si ya existe, pregunta y la rehace (no crea duplicados).
///   - Antes de aplicar, copia la escena guardada en CopiasSeguridad/.
///   - Texturas, materiales y perfil se crean en Assets/_Proyecto/Visual solo si no existen.
///
/// "… → 1. Quitar: sector delantero de la nave"
///   Borra la raíz VISUAL_MUESTRA_NAVE, vuelve a mostrar lo ocultado y restaura la luz ambiental y
///   el posprocesado de la cámara.
///
/// En ambos casos la escena queda modificada sin guardar: Ctrl+S para conservar el cambio.
/// </summary>
public static class MuestraVisual
{
    const string NombreRaiz = "VISUAL_MUESTRA_NAVE";
    const string Carpeta = "Assets/_Proyecto/Visual";

    // ======================================================================== Menús

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/Muestra visual/1. Aplicar: sector delantero de la nave", false, 1012)]
    public static void AplicarNave()
    {
        if (!Comprobar()) return;
        Scene escena = SceneManager.GetActiveScene();
        foreach (GameObject g in escena.GetRootGameObjects())
            if (g.name == "NAVE_AMPLIADA")
            {
                EditorUtility.DisplayDialog("Muestra antigua no compatible",
                    "Esta muestra se diseñó para la cabina pequeña y la nave está ampliada.\n" +
                    "Usa «Prototipo → 11. Acabado: cabina de pilotaje (aplicar)».", "Aceptar");
                return;
            }

        GameObject existente = BuscarRaiz(escena);
        if (existente != null)
        {
            if (!EditorUtility.DisplayDialog("La muestra ya está aplicada",
                    "La escena ya tiene " + NombreRaiz + ".\n\n¿Rehacerla? Se quita la actual (restaurando todo) y se vuelve a crear.",
                    "Rehacer", "Cancelar"))
                return;
            Quitar(existente);
        }

        if (escena.isDirty &&
            !EditorUtility.DisplayDialog("Guardar antes",
                "La escena tiene cambios sin guardar. Para hacer la copia de seguridad hay que guardarla primero.\n\n¿Guardar ahora?",
                "Guardar y continuar", "Cancelar"))
            return;
        if (escena.isDirty) EditorSceneManager.SaveScene(escena);
        CopiaDeSeguridad(escena);

        Transform cabina = BuscarEnEscena(escena, "Cabina");
        Camera camara = BuscarEnEscena(escena, "Camara")?.GetComponent<Camera>();
        Light luzDelantera = cabina != null ? cabina.Find("LuzCabinaDelantera")?.GetComponent<Light>() : null;
        if (cabina == null || camara == null)
        {
            EditorUtility.DisplayDialog("No es la escena esperada",
                "No se encontraron los objetos Cabina y Camara. Abre Scenes/Nave.", "Aceptar");
            return;
        }

        Recursos r = PrepararRecursos();

        GameObject raiz = new GameObject(NombreRaiz);
        SceneManager.MoveGameObjectToScene(raiz, escena);
        CapaVisual capa = raiz.AddComponent<CapaVisual>();

        // --- Ocultar (no borrar) formas provisionales sustituidas
        Ocultar(capa, cabina, "PuestoPilotaje/Consola");
        Ocultar(capa, cabina, "PuestoPilotaje/Pantalla");
        Ocultar(capa, cabina, "MarcoVentanaArriba");
        Ocultar(capa, cabina, "MarcoVentanaAbajo");
        Ocultar(capa, cabina, "MarcoVentanaCentro");

        List<Renderer> tiras = new List<Renderer>();
        Transform t = raiz.transform;

        ConstruirParedes(t, r, tiras);
        ConstruirTechoYSuelo(t, r, tiras);
        ConstruirFrenteYVentana(t, r);
        ConstruirConsola(t, r);
        ConstruirLuces(t);

        // --- Tiras que siguen el color de la luz de la cabina (alertas visibles)
        LucesReactivasVisual reactivas = raiz.AddComponent<LucesReactivasVisual>();
        Asignar(reactivas, "luzReferencia", luzDelantera);
        AsignarLista(reactivas, "tiras", tiras.ToArray());

        // --- Luz ambiental (se guarda la original)
        capa.ambienteGuardado = true;
        capa.modoAmbienteOriginal = RenderSettings.ambientMode;
        capa.ambienteOriginal = RenderSettings.ambientLight;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.17f, 0.21f, 0.28f);

        // --- Posprocesado: volumen global (dentro de la raíz) + activarlo en la cámara
        GameObject vol = new GameObject("Posprocesado");
        vol.transform.SetParent(t, false);
        Volume volumen = vol.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 1f;
        volumen.sharedProfile = r.perfil;

        capa.camara = camara;
        UniversalAdditionalCameraData datos = camara.GetComponent<UniversalAdditionalCameraData>();
        capa.datosCamaraAnadidos = datos == null;
        if (datos == null) datos = camara.gameObject.AddComponent<UniversalAdditionalCameraData>();
        capa.postprocesadoGuardado = true;
        capa.postprocesadoOriginal = datos.renderPostProcessing;
        datos.renderPostProcessing = true;
        EditorUtility.SetDirty(datos);

        EditorUtility.SetDirty(capa);
        EditorSceneManager.MarkSceneDirty(escena);
        Selection.activeGameObject = raiz;
        Debug.Log("[Prototipo] Muestra visual de la nave aplicada. Revisa la vista y pulsa Ctrl+S para guardarla " +
                  "(o cierra la escena sin guardar para descartarla).");
    }

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/Muestra visual/1. Quitar: sector delantero de la nave", false, 1013)]
    public static void QuitarNave()
    {
        if (!Comprobar()) return;
        Scene escena = SceneManager.GetActiveScene();
        GameObject raiz = BuscarRaiz(escena);
        if (raiz == null)
        {
            EditorUtility.DisplayDialog("Nada que quitar", "La escena no tiene " + NombreRaiz + ".", "Aceptar");
            return;
        }
        Quitar(raiz);
        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log("[Prototipo] Muestra visual quitada y valores originales restaurados. Pulsa Ctrl+S para guardar.");
    }

    static bool Comprobar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Sal de Play", "Este menú solo funciona fuera del modo Play.", "Aceptar");
            return false;
        }
        if (SceneManager.GetActiveScene().name != "Nave")
        {
            EditorUtility.DisplayDialog("Abre la escena Nave", "Este menú trabaja sobre la escena Nave. Ábrela primero.", "Aceptar");
            return false;
        }
        return true;
    }

    internal static void Quitar(GameObject raiz)
    {
        CapaVisual capa = raiz.GetComponent<CapaVisual>();
        if (capa != null)
        {
            foreach (Renderer rend in capa.ocultados)
                if (rend != null) { rend.enabled = true; EditorUtility.SetDirty(rend); }

            if (capa.ambienteGuardado)
            {
                RenderSettings.ambientMode = capa.modoAmbienteOriginal;
                RenderSettings.ambientLight = capa.ambienteOriginal;
            }

            if (capa.postprocesadoGuardado && capa.camara != null)
            {
                UniversalAdditionalCameraData datos = capa.camara.GetComponent<UniversalAdditionalCameraData>();
                if (datos != null)
                {
                    if (capa.datosCamaraAnadidos) Object.DestroyImmediate(datos);
                    else { datos.renderPostProcessing = capa.postprocesadoOriginal; EditorUtility.SetDirty(datos); }
                }
            }
        }
        Object.DestroyImmediate(raiz);
    }

    // ======================================================================== Geometría

    // Interior: x ±4 (paredes en ±4,0), suelo y 0, techo y 3,0, ventana en z 5,0 (x ±2, y 1,0–2,6).
    // El sector delantero va de z 1,8 a z 5,0. Ninguna pieza tiene colisionador.

    static void ConstruirParedes(Transform raiz, Recursos r, List<Renderer> tiras)
    {
        Transform paredes = Grupo("Paredes", raiz);
        float[] zCol = { 2.35f, 3.43f, 4.47f };
        float[] zNerv = { 1.85f, 2.89f, 3.96f, 4.94f };

        foreach (int s in new[] { -1, 1 })
        {
            string lado = s < 0 ? "Izq" : "Der";
            for (int c = 0; c < zCol.Length; c++)
            {
                Pieza($"Panel{lado}_{c}_Bajo", paredes, new Vector3(s * 3.965f, 0.85f, zCol[c]), new Vector3(0.05f, 1.1f, 0.96f), r.panel);
                Pieza($"Panel{lado}_{c}_Alto", paredes, new Vector3(s * 3.965f, 2.05f, zCol[c]), new Vector3(0.05f, 1.1f, 0.96f), r.panel);
            }
            foreach (float z in zNerv)
                Pieza($"Nervadura{lado}_{z:0.00}", paredes, new Vector3(s * 3.92f, 1.32f, z), new Vector3(0.12f, 2.64f, 0.14f), r.nervadura);

            // Zócalo con línea de luz
            Pieza($"Zocalo{lado}", paredes, new Vector3(s * 3.93f, 0.15f, 3.4f), new Vector3(0.1f, 0.3f, 3.2f), r.nervadura);
            tiras.Add(Pieza($"TiraZocalo{lado}", paredes, new Vector3(s * 3.87f, 0.31f, 3.4f), new Vector3(0.03f, 0.02f, 3.2f), r.tira).GetComponent<Renderer>());

            // Chaflán entre pared y techo, con tira de luz
            Quaternion giro = Quaternion.Euler(0f, 0f, s * 48f);
            Pieza($"Chaflan{lado}", paredes, new Vector3(s * 3.74f, 2.785f, 3.4f), new Vector3(0.05f, 0.66f, 3.2f), r.panelOscuro, giro);
            tiras.Add(Pieza($"TiraChaflan{lado}", paredes, new Vector3(s * 3.70f, 2.75f, 3.4f), new Vector3(0.02f, 0.05f, 3.1f), r.tira, giro).GetComponent<Renderer>());
        }
    }

    static void ConstruirTechoYSuelo(Transform raiz, Recursos r, List<Renderer> tiras)
    {
        Transform techo = Grupo("Techo", raiz);
        float[] zFila = { 2.35f, 3.43f, 4.47f };
        foreach (int s in new[] { -1, 1 })
            for (int f = 0; f < zFila.Length; f++)
                Pieza($"PanelTecho_{(s < 0 ? "Izq" : "Der")}_{f}", techo, new Vector3(s * 1.95f, 2.975f, zFila[f]), new Vector3(3.0f, 0.05f, 0.96f), r.panel);

        Pieza("CanalLuzTecho", techo, new Vector3(0f, 2.975f, 3.4f), new Vector3(0.9f, 0.05f, 3.2f), r.panelOscuro);
        tiras.Add(Pieza("TiraTecho", techo, new Vector3(0f, 2.945f, 3.4f), new Vector3(0.28f, 0.02f, 3.1f), r.tira).GetComponent<Renderer>());
        foreach (float z in new[] { 1.85f, 2.89f, 3.96f, 4.94f })
            Pieza($"ArcoTecho_{z:0.00}", techo, new Vector3(0f, 2.9f, z), new Vector3(7.0f, 0.12f, 0.14f), r.nervadura);

        Transform suelo = Grupo("Suelo", raiz);
        float[] xCol = { -2.45f, 0f, 2.45f };
        foreach (float x in xCol)
            foreach (float z in new[] { 2.37f, 3.45f })
                Pieza($"Rejilla_{x:0.0}_{z:0.0}", suelo, new Vector3(x, 0.008f, z), new Vector3(2.3f, 0.016f, 1.04f), r.rejilla);
        foreach (float x in new[] { -2.45f, 2.45f })
            Pieza($"Rejilla_{x:0.0}_frente", suelo, new Vector3(x, 0.008f, 4.5f), new Vector3(2.3f, 0.016f, 0.98f), r.rejilla);
        foreach (int s in new[] { -1, 1 })
            tiras.Add(Pieza($"TiraSuelo{(s < 0 ? "Izq" : "Der")}", suelo, new Vector3(s * 1.25f, 0.02f, 3.4f), new Vector3(0.04f, 0.012f, 3.2f), r.tira).GetComponent<Renderer>());
    }

    internal static void ConstruirFrenteYVentana(Transform raiz, Recursos r)
    {
        Transform frente = Grupo("FrenteYVentana", raiz);

        // Paneles de la pared delantera alrededor del hueco (no lo invaden)
        Pieza("PanelFrenteIzq", frente, new Vector3(-3.0f, 1.5f, 4.975f), new Vector3(1.9f, 2.9f, 0.05f), r.panel);
        Pieza("PanelFrenteDer", frente, new Vector3(3.0f, 1.5f, 4.975f), new Vector3(1.9f, 2.9f, 0.05f), r.panel);
        Pieza("PanelFrenteAlto", frente, new Vector3(0f, 2.8f, 4.975f), new Vector3(4.1f, 0.4f, 0.05f), r.panelOscuro);
        Pieza("PanelFrenteBajo", frente, new Vector3(0f, 0.5f, 4.975f), new Vector3(4.1f, 1.0f, 0.05f), r.panelOscuro);

        // Marco biselado: se abre hacia el interior, así no tapa nada de lo que ya se veía por el hueco.
        Losa("BiselArriba", frente, new Vector3(0f, 2.6f, 4.99f), new Vector3(0f, 2.86f, 4.8f), Vector3.right, 4.66f, 0.04f, r.bisel);
        Losa("BiselAbajo", frente, new Vector3(0f, 1.0f, 4.99f), new Vector3(0f, 0.74f, 4.8f), Vector3.right, 4.66f, 0.04f, r.bisel);
        Losa("BiselIzq", frente, new Vector3(-2.0f, 1.8f, 4.99f), new Vector3(-2.31f, 1.8f, 4.8f), Vector3.up, 2.14f, 0.04f, r.bisel);
        Losa("BiselDer", frente, new Vector3(2.0f, 1.8f, 4.99f), new Vector3(2.31f, 1.8f, 4.8f), Vector3.up, 2.14f, 0.04f, r.bisel);

        // Acento naranja en el borde exterior del marco
        Pieza("AcentoMarcoArriba", frente, new Vector3(0f, 2.875f, 4.795f), new Vector3(4.7f, 0.03f, 0.03f), r.acento);
        Pieza("AcentoMarcoAbajo", frente, new Vector3(0f, 0.725f, 4.795f), new Vector3(4.7f, 0.03f, 0.03f), r.acento);
        Pieza("AcentoMarcoIzq", frente, new Vector3(-2.325f, 1.8f, 4.795f), new Vector3(0.03f, 2.18f, 0.03f), r.acento);
        Pieza("AcentoMarcoDer", frente, new Vector3(2.325f, 1.8f, 4.795f), new Vector3(0.03f, 2.18f, 0.03f), r.acento);
    }

    internal static void ConstruirConsola(Transform raiz, Recursos r)
    {
        // Todo queda por debajo de la línea de visión hacia el borde inferior de la ventana
        // desde la vista de pilotaje (0, 1,6, 3,0): y < 1,6 − 0,3·(z − 3).
        Transform consola = Grupo("ConsolaPilotaje", raiz);

        Pieza("Pedestal", consola, new Vector3(0f, 0.42f, 4.1f), new Vector3(1.7f, 0.84f, 0.55f), r.consola);
        Pieza("FaldonFrontal", consola, new Vector3(0f, 0.46f, 3.66f), new Vector3(2.2f, 0.82f, 0.05f), r.panelOscuro, Quaternion.Euler(10f, 0f, 0f));
        Pieza("Mesa", consola, new Vector3(0f, 0.98f, 3.92f), new Vector3(2.4f, 0.07f, 0.86f), r.consola, Quaternion.Euler(-7f, 0f, 0f));
        Pieza("AcentoMesa", consola, new Vector3(0f, 0.93f, 3.49f), new Vector3(2.4f, 0.025f, 0.03f), r.acento);

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Pantalla central en la misma posición y giro que la pantalla original (su colisionador sigue allí).
        Pantalla("PantallaCentral", consola, new Vector3(0f, 1.15f, 3.85f), Quaternion.Euler(-15f, 0f, 0f),
                 new Vector2(1.38f, 0.28f), PantallaDatosNave.Contenido.Estado, 0.0072f, r, fuente);
        Pantalla("PantallaPropulsion", consola, new Vector3(-0.95f, 1.07f, 3.72f), Quaternion.Euler(-15f, -35f, 0f),
                 new Vector2(0.56f, 0.32f), PantallaDatosNave.Contenido.Propulsion, 0.0052f, r, fuente);
        Pantalla("PantallaCasco", consola, new Vector3(0.95f, 1.07f, 3.72f), Quaternion.Euler(-15f, 35f, 0f),
                 new Vector2(0.56f, 0.32f), PantallaDatosNave.Contenido.Casco, 0.0052f, r, fuente);

        // Mandos
        foreach (int s in new[] { -1, 1 })
        {
            Pieza($"Mando{(s < 0 ? "Izq" : "Der")}", consola, new Vector3(s * 0.5f, 1.04f, 3.62f), new Vector3(0.035f, 0.065f, 0.035f), r.nervadura, null, PrimitiveType.Cylinder);
            Pieza($"Pomo{(s < 0 ? "Izq" : "Der")}", consola, new Vector3(s * 0.5f, 1.12f, 3.62f), new Vector3(0.07f, 0.07f, 0.07f), r.acento, null, PrimitiveType.Sphere);
        }

        // Pequeño holograma del destino (a un lado y bajo la línea de visión)
        Pieza("BaseHolograma", consola, new Vector3(0.62f, 1.03f, 4.15f), new Vector3(0.16f, 0.01f, 0.16f), r.holograma, null, PrimitiveType.Cylinder);
        GameObject pivote = new GameObject("Holograma");
        pivote.transform.SetParent(consola, false);
        pivote.transform.localPosition = new Vector3(0.62f, 1.11f, 4.15f);
        Pieza("Destino", pivote.transform, Vector3.zero, Vector3.one * 0.05f, r.holograma, null, PrimitiveType.Sphere);
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            Pieza($"Orbita_{i}", pivote.transform, new Vector3(Mathf.Cos(a) * 0.09f, 0f, Mathf.Sin(a) * 0.09f), Vector3.one * 0.012f, r.holograma, null, PrimitiveType.Sphere);
        }
        pivote.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        MovimientoVisual giro = pivote.AddComponent<MovimientoVisual>();
        SerializedObject so = new SerializedObject(giro);
        so.FindProperty("modo").enumValueIndex = 0;         // RotarSobreSiMismo
        so.FindProperty("eje").vector3Value = Vector3.up;
        so.FindProperty("velocidad").floatValue = 35f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Pantalla(string nombre, Transform padre, Vector3 pos, Quaternion rot, Vector2 tam,
                         PantallaDatosNave.Contenido contenido, float tamLetra, Recursos r, Font fuente)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;

        Pieza("Marco", go.transform, new Vector3(0f, 0f, 0.012f), new Vector3(tam.x + 0.06f, tam.y + 0.06f, 0.03f), r.consola);
        Pieza("Superficie", go.transform, new Vector3(0f, 0f, -0.004f), new Vector3(tam.x, tam.y, 0.01f), r.pantalla);

        if (fuente == null) return;
        GameObject txt = new GameObject("Texto");
        txt.transform.SetParent(go.transform, false);
        txt.transform.localPosition = new Vector3(0f, 0f, -0.012f);
        TextMesh tm = txt.AddComponent<TextMesh>();
        tm.font = fuente;
        tm.fontSize = 80;
        tm.characterSize = tamLetra;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.65f, 1f, 1f);
        tm.text = "-";
        txt.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;

        PantallaDatosNave datos = go.AddComponent<PantallaDatosNave>();
        SerializedObject so = new SerializedObject(datos);
        so.FindProperty("contenido").enumValueIndex = (int)contenido;
        so.FindProperty("texto").objectReferenceValue = tm;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void ConstruirLuces(Transform raiz)
    {
        Transform luces = Grupo("LucesAcento", raiz);
        Luz("LuzConsolaCalida", luces, new Vector3(0f, 1.3f, 3.55f), new Color(1f, 0.6f, 0.25f), 0.6f, 1.7f);
        Luz("LuzFrenteTurquesa", luces, new Vector3(0f, 2.5f, 4.4f), new Color(0.4f, 0.85f, 1f), 0.9f, 5f);
    }

    // ======================================================================== Utilidades de escena

    internal static Transform Grupo(string nombre, Transform padre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        return g.transform;
    }

    /// <summary>Pieza decorativa SIN colisionador y sin sombras.</summary>
    internal static GameObject Pieza(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material m,
                            Quaternion? rot = null, PrimitiveType tipo = PrimitiveType.Cube)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = escala;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = m;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    /// <summary>Losa entre dos puntos (a lo largo), con el ancho en la dirección indicada.</summary>
    internal static GameObject Losa(string nombre, Transform padre, Vector3 a, Vector3 b, Vector3 ejeAncho, float ancho, float grosor, Material m)
    {
        Vector3 largo = b - a;
        Vector3 adelante = largo.normalized;
        Vector3 arriba = Vector3.Cross(adelante, ejeAncho).normalized;
        return Pieza(nombre, padre, (a + b) * 0.5f, new Vector3(ancho, grosor, largo.magnitude), m,
                     Quaternion.LookRotation(adelante, arriba));
    }

    static void Luz(string nombre, Transform padre, Vector3 pos, Color color, float intensidad, float alcance)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = alcance;
        l.shadows = LightShadows.None;
    }

    internal static void Ocultar(CapaVisual capa, Transform cabina, string ruta)
    {
        Transform t = cabina.Find(ruta);
        Renderer rend = t != null ? t.GetComponent<Renderer>() : null;
        if (rend == null)
        {
            Debug.LogWarning("[Prototipo] Muestra visual: no se encontró Cabina/" + ruta + " (se deja como está).");
            return;
        }
        if (!rend.enabled) return;          // ya estaba oculto por otra razón: no se toca
        rend.enabled = false;
        capa.ocultados.Add(rend);
        EditorUtility.SetDirty(rend);
    }

    static GameObject BuscarRaiz(Scene escena)
    {
        foreach (GameObject g in escena.GetRootGameObjects())
            if (g.name == NombreRaiz) return g;
        return null;
    }

    static Transform BuscarEnEscena(Scene escena, string nombre)
    {
        foreach (GameObject g in escena.GetRootGameObjects())
            if (g.name == nombre) return g.transform;
        return null;
    }

    static void CopiaDeSeguridad(Scene escena)
    {
        try
        {
            string raizProyecto = Directory.GetParent(Application.dataPath).FullName;
            string carpeta = Path.Combine(raizProyecto, "CopiasSeguridad");
            Directory.CreateDirectory(carpeta);
            string destino = Path.Combine(carpeta, $"Nave_antes_muestra_{System.DateTime.Now:yyyyMMdd_HHmmss}.unity");
            File.Copy(Path.Combine(raizProyecto, escena.path), destino, false);
            Debug.Log("[Prototipo] Copia de la escena: " + destino);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Prototipo] No se pudo copiar la escena: " + e.Message);
        }
    }

    internal static void Asignar(Object destino, string campo, Object valor)
    {
        SerializedObject so = new SerializedObject(destino);
        so.FindProperty(campo).objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void AsignarLista(Object destino, string campo, Object[] valores)
    {
        SerializedObject so = new SerializedObject(destino);
        SerializedProperty p = so.FindProperty(campo);
        p.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ======================================================================== Recursos (solo si faltan)

    internal class Recursos
    {
        public Material panel, panelOscuro, nervadura, rejilla, tira, acento, consola, pantalla, bisel, holograma;
        public VolumeProfile perfil;
    }

    internal static Recursos PrepararRecursos()
    {
        Carpetas();
        Texture2D panelColor = Textura("T_Panel_Color", 512, 512, false, ColorPanel);
        Texture2D panelNormal = Textura("T_Panel_Normal", 512, 512, true, (u, v) => NormalDesde(AlturaPanel, u, v, 512, 3f));
        Texture2D rejColor = Textura("T_Rejilla_Color", 256, 256, false, ColorRejilla);
        Texture2D rejNormal = Textura("T_Rejilla_Normal", 256, 256, true, (u, v) => NormalDesde(AlturaRejilla, u, v, 256, 2f));
        Texture2D pantallaTex = Textura("T_Pantalla_Interfaz", 512, 256, false, ColorPantalla, TextureWrapMode.Clamp);

        Recursos r = new Recursos
        {
            panel = CrearMaterial("MV_PanelCasco", new Color(0.78f, 0.83f, 0.9f), 0.55f, 0.5f, panelColor, panelNormal),
            panelOscuro = CrearMaterial("MV_PanelOscuro", new Color(0.36f, 0.42f, 0.5f), 0.6f, 0.5f, panelColor, panelNormal),
            nervadura = CrearMaterial("MV_Nervadura", new Color(0.15f, 0.18f, 0.23f), 0.8f, 0.6f),
            rejilla = CrearMaterial("MV_Rejilla", new Color(0.62f, 0.67f, 0.72f), 0.7f, 0.4f, rejColor, rejNormal),
            tira = CrearMaterial("MV_TiraLuz", Color.black, 0f, 0.5f, null, null, new Color(0.25f, 0.85f, 1f) * 2.2f),
            acento = CrearMaterial("MV_AcentoNaranja", new Color(1f, 0.55f, 0.15f), 0.2f, 0.6f, null, null, new Color(1f, 0.45f, 0.1f) * 0.8f),
            consola = CrearMaterial("MV_ConsolaCuerpo", new Color(0.12f, 0.15f, 0.2f), 0.6f, 0.55f),
            pantalla = CrearMaterial("MV_Pantalla", new Color(0.02f, 0.05f, 0.06f), 0f, 0.8f, null, null, new Color(0.7f, 1f, 1f) * 1.1f, pantallaTex),
            bisel = CrearMaterial("MV_BiselVentana", new Color(0.22f, 0.26f, 0.32f), 0.7f, 0.65f),
            holograma = CrearMaterial("MV_Holograma", Color.black, 0f, 0.5f, null, null, new Color(0.3f, 0.95f, 1f) * 1.6f),
            perfil = Perfil()
        };
        AssetDatabase.SaveAssets();
        return r;
    }

    static void Carpetas()
    {
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/_Proyecto", "Visual");
        foreach (string sub in new[] { "Texturas", "Materiales", "Perfiles" })
            if (!AssetDatabase.IsValidFolder(Carpeta + "/" + sub)) AssetDatabase.CreateFolder(Carpeta, sub);
    }

    static Material CrearMaterial(string nombre, Color baseColor, float metalico, float suavidad,
                             Texture2D colorTex = null, Texture2D normalTex = null, Color? emision = null, Texture2D emisionTex = null)
    {
        string ruta = $"{Carpeta}/Materiales/{nombre}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;                                       // no se sobrescriben ajustes del equipo

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(sh);
        m.SetColor("_BaseColor", baseColor);
        m.SetFloat("_Metallic", metalico);
        m.SetFloat("_Smoothness", suavidad);
        if (colorTex != null) m.SetTexture("_BaseMap", colorTex);
        if (normalTex != null)
        {
            m.SetTexture("_BumpMap", normalTex);
            m.EnableKeyword("_NORMALMAP");
        }
        if (emision.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision.Value);
            if (emisionTex != null) m.SetTexture("_EmissionMap", emisionTex);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static VolumeProfile Perfil()
    {
        string ruta = $"{Carpeta}/Perfiles/PerfilCabina.asset";
        VolumeProfile p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (p != null) return p;

        p = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(p, ruta);

        Bloom bloom = p.Add<Bloom>(true);
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.35f);
        bloom.scatter.Override(0.6f);

        Tonemapping tono = p.Add<Tonemapping>(true);
        tono.mode.Override(TonemappingMode.Neutral);

        ColorAdjustments ajuste = p.Add<ColorAdjustments>(true);
        ajuste.postExposure.Override(0.15f);
        ajuste.contrast.Override(10f);
        ajuste.saturation.Override(12f);

        Vignette vineta = p.Add<Vignette>(true);
        vineta.intensity.Override(0.2f);
        vineta.smoothness.Override(0.4f);

        foreach (VolumeComponent c in p.components)
        {
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, p);
        }
        EditorUtility.SetDirty(p);
        return p;
    }

    // ======================================================================== Texturas generadas

    delegate Color Generador(float u, float v);

    static Texture2D Textura(string nombre, int ancho, int alto, bool esNormal, Generador gen,
                             TextureWrapMode envoltura = TextureWrapMode.Repeat)
    {
        string ruta = $"{Carpeta}/Texturas/{nombre}.png";
        Texture2D existente = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (existente != null) return existente;

        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false, esNormal);
        Color[] px = new Color[ancho * alto];
        for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
                px[y * ancho + x] = gen((x + 0.5f) / ancho, (y + 0.5f) / alto);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = esNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        imp.sRGBTexture = !esNormal;
        imp.wrapMode = envoltura;
        imp.anisoLevel = 4;
        imp.mipmapEnabled = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static Color NormalDesde(System.Func<float, float, float> altura, float u, float v, int res, float fuerza)
    {
        float d = 1f / res;
        float dx = altura(u - d, v) - altura(u + d, v);
        float dy = altura(u, v - d) - altura(u, v + d);
        Vector3 n = new Vector3(dx * fuerza * res * 0.05f, dy * fuerza * res * 0.05f, 1f).normalized;
        return new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
    }

    // Panel: bisel en el borde, ranura interior y tornillos en las esquinas.
    static float AlturaPanel(float u, float v)
    {
        float borde = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
        float h = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(borde / 0.03f));
        float ex = Mathf.Max(0.14f - u, u - 0.86f), ey = Mathf.Max(0.14f - v, v - 0.86f);
        float sd = Mathf.Abs(Mathf.Max(ex, ey));
        h -= 0.6f * (1f - Mathf.SmoothStep(0.003f, 0.009f, sd));
        h += 0.5f * (1f - Mathf.SmoothStep(0.012f, 0.02f, DistanciaTornillo(u, v)));
        return h;
    }

    static float DistanciaTornillo(float u, float v)
    {
        float cu = u < 0.5f ? 0.065f : 0.935f, cv = v < 0.5f ? 0.065f : 0.935f;
        return Mathf.Sqrt((u - cu) * (u - cu) + (v - cv) * (v - cv));
    }

    static Color ColorPanel(float u, float v)
    {
        float ruido = Fbm(u * 8f, v * 8f, 3);
        float mancha = Fbm(u * 2f + 7f, v * 2f + 3f, 9);
        float c = Mathf.Lerp(0.82f, 1f, ruido) * Mathf.Lerp(0.88f, 1f, mancha);
        float borde = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
        if (borde < 0.012f) c *= 0.55f;
        float ex = Mathf.Max(0.14f - u, u - 0.86f), ey = Mathf.Max(0.14f - v, v - 0.86f);
        if (Mathf.Abs(Mathf.Max(ex, ey)) < 0.006f) c *= 0.45f;
        if (DistanciaTornillo(u, v) < 0.016f) c = Mathf.Min(1f, c * 1.15f);
        return new Color(c * 0.93f, c * 0.96f, c, 1f);
    }

    // Rejilla: barras y huecos.
    static float AlturaRejilla(float u, float v)
    {
        float fu = Mathf.Repeat(u * 8f, 1f), fv = Mathf.Repeat(v * 8f, 1f);
        float barra = Mathf.Max(1f - Mathf.SmoothStep(0.12f, 0.2f, Mathf.Min(fu, 1f - fu) * 2f),
                                1f - Mathf.SmoothStep(0.12f, 0.2f, Mathf.Min(fv, 1f - fv) * 2f));
        return barra;
    }

    static Color ColorRejilla(float u, float v)
    {
        float h = AlturaRejilla(u, v);
        float c = Mathf.Lerp(0.07f, 0.8f, h) * Mathf.Lerp(0.85f, 1f, Fbm(u * 10f, v * 10f, 5));
        return new Color(c * 0.95f, c * 0.97f, c, 1f);
    }

    // Fondo de pantalla: rejilla tenue y borde; el centro queda limpio para el texto.
    static Color ColorPantalla(float u, float v)
    {
        Color c = new Color(0.03f, 0.11f, 0.13f);
        bool linea = Mathf.Repeat(u * 16f, 1f) < 0.04f || Mathf.Repeat(v * 8f, 1f) < 0.04f;
        if (linea) c = new Color(0.06f, 0.2f, 0.23f);
        if (u < 0.008f || u > 0.992f || v < 0.016f || v > 0.984f) c = new Color(0.2f, 0.75f, 0.85f);
        // Pequeñas barras en la esquina inferior derecha
        if (u > 0.86f && u < 0.97f && v > 0.08f && v < 0.3f)
        {
            int barra = Mathf.FloorToInt((u - 0.86f) / 0.022f);
            float altura = 0.12f + 0.16f * Mathf.Abs(Mathf.Sin(barra * 1.7f));
            if (Mathf.Repeat((u - 0.86f) / 0.022f, 1f) < 0.7f && v < 0.08f + altura) c = new Color(1f, 0.55f, 0.18f);
        }
        return c;
    }

    static float Fbm(float x, float y, int semilla)
    {
        float suma = 0f, amp = 0.5f;
        for (int o = 0; o < 4; o++)
        {
            suma += amp * Ruido(x, y, semilla + o * 31);
            x *= 2f; y *= 2f; amp *= 0.5f;
        }
        return suma / 0.9375f;
    }

    static float Ruido(float x, float y, int s)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    static float Hash(int x, int y, int s)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + s * 982451653;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }
}
