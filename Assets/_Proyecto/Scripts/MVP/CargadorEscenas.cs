using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    /// <summary>
    /// Cambio de escena entre el hangar terrestre y ExpedicionMVP SIN tocar la lista de escenas del build.
    /// En el editor carga por ruta (no hace falta que la escena esté en la lista). En un ejecutable necesita que la escena
    /// esté en la lista: si no está, devuelve un mensaje claro en lugar de fallar en silencio.
    /// </summary>
    public static class CargadorEscenas
    {
        public const string Hangar = "Assets/_Proyecto/Scenes/HangarExpedicion.unity";
        public const string Expedicion = "Assets/_Proyecto/Scenes/ExpedicionMVP.unity";

        public static bool Cargar(string ruta, out string error)
        {
            error = "";
#if UNITY_EDITOR
            if (!System.IO.File.Exists(ruta))
            {
                error = "No existe la escena " + ruta + ". Créala con su menú de Prototipo.";
                return false;
            }
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            return true;
#else
            if (SceneUtility.GetBuildIndexByScenePath(ruta) < 0)
            {
                error = "La escena " + System.IO.Path.GetFileNameWithoutExtension(ruta) + " no está en la lista del ejecutable.";
                return false;
            }
            SceneManager.LoadScene(ruta);
            return true;
#endif
        }
    }
}
