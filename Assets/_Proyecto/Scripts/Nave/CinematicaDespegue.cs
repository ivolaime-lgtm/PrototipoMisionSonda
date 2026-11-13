using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Toma exterior breve del despegue (solo visual y sonido). La añade ControlNave en tiempo de ejecución;
    /// no toca la escena ni las mecánicas: lee Fase, TiempoFase, CuentaAtrasRestante y Etapa.
    ///
    ///   · Últimos segundos de la cuenta atrás: plano desde el suelo, encendido del lanzador, humo y vibración.
    ///   · Ascenso: plano de seguimiento; el cielo pasa de azul a negro y aparecen las estrellas.
    ///   · Separación (la orden sigue siendo Espacio en Pilotaje): el lanzador se suelta y cae, se enciende el motor de la nave.
    ///   · Al pasar a crucero: la cámara se aleja y se ve el borde del planeta; fundido y vuelta a la cabina.
    ///
    /// La nave se construye con el motor y el tanque ELEGIDOS (mismas formas que la vista de Configuracion).
    /// El decorado está muy lejos de la cabina (y = −20 000) y se destruye al terminar. Intro: volver antes a la cabina.
    /// Altitud y velocidad mostradas son ILUSTRATIVAS (tiempo comprimido). Formas simples provisionales.
    /// </summary>
    public class CinematicaDespegue : MonoBehaviour
    {
        /// <summary>True mientras se ve la toma exterior (HudNave oculta sus paneles laterales).</summary>
        public static bool Activa { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() => Activa = false;

        [Tooltip("Segundos de cuenta atrás en los que empieza la toma exterior.")]
        [SerializeField] float segundosAntesDespegue = 3f;
        [Tooltip("Segundos de alejamiento al llegar al crucero, antes de volver a la cabina.")]
        [SerializeField] float segundosAlejamiento = 3f;
        [Tooltip("Altura del decorado al final del ascenso (unidades de escena, no km reales).")]
        [SerializeField] float alturaFinal = 1400f;
        [Range(0f, 1f)] [SerializeField] float volumenGeneral = 0.8f;
        [Range(0f, 1f)] [SerializeField] float vibracion = 1f;

        static readonly Vector3 Origen = new Vector3(0f, -20000f, 0f);
        const float RadioPlaneta = 2000f;

        enum Estado { Esperando, Exterior, Alejamiento, Terminada }
        Estado estado;
        float tEstado, fundido;

        ControlNave control;
        Camera camaraCabina, camara;
        Transform raiz, pila, eje, lanzador, llamaLanzador, llamaNave, estrellas;
        Light luzLlama;
        float escalaLanzador = 1f, tamanoTobera = 1f, factorEmpuje = 1f;
        bool separada;
        Vector3 velLanzador, posAnterior, velNave;
        float tiempoLanzadorSuelto;

        readonly List<Material> materiales = new List<Material>();
        Texture2D texturaPlaneta;
        Material matHumo;
        readonly List<Transform> humo = new List<Transform>();
        readonly List<Vector3> velHumo = new List<Vector3>();
        readonly List<float> vidaHumo = new List<float>();
        float acumHumo;

        AudioSource fuenteMotor, fuenteGolpe;
        AudioClip clipMotor, clipGolpe;

        // ------------------------------------------------------------------ Inicio

        void Start()
        {
            control = GetComponent<ControlNave>();
            CrearSonido();
        }

        void Update()
        {
            if (control == null || control.Nave == null) return;
            float dt = Time.deltaTime;
            ActualizarSonido(dt);

            switch (estado)
            {
                case Estado.Esperando:
                    if (control.Fase == FaseNave.CuentaAtras && control.CuentaAtrasRestante <= segundosAntesDespegue ||
                        control.Fase == FaseNave.Ascenso)
                        Empezar();
                    else if (control.Fase != FaseNave.Preparacion && control.Fase != FaseNave.CuentaAtras)
                        estado = Estado.Terminada;     // la escena empezó más tarde (no debería ocurrir)
                    break;

                case Estado.Exterior:
                    if (PulsoIntro() || control.Fase == FaseNave.Abortada || control.Fase == FaseNave.Preparacion)
                    {
                        Terminar(0.6f);
                        break;
                    }
                    if (control.Fase != FaseNave.CuentaAtras && control.Fase != FaseNave.Ascenso)
                    {
                        estado = Estado.Alejamiento;
                        tEstado = 0f;
                    }
                    Animar(dt);
                    break;

                case Estado.Alejamiento:
                    tEstado += dt;
                    if (PulsoIntro()) { Terminar(0.6f); break; }
                    Animar(dt);
                    fundido = Mathf.Clamp01((tEstado - (segundosAlejamiento - 0.6f)) / 0.6f);
                    if (tEstado >= segundosAlejamiento) Terminar(1f);
                    break;

                case Estado.Terminada:
                    fundido = Mathf.Max(0f, fundido - dt * 2f);
                    break;
            }
        }

        static bool PulsoIntro()
        {
            Keyboard k = Keyboard.current;
            return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
        }

        // ------------------------------------------------------------------ Construcción del decorado

        void Empezar()
        {
            estado = Estado.Exterior;
            Activa = true;

            ResumenMision m = control.Mision;
            OpcionMotorNave motor = m != null ? m.motorNave : null;
            OpcionTanqueNave tanque = m != null ? m.tanqueNave : null;
            escalaLanzador = 1f + 0.25f * (MisionActual.Configurada ? MisionActual.Eleccion.lanzador : 0);
            tamanoTobera = motor != null ? Mathf.Max(0.5f, motor.tamanoTobera) : 1f;
            factorEmpuje = control.Nave.empujeN > 0f ? Mathf.Clamp(control.Nave.empujeN / 26000f, 0.7f, 1.6f) : 1f;

            raiz = new GameObject("CinematicaDespegue (solo visual)").transform;
            raiz.position = Origen;

            ConstruirPlaneta();
            ConstruirPlataforma();
            ConstruirPila(motor, tanque);
            ConstruirEstrellas();
            matHumo = Mat(new Color(0.78f, 0.76f, 0.74f), 0f, 0.1f);

            GameObject sol = new GameObject("Sol");
            sol.transform.SetParent(raiz, false);
            sol.transform.rotation = Quaternion.Euler(35f, -50f, 0f);
            Light ls = sol.AddComponent<Light>();
            ls.type = LightType.Directional;
            ls.intensity = 1.1f;
            ls.color = new Color(1f, 0.97f, 0.92f);

            // Cámara propia; la de la cabina se apaga mientras dura la toma y se restaura al terminar.
            JugadorNave j = FindFirstObjectByType<JugadorNave>();
            camaraCabina = j != null && j.Camara != null ? j.Camara : Camera.main;
            GameObject cam = new GameObject("CamaraDespegue");
            cam.transform.SetParent(raiz, false);
            camara = cam.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.45f, 0.65f, 0.9f);
            camara.nearClipPlane = 0.5f;
            camara.farClipPlane = 9000f;
            camara.fieldOfView = 40f;
            camara.depth = (camaraCabina != null ? camaraCabina.depth : 0f) + 5f;
            if (camaraCabina != null) camaraCabina.enabled = false;

            posAnterior = pila.position;
            Animar(0f);
        }

        void ConstruirPlaneta()
        {
            // Planeta ficticio: esfera grande con textura generada (océano, tierra y nubes). Girada para que la
            // plataforma quede en el ecuador de la textura y no en un polo.
            texturaPlaneta = new Texture2D(512, 256, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, name = "PlanetaFicticio" };
            Color oceano = new Color(0.08f, 0.22f, 0.45f), tierra = new Color(0.28f, 0.42f, 0.22f),
                  arena = new Color(0.55f, 0.5f, 0.36f), nube = new Color(0.92f, 0.94f, 0.97f);
            Color[] px = new Color[512 * 256];
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 512; x++)
                {
                    float u = x / 512f, v = y / 256f;
                    // Ruido periódico en x para que no se vea la costura.
                    float a = u * Mathf.PI * 2f;
                    float n = Mathf.PerlinNoise(3f + Mathf.Cos(a) * 2.2f, v * 5f + Mathf.Sin(a) * 2.2f) * 0.7f +
                              Mathf.PerlinNoise(10f + Mathf.Cos(a) * 6f, v * 14f + Mathf.Sin(a) * 6f) * 0.3f;
                    Color c = n < 0.5f ? oceano : (n < 0.53f ? arena : Color.Lerp(tierra, arena, (n - 0.53f) * 2f));
                    float nub = Mathf.PerlinNoise(40f + Mathf.Cos(a) * 9f, v * 22f + Mathf.Sin(a) * 9f);
                    if (nub > 0.62f) c = Color.Lerp(c, nube, Mathf.Clamp01((nub - 0.62f) * 5f));
                    if (Mathf.Abs(v - 0.5f) > 0.44f) c = Color.Lerp(c, nube, 0.8f);
                    px[y * 512 + x] = c;
                }
            texturaPlaneta.SetPixels(px);
            texturaPlaneta.Apply(true);

            Material mp = Mat(Color.white, 0f, 0.25f);
            mp.SetTexture("_BaseMap", texturaPlaneta);
            mp.mainTexture = texturaPlaneta;
            Transform p = Pieza(PrimitiveType.Sphere, raiz, new Vector3(0f, -RadioPlaneta, 0f), Vector3.one * RadioPlaneta * 2f, mp).transform;
            p.localRotation = Quaternion.Euler(90f, 0f, 0f);
            p.name = "Planeta (ficticio)";
        }

        void ConstruirPlataforma()
        {
            Material suelo = Mat(new Color(0.3f, 0.4f, 0.25f), 0f, 0.1f);
            Material hormigon = Mat(new Color(0.55f, 0.55f, 0.53f), 0f, 0.2f);
            Material torre = Mat(new Color(0.75f, 0.3f, 0.12f), 0.4f, 0.4f);
            // Terreno llano alrededor (tapa las caras de la esfera cerca de la plataforma).
            Pieza(PrimitiveType.Cylinder, raiz, new Vector3(0f, -0.6f, 0f), new Vector3(700f, 0.5f, 700f), suelo);
            Pieza(PrimitiveType.Cube, raiz, new Vector3(0f, -0.5f, 0f), new Vector3(18f, 1f, 18f), hormigon);
            Pieza(PrimitiveType.Cube, raiz, new Vector3(-6f, 14f, 0f), new Vector3(1.6f, 28f, 1.6f), torre);
            Pieza(PrimitiveType.Cube, raiz, new Vector3(-4f, 22f, 0f), new Vector3(3.5f, 0.5f, 0.8f), torre);
            Pieza(PrimitiveType.Cube, raiz, new Vector3(-4f, 8f, 0f), new Vector3(3.5f, 0.5f, 0.8f), torre);
        }

        // Pila vertical: mismas medidas que VistaNaveConfiguracion (allí está tumbada), con la base del lanzador
        // apoyada en la plataforma.
        void ConstruirPila(OpcionMotorNave motor, OpcionTanqueNave tanque)
        {
            Material blanco = Mat(new Color(0.88f, 0.9f, 0.93f), 0.1f, 0.5f);
            Material gris = Mat(new Color(0.5f, 0.55f, 0.62f), 0.5f, 0.5f);
            Material oscuro = Mat(new Color(0.18f, 0.2f, 0.24f), 0.7f, 0.5f);
            Material naranja = Mat(new Color(1f, 0.55f, 0.15f), 0.2f, 0.5f);
            Material vidrio = Mat(new Color(0.25f, 0.75f, 0.9f), 0.2f, 0.8f);

            pila = new GameObject("Nave y lanzador").transform;
            pila.SetParent(raiz, false);
            eje = new GameObject("Eje").transform;
            eje.SetParent(pila, false);
            eje.localPosition = new Vector3(0f, 11.8f * escalaLanzador, 0f);

            // Lanzador (se separa)
            lanzador = new GameObject("Lanzador").transform;
            lanzador.SetParent(eje, false);
            Pieza(PrimitiveType.Cylinder, lanzador, new Vector3(0f, -6f, 0f), new Vector3(2.2f, 5f, 2.2f), blanco);
            Pieza(PrimitiveType.Cylinder, lanzador, new Vector3(0f, -1.3f, 0f), new Vector3(2.25f, 0.3f, 2.25f), naranja);
            Pieza(PrimitiveType.Cylinder, lanzador, new Vector3(0f, -11.4f, 0f), new Vector3(1.4f, 0.4f, 1.4f), oscuro);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                Transform aleta = Pieza(PrimitiveType.Cube, lanzador, Vector3.zero, new Vector3(0.12f, 2f, 1.2f), gris).transform;
                aleta.localRotation = Quaternion.Euler(0f, a, 0f);
                aleta.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, -10.2f, 1.5f);
            }
            lanzador.localScale = new Vector3(Mathf.Lerp(1f, escalaLanzador, 0.5f), escalaLanzador, Mathf.Lerp(1f, escalaLanzador, 0.5f));
            llamaLanzador = Llama(lanzador, new Vector3(0f, -11.8f, 0f), 1.5f, 7f, new Color(1f, 0.55f, 0.15f));

            // Vehículo tripulado (continúa): motor elegido + tanque elegido + bodega + módulo de tripulación.
            Transform nave = new GameObject("Vehículo tripulado").transform;
            nave.SetParent(eje, false);

            float largo = tanque != null ? Mathf.Max(1f, tanque.largo) : 3.2f;
            float diametro = tanque != null ? Mathf.Max(0.5f, tanque.diametro) : 2.0f;
            Material matTanque = Mat(tanque != null ? tanque.color : new Color(0.5f, 0.55f, 0.62f), 0.5f, 0.5f);
            Pieza(PrimitiveType.Cylinder, nave, new Vector3(0f, 1.6f + largo / 2f, 0f), new Vector3(diametro, largo / 2f, diametro), matTanque);
            int bandas = Mathf.Max(1, Mathf.RoundToInt(largo / 2f));
            for (int i = 0; i < bandas; i++)
                Pieza(PrimitiveType.Cylinder, nave, new Vector3(0f, 1.6f + largo * (i + 1) / (bandas + 1), 0f),
                      new Vector3(diametro + 0.05f, 0.06f, diametro + 0.05f), naranja);

            float s = 1.6f + largo;
            Pieza(PrimitiveType.Cylinder, nave, new Vector3(0f, s + 0.6f, 0f), new Vector3(2.1f, 0.6f, 2.1f), oscuro);
            Pieza(PrimitiveType.Cylinder, nave, new Vector3(0f, s + 2.2f, 0f), new Vector3(2.2f, 1.0f, 2.2f), blanco);
            Pieza(PrimitiveType.Cube, nave, new Vector3(0f, s + 2.5f, -1.05f), new Vector3(1.0f, 0.35f, 0.1f), vidrio);
            Pieza(PrimitiveType.Cylinder, nave, new Vector3(0f, s + 3.5f, 0f), new Vector3(1.6f, 0.3f, 1.6f), blanco);
            Pieza(PrimitiveType.Cylinder, nave, new Vector3(0f, s + 4.0f, 0f), new Vector3(1.0f, 0.25f, 1.0f), blanco);
            Pieza(PrimitiveType.Sphere, nave, new Vector3(0f, s + 4.4f, 0f), new Vector3(0.55f, 0.55f, 0.55f), naranja);

            float t = tamanoTobera;
            Material matMotor = Mat(motor != null ? motor.color : new Color(0.35f, 0.37f, 0.4f), 0.8f, 0.6f);
            Vector3 b = new Vector3(0f, 1.6f, 0f);
            Pieza(PrimitiveType.Cylinder, nave, b + new Vector3(0f, -0.15f, 0f), new Vector3(0.6f * t, 0.15f, 0.6f * t), oscuro);
            Pieza(PrimitiveType.Cylinder, nave, b + new Vector3(0f, -0.5f, 0f), new Vector3(0.8f * t, 0.2f * t, 0.8f * t), matMotor);
            Pieza(PrimitiveType.Cylinder, nave, b + new Vector3(0f, -0.5f - 0.4f * t, 0f), new Vector3(1.05f * t, 0.2f * t, 1.05f * t), matMotor);
            Pieza(PrimitiveType.Cylinder, nave, b + new Vector3(0f, -0.5f - 0.8f * t, 0f), new Vector3(1.3f * t, 0.15f * t, 1.3f * t), matMotor);
            int aux = motor != null ? Mathf.Clamp(motor.toberasAuxiliares, 0, 8) : 0;
            for (int i = 0; i < aux; i++)
            {
                Vector3 p = Quaternion.Euler(0f, i * 360f / aux, 0f) * new Vector3(0.75f, -0.4f, 0f);
                Pieza(PrimitiveType.Cylinder, nave, b + p, new Vector3(0.28f, 0.3f, 0.28f), matMotor);
            }
            // Llama del motor de la nave: más grande con más empuje (se enciende tras la separación).
            llamaNave = Llama(nave, b + new Vector3(0f, -0.5f - 0.95f * t, 0f), 0.9f * t, 4.5f * factorEmpuje, new Color(0.55f, 0.75f, 1f));
            llamaNave.gameObject.SetActive(false);

            GameObject l = new GameObject("LuzLlama");
            l.transform.SetParent(lanzador, false);
            l.transform.localPosition = new Vector3(0f, -14f, 0f);
            luzLlama = l.AddComponent<Light>();
            luzLlama.type = LightType.Point;
            luzLlama.range = 70f;
            luzLlama.intensity = 0f;
            luzLlama.color = new Color(1f, 0.6f, 0.25f);
        }

        Transform Llama(Transform padre, Vector3 boca, float ancho, float largo, Color color)
        {
            Transform llama = new GameObject("Llama").transform;
            llama.SetParent(padre, false);
            llama.localPosition = boca;
            Material exterior = Mat(Color.black, 0f, 0f, color * 3f);
            Material nucleo = Mat(Color.black, 0f, 0f, Color.Lerp(color, Color.white, 0.6f) * 5f);
            Pieza(PrimitiveType.Sphere, llama, new Vector3(0f, -largo / 2f, 0f), new Vector3(ancho, largo, ancho), exterior);
            Pieza(PrimitiveType.Sphere, llama, new Vector3(0f, -largo * 0.3f, 0f), new Vector3(ancho * 0.55f, largo * 0.6f, ancho * 0.55f), nucleo);
            llama.localScale = new Vector3(1f, 0.001f, 1f);
            return llama;
        }

        void ConstruirEstrellas()
        {
            // Malla de pequeños cuadrados emisivos en una esfera alrededor de la cámara (aparece al oscurecer el cielo).
            const int n = 450;
            const float radio = 7000f;
            Random.State estadoAzar = Random.state;
            Random.InitState(7);
            var v = new Vector3[n * 4];
            var tri = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                Vector3 d = Random.onUnitSphere;
                Vector3 c = d * radio;
                Vector3 a = Vector3.Cross(d, Mathf.Abs(d.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                Vector3 bb = Vector3.Cross(d, a);
                float s = Random.Range(5f, 12f);
                v[i * 4] = c + (-a - bb) * s; v[i * 4 + 1] = c + (a - bb) * s;
                v[i * 4 + 2] = c + (a + bb) * s; v[i * 4 + 3] = c + (-a + bb) * s;
                tri[i * 6] = i * 4; tri[i * 6 + 1] = i * 4 + 2; tri[i * 6 + 2] = i * 4 + 1;
                tri[i * 6 + 3] = i * 4; tri[i * 6 + 4] = i * 4 + 3; tri[i * 6 + 5] = i * 4 + 2;
            }
            Random.state = estadoAzar;
            Mesh malla = new Mesh { name = "EstrellasDespegue", vertices = v, triangles = tri };
            malla.RecalculateNormals();
            malla.bounds = new Bounds(Vector3.zero, Vector3.one * radio * 2.2f);

            GameObject go = new GameObject("Estrellas");
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mat(Color.black, 0f, 0f, Color.white * 1.6f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Mat2Cara(mr.sharedMaterial);
            estrellas = go.transform;
            go.SetActive(false);
        }

        static void Mat2Cara(Material m)
        {
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);   // visibles desde ambos lados
        }

        // ------------------------------------------------------------------ Animación

        void Animar(float dt)
        {
            if (control.Fase == FaseNave.Ascenso) tFinAscenso = control.TiempoFase;
            float a = Progreso();                              // progreso del ascenso (casi quieto en el alejamiento)

            // Encendido: los dos últimos segundos de la cuenta atrás.
            float encendido = control.Fase == FaseNave.CuentaAtras ? Mathf.Clamp01(1f - control.CuentaAtrasRestante / 2f) : 1f;

            // Trayectoria: sube acelerando y se inclina un poco (giro gravitatorio simplificado).
            float h = alturaFinal * a * a;
            float giro = 22f * Mathf.Clamp01(a);
            pila.localPosition = new Vector3(h * 0.22f * a, h, 0f);
            pila.localRotation = Quaternion.Euler(0f, 0f, -giro);
            if (dt > 0f) velNave = (pila.position - posAnterior) / dt;
            posAnterior = pila.position;

            // Separación: el lanzador se suelta y cae, se apaga su llama y se enciende la de la nave.
            if (!separada && control.Etapa != ControlNave.EstadoEtapa.Pendiente && control.Fase != FaseNave.CuentaAtras)
            {
                separada = true;
                lanzador.SetParent(raiz, true);
                velLanzador = velNave * 0.85f;
                llamaLanzador.gameObject.SetActive(false);
                llamaNave.gameObject.SetActive(true);
                luzLlama.intensity = 0f;
                if (fuenteGolpe != null && clipGolpe != null) fuenteGolpe.PlayOneShot(clipGolpe, volumenGeneral);
            }
            if (separada)
            {
                tiempoLanzadorSuelto += dt;
                velLanzador += Vector3.down * 40f * dt;
                lanzador.position += velLanzador * dt;
                lanzador.Rotate(new Vector3(18f, 0f, 30f) * dt, Space.Self);
                if (tiempoLanzadorSuelto > 15f && lanzador.gameObject.activeSelf) lanzador.gameObject.SetActive(false);
            }

            // Llamas con parpadeo.
            float parpadeo = 1f + 0.12f * (Mathf.PerlinNoise(Time.time * 18f, 0.3f) - 0.5f) * 2f;
            if (!separada)
            {
                llamaLanzador.localScale = new Vector3(Mathf.Lerp(0.5f, 1f, encendido), Mathf.Max(0.001f, encendido * parpadeo), Mathf.Lerp(0.5f, 1f, encendido));
                luzLlama.intensity = 8f * encendido * parpadeo;
            }
            else
            {
                float on = Mathf.Clamp01(tiempoLanzadorSuelto * 2f);
                llamaNave.localScale = new Vector3(1f, Mathf.Max(0.001f, on * parpadeo), 1f);
            }

            // Humo en la plataforma mientras la nave está cerca del suelo.
            ActualizarHumo(dt, encendido > 0.2f && h < 80f ? encendido : 0f);

            // Cielo: de azul a negro con la altura; estrellas cuando ya está oscuro.
            float oscuro = Mathf.Clamp01(a * 1.35f);
            camara.backgroundColor = Color.Lerp(new Color(0.45f, 0.65f, 0.9f), new Color(0.005f, 0.008f, 0.02f), oscuro);
            bool verEstrellas = oscuro > 0.75f;
            if (estrellas.gameObject.activeSelf != verEstrellas) estrellas.gameObject.SetActive(verEstrellas);

            // Cámara: plano de suelo → seguimiento → alejamiento con el borde del planeta.
            Vector3 centro = pila.position + pila.up * 8f;
            Vector3 objetivoPos;
            Vector3 mirar;
            float fov;
            float sacudida;
            // Punto que mira el seguimiento: centro de la pila entera; tras la separación, centro de la nave.
            alturaMirada = Mathf.MoveTowards(alturaMirada, separada ? 4.5f : -1f, dt * 4f);
            Vector3 centroNave = eje.position + pila.up * alturaMirada;
            if (estado == Estado.Alejamiento)
            {
                // Cámara FIJADA a la nave (sin retraso): sube por encima y mira hacia abajo, al borde del planeta.
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tEstado / segundosAlejamiento));
                objetivoPos = pila.position + Vector3.Lerp(pila.rotation * offsetSeguimiento, new Vector3(-60f, 120f, -160f), k);
                mirar = Vector3.Lerp(centroNave, pila.position + Vector3.down * 90f, k);
                fov = 40f;
                sacudida = 0f;
                camara.transform.position = objetivoPos;
            }
            else if (control.Fase == FaseNave.CuentaAtras || a < 0.3f)
            {
                objetivoPos = Origen + new Vector3(-28f, 5f, -38f);
                mirar = Vector3.Lerp(Origen + new Vector3(0f, 12f, 0f), centro, 0.8f);
                fov = Mathf.Lerp(42f, 28f, a / 0.3f);
                sacudida = 0.25f * encendido;
                camara.transform.position = objetivoPos;
            }
            else
            {
                // Seguimiento FIJADO a la nave (sin retraso), para que conserve un tamaño legible aunque acelere.
                objetivoPos = pila.position + pila.rotation * offsetSeguimiento;
                mirar = centroNave;
                fov = 40f;
                sacudida = separada ? 0.04f : 0.12f;
                camara.transform.position = objetivoPos;
            }
            camara.fieldOfView = fov;
            camara.transform.LookAt(mirar);
            sacudida *= vibracion;
            if (sacudida > 0f)
            {
                float tt = Time.time * 25f;
                camara.transform.position += camara.transform.right * (Mathf.PerlinNoise(tt, 1.7f) - 0.5f) * sacudida +
                                             camara.transform.up * (Mathf.PerlinNoise(4.1f, tt) - 0.5f) * sacudida;
                camara.transform.Rotate(0f, 0f, (Mathf.PerlinNoise(tt, 9.3f) - 0.5f) * 2f * sacudida, Space.Self);
            }
            if (estrellas != null) estrellas.position = camara.transform.position;
        }

        float tFinAscenso;
        Vector3 offsetSeguimiento = new Vector3(-10f, -3f, -34f);
        float alturaMirada = -1f;

        /// <summary>Progreso del ascenso. En el alejamiento avanza muy despacio para que el encuadre del planeta sea estable.</summary>
        float Progreso()
        {
            float dur = Mathf.Max(0.1f, control.T.ascensoS);
            float tAsc = control.Fase == FaseNave.CuentaAtras ? 0f
                       : control.Fase == FaseNave.Ascenso ? control.TiempoFase
                       : tFinAscenso + tEstado * 0.03f * dur;
            return Mathf.Min(tAsc / dur, 1.6f);
        }

        void ActualizarHumo(float dt, float intensidad)
        {
            if (intensidad > 0f)
            {
                acumHumo += dt * 18f * intensidad;
                while (acumHumo >= 1f && humo.Count < 40)
                {
                    acumHumo -= 1f;
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Transform s = Pieza(PrimitiveType.Sphere, raiz, dir * 2f + Vector3.up * 1.5f, Vector3.one * 3f, matHumo).transform;
                    s.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    humo.Add(s);
                    velHumo.Add(dir * Random.Range(9f, 16f) + Vector3.up * Random.Range(0.5f, 3f));
                    vidaHumo.Add(0f);
                }
                if (humo.Count >= 40) acumHumo = 0f;
            }
            const float vidaMax = 4.5f;
            for (int i = humo.Count - 1; i >= 0; i--)
            {
                vidaHumo[i] += dt;
                float f = vidaHumo[i] / vidaMax;
                if (f >= 1f)
                {
                    Destroy(humo[i].gameObject);
                    humo.RemoveAt(i); velHumo.RemoveAt(i); vidaHumo.RemoveAt(i);
                    continue;
                }
                velHumo[i] *= 1f - Mathf.Min(1f, dt * 0.8f);
                humo[i].position += velHumo[i] * dt;
                float tam = Mathf.Lerp(3f, 13f, Mathf.Sqrt(f)) * (f > 0.8f ? (1f - f) / 0.2f : 1f);
                humo[i].localScale = Vector3.one * tam;
            }
        }

        void Terminar(float fundidoInicial)
        {
            estado = Estado.Terminada;
            Activa = false;
            fundido = Mathf.Max(fundido, fundidoInicial);
            if (camaraCabina != null) camaraCabina.enabled = true;
            if (raiz != null) Destroy(raiz.gameObject);
            raiz = null;
            camara = null;
            humo.Clear(); velHumo.Clear(); vidaHumo.Clear();
        }

        // ------------------------------------------------------------------ Sonido (generado, sin archivos)

        void CrearSonido()
        {
            const int hz = 22050;
            // Motor: ruido grave + dos tonos bajos, 2 s en bucle con fundido en la costura.
            int n = hz * 2, f = hz / 5;
            float[] datos = new float[n + f];
            float marron = 0f, max = 0.0001f;
            var azar = new System.Random(3);
            for (int i = 0; i < datos.Length; i++)
            {
                marron = marron * 0.985f + (float)(azar.NextDouble() * 2.0 - 1.0) * 0.12f;
                float tt = (float)i / hz;
                datos[i] = marron + 0.25f * Mathf.Sin(2f * Mathf.PI * 38f * tt) + 0.12f * Mathf.Sin(2f * Mathf.PI * 57f * tt);
            }
            float[] bucle = new float[n];
            for (int i = 0; i < n; i++)
            {
                float x = datos[i];
                if (i < f) x = x * ((float)i / f) + datos[n + i] * (1f - (float)i / f);
                bucle[i] = x;
                max = Mathf.Max(max, Mathf.Abs(x));
            }
            for (int i = 0; i < n; i++) bucle[i] *= 0.9f / max;
            clipMotor = AudioClip.Create("MotorDespegue", n, 1, hz, false);
            clipMotor.SetData(bucle, 0);

            // Golpe de la separación: ruido grave que se apaga + tono de 60 Hz.
            int g = (int)(hz * 0.8f);
            float[] golpe = new float[g];
            marron = 0f; max = 0.0001f;
            for (int i = 0; i < g; i++)
            {
                float tt = (float)i / hz;
                marron = marron * 0.97f + (float)(azar.NextDouble() * 2.0 - 1.0) * 0.3f;
                golpe[i] = (marron + 0.6f * Mathf.Sin(2f * Mathf.PI * 60f * tt)) * Mathf.Exp(-tt * 6f);
                max = Mathf.Max(max, Mathf.Abs(golpe[i]));
            }
            for (int i = 0; i < g; i++) golpe[i] *= 0.95f / max;
            clipGolpe = AudioClip.Create("GolpeSeparacion", g, 1, hz, false);
            clipGolpe.SetData(golpe, 0);

            fuenteMotor = gameObject.AddComponent<AudioSource>();
            fuenteMotor.clip = clipMotor;
            fuenteMotor.loop = true;
            fuenteMotor.playOnAwake = false;
            fuenteMotor.spatialBlend = 0f;
            fuenteMotor.volume = 0f;
            fuenteGolpe = gameObject.AddComponent<AudioSource>();
            fuenteGolpe.playOnAwake = false;
            fuenteGolpe.spatialBlend = 0f;
        }

        void ActualizarSonido(float dt)
        {
            if (fuenteMotor == null) return;
            float vol = 0f, tono = 1f;
            switch (control.Fase)
            {
                case FaseNave.CuentaAtras:
                    vol = 0.55f * Mathf.Clamp01(1f - control.CuentaAtrasRestante / 2f); tono = 0.8f; break;
                case FaseNave.Ascenso:
                    bool sep = control.Etapa != ControlNave.EstadoEtapa.Pendiente;
                    vol = sep ? 0.45f : 0.9f; tono = sep ? 1.15f : 0.9f; break;
                case FaseNave.Crucero:
                case FaseNave.Llegada:
                case FaseNave.Orbita:
                    vol = estado == Estado.Alejamiento ? 0.3f : 0.06f; tono = 0.7f; break;
            }
            vol *= volumenGeneral;
            fuenteMotor.volume = Mathf.MoveTowards(fuenteMotor.volume, vol, dt * 1.2f);
            fuenteMotor.pitch = Mathf.MoveTowards(fuenteMotor.pitch, tono, dt * 0.5f);
            if (fuenteMotor.volume > 0.001f && !fuenteMotor.isPlaying) fuenteMotor.Play();
            else if (fuenteMotor.volume <= 0.001f && fuenteMotor.isPlaying) fuenteMotor.Stop();
        }

        // ------------------------------------------------------------------ Rótulo y fundido

        GUIStyle sRotulo;
        Texture2D texNegra;

        void OnGUI()
        {
            if (!Activa && fundido <= 0f) return;
            GUI.depth = -50;
            float escala = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            float ancho = Screen.width / escala;

            if (Activa && control != null)
            {
                if (sRotulo == null)
                    sRotulo = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperLeft, richText = true };
                float a = Progreso();
                string datos = control.Fase == FaseNave.CuentaAtras
                    ? "En la plataforma"
                    : $"Altitud {180f * a * a:0} km · Velocidad {7800f * Mathf.Min(a, 1.1f):0} m/s";
                GUI.Label(new Rect(24f, 24f, 640f, 140f),
                          "<b>VISTA EXTERIOR (simulada)</b>\n" + datos +
                          "\n<size=17>Valores ilustrativos · tiempo comprimido · DEMOSTRACIÓN\nIntro: volver a la cabina</size>", sRotulo);
            }

            if (fundido > 0f)
            {
                if (texNegra == null) { texNegra = new Texture2D(1, 1); texNegra.SetPixel(0, 0, Color.black); texNegra.Apply(); }
                Color c = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, fundido);
                GUI.DrawTexture(new Rect(0f, 0f, ancho, 1080f), texNegra);
                GUI.color = c;
            }
            GUI.matrix = Matrix4x4.identity;
        }

        // ------------------------------------------------------------------ Utilidades

        GameObject Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material m)
        {
            GameObject go = GameObject.CreatePrimitive(tipo);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        Material Mat(Color c, float metalico, float suavidad, Color? emision = null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material m = new Material(sh);
            m.SetColor("_BaseColor", c);
            m.color = c;
            m.SetFloat("_Metallic", metalico);
            m.SetFloat("_Smoothness", suavidad);
            if (emision.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emision.Value);
            }
            materiales.Add(m);
            return m;
        }

        void OnDestroy()
        {
            Activa = false;
            if (camaraCabina != null) camaraCabina.enabled = true;
            if (raiz != null) Destroy(raiz.gameObject);
            foreach (Material m in materiales) if (m != null) Destroy(m);
            if (texturaPlaneta != null) Destroy(texturaPlaneta);
            if (texNegra != null) Destroy(texNegra);
            if (clipMotor != null) Destroy(clipMotor);
            if (clipGolpe != null) Destroy(clipGolpe);
        }
    }
}
