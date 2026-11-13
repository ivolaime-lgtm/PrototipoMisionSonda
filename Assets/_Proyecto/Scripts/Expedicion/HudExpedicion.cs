using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Paneles de la prueba EM-4X (SOLO DIBUJO, IMGUI escalado a 1080 de alto): medidas, volúmenes, recorridos, resultado
    /// de la revisión de distribución, módulos, pendientes y controles. No cambia nada.
    /// </summary>
    public class HudExpedicion : MonoBehaviour
    {
        PruebaExpedicion prueba;
        GUIStyle sTitulo, sTexto, sPequeno, sAviso, sProblema, sBien, sProvisional, sPendiente;
        Vector2 desplazamiento;
        float escala, ancho;

        void Start() { prueba = GetComponent<PruebaExpedicion>(); }

        void Estilos()
        {
            if (sTexto != null) return;
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true, wordWrap = true };
            sTexto.normal.textColor = new Color(0.92f, 0.95f, 1f);
            sTitulo = new GUIStyle(sTexto) { fontSize = 20, fontStyle = FontStyle.Bold };
            sPequeno = new GUIStyle(sTexto) { fontSize = 14 };
            sProblema = new GUIStyle(sTexto);
            sProblema.normal.textColor = new Color(1f, 0.45f, 0.4f);
            sBien = new GUIStyle(sTexto);
            sBien.normal.textColor = new Color(0.5f, 0.9f, 0.55f);
            sPendiente = new GUIStyle(sPequeno);
            sPendiente.normal.textColor = new Color(1f, 0.75f, 0.4f);
            sAviso = new GUIStyle(sTexto) { fontSize = 19, alignment = TextAnchor.MiddleCenter };
            sAviso.normal.textColor = new Color(1f, 0.86f, 0.45f);
            sProvisional = new GUIStyle(sTexto) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sProvisional.normal.textColor = new Color(1f, 0.6f, 0.25f);
        }

        static void Fondo(Rect r)
        {
            Color c = GUI.color;
            GUI.color = new Color(0f, 0.04f, 0.08f, 0.74f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
        }

        static string M(float v) => v.ToString("0.0");
        static string V(float v) => v.ToString("0");

        void OnGUI()
        {
            if (prueba == null || prueba.Revision == null || prueba.Inspeccion == null) return;
            Estilos();
            escala = Screen.height / 1080f;
            ancho = Screen.width / escala;
            GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));

            if (prueba.AyudaVisible)
            {
                PanelDatos();
                PanelControles();
            }
            if (prueba.Inspeccion.ModoActual == InspeccionExpedicion.Modo.Inspeccion)
            {
                Rect r = new Rect(ancho / 2f - 560f, 12f, 1120f, 62f);
                Fondo(r);
                GUI.Label(r, "CÁMARA DE INSPECCIÓN PROVISIONAL · sin colisiones\n<size=15>No valida por sí sola el movimiento real de los personajes ni sus colisiones</size>", sProvisional);
            }
            if (prueba.AvisoVisible)
            {
                Rect r = new Rect(ancho / 2f - 520f, 82f, 1040f, 54f);
                Fondo(r);
                GUI.Label(r, prueba.Aviso, sAviso);
            }
        }

        void PanelDatos()
        {
            RevisionDistribucion.Resultado rv = prueba.Revision;
            DefinicionNaveModular d = prueba.Definicion;
            Rect r = new Rect(14f, 14f, 600f, 1052f);
            Fondo(r);
            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 8f, r.width - 24f, r.height - 16f));
            desplazamiento = GUILayout.BeginScrollView(desplazamiento);

            GUILayout.Label("EM-4X · NAVE DE EXPEDICIÓN MODULAR", sTitulo);
            GUILayout.Label("<color=#FFB040>Bloque PROVISIONAL de demostración para recorrer.</color> Sirve para validar tamaño, distribución y " +
                            "sensación de espacio. <b>No</b> es una nave con balance de vuelo y no forma parte de la partida.", sPequeno);

            Vector3 t = rv.envolventeTotal.size, mo = rv.envolventeModulos.size;
            GUILayout.Space(6f);
            GUILayout.Label("TAMAÑO EXTERIOR TOTAL", sTitulo);
            GUILayout.Label($"Largo <b>{M(t.z)} m</b> (de la proa de la lanzadera al motor) · ancho <b>{M(t.x)} m</b> con antena " +
                            $"({M(mo.x)} m de módulos) · alto <b>{M(t.y)} m</b> (de la bodega al hábitat).", sTexto);
            GUILayout.Label("Paneles y radiadores desplegados: fuera del ancho medido.", sPequeno);

            GUILayout.Space(6f);
            GUILayout.Label("ESPACIO HABITABLE (estimado)", sTitulo);
            GUILayout.Label($"Presurizado: <b>{V(rv.presurizadoM3)} m³</b> ({V(rv.presurizadoSinLanzaderaM3)} m³ sin la lanzadera).\n" +
                            $"Libre de equipos: <b>{V(rv.libreM3)} m³</b> ({V(rv.libreSinLanzaderaM3)} m³ sin la lanzadera).\n" +
                            $"Paso libre para circular: <b>{V(rv.circulacionM3)} m³</b> (pasos entre bastidores, nodos, pasadizos y adaptadores).", sTexto);

            GUILayout.Space(6f);
            GUILayout.Label("RECORRIDOS ENTRE PUESTOS (flotando, por escotillas)", sTitulo);
            foreach (RevisionDistribucion.Ruta ru in rv.rutas)
                GUILayout.Label($"{ru.desde} → {ru.hasta}: <b>{M(ru.metros)} m</b>", sTexto);

            GUILayout.Space(6f);
            GUILayout.Label("REVISIÓN DE LA DISTRIBUCIÓN", sTitulo);
            if (rv.problemas.Count == 0)
                GUILayout.Label($"[OK] {rv.comprobaciones} comprobaciones correctas: sin solapes de módulos, conexiones alineadas, escotillas que caben, " +
                                "equipos dentro del casco, fuera de los pasos y sin tapar escotillas.", sBien);
            else
            {
                GUILayout.Label($"{rv.problemas.Count} problemas ({rv.comprobaciones} comprobaciones correctas):", sProblema);
                foreach (string p in rv.problemas) GUILayout.Label("[X] " + p, sProblema);
            }
            GUILayout.Label("Menor holgura entre módulos no unidos:", sPequeno);
            foreach (string h in rv.holguras) GUILayout.Label("· " + h, sPequeno);
            GUILayout.Label("<i>La revisión comprueba la geometría de los bloques; no comprueba colisiones de personajes.</i>", sPequeno);

            GUILayout.Space(6f);
            GUILayout.Label("MÓDULOS <size=13>(diámetros entre caras del octógono)</size>", sTitulo);
            foreach (RevisionDistribucion.VolumenModulo v in rv.volumenes)
            {
                ModuloNave m = v.modulo;
                string medidas = m.EsCaja
                    ? $"bloque {M(m.caja.x)} × {M(m.caja.y)} × {M(m.caja.z)} m · cabina 3,4 de alto"
                    : $"ext. {M(m.diametroExterior)} · int. {M(m.diametroInterior)} · " +
                      (m.pasoLibre.x > 0f ? $"paso {M(m.pasoLibre.x)} × {M(m.pasoLibre.y)}" : "sin bastidores") + $" · largo {M(m.largo)}";
                GUILayout.Label($"<b>{m.nombre}</b> <color={ColorEstado(m.estado)}>[{NombreEstado(m.estado)}]</color>\n" +
                                $"<size=14>{m.funcion}\n{medidas} · {V(v.presurizadoM3)} m³ presurizados, {V(v.libreM3)} m³ libres</size>", sTexto);
            }

            GUILayout.Space(6f);
            GUILayout.Label("PENDIENTE (no resuelto en esta prueba)", sTitulo);
            GUILayout.Label("· Balance de masa (no se usa la masa del casco de la E-4).\n· Propulsión y combustible.\n· Recursos y energía.\n" +
                            "· Transporte de los módulos hasta la órbita (varios lanzamientos: solo contexto del mundo).\n" +
                            "· Atraque de la lanzadera (solo se representa la unión).\n· Acabado definitivo: no encargado.", sPendiente);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static string ColorEstado(EstadoMedida e) =>
            e == EstadoMedida.Conservada ? "#59CC73" : e == EstadoMedida.Propuesta ? "#5999F2" : e == EstadoMedida.PendientePrueba ? "#F2B840" : "#FF8030";

        static string NombreEstado(EstadoMedida e) =>
            e == EstadoMedida.Conservada ? "conservada" : e == EstadoMedida.Propuesta ? "propuesta" : e == EstadoMedida.PendientePrueba ? "pendiente de prueba" : "reserva";

        void PanelControles()
        {
            InspeccionExpedicion ins = prueba.Inspeccion;
            Rect r = new Rect(ancho - 474f, 14f, 460f, 640f);
            Fondo(r);
            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 8f, r.width - 24f, r.height - 16f));
            GUILayout.Label(ins.ModoActual == InspeccionExpedicion.Modo.Exterior ? "VISTA EXTERIOR" : "INSPECCIÓN · " + ins.UltimaVista, sTitulo);
            GUILayout.Label("Corte: <b>" + VistaExpedicion.NombresCorte[prueba.Vista.Corte] + "</b>", sPequeno);
            if (ins.ModoActual == InspeccionExpedicion.Modo.Exterior)
                GUILayout.Label("Botón derecho del ratón: girar alrededor · rueda: acercar", sTexto);
            else
                GUILayout.Label("W/S/A/D: moverse · Espacio/Ctrl: subir y bajar (según la cámara) · Q/E: girar sobre sí misma · " +
                                "botón derecho: mirar · Mayús: rápido · R: nivelar", sTexto);
            GUILayout.Label("<b>I</b>: exterior / inspección\n<b>1</b> Pilotaje · <b>2</b> Ingeniería · <b>3</b> Ciencia · <b>4</b> Comunicaciones\n" +
                            "<b>0</b> esclusa · <b>5</b> zona común · <b>6</b> lanzadera\n" +
                            "<b>C</b>: corte (horizontal, longitudinal, transversal) · <b>T</b>: rótulos · <b>X</b>: esquema de conexiones y recorridos · <b>H</b>: ocultar paneles", sTexto);
            GUILayout.Space(6f);
            GUILayout.Label("<b>Colores:</b> <color=#C6D6EB>módulos</color> · <color=#C7D6EB>nodos (azulado)</color> · <color=#F28C29>escotillas y asideros</color> · " +
                            "<color=#9AA5B1>bastidores</color> · <color=#3B7DD8>consolas</color> · <color=#B58AD8>camarotes</color> · <color=#F7C99B>carga</color>", sPequeno);
            GUILayout.Space(6f);
            GUILayout.Label("Las figuras son <b>referencias de tamaño de 1,75 m</b>: cuatro con el nombre de su puesto y maniquíes de medida. " +
                            "<b>No son bots ni jugadores conectados.</b>", sTexto);
            GUILayout.Label("«Arriba» y «abajo» son referencias de la nave. No hay gravedad interior ni escaleras: se circula flotando " +
                            "con asideros. La cámara de inspección no valida por sí sola el movimiento real ni las colisiones.", sPequeno);
            GUILayout.Label("Sin atraque, ensamblaje, lanzamientos, bots ni red en esta prueba.", sPequeno);
            GUILayout.EndArea();
        }
    }
}
