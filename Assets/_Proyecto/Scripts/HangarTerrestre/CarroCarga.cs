using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// CARRO DE CARGA reutilizable (solo transporte, sin física de objetos). Se empuja delante del personaje; la carga
    /// va fijada a su bandeja. Si el sitio de delante está ocupado por una pared o un mueble, el carro no avanza
    /// (queda «bloqueado» y se avisa). Recuperar() lo devuelve a su base vacío. No sabe nada de presupuestos: quien lo usa
    /// decide qué significa cargar y entregar.
    /// </summary>
    public class CarroCarga : MonoBehaviour
    {
        public const float Largo = 1.4f, Ancho = 0.9f;
        const float Distancia = 1.45f;     // del personaje al centro del carro

        public bool Empujando { get; private set; }
        public bool Bloqueado { get; private set; }
        public string Lote { get; private set; }
        public string Firma { get; private set; }
        public Transform Bandeja { get; private set; }
        public Vector3 Base { get; private set; }
        public Vector2 LimiteMin, LimiteMax;    // zona por la que puede moverse (planta x, z)

        Transform portador;
        Collider ignorar;

        public void Construir(Vector3 basePos, Material chasis, Material asa)
        {
            name = "Carro de carga";
            Base = basePos;
            transform.position = basePos;
            Pieza("Plataforma", new Vector3(0f, 0.35f, 0f), new Vector3(Ancho, 0.1f, Largo), chasis);
            Pieza("Asa", new Vector3(0f, 0.85f, -Largo / 2f), new Vector3(Ancho, 0.06f, 0.06f), asa);
            Pieza("Barra izquierda", new Vector3(-Ancho / 2f, 0.6f, -Largo / 2f), new Vector3(0.05f, 0.5f, 0.05f), asa);
            Pieza("Barra derecha", new Vector3(Ancho / 2f, 0.6f, -Largo / 2f), new Vector3(0.05f, 0.5f, 0.05f), asa);
            for (int i = 0; i < 4; i++)
                Pieza("Rueda", new Vector3((i % 2 == 0 ? -1f : 1f) * (Ancho / 2f - 0.08f), 0.12f, (i < 2 ? -1f : 1f) * (Largo / 2f - 0.15f)),
                      new Vector3(0.12f, 0.24f, 0.24f), asa);
            Bandeja = new GameObject("Bandeja").transform;
            Bandeja.SetParent(transform, false);
            Bandeja.localPosition = new Vector3(0f, 0.4f, 0f);
        }

        void Pieza(string nombre, Vector3 pos, Vector3 escala, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nombre;
            Collider c = g.GetComponent<Collider>();
            c.enabled = false;          // sin colisiones: el carro no empuja ni bloquea al personaje
            Destroy(c);
            g.transform.SetParent(transform, false);
            g.transform.localPosition = pos;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = m;
        }

        public void Empujar(Transform quien)
        {
            portador = quien;
            ignorar = quien != null ? quien.GetComponent<Collider>() : null;
            Empujando = true;
        }

        public void Soltar() { Empujando = false; portador = null; Bloqueado = false; }

        public void Cargar(string lote, string firma) { Lote = lote; Firma = firma; }

        public void Vaciar() { Lote = null; Firma = null; }

        /// <summary>Vuelve a su base, vacío y suelto (para un carro perdido o atascado).</summary>
        public void Recuperar()
        {
            Soltar();
            Vaciar();
            transform.SetPositionAndRotation(Base, Quaternion.identity);
        }

        void LateUpdate()
        {
            if (!Empujando || portador == null) return;
            Vector3 fw = portador.forward; fw.y = 0f;
            if (fw.sqrMagnitude < 1e-4f) return;
            fw.Normalize();
            Vector3 destino = portador.position + fw * Distancia;
            destino.y = Base.y;
            destino.x = Mathf.Clamp(destino.x, LimiteMin.x, LimiteMax.x);
            destino.z = Mathf.Clamp(destino.z, LimiteMin.y, LimiteMax.y);
            Quaternion giro = Quaternion.LookRotation(fw, Vector3.up);

            // ¿Cabe ahí? (paredes, estanterías, terminales…); el personaje no cuenta.
            bool libre = true;
            foreach (Collider c in Physics.OverlapBox(destino + Vector3.up * 0.6f, new Vector3(Ancho / 2f, 0.45f, Largo / 2f), giro, ~0, QueryTriggerInteraction.Ignore))
                if (c != ignorar && !c.transform.IsChildOf(transform)) { libre = false; break; }
            Bloqueado = !libre;
            if (libre) transform.SetPositionAndRotation(destino, giro);
        }
    }
}
