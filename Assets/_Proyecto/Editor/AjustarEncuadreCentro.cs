using System.IO;
using PrototipoMision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HERRAMIENTA DE EDITOR (no forma parte del juego).
///
/// Menú "Prototipo → 7. Ajustar encuadre del centro de operaciones":
/// mueve la estación, la luna, la órbita del satélite y la dirección del Sol
/// a una disposición calculada para que:
/// - la estación quede a la derecha del panel de interfaz,
/// - la luna quede arriba a la derecha,
/// - el satélite pase casi siempre contra el espacio (no detrás de la estación).
///
/// No borra objetos ni sustituye la escena. NO guarda: revisa el resultado y
/// pulsa Ctrl+S para guardar, o Ctrl+Z para deshacer.
/// </summary>
public static class AjustarEncuadreCentro
{
    const string RutaEscena = "Assets/_Proyecto/Scenes/CentroOperaciones.unity";

    // Valores calculados con la cámara en (-7, 3.5, -26) mirando a (5, 0, 20), campo de visión de 60°.
    static readonly Vector3 PosEstacion = new Vector3(12.6f, -10.1f, 26.1f);
    static readonly Vector3 PosLuna = new Vector3(139f, 46.6f, 154.6f);
    static readonly Vector3 PosCentroOrbita = new Vector3(4.3f, 15.1f, 78.3f);
    static readonly Vector3 PosInicioSatelite = new Vector3(4.3f, 15.1f, 60.2f);
    static readonly Vector3 EjeOrbita = new Vector3(0.1f, 1f, 0.3f);
    static readonly Vector3 RotacionSol = new Vector3(26f, -34f, 0f);

    [MenuItem("Prototipo/Prototipos antiguos (no usar sin copia)/7. Ajustar encuadre del centro de operaciones", false, 1006)]
    public static void Ajustar()
    {
        if (!File.Exists(RutaEscena))
        {
            EditorUtility.DisplayDialog("Falta la escena", "No existe " + RutaEscena + ".\nCréala antes con el menú 6.", "Aceptar");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Ajustar encuadre",
                "Se abrirá CentroOperaciones y se MOVERÁN: EstacionOrbital, la luna, CentroOrbitaRelevo, " +
                "el satélite y la rotación del Sol.\n\n" +
                "No se borra nada y la escena NO se guarda sola: revisa el resultado y pulsa Ctrl+S para " +
                "guardarlo, o Ctrl+Z para deshacer.",
                "Continuar", "Cancelar"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        int movidos = 0;

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            string n = raiz.name;
            if (n == "EstacionOrbital") movidos += Mover(raiz.transform, PosEstacion);
            else if (n.StartsWith("LunaFicticia")) movidos += Mover(raiz.transform, PosLuna);
            else if (n == "CentroOrbitaRelevo") movidos += Mover(raiz.transform, PosCentroOrbita);
            else if (n.StartsWith("SateliteRelevo"))
            {
                movidos += Mover(raiz.transform, PosInicioSatelite);
                MovimientoVisual mv = raiz.GetComponent<MovimientoVisual>();
                if (mv != null)
                {
                    SerializedObject so = new SerializedObject(mv);
                    so.FindProperty("eje").vector3Value = EjeOrbita;
                    so.ApplyModifiedProperties(); // registra Deshacer
                }
            }
            else if (n == "Sol")
            {
                Undo.RecordObject(raiz.transform, "Ajustar encuadre");
                raiz.transform.rotation = Quaternion.Euler(RotacionSol);
                movidos++;
            }
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (movidos < 5)
            Debug.LogWarning($"[Prototipo] Ajuste de encuadre: solo se encontraron {movidos} de 5 objetos. " +
                             "¿Se renombró alguno? Los que falten no se han movido.");
        else
            Debug.Log("[Prototipo] Encuadre ajustado (5 objetos). Revisa la vista Game y pulsa Ctrl+S para guardar.");
    }

    static int Mover(Transform t, Vector3 posicion)
    {
        Undo.RecordObject(t, "Ajustar encuadre");
        t.position = posicion;
        return 1;
    }
}
