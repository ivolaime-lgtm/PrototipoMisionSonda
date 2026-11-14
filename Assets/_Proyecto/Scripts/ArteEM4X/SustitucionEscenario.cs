using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Marca una pieza PROVISIONAL de escenario que un modelo puede sustituir (edificio, L-4, VC-1, servicio orbital…).
    /// El modelo se coloca como hijo del punto, así sigue sus animaciones. La pieza provisional deja de dibujarse pero
    /// conserva sus colisiones, sus puntos de carga y su lógica. Pivote del modelo = el del punto.
    /// </summary>
    public class PuntoSustitucion : MonoBehaviour
    {
        public string id;
        public bool aplicado;
    }

    /// <summary>
    /// SUSTITUCIÓN DE ESCENARIO: el mismo enfoque que SustitucionVisualEM4X, para lo que no es la EM-4X. Usa la misma
    /// tabla (RecursosVisualesEM4X), con entradas de tipo «Escenario» cuyo «nombre» es el id del punto. Sin modelo, o con
    /// la entrada desactivada, se queda la geometría provisional. No cambia medidas, masas, colisiones ni balance.
    /// </summary>
    public static class SustitucionEscenario
    {
        /// <summary>Tabla activa (la asigna el hangar al empezar; null = todo provisional).</summary>
        public static RecursosVisualesEM4X Tabla { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { Tabla = null; }

        /// <summary>Marca «t» como punto de sustitución con ese id (y lo devuelve).</summary>
        public static Transform Punto(Transform t, string id)
        {
            if (t == null) return null;
            PuntoSustitucion p = t.GetComponent<PuntoSustitucion>();
            if (p == null) p = t.gameObject.AddComponent<PuntoSustitucion>();
            p.id = id;
            return t;
        }

        /// <summary>Aplica los modelos de la tabla a todos los puntos bajo «raiz». Devuelve cuántos se aplicaron.</summary>
        public static int Aplicar(Transform raiz)
        {
            RecursosVisualesEM4X tabla = Tabla;
            if (raiz == null || tabla == null || !tabla.usarModelos) return 0;
            Dictionary<string, GameObject> modelos = new Dictionary<string, GameObject>();
            foreach (EntradaVisual e in tabla.entradas)
                if (e.activa && e.tipo == TipoSustitucion.Escenario && e.prefab != null && !string.IsNullOrEmpty(e.nombre))
                    modelos[e.nombre] = e.prefab;
            if (modelos.Count == 0) return 0;
            int n = 0;
            foreach (PuntoSustitucion p in raiz.GetComponentsInChildren<PuntoSustitucion>(true))
            {
                GameObject prefab;
                if (p.aplicado || string.IsNullOrEmpty(p.id) || !modelos.TryGetValue(p.id, out prefab)) continue;
                List<Renderer> provisionales = new List<Renderer>(p.GetComponentsInChildren<Renderer>(true));
                GameObject g = Object.Instantiate(prefab, p.transform, false);
                g.name = "Modelo · escenario/" + p.id;
                g.transform.localPosition = Vector3.zero;
                g.transform.localRotation = Quaternion.identity;
                // Compensa la escala del punto: el modelo va a escala real (1 u = 1 m)
                Vector3 l = p.transform.lossyScale;
                g.transform.localScale = new Vector3(Inv(l.x), Inv(l.y), Inv(l.z));
                foreach (Collider c in g.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Object.Destroy(c); }
                foreach (Joint j in g.GetComponentsInChildren<Joint>(true)) Object.DestroyImmediate(j);
                foreach (Rigidbody rb in g.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
                foreach (Camera c in g.GetComponentsInChildren<Camera>(true)) { c.enabled = false; Object.Destroy(c); }
                foreach (AudioListener a in g.GetComponentsInChildren<AudioListener>(true)) { a.enabled = false; Object.Destroy(a); }
                foreach (Light lu in g.GetComponentsInChildren<Light>(true)) { lu.enabled = false; Object.Destroy(lu); }
                foreach (Renderer r in provisionales) r.forceRenderingOff = true;     // colisiones y lógica se quedan
                p.aplicado = true;
                n++;
            }
            return n;
        }

        static float Inv(float v) => Mathf.Abs(v) < 1e-5f ? 1f : 1f / v;
    }
}
