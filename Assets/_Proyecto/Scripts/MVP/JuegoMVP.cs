using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace PrototipoMision
{
    public enum TipoPunto { Pilotaje, Ciencia, Comunicaciones, Ingenieria, Mesa, Kits, Reparacion }

    /// <summary>Punto de interacción del interior (por cercanía; no añade colisiones ni cambia la geometría).</summary>
    public class PuntoInteraccion
    {
        public TipoPunto tipo;
        public string modulo, nombre;
        public Vector3 posicion;     // en el mundo (la EM-4X está construida en el origen)
    }

    /// <summary>
    /// MVP DE LA EXPEDICIÓN (escena ExpedicionMVP). Integrador: fases, modos, mandos y estado compartido.
    ///
    /// Flujo: preparación con presupuesto → mensajes de envío, instalación y lanzamiento → interior de la EM-4X →
    /// puestos (Pilotaje, Ciencia, Comunicaciones, Ingeniería, Bodega) → regreso → resultados.
    ///
    /// Reutiliza sin modificarlos: la EM-4X (PruebaExpedicion/VistaExpedicion), sus colisiones, las puertas con paneles
    /// (PuertasUnion), el personaje flotante y la cámara de inspección; el vuelo 3D (EstadoVuelo3D, AsistenteFrenado) y los
    /// catálogos. La lógica vive en EstadoExpedicion; este componente solo coordina. El interior y el vuelo son dos
    /// representaciones del MISMO estado: al dejar Pilotaje el vuelo sigue avanzando (motor apagado, ayudas activas).
    /// </summary>
    public class JuegoMVP : MonoBehaviour
    {
        [Tooltip("Catálogo de misión (presupuesto, instrumentos, relevos). Si falta se usan sus valores por defecto.")]
        [SerializeField] CatalogoMision catalogoMision;
        [Tooltip("Solo para que el ejecutable incluya estos sombreadores (la escena se construye al iniciar).")]
        [SerializeField] Material materialConLuz, materialSinLuz, materialTransparente;

        public enum Fase { Cargando, Preparacion, Transicion, Mision, Resultados }
        public enum Modo { Interior, Inspeccion, Pilotaje, Consola }

        public Fase FaseActual { get; private set; } = Fase.Cargando;
        public Modo ModoActual { get; private set; } = Modo.Interior;
        public TipoPunto ConsolaActual { get; private set; }
        public CatalogoMision Catalogo { get; private set; }
        public PreparacionExpedicion Borrador { get; private set; }
        public ResumenExpedicion Resumen { get; private set; }
        public EstadoExpedicion Estado { get; private set; }
        public PruebaExpedicion Expedicion { get; private set; }
        public PersonajeFlotante Personaje { get; private set; }
        public List<PuertasUnion> Uniones { get; } = new List<PuertasUnion>();
        public List<PuntoInteraccion> Puntos { get; } = new List<PuntoInteraccion>();
        public PuntoInteraccion PuntoCercano { get; private set; }
        public VistaVueloMVP VistaVuelo { get; private set; }
        public DetalleInterior Detalle { get; private set; }
        public PrediccionTrayectoria Prediccion { get; private set; }
        public int Aceleracion { get; private set; } = 1;
        public bool MapaVisible { get; private set; } = true;
        public bool VistaExterior { get; private set; } = true;
        public bool AyudaVisible { get; private set; } = true;
        public int Tarjeta { get; private set; }
        public string Aviso { get; private set; } = "";
        public bool AvisoVisible => Aviso.Length > 0 && Time.time < avisoHasta;
        public string TextoReparacion { get; private set; } = "";
        public bool RescatePedido { get; set; }

        static readonly int[] Aceleraciones = { 1, 4, 16 };
        static readonly Vector3 SalidaLocal = new Vector3(0f, 0.5f, 3.6f);
        float avisoHasta, acumulado, proximaPrediccion, reparacionHasta;
        AudioSource audioFuente;
        readonly Dictionary<string, AudioClip> sonidos = new Dictionary<string, AudioClip>();

        void Awake()
        {
            Catalogo = catalogoMision != null ? catalogoMision : ScriptableObject.CreateInstance<CatalogoMision>();
            Expedicion = gameObject.AddComponent<PruebaExpedicion>();
            gameObject.AddComponent<HudMVP>();
        }

        IEnumerator Start()
        {
            // Igual que CirculacionPrueba: esperar a que se construya la EM-4X y a que se retiren las colisiones decorativas.
            while (Expedicion.Vista == null || Expedicion.Vista.Nave == null) yield return null;
            yield return null;
            yield return new WaitForFixedUpdate();

            DefinicionNaveModular d = Expedicion.Definicion;
            DefinicionCirculacion circulacion = DefinicionCirculacion.AcordadaEM4X();
            ColisionesExpedicion.Anadir(Expedicion.Vista.Nave);
            Transform raiz = new GameObject("Circulación (puertas y paneles)").transform;
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (UnionConPuertas u in circulacion.uniones)
            {
                PuertasUnion pu = new GameObject().AddComponent<PuertasUnion>();
                pu.Construir(d, u, raiz, fuente);
                Uniones.Add(pu);
            }
            ModuloNave nc = d.Modulo("NC");
            Personaje = new GameObject().AddComponent<PersonajeFlotante>();
            Personaje.Iniciar(d, nc.AMundo(SalidaLocal), Quaternion.LookRotation(nc.Adelante, nc.Arriba));
            Physics.SyncTransforms();

            CrearPuntos(d);
            Detalle = gameObject.AddComponent<DetalleInterior>();
            Detalle.Construir(this, d, fuente);
            VistaVuelo = gameObject.AddComponent<VistaVueloMVP>();
            VistaVuelo.Construir(this, d);
            CrearSonidos();

            // Herramienta de inspección apagada hasta que se pida con Tab.
            ActivarInspeccion(false);
            Expedicion.Vista.AplicarCorte(0);
            Expedicion.Vista.MostrarEtiquetas(false);
            Expedicion.Vista.MostrarEsquema(false);

            EntrarPreparacion();
        }

        void CrearPuntos(DefinicionNaveModular d)
        {
            void Punto(TipoPunto tipo, string modulo, string nombre, Vector3 local)
            {
                ModuloNave m = d.Modulo(modulo);
                if (m == null) return;
                Puntos.Add(new PuntoInteraccion { tipo = tipo, modulo = modulo, nombre = nombre, posicion = m.AMundo(local) });
            }
            foreach (PuestoExpedicion p in d.puestos)
            {
                TipoPunto t = p.rol == "Pilotaje" ? TipoPunto.Pilotaje : p.rol == "Ciencia" ? TipoPunto.Ciencia
                            : p.rol == "Comunicaciones" ? TipoPunto.Comunicaciones : TipoPunto.Ingenieria;
                Punto(t, p.modulo, "Puesto de " + p.rol, p.posicion);
            }
            Punto(TipoPunto.Mesa, "NC", "Mesa de misión", new Vector3(0f, -1.9f, 2.8f));
            Punto(TipoPunto.Kits, "BO", "Armario de kits de reparación", new Vector3(0f, 0f, 3f));
            Punto(TipoPunto.Reparacion, "MA", "Punto de reparación · Mando", new Vector3(0f, 0f, 3f));
            Punto(TipoPunto.Reparacion, "IN", "Punto de reparación · Ingeniería", new Vector3(0f, 0f, 2.2f));
            Punto(TipoPunto.Reparacion, "CI", "Punto de reparación · Ciencia", new Vector3(0f, 0f, 2.2f));
            Punto(TipoPunto.Reparacion, "CO", "Punto de reparación · Comunicaciones", new Vector3(0f, 0f, 2.2f));
            Punto(TipoPunto.Reparacion, "HA", "Punto de reparación · Hábitat", new Vector3(0f, 0f, 4f));
            Punto(TipoPunto.Reparacion, "SI", "Punto de reparación · Sistemas", new Vector3(0f, 0f, 3f));
        }

        // ------------------------------------------------------------------ Avisos y sonido

        public void Avisar(string texto, float segundos)
        {
            Aviso = texto;
            avisoHasta = Time.time + segundos;
        }

        void CrearSonidos()
        {
            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            audioFuente.spatialBlend = 0f;
            audioFuente.volume = 0.35f;
            sonidos["ok"] = Tono("ok", 880f, 0.12f, 1);
            sonidos["paso"] = Tono("paso", 660f, 0.18f, 2);
            sonidos["error"] = Tono("error", 180f, 0.22f, 1);
            sonidos["alarma"] = Tono("alarma", 440f, 0.25f, 3);
            sonidos["impacto"] = Tono("impacto", 90f, 0.35f, 1);
        }

        static AudioClip Tono(string nombre, float hz, float segundos, int repeticiones)
        {
            const int frecuencia = 22050;
            int tramo = Mathf.CeilToInt(frecuencia * segundos);
            int pausa = repeticiones > 1 ? tramo / 2 : 0;
            float[] datos = new float[(tramo + pausa) * repeticiones];
            for (int r = 0; r < repeticiones; r++)
                for (int i = 0; i < tramo; i++)
                {
                    float envolvente = Mathf.Min(1f, Mathf.Min(i, tramo - i) / (frecuencia * 0.01f));
                    datos[r * (tramo + pausa) + i] = Mathf.Sin(2f * Mathf.PI * hz * i / frecuencia) * envolvente * 0.6f;
                }
            AudioClip clip = AudioClip.Create(nombre, datos.Length, 1, frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }

        public void Sonar(string nombre)
        {
            AudioClip c;
            if (audioFuente != null && sonidos.TryGetValue(nombre, out c)) audioFuente.PlayOneShot(c);
        }

        /// <summary>Resultado de una acción de consola: aviso y sonido.</summary>
        public void Respuesta(bool correcto, string mensaje)
        {
            Avisar(mensaje, 6f);
            Sonar(correcto ? "ok" : "error");
        }

        // ------------------------------------------------------------------ Fases

        void EntrarPreparacion()
        {
            ExpedicionActual.Reabrir();
            Borrador = ExpedicionActual.Preparacion.Copia();
            Recalcular();
            // Solo como fondo de la preparación: la EM-4X en su órbita junto a la base (no es la misión).
            Estado = Resumen.motor != null && Resumen.tanque != null && Resumen.instrumento != null && Resumen.relevo != null
                ? new EstadoExpedicion(Resumen, Catalogo) { SoloOrbita = true } : null;
            Prediccion = null;
            RescatePedido = false;
            Aceleracion = 1;
            foreach (PuertasUnion u in Uniones) if (u != null) u.FijarParaComprobacion(u.Datos.abiertaAlInicio);
            ModuloNave nc = Expedicion.Definicion.Modulo("NC");
            Personaje.Recolocar(nc.AMundo(SalidaLocal), Quaternion.LookRotation(nc.Adelante, nc.Arriba));
            FaseActual = Fase.Preparacion;
            CambiarModo(Modo.Pilotaje);        // cámara de vuelo como fondo: la EM-4X en órbita
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Recalcula el presupuesto PREVISTO del borrador (cambiar opciones no descuenta nada).</summary>
        public void Recalcular()
        {
            Resumen = BalanceEM4X.Calcular(Borrador, Catalogo);
        }

        /// <summary>Cierra la preparación: la misión usará exactamente esta elección.</summary>
        public void ConfirmarPreparacion()
        {
            Recalcular();
            if (!Resumen.PuedeConfirmar) { Respuesta(false, "No se puede confirmar: " + Resumen.problemas[0]); return; }
            ExpedicionActual.Confirmar(Borrador);
            Respuesta(true, $"Preparación CERRADA: {Resumen.costeTotalMUSD:0.#} de {Resumen.presupuestoMUSD:0} M USD.");
        }

        public void ReabrirPreparacion()
        {
            ExpedicionActual.Reabrir();
            Recalcular();
            Respuesta(true, "Preparación REABIERTA: el presupuesto se recalcula entero con lo que elijas (no se cobra dos veces).");
        }

        public void Despegar()
        {
            if (!ExpedicionActual.Confirmada) { Respuesta(false, "Confirma antes la preparación."); return; }
            Tarjeta = 0;
            FaseActual = Fase.Transicion;
        }

        public void SiguienteTarjeta()
        {
            Tarjeta++;
            if (Tarjeta >= 3) IniciarMision();
        }

        void IniciarMision()
        {
            // La misión se crea a partir de la preparación CONFIRMADA (única fuente).
            Resumen = BalanceEM4X.Calcular(ExpedicionActual.Preparacion, Catalogo);
            Estado = new EstadoExpedicion(Resumen, Catalogo);
            acumulado = 0f;
            Aceleracion = 1;
            Prediccion = null;
            proximaPrediccion = 0f;
            FaseActual = Fase.Mision;
            CambiarModo(Modo.Interior);
            Avisar("Estás en el nodo central de la EM-4X. Ve a la MESA DE MISIÓN (delante) o al puesto de PILOTAJE (proa). E: usar.", 10f);
            Sonar("paso");
        }

        public void NuevaExpedicion()
        {
            EntrarPreparacion();
        }

        // ------------------------------------------------------------------ Modos

        void ActivarInspeccion(bool activa)
        {
            InspeccionExpedicion ins = Expedicion.Inspeccion;
            if (ins != null)
            {
                ins.enabled = activa;
                if (ins.Camara != null)
                {
                    ins.Camara.enabled = activa;
                    AudioListener al = ins.Camara.GetComponent<AudioListener>();
                    if (al != null) al.enabled = activa;
                }
            }
            HudExpedicion hudEx = Expedicion.GetComponent<HudExpedicion>();
            if (hudEx != null) hudEx.enabled = activa;
        }

        public void CambiarModo(Modo modo)
        {
            ModoActual = modo;
            ActivarInspeccion(modo == Modo.Inspeccion);
            Expedicion.Vista.MostrarEtiquetas(modo == Modo.Inspeccion);
            if (modo != Modo.Inspeccion) Expedicion.Vista.AplicarCorte(0);
            if (VistaVuelo != null) VistaVuelo.Activar(modo == Modo.Pilotaje);
            Personaje.ActivarControl(modo == Modo.Interior);
            // El cuerpo provisional solo se ve en la cámara de inspección (no tapa la vista del asiento ni de las consolas).
            foreach (Renderer r in Personaje.GetComponentsInChildren<Renderer>()) r.enabled = modo == Modo.Inspeccion;
            if (modo == Modo.Consola && Personaje.Camara != null)
            {
                // En una consola se sigue viendo el interior, pero el ratón queda libre para los botones.
                Personaje.Camara.enabled = true;
                AudioListener al = Personaje.Camara.GetComponent<AudioListener>();
                if (al != null) al.enabled = true;
            }
            if (modo != Modo.Pilotaje) Aceleracion = 1;
        }

        public void SalirDeConsola()
        {
            if (FaseActual != Fase.Mision) return;
            if (ModoActual == Modo.Pilotaje)
                Avisar("Has dejado Pilotaje. El vuelo NO se detiene: motor apagado, la nave sigue su trayectoria. " + AyudasActivas(), 8f);
            CambiarModo(Modo.Interior);
        }

        /// <summary>Qué ayudas siguen activas sin piloto y qué gastan.</summary>
        public string AyudasActivas()
        {
            if (Estado == null) return "";
            List<string> a = new List<string>();
            if (Estado.Frenado.Activa) a.Add("frenado asistido respecto a " + Estado.Objetivo.nombre + " (gasta combustible)");
            else if (Estado.Apuntar > 0) a.Add("orientación automática: " + EstadoExpedicion.NombresApuntar[Estado.Apuntar] + (Estado.Apuntar == 1 ? " (" + Estado.Objetivo.nombre + ")" : "") + " (gasta combustible de orientación)");
            if (Estado.Vuelo.Estabilizacion) a.Add("estabilización del giro (gasta combustible de orientación)");
            return a.Count == 0 ? "Sin ayudas activas: nada mantiene la orientación." : "Ayudas activas: " + string.Join("; ", a) + ".";
        }

        // ------------------------------------------------------------------ Bucle

        static float Eje(KeyControl positivo, KeyControl negativo) =>
            (positivo.isPressed ? 1f : 0f) - (negativo.isPressed ? 1f : 0f);

        void Update()
        {
            if (FaseActual == Fase.Cargando) return;
            Keyboard k = Keyboard.current;

            if (FaseActual == Fase.Preparacion || FaseActual == Fase.Transicion)
            {
                // Fondo: la nave sigue en su órbita junto a la base (sin mandos).
                if (Estado != null) Simular(default(EntradaVuelo), Time.deltaTime);
                if (FaseActual == Fase.Transicion && k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
                    SiguienteTarjeta();
                return;
            }
            if (FaseActual == Fase.Resultados || Estado == null) return;

            EntradaVuelo e = default(EntradaVuelo);
            if (k != null)
            {
                if (ModoActual == Modo.Pilotaje) e = MandosPilotaje(k);
                else if (ModoActual == Modo.Consola) { if (k.qKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame) SalirDeConsola(); }
                else MandosInterior(k);
            }
            Simular(e, Time.deltaTime);
            Sucesos();
            ActualizarAislamiento();

            if (Time.time >= proximaPrediccion)
            {
                proximaPrediccion = Time.time + 0.5f;
                Prediccion = Estado.Sistema.Predecir(Estado.Vuelo.Posicion, Estado.Vuelo.Velocidad, Estado.Tiempo, 1400.0, 2.0, 5, BalanceEM4X.RadioNaveM);
            }

            if (Estado.Resultado != ResultadoExpedicion.EnCurso)
            {
                FaseActual = Fase.Resultados;
                CambiarModo(Modo.Pilotaje);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Sonar(Estado.Resultado == ResultadoExpedicion.Fracaso ? "alarma" : "paso");
            }
        }

        void Simular(EntradaVuelo e, float dtReal)
        {
            const float paso = (float)SistemaKora.PasoSimulacionS;
            acumulado += Mathf.Min(dtReal, 0.1f) * Aceleracion;
            int n = 0;
            while (acumulado >= paso && n < 400)
            {
                Estado.Avanzar(e, paso);
                acumulado -= paso;
                n++;
            }
            if (n >= 400) acumulado = 0f;       // equipo lento: se pierde aceleración del tiempo, no precisión
        }

        EntradaVuelo MandosPilotaje(Keyboard k)
        {
            EntradaVuelo e = default(EntradaVuelo);
            if (k.qKey.wasPressedThisFrame) { SalirDeConsola(); return e; }
            // Mismos mandos que VueloPrueba: W/S cabeceo, A/D guiñada, Z/C alabeo; I/K, J/L, U/O traslación; Espacio motor.
            e.giro = new Vector3(Eje(k.wKey, k.sKey), Eje(k.dKey, k.aKey), Eje(k.zKey, k.cKey));
            e.traslacion = new Vector3(Eje(k.lKey, k.jKey), Eje(k.uKey, k.oKey), Eje(k.iKey, k.kKey));
            e.motorPrincipal = k.spaceKey.isPressed;
            bool manual = e.giro != Vector3.zero || e.traslacion != Vector3.zero || e.motorPrincipal;

            if (k.vKey.wasPressedThisFrame) VistaExterior = !VistaExterior;
            if (k.mKey.wasPressedThisFrame) MapaVisible = !MapaVisible;
            if (k.hKey.wasPressedThisFrame) AyudaVisible = !AyudaVisible;
            if (k.nKey.wasPressedThisFrame)
            {
                Estado.SiguienteObjetivo();
                Avisar("Objetivo y referencia de velocidad: " + Estado.Objetivo.nombre + ".", 3f);
            }
            if (k.tKey.wasPressedThisFrame)
            {
                Estado.Vuelo.Estabilizacion = !Estado.Vuelo.Estabilizacion;
                Avisar(Estado.Vuelo.Estabilizacion ? "Estabilización ACTIVADA: frena el giro de los ejes que no mandas (gasta combustible)."
                                                   : "Estabilización DESACTIVADA: la nave sigue girando hasta que la frenes.", 3f);
            }
            if (k.pKey.wasPressedThisFrame)
            {
                Estado.Apuntar = (Estado.Apuntar + 1) % EstadoExpedicion.NombresApuntar.Length;
                Avisar("Orientación automática: " + EstadoExpedicion.NombresApuntar[Estado.Apuntar] +
                       (Estado.Apuntar == 1 ? " (" + Estado.Objetivo.nombre + ")" : "") +
                       (Estado.Apuntar > 0 ? ". Sigue activa aunque dejes el puesto y gasta combustible de orientación." : "."), 5f);
            }
            if (k.bKey.wasPressedThisFrame)
            {
                if (Estado.Frenado.Activa)
                {
                    Estado.Frenado.Cancelar("Frenado asistido CANCELADO (B).");
                    Avisar(Estado.Frenado.Motivo, 3f);
                }
                else if (Estado.Vuelo.Combustible <= 0f) Avisar("Sin combustible: la asistencia no puede empujar.", 4f);
                else if (Estado.Objetivo.tipo == TipoCuerpo.Planeta || Estado.Objetivo.tipo == TipoCuerpo.Luna)
                    Respuesta(false, "Frenar respecto a " + Estado.Objetivo.nombre + " anularía la órbita y caerías: elige con N la base, K-2 o el cúmulo.");
                else
                {
                    Estado.Vuelo.VelocidadReferencia = VelocidadObjetivo();
                    Estado.Frenado.Activar();
                    Avisar($"Frenado asistido RESPECTO A {Estado.Objetivo.nombre}: anula la velocidad relativa ({Estado.Vuelo.RapidezRelativa:0.0} m/s). " +
                           "No anula la gravedad: al terminar la nave sigue su órbita. B o cualquier mando lo cancela.", 6f);
                }
            }
            if (k.enterKey.wasPressedThisFrame)
            {
                string mensaje;
                bool hecho = Estado.ConfirmarRegreso(out mensaje);
                Respuesta(hecho, mensaje);
            }
            if (k.periodKey.wasPressedThisFrame) CambiarAceleracion(1);
            if (k.commaKey.wasPressedThisFrame) CambiarAceleracion(-1);
            if ((manual || Estado.Frenado.Activa) && Aceleracion != 1)
            {
                Aceleracion = 1;
                Avisar("Tiempo a ×1: hay mandos o frenado asistido en curso.", 2f);
            }
            if (!Estado.Frenado.Activa && Estado.Frenado.Motivo.Length > 0 && Estado.Frenado.Motivo != ultimoMotivoFrenado)
            {
                ultimoMotivoFrenado = Estado.Frenado.Motivo;
                Avisar(ultimoMotivoFrenado, 5f);
            }
            return e;
        }
        string ultimoMotivoFrenado = "";

        Vector3d VelocidadObjetivo()
        {
            Vector3d p, v;
            Estado.Sistema.Estado(Estado.Objetivo, Estado.Tiempo, out p, out v);
            return v;
        }

        void CambiarAceleracion(int sentido)
        {
            int i = System.Array.IndexOf(Aceleraciones, Aceleracion);
            i = Mathf.Clamp(i + sentido, 0, Aceleraciones.Length - 1);
            Aceleracion = Aceleraciones[i];
            Avisar("Tiempo ×" + Aceleracion + (Aceleracion > 1 ? " (solo en Pilotaje y sin mandos)." : "."), 2f);
        }

        void MandosInterior(Keyboard k)
        {
            if (k.tabKey.wasPressedThisFrame)
            {
                CambiarModo(ModoActual == Modo.Interior ? Modo.Inspeccion : Modo.Interior);
                Avisar(ModoActual == Modo.Interior ? "Personaje con colisiones."
                                                   : "CÁMARA DE INSPECCIÓN (herramienta aparte, sin colisiones). El vuelo sigue. Tab: volver.", 4f);
                return;
            }
            if (ModoActual != Modo.Interior) return;
            if (k.f1Key.wasPressedThisFrame) AyudaVisible = !AyudaVisible;
            if (k.f5Key.wasPressedThisFrame)
            {
                ModuloNave nc = Expedicion.Definicion.Modulo("NC");
                Personaje.Recolocar(nc.AMundo(SalidaLocal), Quaternion.LookRotation(nc.Adelante, nc.Arriba));
                Avisar("Vuelta al nodo central.", 2f);
            }

            // Punto de interacción más cercano (los de reparación solo cuentan si su módulo tiene una brecha).
            PuntoCercano = null;
            float mejor = 1.7f;
            Vector3 pos = Personaje.Cuerpo.position;
            foreach (PuntoInteraccion p in Puntos)
            {
                if (p.tipo == TipoPunto.Reparacion && Estado.DanoDe(p.modulo) == null) continue;
                float dist = Vector3.Distance(pos, p.posicion);
                if (dist < mejor) { mejor = dist; PuntoCercano = p; }
            }
            if (Time.time > reparacionHasta) TextoReparacion = "";
            if (PuntoCercano == null || Personaje.PanelMirado != null) return;    // un panel de puerta mirado tiene prioridad

            if (PuntoCercano.tipo == TipoPunto.Reparacion)
            {
                if (k.eKey.isPressed)
                {
                    string mensaje;
                    bool bien = Estado.Reparar(PuntoCercano.modulo, Time.deltaTime, out mensaje);
                    TextoReparacion = mensaje;
                    reparacionHasta = Time.time + (bien ? 0.5f : 3f);
                    if (!bien && k.eKey.wasPressedThisFrame) Sonar("error");
                }
                return;
            }
            if (!k.eKey.wasPressedThisFrame) return;
            switch (PuntoCercano.tipo)
            {
                case TipoPunto.Pilotaje:
                    Estado.PilotajeTomado = true;
                    CambiarModo(Modo.Pilotaje);
                    Avisar("PILOTAJE tomado. Q: dejar el puesto (el vuelo sigue). H: ayuda de mandos.", 5f);
                    Sonar("ok");
                    break;
                case TipoPunto.Kits:
                {
                    string mensaje;
                    bool bien = Estado.RecogerKit(out mensaje);
                    Respuesta(bien, mensaje);
                    break;
                }
                default:
                    ConsolaActual = PuntoCercano.tipo;
                    CambiarModo(Modo.Consola);
                    Sonar("ok");
                    break;
            }
        }

        void Sucesos()
        {
            if (Estado.Sucesos.Count == 0) return;
            foreach (string s in Estado.Sucesos)
            {
                if (s == "alarma" || s == "catastrofe")
                {
                    Sonar("alarma");
                    Avisar(Estado.Registro[Estado.Registro.Count - 1], 10f);
                }
                else if (s == "impacto" || s == "roce")
                {
                    Sonar("impacto");
                    Avisar(Estado.Registro[Estado.Registro.Count - 1], 6f);
                }
                else if (s == "paso")
                {
                    Sonar("paso");
                    Avisar("Paso cumplido. Siguiente: " + Estado.PasoTexto, 8f);
                }
                else Sonar("ok");
            }
            Estado.Sucesos.Clear();
        }

        /// <summary>Un módulo dañado está aislado si TODAS sus puertas de aislamiento están cerradas.</summary>
        void ActualizarAislamiento()
        {
            foreach (DanoModulo d in Estado.Danos)
            {
                if (d.reparado) { d.aislado = false; continue; }
                d.aislado = ModuloAislado(d.modulo);
            }
        }

        public bool ModuloAislado(string modulo)
        {
            int puertas = 0;
            foreach (PuertasUnion u in Uniones)
            {
                if (u == null || u.Datos.tipo != TipoPaso.Aislamiento) continue;
                if (u.Datos.moduloA != modulo && u.Datos.moduloB != modulo) continue;
                puertas++;
                if (u.EstadoActual != PuertasUnion.Estado.Cerrada) return false;
            }
            return puertas > 0;
        }

        /// <summary>Mensaje más reciente de cualquier puerta.</summary>
        public string MensajePuertas()
        {
            string m = ""; float t = 0f;
            foreach (PuertasUnion u in Uniones)
                if (u != null && Time.time < u.MensajeHasta && u.MensajeHasta > t) { t = u.MensajeHasta; m = u.Mensaje; }
            return m;
        }
    }
}
