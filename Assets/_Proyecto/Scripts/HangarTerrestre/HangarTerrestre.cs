using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    public enum EstacionHangar { Mision, Configuracion, Manifiesto, Estanteria, Revision, Embarque }

    /// <summary>Punto de interacción del hangar (por cercanía y mirando hacia él).</summary>
    public class PuntoHangar
    {
        public EstacionHangar tipo;
        public string nombre, lote;
        /// <summary>Dónde se mide la distancia (delante del objeto).</summary>
        public Vector3 posicion;
        /// <summary>El objeto en sí (para comprobar que el jugador lo mira).</summary>
        public Vector3 objetivo;
    }

    /// <summary>
    /// HANGAR TERRESTRE (escena HangarExpedicion). Integrador: construye el hangar con bloques provisionales, mueve al
    /// personaje, gestiona las terminales y el embarque.
    ///
    /// Datos: NO hay otra configuración. Las elecciones son la PreparacionExpedicion de ExpedicionActual (la misma que usa
    /// ExpedicionMVP), el presupuesto y los requisitos salen de BalanceEM4X.Calcular, y la carga se deriva de ellos
    /// (CargaHangar). Al embarcar se confirma esa preparación y se abre ExpedicionMVP, que empieza en la transición.
    /// La EM-4X sigue en órbita: aquí solo se ve como holograma. Sin lanzadera detallada ni despegue.
    /// </summary>
    public class HangarTerrestre : MonoBehaviour
    {
        [SerializeField] CatalogoMision catalogoMision;
        [Tooltip("Solo para que el ejecutable incluya estos sombreadores (el hangar se construye al iniciar).")]
        [SerializeField] Material materialConLuz, materialSinLuz, materialTransparente;

        public CatalogoMision Catalogo { get; private set; }
        public PreparacionExpedicion Borrador { get; private set; }
        public ResumenExpedicion Resumen { get; private set; }
        public List<LoteCarga> Lotes { get; private set; } = new List<LoteCarga>();
        public CaminanteHangar Personaje { get; private set; }
        public PuntoHangar PuntoCercano { get; private set; }
        /// <summary>Terminal abierta (null = ninguna).</summary>
        public PuntoHangar Abierta { get; private set; }
        public bool CursorLibre { get; private set; }
        public bool Listo { get; private set; }
        public string Aviso { get; private set; } = "";
        public bool AvisoVisible => Aviso.Length > 0 && Time.time < avisoHasta;
        public readonly List<PuntoHangar> Puntos = new List<PuntoHangar>();

        float avisoHasta;
        Font fuente;
        Material mSuelo, mPared, mTecho, mTerminal, mPantalla, mEstanteria, mEnvio, mEmbarque, mHolo, mHoloMarcado, mSenal;
        readonly Dictionary<string, Material> mLote = new Dictionary<string, Material>();
        readonly Dictionary<string, Transform> estanterias = new Dictionary<string, Transform>();
        readonly Dictionary<string, Transform> huecosEnvio = new Dictionary<string, Transform>();
        readonly Dictionary<string, TextMesh> rotulosLote = new Dictionary<string, TextMesh>();
        readonly Dictionary<string, Renderer> holoModulos = new Dictionary<string, Renderer>();
        Transform holograma;
        TextMesh rotuloHolo, rotuloRevision, rotuloEmbarque;
        AudioSource audioFuente;
        AudioClip sonidoOk, sonidoError;

        static readonly string[] OrdenLotes = { "combustible", "suministros", "kits", "sonda", "componentes" };
        static readonly string[] NombresLotes = { "Combustible", "Suministros", "Kits de reparación", "Sonda de despliegue", "Componentes de sustitución" };

        void Awake()
        {
            Catalogo = catalogoMision != null ? catalogoMision : ScriptableObject.CreateInstance<CatalogoMision>();
            gameObject.AddComponent<HudHangarTerrestre>();
        }

        void Start()
        {
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Materiales();
            Construir();

            // Vuelta de una expedición: nueva preparación con las mismas selecciones, carga por preparar otra vez.
            if (CargaHangar.Embarcado)
            {
                CargaHangar.Embarcado = false;
                CargaHangar.Vaciar();
                ExpedicionActual.Reabrir();
                Avisar("De vuelta en tierra. Las selecciones se conservan; la carga de la próxima expedición está por preparar.", 8f);
            }
            else Avisar("Hangar terrestre. Sigue los rótulos: 1 Misión · 2 Configuración · 3 Carga · 4 Revisión · 5 Embarque.", 8f);

            Borrador = ExpedicionActual.Preparacion.Copia();
            Recalcular();

            Personaje = new GameObject().AddComponent<CaminanteHangar>();
            Personaje.Iniciar(new Vector3(0f, 0.05f, 3f), 0f);
            BloquearCursor(true);

            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            audioFuente.volume = 0.35f;
            sonidoOk = Tono("ok", 880f, 0.12f);
            sonidoError = Tono("error", 180f, 0.22f);
            Listo = true;
        }

        // ------------------------------------------------------------------ Datos (sin configuración propia)

        public void Recalcular()
        {
            Resumen = BalanceEM4X.Calcular(Borrador, Catalogo);
            Lotes = CargaHangar.Lotes(Borrador, Resumen);
            ActualizarVisual();
        }

        /// <summary>Aplica un cambio a la preparación en curso. Con la preparación cerrada no cambia nada.</summary>
        public bool Cambiar(Action<PreparacionExpedicion> cambio)
        {
            if (ExpedicionActual.Confirmada)
            {
                Respuesta(false, "La preparación está CERRADA. Reábrela en la terminal de Revisión para cambiarla.");
                return false;
            }
            string antes = JsonUtility.ToJson(Borrador);
            cambio(Borrador);
            Borrador.tramosCombustible = Mathf.Clamp(Borrador.tramosCombustible, 0, BalanceEM4X.MaximoTramos(BalanceEM4X.Tanques(Catalogo.catalogoNave)[Mathf.Clamp(Borrador.tanque, 0, 1)]));
            Borrador.suministrosExtra = Mathf.Clamp(Borrador.suministrosExtra, 0, BalanceEM4X.MaximoSuministrosExtra);
            Borrador.kitsExtra = Mathf.Clamp(Borrador.kitsExtra, 0, BalanceEM4X.MaximoKitsExtra);
            if (JsonUtility.ToJson(Borrador) == antes) return false;
            ExpedicionActual.GuardarBorrador(Borrador);     // la misma preparación que usa ExpedicionMVP
            Recalcular();
            return true;
        }

        public void ElegirMision(int i)
        {
            if (ExpedicionActual.Confirmada) { Respuesta(false, "La preparación está CERRADA. Reábrela en la terminal de Revisión."); return; }
            Cambiar(p => p.mision = i);
            CargaHangar.MisionElegida = true;
            ExpedicionActual.GuardarBorrador(Borrador);
            Recalcular();
            Respuesta(true, "Misión seleccionada: " + Resumen.mision.nombre + (Resumen.mision.requiereSonda && !Borrador.sonda ? ". Necesita sonda: añádela en la terminal de Carga." : "."));
        }

        public bool ConfigValida => Resumen != null && Resumen.problemas.Count - (PresupuestoSuperado ? 1 : 0) == 0;
        public bool PresupuestoSuperado => Resumen != null && Resumen.costeTotalMUSD > Resumen.presupuestoMUSD + 0.001f;

        public struct Requisito { public string texto; public bool ok; }

        /// <summary>Requisitos obligatorios para embarcar (todos salen de los validadores existentes y de la carga).</summary>
        public List<Requisito> Requisitos()
        {
            int pendientes = CargaHangar.Pendientes(Lotes);
            return new List<Requisito>
            {
                new Requisito { ok = CargaHangar.MisionElegida, texto = CargaHangar.MisionElegida ? "Misión seleccionada: " + Resumen.mision.nombre : "Elegir la misión en la terminal de Misión" },
                new Requisito { ok = ConfigValida, texto = ConfigValida ? "Configuración válida" : "Configuración no válida (ver problemas)" },
                new Requisito { ok = !PresupuestoSuperado, texto = $"Presupuesto: {Resumen.costeTotalMUSD:0.#} de {Resumen.presupuestoMUSD:0} M USD" + (PresupuestoSuperado ? " · SUPERADO" : "") },
                new Requisito { ok = pendientes == 0, texto = pendientes == 0 ? $"Carga preparada ({Lotes.Count} lotes)" : $"Faltan {pendientes} lote(s) de carga por preparar en las estanterías" },
                new Requisito { ok = ExpedicionActual.Confirmada, texto = ExpedicionActual.Confirmada ? "Preparación cerrada (confirmada)" : "Cerrar la preparación en la terminal de Revisión" }
            };
        }

        public void ConfirmarPreparacion()
        {
            Recalcular();
            if (!CargaHangar.MisionElegida) { Respuesta(false, "Antes elige la misión en la terminal de Misión."); return; }
            if (!Resumen.PuedeConfirmar) { Respuesta(false, "No se puede cerrar: " + Resumen.problemas[0]); return; }
            ExpedicionActual.Confirmar(Borrador);
            Respuesta(true, $"Preparación CERRADA: {Resumen.costeTotalMUSD:0.#} de {Resumen.presupuestoMUSD:0} M USD. Lo que cambies después exige reabrirla.");
            ActualizarVisual();
        }

        public void ReabrirPreparacion()
        {
            ExpedicionActual.Reabrir();
            Borrador = ExpedicionActual.Preparacion.Copia();     // se conservan todas las selecciones
            Recalcular();
            Respuesta(true, "Preparación REABIERTA con las mismas selecciones. El presupuesto se recalcula entero; nada se cobra dos veces.");
        }

        /// <summary>Preparar un lote en su estantería: confirma la carga presupuestada, no crea recursos.</summary>
        void PrepararLote(string id)
        {
            LoteCarga lo = Lotes.Find(x => x.id == id);
            if (lo == null) { Respuesta(false, "Esta carga no hace falta con la selección actual (cantidad 0)."); return; }
            if (lo.preparado) { Respuesta(false, lo.nombre + " ya está preparado en la zona de envío."); return; }
            CargaHangar.Preparar(lo);
            Lotes = CargaHangar.Lotes(Borrador, Resumen);
            ActualizarVisual();
            int faltan = CargaHangar.Pendientes(Lotes);
            Respuesta(true, $"{lo.nombre} preparado ({lo.detalle}) y llevado a la zona de envío." + (faltan > 0 ? $" Faltan {faltan} lote(s)." : " Carga completa."));
        }

        public void Embarcar()
        {
            foreach (Requisito r in Requisitos())
                if (!r.ok) { Respuesta(false, "No se puede embarcar: " + r.texto + "."); return; }
            ExpedicionActual.MarcarEmbarque();
            CargaHangar.Embarcado = true;
            string error;
            if (!CargadorEscenas.Cargar(CargadorEscenas.Expedicion, out error))
            {
                CargaHangar.Embarcado = false;
                ExpedicionActual.CancelarEmbarque();     // no dejar un embarque pendiente si la escena no se abrió
                Respuesta(false, error);
            }
        }

        // ------------------------------------------------------------------ Avisos y sonido

        public void Avisar(string texto, float segundos) { Aviso = texto; avisoHasta = Time.time + segundos; }

        public void Respuesta(bool ok, string texto)
        {
            Avisar(texto, 6f);
            if (audioFuente != null) audioFuente.PlayOneShot(ok ? sonidoOk : sonidoError);
        }

        static AudioClip Tono(string nombre, float hz, float s)
        {
            const int f = 22050;
            int n = Mathf.CeilToInt(f * s);
            float[] d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Sin(2f * Mathf.PI * hz * i / f) * Mathf.Min(1f, Mathf.Min(i, n - i) / (f * 0.01f)) * 0.6f;
            AudioClip c = AudioClip.Create(nombre, n, 1, f, false);
            c.SetData(d, 0);
            return c;
        }

        // ------------------------------------------------------------------ Control

        void BloquearCursor(bool bloquear)
        {
            CursorLibre = !bloquear;
            Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloquear;
            if (Personaje != null) Personaje.Control = bloquear && Abierta == null;
        }

        public void Abrir(PuntoHangar p)
        {
            Abierta = p;
            BloquearCursor(false);       // cursor libre y personaje quieto mientras se usa la terminal
        }

        public void Cerrar()
        {
            Abierta = null;
            BloquearCursor(true);
        }

        void Update()
        {
            if (!Listo) return;
            Keyboard k = Keyboard.current;
            Mouse raton = Mouse.current;
            if (holograma != null) holograma.Rotate(0f, 12f * Time.deltaTime, 0f, Space.World);
            if (k == null) return;

            if (Abierta != null)
            {
                if (k.escapeKey.wasPressedThisFrame) Cerrar();
                return;
            }
            if (k.escapeKey.wasPressedThisFrame)
            {
                BloquearCursor(CursorLibre);
                if (CursorLibre) Avisar("Cursor libre: el personaje está quieto. Clic o Esc para volver a mirar y caminar.", 4f);
            }
            if (CursorLibre && raton != null && raton.leftButton.wasPressedThisFrame) BloquearCursor(true);

            // Punto más cercano delante del personaje
            PuntoCercano = null;
            float mejor = 2.6f;
            Vector3 pos = Personaje.transform.position;
            Vector3 fw = Personaje.transform.forward;
            foreach (PuntoHangar p in Puntos)
            {
                Vector3 d = p.posicion - pos; d.y = 0f;
                float dist = d.magnitude;
                if (dist > mejor) continue;
                Vector3 haciaObjeto = p.objetivo - pos; haciaObjeto.y = 0f;
                if (haciaObjeto.sqrMagnitude > 0.01f && Vector3.Dot(fw, haciaObjeto.normalized) < 0.25f) continue;   // de espaldas al objeto
                mejor = dist;
                PuntoCercano = p;
            }
            if (CursorLibre || PuntoCercano == null || !k.eKey.wasPressedThisFrame) return;
            if (PuntoCercano.tipo == EstacionHangar.Estanteria) PrepararLote(PuntoCercano.lote);
            else Abrir(PuntoCercano);
        }

        // ------------------------------------------------------------------ Construcción (bloques provisionales)

        Material Mat(Color c, bool sinLuz)
        {
            // Si la escena tiene los materiales del MVP asignados se copian (así el ejecutable incluye sus sombreadores).
            Material baseM = sinLuz ? materialSinLuz : materialConLuz;
            Shader sh = Shader.Find(sinLuz ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find(sinLuz ? "Unlit/Color" : "Standard");
            Material m = baseM != null ? new Material(baseM) : new Material(sh);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        Material MatTransparente(Color c)
        {
            Material m = Mat(c, true);
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return m;
        }

        void Materiales()
        {
            mSuelo = Mat(new Color(0.42f, 0.44f, 0.46f), false);
            mPared = Mat(new Color(0.72f, 0.74f, 0.76f), false);
            mTecho = Mat(new Color(0.5f, 0.52f, 0.55f), false);
            mTerminal = Mat(new Color(0.18f, 0.22f, 0.28f), false);
            mPantalla = Mat(new Color(0.3f, 0.85f, 1f), true);
            mEstanteria = Mat(new Color(0.85f, 0.55f, 0.15f), false);
            mEnvio = Mat(new Color(0.95f, 0.85f, 0.2f), false);
            mEmbarque = Mat(new Color(0.25f, 0.75f, 0.4f), false);
            mSenal = Mat(new Color(0.1f, 0.12f, 0.16f), false);
            mHolo = MatTransparente(new Color(0.3f, 0.85f, 1f, 0.35f));
            mHoloMarcado = MatTransparente(new Color(1f, 0.8f, 0.2f, 0.55f));
            mLote["combustible"] = Mat(new Color(0.9f, 0.45f, 0.15f), false);
            mLote["suministros"] = Mat(new Color(0.25f, 0.5f, 0.85f), false);
            mLote["kits"] = Mat(new Color(1f, 0.6f, 0.1f), false);
            mLote["sonda"] = Mat(new Color(0.92f, 0.92f, 0.95f), false);
            mLote["componentes"] = Mat(new Color(0.55f, 0.6f, 0.65f), false);
        }

        Transform Cubo(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material m, bool colision)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nombre;
            if (!colision) { Collider c = g.GetComponent<Collider>(); c.enabled = false; Destroy(c); }
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        TextMesh Rotulo(string texto, Transform padre, Vector3 pos, float tam, Color color, bool mirarCamara = true)
        {
            GameObject g = new GameObject("Rótulo");
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            TextMesh tm = g.AddComponent<TextMesh>();
            tm.font = fuente;
            tm.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            tm.text = texto;
            tm.fontSize = 48;
            tm.characterSize = tam;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            if (mirarCamara) g.AddComponent<RotuloHaciaCamara>();
            return tm;
        }

        void Punto(EstacionHangar tipo, string nombre, Vector3 pos, Vector3 objetivo, string lote = null) =>
            Puntos.Add(new PuntoHangar { tipo = tipo, nombre = nombre, posicion = pos, objetivo = objetivo, lote = lote });

        /// <summary>Terminal: pedestal y pantalla mirando hacia «frente» (dirección hacia donde está el jugador).</summary>
        void Terminal(Transform raiz, string titulo, int numero, Vector3 pos, Vector3 frente, EstacionHangar tipo, Color color)
        {
            Transform t = new GameObject("Terminal · " + titulo).transform;
            t.SetParent(raiz, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.LookRotation(frente, Vector3.up);
            Cubo("Pedestal", t, new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 1.1f, 0.6f), mTerminal, true);
            Transform p = Cubo("Pantalla", t, new Vector3(0f, 1.45f, 0.05f), new Vector3(1.1f, 0.7f, 0.05f), mPantalla, false);
            p.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            Cubo("Marca en el suelo", t, new Vector3(0f, 0.01f, 1.2f), new Vector3(1.6f, 0.02f, 1.2f), Mat(color * 0.8f, true), false);
            Rotulo($"{numero} · {titulo.ToUpper()}\nE: usar", t, new Vector3(0f, 2.45f, 0f), 0.06f, color);
            Punto(tipo, titulo, t.TransformPoint(new Vector3(0f, 0f, 0.8f)), t.position);
        }

        void Construir()
        {
            Transform raiz = new GameObject("Hangar terrestre (bloques provisionales)").transform;
            const float ancho = 40f, fondo = 64f, alto = 12f;

            // Estructura
            Cubo("Suelo", raiz, new Vector3(0f, -0.25f, fondo / 2f), new Vector3(ancho, 0.5f, fondo), mSuelo, true);
            Cubo("Techo", raiz, new Vector3(0f, alto + 0.25f, fondo / 2f), new Vector3(ancho, 0.5f, fondo), mTecho, true);
            Cubo("Pared oeste", raiz, new Vector3(-ancho / 2f - 0.25f, alto / 2f, fondo / 2f), new Vector3(0.5f, alto, fondo), mPared, true);
            Cubo("Pared este", raiz, new Vector3(ancho / 2f + 0.25f, alto / 2f, fondo / 2f), new Vector3(0.5f, alto, fondo), mPared, true);
            Cubo("Pared de entrada", raiz, new Vector3(0f, alto / 2f, -0.25f), new Vector3(ancho, alto, 0.5f), mPared, true);
            // Pared del fondo con el hueco del embarque (4 × 4 m)
            Cubo("Pared del fondo (oeste)", raiz, new Vector3(-11f, alto / 2f, fondo + 0.25f), new Vector3(18f, alto, 0.5f), mPared, true);
            Cubo("Pared del fondo (este)", raiz, new Vector3(11f, alto / 2f, fondo + 0.25f), new Vector3(18f, alto, 0.5f), mPared, true);
            Cubo("Pared del fondo (dintel)", raiz, new Vector3(0f, (alto + 4f) / 2f, fondo + 0.25f), new Vector3(4f, alto - 4f, 0.5f), mPared, true);
            // Pasarela de embarque: corto pasillo cerrado que acaba en la escotilla de la lanzadera (no se ve la lanzadera)
            Cubo("Pasarela (suelo)", raiz, new Vector3(0f, -0.25f, fondo + 3f), new Vector3(4f, 0.5f, 6f), mSuelo, true);
            Cubo("Pasarela (oeste)", raiz, new Vector3(-2.25f, 2f, fondo + 3f), new Vector3(0.5f, 4f, 6f), mEmbarque, true);
            Cubo("Pasarela (este)", raiz, new Vector3(2.25f, 2f, fondo + 3f), new Vector3(0.5f, 4f, 6f), mEmbarque, true);
            Cubo("Pasarela (techo)", raiz, new Vector3(0f, 4.25f, fondo + 3f), new Vector3(5f, 0.5f, 6f), mEmbarque, true);
            Cubo("Escotilla de la lanzadera (cerrada)", raiz, new Vector3(0f, 1.6f, fondo + 6.1f), new Vector3(1.4f, 2f, 0.2f), mTerminal, true);
            Cubo("Valla de embarque", raiz, new Vector3(0f, 0.5f, fondo - 1.2f), new Vector3(5f, 1f, 0.1f), mEnvio, true);
            Cubo("Valla de embarque (oeste)", raiz, new Vector3(-2.5f, 0.5f, fondo - 0.6f), new Vector3(0.1f, 1f, 1.2f), mEnvio, true);
            Cubo("Valla de embarque (este)", raiz, new Vector3(2.5f, 0.5f, fondo - 0.6f), new Vector3(0.1f, 1f, 1.2f), mEnvio, true);

            // Luz: sin sombras para que el techo no deje el interior a oscuras
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.5f, 0.53f);
            Light sol = new GameObject("Luz del hangar").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 1.1f;
            sol.shadows = LightShadows.None;
            sol.transform.rotation = Quaternion.Euler(60f, -30f, 0f);

            // Señales de orientación
            Transform cartel = Cubo("Directorio", raiz, new Vector3(0f, 4.2f, 6f), new Vector3(7f, 2.4f, 0.1f), mSenal, false);
            Rotulo("HANGAR TERRESTRE · PREPARACIÓN DE LA EXPEDICIÓN\n← 1 MISIÓN      2 CONFIGURACIÓN ORBITAL →\n← 3 CARGA (estanterías)      4 REVISIÓN →\n5 EMBARQUE (al fondo) ↑",
                   raiz, new Vector3(0f, 4.2f, 5.9f), 0.06f, new Color(1f, 0.9f, 0.5f), false);   // se lee desde la entrada (mirando a +z)
            cartel.name = "Directorio (cartel)";

            // A · Terminal de misión
            Terminal(raiz, "Misión", 1, new Vector3(-7f, 0f, 11f), new Vector3(1f, 0f, -0.4f), EstacionHangar.Mision, new Color(0.5f, 0.8f, 1f));

            // B · Terminal de configuración orbital + holograma de la EM-4X
            Terminal(raiz, "Configuración orbital", 2, new Vector3(6f, 0f, 16f), new Vector3(-1f, 0f, -0.3f), EstacionHangar.Configuracion, new Color(0.45f, 0.9f, 1f));
            Cubo("Mesa del holograma", raiz, new Vector3(0f, 0.45f, 22f), new Vector3(5f, 0.9f, 5f), mTerminal, true);
            Holograma(raiz, new Vector3(0f, 2.9f, 22f));

            // C · Terminal de carga (manifiesto), estanterías y zona de envío
            Terminal(raiz, "Carga (manifiesto)", 3, new Vector3(-8f, 0f, 29f), new Vector3(1f, 0f, 0f), EstacionHangar.Manifiesto, new Color(1f, 0.7f, 0.3f));
            for (int i = 0; i < OrdenLotes.Length; i++) Estanteria(raiz, OrdenLotes[i], NombresLotes[i], 27f + i * 5f);
            Cubo("Zona de envío", raiz, new Vector3(-6f, 0.03f, 53f), new Vector3(8f, 0.06f, 6f), mEnvio, false);
            Rotulo("ZONA DE ENVÍO A ÓRBITA\n(lotes preparados)", raiz, new Vector3(-6f, 3f, 53f), 0.06f, new Color(1f, 0.9f, 0.3f));
            for (int i = 0; i < OrdenLotes.Length; i++)
            {
                Transform h = new GameObject("Hueco de envío · " + OrdenLotes[i]).transform;
                h.SetParent(raiz, false);
                h.localPosition = new Vector3(-9f + (i % 3) * 3f, 0.06f, 51.5f + (i / 3) * 3f);
                huecosEnvio[OrdenLotes[i]] = h;
            }

            // D · Revisión final
            Terminal(raiz, "Revisión final", 4, new Vector3(8f, 0f, 46f), new Vector3(-1f, 0f, 0f), EstacionHangar.Revision, new Color(0.6f, 1f, 0.6f));
            rotuloRevision = Rotulo("", raiz, new Vector3(8f, 3.4f, 46f), 0.045f, Color.white);

            // E · Embarque
            Rotulo("5 · EMBARQUE · LANZADERA L-4\nE: confirmar la salida", raiz, new Vector3(0f, 5f, fondo - 0.5f), 0.08f, new Color(0.5f, 1f, 0.6f));
            rotuloEmbarque = Rotulo("", raiz, new Vector3(0f, 3.4f, fondo - 1f), 0.05f, Color.white);
            Punto(EstacionHangar.Embarque, "Embarque · Lanzadera L-4", new Vector3(0f, 0f, fondo - 1.8f), new Vector3(0f, 0f, fondo - 1.2f));
        }

        void Estanteria(Transform raiz, string id, string nombre, float z)
        {
            Transform t = new GameObject("Estantería · " + nombre).transform;
            t.SetParent(raiz, false);
            t.localPosition = new Vector3(-17.5f, 0f, z);
            Cubo("Pata", t, new Vector3(0f, 1.5f, -1.9f), new Vector3(1.2f, 3f, 0.1f), mEstanteria, true);
            Cubo("Pata", t, new Vector3(0f, 1.5f, 1.9f), new Vector3(1.2f, 3f, 0.1f), mEstanteria, true);
            Cubo("Balda", t, new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.08f, 3.9f), mEstanteria, true);
            Cubo("Balda", t, new Vector3(0f, 1.6f, 0f), new Vector3(1.2f, 0.08f, 3.9f), mEstanteria, true);
            Transform cajas = new GameObject("Cajas").transform;
            cajas.SetParent(t, false);
            estanterias[id] = cajas;
            rotulosLote[id] = Rotulo(nombre.ToUpper(), t, new Vector3(1.4f, 3.5f, 0f), 0.05f, new Color(1f, 0.8f, 0.4f));
            Punto(EstacionHangar.Estanteria, "Estantería de " + nombre, new Vector3(-16.2f, 0f, z), new Vector3(-17.5f, 0f, z), id);
        }

        /// <summary>Holograma provisional de la EM-4X a escala 1:15 (cajas de los módulos; la nave real está en órbita).</summary>
        void Holograma(Transform raiz, Vector3 pos)
        {
            holograma = new GameObject("Holograma de la EM-4X (en órbita)").transform;
            holograma.SetParent(raiz, false);
            holograma.localPosition = pos;
            Transform escala = new GameObject("Escala 1:15").transform;
            escala.SetParent(holograma, false);
            escala.localScale = Vector3.one / 15f;
            escala.localPosition = new Vector3(0f, 0f, 11f / 15f);     // centra la nave (de z −41 a 18,7)
            DefinicionNaveModular d = DefinicionNaveModular.ExpedicionEM4X();
            foreach (ModuloNave m in d.modulos)
            {
                Bounds b = m.Limites;
                Transform c = Cubo("Módulo " + m.id, escala, b.center, b.size, mHolo, false);
                holoModulos[m.id] = c.GetComponent<Renderer>();
            }
            rotuloHolo = Rotulo("", raiz, pos + new Vector3(0f, 2.3f, 0f), 0.05f, new Color(0.6f, 0.95f, 1f));
        }

        // ------------------------------------------------------------------ Representación de la selección

        void ActualizarVisual()
        {
            if (Resumen == null || rotuloHolo == null) return;
            bool cerrada = ExpedicionActual.Confirmada;

            // Holograma: módulos afectados por lo encargado, en amarillo
            foreach (var kv in holoModulos)
            {
                bool marcado = (kv.Key == "PR" && (Borrador.motor > 0 || Borrador.tanque > 0)) || (kv.Key == "CI" && Borrador.instrumento > 0)
                               || (kv.Key == "CO" && Borrador.relevo > 0);
                kv.Value.sharedMaterial = marcado ? mHoloMarcado : mHolo;
            }
            rotuloHolo.text = "EM-4X · EN ÓRBITA (holograma 1:15)\n" +
                              $"{Resumen.motor.nombre} · {Resumen.tanque.nombre}\n" +
                              $"combustible {Resumen.combustibleKg / 1000f:0.0} de {Resumen.capacidadKg / 1000f:0} t · Δv ≈ {Resumen.deltaVMS:0} m/s\n" +
                              "amarillo = cambios encargados para la nave en órbita";

            // Estanterías y zona de envío
            foreach (string id in OrdenLotes)
            {
                Transform caja = estanterias[id], envio = huecosEnvio[id];
                for (int i = caja.childCount - 1; i >= 0; i--) Destroy(caja.GetChild(i).gameObject);
                for (int i = envio.childCount - 1; i >= 0; i--) Destroy(envio.GetChild(i).gameObject);
                LoteCarga lo = Lotes.Find(x => x.id == id);
                int idx = Array.IndexOf(OrdenLotes, id);
                if (lo == null)
                {
                    rotulosLote[id].text = NombresLotes[idx].ToUpper() + "\nno hace falta (0)";
                    rotulosLote[id].color = new Color(0.7f, 0.7f, 0.7f);
                    continue;
                }
                rotulosLote[id].text = NombresLotes[idx].ToUpper() + $"\n{lo.detalle}\n" + (lo.preparado ? "PREPARADO · en la zona de envío" : "POR PREPARAR · E: preparar lote");
                rotulosLote[id].color = lo.preparado ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.8f, 0.4f);
                int n = Mathf.Min(lo.cantidad, 10);
                Transform destino = lo.preparado ? envio : caja;
                for (int i = 0; i < n; i++)
                {
                    Vector3 s = id == "sonda" ? new Vector3(1f, 1f, 1.4f) : id == "combustible" ? new Vector3(0.5f, 0.6f, 0.5f) : new Vector3(0.55f, 0.5f, 0.55f);
                    // Apoyadas: en la balda de abajo (0,34) o de arriba (1,64); en la zona de envío, sobre el suelo.
                    Vector3 p = lo.preparado
                        ? new Vector3((i % 3 - 1) * 0.7f, s.y / 2f + (i / 9) * (s.y + 0.02f), ((i / 3) % 3 - 1) * 0.7f)
                        : new Vector3(0f, (i < 5 ? 0.34f : 1.64f) + s.y / 2f, id == "sonda" ? 0f : -1.6f + (i % 5) * 0.8f);
                    Cubo(NombresLotes[idx], destino, p, s, mLote[id], false);
                }
            }

            // Rótulos de revisión y embarque
            int pendientes = 0; string falta = "";
            foreach (Requisito r in Requisitos()) if (!r.ok) { pendientes++; if (falta.Length == 0) falta = r.texto; }
            rotuloRevision.text = cerrada ? "PREPARACIÓN CERRADA" : "PREPARACIÓN ABIERTA";
            rotuloRevision.color = cerrada ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.85f, 0.4f);
            rotuloEmbarque.text = pendientes == 0 ? "LISTO PARA EMBARCAR" : $"FALTA: {falta}" + (pendientes > 1 ? $" (+{pendientes - 1})" : "");
            rotuloEmbarque.color = pendientes == 0 ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.55f, 0.45f);
        }
    }
}
