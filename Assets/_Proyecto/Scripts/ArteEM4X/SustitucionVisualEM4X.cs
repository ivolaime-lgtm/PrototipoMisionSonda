using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// PUNTO DE SUSTITUCIÓN VISUAL de la EM-4X (solo visual). Se aplica DESPUÉS de construir la nave, sus colisiones, las
    /// puertas y el detalle interior.
    ///
    /// Para cada entrada de RecursosVisualesEM4X con modelo:
    ///   1. Coloca el modelo como hijo de la pieza provisional (o del marco del módulo), con el pivote acordado.
    ///   2. Quita del modelo cualquier colisión o cuerpo físico que traiga: no se añaden físicas por cada pieza decorativa.
    ///   3. La pieza provisional deja de DIBUJARSE (forceRenderingOff) pero conserva su colisión, su nombre y su lugar en la
    ///      jerarquía. Así no cambian el movimiento, las puertas, el aislamiento, los paneles (E), los puntos de interacción,
    ///      las cámaras, el estado compartido ni el balance.
    /// Si falta el modelo, la pieza provisional sigue a la vista. F3 (en ModeloPrueba) alterna modelos y provisional.
    /// </summary>
    public class SustitucionVisualEM4X : MonoBehaviour
    {
        public class Linea
        {
            public string clave, estado;
            public bool correcto;
            public readonly List<string> detalles = new List<string>();
        }

        public readonly List<Linea> Informe = new List<Linea>();
        public bool ModelosVisibles { get; private set; } = true;
        public int Aplicadas { get; private set; }

        readonly List<Renderer> provisionales = new List<Renderer>();
        readonly List<GameObject> instancias = new List<GameObject>();
        /// <summary>Modelo ↔ pieza provisional: el modelo sigue lo que haga la pieza (p. ej. el corte de la cámara de inspección).</summary>
        readonly List<(Renderer provisional, GameObject modelo)> pares = new List<(Renderer provisional, GameObject modelo)>();
        readonly List<GameObject> cascos = new List<GameObject>();

        /// <summary>
        /// Aplica la tabla. «nave» es VistaExpedicion.Nave; «uniones», las puertas construidas; «detalle», la raíz del detalle
        /// interior del MVP (puede ser null). Llamar SIEMPRE después de ColisionesExpedicion.Anadir: si no, los modelos
        /// recibirían colisiones de caja.
        /// </summary>
        public void Aplicar(Transform nave, DefinicionNaveModular d, IList<PuertasUnion> uniones, Transform detalle, RecursosVisualesEM4X recursos)
        {
            Informe.Clear();
            if (recursos == null)
            {
                Informe.Add(new Linea { clave = "(sin tabla)", estado = "No hay tabla de recursos asignada: todo provisional.", correcto = true });
                return;
            }
            if (!recursos.usarModelos)
            {
                Informe.Add(new Linea { clave = "(desactivado)", estado = "«Usar modelos» está desactivado en la tabla: todo provisional.", correcto = true });
                return;
            }
            foreach (EntradaVisual e in recursos.entradas)
            {
                Linea l = new Linea { clave = e.Clave };
                Informe.Add(l);
                if (!e.activa) { l.estado = "desactivada"; l.correcto = true; continue; }
                if (e.prefab == null) { l.estado = "sin modelo: se ve la pieza provisional"; l.correcto = true; continue; }
                int antes = instancias.Count;
                switch (e.tipo)
                {
                    case TipoSustitucion.CascoModulo: Casco(nave, d, e, l); break;
                    case TipoSustitucion.Equipo: Equipo(nave, d, e, l); break;
                    case TipoSustitucion.HojaPuerta: Hojas(d, uniones, e, l); break;
                    case TipoSustitucion.PanelPuerta: Paneles(uniones, e, l); break;
                    default: Utileria(detalle, e, l); break;
                }
                int puestas = instancias.Count - antes;
                Aplicadas += puestas;
                if (puestas == 0 && l.estado == null) { l.estado = "NO ENCONTRADA la pieza provisional: revisa el módulo o el nombre"; l.correcto = false; }
                else if (l.estado == null) { l.estado = $"aplicada ({puestas})"; l.correcto = l.detalles.Count == 0; }
                Materiales(e.prefab, l);
            }
            foreach (Linea l in Informe)
                if (!l.correcto) Debug.LogWarning("[Arte EM-4X] " + l.clave + ": " + l.estado + " · " + string.Join(" · ", l.detalles), this);
        }

        public void MostrarModelos(bool si)
        {
            ModelosVisibles = si;
            foreach (Renderer r in provisionales) if (r != null) r.forceRenderingOff = si;
            foreach (GameObject g in cascos) if (g != null) g.SetActive(si);
        }

        /// <summary>
        /// Cada modelo de equipo, hoja, panel o utilería se ve solo si su pieza provisional está «encendida»: así el corte
        /// de la cámara de inspección también lo oculta. Los cascos no se recortan (limitación conocida).
        /// </summary>
        void LateUpdate()
        {
            foreach (var p in pares)
            {
                if (p.provisional == null || p.modelo == null) continue;
                bool ver = ModelosVisibles && p.provisional.enabled;
                if (p.modelo.activeSelf != ver) p.modelo.SetActive(ver);
            }
        }

        // ------------------------------------------------------------------ Búsqueda

        static Transform Hijo(Transform padre, string nombre)
        {
            if (padre == null) return null;
            foreach (Transform t in padre) if (t.name == nombre) return t;
            return null;
        }

        // ------------------------------------------------------------------ Tipos

        void Casco(Transform nave, DefinicionNaveModular d, EntradaVisual e, Linea l)
        {
            ModuloNave m = d.Modulo(e.modulo);
            Transform marco = m != null ? Hijo(Hijo(nave, "Cascos de los módulos"), m.nombre) : null;
            if (marco == null) return;
            List<Renderer> viejos = new List<Renderer>(marco.GetComponentsInChildren<Renderer>(true));
            GameObject g = Colocar(e, marco, Vector3.zero, Quaternion.identity, false, l);
            cascos.Add(g);
            foreach (Renderer r in viejos) Ocultar(r);

            // Comprobaciones en ejes del módulo
            Bounds b = LimitesLocales(g.transform, marco, out bool legibles);
            Vector2 ext = m.MedioExterior * 2f;
            Vector3 esperado = new Vector3(ext.x, ext.y, m.largo);
            Escala(b.size, esperado, 0.15f, l);
            if ((b.center - new Vector3(0f, 0f, m.largo / 2f)).magnitude > 0.2f)
                l.detalles.Add($"centro del modelo en {V(b.center)}; se esperaba (0; 0; {m.largo / 2f:0.##}): revisa el pivote (centro de la cara de entrada)");
            if (!legibles) { l.detalles.Add("malla sin Read/Write: no se pudo comprobar si invade el interior (actívalo para la prueba)"); return; }
            if (m.EsCaja || m.tipo == TipoModuloNave.Propulsion) return;
            float ri = m.diametroInterior / 2f - 0.06f, e0 = m.Grosor + 0.05f;
            int dentro = 0; float peor = 0f;
            foreach (Vector3 p in Vertices(g.transform, marco))
            {
                if (p.z < e0 || p.z > m.largo - e0) continue;   // (los asideros, en mallas llamadas «Asidero», no cuentan)
                float o = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y), (Mathf.Abs(p.x) + Mathf.Abs(p.y)) / Mathf.Sqrt(2f));
                if (o < ri) { dentro++; peor = Mathf.Max(peor, ri - o); }
            }
            if (dentro > 0) l.detalles.Add($"{dentro} vértices del casco entran en el interior (hasta {peor:0.00} m más allá de la cara interior de {m.diametroInterior} m)");
            PasoLibre(m, g.transform, marco, l);
        }

        void Equipo(Transform nave, DefinicionNaveModular d, EntradaVisual e, Linea l)
        {
            ModuloNave m = d.Modulo(e.modulo);
            Transform marco = m != null ? Hijo(Hijo(nave, "Equipos interiores (bloques)"), m.nombre) : null;
            if (marco == null) return;
            EquipoModulo dato = null;
            foreach (EquipoModulo q in m.equipos) if (q.nombre == e.nombre) { dato = q; break; }
            foreach (Transform t in marco)
            {
                if (t.name != e.nombre) continue;
                GameObject g = Colocar(e, t, Vector3.zero, Quaternion.identity, true, l);
                Ocultar(t.GetComponent<Renderer>(), g);
                if (dato == null) continue;
                Bounds b = LimitesLocales(g.transform, marco, out bool legibles);
                Escala(b.size, dato.tamano, 0.12f, l);
                if ((b.center - dato.centro).magnitude > 0.12f)
                    l.detalles.Add($"centro del modelo en {V(b.center)}; la caja acordada está en {V(dato.centro)}: revisa el pivote (centro de la caja)");
                if (legibles) PasoLibre(m, g.transform, marco, l);
            }
        }

        void Hojas(DefinicionNaveModular d, IList<PuertasUnion> uniones, EntradaVisual e, Linea l)
        {
            if (uniones == null) return;
            foreach (PuertasUnion u in uniones)
            {
                if (u == null || (!string.IsNullOrEmpty(e.nombre) && u.Datos.id != e.nombre)) continue;
                List<Transform> marcos = new List<Transform>();
                foreach (Transform t in u.transform) if (t.name.StartsWith("Puerta en ")) marcos.Add(t);
                int k = 0;
                foreach (PuertaNave pu in u.Datos.puertas)
                {
                    ModuloNave m = d.Modulo(pu.modulo);
                    PuertoModulo p = m != null ? m.Puerto(pu.puerto) : null;
                    if (p == null || k >= marcos.Count) continue;
                    Transform marco = marcos[k++];
                    GeometriaPuertas.Ejes(p, out Vector3 n, out Vector3 s, out Vector3 h);
                    foreach (Transform hoja in marco)
                    {
                        if (!hoja.name.StartsWith("Hoja ")) continue;
                        // X = deslizamiento (s), Y = alto hacia el «arriba» del módulo, Z = hacia el interior del módulo (−n).
                        GameObject g = Colocar(e, hoja, Vector3.zero, Quaternion.LookRotation(-n, -h), true, l);
                        Ocultar(hoja.GetComponent<Renderer>(), g);
                    }
                }
            }
        }

        void Paneles(IList<PuertasUnion> uniones, EntradaVisual e, Linea l)
        {
            if (uniones == null) return;
            foreach (PuertasUnion u in uniones)
            {
                if (u == null || (!string.IsNullOrEmpty(e.nombre) && u.Datos.id != e.nombre)) continue;
                foreach (Transform marco in u.transform)
                {
                    if (!marco.name.StartsWith("Panel en ")) continue;
                    Transform panel = Hijo(marco, "Panel de puerta");
                    if (panel == null) continue;
                    // Normal del panel: la del dato cuya caja coincide con esta pieza (no depende del orden).
                    Vector3 n = Vector3.zero;
                    foreach (PanelNave pn in u.Datos.paneles)
                        if (Vector3.Distance(GeometriaPuertas.Panel(pn).centro, panel.localPosition) < 0.01f) n = pn.normal.normalized;
                    if (n == Vector3.zero) { l.detalles.Add("panel sin dato que coincida: se deja provisional"); continue; }
                    Vector3 arriba = Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.forward;
                    GameObject g = Colocar(e, panel, Vector3.zero, Quaternion.LookRotation(n, arriba), true, l);  // +Z hacia el jugador
                    Ocultar(panel.GetComponent<Renderer>(), g);   // la luz de estado (verde/roja) se mantiene
                }
            }
        }

        void Utileria(Transform detalle, EntradaVisual e, Linea l)
        {
            if (detalle == null) { l.estado = "esta escena no tiene utilería del MVP (solo en ExpedicionMVP)"; l.correcto = true; return; }
            foreach (Transform t in detalle.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != e.nombre || t.GetComponent<Renderer>() == null) continue;
                GameObject g = Colocar(e, t, Vector3.zero, Quaternion.identity, true, l);
                Ocultar(t.GetComponent<Renderer>(), g);
            }
        }

        // ------------------------------------------------------------------ Colocación

        GameObject Colocar(EntradaVisual e, Transform padre, Vector3 posLocal, Quaternion rotLocal, bool padreEscalado, Linea l)
        {
            GameObject g = Instantiate(e.prefab, padre, false);
            g.name = "Modelo · " + e.Clave;
            g.transform.localPosition = posLocal;
            g.transform.localRotation = rotLocal;
            g.transform.localScale = Vector3.one;

            int fisicas = 0, extras = 0;
            foreach (Collider c in g.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Destroy(c); fisicas++; }
            foreach (Joint j in g.GetComponentsInChildren<Joint>(true)) { DestroyImmediate(j); fisicas++; }       // antes que el Rigidbody
            foreach (Rigidbody rb in g.GetComponentsInChildren<Rigidbody>(true)) { rb.isKinematic = true; DestroyImmediate(rb); fisicas++; }
            // Un .glb puede traer cámaras y luces: quitarían la vista de juego o cambiarían la iluminación.
            foreach (Camera c in g.GetComponentsInChildren<Camera>(true)) { c.enabled = false; Destroy(c); extras++; }
            foreach (AudioListener a in g.GetComponentsInChildren<AudioListener>(true)) { a.enabled = false; Destroy(a); extras++; }
            foreach (Light lu in g.GetComponentsInChildren<Light>(true)) { lu.enabled = false; Destroy(lu); extras++; }
            const string avisoFisicas = "el modelo traía colisiones o física: se han quitado (se usan las colisiones provisionales)";
            const string avisoExtras = "el modelo traía cámaras, luces o audio: se han quitado (expórtalo sin ellos)";
            if (fisicas > 0 && !l.detalles.Contains(avisoFisicas)) l.detalles.Add(avisoFisicas);
            if (extras > 0 && !l.detalles.Contains(avisoExtras)) l.detalles.Add(avisoExtras);

            // Tamaño y centro de la malla provisional (cubo = 1; la marca de puesto es un cilindro de 2 de alto).
            Vector3 tamMalla = Vector3.one, centroMalla = Vector3.zero;
            MeshFilter mp = padre.GetComponent<MeshFilter>();
            if (padreEscalado && mp != null && mp.sharedMesh != null) { tamMalla = mp.sharedMesh.bounds.size; centroMalla = mp.sharedMesh.bounds.center; }

            // La pieza provisional es un cubo de 1 m escalado: se compensa para que el modelo quede a escala real (1 u = 1 m),
            // o se estira para llenar la caja si así se pidió.
            Vector3 lossy = g.transform.lossyScale;     // = tamaño de la caja provisional en los ejes del modelo
            if (e.ajuste == AjusteSustitucion.EscalarALaCaja)
            {
                Bounds bp = LimitesPropios(g.transform);
                Vector3 t = bp.size;
                if (!padreEscalado) l.detalles.Add("«Escalar a la caja» solo sirve para equipos, hojas, paneles y utilería: el casco se coloca a escala real");
                else if (t.x < 1e-4f || t.y < 1e-4f || t.z < 1e-4f)
                {
                    l.detalles.Add("modelo sin volumen en algún eje: no se puede ajustar a la caja; se coloca a escala real");
                    g.transform.localScale = new Vector3(Inv(lossy.x), Inv(lossy.y), Inv(lossy.z));
                }
                else
                {
                    // Tamaño de la malla provisional en los ejes del modelo (rotaciones de 90°: solo permuta ejes).
                    Vector3 tm = Quaternion.Inverse(g.transform.localRotation) * tamMalla;
                    tm = new Vector3(Mathf.Abs(tm.x), Mathf.Abs(tm.y), Mathf.Abs(tm.z));
                    g.transform.localScale = new Vector3(tm.x / t.x, tm.y / t.y, tm.z / t.z);
                    g.transform.localPosition = posLocal + centroMalla - g.transform.localRotation * Vector3.Scale(bp.center, g.transform.localScale);
                }
            }
            else if (padreEscalado)
                g.transform.localScale = new Vector3(Inv(lossy.x), Inv(lossy.y), Inv(lossy.z));

            instancias.Add(g);
            return g;
        }

        static float Inv(float v) => Mathf.Abs(v) > 1e-6f ? 1f / Mathf.Abs(v) : 1f;

        void Ocultar(Renderer r, GameObject modelo = null)
        {
            if (r == null) return;
            r.forceRenderingOff = true;    // sigue existiendo (y su colisión también)
            provisionales.Add(r);
            if (modelo != null) pares.Add((r, modelo));
        }

        // ------------------------------------------------------------------ Medidas

        /// <summary>Límites de las mallas del modelo en su propio espacio.</summary>
        static Bounds LimitesPropios(Transform raiz)
        {
            Matrix4x4 inv = raiz.worldToLocalMatrix;
            bool hay = false; Bounds b = new Bounds();
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 a = inv * mf.transform.localToWorldMatrix;
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = a.MultiplyPoint3x4(c);
                    if (!hay) { b = new Bounds(p, Vector3.zero); hay = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        /// <summary>Límites del modelo en los ejes de «marco» (el módulo). Exactos si las mallas son legibles.</summary>
        static Bounds LimitesLocales(Transform raiz, Transform marco, out bool legibles)
        {
            legibles = true;
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null && !mf.sharedMesh.isReadable) legibles = false;
            bool hay = false; Bounds b = new Bounds();
            if (legibles)
                foreach (Vector3 p in Vertices(raiz, marco))
                {
                    if (!hay) { b = new Bounds(p, Vector3.zero); hay = true; } else b.Encapsulate(p);
                }
            else
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    Bounds w = r.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 c = w.center + Vector3.Scale(w.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        Vector3 p = marco.InverseTransformPoint(c);
                        if (!hay) { b = new Bounds(p, Vector3.zero); hay = true; } else b.Encapsulate(p);
                    }
                }
            return b;
        }

        static IEnumerable<Vector3> Vertices(Transform raiz, Transform marco)
        {
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                if (mf.name.Contains("Asidero")) continue;   // asideros: barras de 5 cm junto a la pared, permitidas
                Matrix4x4 a = marco.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (Vector3 v in mf.sharedMesh.vertices) yield return a.MultiplyPoint3x4(v);
            }
        }

        static void Escala(Vector3 medido, Vector3 esperado, float tolerancia, Linea l)
        {
            Vector3 dif = medido - esperado;
            if (Mathf.Abs(dif.x) <= tolerancia && Mathf.Abs(dif.y) <= tolerancia && Mathf.Abs(dif.z) <= tolerancia) return;
            float ratio = esperado.magnitude > 0f ? medido.magnitude / esperado.magnitude : 1f;
            string pista = ratio > 50f ? " · parece exportado en centímetros (×100)" : ratio < 0.02f ? " · parece muy pequeño (¿unidades?)" : "";
            l.detalles.Add($"tamaño {V(medido)} m; el acordado es {V(esperado)} m (tolerancia {tolerancia:0.00}){pista}");
        }

        static void PasoLibre(ModuloNave m, Transform raiz, Transform marco, Linea l)
        {
            if (m.pasoLibre.x <= 0f) return;
            float hx = m.pasoLibre.x / 2f - 0.02f, hy = m.pasoLibre.y / 2f - 0.02f;
            int dentro = 0;
            foreach (Vector3 p in Vertices(raiz, marco))
                if (Mathf.Abs(p.x) < hx && Mathf.Abs(p.y) < hy && p.z > m.tramoPaso.x + 0.05f && p.z < m.tramoPaso.y - 0.05f) dentro++;
            if (dentro > 0) l.detalles.Add($"{dentro} vértices invaden el PASO LIBRE de {m.pasoLibre.x} × {m.pasoLibre.y} m");
        }

        static void Materiales(GameObject prefab, Linea l)
        {
            HashSet<string> malos = new HashSet<string>();
            foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
                foreach (Material mt in r.sharedMaterials)
                {
                    if (mt == null) { malos.Add("(material vacío)"); continue; }
                    string s = mt.shader != null ? mt.shader.name : "";
                    bool urp = s.StartsWith("Universal Render Pipeline/") || s.StartsWith("Shader Graphs/");
                    if (!urp) malos.Add(mt.name + " usa «" + s + "»");
                }
            if (malos.Count > 0)
            {
                l.detalles.Add("materiales no URP (se verán rosas o mal): " + string.Join(", ", malos) + ". Usa Universal Render Pipeline/Lit");
                l.correcto = false;
            }
        }

        static string V(Vector3 v) => $"({v.x:0.00}; {v.y:0.00}; {v.z:0.00})";
    }
}
