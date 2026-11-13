using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Interfaz provisional (IMGUI) de la parte científica: mira, progreso
    /// del escaneo, memoria, datos transmitidos, estado del enlace, ficha
    /// abierta y aviso de objetivo cumplido. Los textos se editan en el Inspector.
    /// </summary>
    public class HudCiencia : MonoBehaviour
    {
        [SerializeField] Escaner escaner;
        [SerializeField] SistemaDatos datos;
        [SerializeField] Transform sonda;

        [Header("Textos editables")]
        [SerializeField] string tituloPanel = "DATOS CIENTÍFICOS";
        [SerializeField] string textoFueraEstacion = "Fuera de la estación: vuelve para transmitir.";
        [SerializeField] string textoSinDatos = "Sin datos pendientes de transmitir.";
        [TextArea(2, 4)]
        [SerializeField] string textoObjetivoLogrado =
            "OBJETIVO CUMPLIDO\nLos datos requeridos han llegado al satélite de relevo.";

        const float AltoReferencia = 1080f;

        GUIStyle sTitulo, sTexto, sPequeno, sCentro, sBanner;

        void Awake()
        {
            if (escaner == null) escaner = FindAnyObjectByType<Escaner>();
            if (datos == null) datos = FindAnyObjectByType<SistemaDatos>();
            if (sonda == null)
            {
                SistemaEnergia e = FindAnyObjectByType<SistemaEnergia>();
                if (e != null) sonda = e.transform;
            }
        }

        void OnGUI()
        {
            if (escaner == null || datos == null) return;
            PrepararEstilos();

            float escala = Screen.height / AltoReferencia;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            float ancho = Screen.width / escala;
            float cx = ancho / 2f;
            float cy = AltoReferencia / 2f;

            DibujarMira(cx, cy);
            DibujarObjetivoApuntado(cx, cy);
            DibujarPanelDatos(ancho);
            DibujarFicha();

            if (!string.IsNullOrEmpty(escaner.Mensaje))
                GUI.Label(new Rect(cx - 500f, AltoReferencia - 190f, 1000f, 60f), escaner.Mensaje, sCentro);

            if (datos.ObjetivoCumplido)
            {
                Rect banner = new Rect(cx - 420f, 40f, 840f, 100f);
                Rellenar(banner, new Color(0.05f, 0.35f, 0.2f, 0.85f));
                GUI.Label(banner, textoObjetivoLogrado, sBanner);
            }

            GUI.matrix = Matrix4x4.identity;
        }

        void DibujarMira(float cx, float cy)
        {
            Color c = escaner.PuntoApuntado != null ? new Color(0.4f, 1f, 0.9f, 0.9f) : new Color(1f, 1f, 1f, 0.5f);
            Rellenar(new Rect(cx - 12f, cy - 1f, 24f, 2f), c);
            Rellenar(new Rect(cx - 1f, cy - 12f, 2f, 24f), c);
        }

        void DibujarObjetivoApuntado(float cx, float cy)
        {
            PuntoCientifico p = escaner.PuntoApuntado;
            if (p == null) return;

            string texto = p.Escaneado
                ? $"{p.Titulo} · ya escaneado"
                : $"[E] mantener para escanear «{p.Titulo}» · {escaner.DistanciaApuntado:0.0} m";
            GUI.Label(new Rect(cx - 400f, cy + 30f, 800f, 30f), texto, sCentro);

            if (escaner.Escaneando)
            {
                Rect barra = new Rect(cx - 200f, cy + 66f, 400f, 14f);
                Rellenar(barra, new Color(1f, 1f, 1f, 0.15f));
                Rellenar(new Rect(barra.x, barra.y, barra.width * escaner.Progreso, barra.height), new Color(0.4f, 1f, 0.9f));
                OpcionInstrumento ins = escaner.Instrumento;
                if (ins != null)
                    GUI.Label(new Rect(cx - 400f, cy + 84f, 800f, 26f),
                        $"Escaneando con {ins.nombre} · {ins.consumoEscaneoW:0} W", sCentro);
            }
        }

        void DibujarPanelDatos(float ancho)
        {
            bool faltanPuntos = datos.Configurado && datos.PuntosEnEscena < datos.EscaneosRequeridos;
            Rect panel = new Rect(ancho - 520f, 300f, 490f, faltanPuntos ? 330f : 280f);
            Rellenar(panel, new Color(0f, 0.05f, 0.08f, 0.6f));
            float x = panel.x + 16f;
            float w = panel.width - 32f;
            float y = panel.y + 10f;

            GUI.Label(new Rect(x, y, w, 28f), tituloPanel, sTitulo); y += 34f;

            if (!datos.Configurado)
            {
                GUI.Label(new Rect(x, y, w, 60f), "Esperando la configuración de la misión…", sTexto);
                return;
            }

            GUI.Label(new Rect(x, y, w, 24f), $"Memoria: {datos.DatosEnMemoriaMB:0} / {datos.MemoriaMB:0} MB", sTexto); y += 26f;
            Rect barra = new Rect(x, y, w, 14f);
            Rellenar(barra, new Color(1f, 1f, 1f, 0.12f));
            float f = datos.MemoriaMB > 0f ? Mathf.Clamp01(datos.DatosEnMemoriaMB / datos.MemoriaMB) : 0f;
            Rellenar(new Rect(barra.x, barra.y, barra.width * f, barra.height),
                     f > 0.85f ? new Color(1f, 0.6f, 0.25f) : new Color(0.6f, 0.75f, 1f));
            y += 22f;

            GUI.Label(new Rect(x, y, w, 24f),
                $"Recogidos: {datos.DatosRecogidosMB:0} MB · Transmitidos: {datos.DatosTransmitidosMB:0} MB", sTexto); y += 26f;
            GUI.Label(new Rect(x, y, w, 24f),
                $"Escaneos transmitidos: {datos.EscaneosTransmitidos} / {datos.EscaneosRequeridos}  (hechos: {datos.EscaneosRealizados})", sTexto); y += 26f;
            GUI.Label(new Rect(x, y, w, 24f),
                $"Valor científico transmitido: {datos.ValorTransmitido} puntos (DEMOSTRACIÓN)", sTexto); y += 30f;

            GUI.Label(new Rect(x, y, w, 50f), EstadoEnlace(), sTexto); y += 52f;

            if (faltanPuntos)
            {
                GUI.Label(new Rect(x, panel.yMax - 54f, w, 50f),
                    $"<color=#FF6B5B>Solo hay {datos.PuntosEnEscena} puntos en la escena y el objetivo pide " +
                    $"{datos.EscaneosRequeridos}: no se puede cumplir.</color>", sPequeno);
            }

            float d = DistanciaPuntoMasCercano();
            if (!float.IsInfinity(d))
                GUI.Label(new Rect(x, y, w, 24f), $"Punto sin escanear más cercano: {d:0} m", sPequeno);
        }

        string EstadoEnlace()
        {
            if (!datos.EnEstacion)
                return datos.DatosEnMemoriaMB > 0f ? textoFueraEstacion : textoSinDatos;
            if (datos.DatosEnMemoriaMB <= 0f)
                return "En la estación. " + textoSinDatos;
            if (datos.Transmitiendo)
                return $"<color=#7CFC9A>Transmitiendo a {datos.VelocidadEnlaceMBs:0} MB/s</color>";
            if (!datos.HayContacto)
                return float.IsInfinity(datos.SegundosHastaContacto)
                    ? "<color=#FF6B5B>Sin contacto con el relevo.</color>"
                    : $"<color=#FFC857>Sin contacto con el relevo: próxima ventana en {datos.SegundosHastaContacto:0} s</color>";
            return "En la estación.";
        }

        float DistanciaPuntoMasCercano()
        {
            if (sonda == null) return float.PositiveInfinity;
            float minimo = float.PositiveInfinity;
            foreach (PuntoCientifico p in PuntoCientifico.Todos)
            {
                if (p == null || p.Escaneado) continue;
                float d = Vector3.Distance(sonda.position, p.transform.position);
                if (d < minimo) minimo = d;
            }
            return minimo;
        }

        void DibujarFicha()
        {
            FichaCientifica ficha = escaner.FichaAbierta;
            if (ficha == null) return;

            float alturaImagen = ficha.imagen != null ? 170f : 0f;
            Rect panel = new Rect(30f, 370f, 620f, 330f + alturaImagen);
            Rellenar(panel, new Color(0.02f, 0.1f, 0.14f, 0.88f));

            float x = panel.x + 18f;
            float w = panel.width - 36f;
            float y = panel.y + 12f;

            GUI.Label(new Rect(x, y, w, 30f), ficha.titulo, sTitulo); y += 34f;
            if (ficha.esDemostracion)
            {
                GUI.Label(new Rect(x, y, w, 22f), "<color=#FFC857>DEMOSTRACIÓN · texto ficticio, no son datos científicos</color>", sPequeno);
                y += 26f;
            }
            if (ficha.imagen != null)
            {
                GUI.DrawTexture(new Rect(x, y, w, alturaImagen - 10f), ficha.imagen, ScaleMode.ScaleToFit);
                y += alturaImagen;
            }
            GUI.Label(new Rect(x, y, w, 190f), ficha.texto, sTexto);
            y = panel.yMax - 56f;
            if (!string.IsNullOrWhiteSpace(ficha.fuente))
                GUI.Label(new Rect(x, y, w, 24f), "Fuente: " + ficha.fuente, sPequeno);
            GUI.Label(new Rect(x, panel.yMax - 30f, w, 24f), "F: cerrar ficha", sPequeno);
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

            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            sTitulo.normal.textColor = new Color(0.75f, 0.95f, 1f);

            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, richText = true };
            sTexto.normal.textColor = Color.white;

            sPequeno = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = true };
            sPequeno.normal.textColor = new Color(0.8f, 0.9f, 0.95f);

            sCentro = new GUIStyle(sTexto) { alignment = TextAnchor.UpperCenter, fontSize = 20 };

            sBanner = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true
            };
            sBanner.normal.textColor = Color.white;
        }
    }
}
