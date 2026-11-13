using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Prueba de distribución de la nave de expedición modular EM-4X (escena ExpedicionPrueba). Punto de entrada:
    /// crea los datos (DefinicionNaveModular), ejecuta la revisión (RevisionDistribucion), construye la geometría
    /// (VistaExpedicion), los controles de inspección (InspeccionExpedicion) y los paneles (HudExpedicion).
    ///
    /// Es un bloque PROVISIONAL para recorrer: no hay atraque, ensamblaje, lanzamientos, gravedad, bots, red ni balance de
    /// vuelo, y no usa los catálogos ni la configuración de la partida (no cambia masas, motores, combustible ni límites).
    /// </summary>
    public class PruebaExpedicion : MonoBehaviour
    {
        public DefinicionNaveModular Definicion { get; private set; }
        public RevisionDistribucion.Resultado Revision { get; private set; }
        public VistaExpedicion Vista { get; private set; }
        public InspeccionExpedicion Inspeccion { get; private set; }
        public bool AyudaVisible { get; set; } = true;
        public string Aviso { get; private set; } = "";
        public bool AvisoVisible => Aviso.Length > 0 && Time.time < avisoHasta;
        float avisoHasta;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Definicion = DefinicionNaveModular.ExpedicionEM4X();
            Revision = RevisionDistribucion.Revisar(Definicion);
            Debug.Log($"[Expedición] {Definicion.nombre}: {Definicion.modulos.Count} módulos, {Definicion.conexiones.Count} conexiones. " +
                      $"Revisión de distribución: {Revision.comprobaciones} comprobaciones correctas, {Revision.problemas.Count} problemas.", this);
            foreach (string p in Revision.problemas) Debug.LogWarning("[Expedición] " + p, this);

            Vista = gameObject.AddComponent<VistaExpedicion>();
            Vista.Construir(Definicion, Revision);
            Inspeccion = gameObject.AddComponent<InspeccionExpedicion>();
            Inspeccion.Iniciar(this);
            gameObject.AddComponent<HudExpedicion>();

            Avisar(Revision.problemas.Count == 0
                ? "Bloque provisional EM-4X. I: recorrer el interior · 1–4: puestos · 0: esclusa · C: corte · H: ayuda."
                : $"La revisión encontró {Revision.problemas.Count} problemas de distribución: ver el panel izquierdo.", 7f);
        }

        public void Avisar(string texto, float segundos)
        {
            Aviso = texto;
            avisoHasta = Time.time + segundos;
        }
    }
}
