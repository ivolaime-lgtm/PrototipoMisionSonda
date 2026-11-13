using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    public enum FaseNave { Preparacion, CuentaAtras, Ascenso, Crucero, Llegada, Orbita, Despliegue, Abortada }

    /// <summary>
    /// Controla el recorrido dentro de la nave: preparación → despegue → viaje con eventos →
    /// entrada en órbita → despliegue de la sonda → Exploracion.
    ///
    /// Puestos (diferenciados para repartirlos en equipo más adelante):
    /// - Pilotaje: despegue, maniobras para esquivar escombros, frenada para entrar en órbita.
    /// - Ingeniería: comprobar la nave y reparar la avería eléctrica (procedimiento de 3 pasos).
    /// - Comunicaciones: recibir y confirmar instrucciones del centro de operaciones.
    /// - Ciencia: comprobar la sonda y desplegarla.
    ///
    /// Todo es DEMOSTRACIÓN: nave tripulada ficticia, viaje comprimido, escombros ficticios.
    /// Los tiempos se editan aquí; los valores de la nave, en el asset CatalogoNave.
    /// </summary>
    public class ControlNave : MonoBehaviour
    {
        [Serializable]
        public struct Tiempos
        {
            [Tooltip("Segundos de la cuenta atrás.")] public float cuentaAtrasS;
            [Tooltip("Segundos del ascenso tras el despegue.")] public float ascensoS;
            [Tooltip("Segundos reales del viaje (sin contar esperas por eventos sin resolver).")] public float cruceroS;
            [Tooltip("Segundos de aviso antes de que lleguen los escombros.")] public float avisoEscombrosS;
            [Tooltip("Segundos durante los que llegan escombros.")] public float duracionEscombrosS;
            [Tooltip("Segundos entre el despliegue de la sonda y el cambio a Exploracion.")] public float despliegueS;
        }

        [SerializeField] CatalogoMision catalogoMision;
        [SerializeField] CatalogoNave catalogoNave;
        [SerializeField] JugadorNave jugador;
        [SerializeField] ExteriorNave exterior;
        [SerializeField] PuestoNave[] puestos;
        [SerializeField] Light[] lucesCabina;
        [Tooltip("Las tres palancas del panel de ingeniería (circuitos 1, 2 y 3).")]
        [SerializeField] Renderer[] palancasIngenieria;
        [SerializeField] string escenaExploracion = "Exploracion";
        [SerializeField] string escenaResultados = "Resultados";

        [Header("Modo de prueba")]
        [Tooltip("Activado: usa los tiempos de prueba (cuenta atrás y viaje cortos).")]
        [SerializeField] bool modoPrueba = false;
        [SerializeField] Tiempos tiemposNormales = new Tiempos
        {
            cuentaAtrasS = 10f, ascensoS = 8f, cruceroS = 150f,
            avisoEscombrosS = 8f, duracionEscombrosS = 22f, despliegueS = 4f
        };
        [SerializeField] Tiempos tiemposPrueba = new Tiempos
        {
            cuentaAtrasS = 3f, ascensoS = 3f, cruceroS = 40f,
            avisoEscombrosS = 4f, duracionEscombrosS = 10f, despliegueS = 1.5f
        };

        [Header("Eventos del viaje")]
        [Tooltip("Progreso del viaje (0 a 1) en el que se produce la avería.")]
        [Range(0f, 1f)] [SerializeField] float momentoAveria = 0.3f;
        [Tooltip("Progreso del viaje (0 a 1) en el que llega la alerta de escombros (tras resolver la avería).")]
        [Range(0f, 1f)] [SerializeField] float momentoEscombros = 0.6f;
        [Tooltip("Velocidad visual de las estrellas durante el viaje.")]
        [SerializeField] float velocidadEstrellas = 70f;

        [Header("Separación de etapa (despegue) — procedimiento simplificado")]
        [Tooltip("Momento del ascenso (0 a 1) en que se abre la ventana de separación.")]
        [Range(0.1f, 0.6f)] [SerializeField] float inicioVentanaEtapa = 0.3f;
        [Tooltip("Duración mínima de la ventana, en segundos reales (también en modo de prueba).")]
        [SerializeField] float ventanaEtapaMinimaS = 2.5f;
        [Tooltip("Puntos de viaje por separar la etapa dentro de la ventana.")]
        [SerializeField] int puntosSeparacion = 5;

        [Header("Demostración: misión tripulada")]
        [Tooltip("Activado: la misión termina al entrar en órbita y va a Resultados (la exploración con la sonda queda como fase posterior). " +
                 "Desactivado: se despliega la sonda y se pasa a Exploracion, como antes.")]
        [SerializeField] bool terminarAlLlegar = true;
        [Tooltip("Puntos por llegar al destino y entrar en órbita (DEMOSTRACIÓN).")]
        [SerializeField] int puntosLlegada = 10;
        [SerializeField] string destino = "Luna helada (ficticia)";
        [Tooltip("Distancia total mostrada en pantalla, en millones de km. SIMULADA: el progreso del viaje es guionizado.")]
        [SerializeField] float distanciaSimuladaMkm = 600f;

        [Header("Sectores de la cabina (versión reducida)")]
        [Tooltip("Muestra rótulos, bordes en el suelo, tabiques bajos y luz propia en los cuatro sectores. " +
                 "Solo visual: no mueve puestos ni colisionadores.")]
        [SerializeField] bool mostrarSectores = true;

        [Header("Despegue visto desde fuera")]
        [Tooltip("Toma exterior breve del despegue, la separación y la salida al espacio; después vuelve a la cabina. " +
                 "Solo visual y sonido: no cambia tiempos ni la orden de separación (Espacio en Pilotaje). Intro la omite.")]
        [SerializeField] bool mostrarDespegueExterior = true;

        [Header("Observación de aproximación (bonus aparte, DEMOSTRACIÓN)")]
        [Tooltip("Capturar datos del destino en Ciencia (gasta energía de la nave) y enviarlos desde Comunicaciones antes de llegar. " +
                 "Da un bonus aparte que no suma a la puntuación principal. Desactivado: el juego queda exactamente como antes.")]
        [SerializeField] ObservacionAproximacion.Ajustes observacion = ObservacionAproximacion.Ajustes.PorDefecto;

        [Header("Corrección de trayectoria antes del frenado (opcional, DEMOSTRACIÓN)")]
        [Tooltip("Al final del viaje, centrar un corredor de entrada con WASD desde Pilotaje. Si se completa, el marcador de la " +
                 "inserción orbital va más lento. Desactivado: sin anillo, mensajes, consumos ni demora; frenado como antes.")]
        [SerializeField] CorredorAproximacion.Ajustes correccion = CorredorAproximacion.Ajustes.PorDefecto;

        [Header("Navegación longitudinal (opcional, DEMOSTRACIÓN)")]
        [Tooltip("Desactivada por defecto: el viaje es el de siempre. Activada: Espacio mantenido en Pilotaje enciende el motor principal, " +
                 "R gira la nave 180°, la nave vuela por inercia y hay que frenar en la zona de aproximación.")]
        [SerializeField] NavegacionLongitudinal.Ajustes navegacion = NavegacionLongitudinal.Ajustes.PorDefecto;

        [Header("Transición de llegada")]
        [Tooltip("Muestra la explicación del descenso antes de pasar a Exploracion (se puede omitir con Intro).")]
        [SerializeField] bool mostrarTransicion = true;
        [SerializeField] float segundosPorTarjeta = 4.5f;
        [SerializeField] float segundosPorTarjetaPrueba = 2f;

        // ------------------------------------------------------------------ Estado público (lo lee HudNave)

        public FaseNave Fase { get; private set; }
        public ResumenMision Mision { get; private set; }
        public ResumenNave Nave { get; private set; }
        public CatalogoNave Catalogo => catalogoNave;
        public bool ModoPrueba => modoPrueba;

        public float Progreso { get; private set; }
        public float Combustible { get; private set; }
        public float Integridad { get; private set; }
        public float EnergiaNave { get; private set; }
        public float TiempoFase { get; private set; }
        public float FlashImpacto { get; private set; }
        public string MotivoAborto { get; private set; } = "";
        public string Aviso { get; private set; } = "";
        public float TiempoAviso { get; private set; }
        public Vector2 DesplazamientoLateral => desplazamiento;
        public Vector2 VelocidadLateral => velocidadLateral;
        public float MarcadorInsercion { get; private set; }
        public bool InsercionDisponible => Fase == FaseNave.Llegada && MensajePendiente == null;

        /// <summary>Regla de demostración: masa seca + combustible actual + carga científica a bordo.</summary>
        public float MasaActualKg
        {
            get
            {
                if (Nave == null) return catalogoNave.masaCascoKg;
                float carga = Nave.cargaCientificaKg - (sondaABordo || Mision == null ? 0f : Mision.masaSondaKg);
                return Nave.masaSecaKg + carga + Combustible;
            }
        }

        /// <summary>Aceleración de maniobra = empuje del motor elegido / masa total actual (seca + carga + combustible).</summary>
        public float AceleracionMS2 => Nave != null && MasaActualKg > 0f ? Nave.empujeN / MasaActualKg : 0f;

        /// <summary>Consumo de maniobra en este instante (kg/s): propulsores WASD o estabilizador. 0 si no se gasta.</summary>
        public float ConsumoActualKgS { get; private set; }
        /// <summary>Combustible gastado en maniobras (WASD + estabilizador) en este viaje (kg).</summary>
        public float CombustibleManiobrasKg => EstadoViaje.CombustibleManiobrasKg;

        public float CuentaAtrasRestante => Fase == FaseNave.CuentaAtras ? Mathf.Max(0f, T.cuentaAtrasS - TiempoFase) : 0f;

        public enum EstadoAveria { Ninguna, Activa, Resuelta }
        public enum EstadoEscombros { Pendiente, Aviso, Activo, Despejando, Terminado }

        public enum EstadoEtapa { Pendiente, Correcta, Temprana, Automatica }
        public EstadoEtapa Etapa { get; private set; }
        public float VentanaEtapaInicioS => T.ascensoS * inicioVentanaEtapa;
        public float VentanaEtapaFinS => VentanaEtapaInicioS + Mathf.Max(ventanaEtapaMinimaS, T.ascensoS * 0.3f);
        public bool EtapaEnVentana => Fase == FaseNave.Ascenso && Etapa == EstadoEtapa.Pendiente &&
                                      TiempoFase >= VentanaEtapaInicioS && TiempoFase <= VentanaEtapaFinS;

        public EstadoAveria Averia { get; private set; }
        public EstadoEscombros Escombros { get; private set; }
        public float TiempoEscombros { get; private set; }
        public int CircuitoAveriado { get; private set; }
        public int PasoAveria { get; private set; }

        public Tiempos T => modoPrueba ? tiemposPrueba : tiemposNormales;

        public bool TerminaAlLlegar => terminarAlLlegar;
        /// <summary>Nombre del destino de esta partida: el elegido en la selección de misión o, si no hay lista, el de siempre.</summary>
        public string Destino => nombreDestino ?? destino;
        /// <summary>Definición del destino elegido (null si el catálogo no tiene destinos).</summary>
        public DefinicionDestino DestinoElegido { get; private set; }
        /// <summary>Distancia restante mostrada (SIMULADA: depende del progreso guionizado del viaje).</summary>
        public float DistanciaRestanteMkm => Mathf.Max(0f, distanciaSimuladaMkm * (1f - Progreso));
        public bool ObjetivoAlcanzado { get; private set; }

        /// <summary>Observación de aproximación (solo lógica). La leen el HUD y las pantallas; null antes de Start.</summary>
        public ObservacionAproximacion Observacion => obs;
        bool ObsActiva => obs != null && obs.Activa;

        /// <summary>Corrección de trayectoria (solo lógica). La leen VistaCorredor y los textos; null antes de Start.</summary>
        public CorredorAproximacion Corredor => corredor;
        bool CorActiva => corredor != null && corredor.Activa;
        bool VentanaCorreccionAbierta => CorActiva && corredor.EstadoActual == CorredorAproximacion.Estado.Activa;
        /// <summary>Periodo del marcador de inserción: 2,2 s como siempre, salvo corrección completada.</summary>
        float PeriodoInsercionS => CorActiva ? corredor.PeriodoInsercionS : 2.2f;
        /// <summary>En modo de prueba el viaje va más rápido: captura y envío se aceleran en la misma proporción.</summary>
        float EscalaObservacion => modoPrueba && tiemposPrueba.cruceroS > 0f ? tiemposNormales.cruceroS / tiemposPrueba.cruceroS : 1f;

        /// <summary>Navegación longitudinal (solo lógica). La leen el HUD y las vistas; null antes de Start.</summary>
        public NavegacionLongitudinal Navegacion => nav;
        public bool NavActiva => nav != null && nav.Activa;
        /// <summary>True mientras el motor principal está encendido (navegación longitudinal).</summary>
        public bool MotorPrincipalEncendido { get; private set; }
        /// <summary>Por qué no se enciende el motor principal ahora (vacío si puede encenderse).</summary>
        public string MotivoMotorBloqueado { get; private set; } = "";
        /// <summary>Por qué no se autoriza la inserción dentro de la zona (vacío si nada la bloquea).</summary>
        public string MotivoInsercionBloqueada { get; private set; } = "";
        /// <summary>Combustible mínimo (kg) para completar la llegada con las reglas de esta versión: llegar a la zona a velocidad de captura + inserción perfecta.</summary>
        public float CombustibleNecesarioLlegadaKg { get; private set; }
        public float CombustibleMotorPrincipalKg => EstadoViaje.CombustibleMotorPrincipalKg;

        // ------------------------------------------------------------------ Mensajes de comunicaciones

        public class Mensaje
        {
            public string titulo, texto;
            public bool confirmado;
        }

        readonly List<Mensaje> mensajes = new List<Mensaje>();
        public IReadOnlyList<Mensaje> Mensajes => mensajes;

        public Mensaje MensajePendiente
        {
            get
            {
                foreach (Mensaje m in mensajes) if (!m.confirmado) return m;
                return null;
            }
        }

        // ------------------------------------------------------------------ Estado interno

        bool chkComunicaciones, chkIngenieria, chkCiencia;
        ObservacionAproximacion obs;
        CorredorAproximacion corredor;
        NavegacionLongitudinal nav;
        NavegacionLongitudinal.Ajustes nc;       // ajustes de navegación de ESTA partida (inspector + destino elegido)
        string nombreDestino;
        bool motorPedido, giroPedido, espacioPilotaje, esperarSoltarEspacioMotor, esperarSoltarEspacioInsercion;
        bool mensajeInsercionEnviado, avisoZonaRapida, avisoSobrepaso;
        float combustibleAlAbrirCorreccion;
        ObservacionAproximacion.EstadoVentana ventanaAnterior = ObservacionAproximacion.EstadoVentana.Pendiente;
        bool esperarSoltarEspacioComs;
        bool sondaABordo = true;
        Vector2 desplazamiento, velocidadLateral;
        float siguienteEscombro;
        float sacudidaImpacto;
        float progresoAscenso;
        bool transicionIniciada;
        float tiempoCierre;
        float tiempoInsercion;
        float inicioAveria;
        bool terminado;
        Material[] materialesPalancas;
        Color[] coloresLuces;

        const string TituloPlan = "Plan de vuelo";
        const string TituloInsercion = "Autorización de inserción orbital";
        const string TituloDespliegue = "Autorización de despliegue";
        const string TituloEvaluacion = "Evaluación del viaje";
        const string TituloObjetivo = "Objetivo alcanzado";

        // ------------------------------------------------------------------ Inicio

        void Start()
        {
            if (catalogoMision == null || catalogoNave == null || jugador == null || exterior == null)
            {
                Debug.LogError("[Prototipo] ControlNave: faltan referencias (catálogos, jugador o exterior). " +
                               "Vuelve a crear la escena con el menú Prototipo/8.", this);
                enabled = false;
                return;
            }

            if (!MisionActual.Configurada)
                Debug.Log("[Prototipo] Se inició Nave sin pasar por Configuracion: se usan las primeras opciones del catálogo.", this);

            Mision = catalogoMision.Calcular(MisionActual.Eleccion);
            Nave = catalogoNave.Calcular(Mision);

            Combustible = Nave.combustibleInicialKg;
            Integridad = catalogoNave.integridadInicial;
            EnergiaNave = Nave.energiaWh;

            EstadoViaje.Reiniciar();
            EstadoViaje.MasaInicialKg = Nave.masaInicialKg;
            EstadoViaje.CombustibleInicialKg = Nave.combustibleInicialKg;
            EstadoViaje.CombustibleExtraKg = Nave.combustibleExtraKg;
            EstadoViaje.EnergiaInicialWh = Nave.energiaWh;
            EstadoViaje.ZonaVerdeInsercion = catalogoNave.zonaVerdeInsercion;
            EstadoViaje.Lanzador = Mision.lanzador != null ? Mision.lanzador.nombre : "";
            obs = new ObservacionAproximacion(observacion);
            corredor = new CorredorAproximacion(correccion);
            // Destino elegido (CatalogoMision → Destinos). Sin lista, el de siempre con los valores del inspector.
            DestinoElegido = Mision.destino;
            nombreDestino = DestinoElegido != null && !string.IsNullOrEmpty(DestinoElegido.nombre) ? DestinoElegido.nombre : destino;
            nc = navegacion;
            if (nc.usarDestinoElegido && DestinoElegido != null)
            {
                nc.distanciaRutaM = DestinoElegido.distanciaRutaM;
                nc.distanciaRutaPruebaM = DestinoElegido.distanciaRutaPruebaM;
                nc.semiZonaM = DestinoElegido.semiZonaM;
                nc.velocidadCapturaMS = DestinoElegido.velocidadCapturaMS;
            }
            nav = new NavegacionLongitudinal(nc);
            if (nav.Activa)
            {
                nav.Iniciar(modoPrueba);
                if (nc.gravedadExperimental)
                {
                    DefinicionDestino g = DestinoElegido ?? new DefinicionDestino();
                    nav.ConfigurarGravedad(g.Mu, g.DistanciaPasoEfectivaM);
                    EstadoViaje.GravedadActiva = true;
                    if (g.PasoCorregido)
                        Debug.LogWarning($"[Navegación] {g.nombre}: la distancia de paso ({g.distanciaPasoM:0} m) era menor que el radio de " +
                                         $"seguridad ({g.radioSeguridadM:0} m); se usa {g.DistanciaPasoEfectivaM:0} m.", this);
                    Debug.Log($"[Navegación] Gravedad EXPERIMENTAL (un eje): μ = {g.Mu:0} m³/s², paso a {g.DistanciaPasoEfectivaM:0} m del centro, " +
                              $"caída {g.velocidadCaidaMS:0} m/s.", this);
                }
                EstadoViaje.NavegacionActiva = true;
                EstadoViaje.RutaM = nav.L;
                // SOLO PRUEBAS: combustible inicial manual. No cambia el catálogo ni los tanques.
                if (navegacion.combustiblePruebaActivo)
                {
                    Combustible = Mathf.Clamp(navegacion.combustiblePruebaKg, 0f, Nave.capacidadTanqueKg);
                    EstadoViaje.CombustibleInicialKg = Combustible;
                    EstadoViaje.CombustiblePruebaKg = Combustible;
                    EstadoViaje.MasaInicialKg = Nave.masaInicialKg - Nave.combustibleInicialKg + Combustible;   // la masa seca no cambia
                    Debug.LogWarning($"[Combustible] COMBUSTIBLE DE PRUEBA activado: la nave empieza con {Combustible:0.0} kg " +
                                     $"(calculado: {Nave.combustibleInicialKg:0.0} kg). Solo para pruebas.", this);
                }
            }
            if (corredor.Activa) gameObject.AddComponent<VistaCorredor>();

            exterior.Configurar(catalogoNave.radioNaveM, catalogoNave.velocidadEscombrosMS);
            exterior.AlPasarEscombro = RegistrarEscombro;
            exterior.Preparar();
            if (DestinoElegido != null) exterior.ConfigurarDestinoVisual(DestinoElegido.colorVisual, DestinoElegido.escalaVisual);
            if (nav.Activa) exterior.ActivarNavegacion();
            if (mostrarSectores) gameObject.AddComponent<SectoresNave>();
            if (mostrarDespegueExterior) gameObject.AddComponent<CinematicaDespegue>();

            if (palancasIngenieria != null)
            {
                materialesPalancas = new Material[palancasIngenieria.Length];
                for (int i = 0; i < palancasIngenieria.Length; i++)
                    if (palancasIngenieria[i] != null) materialesPalancas[i] = palancasIngenieria[i].material;
            }
            if (lucesCabina != null)
            {
                coloresLuces = new Color[lucesCabina.Length];
                for (int i = 0; i < lucesCabina.Length; i++)
                    if (lucesCabina[i] != null) coloresLuces[i] = lucesCabina[i].color;
            }

            Enviar(TituloPlan,
                "Destino: la luna helada ficticia. Viaje comprimido para la demostración.\n" +
                "Antes de despegar, comprobad la nave en Ingeniería y la sonda en Ciencia, " +
                "y despegad desde Pilotaje.");

            if (modoPrueba)
                Debug.Log("[Prototipo] Nave en MODO DE PRUEBA: tiempos reducidos.", this);
            if (nav.Activa)
                Debug.Log($"[Prototipo] Navegación longitudinal ACTIVADA hacia {nombreDestino}: ruta {nav.L:0} m, salida a {nc.velocidadInicialMS:0} m/s, " +
                          $"zona ±{nc.semiZonaM:0} m, captura ≤ {nc.velocidadCapturaMS:0} m/s.", this);

            CambiarFase(FaseNave.Preparacion);
        }

        void CambiarFase(FaseNave nueva)
        {
            Fase = nueva;
            TiempoFase = 0f;
        }

        void Enviar(string titulo, string texto) => mensajes.Add(new Mensaje { titulo = titulo, texto = texto });

        void Avisar(string texto, float segundos = 3f)
        {
            Aviso = texto;
            TiempoAviso = segundos;
        }

        // ------------------------------------------------------------------ Bucle principal

        void Update()
        {
            float dt = Time.deltaTime;
            TiempoFase += dt;
            if (TiempoAviso > 0f) TiempoAviso -= dt;
            FlashImpacto = Mathf.Max(0f, FlashImpacto - dt * 1.5f);
            sacudidaImpacto = Mathf.Max(0f, sacudidaImpacto - dt * 2f);

            if (Fase != FaseNave.Preparacion && Fase != FaseNave.Abortada && Fase != FaseNave.Despliegue)
                EstadoViaje.SegundosViaje += dt;

            float sacudidaFase = 0f;
            switch (Fase)
            {
                case FaseNave.CuentaAtras:
                    sacudidaFase = CuentaAtrasRestante < 2f ? 0.5f : 0f;
                    if (TiempoFase >= T.cuentaAtrasS)
                    {
                        CambiarFase(FaseNave.Ascenso);
                        Avisar("¡Despegue!", 2f);
                    }
                    break;

                case FaseNave.Ascenso:
                    // Si se cierra la ventana sin orden, el ordenador separa la etapa (tarde).
                    if (Etapa == EstadoEtapa.Pendiente && TiempoFase > VentanaEtapaFinS)
                        Separar(EstadoEtapa.Automatica);
                    progresoAscenso += T.ascensoS > 0f ? dt / T.ascensoS : 1f;
                    if (Etapa == EstadoEtapa.Pendiente) progresoAscenso = Mathf.Min(progresoAscenso, 0.8f);
                    float t = Mathf.Clamp01(progresoAscenso);
                    exterior.Ascenso(t);
                    sacudidaFase = Mathf.Lerp(1.2f, 0.2f, t);
                    if (t >= 1f)
                    {
                        jugador.AsientoBloqueado = false;
                        exterior.VelocidadEstrellas = velocidadEstrellas;
                        CambiarFase(FaseNave.Crucero);
                        esperarSoltarEspacioMotor = true;   // el Espacio de la separación no enciende el motor
                        Debug.Log($"[Combustible] Inicio del crucero: {Combustible:0.0} kg (maniobras hasta ahora {EstadoViaje.CombustibleManiobrasKg:0.0} kg).");
                        Avisar(NavActiva
                            ? $"En ruta a {nav.VelocidadMS:0} m/s. Pilotaje: mantén Espacio para el motor principal; R gira la nave 180°."
                            : "En ruta al destino. Puedes levantarte (Q) y recorrer la cabina.", 4f);
                    }
                    break;

                case FaseNave.Crucero:
                    ActualizarCrucero(dt);
                    break;

                case FaseNave.Llegada:
                    if (!NavActiva) exterior.VelocidadEstrellas = Mathf.MoveTowards(exterior.VelocidadEstrellas, 4f, dt * 30f);
                    break;

                case FaseNave.Orbita:
                    if (NavActiva) exterior.VelocidadEstrellas = Mathf.MoveTowards(exterior.VelocidadEstrellas, 4f, dt * 30f);
                    // Demostración tripulada: al confirmar los mensajes de llegada, la misión termina aquí.
                    if (terminarAlLlegar && MensajePendiente == null && !terminado)
                    {
                        if (!ObjetivoAlcanzado)
                        {
                            ObjetivoAlcanzado = true;
                            tiempoCierre = 0f;
                            CerrarViaje(false);
                            InformeMision.Ultimo = CrearInformeTripulado(MotivoFin.LlegadaAlDestino);
                            Avisar("OBJETIVO ALCANZADO: órbita establecida sobre el destino.", 5f);
                        }
                        tiempoCierre += dt;
                        if (tiempoCierre >= 4f)
                        {
                            terminado = true;
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                            SceneManager.LoadScene(escenaResultados);
                        }
                    }
                    break;

                case FaseNave.Despliegue:
                    if (TiempoFase >= T.despliegueS && !transicionIniciada)
                    {
                        transicionIniciada = true;
                        CerrarViaje(false);
                        if (mostrarTransicion)
                            TransicionDescenso.Iniciar(gameObject, modoPrueba ? segundosPorTarjetaPrueba : segundosPorTarjeta, IrAExploracion);
                        else
                            IrAExploracion();
                    }
                    break;

                case FaseNave.Abortada:
                    exterior.VelocidadEstrellas = Mathf.MoveTowards(exterior.VelocidadEstrellas, 0f, dt * 30f);
                    if (TiempoFase >= 5f && !terminado)
                    {
                        terminado = true;
                        CerrarViaje(true);
                        InformeMision.Ultimo = CrearInformeAborto();
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                        SceneManager.LoadScene(escenaResultados);
                    }
                    break;
            }

            jugador.Sacudida = Mathf.Max(sacudidaFase, sacudidaImpacto);

            // Puesto en uso
            Keyboard k = Keyboard.current;
            PuestoNave actual = jugador.PuestoActual;
            Vector2 empuje = Vector2.zero;
            motorPedido = giroPedido = espacioPilotaje = false;
            if (actual != null && k != null && Fase != FaseNave.Abortada)
            {
                switch (actual.Tipo)
                {
                    case TipoPuesto.Pilotaje: empuje = Pilotaje(k); break;
                    case TipoPuesto.Ingenieria: Ingenieria(k); break;
                    case TipoPuesto.Comunicaciones: Comunicaciones(k); break;
                    case TipoPuesto.Ciencia: Ciencia(k); break;
                }
            }

            if (Fase == FaseNave.Crucero) Maniobrar(empuje, dt);
            else ConsumoActualKgS = 0f;
            if (NavActiva && (Fase == FaseNave.Crucero || Fase == FaseNave.Llegada)) ActualizarNavegacion(dt);
            else MotorPrincipalEncendido = false;
            if (MotorPrincipalEncendido) jugador.Sacudida = Mathf.Max(jugador.Sacudida, 0.25f);
            if (ObsActiva && Fase == FaseNave.Crucero) AvisosObservacion();

            // Avería: pierde energía mientras siga activa
            if (Averia == EstadoAveria.Activa && Fase != FaseNave.Abortada)
            {
                EnergiaNave = Mathf.Max(0f, EnergiaNave - catalogoNave.perdidaAveriaWhPorS * dt);
                EstadoViaje.SegundosAveria += dt;
            }

            // Condiciones de aborto
            if (Fase != FaseNave.Abortada && Fase != FaseNave.Despliegue)
            {
                if (Integridad <= 0f)
                    Abortar("el casco quedó demasiado dañado por los impactos de escombros.");
                else if (EnergiaNave <= 0f)
                    Abortar(ObsActiva && obs.EnergiaUsadaWh > 0f
                        ? $"la nave se quedó sin energía: la avería y la observación ({obs.EnergiaUsadaWh:0} Wh) agotaron la reserva."
                        : "la nave se quedó sin energía: la avería no se reparó a tiempo.");
            }

            ActualizarIndicadores();
            ActualizarLuces();
            ActualizarPalancas();
        }

        void ActualizarCrucero(float dt)
        {
            // Con navegación, el progreso es la fracción MÁXIMA de ruta recorrida (la calcula ActualizarNavegacion): nunca baja.
            if (!NavActiva) Progreso += T.cruceroS > 0f ? dt / T.cruceroS : 1f;

            if (Averia == EstadoAveria.Ninguna && Progreso >= momentoAveria)
            {
                Averia = EstadoAveria.Activa;
                CircuitoAveriado = UnityEngine.Random.Range(0, 3);
                PasoAveria = 0;
                inicioAveria = Time.time;
                Avisar("¡AVERÍA ELÉCTRICA! Ve al puesto de Ingeniería.", 4f);
            }

            switch (Escombros)
            {
                case EstadoEscombros.Pendiente:
                    if (Progreso >= momentoEscombros && Averia != EstadoAveria.Activa)
                    {
                        Escombros = EstadoEscombros.Aviso;
                        TiempoEscombros = 0f;
                        Avisar("¡ALERTA DE ESCOMBROS! Ve al puesto de Pilotaje.", 4f);
                    }
                    break;
                case EstadoEscombros.Aviso:
                    TiempoEscombros += dt;
                    if (TiempoEscombros >= T.avisoEscombrosS)
                    {
                        Escombros = EstadoEscombros.Activo;
                        TiempoEscombros = 0f;
                        siguienteEscombro = 0f;
                    }
                    break;
                case EstadoEscombros.Activo:
                    TiempoEscombros += dt;
                    siguienteEscombro -= dt;
                    if (siguienteEscombro <= 0f)
                    {
                        exterior.LanzarEscombro(UnityEngine.Random.value < catalogoNave.fraccionEscombrosApuntados);
                        siguienteEscombro = catalogoNave.intervaloEscombrosS;
                    }
                    if (TiempoEscombros >= T.duracionEscombrosS) Escombros = EstadoEscombros.Despejando;
                    break;
                case EstadoEscombros.Despejando:
                    if (exterior.EscombrosActivos == 0)
                    {
                        Escombros = EstadoEscombros.Terminado;
                        Avisar($"Zona de escombros superada: {EstadoViaje.Impactos} impactos, {EstadoViaje.EscombrosEsquivados} esquivados.", 4f);
                    }
                    break;
            }

            // No se llega al destino con eventos sin resolver.
            bool pendiente = Averia != EstadoAveria.Resuelta || Escombros != EstadoEscombros.Terminado;
            // Sin navegación se espera congelando el progreso; con navegación la nave sigue moviéndose y lo que se bloquea es la inserción.
            if (pendiente && !NavActiva) Progreso = Mathf.Min(Progreso, 0.99f);

            // Corrección de trayectoria: solo con la avería y los escombros resueltos; duración fija; la llegada espera a que termine.
            if (CorActiva)
            {
                if (corredor.PuedeAbrir(Progreso, !pendiente))
                {
                    corredor.Abrir(desplazamiento, velocidadLateral, catalogoNave.limiteDesplazamientoM, modoPrueba);
                    combustibleAlAbrirCorreccion = Combustible;
                    Debug.Log($"[Combustible] Abre la ventana de corrección: {Combustible:0.0} kg.");
                    Avisar($"CORRECCIÓN DE TRAYECTORIA (opcional, {corredor.DuracionVentanaS:0} s): en Pilotaje, centra el anillo con WASD " +
                           $"y mantenlo {corredor.Configuracion.permanenciaS:0} s.", 5f);
                }
                if (corredor.Actualizar(dt, desplazamiento))
                {
                    AvisarFinCorreccion();
                    Debug.Log($"[Combustible] Cierra la ventana de corrección ({corredor.NombreResultado()}): {combustibleAlAbrirCorreccion:0.0} → " +
                              $"{Combustible:0.0} kg (maniobras en la ventana {corredor.CombustibleKg:0.0} kg).");
                }
                if (VentanaCorreccionAbierta && !NavActiva) Progreso = Mathf.Min(Progreso, 0.99f);
            }

            if (NavActiva) return;   // la llegada la decide ActualizarNavegacion (zona + velocidad + eventos resueltos)

            exterior.Viaje(Progreso);

            if (Progreso >= 1f)
            {
                Progreso = 1f;
                CambiarFase(FaseNave.Llegada);
                Enviar(TituloInsercion,
                    "Estáis llegando al destino. Para quedar en órbita hay que frenar con el motor principal " +
                    "en el momento justo: frenar antes o después gasta más combustible.");
                string perdidos = "";
                if (ObsActiva)
                {
                    obs.Finalizar();
                    if (obs.Perdidos > 0f) perdidos = $" {UD(obs.Perdidos)} UD de observación sin enviar: no cuentan.";
                }
                Avisar("Llegada al destino. Mensaje nuevo en Comunicaciones." + perdidos, 4f);
            }
        }


        // ------------------------------------------------------------------ Navegación longitudinal (opcional)

        /// <summary>
        /// Física longitudinal, consumo del motor principal, llegada a la zona y regla de «llegada imposible».
        /// Se llama en Crucero y en Llegada, después de leer los mandos. La nave nunca se congela: los eventos pendientes
        /// bloquean la AUTORIZACIÓN de inserción, no el movimiento.
        /// </summary>
        void ActualizarNavegacion(float dt)
        {
            bool crucero = Fase == FaseNave.Crucero;
            MotivoMotorBloqueado = "";

            if (crucero && giroPedido && nav.IniciarGiro())
                Avisar($"GIRO DE 180° ({nc.duracionGiroS:0} s): el motor principal queda bloqueado mientras gira. La velocidad no cambia.", 3f);

            bool motor = crucero && motorPedido;
            if (motor && nav.Girando) { MotivoMotorBloqueado = $"la nave está girando ({nav.SegundosGiroRestantes:0.0} s)"; motor = false; }
            if (motor && Combustible <= 0f) { MotivoMotorBloqueado = "no queda combustible"; motor = false; }
            if (crucero && espacioPilotaje && esperarSoltarEspacioMotor) MotivoMotorBloqueado = "suelta Espacio y vuelve a pulsarlo";

            float fraccion = nav.Paso(dt, motor, AceleracionMS2);
            float gasto = Mathf.Min(Combustible, fraccion * Nave.consumoKgS * dt);
            Combustible -= gasto;
            EstadoViaje.CombustibleMotorPrincipalKg += gasto;          // categoría propia: no se mezcla con maniobras ni inserción
            ConsumoActualKgS += dt > 0f ? gasto / dt : 0f;
            MotorPrincipalEncendido = gasto > 0f;

            if (nav.CortePorCero)
            {
                esperarSoltarEspacioMotor = true;
                Avisar("Velocidad 0: nave detenida. Motor apagado: suelta Espacio para volver a encenderlo " +
                       "(empujará hacia donde apunta la proa; la orientación no cambia sola).", 4f);
            }

            if (crucero) Progreso = Mathf.Min(nav.ProgresoMaximo, 0.999f);

            exterior.VelocidadEstrellas = nav.VelocidadMS * nc.escalaVisualEstrellas;
            exterior.Navegacion(nav.DistanciaAlDestinoM, nc.semiZonaM, nav.L, nav.AnguloGrados);

            // Pasarse de largo = salir de la zona de aproximación por el lado del destino (no se atraviesa el planeta).
            if (nav.Sobrepasada && !avisoSobrepaso)
            {
                avisoSobrepaso = true;
                EstadoViaje.Sobrepasos++;
                Avisar("TE HAS PASADO DE LARGO: has sobrepasado la zona de aproximación y el destino queda detrás. " +
                       "Frena, gira con R y vuelve.", 5f);
                Debug.Log($"[Navegación] Sobrepaso de la zona: posición {nav.PosicionM:0} m, velocidad {nav.VelocidadMS:0.0} m/s.");
            }
            else if (avisoSobrepaso && nav.EnZona) avisoSobrepaso = false;

            if (nav.EnZona && !nav.VelocidadDeCaptura)
            {
                if (!avisoZonaRapida)
                {
                    avisoZonaRapida = true;
                    Avisar($"Zona de aproximación a {nav.VelocidadReferenciaCapturaMS:0} m/s: la inserción exige ≤ {nc.velocidadCapturaMS:0} m/s. ¡Frena!", 4f);
                }
            }
            else if (!nav.EnZona) avisoZonaRapida = false;

            MotivoInsercionBloqueada = MotivoBloqueoInsercion();

            if (crucero && nav.PuedeCapturar && MotivoInsercionBloqueada.Length == 0)
                EntrarLlegadaNavegacion();
            else if (Fase == FaseNave.Llegada && !SigueEnCaptura())
            {
                CambiarFase(FaseNave.Crucero);
                esperarSoltarEspacioMotor = true;
                Avisar($"Has salido de la zona de aproximación sin frenar: la inserción ya no está disponible. " +
                       $"Vuelve a la zona a ≤ {nc.velocidadCapturaMS:0} m/s.", 5f);
            }

            ComprobarLlegadaPosible();
            if (Fase == FaseNave.Crucero) AvisosNavegacion();
        }

        /// <summary>
        /// Para SALIR de la llegada se usa un pequeño margen (2 m de zona y 0,5 m/s de velocidad) que evita que la autorización
        /// parpadee justo en el borde por redondeos de la simulación. Entrar sigue exigiendo la condición exacta.
        /// </summary>
        bool SigueEnCaptura() => Mathf.Abs(nav.DistanciaAlDestinoM) <= nc.semiZonaM + 2f &&
                                 nav.VelocidadReferenciaCapturaMS <= nc.velocidadCapturaMS + 0.5f;

        string MotivoBloqueoInsercion()
        {
            if (Averia != EstadoAveria.Resuelta) return "avería eléctrica sin reparar (Ingeniería)";
            if (Escombros != EstadoEscombros.Terminado) return "zona de escombros en curso (Pilotaje)";
            if (CorActiva && corredor.EstadoActual == CorredorAproximacion.Estado.Activa)
                return $"corrección de trayectoria abierta ({corredor.SegundosRestantes:0} s más)";
            if (CorActiva && corredor.EstadoActual == CorredorAproximacion.Estado.Pendiente) return "corrección de trayectoria a punto de abrirse";
            return "";
        }

        void EntrarLlegadaNavegacion()
        {
            Progreso = 1f;
            CambiarFase(FaseNave.Llegada);
            esperarSoltarEspacioInsercion = true;      // el Espacio del motor no cuenta como orden de inserción
            MotorPrincipalEncendido = false;
            if (mensajeInsercionEnviado)
            {
                Avisar("De nuevo en la zona de aproximación: la inserción vuelve a estar disponible en Pilotaje.", 4f);
                return;
            }
            mensajeInsercionEnviado = true;
            Enviar(TituloInsercion,
                $"Estáis en la zona de aproximación a {Mathf.Abs(nav.VelocidadMS):0.0} m/s. Para quedar en órbita hay que hacer el frenado " +
                "de captura con el motor principal en el momento justo: frenar antes o después gasta más combustible. " +
                "Lo que ya frenasteis con el motor principal está cobrado aparte y no se vuelve a cobrar. " +
                "La nave sigue avanzando: si sale de la zona antes de frenar, la inserción deja de estar disponible.");
            string perdidos = "";
            if (ObsActiva)
            {
                obs.Finalizar();
                if (obs.Perdidos > 0f) perdidos = $" {UD(obs.Perdidos)} UD de observación sin enviar: no cuentan.";
            }
            Avisar("En la zona de aproximación. Mensaje nuevo en Comunicaciones." + perdidos, 4f);
            Debug.Log($"[Navegación] Llegada a la zona: posición {nav.PosicionM:0} m de {nav.L:0} m, velocidad {nav.VelocidadMS:0.0} m/s, " +
                      $"combustible {Combustible:0.0} kg.");
        }

        /// <summary>
        /// Regla de esta versión (sin gravedad): la llegada solo se completa entrando en la zona a velocidad de captura con el
        /// motor principal y haciendo después la inserción. Si el combustible no llega ni para eso con una inserción PERFECTA,
        /// la llegada es imposible y la misión se aborta explicando por qué. No se aborta por llegar a 0 kg: se aborta en cuanto
        /// deja de ser posible, que siempre ocurre antes.
        /// </summary>
        void ComprobarLlegadaPosible()
        {
            if (Fase != FaseNave.Crucero && Fase != FaseNave.Llegada) return;
            float masa = MasaActualKg;
            float dv = nav.DeltaVNecesariaMS;
            float motor = NavegacionLongitudinal.CombustibleParaDeltaV(dv, masa, Nave.empujeN, Nave.consumoKgS);
            float insercion = catalogoNave.CosteInsercion(Mathf.Max(0f, masa - motor), 0f);
            CombustibleNecesarioLlegadaKg = motor + insercion;
            if (Combustible + 0.01f >= CombustibleNecesarioLlegadaKg) return;

            string que = nav.SeAleja ? $"frenar ({Mathf.Abs(nav.VelocidadMS):0.0} m/s) y volver hacia el destino"
                       : nav.SeAcerca ? $"frenar hasta {nc.velocidadCapturaMS:0} m/s"
                       : "arrancar hacia el destino";
            Debug.Log($"[Combustible] Llegada imposible: quedan {Combustible:0.0} kg; necesarios {CombustibleNecesarioLlegadaKg:0.0} kg " +
                      $"({motor:0.0} motor para {dv:0.0} m/s + {insercion:0.0} inserción perfecta).");
            Abortar($"la llegada ya no puede completarse con las reglas de esta versión: quedan {Combustible:0.0} kg y harían falta al menos " +
                    $"{CombustibleNecesarioLlegadaKg:0.0} kg ({motor:0.0} kg del motor principal para {que}, {dv:0.0} m/s, " +
                    $"más {insercion:0.0} kg para una inserción perfecta). Sin gravedad, solo el motor principal puede llevar la nave a la zona.");
        }

        // ---------------- Textos de navegación (solo LEEN el estado; no cambian reglas)

        enum Nivel { Normal, Aviso, Urgente }

        static string Pintar(Nivel n, string s) => n == Nivel.Urgente ? $"<color=#FF6B5B><b>{s}</b></color>"
                                                : n == Nivel.Aviso ? $"<color=#FFB040><b>{s}</b></color>"
                                                : $"<color=#7CFC9A>{s}</color>";

        static string Metros(float m) => float.IsInfinity(m) ? "—" : Mathf.Abs(m) >= 1000f ? $"{m / 1000f:0.00} km" : $"{m:0} m";

        string TextoOrientacion() => nav.ProaHaciaDestino ? "proa hacia el destino" : "proa hacia fuera (popa al destino)";

        string TextoMovimiento() => nav.SeAcerca ? "<color=#7CFC9A>se acerca</color>"
                                  : nav.SeAleja ? "<color=#FF6B5B>se aleja</color>"
                                  : "detenida";

        /// <summary>Qué hace Espacio ahora mismo, según la proa y el movimiento (orientación ≠ sentido del movimiento).</summary>
        string EfectoEspacio()
        {
            if (nav.Girando) return "nada (girando)";
            if (nav.MotorFrena) return "FRENA";
            if (nav.Detenida) return nav.ProaHaciaDestino ? "arranca hacia el destino" : "arranca alejándote";
            return "ACELERA";
        }

        /// <summary>Por qué está bloqueada la inserción y cómo resolverlo (vacío si no lo está).</summary>
        string ComoResolverBloqueo()
        {
            if (Averia != EstadoAveria.Resuelta) return "ve a Ingeniería y repara la avería (1, 2, 3 y R)";
            if (Escombros != EstadoEscombros.Terminado) return "espera a que pasen los escombros y esquívalos desde Pilotaje con WASD";
            if (CorActiva && corredor.EstadoActual == CorredorAproximacion.Estado.Activa)
                return $"centra el anillo con WASD 2 s seguidos o espera {corredor.SegundosRestantes:0} s a que se cierre";
            if (CorActiva && corredor.EstadoActual == CorredorAproximacion.Estado.Pendiente) return "se abre en un momento; espera";
            return "";
        }

        /// <summary>La acción principal recomendada ahora mismo en navegación (lo primero que se lee en Pilotaje).</summary>
        string AccionNavegacion(out Nivel nivel)
        {
            NavegacionLongitudinal.Ajustes c = nc;
            float vref = nav.VelocidadReferenciaCapturaMS;
            string girar = nav.MotorFrena ? "" : "gira con R y ";
            nivel = Nivel.Normal;

            if (nav.EnZona && !nav.VelocidadDeCaptura)
            {
                nivel = Nivel.Urgente;
                return $"¡FRENA! Estás en la zona a {vref:0.0} m/s y el máximo es {c.velocidadCapturaMS:0}: {girar}mantén Espacio.";
            }
            if (nav.EnZona && MotivoInsercionBloqueada.Length > 0)
            {
                nivel = Nivel.Aviso;
                return $"Inserción bloqueada: {MotivoInsercionBloqueada}. Para resolverlo, {ComoResolverBloqueo()}. " +
                       "La nave sigue moviéndose: si va a salir de la zona, frena hasta detenerte dentro.";
            }
            if (nav.SeAleja)
            {
                nivel = Nivel.Aviso;
                return (nav.Sobrepasada ? "Te pasaste de largo. " : "Te alejas del destino. ") +
                       $"{(nav.MotorFrena ? "" : "Gira con R, ")}frena con Espacio hasta 0, suelta y vuelve a pulsar para acercarte." +
                       (nav.GravedadActiva ? " La gravedad también tira hacia el destino." : "");
            }
            if (nav.Detenida && !nav.EnZona)
                return nav.ProaHaciaDestino ? "Detenida: pulsa Espacio para avanzar hacia el destino."
                                            : "Detenida con la proa hacia fuera: gira con R y pulsa Espacio para avanzar.";
            if (nav.SeAcerca && !nav.VelocidadDeCaptura)
            {
                float necesaria = nav.DistanciaNecesariaParaEntrarM(AceleracionMS2);
                if (nav.DistanciaAZonaM <= necesaria * 1.15f)
                {
                    nivel = Nivel.Urgente;
                    return $"¡FRENA YA! {(nav.MotorFrena ? "" : "Gira con R y ")}mantén Espacio: necesitas unos {Metros(necesaria)} y quedan {Metros(nav.DistanciaAZonaM)}.";
                }
                return $"Vuela por inercia. Empieza a {(nav.MotorFrena ? "" : "girar y ")}frenar a unos {Metros(necesaria)} de la zona " +
                       $"(faltan {Metros(nav.DistanciaAZonaM)}).";
            }
            if (nav.SeAcerca)
                return $"Llegarás a la zona a velocidad de captura en ≈ {Seg(nav.SegundosAZona)} s. Puedes acelerar si quieres llegar antes.";
            return "En la zona.";
        }

        /// <summary>Bloque de navegación del panel de Pilotaje (crucero): primero la acción, después el estado por filas.</summary>
        string TextoNavegacionCrucero()
        {
            NavegacionLongitudinal.Ajustes c = nc;
            string accion = AccionNavegacion(out Nivel nivel);
            string motor = MotorPrincipalEncendido
                ? Pintar(Nivel.Aviso, $"ENCENDIDO · {(nav.MotorFrena ? "frenando" : "acelerando")} · −{Nave.consumoKgS:0.0} kg/s")
                : MotivoMotorBloqueado.Length > 0 ? Pintar(Nivel.Urgente, "BLOQUEADO: " + MotivoMotorBloqueado)
                : "apagado";
            string giro = nav.Girando ? " · " + Pintar(Nivel.Aviso, $"GIRANDO {nav.SegundosGiroRestantes:0.0} s") : "";
            float vref = nav.VelocidadReferenciaCapturaMS;
            string llegada = $"zona ±{c.semiZonaM:0} m · captura ≤ {c.velocidadCapturaMS:0} m/s · " +
                             (nav.GravedadActiva ? "en el borde llevarías " : "ahora ") +
                             (nav.VelocidadDeCaptura ? Pintar(Nivel.Normal, $"{vref:0.0} m/s") : Pintar(Nivel.Aviso, $"{vref:0.0} m/s"));
            float margen = Combustible - CombustibleNecesarioLlegadaKg;
            Nivel nm = margen < 10f ? Nivel.Urgente : margen < 30f ? Nivel.Aviso : Nivel.Normal;
            string grav = nav.GravedadActiva
                ? $"\n<b>GRAVEDAD</b>  {Mathf.Abs(nav.AceleracionGravedadMS2):0.00} m/s² hacia el destino · motor {Mathf.Abs(nav.AceleracionMotorMS2):0.00} m/s² " +
                  "· <i>experimental, un eje</i>"
                : "";

            return Pintar(nivel, "AHORA: ") + (nivel == Nivel.Normal ? accion : Pintar(nivel, accion)) + "\n" +
                   "<size=16>" +
                   $"<b>MOVIMIENTO</b>  {Mathf.Abs(nav.VelocidadMS):0.0} m/s · la nave {TextoMovimiento()} · destino a {Metros(nav.DistanciaAlDestinoM)}" +
                   (nav.Sobrepasada ? " " + Pintar(Nivel.Aviso, "(pasado)") : "") + "\n" +
                   $"<b>PROA</b>  {(nav.Orientacion > 0 ? "▶" : "◀")} {TextoOrientacion()} · Espacio ahora <b>{EfectoEspacio()}</b>{giro}\n" +
                   $"<b>MOTOR</b>  {motor}{grav}\n" +
                   $"<b>LLEGADA</b>  {llegada}\n" +
                   $"<b>COMBUSTIBLE</b>  {Combustible:0.0} kg · mínimo para llegar {CombustibleNecesarioLlegadaKg:0.0} kg · " +
                   Pintar(nm, $"margen {margen:0.0} kg") + "\n" +
                   "<size=14>Espacio (mantener): motor principal, empuja hacia la proa · R: girar 180° en " +
                   $"{c.duracionGiroS:0} s, sin combustible · WASD: laterales · Ruta comprimida de DEMOSTRACIÓN</size></size>";
        }

        /// <summary>Bloque de navegación del panel de Pilotaje en la llegada.</summary>
        string TextoNavegacionLlegada()
        {
            float salir = nav.SegundosHastaSalirZona;
            return $"<b>ZONA</b>  a {nav.DistanciaAlDestinoM:+0;-0} m del centro · {Mathf.Abs(nav.VelocidadMS):0.0} m/s · " +
                   (float.IsInfinity(salir) ? "detenida" : Pintar(salir < 15f ? Nivel.Aviso : Nivel.Normal, $"sales de la zona en ≈ {salir:0} s")) + "\n" +
                   "<size=15>La inserción es el frenado de captura: no depende de lo que ya frenaste con el motor principal " +
                   $"(eso se cobró aparte: {EstadoViaje.CombustibleMotorPrincipalKg:0.0} kg). Si sales de la zona, vuelves al crucero: " +
                   "frena con Espacio y vuelve a entrar." +
                   (nav.GravedadActiva ? " Con gravedad, la nave se acelera hacia el centro; la captura cuenta la velocidad equivalente en el borde." : "") +
                   "</size>\n\n";
        }

        string InstruccionNavegacion()
        {
            string a = AccionNavegacion(out Nivel n);
            return n == Nivel.Normal ? null : a;
        }

        // Avisos de una vez (se rearman al recuperarse): frenado y margen de combustible.
        bool avisoFrenarYa, avisoMargen30, avisoMargen10;

        void AvisosNavegacion()
        {
            bool frenar = nav.SeAcerca && !nav.VelocidadDeCaptura && !nav.EnZona &&
                          nav.DistanciaAZonaM <= nav.DistanciaNecesariaParaEntrarM(AceleracionMS2) * 1.15f;
            if (frenar && !avisoFrenarYa)
                Avisar($"MOMENTO DE FRENAR: {(nav.MotorFrena ? "" : "gira con R y ")}mantén Espacio en Pilotaje para entrar en la zona a ≤ {nc.velocidadCapturaMS:0} m/s.", 4f);
            avisoFrenarYa = frenar;

            float margen = Combustible - CombustibleNecesarioLlegadaKg;
            if (margen < 30f && !avisoMargen30)
                Avisar($"COMBUSTIBLE: quedan {margen:0} kg por encima del mínimo para llegar. Acelerar más reduce el margen.", 4f);
            if (margen < 10f && !avisoMargen10)
                Avisar($"COMBUSTIBLE CRÍTICO: margen de {margen:0.0} kg. Si baja de 0, la llegada será imposible y la misión se abortará.", 5f);
            if (margen < 30f) avisoMargen30 = true; else if (margen > 35f) avisoMargen30 = false;
            if (margen < 10f) avisoMargen10 = true; else if (margen > 15f) avisoMargen10 = false;
        }

        void IrAExploracion()
        {
            if (terminado) return;
            terminado = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(escenaExploracion);
        }

        // ------------------------------------------------------------------ Puestos

        Vector2 Pilotaje(Keyboard k)
        {
            switch (Fase)
            {
                case FaseNave.Preparacion:
                    if (k.spaceKey.wasPressedThisFrame)
                    {
                        if (chkComunicaciones && chkIngenieria && chkCiencia)
                        {
                            jugador.AsientoBloqueado = true;
                            CambiarFase(FaseNave.CuentaAtras);
                            Avisar("Cuenta atrás iniciada.", 2f);
                        }
                        else Avisar("Faltan comprobaciones: " + Faltan(), 3f);
                    }
                    break;

                case FaseNave.Ascenso:
                    if (Etapa == EstadoEtapa.Pendiente && k.spaceKey.wasPressedThisFrame)
                        Separar(TiempoFase < VentanaEtapaInicioS ? EstadoEtapa.Temprana : EstadoEtapa.Correcta);
                    break;

                case FaseNave.Crucero:
                    if (NavActiva)
                    {
                        if (esperarSoltarEspacioMotor && !k.spaceKey.isPressed) esperarSoltarEspacioMotor = false;
                        espacioPilotaje = k.spaceKey.isPressed;
                        motorPedido = espacioPilotaje && !esperarSoltarEspacioMotor;
                        giroPedido = k.rKey.wasPressedThisFrame;
                    }
                    Vector2 e = Vector2.zero;
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) e.y += 1f;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) e.y -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) e.x += 1f;
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) e.x -= 1f;
                    return Vector2.ClampMagnitude(e, 1f);

                case FaseNave.Llegada:
                    if (!InsercionDisponible) break;
                    tiempoInsercion += Time.deltaTime;
                    MarcadorInsercion = 0.5f + 0.5f * Mathf.Sin(tiempoInsercion * Mathf.PI * 2f / PeriodoInsercionS);
                    // Con navegación, el Espacio que venía del motor principal no cuenta como orden de inserción: hay que soltarlo antes.
                    if (esperarSoltarEspacioInsercion) { if (!k.spaceKey.isPressed) esperarSoltarEspacioInsercion = false; break; }
                    if (k.spaceKey.wasPressedThisFrame) Insertar();
                    break;
            }
            return Vector2.zero;
        }

        void Ingenieria(Keyboard k)
        {
            if (Fase == FaseNave.Preparacion && !chkIngenieria && k.spaceKey.wasPressedThisFrame)
            {
                chkIngenieria = true;
                Avisar("Ingeniería: sistemas de la nave comprobados.", 2.5f);
                return;
            }

            if (Averia != EstadoAveria.Activa) return;

            int tecla = -1;                       // 0,1,2 = circuitos · 3 = R · 4 = Espacio
            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) tecla = 0;
            else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) tecla = 1;
            else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) tecla = 2;
            else if (k.rKey.wasPressedThisFrame) tecla = 3;
            else if (k.spaceKey.wasPressedThisFrame) tecla = 4;
            if (tecla < 0) return;

            bool correcto =
                (PasoAveria == 0 && tecla == CircuitoAveriado) ||
                (PasoAveria == 1 && tecla == 3) ||
                (PasoAveria == 2 && tecla == 4);

            if (!correcto)
            {
                EnergiaNave = Mathf.Max(0f, EnergiaNave - catalogoNave.perdidaErrorAveriaWh);
                EstadoViaje.ErroresAveria++;
                Avisar($"Paso equivocado: chispazo, se pierden {catalogoNave.perdidaErrorAveriaWh:0} Wh. Lee el procedimiento.", 3f);
                sacudidaImpacto = 0.4f;
                return;
            }

            PasoAveria++;
            if (PasoAveria >= 3)
            {
                Averia = EstadoAveria.Resuelta;
                Avisar($"Avería reparada en {Time.time - inicioAveria:0} s.", 3.5f);
            }
        }

        void Comunicaciones(Keyboard k)
        {
            Mensaje m = MensajePendiente;
            if (m != null)
            {
                // Con un mensaje pendiente, Espacio SOLO confirma; para enviar datos hay que soltar y volver a pulsar.
                if (ObsActiva) esperarSoltarEspacioComs = true;
                if (!k.spaceKey.wasPressedThisFrame) return;
                m.confirmado = true;
                if (m.titulo == TituloPlan) chkComunicaciones = true;
                Avisar("Recepción confirmada: " + m.titulo + ".", 2.5f);
                return;
            }

            if (!ObsActiva) return;
            if (esperarSoltarEspacioComs)
            {
                if (!k.spaceKey.isPressed) esperarSoltarEspacioComs = false;
                return;
            }
            if (Fase == FaseNave.Crucero && k.spaceKey.isPressed && obs.PuedeEnviar)
            {
                obs.Enviar(Time.deltaTime, EscalaObservacion);
                if (!obs.PuedeEnviar) Avisar($"Datos enviados: {UD(obs.Enviados)} UD en total. Llegan al centro de operaciones.", 3f);
            }
        }

        void Ciencia(Keyboard k)
        {
            // Observación: mantener Espacio durante la ventana (en crucero Espacio no tiene otro uso en Ciencia).
            if (ObsActiva && Fase == FaseNave.Crucero && k.spaceKey.isPressed && obs.PuedeCapturar(Progreso))
            {
                EnergiaNave = Mathf.Max(0f, EnergiaNave - obs.Capturar(Time.deltaTime, EscalaObservacion, Progreso, EnergiaNave));
                if (!obs.PuedeCapturar(Progreso) && obs.Ventana(Progreso) == ObservacionAproximacion.EstadoVentana.Abierta)
                    Avisar($"Captura completa: {UD(obs.Capturados)} UD. Envíalas desde Comunicaciones antes de llegar.", 3.5f);
            }

            if (!k.spaceKey.wasPressedThisFrame) return;

            if (Fase == FaseNave.Preparacion && !chkCiencia)
            {
                chkCiencia = true;
                Avisar("Ciencia: sonda comprobada y asegurada.", 2.5f);
            }
            else if (Fase == FaseNave.Orbita && !terminarAlLlegar)
            {
                if (MensajePendiente != null)
                    Avisar("Primero confirma la autorización en Comunicaciones.", 2.5f);
                else
                {
                    sondaABordo = false;
                    exterior.LanzarSonda();
                    jugador.AsientoBloqueado = true;
                    CambiarFase(FaseNave.Despliegue);
                    Avisar("Sonda desplegada. Pasas a controlar la sonda…", 4f);
                }
            }
        }

        // ------------------------------------------------------------------ Mecánicas

        void Maniobrar(Vector2 entrada, float dt)
        {
            float acel = AceleracionMS2;
            Vector2 empuje = entrada;

            // Sin mando, el estabilizador frena la deriva lateral (también gasta combustible).
            if (empuje.sqrMagnitude < 0.0001f)
            {
                float v = velocidadLateral.magnitude;
                if (v > 0.01f && acel > 0f)
                    empuje = -velocidadLateral.normalized * Mathf.Min(1f, v / (acel * dt));
            }

            if (Combustible <= 0f) empuje = Vector2.zero;

            float fraccion = empuje.magnitude;
            velocidadLateral += empuje * acel * dt;
            float gasto = Mathf.Min(Combustible, fraccion * Nave.consumoKgS * dt);
            Combustible -= gasto;
            EstadoViaje.CombustibleManiobrasKg += gasto;
            ConsumoActualKgS = dt > 0f ? gasto / dt : 0f;
            if (VentanaCorreccionAbierta) corredor.CombustibleKg += gasto;   // ya incluido en maniobras: solo se mide

            desplazamiento += velocidadLateral * dt;
            float lim = catalogoNave.limiteDesplazamientoM;
            if (Mathf.Abs(desplazamiento.x) > lim) { desplazamiento.x = Mathf.Sign(desplazamiento.x) * lim; velocidadLateral.x = 0f; }
            if (Mathf.Abs(desplazamiento.y) > lim) { desplazamiento.y = Mathf.Sign(desplazamiento.y) * lim; velocidadLateral.y = 0f; }
            exterior.Desplazamiento = desplazamiento;
        }

        /// <summary>
        /// Separación de la primera etapa (procedimiento simplificado del juego). Se resuelve una
        /// sola vez; siempre se continúa. Solo afecta a la evaluación del lanzamiento (puntos de viaje).
        /// </summary>
        void Separar(EstadoEtapa resultado)
        {
            if (Etapa != EstadoEtapa.Pendiente) return;
            Etapa = resultado;
            sacudidaImpacto = 0.9f;
            EstadoViaje.PuntosEtapa = resultado == EstadoEtapa.Correcta ? puntosSeparacion : 0;
            EstadoViaje.ResultadoEtapa = TextoResultadoEtapa();
            Avisar(EstadoViaje.ResultadoEtapa, 5f);
        }

        public string TextoResultadoEtapa()
        {
            switch (Etapa)
            {
                case EstadoEtapa.Correcta:
                    return $"Separación a tiempo: la primera etapa se suelta agotada y la segunda continúa (+{puntosSeparacion}).";
                case EstadoEtapa.Temprana:
                    return "Separación temprana: la primera etapa aún tenía combustible y se desaprovechó su empuje. El ordenador corrigió el ascenso (0).";
                case EstadoEtapa.Automatica:
                    return "Sin orden a tiempo: el ordenador separó la etapa tarde, arrastrando un depósito vacío más de lo necesario (0).";
                default:
                    return "";
            }
        }

        void RegistrarEscombro(bool impacto)
        {
            if (Fase == FaseNave.Abortada) return;
            if (impacto)
            {
                Integridad = Mathf.Max(0f, Integridad - catalogoNave.danoPorImpacto);
                EstadoViaje.Impactos++;
                FlashImpacto = 1f;
                sacudidaImpacto = 1.5f;
                Avisar($"¡IMPACTO! Integridad del casco: {Integridad:0} %", 2.5f);
            }
            else EstadoViaje.EscombrosEsquivados++;
        }

        void Insertar()
        {
            float error = Mathf.Abs(MarcadorInsercion - 0.5f);
            float coste = catalogoNave.CosteInsercion(MasaActualKg, error);
            EstadoViaje.ErrorInsercion = error;

            EstadoViaje.CombustibleAntesInsercionKg = Combustible;
            Debug.Log($"[Combustible] Antes de la inserción: {Combustible:0.0} kg · coste {coste:0.0} kg (error {error:0.000}).");
            if (coste > Combustible)
            {
                EstadoViaje.CombustibleInsercionKg = Combustible;
                Combustible = 0f;
                Abortar($"no quedaba combustible para frenar: hacían falta {coste:0} kg y quedaban {EstadoViaje.CombustibleInsercionKg:0} kg.");
                return;
            }

            Combustible -= coste;
            EstadoViaje.CombustibleInsercionKg = coste;
            Debug.Log($"[Combustible] Después de la inserción: {Combustible:0.0} kg.");
            exterior.Orbita();
            CambiarFase(FaseNave.Orbita);
            Evaluar(error);
            string calidad = error <= catalogoNave.zonaVerdeInsercion ? "frenada precisa" : "frenada imprecisa";
            Avisar($"En órbita ({calidad}): {coste:0} kg de combustible. Evaluación del viaje en Comunicaciones.", 4f);
            Enviar(TituloEvaluacion, TextoEvaluacion());
            if (terminarAlLlegar)
                Enviar(TituloObjetivo,
                    $"Órbita estable sobre {nombreDestino}. Objetivo de la misión tripulada cumplido.\n" +
                    "Confirmad para cerrar la misión y generar el informe. La exploración con la sonda queda para una fase posterior.");
            else
                Enviar(TituloDespliegue,
                    "Órbita estable confirmada. Desplegad la sonda desde el puesto de Ciencia. " +
                    "La sonda funcionará con su propia batería.");
        }

        /// <summary>
        /// Evaluación del viaje, una sola vez, al entrar en órbita (antes de desplegar la sonda).
        /// Cada acción se mide con una sola medida. La frenada no da puntos: solo cambia la carga
        /// inicial de la sonda (regla simplificada, como máximo un 10 %).
        /// </summary>
        void Evaluar(float errorInsercion)
        {
            if (EstadoViaje.Evaluado) return;
            float energiaInicial = Mathf.Max(1f, Nave.energiaWh);
            // La energía gastada en la observación no cuenta como pérdida de la avería (no resta puntos de reparación).
            float energiaObs = ObsActiva ? obs.EnergiaUsadaWh : 0f;
            EstadoViaje.PerdidaEnergiaNave = Mathf.Clamp01((Nave.energiaWh - EnergiaNave - energiaObs) / energiaInicial);
            EstadoViaje.PuntosReparacion = catalogoNave.PuntosReparacion(EstadoViaje.PerdidaEnergiaNave);
            EstadoViaje.PuntosEscombros = catalogoNave.PuntosEscombros(EstadoViaje.Impactos);
            // PuntosEtapa ya se fijó en el despegue (Separar); aquí no se modifica.
            EstadoViaje.ReduccionCargaSonda = catalogoNave.ReduccionCargaSonda(errorInsercion);
            EstadoViaje.Evaluado = true;
        }

        /// <summary>Carga con la que empezará la sonda (Wh), según la evaluación.</summary>
        public float CargaPrevistaSondaWh => Mision != null ? Mision.energiaWh * (1f - EstadoViaje.ReduccionCargaSonda) : 0f;

        string TextoEstadoSonda()
        {
            if (Mision == null) return "";
            float cap = Mision.energiaWh;
            float carga = CargaPrevistaSondaWh;
            if (EstadoViaje.ReduccionCargaSonda <= 0f)
                return $"Carga inicial de la sonda: <b>{carga:0}/{cap:0} Wh</b> (frenada precisa: descenso sin demoras).";
            return $"Carga inicial de la sonda: <b>{carga:0}/{cap:0} Wh</b> (−{cap - carga:0.#} Wh, {EstadoViaje.ReduccionCargaSonda * 100f:0} %).\n" +
                   "<size=15>Regla simplificada: la frenada imprecisa dejó la órbita más lejos del punto de acceso; el descenso " +
                   "se prolonga y la sonda gasta batería en mantenerse caliente y orientada. La capacidad máxima no cambia " +
                   "y la estación recarga con normalidad. No resta puntos.</size>";
        }

        string TextoEvaluacion()
        {
            string etapa = EstadoViaje.PuntosEtapa < 0 ? "no evaluada (no puntúa)" : $"+{EstadoViaje.PuntosEtapa}";
            return "Evaluación del viaje (DEMOSTRACIÓN):\n" +
                   $"• Separación de etapa: {etapa}\n" +
                   $"• Reparación de la avería: {EstadoViaje.PerdidaEnergiaNave * 100f:0} % de energía perdida → +{EstadoViaje.PuntosReparacion}\n" +
                   $"• Escombros: {EstadoViaje.Impactos} impactos → +{EstadoViaje.PuntosEscombros}\n" +
                   $"<b>Puntos de viaje: {EstadoViaje.PuntosViaje}</b>" +
                   (ObsActiva ? $"\nBonus de observación (aparte, no suma): {obs.Bonus} de {obs.Configuracion.puntosMaximos} · enviadas {UD(obs.Enviados)} UD" : "") +
                   (terminarAlLlegar ? "" : "\n\n" + TextoEstadoSonda());
        }

        void Abortar(string motivo)
        {
            MotivoAborto = motivo;
            jugador.AsientoBloqueado = true;
            CambiarFase(FaseNave.Abortada);
            Avisar("MISIÓN ABORTADA", 5f);
        }

        void CerrarViaje(bool abortado)
        {
            EstadoViaje.Realizado = true;
            EstadoViaje.Abortado = abortado;
            EstadoViaje.MotivoAborto = abortado ? MotivoAborto : "";
            EstadoViaje.CombustibleFinalKg = Combustible;
            if (NavActiva)
            {
                float esperado = EstadoViaje.CombustibleInicialKg - EstadoViaje.CombustibleManiobrasKg - EstadoViaje.CombustibleMotorPrincipalKg - EstadoViaje.CombustibleInsercionKg;
                Debug.Log($"[Combustible] Cierre: inicial {EstadoViaje.CombustibleInicialKg:0.0} − maniobras {EstadoViaje.CombustibleManiobrasKg:0.0} " +
                          $"− motor principal {EstadoViaje.CombustibleMotorPrincipalKg:0.0} − inserción {EstadoViaje.CombustibleInsercionKg:0.0} = {esperado:0.0} kg; " +
                          $"final {Combustible:0.0} kg (diferencia {esperado - Combustible:0.000} kg; debe ser 0).");
                EstadoViaje.VelocidadMaximaMS = nav.VelocidadMaximaMS;
                EstadoViaje.DeltaVMotorMS = nav.DeltaVMotorTotalMS;
                EstadoViaje.DeltaVGravedadMS = nav.DeltaVGravedadTotalMS;
            }
            float cuadre = EstadoViaje.CombustibleInicialKg - EstadoViaje.CombustibleManiobrasKg - EstadoViaje.CombustibleInsercionKg - Combustible;
            if (!NavActiva) Debug.Log($"[Combustible] Cierre: inicial {EstadoViaje.CombustibleInicialKg:0.0} − maniobras {EstadoViaje.CombustibleManiobrasKg:0.0} " +
                      $"− inserción {EstadoViaje.CombustibleInsercionKg:0.0} = {EstadoViaje.CombustibleInicialKg - EstadoViaje.CombustibleManiobrasKg - EstadoViaje.CombustibleInsercionKg:0.0} kg; " +
                      $"final {Combustible:0.0} kg (diferencia {cuadre:0.000} kg; debe ser 0).");
            EstadoViaje.IntegridadFinal = Integridad;
            EstadoViaje.EnergiaFinalWh = EnergiaNave;
            if (CorActiva)
            {
                EstadoViaje.CorreccionActiva = true;
                EstadoViaje.CorreccionAbierta = corredor.EstadoActual != CorredorAproximacion.Estado.Pendiente;
                EstadoViaje.CorreccionResultado = corredor.NombreResultado();
                EstadoViaje.CorreccionSegundos = corredor.SegundosAlCompletar;
                EstadoViaje.CorreccionDuracionS = corredor.DuracionVentanaS;
                EstadoViaje.CorreccionCombustibleKg = corredor.CombustibleKg;
                EstadoViaje.PeriodoInsercionS = PeriodoInsercionS;
                EstadoViaje.PeriodoInsercionBaseS = corredor.Configuracion.periodoSinCorregirS;
            }
            if (ObsActiva)
            {
                obs.Finalizar();
                EstadoViaje.ObservacionActiva = true;
                EstadoViaje.ObservacionCapturadaUD = obs.Capturados;
                EstadoViaje.ObservacionEnviadaUD = obs.Enviados;
                EstadoViaje.ObservacionMaximaUD = obs.Configuracion.datosMaximos;
                EstadoViaje.EnergiaObservacionWh = obs.EnergiaUsadaWh;
                EstadoViaje.BonoObservacion = obs.Bonus;
                EstadoViaje.BonoObservacionMax = obs.Configuracion.puntosMaximos;
            }
        }

        InformeMision CrearInformeAborto()
        {
            if (terminarAlLlegar) return CrearInformeTripulado(MotivoFin.ViajeAbortado);
            InformeMision inf = new InformeMision
            {
                motivo = MotivoFin.ViajeAbortado,
                eleccion = MisionActual.Eleccion,
                segundosMision = 0f
            };
            inf.CopiarConfiguracion(Mision, catalogoMision);
            inf.CopiarViaje();
            inf.Completar();
            return inf;
        }

        /// <summary>Informe de la misión tripulada: se evalúa la llegada, no los escaneos.</summary>
        InformeMision CrearInformeTripulado(MotivoFin motivo)
        {
            InformeMision inf = new InformeMision
            {
                motivo = motivo,
                eleccion = MisionActual.Eleccion,
                segundosMision = 0f,
                misionTripulada = true,
                destino = nombreDestino,
                bonoLlegadaConfigurado = puntosLlegada
            };
            inf.CopiarConfiguracion(Mision, catalogoMision);
            inf.CopiarViaje();
            inf.Completar();
            return inf;
        }

        // ------------------------------------------------------------------ Retroalimentación visible

        void ActualizarIndicadores()
        {
            if (puestos == null) return;
            bool preparacion = Fase == FaseNave.Preparacion;
            Mensaje pendiente = MensajePendiente;

            foreach (PuestoNave p in puestos)
            {
                if (p == null) continue;
                EstadoPuesto e = EstadoPuesto.Correcto;
                switch (p.Tipo)
                {
                    case TipoPuesto.Pilotaje:
                        if (Escombros == EstadoEscombros.Aviso || Escombros == EstadoEscombros.Activo) e = EstadoPuesto.Alarma;
                        else if (preparacion) e = Faltan().Length == 0 ? EstadoPuesto.Pendiente : EstadoPuesto.Inactivo;
                        else if (InsercionDisponible) e = EstadoPuesto.Pendiente;
                        else if (VentanaCorreccionAbierta && Fase == FaseNave.Crucero) e = EstadoPuesto.Pendiente;
                        break;
                    case TipoPuesto.Ingenieria:
                        if (Averia == EstadoAveria.Activa) e = EstadoPuesto.Alarma;
                        else if (preparacion && !chkIngenieria) e = EstadoPuesto.Pendiente;
                        break;
                    case TipoPuesto.Comunicaciones:
                        if (pendiente != null) e = EstadoPuesto.Pendiente;
                        else if (ObsActiva && Fase == FaseNave.Crucero && obs.PuedeEnviar) e = EstadoPuesto.Pendiente;
                        break;
                    case TipoPuesto.Ciencia:
                        if (preparacion && !chkCiencia) e = EstadoPuesto.Pendiente;
                        else if (ObsActiva && Fase == FaseNave.Crucero && obs.PuedeCapturar(Progreso)) e = EstadoPuesto.Pendiente;
                        else if (Fase == FaseNave.Orbita && pendiente == null && !terminarAlLlegar) e = EstadoPuesto.Pendiente;
                        break;
                }
                if (Fase == FaseNave.Abortada) e = EstadoPuesto.Alarma;
                p.Estado = e;
            }
        }

        void ActualizarLuces()
        {
            if (lucesCabina == null || coloresLuces == null) return;
            bool alarma = Averia == EstadoAveria.Activa || Fase == FaseNave.Abortada;
            bool alerta = Escombros == EstadoEscombros.Aviso || Escombros == EstadoEscombros.Activo;
            float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);

            for (int i = 0; i < lucesCabina.Length; i++)
            {
                Light l = lucesCabina[i];
                if (l == null) continue;
                if (alarma)
                {
                    l.color = new Color(1f, 0.12f, 0.1f);
                    l.intensity = 0.6f + 1.6f * (UnityEngine.Random.value < 0.08f ? 0f : pulso);
                }
                else if (alerta)
                {
                    l.color = Color.Lerp(coloresLuces[i], new Color(1f, 0.6f, 0.1f), pulso);
                    l.intensity = 2.2f;
                }
                else
                {
                    l.color = coloresLuces[i];
                    l.intensity = 2.5f;
                }
            }
        }

        void ActualizarPalancas()
        {
            if (materialesPalancas == null) return;
            bool parpadeo = Mathf.Repeat(Time.time * 3f, 1f) < 0.5f;
            for (int i = 0; i < materialesPalancas.Length; i++)
            {
                Material m = materialesPalancas[i];
                if (m == null) continue;
                Color c = new Color(0.2f, 0.9f, 0.35f);                         // normal
                if (Averia == EstadoAveria.Activa && i == CircuitoAveriado)
                {
                    if (PasoAveria == 0) c = parpadeo ? new Color(1f, 0.1f, 0.1f) : new Color(0.3f, 0.02f, 0.02f);  // sobrecarga
                    else if (PasoAveria == 1) c = new Color(1f, 0.65f, 0.1f);    // aislado
                    else c = new Color(0.2f, 0.55f, 1f);                         // con línea de reserva
                }
                m.SetColor("_BaseColor", c);
            }
        }

        // ------------------------------------------------------------------ Textos para el HUD

        public string Faltan()
        {
            string t = "";
            if (!chkComunicaciones) t += "Comunicaciones";
            if (!chkIngenieria) t += (t.Length > 0 ? ", " : "") + "Ingeniería";
            if (!chkCiencia) t += (t.Length > 0 ? ", " : "") + "Ciencia";
            return t;
        }

        public string NombreFase()
        {
            switch (Fase)
            {
                case FaseNave.Preparacion: return "Preparación";
                case FaseNave.CuentaAtras: return "Cuenta atrás";
                case FaseNave.Ascenso: return "Despegue";
                case FaseNave.Crucero: return "Viaje";
                case FaseNave.Llegada: return "Llegada";
                case FaseNave.Orbita: return "En órbita";
                case FaseNave.Despliegue: return "Despliegue de la sonda";
                default: return "Abortada";
            }
        }

        /// <summary>Qué debe hacer el jugador ahora.</summary>
        public string Instruccion()
        {
            Mensaje pendiente = MensajePendiente;
            switch (Fase)
            {
                case FaseNave.Preparacion:
                    string f = Faltan();
                    return f.Length > 0
                        ? "Comprueba los puestos con luz ámbar: " + f + ". Después, despega desde Pilotaje."
                        : "Todo comprobado: ve a Pilotaje e inicia la cuenta atrás.";
                case FaseNave.CuentaAtras:
                    return "Cuenta atrás. Prepárate para separar la etapa durante el ascenso.";
                case FaseNave.Ascenso:
                    return Etapa == EstadoEtapa.Pendiente
                        ? "Pilotaje: pulsa Espacio cuando el marcador entre en la zona verde."
                        : "Despegue en curso. Mira por la ventana.";
                case FaseNave.Crucero:
                    if (Averia == EstadoAveria.Activa) return "AVERÍA: ve al puesto de Ingeniería y sigue el procedimiento.";
                    if (Escombros == EstadoEscombros.Aviso || Escombros == EstadoEscombros.Activo)
                        return "ESCOMBROS: ve a Pilotaje y esquívalos con WASD.";
                    string navUrgente = NavActiva ? InstruccionNavegacion() : null;
                    if (navUrgente != null) return navUrgente;
                    if (VentanaCorreccionAbierta)
                        return $"CORRECCIÓN DE TRAYECTORIA (opcional): Pilotaje, centra el anillo con WASD · quedan {corredor.SegundosRestantes:0} s" +
                               (ObsActiva && obs.PuedeEnviar ? $" · {UD(obs.Pendientes)} UD de observación aún sin enviar" : "");
                    string o = ObsActiva ? InstruccionObservacion() : null;
                    if (o == null && NavActiva)
                        return $"Navega desde Pilotaje: Espacio motor principal, R girar. Destino a {nav.DistanciaAlDestinoM:0} m, {Mathf.Abs(nav.VelocidadMS):0.0} m/s.";
                    return o ?? "Viaje en curso. Puedes recorrer la cabina y mirar por la ventana.";
                case FaseNave.Llegada:
                    return pendiente != null ? "Comunicaciones: confirma la autorización de inserción."
                                             : "Pilotaje: frena en el momento justo para entrar en órbita.";
                case FaseNave.Orbita:
                    if (pendiente != null) return "Comunicaciones: lee y confirma: " + pendiente.titulo + ".";
                    return terminarAlLlegar ? "OBJETIVO ALCANZADO. Generando el informe de la misión…" : "Ciencia: despliega la sonda.";
                case FaseNave.Despliegue:
                    return "Sonda desplegada. Pasando al control de la sonda…";
                default:
                    return "MISIÓN ABORTADA: " + MotivoAborto;
            }
        }

        /// <summary>Texto del panel del puesto en uso.</summary>
        public string TextoPuesto(TipoPuesto tipo)
        {
            switch (tipo)
            {
                case TipoPuesto.Pilotaje: return TextoPilotaje();
                case TipoPuesto.Ingenieria: return TextoIngenieria();
                case TipoPuesto.Comunicaciones: return TextoComunicaciones();
                default: return TextoCiencia();
            }
        }

        string TextoPilotaje()
        {
            switch (Fase)
            {
                case FaseNave.Preparacion:
                    string f = Faltan();
                    return f.Length > 0
                        ? $"Antes de despegar faltan comprobaciones en: <b>{f}</b>.\nCada puesto con luz ámbar tiene una tarea."
                        : "Todo comprobado.\n\n<b>Espacio: iniciar la cuenta atrás.</b>\nDurante el despegue permanecerás en el asiento.\n\n" + TextoProcedimientoEtapa();
                case FaseNave.CuentaAtras:
                    return TextoLanzador() + "\n\n" + TextoProcedimientoEtapa();
                case FaseNave.Ascenso:
                    if (Etapa == EstadoEtapa.Pendiente)
                        return TextoLanzador() + "\n\n" + (EtapaEnVentana
                            ? "<color=#7CFC9A><b>¡VENTANA ABIERTA! Espacio: separar la etapa.</b></color>"
                            : "<color=#FFB040><b>Espera a que el marcador entre en la zona verde.</b></color>") +
                            "\n<size=15>Procedimiento simplificado del juego.</size>";
                    return TextoLanzador() + "\n\n" + TextoResultadoEtapa();
                case FaseNave.Crucero:
                    string esc = Escombros == EstadoEscombros.Aviso
                        ? $"<color=#FFB040><b>Escombros en {Mathf.Max(0f, T.avisoEscombrosS - TiempoEscombros):0} s</b></color>\n"
                        : Escombros == EstadoEscombros.Activo || Escombros == EstadoEscombros.Despejando
                            ? "<color=#FF6B5B><b>¡ESCOMBROS! Mantén el centro de la mira libre de rocas.</b></color>\n"
                            : "";
                    if (NavActiva)
                        return esc + TextoNavegacionCrucero() + "\n\n" + (VentanaCorreccionAbierta ? TextoCorreccion() + "\n\n" : "") +
                               $"<size=15><b>LATERALES</b> (WASD) {desplazamiento.x:+0.0;-0.0} / {desplazamiento.y:+0.0;-0.0} m · " +
                               $"{velocidadLateral.magnitude:0.0} m/s · impactos {EstadoViaje.Impactos} · esquivados {EstadoViaje.EscombrosEsquivados}\n" +
                               TextoLibroCombustible() + "</size>";
                    return esc + (VentanaCorreccionAbierta ? TextoCorreccion() + "\n\n" : "") +
                           "<b>WASD / flechas:</b> propulsores laterales (gastan combustible).\n" +
                           "Al soltar, el estabilizador frena la nave, también con combustible: en el espacio no hay rozamiento que la detenga.\n\n" +
                           $"Aceleración = empuje / masa = {Nave.empujeN:0} N / {MasaActualKg:0} kg = <b>{AceleracionMS2:0.00} m/s²</b>\n" +
                           $"Desplazamiento: {desplazamiento.x:+0.0;-0.0} / {desplazamiento.y:+0.0;-0.0} m · velocidad {velocidadLateral.magnitude:0.0} m/s\n" +
                           TextoLibroCombustible() + "\n" +
                           $"Impactos: {EstadoViaje.Impactos} · esquivados: {EstadoViaje.EscombrosEsquivados}";
                case FaseNave.Llegada:
                    if (!InsercionDisponible) return (NavActiva ? TextoNavegacionLlegada() : "") + "Esperando la autorización de inserción (puesto de Comunicaciones).";
                    float perfecto = catalogoNave.CosteInsercion(MasaActualKg, 0f);
                    float peor = catalogoNave.CosteInsercion(MasaActualKg, 0.5f);
                    string riesgo = Combustible < peor
                        ? (Combustible < perfecto
                            ? "\n<color=#FF6B5B><b>No hay combustible ni para una frenada perfecta.</b></color>"
                            : "\n<color=#FFB040>Combustible justo: una frenada imprecisa podría dejarte sin él.</color>")
                        : "";
                    string corr = CorActiva
                        ? $"\nCorrección de trayectoria: <b>{corredor.NombreResultado()}</b> → periodo del marcador {PeriodoInsercionS:0.##} s " +
                          $"(sin corregir: {corredor.Configuracion.periodoSinCorregirS:0.##} s). El consumo depende de cuándo frenes."
                        : "";
                    return "<b>ENTRADA EN ÓRBITA</b>\nPara quedarte en órbita, la nave frena con el motor principal.\n" +
                           (NavActiva ? TextoNavegacionLlegada() : "") +
                           "<b>Espacio</b> cuando el marcador esté en la zona verde. Cuanto más lejos del centro, más combustible.\n\n" +
                           $"Coste: {perfecto:0.0} kg (perfecta) a {peor:0.0} kg (peor) · Combustible ahora: <b>{Combustible:0.0} kg</b>" + riesgo + corr;
                default:
                    return EstadoViaje.ErrorInsercion >= 0f
                        ? $"En órbita. Antes de frenar: {EstadoViaje.CombustibleAntesInsercionKg:0.0} kg → después: {Combustible:0.0} kg " +
                          $"(frenado {EstadoViaje.CombustibleInsercionKg:0.0} kg; maniobras del viaje {EstadoViaje.CombustibleManiobrasKg:0.0} kg)."
                        : "Sin maniobras pendientes.";
            }
        }

        string TextoLanzador()
        {
            if (Mision == null || Mision.lanzador == null) return "";
            return $"<b>Lanzador: {Mision.lanzador.nombre}</b>\n" +
                   $"Margen de carga configurable: {Mision.masaLanzamientoKg:0} / {Mision.capacidadLanzadorKg:0} kg usados " +
                   "<size=15>(el vehículo tripulado base va incluido)</size>\n" +
                   $"Margen sobrante → {Nave.combustibleExtraKg:0} kg de combustible extra (regla de demostración)\n" +
                   $"Motor: {NombreMotor()} · masa total de la nave al despegar: {Nave.masaInicialKg:0} kg";
        }

        string NombreMotor() => Nave != null && Nave.motor != null ? Nave.motor.nombre : "motor por defecto";

        string TextoMasas()
        {
            return $"Masa seca: {Nave.masaSecaKg:0} kg <size=15>(casco {Nave.masaCascoKg:0} + motor {Nave.masaMotorKg:0} + tanque vacío {Nave.masaTanqueKg:0})</size>\n" +
                   $"Carga científica (sonda + relevo): {Nave.cargaCientificaKg:0} kg\n" +
                   $"Combustible: {Nave.combustibleInicialKg:0} kg de {Nave.capacidadTanqueKg:0} kg del tanque " +
                   $"<size=15>({Nave.combustibleBaseKg:0} base + {Nave.combustibleExtraKg:0} extra" +
                   (Nave.combustibleNoCabeKg > 0f ? $"; {Nave.combustibleNoCabeKg:0} kg no caben en el tanque" : "") + ")</size>\n" +
                   $"<b>Masa total inicial: {Nave.masaInicialKg:0} kg</b>\n";
        }

        string TextoProcedimientoEtapa()
        {
            return "<b>Durante el ascenso:</b> cuando el marcador entre en la <color=#7CFC9A>zona verde</color>, " +
                   "pulsa <b>Espacio</b> para separar la primera etapa. La ventana dura al menos " +
                   $"{ventanaEtapaMinimaS:0.#} s. Si fallas, el ordenador la separa y el viaje continúa.\n" +
                   "<size=15>Procedimiento simplificado del juego: solo cuenta en la evaluación del lanzamiento.</size>";
        }

        string TextoIngenieria()
        {
            if (Averia == EstadoAveria.Activa)
            {
                string[] nombres = { "Soporte vital", "Propulsión", "Navegación" };
                string circuitos = "";
                for (int i = 0; i < 3; i++)
                {
                    bool mal = i == CircuitoAveriado;
                    string estado = !mal ? "<color=#7CFC9A>12 A · normal</color>"
                                  : PasoAveria == 0 ? "<color=#FF6B5B><b>38 A · SOBRECARGA</b></color>"
                                  : PasoAveria == 1 ? "<color=#FFB040>aislado</color>"
                                  : "<color=#66A8FF>en línea de reserva</color>";
                    circuitos += $"  {i + 1}. {nombres[i]}: {estado}\n";
                }
                string[] pasos =
                {
                    "Aísla el circuito con sobrecarga: pulsa su número (1, 2 o 3).\n<size=15>Aislar evita que la sobrecarga dañe el resto de sistemas.</size>",
                    "Conecta la línea de reserva: pulsa <b>R</b>.\n<size=15>La reserva alimenta el sistema mientras el circuito está aislado.</size>",
                    "Restablece el circuito: pulsa <b>Espacio</b>."
                };
                return "<color=#FF6B5B><b>AVERÍA ELÉCTRICA</b></color> · la nave pierde " +
                       $"{catalogoNave.perdidaAveriaWhPorS:0} Wh por segundo\n\n" + circuitos +
                       $"\n<b>Paso {PasoAveria + 1} de 3:</b> " + pasos[Mathf.Clamp(PasoAveria, 0, 2)] +
                       $"\n\nUn paso equivocado cuesta {catalogoNave.perdidaErrorAveriaWh:0} Wh. Energía: {EnergiaNave:0} Wh";
            }

            string datos =
                "<b>DATOS DE LA NAVE</b> (no de la sonda) · DEMOSTRACIÓN\n" +
                $"Motor: <b>{NombreMotor()}</b> · empuje {Nave.empujeN / 1000f:0} kN · consumo {Nave.consumoKgS:0.0} kg/s\n" +
                $"Tanque: <b>{(Nave.tanque != null ? Nave.tanque.nombre : "por defecto")}</b> · capacidad {Nave.capacidadTanqueKg:0} kg\n" +
                TextoMasas() +
                $"<size=15>Regla de demostración: el margen de carga que sobra en el lanzador ({Nave.margenLanzadorKg:0} kg) se llena con combustible extra, 1 kg por kg, hasta llenar el tanque.</size>\n" +
                $"Aceleración de maniobra = {Nave.empujeN:0} N / {Nave.masaInicialKg:0} kg = {Nave.aceleracionInicialMS2:0.00} m/s²\n" +
                $"Energía de la nave: {Nave.energiaWh:0} Wh <size=15>(la batería de la sonda es aparte)</size>\n";

            if (Fase == FaseNave.Preparacion)
                return datos + (chkIngenieria ? "\n<color=#7CFC9A>[OK] Sistemas comprobados.</color>" : "\n<b>Espacio: confirmar sistemas.</b>");

            string averia = Averia == EstadoAveria.Resuelta
                ? $"\nAvería reparada · duró {EstadoViaje.SegundosAveria:0} s · errores: {EstadoViaje.ErroresAveria}"
                : "";
            return $"Sistemas nominales. Energía actual: {EnergiaNave:0} / {Nave.energiaWh:0} Wh{averia}\n\n" + datos;
        }

        string TextoComunicaciones()
        {
            Mensaje m = MensajePendiente;
            if (m != null)
                return "<b>MENSAJE DEL CENTRO DE OPERACIONES</b>\n\n" +
                       $"<b>{m.titulo}</b>\n{m.texto}\n\n<b>Espacio: confirmar recepción.</b>";

            string historial = "";
            foreach (Mensaje x in mensajes) historial += $"<color=#7CFC9A>[OK]</color> {x.titulo}\n";
            string envio = ObsActiva && (Fase == FaseNave.Crucero || obs.Usada) ? TextoEnvioObservacion() + "\n\n" : "";
            return envio + "Sin mensajes pendientes.\n\n" + (historial.Length > 0 ? "Confirmados:\n" + historial : "");
        }

        string TextoCiencia()
        {
            string sonda =
                "<b>DATOS DE LA SONDA</b> (separados de la nave)\n" +
                $"Objetivo: {Mision.objetivo.nombre}\n" +
                $"Instrumento: {Mision.instrumento.nombre}\n" +
                $"Batería de la sonda: {Mision.energiaWh:0} Wh\n" +
                $"Masa de la sonda: {Mision.masaSondaKg:0} kg · relevo: {Mision.orbita.nombre}\n";

            switch (Fase)
            {
                case FaseNave.Preparacion:
                    return sonda + (chkCiencia ? "\n<color=#7CFC9A>[OK] Sonda comprobada.</color>" : "\n<b>Espacio: confirmar que la sonda está lista.</b>");
                case FaseNave.Orbita:
                    if (terminarAlLlegar)
                        return sonda + "\nLa sonda queda a bordo: la exploración es una fase posterior, fuera de esta demostración.";
                    return MensajePendiente != null
                        ? sonda + "\n" + TextoEstadoSonda() + "\n\nEsperando la autorización de despliegue (puesto de Comunicaciones)."
                        : sonda + "\n" + TextoEstadoSonda() + "\n\n<b>Espacio: desplegar la sonda.</b>\nDescenderá y explorará con su propia batería.";
                case FaseNave.Despliegue:
                    return "Sonda desplegada.";
                case FaseNave.Crucero:
                    return (ObsActiva ? TextoCapturaObservacion() + "\n\n" : "") + sonda + "\nSonda asegurada en la bahía.";
                default:
                    return sonda + "\nSonda asegurada en la bahía.";
            }
        }

        // ------------------------------------------------------------------ Observación de aproximación (textos y avisos)

        static int UD(float v) => Mathf.FloorToInt(v + 0.001f);

        void AvisosObservacion()
        {
            ObservacionAproximacion.EstadoVentana v = obs.Ventana(Progreso);
            if (v == ventanaAnterior) return;
            ventanaAnterior = v;
            // La avería y los escombros tienen prioridad en el aviso; la instrucción del HUD sigue indicando la ventana.
            bool urgente = Averia == EstadoAveria.Activa || Escombros == EstadoEscombros.Aviso || Escombros == EstadoEscombros.Activo;
            if (v == ObservacionAproximacion.EstadoVentana.Abierta && !urgente)
                Avisar($"Ventana de observación abierta ({Seg(obs.SegundosVentana(Progreso, SegundosViajeEquivalentes))} s). Ciencia: mantén Espacio para capturar datos.", 4f);
            else if (v == ObservacionAproximacion.EstadoVentana.Cerrada && !urgente)
                Avisar(obs.Pendientes > 0f
                    ? $"Ventana de observación cerrada. {UD(obs.Pendientes)} UD pendientes: envíalas desde Comunicaciones antes de llegar."
                    : "Ventana de observación cerrada.", 4f);
        }

        /// <summary>Segundos aproximados hasta la llegada (pueden alargarse si hay eventos sin resolver).
        /// Con navegación: hasta el borde de la zona a la velocidad actual (infinito si no se acerca).</summary>
        float SegundosHastaLlegada => NavActiva ? nav.SegundosAZona : Mathf.Max(0f, (1f - Progreso) * T.cruceroS);

        /// <summary>Segundos que costaría recorrer el viaje entero al ritmo actual (para estimar la ventana de observación).</summary>
        float SegundosViajeEquivalentes => !NavActiva ? T.cruceroS
            : nav.SeAcerca ? nav.L / nav.VelocidadDeAcercamientoMS : float.PositiveInfinity;

        /// <summary>Segundos para mostrar: «—» si no hay estimación (nave quieta o alejándose).</summary>
        static string Seg(float s) => float.IsInfinity(s) || float.IsNaN(s) || s > 3600f ? "—" : s.ToString("0");

        string InstruccionObservacion()
        {
            ObservacionAproximacion.EstadoVentana v = obs.Ventana(Progreso);
            if (v == ObservacionAproximacion.EstadoVentana.Abierta && obs.PuedeCapturar(Progreso))
                return $"OBSERVACIÓN: ventana abierta {Seg(obs.SegundosVentana(Progreso, SegundosViajeEquivalentes))} s más. Ciencia: mantén Espacio " +
                       $"({UD(obs.Capturados)}/{UD(obs.Configuracion.datosMaximos)} UD)." +
                       (obs.PuedeEnviar ? $" Pendientes de envío: {UD(obs.Pendientes)} UD." : "");
            if (obs.PuedeEnviar)
                return $"OBSERVACIÓN: {UD(obs.Pendientes)} UD pendientes de envío. Comunicaciones: mantén Espacio " +
                       $"antes de llegar (≈ {Seg(SegundosHastaLlegada)} s).";
            return null;
        }

        string TextoCapturaObservacion()
        {
            ObservacionAproximacion.Ajustes c = obs.Configuracion;
            string estado;
            switch (obs.Ventana(Progreso))
            {
                case ObservacionAproximacion.EstadoVentana.Pendiente:
                    estado = $"Ventana de observación en {Seg(obs.SegundosVentana(Progreso, SegundosViajeEquivalentes))} s.";
                    break;
                case ObservacionAproximacion.EstadoVentana.Abierta:
                    estado = obs.PuedeCapturar(Progreso)
                        ? $"<color=#7CFC9A><b>VENTANA ABIERTA · {Seg(obs.SegundosVentana(Progreso, SegundosViajeEquivalentes))} s</b></color>\n<b>Mantén Espacio: capturar datos.</b>"
                        : "Captura completa.";
                    break;
                default:
                    estado = "Ventana de observación cerrada.";
                    break;
            }
            return "<b>OBSERVACIÓN DE APROXIMACIÓN</b> · bonus aparte · DEMOSTRACIÓN\n" + estado + "\n" +
                   $"Capturadas: {UD(obs.Capturados)}/{UD(c.datosMaximos)} UD · pendientes de envío: {UD(obs.Pendientes)} UD\n" +
                   $"Coste: {c.costeWhPorUnidad:0} Wh de la nave por UD · usados {obs.EnergiaUsadaWh:0} Wh · energía {EnergiaNave:0}/{Nave.energiaWh:0} Wh\n" +
                   "<size=15>Solo cuentan los datos enviados desde Comunicaciones antes de llegar. La energía de la nave también " +
                   "la consume la avería: si llega a cero, la misión se aborta.</size>";
        }

        string TextoEnvioObservacion()
        {
            if (obs.Finalizada)
                return $"<b>OBSERVACIÓN</b>: enviadas {UD(obs.Enviados)} UD" + (obs.Perdidos > 0f ? $" · {UD(obs.Perdidos)} UD sin enviar (no cuentan)." : ".");
            if (obs.PuedeEnviar)
                return $"<b>OBSERVACIÓN</b> · <color=#FFB040><b>{UD(obs.Pendientes)} UD pendientes de envío</b></color>\n" +
                       $"<b>Mantén Espacio: enviar datos.</b> Tiempo hasta la llegada: ≈ {Seg(SegundosHastaLlegada)} s\n" +
                       $"Enviadas: {UD(obs.Enviados)} UD · bonus actual: {obs.Bonus} de {obs.Configuracion.puntosMaximos}";
            return obs.Usada
                ? $"<b>OBSERVACIÓN</b>: todo enviado ({UD(obs.Enviados)} UD) · bonus {obs.Bonus} de {obs.Configuracion.puntosMaximos}"
                : "<b>OBSERVACIÓN</b>: sin datos que enviar.";
        }

        // ------------------------------------------------------------------ Lectura del combustible

        /// <summary>Libro del combustible con un decimal: al partir, maniobras, ahora y consumo instantáneo.</summary>
        public string TextoLibroCombustible()
        {
            string tasa = ConsumoActualKgS > 0.05f ? $" · <color=#FFB040><b>▼ {ConsumoActualKgS:0.0} kg/s</b></color>" : "";
            if (NavActiva)
                return $"Combustible: al partir {EstadoViaje.CombustibleInicialKg:0.0} kg − maniobras {EstadoViaje.CombustibleManiobrasKg:0.0} kg " +
                       $"− motor principal {EstadoViaje.CombustibleMotorPrincipalKg:0.0} kg = <b>{Combustible:0.0} kg</b>{tasa}";
            return $"Combustible: al partir {EstadoViaje.CombustibleInicialKg:0.0} kg − maniobras {EstadoViaje.CombustibleManiobrasKg:0.0} kg " +
                   $"= <b>{Combustible:0.0} kg</b>{tasa}";
        }

        // ------------------------------------------------------------------ Corrección de trayectoria (textos y avisos)

        void AvisarFinCorreccion()
        {
            CorredorAproximacion.Ajustes c = corredor.Configuracion;
            if (corredor.Completada)
                Avisar($"Trayectoria {corredor.NombreResultado()} ({corredor.SegundosAlCompletar:0.0} s de {corredor.DuracionVentanaS:0} s): " +
                       $"el marcador de frenado irá más lento (periodo {PeriodoInsercionS:0.##} s en lugar de {c.periodoSinCorregirS:0.##} s).", 5f);
            else
                Avisar($"Ventana de corrección terminada sin corregir: el frenado será el de siempre (periodo {c.periodoSinCorregirS:0.##} s).", 4f);
        }

        string TextoCorreccion()
        {
            CorredorAproximacion.Ajustes c = corredor.Configuracion;
            float limiteATiempo = c.fraccionATiempo * corredor.DuracionVentanaS;
            bool aTiempo = corredor.TiempoVentanaS <= limiteATiempo;
            return "<b>CORRECCIÓN DE TRAYECTORIA</b> · opcional · DEMOSTRACIÓN\n" +
                   $"Quedan <b>{corredor.SegundosRestantes:0} s</b> · " +
                   (aTiempo ? $"«a tiempo» durante {limiteATiempo - corredor.TiempoVentanaS:0} s más" : "ya contaría como «tarde»") + "\n" +
                   "Centra el anillo en la mira con WASD (como con los escombros) y mantenlo dentro " +
                   $"{c.permanenciaS:0} s seguidos.\n" +
                   (corredor.Dentro
                       ? $"<color=#7CFC9A><b>DENTRO</b> · {corredor.PermanenciaActualS:0.0}/{c.permanenciaS:0.0} s</color>"
                       : $"<color=#FFB040>Distancia al corredor: {corredor.Error:0.0} m (tolerancia {c.toleranciaM:0.0} m)</color>") + "\n" +
                   $"Combustible de maniobra en esta ventana: {corredor.CombustibleKg:0.0} kg\n" +
                   $"<size=15>A tiempo: frenado con marcador más lento ({c.periodoATiempoS:0.##} s); tarde: {c.periodoTardeS:0.##} s; " +
                   $"sin corregir: {c.periodoSinCorregirS:0.##} s, como siempre. No da puntos.</size>";
        }
    }
}
