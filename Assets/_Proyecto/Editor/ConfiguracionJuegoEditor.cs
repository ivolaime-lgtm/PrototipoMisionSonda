using System.Collections.Generic;
using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HERRAMIENTA DE EDITOR — configuración central (adaptación a requisitos). Guía: Documentacion/ADAPTACION_REQUISITOS.md
///
/// "Prototipo → Juego → Configuración → 1. Crear archivo de configuración (valores actuales)"
///   Crea Assets/_Proyecto/Resources/ConfiguracionJuego.asset con los MISMOS valores que usa el juego sin archivo
///   (tarifas, límites, opciones, misiones, rótulos, planos de cámara, puntos, proporciones). Si ya existe, solo lo
///   selecciona: nunca lo sobrescribe.
/// "… → 2. Comprobar configuración": revisa dependencias (alturas y radios de la L-4 frente a grúa, puerta y plataformas;
///   VC-1 frente a la cofia; límites; opciones; misiones; planos). No cambia nada.
/// "… → 3. Seleccionar el archivo de configuración".
/// </summary>
public static class ConfiguracionJuegoEditor
{
    // Medidas del escenario que dependen de las proporciones (las fija el código del hangar y de las secuencias).
    const float BaseL4EnHangar = 2f;          // HangarTerrestre: la etapa apoya a 2 m (vehiculo.y)
    const float GanchoGrua = 42f - 1.6f;      // IntegracionLanzadera: AlturaPuente − holgura del gancho
    const float AltoPuertaSalida = 42f;       // edificio de integración: puerta de 16 × 42 m
    const float AnchoPuertaSalida = 16f;
    const float PlataformasDesdeEje = 4.7f;   // HangarTerrestre: plataformas de trabajo hasta 4,7 m del eje
    const float PlataformaMovil = 3f;         // secuencia: plataforma móvil (2 m) + apoyo (1 m)
    const float LargoVC1 = 32.5f, DiametroVC1 = 7.6f;   // ModeloTransporte (casco provisional)

    [MenuItem("Prototipo/Juego/Configuración/1. Crear archivo de configuración (valores actuales)", false, 30)]
    public static void Crear()
    {
        ConfiguracionJuego existente = AssetDatabase.LoadAssetAtPath<ConfiguracionJuego>(ConfiguracionJuego.RutaAsset);
        if (existente != null)
        {
            Selection.activeObject = existente;
            EditorGUIUtility.PingObject(existente);
            EditorUtility.DisplayDialog("Ya existe", "El archivo de configuración ya existe y no se sobrescribe:\n" + ConfiguracionJuego.RutaAsset, "Aceptar");
            return;
        }
        string carpeta = Path.GetDirectoryName(ConfiguracionJuego.RutaAsset).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder("Assets/_Proyecto", "Resources");

        ConfiguracionJuego c = ScriptableObject.CreateInstance<ConfiguracionJuego>();
        CatalogoMision cat = null;
        foreach (string g in AssetDatabase.FindAssets("t:CatalogoMision"))
        {
            cat = AssetDatabase.LoadAssetAtPath<CatalogoMision>(AssetDatabase.GUIDToAssetPath(g));
            if (cat != null) break;
        }
        CatalogoNave nave = cat != null ? cat.catalogoNave : null;
        c.opciones.motoresEM4X = BalanceEM4X.MotoresPorDefecto(nave);
        c.opciones.tanquesEM4X = BalanceEM4X.TanquesPorDefecto(nave);
        c.opciones.bodegasL4 = DatosLanzadera.BodegasPorDefecto();
        c.opciones.tanquesL4 = DatosLanzadera.TanquesPorDefecto();
        c.opciones.refuerzosL4 = DatosLanzadera.RefuerzosPorDefecto();
        // misiones.lista se deja VACÍA: así los textos de los pasos salen de «condiciones» (menú 4 para copiarlas y editarlas)
        c.camaras = ConfiguracionJuego.PlanosPorDefecto();
        c.textos = new List<TextoConfigurable>();
        foreach (KeyValuePair<string, string> kv in ConfiguracionJuego.TextosPorDefecto)
            c.textos.Add(new TextoConfigurable { clave = kv.Key, texto = kv.Value });
        AssetDatabase.CreateAsset(c, ConfiguracionJuego.RutaAsset);
        AssetDatabase.SaveAssets();
        ConfiguracionJuego.Recargar();
        Selection.activeObject = c;
        EditorGUIUtility.PingObject(c);
        Debug.Log("[Configuración] Creado " + ConfiguracionJuego.RutaAsset + " con los valores actuales. Nada cambia hasta que se editen.");
        EditorUtility.DisplayDialog("Configuración creada",
            "Creado " + ConfiguracionJuego.RutaAsset + " con los valores actuales del juego.\n\nNada cambia hasta que se edite. " +
            "Después de editarlo usa «2. Comprobar configuración».", "Aceptar");
    }

    [MenuItem("Prototipo/Juego/Configuración/2. Comprobar configuración", false, 31)]
    public static void Comprobar()
    {
        ConfiguracionJuego.Recargar();
        ConfiguracionJuego c = ConfiguracionJuego.Actual;
        List<string> errores = new List<string>(), avisos = new List<string>();

        // L-4: proporciones visuales frente al edificio de integración y a la secuencia
        ProporcionesL4 l = c.lanzadera;
        float maxT = 0f, maxB = 0f;
        for (int i = 0; i < 3; i++) { maxT = Mathf.Max(maxT, l.Tanque(i)); maxB = Mathf.Max(maxB, l.Bodega(i)); }
        float alto = l.alturaEtapa + maxT + maxB + 1f + l.alturaCapsula + 1.3f + l.alturaTorreEscape;
        if (BaseL4EnHangar + alto > GanchoGrua)
            errores.Add($"L-4: con tanque y bodega máximos mide {alto:0.0} m; sobre su apoyo ({BaseL4EnHangar} m) llega a {BaseL4EnHangar + alto:0.0} m y el gancho del puente grúa trabaja hasta {GanchoGrua:0.0} m (IntegracionLanzadera.AlturaPuente).");
        if (PlataformaMovil + alto > AltoPuertaSalida)
            errores.Add($"L-4: sobre la plataforma móvil llega a {PlataformaMovil + alto:0.0} m y la puerta de salida mide {AltoPuertaSalida} m.");
        if (l.RadioMaximo > PlataformasDesdeEje)
            errores.Add($"L-4: con refuerzos ocupa {l.RadioMaximo:0.00} m desde el eje y las plataformas de trabajo llegan a {PlataformasDesdeEje} m (HangarTerrestre, «Plataforma de trabajo»).");
        if (l.RadioMaximo * 2f > AnchoPuertaSalida - 2f)
            avisos.Add($"L-4: ancho total {l.RadioMaximo * 2f:0.0} m frente a una puerta de {AnchoPuertaSalida} m (deja poco margen).");
        if (l.diametroCapsula < 4.4f)
            avisos.Add("L-4: con la cápsula de menos de 4,4 m de diámetro, la cabina interior provisional (3,6 × 2,8 m) asoma por fuera.");
        if (l.diametroCapsula > 5.3f)
            avisos.Add("L-4: una cápsula de más de 5,3 m de diámetro puede tocar el casco del Mando al atracar (revisa el plano «l4.atraque»).");
        if (l.diametro <= 0f || l.alturaEtapa <= 0f || l.diametroRefuerzo <= 0f) errores.Add("L-4: hay medidas a cero o negativas.");

        // LC-2 y VC-1
        ProporcionesLC2 v = c.lanzadorVC1;
        if (v.alturaCofia + 0.5f < LargoVC1) errores.Add($"LC-2: la cofia ({v.alturaCofia} m) no cubre el VC-1 ({LargoVC1} m apoyado a 0,5 m bajo la cara de la etapa).");
        if (v.diametroCofia < DiametroVC1 + 0.4f) errores.Add($"LC-2: la cofia (Ø {v.diametroCofia} m) es demasiado estrecha para el VC-1 (Ø {DiametroVC1} m).");
        if (v.separacionAcelerador < v.diametroEtapa / 2f + v.diametroAcelerador / 2f)
            errores.Add("LC-2: los aceleradores se cruzan con la etapa central (separación menor que la suma de radios).");

        // Transporte VC-1 (reglas de juego)
        TransporteConfig t = c.transporte;
        if (t.diametroBodegaM > DiametroVC1) avisos.Add($"VC-1: la regla admite Ø {t.diametroBodegaM} m pero el casco provisional mide Ø {DiametroVC1} m.");
        if (t.largoBodegaM > 23f) avisos.Add($"VC-1: la regla admite {t.largoBodegaM} m de bodega pero la bodega provisional mide 23 m (las piezas sobresaldrían).");
        if (t.modulosCombustibleMax > 12) avisos.Add("VC-1: el anillo provisional tiene sitio para 12 módulos de combustible.");
        if (t.masaMaximaKg <= 0f) errores.Add("VC-1: masa máxima no válida.");

        // Tarifas
        TarifasConfig f = c.tarifas;
        if (f.transporteOrbitalMUSD < 0f || f.instalacionMUSD < 0f || f.lanzamientoTripulacionMUSD < 0f || f.kitMUSD < 0f || f.suministroMUSD < 0f || f.tramoCombustibleMUSD < 0f)
            errores.Add("Tarifas: hay valores negativos.");
        if (f.maximoKitsExtra < 0 || f.maximoSuministrosExtra < 0) errores.Add("Tarifas: máximos extra negativos.");

        // Opciones (como mucho 3 por tipo: lo que admite el holograma y la integración)
        void Opc(string n, int cuenta) { if (cuenta > 3) errores.Add($"Opciones: «{n}» tiene {cuenta} entradas y el juego admite 3."); }
        Opc("motoresEM4X", c.opciones.motoresEM4X.Count); Opc("tanquesEM4X", c.opciones.tanquesEM4X.Count);
        Opc("bodegasL4", c.opciones.bodegasL4.Count); Opc("tanquesL4", c.opciones.tanquesL4.Count); Opc("refuerzosL4", c.opciones.refuerzosL4.Count);
        foreach (List<OpcionLanzadera> lo in new[] { c.opciones.bodegasL4, c.opciones.tanquesL4, c.opciones.refuerzosL4 })
            if (lo.Count > 0 && lo[0].costeMUSD != 0f) avisos.Add($"Opciones L-4: la opción 0 de «{lo[0].nombre}» cuesta {lo[0].costeMUSD} M (la 0 es la L-4 de siempre, incluida en la tarifa).");
        if (c.opciones.motoresEM4X.Count > 0 && c.opciones.motoresEM4X[0].costeMUSD != 0f) avisos.Add("Opciones EM-4X: el motor 0 es el instalado (coste 0 hasta ahora).");
        if (c.opciones.tanquesEM4X.Count > 0 && c.opciones.tanquesEM4X[0].costeMUSD != 0f) avisos.Add("Opciones EM-4X: el tanque 0 es el instalado (coste 0 hasta ahora).");

        // Misiones
        CondicionesMision m = c.misiones.condiciones;
        if (m.bandaMinM >= m.bandaMaxM) errores.Add("Misión 1: la banda de observación tiene el mínimo mayor o igual que el máximo.");
        if (m.cumuloMinM >= m.cumuloMaxM) errores.Add("Misión 3: el punto de observación tiene el mínimo mayor o igual que el máximo.");
        if (m.regresoDistanciaM >= m.salidaBaseM) avisos.Add("Misiones: la distancia de regreso es mayor o igual que la de salida (el paso 1 y el regreso se solapan).");
        if (c.misiones.lista.Count > 0 && c.misiones.lista.Count != 3) avisos.Add($"Misiones: hay {c.misiones.lista.Count}; la lógica (EstadoExpedicion) conoce las ids 0, 1 y 2.");
        foreach (DefinicionMisionMVP d in c.misiones.lista)
            if (d.id < 0 || d.id > 2) errores.Add($"Misiones: «{d.nombre}» tiene id {d.id}; solo existen 0, 1 y 2.");
        if (c.misiones.lista.Count > 0)
            avisos.Add("Misiones: con lista en el archivo, los números de los pasos NO se recalculan solos al cambiar las condiciones: revisa los textos.");

        // Secuencias y planos
        if (c.secuencias.velocidad <= 0f) errores.Add("Secuencias: velocidad no válida.");
        HashSet<string> ids = new HashSet<string>();
        foreach (PlanoCamara p in c.camaras) if (p != null && !ids.Add(p.id)) errores.Add($"Cámaras: el plano «{p.id}» está repetido.");
        foreach (PlanoCamara p in c.camaras)
        {
            bool conocido = false;
            foreach (PlanoCamara q in ConfiguracionJuego.PlanosPorDefecto()) if (q.id == p.id) conocido = true;
            if (!conocido) avisos.Add($"Cámaras: el plano «{p.id}» no lo usa ninguna secuencia.");
        }
        foreach (TextoConfigurable x in c.textos)
            if (x != null && !ConfiguracionJuego.TextosPorDefecto.ContainsKey(x.clave)) avisos.Add($"Textos: la clave «{x.clave}» no la usa ninguna secuencia.");

        string fuente = ConfiguracionJuego.UsaValoresPorDefecto ? "SIN ARCHIVO (valores del código)" : ConfiguracionJuego.RutaAsset;
        string informe = $"Configuración: {fuente}\n\nERRORES ({errores.Count}):\n" + (errores.Count == 0 ? "  ninguno\n" : "  · " + string.Join("\n  · ", errores) + "\n") +
                         $"\nAVISOS ({avisos.Count}):\n" + (avisos.Count == 0 ? "  ninguno\n" : "  · " + string.Join("\n  · ", avisos) + "\n") +
                         "\nRepite las pruebas indicadas en ADAPTACION_REQUISITOS.md para lo que hayas cambiado.";
        if (errores.Count > 0) Debug.LogWarning("[Configuración] " + informe); else Debug.Log("[Configuración] " + informe);
        EditorUtility.DisplayDialog(errores.Count > 0 ? "Configuración: hay errores" : "Configuración comprobada", informe, "Aceptar");
    }

    [MenuItem("Prototipo/Juego/Configuración/4. Copiar las tres misiones al archivo (para editar sus textos)", false, 33)]
    public static void CopiarMisiones()
    {
        ConfiguracionJuego c = AssetDatabase.LoadAssetAtPath<ConfiguracionJuego>(ConfiguracionJuego.RutaAsset);
        if (c == null) { EditorUtility.DisplayDialog("Sin archivo", "Primero usa «1. Crear archivo de configuración».", "Aceptar"); return; }
        if (c.misiones.lista.Count > 0 && !EditorUtility.DisplayDialog("Ya hay misiones", "El archivo ya tiene misiones. ¿Sustituirlas por las del código?", "Sustituir", "Cancelar")) return;
        if (!EditorUtility.DisplayDialog("Copiar misiones",
                "Se copian las tres misiones (nombres, resúmenes, pasos). A partir de entonces sus textos NO se recalculan al cambiar las condiciones: tendrás que mantener los números a mano.",
                "Copiar", "Cancelar")) return;
        Undo.RecordObject(c, "Copiar misiones");
        ConfiguracionJuego.Recargar();
        c.misiones.lista = DefinicionMisionMVP.PorDefecto();
        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        ConfiguracionJuego.Recargar();
    }

    [MenuItem("Prototipo/Juego/Configuración/3. Seleccionar el archivo de configuración", false, 32)]
    public static void Seleccionar()
    {
        ConfiguracionJuego c = AssetDatabase.LoadAssetAtPath<ConfiguracionJuego>(ConfiguracionJuego.RutaAsset);
        if (c == null) { EditorUtility.DisplayDialog("Sin archivo", "Aún no existe. Usa «1. Crear archivo de configuración».", "Aceptar"); return; }
        Selection.activeObject = c;
        EditorGUIUtility.PingObject(c);
    }
}
