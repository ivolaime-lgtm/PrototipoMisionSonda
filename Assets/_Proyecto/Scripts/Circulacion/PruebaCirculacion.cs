using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Prueba de CIRCULACIÓN INTERIOR (escena CirculacionPrueba). Punto de entrada.
    ///
    /// Reutiliza sin modificarla la EM-4X aceptada: añade el mismo componente PruebaExpedicion (datos, revisión de
    /// distribución, geometría, cámara de inspección y su panel). Encima añade, en archivos propios:
    ///   · ColisionesExpedicion: colisiones sobre los mismos bloques visibles.
    ///   · PuertasUnion: puertas deslizantes provisionales de la lista acordada, con paneles (E) y protección de cierre.
    ///   · PersonajeFlotante: un personaje provisional con colisiones, sin gravedad.
    ///   · RevisionCirculacion y ComprobacionPasos: revisión de datos y comprobación automática con la física.
    /// Tab alterna entre el PERSONAJE (con colisiones) y la CÁMARA DE INSPECCIÓN (sin colisiones, herramienta aparte).
    /// Sin separación, presión, oxígeno, atraque, sonda, bots ni red.
    /// </summary>
    public class PruebaCirculacion : MonoBehaviour
    {
        public enum Modo { Personaje, Inspeccion }

        public PruebaExpedicion Expedicion { get; private set; }
        public DefinicionCirculacion Circulacion { get; private set; }
        public RevisionCirculacion.Resultado RevisionPuertas { get; private set; }
        public ComprobacionPasos.Resultado Comprobacion { get; private set; }
        public PersonajeFlotante Personaje { get; private set; }
        public List<PuertasUnion> Uniones { get; } = new List<PuertasUnion>();
        public Modo ModoActual { get; private set; } = Modo.Personaje;
        public int Colisiones { get; private set; }
        public bool Listo { get; private set; }
        public bool AyudaVisible { get; private set; } = true;
        public string Aviso { get; private set; } = "";
        public bool AvisoVisible => Aviso.Length > 0 && Time.time < avisoHasta;
        /// <summary>Lugares a los que ha llegado el personaje en esta sesión (ayuda para la prueba).</summary>
        public List<(string nombre, bool alcanzado)> Lugares { get; } = new List<(string nombre, bool alcanzado)>();

        float avisoHasta;
        readonly List<(string nombre, string modulo, Vector3 punto)> destinos = new List<(string nombre, string modulo, Vector3 punto)>();
        static readonly Vector3 SalidaLocal = new Vector3(0f, 0.5f, 3.6f);   // zona común, mirando a proa

        void Awake()
        {
            Expedicion = gameObject.AddComponent<PruebaExpedicion>();
            gameObject.AddComponent<HudCirculacion>();
        }

        IEnumerator Start()
        {
            // Esperar a que PruebaExpedicion construya la nave, y un fotograma más para que se eliminen las colisiones
            // que VistaExpedicion quita a sus bloques (Destroy se aplica al final del fotograma).
            while (Expedicion.Vista == null || Expedicion.Vista.Nave == null) yield return null;
            yield return null;
            yield return new WaitForFixedUpdate();

            DefinicionNaveModular d = Expedicion.Definicion;
            Circulacion = DefinicionCirculacion.AcordadaEM4X();
            RevisionPuertas = RevisionCirculacion.Revisar(d, Circulacion);
            Colisiones = ColisionesExpedicion.Anadir(Expedicion.Vista.Nave);

            Transform raiz = new GameObject("Circulación (puertas y paneles provisionales)").transform;
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (UnionConPuertas u in Circulacion.uniones)
            {
                PuertasUnion pu = new GameObject().AddComponent<PuertasUnion>();
                pu.Construir(d, u, raiz, fuente);
                Uniones.Add(pu);
            }

            ModuloNave nc = d.Modulo("NC");
            Personaje = new GameObject().AddComponent<PersonajeFlotante>();
            Personaje.Iniciar(d, nc.AMundo(SalidaLocal), Quaternion.LookRotation(nc.Adelante, nc.Arriba));
            Physics.SyncTransforms();

            Comprobacion = ComprobacionPasos.Ejecutar(d, Circulacion, Uniones, Personaje.Capsula);
            Debug.Log($"[Circulación] {Colisiones} colisiones añadidas. Revisión de puertas y paneles: {RevisionPuertas.comprobaciones} correctas, " +
                      $"{RevisionPuertas.problemas.Count} problemas. Comprobación automática de pasos: {Comprobacion.correctas} correctas, " +
                      $"{Comprobacion.problemas.Count} problemas.", this);
            foreach (string p in RevisionPuertas.problemas) Debug.LogWarning("[Circulación] " + p, this);
            foreach (string p in Comprobacion.problemas) Debug.LogWarning("[Circulación] " + p, this);

            foreach (PuestoExpedicion p in d.puestos) destinos.Add(("Puesto de " + p.rol, p.modulo, p.posicion));
            foreach (VistaInterior v in d.vistas) if (v.nombre.StartsWith("Esclusa")) destinos.Add(("Esclusa", v.modulo, v.posicion));
            destinos.Add(("Hábitat (arriba)", "HA", new Vector3(0f, 0f, 4f)));
            destinos.Add(("Bodega (abajo)", "BO", new Vector3(0f, 0f, 3f)));
            destinos.Add(("Sistemas (arriba, popa)", "SI", new Vector3(0f, 0f, 3f)));
            destinos.Add(("Lanzadera", "LZ", new Vector3(0f, 0.3f, 6f)));
            foreach (var de in destinos) Lugares.Add((de.nombre, false));

            CambiarModo(Modo.Personaje);
            Listo = true;
            int problemas = Expedicion.Revision.problemas.Count + RevisionPuertas.problemas.Count + Comprobacion.problemas.Count;
            Avisar(problemas == 0
                ? "Personaje provisional con colisiones. Ratón: mirar · WASD: moverse · E: panel de puerta · Tab: cámara de inspección."
                : $"Hay {problemas} problemas en las revisiones: ver el panel.", 8f);
        }

        public void Avisar(string texto, float segundos)
        {
            Aviso = texto;
            avisoHasta = Time.time + segundos;
        }

        void CambiarModo(Modo modo)
        {
            ModoActual = modo;
            bool personaje = modo == Modo.Personaje;
            // Herramienta de inspección (sin colisiones): su cámara, sus teclas y su panel solo en modo inspección
            InspeccionExpedicion ins = Expedicion.Inspeccion;
            if (ins != null)
            {
                ins.enabled = !personaje;
                if (ins.Camara != null)
                {
                    ins.Camara.enabled = !personaje;
                    AudioListener al = ins.Camara.GetComponent<AudioListener>();
                    if (al != null) al.enabled = !personaje;
                }
            }
            HudExpedicion hudEx = Expedicion.GetComponent<HudExpedicion>();
            if (hudEx != null) hudEx.enabled = !personaje;
            if (personaje)
            {
                Expedicion.Vista.AplicarCorte(0);
                Expedicion.Vista.MostrarEtiquetas(false);
                Expedicion.Vista.MostrarEsquema(false);
            }
            else Expedicion.Vista.MostrarEtiquetas(true);
            Personaje.ActivarControl(personaje);
        }

        void Update()
        {
            if (!Listo) return;
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.tabKey.wasPressedThisFrame)
            {
                CambiarModo(ModoActual == Modo.Personaje ? Modo.Inspeccion : Modo.Personaje);
                Avisar(ModoActual == Modo.Personaje
                    ? "PERSONAJE provisional con colisiones."
                    : "CÁMARA DE INSPECCIÓN (sin colisiones, herramienta aparte). El personaje queda quieto donde estaba.", 4f);
            }
            if (ModoActual == Modo.Personaje)
            {
                if (k.f1Key.wasPressedThisFrame) AyudaVisible = !AyudaVisible;
                if (k.tKey.wasPressedThisFrame) Expedicion.Vista.MostrarEtiquetas(!Expedicion.Vista.EtiquetasVisibles);
                if (k.f5Key.wasPressedThisFrame)
                {
                    ModuloNave nc = Expedicion.Definicion.Modulo("NC");
                    Personaje.Recolocar(nc.AMundo(SalidaLocal), Quaternion.LookRotation(nc.Adelante, nc.Arriba));
                    Avisar("Vuelta a la zona común.", 2f);
                }
            }
            ActualizarLugares();
        }

        void ActualizarLugares()
        {
            if (Personaje == null || Personaje.Cuerpo == null) return;
            Vector3 p = Personaje.Cuerpo.position;
            for (int i = 0; i < destinos.Count; i++)
            {
                if (Lugares[i].alcanzado) continue;
                ModuloNave m = Expedicion.Definicion.Modulo(destinos[i].modulo);
                if (m == null) continue;
                if (Vector3.Distance(p, m.AMundo(destinos[i].punto)) < 1.6f)
                {
                    Lugares[i] = (Lugares[i].nombre, true);
                    Avisar("Has llegado: " + Lugares[i].nombre, 3f);
                }
            }
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
