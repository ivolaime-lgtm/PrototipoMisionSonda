using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Paneles de la prueba de circulación (SOLO DIBUJO, IMGUI escalado a 1080). En modo personaje: modo, módulo actual,
    /// panel mirado, bloqueos, lugares alcanzados, estado de las puertas, resultados de las revisiones y controles.
    /// En modo inspección solo muestra una banda: el resto lo dibuja HudExpedicion (la herramienta de inspección).
    /// </summary>
    public class HudCirculacion : MonoBehaviour
    {
        PruebaCirculacion prueba;
        GUIStyle sTitulo, sTexto, sPequeno, sAviso, sBanda, sMal, sBien, sMira;
        float escala, ancho;
        Vector2 desplazamiento;

        void Start() { prueba = GetComponent<PruebaCirculacion>(); }

        void Estilos()
        {
            if (sTexto != null) return;
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true, wordWrap = true };
            sTexto.normal.textColor = new Color(0.92f, 0.95f, 1f);
            sTitulo = new GUIStyle(sTexto) { fontSize = 19, fontStyle = FontStyle.Bold };
            sPequeno = new GUIStyle(sTexto) { fontSize = 14 };
            sMal = new GUIStyle(sPequeno); sMal.normal.textColor = new Color(1f, 0.45f, 0.4f);
            sBien = new GUIStyle(sPequeno); sBien.normal.textColor = new Color(0.5f, 0.9f, 0.55f);
            sAviso = new GUIStyle(sTexto) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            sAviso.normal.textColor = new Color(1f, 0.86f, 0.45f);
            sBanda = new GUIStyle(sTexto) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sBanda.normal.textColor = new Color(0.55f, 0.9f, 1f);
            sMira = new GUIStyle(sTexto) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sMira.normal.textColor = new Color(1f, 0.9f, 0.5f);
        }

        static void Fondo(Rect r)
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
            if (!prueba.Listo)
            {
                Rect r0 = new Rect(ancho / 2f - 300f, 500f, 600f, 60f);
                Fondo(r0);
                GUI.Label(r0, "Preparando colisiones, puertas y comprobaciones…", sAviso);
                return;
            }

            bool personaje = prueba.ModoActual == PruebaCirculacion.Modo.Personaje;
            Rect banda = new Rect(ancho / 2f - 520f, personaje ? 12f : 1010f, 1040f, 56f);
            Fondo(banda);
            GUI.Label(banda, personaje
                ? "PERSONAJE PROVISIONAL · con colisiones · sin gravedad\n<size=14>Tab: cámara de inspección (sin colisiones, herramienta aparte)</size>"
                : "CÁMARA DE INSPECCIÓN · sin colisiones · Tab: volver al personaje", sBanda);
            if (!personaje) return;

            // Mira y panel apuntado
            GUI.Label(new Rect(ancho / 2f - 20f, 520f, 40f, 40f), "+", sMira);
            PersonajeFlotante pj = prueba.Personaje;
            if (pj != null && pj.PanelMirado != null && pj.PanelMirado.Union != null)
            {
                PuertasUnion u = pj.PanelMirado.Union;
                string accion = u.EstadoActual == PuertasUnion.Estado.Abierta || u.EstadoActual == PuertasUnion.Estado.Abriendo ? "cerrar" : "abrir";
                GUI.Label(new Rect(ancho / 2f - 400f, 570f, 800f, 40f), $"E: {accion} el paso {u.Datos.nombre}", sMira);
            }

            string aviso = prueba.MensajePuertas();
            if (aviso.Length == 0 && prueba.AvisoVisible) aviso = prueba.Aviso;
            if (aviso.Length > 0)
            {
                Rect r = new Rect(ancho / 2f - 520f, 76f, 1040f, 50f);
                Fondo(r);
                GUI.Label(r, aviso, sAviso);
            }
            if (pj != null && pj.Bloqueo.Length > 0)
            {
                Rect r = new Rect(ancho / 2f - 520f, 640f, 1040f, 50f);
                Fondo(r);
                GUI.Label(r, "<color=#FF8060>" + pj.Bloqueo + "</color>", sAviso);
            }
            if (prueba.AyudaVisible) Panel(pj);
        }

        void Panel(PersonajeFlotante pj)
        {
            Rect r = new Rect(ancho - 494f, 14f, 480f, 1052f);
            Fondo(r);
            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 8f, r.width - 24f, r.height - 16f));
            desplazamiento = GUILayout.BeginScrollView(desplazamiento);
            GUILayout.Label("CIRCULACIÓN INTERIOR · EM-4X", sTitulo);
            if (pj != null)
                GUILayout.Label($"Estás en: <b>{pj.ModuloActual}</b> · {pj.Rapidez:0.0} m/s" +
                                (pj.Contactos.Length > 0 ? $"\n<size=13>Tocando: {pj.Contactos}</size>" : ""), sTexto);

            GUILayout.Space(4f);
            GUILayout.Label("CONTROLES (personaje)", sTitulo);
            GUILayout.Label("Ratón: mirar (gira el cuerpo, sin arriba global) · W/S/A/D: moverse · Espacio/Ctrl: subir/bajar según la vista · " +
                            "Z/X: girar sobre ti · Mayús: rápido · R: nivelar con el «arriba» del módulo · <b>E</b>: panel de puerta · " +
                            "T: rótulos · F5: volver a la zona común · F1: ocultar este panel · Tab: inspección · clic: recuperar el ratón", sPequeno);
            GUILayout.Label("<i>Al soltar las teclas se frena solo: ayuda provisional de la prueba.</i>", sPequeno);

            GUILayout.Space(4f);
            GUILayout.Label("PUERTAS", sTitulo);
            foreach (PuertasUnion u in prueba.Uniones)
            {
                if (u == null) continue;
                string est = u.EstadoActual == PuertasUnion.Estado.Abierta ? "<color=#59CC73>abierta</color>"
                           : u.EstadoActual == PuertasUnion.Estado.Cerrada ? "<color=#FF6050>cerrada</color>"
                           : $"<color=#FFB040>{(u.EstadoActual == PuertasUnion.Estado.Abriendo ? "abriendo" : "cerrando")} {u.Apertura * 100f:0}%</color>";
                GUILayout.Label($"{u.Datos.nombre}: {est}", sPequeno);
            }
            GUILayout.Label("Las puertas de aislamiento de los pasadizos empiezan abiertas y solo se cierran a mano (E en un panel de cualquiera de los dos lados).", sPequeno);
            GUILayout.Label("Escotilla exterior de la esclusa y puerto de suministro: cerrados, sin interacción.", sPequeno);

            GUILayout.Space(4f);
            GUILayout.Label("LUGARES ALCANZADOS (esta sesión)", sTitulo);
            foreach (var l in prueba.Lugares)
                GUILayout.Label((l.alcanzado ? "<color=#59CC73>[x]</color> " : "[ ] ") + l.nombre, sPequeno);

            GUILayout.Space(4f);
            GUILayout.Label("REVISIONES AUTOMÁTICAS", sTitulo);
            RevisionDistribucion.Resultado rd = prueba.Expedicion.Revision;
            Linea($"Distribución EM-4X (geometría y conexiones): {rd.comprobaciones} correctas", rd.problemas.Count, rd.problemas);
            Linea($"Pasos, puertas y paneles (datos): {prueba.RevisionPuertas.comprobaciones} correctas", prueba.RevisionPuertas.problemas.Count, prueba.RevisionPuertas.problemas);
            Linea($"Comprobación con colisiones al iniciar: {prueba.Comprobacion.correctas} correctas", prueba.Comprobacion.problemas.Count, prueba.Comprobacion.problemas);
            foreach (string n in prueba.RevisionPuertas.notas) GUILayout.Label("· " + n, sPequeno);
            GUILayout.Label($"Colisiones añadidas a los bloques de la nave: {prueba.Colisiones}.", sPequeno);
            GUILayout.Label("<i>Son comprobaciones automáticas, no la prueba del equipo: no validan la comodidad del control ni todos los enganches al girar.</i>", sPequeno);

            GUILayout.Space(4f);
            GUILayout.Label("INTENCIÓN FUTURA (sin función ahora)", sTitulo);
            foreach (string s in prueba.Circulacion.intencionesFuturas) GUILayout.Label("· " + s, sPequeno);
            GUILayout.Label("Sin presión, oxígeno, separación, atraque, sonda, bots ni red. Balances y transporte orbital: pendientes.", sPequeno);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void Linea(string texto, int problemas, System.Collections.Generic.List<string> lista)
        {
            if (problemas == 0) GUILayout.Label("[OK] " + texto + " · 0 problemas", sBien);
            else
            {
                GUILayout.Label($"[X] {texto} · {problemas} problemas:", sMal);
                foreach (string p in lista) GUILayout.Label("   " + p, sMal);
            }
        }
    }
}
