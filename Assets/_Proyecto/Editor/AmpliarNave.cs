using System.Collections.Generic;
using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego). Amplía la escena Nave ABIERTA, sin regenerarla.
///
/// "Prototipo → 9. Ampliar nave (4 sectores y pasillo)":
///   Plano (vista desde arriba, z hacia la ventana):
///     Pilotaje      x ±4      z +1,5 … +5     (cabina actual, no se mueve)
///     Pasillo       x ±1,2    z −9 … +1,5     (2,4 m de ancho)
///     Comunicaciones x 1,4…5,2  z −3,5 … +0,5  (puerta de 1,4 m en z −2,2 … −0,8)
///     Ciencia        x −5,2…−1,4 z −3,5 … +0,5 (puerta de 1,4 m)
///     Ingeniería    x ±3,5    z −14 … −9      (barandilla sólida y bahía con el motor y el tanque elegidos)
///   - Desactiva (no borra) ParedIzquierda, ParedDerecha y ParedTrasera de la cabina original.
///   - Mueve los puestos de Ingeniería, Comunicaciones y Ciencia (con su consola, pantalla, luz, rótulo y vista).
///   - Añade paredes, suelos y techos con colisionador, cuatro luces que pasan a la lista de alertas de ControlNave,
///     y sube el aviso de escombros del modo de prueba de 4 a 8 s.
///   - Guarda todo lo original en RegistroAmpliacion. Copia la escena guardada en CopiasSeguridad/ antes de cambiarla.
///   - Si ya está ampliada, pregunta y la rehace (sin duplicados).
///
/// "Prototipo → 10. Volver a la cabina original": restaura puestos, vistas, paredes, luces de alerta y tiempos,
/// y borra NAVE_AMPLIADA.
///
/// La escena queda modificada SIN GUARDAR: Ctrl+S para conservar el cambio.
/// </summary>
public static class AmpliarNave
{
    const string NombreRaiz = "NAVE_AMPLIADA";
    const string Materiales = "Assets/_Proyecto/Materials/";

    [MenuItem("Prototipo/9. Ampliar nave (4 sectores y pasillo)")]
    public static void Ampliar()
    {
        if (!Comprobar(out Scene escena, out Transform cabina, out ControlNave control)) return;

        GameObject existente = BuscarRaiz(escena);
        if (existente != null)
        {
            if (!EditorUtility.DisplayDialog("La nave ya está ampliada",
                    "La escena ya tiene " + NombreRaiz + ".\n\n¿Rehacerla? Primero se restaura la cabina original y después se vuelve a ampliar.",
                    "Rehacer", "Cancelar"))
                return;
            Restaurar(existente);
        }

        if (escena.isDirty &&
            !EditorUtility.DisplayDialog("Guardar antes",
                "La escena tiene cambios sin guardar. Para hacer la copia de seguridad hay que guardarla primero.\n\n¿Guardar ahora?",
                "Guardar y continuar", "Cancelar"))
            return;
        if (escena.isDirty) EditorSceneManager.SaveScene(escena);
        CopiaDeSeguridad(escena);

        // Comprobar que los puestos están en su posición original antes de moverlos.
        Dictionary<TipoPuesto, PuestoNave> puestos = new Dictionary<TipoPuesto, PuestoNave>();
        foreach (PuestoNave p in Object.FindObjectsByType<PuestoNave>(FindObjectsSortMode.None))
            if (p.gameObject.scene == escena) puestos[p.Tipo] = p;
        foreach (TipoPuesto t in new[] { TipoPuesto.Ingenieria, TipoPuesto.Comunicaciones, TipoPuesto.Ciencia })
        {
            if (!puestos.ContainsKey(t))
            {
                EditorUtility.DisplayDialog("Falta un puesto", "No se encontró el puesto " + t + ". No se ha cambiado nada.", "Aceptar");
                return;
            }
            Transform tr = puestos[t].transform;
            if (tr.localPosition.sqrMagnitude > 0.0001f || Quaternion.Angle(tr.localRotation, Quaternion.identity) > 0.1f)
            {
                EditorUtility.DisplayDialog("Puesto movido",
                    "El puesto " + t + " no está en su posición original (0,0,0). No se ha cambiado nada para no descolocarlo.", "Aceptar");
                return;
            }
        }

        Material pared = Mat("CabinaInterior", new Color(0.55f, 0.58f, 0.62f));
        Material suelo = Mat("SueloCabina", new Color(0.22f, 0.24f, 0.27f));
        Material estructura = Mat("Estructura", new Color(0.35f, 0.36f, 0.38f));

        GameObject raiz = new GameObject(NombreRaiz);
        SceneManager.MoveGameObjectToScene(raiz, escena);
        RegistroAmpliacion reg = raiz.AddComponent<RegistroAmpliacion>();
        Transform t0 = raiz.transform;

        // --- 1. Paredes originales que atravesarían el pasillo y las salas: se desactivan (no se borran).
        foreach (string n in new[] { "ParedIzquierda", "ParedDerecha", "ParedTrasera" })
        {
            Transform w = cabina.Find(n);
            if (w == null) { Debug.LogWarning("[Prototipo] Ampliar nave: no se encontró Cabina/" + n); continue; }
            if (!w.gameObject.activeSelf) continue;
            w.gameObject.SetActive(false);
            reg.desactivados.Add(w.gameObject);
        }

        // --- 2. Paredes nuevas (todas con colisionador). Interior a 3 m de alto: y de 0 a 3.
        Transform paredes = Grupo("Paredes", t0);
        foreach (int s in new[] { -1, 1 })
        {
            string l = s < 0 ? "Izq" : "Der";
            Bloque("CabinaLateral" + l, paredes, new Vector3(s * 4.1f, 1.5f, 3.35f), new Vector3(0.2f, 3f, 3.7f), pared);   // z 1,5…5,2
            Bloque("CabinaFondo" + l, paredes, new Vector3(s * 2.7f, 1.5f, 1.4f), new Vector3(3.0f, 3f, 0.2f), pared);      // abertura x ±1,2
            Bloque("PasilloA" + l, paredes, new Vector3(s * 1.3f, 1.5f, 0.25f), new Vector3(0.2f, 3f, 2.1f), pared);        // z 1,3…−0,8
            Bloque("PasilloB" + l, paredes, new Vector3(s * 1.3f, 1.5f, -5.6f), new Vector3(0.2f, 3f, 6.8f), pared);        // z −2,2…−9 (puerta entre A y B)
            Bloque("SalaFrente" + l, paredes, new Vector3(s * 3.4f, 1.5f, 0.6f), new Vector3(4.0f, 3f, 0.2f), pared);
            Bloque("SalaFondo" + l, paredes, new Vector3(s * 3.4f, 1.5f, -3.6f), new Vector3(4.0f, 3f, 0.2f), pared);
            Bloque("SalaExterior" + l, paredes, new Vector3(s * 5.3f, 1.5f, -1.5f), new Vector3(0.2f, 3f, 4.4f), pared);
            Bloque("IngenieriaFrente" + l, paredes, new Vector3(s * 2.45f, 1.5f, -9.1f), new Vector3(2.5f, 3f, 0.2f), pared); // abertura x ±1,2
            Bloque("IngenieriaLateral" + l, paredes, new Vector3(s * 3.6f, 1.5f, -11.6f), new Vector3(0.2f, 3f, 5.2f), pared);
        }
        Bloque("IngenieriaPopa", paredes, new Vector3(0f, 1.5f, -14.1f), new Vector3(7.4f, 3f, 0.2f), pared);
        Bloque("BarandillaBahia", paredes, new Vector3(0f, 0.5f, -12.75f), new Vector3(7.0f, 1.0f, 0.1f), estructura);

        // --- 3. Suelos y techos solo donde no llega la cabina original (x ±4,2, z ±5,2): sin solapes ni huecos.
        Transform suelos = Grupo("SuelosYTechos", t0);
        foreach (int s in new[] { -1, 1 })
            SueloYTecho("Sala" + (s < 0 ? "Izq" : "Der"), suelos, new Vector3(s * 4.8f, 0f, -1.5f), new Vector2(1.2f, 4.4f), suelo, pared);
        SueloYTecho("Pasillo", suelos, new Vector3(0f, 0f, -7.1f), new Vector2(2.8f, 3.8f), suelo, pared);
        SueloYTecho("Ingenieria", suelos, new Vector3(0f, 0f, -11.6f), new Vector2(7.4f, 5.2f), suelo, pared);

        // --- 4. Mover puestos (consola, pantalla, luz de estado, rótulo y vista se mueven juntos).
        Mover(reg, puestos[TipoPuesto.Comunicaciones].transform, new Vector3(1.2f, 0f, -2.0f), Quaternion.identity);
        Mover(reg, puestos[TipoPuesto.Ciencia].transform, new Vector3(-0.25f, 0f, -1.5f), Quaternion.Euler(0f, 90f, 0f));
        Mover(reg, puestos[TipoPuesto.Ingenieria].transform, new Vector3(0.5f, 0f, -8.45f), Quaternion.Euler(0f, -90f, 0f));

        // --- 5. Luces de las salas, añadidas a las alertas de ControlNave (rojo en avería, ámbar con escombros).
        Transform luces = Grupo("Luces", t0);
        List<Light> nuevas = new List<Light>
        {
            Luz("LuzComunicaciones", luces, new Vector3(3.3f, 2.8f, -1.5f)),
            Luz("LuzCiencia", luces, new Vector3(-3.3f, 2.8f, -1.5f)),
            Luz("LuzPasilloPopa", luces, new Vector3(0f, 2.8f, -6.5f)),
            Luz("LuzIngenieria", luces, new Vector3(0f, 2.8f, -11.5f))
        };
        SerializedObject so = new SerializedObject(control);
        SerializedProperty lista = so.FindProperty("lucesCabina");
        reg.control = control;
        for (int i = 0; i < lista.arraySize; i++)
            reg.lucesOriginales.Add(lista.GetArrayElementAtIndex(i).objectReferenceValue as Light);
        int n0 = lista.arraySize;
        lista.arraySize = n0 + nuevas.Count;
        for (int i = 0; i < nuevas.Count; i++) lista.GetArrayElementAtIndex(n0 + i).objectReferenceValue = nuevas[i];

        // --- 6. Aviso de escombros del modo de prueba: 4 → 8 s (el modo normal ya tiene 8 s).
        SerializedProperty aviso = so.FindProperty("tiemposPrueba.avisoEscombrosS");
        if (aviso != null)
        {
            reg.avisoGuardado = true;
            reg.avisoPruebaOriginal = aviso.floatValue;
            aviso.floatValue = Mathf.Max(aviso.floatValue, 8f);
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // --- 7. Bahía de motor: muestra el motor y el tanque elegidos (se construye al empezar la partida).
        GameObject bahia = new GameObject("BahiaMotor");
        bahia.transform.SetParent(t0, false);
        bahia.transform.localPosition = new Vector3(0f, 0f, -13.4f);
        bahia.AddComponent<BahiaMotorNave>();

        EditorUtility.SetDirty(reg);
        EditorUtility.SetDirty(control);
        EditorSceneManager.MarkSceneDirty(escena);
        Selection.activeGameObject = raiz;
        Debug.Log("[Prototipo] Nave ampliada: pasillo, 4 sectores y bahía de motor. Revisa y pulsa Ctrl+S para guardar " +
                  "(o cierra la escena sin guardar para descartar). Para volver: Prototipo → 10.");
    }

    [MenuItem("Prototipo/10. Volver a la cabina original")]
    public static void Volver()
    {
        if (!Comprobar(out Scene escena, out _, out _)) return;
        GameObject raiz = BuscarRaiz(escena);
        if (raiz == null)
        {
            EditorUtility.DisplayDialog("Nada que restaurar", "La escena no tiene " + NombreRaiz + ".", "Aceptar");
            return;
        }
        Restaurar(raiz);
        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log("[Prototipo] Cabina original restaurada (puestos, vistas, paredes, luces de alerta y tiempos). Pulsa Ctrl+S para guardar.");
    }

    static void Restaurar(GameObject raiz)
    {
        RegistroAmpliacion reg = raiz.GetComponent<RegistroAmpliacion>();
        if (reg != null)
        {
            for (int i = 0; i < reg.puestosMovidos.Count; i++)
            {
                Transform t = reg.puestosMovidos[i];
                if (t == null) continue;
                t.localPosition = reg.posicionesOriginales[i];
                t.localRotation = reg.rotacionesOriginales[i];
                EditorUtility.SetDirty(t);
            }
            foreach (GameObject g in reg.desactivados)
                if (g != null) { g.SetActive(true); EditorUtility.SetDirty(g); }

            // Primero se devuelve la lista original de luces, para que no queden referencias a las luces que se borran.
            if (reg.control != null)
            {
                SerializedObject so = new SerializedObject(reg.control);
                SerializedProperty lista = so.FindProperty("lucesCabina");
                lista.arraySize = reg.lucesOriginales.Count;
                for (int i = 0; i < reg.lucesOriginales.Count; i++)
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = reg.lucesOriginales[i];
                if (reg.avisoGuardado)
                {
                    SerializedProperty aviso = so.FindProperty("tiemposPrueba.avisoEscombrosS");
                    if (aviso != null) aviso.floatValue = reg.avisoPruebaOriginal;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(reg.control);
            }
        }
        Object.DestroyImmediate(raiz);
    }

    // ======================================================================== Utilidades

    static bool Comprobar(out Scene escena, out Transform cabina, out ControlNave control)
    {
        escena = SceneManager.GetActiveScene();
        cabina = null;
        control = null;
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
        foreach (GameObject g in escena.GetRootGameObjects())
        {
            if (g.name == "Cabina") cabina = g.transform;
            if (control == null) control = g.GetComponentInChildren<ControlNave>();
        }
        if (cabina == null || control == null)
        {
            EditorUtility.DisplayDialog("No es la escena esperada", "No se encontraron Cabina y ControlNave en la escena.", "Aceptar");
            return false;
        }
        return true;
    }

    static void Mover(RegistroAmpliacion reg, Transform t, Vector3 pos, Quaternion rot)
    {
        reg.puestosMovidos.Add(t);
        reg.posicionesOriginales.Add(t.localPosition);
        reg.rotacionesOriginales.Add(t.localRotation);
        t.localPosition = pos;
        t.localRotation = rot;
        EditorUtility.SetDirty(t);
    }

    static Transform Grupo(string nombre, Transform padre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        return g.transform;
    }

    /// <summary>Caja sólida CON colisionador.</summary>
    static GameObject Bloque(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material m)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static void SueloYTecho(string nombre, Transform padre, Vector3 centro, Vector2 tam, Material suelo, Material techo)
    {
        Bloque("Suelo" + nombre, padre, new Vector3(centro.x, -0.1f, centro.z), new Vector3(tam.x, 0.2f, tam.y), suelo);
        Bloque("Techo" + nombre, padre, new Vector3(centro.x, 3.1f, centro.z), new Vector3(tam.x, 0.2f, tam.y), techo);
    }

    static Light Luz(string nombre, Transform padre, Vector3 pos)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 8f;
        l.intensity = 2.5f;
        l.color = new Color(0.92f, 0.96f, 1f);
        l.shadows = LightShadows.None;
        return l;
    }

    static Material Mat(string nombre, Color respaldo)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(Materiales + nombre + ".mat");
        if (m != null) return m;
        Debug.LogWarning("[Prototipo] Ampliar nave: no se encontró el material " + nombre + "; se usa uno temporal.");
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(sh != null ? sh : Shader.Find("Standard"));
        m.SetColor("_BaseColor", respaldo);
        return m;
    }

    static GameObject BuscarRaiz(Scene escena)
    {
        foreach (GameObject g in escena.GetRootGameObjects())
            if (g.name == NombreRaiz) return g;
        return null;
    }

    static void CopiaDeSeguridad(Scene escena)
    {
        try
        {
            string raizProyecto = Directory.GetParent(Application.dataPath).FullName;
            string carpeta = Path.Combine(raizProyecto, "CopiasSeguridad");
            Directory.CreateDirectory(carpeta);
            string destino = Path.Combine(carpeta, $"Nave_antes_ampliacion_{System.DateTime.Now:yyyyMMdd_HHmmss}.unity");
            File.Copy(Path.Combine(raizProyecto, escena.path), destino, false);
            Debug.Log("[Prototipo] Copia de la escena: " + destino);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Prototipo] No se pudo copiar la escena: " + e.Message);
        }
    }
}
