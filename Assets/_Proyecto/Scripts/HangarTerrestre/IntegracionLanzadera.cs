using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// ZONA B · INTEGRACIÓN DE LA LANZADERA L-4 (vehículo TERRESTRE). Solo representación y vista: la lanzadera en vertical
    /// sobre su plataforma, rótulos de las piezas modificables, puente grúa, resaltado, previsualización con animación
    /// guiada de retirada y colocación (sin física) y cámara propia para verla a escala.
    ///
    /// Estado propio y separado del holograma de la EM-4X: aquí solo se toca PreparacionExpedicion.lanzadera, y solo al
    /// confirmar (HangarTerrestre.EncargarLanzadera). Previsualizar no cambia nada.
    /// </summary>
    public class IntegracionLanzadera : MonoBehaviour
    {
        public bool Activo { get; private set; }
        public PiezaLanzadera Seleccion { get; private set; } = PiezaLanzadera.Bodega;
        /// <summary>Alternativa previsualizada (−1 = ninguna: se ve la configuración actual).</summary>
        public int Candidata { get; private set; } = -1;
        public bool Animando { get; private set; }
        /// <summary>El HUD lo activa con el ratón sobre el panel (la rueda desplaza el panel, no acerca la vista).</summary>
        public bool RuedaBloqueada { get; set; }

        HangarTerrestre h;
        Transform raiz, gancho, cable, puente;
        Vector3 basePos;
        Func<Color, bool, Material> fabrica;
        Material mResaltado, mNueva;
        ModeloLanzadera modelo;
        readonly Dictionary<PiezaLanzadera, TextMesh> rotulos = new Dictionary<PiezaLanzadera, TextMesh>();
        Camera cam;
        float yaw = 100f, pitch = 12f, distancia = 16f;     // de inicio, desde el lado de la consola (oeste)
        const float AlturaPuente = 42f;

        /// <summary>«basePos» = punto (mundo) donde apoya la base de la etapa principal, encima de la plataforma.</summary>
        public void Construir(HangarTerrestre hangar, Transform padre, Vector3 basePos, Func<Color, bool, Material> fabrica,
                              Material resaltado, Material nueva, Font fuente)
        {
            h = hangar;
            this.basePos = basePos;
            this.fabrica = fabrica;
            mResaltado = resaltado;
            mNueva = nueva;
            raiz = new GameObject("Lanzadera en integración").transform;
            raiz.SetParent(padre, false);

            // Puente grúa (rieles fijos en el edificio; el puente y el gancho se mueven en la animación)
            Material acero = fabrica(new Color(0.95f, 0.75f, 0.15f), false), oscuro = fabrica(new Color(0.15f, 0.16f, 0.18f), false);
            puente = Bloque("Puente grúa", raiz, new Vector3(basePos.x, AlturaPuente, basePos.z), new Vector3(1.2f, 1.4f, 34f), acero);
            gancho = Bloque("Carro y gancho", puente, new Vector3(0f, -1.2f, 0f), new Vector3(1.6f / 1.2f, 0.8f / 1.4f, 1.6f / 34f), oscuro);
            cable = Bloque("Cable", raiz, Vector3.zero, new Vector3(0.12f, 1f, 0.12f), oscuro);
            cable.gameObject.SetActive(false);

            foreach (PiezaLanzadera t in new[] { PiezaLanzadera.Bodega, PiezaLanzadera.Tanque, PiezaLanzadera.Refuerzos })
            {
                GameObject g = new GameObject("Rótulo · " + t);
                g.transform.SetParent(raiz, false);
                TextMesh tm = g.AddComponent<TextMesh>();
                tm.font = fuente;
                tm.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
                tm.fontSize = 48;
                tm.characterSize = 0.09f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(1f, 0.85f, 0.4f);
                g.AddComponent<RotuloHaciaCamara>();
                rotulos[t] = tm;
            }

            GameObject cg = new GameObject("Cámara de la integración");
            cam = cg.AddComponent<Camera>();
            cam.enabled = false;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
        }

        static Transform Bloque(string n, Transform padre, Vector3 pos, Vector3 esc, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            Collider c = g.GetComponent<Collider>(); c.enabled = false; Destroy(c);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = esc;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        int Actual(PiezaLanzadera t) => DatosLanzadera.Indice(h.Borrador.lanzadera, t);

        /// <summary>Vuelve a montar la lanzadera con la configuración actual (sin animación) y actualiza los rótulos.</summary>
        public void Actualizar()
        {
            if (raiz == null || h.Borrador == null) return;
            StopAllCoroutines();
            Animando = false;
            Candidata = -1;
            cable.gameObject.SetActive(false);
            Montar(h.Borrador.lanzadera, null);
        }

        void Montar(ConfiguracionLanzadera c, PiezaLanzadera? nueva)
        {
            if (modelo != null) modelo.Destruir();
            ConfiguracionLanzadera cl = c.Copia();
            DatosLanzadera.Limitar(cl);
            modelo = ModeloLanzadera.Construir(raiz, basePos, cl, fabrica, Activo ? Seleccion : (PiezaLanzadera?)null, mResaltado);
            if (nueva.HasValue) modelo.PintarPieza(nueva.Value, mNueva);
            foreach (var kv in rotulos)
            {
                OpcionLanzadera o = DatosLanzadera.Opciones(kv.Key)[DatosLanzadera.Indice(cl, kv.Key)];
                kv.Value.text = DatosLanzadera.NombrePieza(kv.Key) + " · modificable\n" + o.nombre;
                // A un lado del vehículo, mirando hacia la consola (oeste)
                kv.Value.transform.position = basePos + new Vector3(-6.5f, modelo.AlturaPieza[kv.Key], kv.Key == PiezaLanzadera.Bodega ? 3.5f : kv.Key == PiezaLanzadera.Tanque ? -3.5f : 0f);
                kv.Value.color = Activo && kv.Key == Seleccion ? new Color(1f, 0.95f, 0.4f) : new Color(1f, 0.8f, 0.45f);
            }
        }

        // ------------------------------------------------------------------ Modo de consulta (consola B)

        public void Entrar(Camera camaraPersonaje)
        {
            Activo = true;
            if (camaraPersonaje != null) camaraPersonaje.enabled = false;
            cam.enabled = true;
            cam.tag = "MainCamera";
            Actualizar();
        }

        public void Salir(Camera camaraPersonaje)
        {
            Activo = false;
            Actualizar();                // previsualización sin confirmar: se descarta
            cam.enabled = false;
            cam.tag = "Untagged";
            if (camaraPersonaje != null) camaraPersonaje.enabled = true;
        }

        public void Elegir(PiezaLanzadera t)
        {
            Seleccion = t;
            Actualizar();
        }

        /// <summary>Muestra la alternativa con retirada y colocación guiadas por el puente grúa. No cambia la preparación.</summary>
        public void Previsualizar(int indice)
        {
            if (Animando) return;
            if (indice == (Candidata >= 0 ? Candidata : Actual(Seleccion))) return;
            StartCoroutine(Cambio(indice));
        }

        IEnumerator Cambio(int indice)
        {
            Animando = true;
            PiezaLanzadera t = Seleccion;
            Vector3 fuera = new Vector3(-9f, 0f, 0f);         // la grúa la saca hacia el lado de la consola (paso libre de torres)

            // 1 · Retirada de la pieza que se ve ahora (si hay)
            Transform vieja;
            if (modelo.Piezas.TryGetValue(t, out vieja) && vieja != null)
            {
                Vector3 v0 = vieja.localPosition;
                for (float s = 0f; s < 1f; s += Time.deltaTime / 0.9f)
                {
                    vieja.localPosition = v0 + fuera * Mathf.SmoothStep(0f, 1f, s);
                    Grua(vieja);
                    yield return null;
                }
            }

            // 2 · Montaje con la alternativa y colocación de la pieza nueva (si hay)
            ConfiguracionLanzadera c = h.Borrador.lanzadera.Copia();
            DatosLanzadera.Fijar(c, t, indice);
            bool esActual = indice == Actual(t);
            Montar(c, esActual ? (PiezaLanzadera?)null : t);
            Transform nueva;
            if (modelo.Piezas.TryGetValue(t, out nueva) && nueva != null)
            {
                Vector3 n1 = nueva.localPosition;
                for (float s = 0f; s < 1f; s += Time.deltaTime / 0.9f)
                {
                    nueva.localPosition = n1 + fuera * (1f - Mathf.SmoothStep(0f, 1f, s));
                    Grua(nueva);
                    yield return null;
                }
                nueva.localPosition = n1;
            }
            cable.gameObject.SetActive(false);
            Candidata = esActual ? -1 : indice;
            Animando = false;
        }

        /// <summary>Lleva el puente y el gancho encima de la pieza y tiende el cable hasta ella.</summary>
        void Grua(Transform pieza)
        {
            Bounds b = new Bounds(pieza.position, Vector3.zero);
            foreach (Renderer r in pieza.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            puente.position = new Vector3(b.center.x, AlturaPuente, puente.position.z);
            gancho.position = new Vector3(b.center.x, AlturaPuente - 1.2f, b.center.z);
            float arriba = AlturaPuente - 1.6f, abajo = b.max.y;
            cable.gameObject.SetActive(arriba > abajo);
            cable.position = new Vector3(b.center.x, (arriba + abajo) / 2f, b.center.z);
            cable.localScale = new Vector3(0.12f, Mathf.Max(0.01f, arriba - abajo), 0.12f);
        }

        void LateUpdate()
        {
            if (!Activo || cam == null || modelo == null) return;
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            if (m != null && m.rightButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue() * 0.25f;
                yaw += d.x;
                pitch = Mathf.Clamp(pitch - d.y, -10f, 70f);
            }
            if (k != null)
            {
                if (k.leftArrowKey.isPressed) yaw -= 60f * Time.deltaTime;
                if (k.rightArrowKey.isPressed) yaw += 60f * Time.deltaTime;
                if (k.upArrowKey.isPressed) pitch = Mathf.Clamp(pitch + 40f * Time.deltaTime, -10f, 70f);
                if (k.downArrowKey.isPressed) pitch = Mathf.Clamp(pitch - 40f * Time.deltaTime, -10f, 70f);
            }
            if (m != null && !RuedaBloqueada)
            {
                float rueda = m.scroll.ReadValue().y;
                if (Mathf.Abs(rueda) > 0.01f) distancia = Mathf.Clamp(distancia * (rueda > 0f ? 0.9f : 1.1f), 8f, 40f);
            }
            Vector3 foco = basePos + Vector3.up * modelo.AlturaPieza[Seleccion];
            Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
            float d0 = distancia;
            // La cámara no atraviesa las paredes del edificio (solo cuentan las que se llaman «Edificio…»)
            foreach (RaycastHit hit in Physics.RaycastAll(foco, dir, distancia, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.name.StartsWith("Edificio") || hit.collider.name.StartsWith("Pared este")) d0 = Mathf.Min(d0, hit.distance - 0.6f);
            cam.transform.position = foco + dir * Mathf.Max(3f, d0);
            cam.transform.rotation = Quaternion.LookRotation(foco - cam.transform.position, Vector3.up);
            cam.transform.position += cam.transform.right * (Mathf.Max(3f, d0) * 0.2f);   // deja sitio al panel de la derecha
        }
    }
}
