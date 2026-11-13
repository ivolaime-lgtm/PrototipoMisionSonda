using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Puertas deslizantes PROVISIONALES de una unión (INTERACCIÓN + VISUAL SENCILLO). Todas las puertas de la unión se
    /// mueven juntas: así el paso completo se abre o se cierra desde cualquiera de sus paneles, sin tener que cruzar una
    /// puerta cerrada para alcanzar el control.
    ///   · Las hojas se deslizan a los lados (movimiento visible, 1,2 s); no desaparecen.
    ///   · Cerradas bloquean el paso (colisión); abiertas quedan fuera del corredor de la escotilla.
    ///   · Protección: no empieza a cerrar si hay alguien en la zona de las hojas o entre las dos puertas, y si alguien
    ///     entra mientras cierra, vuelve a abrir. Nadie queda atrapado entre las dos puertas de una unión.
    /// Sin presión, oxígeno, separación ni atraque.
    /// </summary>
    public class PuertasUnion : MonoBehaviour
    {
        public enum Estado { Cerrada, Abriendo, Abierta, Cerrando }

        public UnionConPuertas Datos { get; private set; }
        public Estado EstadoActual { get; private set; } = Estado.Cerrada;
        public float Apertura { get; private set; }
        public string Mensaje { get; private set; } = "";
        public float MensajeHasta { get; private set; }
        public bool Abierta => EstadoActual == Estado.Abierta;

        DefinicionNaveModular d;
        readonly List<(Rigidbody rb, Transform marco, ModuloNave m, PuertoModulo p, int etapas, int indice)> hojas =
            new List<(Rigidbody rb, Transform marco, ModuloNave m, PuertoModulo p, int etapas, int indice)>();
        readonly List<Renderer> luces = new List<Renderer>();
        readonly List<Bounds> zonas = new List<Bounds>();
        Material matHoja, matGuia, matUnion, matPanel, matLuzCerrada, matLuzAbierta, matLuzMovimiento;
        Font fuente;

        // ------------------------------------------------------------------ Construcción

        public void Construir(DefinicionNaveModular def, UnionConPuertas union, Transform padre, Font f)
        {
            d = def;
            Datos = union;
            fuente = f;
            name = "Puertas · " + union.nombre;
            transform.SetParent(padre, false);
            matHoja = Mat(new Color(0.88f, 0.74f, 0.45f));
            matGuia = Mat(new Color(0.25f, 0.27f, 0.3f));
            matUnion = Mat(new Color(0.95f, 0.8f, 0.1f));
            matPanel = Mat(new Color(0.18f, 0.2f, 0.24f));
            matLuzCerrada = Mat(new Color(0.9f, 0.15f, 0.12f));
            matLuzAbierta = Mat(new Color(0.2f, 0.85f, 0.3f));
            matLuzMovimiento = Mat(new Color(1f, 0.7f, 0.1f));

            Bounds union_ = new Bounds(); bool primera = true;
            foreach (PuertaNave pu in union.puertas)
            {
                ModuloNave m = d.Modulo(pu.modulo);
                PuertoModulo p = m?.Puerto(pu.puerto);
                if (p == null) continue;
                Transform marco = new GameObject($"Puerta en {m.nombre}").transform;
                marco.SetParent(transform, false);
                marco.localPosition = m.origen;
                marco.localRotation = m.Rotacion;

                List<CajaLocal> cerradas = GeometriaPuertas.Hojas(m, p, pu.etapas, 0f);
                for (int i = 0; i < cerradas.Count; i++)
                {
                    GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    g.name = "Hoja " + (i + 1);
                    g.transform.SetParent(marco, false);
                    g.transform.localPosition = cerradas[i].centro;
                    g.transform.localScale = cerradas[i].tamano;
                    g.GetComponent<Renderer>().sharedMaterial = matHoja;
                    Rigidbody rb = g.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    hojas.Add((rb, marco, m, p, pu.etapas, i));
                }
                foreach (CajaLocal gu in GeometriaPuertas.Guias(m, p, pu.etapas))
                {
                    GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);   // con su BoxCollider
                    g.name = "Guía";
                    g.transform.SetParent(marco, false);
                    g.transform.localPosition = gu.centro;
                    g.transform.localScale = gu.tamano;
                    g.GetComponent<Renderer>().sharedMaterial = matGuia;
                }
                Bounds z = GeometriaPuertas.AMundo(m, GeometriaPuertas.ZonaCierre(m, p, pu.etapas));
                zonas.Add(z);
                if (primera) { union_ = z; primera = false; } else union_.Encapsulate(z);
            }
            // Unión separable: la zona entre las dos puertas también debe quedar libre para cerrar
            if (union.tipo == TipoPaso.UnionSeparable && !primera) zonas.Add(union_);
            if (union.tipo == TipoPaso.UnionSeparable) MarcaUnion();

            foreach (PanelNave pn in union.paneles) Panel(pn);
            if (union.tipo == TipoPaso.Aislamiento)
                Rotulo("PUERTA DE AISLAMIENTO\n(normalmente abierta)", PosicionRotuloPuerta(), 0.02f);
            FijarParaComprobacion(union.abiertaAlInicio);
        }

        /// <summary>Quita la colisión de un bloque decorativo ya (Destroy solo actúa al final del fotograma).</summary>
        static void QuitarColision(GameObject g)
        {
            Collider c = g.GetComponent<Collider>();
            if (c == null) return;
            c.enabled = false;
            Destroy(c);
        }

        static Material Mat(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        /// <summary>Anillo amarillo en el adaptador: unión reconocible de un módulo separable en el futuro.</summary>
        void MarcaUnion()
        {
            ConexionModulos k = null;
            foreach (ConexionModulos c in d.conexiones)
                if ((c.moduloA == Datos.moduloA && c.moduloB == Datos.moduloB) || (c.moduloA == Datos.moduloB && c.moduloB == Datos.moduloA)) k = c;
            if (k == null) return;
            ModuloNave a = d.Modulo(k.moduloA), b = d.Modulo(k.moduloB);
            PuertoModulo pa = a.Puerto(k.puertoA), pb = b.Puerto(k.puertoB);
            Vector3 wa = a.AMundo(pa.posicion), wb = b.AMundo(pb.posicion), dir = (wb - wa).normalized;
            Transform t = new GameObject("Unión separable (marca)").transform;
            t.SetParent(transform, false);
            t.localPosition = (wa + wb) / 2f;
            t.localRotation = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.forward);
            float ext = DefinicionNaveModular.ExteriorAdaptador(pa.lado) + 0.08f;
            float w = ext * Mathf.Tan(22.5f * Mathf.Deg2Rad) + 0.02f;
            for (int i = 0; i < 8; i++)
            {
                float an = i * 45f * Mathf.Deg2Rad;
                Vector3 n = new Vector3(Mathf.Cos(an), Mathf.Sin(an), 0f);
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                QuitarColision(g);
                g.name = "Anillo de unión";
                g.transform.SetParent(t, false);
                g.transform.localPosition = n * (ext / 2f);
                g.transform.localRotation = Quaternion.LookRotation(Vector3.forward, n);
                g.transform.localScale = new Vector3(w, 0.06f, 0.2f);
                g.GetComponent<Renderer>().sharedMaterial = matUnion;
            }
            Rotulo("UNIÓN SEPARABLE (futuro)\n" + Datos.nombre, (wa + wb) / 2f + (Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.forward) * (ext / 2f + 0.6f), 0.03f);
        }

        void Panel(PanelNave pn)
        {
            ModuloNave m = d.Modulo(pn.modulo);
            if (m == null) return;
            CajaLocal c = GeometriaPuertas.Panel(pn);
            Transform marco = new GameObject($"Panel en {m.nombre}").transform;
            marco.SetParent(transform, false);
            marco.localPosition = m.origen;
            marco.localRotation = m.Rotacion;
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);   // con colisión: se apunta con la mirada
            g.name = "Panel de puerta";
            g.transform.SetParent(marco, false);
            g.transform.localPosition = c.centro;
            g.transform.localScale = c.tamano;
            g.GetComponent<Renderer>().sharedMaterial = matPanel;
            g.AddComponent<PanelPuerta>().Union = this;
            GameObject luz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            QuitarColision(luz);
            luz.name = "Luz";
            luz.transform.SetParent(marco, false);
            Vector3 n = pn.normal.normalized;
            luz.transform.localPosition = c.centro + n * (DefinicionCirculacion.GrosorPanel / 2f + 0.01f);
            luz.transform.localScale = Vector3.one * 0.08f - new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z)) * 0.06f;
            luces.Add(luz.GetComponent<Renderer>());
            Rotulo("E · " + Datos.nombre, m.AMundo(c.centro + n * 0.25f), 0.012f);
        }

        Vector3 PosicionRotuloPuerta()
        {
            if (hojas.Count == 0) return Vector3.zero;
            var h = hojas[0];
            GeometriaPuertas.Ejes(h.p, out Vector3 n, out Vector3 s, out Vector3 alto);
            if (Vector3.Dot(h.m.DireccionAMundo(alto), h.m.Arriba) < 0f) alto = -alto;   // rótulo por encima del hueco
            Vector3 centro = GeometriaPuertas.SuperficieInterior(h.m, h.p) - n * 0.3f + alto * (h.p.lado / 2f + 0.35f);
            return h.m.AMundo(centro);
        }

        void Rotulo(string texto, Vector3 posicion, float tamano)
        {
            if (fuente == null) return;
            GameObject g = new GameObject("Rótulo");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = posicion;
            TextMesh tm = g.AddComponent<TextMesh>();
            tm.font = fuente;
            tm.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            tm.text = texto;
            tm.fontSize = 48;
            tm.characterSize = tamano;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.85f, 0.4f);
            g.AddComponent<RotuloHaciaCamara>();
        }

        // ------------------------------------------------------------------ Uso

        void Avisar(string texto) { Mensaje = texto; MensajeHasta = Time.time + 3f; }

        /// <summary>Abre si está cerrada o cerrando; cierra si está abierta o abriendo (si la zona está libre).</summary>
        public void Alternar()
        {
            if (EstadoActual == Estado.Cerrada || EstadoActual == Estado.Cerrando)
            {
                EstadoActual = Estado.Abriendo;
                Avisar(Datos.nombre + ": abriendo.");
            }
            else if (ZonaOcupada(out string quien))
                Avisar(Datos.nombre + ": no se cierra, hay alguien en el paso (" + quien + ").");
            else
            {
                EstadoActual = Estado.Cerrando;
                Avisar(Datos.nombre + ": cerrando.");
            }
        }

        /// <summary>¿Hay un cuerpo móvil (el personaje) en la zona de las hojas o entre las dos puertas?</summary>
        public bool ZonaOcupada(out string quien)
        {
            quien = "";
            foreach (Bounds z in zonas)
                foreach (Collider c in Physics.OverlapBox(z.center, z.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    Rigidbody rb = c.attachedRigidbody;
                    if (rb != null && !rb.isKinematic) { quien = rb.name; return true; }
                }
            return false;
        }

        float aperturaColocada = -1f;

        void FixedUpdate()
        {
            float paso = Time.fixedDeltaTime / DefinicionCirculacion.SegundosPuerta;
            if (EstadoActual == Estado.Abriendo)
            {
                Apertura = Mathf.Min(1f, Apertura + paso);
                if (Apertura >= 1f) EstadoActual = Estado.Abierta;
            }
            else if (EstadoActual == Estado.Cerrando)
            {
                if (ZonaOcupada(out string quien))
                {
                    EstadoActual = Estado.Abriendo;   // protección: vuelve a abrir
                    Avisar(Datos.nombre + ": se detiene y vuelve a abrir, hay alguien en el paso (" + quien + ").");
                }
                else
                {
                    Apertura = Mathf.Max(0f, Apertura - paso);
                    if (Apertura <= 0f) EstadoActual = Estado.Cerrada;
                }
            }
            if (!Mathf.Approximately(Apertura, aperturaColocada)) { Mover(Apertura, false); aperturaColocada = Apertura; }
        }

        void Update()
        {
            Material m = EstadoActual == Estado.Abierta ? matLuzAbierta : EstadoActual == Estado.Cerrada ? matLuzCerrada : matLuzMovimiento;
            foreach (Renderer r in luces) if (r != null && r.sharedMaterial != m) r.sharedMaterial = m;
        }

        void Mover(float apertura, bool inmediato)
        {
            var cache = new Dictionary<PuertoModulo, List<CajaLocal>>();
            foreach (var h in hojas)
            {
                if (h.rb == null) continue;
                if (!cache.TryGetValue(h.p, out List<CajaLocal> cajas))
                {
                    cajas = GeometriaPuertas.Hojas(h.m, h.p, h.etapas, apertura);
                    cache[h.p] = cajas;
                }
                if (inmediato)
                {
                    h.rb.transform.localPosition = cajas[h.indice].centro;
                    h.rb.position = h.marco.TransformPoint(cajas[h.indice].centro);   // sin deslizamiento interpolado
                }
                else h.rb.MovePosition(h.marco.TransformPoint(cajas[h.indice].centro));
            }
        }

        void Colocar(float apertura)
        {
            Apertura = apertura;
            Mover(apertura, true);
            aperturaColocada = apertura;
        }

        /// <summary>SOLO para la comprobación automática al iniciar: coloca las hojas al instante (sin animación).</summary>
        public void FijarParaComprobacion(bool abierta)
        {
            EstadoActual = abierta ? Estado.Abierta : Estado.Cerrada;
            Colocar(abierta ? 1f : 0f);
        }
    }

    /// <summary>Marca un bloque como panel de una unión de puertas (lo usa el personaje al pulsar E).</summary>
    public class PanelPuerta : MonoBehaviour
    {
        public PuertasUnion Union;
    }

    /// <summary>Rótulo que mira a la cámara activa.</summary>
    public class RotuloHaciaCamara : MonoBehaviour
    {
        void LateUpdate()
        {
            Camera c = Camera.main;
            if (c != null) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position, c.transform.up);
        }
    }
}
