using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// DETALLE FUNCIONAL del interior (SOLO DIBUJO, piezas provisionales reemplazables). No cambia medidas, colisiones ni
    /// distribución: añade rótulos de los puestos y puntos de uso, señales de orientación, la pantalla de misión de la mesa,
    /// el armario de kits con su cantidad, balizas de reparación y luces de alarma en los módulos con brecha.
    /// Las piezas no tienen colisión. El modelador puede sustituirlas manteniendo los puntos de JuegoMVP.Puntos.
    /// </summary>
    public class DetalleInterior : MonoBehaviour
    {
        JuegoMVP juego;
        Font fuente;
        TextMesh pantallaMision, rotuloKits;
        readonly List<(string modulo, Renderer r, Light luz)> alarmas = new List<(string modulo, Renderer r, Light luz)>();
        readonly List<(string modulo, Transform t)> reparaciones = new List<(string modulo, Transform t)>();
        readonly List<Transform> kits = new List<Transform>();
        Material matAlarma, matAlarmaApagada;
        readonly List<MeshRenderer> rotulos = new List<MeshRenderer>();

        static Material Mat(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            Material m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        Transform Pieza(PrimitiveType tipo, string nombre, Vector3 pos, Vector3 escala, Material m, Transform padre)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            g.name = nombre;
            Collider col = g.GetComponent<Collider>();
            col.enabled = false;      // sin colisión: no cambia la circulación
            Destroy(col);
            g.transform.SetParent(padre, false);
            g.transform.position = pos;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = m;
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }

        TextMesh Rotulo(string texto, Vector3 pos, float tamano, Color color, Transform padre)
        {
            GameObject g = new GameObject("Rótulo");
            g.transform.SetParent(padre, false);
            g.transform.position = pos;
            TextMesh tm = g.AddComponent<TextMesh>();
            tm.font = fuente;
            tm.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            tm.text = texto;
            tm.fontSize = 48;
            tm.characterSize = tamano;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            g.AddComponent<RotuloHaciaCamara>();
            rotulos.Add(tm.GetComponent<MeshRenderer>());
            return tm;
        }

        public void Construir(JuegoMVP j, DefinicionNaveModular d, Font f)
        {
            juego = j;
            fuente = f;
            Transform raiz = new GameObject("Detalle interior (provisional, sin colisiones)").transform;
            matAlarma = Mat(new Color(1f, 0.15f, 0.1f));
            matAlarmaApagada = Mat(new Color(0.25f, 0.08f, 0.08f));
            Material matKit = Mat(new Color(1f, 0.55f, 0.1f));
            Material matReparar = Mat(new Color(1f, 0.25f, 0.2f));

            foreach (PuntoInteraccion p in j.Puntos)
            {
                Color c = p.tipo == TipoPunto.Pilotaje ? new Color(0.45f, 0.7f, 1f) : p.tipo == TipoPunto.Ciencia ? new Color(0.45f, 1f, 0.6f)
                        : p.tipo == TipoPunto.Comunicaciones ? new Color(0.8f, 0.6f, 1f) : p.tipo == TipoPunto.Ingenieria ? new Color(1f, 0.7f, 0.4f)
                        : p.tipo == TipoPunto.Kits ? new Color(1f, 0.6f, 0.15f) : p.tipo == TipoPunto.Mesa ? new Color(0.9f, 0.95f, 1f)
                        : new Color(1f, 0.35f, 0.3f);
                ModuloNave m = d.Modulo(p.modulo);
                Vector3 arriba = m != null ? m.Arriba : Vector3.up;
                if (p.tipo == TipoPunto.Reparacion)
                {
                    Transform t = new GameObject("Reparación · " + p.modulo).transform;
                    t.SetParent(raiz, false);
                    Pieza(PrimitiveType.Sphere, "Baliza de reparación", p.posicion, Vector3.one * 0.35f, matReparar, t);
                    Rotulo("REPARAR AQUÍ\n(mantén E con un kit)", p.posicion + arriba * 0.6f, 0.035f, c, t);
                    t.gameObject.SetActive(false);
                    reparaciones.Add((p.modulo, t));
                    continue;
                }
                string texto = p.tipo == TipoPunto.Kits ? "KITS DE REPARACIÓN" : p.nombre.ToUpper();
                TextMesh tm = Rotulo(texto + "\nE: usar", p.posicion + arriba * 1.1f, 0.04f, c, raiz);
                Pieza(PrimitiveType.Cylinder, "Marca de puesto", p.posicion - arriba * 0.9f, new Vector3(0.6f, 0.01f, 0.6f), Mat(c * 0.8f), raiz)
                    .rotation = Quaternion.FromToRotation(Vector3.up, arriba);
                if (p.tipo == TipoPunto.Kits)
                {
                    rotuloKits = tm;
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 pos = p.posicion + (m != null ? m.Derecha : Vector3.right) * (-0.75f + 0.3f * i) - arriba * 0.3f;
                        Transform k = Pieza(PrimitiveType.Cube, "Kit", pos, new Vector3(0.25f, 0.18f, 0.35f), matKit, raiz);
                        k.rotation = m != null ? m.Rotacion : Quaternion.identity;
                        kits.Add(k);
                    }
                }
                if (p.tipo == TipoPunto.Mesa)
                {
                    pantallaMision = Rotulo("", p.posicion + arriba * 1.9f, 0.03f, new Color(0.7f, 0.95f, 1f), raiz);
                    pantallaMision.anchor = TextAnchor.MiddleCenter;
                }
            }

            // Señales de orientación en el nodo central (hacia cada módulo)
            ModuloNave nc = d.Modulo("NC");
            if (nc != null)
            {
                Color s = new Color(0.95f, 0.95f, 0.6f);
                Rotulo("PROA → MANDO / PILOTAJE", nc.AMundo(new Vector3(0f, 1.6f, 9.3f)), 0.03f, s, raiz);
                Rotulo("POPA → INGENIERÍA → NODO DE POPA → SISTEMAS", nc.AMundo(new Vector3(0f, 1.6f, 0.7f)), 0.03f, s, raiz);
                Rotulo("← CIENCIA", nc.AMundo(new Vector3(-4.2f, 0.9f, 5f)), 0.03f, s, raiz);
                Rotulo("COMUNICACIONES →", nc.AMundo(new Vector3(4.2f, 0.9f, 5f)), 0.03f, s, raiz);
                Rotulo("↑ HÁBITAT", nc.AMundo(new Vector3(0.9f, 4.2f, 5f)), 0.03f, s, raiz);
                Rotulo("↓ BODEGA (kits)", nc.AMundo(new Vector3(0.9f, -4.2f, 5f)), 0.03f, s, raiz);
            }

            // Luces de alarma: una por módulo que puede sufrir brecha
            foreach (string id in new[] { "MA", "IN", "CI", "CO", "HA", "SI", "NC" })
            {
                ModuloNave m = d.Modulo(id);
                if (m == null) continue;
                Vector3 pos = m.AMundo(new Vector3(0f, m.diametroInterior * 0.45f, m.largo * 0.5f));
                Renderer r = Pieza(PrimitiveType.Sphere, "Luz de alarma", pos, Vector3.one * 0.25f, matAlarmaApagada, raiz).GetComponent<Renderer>();
                Light l = r.gameObject.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.15f, 0.1f);
                l.range = 9f;
                l.intensity = 0f;
                alarmas.Add((id, r, l));
            }
        }

        void Update()
        {
            EstadoExpedicion s = juego != null ? juego.Estado : null;
            bool mision = juego != null && juego.FaseActual == JuegoMVP.Fase.Mision && s != null;
            bool pulso = Mathf.Repeat(Time.time, 0.8f) < 0.4f;
            bool hayBrecha = mision && s.HayBrechaAbierta;
            foreach (var a in alarmas)
            {
                bool activa = mision && (s.DanoDe(a.modulo) != null || (a.modulo == "NC" && hayBrecha));
                a.r.sharedMaterial = activa && pulso ? matAlarma : matAlarmaApagada;
                a.luz.intensity = activa && pulso ? 2.5f : 0f;
            }
            foreach (var rp in reparaciones)
                rp.t.gameObject.SetActive(mision && s.DanoDe(rp.modulo) != null);

            // Los rótulos se dibujan por encima de las paredes: solo se muestran los cercanos a la cámara activa.
            Camera cam = Camera.main;
            if (cam != null)
                foreach (MeshRenderer mr in rotulos)
                    if (mr != null) mr.enabled = juego.ModoActual != JuegoMVP.Modo.Pilotaje && (mr.transform.position - cam.transform.position).sqrMagnitude < 81f;

            int quedan = mision ? s.KitsBodega : 0;
            for (int i = 0; i < kits.Count; i++) kits[i].gameObject.SetActive(i < quedan);
            if (rotuloKits != null) rotuloKits.text = $"KITS DE REPARACIÓN: {quedan}\nE: recoger uno";

            if (pantallaMision != null)
            {
                if (!mision) pantallaMision.text = "PANTALLA DE MISIÓN\n(sin misión en curso)";
                else
                {
                    string brechas = "";
                    foreach (DanoModulo dm in s.Danos) if (!dm.reparado) brechas += $"\n<color=#ff5040>BRECHA: {dm.nombreModulo}{(dm.aislado ? " (aislado)" : "")}</color>";
                    pantallaMision.richText = true;
                    pantallaMision.text = $"{s.Mision.nombre.ToUpper()}\nPaso {Mathf.Min(s.Paso + 1, s.Mision.pasos.Count)} de {s.Mision.pasos.Count}:\n" +
                                          Partir(s.PasoTexto, 46) + brechas;
                }
            }
        }

        static string Partir(string t, int ancho)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int col = 0;
            foreach (string palabra in t.Split(' '))
            {
                if (col + palabra.Length > ancho && col > 0) { sb.Append('\n'); col = 0; }
                else if (col > 0) { sb.Append(' '); col++; }
                sb.Append(palabra);
                col += palabra.Length;
            }
            return sb.ToString();
        }
    }
}
