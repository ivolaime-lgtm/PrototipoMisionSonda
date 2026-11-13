using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// CONTROLES DE INSPECCIÓN PROVISIONALES de la escena ExpedicionPrueba (cámara y teclas; nada de datos ni geometría).
    ///   Exterior: botón derecho del ratón para girar alrededor de la nave, rueda para acercar.
    ///   Inspección (I): cámara libre en 6 ejes, SIN COLISIONES — W/S/A/D, Espacio/Ctrl, Q/E para girar sobre sí misma,
    ///   botón derecho para mirar, Mayús para ir rápido, R para nivelar con el «arriba» de la nave.
    ///   1–4: puestos (Pilotaje, Ingeniería, Ciencia, Comunicaciones) · 0: esclusa · 5: zona común · 6: lanzadera.
    ///   C: corte · T: rótulos · X: esquema de conexiones y recorridos · H: ayuda.
    /// Esta cámara NO valida por sí sola el movimiento real de personajes ni sus colisiones.
    /// </summary>
    public class InspeccionExpedicion : MonoBehaviour
    {
        public enum Modo { Exterior, Inspeccion }
        public Modo ModoActual { get; private set; } = Modo.Exterior;
        public string UltimaVista { get; private set; } = "";
        public Camera Camara { get; private set; }

        PruebaExpedicion prueba;
        DefinicionNaveModular d;
        float yaw = 35f, pitch = 22f, distancia = 78f;
        Vector3 foco = new Vector3(0f, 0f, -9f);
        Vector3 ojo;
        Quaternion giro = Quaternion.identity;

        public void Iniciar(PruebaExpedicion p)
        {
            prueba = p;
            d = p.Definicion;
            GameObject camGo = new GameObject("CamaraInspeccion");
            camGo.tag = "MainCamera";
            Camara = camGo.AddComponent<Camera>();
            Camara.clearFlags = CameraClearFlags.SolidColor;
            Camara.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            Camara.nearClipPlane = 0.05f;
            Camara.farClipPlane = 600f;
            Camara.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();
        }

        void IrAPuesto(int i)
        {
            if (i < 0 || i >= d.puestos.Count) return;
            PuestoExpedicion p = d.puestos[i];
            ModuloNave m = d.Modulo(p.modulo);
            if (m == null) return;
            Colocar(m, p.posicion + new Vector3(0f, 0.6f, 0f), p.mirada, "Puesto de " + p.rol);
        }

        void IrAVista(string empiezaPor)
        {
            foreach (VistaInterior v in d.vistas)
                if (v.nombre.StartsWith(empiezaPor))
                {
                    ModuloNave m = d.Modulo(v.modulo);
                    if (m != null) Colocar(m, v.posicion, v.mirada, v.nombre);
                    return;
                }
        }

        void Colocar(ModuloNave m, Vector3 local, Vector3 miradaLocal, string nombre)
        {
            ojo = m.AMundo(local);
            Vector3 mira = m.DireccionAMundo(miradaLocal).normalized;
            giro = Quaternion.LookRotation(mira, m.Arriba);
            ModoActual = Modo.Inspeccion;
            UltimaVista = nombre;
            prueba.Avisar(nombre + " (" + m.nombre + ")", 3f);
        }

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || d == null) return;
            if (k.hKey.wasPressedThisFrame) prueba.AyudaVisible = !prueba.AyudaVisible;
            if (k.iKey.wasPressedThisFrame)
            {
                if (ModoActual == Modo.Exterior) IrAVista("Nodo central");
                else { ModoActual = Modo.Exterior; UltimaVista = ""; prueba.Avisar("Vista exterior.", 2f); }
            }
            if (k.cKey.wasPressedThisFrame)
            {
                prueba.Vista.AplicarCorte(prueba.Vista.Corte + 1);
                prueba.Avisar("Corte: " + VistaExpedicion.NombresCorte[prueba.Vista.Corte], 3f);
            }
            if (k.tKey.wasPressedThisFrame) prueba.Vista.MostrarEtiquetas(!prueba.Vista.EtiquetasVisibles);
            if (k.xKey.wasPressedThisFrame)
            {
                prueba.Vista.MostrarEsquema(!prueba.Vista.EsquemaVisible);
                prueba.Avisar(prueba.Vista.EsquemaVisible ? "Esquema: naranja = unión con escotilla · gris = unión estructural · rojo = recorridos entre puestos." : "Esquema oculto.", 4f);
            }
            if (k.digit1Key.wasPressedThisFrame) IrAPuesto(0);
            if (k.digit2Key.wasPressedThisFrame) IrAPuesto(1);
            if (k.digit3Key.wasPressedThisFrame) IrAPuesto(2);
            if (k.digit4Key.wasPressedThisFrame) IrAPuesto(3);
            if (k.digit0Key.wasPressedThisFrame) IrAVista("Esclusa");
            if (k.digit5Key.wasPressedThisFrame) IrAVista("Nodo central");
            if (k.digit6Key.wasPressedThisFrame) IrAVista("Lanzadera");
        }

        void LateUpdate()
        {
            if (Camara == null) return;
            if (ModoActual == Modo.Inspeccion) CamaraInspeccion(); else CamaraExterior();
        }

        void CamaraExterior()
        {
            Mouse raton = Mouse.current;
            if (raton != null)
            {
                if (raton.rightButton.isPressed)
                {
                    Vector2 dl = raton.delta.ReadValue();
                    yaw += dl.x * 0.2f;
                    pitch = Mathf.Clamp(pitch - dl.y * 0.2f, -85f, 85f);
                }
                float rueda = raton.scroll.ReadValue().y;
                if (Mathf.Abs(rueda) > 0.01f) distancia = Mathf.Clamp(distancia * (rueda > 0f ? 0.9f : 1.1f), 10f, 220f);
            }
            Transform nave = prueba.Vista.Nave;
            Vector3 f = nave != null ? nave.TransformPoint(foco) : foco;
            Camara.transform.position = f + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distancia);
            Camara.transform.LookAt(f);
        }

        /// <summary>Cámara libre PROVISIONAL (sin colisiones, sin «abajo»): no es la mecánica de movimiento interior.</summary>
        void CamaraInspeccion()
        {
            Keyboard k = Keyboard.current;
            Mouse raton = Mouse.current;
            float dt = Time.deltaTime;
            if (raton != null && raton.rightButton.isPressed)
            {
                Vector2 dl = raton.delta.ReadValue();
                giro = giro * Quaternion.Euler(-dl.y * 0.15f, dl.x * 0.15f, 0f);
            }
            if (k != null)
            {
                if (k.qKey.isPressed) giro = giro * Quaternion.Euler(0f, 0f, 60f * dt);
                if (k.eKey.isPressed) giro = giro * Quaternion.Euler(0f, 0f, -60f * dt);
                if (k.rKey.wasPressedThisFrame)
                {
                    Vector3 adelante = giro * Vector3.forward;
                    if (Mathf.Abs(Vector3.Dot(adelante, Vector3.up)) < 0.98f) giro = Quaternion.LookRotation(adelante, Vector3.up);
                }
                Vector3 mov = Vector3.zero;
                if (k.wKey.isPressed) mov += Vector3.forward;
                if (k.sKey.isPressed) mov += Vector3.back;
                if (k.dKey.isPressed) mov += Vector3.right;
                if (k.aKey.isPressed) mov += Vector3.left;
                if (k.spaceKey.isPressed) mov += Vector3.up;
                if (k.leftCtrlKey.isPressed) mov += Vector3.down;
                float vel = k.leftShiftKey.isPressed ? 6f : 2.2f;
                ojo += giro * mov * vel * dt;
            }
            Transform nave = prueba.Vista.Nave;
            Camara.transform.position = nave != null ? nave.TransformPoint(ojo) : ojo;
            Camara.transform.rotation = (nave != null ? nave.rotation : Quaternion.identity) * giro;
        }
    }
}
