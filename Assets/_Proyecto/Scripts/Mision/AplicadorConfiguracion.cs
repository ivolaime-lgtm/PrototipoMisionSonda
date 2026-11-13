using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    /// <summary>
    /// Va en la escena Exploracion. Al empezar, aplica la configuración elegida:
    /// - Batería: fija la capacidad de SistemaEnergia (APLICADO).
    /// - Masa de la sonda: cambia la aceleración en ControladorSonda (APLICADO).
    /// - Instrumento, memoria y órbita: los leen Escaner y SistemaDatos
    ///   desde la propiedad Resumen.
    /// Tecla M: volver a la pantalla de configuración.
    /// </summary>
    public class AplicadorConfiguracion : MonoBehaviour
    {
        [SerializeField] CatalogoMision catalogo;
        [SerializeField] SistemaEnergia energia;
        [SerializeField] ControladorSonda controlador;
        [SerializeField] string escenaConfiguracion = "Configuracion";
        [SerializeField] bool mostrarPanel = true;

        /// <summary>Configuración aplicada. La usarán el escáner y la transmisión.</summary>
        public ResumenMision Resumen { get; private set; }

        GUIStyle estilo;

        void Start()
        {
            if (catalogo == null)
            {
                Debug.LogError("[Prototipo] AplicadorConfiguracion: falta asignar el CatalogoMision.", this);
                return;
            }

            if (energia == null) energia = FindAnyObjectByType<SistemaEnergia>();
            if (controlador == null) controlador = FindAnyObjectByType<ControladorSonda>();

            if (!MisionActual.Configurada)
                Debug.Log("[Prototipo] Se inició Exploracion sin pasar por Configuracion: se usan las primeras opciones del catálogo.", this);

            Resumen = catalogo.Calcular(MisionActual.Eleccion);

            if (!Resumen.PuedeIniciar)
                Debug.LogWarning("[Prototipo] La configuración supera algún límite: " + string.Join(" | ", Resumen.problemas), this);

            if (Resumen.bateria != null && energia != null)
            {
                energia.ConfigurarBateria(Resumen.energiaWh);
                AplicarEstadoTrasViaje();
            }

            if (controlador != null)
                controlador.EstablecerMasa(Resumen.masaSondaKg);

            // El catálogo usa su escala de tiempo solo para estimaciones; la batería usa la suya.
            if (energia != null && !Mathf.Approximately(energia.MinutosMisionPorSegundo, catalogo.minutosMisionPorSegundo))
                Debug.LogWarning($"[Prototipo] Escala de tiempo distinta: catálogo {catalogo.minutosMisionPorSegundo} min/s, " +
                                 $"batería de la sonda {energia.MinutosMisionPorSegundo} min/s. Las estimaciones de la " +
                                 "configuración no coincidirán con el juego; iguala ambos valores.", this);
        }

        /// <summary>
        /// Si se llegó desde la nave, la sonda empieza con la carga que dejó el descenso
        /// (regla simplificada: frenada imprecisa → descenso prolongado → hasta un 10 % menos).
        /// Se aplica una sola vez por viaje. Sin viaje (Exploracion directa) no se aplica nada.
        /// </summary>
        void AplicarEstadoTrasViaje()
        {
            if (!EstadoViaje.Realizado || EstadoViaje.Abortado || !EstadoViaje.Evaluado)
            {
                EstadoViaje.CargaInicialSondaWh = energia.CapacidadWh;
                return;
            }
            if (EstadoViaje.CargaSondaAplicada) return;

            float reduccion = Mathf.Clamp(EstadoViaje.ReduccionCargaSonda, 0f, 0.1f);
            float carga = energia.CapacidadWh * (1f - reduccion);
            energia.EstablecerCargaActual(carga);
            EstadoViaje.CargaInicialSondaWh = carga;
            EstadoViaje.CargaSondaAplicada = true;
            if (reduccion > 0f)
                Debug.Log($"[Prototipo] Descenso prolongado: la sonda empieza con {carga:0.#}/{energia.CapacidadWh:0} Wh.", this);
        }

        void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.mKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(escenaConfiguracion);
            }
        }

        void OnGUI()
        {
            if (!mostrarPanel || Resumen == null || Resumen.instrumento == null) return;

            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, richText = true };
                estilo.normal.textColor = new Color(0.85f, 0.93f, 0.97f);
            }

            float escala = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            float anchoVirtual = Screen.width / escala;

            bool equipo = ModoEquipo.EnEquipo;
            bool reducida = EstadoViaje.CargaSondaAplicada && EstadoViaje.ReduccionCargaSonda > 0f;
            Rect panel = new Rect(anchoVirtual - 520f, 30f, 490f, 250f + (equipo ? 18f : 0f) + (reducida ? 18f : 0f));
            Color previo = GUI.color;
            GUI.color = new Color(0f, 0.05f, 0.08f, 0.6f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previo;

            string factor = controlador != null ? $" → aceleración ×{controlador.FactorMasa:0.00}" : "";
            string texto =
                "<b>CONFIGURACIÓN DE LA MISIÓN</b>\n" +
                $"Objetivo: {Resumen.objetivo.nombre}\n" +
                $"Instrumento: {Resumen.instrumento.nombre}\n" +
                $"Batería: {Resumen.bateria.nombre} · {Resumen.energiaWh:0} Wh\n" +
                $"Lanzador: {Resumen.lanzador.nombre}\n" +
                $"Relevo: {Resumen.orbita.nombre}\n" +
                $"Masa de la sonda: {Resumen.masaSondaKg:0} kg{factor}\n" +
                (equipo ? $"Equipo: {ModoEquipo.Jugadores} jugadores\n" : "") +
                (reducida ? $"Carga inicial: {EstadoViaje.CargaInicialSondaWh:0}/{Resumen.energiaWh:0} Wh (descenso prolongado)\n" : "") +
                "<size=15>M: volver a la configuración</size>";

            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, panel.height - 20f), texto, estilo);
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
