using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrototipoMision
{
    /// <summary>
    /// Pantallas del hangar terrestre (SOLO DIBUJO y botones; IMGUI escalado a 1080 como el resto del prototipo).
    /// Banda de estado, controles siempre visibles, nombre de la interacción cercana, las terminales, el panel lateral del
    /// holograma (comparar y encargar piezas) y el puesto de combustible. Durante el despegue no dibuja nada (lo hace
    /// SecuenciaDespegue). Toda la lógica está en HangarTerrestre, HologramaInteractivo, BalanceEM4X y CargaHangar.
    /// </summary>
    public class HudHangarTerrestre : MonoBehaviour
    {
        HangarTerrestre h;
        GUIStyle sTitulo, sTexto, sPequeno, sAviso, sBien, sMal, sBoton, sGrande, sMira, sCasilla;
        PuntoHangar ultimaAbierta;
        float escala, ancho;
        Vector2 desp;

        void Start() { h = GetComponent<HangarTerrestre>(); }

        // Las acciones de los botones se ejecutan en Update, no en mitad de OnGUI: así un botón que cambia la pantalla
        // no altera el número de controles entre los pasos de dibujo de IMGUI.
        System.Action pendiente;
        void Hacer(System.Action a) { pendiente = a; }
        void Update()
        {
            if (pendiente == null) return;
            System.Action a = pendiente;
            pendiente = null;
            a();
        }

        void Estilos()
        {
            if (sTexto != null) return;
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true, wordWrap = true };
            sTexto.normal.textColor = new Color(0.92f, 0.95f, 1f);
            sTitulo = new GUIStyle(sTexto) { fontSize = 19, fontStyle = FontStyle.Bold };
            sTitulo.normal.textColor = new Color(0.55f, 0.9f, 1f);
            sGrande = new GUIStyle(sTexto) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sPequeno = new GUIStyle(sTexto) { fontSize = 14 };
            sBien = new GUIStyle(sPequeno); sBien.normal.textColor = new Color(0.5f, 0.9f, 0.55f);
            sMal = new GUIStyle(sPequeno); sMal.normal.textColor = new Color(1f, 0.45f, 0.4f);
            sAviso = new GUIStyle(sTexto) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            sAviso.normal.textColor = new Color(1f, 0.86f, 0.45f);
            sMira = new GUIStyle(sTexto) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sMira.normal.textColor = new Color(1f, 0.9f, 0.5f);
            sBoton = new GUIStyle(GUI.skin.button) { fontSize = 16, richText = true, wordWrap = true };
            sCasilla = new GUIStyle(GUI.skin.toggle) { fontSize = 16, richText = true };
            sCasilla.normal.textColor = new Color(0.92f, 0.95f, 1f);
        }

        static void Fondo(Rect r, float a = 0.78f)
        {
            Color c = GUI.color;
            GUI.color = new Color(0f, 0.04f, 0.08f, a);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
        }

        void OnGUI()
        {
            if (h == null || !h.Listo || h.EnDespegue) return;
            Estilos();
            escala = Screen.height / 1080f;
            ancho = Screen.width / escala;
            GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
            ResumenExpedicion r = h.Resumen;
            bool cerrada = ExpedicionActual.Confirmada;

            // Banda de estado
            Rect banda = new Rect(14f, 10f, ancho - 28f, 62f);
            Fondo(banda);
            int pend = 0; string falta = "";
            foreach (HangarTerrestre.Requisito q in h.Requisitos()) if (!q.ok) { pend++; if (falta.Length == 0) falta = q.texto; }
            GUI.Label(new Rect(banda.x + 12f, banda.y + 6f, banda.width - 24f, 52f),
                $"<b>HANGAR TERRESTRE</b> · Misión: <b>{(CargaHangar.MisionElegida ? r.mision.nombre : "sin elegir")}</b> · " +
                $"Presupuesto previsto <b>{r.costeTotalMUSD:0.#} de {r.presupuestoMUSD:0} M USD</b> · Preparación <b>{(cerrada ? "CERRADA" : "abierta")}</b>\n" +
                (pend == 0 ? "<color=#59CC73>Todo listo: ve al EMBARQUE (al fondo).</color>" : $"<color=#FFD070>Siguiente: {falta}</color>" + (pend > 1 ? $" (y {pend - 1} más)" : "")), sTexto);

            if (h.AvisoVisible)
            {
                Rect ra = new Rect(ancho / 2f - 560f, 80f, 1120f, 50f);
                Fondo(ra);
                GUI.Label(ra, h.Aviso, sAviso);
            }

            if (h.Abierta != null) { Terminal(h.Abierta); return; }

            // Mira, interacción cercana y controles visibles
            if (!h.CursorLibre) GUI.Label(new Rect(ancho / 2f - 20f, 520f, 40f, 40f), "+", sMira);
            if (h.PuntoCercano != null)
            {
                Rect rp = new Rect(ancho / 2f - 380f, 575f, 760f, 46f);
                Fondo(rp);
                GUI.Label(rp, (h.CursorLibre ? "(cursor libre: clic para volver) " : "") + Accion(h.PuntoCercano) + h.PuntoCercano.nombre, sMira);
            }
            else if (h.Carro != null && h.Carro.Empujando)
            {
                Rect rp = new Rect(ancho / 2f - 380f, 575f, 760f, 46f);
                Fondo(rp);
                GUI.Label(rp, "E: soltar el carro", sMira);
            }
            if (h.Carro != null && (h.Carro.Empujando || h.Carro.Lote != null))
            {
                Rect rk = new Rect(ancho / 2f - 380f, 630f, 760f, 34f);
                Fondo(rk, 0.6f);
                GUI.Label(rk, h.EstadoCarro(), h.Carro.Bloqueado ? sMal : sPequeno);
            }
            Rect rc = new Rect(14f, 1010f, 1300f, 56f);
            Fondo(rc);
            GUI.Label(new Rect(rc.x + 12f, rc.y + 6f, rc.width - 24f, 46f),
                "<b>W/A/S/D</b> caminar · <b>Ratón</b> mirar · <b>Mayús</b> correr · <b>E</b> usar terminal, holograma, carro o estantería cercana · " +
                "<b>R</b> recuperar el carro · <b>Esc</b> liberar el cursor (otra vez Esc o clic: volver) · en una terminal, <b>Esc</b> o «Cerrar» para salir", sPequeno);
        }

        string Accion(PuntoHangar p)
        {
            switch (p.tipo)
            {
                case EstacionHangar.Carro: return "E: empujar · ";
                case EstacionHangar.Estanteria: return h.Carro != null && h.Carro.Empujando ? "E: cargar el contenedor en el carro · " : "E: consultar · ";
                case EstacionHangar.Holograma: return "E: consultar y comparar · ";
                case EstacionHangar.Integracion: return "E: elegir y comparar piezas · ";
                case EstacionHangar.Soporte: return "E: preparar y cargar en el VC-1 · ";
                default: return "E: usar · ";
            }
        }

        // ------------------------------------------------------------------ Terminales

        void Terminal(PuntoHangar p)
        {
            if (p != ultimaAbierta) { ultimaAbierta = p; desp = Vector2.zero; }
            if (p.tipo == EstacionHangar.Holograma) { PanelHolograma(); return; }
            if (p.tipo == EstacionHangar.Integracion) { PanelIntegracion(); return; }
            Rect r = p.tipo == EstacionHangar.Combustible ? new Rect(ancho / 2f - 430f, 220f, 860f, 600f) : new Rect(ancho / 2f - 600f, 140f, 1200f, 860f);
            Fondo(r, 0.92f);
            GUILayout.BeginArea(new Rect(r.x + 20f, r.y + 12f, r.width - 40f, r.height - 24f));
            desp = GUILayout.BeginScrollView(desp);
            switch (p.tipo)
            {
                case EstacionHangar.Mision: Mision(); break;
                case EstacionHangar.Configuracion: Configuracion(); break;
                case EstacionHangar.Manifiesto: Manifiesto(); break;
                case EstacionHangar.Revision: Revision(); break;
                case EstacionHangar.Combustible: Combustible(); break;
                default: Embarque(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("CERRAR (Esc)", sBoton, GUILayout.Height(40f))) Hacer(h.Cerrar);
            GUILayout.EndArea();
        }

        void Bloqueo()
        {
            if (ExpedicionActual.Confirmada)
                GUILayout.Label("Preparación CERRADA: para cambiar algo, reábrela en la terminal 4 · Revisión final.", sMal);
        }

        bool Opcion(bool elegida, string titulo, string detalle, float coste, bool instalado)
        {
            string precio = instalado ? "<color=#80E090>ya instalado en órbita · 0 M</color>" : $"<color=#FFD070>{coste:0.#} M USD + instalación</color>";
            bool pulsado = GUILayout.Button($"{(elegida ? "<b>● " : "<b>○ ")}{titulo}</b> · {precio}\n<size=13>{detalle}</size>", sBoton);
            return pulsado && !elegida;
        }

        int Contador(string titulo, int valor, int maximo, string detalle)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("−", sBoton, GUILayout.Width(44f), GUILayout.Height(32f))) valor--;
            GUILayout.Label($"<b>{titulo}: {valor}</b> (máx. {maximo})  <size=13>{detalle}</size>", sTexto);
            if (GUILayout.Button("+", sBoton, GUILayout.Width(44f), GUILayout.Height(32f))) valor++;
            GUILayout.EndHorizontal();
            return Mathf.Clamp(valor, 0, maximo);
        }

        void Mision()
        {
            GUILayout.Label("1 · TERMINAL DE MISIÓN", sGrande);
            Bloqueo();
            List<DefinicionMisionMVP> misiones = DefinicionMisionMVP.Todas();
            for (int i = 0; i < misiones.Count; i++)
            {
                DefinicionMisionMVP m = misiones[i];
                bool activa = CargaHangar.MisionElegida && h.Borrador.mision == i;
                GUILayout.Space(6f);
                GUILayout.Label((activa ? "<color=#59CC73>● SELECCIONADA · </color>" : "") + "<b>" + m.nombre + "</b>", sTitulo);
                GUILayout.Label(m.resumen, sTexto);
                GUILayout.Label($"Requisitos: instrumento recomendado «{m.instrumentoRecomendado}» · cambio de velocidad orientativo {m.deltaVOrientativoMS:0} m/s · " +
                                $"unos {m.duracionOrientativaS / 60f:0} min de vuelo" + (m.requiereSonda ? " · <b>necesita la sonda de despliegue</b>" : "") +
                                (m.danoGuionizado ? " · zona con impactos: llevar kits" : ""), sPequeno);
                foreach (string paso in m.pasos) GUILayout.Label("   · " + paso, sPequeno);
                if (!activa && GUILayout.Button("SELECCIONAR ESTA MISIÓN", sBoton, GUILayout.Height(34f))) { int k = i; Hacer(() => h.ElegirMision(k)); }
            }
        }

        void Configuracion()
        {
            ResumenExpedicion r = h.Resumen;
            CatalogoMision cat = h.Catalogo;
            GUILayout.Label("2 · CONFIGURACIÓN ORBITAL DE LA EM-4X", sGrande);
            GUILayout.Label("La EM-4X está ensamblada EN ÓRBITA. Lo que cambies aquí se ENCARGA: se envía desde tierra y se instala antes de que llegue la tripulación. " +
                            "El holograma de la mesa marca en amarillo los módulos afectados; en él puedes previsualizar y comparar cada alternativa antes de encargarla. " +
                            "Valores efectivos de la EM-4X (DEMOSTRACIÓN).", sPequeno);
            Bloqueo();
            PreparacionExpedicion b = h.Borrador;
            GUILayout.Label("MOTOR PRINCIPAL", sTitulo);
            List<OpcionMotorNave> motores = BalanceEM4X.Motores(cat.catalogoNave);
            for (int i = 0; i < motores.Count; i++)
                if (Opcion(b.motor == i, motores[i].nombre, DatosMotor(motores[i]) + "\n" + motores[i].queCambia, motores[i].costeMUSD, i == 0)) { int k = i; Hacer(() => h.Cambiar(x => x.motor = k)); }
            GUILayout.Label("TANQUE", sTitulo);
            List<OpcionTanqueNave> tanques = BalanceEM4X.Tanques(cat.catalogoNave);
            for (int i = 0; i < tanques.Count; i++)
                if (Opcion(b.tanque == i, tanques[i].nombre, DatosTanque(tanques[i]) + "\n" + tanques[i].queCambia, tanques[i].costeMUSD, i == 0)) { int k = i; Hacer(() => h.Cambiar(x => x.tanque = k)); }
            GUILayout.Label("INSTRUMENTO CIENTÍFICO", sTitulo);
            List<OpcionInstrumento> instrumentos = BalanceEM4X.Instrumentos(cat);
            for (int i = 0; i < instrumentos.Count; i++)
            {
                OpcionInstrumento ins = instrumentos[i];
                string det = DatosInstrumento(ins) + "\n" + UtilidadInstrumento(ins, r.mision) + (string.IsNullOrEmpty(ins.queCambia) ? "" : "\n" + ins.queCambia);
                if (Opcion(b.instrumento == i, ins.nombre, det, ins.costeMUSD, i == 0)) { int k = i; Hacer(() => h.Cambiar(x => x.instrumento = k)); }
            }
            GUILayout.Label("COMUNICACIONES (relevo)", sTitulo);
            for (int i = 0; i < cat.orbitas.Count; i++)
            {
                OpcionOrbita o = cat.orbitas[i];
                string det = $"{o.velocidadEnlaceMBs:0} MB/s · " + (o.fraccionContacto >= 0.999f ? "enlace continuo" : "necesita línea de visión con la base");
                if (Opcion(b.relevo == i, o.nombre, det, o.costeMUSD, i == 0)) { int k = i; Hacer(() => h.Cambiar(x => x.relevo = k)); }
            }
            GUILayout.Label("COMBUSTIBLE", sTitulo);
            int max = BalanceEM4X.MaximoTramos(r.tanque);
            int t = Contador("Tramos enviados desde tierra", b.tramosCombustible, max,
                $"{BalanceEM4X.TramoCombustibleKg:0} kg y {BalanceEM4X.CosteTramoCombustibleMUSD:0.#} M cada uno · ya hay {BalanceEM4X.CombustibleRemanenteKg:0} kg a bordo");
            if (t != b.tramosCombustible) Hacer(() => h.Cambiar(x => x.tramosCombustible = t));
            GUILayout.Label($"Combustible cargado <b>{r.combustibleKg / 1000f:0.0} t</b> de <b>{r.capacidadKg / 1000f:0} t</b> de capacidad del tanque", sTexto);

            GUILayout.Label("VALORES EFECTIVOS DE LA EM-4X (balance provisional · DEMOSTRACIÓN)", sTitulo);
            GUILayout.Label($"Masa seca {r.masaSecaKg / 1000f:0.0} t · masa total {r.masaTotalKg / 1000f:0.0} t\n" +
                            $"Motor {r.motor.empujeN / 1000f:0} kN, {r.motor.consumoKgS:0} kg/s → aceleración {r.aceleracionMS2:0.00} m/s² · maniobra {r.aceleracionManiobraMS2:0.00} m/s²\n" +
                            $"<b>Cambio de velocidad disponible ≈ {r.deltaVMS:0} m/s</b> (misión: unos {r.mision.deltaVOrientativoMS:0} m/s)", sTexto);
            Presupuesto(r);
        }

        void Presupuesto(ResumenExpedicion r)
        {
            GUILayout.Label("PRESUPUESTO PREVISTO (no se cobra nada hasta cerrar la preparación)", sTitulo);
            string partida = "";
            foreach (LineaPresupuesto l in r.lineas)
            {
                if (l.partida != partida) { partida = l.partida; GUILayout.Label("<b>" + partida + "</b>", sPequeno); }
                GUILayout.Label($"   {l.concepto}: <color=#FFD070>{l.costeMUSD:0.#} M</color>", sPequeno);
            }
            GUILayout.Label($"<b>TOTAL {r.costeTotalMUSD:0.#} de {r.presupuestoMUSD:0} M USD · sobran {r.presupuestoMUSD - r.costeTotalMUSD:0.#} M</b>",
                            h.PresupuestoSuperado ? sMal : sTexto);
        }

        static string DatosMotor(OpcionMotorNave o) =>
            $"empuje {o.empujeN / 1000f:0} kN · consumo {o.consumoKgS:0} kg/s · masa {o.masaKg:0} kg · precio {o.costeMUSD:0.#} M USD";

        static string DatosTanque(OpcionTanqueNave o) =>
            $"capacidad {o.capacidadKg:0} kg · masa vacío {o.masaVaciaKg:0} kg · precio {o.costeMUSD:0.#} M USD · " +
            $"admite hasta {BalanceEM4X.MaximoTramos(o)} tramos desde tierra";

        static string DatosInstrumento(OpcionInstrumento ins) =>
            $"captura de {ins.segundosEscaneo * 3f:0} s · {ins.datosPorEscaneoMB:0} MB por captura · valor {ins.valorCientifico} · " +
            $"consumo {ins.consumoEscaneoW * BalanceEM4X.FactorConsumoCatalogo / 1000f:0.#} kW al capturar · {ins.masaKg:0} kg · precio {ins.costeMUSD:0.#} M USD";

        /// <summary>Utilidad del instrumento en cada una de las tres misiones (recomendado = cada captura vale el doble).</summary>
        static string UtilidadInstrumento(OpcionInstrumento ins, DefinicionMisionMVP actual)
        {
            List<string> partes = new List<string>();
            foreach (DefinicionMisionMVP m in DefinicionMisionMVP.Todas())
            {
                bool rec = BalanceEM4X.Recomendado(m, ins);
                string nombre = m == actual || m.nombre == actual.nombre ? "<b>" + m.nombre + "</b>" : m.nombre;
                partes.Add(nombre + (rec ? " <color=#59CC73>recomendado (×2)</color>" : " <color=#FFB040>no recomendado (×1)</color>"));
            }
            return "Utilidad: " + string.Join(" · ", partes);
        }

        void Manifiesto()
        {
            ResumenExpedicion r = h.Resumen;
            PreparacionExpedicion b = h.Borrador;
            GUILayout.Label("3 · CARGA DE LA EXPEDICIÓN (manifiesto)", sGrande);
            GUILayout.Label("Elige aquí cuánto se envía. Después lleva cada lote con el CARRO: empújalo (E en su asa), acércalo a la estantería y " +
                            "pulsa E para fijar el contenedor. Según la lanzadera, cada lote se entrega en la ENTREGA AL VC-1 (amarilla, zona C2, al fondo a la izquierda) " +
                            "o en el MUELLE DE LA LANZADERA (verde, zona B); al llegar al sitio correcto queda LISTO. Los componentes de la EM-4X no van en el carro: " +
                            "aparecen en su soporte de la zona C2 y se preparan con E (la grúa los carga en el VC-1). " +
                            "Cada lote es un solo contenedor con todas sus unidades. Preparar no crea más carga ni se cobra otra vez: confirma lo presupuestado.", sPequeno);
            Bloqueo();
            int s = Contador("Suministros extra", b.suministrosExtra, BalanceEM4X.MaximoSuministrosExtra,
                $"{BalanceEM4X.CosteSuministroMUSD:0.#} M · +{BalanceEM4X.SegundosPorSuministro / 60f:0} min de soporte vital · ya hay {BalanceEM4X.SuministrosABordo} a bordo");
            if (s != b.suministrosExtra) Hacer(() => h.Cambiar(x => x.suministrosExtra = s));
            int k = Contador("Kits de reparación extra", b.kitsExtra, BalanceEM4X.MaximoKitsExtra,
                $"{BalanceEM4X.CosteKitMUSD:0.#} M · cada kit sella una brecha · ya hay {BalanceEM4X.KitsABordo} a bordo");
            if (k != b.kitsExtra) Hacer(() => h.Cambiar(x => x.kitsExtra = k));
            bool sonda = GUILayout.Toggle(b.sonda, $" Sonda de despliegue · {h.Catalogo.costeBaseMUSD:0} M USD" + (r.mision.requiereSonda ? " · <b>la necesita la misión elegida</b>" : ""),
                                          sCasilla, GUILayout.Height(30f));
            if (sonda != b.sonda) Hacer(() => h.Cambiar(x => x.sonda = sonda));
            GUILayout.Label("El combustible se elige en la terminal 2 · Configuración orbital y su llenado se autoriza en el puesto 3b · Combustible (a la derecha).", sPequeno);

            GUILayout.Label("LOTES A PREPARAR", sTitulo);
            if (h.Lotes.Count == 0) GUILayout.Label("No se envía nada desde tierra con la selección actual.", sBien);
            foreach (LoteCarga lo in h.Lotes)
            {
                string como = lo.id == "combustible" ? "autorizar en el puesto de combustible"
                            : CargaHangar.EsComponente(lo.id) ? "E en su soporte de la zona C2 → la grúa lo carga en el VC-1"
                            : h.Carro != null && h.Carro.Lote == lo.id ? "EN EL CARRO"
                            : lo.enLanzadera ? "carro → muelle de la lanzadera (B)" : "carro → zona de envío orbital (C)";
                GUILayout.Label($"{(lo.preparado ? "<color=#59CC73>[x] PREPARADO</color>" : $"<color=#FFB040>[ ] POR PREPARAR · {como}</color>")} · <b>{lo.nombre}</b>: {lo.detalle.Replace("\n", ", ")}", sTexto);
            }
            GUILayout.Label("Si cambias una cantidad o un componente, solo ese lote vuelve a quedar por preparar (su contenedor vuelve a la estantería). " +
                            "Si un cambio de la lanzadera hace que un lote cambie de vehículo, también hay que volver a prepararlo.", sPequeno);
            if (r.lanzadera != null)
                GUILayout.Label($"Lanzadera L-4: {DatosLanzadera.Resumen(r.lanzadera.eleccion)} · " +
                                (r.lanzadera.envioOrbital ? "hace falta envío orbital aparte" : "no hace falta envío orbital aparte"), sPequeno);
            if (h.Carro != null) GUILayout.Label(h.EstadoCarro() + " · si se atasca o se pierde, pulsa R fuera de la terminal para devolverlo a su base.", sPequeno);
        }

        void Revision()
        {
            ResumenExpedicion r = h.Resumen;
            GUILayout.Label("4 · REVISIÓN FINAL", sGrande);
            foreach (HangarTerrestre.Requisito q in h.Requisitos())
                GUILayout.Label((q.ok ? "<color=#59CC73>[OK]</color> " : "<color=#FF6050>[ ]</color> ") + q.texto, sTexto);
            if (r.problemas.Count > 0)
            {
                GUILayout.Label("PROBLEMAS (impiden cerrar la preparación)", sTitulo);
                foreach (string p in r.problemas) GUILayout.Label("[X] " + p, sMal);
            }
            GUILayout.Label("AVISOS (no impiden salir)", sTitulo);
            if (r.avisos.Count == 0) GUILayout.Label("Ninguno.", sBien);
            foreach (string a in r.avisos) GUILayout.Label("<color=#FFD070>[!] " + a + "</color>", sPequeno);
            GUILayout.Label($"Resumen: {r.mision.nombre} · {r.motor.nombre} · {r.tanque.nombre} · {r.instrumento.nombre} · {r.relevo.nombre} · " +
                            $"combustible {r.combustibleKg / 1000f:0.0} t · {r.suministros} suministros · {r.kits} kits · " + (r.sonda ? "con sonda" : "sin sonda") +
                            (r.lanzadera != null ? $"\nLanzadera L-4 (aparte): {DatosLanzadera.Resumen(r.lanzadera.eleccion)} · {Viaja(r.lanzadera)}" : ""), sPequeno);
            Presupuesto(r);
            GUILayout.Space(8f);
            if (!ExpedicionActual.Confirmada)
            {
                if (GUILayout.Button("CERRAR LA PREPARACIÓN (confirmar esta configuración)", sBoton, GUILayout.Height(46f))) Hacer(h.ConfirmarPreparacion);
            }
            else
            {
                GUILayout.Label("Preparación CERRADA: la misión usará exactamente esta configuración.", sBien);
                if (GUILayout.Button("Reabrir la preparación (se conservan las selecciones)", sBoton, GUILayout.Height(36f))) Hacer(h.ReabrirPreparacion);
            }
        }

        void Embarque()
        {
            GUILayout.Label("5 · EMBARQUE · LANZADERA L-4", sGrande);
            ResumenExpedicion re = h.Resumen;
            bool envio = re.transporte != null && re.transporte.necesario;
            GUILayout.Label((envio ? "Primero se despacha el VC-1 (transporte orbital no tripulado): lanzamiento desde su plataforma exterior, llegada a la zona de servicio " +
                                     "e instalación de lo encargado en la EM-4X. "
                                   : "No hace falta envío orbital: no despega ningún transporte no tripulado. ") +
                            "Después, la L-4 (terrestre) con la configuración confirmada: carga, traslado, embarque, cuenta atrás, ascenso y atraque en la EM-4X; " +
                            "terminas a bordo, controlando al personaje. No es pilotable. " +
                            (SecuenciaDespegue.PuedeOmitirse ? "Las partes ya vistas se pueden omitir con Espacio o Intro (el resultado es el mismo)." : "La primera vez se ve entera; después se podrá omitir."), sPequeno);
            bool listo = true;
            foreach (HangarTerrestre.Requisito q in h.Requisitos())
            {
                GUILayout.Label((q.ok ? "<color=#59CC73>[OK]</color> " : "<color=#FF6050>[FALTA]</color> ") + q.texto, sTexto);
                if (!q.ok) listo = false;
            }
            GUILayout.Space(10f);
            if (listo)
            {
                if (GUILayout.Button("CONFIRMAR LA SALIDA Y EMBARCAR", sBoton, GUILayout.Height(54f))) Hacer(h.Embarcar);
            }
            else GUILayout.Label("No se puede embarcar todavía: completa lo marcado como FALTA.", sMal);
        }
    
        // ------------------------------------------------------------------ Puesto de combustible (comprobación corta)

        void Combustible()
        {
            GUILayout.Label("3b · PUESTO DE COMBUSTIBLE", sGrande);
            GUILayout.Label("Aquí no se compra combustible: se autoriza el llenado del que ya está presupuestado en la terminal 2. " +
                            "No cambia cantidades ni presupuesto; marca el lote de combustible como preparado en la lista de embarque.", sPequeno);
            LoteCarga lo = h.LoteCombustible;
            if (lo == null)
            {
                GUILayout.Label("Con la selección actual no se envía combustible desde tierra (0 tramos): no hay nada que autorizar.", sBien);
                return;
            }
            GUILayout.Label(lo.enLanzadera ? "Este combustible viaja en el TANQUE DE TRANSFERENCIA de la lanzadera L-4." : "Este combustible viaja en el ENVÍO ORBITAL sin tripulación.", sTexto);
            GUILayout.Label($"Lote: <b>{lo.detalle.Replace("\n", ", ")}</b> · combustible total a bordo de la EM-4X previsto {h.Resumen.combustibleKg / 1000f:0.0} t de {h.Resumen.capacidadKg / 1000f:0} t", sTexto);
            GUILayout.Space(8f);
            GUILayout.Label((h.AlimentacionVerificada ? "<color=#59CC73>[OK]</color>" : "<color=#FFB040>[1]</color>") + " Verificar la alimentación de la bomba y la línea" + (lo.enLanzadera ? " hasta la lanzadera" : " hasta la zona de envío"), sTexto);
            if (!h.AlimentacionVerificada && GUILayout.Button("VERIFICAR ALIMENTACIÓN", sBoton, GUILayout.Height(40f))) Hacer(h.VerificarAlimentacion);
            GUILayout.Label((lo.preparado ? "<color=#59CC73>[OK]</color>" : "<color=#FFB040>[2]</color>") + " Autorizar el llenado del combustible presupuestado", sTexto);
            if (!lo.preparado)
            {
                GUI.enabled = h.AlimentacionVerificada;
                if (GUILayout.Button(h.AlimentacionVerificada ? "AUTORIZAR EL LLENADO" : "AUTORIZAR EL LLENADO (antes verifica la alimentación)", sBoton, GUILayout.Height(40f))) Hacer(h.AutorizarLlenado);
                GUI.enabled = true;
            }
            else GUILayout.Label("Llenado autorizado. Si cambias los tramos, el tanque o el tanque de transferencia de la lanzadera, habrá que autorizarlo de nuevo.", sBien);
        }

        // ------------------------------------------------------------------ Holograma: panel lateral (comparar y encargar)

        int cacheFrame = -1;
        PiezaHolo cachePieza;
        readonly List<ResumenExpedicion> variantes = new List<ResumenExpedicion>();

        int NumAlternativas(PiezaHolo t) =>
            t == PiezaHolo.Motor ? BalanceEM4X.Motores(h.Catalogo.catalogoNave).Count
            : t == PiezaHolo.Tanque ? BalanceEM4X.Tanques(h.Catalogo.catalogoNave).Count
            : BalanceEM4X.Instrumentos(h.Catalogo).Count;

        int Actual(PiezaHolo t) => t == PiezaHolo.Motor ? h.Borrador.motor : t == PiezaHolo.Tanque ? h.Borrador.tanque : h.Borrador.instrumento;

        /// <summary>Resúmenes hipotéticos de cada alternativa (una vez por fotograma como mucho).</summary>
        void Variantes(PiezaHolo t)
        {
            if (cacheFrame == Time.frameCount && cachePieza == t) return;
            cacheFrame = Time.frameCount; cachePieza = t;
            variantes.Clear();
            int n = NumAlternativas(t);
            for (int i = 0; i < n; i++)
            {
                int k = i;
                variantes.Add(BalanceEM4X.Variante(h.Borrador, h.Catalogo, p =>
                {
                    if (t == PiezaHolo.Motor) p.motor = k;
                    else if (t == PiezaHolo.Tanque) p.tanque = k;
                    else p.instrumento = k;
                }));
            }
        }

        static string Dif(float nuevo, float actual, string formato, string unidad, bool masEsMejor)
        {
            float d = nuevo - actual;
            if (Mathf.Abs(d) < 0.05f) return nuevo.ToString(formato) + unidad;
            bool mejor = (d > 0f) == masEsMejor;
            return $"{nuevo.ToString(formato)}{unidad} <color={(mejor ? "#59CC73" : "#FF7060")}>({(d > 0f ? "+" : "")}{d.ToString(formato)})</color>";
        }

        void PanelHolograma()
        {
            HologramaInteractivo holo = h.Holo;
            PiezaHolo t = holo.Seleccion;
            Variantes(t);
            ResumenExpedicion act = h.Resumen;
            int actual = Actual(t);
            int cand = holo.Candidata;

            Rect r = new Rect(ancho - 700f, 140f, 680f, 860f);
            Fondo(r, 0.9f);
            Mouse m = Mouse.current;
            if (m != null)
            {
                Vector2 pr = m.position.ReadValue();
                Vector2 gui = new Vector2(pr.x, Screen.height - pr.y) / escala;
                holo.RuedaBloqueada = r.Contains(gui);
            }

            GUILayout.BeginArea(new Rect(r.x + 16f, r.y + 10f, r.width - 32f, r.height - 20f));
            GUILayout.Label("HOLOGRAMA DE LA EM-4X · PREVISUALIZACIÓN", sTitulo);
            GUILayout.Label("La EM-4X sigue EN ÓRBITA: esto es una vista de la configuración. Lo que encargues se envía desde tierra y se instala allí. " +
                            "Geometría provisional (no es el modelo final).", sPequeno);
            GUILayout.Label("<b>Botón derecho + arrastrar</b> o <b>flechas</b>: girar · <b>rueda</b> (fuera del panel): acercar · <b>Esc</b>: salir", sPequeno);
            Bloqueo();

            GUILayout.BeginHorizontal();
            foreach (PiezaHolo p in new[] { PiezaHolo.Motor, PiezaHolo.Tanque, PiezaHolo.Instrumento })
            {
                string nombre = p == PiezaHolo.Motor ? "MOTOR" : p == PiezaHolo.Tanque ? "TANQUE" : "INSTRUMENTO";
                GUI.enabled = !holo.Animando;
                if (GUILayout.Button(p == t ? $"<b>[ {nombre} ]</b>" : nombre, sBoton, GUILayout.Height(36f)) && p != t) { PiezaHolo q = p; Hacer(() => holo.Elegir(q)); }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            desp = GUILayout.BeginScrollView(desp, GUILayout.Height(470f));
            for (int i = 0; i < variantes.Count; i++)
            {
                ResumenExpedicion v = variantes[i];
                string nombre, datos, texto;
                if (t == PiezaHolo.Motor) { nombre = v.motor.nombre; datos = DatosMotor(v.motor); texto = v.motor.queCambia; }
                else if (t == PiezaHolo.Tanque) { nombre = v.tanque.nombre; datos = DatosTanque(v.tanque); texto = v.tanque.queCambia; }
                else { nombre = v.instrumento.nombre; datos = DatosInstrumento(v.instrumento) + "\n" + UtilidadInstrumento(v.instrumento, act.mision); texto = v.instrumento.queCambia; }
                string estado = i == actual ? (i == 0 ? "<color=#80E090>INSTALADA</color>" : "<color=#FFD070>ENCARGADA</color>") : i == cand ? "<color=#FFF070>EN PREVISUALIZACIÓN</color>" : "";
                string adv = v.problemas.Count > 0 ? "\n<color=#FF7060>[X] INCOMPATIBLE: " + v.problemas[0] + "</color>" : "";
                foreach (string a in v.avisos)
                    if (!act.avisos.Contains(a)) { adv += "\n<color=#FFB040>[!] " + a + "</color>"; break; }
                GUI.enabled = !holo.Animando;
                if (GUILayout.Button($"<b>{(i == actual ? "● " : "○ ")}{nombre}</b> {estado}\n<size=13>{datos}{(string.IsNullOrEmpty(texto) ? "" : "\n" + texto)}{adv}</size>", sBoton) && i != cand)
                {
                    int k = i;
                    Hacer(() => holo.Previsualizar(k));
                }
                GUI.enabled = true;
            }
            GUILayout.EndScrollView();

            // Comparación actual frente a la previsualizada
            GUILayout.Label("COMPARACIÓN (antes de encargar)", sTitulo);
            if (cand < 0 || cand >= variantes.Count)
            {
                GUILayout.Label(holo.Animando ? "Animando la separación y el encaje…" : "Pulsa una alternativa para verla en el holograma y compararla con la configuración actual.", sPequeno);
            }
            else
            {
                ResumenExpedicion v = variantes[cand];
                GUILayout.Label($"Coste total previsto: {Dif(v.costeTotalMUSD, act.costeTotalMUSD, "0.#", " M", false)} de {v.presupuestoMUSD:0} M USD\n" +
                                $"Cambio de velocidad: {Dif(v.deltaVMS, act.deltaVMS, "0", " m/s", true)} (misión ≈ {v.mision.deltaVOrientativoMS:0} m/s)\n" +
                                $"Aceleración: {Dif(v.aceleracionMS2, act.aceleracionMS2, "0.00", " m/s²", true)} · masa total {Dif(v.masaTotalKg / 1000f, act.masaTotalKg / 1000f, "0.0", " t", false)}\n" +
                                $"Combustible {v.combustibleKg / 1000f:0.0} de {v.capacidadKg / 1000f:0} t" +
                                (t == PiezaHolo.Instrumento ? $" · instrumento {(v.instrumentoRecomendado ? "<color=#59CC73>recomendado</color>" : "<color=#FFB040>no recomendado</color>")} para {v.mision.nombre}" : ""), sTexto);
                if (v.combustibleKg + 0.5f < act.combustibleKg)
                    GUILayout.Label($"<color=#FFB040>[!] Con este tanque caben menos tramos: se cargarían {v.combustibleKg / 1000f:0.0} t en lugar de {act.combustibleKg / 1000f:0.0} t.</color>", sPequeno);
                bool incompatible = v.problemas.Count > 0;
                foreach (string p in v.problemas) GUILayout.Label("[X] " + p, sMal);
                GUILayout.BeginHorizontal();
                GUI.enabled = !holo.Animando && !ExpedicionActual.Confirmada;
                if (GUILayout.Button(incompatible ? "ENCARGAR IGUALMENTE (no se podrá cerrar así)" : "ENCARGAR ESTE CAMBIO", sBoton, GUILayout.Height(40f)))
                {
                    int k = cand; PiezaHolo q = t;
                    Hacer(() => h.EncargarPieza(q, k));
                }
                GUI.enabled = !holo.Animando;
                if (GUILayout.Button("Volver a la pieza actual", sBoton, GUILayout.Height(40f))) { int k = actual; Hacer(() => holo.Previsualizar(k)); }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SALIR DEL HOLOGRAMA (Esc)", sBoton, GUILayout.Height(38f))) Hacer(h.Cerrar);
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ Integración de la lanzadera L-4 (zona B)

        int cacheFrameL = -1;
        PiezaLanzadera cachePiezaL;
        readonly List<ResumenExpedicion> variantesL = new List<ResumenExpedicion>();

        void VariantesL(PiezaLanzadera t)
        {
            if (cacheFrameL == Time.frameCount && cachePiezaL == t) return;
            cacheFrameL = Time.frameCount; cachePiezaL = t;
            variantesL.Clear();
            int n = DatosLanzadera.Opciones(t).Count;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                // Solo cambia la configuración de la LANZADERA (copia completa: la EM-4X y el borrador no se tocan)
                variantesL.Add(BalanceEM4X.Variante(h.Borrador, h.Catalogo, p => DatosLanzadera.Fijar(p.lanzadera, t, k)));
            }
        }

        static string Viaja(ResumenLanzadera l) =>
            (l.enLanzadera.Count > 0 ? "en la L-4: " + string.Join(", ", l.enLanzadera) : "la L-4 no lleva carga") + " · " +
            (l.envioOrbital ? "envío orbital aparte (10 M): " + string.Join(", ", l.enEnvio) : "sin envío orbital aparte");

        void PanelIntegracion()
        {
            IntegracionLanzadera il = h.Integracion;
            PiezaLanzadera t = il.Seleccion;
            VariantesL(t);
            ResumenExpedicion act = h.Resumen;
            ResumenLanzadera la = act.lanzadera;
            int actual = DatosLanzadera.Indice(h.Borrador.lanzadera, t);
            int cand = il.Candidata;

            Rect r = new Rect(ancho - 720f, 140f, 700f, 860f);
            Fondo(r, 0.9f);
            Mouse m = Mouse.current;
            if (m != null)
            {
                Vector2 pr = m.position.ReadValue();
                il.RuedaBloqueada = r.Contains(new Vector2(pr.x, Screen.height - pr.y) / escala);
            }

            GUILayout.BeginArea(new Rect(r.x + 16f, r.y + 10f, r.width - 32f, r.height - 20f));
            GUILayout.Label("B · INTEGRACIÓN DE LA LANZADERA L-4 (vehículo terrestre)", sTitulo);
            GUILayout.Label("Solo se configura la lanzadera que sube a la tripulación. La EM-4X (en órbita) no cambia aquí. " +
                            "Valores de DEMOSTRACIÓN: reglas de juego explícitas, no prestaciones validadas. Geometría provisional.", sPequeno);
            GUILayout.Label("<b>Botón derecho + arrastrar</b> o <b>flechas</b>: girar · <b>rueda</b> (fuera del panel): acercar · <b>Esc</b>: salir", sPequeno);
            Bloqueo();
            if (la != null)
                GUILayout.Label($"Ahora: masa extra {la.masaExtraKg:0} de {la.capacidadKg:0} kg · opciones {la.costeOpcionesMUSD:0.#} M\n{Viaja(la)}",
                                la.CapacidadSuperada ? sMal : sPequeno);

            GUILayout.BeginHorizontal();
            foreach (PiezaLanzadera p in new[] { PiezaLanzadera.Bodega, PiezaLanzadera.Tanque, PiezaLanzadera.Refuerzos })
            {
                string nombre = p == PiezaLanzadera.Bodega ? "BODEGA" : p == PiezaLanzadera.Tanque ? "TANQUE TRANSF." : "REFUERZOS";
                GUI.enabled = !il.Animando;
                if (GUILayout.Button(p == t ? $"<b>[ {nombre} ]</b>" : nombre, sBoton, GUILayout.Height(36f)) && p != t) { PiezaLanzadera q = p; Hacer(() => il.Elegir(q)); }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            desp = GUILayout.BeginScrollView(desp, GUILayout.Height(400f));
            List<OpcionLanzadera> ops = DatosLanzadera.Opciones(t);
            for (int i = 0; i < ops.Count && i < variantesL.Count; i++)
            {
                OpcionLanzadera o = ops[i];
                ResumenExpedicion v = variantesL[i];
                string estado = i == actual ? (i == 0 ? "<color=#80E090>DE SERIE</color>" : "<color=#FFD070>CONFIRMADA</color>") : i == cand ? "<color=#FFF070>EN PREVISUALIZACIÓN</color>" : "";
                string datos = t == PiezaLanzadera.Refuerzos ? $"sube hasta {o.capacidadKg:0} kg extra · {o.costeMUSD:0.#} M"
                             : $"admite {o.capacidadKg:0} kg · pesa {o.masaKg:0} kg · {o.costeMUSD:0.#} M";
                string adv = v.problemas.Count > 0 ? "\n<color=#FF7060>[X] " + v.problemas[0] + "</color>" : "";
                foreach (string a in v.avisos)
                    if (!act.avisos.Contains(a) && (a.Contains("lanzadera") || a.Contains("refuerzos") || a.Contains("envío orbital"))) { adv += "\n<color=#FFB040>[!] " + a + "</color>"; break; }
                GUI.enabled = !il.Animando;
                if (GUILayout.Button($"<b>{(i == actual ? "● " : "○ ")}{o.nombre}</b> {estado}\n<size=13>{o.queHace}\n{datos}\n<i>Efecto: {o.consecuencia}</i>{adv}</size>", sBoton) && i != cand)
                {
                    int k = i;
                    Hacer(() => il.Previsualizar(k));
                }
                GUI.enabled = true;
            }
            GUILayout.EndScrollView();

            GUILayout.Label("COMPARACIÓN (antes de confirmar)", sTitulo);
            if (cand < 0 || cand >= variantesL.Count)
                GUILayout.Label(il.Animando ? "La grúa retira y coloca la pieza…" : "Pulsa una alternativa: la grúa la coloca en el vehículo y aquí se compara con lo actual.", sPequeno);
            else
            {
                ResumenExpedicion v = variantesL[cand];
                ResumenLanzadera lv = v.lanzadera;
                if (lv != null && la != null)
                    GUILayout.Label($"Coste total previsto: {Dif(v.costeTotalMUSD, act.costeTotalMUSD, "0.#", " M", false)} de {v.presupuestoMUSD:0} M USD\n" +
                                    $"Masa extra de la L-4: {Dif(lv.masaExtraKg, la.masaExtraKg, "0", " kg", false)} de {lv.capacidadKg:0} kg" +
                                    (lv.CapacidadSuperada ? " <color=#FF7060>SUPERA LA CAPACIDAD</color>" : "") + "\n" +
                                    Viaja(lv) + "\n" +
                                    "<i>La EM-4X no cambia: mismo Δv, misma masa y mismo combustible a bordo.</i>", sTexto);
                foreach (string p in v.problemas) GUILayout.Label("[X] " + p, sMal);
                GUILayout.BeginHorizontal();
                bool superada = lv != null && lv.CapacidadSuperada;     // no se confirma una lanzadera que no puede subir su masa
                GUI.enabled = !il.Animando && !ExpedicionActual.Confirmada && !superada;
                if (GUILayout.Button(superada ? "NO SE PUEDE CONFIRMAR: supera la capacidad (añade refuerzos antes)" : v.problemas.Count > 0 ? "CONFIRMAR IGUALMENTE (no se podrá cerrar así)" : "CONFIRMAR ESTA PIEZA", sBoton, GUILayout.Height(40f)))
                {
                    int k = cand; PiezaLanzadera q = t;
                    Hacer(() => h.EncargarLanzadera(q, k));
                }
                GUI.enabled = !il.Animando;
                if (GUILayout.Button("Volver a la pieza actual", sBoton, GUILayout.Height(40f))) { int k = actual; Hacer(() => il.Previsualizar(k)); }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SALIR DE LA INTEGRACIÓN (Esc)", sBoton, GUILayout.Height(38f))) Hacer(h.Cerrar);
            GUILayout.EndArea();
        }
}
}
