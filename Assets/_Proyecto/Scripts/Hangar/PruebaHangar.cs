using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace PrototipoMision
{
    /// <summary>
    /// Prueba de hangar (escena HangarPrueba): configurar e inspeccionar una nave base. NO es la preparación final:
    /// no hay despegue, embarque, bots, red ni flotación.
    ///
    /// La configuración es la MISMA del juego: ConfiguracionElegida guardada en MisionActual y calculada con
    /// CatalogoMision.Calcular + CatalogoNave.Calcular (mismos problemas, avisos, masas y costes que la pantalla de
    /// configuración). Esta prueba solo cambia el motor y el tanque.
    /// La vista (VistaHangar) y los paneles (HudHangar) solo leen este componente.
    /// </summary>
    public class PruebaHangar : MonoBehaviour
    {
        [Tooltip("Catálogo de la misión (con su CatalogoNave enlazado). Lo asigna el menú 13.")]
        [SerializeField] CatalogoMision catalogo;
        [Tooltip("Bases disponibles. En esta prueba solo hay una; la lista admite más.")]
        [SerializeField] List<DefinicionNaveBase> bases = new List<DefinicionNaveBase> { DefinicionNaveBase.ExpedicionE4() };
        [SerializeField] int baseElegida;

        public enum Modo { Exterior, Inspeccion }

        public CatalogoMision Catalogo => catalogo;
        public CatalogoNave CatalogoNave => catalogo != null ? catalogo.catalogoNave : null;
        public DefinicionNaveBase Base => bases != null && bases.Count > 0 ? bases[Mathf.Clamp(baseElegida, 0, bases.Count - 1)] : null;
        public ConfiguracionElegida Eleccion => eleccion;
        public ResumenMision Resumen { get; private set; }
        public ResumenNave ResumenNave { get; private set; }
        public DesgloseMasa Masas => DesgloseMasa.De(ResumenNave);
        public Modo ModoActual { get; private set; } = Modo.Exterior;
        public bool Corte { get; private set; }
        public bool EstadosVisibles { get; private set; }
        public bool AyudaVisible { get; private set; } = true;
        public string Aviso { get; private set; } = "";
        public bool AvisoVisible => Aviso.Length > 0 && Time.time < avisoHasta;
        public VistaHangar Vista { get; private set; }
        public string Error { get; private set; } = "";

        ConfiguracionElegida eleccion;
        float avisoHasta;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (catalogo == null || catalogo.catalogoNave == null || Base == null)
            {
                Error = catalogo == null ? "Falta asignar el CatalogoMision en PruebaHangar (usa el menú 13 o arrástralo en el inspector)."
                      : catalogo.catalogoNave == null ? "El CatalogoMision no tiene enlazado el CatalogoNave."
                      : "No hay ninguna nave base definida.";
                Debug.LogError("[Hangar] " + Error, this);
                return;
            }

            // Misma configuración que el resto del juego: si ya hay una elegida en esta sesión, se continúa con ella.
            eleccion = MisionActual.Configurada ? MisionActual.Eleccion : default;
            eleccion.motorNave = Ajustar(eleccion.motorNave, CatalogoNave.motores);
            eleccion.tanqueNave = Ajustar(eleccion.tanqueNave, CatalogoNave.tanques);
            Recalcular();

            Vista = gameObject.AddComponent<VistaHangar>();
            Vista.Construir(this);
            gameObject.AddComponent<HudHangar>();

            Avisar(MisionActual.Configurada
                ? "Configuración recuperada de esta sesión: motor y tanque como los dejaste."
                : "Hangar de prueba. Cambia motor y tanque en el panel; I: inspeccionar el interior. H: ayuda.", 6f);
            Debug.Log($"[Hangar] Base «{Base.nombre}». Configuración {(MisionActual.Configurada ? "recuperada de MisionActual" : "inicial (primeras opciones)")}: " +
                      $"motor «{ResumenNave.motor?.nombre}», tanque «{ResumenNave.tanque?.nombre}».", this);
        }

        static int Ajustar<T>(int i, List<T> lista) => lista == null || lista.Count == 0 ? 0 : Mathf.Clamp(i, 0, lista.Count - 1);

        void Recalcular()
        {
            Resumen = catalogo.Calcular(eleccion);
            ResumenNave = catalogo.catalogoNave.Calcular(Resumen);
        }

        public void Avisar(string texto, float segundos)
        {
            Aviso = texto;
            avisoHasta = Time.time + segundos;
        }

        /// <summary>Cambia el motor (paso −1 o +1 en la lista del catálogo). Guarda la configuración del juego.</summary>
        public void CambiarMotor(int paso)
        {
            if (CatalogoNave == null || CatalogoNave.motores.Count == 0) return;
            int n = CatalogoNave.motores.Count;
            eleccion.motorNave = ((eleccion.motorNave + paso) % n + n) % n;
            Aplicar($"Motor: {CatalogoNave.motores[eleccion.motorNave].nombre}");
        }

        public void CambiarTanque(int paso)
        {
            if (CatalogoNave == null || CatalogoNave.tanques.Count == 0) return;
            int n = CatalogoNave.tanques.Count;
            eleccion.tanqueNave = ((eleccion.tanqueNave + paso) % n + n) % n;
            Aplicar($"Tanque: {CatalogoNave.tanques[eleccion.tanqueNave].nombre}");
        }

        void Aplicar(string que)
        {
            Recalcular();
            MisionActual.Guardar(eleccion);   // la misma configuración que usan Configuración y Nave
            if (Vista != null) Vista.ActualizarModulos();
            Avisar($"{que} · masa total {ResumenNave.masaInicialKg:0} kg · coste {Resumen.costeTotalMUSD:0.#} M USD" +
                   (Resumen.PuedeIniciar ? "" : " · NO SE PUEDE INICIAR (ver límites)"), 4f);
        }

        void Update()
        {
            if (Error.Length > 0) return;
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.hKey.wasPressedThisFrame) AyudaVisible = !AyudaVisible;
            if (k.iKey.wasPressedThisFrame)
            {
                ModoActual = ModoActual == Modo.Exterior ? Modo.Inspeccion : Modo.Exterior;
                if (Vista != null) Vista.CambiarModo(ModoActual);
                Avisar(ModoActual == Modo.Inspeccion
                    ? "CÁMARA DE INSPECCIÓN PROVISIONAL: no es el movimiento interior final y no tiene colisiones."
                    : "Vista exterior.", 4f);
            }
            if (k.cKey.wasPressedThisFrame)
            {
                Corte = !Corte;
                Avisar(Corte ? "Corte: techo y cubierta ocultos para ver el interior desde fuera." : "Corte desactivado.", 3f);
            }
            if (k.lKey.wasPressedThisFrame)
            {
                EstadosVisibles = !EstadosVisibles;
                Avisar(EstadosVisibles ? "Colores de estado: verde conservada · azul propuesta · ámbar pendiente de prueba · naranja reserva." : "Colores de estado ocultos.", 5f);
            }
        }

        /// <summary>Solo en el editor: vuelve a cargar esta escena en Play para comprobar que la configuración se conserva.</summary>
        public void RecargarHangar()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(gameObject.scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        }

        /// <summary>Solo en el editor: abre la pantalla de Configuración existente con la misma configuración.</summary>
        public void AbrirConfiguracion()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Scenes/Configuracion.unity", new LoadSceneParameters(LoadSceneMode.Single));
#endif
        }

        public static bool EnEditor
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
