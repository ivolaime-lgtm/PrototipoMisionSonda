using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// ESCENARIO ORBITAL de las secuencias (solo visual): la EM-4X REAL (la misma geometría que construye VistaExpedicion
    /// en la expedición, con su sustitución de arte) y una PLATAFORMA DE SERVICIO por detrás y por debajo de la etapa de
    /// propulsión, donde se aparca el VC-1, se reciben las piezas, se dejan las retiradas y sale la conexión de servicio.
    ///
    /// Coordenadas = las de la EM-4X (NaveModular): proa hacia +Z; etapa de propulsión de z −22 a −37 (motor hasta −41);
    /// paneles solares a ambos lados (x 3,5–19, z −35,2 a −25,2, y = 0); Ciencia hacia −X; Comunicaciones hacia +X; el
    /// puerto de atraque del Mando en (0; −3; 11,7) mirando hacia abajo. Todos los puntos (aparcamiento, bandeja,
    /// bastidor, toma de servicio, montajes, pasillo seguro) están en ConfiguracionJuego → puntos.
    ///
    /// Para que nada se cruce: el VC-1 y la plataforma quedan detrás de la nave (z menor que −42), las piezas viajan por
    /// un PASILLO SEGURO (detrás de la nave y por fuera de los paneles) y solo se acercan a su montaje al final.
    /// En las secuencias se ocultan: los rótulos y esquemas, el bloque antiguo de la lanzadera (LZ: llega la L-4 real), el
    /// motor de bloques de la etapa (lo representa el motor instalado) y, mientras se cambia el tanque, el casco de la
    /// etapa de propulsión (vista en corte, con su armazón).
    /// </summary>
    public class EscenarioOrbital
    {
        public Transform Raiz { get; private set; }
        public Transform Nave { get; private set; }
        public Transform Recepcion { get; private set; }
        /// <summary>Remolcador de servicio guiado (visual): acompaña a cada pieza; no es un brazo con física.</summary>
        public Transform Remolcador { get; private set; }
        /// <summary>Donde cuelgan los componentes instalados (misma posición que «Nave»).</summary>
        public Transform Equipos => equipos;
        public readonly Dictionary<TipoComponente, Transform> Instalado = new Dictionary<TipoComponente, Transform>();
        public DefinicionNaveModular Definicion { get; private set; }
        /// <summary>Popa del VC-1 aparcado (mundo); el VC-1 mira a +Z de la nave con la bodega hacia +Y.</summary>
        public Vector3 AparcamientoVC1 => Raiz.TransformPoint(P.aparcamientoVC1);
        public Vector3 EsperaVC1 => Raiz.TransformPoint(P.aparcamientoVC1 + P.esperaVC1);
        public Vector3 LlegadaLejosVC1 => Raiz.TransformPoint(P.aparcamientoVC1 + P.llegadaVC1);
        public Quaternion GiroNave => Raiz.rotation;
        /// <summary>Puerto de atraque del Mando (cara exterior) + desfase configurado. La L-4 apoya aquí su morro.</summary>
        public Vector3 AtraqueL4 { get; private set; }
        /// <summary>Puerto de suministro del nodo de popa (cara exterior) y su normal hacia fuera.</summary>
        public Vector3 PuertoSuministro { get; private set; }
        public Vector3 NormalSuministro { get; private set; }
        public Vector3 TomaCombustible => Raiz.TransformPoint(P.tomaCombustible);
        public Vector3 TomaServicio => Raiz.TransformPoint(P.tomaServicio);

        static PuntosOrbitalesConfig P => ConfiguracionJuego.Actual.puntos;

        Func<Color, bool, Material> mat;
        CatalogoMision cat;
        Transform equipos, conexion, acoplador, armazon, bastidor;
        GameObject vistaGo;
        readonly List<Renderer> cascoEtapa = new List<Renderer>();
        readonly List<Transform> mangueras = new List<Transform>();
        Transform tapaSuministro, escotillaSuministro;
        Vector3 tapaSuministroCerrada;
        float cursorRetiradas;
        Material mManguera;

        /// <summary>Montaje (cara de montaje) de cada componente, en coordenadas de la nave.</summary>
        public static Vector3 Montaje(TipoComponente t) =>
            t == TipoComponente.Motor ? P.montajeMotor : t == TipoComponente.Tanque ? P.montajeTanque
            : t == TipoComponente.Instrumento ? P.montajeInstrumento : P.montajeRelevo;

        /// <summary>Orientación de montaje (la pieza sale hacia su +Z local girada así).</summary>
        public static Quaternion Giro(TipoComponente t) => Quaternion.Euler(
            t == TipoComponente.Motor ? P.giroMotor : t == TipoComponente.Tanque ? P.giroTanque
            : t == TipoComponente.Instrumento ? P.giroInstrumento : P.giroRelevo);

        Transform Caja(string n, Transform p, Vector3 c, Vector3 t, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            Collider col = g.GetComponent<Collider>(); col.enabled = false; UnityEngine.Object.Destroy(col);
            g.transform.SetParent(p, false);
            g.transform.localPosition = c;
            g.transform.localScale = t;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        /// <summary>«indices» = pieza instalada de cada tipo al empezar (0 = de serie).</summary>
        public static EscenarioOrbital Construir(Vector3 lugar, Dictionary<TipoComponente, int> indices, ResumenExpedicion r, CatalogoMision cat,
                                                 Func<Color, bool, Material> mat)
        {
            EscenarioOrbital e = new EscenarioOrbital { mat = mat, cat = cat };
            e.Montar(lugar, indices, r);
            return e;
        }

        void Montar(Vector3 lugar, Dictionary<TipoComponente, int> indices, ResumenExpedicion r)
        {
            Raiz = new GameObject("Escenario orbital · EM-4X real y plataforma de servicio").transform;
            Raiz.position = lugar;
            Material estructura = mat(new Color(0.4f, 0.42f, 0.46f), false), amarillo = mat(new Color(0.95f, 0.78f, 0.15f), false),
                     oscuro = mat(new Color(0.18f, 0.19f, 0.22f), false);
            mManguera = mat(new Color(1f, 0.6f, 0.2f), true);
            Definicion = DefinicionNaveModular.ExpedicionEM4X();
            ConstruirNave();

            // Los componentes cuelgan de un hermano de «Nave» con su misma posición: si un modelo sustituye a la EM-4X, no los oculta
            equipos = new GameObject("Componentes instalados").transform;
            equipos.SetParent(Raiz, false);

            // Plataforma de servicio: viga por detrás y por debajo de la etapa, unida a ella por dos brazos
            Transform plat = SustitucionEscenario.Punto(new GameObject("Plataforma de servicio orbital").transform, "Orbita/PlataformaServicio");
            plat.SetParent(Raiz, false);
            Vector3 v = P.vigaServicio;
            Caja("Viga de la plataforma", plat, v, new Vector3(2.5f, 1.2f, P.largoViga), estructura);
            float yTecho = -3f, yViga = v.y + 0.6f;
            foreach (float zb in new[] { -25f, -33f })
                if (zb < v.z + P.largoViga / 2f && zb > v.z - P.largoViga / 2f && yTecho > yViga)
                    Caja("Brazo de unión a la etapa", plat, new Vector3(v.x, (yTecho + yViga) / 2f, zb), new Vector3(1f, yTecho - yViga, 1f), estructura);
            Recepcion = SustitucionEscenario.Punto(new GameObject("Soporte de recepción").transform, "Orbita/SoporteRecepcion");
            Recepcion.SetParent(plat, false);
            Recepcion.localPosition = P.bandejaRecepcion;
            Caja("Bandeja de recepción", Recepcion, new Vector3(0f, -0.15f, 0f), new Vector3(6f, 0.3f, 6f), amarillo);
            Union(plat, new Vector3(v.x, v.y, P.bandejaRecepcion.z), P.bandejaRecepcion + new Vector3(-3f * Mathf.Sign(P.bandejaRecepcion.x - v.x), -0.15f, 0f), estructura);
            bastidor = SustitucionEscenario.Punto(new GameObject("Soporte de piezas retiradas").transform, "Orbita/SoporteRetiradas");
            bastidor.SetParent(plat, false);
            bastidor.localPosition = P.bastidorRetiradas;
            Caja("Bastidor de piezas retiradas", bastidor, new Vector3(-2f, -0.15f, 0f), new Vector3(10f, 0.3f, 16f), oscuro);   // de x −15 a −5: cabe el tanque tumbado
            Union(plat, new Vector3(v.x, v.y, P.bastidorRetiradas.z), P.bastidorRetiradas + new Vector3(-3f * Mathf.Sign(P.bastidorRetiradas.x - v.x), -0.15f, 0f), estructura);
            cursorRetiradas = -7.5f;
            Remolcador = Caja("Remolcador de servicio guiado (visual)", plat, Vector3.zero, new Vector3(1.4f, 0.7f, 1.4f), amarillo);
            Caja("Luz del remolcador", Remolcador, new Vector3(0f, 0.6f, 0f), new Vector3(0.3f, 0.5f, 0.3f), mat(new Color(1f, 0.5f, 0.1f), true));
            Remolcador.gameObject.SetActive(false);
            // Conexión de servicio (carga, sin paso de personas): sale de la viga hacia el costado del VC-1
            Transform tomaS = Caja("Toma de servicio (viga)", plat, P.tomaServicio, new Vector3(1.2f, 1.2f, 1.2f), amarillo);
            SustitucionEscenario.Punto(tomaS, "Orbita/TomaServicio");
            conexion = Caja("Conexión de servicio · carga (sin acceso de tripulación)", plat, P.tomaServicio, new Vector3(0.6f, 0.6f, 0.1f), amarillo);
            acoplador = Caja("Acoplador de servicio", plat, P.tomaServicio, new Vector3(1.4f, 1.4f, 0.4f), oscuro);
            conexion.gameObject.SetActive(false);
            acoplador.gameObject.SetActive(false);

            ModuloNave ma = Definicion.Modulo("MA");
            PuertoModulo pa = ma != null ? ma.Puerto("atraque") : null;
            AtraqueL4 = Raiz.TransformPoint((pa != null ? ma.AMundo(pa.posicion) : new Vector3(0f, -3f, 11.7f)) + P.atraqueL4Desfase);
            ModuloNave n2 = Definicion.Modulo("N2");
            PuertoModulo ps = n2 != null ? n2.Puerto("suministro") : null;
            PuertoSuministro = Raiz.TransformPoint(ps != null ? n2.AMundo(ps.posicion) : new Vector3(3.3f, 0f, -18.9f));
            NormalSuministro = Raiz.TransformDirection(ps != null ? n2.DireccionAMundo(ps.normal).normalized : Vector3.right);

            foreach (TipoComponente t in new[] { TipoComponente.Motor, TipoComponente.Tanque, TipoComponente.Instrumento, TipoComponente.Relevo })
            {
                int i;
                if (!indices.TryGetValue(t, out i)) i = 0;
                Poner(t, i, r);
            }
            SustitucionEscenario.Aplicar(Raiz);
        }

        void Union(Transform p, Vector3 a, Vector3 b, Material m)
        {
            Vector3 c = (a + b) / 2f, d = b - a;
            Transform u = Caja("Unión", p, c, new Vector3(0.6f, 0.6f, Mathf.Max(0.1f, d.magnitude)), m);
            if (d.sqrMagnitude > 0.001f) u.localRotation = Quaternion.LookRotation(d);
        }

        /// <summary>
        /// La EM-4X de la expedición (VistaExpedicion, la misma geometría) con la sustitución de arte, sin sus luces ni su
        /// iluminación ambiente (no deben cambiar el hangar), sin rótulos ni esquemas y sin el bloque LZ.
        /// </summary>
        void ConstruirNave()
        {
            Light[] antes = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            UnityEngine.Rendering.AmbientMode modo = RenderSettings.ambientMode;
            Color ambiente = RenderSettings.ambientLight;
            vistaGo = new GameObject("EM-4X de las secuencias (VistaExpedicion)");
            vistaGo.transform.SetParent(Raiz, false);
            VistaExpedicion vista = vistaGo.AddComponent<VistaExpedicion>();
            vista.Construir(Definicion, null);
            vista.MostrarEtiquetas(false);
            vista.MostrarEsquema(false);
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (Array.IndexOf(antes, l) < 0 && (l.name == "Luz principal" || l.name == "Luz de relleno")) UnityEngine.Object.Destroy(l.gameObject);
            RenderSettings.ambientMode = modo;
            RenderSettings.ambientLight = ambiente;

            Nave = vista.Nave;
            Nave.SetParent(Raiz, false);
            SustitucionEscenario.Punto(Nave, "Orbita/EM4X");
            // Arte del equipo (mismas entradas que en la expedición; las puertas y la utilería no existen aquí)
            SustitucionVisualEM4X arte = vistaGo.AddComponent<SustitucionVisualEM4X>();
            arte.Aplicar(Nave, Definicion, new List<PuertasUnion>(), null, SustitucionEscenario.Tabla);

            ModuloNave lz = Definicion.Modulo("LZ"), pr = Definicion.Modulo("PR");
            foreach (Transform grupo in Nave)
            {
                if (grupo.name.StartsWith("Referencias")) { grupo.gameObject.SetActive(false); continue; }
                foreach (Transform h in grupo)
                {
                    if (lz != null && h.name == lz.nombre) h.gameObject.SetActive(false);              // la L-4 real llega después
                    else if (h.name.StartsWith("Adaptador") && h.name.Contains("LZ.")) h.gameObject.SetActive(false);
                    else if (h.name == "Garganta del motor" || h.name == "Tobera") h.gameObject.SetActive(false);   // lo representa el motor instalado
                    else if (h.name == "Tapa del puerto de suministro") { tapaSuministro = h; tapaSuministroCerrada = h.localPosition; }
                    else if (pr != null && h.name == pr.nombre && grupo.name.StartsWith("Cascos"))
                        cascoEtapa.AddRange(h.GetComponentsInChildren<Renderer>(true));
                    if (n2Nombre != null && h.name == n2Nombre)
                        foreach (Transform x in h.GetComponentsInChildren<Transform>(true))
                            if (x.name == "Escotilla cerrada (suministro)") escotillaSuministro = x;
                }
            }

            // Armazón de la etapa para la vista en corte (solo se ve mientras se cambia el tanque): cunas en U, abiertas por arriba
            armazon = new GameObject("Armazón de la etapa (vista en corte)").transform;
            armazon.SetParent(Raiz, false);
            Material e = mat(new Color(0.4f, 0.42f, 0.46f), false);
            if (pr != null)
            {
                Bounds b = pr.Limites;
                for (float z = b.min.z; z <= b.max.z + 0.01f; z += 5f)
                {
                    Caja("Cuna de la etapa (fondo)", armazon, new Vector3(b.center.x, b.min.y + 0.15f, z), new Vector3(b.size.x, 0.3f, 0.3f), e);
                    Caja("Cuna de la etapa (costado)", armazon, new Vector3(b.min.x + 0.15f, b.center.y - b.extents.y / 2f, z), new Vector3(0.3f, b.extents.y, 0.3f), e);
                    Caja("Cuna de la etapa (costado)", armazon, new Vector3(b.max.x - 0.15f, b.center.y - b.extents.y / 2f, z), new Vector3(0.3f, b.extents.y, 0.3f), e);
                }
                Caja("Larguero de la etapa", armazon, new Vector3(b.min.x + 0.15f, b.min.y + 0.15f, b.center.z), new Vector3(0.3f, 0.3f, b.size.z), e);
                Caja("Larguero de la etapa", armazon, new Vector3(b.max.x - 0.15f, b.min.y + 0.15f, b.center.z), new Vector3(0.3f, 0.3f, b.size.z), e);
            }
            armazon.gameObject.SetActive(false);
        }

        string n2Nombre => Definicion != null && Definicion.Modulo("N2") != null ? Definicion.Modulo("N2").nombre : null;

        /// <summary>Vista en corte de la etapa de propulsión (para ver el tanque que se cambia).</summary>
        public void CorteEtapa(bool corte)
        {
            foreach (Renderer r in cascoEtapa) if (r != null) r.enabled = !corte;
            if (armazon != null) armazon.gameObject.SetActive(corte);
        }

        /// <summary>Abre (desliza) o cierra la tapa del puerto de suministro y su escotilla.</summary>
        public void AbrirSuministro(float k)
        {
            if (tapaSuministro != null) tapaSuministro.localPosition = tapaSuministroCerrada + Vector3.forward * 3f * Mathf.Clamp01(k);
            if (escotillaSuministro != null) escotillaSuministro.gameObject.SetActive(k < 0.5f);
        }

        /// <summary>Coloca (sin animación) la pieza «i» de un tipo en su punto de montaje y la devuelve.</summary>
        public Transform Poner(TipoComponente t, int i, ResumenExpedicion r)
        {
            Transform viejo;
            if (Instalado.TryGetValue(t, out viejo) && viejo != null) UnityEngine.Object.Destroy(viejo.gameObject);
            Transform p = ModeloComponentes.Componente(t, i, r, cat, equipos, mat);
            if (p == null) return null;
            p.localPosition = Montaje(t);
            p.localRotation = Giro(t);
            Instalado[t] = p;
            return p;
        }

        /// <summary>Siguiente hueco del bastidor de piezas retiradas para una pieza de ese diámetro (mundo).</summary>
        public Vector3 HuecoRetirada(float diametro)
        {
            float z = cursorRetiradas + diametro / 2f + 0.3f;
            cursorRetiradas = z + diametro / 2f + 0.3f;
            return bastidor.TransformPoint(new Vector3(3f, 0.1f + diametro / 2f, Mathf.Min(z, 7.5f)));
        }

        /// <summary>Giro de las piezas en el bastidor: tumbadas hacia fuera de la viga (−X), sin cruzarla.</summary>
        public Quaternion GiroRetirada => Raiz.rotation * Quaternion.Euler(0f, -90f, 0f);

        public Transform Bastidor => bastidor;

        /// <summary>Pone el remolcador guiado encima del centro de una pieza de ese radio (null = lo retira).</summary>
        public void Apuntar(Vector3? centro, float radio = 1f)
        {
            if (!centro.HasValue) { Remolcador.gameObject.SetActive(false); return; }
            Remolcador.gameObject.SetActive(true);
            Remolcador.position = centro.Value + Raiz.up * (radio + 0.4f);
            Remolcador.rotation = Raiz.rotation;
        }

        /// <summary>Conexión de servicio desde la toma de la viga hasta «destino» (k: 0 recogida · 1 conectada).</summary>
        public void Conexion(float k, Vector3 destino)
        {
            bool ver = k > 0.01f;
            conexion.gameObject.SetActive(ver);
            acoplador.gameObject.SetActive(ver);
            if (!ver) return;
            Vector3 a = TomaServicio, b = Vector3.Lerp(a, destino, Mathf.Clamp01(k)), d = b - a;
            conexion.position = (a + b) / 2f;
            conexion.rotation = Quaternion.LookRotation(d.sqrMagnitude > 0.001f ? d : Raiz.right, Raiz.up);
            conexion.localScale = new Vector3(0.6f, 0.6f, Mathf.Max(0.1f, d.magnitude));
            acoplador.position = b;
            acoplador.rotation = conexion.rotation;
        }

        /// <summary>Manguera visual por varios puntos (mundo). Devuelve su raíz (se quita con QuitarMangueras).</summary>
        public Transform Manguera(string nombre, params Vector3[] puntos)
        {
            Transform m = new GameObject(nombre).transform;
            m.SetParent(Raiz, true);
            for (int i = 0; i + 1 < puntos.Length; i++)
            {
                Vector3 a = puntos[i], b = puntos[i + 1], d = b - a;
                if (d.sqrMagnitude < 0.0001f) continue;
                Transform s = Caja("Tramo", m, Vector3.zero, Vector3.one, mManguera);
                s.position = (a + b) / 2f;
                s.rotation = Quaternion.LookRotation(d, Mathf.Abs(Vector3.Dot(d.normalized, Raiz.up)) > 0.95f ? Raiz.forward : Raiz.up);
                s.localScale = new Vector3(0.35f, 0.35f, d.magnitude + 0.3f);
            }
            mangueras.Add(m);
            return m;
        }

        public void QuitarMangueras()
        {
            foreach (Transform m in mangueras) if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            mangueras.Clear();
        }

        // ------------------------------------------------------------------ Recorridos seguros (coordenadas de la nave)

        /// <summary>Punto del pasillo seguro (detrás de la nave) a la x dada.</summary>
        public Vector3 Pasillo(float x) => Raiz.TransformPoint(new Vector3(x, P.pasilloY, P.pasilloZ));

        Vector3 N(Vector3 local) => Raiz.TransformPoint(local);

        /// <summary>
        /// Recorrido desde el pasillo seguro hasta el montaje (sin el montaje). Se recorre al revés para retirar.
        /// «largo» es lo que la pieza sale de su cara de montaje.
        /// </summary>
        public List<Vector3> Acceso(TipoComponente t, float largo)
        {
            Vector3 m = Montaje(t);
            List<Vector3> l = new List<Vector3>();
            switch (t)
            {
                case TipoComponente.Motor:
                    l.Add(Pasillo(m.x));
                    l.Add(N(new Vector3(m.x, m.y, m.z - largo - 12f)));
                    break;
                case TipoComponente.Tanque:
                    l.Add(N(new Vector3(m.x, P.pasilloTanqueY, P.pasilloZ)));
                    l.Add(N(new Vector3(m.x, P.pasilloTanqueY, m.z)));
                    break;
                case TipoComponente.Instrumento:
                    l.Add(Pasillo(-P.pasilloX));
                    l.Add(N(new Vector3(-P.pasilloX, m.y + 4f, m.z)));
                    l.Add(N(new Vector3(m.x - 6f, m.y, m.z)));
                    break;
                default:
                    l.Add(Pasillo(P.pasilloX));
                    l.Add(N(new Vector3(P.pasilloX, m.y + 5f, m.z)));
                    l.Add(N(new Vector3(m.x, m.y + 5f, m.z)));
                    break;
            }
            return l;
        }

        public void Destruir()
        {
            if (Raiz != null) UnityEngine.Object.Destroy(Raiz.gameObject);
            if (vistaGo != null) UnityEngine.Object.Destroy(vistaGo);
        }
    }
}
