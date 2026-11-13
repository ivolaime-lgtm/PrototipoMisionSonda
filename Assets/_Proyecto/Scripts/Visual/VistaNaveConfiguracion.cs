using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Vista exterior de la nave en Configuracion (solo visual). Se crea en tiempo de ejecución,
    /// lejos de la escena, y se dibuja en una RenderTexture que muestra PantallaConfiguracion.
    ///
    /// Disposición (horizontal, de izquierda a derecha):
    ///   LANZADOR (se separa en el despegue) │ hueco │ MOTOR → TANQUE (módulo de servicio) → bodega → módulo de tripulación
    /// Motor y tanque cuelgan de puntos de anclaje; al cambiar de opción se sustituye la variante.
    /// La parte superior (bodega y tripulación) se coloca al final del tanque elegido.
    /// Formas simples provisionales (sin modelos descargados).
    /// </summary>
    public class VistaNaveConfiguracion : MonoBehaviour
    {
        public RenderTexture Textura { get; private set; }

        static readonly Vector3 Origen = new Vector3(0f, -3000f, 0f);

        Transform raiz, eje, lanzador, anclajeMotor, motorActual, anclajeTanque, tanqueActual, superior;
        OpcionTanqueNave tanqueMostrado;
        bool hayTanqueMostrado;
        Camera camara;
        Light luz;
        readonly List<Material> materiales = new List<Material>();
        OpcionMotorNave motorMostrado;
        bool hayMotorMostrado;
        float escalaLanzadorMostrada = -1f;

        Material matBlanco, matGris, matOscuro, matNaranja, matVidrio;

        void Awake()
        {
            Textura = new RenderTexture(1024, 360, 16) { name = "VistaNave", antiAliasing = 2 };
            matBlanco = Mat(new Color(0.88f, 0.9f, 0.93f), 0.1f, 0.5f);
            matGris = Mat(new Color(0.5f, 0.55f, 0.62f), 0.5f, 0.5f);
            matOscuro = Mat(new Color(0.18f, 0.2f, 0.24f), 0.7f, 0.5f);
            matNaranja = Mat(new Color(1f, 0.55f, 0.15f), 0.2f, 0.5f);
            matVidrio = Mat(new Color(0.25f, 0.75f, 0.9f), 0.2f, 0.8f);

            raiz = new GameObject("VistaNave (solo visual)").transform;
            raiz.position = Origen;
            // El eje de la pila apunta a la derecha; la pila gira sobre ese eje para dar volumen.
            eje = new GameObject("Eje").transform;
            eje.SetParent(raiz, false);
            eje.localRotation = Quaternion.Euler(0f, 0f, -90f);

            ConstruirLanzador();
            ConstruirNave();

            GameObject cam = new GameObject("CamaraVistaNave");
            cam.transform.SetParent(raiz, false);
            cam.transform.localPosition = new Vector3(-1.5f, 3f, -24f);
            cam.transform.LookAt(raiz.position + new Vector3(-1.5f, 0f, 0f));
            camara = cam.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.03f, 0.09f, 0.12f);
            camara.fieldOfView = 26f;
            camara.nearClipPlane = 1f;
            camara.farClipPlane = 80f;
            camara.targetTexture = Textura;
            camara.depth = -10f;

            GameObject l = new GameObject("LuzVistaNave");
            l.transform.SetParent(raiz, false);
            l.transform.localRotation = Quaternion.Euler(30f, -40f, 0f);
            luz = l.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.2f;
        }

        // Altura a lo largo del eje (en el eje local Y de "eje"): el lanzador en negativo, la nave en positivo.
        void ConstruirLanzador()
        {
            lanzador = new GameObject("Lanzador (se separa)").transform;
            lanzador.SetParent(eje, false);
            Pieza(PrimitiveType.Cylinder, lanzador, new Vector3(0f, -6f, 0f), new Vector3(2.2f, 5f, 2.2f), matBlanco);
            Pieza(PrimitiveType.Cylinder, lanzador, new Vector3(0f, -1.3f, 0f), new Vector3(2.25f, 0.3f, 2.25f), matNaranja);
            Pieza(PrimitiveType.Cylinder, lanzador, new Vector3(0f, -11.4f, 0f), new Vector3(1.4f, 0.4f, 1.4f), matOscuro);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                Transform aleta = Pieza(PrimitiveType.Cube, lanzador, Vector3.zero, new Vector3(0.12f, 2f, 1.2f), matGris).transform;
                aleta.localRotation = Quaternion.Euler(0f, a, 0f);
                aleta.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, -10.2f, 1.5f);
            }
        }

        void ConstruirNave()
        {
            Transform nave = new GameObject("Vehículo tripulado (continúa)").transform;
            nave.SetParent(eje, false);
            // Punto de anclaje del tanque (módulo de servicio): empieza encima del motor.
            anclajeTanque = new GameObject("Anclaje_Tanque").transform;
            anclajeTanque.SetParent(nave, false);
            anclajeTanque.localPosition = new Vector3(0f, 1.6f, 0f);

            // Parte superior: se coloca al final del tanque elegido.
            superior = new GameObject("Bodega y tripulación").transform;
            superior.SetParent(nave, false);
            superior.localPosition = new Vector3(0f, 1.6f + 3.2f, 0f);
            // Bodega de carga científica (sonda + relevo)
            Pieza(PrimitiveType.Cylinder, superior, new Vector3(0f, 0.6f, 0f), new Vector3(2.1f, 0.6f, 2.1f), matOscuro);
            // Módulo de tripulación con ventanas y punta
            Pieza(PrimitiveType.Cylinder, superior, new Vector3(0f, 2.2f, 0f), new Vector3(2.2f, 1.0f, 2.2f), matBlanco);
            Pieza(PrimitiveType.Cube, superior, new Vector3(0f, 2.5f, -1.05f), new Vector3(1.0f, 0.35f, 0.1f), matVidrio);
            Pieza(PrimitiveType.Cylinder, superior, new Vector3(0f, 3.5f, 0f), new Vector3(1.6f, 0.3f, 1.6f), matBlanco);
            Pieza(PrimitiveType.Cylinder, superior, new Vector3(0f, 4.0f, 0f), new Vector3(1.0f, 0.25f, 1.0f), matBlanco);
            Pieza(PrimitiveType.Sphere, superior, new Vector3(0f, 4.4f, 0f), new Vector3(0.55f, 0.55f, 0.55f), matNaranja);

            // Punto de anclaje del motor: base del módulo de servicio, apuntando hacia el lanzador.
            anclajeMotor = new GameObject("Anclaje_Motor").transform;
            anclajeMotor.SetParent(nave, false);
            anclajeMotor.localPosition = new Vector3(0f, 1.6f, 0f);
        }

        /// <summary>Muestra el motor y el tanque elegidos y ajusta el lanzador. Solo reconstruye si algo cambió.</summary>
        public void Mostrar(OpcionMotorNave motor, OpcionTanqueNave tanque, float escalaLanzador)
        {
            if (!hayTanqueMostrado || tanque != tanqueMostrado)
            {
                hayTanqueMostrado = true;
                tanqueMostrado = tanque;
                if (tanqueActual != null) Destroy(tanqueActual.gameObject);
                tanqueActual = ConstruirTanque(tanque);
            }
            if (!hayMotorMostrado || motor != motorMostrado)
            {
                hayMotorMostrado = true;
                motorMostrado = motor;
                if (motorActual != null) Destroy(motorActual.gameObject);
                motorActual = ConstruirMotor(motor);
            }
            if (!Mathf.Approximately(escalaLanzador, escalaLanzadorMostrada))
            {
                escalaLanzadorMostrada = escalaLanzador;
                lanzador.localScale = new Vector3(Mathf.Lerp(1f, escalaLanzador, 0.5f), escalaLanzador, Mathf.Lerp(1f, escalaLanzador, 0.5f));
            }
        }

        Transform ConstruirTanque(OpcionTanqueNave t)
        {
            float largo = t != null ? Mathf.Max(1f, t.largo) : 3.2f;
            float diametro = t != null ? Mathf.Max(0.5f, t.diametro) : 2.0f;
            Transform variante = new GameObject(t != null ? "Tanque: " + t.nombre : "Tanque").transform;
            variante.SetParent(anclajeTanque, false);
            Material mat = Mat(t != null ? t.color : new Color(0.5f, 0.55f, 0.62f), 0.5f, 0.5f);
            Pieza(PrimitiveType.Cylinder, variante, new Vector3(0f, largo / 2f, 0f), new Vector3(diametro, largo / 2f, diametro), mat);
            // Bandas naranjas: una por cada 2 m de tanque (se nota el tamaño)
            int bandas = Mathf.Max(1, Mathf.RoundToInt(largo / 2f));
            for (int i = 0; i < bandas; i++)
            {
                float y = largo * (i + 1) / (bandas + 1);
                Pieza(PrimitiveType.Cylinder, variante, new Vector3(0f, y, 0f), new Vector3(diametro + 0.05f, 0.06f, diametro + 0.05f), matNaranja);
            }
            superior.localPosition = new Vector3(0f, 1.6f + largo, 0f);
            return variante;
        }

        Transform ConstruirMotor(OpcionMotorNave m)
        {
            Transform variante = new GameObject(m != null ? "Motor: " + m.nombre : "Motor").transform;
            variante.SetParent(anclajeMotor, false);
            float t = m != null ? Mathf.Max(0.5f, m.tamanoTobera) : 1f;
            Material mat = Mat(m != null ? m.color : new Color(0.35f, 0.37f, 0.4f), 0.8f, 0.6f);

            // Tobera en forma de campana: anillos que se ensanchan hacia el lanzador.
            Pieza(PrimitiveType.Cylinder, variante, new Vector3(0f, -0.15f, 0f), new Vector3(0.6f * t, 0.15f, 0.6f * t), matOscuro);
            Pieza(PrimitiveType.Cylinder, variante, new Vector3(0f, -0.5f, 0f), new Vector3(0.8f * t, 0.2f * t, 0.8f * t), mat);
            Pieza(PrimitiveType.Cylinder, variante, new Vector3(0f, -0.5f - 0.4f * t, 0f), new Vector3(1.05f * t, 0.2f * t, 1.05f * t), mat);
            Pieza(PrimitiveType.Cylinder, variante, new Vector3(0f, -0.5f - 0.8f * t, 0f), new Vector3(1.3f * t, 0.15f * t, 1.3f * t), mat);

            int aux = m != null ? Mathf.Clamp(m.toberasAuxiliares, 0, 8) : 0;
            for (int i = 0; i < aux; i++)
            {
                float a = i * 360f / aux;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(0.75f, -0.4f, 0f);
                Pieza(PrimitiveType.Cylinder, variante, p, new Vector3(0.28f, 0.3f, 0.28f), mat);
            }
            return variante;
        }

        void Update()
        {
            eje.Rotate(Vector3.up, 18f * Time.deltaTime, Space.Self);
        }

        GameObject Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Material m)
        {
            GameObject go = GameObject.CreatePrimitive(tipo);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        Material Mat(Color c, float metalico, float suavidad)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material m = new Material(sh);
            m.SetColor("_BaseColor", c);
            m.color = c;
            m.SetFloat("_Metallic", metalico);
            m.SetFloat("_Smoothness", suavidad);
            materiales.Add(m);
            return m;
        }

        void OnDestroy()
        {
            if (camara != null) camara.targetTexture = null;
            if (Textura != null) Textura.Release();
            if (raiz != null) Destroy(raiz.gameObject);
            foreach (Material m in materiales) if (m != null) Destroy(m);
        }
    }
}
