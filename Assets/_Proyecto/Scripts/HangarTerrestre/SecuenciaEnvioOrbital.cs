using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Parte de SecuenciaDespegue: ENVÍO ORBITAL NO TRIPULADO (VC-1), carga de la L-4 y LLEGADA de la tripulación a la EM-4X.
    /// Planos con geometría provisional y movimientos guiados (sin física ni pilotaje). Solo representa lo que ya está
    /// decidido en la preparación cerrada: no cambia configuración, recursos ni costes.
    ///
    /// VC-1: salida de la nave C2 → integración en el LC-2 → cuenta atrás, encendido y ascenso → en el espacio: separación
    /// de la cofia y del lanzador → aproximación visible a la EM-4X REAL → punto de espera y alineación → aparcamiento →
    /// conexión de SERVICIO (carga, sin paso de personas) → recepción, retirada, colocación y comprobación de cada pieza
    /// (por un pasillo seguro, sin cruzar la nave) → combustible y carga pequeña → desconexión y salida del VC-1.
    /// L-4: en el espacio, separación de la etapa → aproximación → alineación bajo el puerto del Mando → atraque →
    /// transferencias → apertura del acceso y paso en primera persona (continúa en la expedición: EntradaABordo).
    ///
    /// Rótulos breves, cuenta atrás e indicadores (ConfiguracionJuego → textos); planos de cámara en ConfiguracionJuego →
    /// camaras; puntos en ConfiguracionJuego → puntos; duraciones en ConfiguracionJuego → secuencias.
    /// </summary>
    public partial class SecuenciaDespegue
    {
        static readonly Vector3 LugarEnvio = new Vector3(-5000f, 0f, 0f);       // plataforma exterior del VC-1
        static readonly Vector3 LugarOrbita = new Vector3(0f, 4000f, 9000f);    // zona de servicio orbital de la EM-4X
        static readonly Vector3 LugarEspacio = new Vector3(0f, 8000f, -9000f);  // planos de separación sobre el planeta

        Transform raizEnvio, raizEspacio;
        ModeloTransporte vc1;
        EscenarioOrbital orbita;
        readonly List<Transform> cargaL4 = new List<Transform>();
        Font fuenteRotulos;

        static SecuenciasConfig Cfg => ConfiguracionJuego.Actual.secuencias;
        static PuntosOrbitalesConfig Pts => ConfiguracionJuego.Actual.puntos;
        static string T(string clave) => ConfiguracionJuego.Texto(clave);
        static float D(float s) => ConfiguracionJuego.D(s);

        void LimpiarEnvio()
        {
            if (raizEnvio != null) Destroy(raizEnvio.gameObject);
            raizEnvio = null;
            vc1 = null;
            LimpiarEspacio();
            if (orbita != null) { orbita.Destruir(); orbita = null; }
        }

        void LimpiarEspacio()
        {
            if (raizEspacio != null) Destroy(raizEspacio.gameObject);
            raizEspacio = null;
        }

        TextMesh Rotulo3D(Transform padre, Vector3 pos, string texto, float tam, Color color)
        {
            if (fuenteRotulos == null) fuenteRotulos = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject g = new GameObject("Rótulo");
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            TextMesh tm = g.AddComponent<TextMesh>();
            tm.font = fuenteRotulos;
            tm.GetComponent<MeshRenderer>().sharedMaterial = fuenteRotulos.material;
            tm.text = texto;
            tm.fontSize = 48;
            tm.characterSize = tam;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            g.AddComponent<RotuloHaciaCamara>();
            return tm;
        }

        static IEnumerator MoverA(Transform t, Vector3 destino, Quaternion giro, float s, System.Action<Vector3> cadaPaso = null)
        {
            Vector3 p0 = t.position; Quaternion q0 = t.rotation;
            for (float u = 0f; u < 1f; u += Time.deltaTime / Mathf.Max(0.01f, s))
            {
                float k = Mathf.SmoothStep(0f, 1f, u);
                t.position = Vector3.Lerp(p0, destino, k);
                t.rotation = Quaternion.Slerp(q0, giro, k);
                cadaPaso?.Invoke(t.position);
                yield return null;
            }
            t.position = destino;
            t.rotation = giro;
            cadaPaso?.Invoke(t.position);
        }

        void Mirar(Vector3 desde, Vector3 hacia)
        {
            cam.transform.SetParent(null, true);
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
        }

        // ------------------------------------------------------------------ Planos de cámara (ConfiguracionJuego → camaras)

        Vector3 PuntoAncla(AnclaCamara a, Vector3 v, Transform vehiculo, Vector3 plataforma) =>
            a == AnclaCamara.EM4X ? (orbita != null ? orbita.Raiz.TransformPoint(v) : LugarOrbita + v)
            : a == AnclaCamara.Vehiculo ? (vehiculo != null ? vehiculo.position : plataforma) + v
            : plataforma + v;

        /// <summary>Coloca la cámara en el plano «id» (posición y foco relativos a sus anclas).</summary>
        void Encuadre(string id, Transform vehiculo = null, Vector3? plataforma = null)
        {
            PlanoCamara p = ConfiguracionJuego.Plano(id);
            Vector3 pl = plataforma ?? Vector3.zero;
            Mirar(PuntoAncla(p.ancla, p.posicion, vehiculo, pl), PuntoAncla(p.anclaFoco, p.foco, vehiculo, pl));
            cam.fieldOfView = p.campo > 1f ? p.campo : 70f;
        }

        /// <summary>Planeta provisional bajo los planos de espacio (para que se lea «en órbita»).</summary>
        void Planeta(Transform padre, float radio, float bajo)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Planeta (provisional)";
            Collider c = g.GetComponent<Collider>(); c.enabled = false; Destroy(c);
            g.transform.SetParent(padre, false);
            g.transform.localPosition = new Vector3(0f, -bajo - radio, 0f);
            g.transform.localScale = Vector3.one * radio * 2f;
            g.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.32f, 0.4f, 0.52f), false);
            SustitucionEscenario.Punto(g.transform, "Espacio/Planeta");
        }

        /// <summary>Llena el VC-1 con lo que lleva (componentes por índice, módulos de combustible y contenedores).</summary>
        Dictionary<TipoComponente, Transform> Llenar(ModeloTransporte m, List<Transform> combustible, List<Transform> contenedores)
        {
            Dictionary<TipoComponente, Transform> piezas = new Dictionary<TipoComponente, Transform>();
            ResumenTransporte tr = resumen.transporte;
            foreach (ElementoTransporte e in tr.componentes)
            {
                Transform h;
                if (!m.Huecos.TryGetValue(e.tipo, out h)) continue;
                Transform p = ModeloComponentes.Componente(e.tipo, e.indice, resumen, catalogo, m.Raiz, fabricaMat);
                if (p != null) piezas[e.tipo] = m.Poner(p, h);
            }
            for (int i = 0; i < tr.modulosCombustible && i < m.HuecosCombustible.Count; i++)
                combustible?.Add(m.Poner(ModeloComponentes.ModuloCombustible(m.Raiz, fabricaMat), m.HuecosCombustible[i]));
            foreach (string lote in tr.contenedores)
            {
                Transform h;
                if (m.HuecosContenedor.TryGetValue(lote, out h))
                {
                    Transform c = m.Poner(ModeloComponentes.Contenedor(lote, m.Raiz, fabricaMat), h);
                    c.localPosition = new Vector3(0f, -0.6f, 0f);
                    contenedores?.Add(c);
                }
            }
            SustitucionEscenario.Aplicar(m.Raiz);
            return piezas;
        }

        static string Lista(ResumenTransporte tr)
        {
            List<string> l = new List<string>();
            foreach (ElementoTransporte e in tr.componentes) l.Add(e.nombre);
            if (tr.modulosCombustible > 0) l.Add($"{tr.combustibleKg:0} kg de combustible ({tr.modulosCombustible} módulos)");
            if (tr.contenedores.Count > 0) l.Add(string.Join(", ", tr.contenedores));
            return string.Join("\n", l);
        }

        /// <summary>Recorre puntos (mundo) con un giro por punto; el remolcador acompaña al centro de la pieza.</summary>
        IEnumerator Recorrer(Transform p, List<Vector3> puntos, List<Quaternion> giros, float largo, float radio, float sPorTramo)
        {
            for (int i = 0; i < puntos.Count; i++)
            {
                float tramo = Vector3.Distance(p.position, puntos[i]);
                float s = Mathf.Clamp(sPorTramo * Mathf.Sqrt(Mathf.Max(1f, tramo) / 20f), sPorTramo * 0.5f, sPorTramo * 1.6f);
                yield return MoverA(p, puntos[i], giros[i], s, x => orbita.Apuntar(x + p.forward * (largo / 2f), radio));
            }
        }

        // ================================================================== ENVÍO ORBITAL NO TRIPULADO

        IEnumerator EjecutarEnvio()
        {
            SecuenciasConfig q = Cfg;
            ResumenTransporte tr = resumen.transporte;
            LimpiarEnvio();
            cam.transform.SetParent(null, true);
            cam.backgroundColor = cieloTierra;
            raizEnvio = new GameObject("Envío orbital · plataforma exterior y VC-1 (provisional)").transform;
            raizEnvio.position = LugarEnvio;
            Material suelo = Mat(new Color(0.45f, 0.5f, 0.38f), false), hormigon = Mat(new Color(0.62f, 0.62f, 0.6f), false),
                     pared = Mat(new Color(0.55f, 0.57f, 0.6f), false), oscuro = Mat(new Color(0.2f, 0.21f, 0.24f), false);
            Caja("Terreno", raizEnvio, new Vector3(0f, -0.5f, 0f), new Vector3(800f, 1f, 800f), suelo);
            SustitucionEscenario.Punto(Caja("Plataforma exterior del VC-1", raizEnvio, new Vector3(0f, 0.5f, 0f), new Vector3(22f, 1f, 22f), hormigon), "LC2/Plataforma");
            Caja("Camino de traslado", raizEnvio, new Vector3(45f, 0.02f, 0f), new Vector3(90f, 0.05f, 10f), oscuro);
            Transform nave = SustitucionEscenario.Punto(new GameObject("Nave C2 (exterior)").transform, "C2/NaveExterior");
            nave.SetParent(raizEnvio, false);
            nave.localPosition = new Vector3(72f, 0f, 0f);
            Caja("Nave C2 · pared norte", nave, new Vector3(0f, 9f, 15f), new Vector3(34f, 18f, 0.5f), pared);
            Caja("Nave C2 · pared sur", nave, new Vector3(0f, 9f, -15f), new Vector3(34f, 18f, 0.5f), pared);
            Caja("Nave C2 · fondo", nave, new Vector3(17f, 9f, 0f), new Vector3(0.5f, 18f, 30f), pared);
            Caja("Nave C2 · techo", nave, new Vector3(0f, 18f, 0f), new Vector3(34f, 0.5f, 30f), pared);
            Caja("Nave C2 · dintel", nave, new Vector3(-17f, 15f, 0f), new Vector3(0.5f, 6f, 30f), pared);

            Transform cofia, llama;
            Transform lanzador = ModeloTransporte.Lanzador(raizEnvio, new Vector3(0f, 1f, 0f), fabricaMat, out cofia, out llama);
            cofia.gameObject.SetActive(false);
            Transform transportador = Caja("Transportador del VC-1", raizEnvio, new Vector3(71f, 0.6f, 0f), new Vector3(32f, 1.2f, 9f), oscuro);

            vc1 = ModeloTransporte.Construir(raizEnvio, new Vector3(87f, 4.6f, 0f), Quaternion.Euler(0f, -90f, 0f), tr, catalogo, fabricaMat);
            Llenar(vc1, null, null);
            vc1.Puertas(1f);
            // Rótulo breve sobre el vehículo: qué es y qué lleva (indicador, no cartilla)
            TextMesh rot = Rotulo3D(raizEnvio, new Vector3(60f, 15f, 0f), "VC-1 · sin tripulación\n" + Lista(tr), 0.12f, new Color(1f, 0.9f, 0.55f));

            // 1 · Traslado desde la nave C2, con la bodega abierta para ver lo que lleva
            titulo = T("vc1.salida");
            progreso = "VC-1 · 1/8";
            Vector3 v0 = vc1.Raiz.localPosition, v1 = new Vector3(40f, 4.6f, 0f), t0 = transportador.localPosition, t1 = new Vector3(24f, 0.6f, 0f);
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(q.vc1Salida))
            {
                fundido = Mathf.Max(0f, 1f - s * 4f);
                float k = Mathf.SmoothStep(0f, 1f, s);
                vc1.Raiz.localPosition = Vector3.Lerp(v0, v1, k);
                transportador.localPosition = Vector3.Lerp(t0, t1, k);
                rot.transform.localPosition = vc1.Raiz.localPosition + new Vector3(-15f, 11f, 0f);
                Encuadre("vc1.salida", vc1.Raiz, LugarEnvio);
                yield return null;
            }
            fundido = 0f;
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(1.2f)) { vc1.Puertas(1f - s); yield return null; }
            vc1.Puertas(0f);
            rot.gameObject.SetActive(false);

            // 2 · Integración en el lanzador provisional (fuera de los edificios) y cofia
            titulo = T("vc1.integracion");
            progreso = "VC-1 · 2/8";
            Quaternion q1 = raizEnvio.rotation * Quaternion.Euler(-90f, 0f, 0f);
            yield return MoverA(vc1.Raiz, lanzador.TransformPoint(new Vector3(0f, ModeloTransporte.ApoyoVC1, 0f)), q1, D(q.vc1Integracion),
                                p => Encuadre("vc1.integracion", null, LugarEnvio));
            vc1.Raiz.SetParent(lanzador, true);
            cofia.gameObject.SetActive(true);
            Vector3 cofiaEsc = cofia.localScale;
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(0.8f)) { cofia.localScale = new Vector3(cofiaEsc.x, cofiaEsc.y * Mathf.Max(0.02f, s), cofiaEsc.z); yield return null; }
            cofia.localScale = cofiaEsc;

            // 3 · Cuenta atrás, encendido y lanzamiento
            titulo = T("vc1.lanzamiento");
            progreso = "VC-1 · 3/8";
            Encuadre("vc1.lanzamiento", lanzador, LugarEnvio);
            for (int n = 3; n >= 1; n--) { grande = n.ToString(); audioFuente.PlayOneShot(pitido); yield return Esperar(D(0.8f)); }
            grande = T("l4.encendido");
            llama.gameObject.SetActive(true);
            audioFuente.PlayOneShot(motor);
            yield return Esperar(D(0.8f));
            grande = "";
            titulo = T("vc1.ascenso");
            Vector3 l0 = lanzador.localPosition;
            float dur = D(q.vc1Ascenso);
            for (float s = 0f; s < dur; s += Time.deltaTime)
            {
                float u = s / dur * 4f;                                        // misma subida que antes, escalada al tiempo
                lanzador.localPosition = l0 + Vector3.up * (0.5f * 10f * u * u);
                llama.localScale = new Vector3(6f, 8f + Mathf.Sin(Time.time * 40f), 6f);
                PlanoCamara pl = ConfiguracionJuego.Plano("vc1.lanzamiento");
                cam.transform.LookAt(PuntoAncla(pl.anclaFoco, pl.foco, lanzador, LugarEnvio));
                cam.backgroundColor = Color.Lerp(cieloTierra, cieloEspacio, s / dur * 0.6f);
                yield return null;
            }
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / D(0.5f)) yield return null;
            raizEnvio.gameObject.SetActive(false);

            // 4 · En el espacio: separación de la cofia y del lanzador (transición breve, sin cartilla)
            if (q.mostrarSeparaciones) yield return EspacioVC1(tr);

            // 5 · Aproximación visible a la EM-4X real (ambas en el plano), punto de espera y alineación
            Dictionary<TipoComponente, int> deSerie = new Dictionary<TipoComponente, int>();     // lo instalado antes del envío
            orbita = EscenarioOrbital.Construir(LugarOrbita, deSerie, resumen, catalogo, fabricaMat);
            cam.backgroundColor = cieloEspacio;
            ModeloTransporte llegada = ModeloTransporte.Construir(orbita.Raiz, Vector3.zero, Quaternion.identity, tr, catalogo, fabricaMat);
            List<Transform> modulos = new List<Transform>(), contenedores = new List<Transform>();
            Dictionary<TipoComponente, Transform> piezas = Llenar(llegada, modulos, contenedores);
            llegada.Puertas(0f);
            llegada.Llamas.gameObject.SetActive(true);
            Quaternion alineado = orbita.GiroNave, desviado = alineado * Quaternion.Euler(-4f, 9f, 6f);
            llegada.Raiz.SetPositionAndRotation(orbita.LlegadaLejosVC1, desviado);
            titulo = T("vc1.aproximacion");
            progreso = "VC-1 · 4/8";
            dur = D(q.vc1Aproximacion);
            for (float s = 0f; s < dur; s += Time.deltaTime)
            {
                float k = s / dur;
                fundido = Mathf.Max(0f, 1f - k * 5f);
                llegada.Raiz.position = Vector3.Lerp(orbita.LlegadaLejosVC1, orbita.EsperaVC1, 1f - (1f - k) * (1f - k));   // frena al llegar
                llegada.Llamas.localScale = new Vector3(3f, 3f, 3f) * (1f - k * 0.6f + Mathf.Sin(Time.time * 30f) * 0.08f);
                Encuadre(k < 0.55f ? "vc1.lejos" : "vc1.espera");
                yield return null;
            }
            fundido = 0f;
            llegada.Raiz.position = orbita.EsperaVC1;
            llegada.Llamas.gameObject.SetActive(false);
            titulo = T("vc1.espera");
            yield return Esperar(D(0.6f));
            titulo = T("vc1.alineacion");
            progreso = "VC-1 · 5/8";
            Encuadre("vc1.espera");
            yield return MoverA(llegada.Raiz, orbita.EsperaVC1, alineado, D(q.vc1Alineacion));
            grande = "ALINEADO";
            audioFuente.PlayOneShot(pitido);
            yield return Esperar(D(0.5f));
            grande = "";
            Encuadre("vc1.servicio");
            yield return MoverA(llegada.Raiz, orbita.AparcamientoVC1, alineado, D(q.vc1Acercamiento));

            // 6 · Conexión de SERVICIO (carga): sale de la viga hasta el puerto de servicio del costado del VC-1
            titulo = T("vc1.conexion");
            progreso = "VC-1 · 6/8";
            Vector3 puertoVC1 = llegada.Raiz.TransformPoint(Pts.puertoServicioVC1);
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(q.vc1Conexion)) { orbita.Conexion(Mathf.SmoothStep(0f, 1f, s), puertoVC1); yield return null; }
            orbita.Conexion(1f, puertoVC1);
            audioFuente.PlayOneShot(pitido);
            grande = "CONECTADO";
            yield return Esperar(D(0.6f));
            grande = "";
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(1f)) { llegada.Puertas(s); yield return null; }
            llegada.Puertas(1f);

            // 7 · Recepción, retirada, colocación y comprobación de cada componente (llegar no es estar instalado)
            progreso = "VC-1 · 7/8";
            float libre = orbita.Raiz.InverseTransformPoint(llegada.Raiz.position).y + ModeloTransporte.DiametroCasco / 2f + 4f;
            foreach (ElementoTransporte e in tr.componentes)
            {
                Transform p;
                if (!piezas.TryGetValue(e.tipo, out p) || p == null) continue;
                string nombre = ModeloComponentes.NombreTipo(e.tipo);
                float rad = e.medidas.diametro / 2f, largo = e.medidas.largo;
                Quaternion giro = orbita.Raiz.rotation * EscenarioOrbital.Giro(e.tipo);
                Encuadre("vc1.servicio");

                // Recepción: sale de la bodega en vertical (sin girar) y va a la bandeja
                titulo = $"{T("vc1.recepcion")} · {nombre}: {e.nombre}";
                p.SetParent(orbita.Raiz, true);
                Vector3 enBodega = orbita.Raiz.InverseTransformPoint(p.position), bandeja = orbita.Raiz.InverseTransformPoint(orbita.Recepcion.position);
                List<Vector3> ruta = new List<Vector3>
                {
                    orbita.Raiz.TransformPoint(new Vector3(enBodega.x, libre + rad, enBodega.z)),
                    orbita.Raiz.TransformPoint(new Vector3(bandeja.x, libre + rad, bandeja.z)),
                    orbita.Recepcion.position + orbita.Raiz.up * (0.1f + rad)
                };
                yield return Recorrer(p, ruta, new List<Quaternion> { p.rotation, giro, giro }, largo, rad, D(q.vc1Tramo));

                // Retirada de la pieza de serie (si la hay): por el pasillo seguro hasta el bastidor
                Transform vieja;
                if (orbita.Instalado.TryGetValue(e.tipo, out vieja) && vieja != null)
                {
                    titulo = $"{T("vc1.retirada")} · {nombre} de serie";
                    MedidasTransporte mv = ModeloComponentes.Medidas(e.tipo, 0, catalogo);
                    if (e.tipo == TipoComponente.Tanque) orbita.CorteEtapa(true);
                    Encuadre(Plano(e.tipo));
                    vieja.SetParent(orbita.Raiz, true);
                    List<Vector3> ida = orbita.Acceso(e.tipo, mv.largo);
                    ida.Reverse();
                    Vector3 hueco = orbita.HuecoRetirada(mv.diametro);
                    ida.Add(hueco + orbita.Raiz.up * 6f);
                    ida.Add(hueco);
                    List<Quaternion> g = new List<Quaternion>();
                    for (int i = 0; i < ida.Count; i++) g.Add(i < ida.Count - 2 ? giro : orbita.GiroRetirada);
                    yield return Recorrer(vieja, ida, g, mv.largo, mv.diametro / 2f, D(q.vc1Tramo));
                    vieja.SetParent(orbita.Bastidor, true);
                    orbita.Instalado.Remove(e.tipo);
                }

                // Colocación: de la bandeja al montaje por el pasillo seguro
                titulo = $"{T("vc1.colocacion")} · {nombre}: {e.nombre}";
                Encuadre(Plano(e.tipo));
                List<Vector3> va = orbita.Acceso(e.tipo, largo);
                // Primero sube en vertical desde la bandeja hasta la altura del pasillo (no pasa sobre el bastidor de retiradas)
                va.Insert(0, orbita.Raiz.TransformPoint(new Vector3(bandeja.x, e.tipo == TipoComponente.Tanque ? Pts.pasilloTanqueY : Pts.pasilloY, bandeja.z)));
                va.Add(orbita.Raiz.TransformPoint(EscenarioOrbital.Montaje(e.tipo)));
                List<Quaternion> gv = new List<Quaternion>();
                for (int i = 0; i < va.Count; i++) gv.Add(giro);
                yield return Recorrer(p, va, gv, largo, rad, D(q.vc1Tramo));
                p.SetParent(orbita.Equipos, true);
                orbita.Instalado[e.tipo] = p;
                orbita.Apuntar(null);
                titulo = $"{T("vc1.comprobacion")} · {nombre}";
                grande = "COMPROBACIÓN CORRECTA";
                audioFuente.PlayOneShot(pitido);
                yield return Esperar(D(0.7f));
                grande = "";
                if (e.tipo == TipoComponente.Tanque) orbita.CorteEtapa(false);
            }

            // 8 · Combustible y carga pequeña (por la conexión y el puerto de suministro; sin personas)
            progreso = "VC-1 · 8/8";
            if (modulos.Count > 0)
            {
                titulo = $"{T("vc1.combustible")} · {tr.combustibleKg:0} kg";
                Encuadre("vc1.servicio");
                Vector3 n = orbita.Raiz.InverseTransformPoint(llegada.Raiz.TransformPoint(new Vector3(-ModeloTransporte.DiametroCasco / 2f, 0f, ModeloTransporte.ZAnillo + 2f)));
                Vector3 toma = Pts.tomaCombustible;
                float xm = toma.x + 2.6f;
                orbita.Manguera("Manguera de combustible (VC-1 → etapa)",
                    orbita.Raiz.TransformPoint(n), orbita.Raiz.TransformPoint(new Vector3(xm, n.y, n.z)),
                    orbita.Raiz.TransformPoint(new Vector3(xm, n.y, toma.z)), orbita.Raiz.TransformPoint(new Vector3(xm, toma.y, toma.z)),
                    orbita.TomaCombustible);
                foreach (Transform m in modulos)
                {
                    Vector3 e0 = m.localScale;
                    for (float s = 0f; s < 1f; s += Time.deltaTime / D(0.18f)) { m.localScale = e0 * (1f - s); yield return null; }
                    m.gameObject.SetActive(false);
                }
                orbita.QuitarMangueras();
            }
            if (contenedores.Count > 0)
            {
                titulo = T("vc1.carga") + " · " + string.Join(", ", tr.contenedores);
                Encuadre("vc1.suministro");
                for (float s = 0f; s < 1f; s += Time.deltaTime / D(0.6f)) { orbita.AbrirSuministro(s); yield return null; }
                orbita.AbrirSuministro(1f);
                Vector3 port = orbita.PuertoSuministro, nrm = orbita.NormalSuministro;
                foreach (Transform c in contenedores)
                {
                    c.SetParent(orbita.Raiz, true);
                    Vector3 sube = c.position; sube.y = orbita.Raiz.TransformPoint(new Vector3(0f, Pts.pasilloY, 0f)).y;
                    List<Vector3> r = new List<Vector3> { sube, port + nrm * 6f + orbita.Raiz.up * 4f, port + nrm * 6f, port + nrm * 0.4f };
                    yield return Recorrer(c, r, new List<Quaternion> { c.rotation, orbita.GiroNave, orbita.GiroNave, orbita.GiroNave }, 1f, 0.5f, D(0.7f));
                    c.gameObject.SetActive(false);
                }
                orbita.Apuntar(null);
                for (float s = 0f; s < 1f; s += Time.deltaTime / D(0.6f)) { orbita.AbrirSuministro(1f - s); yield return null; }
                orbita.AbrirSuministro(0f);
            }

            // Desconexión y salida del VC-1 (no se queda en ningún punto de atraque: la L-4 llega después a otro sitio)
            titulo = T("vc1.desconexion");
            Encuadre("vc1.espera");
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(0.8f)) { llegada.Puertas(1f - s); yield return null; }
            llegada.Puertas(0f);
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(q.vc1Conexion)) { orbita.Conexion(1f - s, puertoVC1); yield return null; }
            orbita.Conexion(0f, puertoVC1);
            llegada.Llamas.gameObject.SetActive(false);      // retrocede con los propulsores de maniobra (sin llama de popa)
            Vector3 a0 = llegada.Raiz.position, a1 = orbita.EsperaVC1 + (orbita.EsperaVC1 - orbita.AparcamientoVC1) * 2f;
            dur = D(q.vc1Salida2);
            for (float s = 0f; s < dur; s += Time.deltaTime)
            {
                float k = s / dur;
                llegada.Raiz.position = Vector3.Lerp(a0, a1, k * k);
                if (k > 0.5f) grande = T("vc1.preparada");
                yield return null;
            }
            llegada.Raiz.gameObject.SetActive(false);
            titulo = T("vc1.preparada");
            yield return Esperar(D(0.8f));
            grande = "";
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / D(0.6f)) yield return null;
        }

        static string Plano(TipoComponente t) =>
            t == TipoComponente.Instrumento ? "vc1.instalacion.ciencia" : t == TipoComponente.Relevo ? "vc1.instalacion.comunicaciones" : "vc1.instalacion.popa";

        /// <summary>Plano en el espacio: el LC-2 sigue subiendo, se separa la cofia y después el VC-1, que enciende sus motores.</summary>
        IEnumerator EspacioVC1(ResumenTransporte tr)
        {
            SecuenciasConfig q = Cfg;
            LimpiarEspacio();
            raizEspacio = new GameObject("Espacio · separación del VC-1 (provisional)").transform;
            raizEspacio.position = LugarEspacio;
            Planeta(raizEspacio, 1500f, 200f);
            cam.backgroundColor = cieloEspacio;
            Transform cofia, llama;
            Transform lz = ModeloTransporte.Lanzador(raizEspacio, Vector3.zero, fabricaMat, out cofia, out llama);
            Transform acel = lz.Find("Aceleradores");
            if (acel != null) acel.gameObject.SetActive(false);          // ya se separaron durante el ascenso
            ModeloTransporte v = ModeloTransporte.Construir(lz, new Vector3(0f, ModeloTransporte.ApoyoVC1, 0f), Quaternion.Euler(-90f, 0f, 0f), tr, catalogo, fabricaMat);
            Llenar(v, null, null);
            llama.gameObject.SetActive(true);
            titulo = T("vc1.ascenso");
            float dur = D(q.vc1Espacio), t1 = dur * 0.25f, t2 = dur * 0.55f;
            Transform[] mitades = null;
            float dc = ConfiguracionJuego.Actual.lanzadorVC1.diametroCofia;
            Vector3 pBase = lz.position;
            for (float s = 0f; s < dur; s += Time.deltaTime)
            {
                fundido = Mathf.Max(0f, 1f - s / D(0.4f));
                lz.position = pBase + Vector3.up * (s * 30f);
                if (s < t1) llama.localScale = new Vector3(6f, 8f + Mathf.Sin(Time.time * 40f), 6f);
                else if (mitades == null)
                {
                    // Separación de la cofia: dos mitades que se abren hacia los lados
                    llama.gameObject.SetActive(false);
                    titulo = T("vc1.cofia");
                    mitades = new Transform[2];
                    for (int i = 0; i < 2; i++)
                    {
                        Transform m = Instantiate(cofia.gameObject, cofia.parent).transform;
                        m.name = "Mitad de la cofia";
                        m.localPosition = cofia.localPosition + new Vector3((i == 0 ? -1f : 1f) * (dc / 4f + ModeloTransporte.DiametroCasco / 2f), 0f, 0f);   // empiezan ya fuera del casco del VC-1
                        m.localRotation = cofia.localRotation;
                        m.localScale = new Vector3(cofia.localScale.x * 0.5f, cofia.localScale.y, cofia.localScale.z);
                        m.SetParent(raizEspacio, true);
                        mitades[i] = m;
                    }
                    cofia.gameObject.SetActive(false);
                    audioFuente.PlayOneShot(pitido);
                }
                else
                {
                    float k = (s - t1) / (dur - t1);
                    for (int i = 0; i < 2; i++)
                    {
                        float lado = i == 0 ? -1f : 1f;
                        mitades[i].position += (Vector3.right * lado * 9f + Vector3.up * 26f) * Time.deltaTime;
                        mitades[i].rotation = Quaternion.Euler(0f, 0f, lado * -40f * k);
                    }
                    if (s >= t2)
                    {
                        // Separación del VC-1: el lanzador se queda atrás y el VC-1 enciende sus motores de maniobra
                        if (v.Raiz.parent == lz)
                        {
                            titulo = T("vc1.separacion");
                            v.Raiz.SetParent(raizEspacio, true);
                            audioFuente.PlayOneShot(pitido);
                        }
                        float k2 = (s - t2) / (dur - t2);
                        v.Raiz.position = lz.TransformPoint(new Vector3(0f, ModeloTransporte.ApoyoVC1, 0f)) + Vector3.up * (k2 * k2 * 40f);
                        v.Llamas.gameObject.SetActive(k2 > 0.25f);
                    }
                }
                Encuadre("vc1.espacio", lz, LugarEspacio);
                if (s > dur - D(0.5f)) fundido = (s - (dur - D(0.5f))) / D(0.5f);
                yield return null;
            }
            fundido = 1f;
            LimpiarEspacio();
        }

        // ================================================================== CARGA DE LA L-4 Y LLEGADA DE LA TRIPULACIÓN

        /// <summary>Contenedores que viajan en la L-4 (los mismos lotes que la preparación asigna a la lanzadera).</summary>
        List<string> CargaDeLaL4()
        {
            List<string> l = new List<string>();
            ResumenLanzadera rl = resumen != null ? resumen.lanzadera : null;
            if (rl == null || !rl.cargaPequenaEnLanzadera) return l;
            if (preparacion.suministrosExtra > 0) l.Add("suministros");
            if (preparacion.kitsExtra > 0) l.Add("kits");
            if (preparacion.sonda) l.Add("sonda");
            return l;
        }

        IEnumerator CargaL4()
        {
            List<string> lotes = CargaDeLaL4();
            ResumenLanzadera rl = resumen != null ? resumen.lanzadera : null;
            bool comb = rl != null && rl.combustibleEnLanzadera;
            if (lotes.Count == 0 && !comb) yield break;
            titulo = T("l4.carga") + (comb ? $" · {rl.combustibleEncargadoKg:0} kg en el tanque de transferencia" : "");
            Vector3 b = modelo.HuecosBodega.Count > 0 ? modelo.HuecosBodega[0].position : conjunto.position + Vector3.up * 20f;
            Mirar(Lugar + new Vector3(36f, 16f, 60f), b + new Vector3(0f, -6f, 0f));
            for (int i = 0; i < lotes.Count && i < modelo.HuecosBodega.Count; i++)
            {
                Transform c = ModeloComponentes.Contenedor(lotes[i], raiz, fabricaMat);
                c.position = conjunto.position + new Vector3(7f, 0.1f, -1.5f + i * 1.5f);
                cargaL4.Add(c);
            }
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(0.6f)) { fundido = 1f - s; yield return null; }
            fundido = 0f;
            for (int i = 0; i < cargaL4.Count; i++)
            {
                Transform c = cargaL4[i], h = modelo.HuecosBodega[i];
                yield return MoverA(c, h.position, h.rotation, D(1f));
                c.SetParent(h, true);
            }
            if (comb) { grande = "TANQUE DE TRANSFERENCIA LLENO"; yield return Esperar(D(0.8f)); grande = ""; }
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / D(0.4f)) yield return null;
            fundido = 1f;
        }

        /// <summary>Plano en el espacio: la L-4 (ya sin refuerzos) sigue subiendo y se separa la etapa principal.</summary>
        IEnumerator EspacioL4()
        {
            SecuenciasConfig q = Cfg;
            LimpiarEspacio();
            raizEspacio = new GameObject("Espacio · separación de la etapa de la L-4 (provisional)").transform;
            raizEspacio.position = LugarEspacio;
            Planeta(raizEspacio, 1500f, 200f);
            cam.backgroundColor = cieloEspacio;
            ConfiguracionLanzadera c = ConfigActual().Copia();
            ModeloLanzadera m = ModeloLanzadera.Construir(raizEspacio, Vector3.zero, c, fabricaMat);
            Transform refuerzos;
            if (m.Piezas.TryGetValue(PiezaLanzadera.Refuerzos, out refuerzos) && refuerzos != null) refuerzos.gameObject.SetActive(false);
            Transform etapa = m.Raiz.Find("Etapa principal (pieza)");
            m.Llama.gameObject.SetActive(true);
            titulo = T("l4.ascenso");
            float dur = D(q.l4Espacio), t1 = dur * 0.35f;
            Vector3 p0 = m.Raiz.position;
            bool separada = false;
            for (float s = 0f; s < dur; s += Time.deltaTime)
            {
                fundido = Mathf.Max(0f, 1f - s / D(0.4f));
                m.Raiz.position = p0 + Vector3.up * (s * 25f);
                if (s < t1) m.Llama.localScale = new Vector3(3f, 5f + Mathf.Sin(Time.time * 40f) * 0.7f, 3f);
                else
                {
                    if (!separada)
                    {
                        separada = true;
                        titulo = T("l4.etapa");
                        m.Llama.gameObject.SetActive(false);
                        if (etapa != null) etapa.SetParent(raizEspacio, true);
                        audioFuente.PlayOneShot(pitido);
                    }
                    if (etapa != null)
                    {
                        etapa.position += Vector3.up * (25f - 14f * (s - t1)) * Time.deltaTime;   // se queda atrás
                        etapa.rotation = Quaternion.Euler(6f * (s - t1), 0f, 3f * (s - t1));
                    }
                }
                Encuadre("l4.espacio", m.Raiz, LugarEspacio);
                if (s > dur - D(0.5f)) fundido = (s - (dur - D(0.5f))) / D(0.5f);
                yield return null;
            }
            fundido = 1f;
            LimpiarEspacio();
        }

        IEnumerator LlegadaL4()
        {
            SecuenciasConfig q = Cfg;
            cam.transform.SetParent(null, true);
            Dentro(false);
            modelo.MostrarExterior(false);       // la L-4 de tierra se queda atrás; en órbita se ve su parte superior
            if (q.mostrarSeparaciones) yield return EspacioL4();
            if (orbita != null) orbita.Destruir();
            Dictionary<TipoComponente, int> final = new Dictionary<TipoComponente, int>
            {
                { TipoComponente.Motor, preparacion.motor }, { TipoComponente.Tanque, preparacion.tanque },
                { TipoComponente.Instrumento, preparacion.instrumento }, { TipoComponente.Relevo, preparacion.relevo }
            };
            // La EM-4X ya preparada (si hubo envío, el VC-1 ya se fue: no ocupa ningún punto de atraque)
            orbita = EscenarioOrbital.Construir(LugarOrbita, final, resumen, catalogo, fabricaMat);
            ConfiguracionLanzadera c = preparacion.lanzadera != null ? preparacion.lanzadera.Copia() : new ConfiguracionLanzadera();
            DatosLanzadera.Limitar(c);
            ModeloLanzadera l4 = ModeloLanzadera.Construir(orbita.Raiz, Vector3.zero, c, fabricaMat, null, null, true);
            Transform cab = CrearCabina(l4, out Transform escotilla, out _);
            l4.Interior = cab;
            cab.gameObject.SetActive(false);
            List<string> lotes = CargaDeLaL4();
            for (int i = 0; i < lotes.Count && i < l4.HuecosBodega.Count; i++)
            {
                Transform ct = ModeloComponentes.Contenedor(lotes[i], l4.HuecosBodega[i], fabricaMat);
                ct.localPosition = Vector3.zero;
            }
            Vector3 arriba = orbita.Raiz.up;
            Vector3 fin = orbita.AtraqueL4 - arriba * l4.Morro;                   // el morro toca el puerto del Mando
            Vector3 espera = fin - arriba * Pts.esperaL4M;
            Vector3 ini = espera + orbita.Raiz.TransformDirection(new Vector3(30f, -140f, -260f));
            Quaternion alineado = orbita.GiroNave, desviado = alineado * Quaternion.Euler(7f, 0f, -8f);
            l4.Raiz.SetPositionAndRotation(ini, desviado);
            cam.backgroundColor = cieloEspacio;

            // Aproximación visible (la EM-4X y la L-4 en el mismo plano)
            titulo = T("l4.aproximacion");
            progreso = "TRIPULACIÓN · aproximación";
            float dur = D(q.l4Aproximacion);
            for (float s = 0f; s < dur; s += Time.deltaTime)
            {
                float k = s / dur;
                fundido = Mathf.Max(0f, 1f - k * 5f);
                l4.Raiz.position = Vector3.Lerp(ini, espera, 1f - (1f - k) * (1f - k));
                Encuadre(k < 0.6f ? "l4.lejos" : "l4.alineacion");
                yield return null;
            }
            fundido = 0f;
            l4.Raiz.position = espera;

            // Alineación bajo el puerto del Mando y atraque guiado
            titulo = T("l4.alineacion");
            Encuadre("l4.alineacion");
            yield return MoverA(l4.Raiz, espera, alineado, D(q.l4Alineacion));
            grande = "ALINEADA";
            audioFuente.PlayOneShot(pitido);
            yield return Esperar(D(0.5f));
            grande = "";
            Encuadre("l4.atraque");
            yield return MoverA(l4.Raiz, fin, alineado, D(q.l4Atraque));
            titulo = T("l4.atraque");
            grande = T("l4.atraque");
            audioFuente.PlayOneShot(pitido);
            yield return Esperar(D(0.8f));
            grande = "";

            // Transferencias (por dentro: la carga no atraviesa cascos; se indica la operación)
            ResumenLanzadera rl = resumen != null ? resumen.lanzadera : null;
            if (lotes.Count > 0)
            {
                titulo = T("l4.transferencia") + " · interior · " + string.Join(", ", lotes);
                for (float s = 0f; s < 1f; s += Time.deltaTime / D(1.1f)) { progreso = $"TRANSFERENCIA · {s * 100f:0} %"; yield return null; }
                foreach (Transform h in l4.HuecosBodega) foreach (Transform ct in h) ct.gameObject.SetActive(false);
            }
            if (rl != null && rl.combustibleEnLanzadera)
            {
                titulo = $"{T("vc1.combustible")} · L-4 → EM-4X · {rl.combustibleEncargadoKg:0} kg";
                // Manguera por fuera y por debajo de la nave (no cruza la bodega, la esclusa ni la viga)
                Vector3 a = l4.Piezas.ContainsKey(PiezaLanzadera.Tanque) ? l4.Piezas[PiezaLanzadera.Tanque].position : fin;
                Vector3 al = orbita.Raiz.InverseTransformPoint(a), toma = Pts.tomaCombustible;
                float xm = toma.x + 2.6f, yb = Mathf.Min(al.y, Pts.vigaServicio.y - 3f);
                orbita.Manguera("Manguera de combustible (L-4 → etapa)",
                    orbita.Raiz.TransformPoint(al + new Vector3(ModeloLanzadera.Diametro / 2f, 0f, 0f)),
                    orbita.Raiz.TransformPoint(new Vector3(xm, yb, al.z)), orbita.Raiz.TransformPoint(new Vector3(xm, yb, toma.z)),
                    orbita.Raiz.TransformPoint(new Vector3(xm, toma.y, toma.z)), orbita.TomaCombustible);
                for (float s = 0f; s < 1f; s += Time.deltaTime / D(1.1f)) { progreso = $"COMBUSTIBLE · {s * 100f:0} %"; yield return null; }
                orbita.QuitarMangueras();
            }
            progreso = "TRIPULACIÓN · a bordo";

            // Apertura del acceso y paso en primera persona (continúa sin corte en la expedición, EntradaABordo)
            titulo = T("l4.apertura");
            l4.MostrarExterior(false);
            cab.gameObject.SetActive(true);
            cam.transform.SetParent(cab, false);
            cam.fieldOfView = 70f;
            cam.transform.localPosition = new Vector3(0.3f, -0.4f, -0.3f);
            cam.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            Vector3 e0 = escotilla.localPosition;
            for (float s = 0f; s < 1f; s += Time.deltaTime / D(q.l4Apertura))
            {
                escotilla.localPosition = e0 + Vector3.right * 1.3f * Mathf.SmoothStep(0f, 1f, s);
                yield return null;
            }
            audioFuente.PlayOneShot(pitido);
            titulo = T("l4.paso");
            cam.transform.SetParent(null, true);
            Vector3 c0 = cam.transform.position;
            Quaternion r0 = cam.transform.rotation;
            Vector3 bajo = cab.TransformPoint(new Vector3(0f, 0.6f, 0f));
            Vector3 posFin; Quaternion rotFin;
            EntradaABordo.PoseInicial(orbita.Definicion, out posFin, out rotFin);
            posFin = orbita.Raiz.TransformPoint(posFin);
            rotFin = orbita.Raiz.rotation * rotFin;
            dur = D(q.l4Paso);
            for (float s = 0f; s < 1f; s += Time.deltaTime / dur)
            {
                float k = Mathf.SmoothStep(0f, 1f, s);
                cam.transform.position = k < 0.4f ? Vector3.Lerp(c0, bajo, k / 0.4f) : Vector3.Lerp(bajo, posFin, (k - 0.4f) / 0.6f);
                cam.transform.rotation = Quaternion.Slerp(r0, rotFin, k);
                fundido = s > 0.7f ? (s - 0.7f) / 0.3f : 0f;
                yield return null;
            }
            cam.transform.SetPositionAndRotation(posFin, rotFin);
            fundido = 1f;
        }
    }
}
