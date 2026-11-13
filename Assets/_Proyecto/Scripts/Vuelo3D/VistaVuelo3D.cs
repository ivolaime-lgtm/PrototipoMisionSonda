using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Dibujo de la prueba V1 (SOLO VISUAL; lee PruebaVuelo3D y no cambia la lógica). Todo se crea en tiempo de ejecución
    /// con formas provisionales.
    ///
    /// Marco: la nave se dibuja en el origen de Unity con su orientación real; el mundo se coloca en (posición − posición
    /// de la nave) sin girarlo. Así la precisión no se degrada lejos de la salida y la rotación es la de la nave, no una
    /// animación del exterior. Los cuerpos lejanos se acercan a 9 km reduciendo su tamaño en la misma proporción
    /// (mismo tamaño aparente) para no forzar el plano lejano de la cámara.
    /// </summary>
    public class VistaVuelo3D : MonoBehaviour
    {
        const float DistanciaVisualMaxima = 9000f;
        const float RadioEstrellas = 9500f;

        PruebaVuelo3D prueba;
        Camera camara;
        Transform nave, exteriorNave, cabina, llama;
        readonly List<Transform> balizas = new List<Transform>();
        readonly List<Transform> zonasBaliza = new List<Transform>();
        readonly List<Transform> cuerpos = new List<Transform>();
        Transform estrellas;
        readonly List<Renderer> propTraslacion = new List<Renderer>();   // 6: +x −x +y −y +z −z
        readonly List<Renderer> propGiro = new List<Renderer>();         // 12: por eje, 4 (dos por sentido)
        Material matApagado, matEncendido;
        float mirarYaw, mirarPitch;

        public Camera Camara => camara;
        /// <summary>Posición en la escena (marco de dibujo) de un punto del mundo.</summary>
        public Vector3 AEscena(Vector3 posicionMundoM) =>
            (Vector3d.De(posicionMundoM) - prueba.Estado.Posicion).AVector3();

        public void Construir(PruebaVuelo3D p)
        {
            prueba = p;

            // Cámara y luz
            GameObject camGo = new GameObject("CamaraVuelo");
            camGo.tag = "MainCamera";
            camara = camGo.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
            camara.nearClipPlane = 0.3f;
            camara.farClipPlane = 12000f;
            camara.fieldOfView = 65f;
            camGo.AddComponent<AudioListener>();

            GameObject sol = new GameObject("Sol (luz direccional, fija en el mundo)");
            Light luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.4f;
            sol.transform.rotation = Quaternion.Euler(25f, -50f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.07f, 0.09f);

            matApagado = Mat(new Color(0.25f, 0.27f, 0.3f), true);
            matEncendido = Mat(new Color(1f, 0.85f, 0.55f), true);

            CrearEstrellas();
            foreach (CuerpoReferencia c in p.Cuerpos)
            {
                Transform t = Primitiva(PrimitiveType.Sphere, c.nombre, null, Vector3.zero, Vector3.one, Mat(c.color, false));
                cuerpos.Add(t);
            }
            foreach (DefinicionBaliza b in p.Balizas)
            {
                Transform t = Primitiva(PrimitiveType.Sphere, b.nombre, null, Vector3.zero, Vector3.one * 8f, Mat(b.color, true));
                // Mástil vertical para leer mejor la orientación de la baliza
                Primitiva(PrimitiveType.Cube, "Mastil", t, Vector3.zero, new Vector3(0.08f, 6f, 0.08f), Mat(b.color, true));
                balizas.Add(t);
                zonasBaliza.Add(CrearZona(t, p, b.color));
            }

            CrearNave();
        }

        // ------------------------------------------------------------------ Construcción

        static Material Mat(Color c, bool emisivo)
        {
            Shader sh = Shader.Find(emisivo ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find(emisivo ? "Unlit/Color" : "Standard");
            Material m = new Material(sh);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static Transform Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            g.name = nombre;
            Collider col = g.GetComponent<Collider>();
            if (col != null) Destroy(col);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = escala;
            Renderer r = g.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }

        /// <summary>Tres círculos de la zona de llegada (radio = distancia de llegada del objetivo).</summary>
        Transform CrearZona(Transform baliza, PruebaVuelo3D p, Color color)
        {
            Transform zona = new GameObject("ZonaLlegada").transform;
            zona.SetParent(baliza, false);
            float r = p.Objetivo != null ? p.Objetivo.Configuracion.distanciaLlegadaM : 50f;
            Material m = Mat(new Color(color.r, color.g, color.b) * 0.8f, true);
            for (int eje = 0; eje < 3; eje++)
            {
                LineRenderer lr = new GameObject("Circulo" + eje).AddComponent<LineRenderer>();
                lr.transform.SetParent(zona, false);
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = 48;
                lr.widthMultiplier = 0.6f;
                lr.sharedMaterial = m;
                for (int i = 0; i < 48; i++)
                {
                    float a = i * Mathf.PI * 2f / 48f;
                    Vector3 v = eje == 0 ? new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f)
                              : eje == 1 ? new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))
                              : new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a));
                    lr.SetPosition(i, v * r);
                }
            }
            // La baliza está escalada ×8: la zona se compensa para medir r metros.
            zona.localScale = Vector3.one / 8f;
            zona.gameObject.SetActive(false);
            return zona;
        }

        void CrearEstrellas()
        {
            const int cantidad = 1800;
            var vertices = new Vector3[cantidad * 4];
            var indices = new int[cantidad * 12];
            Random.State antes = Random.state;
            Random.InitState(20261111);
            for (int i = 0; i < cantidad; i++)
            {
                Vector3 d = Random.onUnitSphere;
                Vector3 c = d * RadioEstrellas;
                Vector3 ref1 = Mathf.Abs(d.y) < 0.9f ? Vector3.up : Vector3.right;
                Vector3 r = Vector3.Cross(d, ref1).normalized;
                Vector3 u = Vector3.Cross(r, d);
                float s = Random.Range(9f, 22f);
                int v = i * 4;
                vertices[v] = c + (-r - u) * s;
                vertices[v + 1] = c + (r - u) * s;
                vertices[v + 2] = c + (r + u) * s;
                vertices[v + 3] = c + (-r + u) * s;
                int k = i * 12;
                // Dos caras para que se vean sin depender del sentido de los triángulos
                indices[k] = v; indices[k + 1] = v + 1; indices[k + 2] = v + 2;
                indices[k + 3] = v; indices[k + 4] = v + 2; indices[k + 5] = v + 3;
                indices[k + 6] = v; indices[k + 7] = v + 2; indices[k + 8] = v + 1;
                indices[k + 9] = v; indices[k + 10] = v + 3; indices[k + 11] = v + 2;
            }
            Random.state = antes;
            Mesh malla = new Mesh { name = "EstrellasVuelo" };
            malla.vertices = vertices;
            malla.triangles = indices;
            malla.bounds = new Bounds(Vector3.zero, Vector3.one * RadioEstrellas * 2.2f);
            GameObject g = new GameObject("Estrellas (direcciones fijas del mundo)");
            g.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mat(new Color(0.85f, 0.88f, 1f), true);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            estrellas = g.transform;
        }

        /// <summary>Nave provisional: casco por fuera (cámara exterior) y marco de cabina (vista desde el asiento).</summary>
        void CrearNave()
        {
            nave = new GameObject("Nave (origen del dibujo)").transform;

            exteriorNave = new GameObject("Exterior").transform;
            exteriorNave.SetParent(nave, false);
            Material casco = Mat(new Color(0.85f, 0.86f, 0.88f), false);
            Material oscuro = Mat(new Color(0.22f, 0.24f, 0.28f), false);
            Material naranja = Mat(new Color(0.95f, 0.55f, 0.16f), false);
            Primitiva(PrimitiveType.Cube, "Casco", exteriorNave, new Vector3(0f, 0f, -2f), new Vector3(8.4f, 3.6f, 19f), casco);
            Primitiva(PrimitiveType.Cube, "Proa", exteriorNave, new Vector3(0f, -0.2f, 8.2f), new Vector3(6.4f, 2.8f, 1.4f), casco);
            Primitiva(PrimitiveType.Cube, "Ventana", exteriorNave, new Vector3(0f, 0.5f, 8.95f), new Vector3(4.4f, 0.8f, 0.1f), Mat(new Color(0.36f, 0.78f, 0.88f), true));
            Primitiva(PrimitiveType.Cube, "FranjaNaranja", exteriorNave, new Vector3(0f, 1.85f, -2f), new Vector3(1.2f, 0.1f, 19f), naranja);
            Transform tobera = Primitiva(PrimitiveType.Cylinder, "Tobera", exteriorNave, new Vector3(0f, 0f, -12.6f), new Vector3(2.6f, 1.2f, 2.6f), oscuro);
            tobera.localRotation = Quaternion.Euler(90f, 0f, 0f);
            llama = Primitiva(PrimitiveType.Sphere, "Llama", exteriorNave, new Vector3(0f, 0f, -16f), new Vector3(1.8f, 1.8f, 6f), Mat(new Color(1f, 0.62f, 0.2f), true));

            // Propulsores de traslación: se encienden en la cara OPUESTA al sentido del empuje.
            Vector3[] caras = { new Vector3(-4.35f, 0f, -2f), new Vector3(4.35f, 0f, -2f), new Vector3(0f, -1.95f, -2f),
                                new Vector3(0f, 1.95f, -2f), new Vector3(0f, 0f, -11.6f), new Vector3(0f, 0.9f, 8.95f) };
            foreach (Vector3 c in caras)
                propTraslacion.Add(Primitiva(PrimitiveType.Cube, "RCS_T", exteriorNave, c, Vector3.one * 0.7f, matApagado).GetComponent<Renderer>());

            // Propulsores de orientación: pares en proa y popa (cabeceo y guiñada) y en los costados (alabeo).
            float zp = 6.5f, zt = -10.5f;
            Vector3[] giro =
            {
                new Vector3(0f, 1.95f, zp), new Vector3(0f, -1.95f, zt),   // cabeceo + (morro abajo)
                new Vector3(0f, -1.95f, zp), new Vector3(0f, 1.95f, zt),   // cabeceo −
                new Vector3(-4.35f, 0f, zp), new Vector3(4.35f, 0f, zt),   // guiñada + (a la derecha)
                new Vector3(4.35f, 0f, zp), new Vector3(-4.35f, 0f, zt),   // guiñada −
                new Vector3(4.35f, -1.95f, -2f), new Vector3(-4.35f, 1.95f, -2f),  // alabeo + (a la izquierda: sube el costado derecho)
                new Vector3(-4.35f, -1.95f, -2f), new Vector3(4.35f, 1.95f, -2f)   // alabeo −
            };
            foreach (Vector3 g in giro)
                propGiro.Add(Primitiva(PrimitiveType.Cube, "RCS_G", exteriorNave, g, Vector3.one * 0.5f, matApagado).GetComponent<Renderer>());

            // Cabina provisional: marco de ventana y consola delante de la vista del asiento.
            cabina = new GameObject("CabinaProvisional").transform;
            cabina.SetParent(nave, false);
            cabina.localPosition = new Vector3(0f, 0.9f, 5.2f);
            Material marco = Mat(new Color(0.3f, 0.32f, 0.36f), false);
            Primitiva(PrimitiveType.Cube, "MarcoArriba", cabina, new Vector3(0f, 0.95f, 1.6f), new Vector3(3.6f, 0.14f, 0.14f), marco);
            Primitiva(PrimitiveType.Cube, "MarcoAbajo", cabina, new Vector3(0f, -0.65f, 1.6f), new Vector3(3.6f, 0.14f, 0.14f), marco);
            Primitiva(PrimitiveType.Cube, "MarcoIzq", cabina, new Vector3(-1.8f, 0.15f, 1.6f), new Vector3(0.14f, 1.74f, 0.14f), marco);
            Primitiva(PrimitiveType.Cube, "MarcoDer", cabina, new Vector3(1.8f, 0.15f, 1.6f), new Vector3(0.14f, 1.74f, 0.14f), marco);
            Primitiva(PrimitiveType.Cube, "MontanteCentral", cabina, new Vector3(0f, 0.15f, 1.6f), new Vector3(0.06f, 1.74f, 0.06f), marco);
            Primitiva(PrimitiveType.Cube, "Consola", cabina, new Vector3(0f, -0.95f, 1.0f), new Vector3(3.0f, 0.5f, 0.9f), oscuro);
            Primitiva(PrimitiveType.Cube, "BordeNaranja", cabina, new Vector3(0f, -0.69f, 0.56f), new Vector3(3.0f, 0.04f, 0.04f), naranja);
        }

        // ------------------------------------------------------------------ Cada fotograma

        void LateUpdate()
        {
            if (prueba == null || prueba.Estado == null) return;
            EstadoVuelo3D e = prueba.Estado;

            nave.position = Vector3.zero;
            nave.rotation = e.Orientacion;
            if (estrellas != null) estrellas.position = Vector3.zero;   // a distancia «infinita»: no se acercan, solo giran con la vista

            for (int i = 0; i < cuerpos.Count && i < prueba.Cuerpos.Count; i++)
            {
                CuerpoReferencia c = prueba.Cuerpos[i];
                Vector3 rel = AEscena(c.posicionM);
                float d = rel.magnitude;
                float k = d > DistanciaVisualMaxima ? DistanciaVisualMaxima / d : 1f;
                cuerpos[i].position = rel * k;
                cuerpos[i].localScale = Vector3.one * (2f * c.radioM * k);
            }

            int objetivo = prueba.Objetivo.IndiceActual;
            for (int i = 0; i < balizas.Count && i < prueba.Balizas.Count; i++)
            {
                balizas[i].position = AEscena(prueba.Balizas[i].posicionM);
                bool esObjetivo = i == objetivo;
                if (zonasBaliza[i].gameObject.activeSelf != esObjetivo) zonasBaliza[i].gameObject.SetActive(esObjetivo);
                float pulso = esObjetivo ? 1f + 0.25f * Mathf.Sin(Time.time * 4f) : (prueba.Objetivo.Visitada(i) ? 0.6f : 1f);
                balizas[i].localScale = Vector3.one * 8f * pulso;
                zonasBaliza[i].localScale = Vector3.one / (8f * pulso);   // la zona mide siempre la distancia de llegada
            }

            // Propulsores (solo exterior)
            Vector3 t = e.TraslacionActiva;
            Encender(propTraslacion, 0, t.x > 0.05f); Encender(propTraslacion, 1, t.x < -0.05f);
            Encender(propTraslacion, 2, t.y > 0.05f); Encender(propTraslacion, 3, t.y < -0.05f);
            Encender(propTraslacion, 4, t.z > 0.05f); Encender(propTraslacion, 5, t.z < -0.05f);
            Vector3 g = e.GiroActivo;
            for (int eje = 0; eje < 3; eje++)
            {
                bool mas = g[eje] > 0.05f, menos = g[eje] < -0.05f;
                Encender(propGiro, eje * 4, mas); Encender(propGiro, eje * 4 + 1, mas);
                Encender(propGiro, eje * 4 + 2, menos); Encender(propGiro, eje * 4 + 3, menos);
            }
            if (llama != null)
            {
                bool on = e.MotorEncendido;
                if (llama.gameObject.activeSelf != on) llama.gameObject.SetActive(on);
                if (on) llama.localScale = new Vector3(1.8f, 1.8f, 6f + Random.Range(-0.8f, 0.8f));
            }

            ColocarCamara(e);
        }

        void Encender(List<Renderer> lista, int i, bool on)
        {
            if (i < lista.Count) lista[i].sharedMaterial = on ? matEncendido : matApagado;
        }

        void ColocarCamara(EstadoVuelo3D e)
        {
            // Botón derecho del ratón: mirar alrededor (asiento) u orbitar (exterior). Al soltar, vuelve al frente.
            Mouse raton = Mouse.current;
            if (raton != null && raton.rightButton.isPressed)
            {
                Vector2 d = raton.delta.ReadValue();
                mirarYaw = Mathf.Clamp(mirarYaw + d.x * 0.15f, -150f, 150f);
                mirarPitch = Mathf.Clamp(mirarPitch - d.y * 0.15f, -80f, 80f);
            }
            else
            {
                mirarYaw = Mathf.MoveTowards(mirarYaw, 0f, 120f * Time.deltaTime);
                mirarPitch = Mathf.MoveTowards(mirarPitch, 0f, 120f * Time.deltaTime);
            }
            Quaternion mirar = Quaternion.Euler(mirarPitch, mirarYaw, 0f);

            bool exterior = prueba.VistaExterior;
            if (exteriorNave.gameObject.activeSelf != exterior) exteriorNave.gameObject.SetActive(exterior);
            if (cabina.gameObject.activeSelf == exterior) cabina.gameObject.SetActive(!exterior);

            Transform c = camara.transform;
            if (exterior)
            {
                Vector3 desplazamiento = mirar * new Vector3(0f, 8f, -42f);
                Vector3 foco = new Vector3(0f, 0f, 4f);
                c.position = nave.TransformPoint(desplazamiento);
                c.rotation = Quaternion.LookRotation(nave.TransformPoint(foco) - c.position, nave.rotation * (mirar * Vector3.up));
            }
            else
            {
                Vector3 ojo = new Vector3(0f, 0.9f, 5.2f);
                c.position = nave.TransformPoint(ojo);
                c.rotation = nave.rotation * mirar;
                if (e.MotorEncendido) c.position += Random.insideUnitSphere * 0.015f;   // leve vibración del motor
            }
        }
    }
}
