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
/// HERRAMIENTA DE EDITOR. Acabado visual mínimo de la CABINA DE PILOTAJE de la nave ampliada (menú 9).
///
/// "Prototipo → 11. Acabado: cabina de pilotaje (aplicar)": con la escena Nave ampliada abierta y fuera de Play,
///   añade la raíz ACABADO_CABINA_PILOTAJE con paneles y nervaduras (paredes, fondo, techo y suelo de la cabina),
///   marco biselado de la ventana, consola reconocible con pantallas de datos (solo lectura), tiras de luz que
///   siguen las alertas, dos luces de acento y un posprocesado moderado.
///   - Reutiliza los generadores de texturas, materiales y la consola de MuestraVisual (no su cabina antigua).
///   - Sin colisionadores nuevos; no mueve puestos, vistas ni colisionadores; no cambia mecánicas.
///   - Oculta (no borra) el dibujo de: consola y pantalla de pilotaje (sus colisionadores siguen), listones
///     antiguos de la ventana y el rótulo flotante "PILOTAJE" (ya lo identifica el rótulo del sector).
///   - No cambia la luz ambiental. Activa el posprocesado de la cámara y guarda el valor original.
///   - Si ya existe, pregunta y lo rehace. Copia la escena en CopiasSeguridad/ antes de aplicar.
/// "… (quitar)": restaura lo ocultado y el posprocesado de la cámara y borra la raíz.
/// Después de aplicar o quitar: Ctrl+S para guardar.
/// </summary>
public static class AcabadoCabina
{
    const string NombreRaiz = "ACABADO_CABINA_PILOTAJE";

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/11. Acabado: cabina de pilotaje (aplicar)", false, 1010)]
    public static void Aplicar()
    {
        if (!Comprobar(out Scene escena)) return;

        GameObject existente = Buscar(escena, NombreRaiz);
        if (existente != null)
        {
            if (!EditorUtility.DisplayDialog("El acabado ya está aplicado",
                    "La escena ya tiene " + NombreRaiz + ".\n\n¿Rehacerlo? Se quita el actual (restaurando lo ocultado) y se vuelve a crear.",
                    "Rehacer", "Cancelar"))
                return;
            MuestraVisual.Quitar(existente);
        }

        if (escena.isDirty &&
            !EditorUtility.DisplayDialog("Guardar antes",
                "La escena tiene cambios sin guardar. Para hacer la copia de seguridad hay que guardarla primero.\n\n¿Guardar ahora?",
                "Guardar y continuar", "Cancelar"))
            return;
        if (escena.isDirty) EditorSceneManager.SaveScene(escena);
        CopiaDeSeguridad(escena);

        Transform cabina = Buscar(escena, "Cabina")?.transform;
        Camera camara = Buscar(escena, "Camara")?.GetComponent<Camera>();
        Light luzDelantera = cabina != null ? cabina.Find("LuzCabinaDelantera")?.GetComponent<Light>() : null;
        if (cabina == null || camara == null)
        {
            EditorUtility.DisplayDialog("No es la escena esperada", "No se encontraron Cabina y Camara.", "Aceptar");
            return;
        }

        MuestraVisual.Recursos r = MuestraVisual.PrepararRecursos();

        GameObject raiz = new GameObject(NombreRaiz);
        SceneManager.MoveGameObjectToScene(raiz, escena);
        CapaVisual capa = raiz.AddComponent<CapaVisual>();
        Transform t = raiz.transform;

        MuestraVisual.Ocultar(capa, cabina, "PuestoPilotaje/Consola");
        MuestraVisual.Ocultar(capa, cabina, "PuestoPilotaje/Pantalla");
        MuestraVisual.Ocultar(capa, cabina, "PuestoPilotaje/Etiqueta_PILOTAJE");
        MuestraVisual.Ocultar(capa, cabina, "MarcoVentanaArriba");
        MuestraVisual.Ocultar(capa, cabina, "MarcoVentanaAbajo");
        MuestraVisual.Ocultar(capa, cabina, "MarcoVentanaCentro");

        List<Renderer> tiras = new List<Renderer>();
        ParedesYFondo(t, r, tiras);
        TechoYSuelo(t, r, tiras);
        MuestraVisual.ConstruirFrenteYVentana(t, r);   // pared delantera y ventana: no cambian con la ampliación
        MuestraVisual.ConstruirConsola(t, r);          // puesto de pilotaje: no se movió
        MuestraVisual.ConstruirLuces(t);

        LucesReactivasVisual reactivas = raiz.AddComponent<LucesReactivasVisual>();
        MuestraVisual.Asignar(reactivas, "luzReferencia", luzDelantera);
        MuestraVisual.AsignarLista(reactivas, "tiras", tiras.ToArray());

        // Posprocesado moderado (afecta a toda la nave) + activarlo en la cámara, guardando el valor original.
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
        Debug.Log("[Prototipo] Acabado de la cabina de pilotaje aplicado. Revisa y pulsa Ctrl+S (o cierra sin guardar para descartar).");
    }

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/11. Acabado: cabina de pilotaje (quitar)", false, 1011)]
    public static void Quitar()
    {
        if (!Comprobar(out Scene escena)) return;
        GameObject raiz = Buscar(escena, NombreRaiz);
        if (raiz == null)
        {
            EditorUtility.DisplayDialog("Nada que quitar", "La escena no tiene " + NombreRaiz + ".", "Aceptar");
            return;
        }
        MuestraVisual.Quitar(raiz);
        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log("[Prototipo] Acabado de la cabina quitado y valores originales restaurados. Pulsa Ctrl+S para guardar.");
    }

    // ======================================================================== Geometría de la cabina ampliada
    // Cabina: interior x ±4 (paredes nuevas en ±4,1), z de 1,5 (fondo con abertura x ±1,2) a 5,0 (ventana).
    // Ninguna pieza tiene colisionador.

    static void ParedesYFondo(Transform raiz, MuestraVisual.Recursos r, List<Renderer> tiras)
    {
        Transform g = MuestraVisual.Grupo("ParedesYFondo", raiz);
        float[] zCol = { 2.1f, 3.2f, 4.3f };
        float[] zNerv = { 1.58f, 2.65f, 3.75f, 4.85f };

        foreach (int s in new[] { -1, 1 })
        {
            string l = s < 0 ? "Izq" : "Der";
            for (int c = 0; c < zCol.Length; c++)
            {
                MuestraVisual.Pieza($"Panel{l}_{c}_Bajo", g, new Vector3(s * 3.965f, 0.85f, zCol[c]), new Vector3(0.05f, 1.1f, 1.0f), r.panel);
                MuestraVisual.Pieza($"Panel{l}_{c}_Alto", g, new Vector3(s * 3.965f, 2.05f, zCol[c]), new Vector3(0.05f, 1.1f, 1.0f), r.panel);
            }
            foreach (float z in zNerv)
                MuestraVisual.Pieza($"Nervadura{l}_{z:0.00}", g, new Vector3(s * 3.92f, 1.32f, z), new Vector3(0.12f, 2.64f, 0.14f), r.nervadura);

            MuestraVisual.Pieza($"Zocalo{l}", g, new Vector3(s * 3.93f, 0.15f, 3.25f), new Vector3(0.1f, 0.3f, 3.4f), r.nervadura);
            tiras.Add(MuestraVisual.Pieza($"TiraZocalo{l}", g, new Vector3(s * 3.87f, 0.31f, 3.25f), new Vector3(0.03f, 0.02f, 3.4f), r.tira).GetComponent<Renderer>());

            Quaternion giro = Quaternion.Euler(0f, 0f, s * 48f);
            MuestraVisual.Pieza($"Chaflan{l}", g, new Vector3(s * 3.74f, 2.785f, 3.25f), new Vector3(0.05f, 0.66f, 3.4f), r.panelOscuro, giro);
            tiras.Add(MuestraVisual.Pieza($"TiraChaflan{l}", g, new Vector3(s * 3.70f, 2.75f, 3.25f), new Vector3(0.02f, 0.05f, 3.3f), r.tira, giro).GetComponent<Renderer>());

            // Fondo de la cabina (cara z = 1,5), a cada lado de la abertura al pasillo (x ±1,2).
            MuestraVisual.Pieza($"Fondo{l}_Ext_Bajo", g, new Vector3(s * 3.25f, 0.85f, 1.525f), new Vector3(1.3f, 1.1f, 0.05f), r.panel);
            MuestraVisual.Pieza($"Fondo{l}_Ext_Alto", g, new Vector3(s * 3.25f, 2.05f, 1.525f), new Vector3(1.3f, 1.1f, 0.05f), r.panel);
            MuestraVisual.Pieza($"Fondo{l}_Int_Bajo", g, new Vector3(s * 1.9f, 0.85f, 1.525f), new Vector3(1.2f, 1.1f, 0.05f), r.panelOscuro);
            MuestraVisual.Pieza($"Fondo{l}_Int_Alto", g, new Vector3(s * 1.9f, 2.05f, 1.525f), new Vector3(1.2f, 1.1f, 0.05f), r.panelOscuro);
            // Marco de la abertura al pasillo, con acento naranja (no estrecha el paso: queda sobre la pared).
            MuestraVisual.Pieza($"MarcoPuerta{l}", g, new Vector3(s * 1.31f, 1.5f, 1.56f), new Vector3(0.16f, 3.0f, 0.1f), r.nervadura);
            MuestraVisual.Pieza($"AcentoPuerta{l}", g, new Vector3(s * 1.31f, 1.5f, 1.615f), new Vector3(0.04f, 2.9f, 0.02f), r.acento);
        }
    }

    static void TechoYSuelo(Transform raiz, MuestraVisual.Recursos r, List<Renderer> tiras)
    {
        Transform techo = MuestraVisual.Grupo("Techo", raiz);
        float[] zFila = { 2.1f, 3.2f, 4.3f };
        foreach (int s in new[] { -1, 1 })
            for (int f = 0; f < zFila.Length; f++)
                MuestraVisual.Pieza($"PanelTecho_{(s < 0 ? "Izq" : "Der")}_{f}", techo, new Vector3(s * 1.95f, 2.975f, zFila[f]), new Vector3(3.0f, 0.05f, 1.0f), r.panel);
        MuestraVisual.Pieza("CanalLuzTecho", techo, new Vector3(0f, 2.975f, 3.25f), new Vector3(0.9f, 0.05f, 3.4f), r.panelOscuro);
        tiras.Add(MuestraVisual.Pieza("TiraTecho", techo, new Vector3(0f, 2.945f, 3.25f), new Vector3(0.28f, 0.02f, 3.3f), r.tira).GetComponent<Renderer>());
        foreach (float z in new[] { 1.58f, 2.65f, 3.75f, 4.85f })
            MuestraVisual.Pieza($"ArcoTecho_{z:0.00}", techo, new Vector3(0f, 2.9f, z), new Vector3(7.8f, 0.12f, 0.14f), r.nervadura);

        // Suelo de rejilla muy fino (0,008 m) para que el borde de color del sector siga viéndose encima.
        Transform suelo = MuestraVisual.Grupo("Suelo", raiz);
        foreach (float x in new[] { -2.45f, 0f, 2.45f })
            foreach (float z in new[] { 2.1f, 3.15f })
                MuestraVisual.Pieza($"Rejilla_{x:0.0}_{z:0.0}", suelo, new Vector3(x, 0.004f, z), new Vector3(2.3f, 0.008f, 1.02f), r.rejilla);
        foreach (float x in new[] { -2.45f, 2.45f })
            MuestraVisual.Pieza($"Rejilla_{x:0.0}_frente", suelo, new Vector3(x, 0.004f, 4.32f), new Vector3(2.3f, 0.008f, 1.26f), r.rejilla);
        foreach (int s in new[] { -1, 1 })
            tiras.Add(MuestraVisual.Pieza($"TiraSuelo{(s < 0 ? "Izq" : "Der")}", suelo, new Vector3(s * 1.25f, 0.02f, 3.25f), new Vector3(0.04f, 0.012f, 3.4f), r.tira).GetComponent<Renderer>());
    }

    // ======================================================================== Utilidades

    static bool Comprobar(out Scene escena)
    {
        escena = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Sal de Play", "Este menú solo funciona fuera del modo Play.", "Aceptar");
            return false;
        }
        if (escena.name != "Nave")
        {
            EditorUtility.DisplayDialog("Abre la escena Nave", "Este menú trabaja sobre la escena Nave. Ábrela primero.", "Aceptar");
            return false;
        }
        if (Buscar(escena, "NAVE_AMPLIADA") == null)
        {
            EditorUtility.DisplayDialog("Falta la ampliación",
                "Este acabado está pensado para la nave ampliada. Ejecuta antes «Prototipo → 9. Ampliar nave».", "Aceptar");
            return false;
        }
        return true;
    }

    static GameObject Buscar(Scene escena, string nombre)
    {
        foreach (GameObject g in escena.GetRootGameObjects())
            if (g.name == nombre) return g;
        return null;
    }

    static void CopiaDeSeguridad(Scene escena)
    {
        try
        {
            string raizProyecto = Directory.GetParent(Application.dataPath).FullName;
            string carpeta = Path.Combine(raizProyecto, "CopiasSeguridad");
            Directory.CreateDirectory(carpeta);
            string destino = Path.Combine(carpeta, $"Nave_antes_acabado_cabina_{System.DateTime.Now:yyyyMMdd_HHmmss}.unity");
            File.Copy(Path.Combine(raizProyecto, escena.path), destino, false);
            Debug.Log("[Prototipo] Copia de la escena: " + destino);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Prototipo] No se pudo copiar la escena: " + e.Message);
        }
    }
}
