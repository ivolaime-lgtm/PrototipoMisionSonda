using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    // =====================================================================================================================
    // CONFIGURACIÓN CENTRAL DEL JUEGO (adaptación a requisitos). Guía: Documentacion/ADAPTACION_REQUISITOS.md
    //
    // Una sola fuente para lo que puede cambiar con los requisitos: el hangar, las secuencias y la misión leen de aquí.
    // · Sin archivo, el juego usa los VALORES POR DEFECTO de este script, que son exactamente los de la versión validada
    //   (AG): nada cambia si no se crea el archivo.
    // · Con «Juego → Configuración → 1. Crear archivo de configuración» se crea Assets/_Proyecto/Resources/
    //   ConfiguracionJuego.asset con esos mismos valores; se edita en el Inspector y entra en el ejecutable (Resources).
    // · «Juego → Configuración → 2. Comprobar configuración» revisa dependencias (alturas, radios, cofia, etc.).
    //
    // Las listas vacías («opciones», «misiones», «textos», «camaras») significan «usar los valores del código»; el menú 1 las
    // rellena con esos valores para poder editarlos. Las proporciones de la L-4 y del LC-2 son SOLO VISUALES: no cambian
    // masa, capacidad, empuje ni combustible (eso está en «opciones» y en «transporte»).
    // =====================================================================================================================

    [CreateAssetMenu(fileName = "ConfiguracionJuego", menuName = "Prototipo/Configuración del juego (adaptación a requisitos)")]
    public class ConfiguracionJuego : ScriptableObject
    {
        public const string RutaResources = "ConfiguracionJuego";
        public const string RutaAsset = "Assets/_Proyecto/Resources/ConfiguracionJuego.asset";

        static ConfiguracionJuego actual;
        static bool porDefecto;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { actual = null; porDefecto = false; }

        /// <summary>La configuración en uso: el archivo de Resources o, si no existe, los valores por defecto.</summary>
        public static ConfiguracionJuego Actual
        {
            get
            {
                if (actual != null) return actual;
                actual = Resources.Load<ConfiguracionJuego>(RutaResources);
                porDefecto = actual == null;
                if (porDefecto)
                {
                    actual = CreateInstance<ConfiguracionJuego>();
                    actual.name = "ConfiguracionJuego (valores por defecto, sin archivo)";
                    actual.hideFlags = HideFlags.DontSave;
                }
                return actual;
            }
        }

        /// <summary>true si no hay archivo y se usan los valores del código.</summary>
        public static bool UsaValoresPorDefecto { get { _ = Actual; return porDefecto; } }

        /// <summary>Solo editor: olvida la configuración cargada (tras crear o modificar el archivo).</summary>
        public static void Recargar() { actual = null; }

        [Header("Secuencias (duración, orden, omisión, activación)")]
        public SecuenciasConfig secuencias = new SecuenciasConfig();
        [Header("Puntos de carga, conexión y atraque (coordenadas de la EM-4X)")]
        public PuntosOrbitalesConfig puntos = new PuntosOrbitalesConfig();
        [Header("Planos de cámara (relativos a un ancla). Vacío = los del código")]
        public List<PlanoCamara> camaras = new List<PlanoCamara>();
        [Header("Rótulos e indicadores de las secuencias. Vacío = los del código")]
        public List<TextoConfigurable> textos = new List<TextoConfigurable>();
        [Header("Proporciones VISUALES de la L-4 (no cambian masa, capacidad ni empuje)")]
        public ProporcionesL4 lanzadera = new ProporcionesL4();
        [Header("Proporciones VISUALES del lanzador LC-2 del VC-1")]
        public ProporcionesLC2 lanzadorVC1 = new ProporcionesLC2();
        [Header("Transporte VC-1: límites de carga (reglas de juego)")]
        public TransporteConfig transporte = new TransporteConfig();
        [Header("Tarifas y costes (M USD)")]
        public TarifasConfig tarifas = new TarifasConfig();
        [Header("Opciones de componentes y sus valores de balance. Vacío = las del código")]
        public OpcionesConfig opciones = new OpcionesConfig();
        [Header("Misiones: textos y condiciones")]
        public MisionesConfig misiones = new MisionesConfig();

        // ------------------------------------------------------------------ Textos

        /// <summary>Texto por clave (de la lista del archivo o, si falta, el del código).</summary>
        public static string Texto(string clave)
        {
            ConfiguracionJuego c = Actual;
            if (c.textos != null)
                foreach (TextoConfigurable t in c.textos)
                    if (t != null && t.clave == clave) return t.texto ?? "";
            string d;
            return TextosPorDefecto.TryGetValue(clave, out d) ? d : clave;
        }

        /// <summary>Rótulos breves de las secuencias (no son cartillas: una línea, como indicador de operación).</summary>
        public static readonly Dictionary<string, string> TextosPorDefecto = new Dictionary<string, string>
        {
            { "vc1.salida", "VC-1 · SALIDA HACIA LA PLATAFORMA" },
            { "vc1.integracion", "VC-1 · INTEGRACIÓN EN EL LC-2" },
            { "vc1.lanzamiento", "VC-1 · LANZAMIENTO" },
            { "vc1.ascenso", "VC-1 · ASCENSO" },
            { "vc1.cofia", "SEPARACIÓN DE LA COFIA" },
            { "vc1.separacion", "VC-1 · SEPARACIÓN DEL LANZADOR" },
            { "vc1.aproximacion", "VC-1 · APROXIMACIÓN A LA EM-4X" },
            { "vc1.espera", "VC-1 · PUNTO DE ESPERA" },
            { "vc1.alineacion", "VC-1 · ALINEACIÓN" },
            { "vc1.conexion", "CONEXIÓN DE SERVICIO · CARGA (SIN TRIPULACIÓN)" },
            { "vc1.recepcion", "RECEPCIÓN" },
            { "vc1.retirada", "RETIRADA" },
            { "vc1.colocacion", "COLOCACIÓN" },
            { "vc1.comprobacion", "COMPROBACIÓN" },
            { "vc1.combustible", "TRANSFERENCIA DE COMBUSTIBLE" },
            { "vc1.carga", "CARGA AL PUERTO DE SUMINISTRO" },
            { "vc1.desconexion", "DESCONEXIÓN Y SALIDA DEL VC-1" },
            { "vc1.preparada", "EM-4X PREPARADA" },
            { "l4.carga", "L-4 · CARGA DE LA BODEGA" },
            { "l4.traslado", "L-4 · TRASLADO A LA PLATAFORMA" },
            { "l4.embarque", "L-4 · EMBARQUE" },
            { "l4.compuerta", "CIERRE DE COMPUERTA" },
            { "l4.comprobacion", "COMPROBACIÓN" },
            { "l4.cuenta", "CUENTA ATRÁS" },
            { "l4.encendido", "ENCENDIDO" },
            { "l4.ascenso", "L-4 · ASCENSO" },
            { "l4.refuerzos", "SEPARACIÓN DE LOS REFUERZOS" },
            { "l4.etapa", "SEPARACIÓN DE LA ETAPA PRINCIPAL" },
            { "l4.aproximacion", "L-4 · APROXIMACIÓN A LA EM-4X" },
            { "l4.alineacion", "L-4 · ALINEACIÓN CON EL PUERTO DEL MANDO" },
            { "l4.atraque", "ATRAQUE" },
            { "l4.transferencia", "TRANSFERENCIA DE CARGA" },
            { "l4.apertura", "APERTURA DEL ACCESO" },
            { "l4.paso", "PASO A LA EM-4X" },
            { "abordo.paso", "A BORDO · MÓDULO DE MANDO → NODO CENTRAL" },
            { "omitir", "ESPACIO o INTRO: omitir" },
            { "omitir.luego", "se podrá omitir las próximas veces" },
        };

        // ------------------------------------------------------------------ Cámaras

        /// <summary>Plano por identificador (del archivo o, si falta, el del código).</summary>
        public static PlanoCamara Plano(string id)
        {
            ConfiguracionJuego c = Actual;
            if (c.camaras != null)
                foreach (PlanoCamara p in c.camaras)
                    if (p != null && p.id == id) return p;
            foreach (PlanoCamara p in PlanosPorDefecto())
                if (p.id == id) return p;
            return new PlanoCamara { id = id, ancla = AnclaCamara.EM4X, posicion = new Vector3(60f, 10f, -40f), foco = Vector3.zero };
        }

        /// <summary>Planos de las secuencias. «posicion» y «foco» van en ejes del ancla (ver AnclaCamara).</summary>
        public static List<PlanoCamara> PlanosPorDefecto() => new List<PlanoCamara>
        {
            // Envío VC-1 (ancla: plataforma exterior del VC-1 / lanzador / VC-1 / EM-4X)
            P("vc1.salida", AnclaCamara.Plataforma, new Vector3(30f, 20f, 38f), AnclaCamara.Vehiculo, new Vector3(-12f, 0f, 0f)),
            P("vc1.integracion", AnclaCamara.Plataforma, new Vector3(70f, 30f, 80f), new Vector3(10f, 35f, 0f)),
            P("vc1.lanzamiento", AnclaCamara.Plataforma, new Vector3(110f, 8f, -150f), AnclaCamara.Vehiculo, new Vector3(0f, 40f, 0f)),
            P("vc1.espacio", AnclaCamara.Vehiculo, new Vector3(34f, 12f, -46f), new Vector3(0f, 45f, 0f)),
            P("vc1.lejos", AnclaCamara.EM4X, new Vector3(150f, 45f, -40f), new Vector3(0f, -10f, -60f)),
            P("vc1.espera", AnclaCamara.EM4X, new Vector3(62f, 18f, -125f), new Vector3(8f, -8f, -60f)),
            P("vc1.servicio", AnclaCamara.EM4X, new Vector3(48f, 10f, -78f), new Vector3(4f, -6f, -48f)),
            P("vc1.instalacion.popa", AnclaCamara.EM4X, new Vector3(36f, 16f, -76f), new Vector3(0f, 0f, -40f)),
            P("vc1.instalacion.ciencia", AnclaCamara.EM4X, new Vector3(-44f, 14f, -18f), new Vector3(-16f, 0f, 1.5f)),
            P("vc1.instalacion.comunicaciones", AnclaCamara.EM4X, new Vector3(44f, 18f, -16f), new Vector3(12f, 3f, 1.5f)),
            P("vc1.suministro", AnclaCamara.EM4X, new Vector3(34f, 10f, -42f), new Vector3(4f, 0f, -20f)),
            // Tripulación L-4 (ancla: plataforma de lanzamiento / L-4 / EM-4X)
            P("l4.traslado", AnclaCamara.Plataforma, new Vector3(55f, 14f, 40f), AnclaCamara.Vehiculo, new Vector3(0f, 16f, 0f)),
            P("l4.ascenso", AnclaCamara.Plataforma, new Vector3(60f, 4f, -90f), AnclaCamara.Vehiculo, new Vector3(0f, 14f, 0f)),
            P("l4.espacio", AnclaCamara.Vehiculo, new Vector3(30f, 8f, -34f), new Vector3(0f, 18f, 0f)),
            P("l4.lejos", AnclaCamara.EM4X, new Vector3(120f, -30f, 90f), new Vector3(0f, -10f, 0f)),
            P("l4.alineacion", AnclaCamara.EM4X, new Vector3(34f, -18f, 30f), new Vector3(0f, -12f, 11.7f)),
            P("l4.atraque", AnclaCamara.EM4X, new Vector3(16f, -8f, 22f), new Vector3(0f, -5f, 11.7f)),
        };

        static PlanoCamara P(string id, AnclaCamara a, Vector3 pos, Vector3 foco) => P(id, a, pos, a, foco);

        static PlanoCamara P(string id, AnclaCamara a, Vector3 pos, AnclaCamara af, Vector3 foco) =>
            new PlanoCamara { id = id, ancla = a, posicion = pos, anclaFoco = af, foco = foco, campo = 70f };

        /// <summary>Copia independiente de una lista de opciones del archivo (nadie modifica el archivo al usarlas).</summary>
        public static List<T> Copia<T>(List<T> origen)
        {
            // Una copia por fotograma y lista (la interfaz lo pide en cada OnGUI); cada llamada recibe su propia lista
            object guardada;
            if (copias.TryGetValue(origen, out guardada) && fotogramaCopias == Time.frameCount) return new List<T>((List<T>)guardada);
            if (fotogramaCopias != Time.frameCount) { copias.Clear(); fotogramaCopias = Time.frameCount; }
            List<T> l = new List<T>();
            foreach (T x in origen)
                if (x != null) l.Add(JsonUtility.FromJson<T>(JsonUtility.ToJson(x)));
            copias[origen] = l;
            return new List<T>(l);
        }

        static readonly Dictionary<object, object> copias = new Dictionary<object, object>();
        static int fotogramaCopias = -1;

        /// <summary>Escala de tiempo de las secuencias (duración = valor / velocidad).</summary>
        public static float D(float segundos) => segundos / Mathf.Max(0.1f, Actual.secuencias.velocidad);
    }

    // ==================================================================================================================== Secciones

    [Serializable]
    public class SecuenciasConfig
    {
        [Tooltip("Mostrar la animación del envío orbital VC-1 cuando hace falta envío. Si está desactivada, el envío se da por hecho (mismos costes y piezas instaladas).")]
        public bool mostrarEnvioVC1 = true;
        [Tooltip("Mostrar la animación de la tripulación L-4 (embarque, lanzamiento, atraque). Si está desactivada se pasa directamente a bordo.")]
        public bool mostrarTripulacionL4 = true;
        [Tooltip("Mostrar el paso animado del acceso al nodo central al entrar en la expedición.")]
        public bool mostrarEntradaABordo = true;
        [Tooltip("Permitir omitir cada parte desde la primera vez (por defecto, solo después de verla entera en la sesión).")]
        public bool omitirDesdeLaPrimeraVez = false;
        [Tooltip("Velocidad de reproducción de todas las secuencias (1 = normal; 2 = el doble de rápido).")]
        [Range(0.25f, 4f)] public float velocidad = 1f;
        [Tooltip("Mostrar la separación de refuerzos y etapa de la L-4 y de la cofia del VC-1.")]
        public bool mostrarSeparaciones = true;

        [Header("Duraciones VC-1 (s)")]
        public float vc1Salida = 3f;
        public float vc1Integracion = 2.4f;
        public float vc1Ascenso = 4f;
        public float vc1Espacio = 4.5f;
        public float vc1Aproximacion = 5f;
        public float vc1Alineacion = 2f;
        public float vc1Acercamiento = 3f;
        public float vc1Conexion = 1.8f;
        public float vc1Tramo = 1.3f;
        public float vc1Salida2 = 3f;

        [Header("Duraciones L-4 (s)")]
        public float l4Traslado = 4f;
        public float l4Embarque = 2.2f;
        public float l4Compuerta = 1.8f;
        public int l4CuentaAtras = 5;
        public float l4Ascenso = 4.5f;
        public float l4Espacio = 5f;
        public float l4Aproximacion = 5f;
        public float l4Alineacion = 2f;
        public float l4Atraque = 3f;
        public float l4Apertura = 1.6f;
        public float l4Paso = 1.2f;
        public float entradaABordo = 7f;
    }

    [Serializable]
    public class PuntosOrbitalesConfig
    {
        [Tooltip("Desplazamiento del atraque de la L-4 respecto al puerto «atraque» del Mando (definición de la EM-4X).")]
        public Vector3 atraqueL4Desfase = Vector3.zero;
        [Tooltip("Distancia bajo el puerto donde la L-4 se detiene para alinearse (m).")]
        public float esperaL4M = 22f;
        [Tooltip("Popa del VC-1 aparcado (coordenadas de la EM-4X; el VC-1 apunta a +Z con la bodega hacia +Y).")]
        public Vector3 aparcamientoVC1 = new Vector3(14f, -14f, -80f);
        [Tooltip("Punto de espera del VC-1 antes de la aproximación final, relativo al aparcamiento.")]
        public Vector3 esperaVC1 = new Vector3(0f, -6f, -60f);
        [Tooltip("Llegada visible del VC-1 desde lejos, relativa al aparcamiento.")]
        public Vector3 llegadaVC1 = new Vector3(60f, -70f, -420f);
        [Tooltip("Plataforma de servicio: centro de la viga (coordenadas de la EM-4X).")]
        public Vector3 vigaServicio = new Vector3(0f, -10f, -44f);
        public float largoViga = 44f;
        [Tooltip("Bandeja de recepción (coordenadas de la EM-4X).")]
        public Vector3 bandejaRecepcion = new Vector3(5f, -9f, -56f);
        [Tooltip("Bastidor de piezas retiradas (coordenadas de la EM-4X).")]
        public Vector3 bastidorRetiradas = new Vector3(-8f, -9f, -50f);
        [Tooltip("Toma de la conexión de servicio en la viga (coordenadas de la EM-4X).")]
        public Vector3 tomaServicio = new Vector3(1.25f, -11f, -62f);
        [Tooltip("Puerto de servicio del VC-1 (coordenadas del VC-1: popa en z = 0, babor en −x).")]
        public Vector3 puertoServicioVC1 = new Vector3(-3.8f, 0f, 18f);
        [Tooltip("Toma de combustible de la etapa de propulsión (coordenadas de la EM-4X).")]
        public Vector3 tomaCombustible = new Vector3(0f, -3f, -29f);
        [Tooltip("Pasillo seguro para las piezas: z por detrás de la nave, altura y x de los costados (no cruza la EM-4X ni los paneles).")]
        public float pasilloZ = -62f;
        public float pasilloY = 4f;
        public float pasilloX = 24f;
        public float pasilloTanqueY = 8f;
        [Tooltip("Montaje de cada componente (cara de montaje; la pieza sale en la dirección «giro»). Coordenadas de la EM-4X.")]
        public Vector3 montajeMotor = new Vector3(0f, 0f, -37f);
        public Vector3 giroMotor = new Vector3(0f, 180f, 0f);
        public Vector3 montajeTanque = new Vector3(0f, 0f, -23f);
        public Vector3 giroTanque = new Vector3(0f, 180f, 0f);
        public Vector3 montajeInstrumento = new Vector3(-15.2f, 0f, 1.5f);
        public Vector3 giroInstrumento = new Vector3(0f, -90f, 0f);
        [Tooltip("El relevo va sobre el módulo de Comunicaciones (la punta del módulo la ocupa la antena).")]
        public Vector3 montajeRelevo = new Vector3(9.7f, 3f, 1.5f);
        public Vector3 giroRelevo = new Vector3(-90f, 0f, 0f);
    }

    public enum AnclaCamara { EM4X, Vehiculo, Plataforma }

    [Serializable]
    public class PlanoCamara
    {
        public string id;
        [Tooltip("Ancla de «posicion». EM4X: ejes de la nave · Vehiculo: origen del vehículo del plano (ejes del mundo) · Plataforma: la plataforma de tierra o el punto en el espacio del plano.")]
        public AnclaCamara ancla;
        public Vector3 posicion;
        [Tooltip("Ancla de «foco» (puede ser otra: p. ej. cámara fija en la plataforma que sigue al vehículo).")]
        public AnclaCamara anclaFoco;
        public Vector3 foco;
        public float campo = 70f;
    }

    [Serializable]
    public class TextoConfigurable
    {
        public string clave;
        [TextArea] public string texto;
    }

    /// <summary>Proporciones VISUALES de la L-4 (m). No cambian masa, capacidad, empuje ni combustible.</summary>
    [Serializable]
    public class ProporcionesL4
    {
        [Tooltip("Etapa principal: diámetro (también el de tanque de transferencia y bodega).")]
        public float diametro = 5f;
        [Tooltip("Etapa principal: altura.")]
        public float alturaEtapa = 18f;
        public float diametroToberas = 3.2f;
        [Tooltip("Propulsores de refuerzo: diámetro, altura y separación entre su casco y el de la etapa.")]
        public float diametroRefuerzo = 1.8f;
        public float alturaRefuerzo = 14f;
        public float separacionRefuerzo = 0.15f;
        [Tooltip("Altura del tanque de transferencia para la opción 0, 1 y 2 (0 = sin tanque).")]
        public float[] alturaTanque = { 0f, 3f, 5f };
        [Tooltip("Altura de la bodega para la opción 0, 1 y 2 (0 = sin bodega).")]
        public float[] alturaBodega = { 0f, 2.5f, 4f };
        public float diametroCapsula = 4.4f;
        public float alturaCapsula = 3.2f;
        public float alturaTorreEscape = 3f;

        public float Tanque(int i) => alturaTanque != null && i >= 0 && i < alturaTanque.Length ? alturaTanque[i] : 0f;
        public float Bodega(int i) => alturaBodega != null && i >= 0 && i < alturaBodega.Length ? alturaBodega[i] : 0f;
        /// <summary>Distancia del eje al eje de cada refuerzo.</summary>
        public float RadioRefuerzos => diametro / 2f + separacionRefuerzo + diametroRefuerzo / 2f;
        /// <summary>Radio exterior máximo (con refuerzos).</summary>
        public float RadioMaximo => Mathf.Max(diametro / 2f, RadioRefuerzos + diametroRefuerzo / 2f);
    }

    [Serializable]
    public class ProporcionesLC2
    {
        public float diametroEtapa = 8f;
        public float alturaEtapa = 40f;
        public float diametroAcelerador = 3.2f;
        public float alturaAcelerador = 24f;
        public float separacionAcelerador = 5.8f;
        public float diametroCofia = 8.6f;
        public float alturaCofia = 32f;
    }

    [Serializable]
    public class TransporteConfig
    {
        public float masaMaximaKg = 34000f;
        public float diametroBodegaM = 7.2f;
        public float largoBodegaM = 23f;
        public float separacionM = 0.3f;
        public int modulosCombustibleMax = 12;
    }

    [Serializable]
    public class TarifasConfig
    {
        public float transporteOrbitalMUSD = 10f;
        public float instalacionMUSD = 3f;
        public float lanzamientoTripulacionMUSD = 18f;
        public float kitMUSD = 1.5f;
        public float suministroMUSD = 1f;
        public float tramoCombustibleMUSD = 1.2f;
        public int maximoSuministrosExtra = 4;
        public int maximoKitsExtra = 3;
    }

    [Serializable]
    public class OpcionesConfig
    {
        [Tooltip("Vacío = las del código. Máximo 3 por tipo (lo que admite el holograma).")]
        public List<OpcionMotorNave> motoresEM4X = new List<OpcionMotorNave>();
        public List<OpcionTanqueNave> tanquesEM4X = new List<OpcionTanqueNave>();
        public List<OpcionLanzadera> bodegasL4 = new List<OpcionLanzadera>();
        public List<OpcionLanzadera> tanquesL4 = new List<OpcionLanzadera>();
        public List<OpcionLanzadera> refuerzosL4 = new List<OpcionLanzadera>();
    }

    [Serializable]
    public class MisionesConfig
    {
        [Tooltip("Vacío = las tres misiones del código. Los pasos se comprueban en EstadoExpedicion por posición (no se añaden pasos nuevos sin código).")]
        public List<DefinicionMisionMVP> lista = new List<DefinicionMisionMVP>();
        public CondicionesMision condiciones = new CondicionesMision();
    }

    /// <summary>Límites y condiciones de las tres misiones (los mismos números aparecen en los textos de los pasos).</summary>
    [Serializable]
    public class CondicionesMision
    {
        [Tooltip("Paso 1 de todas: distancia mínima a la base para «haber salido» (m).")]
        public float salidaBaseM = 600f;
        [Tooltip("Regreso: distancia máxima a la base (m) y velocidad relativa máxima (m/s).")]
        public float regresoDistanciaM = 400f;
        public float regresoVelocidadMS = 2f;
        [Tooltip("Capturas: desvío máximo de la proa (°) y giro máximo (°/s).")]
        public float proaGrados = 15f;
        public float giroGradosS = 1f;
        [Tooltip("Misión 1: banda de observación (m desde el centro de Kora).")]
        public float bandaMinM = 24000f;
        public float bandaMaxM = 28000f;
        [Tooltip("Misión 2: distancia (m) y velocidad (m/s) máximas respecto al K-2.")]
        public float k2DistanciaM = 300f;
        public float k2VelocidadMS = 2f;
        [Tooltip("Misión 3: punto de observación del cúmulo (m), velocidad máxima (m/s); acercamiento a T-7 (m sobre la superficie, m/s).")]
        public float cumuloMinM = 3000f;
        public float cumuloMaxM = 6000f;
        public float cumuloVelocidadMS = 5f;
        public float t7SuperficieM = 400f;
        public float t7VelocidadMS = 3f;
    }
}
