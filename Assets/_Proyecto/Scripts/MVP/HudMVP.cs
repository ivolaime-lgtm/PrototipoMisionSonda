using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Pantallas y paneles del MVP (SOLO DIBUJO e interacción de botones; IMGUI escalado a 1080 como el resto del prototipo).
    /// Portada, preparación con presupuesto, mensajes de transición, panel del interior, consolas de los puestos,
    /// instrumentos de Pilotaje con mapa orbital y resultados. Toda la lógica está en EstadoExpedicion y BalanceEM4X.
    /// </summary>
    public class HudMVP : MonoBehaviour
    {
        JuegoMVP juego;
        GUIStyle sTitulo, sTexto, sPequeno, sAviso, sBanda, sMal, sBien, sMira, sBoton, sGrande, sAlarma;
        float escala, ancho;
        Vector2 despIzq, despDer, despConsola;
        bool portada = true, confirmarRescate;

        void Start() { juego = GetComponent<JuegoMVP>(); }

        void Estilos()
        {
            if (sTexto != null) return;
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true, wordWrap = true };
            sTexto.normal.textColor = new Color(0.92f, 0.95f, 1f);
            sTitulo = new GUIStyle(sTexto) { fontSize = 19, fontStyle = FontStyle.Bold };
            sTitulo.normal.textColor = new Color(0.55f, 0.9f, 1f);
            sGrande = new GUIStyle(sTexto) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sPequeno = new GUIStyle(sTexto) { fontSize = 14 };
            sMal = new GUIStyle(sPequeno); sMal.normal.textColor = new Color(1f, 0.45f, 0.4f);
            sBien = new GUIStyle(sPequeno); sBien.normal.textColor = new Color(0.5f, 0.9f, 0.55f);
            sAviso = new GUIStyle(sTexto) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            sAviso.normal.textColor = new Color(1f, 0.86f, 0.45f);
            sAlarma = new GUIStyle(sAviso) { fontStyle = FontStyle.Bold };
            sAlarma.normal.textColor = new Color(1f, 0.35f, 0.3f);
            sBanda = new GUIStyle(sTexto) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sBanda.normal.textColor = new Color(0.55f, 0.9f, 1f);
            sMira = new GUIStyle(sTexto) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sMira.normal.textColor = new Color(1f, 0.9f, 0.5f);
            sBoton = new GUIStyle(GUI.skin.button) { fontSize = 16, richText = true, wordWrap = true };
        }

        static void Fondo(Rect r, float alfa = 0.78f)
        {
            Color c = GUI.color;
            GUI.color = new Color(0f, 0.04f, 0.08f, alfa);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
        }

        static void Punto(float x, float y, float lado, Color color)
        {
            Color c = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x - lado * 0.5f, y - lado * 0.5f, lado, lado), Texture2D.whiteTexture);
            GUI.color = c;
        }

        void Barra(string nombre, float fraccion, string valor, Color color)
        {
            GUILayout.Label($"{nombre}: {valor}", sPequeno);
            Rect r = GUILayoutUtility.GetRect(10f, 8f, GUILayout.ExpandWidth(true));
            Color c = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraccion), r.height), Texture2D.whiteTexture);
            GUI.color = c;
        }

        void OnGUI()
        {
            if (juego == null) return;
            Estilos();
            escala = Screen.height / 1080f;
            ancho = Screen.width / escala;
            GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));

            switch (juego.FaseActual)
            {
                case JuegoMVP.Fase.Cargando:
                    Rect r0 = new Rect(ancho / 2f - 300f, 500f, 600f, 60f);
                    Fondo(r0);
                    GUI.Label(r0, "Construyendo la EM-4X…", sAviso);
                    return;
                case JuegoMVP.Fase.Preparacion:
                    if (portada) Portada(); else Preparacion();
                    break;
                case JuegoMVP.Fase.Transicion: Transicion(); break;
                case JuegoMVP.Fase.Mision: Mision(); break;
                case JuegoMVP.Fase.Resultados: Resultados(); break;
            }

            if (juego.AvisoVisible && !(juego.FaseActual == JuegoMVP.Fase.Preparacion && portada))
            {
                Rect r = new Rect(ancho / 2f - 560f, juego.FaseActual == JuegoMVP.Fase.Mision ? 150f : 1010f, 1120f, 54f);
                Fondo(r);
                GUI.Label(r, juego.Aviso, sAviso);
            }
        }

        // ------------------------------------------------------------------ Portada

        void Portada()
        {
            Rect r = new Rect(ancho / 2f - 520f, 260f, 1040f, 520f);
            Fondo(r, 0.85f);
            GUILayout.BeginArea(new Rect(r.x + 30f, r.y + 20f, r.width - 60f, r.height - 40f));
            GUILayout.Label("EXPEDICIÓN EM-4X", sGrande);
            GUILayout.Label("Diseña, prepara y vuela una misión completa de ida y vuelta", sBanda);
            GUILayout.Space(14f);
            GUILayout.Label("Prototipo de un equipo universitario para el reto de diseño de misiones espaciales. " +
                            "Todo es DEMOSTRACIÓN: el sistema «Kora», la nave EM-4X, sus masas, precios y distancias son ficticios " +
                            "y están a escala reducida. No es un producto oficial ni está respaldado por ninguna agencia espacial.", sTexto);
            GUILayout.Space(10f);
            GUILayout.Label("1. Prepara la expedición con 100 M USD: misión, motor, tanque, instrumento, combustible, suministros y kits.\n" +
                            "2. La tripulación sube en la lanzadera a la EM-4X, que ya está ensamblada en órbita.\n" +
                            "3. Muévete por la nave y usa los puestos: Pilotaje, Ciencia, Comunicaciones, Ingeniería y Bodega.\n" +
                            "4. Vuela con gravedad real del planeta y de su luna, cumple los objetivos y vuelve a la base.", sTexto);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("EMPEZAR: PREPARAR LA EXPEDICIÓN", sBoton, GUILayout.Height(54f))) { portada = false; juego.Sonar("ok"); }
            if (GUILayout.Button("Salir", sBoton, GUILayout.Height(34f))) Application.Quit();
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ Preparación

        bool Opcion(bool elegida, string titulo, string detalle, float coste, bool instalado)
        {
            string precio = instalado ? "<color=#80E090>ya instalado · 0 M</color>" : $"<color=#FFD070>{coste:0.#} M USD</color>";
            string marca = elegida ? "<b>● " : "<b>○ ";
            bool pulsado = GUILayout.Button($"{marca}{titulo}</b> · {precio}\n<size=13>{detalle}</size>", sBoton);
            return pulsado && !elegida;
        }

        int Contador(string titulo, int valor, int minimo, int maximo, string detalle)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("−", sBoton, GUILayout.Width(44f), GUILayout.Height(32f))) valor--;
            GUILayout.Label($"<b>{titulo}: {valor}</b>  <size=13>{detalle}</size>", sTexto);
            if (GUILayout.Button("+", sBoton, GUILayout.Width(44f), GUILayout.Height(32f))) valor++;
            GUILayout.EndHorizontal();
            return Mathf.Clamp(valor, minimo, maximo);
        }

        void Preparacion()
        {
            PreparacionExpedicion b = juego.Borrador;
            ResumenExpedicion r = juego.Resumen;
            CatalogoMision cat = juego.Catalogo;
            bool cerrada = ExpedicionActual.Confirmada;
            string antes = JsonUtility.ToJson(b);

            Rect banda = new Rect(ancho / 2f - 560f, 10f, 1120f, 58f);
            Fondo(banda);
            GUI.Label(banda, "PREPARACIÓN EN TIERRA · la EM-4X ya está ensamblada EN ÓRBITA junto a la base (se ve al fondo)\n" +
                             "<size=14>Lo que encargues se envía e instala antes de que llegue la tripulación. Valores de DEMOSTRACIÓN.</size>", sBanda);

            // ---- Columna izquierda: elecciones
            Rect izq = new Rect(14f, 78f, 640f, 990f);
            Fondo(izq);
            GUILayout.BeginArea(new Rect(izq.x + 10f, izq.y + 8f, izq.width - 20f, izq.height - 16f));
            despIzq = GUILayout.BeginScrollView(despIzq);
            GUI.enabled = !cerrada;

            GUILayout.Label("MISIÓN", sTitulo);
            List<DefinicionMisionMVP> misiones = DefinicionMisionMVP.Todas();
            for (int i = 0; i < misiones.Count; i++)
                if (GUILayout.Button((b.mision == i ? "<b>● " : "<b>○ ") + misiones[i].nombre + "</b>\n<size=13>" + misiones[i].resumen + "</size>", sBoton))
                    b.mision = i;

            GUILayout.Label("MOTOR PRINCIPAL", sTitulo);
            List<OpcionMotorNave> motores = BalanceEM4X.Motores(cat.catalogoNave);
            for (int i = 0; i < motores.Count; i++)
                if (Opcion(b.motor == i, motores[i].nombre, motores[i].queCambia, motores[i].costeMUSD, i == 0)) b.motor = i;

            GUILayout.Label("TANQUE (capacidad; el combustible se carga aparte)", sTitulo);
            List<OpcionTanqueNave> tanques = BalanceEM4X.Tanques(cat.catalogoNave);
            for (int i = 0; i < tanques.Count; i++)
                if (Opcion(b.tanque == i, tanques[i].nombre, tanques[i].queCambia, tanques[i].costeMUSD, i == 0)) b.tanque = i;

            GUILayout.Label("INSTRUMENTO CIENTÍFICO", sTitulo);
            for (int i = 0; i < cat.instrumentos.Count; i++)
            {
                OpcionInstrumento ins = cat.instrumentos[i];
                string det = $"Captura de {ins.segundosEscaneo * 3f:0} s · {ins.datosPorEscaneoMB:0} MB · valor {ins.valorCientifico} · " +
                             $"consumo {ins.consumoEscaneoW * BalanceEM4X.FactorConsumoCatalogo / 1000f:0.#} kW · {ins.masaKg:0} kg";
                if (Opcion(b.instrumento == i, ins.nombre, det, ins.costeMUSD, i == 0)) b.instrumento = i;
            }

            GUILayout.Label("RELEVO DE COMUNICACIONES", sTitulo);
            for (int i = 0; i < cat.orbitas.Count; i++)
            {
                OpcionOrbita o = cat.orbitas[i];
                string det = o.fraccionContacto >= 0.999f
                    ? $"{o.velocidadEnlaceMBs:0} MB/s · enlace continuo aunque Kora tape la base"
                    : $"{o.velocidadEnlaceMBs:0} MB/s · necesita línea de visión con la base (Kora puede taparla)";
                if (Opcion(b.relevo == i, o.nombre, det, o.costeMUSD, i == 0)) b.relevo = i;
            }

            GUILayout.Label("CARGA DE LA EXPEDICIÓN", sTitulo);
            int maxTramos = BalanceEM4X.MaximoTramos(tanques[Mathf.Clamp(b.tanque, 0, tanques.Count - 1)]);
            b.tramosCombustible = Contador("Tramos de combustible", Mathf.Min(b.tramosCombustible, maxTramos), 0, maxTramos,
                $"× {BalanceEM4X.TramoCombustibleKg:0} kg · {BalanceEM4X.CosteTramoCombustibleMUSD:0.#} M cada uno · ya hay {BalanceEM4X.CombustibleRemanenteKg:0} kg a bordo");
            b.suministrosExtra = Contador("Suministros extra", b.suministrosExtra, 0, BalanceEM4X.MaximoSuministrosExtra,
                $"{BalanceEM4X.CosteSuministroMUSD:0.#} M · +{BalanceEM4X.SegundosPorSuministro / 60f:0} min de soporte vital cada uno");
            b.kitsExtra = Contador("Kits de reparación extra", b.kitsExtra, 0, BalanceEM4X.MaximoKitsExtra,
                $"{BalanceEM4X.CosteKitMUSD:0.#} M · cada kit sella una brecha");
            bool sonda = GUILayout.Toggle(b.sonda, $" Sonda de despliegue · {cat.costeBaseMUSD:0} M USD · {cat.masaBaseKg:0} kg (necesaria en la misión 3)", GUILayout.Height(28f));
            b.sonda = sonda;
            GUI.enabled = true;
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (!cerrada && JsonUtility.ToJson(b) != antes) juego.Recalcular();

            // ---- Columna derecha: presupuesto, prestaciones, requisitos
            Rect der = new Rect(ancho - 654f, 78f, 640f, 990f);
            Fondo(der);
            GUILayout.BeginArea(new Rect(der.x + 10f, der.y + 8f, der.width - 20f, der.height - 16f));
            despDer = GUILayout.BeginScrollView(despDer);
            GUILayout.Label(cerrada ? "PRESUPUESTO CONFIRMADO" : "PRESUPUESTO PREVISTO (no se descuenta nada hasta confirmar)", sTitulo);
            string partida = "";
            foreach (LineaPresupuesto l in r.lineas)
            {
                if (l.partida != partida) { partida = l.partida; GUILayout.Label("<b>" + partida + "</b>", sPequeno); }
                GUILayout.Label($"   {l.concepto}: <color=#FFD070>{l.costeMUSD:0.#} M</color>", sPequeno);
            }
            bool pasa = r.costeTotalMUSD > r.presupuestoMUSD + 0.001f;
            GUILayout.Label($"<b>TOTAL: {r.costeTotalMUSD:0.#} de {r.presupuestoMUSD:0} M USD · sobran {r.presupuestoMUSD - r.costeTotalMUSD:0.#} M</b>", pasa ? sMal : sTexto);

            if (r.motor != null && r.tanque != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label("BALANCE PROVISIONAL DE LA EM-4X (DEMOSTRACIÓN)", sTitulo);
                GUILayout.Label($"Masa seca {r.masaSecaKg / 1000f:0.0} t (estructura {r.masaEstructuraKg / 1000f:0} t + motor {r.motor.masaKg / 1000f:0.0} t + tanque {r.tanque.masaVaciaKg / 1000f:0.0} t + carga {r.cargaKg:0} kg)\n" +
                                $"Combustible {r.combustibleKg / 1000f:0.0} t de {r.capacidadKg / 1000f:0} t de capacidad · masa total {r.masaTotalKg / 1000f:0.0} t\n" +
                                $"Motor principal {r.motor.empujeN / 1000f:0} kN y {r.motor.consumoKgS:0} kg/s → aceleración {r.aceleracionMS2:0.00} m/s²\n" +
                                $"Maniobra {BalanceEM4X.Maniobra.empujeTraslacionN / 1000f:0} kN por eje → {r.aceleracionManiobraMS2:0.00} m/s²\n" +
                                $"<b>Cambio de velocidad disponible ≈ {r.deltaVMS:0} m/s</b> (orientativo para la misión: {r.mision.deltaVOrientativoMS:0} m/s)\n" +
                                $"Batería {BalanceEM4X.BateriaWh / 1000f:0} kWh · paneles {BalanceEM4X.GeneracionW / 1000f:0.#} kW · consumo base {BalanceEM4X.ConsumoBaseW / 1000f:0.#} kW\n" +
                                $"Soporte vital {r.soporteVitalS / 60f:0} min de vuelo ({r.suministros} suministros) · kits {r.kits}", sPequeno);
            }
            GUILayout.Space(6f);
            GUILayout.Label("REQUISITOS Y AVISOS", sTitulo);
            if (r.problemas.Count == 0) GUILayout.Label("[OK] Sin problemas que impidan confirmar.", sBien);
            foreach (string p in r.problemas) GUILayout.Label("[X] " + p, sMal);
            foreach (string a in r.avisos) GUILayout.Label("<color=#FFD070>[!] " + a + "</color>", sPequeno);

            GUILayout.Space(6f);
            GUILayout.Label("ENVÍO DESDE TIERRA", sTitulo);
            GUILayout.Label(r.enviaDesdeTierra ? "Un solo envío con: " + string.Join(", ", r.enviado) + ". Se instala antes de que llegue la tripulación."
                                               : "No se envía nada: la nave sale con lo que ya tiene a bordo (sin cargo de transporte).", sPequeno);
            GUILayout.Label("La lanzadera L-4 tiene configuración fija y recursos propios, separados de los de la EM-4X.\n" +
                            "<i>Pendiente como ingeniería del mundo: cómo se transportó y montó el nodo central de 10 m.</i>", sPequeno);

            GUILayout.FlexibleSpace();
            if (!cerrada)
            {
                GUI.enabled = r.PuedeConfirmar;
                if (GUILayout.Button("CONFIRMAR LA PREPARACIÓN (se cierra la configuración)", sBoton, GUILayout.Height(48f))) juego.ConfirmarPreparacion();
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("CONFIGURACIÓN CERRADA: la misión usará exactamente esta elección.", sBien);
                if (GUILayout.Button("DESPEGAR HACIA LA EM-4X", sBoton, GUILayout.Height(48f))) juego.Despegar();
                if (GUILayout.Button("Reabrir la preparación", sBoton, GUILayout.Height(34f))) juego.ReabrirPreparacion();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ Transición

        void Transicion()
        {
            ResumenExpedicion r = juego.Resumen;
            Rect c = new Rect(ancho / 2f - 480f, 330f, 960f, 380f);
            Fondo(c, 0.88f);
            GUILayout.BeginArea(new Rect(c.x + 30f, c.y + 20f, c.width - 60f, c.height - 40f));
            if (juego.Tarjeta == 0)
            {
                GUILayout.Label("1 · ENVÍO E INSTALACIÓN", sGrande);
                GUILayout.Label(r.enviaDesdeTierra
                    ? "Un transporte de carga ha llevado a la órbita: " + string.Join(", ", r.enviado) + ".\n\n" +
                      (r.sustituciones > 0 ? $"El equipo de la base ha instalado {r.sustituciones} componente(s) en la EM-4X. " : "") +
                      "Todo está a bordo antes de que llegue la tripulación."
                    : "No se ha enviado nada desde tierra. La EM-4X sale con el equipo y el combustible que ya tenía.", sTexto);
                GUILayout.Label("<i>(Mensaje de transición: en este MVP no se simulan los vuelos de carga ni el montaje.)</i>", sPequeno);
            }
            else if (juego.Tarjeta == 1)
            {
                GUILayout.Label("2 · LANZAMIENTO DE LA TRIPULACIÓN", sGrande);
                GUILayout.Label("La lanzadera L-4 despega desde tierra con la tripulación, alcanza la órbita de la base y atraca bajo el módulo de Mando de la EM-4X.\n\n" +
                                "La lanzadera tiene configuración fija en este MVP: su combustible y sus recursos son suyos y no se mezclan con los de la EM-4X.", sTexto);
                GUILayout.Label("<i>(Mensaje de transición: el despegue terrestre existe aparte en la escena Nave y no se ha modificado.)</i>", sPequeno);
            }
            else
            {
                GUILayout.Label("3 · A BORDO DE LA EM-4X", sGrande);
                GUILayout.Label(r.mision.nombre + "\n\n" + r.mision.resumen + "\n\n" +
                                "Empiezas en el nodo central. La mesa de misión muestra el paso actual. Las puertas de aislamiento están abiertas.", sTexto);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(juego.Tarjeta >= 2 ? "EMPEZAR LA MISIÓN (Intro)" : "Continuar (Intro)", sBoton, GUILayout.Height(46f))) juego.SiguienteTarjeta();
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ Misión

        void Mision()
        {
            EstadoExpedicion s = juego.Estado;
            if (s == null) return;

            // Banda superior: misión, paso y recursos (igual en todos los modos)
            Rect banda = new Rect(14f, 8f, ancho - 28f, 134f);
            Fondo(banda);
            GUILayout.BeginArea(new Rect(banda.x + 10f, banda.y + 4f, banda.width - 20f, banda.height - 8f));
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(ancho * 0.52f));
            GUILayout.Label($"{s.Mision.nombre} · paso {Mathf.Min(s.Paso + 1, s.Mision.pasos.Count)} de {s.Mision.pasos.Count}", sTitulo);
            GUILayout.Label(s.PasoTexto, sTexto);
            if (s.RegresoAnticipado) GUILayout.Label("REGRESO ANTICIPADO RECOMENDADO: soporte vital crítico.", sMal);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            Barra("Combustible", s.Vuelo.Combustible / Mathf.Max(1f, s.Config.capacidadKg), $"{s.Vuelo.Combustible:0} kg · Δv {s.Vuelo.DeltaVMotorRestanteMS:0} m/s", new Color(1f, 0.7f, 0.3f));
            Barra("Energía", s.EnergiaWh / BalanceEM4X.BateriaWh, $"{s.EnergiaWh / 1000f:0.0} kWh · {(s.GeneracionW - s.ConsumoW) / 1000f:+0.0;-0.0} kW", new Color(0.4f, 0.8f, 1f));
            GUILayout.EndVertical();
            GUILayout.Space(12f);
            GUILayout.BeginVertical();
            Barra("Soporte vital", s.SoporteFraccion, $"{s.SoporteS / 60f:0.0} min", s.SoporteFraccion < 0.15f ? new Color(1f, 0.3f, 0.3f) : new Color(0.5f, 0.9f, 0.55f));
            Barra("Casco", s.Integridad / 100f, $"{s.Integridad:0} %", s.Integridad < 50f ? new Color(1f, 0.3f, 0.3f) : new Color(0.8f, 0.85f, 0.9f));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            int minutos = (int)(s.Tiempo / 60.0), segundos = (int)(s.Tiempo % 60.0);
            GUILayout.Label($"Tiempo de vuelo {minutos:00}:{segundos:00} · datos a bordo {s.DatosMB:0}/{s.Config.memoriaMB:0} MB · kits: bodega {s.KitsBodega}{(s.KitEnMano ? " + 1 EN MANO" : "")}", sPequeno);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Alarma de brecha (visible en cualquier modo)
            if (s.HayBrechaAbierta)
            {
                string texto = "";
                foreach (DanoModulo d in s.Danos)
                    if (!d.reparado) texto += $"ALARMA · {(d.grave ? "BRECHA GRAVE" : "BRECHA")} en {d.nombreModulo.ToUpper()} · {(d.aislado ? "módulo AISLADO (fuga reducida)" : "SIN AISLAR: se pierde soporte vital")}   ";
                Rect ra = new Rect(ancho / 2f - 620f, 208f, 1240f, 40f);
                Fondo(ra);
                if (Mathf.Repeat(Time.time, 1f) < 0.7f) GUI.Label(ra, texto, sAlarma);
            }

            if (juego.ModoActual == JuegoMVP.Modo.Pilotaje) Pilotaje(s);
            else if (juego.ModoActual == JuegoMVP.Modo.Consola) Consola(s);
            else if (juego.ModoActual == JuegoMVP.Modo.Interior) Interior(s);
            else
            {
                Rect r = new Rect(ancho / 2f - 520f, 1010f, 1040f, 50f);
                Fondo(r);
                GUI.Label(r, "CÁMARA DE INSPECCIÓN · sin colisiones · el vuelo sigue · Tab: volver al personaje", sBanda);
            }
        }

        void Interior(EstadoExpedicion s)
        {
            GUI.Label(new Rect(ancho / 2f - 20f, 520f, 40f, 40f), "+", sMira);
            PersonajeFlotante pj = juego.Personaje;
            string accion = "";
            if (pj != null && pj.PanelMirado != null && pj.PanelMirado.Union != null)
            {
                PuertasUnion u = pj.PanelMirado.Union;
                bool abierta = u.EstadoActual == PuertasUnion.Estado.Abierta || u.EstadoActual == PuertasUnion.Estado.Abriendo;
                accion = $"E: {(abierta ? "cerrar" : "abrir")} el paso {u.Datos.nombre}";
            }
            else if (juego.PuntoCercano != null)
            {
                PuntoInteraccion p = juego.PuntoCercano;
                accion = p.tipo == TipoPunto.Reparacion ? (juego.TextoReparacion.Length > 0 ? juego.TextoReparacion : "Mantén E: reparar la brecha (" + p.nombre + ")")
                       : p.tipo == TipoPunto.Kits ? $"E: recoger un kit de reparación (quedan {s.KitsBodega})"
                       : p.tipo == TipoPunto.Pilotaje ? "E: tomar el puesto de PILOTAJE"
                       : "E: usar " + p.nombre;
            }
            if (accion.Length > 0)
            {
                Rect ra = new Rect(ancho / 2f - 460f, 574f, 920f, 44f);
                Fondo(ra);
                GUI.Label(ra, accion, sMira);
            }
            string puertas = juego.MensajePuertas();
            if (puertas.Length > 0)
            {
                Rect r = new Rect(ancho / 2f - 520f, 640f, 1040f, 44f);
                Fondo(r);
                GUI.Label(r, puertas, sAviso);
            }
            if (pj != null && pj.Bloqueo.Length > 0)
                GUI.Label(new Rect(ancho / 2f - 520f, 690f, 1040f, 40f), "<color=#FF8060>" + pj.Bloqueo + "</color>", sAviso);

            // Resumen del vuelo mientras nadie pilota: el vuelo no está congelado.
            Rect rv = new Rect(14f, 1000f, 900f, 66f);
            Fondo(rv);
            s.Relativo(s.Sistema.Cuerpo("base"), out float dBase, out float vBase, out float aBase);
            GUI.Label(new Rect(rv.x + 10f, rv.y + 4f, rv.width - 20f, rv.height - 8f),
                $"<b>PILOTAJE SIN ATENDER</b> · el vuelo sigue (motor apagado) · base a {dBase / 1000f:0.00} km, {vBase:0.0} m/s · altitud sobre Kora {s.AltitudKoraM / 1000f:0.0} km\n" +
                juego.AyudasActivas() + (juego.Prediccion != null && juego.Prediccion.impacto ? $"  <color=#FF5040>TRAYECTORIA DE IMPACTO en {juego.Prediccion.segundosImpacto:0} s</color>" : ""), sPequeno);

            if (!juego.AyudaVisible) return;
            Rect ay = new Rect(ancho - 474f, 256f, 460f, 420f);
            Fondo(ay);
            GUILayout.BeginArea(new Rect(ay.x + 10f, ay.y + 6f, ay.width - 20f, ay.height - 12f));
            GUILayout.Label("INTERIOR · " + (pj != null ? pj.ModuloActual : ""), sTitulo);
            GUILayout.Label("Ratón: mirar · W/S/A/D: moverse · Espacio/Ctrl: subir/bajar · Z/X: girar sobre ti · Mayús: rápido · R: nivelar con el módulo\n" +
                            "<b>E</b>: usar el puesto cercano o el panel de puerta que miras · mantén <b>E</b> para reparar\n" +
                            "F5: volver al nodo central · Tab: cámara de inspección · F1: ocultar esta ayuda", sPequeno);
            GUILayout.Label("PUESTOS", sTitulo);
            GUILayout.Label("Pilotaje: Mando (proa) · Ciencia: babor · Comunicaciones: estribor · Ingeniería: popa · Kits: Bodega (abajo; abre su puerta con E) · Mesa de misión: nodo central", sPequeno);
            GUILayout.Label("SI HAY UNA BRECHA", sTitulo);
            GUILayout.Label("1. Mira el diagnóstico (Ingeniería o Mesa). 2. Recoge un kit en la Bodega. 3. Cierra la puerta de aislamiento del módulo para reducir la fuga " +
                            "(cerrar NO repara). 4. Entra, ve a la baliza roja y mantén E. Las puertas se abren desde los dos lados: no te quedas encerrado.", sPequeno);
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ Consolas

        void Consola(EstadoExpedicion s)
        {
            Rect r = new Rect(ancho / 2f - 430f, 256f, 860f, 730f);
            Fondo(r, 0.9f);
            GUILayout.BeginArea(new Rect(r.x + 20f, r.y + 12f, r.width - 40f, r.height - 24f));
            despConsola = GUILayout.BeginScrollView(despConsola);
            string mensaje;
            switch (juego.ConsolaActual)
            {
                case TipoPunto.Ciencia:
                {
                    GUILayout.Label("PUESTO DE CIENCIA", sGrande);
                    GUILayout.Label($"Instrumento: <b>{s.Instrumento.nombre}</b>{(s.Config.instrumentoRecomendado ? " (recomendado: cada captura vale el doble)" : "")} · " +
                                    $"captura de {s.DuracionCapturaS:0} s · {s.Instrumento.datosPorEscaneoMB:0} MB · {s.Instrumento.consumoEscaneoW * BalanceEM4X.FactorConsumoCatalogo / 1000f:0.#} kW mientras captura", sTexto);
                    GUILayout.Label("Bus de ciencia: " + NombreBus(s.BusCiencia) + " (se alimenta en Ingeniería)", sTexto);
                    string falta;
                    bool listo = s.CondicionesCaptura(out falta);
                    GUILayout.Label(listo ? "[OK] Condiciones de captura cumplidas." : "[X] Falta: " + falta + ".", listo ? sBien : sMal);
                    if (s.CapturaProgresoS >= 0f)
                        Barra("Captura en curso (mantén las condiciones)", s.CapturaProgresoS / s.CapturaDuracionS, $"{s.CapturaProgresoS:0.0} de {s.CapturaDuracionS:0} s", new Color(0.45f, 1f, 0.6f));
                    else if (s.CapturaAbortada.Length > 0) GUILayout.Label(s.CapturaAbortada, sMal);
                    string nombreCaptura = s.Mision.id == 1 ? "INICIAR DIAGNÓSTICO DEL SATÉLITE" : s.Mision.id == 2 ? "ESCANEAR EL CÚMULO" : "CAPTURAR OBSERVACIÓN";
                    if (GUILayout.Button(nombreCaptura, sBoton, GUILayout.Height(44f))) juego.Respuesta(s.IniciarCaptura(out mensaje), mensaje);
                    GUILayout.Label($"Capturas: {s.Capturas} de {s.Mision.capturasRequeridas} · transmitidas {s.CapturasTransmitidas} · datos a bordo {s.DatosMB:0} de {s.Config.memoriaMB:0} MB", sTexto);
                    if (s.Mision.id == 1 && s.DiagnosticoHecho)
                        GUILayout.Label("DIAGNÓSTICO: el ordenador de K-2 está bloqueado en modo seguro. Solución: orden de reinicio desde Comunicaciones con el bus de servicio alimentado.", sBien);
                    if (s.Mision.id == 2)
                    {
                        GUILayout.Space(8f);
                        string faltaSonda;
                        bool sondaLista = s.CondicionesSonda(out faltaSonda);
                        GUILayout.Label(sondaLista ? "[OK] Sonda lista para desplegar sobre T-7." : "Sonda: " + faltaSonda + ".", sondaLista ? sBien : sPequeno);
                        if (GUILayout.Button("DESPLEGAR LA SONDA", sBoton, GUILayout.Height(40f))) juego.Respuesta(s.DesplegarSonda(out mensaje), mensaje);
                    }
                    break;
                }
                case TipoPunto.Comunicaciones:
                {
                    GUILayout.Label("PUESTO DE COMUNICACIONES", sGrande);
                    string motivo;
                    bool enlace = s.EnlaceDisponible(out motivo);
                    GUILayout.Label($"Relevo: <b>{s.Config.relevo.nombre}</b> · {s.Config.relevo.velocidadEnlaceMBs:0} MB/s · bus: {NombreBus(s.BusComunicaciones)}", sTexto);
                    GUILayout.Label(enlace ? "[OK] ENLACE CON LA BASE disponible." : "[X] SIN ENLACE: " + motivo + ".", enlace ? sBien : sMal);
                    Barra("Datos por enviar", s.DatosMB / Mathf.Max(1f, s.Config.memoriaMB), $"{s.DatosMB:0} MB a bordo · {s.TransmitidoMB:0} MB recibidos en la base", new Color(0.8f, 0.6f, 1f));
                    GUILayout.Label(s.Transmitiendo ? "TRANSMITIENDO…" : "Transmisión parada.", s.Transmitiendo ? sBien : sPequeno);
                    if (GUILayout.Button(s.Transmitiendo ? "DETENER LA TRANSMISIÓN" : "TRANSMITIR LOS DATOS A LA BASE", sBoton, GUILayout.Height(44f)))
                        juego.Respuesta(s.AlternarTransmision(out mensaje), mensaje);
                    if (GUILayout.Button(s.OperacionSolicitada ? "Operación ya comunicada a la base" : "COMUNICAR A LA BASE EL INICIO DE LA OPERACIÓN", sBoton, GUILayout.Height(36f)))
                    {
                        if (!enlace) juego.Respuesta(false, "Sin enlace: " + motivo + ".");
                        else if (!s.OperacionSolicitada) { s.OperacionSolicitada = true; s.Anotar("Comunicaciones informa a la base del inicio de la operación."); juego.Respuesta(true, "La base confirma: operación registrada."); }
                    }
                    if (s.Mision.id == 1)
                    {
                        GUILayout.Space(8f);
                        string faltaOrden;
                        bool ordenLista = s.CondicionesOrden(out faltaOrden);
                        GUILayout.Label(ordenLista ? "[OK] Orden de reinicio lista." : "Orden al satélite: " + faltaOrden + ".", ordenLista ? sBien : sPequeno);
                        if (s.OrdenProgresoS >= 0f) Barra("Enviando la orden", s.OrdenProgresoS / 6f, $"{s.OrdenProgresoS:0.0} de 6 s", new Color(1f, 0.8f, 0.3f));
                        if (GUILayout.Button("ENVIAR LA ORDEN DE REINICIO AL SATÉLITE K-2", sBoton, GUILayout.Height(40f))) juego.Respuesta(s.EnviarOrden(out mensaje), mensaje);
                        GUILayout.Label("Estado de K-2: " + (s.SateliteReparado ? "OPERATIVO" : "en modo seguro"), s.SateliteReparado ? sBien : sPequeno);
                    }
                    break;
                }
                case TipoPunto.Ingenieria:
                {
                    GUILayout.Label("PUESTO DE INGENIERÍA", sGrande);
                    GUILayout.Label("DIAGNÓSTICO", sTitulo);
                    GUILayout.Label($"Casco {s.Integridad:0} % · impactos {s.Impactos} · soporte vital {s.SoporteS / 60f:0.0} min", sTexto);
                    if (s.Danos.Count == 0) GUILayout.Label("Sin daños registrados.", sBien);
                    foreach (DanoModulo d in s.Danos)
                        GUILayout.Label($"{(d.reparado ? "[reparado]" : d.grave ? "[BRECHA GRAVE]" : "[BRECHA]")} {d.nombreModulo} · {d.causa} a {d.velocidadMS:0.0} m/s" +
                                        (d.guionizado ? " · evento guionizado de demostración" : "") +
                                        (d.reparado ? "" : d.aislado ? " · AISLADO (fuga ×" + BalanceEM4X.FugaAislada + ")" : " · sin aislar"), d.reparado ? sBien : sMal);
                    GUILayout.Label("ENERGÍA", sTitulo);
                    GUILayout.Label($"Paneles {s.GeneracionW / 1000f:0.0} kW{(s.GeneracionReducida ? " (REDUCIDA por avería en Sistemas)" : "")} · consumo {s.ConsumoW / 1000f:0.0} kW · " +
                                    $"balance {(s.GeneracionW - s.ConsumoW) / 1000f:+0.0;-0.0} kW · batería {s.EnergiaWh / 1000f:0.0} de {BalanceEM4X.BateriaWh / 1000f:0} kWh", sTexto);
                    GUILayout.Label("Cada bus alimentado consume aunque no se use. Apágalos cuando no hagan falta.", sPequeno);
                    if (GUILayout.Button($"Bus de CIENCIA: {NombreBus(s.BusCiencia)} · {BalanceEM4X.ConsumoBusCienciaW / 1000f:0.0} kW · necesario para capturar y para la sonda", sBoton, GUILayout.Height(38f)))
                        juego.Respuesta(s.AlternarBus(ref s.BusCiencia, "Bus de ciencia", out mensaje), mensaje);
                    if (GUILayout.Button($"Bus de COMUNICACIONES: {NombreBus(s.BusComunicaciones)} · {BalanceEM4X.ConsumoBusComunicacionesW / 1000f:0.0} kW · necesario para transmitir", sBoton, GUILayout.Height(38f)))
                        juego.Respuesta(s.AlternarBus(ref s.BusComunicaciones, "Bus de comunicaciones", out mensaje), mensaje);
                    if (GUILayout.Button($"Bus de SERVICIO: {NombreBus(s.BusServicio)} · {BalanceEM4X.ConsumoBusServicioW / 1000f:0.0} kW · necesario para la orden al satélite", sBoton, GUILayout.Height(38f)))
                        juego.Respuesta(s.AlternarBus(ref s.BusServicio, "Bus de servicio", out mensaje), mensaje);
                    if (GUILayout.Button("RESTABLECER sistemas protegidos (después de reparar el módulo)", sBoton, GUILayout.Height(38f)))
                        juego.Respuesta(s.Restablecer(out mensaje), mensaje);
                    break;
                }
                default:
                {
                    GUILayout.Label("MESA DE MISIÓN", sGrande);
                    GUILayout.Label(s.Mision.nombre, sTitulo);
                    for (int i = 0; i < s.Mision.pasos.Count; i++)
                        GUILayout.Label((i < s.Paso ? "<color=#59CC73>[x]</color> " : i == s.Paso ? "<color=#FFD070>[>]</color> " : "[ ] ") + s.Mision.pasos[i], i == s.Paso ? sTexto : sPequeno);
                    GUILayout.Label("REGISTRO", sTitulo);
                    for (int i = Mathf.Max(0, s.Registro.Count - 8); i < s.Registro.Count; i++) GUILayout.Label(s.Registro[i], sPequeno);
                    GUILayout.Space(8f);
                    if (!confirmarRescate)
                    {
                        if (GUILayout.Button("Abandonar: pedir rescate (la misión termina como fallida)", sBoton, GUILayout.Height(34f))) confirmarRescate = true;
                    }
                    else
                    {
                        GUILayout.Label("¿Seguro? La misión terminará como FALLIDA.", sMal);
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Sí, pedir rescate", sBoton, GUILayout.Height(34f))) { confirmarRescate = false; s.PedirRescate(); }
                        if (GUILayout.Button("No, seguir", sBoton, GUILayout.Height(34f))) confirmarRescate = false;
                        GUILayout.EndHorizontal();
                    }
                    break;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("DEJAR EL PUESTO (Q)", sBoton, GUILayout.Height(40f))) juego.SalirDeConsola();
            GUILayout.EndArea();
        }

        static string NombreBus(EstadoBus b) =>
            b == EstadoBus.Alimentado ? "<color=#59CC73>ALIMENTADO</color>" : b == EstadoBus.Protegido ? "<color=#FF6050>PROTEGIDO (avería)</color>" : "<color=#B0B8C0>apagado</color>";

        // ------------------------------------------------------------------ Pilotaje

        void Pilotaje(EstadoExpedicion s)
        {
            CuerpoKora obj = s.Objetivo;
            s.Relativo(obj, out float dist, out float vrel, out float acerca);
            double periapsis = SistemaKora.Periapsis(s.Vuelo.Posicion, s.Vuelo.Velocidad);
            double apoapsis = SistemaKora.Apoapsis(s.Vuelo.Posicion, s.Vuelo.Velocidad);
            PrediccionTrayectoria pr = juego.Prediccion;

            // Marca del objetivo en pantalla
            Camera cam = juego.VistaVuelo != null ? juego.VistaVuelo.Camara : null;
            if (cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(juego.VistaVuelo.PosicionMarca);
                if (sp.z > 0f)
                {
                    float x = sp.x / escala, y = (Screen.height - sp.y) / escala;
                    GUI.Label(new Rect(x - 200f, y - 46f, 400f, 30f), $"<color=#FFD840>[ {obj.nombre} · {Distancia(dist)} ]</color>", sAviso);
                }
            }

            Rect izq = new Rect(14f, 256f, 470f, 560f);
            Fondo(izq);
            GUILayout.BeginArea(new Rect(izq.x + 10f, izq.y + 6f, izq.width - 20f, izq.height - 12f));
            GUILayout.Label("PILOTAJE · " + (juego.VistaExterior ? "cámara exterior" : "vista desde el asiento"), sTitulo);
            GUILayout.Label($"OBJETIVO Y REFERENCIA: <b>{obj.nombre}</b> (N: siguiente)", sTexto);
            GUILayout.Label($"Distancia <b>{Distancia(dist)}</b> · velocidad relativa <b>{vrel:0.0} m/s</b>\n" +
                            $"{(acerca >= 0f ? "acercándote" : "alejándote")} a {Mathf.Abs(acerca):0.0} m/s · proa desviada {s.DesvioProaGrados(obj):0}°", sTexto);
            GUILayout.Space(4f);
            GUILayout.Label("ÓRBITA ALREDEDOR DE KORA", sTitulo);
            GUILayout.Label($"Distancia al centro {s.RadioKoraM / 1000f:0.00} km · altitud {s.AltitudKoraM / 1000f:0.00} km · velocidad {s.Vuelo.Rapidez:0.0} m/s\n" +
                            $"Punto más bajo {periapsis / 1000.0:0.0} km · más alto {(double.IsInfinity(apoapsis) ? "escape" : (apoapsis / 1000.0).ToString("0.0") + " km")} (radio de Kora: {SistemaKora.RadioPlanetaM / 1000f:0} km)", sTexto);
            if (pr != null && pr.impacto)
                GUILayout.Label($"TRAYECTORIA DE IMPACTO contra {pr.cuerpoImpacto} en {pr.segundosImpacto:0} s: acelera en el sentido de la marcha (progrado).", sMal);
            else if (pr != null)
                GUILayout.Label($"Sin impacto previsto en {1400f / 60f:0} min · altitud mínima prevista {pr.altitudMinimaM / 1000.0:0.0} km.", sBien);
            GUILayout.Space(4f);
            GUILayout.Label("NAVE", sTitulo);
            GUILayout.Label($"Masa {s.Vuelo.MasaKg / 1000f:0.0} t · motor {s.Vuelo.AceleracionMotorMS2:0.00} m/s² · maniobra {s.Vuelo.AceleracionTraslacionMS2:0.00} m/s² · giro {s.Vuelo.VelocidadAngularGradS.magnitude:0.0}°/s\n" +
                            $"Motor: {(s.Vuelo.MotorEncendido ? "<color=#FFB040>ENCENDIDO</color>" : "apagado")} · gasto: motor {s.Vuelo.GastoMotorKg:0} kg, maniobra {s.Vuelo.GastoTraslacionKg:0} kg, orientación {s.Vuelo.GastoOrientacionKg:0} kg", sPequeno);
            GUILayout.Label($"Estabilización (T): {(s.Vuelo.Estabilizacion ? "activa" : "no")} · orientación automática (P): {EstadoExpedicion.NombresApuntar[s.Apuntar]}\n" +
                            $"Frenado asistido (B): {(s.Frenado.Activa ? AsistenteFrenado.Nombre(s.Frenado.FaseActual) + " respecto a " + obj.nombre : "inactivo")} · tiempo ×{juego.Aceleracion} (, y .)", sPequeno);
            string falta;
            bool enBase = s.EnBase(out falta);
            if (enBase && (s.Paso >= 1 || s.RegresoAnticipado)) GUILayout.Label("EN LA ZONA DE LA BASE: pulsa <b>Intro</b> para confirmar el regreso.", sBien);
            else if (s.Paso >= s.Mision.pasos.Count - 1 || s.RegresoAnticipado) GUILayout.Label("Regreso: " + falta + ".", sPequeno);
            GUILayout.EndArea();

            if (juego.AyudaVisible)
            {
                Rect ay = new Rect(14f, 824f, 900f, 168f);
                Fondo(ay);
                GUI.Label(new Rect(ay.x + 10f, ay.y + 4f, ay.width - 20f, ay.height - 8f),
                    "<b>MANDOS</b> (los de VueloPrueba) · W/S cabeceo · A/D guiñada · Z/C alabeo · I/K adelante/atrás · J/L izquierda/derecha · U/O arriba/abajo · <b>Espacio</b> motor principal\n" +
                    "N objetivo · P orientación automática (objetivo / retrógrado / progrado) · B frenado asistido RESPECTO AL OBJETIVO · T estabilización · , y . tiempo ×1/×4/×16\n" +
                    "V cámara · botón derecho: mirar · M mapa · H esta ayuda · Intro confirmar regreso en la base · <b>Q dejar el puesto</b> (el vuelo sigue)\n" +
                    "<i>Apagar el motor no quita la gravedad: la nave sigue en órbita. Para acercarte a algo importa la velocidad RELATIVA a ese objetivo.\n" +
                    "Receta: N elige objetivo → P apunta la proa → Espacio unos segundos → espera → B frena respecto al objetivo al llegar.</i>", sPequeno);
            }
            if (juego.MapaVisible) Mapa(s, pr);
        }

        static string Distancia(float m) => m >= 10000f ? $"{m / 1000f:0.0} km" : m >= 1000f ? $"{m / 1000f:0.00} km" : $"{m:0} m";

        /// <summary>Mapa orbital: proyección sobre el plano de las órbitas (XZ), Kora en el centro.</summary>
        void Mapa(EstadoExpedicion s, PrediccionTrayectoria pr)
        {
            const float lado = 430f, alcanceM = 42000f;
            Rect r = new Rect(ancho - lado - 14f, 256f, lado, lado + 54f);
            Fondo(r);
            float cx = r.x + lado * 0.5f, cy = r.y + lado * 0.5f, k = lado * 0.5f / alcanceM;
            // Kora y la banda de observación
            for (int i = 0; i < 72; i++)
            {
                float a = i * Mathf.PI * 2f / 72f, co = Mathf.Cos(a), si = Mathf.Sin(a);
                Punto(cx + co * SistemaKora.RadioPlanetaM * k, cy - si * SistemaKora.RadioPlanetaM * k, 3f, new Color(0.35f, 0.55f, 0.9f));
                if (s.Mision.id == 0 && i % 2 == 0)
                {
                    Punto(cx + co * SistemaKora.BandaObservacionMinM * k, cy - si * SistemaKora.BandaObservacionMinM * k, 2f, new Color(0.4f, 0.9f, 0.5f, 0.7f));
                    Punto(cx + co * SistemaKora.BandaObservacionMaxM * k, cy - si * SistemaKora.BandaObservacionMaxM * k, 2f, new Color(0.4f, 0.9f, 0.5f, 0.7f));
                }
                if (i % 3 == 0) Punto(cx + co * (float)SistemaKora.RadioOrbitaBaseM * k, cy - si * (float)SistemaKora.RadioOrbitaBaseM * k, 1.5f, new Color(1f, 1f, 1f, 0.25f));
            }
            // Trayectoria prevista sin motor
            if (pr != null)
                foreach (Vector3d p in pr.puntos)
                {
                    float x = cx + (float)p.x * k, y = cy - (float)p.z * k;
                    if (r.Contains(new Vector2(x, y))) Punto(x, y, 2f, pr.impacto ? new Color(1f, 0.4f, 0.3f) : new Color(0.4f, 0.85f, 1f, 0.8f));
                }
            foreach (CuerpoKora c in s.Objetivos)
            {
                if (c.tipo == TipoCuerpo.Planeta) continue;
                Vector3d p = s.Sistema.Posicion(c, s.Tiempo);
                float x = cx + (float)p.x * k, y = cy - (float)p.z * k;
                if (!r.Contains(new Vector2(x, y))) continue;
                Punto(x, y, c == s.Objetivo ? 9f : 6f, c.color);
                GUI.Label(new Rect(x + 6f, y - 10f, 160f, 20f), "<size=12>" + c.nombre + "</size>", sPequeno);
            }
            float nx = cx + (float)s.Vuelo.Posicion.x * k, ny = cy - (float)s.Vuelo.Posicion.z * k;
            Punto(nx, ny, 8f, Color.white);
            Vector3 proa = s.Vuelo.Proa;
            Punto(nx + proa.x * 12f, ny - proa.z * 12f, 4f, new Color(1f, 0.85f, 0.2f));
            GUI.Label(new Rect(r.x + 8f, r.y + lado - 2f, lado - 16f, 54f),
                "<size=12>MAPA (M) · plano de las órbitas · blanco: EM-4X (amarillo: proa) · azul: trayectoria sin motor de los próximos 23 min" +
                (s.Mision.id == 0 ? " · verde: banda de observación" : "") + " · Nix queda fuera (200 km)</size>", sPequeno);
        }

        // ------------------------------------------------------------------ Resultados

        void Resultados()
        {
            EstadoExpedicion s = juego.Estado;
            if (s == null) return;
            ResumenExpedicion c = s.Config;
            Rect r = new Rect(ancho / 2f - 560f, 60f, 1120f, 960f);
            Fondo(r, 0.9f);
            GUILayout.BeginArea(new Rect(r.x + 30f, r.y + 16f, r.width - 60f, r.height - 32f));
            despConsola = GUILayout.BeginScrollView(despConsola);
            string titulo = s.Resultado == ResultadoExpedicion.Exito ? "<color=#59CC73>MISIÓN CUMPLIDA</color>"
                          : s.Resultado == ResultadoExpedicion.Parcial ? "<color=#FFD070>ÉXITO PARCIAL</color>" : "<color=#FF6050>MISIÓN FALLIDA</color>";
            GUILayout.Label(titulo, sGrande);
            GUILayout.Label(s.Mision.nombre + "\n" + s.MotivoFin, sAviso);
            if (s.Resultado != ResultadoExpedicion.Fracaso)
                GUILayout.Label("La tripulación vuelve a tierra en la lanzadera L-4. La EM-4X queda en la base orbital para la siguiente expedición.", sTexto);

            GUILayout.Label("OBJETIVOS", sTitulo);
            for (int i = 0; i < s.Mision.pasos.Count; i++)
                GUILayout.Label((i < s.Paso ? "<color=#59CC73>[x]</color> " : "<color=#FF6050>[ ]</color> ") + s.Mision.pasos[i], sPequeno);

            int ciencia = Mathf.RoundToInt(s.CapturasTransmitidas * s.ValorCaptura);
            int bonoObjetivo = s.Resultado == ResultadoExpedicion.Exito ? 20 : 0;
            float sobrante = Mathf.Max(0f, c.presupuestoMUSD - c.costeTotalMUSD);
            int bonoPresupuesto = s.Resultado == ResultadoExpedicion.Exito ? Mathf.RoundToInt(sobrante * 0.2f) : 0;
            int penalizacion = 0;
            foreach (DanoModulo d in s.Danos) if (!d.reparado) penalizacion += 5;
            int total = s.Resultado == ResultadoExpedicion.Fracaso ? 0 : Mathf.Max(0, ciencia + bonoObjetivo + bonoPresupuesto - penalizacion);

            GUILayout.Label("QUÉ OCURRIÓ", sTitulo);
            int minutos = (int)(s.Tiempo / 60.0), segundos = (int)(s.Tiempo % 60.0);
            GUILayout.Label($"Tiempo de vuelo {minutos:00}:{segundos:00}\n" +
                            $"Combustible: {s.CombustibleInicialKg:0} kg al salir, {s.Vuelo.Combustible:0} kg al terminar (motor {s.Vuelo.GastoMotorKg:0}, maniobra {s.Vuelo.GastoTraslacionKg:0}, orientación {s.Vuelo.GastoOrientacionKg:0})\n" +
                            $"Energía consumida {s.EnergiaGastadaWh / 1000f:0.0} kWh · batería final {s.EnergiaWh / 1000f:0.0} kWh\n" +
                            $"Soporte vital restante {s.SoporteS / 60f:0.0} min de {c.soporteVitalS / 60f:0} min\n" +
                            $"Capturas {s.Capturas} · transmitidas {s.CapturasTransmitidas} · datos recibidos en la base {s.TransmitidoMB:0} MB · sin transmitir {s.DatosMB:0} MB\n" +
                            $"Impactos {s.Impactos} · casco {s.Integridad:0} % · kits usados {s.KitsUsados} de {c.kits}", sTexto);
            foreach (DanoModulo d in s.Danos)
                GUILayout.Label($"   Daño en {d.nombreModulo}: {d.causa} a {d.velocidadMS:0.0} m/s · {(d.reparado ? "reparado" : "SIN REPARAR")}{(d.guionizado ? " · evento guionizado" : "")}", d.reparado ? sBien : sMal);

            GUILayout.Label("PRESUPUESTO Y PUNTUACIÓN (DEMOSTRACIÓN)", sTitulo);
            GUILayout.Label($"Gastado {c.costeTotalMUSD:0.#} de {c.presupuestoMUSD:0} M USD (sobran {sobrante:0.#} M)\n" +
                            $"Ciencia: {s.CapturasTransmitidas} capturas transmitidas × {s.ValorCaptura:0} = {ciencia} puntos" +
                            (c.instrumentoRecomendado ? " (instrumento recomendado: ×2)" : $" (con «{s.Mision.instrumentoRecomendado}» valdrían el doble)") + "\n" +
                            $"Objetivos y regreso: {bonoObjetivo} · presupuesto sin gastar: {bonoPresupuesto} · brechas sin reparar: −{penalizacion}\n" +
                            $"<b>PUNTUACIÓN: {total}</b>", sTexto);

            GUILayout.Label("POR QUÉ", sTitulo);
            foreach (string linea in Explicacion(s)) GUILayout.Label("· " + linea, sPequeno);

            GUILayout.Label("REGISTRO", sTitulo);
            foreach (string linea in s.Registro) GUILayout.Label(linea, sPequeno);
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (ExpedicionActual.DesdeHangar && GUILayout.Button("VOLVER AL HANGAR TERRESTRE", sBoton, GUILayout.Height(46f))) juego.VolverAlHangar();
            if (GUILayout.Button("NUEVA EXPEDICIÓN (volver a la preparación)", sBoton, GUILayout.Height(46f))) juego.NuevaExpedicion();
            if (GUILayout.Button("Salir", sBoton, GUILayout.Height(46f), GUILayout.Width(160f))) Application.Quit();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        static List<string> Explicacion(EstadoExpedicion s)
        {
            List<string> l = new List<string>();
            ResumenExpedicion c = s.Config;
            float usado = s.CombustibleInicialKg - s.Vuelo.Combustible;
            l.Add($"Saliste con {c.deltaVMS:0} m/s de cambio de velocidad disponible; la misión pedía unos {s.Mision.deltaVOrientativoMS:0}. Gastaste el {usado / Mathf.Max(1f, s.CombustibleInicialKg) * 100f:0} % del combustible.");
            if (s.Vuelo.Combustible <= 1f) l.Add("Te quedaste sin combustible: sin él no se puede frenar ni igualar la velocidad de la base. Más combustible o maniobras más cortas lo evitan.");
            if (!c.instrumentoRecomendado) l.Add("El instrumento no era el recomendado: cada captura valió la mitad.");
            if (s.DatosMB > 0.5f) l.Add("Quedaron datos a bordo sin transmitir: los datos solo cuentan cuando llegan a la base.");
            if (s.Impactos > 0) l.Add("Hubo impactos: el daño depende de la velocidad RELATIVA al chocar. Acercarse despacio evita brechas.");
            foreach (DanoModulo d in s.Danos) if (!d.reparado) { l.Add("Volviste con una brecha abierta: aislar el módulo reduce la fuga, pero solo un kit la sella."); break; }
            if (s.SoporteFraccion < 0.3f) l.Add("El soporte vital acabó justo: las fugas y los vuelos largos lo consumen. Más suministros dan margen.");
            if (s.EnergiaWh < BalanceEM4X.BateriaWh * 0.2f) l.Add("La batería acabó baja: los buses alimentados consumen aunque no se usen.");
            if (s.Resultado == ResultadoExpedicion.Exito && l.Count == 1) l.Add("Preparación equilibrada y vuelo limpio: objetivos cumplidos y regreso con margen.");
            return l;
        }
    }
}
