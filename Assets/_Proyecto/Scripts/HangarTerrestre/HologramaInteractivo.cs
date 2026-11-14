using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    public enum PiezaHolo { Motor, Tanque, Instrumento }

    /// <summary>
    /// HOLOGRAMA INTERACTIVO de la EM-4X (solo visual y vista; los cambios los aplica HangarTerrestre con los datos de
    /// siempre). Es una PREVISUALIZACIÓN de la configuración: la EM-4X está en órbita, no en el hangar.
    ///
    /// · Módulos: las cajas de NaveModular a escala 1:15 (sin cambiar distribución ni medidas).
    /// · Piezas provisionales que cambian con la elección: motor (detrás de la etapa de propulsión), tanque (dentro de la
    ///   etapa, largo según capacidad) e instrumento (cápsula ampliada junto a Ciencia, ×2 para que se vea).
    /// · Modo de consulta: cámara propia que gira alrededor (botón derecho o ←/→, ↑/↓ y rueda), resaltado de la pieza y
    ///   animación de separación y encaje al previsualizar una alternativa.
    /// </summary>
    public class HologramaInteractivo : MonoBehaviour
    {
        const float Escala = 1f / 15f;

        public bool Activo { get; private set; }
        public PiezaHolo Seleccion { get; private set; } = PiezaHolo.Motor;
        /// <summary>Alternativa que se está previsualizando (−1 = ninguna: se ve la actual).</summary>
        public int Candidata { get; private set; } = -1;
        public bool Animando { get; private set; }
        /// <summary>El HUD lo activa cuando el ratón está sobre el panel (la rueda desplaza el panel, no acerca la vista).</summary>
        public bool RuedaBloqueada { get; set; }

        HangarTerrestre h;
        Transform raiz, nave, rotuloRaiz;
        TextMesh rotulo;
        Camera cam;
        Material mHolo, mMarcado, mPieza, mPiezaNueva;
        readonly Dictionary<string, Renderer> modulos = new Dictionary<string, Renderer>();
        readonly Dictionary<PiezaHolo, Transform> piezas = new Dictionary<PiezaHolo, Transform>();
        float yaw = 30f, pitch = 22f, distancia = 7f;

        public void Construir(HangarTerrestre hangar, Transform padre, Vector3 pos, Material holo, Material marcado, Material pieza, Material piezaNueva)
        {
            h = hangar;
            mHolo = holo; mMarcado = marcado; mPieza = pieza; mPiezaNueva = piezaNueva;
            raiz = new GameObject("Holograma de la EM-4X (en órbita · previsualización)").transform;
            raiz.SetParent(padre, false);
            raiz.localPosition = pos;
            nave = new GameObject("Escala 1:15").transform;
            nave.SetParent(raiz, false);
            nave.localScale = Vector3.one * Escala;
            nave.localPosition = new Vector3(0f, 0f, 11f * Escala);     // centra la nave (de z −41 a 18,7)
            foreach (ModuloNave m in DefinicionNaveModular.ExpedicionEM4X().modulos)
            {
                Bounds b = m.Limites;
                modulos[m.id] = Caja("Módulo " + m.id, nave, b.center, b.size, mHolo).GetComponent<Renderer>();
            }
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rotuloRaiz = new GameObject("Rótulo del holograma").transform;
            rotuloRaiz.SetParent(padre, false);
            rotuloRaiz.localPosition = pos + new Vector3(0f, 2.3f, 0f);
            rotulo = rotuloRaiz.gameObject.AddComponent<TextMesh>();
            rotulo.font = f;
            rotulo.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            rotulo.fontSize = 48;
            rotulo.characterSize = 0.05f;
            rotulo.anchor = TextAnchor.MiddleCenter;
            rotulo.alignment = TextAlignment.Center;
            rotulo.color = new Color(0.6f, 0.95f, 1f);
            rotuloRaiz.gameObject.AddComponent<RotuloHaciaCamara>();

            GameObject cg = new GameObject("Cámara del holograma");
            cam = cg.AddComponent<Camera>();
            cam.enabled = false;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 45f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f);
        }

        static Transform Caja(string nombre, Transform padre, Vector3 c, Vector3 s, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nombre;
            Collider col = g.GetComponent<Collider>(); col.enabled = false; Destroy(col);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = c;
            g.transform.localScale = s;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        static Transform Cilindro(string nombre, Transform padre, Vector3 c, float diametro, float largo, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = nombre;
            Collider col = g.GetComponent<Collider>(); col.enabled = false; Destroy(col);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = c;
            g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);    // eje a lo largo de z
            g.transform.localScale = new Vector3(diametro, largo / 2f, diametro);
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        // ------------------------------------------------------------------ Piezas provisionales (coordenadas de la nave, m)

        /// <summary>Construye la pieza de una alternativa. El tamaño refleja sus datos; no es el modelo final.</summary>
        Transform CrearPieza(PiezaHolo tipo, int indice, Material m)
        {
            Transform p = new GameObject(tipo + " " + indice).transform;
            p.SetParent(nave, false);
            if (tipo == PiezaHolo.Motor)
            {
                OpcionMotorNave o = BalanceEM4X.Motores(h.Catalogo.catalogoNave)[indice];
                float d = 4.5f * Mathf.Sqrt(o.empujeN / 400000f);            // tobera más ancha con más empuje
                float l = 4f * (o.empujeN / o.consumoKgS) / 4000f;          // más larga cuanto más eficiente
                p.localPosition = new Vector3(0f, 0f, -37f - l / 2f);
                Cilindro("Tobera", p, Vector3.zero, d, l, m);
                if (o.empujeN > 450000f)
                    for (int i = 0; i < 4; i++)
                        Cilindro("Tobera auxiliar", p, new Vector3((i % 2 == 0 ? 1 : -1) * d * 0.55f, (i < 2 ? 1 : -1) * d * 0.55f, 0.3f), 1f, l * 0.6f, m);
            }
            else if (tipo == PiezaHolo.Tanque)
            {
                OpcionTanqueNave o = BalanceEM4X.Tanques(h.Catalogo.catalogoNave)[indice];
                float l = 13f * Mathf.Clamp01(o.capacidadKg / 36000f);       // dentro de la etapa de propulsión (15 m)
                p.localPosition = new Vector3(0f, 0f, -23f - l / 2f);
                Cilindro("Tanque", p, Vector3.zero, 4.6f, l, m);
            }
            else
            {
                List<OpcionInstrumento> lista = BalanceEM4X.Instrumentos(h.Catalogo);
                string n = lista[indice].nombre;
                p.localPosition = new Vector3(-17.2f, 0f, 1.5f);              // junto al fondo de Ciencia (x = −15,2), ampliado ×2
                if (n.StartsWith("Cámara")) Cilindro("Objetivo", p, Vector3.zero, 2.2f, 3f, m).localRotation = Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(0f, 90f, 0f);
                else if (n.StartsWith("Espectr")) Caja("Espectrómetro", p, Vector3.zero, new Vector3(3f, 2.4f, 2.4f), m);
                else
                {
                    GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Collider col = s.GetComponent<Collider>(); col.enabled = false; Destroy(col);
                    s.transform.SetParent(p, false);
                    s.transform.localScale = Vector3.one * 2.2f;
                    s.GetComponent<Renderer>().sharedMaterial = m;
                    Caja("Mástil", p, new Vector3(1.2f, 0f, 0f), new Vector3(2f, 0.3f, 0.3f), m);
                }
            }
            return p;
        }

        static Vector3 DireccionSalida(PiezaHolo t) =>
            t == PiezaHolo.Motor ? new Vector3(0f, 0f, -9f) : t == PiezaHolo.Tanque ? new Vector3(0f, 9f, 0f) : new Vector3(-8f, 0f, 0f);

        int Actual(PiezaHolo t) => t == PiezaHolo.Motor ? h.Borrador.motor : t == PiezaHolo.Tanque ? h.Borrador.tanque : h.Borrador.instrumento;

        /// <summary>Vuelve a dibujar las piezas actuales y los módulos marcados (sin animación).</summary>
        public void Actualizar()
        {
            if (nave == null || h.Borrador == null) return;
            StopAllCoroutines();
            Animando = false;
            Candidata = -1;
            foreach (PiezaHolo t in new[] { PiezaHolo.Motor, PiezaHolo.Tanque, PiezaHolo.Instrumento })
            {
                Transform viejo;
                if (piezas.TryGetValue(t, out viejo) && viejo != null) Destroy(viejo.gameObject);
                piezas[t] = CrearPieza(t, Actual(t), mPieza);
            }
            Marcar();
            ResumenExpedicion r = h.Resumen;
            rotulo.text = "EM-4X · EN ÓRBITA (holograma 1:15 · previsualización)\n" +
                          $"{r.motor.nombre} · {r.tanque.nombre}\n{r.instrumento.nombre}\n" +
                          $"combustible {r.combustibleKg / 1000f:0.0} de {r.capacidadKg / 1000f:0} t · Δv ≈ {r.deltaVMS:0} m/s\n" +
                          "amarillo = cambio encargado para la nave en órbita · E: consultar";
        }

        void Marcar()
        {
            foreach (var kv in modulos)
            {
                bool encargado = (kv.Key == "PR" && (h.Borrador.motor > 0 || h.Borrador.tanque > 0)) || (kv.Key == "CI" && h.Borrador.instrumento > 0)
                                 || (kv.Key == "CO" && h.Borrador.relevo > 0);
                bool resaltado = Activo && ((kv.Key == "PR" && Seleccion != PiezaHolo.Instrumento) || (kv.Key == "CI" && Seleccion == PiezaHolo.Instrumento));
                kv.Value.sharedMaterial = encargado || resaltado ? mMarcado : mHolo;
            }
        }

        // ------------------------------------------------------------------ Modo de consulta

        public void Entrar(Camera camaraPersonaje)
        {
            Activo = true;
            if (camaraPersonaje != null) camaraPersonaje.enabled = false;
            cam.enabled = true;
            cam.tag = "MainCamera";
            Marcar();
        }

        public void Salir(Camera camaraPersonaje)
        {
            if (Candidata >= 0 || Animando) Actualizar();      // previsualización sin encargar (o a medias): se descarta
            Activo = false;
            cam.enabled = false;
            cam.tag = "Untagged";
            if (camaraPersonaje != null) camaraPersonaje.enabled = true;
            Marcar();
        }

        public void Elegir(PiezaHolo t)
        {
            if (Candidata >= 0 || Animando) Actualizar();
            Seleccion = t;
            Marcar();
        }

        /// <summary>Muestra la alternativa con la animación de separación y encaje. No cambia la preparación.</summary>
        public void Previsualizar(int indice)
        {
            if (Animando) return;
            if (indice == Actual(Seleccion)) { if (Candidata >= 0) StartCoroutine(Cambio(Actual(Seleccion), -1)); return; }
            StartCoroutine(Cambio(indice, indice));
        }

        IEnumerator Cambio(int indiceVisible, int candidata)
        {
            Animando = true;
            PiezaHolo t = Seleccion;
            Transform vieja = piezas[t];
            Vector3 salida = DireccionSalida(t);
            Vector3 v0 = vieja.localPosition;
            for (float s = 0f; s < 1f; s += Time.deltaTime / 0.7f)
            {
                vieja.localPosition = v0 + salida * Mathf.SmoothStep(0f, 1f, s);
                yield return null;
            }
            Destroy(vieja.gameObject);
            Transform nueva = CrearPieza(t, indiceVisible, candidata >= 0 ? mPiezaNueva : mPieza);
            piezas[t] = nueva;     // registrada ya: si se interrumpe la animación, Actualizar la elimina
            Vector3 n1 = nueva.localPosition;
            for (float s = 0f; s < 1f; s += Time.deltaTime / 0.7f)
            {
                nueva.localPosition = n1 + salida * (1f - Mathf.SmoothStep(0f, 1f, s));
                yield return null;
            }
            nueva.localPosition = n1;
            piezas[t] = nueva;
            Candidata = candidata;
            Animando = false;
        }

        void LateUpdate()
        {
            if (raiz == null) return;
            if (!Activo)
            {
                raiz.Rotate(0f, 12f * Time.deltaTime, 0f, Space.World);    // giro lento en reposo
                return;
            }
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            if (m != null && m.rightButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue() * 0.25f;
                yaw += d.x;
                pitch = Mathf.Clamp(pitch - d.y, -10f, 75f);
            }
            if (k != null)
            {
                if (k.leftArrowKey.isPressed) yaw -= 60f * Time.deltaTime;
                if (k.rightArrowKey.isPressed) yaw += 60f * Time.deltaTime;
                if (k.upArrowKey.isPressed) pitch = Mathf.Clamp(pitch + 40f * Time.deltaTime, -10f, 75f);
                if (k.downArrowKey.isPressed) pitch = Mathf.Clamp(pitch - 40f * Time.deltaTime, -10f, 75f);
            }
            if (m != null && !RuedaBloqueada)
            {
                float rueda = m.scroll.ReadValue().y;
                if (Mathf.Abs(rueda) > 0.01f) distancia = Mathf.Clamp(distancia * (rueda > 0f ? 0.9f : 1.1f), 3.5f, 14f);
            }
            Quaternion q = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 foco = raiz.position;
            cam.transform.position = foco - q * Vector3.forward * distancia;
            cam.transform.rotation = Quaternion.LookRotation(foco - cam.transform.position, Vector3.up);
            // la cámara se desplaza a la izquierda para que el panel de la derecha no tape la nave
            cam.transform.position += cam.transform.right * (distancia * 0.18f);
        }
    }
}
