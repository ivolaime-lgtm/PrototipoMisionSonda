using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// ZONA C2 · CARGA DEL TRANSPORTE ORBITAL NO TRIPULADO (VC-1), en una nave anexa detrás del hangar (paso en la pared del
    /// fondo, a la izquierda del embarque). Solo representación y la interacción de «preparar y cargar»:
    ///   · Encargo confirmado → la pieza aparece a escala real en su SOPORTE DE PREPARACIÓN, con su destino.
    ///   · Preparación → E en el soporte (comprobación sencilla).
    ///   · Carga → el puente grúa la lleva a su hueco de la bodega del VC-1 (animación guiada, sin física).
    /// El estado sale de CargaHangar (un lote por componente): no hay otra preparación. Cambiar o cancelar un componente
    /// mientras la preparación está abierta retira su representación y solo invalida su lote. Nada de esto se cobra.
    /// El despacho (lanzamiento) solo ocurre al embarcar, con la preparación cerrada y validada (SecuenciaEnvioOrbital).
    /// </summary>
    public class ZonaTransporteOrbital : MonoBehaviour
    {
        // Nave anexa (planta x, z) y posición del VC-1 tumbado (eje a lo largo de +Z)
        public const float X0 = -32f, X1 = -2.5f, Z0 = 64.5f, Z1 = 104f, Alto = 18f;
        public static readonly Vector3 PosicionVC1 = new Vector3(-26f, 4.6f, 70f);
        const float XSoporte = -15f, AlturaCuna = 1f, AlturaIzado = 12.5f, AlturaPuente = 16f;
        static readonly Dictionary<TipoComponente, float> ZSoporte = new Dictionary<TipoComponente, float>
        {
            { TipoComponente.Tanque, 80f }, { TipoComponente.Motor, 90.5f }, { TipoComponente.Instrumento, 95.5f }, { TipoComponente.Relevo, 99f }
        };
        public static readonly TipoComponente[] Tipos = { TipoComponente.Motor, TipoComponente.Tanque, TipoComponente.Instrumento, TipoComponente.Relevo };

        public bool Animando { get; private set; }
        public ModeloTransporte VC1 { get; private set; }

        HangarTerrestre h;
        Transform raiz, puente, carro, cable;
        Func<Color, bool, Material> mat;
        readonly Dictionary<TipoComponente, Transform> enSoporte = new Dictionary<TipoComponente, Transform>();
        readonly Dictionary<TipoComponente, TextMesh> rotulos = new Dictionary<TipoComponente, TextMesh>();
        readonly Dictionary<string, Transform> huecosContenedor = new Dictionary<string, Transform>();
        readonly List<Transform> modulosCombustible = new List<Transform>();
        TextMesh rotuloVC1;

        public static float ZDeSoporte(TipoComponente t) => ZSoporte[t];

        public void Construir(HangarTerrestre hangar, Transform padre, Func<Color, bool, Material> fabrica, Material suelo, Material techo,
                              Func<string, Transform, Vector3, float, Color, TextMesh> rotulo)
        {
            h = hangar;
            mat = fabrica;
            raiz = new GameObject("Zona C2 · carga del transporte orbital no tripulado (provisional)").transform;
            raiz.SetParent(padre, false);
            Material pared = fabrica(new Color(0.5f, 0.52f, 0.55f), false), viga = fabrica(new Color(0.3f, 0.32f, 0.35f), false),
                     amarillo = fabrica(new Color(0.95f, 0.75f, 0.15f), false), cuna = fabrica(new Color(0.35f, 0.38f, 0.42f), false);
            float cx = (X0 + X1) / 2f, cz = (Z0 + Z1) / 2f;

            // Envolvente (con colisiones: limita al jugador y al carro)
            Bloque("Anexo · suelo", raiz, new Vector3(cx, -0.25f, cz), new Vector3(X1 - X0, 0.5f, Z1 - Z0), suelo, true);
            Bloque("Anexo · umbral", raiz, new Vector3(-6f, -0.25f, 64.25f), new Vector3(6f, 0.5f, 0.6f), suelo, true);
            Bloque("Anexo · techo", raiz, new Vector3(cx, Alto + 0.25f, cz), new Vector3(X1 - X0 + 1f, 0.5f, Z1 - Z0 + 1f), techo, true);
            Bloque("Anexo · pared oeste", raiz, new Vector3(X0 - 0.25f, Alto / 2f, cz), new Vector3(0.5f, Alto, Z1 - Z0), pared, true);
            Bloque("Anexo · pared este", raiz, new Vector3(X1 + 0.25f, Alto / 2f, cz), new Vector3(0.5f, Alto, Z1 - Z0), pared, true);
            Bloque("Anexo · pared del fondo", raiz, new Vector3(cx, Alto / 2f, Z1 + 0.25f), new Vector3(X1 - X0 + 1f, Alto, 0.5f), pared, true);
            // Pared sur: la parte que no cubre el hangar (x < −20) y lo que queda por encima de él (12–18 m)
            Bloque("Anexo · pared sur (fuera del hangar)", raiz, new Vector3((X0 - 20f) / 2f, Alto / 2f, Z0 - 0.25f), new Vector3(-20f - X0, Alto, 0.5f), pared, true);
            Bloque("Anexo · pared sur (sobre el hangar)", raiz, new Vector3((-20f + X1) / 2f, 12f + (Alto - 12f) / 2f, Z0 - 0.25f), new Vector3(X1 + 20f, Alto - 12f, 0.5f), pared, true);
            for (float z = Z0 + 3f; z < Z1; z += 6f)
            {
                Bloque("Pilar", raiz, new Vector3(X0 + 0.4f, Alto / 2f, z), new Vector3(0.5f, Alto, 0.5f), viga, false);
                Bloque("Pilar", raiz, new Vector3(X1 - 0.4f, Alto / 2f, z), new Vector3(0.5f, Alto, 0.5f), viga, false);
            }
            Bloque("Riel del puente grúa (oeste)", raiz, new Vector3(X0 + 1f, AlturaPuente + 0.5f, cz), new Vector3(0.5f, 0.5f, Z1 - Z0 - 1f), amarillo, false);
            Bloque("Riel del puente grúa (este)", raiz, new Vector3(X1 - 1f, AlturaPuente + 0.5f, cz), new Vector3(0.5f, 0.5f, Z1 - Z0 - 1f), amarillo, false);
            puente = Bloque("Puente grúa", raiz, new Vector3(cx, AlturaPuente, 80f), new Vector3(X1 - X0 - 2f, 0.9f, 1f), amarillo, false);
            SustitucionEscenario.Punto(puente, "C2/PuenteGrua");
            carro = Bloque("Carro y gancho", raiz, new Vector3(XSoporte, AlturaPuente - 0.7f, 80f), new Vector3(1.4f, 0.6f, 1.4f), viga, false);
            cable = Bloque("Cable", raiz, Vector3.zero, new Vector3(0.1f, 1f, 0.1f), viga, false);
            cable.gameObject.SetActive(false);

            // Cunas del VC-1 (con colisión) y soportes de preparación
            for (int i = 0; i < 4; i++)
                Bloque("Cuna del VC-1", raiz, new Vector3(PosicionVC1.x, 0.5f, PosicionVC1.z + 3f + i * 8f), new Vector3(6f, 1f, 1.2f), cuna, true);
            foreach (TipoComponente t in Tipos)
            {
                float z = ZSoporte[t];
                float largo = t == TipoComponente.Tanque ? 13.5f : t == TipoComponente.Motor ? 5.5f : 2.6f;
                Transform s = Bloque("Soporte de preparación · " + t, raiz, new Vector3(XSoporte, AlturaCuna / 2f, z), new Vector3(3f, AlturaCuna, largo), cuna, true);
                SustitucionEscenario.Punto(s, "C2/Soporte");
                Bloque("Marca del soporte", raiz, new Vector3(XSoporte + 4f, 0.02f, z), new Vector3(1.4f, 0.04f, 1.4f), amarillo, false);
                rotulos[t] = rotulo("", raiz, new Vector3(XSoporte + 4.2f, 2.6f, z), 0.04f, Color.white);
                h.Puntos.Add(new PuntoHangar { tipo = EstacionHangar.Soporte, nombre = "Soporte de preparación · " + ModeloComponentes.NombreTipo(t).ToLower(),
                                               lote = CargaHangar.IdComponente(t), posicion = new Vector3(XSoporte + 4.5f, 0f, z), objetivo = new Vector3(XSoporte, 0f, z) });
            }
            rotuloVC1 = rotulo("", raiz, new Vector3(PosicionVC1.x, 13.8f, PosicionVC1.z + 15f), 0.07f, Color.white);
            rotulo("C2 · TRANSPORTE ORBITAL NO TRIPULADO (VC-1)\nsoportes de preparación · grúa · bodega del VC-1", raiz,
                   new Vector3(-6f, 7.5f, 66f), 0.07f, new Color(1f, 0.85f, 0.45f));

            // Huecos fijos del anillo para los contenedores pequeños que van en el VC-1 (no dependen de reconstruir el VC-1)
            string[] lotes = { "suministros", "kits", "sonda" };
            for (int i = 0; i < lotes.Length; i++)
            {
                Transform hc = new GameObject("Anillo del VC-1 · hueco " + lotes[i]).transform;
                hc.SetParent(raiz, false);
                hc.localPosition = PosicionVC1 + new Vector3(-2.4f + i * 2.4f, -ModeloTransporte.DiametroCasco / 2f + 0.3f, ModeloTransporte.ZAnillo + 3.2f);
                huecosContenedor[lotes[i]] = hc;
            }
        }

        static Transform Bloque(string n, Transform padre, Vector3 pos, Vector3 esc, Material m, bool colision)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            if (!colision) { Collider c = g.GetComponent<Collider>(); c.enabled = false; Destroy(c); }
            g.transform.SetParent(padre, false);
            g.transform.localPosition = pos;
            g.transform.localScale = esc;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        /// <summary>Hueco del anillo del VC-1 para el contenedor de un lote pequeño que va en el envío orbital.</summary>
        public Transform HuecoContenedor(string lote) { Transform t; return huecosContenedor.TryGetValue(lote, out t) ? t : null; }

        LoteCarga Lote(TipoComponente t) => h.Lotes.Find(x => x.id == CargaHangar.IdComponente(t));

        /// <summary>Vuelve a representar todo a partir del estado (sin animación). Retira lo que ya no corresponde.</summary>
        public void Actualizar()
        {
            if (raiz == null || h.Resumen == null) return;
            StopAllCoroutines();
            Animando = false;
            cable.gameObject.SetActive(false);
            foreach (Transform p in enSoporte.Values) if (p != null) Destroy(p.gameObject);
            enSoporte.Clear();
            modulosCombustible.Clear();
            if (VC1 != null) VC1.Destruir();

            ResumenTransporte tr = h.Resumen.transporte;
            VC1 = ModeloTransporte.Construir(raiz, PosicionVC1, Quaternion.identity, tr, h.Catalogo, mat);
            bool necesario = tr != null && tr.necesario;
            VC1.Puertas(necesario ? 1f : 0f);

            int cargados = 0, encargados = 0;
            foreach (TipoComponente t in Tipos)
            {
                LoteCarga lo = Lote(t);
                int indice = Indice(t);
                TextMesh rt = rotulos[t];
                if (lo == null)
                {
                    rt.text = ModeloComponentes.NombreTipo(t) + "\nsin encargo: se usa el equipo ya instalado en órbita";
                    rt.color = new Color(0.7f, 0.7f, 0.7f);
                    continue;
                }
                encargados++;
                Transform hueco;
                if (lo.preparado && VC1.Huecos.TryGetValue(t, out hueco))
                {
                    cargados++;
                    VC1.Poner(ModeloComponentes.Componente(t, indice, h.Resumen, h.Catalogo, raiz, mat), hueco);
                    rt.text = ModeloComponentes.NombreTipo(t) + " · " + lo.nombre + "\nCARGADO EN EL VC-1 · Transporte orbital no tripulado";
                    rt.color = new Color(0.5f, 1f, 0.55f);
                }
                else
                {
                    Transform p = ModeloComponentes.Componente(t, indice, h.Resumen, h.Catalogo, raiz, mat);
                    if (p != null)
                    {
                        MedidasTransporte m = ModeloComponentes.Medidas(t, indice, h.Catalogo);
                        p.localPosition = new Vector3(XSoporte, AlturaCuna + m.diametro / 2f, ZSoporte[t] - m.largo / 2f);
                        enSoporte[t] = p;
                    }
                    rt.text = ModeloComponentes.NombreTipo(t) + " · ENCARGO CONFIRMADO\n" + lo.nombre + "\nTransporte orbital no tripulado\nE: preparar y cargar en el VC-1";
                    rt.color = new Color(1f, 0.82f, 0.4f);
                }
            }

            // Combustible que va en el VC-1 (uno por tramo) cuando su llenado está autorizado
            LoteCarga comb = h.Lotes.Find(x => x.id == "combustible");
            if (comb != null && !comb.enLanzadera && comb.preparado)
                for (int i = 0; i < Mathf.Min(comb.cantidad, VC1.HuecosCombustible.Count); i++)
                {
                    Transform mc = ModeloComponentes.ModuloCombustible(raiz, mat);
                    VC1.Poner(mc, VC1.HuecosCombustible[i]);
                    modulosCombustible.Add(mc);
                }
            SustitucionEscenario.Aplicar(raiz);

            if (!necesario)
            {
                rotuloVC1.text = "VC-1 · TRANSPORTE ORBITAL NO TRIPULADO\nNo hace falta en esta expedición: no se prepara ni despega";
                rotuloVC1.color = new Color(0.75f, 0.75f, 0.75f);
            }
            else if (!tr.Cabe)
            {
                rotuloVC1.text = "VC-1 · NO SE PUEDE DESPACHAR\n" + tr.motivos[0];
                rotuloVC1.color = new Color(1f, 0.5f, 0.4f);
            }
            else
            {
                rotuloVC1.text = $"VC-1 · TRANSPORTE ORBITAL NO TRIPULADO\nmasa {tr.masaTotalKg:0} de {DatosTransporteOrbital.MasaMaximaKg:0} kg · bodega {tr.largoUsadoM:0.0} de {DatosTransporteOrbital.LargoBodegaM:0} m\n" +
                                 (cargados == encargados ? "componentes cargados · sale al embarcar con la preparación cerrada" : $"componentes cargados {cargados} de {encargados}");
                rotuloVC1.color = new Color(1f, 0.9f, 0.55f);
            }
        }

        int Indice(TipoComponente t) =>
            t == TipoComponente.Motor ? h.Borrador.motor : t == TipoComponente.Tanque ? h.Borrador.tanque : t == TipoComponente.Instrumento ? h.Borrador.instrumento : h.Borrador.relevo;

        /// <summary>Prepara la pieza del soporte y la grúa la carga en su hueco del VC-1. Al terminar avisa al hangar.</summary>
        public bool Preparar(TipoComponente t, Action<TipoComponente> alCargar)
        {
            Transform pieza, hueco;
            if (Animando || !enSoporte.TryGetValue(t, out pieza) || pieza == null || VC1 == null || !VC1.Huecos.TryGetValue(t, out hueco)) return false;
            StartCoroutine(Carga(t, pieza, hueco, alCargar));
            return true;
        }

        IEnumerator Carga(TipoComponente t, Transform pieza, Transform hueco, Action<TipoComponente> alCargar)
        {
            Animando = true;
            Vector3 p0 = pieza.position, destino = hueco.position;
            Vector3 arriba0 = new Vector3(p0.x, AlturaIzado, p0.z), arriba1 = new Vector3(destino.x, AlturaIzado, destino.z);
            yield return Mover(null, carro.position, new Vector3(p0.x, carro.position.y, p0.z), 0.6f);          // la grúa va a la pieza
            yield return Mover(pieza, p0, arriba0, 1.1f);                                                       // izado
            yield return Mover(pieza, arriba0, arriba1, 1.6f);                                                  // traslado
            yield return Mover(pieza, arriba1, destino, 1.1f);                                                  // descenso al hueco
            cable.gameObject.SetActive(false);
            Animando = false;
            alCargar?.Invoke(t);     // el hangar marca el lote y vuelve a representar (la pieza queda en la bodega)
        }

        IEnumerator Mover(Transform pieza, Vector3 a, Vector3 b, float s)
        {
            for (float u = 0f; u < 1f; u += Time.deltaTime / s)
            {
                Vector3 p = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, u));
                if (pieza != null) pieza.position = p;
                Grua(pieza != null ? p : new Vector3(p.x, 0f, p.z), pieza);
                yield return null;
            }
            if (pieza != null) pieza.position = b;
        }

        void Grua(Vector3 sobre, Transform pieza)
        {
            puente.position = new Vector3(puente.position.x, puente.position.y, sobre.z + (pieza != null ? 0f : 0f));
            carro.position = new Vector3(sobre.x, AlturaPuente - 0.7f, sobre.z);
            if (pieza == null) { cable.gameObject.SetActive(false); return; }
            float arriba = AlturaPuente - 1f, abajo = sobre.y + 0.5f;
            Bounds b = new Bounds(pieza.position, Vector3.zero);
            foreach (Renderer r in pieza.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            abajo = b.max.y;
            cable.gameObject.SetActive(arriba > abajo);
            cable.position = new Vector3(b.center.x, (arriba + abajo) / 2f, b.center.z);
            cable.localScale = new Vector3(0.1f, Mathf.Max(0.01f, arriba - abajo), 0.1f);
            carro.position = new Vector3(b.center.x, AlturaPuente - 0.7f, b.center.z);
            puente.position = new Vector3(puente.position.x, puente.position.y, b.center.z);
        }
    }
}
