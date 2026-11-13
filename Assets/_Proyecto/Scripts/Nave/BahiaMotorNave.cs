using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Bahía de motor de la sala de Ingeniería (solo visual). Al empezar la nave construye el tanque y el motor
    /// ELEGIDOS en Configuracion, con su tamaño y color, detrás de la barandilla. Lee los datos de ControlNave;
    /// no cambia nada de la misión. Formas simples provisionales, a media escala.
    /// </summary>
    public class BahiaMotorNave : MonoBehaviour
    {
        [Tooltip("Escala de la maqueta respecto a las medidas de la vista exterior.")]
        [SerializeField] float escala = 0.45f;

        ControlNave control;
        bool construida;

        void Update()
        {
            if (construida) return;
            if (control == null) control = FindFirstObjectByType<ControlNave>();
            if (control == null || control.Nave == null) return;   // espera a que la nave calcule sus datos
            construida = true;
            Construir(control.Nave);
        }

        void Construir(ResumenNave n)
        {
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Tanque: cilindro tumbado a lo largo de x, a la izquierda.
            OpcionTanqueNave t = n.tanque;
            float largo = (t != null ? t.largo : 3.2f) * escala;
            float diametro = (t != null ? t.diametro : 2.0f) * escala;
            Material matTanque = Mat(t != null ? t.color : new Color(0.5f, 0.55f, 0.62f), 0.5f);
            Material naranja = Mat(new Color(1f, 0.55f, 0.15f), 0.2f, new Color(1f, 0.45f, 0.1f) * 0.6f);
            Transform tanque = Pieza(PrimitiveType.Cylinder, new Vector3(-1.6f, 0.3f + diametro / 2f, 0f),
                                     new Vector3(diametro, largo / 2f, diametro), matTanque).transform;
            tanque.localRotation = Quaternion.Euler(0f, 0f, 90f);
            int bandas = Mathf.Max(1, Mathf.RoundToInt(largo / 0.8f));
            for (int i = 0; i < bandas; i++)
            {
                float x = -1.6f - largo / 2f + largo * (i + 1) / (bandas + 1);
                Transform b = Pieza(PrimitiveType.Cylinder, new Vector3(x, 0.3f + diametro / 2f, 0f),
                                    new Vector3(diametro + 0.04f, 0.03f, diametro + 0.04f), naranja).transform;
                b.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            Pieza(PrimitiveType.Cube, new Vector3(-1.6f, 0.15f, 0f), new Vector3(largo, 0.3f, diametro * 0.8f), Mat(new Color(0.18f, 0.2f, 0.24f), 0.7f));

            // Motor: tobera en campana apuntando hacia la popa (−z), a la derecha.
            OpcionMotorNave m = n.motor;
            float tob = (m != null ? Mathf.Max(0.5f, m.tamanoTobera) : 1f) * escala;
            Material matMotor = Mat(m != null ? m.color : new Color(0.35f, 0.37f, 0.4f), 0.8f);
            Vector3 baseMotor = new Vector3(1.8f, 1.1f, 0.2f);
            Pieza(PrimitiveType.Cube, baseMotor + new Vector3(0f, -0.55f, 0f), new Vector3(1.2f, 1.1f, 0.6f), Mat(new Color(0.18f, 0.2f, 0.24f), 0.7f));
            for (int i = 0; i < 4; i++)
            {
                float r = (0.6f + 0.25f * i) * tob;
                Transform anillo = Pieza(PrimitiveType.Cylinder, baseMotor + new Vector3(0f, 0.15f, -0.2f - 0.25f * i * tob),
                                         new Vector3(r, 0.13f * tob, r), i == 0 ? naranja : matMotor).transform;
                anillo.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            int aux = m != null ? Mathf.Clamp(m.toberasAuxiliares, 0, 8) : 0;
            for (int i = 0; i < aux; i++)
            {
                float a = i * Mathf.PI * 2f / aux;
                Transform p = Pieza(PrimitiveType.Cylinder, baseMotor + new Vector3(Mathf.Cos(a) * 0.55f, 0.15f + Mathf.Sin(a) * 0.55f, -0.3f),
                                    new Vector3(0.18f, 0.15f, 0.18f), matMotor).transform;
                p.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // Rótulos en la pared de popa, legibles desde la sala.
            Rotulo(fuente, new Vector3(-1.6f, 2.35f, -0.35f),
                   $"TANQUE: {(t != null ? t.nombre : "por defecto")}\n{n.combustibleInicialKg:0} de {n.capacidadTanqueKg:0} kg");
            Rotulo(fuente, new Vector3(1.8f, 2.35f, -0.35f),
                   $"MOTOR: {(m != null ? m.nombre : "por defecto")}\n{n.empujeN / 1000f:0} kN · {n.consumoKgS:0.0} kg/s");
        }

        void Rotulo(Font fuente, Vector3 pos, string texto)
        {
            if (fuente == null) return;
            GameObject go = new GameObject("Rótulo");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // se lee mirando hacia la popa (−z)
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = fuente;
            tm.fontSize = 80;
            tm.characterSize = 0.009f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.75f, 0.4f);
            tm.text = texto;
            go.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        }

        GameObject Pieza(PrimitiveType tipo, Vector3 pos, Vector3 escalaPieza, Material m)
        {
            GameObject go = GameObject.CreatePrimitive(tipo);
            Destroy(go.GetComponent<Collider>());     // detrás de la barandilla sólida: no hace falta colisionador
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escalaPieza;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        static Material Mat(Color c, float metalico, Color? emision = null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material mat = new Material(sh);
            mat.SetColor("_BaseColor", c);
            mat.color = c;
            mat.SetFloat("_Metallic", metalico);
            mat.SetFloat("_Smoothness", 0.5f);
            if (emision.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emision.Value);
            }
            return mat;
        }
    }
}
