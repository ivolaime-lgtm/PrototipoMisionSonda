using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    /// <summary>
    /// Pantalla del informe final (interfaz provisional IMGUI).
    /// Muestra configuración, objetivos, energía, datos, explicaciones y
    /// puntuación de DEMOSTRACIÓN, con botones para volver a configurar
    /// o repetir con la misma configuración.
    /// </summary>
    public class PantallaResultados : MonoBehaviour
    {
        [SerializeField] string escenaConfiguracion = "Configuracion";
        [SerializeField] string escenaExploracion = "Exploracion";
        [Tooltip("Si está activado y la escena existe, «Repetir» vuelve a empezar desde la nave.")]
        [SerializeField] string escenaNave = "Nave";
        [SerializeField] bool repetirPasandoPorNave = true;

        [Header("Textos editables")]
        [SerializeField] string tituloExito = "MISIÓN CUMPLIDA";
        [SerializeField] string tituloParcial = "MISIÓN PARCIALMENTE CUMPLIDA";
        [SerializeField] string tituloFracaso = "MISIÓN FALLIDA";
        [SerializeField] string notaDemo = "Informe de DEMOSTRACIÓN: modelo simplificado, sin datos de misiones reales.";

        Vector2 desplazamiento;
        GUIStyle sTitulo, sSeccion, sTexto, sPequeno, sBoton, sOk, sNo;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnGUI()
        {
            PrepararEstilos();

            float escala = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            Rellenar(new Rect(0, 0, Screen.width / escala, Screen.height / escala), new Color(0.02f, 0.08f, 0.11f));

            InformeMision inf = InformeMision.Ultimo;
            bool volver = false, repetir = false, explorar = false;

            if (inf == null)
            {
                GUILayout.BeginArea(new Rect(40, 40, 1000, 400));
                GUILayout.Label("No hay ningún informe todavía.", sTitulo);
                GUILayout.Label("Juega una misión empezando en la escena Configuracion.", sTexto);
                if (GUILayout.Button("IR A LA CONFIGURACIÓN", sBoton, GUILayout.Height(56), GUILayout.Width(420))) volver = true;
                GUILayout.EndArea();
            }
            else if (inf.misionTripulada)
            {
                DibujarTripuladaIzquierda(inf);
                DibujarTripuladaDerecha(inf, ref volver, ref repetir, ref explorar);
            }
            else
            {
                DibujarColumnaIzquierda(inf);
                DibujarColumnaDerecha(inf, ref volver, ref repetir);
            }

            GUI.matrix = Matrix4x4.identity;

            // Cambios de escena al final del evento.
            if (volver) SceneManager.LoadScene(escenaConfiguracion);
            // Fase posterior opcional: la sonda empieza con el estado que dejó el viaje (no se reinicia).
            if (explorar && inf != null && inf.LlegadaCumplida)
            {
                MisionActual.Guardar(inf.eleccion);
                SceneManager.LoadScene(escenaExploracion);
            }
            if (repetir && inf != null)
            {
                MisionActual.Guardar(inf.eleccion);
                EstadoViaje.Reiniciar();
                SceneManager.LoadScene(EstadoViaje.EscenaDeInicio(repetirPasandoPorNave, escenaNave, escenaExploracion));
            }
        }

        // ================================================================ Misión tripulada

        void DibujarTripuladaIzquierda(InformeMision inf)
        {
            GUILayout.BeginArea(new Rect(40, 25, 900, 1035));
            bool ok = inf.LlegadaCumplida;
            Color previo = sTitulo.normal.textColor;
            sTitulo.normal.textColor = ok ? new Color(0.45f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f);
            GUILayout.Label(ok ? "OBJETIVO ALCANZADO" : tituloFracaso, sTitulo);
            sTitulo.normal.textColor = previo;
            GUILayout.Label($"Misión tripulada (DEMOSTRACIÓN) · destino: {inf.destino}", sTexto);
            GUILayout.Label($"Viaje: {InformeMision.FormatoTiempo(inf.segundosViaje)} reales (comprimido) · " + Motivo(inf.motivo), sTexto);

            Seccion("OBJETIVOS");
            Check(ok, "Llegar al destino y entrar en órbita");
            Check(inf.costeMUSD <= inf.presupuestoMUSD, $"Respetar el presupuesto ({inf.costeMUSD:0}/{inf.presupuestoMUSD:0} M USD)");
            Linea("Exploración con la sonda: fase posterior (no forma parte de esta demostración).");

            Seccion("CONFIGURACIÓN");
            Linea($"Lanzador: {inf.lanzador} · margen de carga configurable {inf.masaLanzamientoKg:0}/{inf.capacidadLanzadorKg:0} kg (vehículo base incluido)");
            if (!string.IsNullOrEmpty(inf.motorNave))
                Linea($"Motor de la nave: {inf.motorNave} · empuje {inf.empujeMotorN / 1000f:0} kN · consumo {inf.consumoMotorKgS:0.0} kg/s");
            if (!string.IsNullOrEmpty(inf.tanqueNave))
                Linea($"Tanque de la nave: {inf.tanqueNave} · capacidad {inf.capacidadTanqueKg:0} kg · piezas de la nave {inf.costeNaveMUSD:0} M USD");
            Linea($"Carga científica a bordo: sonda ({inf.instrumento}, batería {inf.bateria}) y relevo ({inf.orbita})");
            Linea($"Objetivo científico previsto para la fase posterior: {inf.objetivo}");

            if (inf.viajeRealizado)
            {
                Seccion("NAVE Y RECURSOS");
                Linea($"Masa total inicial {inf.masaNaveKg:0} kg = seca {inf.MasaSecaNaveKg:0} + carga científica {inf.cargaCientificaKg:0} + combustible {inf.combustibleInicialKg:0}");
                Barra("Combustible final", inf.combustibleFinalKg, inf.combustibleInicialKg, "kg", new Color(1f, 0.8f, 0.3f));
                if (inf.navegacionActiva)
                {
                    // Desglose completo con navegación: cada categoría se cobra una sola vez.
                    float cuadre = inf.combustibleInicialKg - inf.combustibleManiobrasKg - inf.combustibleMotorPrincipalKg -
                                   inf.combustibleInsercionKg - inf.combustibleFinalKg;
                    Linea($"  Al partir {inf.combustibleInicialKg:0.0} kg" +
                          (inf.combustiblePruebaKg >= 0f ? " <color=#FFB040>(COMBUSTIBLE DE PRUEBA)</color>"
                                                         : $" ({inf.combustibleExtraKg:0} kg extra por la capacidad sobrante del lanzador)"));
                    Linea($"  − maniobras laterales {inf.combustibleManiobrasKg:0.0} kg · − motor principal {inf.combustibleMotorPrincipalKg:0.0} kg " +
                          $"· − entrada en órbita {inf.combustibleInsercionKg:0.0} kg");
                    Linea($"  = al final {inf.combustibleFinalKg:0.0} kg" + (Mathf.Abs(cuadre) > 0.05f ? $" (diferencia {cuadre:0.0} kg: revisar)" : " (cuadra)"));
                }
                else
                Linea($"  Maniobras {inf.combustibleManiobrasKg:0} kg · " +
                      $"entrada en órbita {inf.combustibleInsercionKg:0} kg " +
                      $"({inf.combustibleExtraKg:0} kg extra por la capacidad sobrante del lanzador)");
                Barra("Energía de la nave", inf.energiaNaveFinalWh, inf.energiaNaveInicialWh, "Wh", new Color(0.4f, 0.7f, 1f));
                Barra("Integridad del casco", inf.integridadFinal, 100f, "%", new Color(0.5f, 1f, 0.6f));

                Seccion("DESEMPEÑO EN EL VIAJE");
                string etapa = inf.puntosEtapa < 0 ? "sin evaluar" : inf.puntosEtapa > 0 ? "a tiempo" : "fuera de la ventana";
                Linea($"Separación de etapa: {etapa}");
                Linea($"Avería: {inf.segundosAveria:0} s, {inf.erroresAveria} pasos equivocados");
                Linea($"Escombros: {inf.impactos} impactos, {inf.escombrosEsquivados} esquivados");
                Linea(inf.errorInsercion < 0f ? "Frenada: no se llegó a frenar"
                    : inf.errorInsercion <= inf.zonaVerdeInsercion ? "Frenada: en el momento justo"
                    : $"Frenada: desviada un {inf.errorInsercion / 0.5f * 100f:0} % del momento óptimo");
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(notaDemo, sPequeno);
            GUILayout.EndArea();
        }

        void DibujarTripuladaDerecha(InformeMision inf, ref bool volver, ref bool repetir, ref bool explorar)
        {
            GUILayout.BeginArea(new Rect(980, 25, 900, 1035));

            Seccion("POR QUÉ");
            desplazamiento = GUILayout.BeginScrollView(desplazamiento, GUILayout.Height(470));
            foreach (string e in inf.explicaciones)
            {
                GUILayout.Label("• " + e, sTexto);
                GUILayout.Space(6);
            }
            GUILayout.EndScrollView();

            Seccion("EVALUACIÓN DE LA MISIÓN TRIPULADA (DEMOSTRACIÓN)");
            Linea($"Llegada al destino: {inf.bonoLlegada}");
            Linea($"Presupuesto: {inf.bonoPresupuesto} ({inf.puntosPorMUSDSobrante:0.##} por M USD sobrante, solo si se llega)");
            Linea(inf.LlegadaCumplida && inf.viajeEvaluado
                ? $"Viaje: {inf.puntosViaje} (separación +{Mathf.Max(0, inf.puntosEtapa)}, reparación +{inf.puntosReparacion}, escombros +{inf.puntosEscombros})"
                : "Viaje: 0 (no se llegó al destino)");
            Linea("Ciencia: no realizada (la exploración es una fase posterior)");
            GUILayout.Label($"TOTAL: {inf.puntuacionTotal} puntos", sSeccion);
            if (inf.observacionActiva)
                Linea($"Bonus de observación (aparte, no suma al total): {inf.bonoObservacion} de {inf.bonoObservacionMax} · " +
                      $"enviadas {Mathf.FloorToInt(inf.observacionEnviadaUD + 0.001f)} de {Mathf.FloorToInt(inf.observacionCapturadaUD + 0.001f)} UD capturadas");

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("VOLVER A CONFIGURAR", sBoton, GUILayout.Height(64))) volver = true;
            if (GUILayout.Button("REPETIR LA MISIÓN", sBoton, GUILayout.Height(64))) repetir = true;
            GUILayout.EndHorizontal();
            if (inf.LlegadaCumplida)
            {
                GUILayout.Space(8);
                if (GUILayout.Button("EXPLORAR CON LA SONDA (FASE POSTERIOR, OPCIONAL)", sBoton, GUILayout.Height(52))) explorar = true;
            }
            GUILayout.Space(10);
            GUILayout.EndArea();
        }

        // ================================================================ Misión con exploración

        void DibujarColumnaIzquierda(InformeMision inf)
        {
            GUILayout.BeginArea(new Rect(40, 25, 900, 1035));

            string titulo; Color color;
            switch (inf.resultado)
            {
                case ResultadoMision.Exito: titulo = tituloExito; color = new Color(0.45f, 1f, 0.6f); break;
                case ResultadoMision.ExitoParcial: titulo = tituloParcial; color = new Color(1f, 0.82f, 0.35f); break;
                default: titulo = tituloFracaso; color = new Color(1f, 0.45f, 0.4f); break;
            }
            Color previo = sTitulo.normal.textColor;
            sTitulo.normal.textColor = color;
            GUILayout.Label(titulo, sTitulo);
            sTitulo.normal.textColor = previo;

            string duracion = inf.ViajeAbortado
                ? $"Viaje: {InformeMision.FormatoTiempo(inf.segundosViaje)} reales (comprimido)"
                : $"Duración de la exploración: {inf.DuracionTexto()}";
            GUILayout.Label(duracion + " · " + Motivo(inf.motivo), sTexto);

            Seccion("OBJETIVOS");
            if (inf.viajeRealizado)
                Check(!inf.ViajeAbortado, "Llegar al destino y desplegar la sonda");
            Check(inf.ObjetivoCumplido && !inf.ViajeAbortado, $"Transmitir {inf.escaneosRequeridos} escaneos ({inf.escaneosTransmitidos}/{inf.escaneosRequeridos})");
            if (!inf.ViajeAbortado)
                Check(inf.SondaOperativa, "Terminar con la sonda operativa (energía > 0)");
            Check(inf.costeMUSD <= inf.presupuestoMUSD, $"Respetar el presupuesto ({inf.costeMUSD:0}/{inf.presupuestoMUSD:0} M USD)");
            Check(inf.instrumentoRecomendado, "Usar el instrumento recomendado para el objetivo");

            Seccion("CONFIGURACIÓN");
            Linea($"Objetivo: {inf.objetivo}");
            Linea($"Instrumento: {inf.instrumento} · Batería: {inf.bateria}");
            Linea($"Lanzador: {inf.lanzador} · Relevo: {inf.orbita}");
            Linea($"Masa de la sonda {inf.masaSondaKg:0} kg · margen de carga del lanzador {inf.masaLanzamientoKg:0}/{inf.capacidadLanzadorKg:0} kg");

            if (inf.viajeRealizado)
            {
                Seccion("VIAJE EN LA NAVE (DEMOSTRACIÓN)");
                Linea($"Tiempo de viaje: {InformeMision.FormatoTiempo(inf.segundosViaje)} reales · masa de la nave al partir {inf.masaNaveKg:0} kg");
                string etapa = inf.puntosEtapa < 0 ? "sin evaluar" : inf.puntosEtapa > 0 ? "a tiempo" : "fuera de la ventana";
                Linea($"Despegue: {inf.lanzador} · separación de etapa {etapa}");
                Linea($"Casco: {inf.integridadFinal:0} % · impactos {inf.impactos} · escombros esquivados {inf.escombrosEsquivados}");
                Linea($"Combustible: {inf.combustibleFinalKg:0}/{inf.combustibleInicialKg:0} kg · energía de la nave " +
                      $"{inf.energiaNaveFinalWh:0}/{inf.energiaNaveInicialWh:0} Wh");
                if (inf.reduccionCargaSonda > 0f)
                    Linea($"Sonda: empezó con {inf.cargaInicialSondaWh:0}/{inf.capacidadWh:0} Wh (descenso prolongado, regla simplificada; no resta puntos)");
            }

            if (inf.ViajeAbortado)
            {
                Seccion("SONDA");
                Linea("La sonda no llegó a desplegarse: no hubo exploración.");
                GUILayout.FlexibleSpace();
                GUILayout.Label(notaDemo, sPequeno);
                GUILayout.EndArea();
                return;
            }

            Seccion("ENERGÍA");
            Barra("Final", inf.energiaFinalWh, inf.capacidadWh, "Wh", new Color(0.3f, 0.9f, 1f));
            Linea($"Consumida: {inf.energiaConsumidaWh:0.0} Wh · Recargada en la estación: {inf.energiaRecargadaWh:0.0} Wh");
            Linea($"  Sistemas y movimiento {inf.energiaSistemasYMovimientoWh:0.0} Wh · " +
                  $"Escaneo {inf.energiaEscaneoWh:0.0} Wh · Transmisión {inf.energiaTransmisionWh:0.0} Wh");

            Seccion("DATOS");
            Linea($"Recogidos: {inf.datosRecogidosMB:0} MB en {inf.escaneosRealizados} escaneos");
            Linea($"Transmitidos: {inf.datosTransmitidosMB:0} MB · Perdidos en memoria: {inf.datosPerdidosMB:0} MB");
            Linea($"Tiempo sin contacto con el relevo: {inf.segundosSinContacto:0} s · Memoria llena: {inf.vecesMemoriaLlena} veces");

            GUILayout.FlexibleSpace();
            GUILayout.Label(notaDemo, sPequeno);
            GUILayout.EndArea();
        }

        void DibujarColumnaDerecha(InformeMision inf, ref bool volver, ref bool repetir)
        {
            GUILayout.BeginArea(new Rect(980, 25, 900, 1035));

            Seccion("POR QUÉ");
            desplazamiento = GUILayout.BeginScrollView(desplazamiento, GUILayout.Height(inf.EnEquipo ? 330 : 560));
            foreach (string e in inf.explicaciones)
            {
                GUILayout.Label("• " + e, sTexto);
                GUILayout.Space(6);
            }
            GUILayout.EndScrollView();

            if (inf.EnEquipo)
            {
                Seccion($"PRIORIDADES DE CADA ROL ({inf.PrioridadesCumplidas}/{ModoEquipo.MaxJugadores})");
                for (int i = 0; i < ModoEquipo.MaxJugadores; i++)
                {
                    EstadoPrioridad p = inf.prioridades[i];
                    int jugador = inf.responsables != null && i < inf.responsables.Length ? inf.responsables[i] : 0;
                    Check(p.cumple, $"{ModoEquipo.NombresRol[i]} ({ModoEquipo.NombreJugador(jugador)}): {p.detalle}");
                    GUILayout.Label("     Resultado: " + inf.resultadoRol[i], sPequeno);
                }
            }

            Seccion("PUNTUACIÓN (DEMOSTRACIÓN)");
            Linea($"Ciencia: {inf.puntosCiencia} ({inf.escaneosTransmitidos} × {inf.valorPorEscaneo})");
            Linea($"Objetivo: {inf.bonoObjetivo}");
            Linea($"Presupuesto: {inf.bonoPresupuesto} " +
                  $"({inf.puntosPorMUSDSobrante:0.##} por M USD sobrante, solo si se cumple el objetivo)");
            string viaje;
            if (!inf.viajeRealizado) viaje = "Viaje: 0 (no se pasó por la nave)";
            else if (inf.ViajeAbortado || !inf.viajeEvaluado) viaje = "Viaje: 0 (la sonda no se desplegó)";
            else viaje = $"Viaje: {inf.puntosViaje} (reparación +{inf.puntosReparacion}, escombros +{inf.puntosEscombros}, " +
                         (inf.puntosEtapa < 0 ? "separación de etapa sin evaluar)" : $"separación +{inf.puntosEtapa})");
            Linea(viaje);
            GUILayout.Label($"TOTAL: {inf.puntuacionTotal} puntos", sSeccion);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("VOLVER A CONFIGURAR", sBoton, GUILayout.Height(64))) volver = true;
            if (GUILayout.Button("REPETIR CON LA MISMA CONFIGURACIÓN", sBoton, GUILayout.Height(64))) repetir = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.EndArea();
        }

        static string Motivo(MotivoFin m)
        {
            switch (m)
            {
                case MotivoFin.ObjetivoCumplido: return "fin: objetivo cumplido";
                case MotivoFin.BateriaAgotada: return "fin: batería agotada";
                case MotivoFin.ViajeAbortado: return "fin: misión abortada durante el viaje";
                case MotivoFin.LlegadaAlDestino: return "fin: órbita establecida";
                default: return "fin: terminada por el equipo en la estación";
            }
        }

        void Seccion(string t)
        {
            GUILayout.Space(14);
            GUILayout.Label(t, sSeccion);
        }

        void Linea(string t) => GUILayout.Label(t, sTexto);

        void Check(bool ok, string t)
        {
            GUILayout.Label((ok ? "[OK] " : "[X] ") + t, ok ? sOk : sNo);
        }

        void Barra(string etiqueta, float valor, float maximo, string unidad, Color color)
        {
            GUILayout.Label($"{etiqueta}: {valor:0.0} / {maximo:0} {unidad}", sTexto);
            Rect r = GUILayoutUtility.GetRect(600, 18, GUILayout.ExpandWidth(true));
            Rellenar(r, new Color(1f, 1f, 1f, 0.12f));
            float f = maximo > 0f ? Mathf.Clamp01(valor / maximo) : 0f;
            Rellenar(new Rect(r.x, r.y, r.width * f, r.height), color);
            GUILayout.Space(4);
        }

        static void Rellenar(Rect r, Color c)
        {
            Color previo = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previo;
        }

        void PrepararEstilos()
        {
            if (sTitulo != null) return;

            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
            sTitulo.normal.textColor = Color.white;

            sSeccion = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            sSeccion.normal.textColor = new Color(0.55f, 0.85f, 0.95f);

            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, richText = true };
            sTexto.normal.textColor = Color.white;

            sPequeno = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            sPequeno.normal.textColor = new Color(0.78f, 0.88f, 0.93f);

            sBoton = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true };

            sOk = new GUIStyle(sTexto);
            sOk.normal.textColor = new Color(0.45f, 1f, 0.6f);

            sNo = new GUIStyle(sTexto);
            sNo.normal.textColor = new Color(1f, 0.45f, 0.4f);
        }
    }
}
