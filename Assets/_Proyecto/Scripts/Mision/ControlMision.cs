using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    /// <summary>
    /// Va en la escena Exploracion. Detecta el final de la misión,
    /// genera el InformeMision y abre la escena de resultados:
    /// - Objetivo cumplido (todos los escaneos requeridos transmitidos).
    /// - Batería agotada.
    /// - El jugador pulsa T dentro de la estación.
    /// </summary>
    public class ControlMision : MonoBehaviour
    {
        [SerializeField] CatalogoMision catalogo;
        [SerializeField] SistemaEnergia energia;
        [SerializeField] SistemaDatos datos;
        [SerializeField] Escaner escaner;
        [SerializeField] AplicadorConfiguracion aplicador;
        [SerializeField] ControladorSonda controlador;
        [SerializeField] Transform estacion;

        [SerializeField] string escenaResultados = "Resultados";

        [Tooltip("Segundos que se ve el aviso de objetivo cumplido antes del informe.")]
        [SerializeField] float esperaTrasObjetivoS = 4f;

        [Tooltip("Segundos que se ve el aviso de batería agotada antes del informe.")]
        [SerializeField] float esperaTrasAgotarS = 3f;

        [Header("Textos editables")]
        [SerializeField] string textoTerminar = "T: terminar la misión y ver el informe";
        [SerializeField] string textoSoloEnEstacion = "Solo puedes terminar la misión dentro de la estación.";

        float inicio;
        bool terminando;
        float cuentaAtras;
        float tiempoAviso;

        GUIStyle estilo;

        void Awake()
        {
            if (energia == null) energia = FindAnyObjectByType<SistemaEnergia>();
            if (datos == null) datos = FindAnyObjectByType<SistemaDatos>();
            if (escaner == null) escaner = FindAnyObjectByType<Escaner>();
            if (aplicador == null) aplicador = FindAnyObjectByType<AplicadorConfiguracion>();
            if (controlador == null) controlador = FindAnyObjectByType<ControladorSonda>();
            if (estacion == null)
            {
                EstacionSegura e = FindAnyObjectByType<EstacionSegura>();
                if (e != null) estacion = e.transform;
            }
        }

        void Start()
        {
            inicio = Time.time;
            if (catalogo == null)
                Debug.LogWarning("[Prototipo] ControlMision: falta el CatalogoMision; los bonos de puntuación serán 0.", this);
        }

        void Update()
        {
            if (terminando)
            {
                cuentaAtras -= Time.deltaTime;
                if (cuentaAtras <= 0f)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    SceneManager.LoadScene(escenaResultados);
                }
                return;
            }

            if (tiempoAviso > 0f) tiempoAviso -= Time.deltaTime;

            if (datos != null && datos.ObjetivoCumplido)
            {
                Terminar(MotivoFin.ObjetivoCumplido, esperaTrasObjetivoS);
                return;
            }

            if (energia != null && energia.Agotada)
            {
                Terminar(MotivoFin.BateriaAgotada, esperaTrasAgotarS);
                return;
            }

            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.tKey.wasPressedThisFrame)
            {
                if (energia != null && energia.Cargando) Terminar(MotivoFin.TerminadaPorJugador, 0.3f);
                else tiempoAviso = 3f;
            }
        }

        void Terminar(MotivoFin motivo, float espera)
        {
            terminando = true;
            cuentaAtras = espera;
            InformeMision.Ultimo = CrearInforme(motivo);
        }

        InformeMision CrearInforme(MotivoFin motivo)
        {
            InformeMision inf = new InformeMision
            {
                motivo = motivo,
                eleccion = MisionActual.Eleccion,
                segundosMision = Time.time - inicio
            };

            ResumenMision r = aplicador != null ? aplicador.Resumen : null;
            if (r != null && r.instrumento != null)
            {
                inf.objetivo = r.objetivo.nombre;
                inf.instrumento = r.instrumento.nombre;
                inf.bateria = r.bateria.nombre;
                inf.lanzador = r.lanzador.nombre;
                inf.orbita = r.orbita.nombre;
                inf.instrumentoRecomendado = r.instrumentoRecomendado;
                inf.instrumentoSugerido = r.objetivo.instrumentoRecomendado;
                inf.costeMUSD = r.costeTotalMUSD;
                inf.presupuestoMUSD = r.presupuestoMUSD;
                inf.masaSondaKg = r.masaSondaKg;
                inf.masaLanzamientoKg = r.masaLanzamientoKg;
                inf.capacidadLanzadorKg = r.capacidadLanzadorKg;
                inf.memoriaMB = r.memoriaMB;
                inf.datosPorEscaneoMB = r.instrumento.datosPorEscaneoMB;
                inf.fraccionContacto = r.orbita.fraccionContacto;
                inf.valorPorEscaneo = r.valorPorEscaneo;
            }

            if (energia != null)
            {
                inf.capacidadWh = energia.CapacidadWh;
                inf.minutosMisionPorSegundo = energia.MinutosMisionPorSegundo;
                inf.energiaFinalWh = energia.ActualWh;
                inf.energiaConsumidaWh = energia.ConsumidaTotalWh;
                if (estacion != null)
                    inf.distanciaEstacionFinalM = Vector3.Distance(energia.transform.position, estacion.position);
            }

            if (escaner != null)
            {
                inf.energiaEscaneoWh = escaner.EnergiaEscaneoWh;
                inf.vecesMemoriaLlena = escaner.VecesMemoriaLlena;
            }

            if (datos != null)
            {
                inf.energiaTransmisionWh = datos.EnergiaTransmisionWh;
                inf.datosRecogidosMB = datos.DatosRecogidosMB;
                inf.datosTransmitidosMB = datos.DatosTransmitidosMB;
                inf.datosPerdidosMB = datos.DatosEnMemoriaMB;
                inf.escaneosRealizados = datos.EscaneosRealizados;
                inf.escaneosTransmitidos = datos.EscaneosTransmitidos;
                inf.escaneosRequeridos = datos.EscaneosRequeridos;
                inf.segundosSinContacto = datos.SegundosSinContacto;
                inf.puntosCiencia = datos.ValorTransmitido;
            }

            if (controlador != null)
                inf.factorMasa = controlador.FactorMasa;

            if (catalogo != null)
            {
                inf.bonoObjetivoConfigurado = catalogo.bonoObjetivoCumplido;
                inf.puntosPorMUSDSobrante = catalogo.puntosPorMUSDSobrante;
                foreach (OpcionBateria b in catalogo.baterias)
                    if (b != null && b.capacidadWh > inf.bateriaMayorDisponibleWh)
                        inf.bateriaMayorDisponibleWh = b.capacidadWh;
            }

            // Modo equipo: quién decidió cada parte y si el plan cumplía la prioridad de cada rol.
            inf.jugadores = ModoEquipo.Jugadores;
            inf.responsables = ModoEquipo.CopiaReparto();
            if (r != null && r.instrumento != null && catalogo != null)
                for (int i = 0; i < ModoEquipo.MaxJugadores; i++)
                    inf.prioridades[i] = ModoEquipo.Evaluar((Rol)i, r, catalogo);

            inf.CopiarViaje();   // datos de la nave, si se pasó por ella
            inf.Completar();
            return inf;
        }

        void OnGUI()
        {
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperCenter, wordWrap = true };
                estilo.normal.textColor = new Color(0.85f, 0.95f, 1f);
            }

            float escala = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            float ancho = Screen.width / escala;

            string texto = null;
            if (terminando) texto = $"Generando el informe de la misión… {Mathf.Max(0f, cuentaAtras):0} s";
            else if (tiempoAviso > 0f) texto = textoSoloEnEstacion;
            else if (energia != null && energia.Cargando)
            {
                texto = textoTerminar;
                if (datos != null && datos.DatosEnMemoriaMB > 0.5f)
                    texto += $" · aún hay {datos.DatosEnMemoriaMB:0} MB sin transmitir, se perderían";
            }

            if (texto != null)
                GUI.Label(new Rect(ancho / 2f - 450f, 1080f - 130f, 900f, 60f), texto, estilo);

            GUI.matrix = Matrix4x4.identity;
        }
    }
}
