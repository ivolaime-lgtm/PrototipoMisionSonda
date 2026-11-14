using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Parte de SecuenciaDespegue: ENVÍO ORBITAL NO TRIPULADO (VC-1), carga de la L-4 y LLEGADA de la tripulación a la EM-4X.
    /// Planos breves con geometría provisional y movimientos guiados (sin física). Solo representa lo que ya está decidido
    /// en la preparación cerrada: no cambia configuración, recursos ni costes.
    /// </summary>
    public partial class SecuenciaDespegue
    {
        static readonly Vector3 LugarEnvio = new Vector3(-5000f, 0f, 0f);      // plataforma exterior del VC-1
        static readonly Vector3 LugarOrbita = new Vector3(0f, 4000f, 9000f);   // zona de servicio orbital de la EM-4X

        Transform raizEnvio;
        ModeloTransporte vc1;
        EscenarioOrbital orbita;
        readonly List<Transform> cargaL4 = new List<Transform>();
        Font fuenteRotulos;

        void LimpiarEnvio()
        {
            if (raizEnvio != null) Destroy(raizEnvio.gameObject);
            raizEnvio = null;
            vc1 = null;
            if (orbita != null) { orbita.Destruir(); orbita = null; }
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
            for (float u = 0f; u < 1f; u += Time.deltaTime / s)
            {
                float k = Mathf.SmoothStep(0f, 1f, u);
                t.position = Vector3.Lerp(p0, destino, k);
                t.rotation = Quaternion.Slerp(q0, giro, k);
                cadaPaso?.Invoke(t.position);
                yield return null;
            }
            t.position = destino;
            t.rotation = giro;
        }

        void Mirar(Vector3 desde, Vector3 hacia)
        {
            cam.transform.SetParent(null, true);
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
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

        // ================================================================== ENVÍO ORBITAL NO TRIPULADO

        IEnumerator EjecutarEnvio()
        {
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
            TextMesh rot = Rotulo3D(raizEnvio, new Vector3(60f, 15f, 0f), "VC-1 · TRANSPORTE ORBITAL NO TRIPULADO\nsin tripulación · lleva:\n" + Lista(tr), 0.12f, new Color(1f, 0.9f, 0.55f));

            // 1 · Traslado desde la nave C2, con la bodega abierta para ver lo que lleva
            titulo = "1 · ENVÍO ORBITAL · VC-1 cargado sale de la nave C2 hacia la plataforma exterior";
            progreso = "ENVÍO ORBITAL · 1 de 6";
            Vector3 v0 = vc1.Raiz.localPosition, v1 = new Vector3(40f, 4.6f, 0f), t0 = transportador.localPosition, t1 = new Vector3(24f, 0.6f, 0f);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 3f)
            {
                fundido = Mathf.Max(0f, 1f - s * 4f);
                float k = Mathf.SmoothStep(0f, 1f, s);
                vc1.Raiz.localPosition = Vector3.Lerp(v0, v1, k);
                transportador.localPosition = Vector3.Lerp(t0, t1, k);
                rot.transform.localPosition = vc1.Raiz.localPosition + new Vector3(-15f, 11f, 0f);
                Mirar(LugarEnvio + new Vector3(30f, 20f, 38f), vc1.Raiz.position + new Vector3(-12f, 0f, 0f));
                yield return null;
            }
            fundido = 0f;
            for (float s = 0f; s < 1f; s += Time.deltaTime / 1.2f) { vc1.Puertas(1f - s); yield return null; }
            vc1.Puertas(0f);
            rot.gameObject.SetActive(false);

            // 2 · Integración en el lanzador provisional (fuera de los edificios) y cofia
            titulo = "2 · Integración del VC-1 en el lanzador provisional LC-2 (plataforma exterior)";
            progreso = "ENVÍO ORBITAL · 2 de 6";
            Quaternion q1 = raizEnvio.rotation * Quaternion.Euler(-90f, 0f, 0f);
            yield return MoverA(vc1.Raiz, lanzador.TransformPoint(new Vector3(0f, 40.5f, 0f)), q1, 2.4f,
                                p => Mirar(LugarEnvio + new Vector3(70f, 30f, 80f), LugarEnvio + new Vector3(10f, 35f, 0f)));
            vc1.Raiz.SetParent(lanzador, true);
            cofia.gameObject.SetActive(true);
            Vector3 cofiaEsc = cofia.localScale;
            for (float s = 0f; s < 1f; s += Time.deltaTime / 0.8f) { cofia.localScale = new Vector3(cofiaEsc.x, cofiaEsc.y * Mathf.Max(0.02f, s), cofiaEsc.z); yield return null; }
            cofia.localScale = cofiaEsc;

            // 3 · Cuenta atrás, encendido y lanzamiento
            titulo = "3 · Lanzamiento del transporte orbital no tripulado";
            progreso = "ENVÍO ORBITAL · 3 de 6";
            Mirar(LugarEnvio + new Vector3(110f, 8f, -150f), LugarEnvio + new Vector3(0f, 40f, 0f));
            for (int n = 3; n >= 1; n--) { grande = n.ToString(); audioFuente.PlayOneShot(pitido); yield return Esperar(0.8f); }
            grande = "ENCENDIDO";
            llama.gameObject.SetActive(true);
            audioFuente.PlayOneShot(motor);
            yield return Esperar(0.8f);
            grande = "";
            Vector3 l0 = lanzador.localPosition;
            for (float s = 0f; s < 4f; s += Time.deltaTime)
            {
                lanzador.localPosition = l0 + Vector3.up * (0.5f * 10f * s * s);
                llama.localScale = new Vector3(6f, 8f + Mathf.Sin(Time.time * 40f), 6f);
                cam.transform.LookAt(lanzador.position + Vector3.up * 40f);
                cam.backgroundColor = Color.Lerp(cieloTierra, cieloEspacio, s / 4f * 0.5f);
                yield return null;
            }
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / 0.5f) yield return null;

            // 4 · Llegada a la zona de servicio orbital
            raizEnvio.gameObject.SetActive(false);
            Dictionary<TipoComponente, int> deSerie = new Dictionary<TipoComponente, int>();     // lo instalado antes del envío
            orbita = EscenarioOrbital.Construir(LugarOrbita, deSerie, resumen, catalogo, fabricaMat);
            cam.backgroundColor = cieloEspacio;
            ModeloTransporte llegada = ModeloTransporte.Construir(orbita.Raiz, Vector3.zero, Quaternion.identity, tr, catalogo, fabricaMat);
            List<Transform> modulos = new List<Transform>(), contenedores = new List<Transform>();
            Dictionary<TipoComponente, Transform> piezas = Llenar(llegada, modulos, contenedores);
            llegada.Puertas(0f);
            llegada.Raiz.position = orbita.LlegadaVC1 + new Vector3(0f, -45f, -140f);
            titulo = "4 · El VC-1 llega a la zona de servicio orbital de la EM-4X";
            progreso = "ENVÍO ORBITAL · 4 de 6";
            Vector3 vista = LugarOrbita + new Vector3(72f, -4f, -12f), foco = LugarOrbita + new Vector3(0f, -10f, -20f);
            Mirar(vista, foco);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 3.2f)
            {
                fundido = Mathf.Max(0f, 1f - s * 3f);
                llegada.Raiz.position = Vector3.Lerp(orbita.LlegadaVC1 + new Vector3(0f, -45f, -140f), orbita.LlegadaVC1, Mathf.SmoothStep(0f, 1f, s));
                yield return null;
            }
            fundido = 0f;
            llegada.Llamas.gameObject.SetActive(false);
            for (float s = 0f; s < 1f; s += Time.deltaTime / 1f) { llegada.Puertas(s); yield return null; }
            llegada.Puertas(1f);

            // 5 · Recepción, retirada, colocación y comprobación de cada componente (llegar no es estar instalado)
            progreso = "ENVÍO ORBITAL · 5 de 6";
            int k2 = 0;
            foreach (ElementoTransporte e in tr.componentes)
            {
                Transform p;
                if (!piezas.TryGetValue(e.tipo, out p) || p == null) continue;
                string nombre = ModeloComponentes.NombreTipo(e.tipo);
                Quaternion giro = orbita.Nave.rotation * EscenarioOrbital.Giro(e.tipo);
                titulo = $"5 · RECEPCIÓN · {nombre}: {e.nombre}";
                p.SetParent(orbita.Raiz, true);
                yield return MoverA(p, orbita.Recepcion.position, giro, 1.3f, x => orbita.Apuntar(x));
                Transform vieja;
                if (orbita.Instalado.TryGetValue(e.tipo, out vieja) && vieja != null && k2 < orbita.HuecosRetiradas.Count)
                {
                    titulo = $"5 · RETIRADA · {nombre} de serie → soporte de piezas retiradas (queda sujeta allí)";
                    vieja.SetParent(orbita.Raiz, true);
                    yield return MoverA(vieja, orbita.HuecosRetiradas[k2].position, orbita.Raiz.rotation * Quaternion.Euler(0f, 90f, 0f), 1.3f, x => orbita.Apuntar(x));   // atravesada en el bastidor
                    vieja.SetParent(orbita.HuecosRetiradas[k2], true);
                    k2++;
                }
                titulo = $"5 · COLOCACIÓN · {nombre}: {e.nombre}";
                yield return MoverA(p, orbita.Nave.TransformPoint(EscenarioOrbital.Montaje(e.tipo)), giro, 1.3f, x => orbita.Apuntar(x));
                p.SetParent(orbita.Equipos, true);
                orbita.Instalado[e.tipo] = p;
                orbita.Apuntar(null);
                titulo = $"5 · COMPROBACIÓN · {nombre} instalado";
                grande = "COMPROBACIÓN CORRECTA";
                audioFuente.PlayOneShot(pitido);
                yield return Esperar(0.7f);
                grande = "";
            }

            // 6 · Combustible y carga pequeña
            progreso = "ENVÍO ORBITAL · 6 de 6";
            if (modulos.Count > 0)
            {
                titulo = $"6 · TRANSFERENCIA DE COMBUSTIBLE · {tr.combustibleKg:0} kg al tanque de la EM-4X";
                orbita.Apuntar(orbita.TomaCombustible);
                foreach (Transform m in modulos)
                {
                    Vector3 e0 = m.localScale;
                    for (float s = 0f; s < 1f; s += Time.deltaTime / 0.18f) { m.localScale = e0 * (1f - s); yield return null; }
                    m.gameObject.SetActive(false);
                }
                orbita.Apuntar(null);
            }
            if (contenedores.Count > 0)
            {
                titulo = "6 · CARGA RECIBIDA por el puerto de suministro (nodo de popa): " + string.Join(", ", tr.contenedores);
                foreach (Transform c in contenedores) c.SetParent(orbita.Raiz, true);
                for (float s = 0f; s < 1f; s += Time.deltaTime / 1.2f)
                {
                    foreach (Transform c in contenedores) c.position = Vector3.Lerp(c.position, orbita.PuertoSuministro, Mathf.Min(1f, Time.deltaTime * 4f));
                    yield return null;
                }
                foreach (Transform c in contenedores) c.gameObject.SetActive(false);
            }
            for (float s = 0f; s < 1f; s += Time.deltaTime / 0.8f) { llegada.Puertas(1f - s); yield return null; }
            titulo = "EM-4X PREPARADA · lo encargado está instalado antes de que llegue la tripulación";
            grande = "EM-4X PREPARADA";
            yield return Esperar(1.3f);
            grande = "";
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / 0.6f) yield return null;
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
            titulo = "CARGA DE LA L-4 · tripulación y carga" + (comb ? $" · tanque de transferencia con {rl.combustibleEncargadoKg:0} kg para la EM-4X" : "");
            Vector3 b = modelo.HuecosBodega.Count > 0 ? modelo.HuecosBodega[0].position : conjunto.position + Vector3.up * 20f;
            Mirar(Lugar + new Vector3(36f, 16f, 60f), b + new Vector3(0f, -6f, 0f));
            for (int i = 0; i < lotes.Count && i < modelo.HuecosBodega.Count; i++)
            {
                Transform c = ModeloComponentes.Contenedor(lotes[i], raiz, fabricaMat);
                c.position = conjunto.position + new Vector3(7f, 0.1f, -1.5f + i * 1.5f);
                cargaL4.Add(c);
            }
            for (float s = 0f; s < 1f; s += Time.deltaTime / 0.6f) { fundido = 1f - s; yield return null; }
            fundido = 0f;
            for (int i = 0; i < cargaL4.Count; i++)
            {
                Transform c = cargaL4[i], h = modelo.HuecosBodega[i];
                yield return MoverA(c, h.position, h.rotation, 1f);
                c.SetParent(h, true);
            }
            if (comb) { grande = "TANQUE DE TRANSFERENCIA LLENO"; yield return Esperar(0.8f); grande = ""; }
            for (fundido = 0f; fundido < 1f; fundido += Time.deltaTime / 0.4f) yield return null;
            fundido = 1f;
        }

        IEnumerator LlegadaL4()
        {
            cam.transform.SetParent(null, true);
            Dentro(false);
            modelo.MostrarExterior(false);       // la L-4 de tierra se queda atrás; en órbita se ve su parte superior
            if (orbita != null) orbita.Destruir();
            Dictionary<TipoComponente, int> final = new Dictionary<TipoComponente, int>
            {
                { TipoComponente.Motor, preparacion.motor }, { TipoComponente.Tanque, preparacion.tanque },
                { TipoComponente.Instrumento, preparacion.instrumento }, { TipoComponente.Relevo, preparacion.relevo }
            };
            orbita = EscenarioOrbital.Construir(LugarOrbita, final, resumen, catalogo, fabricaMat);
            if (HayEnvio)
            {
                ModeloTransporte atracado = ModeloTransporte.Construir(orbita.Raiz, Vector3.zero, Quaternion.identity, resumen.transporte, catalogo, fabricaMat);
                atracado.Raiz.position = orbita.LlegadaVC1;
            }
            ConfiguracionLanzadera c = preparacion.lanzadera != null ? preparacion.lanzadera.Copia() : new ConfiguracionLanzadera();
            DatosLanzadera.Limitar(c);
            ModeloLanzadera l4 = ModeloLanzadera.Construir(orbita.Raiz, Vector3.zero, c, fabricaMat, null, null, true);
            List<Transform> bodega = new List<Transform>();
            List<string> lotes = CargaDeLaL4();
            for (int i = 0; i < lotes.Count && i < l4.HuecosBodega.Count; i++)
            {
                Transform ct = ModeloComponentes.Contenedor(lotes[i], l4.HuecosBodega[i], fabricaMat);
                ct.localPosition = Vector3.zero;
                bodega.Add(ct);
            }
            Vector3 fin = orbita.AtraqueL4 - Vector3.up * l4.AlturaTotal, ini = fin + new Vector3(0f, -45f, -70f);
            l4.Raiz.position = ini;
            cam.backgroundColor = cieloEspacio;
            titulo = "LLEGADA A LA EM-4X · la L-4 sube con la tripulación y atraca bajo el módulo de Mando";
            progreso = "TRIPULACIÓN · llegada";
            Mirar(LugarOrbita + new Vector3(42f, -16f, 36f), orbita.AtraqueL4 + new Vector3(0f, -9f, 0f));
            for (float s = 0f; s < 1f; s += Time.deltaTime / 3.5f)
            {
                fundido = Mathf.Max(0f, 1f - s * 3f);
                l4.Raiz.position = Vector3.Lerp(ini, fin, Mathf.SmoothStep(0f, 1f, s));
                yield return null;
            }
            fundido = 0f;
            grande = "ATRAQUE";
            audioFuente.PlayOneShot(pitido);
            yield return Esperar(0.8f);
            grande = "";
            if (bodega.Count > 0)
            {
                titulo = "CARGA DE LA L-4 → EM-4X: " + string.Join(", ", lotes);
                foreach (Transform b in bodega) b.SetParent(orbita.Raiz, true);
                for (float s = 0f; s < 1f; s += Time.deltaTime / 1.1f)
                {
                    foreach (Transform b in bodega) b.position = Vector3.Lerp(b.position, orbita.AtraqueL4, Mathf.Min(1f, Time.deltaTime * 4f));
                    yield return null;
                }
                foreach (Transform b in bodega) b.gameObject.SetActive(false);
            }
            ResumenLanzadera rl = resumen != null ? resumen.lanzadera : null;
            if (rl != null && rl.combustibleEnLanzadera)
            {
                titulo = $"COMBUSTIBLE DE LA L-4 → tanque de la EM-4X: {rl.combustibleEncargadoKg:0} kg";
                Transform linea = Caja("Conexión de combustible", orbita.Raiz, Vector3.zero, Vector3.one, Mat(new Color(1f, 0.6f, 0.2f), true));
                Vector3 a = l4.Piezas.ContainsKey(PiezaLanzadera.Tanque) ? l4.Piezas[PiezaLanzadera.Tanque].position : fin, z = orbita.TomaCombustible;
                linea.position = (a + z) / 2f;
                linea.rotation = Quaternion.LookRotation(z - a);
                linea.localScale = new Vector3(0.3f, 0.3f, (z - a).magnitude);
                yield return Esperar(1.1f);
            }
            titulo = "TRIPULACIÓN A BORDO DE LA EM-4X";
            yield return Esperar(1f);
        }
    }
}
