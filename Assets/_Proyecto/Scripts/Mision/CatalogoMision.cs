using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    // ======================================================================
    // OPCIONES DE CONFIGURACIÓN
    // Todas las cifras por defecto son de DEMOSTRACIÓN: están simplificadas
    // y no proceden de ninguna misión ni de ningún dato real.
    // ======================================================================

    [Serializable]
    public class ObjetivoCientifico
    {
        public string nombre = "Objetivo";

        [TextArea(2, 4)]
        public string descripcion = "";

        [Tooltip("Nombre EXACTO del instrumento más adecuado para este objetivo.")]
        public string instrumentoRecomendado = "";

        [Tooltip("Puntos de interés que hay que escanear. Hay que TRANSMITIRLOS para cumplir el objetivo.")]
        public int escaneosRequeridos = 3;
    }

    [Serializable]
    public class OpcionInstrumento
    {
        public string nombre = "Instrumento";

        [TextArea(2, 4)]
        [Tooltip("Qué cambia al elegir esta opción. Se muestra en la pantalla.")]
        public string queCambia = "";

        [Tooltip("Coste en millones de dólares (M USD). DEMOSTRACIÓN.")]
        public float costeMUSD = 10f;

        [Tooltip("Masa en kilogramos (kg). DEMOSTRACIÓN.")]
        public float masaKg = 10f;

        [Tooltip("Consumo mientras escanea, en vatios (W). Se APLICA a la batería. DEMOSTRACIÓN.")]
        public float consumoEscaneoW = 20f;

        [Tooltip("Datos que genera cada escaneo, en megabytes (MB). Ocupan memoria hasta transmitirlos. DEMOSTRACIÓN.")]
        public float datosPorEscaneoMB = 100f;

        [Tooltip("Segundos reales que dura un escaneo. DEMOSTRACIÓN.")]
        public float segundosEscaneo = 3f;

        [Tooltip("Valor científico de cada escaneo transmitido (puntos). DEMOSTRACIÓN.")]
        public int valorCientifico = 1;
    }

    [Serializable]
    public class OpcionBateria
    {
        public string nombre = "Batería";

        [TextArea(2, 4)]
        public string queCambia = "";

        [Tooltip("Coste en millones de dólares (M USD). DEMOSTRACIÓN.")]
        public float costeMUSD = 5f;

        [Tooltip("Masa en kilogramos (kg). DEMOSTRACIÓN.")]
        public float masaKg = 10f;

        [Tooltip("Energía que guarda, en vatios-hora (Wh). Se APLICA a la sonda. DEMOSTRACIÓN.")]
        public float capacidadWh = 100f;
    }

    [Serializable]
    public class OpcionLanzador
    {
        public string nombre = "Lanzador";

        [TextArea(2, 4)]
        public string queCambia = "";

        [Tooltip("Coste en millones de dólares (M USD). DEMOSTRACIÓN.")]
        public float costeMUSD = 20f;

        [Tooltip("Masa máxima que puede lanzar (sonda + satélite de relevo), en kg. DEMOSTRACIÓN.")]
        public float capacidadCargaKg = 200f;
    }

    [Serializable]
    public class OpcionOrbita
    {
        public string nombre = "Órbita";

        [TextArea(2, 4)]
        public string queCambia = "";

        [Tooltip("Coste en millones de dólares (M USD). DEMOSTRACIÓN.")]
        public float costeMUSD = 5f;

        [Tooltip("Masa del satélite de relevo para esta órbita, en kg. Cuenta para el lanzador. DEMOSTRACIÓN.")]
        public float masaReleKg = 20f;

        [Tooltip("Velocidad del enlace cuando hay contacto, en megabytes por segundo (MB/s). DEMOSTRACIÓN.")]
        public float velocidadEnlaceMBs = 20f;

        [Tooltip("Fracción del tiempo con contacto (0 = nunca, 1 = siempre). Con menos de 1, la transmisión se corta a ratos. DEMOSTRACIÓN.")]
        [Range(0f, 1f)]
        public float fraccionContacto = 1f;
    }

    /// <summary>Índices de las opciones elegidas en cada categoría.</summary>
    [Serializable]
    public struct ConfiguracionElegida
    {
        public int objetivo;
        public int instrumento;
        public int bateria;
        public int lanzador;
        public int orbita;
        public int motorNave;      // componente de la NAVE (no de la sonda)
        public int tanqueNave;     // componente de la NAVE (no de la sonda)
        public int destino;        // índice en CatalogoMision.destinos (0 = el destino de siempre)
    }

    /// <summary>Resultado de comprobar una configuración frente a los límites.</summary>
    public class ResumenMision
    {
        public ObjetivoCientifico objetivo;
        public OpcionInstrumento instrumento;
        public OpcionBateria bateria;
        public OpcionLanzador lanzador;
        public OpcionOrbita orbita;

        public float costeTotalMUSD;
        public float presupuestoMUSD;
        public float masaSondaKg;
        public float cargaCientificaKg;     // sonda + relevo
        public OpcionMotorNave motorNave;   // null si no hay catálogo de nave
        public OpcionTanqueNave tanqueNave; // null si no hay catálogo de nave
        public DefinicionDestino destino;   // null si el catálogo no tiene destinos
        public float masaVariantesNaveKg;   // masa de más de las piezas de la nave no estándar
        public float costeNaveMUSD;         // coste de las piezas de la nave
        /// <summary>Margen de carga configurable USADO en el lanzador: carga científica + variantes de la nave.
        /// El vehículo tripulado base se considera incluido en la capacidad del lanzador (simplificación).</summary>
        public float masaLanzamientoKg;
        public float capacidadLanzadorKg;
        public float energiaWh;
        public float datosMisionMB;
        public float memoriaMB;
        public float enlaceEfectivoMBs;
        public float segundosTransmision;
        public float energiaEscaneosWh;
        public float consumoTransmisionW;
        public bool instrumentoRecomendado;
        public int valorPorEscaneo;

        public readonly List<string> problemas = new List<string>();
        public readonly List<string> avisos = new List<string>();

        public bool PuedeIniciar => problemas.Count == 0;
    }

    // ======================================================================
    // CATÁLOGO: un solo archivo editable desde el Inspector.
    // ======================================================================

    [CreateAssetMenu(fileName = "CatalogoMision", menuName = "Prototipo/Catálogo de misión")]
    public class CatalogoMision : ScriptableObject
    {
        [Header("Escenario (texto de DEMOSTRACIÓN)")]
        [TextArea(2, 4)]
        public string escenario =
            "Escenario de DEMOSTRACIÓN: una sonda submarina explora el océano bajo el hielo de una luna ficticia. " +
            "Ninguna cifra de esta pantalla procede de una misión real.";

        [Header("Misión seleccionable (DEMOSTRACIÓN)")]
        public string nombreMision = "Travesía al hielo";
        public string destinoMision = "Luna helada (ficticia)";
        [TextArea(2, 3)] public string objetivoMision = "Llevar la nave tripulada hasta el destino y entrar en órbita con la carga científica a bordo.";
        public string dificultadMision = "Media";
        public List<string> condicionesMision = new List<string>
        {
            "Una avería eléctrica en ruta: hay que repararla en Ingeniería.",
            "Una zona de escombros: hay que maniobrar desde Pilotaje.",
            "Una frenada de precisión al llegar: fallar gasta combustible.",
            "Presupuesto de 100 M USD y margen de carga del lanzador limitado."
        };
        [Tooltip("Destinos seleccionables antes de la misión (DEMOSTRACIÓN). El primero es el de siempre. " +
                 "Cada uno define nombre, aspecto provisional, ruta, zona de aproximación, condiciones de llegada y gravedad experimental.")]
        public List<DefinicionDestino> destinos = DefinicionDestino.PorDefecto();

        /// <summary>Nombre del destino elegido (o el texto de siempre si no hay lista de destinos).</summary>
        public string NombreDestino(int indice)
        {
            DefinicionDestino d = Elegir(destinos, indice);
            return d != null ? d.nombre : destinoMision;
        }

        [Tooltip("Misiones que solo se muestran como «Próximamente» (sin botón).")]
        public List<string> misionesProximamente = new List<string>
        {
            "Relevo en el anillo exterior — gigante gaseoso (ficticio)",
            "Retorno de muestras — asteroide cercano (ficticio)"
        };

        [Header("Nave tripulada")]
        [Tooltip("Catálogo de la nave (motores, tanques). Sus piezas suman coste y, si no son las estándar, masa al margen de carga.")]
        public CatalogoNave catalogoNave;

        [Header("Límites — DEMOSTRACIÓN")]
        [Tooltip("Presupuesto total de la misión, en millones de dólares (M USD).")]
        public float presupuestoMUSD = 100f;

        [Header("Sonda base: estructura, propulsores, ordenador y comunicaciones — DEMOSTRACIÓN")]
        [Tooltip("Coste de la sonda sin instrumento ni batería, en M USD.")]
        public float costeBaseMUSD = 30f;

        [Tooltip("Masa de la sonda sin instrumento ni batería, en kg.")]
        public float masaBaseKg = 150f;

        [Tooltip("Memoria de a bordo para guardar datos antes de transmitirlos, en MB. Si se llena, no se puede escanear.")]
        public float memoriaMB = 400f;

        [Tooltip("Consumo de las comunicaciones mientras se transmite desde la estación, en vatios (W). DEMOSTRACIÓN.")]
        public float consumoTransmisionW = 25f;

        [Tooltip("Multiplicador del valor científico si el instrumento es el recomendado para el objetivo. DEMOSTRACIÓN.")]
        public float multiplicadorInstrumentoRecomendado = 2f;

        [Header("Puntuación del informe — DEMOSTRACIÓN")]
        [Tooltip("Puntos extra si se cumple el objetivo científico.")]
        public int bonoObjetivoCumplido = 10;

        [Tooltip("Puntos por cada millón de dólares sin gastar. Solo cuenta si se cumple el objetivo.")]
        public float puntosPorMUSDSobrante = 0.5f;

        [Header("Modo equipo — prioridad de cada rol (DEMOSTRACIÓN)")]
        [Tooltip("Ingeniería: Wh que deben quedar en la batería después de la energía estimada para escanear.")]
        public float margenEnergiaMinimoWh = 100f;

        [Tooltip("Lanzamiento: millones de dólares que deben sobrar del presupuesto.")]
        public float reservaPresupuestoMinimaMUSD = 20f;

        [Tooltip("Comunicaciones: segundos reales máximos (media) para transmitir todos los datos de la misión.")]
        public float tiempoTransmisionMaximoS = 20f;

        [Tooltip("Debe coincidir con 'Minutos Mision Por Segundo' de SistemaEnergia. Solo se usa para estimaciones.")]
        public float minutosMisionPorSegundo = 1f;

        [Header("Opciones (se pueden añadir más con el botón +)")]
        public List<ObjetivoCientifico> objetivos = new List<ObjetivoCientifico>
        {
            new ObjetivoCientifico
            {
                nombre = "Cartografiar colonias luminosas",
                descripcion = "Fotografiar formaciones que emiten luz para conocer cómo se distribuyen por el fondo.",
                instrumentoRecomendado = "Cámara multiespectral",
                escaneosRequeridos = 3
            },
            new ObjetivoCientifico
            {
                nombre = "Analizar fuentes termales",
                descripcion = "Medir la composición del agua cerca de respiraderos calientes del fondo.",
                instrumentoRecomendado = "Espectrómetro",
                escaneosRequeridos = 3
            }
        };

        public List<OpcionInstrumento> instrumentos = new List<OpcionInstrumento>
        {
            new OpcionInstrumento
            {
                nombre = "Cámara multiespectral",
                queCambia = "Ligera y barata. Escanea rápido, pero genera muchos datos por escaneo: llena antes la memoria y tarda más en transmitirse.",
                costeMUSD = 12f, masaKg = 8f,
                consumoEscaneoW = 15f, datosPorEscaneoMB = 120f, segundosEscaneo = 3f, valorCientifico = 2
            },
            new OpcionInstrumento
            {
                nombre = "Espectrómetro",
                queCambia = "Más caro, pesado y con mayor consumo. Escaneo lento, pero pocos datos y más valor científico por medición.",
                costeMUSD = 28f, masaKg = 25f,
                consumoEscaneoW = 45f, datosPorEscaneoMB = 60f, segundosEscaneo = 6f, valorCientifico = 4
            }
        };

        public List<OpcionBateria> baterias = new List<OpcionBateria>
        {
            new OpcionBateria
            {
                nombre = "Batería ligera",
                queCambia = "Barata y ligera: la sonda acelera mejor, pero tendrás que volver antes a la estación.",
                costeMUSD = 6f, masaKg = 12f, capacidadWh = 80f
            },
            new OpcionBateria
            {
                nombre = "Batería de alta capacidad",
                queCambia = "Doble de energía para alejarte más, pero cuesta más y pesa casi el triple: la sonda acelera peor y ocupa capacidad del lanzador.",
                costeMUSD = 15f, masaKg = 35f, capacidadWh = 160f
            }
        };

        public List<OpcionLanzador> lanzadores = new List<OpcionLanzador>
        {
            new OpcionLanzador
            {
                nombre = "Lanzador pequeño",
                queCambia = "Barato, pero solo admite cargas ligeras. Obliga a elegir componentes de poca masa.",
                costeMUSD = 20f, capacidadCargaKg = 230f
            },
            new OpcionLanzador
            {
                nombre = "Lanzador mediano",
                queCambia = "Admite casi el doble de masa, pero consume gran parte del presupuesto.",
                costeMUSD = 38f, capacidadCargaKg = 450f
            }
        };

        public List<OpcionOrbita> orbitas = new List<OpcionOrbita>
        {
            new OpcionOrbita
            {
                nombre = "Relevo en órbita baja",
                queCambia = "Satélite de relevo ligero y barato. Enlace rápido, pero solo hay contacto la mitad del tiempo.",
                costeMUSD = 4f, masaReleKg = 20f, velocidadEnlaceMBs = 40f, fraccionContacto = 0.5f
            },
            new OpcionOrbita
            {
                nombre = "Relevo en órbita alta",
                queCambia = "Contacto continuo, pero el enlace es más lento y el satélite necesita más combustible: pesa más en el lanzador.",
                costeMUSD = 8f, masaReleKg = 45f, velocidadEnlaceMBs = 15f, fraccionContacto = 1f
            }
        };

        /// <summary>Calcula totales y comprueba límites para una elección.</summary>
        public ResumenMision Calcular(ConfiguracionElegida e)
        {
            ResumenMision r = new ResumenMision
            {
                objetivo = Elegir(objetivos, e.objetivo),
                instrumento = Elegir(instrumentos, e.instrumento),
                bateria = Elegir(baterias, e.bateria),
                destino = Elegir(destinos, e.destino),
                lanzador = Elegir(lanzadores, e.lanzador),
                orbita = Elegir(orbitas, e.orbita),
                presupuestoMUSD = presupuestoMUSD,
                memoriaMB = memoriaMB
            };

            if (r.objetivo == null) r.problemas.Add("El catálogo no tiene objetivos.");
            if (r.instrumento == null) r.problemas.Add("El catálogo no tiene instrumentos.");
            if (r.bateria == null) r.problemas.Add("El catálogo no tiene baterías.");
            if (r.lanzador == null) r.problemas.Add("El catálogo no tiene lanzadores.");
            if (r.orbita == null) r.problemas.Add("El catálogo no tiene órbitas.");
            if (r.problemas.Count > 0) return r;

            r.costeTotalMUSD = costeBaseMUSD + r.instrumento.costeMUSD + r.bateria.costeMUSD
                             + r.lanzador.costeMUSD + r.orbita.costeMUSD;
            r.masaSondaKg = masaBaseKg + r.instrumento.masaKg + r.bateria.masaKg;
            r.cargaCientificaKg = r.masaSondaKg + r.orbita.masaReleKg;

            // Piezas de la nave tripulada (catálogo propio, no campos de la sonda).
            if (catalogoNave != null)
            {
                r.motorNave = catalogoNave.Motor(e.motorNave);
                if (r.motorNave != null)
                {
                    r.costeNaveMUSD += r.motorNave.costeMUSD;
                    r.masaVariantesNaveKg += catalogoNave.MasaExtraMotor(r.motorNave);
                }
                r.tanqueNave = catalogoNave.Tanque(e.tanqueNave);
                if (r.tanqueNave != null)
                {
                    r.costeNaveMUSD += r.tanqueNave.costeMUSD;
                    r.masaVariantesNaveKg += catalogoNave.MasaExtraTanque(r.tanqueNave);
                }
            }
            r.costeTotalMUSD += r.costeNaveMUSD;
            r.masaLanzamientoKg = r.cargaCientificaKg + r.masaVariantesNaveKg;
            r.capacidadLanzadorKg = r.lanzador.capacidadCargaKg;
            r.energiaWh = r.bateria.capacidadWh;
            r.datosMisionMB = r.objetivo.escaneosRequeridos * r.instrumento.datosPorEscaneoMB;
            r.enlaceEfectivoMBs = r.orbita.velocidadEnlaceMBs * Mathf.Clamp01(r.orbita.fraccionContacto);
            r.segundosTransmision = r.enlaceEfectivoMBs > 0f ? r.datosMisionMB / r.enlaceEfectivoMBs : float.PositiveInfinity;

            // Wh = W × horas de misión; horas = segundos reales × (min/s) / 60.
            float horasEscaneo = r.objetivo.escaneosRequeridos * r.instrumento.segundosEscaneo * minutosMisionPorSegundo / 60f;
            r.energiaEscaneosWh = r.instrumento.consumoEscaneoW * horasEscaneo;

            r.consumoTransmisionW = consumoTransmisionW;
            r.instrumentoRecomendado =
                string.IsNullOrWhiteSpace(r.objetivo.instrumentoRecomendado) ||
                string.Equals(r.objetivo.instrumentoRecomendado.Trim(), r.instrumento.nombre.Trim(), StringComparison.OrdinalIgnoreCase);
            float mult = r.instrumentoRecomendado ? Mathf.Max(1f, multiplicadorInstrumentoRecomendado) : 1f;
            r.valorPorEscaneo = Mathf.RoundToInt(r.instrumento.valorCientifico * mult);

            // --- Límites que BLOQUEAN el inicio ---
            // Configuraciones con las que la misión no podría completarse nunca
            // (solo pueden aparecer si se editan los valores del catálogo).
            if (r.objetivo.escaneosRequeridos < 1)
                r.problemas.Add("El objetivo debe pedir al menos 1 escaneo. Revisa «Escaneos requeridos» en el catálogo.");

            if (r.instrumento.datosPorEscaneoMB > memoriaMB)
                r.problemas.Add(
                    $"La memoria ({memoriaMB:0} MB) no admite ni un escaneo de {r.instrumento.nombre} " +
                    $"({r.instrumento.datosPorEscaneoMB:0} MB): la sonda no podría guardar datos.");

            if (r.enlaceEfectivoMBs <= 0f)
                r.problemas.Add(
                    $"{r.orbita.nombre} no tiene enlace (velocidad o contacto en 0): los datos nunca se transmitirían.");

            if (r.costeTotalMUSD > presupuestoMUSD)
                r.problemas.Add(
                    $"Coste total {r.costeTotalMUSD:0} M USD: supera el presupuesto de {presupuestoMUSD:0} M USD " +
                    $"por {r.costeTotalMUSD - presupuestoMUSD:0}. Elige opciones más baratas.");

            if (r.masaLanzamientoKg > r.capacidadLanzadorKg)
                r.problemas.Add(
                    $"Margen de carga configurable superado: {r.masaLanzamientoKg:0} de {r.capacidadLanzadorKg:0} kg en el " +
                    $"{r.lanzador.nombre} (carga científica {r.cargaCientificaKg:0} kg + piezas de la nave no estándar " +
                    $"{r.masaVariantesNaveKg:0} kg). Elige un lanzador mayor o piezas más ligeras.");

            // --- Avisos que NO bloquean ---
            if (!r.instrumentoRecomendado)
                r.avisos.Add(
                    $"Para este objetivo se recomienda «{r.objetivo.instrumentoRecomendado}». " +
                    $"Con el instrumento elegido cada escaneo vale {r.valorPorEscaneo} puntos; " +
                    $"con el recomendado, su valor se multiplica ×{multiplicadorInstrumentoRecomendado:0.#}.");

            if (r.datosMisionMB > memoriaMB)
                r.avisos.Add(
                    $"Los datos previstos ({r.datosMisionMB:0} MB) superan la memoria ({memoriaMB:0} MB): " +
                    "tendrás que volver a la estación a transmitir antes de terminar.");

            if (r.energiaEscaneosWh > r.energiaWh * 0.5f)
                r.avisos.Add("Escanear consumirá más de la mitad de la batería.");

            return r;
        }

        static T Elegir<T>(List<T> lista, int indice) where T : class
        {
            if (lista == null || lista.Count == 0) return null;
            return lista[Mathf.Clamp(indice, 0, lista.Count - 1)];
        }
    }
}
