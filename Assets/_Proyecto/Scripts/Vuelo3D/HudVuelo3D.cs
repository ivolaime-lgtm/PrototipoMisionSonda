using UnityEngine;

namespace PrototipoMision
{
    /// <summary>
    /// Indicadores de la prueba V1 (SOLO DIBUJO, IMGUI escalado a 1080 de alto como el resto del prototipo).
    /// Proa (+), dirección del movimiento (O) y su contraria (X), balizas con flecha en el borde si quedan fuera de la vista,
    /// paneles de nave y objetivo, avisos y ayuda de controles. Solo lee PruebaVuelo3D.
    /// </summary>
    public class HudVuelo3D : MonoBehaviour
    {
        PruebaVuelo3D prueba;
        GUIStyle sTexto, sPequeno, sCentro, sAviso, sMarca;
        Texture2D blanco;
        float escala, ancho;

        void Start() { prueba = GetComponent<PruebaVuelo3D>(); }

        void Estilos()
        {
            if (sTexto != null) return;
            blanco = Texture2D.whiteTexture;
            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true, wordWrap = true };
            sTexto.normal.textColor = new Color(0.9f, 0.94f, 1f);
            sPequeno = new GUIStyle(sTexto) { fontSize = 15 };
            sCentro = new GUIStyle(sTexto) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            sAviso = new GUIStyle(sTexto) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            sAviso.normal.textColor = new Color(1f, 0.85f, 0.45f);
            sMarca = new GUIStyle(sCentro) { fontSize = 17 };
        }

        void Rellenar(Rect r, Color c)
        {
            Color antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, blanco);
            GUI.color = antes;
        }

        void OnGUI()
        {
            if (prueba == null || prueba.Estado == null || prueba.Vista == null || prueba.Vista.Camara == null) return;
            Estilos();
            escala = Screen.height / 1080f;
            ancho = Screen.width / escala;
            GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));

            Marcas();
            PanelNave();
            PanelFrenado();
            IndicadorAlineacion();
            PanelObjetivo();
            Avisos();
            Ayuda();
        }

        // ------------------------------------------------------------------ Marcas sobre la vista

        /// <summary>Dibuja una marca en un punto de la escena; si queda fuera de la vista, en el borde con flecha.</summary>
        void Marca(Vector3 puntoEscena, string simbolo, string texto, string color)
        {
            Camera cam = prueba.Vista.Camara;
            Vector3 sp = cam.WorldToScreenPoint(puntoEscena);
            bool detras = sp.z < 0f;
            if (detras) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }
            float x = sp.x / escala, y = (Screen.height - sp.y) / escala;
            const float m = 60f;
            bool dentro = !detras && x > m && x < ancho - m && y > m && y < 1080f - m;
            if (dentro)
            {
                GUI.Label(new Rect(x - 20f, y - 16f, 40f, 32f), $"<color={color}><b>{simbolo}</b></color>", sMarca);
                if (texto.Length > 0)
                    GUI.Label(new Rect(x - 160f, y + 14f, 320f, 24f), $"<color={color}>{texto}</color>", sMarca);
                return;
            }
            float cx = ancho / 2f, cy = 540f;
            float dx = x - cx, dy = y - cy;
            if (Mathf.Abs(dx) < 0.01f && Mathf.Abs(dy) < 0.01f) dy = 1f;
            float k = Mathf.Min((cx - m) / Mathf.Max(0.01f, Mathf.Abs(dx)), (cy - m) / Mathf.Max(0.01f, Mathf.Abs(dy)));
            float ex = cx + dx * k, ey = cy + dy * k;
            string flecha = Mathf.Abs(dx) * (cy - m) > Mathf.Abs(dy) * (cx - m) ? (dx > 0f ? "▶" : "◀") : (dy > 0f ? "▼" : "▲");
            GUI.Label(new Rect(ex - 170f, ey - 14f, 340f, 28f),
                      $"<color={color}><b>{flecha} {simbolo}</b> {texto}{(detras ? " (detrás)" : "")}</color>", sMarca);
        }

        void Marcas()
        {
            EstadoVuelo3D e = prueba.Estado;
            Camera cam = prueba.Vista.Camara;
            Vector3 ojo = cam.transform.position;

            Marca(e.Proa * 5000f, "+", "proa", "#FFFFFF");
            if (e.Rapidez > 0.05f)
            {
                Vector3 dir = e.Velocidad.AVector3().normalized;
                Marca(ojo + dir * 5000f, "O", $"movimiento {e.Rapidez:0.0} m/s", "#7CFC9A");
                Marca(ojo - dir * 5000f, "X", "FRENAR: lleva la proa (+) aquí", "#FF8A7A");
            }

            int objetivo = prueba.Objetivo.IndiceActual;
            for (int i = 0; i < prueba.Balizas.Count; i++)
            {
                DefinicionBaliza b = prueba.Balizas[i];
                Vector3 p = prueba.Vista.AEscena(b.posicionM);
                bool esObjetivo = i == objetivo;
                bool visitada = prueba.Objetivo.Visitada(i);
                if (!esObjetivo && !(prueba.Vista.Camara.WorldToScreenPoint(p).z > 0f)) continue;   // las demás, solo si están delante
                string col = esObjetivo ? "#5CE1FF" : visitada ? "#8A96A8" : "#C9B8FF";
                Marca(p, esObjetivo ? "[ ]" : "·", $"{b.nombre} {Metros(p.magnitude)}{(visitada ? " (visitada)" : "")}", col);
            }
        }

        static string Metros(float m) => m >= 1000f ? $"{m / 1000f:0.00} km" : $"{m:0} m";

        // ------------------------------------------------------------------ Paneles

        void PanelNave()
        {
            EstadoVuelo3D e = prueba.Estado;
            Rect r = new Rect(20f, 20f, 500f, 470f);
            Rellenar(r, new Color(0f, 0.04f, 0.08f, 0.62f));
            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 10f, r.width - 28f, r.height - 20f));

            Vector3 vn = e.VelocidadEnNave;
            float angulo = e.Rapidez > 0.05f ? Vector3.Angle(e.Proa, e.Velocidad.AVector3()) : 0f;
            GUILayout.Label("<b>NAVE</b>  <size=14>(DEMOSTRACIÓN · sin gravedad)</size>", sTexto);
            GUILayout.Label($"<b>VELOCIDAD</b>  {e.Rapidez:0.00} m/s" +
                            (e.Rapidez > 0.05f ? $" · {angulo:0}° entre proa y movimiento" : " · quieta"), sTexto);
            GUILayout.Label($"<size=15>En ejes de la nave: adelante {vn.z:+0.00;-0.00} · derecha {vn.x:+0.00;-0.00} · arriba {vn.y:+0.00;-0.00} m/s</size>", sTexto);

            Vector3 w = e.VelocidadAngularGradS;
            GUILayout.Label("<b>GIRO</b>  <size=15>(°/s; la nave sigue girando hasta que lo frenes)</size>", sTexto);
            BarraGiro("cabeceo (W/S)", w.x, e.GiroActivo.x);
            BarraGiro("guiñada (A/D)", w.y, e.GiroActivo.y);
            BarraGiro("alabeo (Z/C)", w.z, e.GiroActivo.z);
            GUILayout.Label("<b>ESTABILIZACIÓN DE GIRO</b> (T)  " + (e.Estabilizacion ? "<color=#7CFC9A>activada</color>" : "<color=#FFB040>desactivada</color>") +
                            " <size=15>· solo frena el GIRO, no la velocidad</size>", sTexto);

            Vector3 t = e.TraslacionActiva;
            string tras = t.sqrMagnitude > 0.0025f ? "<color=#FFCC66>activa</color>" : "—";
            GUILayout.Label($"<b>PROPULSORES</b>  motor {(e.MotorEncendido ? "<color=#FFB040><b>ENCENDIDO</b></color>" : "apagado")} · traslación {tras}", sTexto);

            float pct = e.Datos.combustibleKg > 0f ? e.Combustible / e.Datos.combustibleKg : 0f;
            string colC = pct < 0.1f ? "#FF6B5B" : pct < 0.25f ? "#FFB040" : "#FFFFFF";
            GUILayout.Label($"<b>COMBUSTIBLE</b>  <color={colC}>{e.Combustible:0.0} kg ({pct * 100f:0} %)</color> · Δv del motor {e.DeltaVMotorRestanteMS:0} m/s", sTexto);
            GUILayout.Label($"<size=15>Gastado: motor principal {e.GastoMotorKg:0.0} · traslación {e.GastoTraslacionKg:0.0} · orientación {e.GastoOrientacionKg:0.0} kg</size>", sTexto);
            GUILayout.Label($"<size=14>Aceleración: motor {e.AceleracionMotorMS2:0.00} m/s² · traslación {e.AceleracionTraslacionMS2:0.000} m/s²</size>", sTexto);
            GUILayout.EndArea();
        }

        /// <summary>Frenado del DESPLAZAMIENTO: guía, estimaciones y estado de la asistencia.</summary>
        void PanelFrenado()
        {
            EstadoVuelo3D e = prueba.Estado;
            AsistenteFrenado asis = prueba.Asistente;
            if (asis == null) return;
            Rect r = new Rect(20f, 500f, 500f, 270f);
            Rellenar(r, new Color(0f, 0.04f, 0.08f, 0.62f));
            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 10f, r.width - 28f, r.height - 20f));
            GUILayout.Label("<b>FRENADO DEL DESPLAZAMIENTO</b>  <size=15>(distinto de la estabilización de giro)</size>", sTexto);

            if (e.Rapidez <= 0.05f)
                GUILayout.Label("<color=#7CFC9A>Nave quieta (≤ 0,05 m/s).</color>", sTexto);
            else
            {
                float desvio = Vector3.Angle(e.Proa, -e.Velocidad.AVector3());
                string efecto = desvio <= 10f ? "<color=#7CFC9A><b>bien orientada: Espacio FRENA</b></color>"
                              : desvio < 90f ? $"<color=#FFB040>Espacio frena solo en parte (desvío {desvio:0}°)</color>"
                              : $"<color=#FF8A7A>Espacio ACELERARÍA (desvío {desvio:0}°)</color>";
                GUILayout.Label($"<size=16>Proa respecto a la X (frenar): {desvio:0}° · {efecto}</size>", sTexto);

                AsistenteFrenado.Estimacion m = AsistenteFrenado.ConMotor(e);
                AsistenteFrenado.Estimacion t = AsistenteFrenado.SoloTraslacion(e);
                GUILayout.Label($"<size=15>Con motor: girar ≈ {m.giroS:0} s y parar en ≈ {m.tiempoS:0} s y {Metros(m.distanciaM)} · ≈ {m.combustibleKg:0.0} kg</size>", sTexto);
                GUILayout.Label($"<size=15>Solo traslación: parar en ≈ {t.tiempoS:0} s y {Metros(t.distanciaM)} · ≈ {t.combustibleKg:0.0} kg</size>", sTexto);
                GUILayout.Label("<size=13>Estimación: empuje constante, giro desde parado a la máxima aceleración; no cuenta el giro que ya lleves ni las correcciones.</size>", sTexto);
            }

            string estado = asis.Activa
                ? $"<color=#FFCC66><b>{AsistenteFrenado.Nombre(asis.FaseActual)}</b></color> · gastado {asis.GastoKg:0.0} kg · B o cualquier mando cancela"
                : "inactiva · B la activa";
            GUILayout.Label($"<b>ASISTENCIA (B)</b>  {estado}", sTexto);
            GUILayout.EndArea();
        }

        /// <summary>Rótulo bajo la mira cuando la proa apunta contra el movimiento (el motor principal frena).</summary>
        void IndicadorAlineacion()
        {
            EstadoVuelo3D e = prueba.Estado;
            if (e.Rapidez <= 0.05f) return;
            float desvio = Vector3.Angle(e.Proa, -e.Velocidad.AVector3());
            if (desvio > 10f) return;
            GUI.Label(new Rect(ancho / 2f - 300f, 600f, 600f, 30f),
                      $"<color=#7CFC9A><b>ALINEADA PARA FRENAR</b> · Espacio reduce la velocidad ({e.Rapidez:0.0} m/s)</color>", sMarca);
        }

        void BarraGiro(string nombre, float gradS, float mando)
        {
            Rect fila = GUILayoutUtility.GetRect(460f, 24f);
            GUI.Label(new Rect(fila.x, fila.y, 150f, 24f), $"<size=15>{nombre}</size>", sTexto);
            float max = Mathf.Max(1f, prueba.Estado.Maniobra.velocidadAngularMaximaGradS > 0f ? prueba.Estado.Maniobra.velocidadAngularMaximaGradS : 45f);
            Rect barra = new Rect(fila.x + 150f, fila.y + 8f, 200f, 8f);
            Rellenar(barra, new Color(1f, 1f, 1f, 0.12f));
            float mitad = barra.x + barra.width / 2f;
            float largo = Mathf.Clamp(gradS / max, -1f, 1f) * barra.width / 2f;
            Rellenar(new Rect(largo >= 0f ? mitad : mitad + largo, barra.y, Mathf.Abs(largo), barra.height),
                     Mathf.Abs(mando) > 0.05f ? new Color(1f, 0.75f, 0.3f) : new Color(0.4f, 0.85f, 1f));
            Rellenar(new Rect(mitad - 1f, barra.y - 3f, 2f, barra.height + 6f), new Color(1f, 1f, 1f, 0.5f));
            GUI.Label(new Rect(fila.x + 360f, fila.y, 100f, 24f), $"<size=15>{gradS:+0.0;-0.0;0.0}</size>", sTexto);
        }

        void PanelObjetivo()
        {
            ObjetivoBalizas o = prueba.Objetivo;
            EstadoVuelo3D e = prueba.Estado;
            Rect r = new Rect(ancho - 500f, 20f, 480f, 360f);
            Rellenar(r, new Color(0f, 0.04f, 0.08f, 0.62f));
            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 10f, r.width - 28f, r.height - 20f));
            ObjetivoBalizas.Ajustes a = o.Configuracion;

            if (o.Completado)
            {
                GUILayout.Label("<b>OBJETIVO</b>  <color=#7CFC9A><b>COMPLETADO</b></color>", sTexto);
                GUILayout.Label($"Tiempo: {o.TiempoFinalS:0} s", sTexto);
                GUILayout.Label($"Combustible usado: {e.GastoMotorKg + e.GastoTraslacionKg + e.GastoOrientacionKg:0.0} kg", sTexto);
                GUILayout.Label("<size=15>Puedes seguir volando o reiniciar con F5.</size>", sTexto);
            }
            else
            {
                DefinicionBaliza b = o.Actual;
                GUILayout.Label($"<b>OBJETIVO</b>  {o.PasoActual + 1} de {o.Pasos}: <b>{(b != null ? b.nombre : "—")}</b>", sTexto);
                if (b != null && b.direccion.Length > 0) GUILayout.Label($"<size=15>Desde la salida: {b.direccion}</size>", sTexto);
                string mov = o.AcercamientoMS > 0.05f ? $"te acercas a {o.AcercamientoMS:0.00} m/s"
                           : o.AcercamientoMS < -0.05f ? $"<color=#FF8A7A>te alejas a {-o.AcercamientoMS:0.00} m/s</color>" : "ni te acercas ni te alejas";
                string quieta = o.VelocidadRelativaMS <= a.velocidadMaximaMS ? "#7CFC9A" : "#FFFFFF";
                GUILayout.Label($"Distancia restante <b>{Metros(o.DistanciaM)}</b>", sTexto);
                GUILayout.Label($"Velocidad total respecto a la baliza <color={quieta}><b>{o.VelocidadRelativaMS:0.00} m/s</b></color>", sTexto);
                GUILayout.Label($"<size=15>{mov} · lateral {o.LateralMS:0.00} m/s</size>", sTexto);
                string paso;
                switch (o.SituacionActual)
                {
                    case ObjetivoBalizas.Situacion.DemasiadoRapido:
                        paso = $"<color=#FFB040>Dentro de {a.distanciaLlegadaM:0} m: frena hasta {a.velocidadMaximaMS:0.0} m/s o menos de velocidad TOTAL.</color>"; break;
                    case ObjetivoBalizas.Situacion.Manteniendo:
                        paso = $"<color=#7CFC9A>Mantente así: {o.PermanenciaS:0.0} de {a.permanenciaS:0} s.</color>"; break;
                    default:
                        paso = $"Acércate y detente a menos de {a.distanciaLlegadaM:0} m (a {a.velocidadMaximaMS:0.0} m/s o menos, {a.permanenciaS:0} s seguidos)."; break;
                }
                GUILayout.Label(paso, sTexto);
                GUILayout.Label($"<size=15>Tiempo {o.TiempoS:0} s</size>", sTexto);
            }
            string lista = "";
            for (int i = 0; i < prueba.Balizas.Count; i++)
                lista += $"{prueba.Balizas[i].nombre}{(o.Visitada(i) ? " (visitada)" : "")}   ";
            GUILayout.Label($"<size=15>{lista}</size>", sTexto);
            GUILayout.EndArea();
        }

        void Avisos()
        {
            if (prueba.ResultadoActual == PruebaVuelo3D.Resultado.Colision)
            {
                Rect r = new Rect(ancho / 2f - 420f, 420f, 840f, 110f);
                Rellenar(r, new Color(0.25f, 0f, 0f, 0.8f));
                GUI.Label(r, $"<b>PRUEBA TERMINADA</b>\n{prueba.MotivoFin}\nF5: reiniciar", sAviso);
                return;
            }
            if (prueba.AvisoVisible)
            {
                Rect r = new Rect(ancho / 2f - 430f, 24f, 860f, 64f);
                Rellenar(r, new Color(0f, 0f, 0f, 0.55f));
                GUI.Label(r, prueba.Aviso, sAviso);
            }
        }

        void Ayuda()
        {
            if (!prueba.AyudaVisible)
            {
                GUI.Label(new Rect(20f, 1080f - 40f, 900f, 30f), "<size=15>H: ayuda de controles · B: frenado asistido · F5: reiniciar la prueba · V: cámara</size>", sTexto);
                return;
            }
            EstadoVuelo3D e = prueba.Estado;
            DatosCatalogoVuelo d = e.Datos;
            AjustesManiobra m = e.Maniobra;
            Rect r = new Rect(20f, 1080f - 300f, 980f, 280f);
            Rellenar(r, new Color(0f, 0.04f, 0.08f, 0.7f));
            GUILayout.BeginArea(new Rect(r.x + 14f, r.y + 10f, r.width - 28f, r.height - 20f));
            GUILayout.Label("<b>CONTROLES</b>  <size=14>(H oculta esta ayuda)</size>", sTexto);
            GUILayout.Label("<size=16><b>Orientación</b> (giro progresivo; se para en cualquier ángulo): W/S cabeceo (W baja el morro) · A/D guiñada · " +
                            "Z/C alabeo · T estabilización asistida</size>", sTexto);
            GUILayout.Label("<size=16><b>Traslación</b> (propulsores de maniobra): I/K adelante y atrás · J/L izquierda y derecha · U/O arriba y abajo</size>", sTexto);
            GUILayout.Label("<size=16><b>Motor principal</b>: Espacio mantenido, empuja hacia la proa · <b>B</b>: frenado asistido (B o cualquier mando lo cancela) · " +
                            "<b>Cámara</b>: V asiento o exterior · botón derecho del ratón: mirar · <b>F5</b>: reiniciar</size>", sTexto);
            GUILayout.Label($"<size=14><b>Del catálogo</b> ({d.origen}): masa seca {d.masaSecaKg:0} kg · combustible {d.combustibleKg:0.0} kg · " +
                            $"motor principal {d.empujePrincipalN:0} N y {d.consumoPrincipalKgS:0.00} kg/s</size>", sTexto);
            GUILayout.Label($"<size=14><b>Provisionales</b>: traslación {m.empujeTraslacionN:0} N y {m.consumoTraslacionKgS:0.00} kg/s por eje · " +
                            $"giro {m.aceleracionAngularGradS2.x:0}/{m.aceleracionAngularGradS2.y:0}/{m.aceleracionAngularGradS2.z:0} °/s² y " +
                            $"{m.consumoOrientacionKgS:0.00} kg/s por eje · límite de giro {m.velocidadAngularMaximaGradS:0} °/s</size>", sTexto);
            GUILayout.EndArea();
        }
    }
}
