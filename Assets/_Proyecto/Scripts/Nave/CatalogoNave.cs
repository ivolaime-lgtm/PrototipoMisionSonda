using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>Tanque de combustible del módulo de servicio de la nave (DEMOSTRACIÓN). No es un componente de la sonda.</summary>
    [Serializable]
    public class OpcionTanqueNave
    {
        public string nombre = "Tanque";
        [TextArea(1, 3)] public string queCambia = "";
        [Tooltip("Coste, en M USD. Se suma al coste total de la misión.")]
        public float costeMUSD;
        [Tooltip("Masa del tanque vacío, en kg. Forma parte de la masa seca de la nave.")]
        public float masaVaciaKg = 150f;
        [Tooltip("Combustible máximo que cabe, en kg. El combustible inicial nunca lo supera.")]
        public float capacidadKg = 350f;
        [Header("Aspecto en la vista exterior")]
        public float largo = 3.2f;
        public float diametro = 2.0f;
        public Color color = new Color(0.5f, 0.55f, 0.62f);
    }

    /// <summary>Motor del módulo de servicio de la nave tripulada (DEMOSTRACIÓN). No es un componente de la sonda.</summary>
    [Serializable]
    public class OpcionMotorNave
    {
        public string nombre = "Motor";
        [TextArea(1, 3)] public string queCambia = "";
        [Tooltip("Coste, en M USD. Se suma al coste total de la misión.")]
        public float costeMUSD;
        [Tooltip("Masa del motor, en kg. Forma parte de la masa seca de la nave.")]
        public float masaKg = 400f;
        [Tooltip("Empuje de los propulsores de maniobra, en newtons (N).")]
        public float empujeN = 26000f;
        [Tooltip("Combustible que gastan a pleno empuje, en kg por segundo real.")]
        public float consumoKgS = 2.5f;
        [Header("Aspecto en la vista exterior")]
        [Tooltip("Tamaño de la tobera principal (1 = estándar).")]
        public float tamanoTobera = 1f;
        [Tooltip("Toberas auxiliares alrededor de la principal.")]
        public int toberasAuxiliares;
        public Color color = new Color(0.35f, 0.37f, 0.4f);
    }

    /// <summary>
    /// Datos de la NAVE TRIPULADA FICTICIA, separados de los de la sonda.
    /// Se editan en el asset Data/CatalogoNave desde el Inspector. Todo es DEMOSTRACIÓN.
    ///
    /// Reglas de demostración que relacionan la nave con la configuración de la misión:
    /// 1. La carga científica (sonda + satélite de relevo) viaja a bordo de la nave.
    /// 2. La capacidad que sobra en el lanzador elegido se llena con combustible extra
    ///    para la nave (kgCombustiblePorKgMargenLanzador kg por cada kg sobrante).
    /// 3. Masa total de la nave = masa seca + combustible + carga científica.
    /// 4. Aceleración de maniobra = empuje / masa total (más masa, menos agilidad).
    /// 5. Combustible para entrar en órbita = base × (masa actual / masa de referencia),
    ///    más un sobrecoste si se frena fuera del momento justo.
    /// La batería de la sonda NO es la energía de la nave: la nave tiene la suya.
    /// </summary>
    [CreateAssetMenu(fileName = "CatalogoNave", menuName = "Prototipo/Catálogo de la nave")]
    public class CatalogoNave : ScriptableObject
    {
        [TextArea(2, 4)]
        public string aviso =
            "DEMOSTRACIÓN: nave tripulada ficticia y viaje comprimido. No representa una misión real " +
            "ni la duración real de un viaje a una luna helada.";

        [Header("Nave (independiente de la sonda) — DEMOSTRACIÓN")]
        [Tooltip("Masa del casco, módulo de tripulación y sistemas, SIN motor ni tanque, en kg. " +
                 "Masa seca de la nave = esta masa + motor + tanque vacío.")]
        public float masaCascoKg = 5450f;

        [Header("Tanques (el primero es el estándar de referencia) — DEMOSTRACIÓN")]
        public List<OpcionTanqueNave> tanques = new List<OpcionTanqueNave>
        {
            new OpcionTanqueNave
            {
                nombre = "Tanque estándar",
                queCambia = "Hasta 350 kg de combustible. Es la referencia: no añade masa al margen de carga.",
                costeMUSD = 0f, masaVaciaKg = 150f, capacidadKg = 350f,
                largo = 3.2f, diametro = 2.0f, color = new Color(0.5f, 0.55f, 0.62f)
            },
            new OpcionTanqueNave
            {
                nombre = "Tanque ampliado",
                queCambia = "Hasta 600 kg de combustible, pero pesa 50 kg más vacío: necesita margen de carga en el lanzador.",
                costeMUSD = 6f, masaVaciaKg = 200f, capacidadKg = 600f,
                largo = 4.4f, diametro = 2.3f, color = new Color(0.62f, 0.6f, 0.5f)
            }
        };

        [Header("Motores (el primero es el estándar de referencia) — DEMOSTRACIÓN")]
        public List<OpcionMotorNave> motores = new List<OpcionMotorNave>
        {
            new OpcionMotorNave
            {
                nombre = "Motor estándar",
                queCambia = "Empuje moderado y consumo contenido. Es la referencia: no añade masa al margen de carga.",
                costeMUSD = 0f, masaKg = 400f, empujeN = 26000f, consumoKgS = 2.5f,
                tamanoTobera = 1f, toberasAuxiliares = 0, color = new Color(0.35f, 0.37f, 0.4f)
            },
            new OpcionMotorNave
            {
                nombre = "Motor de alto empuje",
                queCambia = "Más empuje: la nave acelera más al esquivar escombros, pero gasta más combustible por segundo y pesa 30 kg más.",
                costeMUSD = 8f, masaKg = 430f, empujeN = 36000f, consumoKgS = 3.4f,
                tamanoTobera = 1.35f, toberasAuxiliares = 4, color = new Color(0.55f, 0.36f, 0.22f)
            }
        };

        [Tooltip("Combustible que lleva siempre la nave, en kg.")]
        public float combustibleBaseKg = 300f;

        [Tooltip("Regla de demostración: kg de combustible extra por cada kg de capacidad sobrante del lanzador.")]
        public float kgCombustiblePorKgMargenLanzador = 1f;

        [Tooltip("Energía de los sistemas de la nave, en Wh. Es distinta de la batería de la sonda.")]
        public float energiaNaveWh = 1000f;

        [Tooltip("Integridad del casco al despegar, en %. Si llega a 0, la misión se aborta.")]
        public float integridadInicial = 100f;

        [Header("Maniobras laterales (puesto de pilotaje)")]
        [Tooltip("Solo se usa si la lista de motores está vacía.")]
        public float empujeManiobraN = 26000f;

        [Tooltip("Solo se usa si la lista de motores está vacía.")]
        public float consumoManiobraKgS = 2.5f;

        [Tooltip("Desplazamiento lateral máximo respecto a la ruta, en metros.")]
        public float limiteDesplazamientoM = 14f;

        [Tooltip("Radio de choque de la nave, en metros (medio ancho de la cabina).")]
        public float radioNaveM = 4f;

        [Header("Entrada en órbita (llegada)")]
        [Tooltip("Combustible para frenar y quedar en órbita con una frenada perfecta y la masa de referencia, en kg.")]
        public float combustibleInsercionKg = 180f;

        [Tooltip("Masa de referencia para el coste de la inserción, en kg.")]
        public float masaReferenciaKg = 6500f;

        [Tooltip("Sobrecoste máximo de la inserción si se frena en el peor momento (0,6 = +60 %).")]
        public float sobrecosteMaximoInsercion = 0.4f;

        [Tooltip("Error de tiempo (0 a 0,5) que se considera zona verde.")]
        public float zonaVerdeInsercion = 0.08f;

        [Tooltip("Regla simplificada: con una frenada imprecisa, la órbita queda más lejos del punto de acceso y el " +
                 "descenso se prolonga; la sonda llega con menos carga. Fracción máxima de reducción (0,1 = 10 %).")]
        [Range(0f, 0.1f)]
        public float reduccionMaximaCargaSonda = 0.1f;

        [Header("Puntos de viaje — DEMOSTRACIÓN")]
        [Tooltip("Reparación: fracción de energía de la nave perdida para obtener los puntos máximos / intermedios.")]
        public float perdidaEnergiaBuena = 0.25f;
        public float perdidaEnergiaRegular = 0.5f;
        public int puntosReparacionBuena = 5;
        public int puntosReparacionRegular = 2;
        public int puntosSinImpactos = 6;
        public int puntosUnImpacto = 3;

        [Header("Escombros (peligro FICTICIO localizado)")]
        [Tooltip("Daño al casco por cada impacto, en %.")]
        public float danoPorImpacto = 25f;

        [Tooltip("Velocidad con la que se acercan los escombros, en m/s.")]
        public float velocidadEscombrosMS = 45f;

        [Tooltip("Segundos reales entre escombros.")]
        public float intervaloEscombrosS = 1.5f;

        [Tooltip("Fracción de escombros que vienen directos hacia la nave.")]
        [Range(0f, 1f)]
        public float fraccionEscombrosApuntados = 0.6f;

        [Header("Avería eléctrica (puesto de ingeniería)")]
        [Tooltip("Energía de la nave que se pierde por segundo real mientras dura la avería, en Wh.")]
        public float perdidaAveriaWhPorS = 12f;

        [Tooltip("Energía que se pierde por cada paso equivocado al reparar, en Wh.")]
        public float perdidaErrorAveriaWh = 40f;

        public OpcionMotorNave Motor(int indice)
        {
            if (motores == null || motores.Count == 0) return null;
            return motores[Mathf.Clamp(indice, 0, motores.Count - 1)];
        }

        public OpcionTanqueNave Tanque(int indice)
        {
            if (tanques == null || tanques.Count == 0) return null;
            return tanques[Mathf.Clamp(indice, 0, tanques.Count - 1)];
        }

        /// <summary>Masa en vacío que el tanque añade sobre el estándar (la que cuenta en el margen de carga del lanzador).</summary>
        public float MasaExtraTanque(OpcionTanqueNave t)
        {
            OpcionTanqueNave referencia = Tanque(0);
            if (t == null || referencia == null) return 0f;
            return Mathf.Max(0f, t.masaVaciaKg - referencia.masaVaciaKg);
        }

        /// <summary>Masa que el motor añade sobre el estándar (la que cuenta en el margen de carga del lanzador).</summary>
        public float MasaExtraMotor(OpcionMotorNave m)
        {
            OpcionMotorNave referencia = Motor(0);
            if (m == null || referencia == null) return 0f;
            return Mathf.Max(0f, m.masaKg - referencia.masaKg);
        }

        /// <summary>
        /// Calcula los datos de la nave para una configuración de misión.
        /// Categorías de masa (sin contar nada dos veces):
        ///   masa seca = casco + motor + tanque vacío
        ///   carga científica = sonda + relevo (una sola vez)
        ///   combustible = mínimo(capacidad del tanque, base + margen de carga sobrante del lanzador)
        ///   (el combustible solo sale de esta regla: el tanque pone el tope, no añade combustible)
        ///   masa total inicial = masa seca + carga científica + combustible
        /// </summary>
        public ResumenNave Calcular(ResumenMision mision)
        {
            ResumenNave n = new ResumenNave();
            n.motor = mision != null && mision.motorNave != null ? mision.motorNave : Motor(0);
            n.empujeN = n.motor != null ? n.motor.empujeN : empujeManiobraN;
            n.consumoKgS = n.motor != null ? n.motor.consumoKgS : consumoManiobraKgS;
            n.masaMotorKg = n.motor != null ? n.motor.masaKg : 0f;
            n.tanque = mision != null && mision.tanqueNave != null ? mision.tanqueNave : Tanque(0);
            n.masaTanqueKg = n.tanque != null ? n.tanque.masaVaciaKg : 0f;
            n.capacidadTanqueKg = n.tanque != null ? n.tanque.capacidadKg : float.PositiveInfinity;

            if (mision != null)
            {
                n.cargaCientificaKg = mision.cargaCientificaKg;            // sonda + relevo
                n.cargaLanzadorKg = mision.masaLanzamientoKg;              // margen de carga usado
                n.capacidadLanzadorKg = mision.capacidadLanzadorKg;
            }
            // Regla de demostración: la capacidad del margen de carga que no se usa se llena con
            // un depósito auxiliar de propelente para la nave (1 kg de capacidad → 1 kg de combustible).
            n.margenLanzadorKg = Mathf.Max(0f, n.capacidadLanzadorKg - n.cargaLanzadorKg);
            float disponible = combustibleBaseKg + n.margenLanzadorKg * kgCombustiblePorKgMargenLanzador;
            n.combustibleInicialKg = Mathf.Min(disponible, n.capacidadTanqueKg);           // nunca supera el tanque
            n.combustibleNoCabeKg = Mathf.Max(0f, disponible - n.combustibleInicialKg);
            n.combustibleBaseKg = Mathf.Min(combustibleBaseKg, n.combustibleInicialKg);
            n.combustibleExtraKg = n.combustibleInicialKg - n.combustibleBaseKg;
            n.masaCascoKg = masaCascoKg;
            n.masaSecaKg = masaCascoKg + n.masaMotorKg + n.masaTanqueKg;
            n.masaInicialKg = n.masaSecaKg + n.cargaCientificaKg + n.combustibleInicialKg;
            n.aceleracionInicialMS2 = n.masaInicialKg > 0f ? n.empujeN / n.masaInicialKg : 0f;
            n.energiaWh = energiaNaveWh;
            return n;
        }

        /// <summary>
        /// Fracción de carga que pierde la sonda por una frenada imprecisa (0 en la zona verde,
        /// hasta reduccionMaximaCargaSonda en el peor momento). Nunca supera el 10 %.
        /// </summary>
        public float ReduccionCargaSonda(float error)
        {
            if (error <= zonaVerdeInsercion) return 0f;
            float t = Mathf.Clamp01((error - zonaVerdeInsercion) / Mathf.Max(0.0001f, 0.5f - zonaVerdeInsercion));
            return Mathf.Clamp(t * reduccionMaximaCargaSonda, 0f, 0.1f);
        }

        public int PuntosReparacion(float fraccionEnergiaPerdida)
        {
            if (fraccionEnergiaPerdida <= perdidaEnergiaBuena) return puntosReparacionBuena;
            if (fraccionEnergiaPerdida <= perdidaEnergiaRegular) return puntosReparacionRegular;
            return 0;
        }

        public int PuntosEscombros(int impactos)
        {
            if (impactos <= 0) return puntosSinImpactos;
            if (impactos == 1) return puntosUnImpacto;
            return 0;
        }

        /// <summary>Combustible para entrar en órbita con una masa y un error de tiempo (0 a 0,5).</summary>
        public float CosteInsercion(float masaActualKg, float error)
        {
            float factorMasa = masaReferenciaKg > 0f ? masaActualKg / masaReferenciaKg : 1f;
            float sobrecoste = Mathf.Clamp01(error / 0.5f) * sobrecosteMaximoInsercion;
            return combustibleInsercionKg * factorMasa * (1f + sobrecoste);
        }
    }

    /// <summary>Datos calculados de la nave para la configuración elegida.</summary>
    public class ResumenNave
    {
        public OpcionMotorNave motor;
        public OpcionTanqueNave tanque;
        public float empujeN, consumoKgS, masaMotorKg, masaCascoKg, masaTanqueKg, capacidadTanqueKg;
        /// <summary>Combustible que el margen permitiría cargar pero no cabe en el tanque.</summary>
        public float combustibleNoCabeKg;
        public float cargaLanzadorKg;
        public float cargaCientificaKg;
        public float capacidadLanzadorKg;
        public float margenLanzadorKg;
        public float combustibleBaseKg;
        public float combustibleExtraKg;
        public float combustibleInicialKg;
        public float masaSecaKg;
        public float masaInicialKg;
        public float aceleracionInicialMS2;
        public float energiaWh;
    }
}
