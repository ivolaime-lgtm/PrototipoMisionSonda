using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Lo que se ve por la ventana de la cabina: plataforma de lanzamiento, cielo, estrellas en
    /// movimiento, luna de destino, escombros y la sonda al desplegarse.
    /// La nave no se mueve: el exterior se desplaza a su alrededor. ControlNave le dice qué mostrar.
    /// Representación visual simplificada (DEMOSTRACIÓN), no a escala.
    /// </summary>
    public class ExteriorNave : MonoBehaviour
    {
        [SerializeField] Camera camara;
        [SerializeField] Transform plataforma;
        [SerializeField] Transform luna;
        [SerializeField] MeshFilter estrellas;
        [SerializeField] Material materialEscombros;
        [SerializeField] Material materialSonda;
        [SerializeField] Color cieloSuperficie = new Color(0.45f, 0.62f, 0.85f);
        [SerializeField] Color cieloEspacio = new Color(0.008f, 0.01f, 0.018f);
        [SerializeField] int cantidadEstrellas = 500;
        [Tooltip("Plano de la proa (z, en metros): cada escombro se evalúa UNA vez al cruzarlo y después se elimina, " +
                 "impacte o no. Así no atraviesa por dentro las salas de la nave ampliada.")]
        [SerializeField] float zProa = 5.3f;
        [Tooltip("Centro de la nave (a la altura de la ventana).")]
        [SerializeField] Vector3 centroNave = new Vector3(0f, 1.6f, 0f);

        /// <summary>Desplazamiento lateral de la nave respecto a la ruta (x, y), en metros.</summary>
        public Vector2 Desplazamiento { get; set; }

        /// <summary>Centro de la nave (a la altura de la ventana). Lo usan las vistas que siguen el mismo criterio que los escombros.</summary>
        public Vector3 CentroNave => centroNave;

        /// <summary>Velocidad visual de las estrellas, en m/s.</summary>
        public float VelocidadEstrellas { get; set; }

        /// <summary>Se llama cuando un escombro pasa por la nave: true = impacto, false = esquivado.</summary>
        public System.Action<bool> AlPasarEscombro;

        public int EscombrosActivos => rocas.Count;

        class Roca
        {
            public Transform t;
            public Vector2 xy;
            public float z, radio;
            public Vector3 giro;
        }

        readonly List<Roca> rocas = new List<Roca>();
        float radioNave = 4f;
        float velocidadEscombros = 45f;

        Mesh malla;
        Vector3[] centros;
        float[] tamanos;
        Vector3[] vertices;

        Vector3 plataformaInicial;
        bool enOrbita;
        Vector3 lunaOrbita;
        Transform sonda;
        float tiempoSonda;

        const float ZMin = -100f, ZMax = 900f;
        float zMin = ZMin, zMax = ZMax;            // con navegación: campo simétrico para poder girar 180°

        // Navegación longitudinal (solo se usa si ControlNave la activa)
        bool navegacion;
        float anguloNave;                          // grados: 0 = proa en el sentido de la ruta
        Quaternion giro = Quaternion.identity;
        /// <summary>Lateral fijo de la luna en navegación: la ruta pasa a su lado (|(40, −110)| ≈ 117 m &gt; radio visual de 75 m).</summary>
        static readonly Vector2 LateralLunaNavegacion = new Vector2(40f, -110f);

        /// <summary>Posición del destino en la escena (para el indicador del HUD).</summary>
        public Vector3 PosicionDestino => luna != null ? luna.position : centroNave + Vector3.forward * 1000f;

        void Awake()
        {
            if (plataforma != null) plataformaInicial = plataforma.position;
            CrearEstrellas();
        }

        public void Configurar(float radioNaveM, float velocidadEscombrosMS)
        {
            radioNave = radioNaveM;
            velocidadEscombros = velocidadEscombrosMS;
        }

        // ------------------------------------------------------------------ Destino elegido (solo dibujo)

        /// <summary>
        /// Representación provisional del destino: tinte y tamaño del cuerpo de la ventana. Con blanco y 1 queda igual que siempre.
        /// Usa una copia del material en tiempo de ejecución: no modifica el material del proyecto.
        /// </summary>
        public void ConfigurarDestinoVisual(Color tinte, float escala)
        {
            if (luna == null) return;
            escala = Mathf.Clamp(escala, 0.5f, 1.4f);
            if (!Mathf.Approximately(escala, 1f)) luna.localScale *= escala;
            if (tinte == Color.white) return;
            Renderer r = luna.GetComponentInChildren<Renderer>();
            if (r == null) return;
            Material m = r.material;   // copia de ejecución
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * tinte);
            else m.color = m.color * tinte;
        }

        // ------------------------------------------------------------------ Navegación longitudinal (opcional)

        /// <summary>
        /// Prepara el dibujo para la navegación: estrellas por delante Y por detrás de la nave (el doble de estrellas, con la
        /// misma densidad delante), para que tras girar 180° la ventana no quede vacía. Solo visual.
        /// </summary>
        public void ActivarNavegacion()
        {
            if (navegacion) return;
            navegacion = true;
            zMin = -ZMax;
            zMax = ZMax;
            if (estrellas == null) return;
            if (malla != null) Destroy(malla);
            cantidadEstrellas *= 2;
            CrearEstrellas();
        }

        /// <summary>
        /// Navegación: d = distancia por la ruta al centro de la zona (m, negativa si se ha pasado), zona = media longitud de la zona,
        /// L = longitud de la ruta, angulo = orientación de la nave (grados). La luna se dibuja al lado de la ruta: a 1:1 dentro de la
        /// zona y comprimida más lejos (de 1800 m visuales en la salida a 450 m en el borde de la zona). Pasarse de largo la deja
        /// al lado y detrás, nunca en el camino. Todo el exterior (estrellas y luna) gira alrededor de la nave con el ángulo.
        /// </summary>
        public void Navegacion(float d, float zona, float L, float angulo)
        {
            Cielo(cieloEspacio);
            anguloNave = angulo;
            giro = Quaternion.Euler(0f, angulo, 0f);
            AplicarGiroEstrellas();
            if (luna == null || enOrbita) return;
            if (!luna.gameObject.activeSelf) luna.gameObject.SetActive(true);
            const float zCerca = 200f, zLejos = 1800f;
            float z = d <= zona
                ? zCerca + d
                : zCerca + zona + (zLejos - zCerca - zona) * Mathf.Clamp01((d - zona) / Mathf.Max(1f, L - zona));
            Vector3 rel = new Vector3(LateralLunaNavegacion.x, LateralLunaNavegacion.y, z);
            luna.position = centroNave + Quaternion.Inverse(giro) * rel;
        }

        void AplicarGiroEstrellas()
        {
            if (estrellas == null) return;
            Quaternion q = Quaternion.Inverse(giro);
            estrellas.transform.rotation = q;
            estrellas.transform.position = centroNave - q * centroNave;   // gira alrededor del centro de la nave
        }

        // ------------------------------------------------------------------ Fases

        public void Preparar()
        {
            Cielo(cieloSuperficie);
            if (estrellas != null) estrellas.GetComponent<MeshRenderer>().enabled = false;
            if (luna != null) luna.gameObject.SetActive(false);
            if (plataforma != null) plataforma.gameObject.SetActive(true);
            VelocidadEstrellas = 0f;
        }

        /// <summary>t de 0 a 1: la plataforma cae y el cielo se oscurece.</summary>
        public void Ascenso(float t)
        {
            t = Mathf.Clamp01(t);
            if (plataforma != null)
            {
                plataforma.position = plataformaInicial + Vector3.down * (600f * t * t) + Vector3.back * (60f * t * t);
                if (t >= 1f) plataforma.gameObject.SetActive(false);
            }
            Cielo(Color.Lerp(cieloSuperficie, cieloEspacio, Mathf.SmoothStep(0f, 1f, t)));
            if (estrellas != null) estrellas.GetComponent<MeshRenderer>().enabled = t > 0.45f;
        }

        /// <summary>p de 0 a 1: progreso del viaje. La luna aparece y crece en la segunda mitad.</summary>
        public void Viaje(float p)
        {
            Cielo(cieloEspacio);
            if (luna == null) return;
            bool visible = p > 0.45f;
            if (luna.gameObject.activeSelf != visible) luna.gameObject.SetActive(visible);
            float u = Mathf.Clamp01((p - 0.45f) / 0.55f);
            luna.position = centroNave + new Vector3(30f, 25f, Mathf.Lerp(1800f, 320f, u * u));
        }

        public void Orbita()
        {
            enOrbita = true;
            if (luna != null) lunaOrbita = centroNave + new Vector3(10f, -95f, 250f);
            if (navegacion && luna != null && !luna.gameObject.activeSelf) luna.gameObject.SetActive(true);
        }

        public void LanzarSonda()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "SondaDesplegada";
            Destroy(go.GetComponent<Collider>());
            if (materialSonda != null) go.GetComponent<Renderer>().sharedMaterial = materialSonda;
            go.transform.position = centroNave + new Vector3(0f, -0.3f, 6.5f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            sonda = go.transform;
            tiempoSonda = 0f;
        }

        // ------------------------------------------------------------------ Escombros

        /// <summary>Crea un escombro delante de la nave. Si es apuntado, viene hacia la posición actual de la nave.</summary>
        public void LanzarEscombro(bool apuntado)
        {
            Vector2 xy = apuntado
                ? Desplazamiento + Random.insideUnitCircle * 1.5f
                : Desplazamiento + Random.insideUnitCircle.normalized * Random.Range(6f, 13f);

            GameObject go = GameObject.CreatePrimitive(Random.value < 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            go.name = "Escombro";
            Destroy(go.GetComponent<Collider>());
            if (materialEscombros != null) go.GetComponent<Renderer>().sharedMaterial = materialEscombros;
            float tam = Random.Range(1.4f, 3f);
            go.transform.localScale = new Vector3(tam, tam * Random.Range(0.6f, 1f), tam * Random.Range(0.7f, 1.2f));
            go.transform.rotation = Random.rotation;

            Roca r = new Roca { t = go.transform, xy = xy, z = 170f, radio = tam * 0.5f, giro = Random.insideUnitSphere * 90f };
            rocas.Add(r);
            Colocar(r);
        }

        void Colocar(Roca r)
        {
            r.t.position = centroNave + new Vector3(r.xy.x - Desplazamiento.x, r.xy.y - Desplazamiento.y, r.z);
        }

        // ------------------------------------------------------------------ Actualización

        void Update()
        {
            float dt = Time.deltaTime;

            // Escombros
            for (int i = rocas.Count - 1; i >= 0; i--)
            {
                Roca r = rocas[i];
                float zAntes = r.z;
                r.z -= velocidadEscombros * dt;
                r.t.Rotate(r.giro * dt, Space.World);
                Colocar(r);

                // Evaluación en la proa: un solo resultado por roca (impacto o esquivada) y la roca se elimina.
                // Las que no impactan pasan por fuera del contorno de la cabina y desaparecen fuera de la vista
                // de la ventana; nunca entran en las salas.
                if (zAntes > zProa && r.z <= zProa)
                {
                    bool impacto = (r.xy - Desplazamiento).magnitude < radioNave + r.radio;
                    AlPasarEscombro?.Invoke(impacto);
                    Destroy(r.t.gameObject);
                    rocas.RemoveAt(i);
                    continue;
                }
                if (r.z < -30f)
                {
                    Destroy(r.t.gameObject);
                    rocas.RemoveAt(i);
                }
            }

            // Estrellas (con navegación la velocidad puede ser negativa: la nave va hacia atrás por la ruta)
            if (malla != null && (VelocidadEstrellas > 0f || (navegacion && VelocidadEstrellas != 0f)))
            {
                float avance = VelocidadEstrellas * dt;
                for (int i = 0; i < centros.Length; i++)
                {
                    Vector3 c = centros[i];
                    c.z -= avance;
                    if (c.z < zMin) c.z += zMax - zMin;
                    else if (c.z > zMax) c.z -= zMax - zMin;
                    centros[i] = c;
                    EscribirEstrella(i);
                }
                malla.vertices = vertices;
            }

            // En órbita (con navegación): la vista vuelve a la proa de referencia poco a poco (solo dibujo; la nave ya no navega).
            if (navegacion && enOrbita && anguloNave != 0f)
            {
                anguloNave = Mathf.MoveTowards(anguloNave, anguloNave > 180f ? 360f : 0f, 60f * dt);
                if (anguloNave >= 360f) anguloNave = 0f;
                giro = Quaternion.Euler(0f, anguloNave, 0f);
                AplicarGiroEstrellas();
            }

            // Luna
            if (luna != null && luna.gameObject.activeSelf)
            {
                luna.Rotate(Vector3.up, 1.5f * dt, Space.World);
                if (enOrbita)
                    luna.position = Vector3.Lerp(luna.position, lunaOrbita, 1f - Mathf.Exp(-0.6f * dt));
            }

            // Sonda desplegada: se aleja hacia la luna y se hace pequeña
            if (sonda != null)
            {
                tiempoSonda += dt;
                sonda.position += new Vector3(0f, -1.2f, 8f) * dt;
                if (tiempoSonda > 8f)
                {
                    Destroy(sonda.gameObject);
                    sonda = null;
                }
            }
        }

        // ------------------------------------------------------------------ Utilidades

        void Cielo(Color c)
        {
            if (camara != null) camara.backgroundColor = c;
        }

        void CrearEstrellas()
        {
            if (estrellas == null) return;
            int n = Mathf.Max(1, cantidadEstrellas);
            centros = new Vector3[n];
            tamanos = new float[n];
            vertices = new Vector3[n * 4];
            int[] tri = new int[n * 12];

            for (int i = 0; i < n; i++)
            {
                Vector3 c;
                do c = new Vector3(Random.Range(-450f, 450f), Random.Range(-320f, 320f), Random.Range(zMin, zMax));
                while (Mathf.Abs(c.x) < 25f && Mathf.Abs(c.y) < 25f);
                centros[i] = c;
                tamanos[i] = Random.Range(0.6f, 1.8f);
                EscribirEstrella(i);

                int k = i * 4, t = i * 12;
                // Doble cara
                tri[t] = k; tri[t + 1] = k + 1; tri[t + 2] = k + 2;
                tri[t + 3] = k; tri[t + 4] = k + 2; tri[t + 5] = k + 3;
                tri[t + 6] = k; tri[t + 7] = k + 2; tri[t + 8] = k + 1;
                tri[t + 9] = k; tri[t + 10] = k + 3; tri[t + 11] = k + 2;
            }

            malla = new Mesh { name = "EstrellasViaje" };
            malla.MarkDynamic();
            malla.vertices = vertices;
            malla.triangles = tri;
            malla.bounds = new Bounds(Vector3.zero, new Vector3(2000f, 2000f, 2000f));
            estrellas.sharedMesh = malla;
            estrellas.transform.position = Vector3.zero;
        }

        void EscribirEstrella(int i)
        {
            Vector3 c = centros[i];
            float s = tamanos[i];
            int k = i * 4;
            vertices[k] = c + new Vector3(-s, -s, 0f);
            vertices[k + 1] = c + new Vector3(-s, s, 0f);
            vertices[k + 2] = c + new Vector3(s, s, 0f);
            vertices[k + 3] = c + new Vector3(s, -s, 0f);
        }
    }
}
