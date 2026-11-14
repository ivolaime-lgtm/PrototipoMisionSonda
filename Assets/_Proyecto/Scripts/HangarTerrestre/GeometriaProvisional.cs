using System;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Ayudas de geometría provisional compartidas por el hangar y las secuencias: cubierta abovedada de perfil ovalado
    /// (semielipse) hecha con tramos rectos, arcos de estructura y hastiales que cierran los extremos.
    /// </summary>
    public static class GeometriaProvisional
    {
        /// <summary>Altura de la bóveda en la coordenada «b» del perfil (semielipse entre b0 y b1, desde «alero»).</summary>
        public static float Altura(float b, float b0, float b1, float alero, float flecha)
        {
            float c = (b0 + b1) / 2f, m = (b1 - b0) / 2f;
            float u = Mathf.Clamp((b - c) / m, -1f, 1f);
            return alero + flecha * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
        }

        static Transform Caja(string n, Transform p, Vector3 c, Vector3 t, Quaternion r, Material m, bool colision)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = n;
            if (!colision) { Collider col = g.GetComponent<Collider>(); col.enabled = false; UnityEngine.Object.Destroy(col); }
            g.transform.SetParent(p, false);
            g.transform.localPosition = c;
            g.transform.localRotation = r;
            g.transform.localScale = t;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g.transform;
        }

        /// <summary>
        /// Cubierta abovedada. «ejeX» = la bóveda corre a lo largo de X (de a0 a a1) y su perfil se dibuja en Z (de b0 a b1);
        /// si no, corre a lo largo de Z y el perfil va en X. «grosor» y «ancho» permiten usarla también para los arcos.
        /// </summary>
        public static Transform Boveda(string nombre, Transform padre, bool ejeX, float a0, float a1, float b0, float b1,
                                       float alero, float flecha, float grosor, Material m, bool colision, int tramos = 20)
        {
            Transform raiz = new GameObject(nombre).transform;
            raiz.SetParent(padre, false);
            float largoEje = a1 - a0, cEje = (a0 + a1) / 2f;
            for (int k = 0; k < tramos; k++)
            {
                float bA = b0 + (b1 - b0) * k / tramos, bB = b0 + (b1 - b0) * (k + 1) / tramos;
                float yA = Altura(bA, b0, b1, alero, flecha), yB = Altura(bB, b0, b1, alero, flecha);
                float db = bB - bA, dy = yB - yA, l = Mathf.Sqrt(db * db + dy * dy) + 0.06f;
                float ang = Mathf.Atan2(dy, db) * Mathf.Rad2Deg;
                if (ejeX)
                    Caja(nombre + " · tramo", raiz, new Vector3(cEje, (yA + yB) / 2f, (bA + bB) / 2f), new Vector3(largoEje, grosor, l),
                         Quaternion.Euler(-ang, 0f, 0f), m, colision);
                else
                    Caja(nombre + " · tramo", raiz, new Vector3((bA + bB) / 2f, (yA + yB) / 2f, cEje), new Vector3(l, grosor, largoEje),
                         Quaternion.Euler(0f, 0f, ang), m, colision);
            }
            return raiz;
        }

        /// <summary>
        /// Hastial: cierra el extremo de la bóveda en el plano «plano» (X si ejeX, Z si no), con franjas verticales desde
        /// «base(b)» hasta la bóveda. «base» permite dejar huecos (puertas) devolviendo una altura mayor.
        /// </summary>
        public static Transform Hastial(string nombre, Transform padre, bool ejeX, float plano, float b0, float b1, float alero,
                                        float flecha, Func<float, float> baseEn, Material m, bool colision, int franjas = 18)
        {
            Transform raiz = new GameObject(nombre).transform;
            raiz.SetParent(padre, false);
            float paso = (b1 - b0) / franjas;
            for (int k = 0; k < franjas; k++)
            {
                float bc = b0 + paso * (k + 0.5f);
                float top = Mathf.Max(Altura(b0 + paso * k, b0, b1, alero, flecha), Altura(b0 + paso * (k + 1), b0, b1, alero, flecha));
                float bot = baseEn(bc);
                if (top - bot < 0.05f) continue;
                Vector3 c = ejeX ? new Vector3(plano, (top + bot) / 2f, bc) : new Vector3(bc, (top + bot) / 2f, plano);
                Vector3 t = ejeX ? new Vector3(0.5f, top - bot, paso + 0.02f) : new Vector3(paso + 0.02f, top - bot, 0.5f);
                Caja(nombre + " · franja", raiz, c, t, Quaternion.identity, m, colision);
            }
            return raiz;
        }
    }
}
