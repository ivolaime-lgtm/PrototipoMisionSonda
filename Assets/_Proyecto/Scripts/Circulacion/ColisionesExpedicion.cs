using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Colisiones de la EM-4X para el personaje provisional (SOLO FÍSICA). No construye geometría nueva: añade una
    /// BoxCollider a cada bloque que ya construyó VistaExpedicion (cascos con sus huecos, tapas, ventanas, escotillas
    /// cerradas, adaptadores, equipos y lanzadera), así lo que se ve y lo que choca son exactamente las mismas piezas.
    /// No se tocan VistaExpedicion ni la escena ExpedicionPrueba.
    ///
    /// Se excluyen: asideros (barras visuales de 5 cm, no deben enganchar), rótulos, esquema, referencias humanas y el
    /// exterior (antena, motor, paneles, reserva de la sonda: el personaje no sale de la nave).
    /// </summary>
    public static class ColisionesExpedicion
    {
        static readonly string[] Raices =
        {
            "Cascos de los módulos", "Adaptadores y escotillas", "Equipos interiores (bloques)"
        };

        /// <summary>Devuelve cuántas colisiones añadió (0 si no encontró la nave).</summary>
        public static int Anadir(Transform nave)
        {
            if (nave == null) return 0;
            int n = 0;
            foreach (string nombre in Raices)
            {
                Transform raiz = nave.Find(nombre);
                if (raiz == null)
                {
                    Debug.LogWarning("[Circulación] No se encontró «" + nombre + "» en la nave: esas piezas no tendrán colisión.");
                    continue;
                }
                foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
                {
                    GameObject g = mf.gameObject;
                    if (g.name == "Asidero") continue;
                    if (g.GetComponent<Collider>() != null) continue;
                    // Todas las piezas de casco, adaptador y equipo son cubos primitivos: la caja de 1 × 1 × 1 escalada coincide
                    g.AddComponent<BoxCollider>();
                    n++;
                }
            }
            return n;
        }
    }
}
