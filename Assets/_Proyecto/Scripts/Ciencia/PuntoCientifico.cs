using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Objeto que se puede escanear. Necesita un Collider (no trigger) para
    /// que el escáner lo detecte. Para añadir uno nuevo: arrastra el prefab
    /// PuntoCientifico a la escena y asígnale una ficha.
    /// </summary>
    public class PuntoCientifico : MonoBehaviour
    {
        /// <summary>Todos los puntos activos en la escena.</summary>
        public static readonly List<PuntoCientifico> Todos = new List<PuntoCientifico>();

        [SerializeField] FichaCientifica ficha;

        [Header("Aspecto")]
        [Tooltip("Objeto visible que cambia de material al escanearlo.")]
        [SerializeField] Renderer indicador;
        [SerializeField] Material materialEscaneado;
        [SerializeField] Light luz;
        [SerializeField] Color colorLuzEscaneado = new Color(0.3f, 0.6f, 0.65f);

        float intensidadBase;

        public FichaCientifica Ficha => ficha;
        public string Titulo => ficha != null ? ficha.titulo : name;
        public bool Escaneado { get; private set; }

        void Awake()
        {
            if (luz != null) intensidadBase = luz.intensity;
        }

        void OnEnable()
        {
            if (!Todos.Contains(this)) Todos.Add(this);
        }

        void OnDisable()
        {
            Todos.Remove(this);
        }

        void Update()
        {
            // Pulso suave de la luz mientras no se ha escaneado.
            if (!Escaneado && luz != null)
                luz.intensity = intensidadBase * (0.75f + 0.25f * Mathf.Sin(Time.time * 2.5f));
        }

        public void MarcarEscaneado()
        {
            if (Escaneado) return;
            Escaneado = true;

            if (indicador != null && materialEscaneado != null)
                indicador.sharedMaterial = materialEscaneado;

            if (luz != null)
            {
                luz.color = colorLuzEscaneado;
                luz.intensity = intensidadBase * 0.4f;
            }
        }
    }
}
