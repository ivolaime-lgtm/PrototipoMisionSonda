using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Paneles de la prueba de hangar (SOLO DIBUJO, IMGUI escalado a 1080 de alto). Requisitos de la misión, cambio de
    /// motor y tanque, desglose de masa, coste, límites (los mismos de la pantalla de configuración) y ayuda.
    /// Los botones solo llaman a PruebaHangar.CambiarMotor / CambiarTanque.
    /// </summary>
    public class HudHangar : MonoBehaviour
    {
        PruebaHangar prueba;
        GUIStyle sTitulo, sTexto, sPequeno, sAviso, sBoton, sProblema, sAdvertencia, sProvisional;
        Vector2 desplazamiento;
        float escala, ancho;

        void Start() { prueba = GetComponent<PruebaHangar>(); }

        void Estilos()
        {
            if (sTexto != null) return;
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true, wordWrap = true };
            sTexto.normal.textColor = new Color(0.92f, 0.95f, 1f);
            sTitulo = new GUIStyle(sTexto) { fontSize = 21, fontStyle = FontStyle.Bold };
            sPequeno = new GUIStyle(sTexto) { fontSize = 14 };
            sProblema = new GUIStyle(sTexto);
            sProblema.normal.textColor = new Color(1f, 0.45f, 0.4f);
            sAdvertencia = new GUIStyle(sTexto);
            sAdvertencia.normal.textColor = new Color(1f, 0.78f, 0.35f);
            sAviso = new GUIStyle(sTexto) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            sAviso.normal.textColor = new Color(1f, 0.86f, 0.45f);
            sProvisional = new GUIStyle(sTexto) { fontSize = 22, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sProvisional.normal.textColor = new Color(1f, 0.6f, 0.25f);
            sBoton = new GUIStyle(GUI.skin.button) { fontSize = 18, richText = true };
        }

        void Fondo(Rect r)
        {
            Color c = GUI.color;
            GUI.color = new Color(0f, 0.04f, 0.08f, 0.72f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
        }

        void OnGUI()
        {
            if (prueba == null) return;
            Estilos();
            escala = Screen.height / 1080f;
            ancho = Screen.width / escala;
            GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));

            if (prueba.Error.Length > 0)
            {
                GUI.Label(new Rect(20f, 20f, 1200f, 60f), "<b>Hangar:</b> " + prueba.Error, sProblema);
                return;
            }
            if (prueba.Resumen == null || prueba.ResumenNave == null) return;

            PanelConfiguracion();
            PanelDerecho();
            if (prueba.ModoActual == PruebaHangar.Modo.Inspeccion)
            {
                Rect r = new Rect(ancho / 2f - 480f, 16f, 960f, 40f);
                Fondo(r);
                GUI.Label(r, "CÁMARA DE INSPECCIÓN PROVISIONAL · no es el movimiento interior final · sin colisiones", sProvisional);
            }
            if (prueba.AvisoVisible)
            {
                Rect r = new Rect(ancho / 2f - 470f, 64f, 940f, 60f);
                Fondo(r);
                GUI.Label(r, prueba.Aviso, sAviso);
            }
        }

        void PanelConfiguracion()
        {
            Rect r = new Rect(16f, 16f, 600f, 1048f);
            Fondo(r);
            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 10f, r.width - 28f, r.height - 20f));
            desplazamiento = GUILayout.BeginScrollView(desplazamiento);

            CatalogoMision cm = prueba.Catalogo;
            ResumenMision res = prueba.Resumen;
            ResumenNave nave = prueba.ResumenNave;
            CatalogoNave cn = prueba.CatalogoNave;

            GUILayout.Label("REQUISITOS DE LA MISIÓN", sTitulo);
            GUILayout.Label($"<b>{cm.nombreMision}</b> · destino: {cm.NombreDestino(prueba.Eleccion.destino)} · dificultad {cm.dificultadMision}", sTexto);
            GUILayout.Label(cm.objetivoMision, sPequeno);
            foreach (string c in cm.condicionesMision) GUILayout.Label("· " + c, sPequeno);

            GUILayout.Space(8);
            GUILayout.Label("NAVE BASE", sTitulo);
            GUILayout.Label($"<b>{prueba.Base.nombre}</b> · {prueba.Base.tripulantes} tripulantes", sTexto);
            GUILayout.Label(prueba.Base.descripcion, sPequeno);

            GUILayout.Space(8);
            GUILayout.Label("COMPONENTES INSTALADOS", sTitulo);
            OpcionMotorNave mot = nave.motor;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", sBoton, GUILayout.Width(44), GUILayout.Height(36))) prueba.CambiarMotor(-1);
            GUILayout.Label($"<b>Motor:</b> {(mot != null ? mot.nombre : "—")}", sTexto, GUILayout.Height(36));
            if (GUILayout.Button("▶", sBoton, GUILayout.Width(44), GUILayout.Height(36))) prueba.CambiarMotor(1);
            GUILayout.EndHorizontal();
            if (mot != null)
                GUILayout.Label($"<size=15>{mot.queCambia}\nCoste {mot.costeMUSD:0.#} M USD · masa {mot.masaKg:0} kg · empuje {mot.empujeN / 1000f:0} kN · " +
                                $"consumo {mot.consumoKgS:0.0} kg/s · montado en ANC_I2_Motor (piezas en el anclaje: {prueba.Vista.PiezasEnAnclajeMotor})</size>", sTexto);

            OpcionTanqueNave tan = nave.tanque;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", sBoton, GUILayout.Width(44), GUILayout.Height(36))) prueba.CambiarTanque(-1);
            GUILayout.Label($"<b>Tanque:</b> {(tan != null ? tan.nombre : "—")}", sTexto, GUILayout.Height(36));
            if (GUILayout.Button("▶", sBoton, GUILayout.Width(44), GUILayout.Height(36))) prueba.CambiarTanque(1);
            GUILayout.EndHorizontal();
            if (tan != null)
                GUILayout.Label($"<size=15>{tan.queCambia}\nCoste {tan.costeMUSD:0.#} M USD · masa vacío {tan.masaVaciaKg:0} kg · capacidad {tan.capacidadKg:0} kg · " +
                                $"módulo de servicio de {prueba.Base.LargoModuloServicio(tan):0.0} m (piezas: {prueba.Vista.PiezasEnModuloServicio}) · " +
                                $"ANC_I2_Motor en z {prueba.Vista.ZAnclajeMotorActual:0.0} m</size>", sTexto);
            GUILayout.Label("<size=14>Tercer módulo: no se añade. La carga (sonda y relevo) va en la bodega, que sigue PENDIENTE: solo hay una reserva de volumen.</size>", sTexto);

            GUILayout.Space(8);
            GUILayout.Label("MASA <size=14>(datos del catálogo)</size>", sTitulo);
            DesgloseMasa m = prueba.Masas;
            GUILayout.Label($"Estructura (casco): <b>{m.estructuralKg:0} kg</b>", sTexto);
            GUILayout.Label("<size=14><color=#FFB040>PROVISIONAL: el bloque ampliado aún no tiene balance de masa; se mantiene la masa del casco del catálogo.</color></size>", sTexto);
            GUILayout.Label($"Componentes instalados: <b>{m.ComponentesKg:0} kg</b> <size=15>(motor {m.motorKg:0} + tanque vacío {m.tanqueVacioKg:0})</size>", sTexto);
            GUILayout.Label($"Combustible al partir: <b>{m.combustibleKg:0} kg</b> <size=15>(tope del tanque {nave.capacidadTanqueKg:0} kg)</size>", sTexto);
            GUILayout.Label($"Carga científica: <b>{m.cargaKg:0} kg</b> <size=15>(sonda + relevo)</size>", sTexto);
            GUILayout.Label($"<b>Total al partir: {m.totalKg:0} kg</b> · aceleración {nave.aceleracionInicialMS2:0.00} m/s²", sTexto);

            GUILayout.Space(8);
            GUILayout.Label("COSTE Y LANZADOR", sTitulo);
            GUILayout.Label($"Coste total <b>{res.costeTotalMUSD:0.#}</b> de {res.presupuestoMUSD:0} M USD <size=15>(piezas de la nave {res.costeNaveMUSD:0.#})</size>", sTexto);
            GUILayout.Label($"Margen de carga del lanzador: <b>{res.masaLanzamientoKg:0}</b> de {res.capacidadLanzadorKg:0} kg " +
                            $"<size=15>(carga {res.cargaCientificaKg:0} + piezas no estándar {res.masaVariantesNaveKg:0})</size>", sTexto);

            GUILayout.Space(8);
            GUILayout.Label("LÍMITES <size=14>(los mismos de Configuración)</size>", sTitulo);
            foreach (string p in res.problemas) GUILayout.Label("[X] Impide despegar: " + p, sProblema);
            foreach (string a in res.avisos) GUILayout.Label("[!] Advertencia: " + a, sAdvertencia);
            if (res.problemas.Count == 0 && res.avisos.Count == 0) GUILayout.Label("[OK] La configuración cumple todos los límites.", sTexto);

            GUILayout.Space(8);
            GUILayout.Label("<size=14>Cambiar una pieza aquí cambia la configuración de la partida (la misma que usan Configuración y Nave).</size>", sTexto);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void PanelDerecho()
        {
            Rect r = new Rect(ancho - 476f, 16f, 460f, prueba.AyudaVisible ? 610f : 170f);
            Fondo(r);
            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 10f, r.width - 28f, r.height - 20f));
            GUILayout.Label(prueba.ModoActual == PruebaHangar.Modo.Exterior ? "VISTA EXTERIOR" : "INSPECCIÓN INTERIOR (provisional)", sTitulo);
            if (prueba.AyudaVisible)
            {
                if (prueba.ModoActual == PruebaHangar.Modo.Exterior)
                    GUILayout.Label("<size=15>Botón derecho del ratón: girar alrededor · rueda: acercar\n<b>C</b>: corte (ver el interior desde fuera)</size>", sTexto);
                else
                    GUILayout.Label("<size=15>WASD: moverse · Espacio/Ctrl: subir y bajar · Mayús: más rápido · botón derecho: mirar\n" +
                                    "<b>1</b> Pilotaje · <b>2</b> Ingeniería · <b>3</b> Ciencia · <b>4</b> Comunicaciones (vista sentada) · <b>0</b> esclusa</size>", sTexto);
                GUILayout.Label("<size=15><b>I</b>: exterior / inspección · <b>L</b>: colores de estado de las medidas · <b>H</b>: ayuda</size>", sTexto);
                GUILayout.Space(6);
                GUILayout.Label("<size=15><b>Colores (L):</b> <color=#59CC73>conservada</color> · <color=#5999F2>propuesta</color> · " +
                                "<color=#F2B840>pendiente de prueba</color> · <color=#FF8026>reserva (sin diseñar)</color></size>", sTexto);
                GUILayout.Label("<size=15>Las seis figuras son <b>referencias de tamaño</b> (1,75 m): cuatro con el nombre de su rol y dos maniquíes " +
                                "de medida en el pasillo. <b>No son bots ni jugadores conectados.</b></size>", sTexto);
                GUILayout.Label("<size=14>Sin despegue, embarque, flotación, bots ni red en esta prueba.</size>", sTexto);
            }
            if (PruebaHangar.EnEditor)
            {
                GUILayout.Space(6);
                GUILayout.Label("<size=14>Prueba de conservación (solo en el editor):</size>", sTexto);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<size=15>Recargar hangar</size>", sBoton, GUILayout.Height(34))) prueba.RecargarHangar();
                if (GUILayout.Button("<size=15>Abrir Configuración</size>", sBoton, GUILayout.Height(34))) prueba.AbrirConfiguracion();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }
    }
}
