using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// RECORRIDO VISUAL DEL DESPACHO Y EL DESPEGUE (solo representación; nada de esto es pilotable ni simulación).
    ///
    /// 1. ENVÍO ORBITAL (solo si hace falta; SecuenciaEnvioOrbital.cs): VC-1 cargado → traslado a la plataforma exterior →
    ///    integración en su lanzador → cuenta atrás, encendido y lanzamiento → llegada a la zona de servicio orbital →
    ///    recepción, retirada de la pieza anterior, colocación y comprobación de cada componente → combustible y carga →
    ///    EM-4X preparada. Sin envío, no se prepara ni se lanza ningún transporte.
    /// 2. TRIPULACIÓN (L-4 con la configuración CONFIRMADA): carga de la bodega → traslado desde el edificio de integración a
    ///    la plataforma → acceso y embarque → cierre de compuerta → comprobación → cuenta atrás → encendido y ascenso →
    ///    llegada y atraque bajo el Mando de la EM-4X → a bordo (control del personaje en ExpedicionMVP).
    ///
    /// Todo se decide ANTES de empezar (requisitos, preparación cerrada, costes): las secuencias solo lo muestran. Cada
    /// parte se puede omitir (Espacio/Intro) después de haberla visto una vez en la sesión; omitirla llama a lo mismo que
    /// verla entera, así que el estado final es idéntico.
    /// </summary>
    public partial class SecuenciaDespegue : MonoBehaviour
    {
        static bool vista, vistaEnvio;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { vista = false; vistaEnvio = false; }

        enum Segmento { Envio, Tripulacion }
        Segmento segmento;
        ResumenExpedicion resumen;
        PreparacionExpedicion preparacion;
        CatalogoMision catalogo;
        Func<Color, bool, Material> fabricaMat;
        string progreso = "";

        public bool Activa { get; private set; }
        /// <summary>La parte de la tripulación ya se vio entera en esta sesión (se puede omitir).</summary>
        public static bool PuedeOmitirse => vista;
        /// <summary>La parte del envío orbital ya se vio entera en esta sesión (se puede omitir).</summary>
        public static bool PuedeOmitirseEnvio => vistaEnvio;

        static readonly Vector3 Lugar = new Vector3(5000f, 0f, 0f);     // lejos del hangar: plataforma de lanzamiento
        const float AlturaPlataformaMovil = 2f;
        static readonly Vector3 SalidaEdificio = new Vector3(0f, 0f, 95f);   // donde empieza el traslado (local a Lugar)

        Camera cam;
        Transform raiz, conjunto, cabina, compuerta, brazo;
        ModeloLanzadera modelo;
        Action alTerminar;
        List<string> lineas = new List<string>();
        string titulo = "", grande = "";
        int lineasVisibles;
        float fundido;
        bool terminada;
        AudioSource audioFuente;
        AudioClip pitido, motor;
        GUIStyle sTitulo, sTexto, sGrande, sPista;
        Color cieloTierra = new Color(0.55f, 0.72f, 0.9f), cieloEspacio = new Color(0.01f, 0.01f, 0.03f);
        Material mInterior, mNaranja, mPantalla;

        static Material Mat(Color c, bool sinLuz)
        {
            Shader sh = Shader.Find(sinLuz ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find(sinLuz ? "Unlit/Color" : "Standard");
            Material m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        Transform Caja(string n, Transform padre, Vector3 pos, Vector3 esc, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            Collider c = g.GetComponent<Collider>(); c.enabled = false; Destroy(c);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = esc;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        /// <summary>Escenario fijo: terreno, plataforma de lanzamiento, torre con brazo de acceso y edificio de integración.</summary>
        void Construir()
        {
            raiz = new GameObject("Plataforma de lanzamiento (provisional)").transform;
            raiz.position = Lugar;
            Material suelo = Mat(new Color(0.45f, 0.5f, 0.38f), false), hormigon = Mat(new Color(0.62f, 0.62f, 0.6f), false),
                     pared = Mat(new Color(0.55f, 0.57f, 0.6f), false), oscuro = Mat(new Color(0.2f, 0.21f, 0.24f), false);
            mNaranja = Mat(new Color(1f, 0.55f, 0.15f), false);
            mInterior = Mat(new Color(0.3f, 0.33f, 0.38f), false);
            mPantalla = Mat(new Color(0.3f, 0.85f, 1f), true);
            Caja("Terreno", raiz, new Vector3(0f, -0.5f, 0f), new Vector3(800f, 1f, 800f), suelo);
            Caja("Plataforma de lanzamiento", raiz, new Vector3(0f, 0.5f, 0f), new Vector3(18f, 1f, 18f), hormigon);
            Caja("Camino de traslado", raiz, new Vector3(0f, 0.02f, SalidaEdificio.z / 2f), new Vector3(10f, 0.05f, SalidaEdificio.z), oscuro);
            Caja("Torre de servicio", raiz, new Vector3(0f, 22f, -10f), new Vector3(3f, 44f, 3f), mNaranja);
            brazo = Caja("Brazo de acceso", raiz, new Vector3(0f, 20f, -5.4f), new Vector3(1.6f, 0.3f, 5.6f), mNaranja);

            // Edificio de integración visto desde fuera (de aquí sale la lanzadera montada)
            Vector3 e = SalidaEdificio + new Vector3(0f, 0f, 18f);
            // Misma forma que en el hangar: paredes hasta 36 m, cubierta abovedada de perfil ovalado hasta 52 m y puerta de 16 × 42 m
            Transform edif = new GameObject("Edificio de integración de la L-4 (exterior)").transform;
            edif.SetParent(raiz, false);
            edif.localPosition = e;
            SustitucionEscenario.Punto(edif, "B/EdificioExterior");
            Caja("Pared (oeste)", edif, new Vector3(-16f, 18f, 0f), new Vector3(0.5f, 36f, 36f), pared);
            Caja("Pared (este)", edif, new Vector3(16f, 18f, 0f), new Vector3(0.5f, 36f, 36f), pared);
            Caja("Fachada (a la izquierda de la puerta)", edif, new Vector3(-12f, 18f, -18f), new Vector3(8f, 36f, 0.5f), pared);
            Caja("Fachada (a la derecha de la puerta)", edif, new Vector3(12f, 18f, -18f), new Vector3(8f, 36f, 0.5f), pared);
            Caja("Fondo", edif, new Vector3(0f, 18f, 18f), new Vector3(32f, 36f, 0.5f), oscuro);
            GeometriaProvisional.Hastial("Hastial de la fachada", edif, false, -18f, -16f, 16f, 36f, 16f, b => Mathf.Abs(b) < 8f ? 42f : 36f, pared, false);
            GeometriaProvisional.Hastial("Hastial del fondo", edif, false, 18f, -16f, 16f, 36f, 16f, b => 36f, oscuro, false);
            GeometriaProvisional.Boveda("Cubierta abovedada", edif, false, -18.3f, 18.3f, -16.3f, 16.3f, 36f, 16f, 0.5f, pared, false);

            GameObject cg = new GameObject("Cámara del despegue");
            cam = cg.AddComponent<Camera>();
            cam.enabled = false;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cieloTierra;

            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            audioFuente.volume = 0.4f;
            pitido = Tono("pitido", 990f, 0.15f, 0f);
            motor = Tono("motor", 70f, 3f, 0.5f);
        }

        /// <summary>Monta la lanzadera con la configuración confirmada, sobre su plataforma móvil, y la cabina interior.</summary>
        void MontarLanzadera(ConfiguracionLanzadera config, Func<Color, bool, Material> fabrica)
        {
            if (modelo != null) modelo.Destruir();        // tras el ascenso cuelga de «raiz», no del conjunto
            if (refuerzosSueltos != null) Destroy(refuerzosSueltos.gameObject);
            refuerzosSueltos = null;
            foreach (Transform viejo in cargaL4) if (viejo != null) Destroy(viejo.gameObject);
            cargaL4.Clear();
            if (conjunto != null) Destroy(conjunto.gameObject);
            conjunto = new GameObject("Lanzadera L-4 y plataforma móvil").transform;
            conjunto.SetParent(raiz, false);
            Caja("Plataforma móvil", conjunto, new Vector3(0f, AlturaPlataformaMovil / 2f, 0f), new Vector3(12f, AlturaPlataformaMovil, 12f), mInterior);
            ConfiguracionLanzadera c = config != null ? config.Copia() : new ConfiguracionLanzadera();
            DatosLanzadera.Limitar(c);
            modelo = ModeloLanzadera.Construir(conjunto, new Vector3(0f, AlturaPlataformaMovil + 1f, 0f), c, fabrica ?? new Func<Color, bool, Material>(Mat));
            configActual = c;

            cabina = CrearCabina(modelo, out escotillaSuperior, out compuerta);
            cabina.gameObject.SetActive(false);
            modelo.Interior = cabina;
        }

        Transform escotillaSuperior, refuerzosSueltos;

        /// <summary>
        /// Cabina interior (se ve desde el asiento): paneles con ventana en el techo y, en el centro del techo, la escotilla
        /// del puerto de atraque (se abre en órbita). Girada para que la compuerta lateral quede en el lado de la escotilla
        /// de tierra (−z). La misma cabina se usa en tierra y en la L-4 de órbita.
        /// </summary>
        Transform CrearCabina(ModeloLanzadera m, out Transform escotilla, out Transform lateral)
        {
            Transform c = new GameObject("Cabina (interior)").transform;
            c.SetParent(m.Capsula, false);
            c.localPosition = new Vector3(0f, 1.5f, 0f);
            c.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Caja("Suelo de la cabina", c, new Vector3(0f, -1.4f, 0f), new Vector3(3.6f, 0.1f, 2.8f), mInterior);
            Caja("Pared trasera", c, new Vector3(0f, 0f, -1.4f), new Vector3(3.6f, 2.8f, 0.1f), mInterior);
            Caja("Pared delantera", c, new Vector3(0f, 0f, 1.4f), new Vector3(3.6f, 2.8f, 0.1f), mInterior);
            Caja("Pared derecha", c, new Vector3(1.8f, 0f, 0f), new Vector3(0.1f, 2.8f, 2.8f), mInterior);
            Caja("Pared izquierda (marco de la compuerta)", c, new Vector3(-1.8f, 0f, -0.95f), new Vector3(0.1f, 2.8f, 0.9f), mInterior);
            Caja("Pared izquierda (marco de la compuerta)", c, new Vector3(-1.8f, 0f, 0.95f), new Vector3(0.1f, 2.8f, 0.9f), mInterior);
            Caja("Techo (marco)", c, new Vector3(0f, 1.4f, -1f), new Vector3(3.6f, 0.1f, 0.8f), mInterior);
            Caja("Techo (marco)", c, new Vector3(0f, 1.4f, 1f), new Vector3(3.6f, 0.1f, 0.8f), mInterior);
            Caja("Consola", c, new Vector3(0f, 0.8f, 1.1f), new Vector3(2.2f, 0.5f, 0.4f), mPantalla);
            Caja("Asiento", c, new Vector3(0f, -0.9f, -0.3f), new Vector3(0.7f, 0.2f, 1.5f), mNaranja);
            lateral = Caja("Compuerta", c, new Vector3(-1.85f, 0f, -1.2f), new Vector3(0.08f, 2f, 1.1f), mNaranja);
            escotilla = Caja("Escotilla superior (puerto de atraque)", c, new Vector3(0f, 1.42f, 0f), new Vector3(1.2f, 0.06f, 1.2f), mNaranja);
            SustitucionEscenario.Punto(c, "L4/Cabina");
            SustitucionEscenario.Aplicar(c);
            return c;
        }

        static AudioClip Tono(string nombre, float hz, float s, float ruido)
        {
            const int f = 22050;
            int n = Mathf.CeilToInt(f * s);
            float[] d = new float[n];
            System.Random r = new System.Random(3);
            for (int i = 0; i < n; i++)
                d[i] = (Mathf.Sin(2f * Mathf.PI * hz * i / f) * (1f - ruido) + ((float)r.NextDouble() * 2f - 1f) * ruido)
                       * Mathf.Min(1f, Mathf.Min(i, n - i) / (f * 0.02f)) * 0.6f;
            AudioClip c = AudioClip.Create(nombre, n, 1, f, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>Vista desde dentro (cabina visible, exterior oculto) o desde fuera.</summary>
        void Dentro(bool dentro)
        {
            modelo.MostrarExterior(!dentro);
            cabina.gameObject.SetActive(dentro);
        }

        /// <summary>
        /// Empieza la secuencia. «comprobacion» son las líneas de la comprobación (requisitos ya cumplidos); «config» es la
        /// configuración CONFIRMADA de la lanzadera; «fabrica» da los materiales (null = propios).
        /// </summary>
        public void Iniciar(Vector3 ojos, List<string> comprobacion, ResumenExpedicion r, PreparacionExpedicion p, CatalogoMision cat,
                            Func<Color, bool, Material> fabrica, Action fin)
        {
            if (cam == null) Construir();
            resumen = r;
            segmento = HayEnvio ? Segmento.Envio : Segmento.Tripulacion;
            lineasVisibles = 0; grande = ""; progreso = "";
            preparacion = p.Copia();
            catalogo = cat;
            fabricaMat = fabrica ?? new Func<Color, bool, Material>(Mat);
            lineas = comprobacion;
            alTerminar = fin;
            terminada = false;
            cam.enabled = true;
            cam.tag = "MainCamera";
            Activa = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StartCoroutine(Ejecutar());
        }

        bool HayEnvio => resumen != null && resumen.transporte != null && resumen.transporte.necesario;

        IEnumerator Ejecutar()
        {
            titulo = "EMBARQUE CONFIRMADO"; grande = ""; progreso = "";
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / 0.5f) yield return null;
            if (HayEnvio && Cfg.mostrarEnvioVC1)
            {
                segmento = Segmento.Envio;
                yield return EjecutarEnvio();
                vistaEnvio = true;
            }
            if (Cfg.mostrarTripulacionL4) yield return EjecutarTripulacion();
            Terminar();
        }

        /// <summary>Prepara la parte de la tripulación desde cero (también tras omitir el envío).</summary>
        void PrepararTripulacion()
        {
            segmento = Segmento.Tripulacion;
            LimpiarEnvio();
            cam.transform.SetParent(null, true);          // antes de rehacer la lanzadera (la cámara pudo quedar en la cabina)
            MontarLanzadera(preparacion.lanzadera, fabricaMat);
            fundido = 1f; lineasVisibles = 0;
            titulo = ""; grande = ""; progreso = "TRIPULACIÓN · " + DatosLanzadera.Resumen(ConfigActual());
            conjunto.localPosition = SalidaEdificio;
            cam.backgroundColor = cieloTierra;
            Dentro(false);
            cam.transform.position = Lugar + new Vector3(55f, 14f, 40f);
            cam.transform.LookAt(conjunto.position + Vector3.up * 16f);
        }

        IEnumerator EjecutarTripulacion()
        {
            PrepararTripulacion();
            yield return CargaL4();

            // 1 · Traslado: la lanzadera montada sale del edificio de integración hacia la plataforma (no es la plataforma)
            SecuenciasConfig q = Cfg;
            titulo = T("l4.traslado");
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(q.l4Traslado))
            {
                fundido = Mathf.Max(0f, 1f - s * 4f);
                conjunto.localPosition = Vector3.Lerp(SalidaEdificio, Vector3.zero, Mathf.SmoothStep(0f, 1f, s));
                Encuadre("l4.traslado", conjunto, Lugar);
                yield return null;
            }
            conjunto.localPosition = Vector3.zero;
            fundido = 0f;

            // 2 · Acceso y embarque por el brazo de la torre hasta la escotilla (lado −z de la cápsula)
            titulo = T("l4.embarque");
            cam.fieldOfView = 70f;
            float hc = modelo.Capsula.position.y + 1.6f;
            brazo.position = new Vector3(Lugar.x, hc - 1.7f, Lugar.z - 5.4f);
            Vector3 a0 = new Vector3(Lugar.x, hc, Lugar.z - 8.2f), a1 = new Vector3(Lugar.x, hc, Lugar.z - 3.2f);
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(q.l4Embarque))
            {
                cam.transform.SetPositionAndRotation(Vector3.Lerp(a0, a1, Mathf.SmoothStep(0f, 1f, s)), Quaternion.LookRotation(Vector3.forward));
                yield return null;
            }

            // 3 · Asiento y cierre de compuerta
            titulo = T("l4.compuerta");
            Dentro(true);                                                          // desde dentro: cabina y, por la ventana, el cielo
            cam.transform.SetParent(cabina, false);
            cam.transform.localPosition = new Vector3(0.3f, -0.4f, -0.3f);
            cam.transform.localRotation = Quaternion.LookRotation(Vector3.left + Vector3.up * 0.1f, Vector3.up);
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(q.l4Compuerta))
            {
                compuerta.localPosition = new Vector3(-1.85f, 0f, Mathf.Lerp(-1.2f, 0f, Mathf.SmoothStep(0f, 1f, s)));
                yield return null;
            }
            audioFuente.PlayOneShot(pitido);
            grande = "COMPUERTA CERRADA";
            yield return Esperar(0.8f);
            grande = "";

            // 4 · Comprobación de la preparación (la de verdad: ya se verificó antes de empezar)
            titulo = T("l4.comprobacion");
            Quaternion qa = cam.transform.localRotation, qb = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 1f)
            {
                cam.transform.localRotation = Quaternion.Slerp(qa, qb, Mathf.SmoothStep(0f, 1f, s));
                yield return null;
            }
            for (lineasVisibles = 0; lineasVisibles < lineas.Count; lineasVisibles++) yield return Esperar(D(0.45f));
            yield return Esperar(D(0.6f));
            lineasVisibles = 0;

            // 5 · Cuenta atrás
            titulo = T("l4.cuenta");
            for (int n = Mathf.Max(1, q.l4CuentaAtras); n >= 1; n--)
            {
                grande = n.ToString();
                audioFuente.PlayOneShot(pitido);
                yield return Esperar(D(1f));
            }

            // 6 · Encendido
            titulo = T("l4.encendido");
            grande = "IGNICIÓN";
            Transform llama = modelo.Llama;
            llama.gameObject.SetActive(true);
            audioFuente.PlayOneShot(motor);
            Vector3 base0 = cam.transform.localPosition;
            for (float s = 0f; s < D(1.8f); s += Time.deltaTime)
            {
                cam.transform.localPosition = base0 + UnityEngine.Random.insideUnitSphere * 0.03f;
                llama.localScale = new Vector3(3f, 5f + Mathf.Sin(Time.time * 40f) * 0.7f, 3f);
                yield return null;
            }
            grande = "";

            // 7 · Ascenso: vista desde fuera y vuelta al asiento (la plataforma móvil se queda en tierra)
            titulo = T("l4.ascenso");
            Dentro(false);
            cam.transform.SetParent(null, true);
            modelo.Raiz.SetParent(raiz, true);
            Vector3 inicio = modelo.Raiz.localPosition;
            float subida = 0f, durAsc = D(q.l4Ascenso);
            Transform refuerzos;
            modelo.Piezas.TryGetValue(PiezaLanzadera.Refuerzos, out refuerzos);
            bool sueltos = false;
            Vector3 velRefuerzos = Vector3.zero;
            for (float s = 0f; s < durAsc; s += Time.deltaTime)
            {
                float u = s / durAsc * 4.5f;                                   // misma subida que antes, escalada al tiempo
                subida = 0.5f * 9f * u * u;
                modelo.Raiz.localPosition = inicio + Vector3.up * subida;
                llama.localScale = new Vector3(3f, 5f + Mathf.Sin(Time.time * 40f) * 0.7f, 3f);
                // Separación de los refuerzos (si los hay): se sueltan y caen hacia fuera, sin cruzarse con la etapa
                if (refuerzos != null && q.mostrarSeparaciones && u > 3.2f)
                {
                    if (!sueltos)
                    {
                        sueltos = true;
                        titulo = T("l4.refuerzos");
                        refuerzos.SetParent(raiz, true);
                        refuerzosSueltos = refuerzos;
                        velRefuerzos = Vector3.up * (9f * u * 0.6f);
                        audioFuente.PlayOneShot(pitido);
                    }
                    velRefuerzos += Vector3.down * 9f * Time.deltaTime;
                    refuerzos.position += velRefuerzos * Time.deltaTime;
                    foreach (Transform r1 in refuerzos)
                        r1.localPosition += new Vector3(r1.localPosition.x, 0f, r1.localPosition.z).normalized * 3f * Time.deltaTime;
                }
                Encuadre("l4.ascenso", modelo.Raiz, Lugar);
                cam.backgroundColor = Color.Lerp(cieloTierra, cieloEspacio, s / durAsc * 0.4f);
                yield return null;
            }
            if (refuerzos != null && sueltos) refuerzos.gameObject.SetActive(false);
            Dentro(true);
            cam.transform.SetParent(cabina, false);
            cam.transform.localPosition = base0;
            cam.transform.localRotation = qb;
            titulo = T("l4.ascenso");
            float v = 9f * 4.5f;
            for (float s = 0f; s < D(3f); s += Time.deltaTime)
            {
                subida += v * Time.deltaTime;
                modelo.Raiz.localPosition = inicio + Vector3.up * subida;
                cam.backgroundColor = Color.Lerp(cieloTierra, cieloEspacio, 0.4f + s / D(3f) * 0.6f);
                cam.transform.localPosition = base0 + UnityEngine.Random.insideUnitSphere * 0.015f;
                yield return null;
            }

            // 8 · Espacio (separación de la etapa), aproximación, alineación, atraque, apertura y paso a la EM-4X
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / D(0.6f)) yield return null;
            yield return LlegadaL4();
            fundido = 1f;         // la cámara ya está en el puerto del Mando: la expedición sigue desde esa misma posición
        }

        ConfiguracionLanzadera configActual;
        ConfiguracionLanzadera ConfigActual() => configActual ?? new ConfiguracionLanzadera();

        IEnumerator Esperar(float s)
        {
            for (float x = 0f; x < s; x += Time.deltaTime) yield return null;
        }

        IEnumerator TripulacionYFin()
        {
            if (Cfg.mostrarTripulacionL4) yield return EjecutarTripulacion();
            Terminar();
        }

        void Terminar()
        {
            if (terminada) return;
            terminada = true;
            vista = true;
            if (HayEnvio) vistaEnvio = true;
            StopAllCoroutines();
            fundido = 1f;
            alTerminar?.Invoke();
        }

        /// <summary>Cierra la secuencia sin cargar nada (por ejemplo, si la escena de la expedición no se pudo abrir).</summary>
        public void Detener()
        {
            StopAllCoroutines();
            Activa = false;
            terminada = true;
            fundido = 0f;
            LimpiarEnvio();
            if (orbita != null) { orbita.Destruir(); orbita = null; }
            if (cam != null) { cam.enabled = false; cam.tag = "Untagged"; }
        }

        void Update()
        {
            if (!Activa || terminada) return;
            Keyboard k = Keyboard.current;
            if (k == null || !(k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) return;
            bool libre = Cfg.omitirDesdeLaPrimeraVez;
            if (segmento == Segmento.Envio && (vistaEnvio || libre))
            {
                // Omitir el envío: solo se salta su representación; se sigue con la tripulación (mismo estado final)
                StopAllCoroutines();
                StartCoroutine(TripulacionYFin());
            }
            else if (segmento == Segmento.Tripulacion && (vista || libre)) Terminar();
        }

        void OnGUI()
        {
            if (!Activa) return;
            if (sTexto == null)
            {
                sTexto = new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true, alignment = TextAnchor.UpperLeft };
                sTexto.normal.textColor = new Color(0.9f, 0.97f, 1f);
                sTitulo = new GUIStyle(sTexto) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                sTitulo.normal.textColor = new Color(0.55f, 0.9f, 1f);
                sGrande = new GUIStyle(sTexto) { fontSize = 90, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                sGrande.normal.textColor = new Color(1f, 0.85f, 0.4f);
                sPista = new GUIStyle(sTexto) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            }
            float esc = Screen.height / 1080f, ancho = Screen.width / esc;
            GUI.matrix = Matrix4x4.Scale(new Vector3(esc, esc, 1f));
            Color c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, ancho, 70f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, 1010f, ancho, 70f), Texture2D.whiteTexture);
            GUI.color = c;
            GUI.Label(new Rect(0f, 8f, ancho, 54f), titulo, sTitulo);
            if (progreso.Length > 0) GUI.Label(new Rect(ancho - 420f, 76f, 400f, 30f), progreso, sPista);
            if (grande.Length > 0) GUI.Label(new Rect(0f, 380f, ancho, 160f), grande, sGrande);
            if (lineasVisibles > 0)
            {
                Rect r = new Rect(60f, 120f, 900f, 40f + 34f * lineasVisibles);
                GUI.color = new Color(0f, 0.04f, 0.08f, 0.7f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = c;
                for (int i = 0; i < lineasVisibles && i < lineas.Count; i++)
                    GUI.Label(new Rect(r.x + 16f, r.y + 14f + i * 34f, r.width - 32f, 34f), "<color=#59CC73>[OK]</color> " + lineas[i], sTexto);
            }
            bool omitible = (segmento == Segmento.Envio ? vistaEnvio : vista) || Cfg.omitirDesdeLaPrimeraVez;
            string que = segmento == Segmento.Envio ? "VC-1 · envío orbital sin tripulación (representación)" : "L-4 · tripulación (representación, no pilotable)";
            GUI.Label(new Rect(0f, 1022f, ancho, 46f), que + " · " + (omitible ? T("omitir") : T("omitir.luego")), sPista);
            if (fundido > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(fundido));
                GUI.DrawTexture(new Rect(0f, 0f, ancho, 1080f), Texture2D.whiteTexture);
                GUI.color = c;
            }
        }
    }
}
