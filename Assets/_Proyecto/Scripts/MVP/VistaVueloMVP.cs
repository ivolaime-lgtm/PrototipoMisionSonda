using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Vista de PILOTAJE del MVP (SOLO DIBUJO; lee EstadoExpedicion y no cambia la lógica).
    ///
    /// Marco de dibujo: la EM-4X real (la misma geometría del interior, construida en el origen) representa la nave en sus
    /// propios ejes; el resto del sistema se dibuja girado por la inversa de la orientación de la nave y desplazado por su
    /// posición. Así se ve el exterior real de la EM-4X sin duplicarla y sin mover la física del interior. Los cuerpos
    /// lejanos se acercan a 9 km reduciendo su tamaño en la misma proporción (mismo tamaño aparente), como en VueloPrueba.
    /// </summary>
    public class VistaVueloMVP : MonoBehaviour
    {
        const float DistanciaVisualMaxima = 9000f;
        const float RadioEstrellas = 9500f;

        JuegoMVP juego;
        Camera camara;
        Light sol;
        Transform raiz, estrellas, llama;
        readonly List<(CuerpoKora c, Transform t)> cuerpos = new List<(CuerpoKora c, Transform t)>();
        readonly List<Vector3> dirEstrellas = new List<Vector3>();
        LineRenderer trayectoria;
        Transform marcaObjetivo;
        Vector3 asientoLocal, asientoMirada;
        static readonly Quaternion SolMundo = Quaternion.Euler(25f, -50f, 0f);
        float mirarYaw, mirarPitch = 12f;

        public Camera Camara => camara;
        public Vector3 PosicionMarca => marcaObjetivo != null ? marcaObjetivo.position : Vector3.zero;

        static Material Mat(Color c, bool sinLuz)
        {
            Shader sh = Shader.Find(sinLuz ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find(sinLuz ? "Unlit/Color" : "Standard");
            Material m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static Transform Esfera(string nombre, Transform padre, Material mat)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = nombre;
            Collider col = g.GetComponent<Collider>();
            col.enabled = false;
            Destroy(col);
            g.transform.SetParent(padre, false);
            Renderer r = g.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }

        public void Construir(JuegoMVP j, DefinicionNaveModular d)
        {
            juego = j;
            raiz = new GameObject("Vista de vuelo (sistema Kora, solo dibujo)").transform;

            GameObject camGo = new GameObject("CamaraPilotaje");
            camGo.tag = "MainCamera";
            camara = camGo.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
            camara.nearClipPlane = 0.3f;
            camara.farClipPlane = 12000f;
            camara.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();

            sol = new GameObject("Sol (vuelo)").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 1.3f;
            sol.shadows = LightShadows.None;

            // Estrellas
            estrellas = new GameObject("Estrellas").transform;
            estrellas.SetParent(raiz, false);
            Material mEst = Mat(new Color(0.85f, 0.88f, 1f), true);
            System.Random azar = new System.Random(11);
            for (int i = 0; i < 260; i++)
            {
                Vector3 dir = new Vector3((float)azar.NextDouble() * 2f - 1f, (float)azar.NextDouble() * 2f - 1f, (float)azar.NextDouble() * 2f - 1f);
                if (dir.sqrMagnitude < 0.01f || dir.sqrMagnitude > 1f) { i--; continue; }
                dir.Normalize();
                dirEstrellas.Add(dir);
                Transform e = Esfera("Estrella", estrellas, mEst);
                e.localPosition = dir * RadioEstrellas;
                e.localScale = Vector3.one * (8f + (float)azar.NextDouble() * 14f);
            }

            foreach (CuerpoKora c in j.Estado != null ? j.Estado.Sistema.cuerpos : SistemaKora.Crear().cuerpos)
            {
                if (c.tipo == TipoCuerpo.Cumulo) continue;
                Transform t;
                if (c.tipo == TipoCuerpo.Base || c.tipo == TipoCuerpo.Satelite) t = Estructura(c);
                else t = Esfera(c.nombre, raiz, Mat(c.color, false));
                cuerpos.Add((c, t));
            }

            // Trayectoria prevista (sin motor) y marca del objetivo
            trayectoria = new GameObject("Trayectoria prevista").AddComponent<LineRenderer>();
            trayectoria.transform.SetParent(raiz, false);
            trayectoria.useWorldSpace = false;
            trayectoria.widthMultiplier = 6f;
            trayectoria.sharedMaterial = Mat(new Color(0.4f, 0.85f, 1f), true);
            marcaObjetivo = Esfera("Marca del objetivo", raiz, Mat(new Color(1f, 0.85f, 0.2f), true));

            // Llama del motor principal, detrás de la etapa de propulsión de la EM-4X
            ModuloNave pr = d.Modulo("PR");
            Vector3 popa = pr != null ? pr.AMundo(new Vector3(0f, 0f, pr.largo)) : new Vector3(0f, 0f, -30f);
            llama = Esfera("Llama del motor", raiz, Mat(new Color(1f, 0.75f, 0.35f), true));
            llama.SetParent(null, false);
            llama.position = popa + Vector3.back * 6f;
            llama.localScale = new Vector3(5f, 5f, 14f);

            PuestoExpedicion p = d.puestos.Count > 0 ? d.puestos[0] : null;
            ModuloNave ma = p != null ? d.Modulo(p.modulo) : null;
            asientoLocal = ma != null ? ma.AMundo(p.posicion + new Vector3(0f, 0.7f, 0f)) : new Vector3(0f, 0f, 10f);
            asientoMirada = ma != null ? ma.DireccionAMundo(p.mirada).normalized : Vector3.forward;
            Activar(false);
        }

        /// <summary>Estación o satélite: bloques provisionales con tamaño real (no es arte final).</summary>
        Transform Estructura(CuerpoKora c)
        {
            Transform t = new GameObject(c.nombre).transform;
            t.SetParent(raiz, false);
            Material casco = Mat(new Color(0.75f, 0.78f, 0.82f), false), panel = Mat(new Color(0.15f, 0.25f, 0.55f), false), luz = Mat(c.color, true);
            void Cubo(Vector3 pos, Vector3 esc, Material m)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Collider col = g.GetComponent<Collider>(); col.enabled = false; Destroy(col);
                g.transform.SetParent(t, false);
                g.transform.localPosition = pos;
                g.transform.localScale = esc;
                g.GetComponent<Renderer>().sharedMaterial = m;
            }
            if (c.tipo == TipoCuerpo.Base)
            {
                Cubo(Vector3.zero, new Vector3(12f, 12f, 60f), casco);
                Cubo(new Vector3(0f, 0f, 20f), new Vector3(40f, 8f, 8f), casco);
                Cubo(new Vector3(-45f, 0f, 0f), new Vector3(50f, 1f, 18f), panel);
                Cubo(new Vector3(45f, 0f, 0f), new Vector3(50f, 1f, 18f), panel);
                Cubo(new Vector3(0f, 9f, -25f), new Vector3(3f, 3f, 3f), luz);
            }
            else
            {
                Cubo(Vector3.zero, new Vector3(4f, 4f, 6f), casco);
                Cubo(new Vector3(-9f, 0f, 0f), new Vector3(12f, 0.3f, 4f), panel);
                Cubo(new Vector3(9f, 0f, 0f), new Vector3(12f, 0.3f, 4f), panel);
                Cubo(new Vector3(0f, 3f, 0f), new Vector3(1.5f, 1.5f, 1.5f), luz);
            }
            return t;
        }

        public void Activar(bool activa)
        {
            if (camara == null) return;
            camara.enabled = activa;
            AudioListener al = camara.GetComponent<AudioListener>();
            if (al != null) al.enabled = activa;
            sol.enabled = activa;
            raiz.gameObject.SetActive(activa);
            if (!activa) llama.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (juego == null || camara == null || !camara.enabled || juego.Estado == null) return;
            EstadoExpedicion s = juego.Estado;
            Quaternion inv = Quaternion.Inverse(s.Vuelo.Orientacion);
            sol.transform.rotation = inv * SolMundo;
            estrellas.localRotation = inv;

            foreach (var x in cuerpos)
            {
                Vector3d pos = s.Sistema.Posicion(x.c, s.Tiempo);
                Vector3 rel = inv * (pos - s.Vuelo.Posicion).AVector3();
                float d = rel.magnitude;
                float escala = 1f;
                if (d > DistanciaVisualMaxima) { escala = DistanciaVisualMaxima / d; rel *= escala; }
                x.t.localPosition = rel;
                if (x.c.tipo == TipoCuerpo.Base || x.c.tipo == TipoCuerpo.Satelite)
                {
                    x.t.localScale = Vector3.one * escala;
                    x.t.localRotation = inv;
                }
                else x.t.localScale = Vector3.one * (x.c.radioM * 2f * escala);
            }

            // Marca del objetivo (tamaño constante en pantalla aproximado)
            Vector3d po = s.Sistema.Posicion(s.Objetivo, s.Tiempo);
            Vector3 ro = inv * (po - s.Vuelo.Posicion).AVector3();
            float dob = ro.magnitude;
            if (dob > DistanciaVisualMaxima) ro *= DistanciaVisualMaxima / dob;
            marcaObjetivo.localPosition = ro;
            marcaObjetivo.localScale = Vector3.one * Mathf.Max(2f, ro.magnitude * 0.012f);

            // Trayectoria prevista (comprimida igual que los cuerpos)
            PrediccionTrayectoria pr = juego.Prediccion;
            if (pr != null && pr.puntos.Count > 1)
            {
                trayectoria.positionCount = pr.puntos.Count;
                for (int i = 0; i < pr.puntos.Count; i++)
                {
                    Vector3 r = inv * (pr.puntos[i] - s.Vuelo.Posicion).AVector3();
                    float m = r.magnitude;
                    if (m > DistanciaVisualMaxima) r *= DistanciaVisualMaxima / m;
                    trayectoria.SetPosition(i, r);
                }
                Color c = pr.impacto ? new Color(1f, 0.35f, 0.3f) : new Color(0.4f, 0.85f, 1f);
                trayectoria.startColor = trayectoria.endColor = c;
                trayectoria.sharedMaterial.color = c;
                if (trayectoria.sharedMaterial.HasProperty("_BaseColor")) trayectoria.sharedMaterial.SetColor("_BaseColor", c);
            }
            else trayectoria.positionCount = 0;

            llama.gameObject.SetActive(s.Vuelo.MotorEncendido);

            // Cámara: exterior (orbita la EM-4X con el ratón derecho) o desde el asiento de Pilotaje.
            UnityEngine.InputSystem.Mouse raton = UnityEngine.InputSystem.Mouse.current;
            if (raton != null && raton.rightButton.isPressed)
            {
                Vector2 dl = raton.delta.ReadValue() * 0.15f;
                mirarYaw += dl.x;
                mirarPitch = Mathf.Clamp(mirarPitch - dl.y, -80f, 80f);
            }
            if (juego.VistaExterior || juego.FaseActual != JuegoMVP.Fase.Mision)
            {
                Quaternion q = Quaternion.Euler(mirarPitch, mirarYaw, 0f);
                Vector3 foco = new Vector3(0f, 0f, -6f);
                camara.transform.position = foco + q * new Vector3(0f, 0f, -110f);
                camara.transform.rotation = Quaternion.LookRotation(foco - camara.transform.position, q * Vector3.up);
                camara.nearClipPlane = 0.3f;
            }
            else
            {
                camara.transform.position = asientoLocal;
                camara.transform.rotation = Quaternion.LookRotation(asientoMirada, Vector3.up) * Quaternion.Euler(mirarPitch * 0.3f - 3.6f, mirarYaw * 0.3f, 0f);
                camara.nearClipPlane = 0.05f;
            }
        }
    }
}
