using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    // =====================================================================================================================
    // BALANCE PROVISIONAL DE LA EM-4X y PREPARACIÓN DE LA EXPEDICIÓN (solo datos y cálculo; nada visual ni de controles).
    //
    // Todo es DEMOSTRACIÓN: valores ficticios elegidos para que las tres misiones del MVP se puedan jugar. No representan
    // una nave real. NO usa los 5450 kg del casco de la nave anterior ni modifica CatalogoNave ni CatalogoMision.
    //
    // Qué se reutiliza de los catálogos existentes (sin cambiarlos):
    //   · Presupuesto (CatalogoMision.presupuestoMUSD), instrumentos, relevos (lista «orbitas»), memoria de datos,
    //     coste y masa de la sonda base, consumo de transmisión y la regla «1 s real = N minutos de misión» para la energía.
    //   · Nombres y PRECIOS del motor y del tanque no estándar (CatalogoNave). Sus empujes, masas y consumos están pensados
    //     para una nave de 7 t y no sirven para la EM-4X: aquí se definen versiones EM-4X con la misma estructura de datos.
    //
    // Pendiente como ingeniería del mundo (no se resuelve aquí): transporte y construcción en órbita del nodo de 10 m.
    // =====================================================================================================================

    /// <summary>Elecciones del jugador en la preparación. Índices sobre las listas de BalanceEM4X y de CatalogoMision.</summary>
    [Serializable]
    public class PreparacionExpedicion
    {
        public int mision;
        public int motor;            // 0 = instalado
        public int tanque;           // 0 = instalado
        public int instrumento;      // 0 = instalado (CatalogoMision.instrumentos[0])
        public int relevo;           // 0 = instalado (CatalogoMision.orbitas[0])
        public bool sonda;           // sonda de despliegue (necesaria en la misión 3)
        public int tramosCombustible = 4;   // tramos cargados desde tierra, además del remanente a bordo
        public int suministrosExtra;        // unidades además de las que ya hay a bordo
        public int kitsExtra = 1;           // kits además del que ya hay a bordo

        public PreparacionExpedicion Copia() => (PreparacionExpedicion)MemberwiseClone();
    }

    public class LineaPresupuesto
    {
        public string partida, concepto;
        public float costeMUSD;
    }

    /// <summary>Resultado de calcular una preparación: equipos, masas, prestaciones, presupuesto, problemas y avisos.</summary>
    public class ResumenExpedicion
    {
        public PreparacionExpedicion eleccion;
        public DefinicionMisionMVP mision;
        public OpcionMotorNave motor;
        public OpcionTanqueNave tanque;
        public OpcionInstrumento instrumento;
        public OpcionOrbita relevo;
        public bool instrumentoRecomendado;
        public bool sonda;

        public float masaEstructuraKg, masaSecaKg, cargaKg, combustibleKg, capacidadKg, masaTotalKg;
        public float aceleracionMS2, aceleracionManiobraMS2, deltaVMS;
        public int kits, suministros;
        public float soporteVitalS;
        public float memoriaMB;

        public readonly List<LineaPresupuesto> lineas = new List<LineaPresupuesto>();
        public float costeTotalMUSD, presupuestoMUSD;
        public int sustituciones;
        public bool enviaDesdeTierra;
        public readonly List<string> enviado = new List<string>();
        public readonly List<string> problemas = new List<string>();
        public readonly List<string> avisos = new List<string>();
        public bool PuedeConfirmar => problemas.Count == 0;
    }

    public static class BalanceEM4X
    {
        // ------------------------------------------------------------------ Masas (kg) — DEMOSTRACIÓN
        /// <summary>Estructura, módulos y sistemas de la EM-4X SIN motor ni tanque.</summary>
        public const float MasaEstructuraKg = 96000f;
        public const float MasaKitKg = 40f;
        public const float MasaSuministroKg = 120f;

        // ------------------------------------------------------------------ Combustible — DEMOSTRACIÓN
        /// <summary>Combustible que ya está a bordo al empezar (no se cobra ni se transporta).</summary>
        public const float CombustibleRemanenteKg = 6000f;
        public const float TramoCombustibleKg = 3000f;
        public const float CosteTramoCombustibleMUSD = 1.2f;

        // ------------------------------------------------------------------ Maniobra — DEMOSTRACIÓN
        public static AjustesManiobra Maniobra => new AjustesManiobra
        {
            empujeTraslacionN = 60000f,
            consumoTraslacionKgS = 20f,
            aceleracionAngularGradS2 = new Vector3(6f, 6f, 8f),
            consumoOrientacionKgS = 1.5f,
            velocidadAngularMaximaGradS = 30f,
            estabilizacionInicial = true
        };

        // ------------------------------------------------------------------ Tarifas (M USD) — DEMOSTRACIÓN, configurables aquí
        /// <summary>Cargo fijo si se envía cualquier pieza o carga desde tierra (un solo envío, se cobra una vez).</summary>
        public const float TarifaTransporteOrbitalMUSD = 10f;
        /// <summary>Por cada componente sustituido en la nave (motor, tanque, instrumento, relevo).</summary>
        public const float TarifaInstalacionMUSD = 3f;
        /// <summary>Lanzamiento de la tripulación en la lanzadera (configuración fija).</summary>
        public const float TarifaLanzamientoTripulacionMUSD = 18f;
        public const float CosteKitMUSD = 1.5f;
        public const float CosteSuministroMUSD = 1f;

        // ------------------------------------------------------------------ Energía — DEMOSTRACIÓN
        public const float BateriaWh = 12000f;
        public const float GeneracionW = 4000f;
        public const float ConsumoBaseW = 3600f;
        public const float ConsumoBusCienciaW = 800f;
        public const float ConsumoBusComunicacionesW = 600f;
        public const float ConsumoBusServicioW = 1500f;
        /// <summary>Los consumos de instrumento y transmisión del catálogo son de una sonda pequeña: en la EM-4X se multiplican.</summary>
        public const float FactorConsumoCatalogo = 100f;

        // ------------------------------------------------------------------ Soporte vital y kits — DEMOSTRACIÓN
        public const int SuministrosABordo = 2;
        public const int KitsABordo = 1;
        /// <summary>Segundos de vuelo que cubre cada unidad de suministros en condiciones normales.</summary>
        public const float SegundosPorSuministro = 1200f;
        public const int MaximoSuministrosExtra = 4;
        public const int MaximoKitsExtra = 3;

        // ------------------------------------------------------------------ Daño — DEMOSTRACIÓN
        public const float RadioNaveM = 32f;
        /// <summary>Por debajo de esta velocidad relativa (m/s) un contacto es un roce sin daño.</summary>
        public const float VelocidadRoceMS = 2f;
        /// <summary>Desde esta velocidad relativa (m/s) el impacto abre una brecha reparable.</summary>
        public const float VelocidadBrechaMS = 4f;
        /// <summary>Desde esta velocidad la brecha es grave (fuga rápida y un sistema protegido).</summary>
        public const float VelocidadGraveMS = 10f;
        /// <summary>Desde esta velocidad el impacto es catastrófico (fin de la misión).</summary>
        public const float VelocidadCatastroficaMS = 25f;
        public const float DanoPorMS = 1.6f;            // % de integridad por cada m/s de impacto
        public const float FugaLeve = 3f;               // multiplica el gasto de soporte vital mientras hay brecha
        public const float FugaGrave = 8f;
        public const float FugaAislada = 0.2f;          // fracción de la fuga que queda con el módulo aislado
        public const float SegundosReparacion = 8f;
        public const float IntegridadRecuperadaKit = 10f;
        public const float SoporteCriticoFraccion = 0.15f;

        // ------------------------------------------------------------------ Opciones EM-4X (estructura de CatalogoNave)

        public static List<OpcionMotorNave> Motores(CatalogoNave cat)
        {
            OpcionMotorNave c0 = cat != null ? cat.Motor(0) : null, c1 = cat != null ? cat.Motor(1) : null;
            return new List<OpcionMotorNave>
            {
                new OpcionMotorNave
                {
                    nombre = (c0 != null ? c0.nombre : "Motor estándar") + " · EM-4X",
                    queCambia = "INSTALADO. 400 kN y 100 kg/s: el más eficiente (más cambio de velocidad por kg de combustible).",
                    costeMUSD = 0f, masaKg = 3000f, empujeN = 400000f, consumoKgS = 100f
                },
                new OpcionMotorNave
                {
                    nombre = (c1 != null && c1 != c0 ? c1.nombre : "Motor de alto empuje") + " · EM-4X",
                    queCambia = "560 kN: acelera y frena un 40 % más rápido, pero gasta 155 kg/s (menos eficiente) y pesa 600 kg más.",
                    costeMUSD = c1 != null && c1 != c0 ? c1.costeMUSD : 8f, masaKg = 3600f, empujeN = 560000f, consumoKgS = 155f
                }
            };
        }

        public static List<OpcionTanqueNave> Tanques(CatalogoNave cat)
        {
            OpcionTanqueNave c0 = cat != null ? cat.Tanque(0) : null, c1 = cat != null ? cat.Tanque(1) : null;
            return new List<OpcionTanqueNave>
            {
                new OpcionTanqueNave
                {
                    nombre = (c0 != null ? c0.nombre : "Tanque estándar") + " · EM-4X",
                    queCambia = "INSTALADO. Caben 24 000 kg de combustible.",
                    costeMUSD = 0f, masaVaciaKg = 2500f, capacidadKg = 24000f
                },
                new OpcionTanqueNave
                {
                    nombre = (c1 != null && c1 != c0 ? c1.nombre : "Tanque ampliado") + " · EM-4X",
                    queCambia = "Caben 36 000 kg, pero pesa 1100 kg más vacío: con la misma carga de combustible la nave acelera menos.",
                    costeMUSD = c1 != null && c1 != c0 ? c1.costeMUSD : 6f, masaVaciaKg = 3600f, capacidadKg = 36000f
                }
            };
        }

        static T Elegir<T>(List<T> lista, int i) where T : class
        {
            if (lista == null || lista.Count == 0) return null;
            return lista[Mathf.Clamp(i, 0, lista.Count - 1)];
        }

        public static int MaximoTramos(OpcionTanqueNave t) =>
            t == null ? 0 : Mathf.FloorToInt((t.capacidadKg - CombustibleRemanenteKg) / TramoCombustibleKg + 0.001f);

        /// <summary>Calcula la preparación completa. No descuenta dinero: es el presupuesto PREVISTO de esta elección.</summary>
        public static ResumenExpedicion Calcular(PreparacionExpedicion p, CatalogoMision cat)
        {
            ResumenExpedicion r = new ResumenExpedicion { eleccion = p };
            CatalogoNave catNave = cat != null ? cat.catalogoNave : null;
            List<DefinicionMisionMVP> misiones = DefinicionMisionMVP.Todas();
            r.mision = misiones[Mathf.Clamp(p.mision, 0, misiones.Count - 1)];
            r.motor = Elegir(Motores(catNave), p.motor);
            r.tanque = Elegir(Tanques(catNave), p.tanque);
            r.instrumento = cat != null ? Elegir(cat.instrumentos, p.instrumento) : null;
            r.relevo = cat != null ? Elegir(cat.orbitas, p.relevo) : null;
            r.presupuestoMUSD = cat != null ? cat.presupuestoMUSD : 100f;
            r.memoriaMB = cat != null ? cat.memoriaMB : 400f;
            r.sonda = p.sonda;
            if (r.instrumento == null) r.problemas.Add("El catálogo de misión no tiene instrumentos.");
            if (r.relevo == null) r.problemas.Add("El catálogo de misión no tiene relevos de comunicaciones.");
            if (r.problemas.Count > 0) return r;

            r.instrumentoRecomendado = string.Equals(r.mision.instrumentoRecomendado.Trim(), r.instrumento.nombre.Trim(), StringComparison.OrdinalIgnoreCase);

            // ---- Combustible: capacidad del tanque frente a combustible cargado
            int tramos = Mathf.Clamp(p.tramosCombustible, 0, MaximoTramos(r.tanque));
            r.capacidadKg = r.tanque.capacidadKg;
            r.combustibleKg = Mathf.Min(r.capacidadKg, CombustibleRemanenteKg + tramos * TramoCombustibleKg);

            // ---- Carga y masas
            int kitsExtra = Mathf.Clamp(p.kitsExtra, 0, MaximoKitsExtra);
            int sumExtra = Mathf.Clamp(p.suministrosExtra, 0, MaximoSuministrosExtra);
            r.kits = KitsABordo + kitsExtra;
            r.suministros = SuministrosABordo + sumExtra;
            r.soporteVitalS = r.suministros * SegundosPorSuministro;
            float masaSonda = p.sonda ? (cat != null ? cat.masaBaseKg : 150f) : 0f;
            r.cargaKg = r.instrumento.masaKg + r.relevo.masaReleKg + masaSonda + r.kits * MasaKitKg + r.suministros * MasaSuministroKg;
            r.masaEstructuraKg = MasaEstructuraKg;
            r.masaSecaKg = MasaEstructuraKg + r.motor.masaKg + r.tanque.masaVaciaKg + r.cargaKg;
            r.masaTotalKg = r.masaSecaKg + r.combustibleKg;
            r.aceleracionMS2 = r.motor.empujeN / r.masaTotalKg;
            r.aceleracionManiobraMS2 = Maniobra.empujeTraslacionN / r.masaTotalKg;
            float ve = r.motor.consumoKgS > 0f ? r.motor.empujeN / r.motor.consumoKgS : 0f;
            r.deltaVMS = ve * Mathf.Log(r.masaTotalKg / r.masaSecaKg);

            // ---- Presupuesto por partidas (previsto; se calcula entero cada vez, nunca se acumula)
            void Linea(string partida, string concepto, float coste) =>
                r.lineas.Add(new LineaPresupuesto { partida = partida, concepto = concepto, costeMUSD = coste });

            Linea("Ya instalado en órbita", "EM-4X, motor y tanque estándar, " + Elegir(cat.instrumentos, 0).nombre + ", " + Elegir(cat.orbitas, 0).nombre +
                  $", {CombustibleRemanenteKg:0} kg de combustible, {KitsABordo} kit y {SuministrosABordo} suministros", 0f);

            if (p.motor > 0) { Linea("Compras y sustituciones", r.motor.nombre, r.motor.costeMUSD); r.sustituciones++; r.enviado.Add(r.motor.nombre); }
            if (p.tanque > 0) { Linea("Compras y sustituciones", r.tanque.nombre, r.tanque.costeMUSD); r.sustituciones++; r.enviado.Add(r.tanque.nombre); }
            if (p.instrumento > 0) { Linea("Compras y sustituciones", r.instrumento.nombre, r.instrumento.costeMUSD); r.sustituciones++; r.enviado.Add(r.instrumento.nombre); }
            if (p.relevo > 0) { Linea("Compras y sustituciones", r.relevo.nombre, r.relevo.costeMUSD); r.sustituciones++; r.enviado.Add(r.relevo.nombre); }
            if (p.sonda)
            {
                Linea("Compras y sustituciones", "Sonda de despliegue (sonda base del catálogo)", cat.costeBaseMUSD);
                r.enviado.Add("sonda de despliegue");
            }
            if (tramos > 0)
            {
                Linea("Combustible, suministros y kits", $"Combustible: {tramos * TramoCombustibleKg:0} kg", tramos * CosteTramoCombustibleMUSD);
                r.enviado.Add($"{tramos * TramoCombustibleKg:0} kg de combustible");
            }
            if (sumExtra > 0)
            {
                Linea("Combustible, suministros y kits", $"Suministros: {sumExtra} unidades", sumExtra * CosteSuministroMUSD);
                r.enviado.Add($"{sumExtra} suministros");
            }
            if (kitsExtra > 0)
            {
                Linea("Combustible, suministros y kits", $"Kits de reparación: {kitsExtra}", kitsExtra * CosteKitMUSD);
                r.enviado.Add($"{kitsExtra} kits de reparación");
            }
            r.enviaDesdeTierra = r.enviado.Count > 0;
            if (r.enviaDesdeTierra) Linea("Transporte orbital", "Un envío desde tierra con todo lo anterior (cargo fijo)", TarifaTransporteOrbitalMUSD);
            if (r.sustituciones > 0)
                Linea("Instalación", $"{r.sustituciones} componente(s) sustituido(s) × {TarifaInstalacionMUSD:0.#} M", r.sustituciones * TarifaInstalacionMUSD);
            Linea("Lanzamiento de la tripulación", "Lanzadera L-4 (configuración fija)", TarifaLanzamientoTripulacionMUSD);

            r.costeTotalMUSD = 0f;
            foreach (LineaPresupuesto l in r.lineas) r.costeTotalMUSD += l.costeMUSD;

            // ---- Límites que bloquean
            if (r.costeTotalMUSD > r.presupuestoMUSD + 0.001f)
                r.problemas.Add($"Coste previsto {r.costeTotalMUSD:0.#} M USD: supera el presupuesto de {r.presupuestoMUSD:0} M por {r.costeTotalMUSD - r.presupuestoMUSD:0.#} M.");
            if (r.mision.requiereSonda && !p.sonda)
                r.problemas.Add("Esta misión despliega una sonda: hay que incluir la sonda de despliegue.");
            if (r.instrumento.datosPorEscaneoMB > r.memoriaMB)
                r.problemas.Add($"La memoria ({r.memoriaMB:0} MB) no admite ni una captura de {r.instrumento.nombre}.");

            // ---- Avisos que no bloquean
            if (r.deltaVMS < r.mision.deltaVOrientativoMS)
                r.avisos.Add($"Cambio de velocidad disponible ≈ {r.deltaVMS:0} m/s; para esta misión se aconsejan unos {r.mision.deltaVOrientativoMS:0} m/s " +
                             "(ida, maniobras y regreso). Carga más combustible o reduce masa.");
            if (!r.instrumentoRecomendado)
                r.avisos.Add($"Para esta misión se recomienda «{r.mision.instrumentoRecomendado}»: con él cada captura vale el doble.");
            if (r.kits < 2 && r.mision.danoGuionizado)
                r.avisos.Add("Zona de asteroides: se esperan impactos. Con un solo kit no hay margen para una segunda brecha.");
            float datos = r.mision.capturasRequeridas * r.instrumento.datosPorEscaneoMB;
            if (datos > r.memoriaMB)
                r.avisos.Add($"Los datos previstos ({datos:0} MB) superan la memoria ({r.memoriaMB:0} MB): habrá que transmitir antes de terminar las capturas.");
            if (r.soporteVitalS < r.mision.duracionOrientativaS)
                r.avisos.Add($"Soporte vital para {r.soporteVitalS / 60f:0} min de vuelo; la misión suele durar unos {r.mision.duracionOrientativaS / 60f:0} min.");
            return r;
        }
    }

    /// <summary>
    /// Preparación de la expedición en curso. Estado ÚNICO y propio del MVP: no usa ni sobrescribe MisionActual (la
    /// configuración de las escenas anteriores). Confirmar la cierra; reabrir la devuelve a planificación.
    /// </summary>
    public static class ExpedicionActual
    {
        public static PreparacionExpedicion Preparacion { get; private set; } = new PreparacionExpedicion();
        public static bool Confirmada { get; private set; }

        public static void Confirmar(PreparacionExpedicion p) { Preparacion = p.Copia(); Confirmada = true; }
        public static void Reabrir() { Confirmada = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar()
        {
            Preparacion = new PreparacionExpedicion();
            Confirmada = false;
        }
    }

    /// <summary>Definición de una misión del MVP (solo textos y parámetros; la lógica está en EstadoExpedicion).</summary>
    public class DefinicionMisionMVP
    {
        public int id;
        public string nombre, resumen, instrumentoRecomendado;
        public float deltaVOrientativoMS, duracionOrientativaS;
        public bool requiereSonda, danoGuionizado;
        public int capturasRequeridas;
        public string objetivoInicial;     // id del cuerpo que se selecciona al empezar
        public List<string> pasos = new List<string>();

        public static List<DefinicionMisionMVP> Todas()
        {
            return new List<DefinicionMisionMVP>
            {
                new DefinicionMisionMVP
                {
                    id = 0, nombre = "Misión 1 · Observación planetaria",
                    resumen = "Bajar a la banda de observación de Kora, capturar tres observaciones, transmitirlas y volver a la base.",
                    instrumentoRecomendado = "Cámara multiespectral", deltaVOrientativoMS = 450f, duracionOrientativaS = 900f,
                    capturasRequeridas = 3, objetivoInicial = "kora",
                    pasos = new List<string>
                    {
                        "PILOTAJE: toma el control y aléjate más de 600 m de la base.",
                        "PILOTAJE: entra en la banda de observación (entre 24 y 28 km del centro de Kora).",
                        "CIENCIA: captura 3 observaciones. Condiciones: dentro de la banda, proa hacia Kora (±15°) y giro menor de 1°/s.",
                        "COMUNICACIONES: transmite las tres capturas a la base.",
                        "PILOTAJE: vuelve a la base (menos de 400 m y de 2 m/s respecto a ella) y confirma el regreso."
                    }
                },
                new DefinicionMisionMVP
                {
                    id = 1, nombre = "Misión 2 · Inspección del satélite K-2",
                    resumen = "Encuentro con el satélite artificial K-2, diagnóstico, reinicio de servicio y regreso a la base.",
                    instrumentoRecomendado = "Espectrómetro", deltaVOrientativoMS = 450f, duracionOrientativaS = 900f,
                    capturasRequeridas = 1, objetivoInicial = "k2",
                    pasos = new List<string>
                    {
                        "PILOTAJE: toma el control y aléjate más de 600 m de la base.",
                        "PILOTAJE: encuentro con el satélite K-2: a menos de 300 m y de 2 m/s respecto a él.",
                        "CIENCIA: haz el diagnóstico del satélite (mantén la distancia y la velocidad relativa).",
                        "INGENIERÍA: alimenta el bus de servicio.",
                        "COMUNICACIONES: envía la orden de reinicio al satélite (sigue a menos de 300 m).",
                        "COMUNICACIONES: transmite el informe del diagnóstico a la base.",
                        "PILOTAJE: vuelve a la base (menos de 400 m y de 2 m/s respecto a ella) y confirma el regreso."
                    }
                },
                new DefinicionMisionMVP
                {
                    id = 2, nombre = "Misión 3 · Exploración de asteroides",
                    resumen = "Escanear el cúmulo Tesel desde fuera, acercarse al asteroide T-7, desplegar la sonda y volver. Zona con riesgo de impactos.",
                    instrumentoRecomendado = "Espectrómetro", deltaVOrientativoMS = 600f, duracionOrientativaS = 1500f,
                    capturasRequeridas = 2, requiereSonda = true, danoGuionizado = true, objetivoInicial = "tesel",
                    pasos = new List<string>
                    {
                        "PILOTAJE: toma el control y aléjate más de 600 m de la base.",
                        "PILOTAJE: llega al punto de observación: entre 3 y 6 km del centro del cúmulo y a menos de 5 m/s respecto a él.",
                        "CIENCIA: escanea el cúmulo 2 veces. Condiciones: en el punto de observación, proa hacia el cúmulo (±15°) y giro menor de 1°/s.",
                        "PILOTAJE: acércate al asteroide T-7: a menos de 400 m de su superficie y de 3 m/s respecto a él.",
                        "CIENCIA: despliega la sonda sobre T-7.",
                        "COMUNICACIONES: transmite los escaneos y los datos de la sonda a la base.",
                        "PILOTAJE: vuelve a la base (menos de 400 m y de 2 m/s respecto a ella) y confirma el regreso."
                    }
                }
            };
        }
    }
}
