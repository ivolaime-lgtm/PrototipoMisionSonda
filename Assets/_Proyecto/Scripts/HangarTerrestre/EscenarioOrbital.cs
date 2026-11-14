using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// ESCENARIO ORBITAL PROVISIONAL para las secuencias (solo visual): la EM-4X a escala real, con la distribución y las
    /// medidas de DefinicionNaveModular (no se cambian), y una PLATAFORMA DE SERVICIO bajo la etapa de propulsión con:
    /// soporte de recepción, soporte para piezas retiradas y un brazo guiado. Aquí llega el VC-1, se descarga y se instala
    /// lo encargado; después atraca la L-4 bajo el módulo de Mando.
    ///
    /// Coordenadas de la nave (las de NaveModular): proa hacia +Z; etapa de propulsión de z −22 a −37; Ciencia hacia −X;
    /// Comunicaciones hacia +X. Puntos de montaje (cara de montaje de cada pieza, su largo sale hacia fuera):
    /// motor (0; 0; −37) hacia −Z · tanque (0; 0; −23) hacia −Z, dentro de la etapa · instrumento (−15,2; 0; 1,5) hacia −X
    /// · relevo (13,2; 0; 1,5) hacia +X. Son representativos (simplificación), no un plano de ingeniería.
    /// </summary>
    public class EscenarioOrbital
    {
        public Transform Raiz { get; private set; }
        public Transform Nave { get; private set; }
        public Transform Recepcion { get; private set; }
        public Transform Brazo { get; private set; }
        /// <summary>Donde cuelgan los componentes instalados (misma posición que «Nave»).</summary>
        public Transform Equipos => equipos;
        public readonly List<Transform> HuecosRetiradas = new List<Transform>();
        public readonly Dictionary<TipoComponente, Transform> Instalado = new Dictionary<TipoComponente, Transform>();
        /// <summary>Donde se detiene el VC-1 (su popa), con la bodega hacia +Y, bajo la plataforma.</summary>
        public Vector3 LlegadaVC1 { get; private set; }
        /// <summary>Punto de atraque de la L-4 bajo el módulo de Mando (la cápsula sube hacia +Y).</summary>
        public Vector3 AtraqueL4 { get; private set; }
        /// <summary>Puerto de suministro (nodo de popa) donde entra la carga pequeña.</summary>
        public Vector3 PuertoSuministro { get; private set; }
        /// <summary>Conexión de combustible en la etapa de propulsión.</summary>
        public Vector3 TomaCombustible { get; private set; }

        Func<Color, bool, Material> mat;
        CatalogoMision cat;
        Transform brazoBase, equipos;

        public static Vector3 Montaje(TipoComponente t) =>
            t == TipoComponente.Motor ? new Vector3(0f, 0f, -37f) : t == TipoComponente.Tanque ? new Vector3(0f, 0f, -23f)
            : t == TipoComponente.Instrumento ? new Vector3(-15.2f, 0f, 1.5f) : new Vector3(13.2f, 0f, 1.5f);

        public static Quaternion Giro(TipoComponente t) =>
            t == TipoComponente.Motor || t == TipoComponente.Tanque ? Quaternion.Euler(0f, 180f, 0f)
            : t == TipoComponente.Instrumento ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f);

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
            Raiz = new GameObject("Escenario orbital (provisional)").transform;
            Raiz.position = lugar;
            Nave = new GameObject("EM-4X en órbita (bloques a escala real)").transform;
            Nave.SetParent(Raiz, false);
            SustitucionEscenario.Punto(Nave, "Orbita/EM4X");
            Material casco = mat(new Color(0.82f, 0.83f, 0.85f), false), estructura = mat(new Color(0.4f, 0.42f, 0.46f), false),
                     amarillo = mat(new Color(0.95f, 0.78f, 0.15f), false), oscuro = mat(new Color(0.18f, 0.19f, 0.22f), false);
            DefinicionNaveModular d = DefinicionNaveModular.ExpedicionEM4X();
            foreach (ModuloNave m in d.modulos)
            {
                if (m.id == "LZ") continue;          // la L-4 se representa aparte (llega después y atraca)
                Bounds b = m.Limites;
                if (m.id == "PR")
                {
                    // Etapa de propulsión como armazón abierto: se ve el tanque dentro y el motor detrás
                    Vector3 ex = b.extents;
                    for (int i = 0; i < 4; i++)
                        Caja("Larguero de la etapa", Nave, b.center + new Vector3((i % 2 == 0 ? -1 : 1) * ex.x, (i < 2 ? -1 : 1) * ex.y, 0f), new Vector3(0.35f, 0.35f, b.size.z), estructura);
                    for (float z = b.min.z; z <= b.max.z + 0.01f; z += 5f)
                        Caja("Cuaderna de la etapa", Nave, new Vector3(b.center.x, b.center.y, z), new Vector3(b.size.x, b.size.y, 0.3f), estructura);
                    continue;
                }
                Caja("Módulo " + m.id + " · " + m.nombre, Nave, b.center, b.size, casco);
            }
            Material m0 = estructura;
            // Los componentes cuelgan de un hermano de «Nave» con su misma posición: si un modelo sustituye a la EM-4X, no los oculta
            equipos = new GameObject("Componentes instalados").transform;
            equipos.SetParent(Raiz, false);

            // Plataforma de servicio bajo la etapa de propulsión
            Transform plat = SustitucionEscenario.Punto(new GameObject("Plataforma de servicio orbital").transform, "Orbita/PlataformaServicio");
            plat.SetParent(Raiz, false);
            Caja("Viga de la plataforma", plat, new Vector3(0f, -12f, -24f), new Vector3(3f, 1.4f, 44f), m0);
            Caja("Brazo de unión a la EM-4X", plat, new Vector3(0f, -7.5f, -26f), new Vector3(1f, 8f, 1f), m0);
            Caja("Brazo de unión a la EM-4X", plat, new Vector3(0f, -7.5f, -6f), new Vector3(1f, 8f, 1f), m0);
            Recepcion = SustitucionEscenario.Punto(new GameObject("Soporte de recepción").transform, "Orbita/SoporteRecepcion");
            Recepcion.SetParent(plat, false);
            Recepcion.localPosition = new Vector3(7f, -10.5f, -30f);
            Caja("Bandeja de recepción", Recepcion, new Vector3(0f, -0.6f, 0f), new Vector3(6f, 0.3f, 6f), amarillo);
            Caja("Unión de la bandeja", plat, new Vector3(3.5f, -11.5f, -30f), new Vector3(5f, 0.6f, 0.6f), m0);
            Transform ret = SustitucionEscenario.Punto(new GameObject("Soporte de piezas retiradas").transform, "Orbita/SoporteRetiradas");
            ret.SetParent(plat, false);
            ret.localPosition = new Vector3(-8f, -10.5f, -14f);
            Caja("Bastidor de piezas retiradas", ret, new Vector3(0f, -0.6f, 0f), new Vector3(6f, 0.3f, 16f), oscuro);
            Caja("Unión del bastidor", plat, new Vector3(-4.5f, -11.5f, -14f), new Vector3(6f, 0.6f, 0.6f), m0);
            for (int i = 0; i < 4; i++)
            {
                Transform h = new GameObject("Hueco de pieza retirada " + (i + 1)).transform;
                h.SetParent(ret, false);
                h.localPosition = new Vector3(0f, 2.8f, -6.5f + i * 3.4f);
                HuecosRetiradas.Add(h);
            }
            brazoBase = Caja("Base del brazo guiado", plat, new Vector3(0f, -11f, -20f), new Vector3(1.6f, 1.2f, 1.6f), amarillo);
            Brazo = Caja("Brazo guiado (visual)", plat, Vector3.zero, new Vector3(0.5f, 0.5f, 1f), amarillo);
            Brazo.gameObject.SetActive(false);

            LlegadaVC1 = Raiz.TransformPoint(new Vector3(0f, -21f, -46f));
            AtraqueL4 = Raiz.TransformPoint(new Vector3(0f, -3.2f, 11f));
            ModuloNave n2 = d.Modulo("N2");
            PuertoSuministro = Raiz.TransformPoint(n2 != null ? new Vector3(n2.Limites.center.x, n2.Limites.min.y, n2.Limites.center.z) : new Vector3(0f, -4f, -15f));
            TomaCombustible = Raiz.TransformPoint(new Vector3(0f, -3f, -29f));

            foreach (TipoComponente t in new[] { TipoComponente.Motor, TipoComponente.Tanque, TipoComponente.Instrumento, TipoComponente.Relevo })
            {
                int i;
                if (!indices.TryGetValue(t, out i)) i = 0;
                Poner(t, i, r);
            }
            SustitucionEscenario.Aplicar(Raiz);
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

        /// <summary>Tiende el brazo guiado desde su base hasta «punto» (o lo oculta con null).</summary>
        public void Apuntar(Vector3? punto)
        {
            if (!punto.HasValue) { Brazo.gameObject.SetActive(false); return; }
            Vector3 a = brazoBase.position, b = punto.Value;
            Brazo.gameObject.SetActive(true);
            Brazo.position = (a + b) / 2f;
            Brazo.rotation = Quaternion.LookRotation((b - a).sqrMagnitude > 0.01f ? (b - a) : Vector3.forward);
            Brazo.localScale = new Vector3(0.5f, 0.5f, Mathf.Max(0.1f, (b - a).magnitude));
        }

        public void Destruir() { if (Raiz != null) UnityEngine.Object.Destroy(Raiz.gameObject); }
    }
}
