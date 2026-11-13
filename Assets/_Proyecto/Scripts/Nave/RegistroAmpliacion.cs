using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Va en la raíz NAVE_AMPLIADA. Guarda lo que el menú "Prototipo → 9. Ampliar nave" cambió FUERA de esa raíz
    /// para que "Prototipo → 10. Volver a la cabina original" lo restaure exactamente:
    /// posición y giro de los puestos movidos, paredes antiguas desactivadas, luces de alerta de ControlNave
    /// y aviso de escombros del modo de prueba. Sin lógica de juego.
    /// </summary>
    public class RegistroAmpliacion : MonoBehaviour
    {
        public List<Transform> puestosMovidos = new List<Transform>();
        public List<Vector3> posicionesOriginales = new List<Vector3>();
        public List<Quaternion> rotacionesOriginales = new List<Quaternion>();

        [Tooltip("Paredes de la cabina original desactivadas (dibujo y colisionador).")]
        public List<GameObject> desactivados = new List<GameObject>();

        public ControlNave control;
        [Tooltip("Lista original de luces de alerta de ControlNave.")]
        public List<Light> lucesOriginales = new List<Light>();
        public bool avisoGuardado;
        public float avisoPruebaOriginal;
    }
}
