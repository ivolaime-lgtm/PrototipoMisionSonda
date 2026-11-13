using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Interfaz provisional (IMGUI) de la escena Nave: estado de la nave, instrucción actual,
    /// panel del puesto en uso, mira de pilotaje, barra de inserción, cuenta atrás y avisos.
    /// </summary>
    public class HudNave : MonoBehaviour
    {
        [SerializeField] ControlNave control;
        [SerializeField] JugadorNave jugador;
        [SerializeField] string textoDemo = "DEMOSTRACIÓN · nave tripulada ficticia · viaje comprimido";

        GUIStyle sTitulo, sTexto, sPequeno, sAviso, sGrande, sCentro;
        ExteriorNave exterior;

        void OnGUI()
        {
            if (control == null || jugador == null || control.Nave == null) return;
            Preparar();

            float escala = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            float ancho = Screen.width / escala;

            // Destello rojo al recibir un impacto
            if (control.FlashImpacto > 0f)
                Rellenar(new Rect(0, 0, ancho, 1080f), new Color(1f, 0.1f, 0.05f, 0.35f * control.FlashImpacto));

            // Durante la toma exterior del despegue se ocultan los paneles laterales (siguen la barra de etapa y los avisos).
            bool vistaExterior = CinematicaDespegue.Activa;
            if (!vistaExterior) PanelEstado();

            PuestoNave actual = jugador.PuestoActual;
            if (actual != null) { if (!vistaExterior) PanelPuesto(actual, ancho); }
            else if (control.Fase != FaseNave.Abortada)
                GUI.Label(new Rect(ancho / 2f - 4f, 540f - 14f, 8f, 28f), "+", sCentro);   // punto de mira

            if (actual != null && actual.Tipo == TipoPuesto.Pilotaje)
            {
                if (control.Fase == FaseNave.Crucero) Mira(ancho);
                if (control.InsercionDisponible) BarraInsercion(ancho);
                if (control.Fase == FaseNave.Ascenso && control.Etapa == ControlNave.EstadoEtapa.Pendiente) BarraEtapa(ancho);
            }

            // Navegación longitudinal (opcional): esquema, indicador del destino y aviso del giro.
            if (!vistaExterior && control.NavActiva && (control.Fase == FaseNave.Crucero || control.Fase == FaseNave.Llegada))
            {
                IndicadorDestino(ancho, escala);
                EsquemaNavegacion(ancho);
                if (control.Navegacion.Girando) RotuloGiro(ancho);
            }

            // Cuenta atrás
            // (en la vista exterior se aparta a la derecha para no tapar el vehículo)
            if (control.Fase == FaseNave.CuentaAtras)
                GUI.Label(vistaExterior ? new Rect(ancho - 380f, 60f, 300f, 140f) : new Rect(ancho / 2f - 300f, 220f, 600f, 140f),
                          Mathf.CeilToInt(control.CuentaAtrasRestante).ToString(), sGrande);

            // Aviso temporal
            if (control.TiempoAviso > 0f && !string.IsNullOrEmpty(control.Aviso))
                GUI.Label(new Rect(ancho / 2f - 600f, 120f, 1200f, 80f), control.Aviso, sAviso);

            // Objetivo alcanzado (misión tripulada)
            if (control.ObjetivoAlcanzado)
            {
                Rellenar(new Rect(ancho / 2f - 560f, 400f, 1120f, 200f), new Color(0f, 0.18f, 0.08f, 0.88f));
                GUI.Label(new Rect(ancho / 2f - 540f, 420f, 1080f, 170f),
                          "<color=#7CFC9A><b>OBJETIVO ALCANZADO</b></color>\nÓrbita establecida sobre " + control.Destino +
                          "\nGenerando el informe de la misión…", sAviso);
            }

            // Aborto
            if (control.Fase == FaseNave.Abortada)
            {
                Rellenar(new Rect(ancho / 2f - 560f, 400f, 1120f, 200f), new Color(0.2f, 0f, 0f, 0.85f));
                GUI.Label(new Rect(ancho / 2f - 540f, 420f, 1080f, 170f),
                          "<b>MISIÓN ABORTADA</b>\n" + control.MotivoAborto + "\nGenerando el informe…", sAviso);
            }

            // Indicación inferior
            string abajo = null;
            if (actual != null)
                abajo = jugador.AsientoBloqueado ? $"{actual.Nombre} · permanece en el asiento"
                                                 : $"{actual.Nombre} · Q: salir del puesto";
            else if (jugador.PuestoMirado != null)
                abajo = $"E: usar {jugador.PuestoMirado.Nombre}";
            if (abajo != null)
                GUI.Label(new Rect(ancho / 2f - 450f, 1080f - 90f, 900f, 40f), abajo, sCentro);

            if (Cursor.lockState != CursorLockMode.Locked)
                GUI.Label(new Rect(ancho / 2f - 450f, 1080f - 50f, 900f, 34f), "Haz clic para controlar la cámara", sCentro);

            GUI.matrix = Matrix4x4.identity;
        }

        void PanelEstado()
        {
            Rect r = new Rect(20f, 20f, 560f, 370f);
            Rellenar(r, new Color(0f, 0.05f, 0.08f, 0.65f));
            GUILayout.BeginArea(new Rect(r.x + 16f, r.y + 10f, r.width - 32f, r.height - 20f));
            GUILayout.Label("NAVE · " + control.NombreFase() + (control.ModoPrueba ? "  <color=#FFB040>[MODO DE PRUEBA]</color>" : ""), sTitulo);
            GUILayout.Label(textoDemo, sPequeno);
            GUILayout.Label($"<b>Destino:</b> {control.Destino}", sTexto);
            if (control.NavActiva && control.Fase != FaseNave.Preparacion && control.Fase != FaseNave.CuentaAtras && control.Fase != FaseNave.Ascenso)
            {
                NavegacionLongitudinal n = control.Navegacion;
                string mov = n.SeAcerca ? "se acerca" : n.SeAleja ? "<color=#FF6B5B>se aleja</color>" : "detenida";
                Barra("Viaje", Mathf.Clamp01(n.PosicionM / n.L), 1f,
                      $"destino a {n.DistanciaAlDestinoM:0} m · {Mathf.Abs(n.VelocidadMS):0.0} m/s · {mov} · " +
                      (n.ProaHaciaDestino ? "proa al destino" : "proa hacia fuera") + (n.Girando ? " · <b>GIRANDO</b>" : ""),
                      new Color(0.3f, 0.9f, 1f));
            }
            else
            {
                Barra("Viaje", control.Progreso, 1f,
                      $"{control.Progreso * 100f:0} % · faltan {control.DistanciaRestanteMkm:0} M km (simulado: progreso guionizado)",
                      new Color(0.3f, 0.9f, 1f));
            }
            Barra("Combustible", control.Combustible, control.Nave.combustibleInicialKg,
                  $"{control.Combustible:0.0} kg · maniobras {control.CombustibleManiobrasKg:0.0} kg" +
                  (control.NavActiva ? $" · motor principal {control.CombustibleMotorPrincipalKg:0.0} kg" : "") +
                  (control.NavActiva && (control.Fase == FaseNave.Crucero || control.Fase == FaseNave.Llegada)
                      ? MargenTexto(control.Combustible - control.CombustibleNecesarioLlegadaKg) : "") +
                  (EstadoViaje.CombustiblePruebaKg >= 0f ? "  <color=#FFB040>[COMBUSTIBLE DE PRUEBA]</color>" : "") +
                  (control.ConsumoActualKgS > 0.05f ? $"  <color=#FFB040><b>▼ {control.ConsumoActualKgS:0.0} kg/s</b></color>" : ""),
                  new Color(1f, 0.8f, 0.3f));
            Barra("Integridad del casco", control.Integridad, control.Catalogo.integridadInicial, $"{control.Integridad:0} %", new Color(0.5f, 1f, 0.6f));
            Barra("Energía de la nave", control.EnergiaNave, control.Nave.energiaWh, $"{control.EnergiaNave:0} Wh", new Color(0.4f, 0.7f, 1f));
            GUILayout.Space(4);
            GUILayout.Label("<b>Ahora:</b> " + control.Instruccion(), sTexto);
            GUILayout.EndArea();
        }

        void PanelPuesto(PuestoNave p, float ancho)
        {
            Rect r = new Rect(ancho - 700f, 140f, 680f, 620f);
            Rellenar(r, new Color(0f, 0.05f, 0.08f, 0.78f));
            GUILayout.BeginArea(new Rect(r.x + 18f, r.y + 12f, r.width - 36f, r.height - 24f));
            GUILayout.Label("PUESTO DE " + p.Nombre.ToUpperInvariant(), sTitulo);
            GUILayout.Label(control.TextoPuesto(p.Tipo), sTexto);
            GUILayout.EndArea();
        }

        void Mira(float ancho)
        {
            // Mira fija en el centro: los escombros que pasan por ella golpean la nave.
            Color c = new Color(0.4f, 1f, 0.9f, 0.9f);
            float cx = ancho / 2f, cy = 540f;
            Rellenar(new Rect(cx - 60f, cy - 1f, 40f, 2f), c);
            Rellenar(new Rect(cx + 20f, cy - 1f, 40f, 2f), c);
            Rellenar(new Rect(cx - 1f, cy - 60f, 2f, 40f), c);
            Rellenar(new Rect(cx - 1f, cy + 20f, 2f, 40f), c);

            // Indicador de deriva lateral
            Vector2 v = control.VelocidadLateral;
            Rellenar(new Rect(cx + v.x * 8f - 4f, cy - v.y * 8f - 4f, 8f, 8f), new Color(1f, 0.8f, 0.3f, 0.9f));
        }

        void BarraInsercion(float ancho)
        {
            Rect barra = new Rect(ancho / 2f - 400f, 760f, 800f, 36f);
            Rellenar(barra, new Color(1f, 1f, 1f, 0.15f));
            float verde = control.Catalogo.zonaVerdeInsercion;
            Rellenar(new Rect(barra.x + barra.width * (0.5f - verde), barra.y, barra.width * verde * 2f, barra.height),
                     new Color(0.2f, 0.9f, 0.35f, 0.7f));
            float x = barra.x + barra.width * control.MarcadorInsercion;
            Rellenar(new Rect(x - 3f, barra.y - 10f, 6f, barra.height + 20f), Color.white);
            GUI.Label(new Rect(barra.x, barra.y + 44f, barra.width, 30f), "Espacio: encender el motor y frenar", sCentro);
        }

        /// <summary>Ventana de separación de etapa: el marcador avanza con el tiempo del ascenso.</summary>
        void BarraEtapa(float ancho)
        {
            float total = control.VentanaEtapaFinS + 0.8f;
            Rect barra = new Rect(ancho / 2f - 400f, 760f, 800f, 36f);
            Rellenar(barra, new Color(1f, 1f, 1f, 0.15f));
            float x0 = barra.x + barra.width * Mathf.Clamp01(control.VentanaEtapaInicioS / total);
            float x1 = barra.x + barra.width * Mathf.Clamp01(control.VentanaEtapaFinS / total);
            bool abierta = control.EtapaEnVentana;
            Rellenar(new Rect(x0, barra.y, x1 - x0, barra.height),
                     abierta ? new Color(0.2f, 1f, 0.4f, 0.85f) : new Color(0.2f, 0.9f, 0.35f, 0.45f));
            float x = barra.x + barra.width * Mathf.Clamp01(control.TiempoFase / total);
            Rellenar(new Rect(x - 3f, barra.y - 10f, 6f, barra.height + 20f), Color.white);
            GUI.Label(new Rect(barra.x, barra.y - 44f, barra.width, 34f),
                      abierta ? "<b>¡AHORA! Espacio: separar la etapa</b>" : "Separación de etapa: espera a la zona verde", sCentro);
        }

        // ------------------------------------------------------------------ Navegación longitudinal (solo dibujo)

        /// <summary>
        /// Marca del destino: sobre la luna si se ve en pantalla; si no, en el borde, con flecha hacia donde está.
        /// Si queda detrás de la cámara, abajo en el centro con «DETRÁS».
        /// </summary>
        void IndicadorDestino(float ancho, float escala)
        {
            if (exterior == null) exterior = FindFirstObjectByType<ExteriorNave>();
            Camera cam = jugador.Camara;
            if (exterior == null || cam == null) return;
            NavegacionLongitudinal n = control.Navegacion;
            string dist = n.DistanciaAbsolutaM >= 1000f ? $"{n.DistanciaAbsolutaM / 1000f:0.0} km" : $"{n.DistanciaAbsolutaM:0} m";
            Color c = n.EnZona ? new Color(0.45f, 1f, 0.6f, 0.95f) : new Color(0.4f, 0.9f, 1f, 0.95f);

            Vector3 sp = cam.WorldToScreenPoint(exterior.PosicionDestino);
            float x = sp.x / escala, y = (Screen.height - sp.y) / escala;
            const float margen = 70f;
            // Con un puesto abierto, el panel ocupa la derecha (x > ancho − 700, y 140–760): la marca no se pone debajo.
            bool panel = jugador.PuestoActual != null;
            float maxX = panel ? ancho - 730f : ancho - margen;
            bool delante = sp.z > 0f;
            bool dentro = delante && x > margen && x < maxX && y > margen && y < 1080f - margen;
            string mov = n.SeAcerca ? "te acercas" : n.SeAleja ? "te alejas" : "detenida";

            if (dentro)
            {
                // Corchetes alrededor del destino
                float r = 34f;
                Rellenar(new Rect(x - r, y - r, 14f, 2f), c); Rellenar(new Rect(x - r, y - r, 2f, 14f), c);
                Rellenar(new Rect(x + r - 14f, y - r, 14f, 2f), c); Rellenar(new Rect(x + r - 2f, y - r, 2f, 14f), c);
                Rellenar(new Rect(x - r, y + r - 2f, 14f, 2f), c); Rellenar(new Rect(x - r, y + r - 14f, 2f, 14f), c);
                Rellenar(new Rect(x + r - 14f, y + r - 2f, 14f, 2f), c); Rellenar(new Rect(x + r - 2f, y + r - 14f, 2f, 14f), c);
                GUI.Label(new Rect(x - 170f, y + r + 4f, 340f, 28f), $"DESTINO · {dist} · {mov}", sCentro);
                return;
            }

            string flecha;
            float ex, ey;
            if (!delante)
            {
                flecha = "▼";
                ex = ancho / 2f; ey = 690f;   // encima de la barra de inserción y del esquema
                GUI.Label(new Rect(ex - 330f, ey - 14f, 660f, 30f), $"<b>{flecha} DESTINO DETRÁS · {dist}</b> · {mov}", sCentro);
                GUI.Label(new Rect(ex - 330f, ey + 14f, 660f, 26f), "<size=15>La proa mira hacia fuera: con R la giras para verlo delante.</size>", sCentro);
                return;
            }
            ex = Mathf.Clamp(x, margen, maxX);
            ey = Mathf.Clamp(y, margen, 1080f - margen);
            float dx = x - ancho / 2f, dy = y - 540f;
            flecha = Mathf.Abs(dx) * 1080f > Mathf.Abs(dy) * ancho ? (dx > 0f ? "▶" : "◀") : (dy > 0f ? "▼" : "▲");
            GUI.Label(new Rect(ex - 170f, ey - 14f, 340f, 30f), $"<b>{flecha} DESTINO · {dist}</b> · {mov}", sCentro);
        }

        /// <summary>
        /// Esquema sencillo de la ruta (no a escala lateral): salida, zona de aproximación, destino y nave, con la proa
        /// (triángulo) y la velocidad (flecha amarilla).
        /// </summary>
        void EsquemaNavegacion(float ancho)
        {
            NavegacionLongitudinal n = control.Navegacion;
            float zona = n.Configuracion.semiZonaM;
            float min = Mathf.Min(-0.04f * n.L, n.PosicionM - 0.04f * n.L);
            float max = Mathf.Max(n.L + 4f * zona, n.PosicionM + 0.04f * n.L);
            Rect r = new Rect(ancho / 2f - 470f, 1080f - 200f, 940f, 54f);
            Rellenar(new Rect(r.x - 10f, r.y - 26f, r.width + 20f, r.height + 34f), new Color(0f, 0.05f, 0.08f, 0.6f));
            GUI.Label(new Rect(r.x, r.y - 26f, r.width, 22f),
                      "<size=14>ESQUEMA DE LA RUTA · ▶◀ proa · <color=#FFCC4D>amarillo: velocidad</color>" +
                      (n.GravedadActiva ? " · <color=#6FA8FF>azul: gravedad (experimental)</color>" : "") + " · DEMOSTRACIÓN</size>", sCentro);
            float X(float s) => r.x + r.width * Mathf.InverseLerp(min, max, s);

            float yLinea = r.y + r.height * 0.5f;
            Rellenar(new Rect(r.x, yLinea - 1f, r.width, 2f), new Color(1f, 1f, 1f, 0.35f));
            // Salida
            Rellenar(new Rect(X(0f) - 1f, yLinea - 10f, 2f, 20f), new Color(1f, 1f, 1f, 0.6f));
            // Zona de aproximación
            bool lista = n.PuedeCapturar && control.MotivoInsercionBloqueada.Length == 0;
            Rellenar(new Rect(X(n.L - zona), yLinea - 12f, X(n.L + zona) - X(n.L - zona), 24f),
                     lista ? new Color(0.3f, 1f, 0.45f, 0.45f) : new Color(1f, 0.75f, 0.25f, 0.3f));
            // Destino: al lado de la ruta (no se atraviesa)
            float xd = X(n.L);
            Rellenar(new Rect(xd - 9f, yLinea + 14f, 18f, 18f), new Color(0.75f, 0.85f, 1f, 0.9f));
            GUI.Label(new Rect(xd - 80f, yLinea + 30f, 160f, 20f), "<size=13>destino</size>", sCentro);
            GUI.Label(new Rect(X(0f) - 60f, yLinea + 12f, 120f, 20f), "<size=13>salida</size>", sCentro);

            // Nave: rectángulo + triángulo de proa
            float xn = X(n.PosicionM);
            Color cn = new Color(1f, 1f, 1f, 0.95f);
            Rellenar(new Rect(xn - 6f, yLinea - 6f, 12f, 12f), cn);
            GUI.Label(new Rect(xn + (n.Orientacion > 0 ? 2f : -28f), yLinea - 15f, 26f, 30f),
                      n.Orientacion > 0 ? "<b>▶</b>" : "<b>◀</b>", sCentro);
            // Velocidad: flecha amarilla proporcional (40 m/s = 120 px)
            float largo = Mathf.Clamp(n.VelocidadMS * 3f, -200f, 200f);
            if (Mathf.Abs(largo) > 1f)
                Rellenar(new Rect(largo > 0f ? xn : xn + largo, yLinea - 24f, Mathf.Abs(largo), 3f), new Color(1f, 0.8f, 0.3f, 0.95f));
            // Gravedad (experimental): flecha azul proporcional a la aceleración (0,5 m/s² = 100 px)
            if (n.GravedadActiva)
            {
                float g = Mathf.Clamp(n.AceleracionGravedadMS2 * 200f, -200f, 200f);
                if (Mathf.Abs(g) > 1f)
                    Rellenar(new Rect(g > 0f ? xn : xn + g, yLinea + 20f, Mathf.Abs(g), 3f), new Color(0.43f, 0.66f, 1f, 0.95f));
            }
        }

        static string MargenTexto(float m) => m < 10f ? $" · <color=#FF6B5B><b>margen {m:0.0} kg</b></color>"
                                            : m < 30f ? $" · <color=#FFB040>margen {m:0} kg</color>" : $" · margen {m:0} kg";

        /// <summary>Rótulo central durante el giro: el exterior gira y el motor queda bloqueado.</summary>
        void RotuloGiro(float ancho)
        {
            NavegacionLongitudinal n = control.Navegacion;
            float f = 1f - n.SegundosGiroRestantes / Mathf.Max(0.01f, n.Configuracion.duracionGiroS);
            Rect b = new Rect(ancho / 2f - 220f, 610f, 440f, 10f);
            Rellenar(b, new Color(1f, 1f, 1f, 0.15f));
            Rellenar(new Rect(b.x, b.y, b.width * f, b.height), new Color(1f, 0.7f, 0.25f, 0.9f));
            GUI.Label(new Rect(ancho / 2f - 300f, 625f, 600f, 30f),
                      $"<b>GIRANDO 180°</b> · motor principal bloqueado · {n.SegundosGiroRestantes:0.0} s", sCentro);
        }

        void Barra(string etiqueta, float valor, float maximo, string texto, Color color)
        {
            GUILayout.Label($"{etiqueta}: {texto}", sPequeno);
            Rect r = GUILayoutUtility.GetRect(500f, 12f, GUILayout.ExpandWidth(true));
            Rellenar(r, new Color(1f, 1f, 1f, 0.12f));
            float f = maximo > 0f ? Mathf.Clamp01(valor / maximo) : 0f;
            Rellenar(new Rect(r.x, r.y, r.width * f, r.height), f < 0.25f ? new Color(1f, 0.35f, 0.3f) : color);
            GUILayout.Space(2);
        }

        static void Rellenar(Rect r, Color c)
        {
            Color previo = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previo;
        }

        void Preparar()
        {
            if (sTitulo != null) return;
            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, richText = true };
            sTitulo.normal.textColor = new Color(0.6f, 0.9f, 1f);
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, richText = true };
            sTexto.normal.textColor = Color.white;
            sPequeno = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true };
            sPequeno.normal.textColor = new Color(0.8f, 0.9f, 0.95f);
            sAviso = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
            sAviso.normal.textColor = new Color(1f, 0.85f, 0.4f);
            sGrande = new GUIStyle(GUI.skin.label) { fontSize = 110, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sGrande.normal.textColor = Color.white;
            sCentro = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, richText = true };
            sCentro.normal.textColor = new Color(0.9f, 0.97f, 1f);
        }
    }
}
