using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    public enum EstacionHangar { Mision, Configuracion, Manifiesto, Estanteria, Revision, Embarque, Holograma, Combustible, Carro, Integracion }

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
    /// personaje, gestiona terminales, holograma, carro de carga, puesto de combustible, embarque y despegue.
    ///
    /// Datos: NO hay otra configuración. Las elecciones son la PreparacionExpedicion de ExpedicionActual (la misma que usa
    /// ExpedicionMVP), el presupuesto y los requisitos salen de BalanceEM4X.Calcular, y la carga se deriva de ellos
    /// (CargaHangar). Al embarcar se confirma esa preparación; tras la secuencia de despegue se abre ExpedicionMVP.
    /// La EM-4X sigue en órbita: aquí solo se ve como holograma.
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
        public bool EnDespegue => despegue != null && despegue.Activa;
        public HologramaInteractivo Holo { get; private set; }
        /// <summary>Zona B: la lanzadera L-4 en el edificio de integración (estado de vista propio, separado del holograma).</summary>
        public IntegracionLanzadera Integracion { get; private set; }
        public CarroCarga Carro { get; private set; }
        public bool AlimentacionVerificada { get; private set; }
        public string Aviso { get; private set; } = "";
        public bool AvisoVisible => Aviso.Length > 0 && Time.time < avisoHasta;
        public readonly List<PuntoHangar> Puntos = new List<PuntoHangar>();

        /// <summary>Lotes que se llevan con el carro (el combustible se autoriza en su puesto).</summary>
        public static readonly string[] LotesCarro = { "suministros", "kits", "sonda", "componentes" };
        static readonly string[] NombresCarro = { "Suministros", "Kits de reparación", "Sonda de despliegue", "Componentes de sustitución" };

        float avisoHasta;
        Font fuente;
        Material mSuelo, mPared, mTecho, mTerminal, mPantalla, mEstanteria, mEnvio, mEmbarque, mSenal, mVerde, mRojo;
        readonly Dictionary<string, Material> mLote = new Dictionary<string, Material>();
        readonly Dictionary<string, Transform> estanterias = new Dictionary<string, Transform>();
        readonly Dictionary<string, Transform> huecosEnvio = new Dictionary<string, Transform>();
        readonly Dictionary<string, Transform> contenedores = new Dictionary<string, Transform>();
        readonly Dictionary<string, TextMesh> rotulosContenedor = new Dictionary<string, TextMesh>();
        readonly Dictionary<string, TextMesh> rotulosLote = new Dictionary<string, TextMesh>();
        readonly PuntoHangar puntoCarro = new PuntoHangar { tipo = EstacionHangar.Carro, nombre = "Carro de carga" };
        Renderer indicadorCombustible;
        TextMesh rotuloCombustible, rotuloRevision, rotuloEmbarque, rotuloLanzadera;
        SecuenciaDespegue despegue;
        AudioSource audioFuente;
        AudioClip sonidoOk, sonidoError;
        static readonly Rect ZonaEnvio = new Rect(-10f, 50f, 8f, 6f);    // planta (x, z)
        /// <summary>Muelle de la bodega de la lanzadera, dentro del edificio de integración (planta x, z).</summary>
        static readonly Rect ZonaMuelle = new Rect(22.5f, 29f, 6f, 6f);
        readonly Dictionary<string, Transform> huecosMuelle = new Dictionary<string, Transform>();
        readonly Dictionary<Color, Material> cacheMat = new Dictionary<Color, Material>();
        string zonaAvisada;

        void Awake()
        {
            Catalogo = catalogoMision != null ? catalogoMision : ScriptableObject.CreateInstance<CatalogoMision>();
            gameObject.AddComponent<HudHangarTerrestre>();
        }

        void Start()
        {
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Materiales();

            // Vuelta de una expedición: nueva preparación con las mismas selecciones, carga por preparar otra vez.
            if (CargaHangar.Embarcado)
            {
                CargaHangar.Embarcado = false;
                CargaHangar.Vaciar();
                ExpedicionActual.Reabrir();
                Avisar("De vuelta en tierra. Las selecciones se conservan; la carga de la próxima expedición está por preparar.", 8f);
            }
            else Avisar("Hangar terrestre. Sigue los rótulos: 1 Misión · 2 Configuración y holograma · 3 Carga y combustible · 4 Revisión · 5 Embarque.", 8f);

            Borrador = ExpedicionActual.Preparacion.Copia();
            Resumen = BalanceEM4X.Calcular(Borrador, Catalogo);
            Lotes = CargaHangar.Lotes(Borrador, Resumen);
            Construir();
            despegue = gameObject.AddComponent<SecuenciaDespegue>();

            Personaje = new GameObject().AddComponent<CaminanteHangar>();
            Personaje.Iniciar(new Vector3(0f, 0.05f, 3f), 0f);
            BloquearCursor(true);

            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            audioFuente.volume = 0.35f;
            sonidoOk = Tono("ok", 880f, 0.12f);
            sonidoError = Tono("error", 180f, 0.22f);
            Recalcular();
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
            List<OpcionTanqueNave> tanques = BalanceEM4X.Tanques(Catalogo.catalogoNave);
            Borrador.motor = Mathf.Clamp(Borrador.motor, 0, BalanceEM4X.Motores(Catalogo.catalogoNave).Count - 1);
            Borrador.tanque = Mathf.Clamp(Borrador.tanque, 0, tanques.Count - 1);
            Borrador.instrumento = Mathf.Clamp(Borrador.instrumento, 0, BalanceEM4X.Instrumentos(Catalogo).Count - 1);
            Borrador.tramosCombustible = Mathf.Clamp(Borrador.tramosCombustible, 0, BalanceEM4X.MaximoTramos(tanques[Borrador.tanque]));
            Borrador.suministrosExtra = Mathf.Clamp(Borrador.suministrosExtra, 0, BalanceEM4X.MaximoSuministrosExtra);
            Borrador.kitsExtra = Mathf.Clamp(Borrador.kitsExtra, 0, BalanceEM4X.MaximoKitsExtra);
            if (Borrador.lanzadera == null) Borrador.lanzadera = new ConfiguracionLanzadera();
            DatosLanzadera.Limitar(Borrador.lanzadera);
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

        /// <summary>Encarga la alternativa previsualizada en el holograma (mismo cambio que en la terminal 2).</summary>
        public void EncargarPieza(PiezaHolo pieza, int indice)
        {
            bool hecho = Cambiar(p =>
            {
                if (pieza == PiezaHolo.Motor) p.motor = indice;
                else if (pieza == PiezaHolo.Tanque) p.tanque = indice;
                else p.instrumento = indice;
            });
            if (hecho) Respuesta(true, "Cambio encargado para la EM-4X en órbita. El presupuesto previsto se ha recalculado entero.");
        }

        /// <summary>Confirma una pieza de la LANZADERA L-4 (solo toca Borrador.lanzadera; la EM-4X no cambia).</summary>
        public void EncargarLanzadera(PiezaLanzadera pieza, int indice)
        {
            bool hecho = Cambiar(p => DatosLanzadera.Fijar(p.lanzadera, pieza, indice));
            if (hecho) Respuesta(true, $"Lanzadera L-4: {DatosLanzadera.Opciones(pieza)[indice].nombre} confirmado en la integración. " +
                                       "Presupuesto recalculado; revisa dónde se entrega ahora cada lote.");
        }

        public bool ConfigValida => Resumen != null && Resumen.problemas.Count - (PresupuestoSuperado ? 1 : 0) == 0;
        public bool PresupuestoSuperado => Resumen != null && Resumen.costeTotalMUSD > Resumen.presupuestoMUSD + 0.001f;

        public struct Requisito { public string texto; public bool ok; }

        /// <summary>Requisitos obligatorios para embarcar (validadores existentes + carga preparada).</summary>
        public List<Requisito> Requisitos()
        {
            List<string> pendientes = new List<string>();
            foreach (LoteCarga lo in Lotes) if (!lo.preparado) pendientes.Add(lo.nombre.ToLower());
            return new List<Requisito>
            {
                new Requisito { ok = CargaHangar.MisionElegida, texto = CargaHangar.MisionElegida ? "Misión seleccionada: " + Resumen.mision.nombre : "Elegir la misión en la terminal de Misión" },
                new Requisito { ok = ConfigValida, texto = ConfigValida ? "Configuración válida" : "Configuración no válida (ver problemas)" },
                new Requisito { ok = !PresupuestoSuperado, texto = $"Presupuesto: {Resumen.costeTotalMUSD:0.#} de {Resumen.presupuestoMUSD:0} M USD" + (PresupuestoSuperado ? " · SUPERADO" : "") },
                new Requisito { ok = pendientes.Count == 0, texto = pendientes.Count == 0 ? $"Carga preparada ({Lotes.Count} lotes)" : "Falta preparar: " + string.Join(", ", pendientes) },
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

        // ------------------------------------------------------------------ Combustible (comprobación corta con consecuencia)

        public LoteCarga LoteCombustible => Lotes.Find(x => x.id == "combustible");

        public void VerificarAlimentacion()
        {
            AlimentacionVerificada = true;
            ActualizarVisual();
            Respuesta(true, "Alimentación del puesto de combustible verificada: bomba y línea a la zona de envío en verde.");
        }

        /// <summary>Autoriza el llenado del combustible YA presupuestado (no añade combustible ni coste).</summary>
        public void AutorizarLlenado()
        {
            LoteCarga lo = LoteCombustible;
            if (lo == null) { Respuesta(false, "No se envía combustible desde tierra con la selección actual."); return; }
            if (lo.preparado) { Respuesta(false, "El llenado ya está autorizado."); return; }
            if (!AlimentacionVerificada) { Respuesta(false, "Primero verifica la alimentación del puesto."); return; }
            CargaHangar.Preparar(lo);
            Lotes = CargaHangar.Lotes(Borrador, Resumen);
            ActualizarVisual();
            Respuesta(true, $"Llenado autorizado: {lo.detalle.Replace("\n", ", ")}" + (lo.enLanzadera ? " (tanque de transferencia de la lanzadera)." : " (depósito del envío orbital)."));
        }

        // ------------------------------------------------------------------ Carro de carga

        void CargarEnCarro(string id)
        {
            LoteCarga lo = Lotes.Find(x => x.id == id);
            if (lo == null) { Respuesta(false, "Esta carga no hace falta con la selección actual (cantidad 0)."); return; }
            if (lo.preparado) { Respuesta(false, lo.nombre + " ya está entregado en " + Destino(lo) + "."); return; }
            if (Carro.Lote != null) { Respuesta(false, "El carro ya lleva " + NombreLote(Carro.Lote) + ": entrégalo antes."); return; }
            Carro.Cargar(lo.id, lo.firma);
            ActualizarVisual();
            Respuesta(true, $"Contenedor de {lo.nombre.ToLower()} ({lo.detalle.Replace("\n", ", ")}) fijado al carro. Llévalo a {Destino(lo)}.");
        }

        /// <summary>Dónde se entrega un lote: muelle de la lanzadera (zona B) o zona de envío orbital (zona C).</summary>
        public static string Destino(LoteCarga lo) => lo.enLanzadera ? "el MUELLE DE LA LANZADERA (zona B, edificio de integración)" : "la ZONA DE ENVÍO ORBITAL (amarilla, zona C)";

        static string NombreLote(string id)
        {
            int i = Array.IndexOf(LotesCarro, id);
            return i >= 0 ? NombresCarro[i].ToLower() : id;
        }

        void Entregar(bool enMuelle)
        {
            LoteCarga lo = Lotes.Find(x => x.id == Carro.Lote);
            if (lo == null || lo.firma != Carro.Firma) { Carro.Vaciar(); ActualizarVisual(); return; }
            string zona = enMuelle ? "muelle" : "envio";
            if (lo.enLanzadera != enMuelle)
            {
                if (zonaAvisada != zona) Respuesta(false, $"{lo.nombre} no va aquí: con la configuración actual viaja {(lo.enLanzadera ? "en la lanzadera" : "en el envío orbital")}. Llévalo a {Destino(lo)}.");
                zonaAvisada = zona;
                return;
            }
            CargaHangar.Preparar(lo);
            Carro.Vaciar();
            Lotes = CargaHangar.Lotes(Borrador, Resumen);
            ActualizarVisual();
            int faltan = CargaHangar.Pendientes(Lotes);
            Respuesta(true, $"{lo.nombre} entregado en {(enMuelle ? "el muelle de la lanzadera" : "la zona de envío orbital")}: LISTO." + (faltan > 0 ? $" Faltan {faltan} lote(s)." : " Carga completa."));
        }

        public void RecuperarCarro()
        {
            Carro.Recuperar();
            Personaje.FactorVelocidad = 1f;
            ActualizarVisual();
            Respuesta(true, "Carro devuelto a su base, vacío. Lo que llevaba ha vuelto a su estantería.");
        }

        public string EstadoCarro()
        {
            if (Carro == null) return "";
            string carga = Carro.Lote != null ? "lleva " + NombreLote(Carro.Lote) : "vacío";
            return $"Carro: {carga}{(Carro.Empujando ? " · empujando" : "")}{(Carro.Bloqueado ? " · BLOQUEADO (retrocede, gira o pulsa R)" : "")}";
        }

        // ------------------------------------------------------------------ Embarque y despegue

        public void Embarcar()
        {
            foreach (Requisito r in Requisitos())
                if (!r.ok) { Respuesta(false, "No se puede embarcar: " + r.texto + "."); return; }
            Abierta = null;
            Carro.Soltar();
            Personaje.Control = false;
            Personaje.Camara.enabled = false;
            ResumenExpedicion re = Resumen;
            List<string> comprobacion = new List<string>
            {
                "Preparación cerrada: " + re.mision.nombre,
                $"Presupuesto: {re.costeTotalMUSD:0.#} de {re.presupuestoMUSD:0} M USD",
                $"Carga preparada: {Lotes.Count} lote(s)" + (re.lanzadera != null && re.lanzadera.enLanzadera.Count > 0 ? " · en la L-4: " + string.Join(", ", re.lanzadera.enLanzadera) : ""),
                re.lanzadera != null ? $"Lanzadera L-4: {DatosLanzadera.Resumen(re.lanzadera.eleccion)} · {re.lanzadera.masaExtraKg:0} de {re.lanzadera.capacidadKg:0} kg extra" : "Lanzadera L-4",
                $"EM-4X (en órbita): {re.motor.nombre} · {re.tanque.nombre}",
                $"{re.instrumento.nombre} · combustible {re.combustibleKg / 1000f:0.0} t · Δv ≈ {re.deltaVMS:0} m/s",
                "Tripulación a bordo de la lanzadera L-4 · compuerta cerrada"
            };
            despegue.Iniciar(Personaje.Ojos, comprobacion, ExpedicionActual.Preparacion.lanzadera, MaterialCache, AbrirExpedicion);
        }

        void AbrirExpedicion()
        {
            ExpedicionActual.MarcarEmbarque(true);
            CargaHangar.Embarcado = true;
            string error;
            if (!CargadorEscenas.Cargar(CargadorEscenas.Expedicion, out error))
            {
                CargaHangar.Embarcado = false;
                ExpedicionActual.CancelarEmbarque();     // no dejar un embarque pendiente si la escena no se abrió
                despegue.Detener();
                Personaje.Camara.enabled = true;
                BloquearCursor(true);
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
            if (p.tipo == EstacionHangar.Holograma) Holo.Entrar(Personaje.Camara);
            if (p.tipo == EstacionHangar.Integracion) Integracion.Entrar(Personaje.Camara);
        }

        public void Cerrar()
        {
            if (Abierta != null && Abierta.tipo == EstacionHangar.Holograma) Holo.Salir(Personaje.Camara);
            if (Abierta != null && Abierta.tipo == EstacionHangar.Integracion) Integracion.Salir(Personaje.Camara);
            Abierta = null;
            BloquearCursor(true);
        }

        void Update()
        {
            if (!Listo || EnDespegue) return;
            Keyboard k = Keyboard.current;
            Mouse raton = Mouse.current;
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
            if (!CursorLibre && k.rKey.wasPressedThisFrame) RecuperarCarro();

            // Entrega automática al llegar a la zona de envío con un contenedor
            Vector2 pc = new Vector2(Carro.transform.position.x, Carro.transform.position.z);
            bool enEnvio = ZonaEnvio.Contains(pc), enMuelle = ZonaMuelle.Contains(pc);
            if (!enEnvio && !enMuelle) zonaAvisada = null;
            if (Carro.Lote != null && (enEnvio || enMuelle)) Entregar(enMuelle);

            // Punto más cercano delante del personaje (el carro cuenta cuando no se está empujando)
            PuntoCercano = null;
            float mejor = 2.6f;
            Vector3 pos = Personaje.transform.position;
            Vector3 fw = Personaje.transform.forward;
            puntoCarro.posicion = Carro.transform.position - Carro.transform.forward * (CarroCarga.Largo / 2f + 0.3f);
            puntoCarro.objetivo = Carro.transform.position;
            foreach (PuntoHangar p in Puntos)
                if (Mas(p, pos, fw, ref mejor)) PuntoCercano = p;
            if (!Carro.Empujando && Mas(puntoCarro, pos, fw, ref mejor)) PuntoCercano = puntoCarro;
            if (CursorLibre || !k.eKey.wasPressedThisFrame) return;

            if (Carro.Empujando)
            {
                if (PuntoCercano != null && PuntoCercano.tipo == EstacionHangar.Estanteria) CargarEnCarro(PuntoCercano.lote);
                else { Carro.Soltar(); Personaje.FactorVelocidad = 1f; Avisar("Carro soltado. E junto al asa para volver a empujarlo.", 3f); }
                return;
            }
            if (PuntoCercano == null) return;
            switch (PuntoCercano.tipo)
            {
                case EstacionHangar.Carro:
                    Carro.Empujar(Personaje.transform);
                    Personaje.FactorVelocidad = 0.7f;
                    Avisar("Empujando el carro. Acércalo a una estantería y pulsa E para cargar su contenedor; E en otro sitio lo suelta.", 6f);
                    break;
                case EstacionHangar.Estanteria:
                {
                    LoteCarga lo = Lotes.Find(x => x.id == PuntoCercano.lote);
                    Respuesta(false, lo == null ? "Esta carga no hace falta con la selección actual."
                                   : lo.preparado ? lo.nombre + " ya está entregado en " + Destino(lo) + "."
                                   : "Trae el carro (junto a las estanterías, E en su asa para empujarlo) y pulsa E aquí para cargar el contenedor.");
                    break;
                }
                default:
                    Abrir(PuntoCercano);
                    break;
            }
        }

        static bool Mas(PuntoHangar p, Vector3 pos, Vector3 fw, ref float mejor)
        {
            Vector3 d = p.posicion - pos; d.y = 0f;
            float dist = d.magnitude;
            if (dist > mejor) return false;
            Vector3 haciaObjeto = p.objetivo - pos; haciaObjeto.y = 0f;
            if (haciaObjeto.sqrMagnitude > 0.01f && Vector3.Dot(fw, haciaObjeto.normalized) < 0.25f) return false;   // de espaldas al objeto
            mejor = dist;
            return true;
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

        /// <summary>Materiales sólidos reutilizados por color (la lanzadera se vuelve a montar a menudo).</summary>
        public Material MaterialCache(Color c, bool sinLuz)
        {
            Color clave = new Color(c.r, c.g, c.b, sinLuz ? 0.5f : 1f);
            Material m;
            if (!cacheMat.TryGetValue(clave, out m) || m == null) cacheMat[clave] = m = Mat(c, sinLuz);
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
            mVerde = Mat(new Color(0.3f, 0.95f, 0.45f), true);
            mRojo = Mat(new Color(1f, 0.3f, 0.25f), true);
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
        Transform Terminal(Transform raiz, string titulo, string numero, Vector3 pos, Vector3 frente, EstacionHangar tipo, Color color)
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
            return t;
        }

        void Construir()
        {
            Transform raiz = new GameObject("Hangar terrestre (bloques provisionales)").transform;
            const float ancho = 40f, fondo = 64f, alto = 12f;

            // Estructura
            Cubo("Suelo", raiz, new Vector3(0f, -0.25f, fondo / 2f), new Vector3(ancho, 0.5f, fondo), mSuelo, true);
            Cubo("Techo", raiz, new Vector3(0f, alto + 0.25f, fondo / 2f), new Vector3(ancho, 0.5f, fondo), mTecho, true);
            Cubo("Pared oeste", raiz, new Vector3(-ancho / 2f - 0.25f, alto / 2f, fondo / 2f), new Vector3(0.5f, alto, fondo), mPared, true);
            // Pared este con el paso al edificio de integración de la lanzadera (zona B): hueco de z 14 a 24 y 9 m de alto
            Cubo("Pared este (sur)", raiz, new Vector3(ancho / 2f + 0.25f, alto / 2f, 7f), new Vector3(0.5f, alto, 14f), mPared, true);
            Cubo("Pared este (norte)", raiz, new Vector3(ancho / 2f + 0.25f, alto / 2f, 44f), new Vector3(0.5f, alto, 40f), mPared, true);
            Cubo("Pared este (dintel del paso)", raiz, new Vector3(ancho / 2f + 0.25f, 10.5f, 19f), new Vector3(0.5f, 3f, 10f), mPared, true);
            Cubo("Pared de entrada", raiz, new Vector3(0f, alto / 2f, -0.25f), new Vector3(ancho, alto, 0.5f), mPared, true);
            Cubo("Pared del fondo (oeste)", raiz, new Vector3(-11f, alto / 2f, fondo + 0.25f), new Vector3(18f, alto, 0.5f), mPared, true);
            Cubo("Pared del fondo (este)", raiz, new Vector3(11f, alto / 2f, fondo + 0.25f), new Vector3(18f, alto, 0.5f), mPared, true);
            Cubo("Pared del fondo (dintel)", raiz, new Vector3(0f, (alto + 4f) / 2f, fondo + 0.25f), new Vector3(4f, alto - 4f, 0.5f), mPared, true);
            Cubo("Pasarela (suelo)", raiz, new Vector3(0f, -0.25f, fondo + 3f), new Vector3(4f, 0.5f, 6f), mSuelo, true);
            Cubo("Pasarela (oeste)", raiz, new Vector3(-2.25f, 2f, fondo + 3f), new Vector3(0.5f, 4f, 6f), mEmbarque, true);
            Cubo("Pasarela (este)", raiz, new Vector3(2.25f, 2f, fondo + 3f), new Vector3(0.5f, 4f, 6f), mEmbarque, true);
            Cubo("Pasarela (techo)", raiz, new Vector3(0f, 4.25f, fondo + 3f), new Vector3(5f, 0.5f, 6f), mEmbarque, true);
            Cubo("Escotilla de la lanzadera (cerrada)", raiz, new Vector3(0f, 1.6f, fondo + 6.1f), new Vector3(1.4f, 2f, 0.2f), mTerminal, true);
            Cubo("Valla de embarque", raiz, new Vector3(0f, 0.5f, fondo - 1.2f), new Vector3(5f, 1f, 0.1f), mEnvio, true);
            Cubo("Valla de embarque (oeste)", raiz, new Vector3(-2.5f, 0.5f, fondo - 0.6f), new Vector3(0.1f, 1f, 1.2f), mEnvio, true);
            Cubo("Valla de embarque (este)", raiz, new Vector3(2.5f, 0.5f, fondo - 0.6f), new Vector3(0.1f, 1f, 1.2f), mEnvio, true);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.5f, 0.53f);
            Light sol = new GameObject("Luz del hangar").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 1.1f;
            sol.shadows = LightShadows.None;
            sol.transform.rotation = Quaternion.Euler(60f, -30f, 0f);

            // Señales de orientación
            Cubo("Directorio (cartel)", raiz, new Vector3(0f, 4.2f, 6f), new Vector3(8f, 2.4f, 0.1f), mSenal, false);
            Rotulo("HANGAR TERRESTRE · PREPARACIÓN DE LA EXPEDICIÓN\nA · NAVE ORBITAL EM-4X: 1 Misión (←) · 2 Configuración y holograma (centro)\nB · LANZADERA L-4: edificio de integración (→, paso en la pared derecha)\nC · CARGA: carro y estanterías (←) · combustible y 4 Revisión (→)\nD · EMBARQUE: 5, al fondo ↑",
                   raiz, new Vector3(0f, 4.2f, 5.9f), 0.06f, new Color(1f, 0.9f, 0.5f), false);   // se lee desde la entrada

            // A · Misión
            Terminal(raiz, "Misión", "1", new Vector3(-7f, 0f, 11f), new Vector3(1f, 0f, -0.4f), EstacionHangar.Mision, new Color(0.5f, 0.8f, 1f));

            // B · Configuración orbital + holograma interactivo (centro visual)
            Terminal(raiz, "Configuración orbital", "2", new Vector3(6f, 0f, 16f), new Vector3(-1f, 0f, -0.3f), EstacionHangar.Configuracion, new Color(0.45f, 0.9f, 1f));
            Cubo("Mesa del holograma", raiz, new Vector3(0f, 0.45f, 22f), new Vector3(5f, 0.9f, 5f), mTerminal, true);
            Holo = gameObject.AddComponent<HologramaInteractivo>();
            Holo.Construir(this, raiz, new Vector3(0f, 2.9f, 22f), MatTransparente(new Color(0.3f, 0.85f, 1f, 0.35f)),
                           MatTransparente(new Color(1f, 0.8f, 0.2f, 0.55f)), MatTransparente(new Color(0.6f, 1f, 1f, 0.8f)),
                           MatTransparente(new Color(1f, 0.95f, 0.4f, 0.9f)));
            Rotulo("A · NAVE ORBITAL EM-4X (en órbita)\n2 · HOLOGRAMA · E: consultar y comparar piezas", raiz, new Vector3(0f, 1.6f, 19.2f), 0.045f, new Color(0.6f, 0.95f, 1f));
            Punto(EstacionHangar.Holograma, "Holograma de la EM-4X (comparar y encargar piezas)", new Vector3(0f, 0f, 18.7f), new Vector3(0f, 0f, 22f));

            // C · Carga: manifiesto, carro, estanterías y zona de envío
            Terminal(raiz, "Carga (manifiesto)", "3", new Vector3(-8f, 0f, 29f), new Vector3(1f, 0f, 0f), EstacionHangar.Manifiesto, new Color(1f, 0.7f, 0.3f));
            for (int i = 0; i < LotesCarro.Length; i++) Estanteria(raiz, LotesCarro[i], NombresCarro[i], 28f + i * 5.5f);
            Cubo("Zona de envío", raiz, new Vector3(ZonaEnvio.center.x, 0.03f, ZonaEnvio.center.y), new Vector3(ZonaEnvio.width, 0.06f, ZonaEnvio.height), mEnvio, false);
            Rotulo("C · ZONA DE ENVÍO ORBITAL (sin tripulación)\ntrae aquí el carro con los lotes que no viajan en la lanzadera", raiz, new Vector3(ZonaEnvio.center.x, 3f, ZonaEnvio.center.y), 0.06f, new Color(1f, 0.9f, 0.3f));
            for (int i = 0; i < LotesCarro.Length; i++)
            {
                Transform h = new GameObject("Hueco de envío · " + LotesCarro[i]).transform;
                h.SetParent(raiz, false);
                h.localPosition = new Vector3(-8.5f + (i % 2) * 5f, 0.06f, 51.5f + (i / 2) * 3f);
                huecosEnvio[LotesCarro[i]] = h;
            }
            Carro = new GameObject().AddComponent<CarroCarga>();
            Carro.Construir(new Vector3(-12f, 0f, 24f), mEstanteria, mTerminal);
            Carro.LimiteMin = new Vector2(-19.2f, 1f);
            Carro.LimiteMax = new Vector2(52f, fondo - 2f);     // incluye el edificio de integración (las paredes limitan el resto)

            // B · Edificio de integración de la lanzadera L-4
            ConstruirIntegracion(raiz);
            Rotulo("CARRO DE CARGA · base\nR: recuperar el carro", raiz, new Vector3(-12f, 2.2f, 24f), 0.045f, new Color(1f, 0.8f, 0.4f));

            // Puesto de combustible (comprobación corta)
            Transform pc = Terminal(raiz, "Combustible", "3b", new Vector3(14f, 0f, 30f), new Vector3(-1f, 0f, 0f), EstacionHangar.Combustible, new Color(1f, 0.55f, 0.25f));
            Cubo("Depósito de combustible", raiz, new Vector3(17.5f, 2f, 30f), new Vector3(2.5f, 4f, 2.5f), mEstanteria, true);
            indicadorCombustible = Cubo("Indicador del llenado", raiz, new Vector3(16.2f, 3.5f, 30f), new Vector3(0.1f, 0.6f, 0.6f), mRojo, false).GetComponent<Renderer>();
            rotuloCombustible = Rotulo("", raiz, new Vector3(14f, 3.4f, 30f), 0.045f, Color.white);
            pc.name = "Terminal · Combustible";

            // D · Revisión final
            Terminal(raiz, "Revisión final", "4", new Vector3(8f, 0f, 46f), new Vector3(-1f, 0f, 0f), EstacionHangar.Revision, new Color(0.6f, 1f, 0.6f));
            rotuloRevision = Rotulo("", raiz, new Vector3(8f, 3.4f, 46f), 0.045f, Color.white);

            // E · Embarque
            Rotulo("D · EMBARQUE · 5 · LANZADERA L-4\nE: confirmar la salida", raiz, new Vector3(0f, 5f, fondo - 0.5f), 0.08f, new Color(0.5f, 1f, 0.6f));
            rotuloEmbarque = Rotulo("", raiz, new Vector3(0f, 3.4f, fondo - 1f), 0.05f, Color.white);
            Punto(EstacionHangar.Embarque, "Embarque · Lanzadera L-4", new Vector3(0f, 0f, fondo - 1.8f), new Vector3(0f, 0f, fondo - 1.2f));
        }

        /// <summary>
        /// ZONA B · Edificio de integración (bloques provisionales): nave alta junto a la pared este del hangar, con la
        /// lanzadera L-4 en vertical sobre su plataforma, dos torres de servicio, plataformas de trabajo, pasarelas, puente
        /// grúa, focos de trabajo y el muelle de la bodega. Referencia de AMBIENTE (no copia ningún vehículo ni edificio real).
        /// Medidas: interior x 20,5–52,5 · z 2–38 · 46 m de alto. Lanzadera en (36,5; 20), base sobre la plataforma (2 m).
        /// </summary>
        void ConstruirIntegracion(Transform raizHangar)
        {
            Transform raiz = new GameObject("Edificio de integración de la lanzadera (zona B, provisional)").transform;
            raiz.SetParent(raizHangar, false);
            const float x0 = 20.5f, x1 = 52.5f, z0 = 2f, z1 = 38f, alto = 46f;
            float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
            Vector3 vehiculo = new Vector3(36.5f, 2f, 20f);
            Material pared = Mat(new Color(0.42f, 0.44f, 0.47f), false), viga = Mat(new Color(0.3f, 0.32f, 0.35f), false),
                     amarillo = Mat(new Color(0.95f, 0.75f, 0.15f), false), rojo = Mat(new Color(0.75f, 0.22f, 0.15f), false),
                     foco = Mat(new Color(1f, 0.92f, 0.7f), true), rejilla = Mat(new Color(0.5f, 0.52f, 0.5f), false);

            // Envolvente (los nombres «Edificio…» los usa la cámara de la integración para no atravesar paredes)
            Cubo("Edificio · suelo", raiz, new Vector3(cx, -0.25f, cz), new Vector3(x1 - x0, 0.5f, z1 - z0), mSuelo, true);
            Cubo("Umbral del paso", raiz, new Vector3(20.25f, -0.25f, 19f), new Vector3(0.7f, 0.5f, 10f), mSuelo, true);
            Cubo("Edificio · pared oeste (sobre el hangar)", raiz, new Vector3(20.25f, 12f + (alto - 12f) / 2f, cz), new Vector3(0.5f, alto - 12f, z1 - z0), pared, true);
            Cubo("Edificio · pared sur", raiz, new Vector3(cx, alto / 2f, z0 - 0.25f), new Vector3(x1 - x0 + 1f, alto, 0.5f), pared, true);
            Cubo("Edificio · pared norte", raiz, new Vector3(cx, alto / 2f, z1 + 0.25f), new Vector3(x1 - x0 + 1f, alto, 0.5f), pared, true);
            Cubo("Edificio · pared este", raiz, new Vector3(x1 + 0.25f, alto / 2f, cz), new Vector3(0.5f, alto, z1 - z0), pared, true);
            Cubo("Edificio · techo", raiz, new Vector3(cx, alto + 0.25f, cz), new Vector3(x1 - x0 + 1f, 0.5f, z1 - z0 + 1f), mTecho, true);
            // Costillas y vigas de las paredes (sensación industrial y de escala)
            for (float x = x0 + 2f; x < x1; x += 6f)
            {
                Cubo("Pilar", raiz, new Vector3(x, alto / 2f, z0 + 0.4f), new Vector3(0.6f, alto, 0.6f), viga, false);
                Cubo("Pilar", raiz, new Vector3(x, alto / 2f, z1 - 0.4f), new Vector3(0.6f, alto, 0.6f), viga, false);
            }
            for (float y = 12f; y < alto; y += 12f)
            {
                Cubo("Viga", raiz, new Vector3(cx, y, z0 + 0.5f), new Vector3(x1 - x0, 0.8f, 0.5f), viga, false);
                Cubo("Viga", raiz, new Vector3(cx, y, z1 - 0.5f), new Vector3(x1 - x0, 0.8f, 0.5f), viga, false);
                Cubo("Viga", raiz, new Vector3(x1 - 0.5f, y, cz), new Vector3(0.5f, 0.8f, z1 - z0), viga, false);
            }
            Cubo("Riel de la grúa (sur)", raiz, new Vector3(cx, 42.6f, 3.2f), new Vector3(x1 - x0, 0.6f, 0.6f), amarillo, false);
            Cubo("Riel de la grúa (norte)", raiz, new Vector3(cx, 42.6f, 36.8f), new Vector3(x1 - x0, 0.6f, 0.6f), amarillo, false);

            // Plataforma móvil (se ve la base; no se puede subir) y franjas de seguridad
            Cubo("Plataforma de la lanzadera", raiz, new Vector3(vehiculo.x, 1f, vehiculo.z), new Vector3(12f, 2f, 12f), viga, true);
            Cubo("Franja de seguridad (oeste)", raiz, new Vector3(vehiculo.x - 7f, 0.02f, vehiculo.z), new Vector3(0.4f, 0.04f, 14f), amarillo, false);
            Cubo("Franja de seguridad (este)", raiz, new Vector3(vehiculo.x + 7f, 0.02f, vehiculo.z), new Vector3(0.4f, 0.04f, 14f), amarillo, false);
            Cubo("Franja de seguridad (sur)", raiz, new Vector3(vehiculo.x, 0.02f, vehiculo.z - 7f), new Vector3(14f, 0.04f, 0.4f), amarillo, false);
            Cubo("Franja de seguridad (norte)", raiz, new Vector3(vehiculo.x, 0.02f, vehiculo.z + 7f), new Vector3(14f, 0.04f, 0.4f), amarillo, false);

            // Dos torres de servicio (sur y norte del vehículo) con plataformas de trabajo hacia él
            foreach (float zt in new[] { vehiculo.z - 8.5f, vehiculo.z + 8.5f })
            {
                float hacia = Mathf.Sign(vehiculo.z - zt);
                for (int i = 0; i < 4; i++)
                {
                    float px = vehiculo.x + (i % 2 == 0 ? -1.5f : 1.5f), pz = zt + (i < 2 ? -1.5f : 1.5f);
                    Cubo("Torre · columna", raiz, new Vector3(px, 20f, pz), new Vector3(0.4f, 40f, 0.4f), rojo, true);
                }
                for (float y = 4f; y <= 36f; y += 4f)
                {
                    Cubo("Torre · travesaño", raiz, new Vector3(vehiculo.x, y, zt - 1.5f), new Vector3(3.4f, 0.25f, 0.25f), rojo, false);
                    Cubo("Torre · travesaño", raiz, new Vector3(vehiculo.x, y, zt + 1.5f), new Vector3(3.4f, 0.25f, 0.25f), rojo, false);
                    Transform d = Cubo("Torre · diagonal", raiz, new Vector3(vehiculo.x - 1.5f, y - 2f, zt), new Vector3(0.18f, 5f, 0.18f), rojo, false);
                    d.localRotation = Quaternion.Euler((y / 4f) % 2 == 0 ? 37f : -37f, 0f, 0f);
                }
                for (float y = 10f; y <= 34f; y += 8f)
                {
                    // Plataforma de trabajo: de la torre hasta 4,7 m del eje (deja sitio a los refuerzos)
                    float desde = zt + hacia * 1.5f, hasta = vehiculo.z - hacia * 4.7f;
                    Cubo("Plataforma de trabajo", raiz, new Vector3(vehiculo.x, y, (desde + hasta) / 2f), new Vector3(6f, 0.25f, Mathf.Abs(hasta - desde)), rejilla, false);
                    Cubo("Barandilla", raiz, new Vector3(vehiculo.x - 3f, y + 0.55f, (desde + hasta) / 2f), new Vector3(0.08f, 1.1f, Mathf.Abs(hasta - desde)), amarillo, false);
                    Cubo("Barandilla", raiz, new Vector3(vehiculo.x + 3f, y + 0.55f, (desde + hasta) / 2f), new Vector3(0.08f, 1.1f, Mathf.Abs(hasta - desde)), amarillo, false);
                    Cubo("Foco de trabajo", raiz, new Vector3(vehiculo.x - 2.6f, y + 1.6f, (desde + hasta) / 2f), new Vector3(0.6f, 0.4f, 0.6f), foco, false);
                }
            }
            // Pasarelas de acceso de la pared oeste a la torre sur, a dos alturas
            foreach (float y in new[] { 18f, 26f })
            {
                Cubo("Pasarela de acceso", raiz, new Vector3((x0 + vehiculo.x - 1.5f) / 2f, y, vehiculo.z - 8.5f), new Vector3(vehiculo.x - 1.5f - x0, 0.25f, 1.6f), rejilla, false);
                Cubo("Barandilla", raiz, new Vector3((x0 + vehiculo.x - 1.5f) / 2f, y + 0.55f, vehiculo.z - 9.3f), new Vector3(vehiculo.x - 1.5f - x0, 1.1f, 0.08f), amarillo, false);
                Cubo("Barandilla", raiz, new Vector3((x0 + vehiculo.x - 1.5f) / 2f, y + 0.55f, vehiculo.z - 7.7f), new Vector3(vehiculo.x - 1.5f - x0, 1.1f, 0.08f), amarillo, false);
            }

            // Iluminación de trabajo (cálida, sin sombras)
            foreach (Vector3 lp in new[] { new Vector3(28f, 10f, 20f), new Vector3(45f, 10f, 20f), new Vector3(36.5f, 28f, 10f), new Vector3(36.5f, 28f, 30f), new Vector3(30f, 40f, 20f) })
            {
                Light l = new GameObject("Foco de trabajo (luz)").AddComponent<Light>();
                l.transform.SetParent(raiz, false);
                l.transform.localPosition = lp;
                l.type = LightType.Point;
                l.range = 26f;
                l.intensity = 1.6f;
                l.color = new Color(1f, 0.88f, 0.7f);
                l.shadows = LightShadows.None;
            }

            // Lanzadera L-4 en vertical (vista y previsualización en IntegracionLanzadera)
            Integracion = gameObject.AddComponent<IntegracionLanzadera>();
            Integracion.Construir(this, raiz, vehiculo, MaterialCache, MatTransparente(new Color(1f, 0.8f, 0.2f, 0.85f)),
                                  MatTransparente(new Color(0.4f, 1f, 0.6f, 0.9f)), fuente);

            // Consola B y rótulos
            Terminal(raiz, "Integración de la lanzadera", "B", new Vector3(27f, 0f, 16f), new Vector3(-1f, 0f, 0f), EstacionHangar.Integracion, new Color(0.7f, 0.9f, 1f));
            // La consola mira al paso del hangar; el punto de uso se pone delante de ella
            Rotulo("B · INTEGRACIÓN DE LA LANZADERA L-4 (vehículo terrestre)\nla EM-4X sigue en órbita · E en la consola B: elegir y comparar piezas",
                   raiz, new Vector3(30f, 8f, 20f), 0.09f, new Color(0.75f, 0.95f, 1f));
            rotuloLanzadera = Rotulo("", raiz, new Vector3(27f, 3.6f, 16f), 0.045f, Color.white);
            // Cartel en el lado del hangar, sobre el paso
            Rotulo("B · INTEGRACIÓN DE LA LANZADERA L-4 →", raizHangar, new Vector3(19.6f, 9.8f, 19f), 0.07f, new Color(0.75f, 0.95f, 1f), false)
                .transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Muelle de la bodega: los lotes que viajan en la lanzadera se entregan aquí
            Cubo("Muelle de la bodega", raiz, new Vector3(ZonaMuelle.center.x, 0.03f, ZonaMuelle.center.y), new Vector3(ZonaMuelle.width, 0.06f, ZonaMuelle.height), mEmbarque, false);
            Rotulo("B · MUELLE DE LA BODEGA DE LA LANZADERA\ntrae aquí los lotes que viajan en la L-4", raiz,
                   new Vector3(ZonaMuelle.center.x, 3f, ZonaMuelle.center.y), 0.06f, new Color(0.5f, 1f, 0.6f));
            for (int i = 0; i < 3; i++)
            {
                Transform hm = new GameObject("Hueco del muelle · " + LotesCarro[i]).transform;
                hm.SetParent(raiz, false);
                hm.localPosition = new Vector3(ZonaMuelle.x + 1.2f + i * 1.8f, 0.06f, ZonaMuelle.yMax - 1f);
                huecosMuelle[LotesCarro[i]] = hm;
            }
            huecosMuelle["componentes"] = huecosEnvio["componentes"];     // los componentes nunca viajan en la lanzadera
        }

        void Estanteria(Transform raiz, string id, string nombre, float z)
        {
            Transform t = new GameObject("Estantería · " + nombre).transform;
            t.SetParent(raiz, false);
            t.localPosition = new Vector3(-17.5f, 0f, z);
            Cubo("Pata", t, new Vector3(0f, 1.5f, -1.9f), new Vector3(1.2f, 3f, 0.1f), mEstanteria, true);
            Cubo("Pata", t, new Vector3(0f, 1.5f, 1.9f), new Vector3(1.2f, 3f, 0.1f), mEstanteria, true);
            Cubo("Balda", t, new Vector3(0f, 1.6f, 0f), new Vector3(1.2f, 0.08f, 3.9f), mEstanteria, true);
            Transform hueco = new GameObject("Hueco del contenedor").transform;
            hueco.SetParent(t, false);
            hueco.localPosition = Vector3.zero;
            estanterias[id] = hueco;
            rotulosLote[id] = Rotulo(nombre.ToUpper(), t, new Vector3(1.4f, 3.5f, 0f), 0.05f, new Color(1f, 0.8f, 0.4f));
            Punto(EstacionHangar.Estanteria, "Estantería de " + nombre, new Vector3(-16.2f, 0f, z), new Vector3(-17.5f, 0f, z), id);

            // Un contenedor por lote (agrupa todas las unidades: no hay que llevar una caja por unidad)
            Transform c = new GameObject("Contenedor · " + nombre).transform;
            c.SetParent(hueco, false);
            Vector3 tam = id == "sonda" ? new Vector3(0.8f, 0.9f, 1.2f) : new Vector3(0.75f, 0.7f, 1.1f);
            Cubo("Caja", c, new Vector3(0f, tam.y / 2f, 0f), tam, mLote[id], false);
            rotulosContenedor[id] = Rotulo("", c, new Vector3(0f, tam.y + 0.35f, 0f), 0.03f, Color.white);
            contenedores[id] = c;
        }

        // ------------------------------------------------------------------ Representación de la selección

        void ActualizarVisual()
        {
            if (Resumen == null || rotuloRevision == null) return;
            bool cerrada = ExpedicionActual.Confirmada;
            if (Holo != null && !Holo.Animando) Holo.Actualizar();
            if (Integracion != null && !Integracion.Animando) Integracion.Actualizar();

            // Si el carro lleva un contenedor que ya no corresponde a la selección, se descarga (vuelve a su estantería).
            if (Carro.Lote != null)
            {
                LoteCarga lc = Lotes.Find(x => x.id == Carro.Lote);
                if (lc == null || lc.preparado || lc.firma != Carro.Firma) Carro.Vaciar();
            }

            for (int i = 0; i < LotesCarro.Length; i++)
            {
                string id = LotesCarro[i];
                LoteCarga lo = Lotes.Find(x => x.id == id);
                Transform c = contenedores[id];
                if (lo == null)
                {
                    c.gameObject.SetActive(false);
                    rotulosLote[id].text = NombresCarro[i].ToUpper() + "\nno hace falta (0)";
                    rotulosLote[id].color = new Color(0.7f, 0.7f, 0.7f);
                    continue;
                }
                c.gameObject.SetActive(true);
                rotulosContenedor[id].text = $"{NombresCarro[i].ToUpper()} × {lo.cantidad}";
                Transform destino = lo.preparado ? (lo.enLanzadera ? huecosMuelle[id] : huecosEnvio[id]) : Carro.Lote == id ? Carro.Bandeja : estanterias[id];
                if (c.parent != destino)
                {
                    c.SetParent(destino, false);
                    c.localPosition = destino == estanterias[id] ? new Vector3(0f, 0.02f, 0f) : Vector3.zero;
                    c.localRotation = Quaternion.identity;
                }
                string donde = lo.enLanzadera ? "muelle de la lanzadera (B)" : "zona de envío (C)";
                string estado = lo.preparado ? "LISTO · en el " + donde : Carro.Lote == id ? "EN EL CARRO · llévalo al " + donde : "POR PREPARAR · carro → " + donde;
                rotulosLote[id].text = NombresCarro[i].ToUpper() + "\n" + lo.detalle.Replace("\n", ", ") + "\n" + estado;
                rotulosLote[id].color = lo.preparado ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.8f, 0.4f);
            }

            LoteCarga comb = LoteCombustible;
            bool combListo = comb == null || comb.preparado;
            indicadorCombustible.sharedMaterial = combListo ? mVerde : mRojo;
            rotuloCombustible.text = comb == null ? "COMBUSTIBLE · no se envía (0)" : comb.preparado ? "COMBUSTIBLE · LLENADO AUTORIZADO\n" + comb.detalle
                                   : "COMBUSTIBLE · por autorizar\n" + comb.detalle + (AlimentacionVerificada ? "\nalimentación verificada" : "");
            if (rotuloLanzadera != null && Resumen.lanzadera != null)
            {
                ResumenLanzadera rl = Resumen.lanzadera;
                rotuloLanzadera.text = $"B · LANZADERA L-4 · {(rl.CapacidadSuperada ? "CAPACIDAD SUPERADA" : "configuración válida")}\n" +
                                       $"masa extra {rl.masaExtraKg:0} de {rl.capacidadKg:0} kg · opciones {rl.costeOpcionesMUSD:0.#} M\n" +
                                       (rl.enLanzadera.Count > 0 ? "lleva: " + string.Join(", ", rl.enLanzadera) : "no lleva carga") +
                                       (rl.envioOrbital ? "\nenvío orbital aparte: sí (10 M)" : "\nenvío orbital aparte: no hace falta");
                rotuloLanzadera.color = rl.CapacidadSuperada ? new Color(1f, 0.5f, 0.4f) : new Color(0.75f, 0.95f, 1f);
            }
            rotuloCombustible.color = combListo ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.75f, 0.45f);

            int pendientes = 0; string falta = "";
            foreach (Requisito r in Requisitos()) if (!r.ok) { pendientes++; if (falta.Length == 0) falta = r.texto; }
            rotuloRevision.text = cerrada ? "PREPARACIÓN CERRADA" : "PREPARACIÓN ABIERTA";
            rotuloRevision.color = cerrada ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.85f, 0.4f);
            rotuloEmbarque.text = pendientes == 0 ? "LISTO PARA EMBARCAR" : $"FALTA: {falta}" + (pendientes > 1 ? $" (+{pendientes - 1})" : "");
            rotuloEmbarque.color = pendientes == 0 ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.55f, 0.45f);
        }
    }
}
