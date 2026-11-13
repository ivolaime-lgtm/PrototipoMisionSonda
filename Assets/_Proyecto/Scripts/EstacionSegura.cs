using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Zona de la estación. Mientras la sonda está dentro del collider
    /// (marcado como "Is Trigger"), su batería se recarga.
    /// En el próximo incremento también será el punto de transmisión de datos.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EstacionSegura : MonoBehaviour
    {
        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider otro)
        {
            SistemaEnergia energia = BuscarEnergia(otro);
            if (energia != null) energia.EstablecerCarga(true);
        }

        void OnTriggerExit(Collider otro)
        {
            SistemaEnergia energia = BuscarEnergia(otro);
            if (energia != null) energia.EstablecerCarga(false);
        }

        static SistemaEnergia BuscarEnergia(Collider otro)
        {
            if (otro.attachedRigidbody != null)
                return otro.attachedRigidbody.GetComponent<SistemaEnergia>();
            return otro.GetComponentInParent<SistemaEnergia>();
        }
    }
}
