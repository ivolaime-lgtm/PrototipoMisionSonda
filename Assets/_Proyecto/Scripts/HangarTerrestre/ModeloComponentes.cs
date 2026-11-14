using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// REPRESENTACIÓN PROVISIONAL A ESCALA REAL de los componentes de la EM-4X y de la carga (solo visual, sin colisiones).
    /// Medidas de DatosTransporteOrbital (las mismas que se comprueban contra la bodega del VC-1). Se usan en el soporte de
    /// preparación, en la bodega del VC-1, en el servicio orbital y, con «escala» < 1, en las maquetas de la estación del
    /// holograma (maqueta identificada; la pieza real nunca se reduce).
    ///
    /// Ejes de cada pieza: largo en +Z desde z = 0 (cara de montaje), centrada en X e Y. Cada raíz es un PuntoSustitucion
    /// («EM4X/Motor/1», «Carga/kits»…) para que un modelo pueda sustituirla.
    /// </summary>
    public static class ModeloComponentes
    {
        static Transform Raiz(string nombre, string id, Transform padre, float escala)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            t.localScale = Vector3.one * escala;
            SustitucionEscenario.Punto(t, id);
            return t;
        }

        static Transform Cil(string n, Transform p, float d, float z0, float z1, Material m, float x = 0f, float y = 0f)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = n;
            Collider c = g.GetComponent<Collider>(); c.enabled = false; UnityEngine.Object.Destroy(c);
            g.transform.SetParent(p, false);
            g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            g.transform.localPosition = new Vector3(x, y, (z0 + z1) / 2f);
            g.transform.localScale = new Vector3(d, Mathf.Max(0.01f, (z1 - z0) / 2f), d);
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        static Transform Caja(string n, Transform p, Vector3 centro, Vector3 tam, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            Collider c = g.GetComponent<Collider>(); c.enabled = false; UnityEngine.Object.Destroy(c);
            g.transform.SetParent(p, false);
            g.transform.localPosition = centro;
            g.transform.localScale = tam;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        static Transform Esfera(string n, Transform p, Vector3 centro, Vector3 tam, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = n;
            Collider c = g.GetComponent<Collider>(); c.enabled = false; UnityEngine.Object.Destroy(c);
            g.transform.SetParent(p, false);
            g.transform.localPosition = centro;
            g.transform.localScale = tam;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        public static Transform Motor(OpcionMotorNave o, int indice, Transform padre, Func<Color, bool, Material> mat, float escala = 1f)
        {
            MedidasTransporte md = DatosTransporteOrbital.Motor(o);
            Transform t = Raiz("Motor · " + o.nombre, "EM4X/Motor/" + indice, padre, escala);
            Material metal = mat(new Color(0.55f, 0.57f, 0.6f), false), oscuro = mat(new Color(0.2f, 0.2f, 0.22f), false),
                     cobre = mat(new Color(0.75f, 0.45f, 0.25f), false);
            float d = md.diametro, l = md.largo;
            Cil("Placa de montaje", t, d * 0.62f, 0f, 0.3f, metal);
            Cil("Cámara de combustión", t, d * 0.35f, 0.3f, l * 0.35f, oscuro);
            Cil("Garganta", t, d * 0.45f, l * 0.35f, l * 0.55f, cobre);
            Cil("Tobera (tramo medio)", t, d * 0.75f, l * 0.55f, l * 0.8f, cobre);
            Cil("Tobera (salida)", t, d, l * 0.8f, l, cobre);
            if (o.empujeN > 450000f)       // motor de alto empuje: toberas auxiliares DENTRO del diámetro de la principal
                for (int i = 0; i < 4; i++)
                {
                    float a = (45f + 90f * i) * Mathf.Deg2Rad;
                    Cil("Tobera auxiliar", t, 0.7f, 0.3f, l * 0.55f, oscuro, Mathf.Cos(a) * d * 0.3f, Mathf.Sin(a) * d * 0.3f);
                }
            return t;
        }

        public static Transform Tanque(OpcionTanqueNave o, int indice, Transform padre, Func<Color, bool, Material> mat, float escala = 1f)
        {
            MedidasTransporte md = DatosTransporteOrbital.Tanque(o);
            Transform t = Raiz("Tanque · " + o.nombre, "EM4X/Tanque/" + indice, padre, escala);
            Material blanco = mat(new Color(0.88f, 0.88f, 0.9f), false), naranja = mat(new Color(0.95f, 0.55f, 0.15f), false);
            float d = md.diametro, l = md.largo, cap = Mathf.Min(d * 0.25f, l * 0.2f);
            Cil("Cuerpo del tanque", t, d, cap, l - cap, blanco);
            Esfera("Fondo (proa)", t, new Vector3(0f, 0f, cap), new Vector3(d, d, cap * 2f), blanco);
            Esfera("Fondo (popa)", t, new Vector3(0f, 0f, l - cap), new Vector3(d, d, cap * 2f), blanco);
            Cil("Aro", t, d + 0.04f, l / 2f - 0.15f, l / 2f + 0.15f, naranja);
            return t;
        }

        public static Transform Instrumento(OpcionInstrumento o, int indice, Transform padre, Func<Color, bool, Material> mat, float escala = 1f)
        {
            Transform t = Raiz("Instrumento · " + o.nombre, "EM4X/Instrumento/" + indice, padre, escala);
            Material blanco = mat(new Color(0.9f, 0.9f, 0.92f), false), oscuro = mat(new Color(0.15f, 0.16f, 0.2f), false),
                     oro = mat(new Color(0.85f, 0.7f, 0.25f), false);
            if (o.nombre.StartsWith("Cámara"))
            {
                Cil("Cuerpo", t, 1.1f, 0f, 1.2f, blanco);
                Cil("Objetivo", t, 0.7f, 1.2f, 1.5f, oscuro);
            }
            else if (o.nombre.StartsWith("Espectr"))
            {
                Caja("Cuerpo", t, new Vector3(0f, 0f, 0.75f), new Vector3(1.2f, 1.2f, 1.5f), oro);
                Cil("Rendija", t, 0.4f, 1.5f, 1.55f, oscuro);
            }
            else
            {
                Esfera("Sensor", t, new Vector3(0f, 0f, 0.55f), Vector3.one * 1.1f, blanco);
                Cil("Mástil", t, 0.15f, 1.1f, 2.1f, oscuro);
            }
            return t;
        }

        public static Transform Relevo(OpcionOrbita o, int indice, Transform padre, Func<Color, bool, Material> mat, float escala = 1f)
        {
            Transform t = Raiz("Relevo · " + o.nombre, "EM4X/Relevo/" + indice, padre, escala);
            Material oro = mat(new Color(0.85f, 0.7f, 0.25f), false), azul = mat(new Color(0.15f, 0.25f, 0.55f), false);
            Caja("Cuerpo del relevo", t, new Vector3(0f, 0f, 0.6f), new Vector3(0.8f, 0.8f, 1.2f), oro);
            Caja("Panel solar plegado", t, new Vector3(0.48f, 0f, 0.6f), new Vector3(0.08f, 0.75f, 1.1f), azul);
            Caja("Panel solar plegado", t, new Vector3(-0.48f, 0f, 0.6f), new Vector3(0.08f, 0.75f, 1.1f), azul);
            return t;
        }

        /// <summary>Contenedor de un lote pequeño (suministros, kits, sonda). Base en y = 0.</summary>
        public static Transform Contenedor(string lote, Transform padre, Func<Color, bool, Material> mat)
        {
            Transform t = Raiz("Contenedor · " + lote, "Carga/" + lote, padre, 1f);
            Color c = lote == "kits" ? new Color(1f, 0.6f, 0.1f) : lote == "sonda" ? new Color(0.92f, 0.92f, 0.95f) : new Color(0.25f, 0.5f, 0.85f);
            Vector3 tam = lote == "sonda" ? new Vector3(0.8f, 0.9f, 1.2f) : new Vector3(0.75f, 0.7f, 1.1f);
            Caja("Caja", t, new Vector3(0f, tam.y / 2f, 0f), tam, mat(c, false));
            return t;
        }

        /// <summary>Módulo de combustible (un tramo de 3000 kg): Ø 2,4 × 1,4 m a lo largo de Z.</summary>
        public static Transform ModuloCombustible(Transform padre, Func<Color, bool, Material> mat)
        {
            Transform t = Raiz("Módulo de combustible (3000 kg)", "Carga/ModuloCombustible", padre, 1f);
            Cil("Depósito", t, 2.4f, 0f, 1.4f, mat(new Color(0.95f, 0.5f, 0.12f), false));
            return t;
        }

        /// <summary>Pieza del componente encargado de un tipo (null si el índice es 0 o no existe).</summary>
        public static Transform Componente(TipoComponente tipo, int indice, ResumenExpedicion r, CatalogoMision cat, Transform padre,
                                           Func<Color, bool, Material> mat, float escala = 1f)
        {
            switch (tipo)
            {
                case TipoComponente.Motor:
                {
                    List<OpcionMotorNave> l = BalanceEM4X.Motores(cat.catalogoNave);
                    return indice >= 0 && indice < l.Count ? Motor(l[indice], indice, padre, mat, escala) : null;
                }
                case TipoComponente.Tanque:
                {
                    List<OpcionTanqueNave> l = BalanceEM4X.Tanques(cat.catalogoNave);
                    return indice >= 0 && indice < l.Count ? Tanque(l[indice], indice, padre, mat, escala) : null;
                }
                case TipoComponente.Instrumento:
                {
                    List<OpcionInstrumento> l = BalanceEM4X.Instrumentos(cat);
                    return indice >= 0 && indice < l.Count ? Instrumento(l[indice], indice, padre, mat, escala) : null;
                }
                default:
                    return cat.orbitas != null && indice >= 0 && indice < cat.orbitas.Count ? Relevo(cat.orbitas[indice], indice, padre, mat, escala) : null;
            }
        }

        public static MedidasTransporte Medidas(TipoComponente tipo, int indice, CatalogoMision cat)
        {
            switch (tipo)
            {
                case TipoComponente.Motor: return DatosTransporteOrbital.Motor(BalanceEM4X.Motores(cat.catalogoNave)[indice]);
                case TipoComponente.Tanque: return DatosTransporteOrbital.Tanque(BalanceEM4X.Tanques(cat.catalogoNave)[indice]);
                case TipoComponente.Instrumento: return DatosTransporteOrbital.Instrumento(BalanceEM4X.Instrumentos(cat)[indice]);
                default: return DatosTransporteOrbital.Relevo(cat.orbitas[indice]);
            }
        }

        public static string NombreTipo(TipoComponente t) =>
            t == TipoComponente.Motor ? "MOTOR" : t == TipoComponente.Tanque ? "TANQUE" : t == TipoComponente.Instrumento ? "INSTRUMENTO" : "RELEVO";
    }
}
