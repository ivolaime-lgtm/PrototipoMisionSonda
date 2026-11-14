using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// HERRAMIENTA DE EDITOR: menús para ABRIR escenas (no crean, no guardan, no mueven ni borran nada).
/// Clasificación (ver LEEME_EQUIPO.md · «Escenas del proyecto»):
///   Juego ............ HangarExpedicion (entrada) y ExpedicionMVP.
///   Arte y pruebas ... ModeloPrueba (importar modelos), VueloPrueba, CirculacionPrueba, ExpedicionPrueba, HangarPrueba.
///   Prototipos antiguos: CentroOperaciones, Configuracion, Nave, Exploracion, Resultados.
/// Las rutas de las escenas NO cambian: muchos menús de creación y CargadorEscenas las usan tal cual.
/// </summary>
public static class MenuEscenas
{
    const string Escenas = "Assets/_Proyecto/Scenes/";

    static void Abrir(string ruta)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Sal de Play", "Detén el modo Play antes de abrir otra escena.", "Aceptar");
            return;
        }
        if (!File.Exists(ruta))
        {
            EditorUtility.DisplayDialog("La escena no existe", "No existe " + ruta + ".\nCréala con su menú de creación (este menú solo abre).", "Aceptar");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        Debug.Log("[Escenas] Abierta: " + ruta + " (no se ha modificado).");
    }

    // ---- Juego
    [MenuItem("Prototipo/Juego/3. Abrir HangarExpedicion (JUGAR: entrada del juego)", false, 10)]
    static void Hangar() => Abrir(CargadorEscenas.Hangar);

    [MenuItem("Prototipo/Juego/4. Abrir ExpedicionMVP (acceso directo al MVP, sin hangar)", false, 11)]
    static void Mvp() => Abrir(CargadorEscenas.Expedicion);

    // ---- Arte y pruebas
    [MenuItem("Prototipo/Arte y pruebas/4. Abrir ModeloPrueba (IMPORTAR MODELOS)", false, 110)]
    static void ModeloPrueba() => Abrir(Escenas + "ModeloPrueba.unity");

    [MenuItem("Prototipo/Arte y pruebas/Abrir escenas de prueba/VueloPrueba (regresión del vuelo)", false, 120)]
    static void VueloPrueba() => Abrir(Escenas + "VueloPrueba.unity");

    [MenuItem("Prototipo/Arte y pruebas/Abrir escenas de prueba/CirculacionPrueba (personaje y puertas)", false, 121)]
    static void CirculacionPrueba() => Abrir(Escenas + "CirculacionPrueba.unity");

    [MenuItem("Prototipo/Arte y pruebas/Abrir escenas de prueba/ExpedicionPrueba (nave modular)", false, 122)]
    static void ExpedicionPrueba() => Abrir(Escenas + "ExpedicionPrueba.unity");

    [MenuItem("Prototipo/Arte y pruebas/Abrir escenas de prueba/HangarPrueba (nave base)", false, 123)]
    static void HangarPrueba() => Abrir(Escenas + "HangarPrueba.unity");

    // ---- Prototipos antiguos (solo abrir; sus menús de creación siguen en su carpeta)
    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/Abrir/CentroOperaciones", false, 1100)]
    static void Centro() => Abrir(Escenas + "CentroOperaciones.unity");

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/Abrir/Configuracion", false, 1101)]
    static void Configuracion() => Abrir(Escenas + "Configuracion.unity");

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/Abrir/Nave", false, 1102)]
    static void Nave() => Abrir(Escenas + "Nave.unity");

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/Abrir/Exploracion", false, 1103)]
    static void Exploracion() => Abrir(Escenas + "Exploracion.unity");

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/Abrir/Resultados", false, 1104)]
    static void Resultados() => Abrir(Escenas + "Resultados.unity");
}
