using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    public enum ResultadoMision { Exito, ExitoParcial, Fracaso }
    public enum MotivoFin { ObjetivoCumplido, BateriaAgotada, TerminadaPorJugador, ViajeAbortado, LlegadaAlDestino }

    /// <summary>
    /// Foto de la partida al terminar: configuración, energía, datos,
    /// puntuación y explicaciones. La crea ControlMision y la muestra
    /// PantallaResultados. Todas las cifras son de DEMOSTRACIÓN.
    /// </summary>
    public class InformeMision
    {
        /// <summary>Último informe generado. Se borra al pulsar Play.</summary>
        public static InformeMision Ultimo { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() => Ultimo = null;

        public MotivoFin motivo;
        public ResultadoMision resultado;
        public ConfiguracionElegida eleccion;
        public float segundosMision;
        public float minutosMisionPorSegundo = 1f;

        // Configuración
        public string objetivo, instrumento, bateria, lanzador, orbita;
        public bool instrumentoRecomendado;
        public string instrumentoSugerido;
        public float costeMUSD, presupuestoMUSD, masaSondaKg, masaLanzamientoKg, capacidadLanzadorKg;
        public float factorMasa = 1f;

        // Energía (Wh)
        public float capacidadWh, energiaFinalWh, energiaConsumidaWh, energiaRecargadaWh;
        public float energiaEscaneoWh, energiaTransmisionWh, energiaSistemasYMovimientoWh;
        public float bateriaMayorDisponibleWh;
        public float distanciaEstacionFinalM;

        // Datos
        public float memoriaMB, datosRecogidosMB, datosTransmitidosMB, datosPerdidosMB, datosPorEscaneoMB;
        public int escaneosRealizados, escaneosTransmitidos, escaneosRequeridos;
        public int vecesMemoriaLlena;
        public float segundosSinContacto, fraccionContacto;

        // Puntuación
        public int valorPorEscaneo, valorConRecomendado;
        public int puntosCiencia, bonoObjetivo, bonoPresupuesto, puntuacionTotal;
        public int bonoObjetivoConfigurado;
        public float puntosPorMUSDSobrante;

        // Modo equipo (1 jugador = sin equipo)
        public int jugadores = 1;
        public int[] responsables = new int[ModoEquipo.MaxJugadores];
        /// <summary>Prioridad de cada rol según el plan (configuración elegida).</summary>
        public readonly EstadoPrioridad[] prioridades = new EstadoPrioridad[ModoEquipo.MaxJugadores];
        /// <summary>Qué pasó en la misión en el área de cada rol.</summary>
        public readonly string[] resultadoRol = new string[ModoEquipo.MaxJugadores];

        // Viaje en la nave (vacío si se jugó Exploracion sin pasar por la nave). DEMOSTRACIÓN.
        public bool viajeRealizado;
        public string motivoAborto = "";
        public float masaNaveKg;
        public float combustibleInicialKg, combustibleExtraKg, combustibleFinalKg;
        public float combustibleManiobrasKg, combustibleInsercionKg;
        public float combustibleAntesInsercionKg = -1f;
        public float integridadFinal;
        public int impactos, escombrosEsquivados;
        public float energiaNaveInicialWh, energiaNaveFinalWh, segundosAveria;
        public int erroresAveria;
        public float errorInsercion = -1f;
        public float zonaVerdeInsercion = 0.08f;
        public bool viajeEvaluado;
        public float perdidaEnergiaNave;
        public int puntosReparacion, puntosEscombros, puntosEtapa = -1, puntosViaje;
        public float reduccionCargaSonda;          // fracción 0..0,1 (regla simplificada)
        public float cargaInicialSondaWh = -1f;    // -1 = batería llena
        public string resultadoEtapa = "";

        // Observación de aproximación: bonus APARTE, no suma a puntuacionTotal. DEMOSTRACIÓN.
        public bool observacionActiva;
        public float observacionCapturadaUD, observacionEnviadaUD, observacionMaximaUD, energiaObservacionWh;
        public int bonoObservacion, bonoObservacionMax;

        // Corrección de trayectoria antes del frenado (no da puntos). DEMOSTRACIÓN.
        public bool correccionActiva, correccionAbierta;
        public string correccionResultado = "";
        public float correccionSegundos = -1f, correccionDuracionS, correccionCombustibleKg;
        public float periodoInsercionS = 2.2f, periodoInsercionBaseS = 2.2f;

        // Navegación longitudinal (opcional). DEMOSTRACIÓN.
        public bool navegacionActiva;
        public float combustibleMotorPrincipalKg, combustiblePruebaKg = -1f, rutaM, velocidadMaximaMS;
        public int sobrepasos;
        public bool gravedadActiva;
        public float deltaVMotorMS, deltaVGravedadMS;

        // Misión tripulada (demostración): se evalúa la llegada, no los escaneos de la sonda.
        public bool misionTripulada;
        public string destino = "";
        public int bonoLlegadaConfigurado = 10;
        public int bonoLlegada;
        public bool LlegadaCumplida => misionTripulada && viajeRealizado && !ViajeAbortado;

        // Piezas de la nave (no de la sonda)
        public string motorNave = "";
        public string tanqueNave = "";
        public float capacidadTanqueKg;
        public float empujeMotorN, consumoMotorKgS, costeNaveMUSD, masaVariantesNaveKg, cargaCientificaKg;
        /// <summary>Masa seca = masa total inicial − carga científica − combustible inicial.</summary>
        public float MasaSecaNaveKg => Mathf.Max(0f, masaNaveKg - cargaCientificaKg - combustibleInicialKg);
        public float segundosViaje;

        public readonly List<string> explicaciones = new List<string>();

        public bool EnEquipo => jugadores > 1;
        public bool ViajeAbortado => motivo == MotivoFin.ViajeAbortado;

        public int PrioridadesCumplidas
        {
            get
            {
                int n = 0;
                foreach (EstadoPrioridad p in prioridades) if (p.cumple) n++;
                return n;
            }
        }

        public bool ObjetivoCumplido => escaneosTransmitidos >= escaneosRequeridos;
        public bool SondaOperativa => energiaFinalWh > 0f;

        /// <summary>Copia los datos del viaje en la nave, si lo hubo.</summary>
        public void CopiarViaje()
        {
            viajeRealizado = EstadoViaje.Realizado;
            if (!viajeRealizado) return;
            motivoAborto = EstadoViaje.MotivoAborto;
            masaNaveKg = EstadoViaje.MasaInicialKg;
            combustibleInicialKg = EstadoViaje.CombustibleInicialKg;
            combustibleExtraKg = EstadoViaje.CombustibleExtraKg;
            combustibleFinalKg = EstadoViaje.CombustibleFinalKg;
            combustibleManiobrasKg = EstadoViaje.CombustibleManiobrasKg;
            combustibleInsercionKg = EstadoViaje.CombustibleInsercionKg;
            combustibleAntesInsercionKg = EstadoViaje.CombustibleAntesInsercionKg;
            integridadFinal = EstadoViaje.IntegridadFinal;
            impactos = EstadoViaje.Impactos;
            escombrosEsquivados = EstadoViaje.EscombrosEsquivados;
            energiaNaveInicialWh = EstadoViaje.EnergiaInicialWh;
            energiaNaveFinalWh = EstadoViaje.EnergiaFinalWh;
            segundosAveria = EstadoViaje.SegundosAveria;
            erroresAveria = EstadoViaje.ErroresAveria;
            errorInsercion = EstadoViaje.ErrorInsercion;
            zonaVerdeInsercion = EstadoViaje.ZonaVerdeInsercion;
            viajeEvaluado = EstadoViaje.Evaluado;
            perdidaEnergiaNave = EstadoViaje.PerdidaEnergiaNave;
            puntosReparacion = EstadoViaje.PuntosReparacion;
            puntosEscombros = EstadoViaje.PuntosEscombros;
            puntosEtapa = EstadoViaje.PuntosEtapa;
            resultadoEtapa = EstadoViaje.ResultadoEtapa;
            reduccionCargaSonda = EstadoViaje.CargaSondaAplicada ? EstadoViaje.ReduccionCargaSonda : 0f;
            cargaInicialSondaWh = EstadoViaje.CargaSondaAplicada ? EstadoViaje.CargaInicialSondaWh : -1f;
            segundosViaje = EstadoViaje.SegundosViaje;
            observacionActiva = EstadoViaje.ObservacionActiva;
            observacionCapturadaUD = EstadoViaje.ObservacionCapturadaUD;
            observacionEnviadaUD = EstadoViaje.ObservacionEnviadaUD;
            observacionMaximaUD = EstadoViaje.ObservacionMaximaUD;
            energiaObservacionWh = EstadoViaje.EnergiaObservacionWh;
            bonoObservacion = EstadoViaje.BonoObservacion;
            bonoObservacionMax = EstadoViaje.BonoObservacionMax;
            correccionActiva = EstadoViaje.CorreccionActiva;
            correccionAbierta = EstadoViaje.CorreccionAbierta;
            correccionResultado = EstadoViaje.CorreccionResultado;
            correccionSegundos = EstadoViaje.CorreccionSegundos;
            correccionDuracionS = EstadoViaje.CorreccionDuracionS;
            correccionCombustibleKg = EstadoViaje.CorreccionCombustibleKg;
            periodoInsercionS = EstadoViaje.PeriodoInsercionS;
            periodoInsercionBaseS = EstadoViaje.PeriodoInsercionBaseS;
            navegacionActiva = EstadoViaje.NavegacionActiva;
            combustibleMotorPrincipalKg = EstadoViaje.CombustibleMotorPrincipalKg;
            combustiblePruebaKg = EstadoViaje.CombustiblePruebaKg;
            rutaM = EstadoViaje.RutaM;
            velocidadMaximaMS = EstadoViaje.VelocidadMaximaMS;
            sobrepasos = EstadoViaje.Sobrepasos;
            gravedadActiva = EstadoViaje.GravedadActiva;
            deltaVMotorMS = EstadoViaje.DeltaVMotorMS;
            deltaVGravedadMS = EstadoViaje.DeltaVGravedadMS;
        }

        /// <summary>
        /// Copia la configuración cuando la misión termina antes de la exploración (viaje abortado).
        /// La sonda no se usó: su batería figura llena y sin datos.
        /// </summary>
        public void CopiarConfiguracion(ResumenMision r, CatalogoMision c)
        {
            if (r != null && r.instrumento != null)
            {
                objetivo = r.objetivo.nombre;
                instrumento = r.instrumento.nombre;
                bateria = r.bateria.nombre;
                lanzador = r.lanzador.nombre;
                orbita = r.orbita.nombre;
                instrumentoRecomendado = r.instrumentoRecomendado;
                instrumentoSugerido = r.objetivo.instrumentoRecomendado;
                costeMUSD = r.costeTotalMUSD;
                presupuestoMUSD = r.presupuestoMUSD;
                masaSondaKg = r.masaSondaKg;
                masaLanzamientoKg = r.masaLanzamientoKg;
                capacidadLanzadorKg = r.capacidadLanzadorKg;
                memoriaMB = r.memoriaMB;
                datosPorEscaneoMB = r.instrumento.datosPorEscaneoMB;
                fraccionContacto = r.orbita.fraccionContacto;
                valorPorEscaneo = r.valorPorEscaneo;
                escaneosRequeridos = r.objetivo.escaneosRequeridos;
                capacidadWh = r.energiaWh;
                energiaFinalWh = r.energiaWh;
                cargaCientificaKg = r.cargaCientificaKg;
                costeNaveMUSD = r.costeNaveMUSD;
                masaVariantesNaveKg = r.masaVariantesNaveKg;
                if (r.tanqueNave != null)
                {
                    tanqueNave = r.tanqueNave.nombre;
                    capacidadTanqueKg = r.tanqueNave.capacidadKg;
                }
                if (r.motorNave != null)
                {
                    motorNave = r.motorNave.nombre;
                    empujeMotorN = r.motorNave.empujeN;
                    consumoMotorKgS = r.motorNave.consumoKgS;
                }
            }
            if (c != null)
            {
                minutosMisionPorSegundo = c.minutosMisionPorSegundo;
                bonoObjetivoConfigurado = c.bonoObjetivoCumplido;
                puntosPorMUSDSobrante = c.puntosPorMUSDSobrante;
            }
        }

        /// <summary>Decide el resultado, calcula la puntuación y redacta las explicaciones.</summary>
        public void Completar()
        {
            if (misionTripulada)
            {
                CompletarTripulada();
                return;
            }

            if (ViajeAbortado) resultado = ResultadoMision.Fracaso;
            else if (ObjetivoCumplido) resultado = ResultadoMision.Exito;
            else if (escaneosTransmitidos > 0) resultado = ResultadoMision.ExitoParcial;
            else resultado = ResultadoMision.Fracaso;

            // Balance: final = carga inicial − consumida + recargada.
            // La carga inicial es la capacidad, salvo que el descenso la redujera (regla simplificada).
            float inicial = cargaInicialSondaWh >= 0f ? cargaInicialSondaWh : capacidadWh;
            energiaRecargadaWh = Mathf.Max(0f, energiaFinalWh - inicial + energiaConsumidaWh);
            energiaSistemasYMovimientoWh = Mathf.Max(0f, energiaConsumidaWh - energiaEscaneoWh - energiaTransmisionWh);

            puntosCiencia = Mathf.Max(0, puntosCiencia);
            bool cumplido = ObjetivoCumplido && !ViajeAbortado;
            bonoObjetivo = cumplido ? bonoObjetivoConfigurado : 0;
            bonoPresupuesto = cumplido
                ? Mathf.RoundToInt(Mathf.Max(0f, presupuestoMUSD - costeMUSD) * puntosPorMUSDSobrante)
                : 0;
            // Puntos de viaje: solo si la sonda llegó a desplegarse (viaje evaluado y no abortado).
            puntosViaje = viajeRealizado && viajeEvaluado && !ViajeAbortado
                ? puntosReparacion + puntosEscombros + Mathf.Max(0, puntosEtapa)
                : 0;
            puntuacionTotal = puntosCiencia + bonoObjetivo + bonoPresupuesto + puntosViaje;

            DescribirRoles();
            Explicar();
        }

        void Explicar()
        {
            explicaciones.Clear();

            // 1. Qué pasó
            switch (motivo)
            {
                case MotivoFin.ObjetivoCumplido:
                    explicaciones.Add($"Transmitiste {escaneosTransmitidos} de {escaneosRequeridos} escaneos requeridos en " +
                                      $"{DuracionTexto()}: el objetivo «{objetivo}» se cumplió.");
                    break;
                case MotivoFin.BateriaAgotada:
                    explicaciones.Add($"La batería se agotó a {distanciaEstacionFinalM:0} m de la estación. " +
                                      "Sin energía, la sonda no pudo moverse ni volver a transmitir.");
                    break;
                case MotivoFin.TerminadaPorJugador:
                    explicaciones.Add($"Terminaste la misión en la estación con {escaneosTransmitidos} de " +
                                      $"{escaneosRequeridos} escaneos transmitidos.");
                    break;
                case MotivoFin.ViajeAbortado:
                    explicaciones.Add($"La misión se abortó durante el viaje: {motivoAborto} La sonda no llegó a desplegarse.");
                    break;
            }

            // 1b. Viaje en la nave
            if (viajeRealizado) ExplicarViaje();

            if (ViajeAbortado)
            {
                if (costeMUSD < presupuestoMUSD)
                    explicaciones.Add("El presupuesto sobrante no suma puntos porque el objetivo no se cumplió.");
                return;
            }

            // 2. Datos que no llegaron
            if (datosPerdidosMB > 0.5f)
                explicaciones.Add($"Se perdieron {datosPerdidosMB:0} MB que seguían en la memoria: los datos solo cuentan " +
                                  "cuando llegan al satélite de relevo, no al recogerlos.");

            // 3. Batería
            if (motivo == MotivoFin.BateriaAgotada)
            {
                if (bateriaMayorDisponibleWh > capacidadWh)
                    explicaciones.Add($"Con la batería de {capacidadWh:0} Wh el margen era corto. Había una de " +
                                      $"{bateriaMayorDisponibleWh:0} Wh, a cambio de más masa y coste; también ayuda volver " +
                                      "a la estación antes de alejarse demasiado.");
                else
                    explicaciones.Add("Ya llevabas la batería de mayor capacidad: la clave era planificar los regresos a la estación.");
            }
            else if (energiaRecargadaWh > capacidadWh * 0.5f)
            {
                explicaciones.Add($"La estación aportó {energiaRecargadaWh:0} Wh, más de la mitad de la batería ({capacidadWh:0} Wh): " +
                                  "volver a recargar fue parte esencial de la misión.");
            }

            // 4. En qué se gastó la energía
            if (energiaConsumidaWh > 0.1f)
            {
                float t = energiaConsumidaWh;
                explicaciones.Add($"Energía usada: {Pct(energiaSistemasYMovimientoWh, t)} en sistemas y movimiento, " +
                                  $"{Pct(energiaEscaneoWh, t)} en escanear y {Pct(energiaTransmisionWh, t)} en transmitir.");
            }

            // 5. Masa
            if (factorMasa < 0.9f)
                explicaciones.Add($"La sonda pesaba {masaSondaKg:0} kg: aceleraba al {factorMasa * 100f:0} % de la de referencia, " +
                                  "lo que alarga cada desplazamiento.");

            // 6. Comunicaciones
            if (segundosSinContacto > 3f)
                explicaciones.Add($"Esperaste {segundosSinContacto:0} s en la estación sin contacto con el relevo " +
                                  $"({orbita}, contacto {fraccionContacto * 100f:0} % del tiempo). Un relevo con contacto " +
                                  "continuo evita esas esperas, pero pesa más en el lanzador.");

            // 7. Memoria
            if (vecesMemoriaLlena > 0)
                explicaciones.Add($"La memoria ({memoriaMB:0} MB) se llenó {vecesMemoriaLlena} " +
                                  (vecesMemoriaLlena == 1 ? "vez" : "veces") +
                                  $": con {instrumento} cada escaneo ocupa {datosPorEscaneoMB:0} MB.");

            // 8. Instrumento
            if (!instrumentoRecomendado)
                explicaciones.Add($"{instrumento} no era el instrumento recomendado para este objetivo ({instrumentoSugerido}): " +
                                  $"cada escaneo valió {valorPorEscaneo} puntos; con el recomendado habría valido más.");

            // 9. Presupuesto
            float sobrante = presupuestoMUSD - costeMUSD;
            if (ObjetivoCumplido && sobrante > 0f)
                explicaciones.Add($"Gastaste {costeMUSD:0} de {presupuestoMUSD:0} M USD: los {sobrante:0} M USD sobrantes " +
                                  $"suman {bonoPresupuesto} puntos.");
            else if (!ObjetivoCumplido && sobrante > 0f)
                explicaciones.Add("El presupuesto sobrante no suma puntos porque el objetivo no se cumplió.");

            // 10. Equipo
            if (EnEquipo)
                explicaciones.Add($"El equipo ({jugadores} jugadores) cumplió {PrioridadesCumplidas} de {ModoEquipo.MaxJugadores} " +
                                  "prioridades de rol con su plan. Las prioridades chocan entre sí: mejorar una suele " +
                                  "empeorar otra, y negociar cuál ceder es parte del diseño de una misión.");
        }

        /// <summary>Resultado real de la misión en el área de cada rol.</summary>
        void DescribirRoles()
        {
            resultadoRol[(int)Rol.Ciencia] =
                $"Transmitidos {escaneosTransmitidos}/{escaneosRequeridos} escaneos · {puntosCiencia} puntos de ciencia";

            resultadoRol[(int)Rol.Ingenieria] = energiaFinalWh <= 0f
                ? "La batería se agotó"
                : $"Energía final {energiaFinalWh:0}/{capacidadWh:0} Wh · recargados {energiaRecargadaWh:0} Wh en la estación";

            float reserva = presupuestoMUSD - costeMUSD;
            resultadoRol[(int)Rol.Lanzamiento] = ObjetivoCumplido
                ? $"Reserva {reserva:0} M USD → {bonoPresupuesto} puntos de bono"
                : $"Reserva {reserva:0} M USD (no suma: objetivo no cumplido)";

            resultadoRol[(int)Rol.Comunicaciones] =
                $"Transmitidos {datosTransmitidosMB:0} MB · {segundosSinContacto:0} s esperando contacto · perdidos {datosPerdidosMB:0} MB";
        }

        /// <summary>
        /// Evaluación de la misión tripulada, separada y transparente:
        /// llegada al destino + presupuesto sobrante (solo si se llega) + puntos de viaje.
        /// No hay ciencia ni objetivo de escaneo: la exploración con la sonda es una fase posterior.
        /// </summary>
        void CompletarTripulada()
        {
            bool llegada = LlegadaCumplida;
            resultado = llegada ? ResultadoMision.Exito : ResultadoMision.Fracaso;

            puntosCiencia = 0;
            bonoObjetivo = 0;
            bonoLlegada = llegada ? bonoLlegadaConfigurado : 0;
            float sobrante = Mathf.Max(0f, presupuestoMUSD - costeMUSD);
            bonoPresupuesto = llegada ? Mathf.RoundToInt(sobrante * puntosPorMUSDSobrante) : 0;
            puntosViaje = llegada && viajeEvaluado ? puntosReparacion + puntosEscombros + Mathf.Max(0, puntosEtapa) : 0;
            puntuacionTotal = bonoLlegada + bonoPresupuesto + puntosViaje;

            explicaciones.Clear();
            if (llegada)
                explicaciones.Add($"La nave llegó a {destino} y entró en órbita: objetivo de la misión tripulada cumplido (+{bonoLlegada}).");
            else
                explicaciones.Add($"La misión tripulada se abortó durante el viaje: {motivoAborto} No hay puntos de llegada, presupuesto ni viaje.");

            if (viajeRealizado) ExplicarViaje();

            if (llegada)
                explicaciones.Add($"Presupuesto: costó {costeMUSD:0} de {presupuestoMUSD:0} M USD; sobraron {sobrante:0} M USD → +{bonoPresupuesto} " +
                                  $"({puntosPorMUSDSobrante:0.##} por M USD, solo si se llega al destino).");

            explicaciones.Add("Esta puntuación no incluye ciencia: la exploración con la sonda es una fase posterior y no se da por realizada.");
            if (observacionActiva)
                explicaciones.Add(observacionCapturadaUD > 0f
                    ? $"Observación de aproximación (bonus aparte, no suma al total): capturaste {Mathf.FloorToInt(observacionCapturadaUD + 0.001f)} " +
                      $"de {Mathf.FloorToInt(observacionMaximaUD + 0.001f)} UD con {energiaObservacionWh:0} Wh de la nave y enviaste " +
                      $"{Mathf.FloorToInt(observacionEnviadaUD + 0.001f)} → bonus {bonoObservacion} de {bonoObservacionMax}. " +
                      "Solo cuentan los datos enviados antes de llegar."
                    : $"Observación de aproximación: no se capturaron datos (bonus 0 de {bonoObservacionMax}, sin gasto de energía).");
            explicaciones.Add("Valores, distancia y duración del viaje son de DEMOSTRACIÓN (viaje comprimido y progreso guionizado).");
        }

        void ExplicarViaje()
        {
            if (!string.IsNullOrEmpty(resultadoEtapa))
                explicaciones.Add($"Despegue con el lanzador {lanzador}. {resultadoEtapa} Procedimiento simplificado del juego.");

            if (impactos > 0)
                explicaciones.Add($"La nave recibió {impactos} impacto" + (impactos == 1 ? "" : "s") +
                                  $" de escombros (integridad final {integridadFinal:0} %). Desde el puesto de pilotaje " +
                                  "se pueden esquivar, a cambio de combustible.");
            else if (escombrosEsquivados > 0)
                explicaciones.Add($"Esquivaste los {escombrosEsquivados} escombros sin ningún impacto.");

            if (segundosAveria > 0f)
                explicaciones.Add($"La avería eléctrica duró {segundosAveria:0} s y la nave perdió " +
                                  $"{energiaNaveInicialWh - energiaNaveFinalWh - energiaObservacionWh:0} Wh de energía" +
                                  (erroresAveria > 0 ? $", con {erroresAveria} paso" + (erroresAveria == 1 ? "" : "s") + " equivocado" + (erroresAveria == 1 ? "" : "s") + "." : ".") +
                                  " Seguir el procedimiento en orden reduce las pérdidas.");

            if (viajeEvaluado)
            {
                explicaciones.Add($"Puntos de viaje: {puntosViaje}. Reparación de la avería: {perdidaEnergiaNave * 100f:0} % de la energía " +
                                  $"de la nave perdida → +{puntosReparacion}. Escombros: {impactos} impacto" + (impactos == 1 ? "" : "s") +
                                  $" → +{puntosEscombros}." +
                                  (puntosEtapa < 0 ? " La separación de etapa no se evaluó." : $" Separación de etapa → +{puntosEtapa}."));
                if (reduccionCargaSonda > 0f)
                    explicaciones.Add($"Estado de la sonda (no resta puntos): la frenada imprecisa dejó la órbita más lejos del punto de acceso; " +
                                      $"el descenso se prolongó y la sonda empezó con {cargaInicialSondaWh:0}/{capacidadWh:0} Wh " +
                                      $"({reduccionCargaSonda * 100f:0} % menos). Regla simplificada del juego; la estación recarga con normalidad.");
            }

            if (combustiblePruebaKg >= 0f)
                explicaciones.Add($"COMBUSTIBLE DE PRUEBA: la partida empezó con {combustiblePruebaKg:0.0} kg fijados a mano en el ajuste de pruebas, " +
                                  "no con el combustible calculado por la configuración. No es una partida normal.");
            if (navegacionActiva)
                explicaciones.Add($"Navegación longitudinal (DEMOSTRACIÓN, ruta comprimida de {rutaM:0} m, sin gravedad): velocidad máxima " +
                                  $"{velocidadMaximaMS:0.0} m/s; motor principal {combustibleMotorPrincipalKg:0.0} kg para acelerar, frenar y volver" +
                                  (sobrepasos > 0 ? $"; te pasaste de largo de la zona de aproximación {sobrepasos} " + (sobrepasos == 1 ? "vez." : "veces.") : ".") +
                                  " La inserción orbital se cobra aparte: es el frenado de captura desde la zona.");
            if (navegacionActiva && gravedadActiva)
                explicaciones.Add($"Gravedad EXPERIMENTAL (un eje, sin órbitas): el motor principal cambió la velocidad {deltaVMotorMS:0.0} m/s en total " +
                                  $"y la atracción del destino {deltaVGravedadMS:0.0} m/s. La gravedad no gasta combustible, pero acelera la " +
                                  "nave hacia el destino y obliga a frenar más.");
            explicaciones.Add($"Combustible de la nave: {combustibleInicialKg:0} kg al partir ({combustibleExtraKg:0} kg extra por la " +
                              $"capacidad sobrante del lanzador, regla de demostración). Maniobras {combustibleManiobrasKg:0.0} kg" +
                              (navegacionActiva ? $"; motor principal {combustibleMotorPrincipalKg:0.0} kg" : "") +
                              (combustibleAntesInsercionKg >= 0f
                                  ? $"; antes de la entrada en órbita quedaban {combustibleAntesInsercionKg:0.0} kg y el frenado gastó {combustibleInsercionKg:0.0} kg"
                                  : $"; entrada en órbita {combustibleInsercionKg:0.0} kg") +
                              $"; quedaron {combustibleFinalKg:0.0} kg.");

            if (correccionActiva && correccionAbierta)
                explicaciones.Add(correccionSegundos >= 0f
                    ? $"Corrección de trayectoria {correccionResultado} ({correccionSegundos:0.0} s de una ventana de {correccionDuracionS:0} s): " +
                      $"el marcador de frenado fue más lento (periodo {periodoInsercionS:0.##} s en lugar de {periodoInsercionBaseS:0.##} s), " +
                      "lo que facilita frenar cerca del centro; el combustible del frenado dependió de cuándo frenaste. " +
                      $"Maniobras durante la ventana: {correccionCombustibleKg:0.0} kg (incluidos en las maniobras). No da puntos."
                    : $"No se corrigió la trayectoria en la ventana de {correccionDuracionS:0} s (era opcional): frenado con el marcador " +
                      $"de siempre (periodo {periodoInsercionBaseS:0.##} s). Maniobras durante la ventana: {correccionCombustibleKg:0.0} kg.");

            if (errorInsercion >= 0f)
                explicaciones.Add(errorInsercion <= zonaVerdeInsercion
                    ? "Frenaste en el momento justo para entrar en órbita: gastaste el mínimo de combustible."
                    : $"Al frenar para entrar en órbita te desviaste un {errorInsercion / 0.5f * 100f:0} % del momento óptimo, " +
                      "y eso costó combustible extra.");
        }

        static string Pct(float parte, float total) => total > 0f ? $"{parte / total * 100f:0} %" : "0 %";

        /// <summary>Duración en tiempo real y su equivalente en tiempo de misión.</summary>
        public string DuracionTexto()
        {
            float minutosMision = segundosMision * minutosMisionPorSegundo;
            return $"{FormatoTiempo(segundosMision)} reales (≈ {minutosMision:0} min de misión)";
        }

        public static string FormatoTiempo(float segundos)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(segundos));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
