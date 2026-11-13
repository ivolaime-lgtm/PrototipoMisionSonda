using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrototipoMision
{
    /// <summary>
    /// Pantalla de configuración de la misión (interfaz provisional IMGUI).
    /// Lee las opciones del CatalogoMision, muestra coste, masa, energía y datos,
    /// explica qué cambia cada elección y bloquea el inicio si se superan los límites.
    ///
    /// Modo equipo (1 a 4 jugadores en el mismo PC):
    /// - Cada rol decide su parte: Ciencia (objetivo e instrumento), Ingeniería (batería),
    ///   Lanzamiento (lanzador) y Comunicaciones (órbita).
    /// - Solo el jugador en turno puede cambiar las secciones de sus roles.
    /// - Para iniciar, todos los jugadores deben aprobar. Cualquier cambio anula las aprobaciones.
    /// Las opciones y los textos se editan en el asset CatalogoMision, no aquí.
    /// </summary>
    public class PantallaConfiguracion : MonoBehaviour
    {
        [SerializeField] CatalogoMision catalogo;
        [SerializeField] string escenaExploracion = "Exploracion";
        [Tooltip("Escena de la nave. Si está activado y existe, la misión empieza en la nave.")]
        [SerializeField] string escenaNave = "Nave";
        [SerializeField] bool pasarPorNave = true;

        [Header("Textos editables")]
        [SerializeField] string titulo = "CONFIGURACIÓN DE LA MISIÓN";
        [SerializeField] string textoUnidades =
            "Unidades: M USD = millones de dólares · kg = kilogramos · Wh = energía guardada · " +
            "W = consumo · MB = megabytes de datos · MB/s = velocidad de transmisión. Todos los valores son DEMOSTRACIÓN.";

        const string Aplicado = "<color=#7CFC9A><b>APLICADO</b></color>";
        const string Cumple = "<color=#7CFC9A><b>SE CUMPLE</b></color>";
        const string NoCumple = "<color=#FF6B5B><b>NO SE CUMPLE</b></color>";

        ConfiguracionElegida eleccion;
        ResumenMision resumen;
        Vector2 desplazamiento;

        // Modo equipo
        int turno;                                              // jugador que puede editar ahora
        readonly bool[] aprobado = new bool[ModoEquipo.MaxJugadores];

        // Selección de misión (A2) y vista exterior de la nave (B)
        bool mostrandoMisiones;
        int destinoPedido = -1;                                 // se aplica al final del evento de dibujo
        VistaNaveConfiguracion vista;
        ResumenNave resumenNave;

        GUIStyle sTitulo, sSeccion, sTexto, sPequeno, sBoton, sBotonGrande, sProblema, sAviso;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (MisionActual.Configurada)
                eleccion = MisionActual.Eleccion;

            // Si el catálogo cambió y alguna opción guardada ya no existe, se ajusta a una válida
            // para que el botón marcado coincida con lo que muestra el resumen.
            if (catalogo != null)
            {
                eleccion.objetivo = Ajustar(eleccion.objetivo, catalogo.objetivos);
                eleccion.instrumento = Ajustar(eleccion.instrumento, catalogo.instrumentos);
                eleccion.bateria = Ajustar(eleccion.bateria, catalogo.baterias);
                eleccion.lanzador = Ajustar(eleccion.lanzador, catalogo.lanzadores);
                eleccion.orbita = Ajustar(eleccion.orbita, catalogo.orbitas);
                eleccion.destino = Ajustar(eleccion.destino, catalogo.destinos);
                if (catalogo.catalogoNave != null)
                {
                    eleccion.motorNave = Ajustar(eleccion.motorNave, catalogo.catalogoNave.motores);
                    eleccion.tanqueNave = Ajustar(eleccion.tanqueNave, catalogo.catalogoNave.tanques);
                }
                else
                    Debug.LogWarning("[Prototipo] CatalogoMision no tiene enlazado el CatalogoNave: no se mostrarán las piezas de la nave.", this);
            }

            // La primera vez se elige la misión; al volver a configurar se va directo a las piezas.
            mostrandoMisiones = !MisionActual.Configurada;
            vista = gameObject.AddComponent<VistaNaveConfiguracion>();

            // Cada vez que se entra en la configuración, el equipo vuelve a aprobar.
            turno = 0;
            AnularAprobaciones();
            Recalcular();
        }

        void Recalcular()
        {
            resumen = catalogo != null ? catalogo.Calcular(eleccion) : null;
            resumenNave = catalogo != null && catalogo.catalogoNave != null && resumen != null
                ? catalogo.catalogoNave.Calcular(resumen) : null;
        }

        void AnularAprobaciones()
        {
            for (int i = 0; i < aprobado.Length; i++) aprobado[i] = false;
        }

        bool TodosAprueban()
        {
            if (!ModoEquipo.EnEquipo) return true;
            for (int i = 0; i < ModoEquipo.Jugadores; i++)
                if (!aprobado[i]) return false;
            return true;
        }

        string Pendientes()
        {
            string t = "";
            for (int i = 0; i < ModoEquipo.Jugadores; i++)
                if (!aprobado[i]) t += (t.Length > 0 ? ", " : "") + ModoEquipo.NombreJugador(i);
            return t;
        }

        bool PuedeEditar(Rol rol) => !ModoEquipo.EnEquipo || ModoEquipo.Responsable(rol) == turno;

        void OnGUI()
        {
            if (catalogo == null || resumen == null)
            {
                GUI.Label(new Rect(20, 20, 900, 40),
                    "Falta asignar el asset CatalogoMision en el componente PantallaConfiguracion.");
                return;
            }

            PrepararEstilos();

            // Catálogo incompleto (alguna lista vacía): solo se muestran los problemas.
            if (resumen.objetivo == null || resumen.instrumento == null || resumen.bateria == null ||
                resumen.lanzador == null || resumen.orbita == null)
            {
                GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
                GUILayout.Label("El catálogo de misión está incompleto:", sTexto);
                foreach (string p in resumen.problemas) GUILayout.Label("[X] " + p, sProblema);
                GUILayout.EndArea();
                return;
            }

            float escala = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
            Rellenar(new Rect(0, 0, Screen.width / escala, Screen.height / escala), new Color(0.02f, 0.08f, 0.11f));

            if (mostrandoMisiones)
            {
                bool elegir = DibujarMisiones();
                GUI.matrix = Matrix4x4.identity;
                if (destinoPedido >= 0)
                {
                    eleccion.destino = Ajustar(destinoPedido, catalogo.destinos);
                    destinoPedido = -1;
                    Recalcular();
                    AnularAprobaciones();
                }
                if (elegir) mostrandoMisiones = false;
                return;
            }
            bool verMisiones = false;

            // Todos los cambios se anotan aquí y se aplican al final del evento,
            // para no alterar la interfaz a mitad de un dibujado.
            ConfiguracionElegida nueva = eleccion;
            bool iniciar = false;
            int nuevosJugadores = -1;
            int nuevoTurno = -1;
            int rolReasignado = -1, jugadorReasignado = -1;
            int aprobacionCambiada = -1;

            // ---------------- Columna izquierda: equipo y opciones
            GUILayout.BeginArea(new Rect(40, 25, 1120, 1035));
            GUILayout.BeginHorizontal();
            GUILayout.Label(titulo, sTitulo);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("VER MISIÓN", sBoton, GUILayout.Height(40), GUILayout.Width(200))) verMisiones = true;
            GUILayout.EndHorizontal();
            GUILayout.Label($"<b>Misión:</b> {catalogo.nombreMision} · destino: {catalogo.NombreDestino(eleccion.destino)} · dificultad {catalogo.dificultadMision}", sTexto);
            desplazamiento = GUILayout.BeginScrollView(desplazamiento);

            DibujarEquipo(ref nuevosJugadores, ref nuevoTurno, ref rolReasignado, ref jugadorReasignado);

            nueva.objetivo = Seccion("1. OBJETIVO CIENTÍFICO", Rol.Ciencia, Nombres(catalogo.objetivos, o => o.nombre), eleccion.objetivo);
            Texto(resumen.objetivo.descripcion);
            Datos($"Escaneos requeridos: {resumen.objetivo.escaneosRequeridos} {Aplicado} · " +
                  $"Instrumento recomendado: {resumen.objetivo.instrumentoRecomendado} " +
                  $"(×{catalogo.multiplicadorInstrumentoRecomendado:0.#} de valor científico)");

            nueva.instrumento = Seccion("2. INSTRUMENTO CIENTÍFICO", Rol.Ciencia, Nombres(catalogo.instrumentos, i => i.nombre), eleccion.instrumento);
            OpcionInstrumento ins = resumen.instrumento;
            Texto(ins.queCambia);
            Datos($"Coste {ins.costeMUSD:0.#} M USD · Masa {ins.masaKg:0.#} kg {Aplicado}\n" +
                  $"Consumo al escanear {ins.consumoEscaneoW:0} W · {ins.datosPorEscaneoMB:0} MB por escaneo · " +
                  $"{ins.segundosEscaneo:0.#} s por escaneo · valor {ins.valorCientifico} {Aplicado}");
            Prioridad(Rol.Ciencia);

            nueva.bateria = Seccion("3. BATERÍA", Rol.Ingenieria, Nombres(catalogo.baterias, b => b.nombre), eleccion.bateria);
            OpcionBateria bat = resumen.bateria;
            Texto(bat.queCambia);
            Datos($"Coste {bat.costeMUSD:0.#} M USD · Masa {bat.masaKg:0.#} kg · Capacidad {bat.capacidadWh:0} Wh {Aplicado}");
            Prioridad(Rol.Ingenieria);

            nueva.lanzador = Seccion("4. LANZADOR", Rol.Lanzamiento, Nombres(catalogo.lanzadores, l => l.nombre), eleccion.lanzador);
            OpcionLanzador lan = resumen.lanzador;
            Texto(lan.queCambia);
            Datos($"Coste {lan.costeMUSD:0.#} M USD · Capacidad de carga {lan.capacidadCargaKg:0} kg {Aplicado} (bloquea el inicio si se supera)");
            Prioridad(Rol.Lanzamiento);

            nueva.orbita = Seccion("5. ÓRBITA DEL SATÉLITE DE RELEVO", Rol.Comunicaciones, Nombres(catalogo.orbitas, o => o.nombre), eleccion.orbita);
            OpcionOrbita orb = resumen.orbita;
            Texto(orb.queCambia);
            Datos($"Coste {orb.costeMUSD:0.#} M USD · Masa del relevo {orb.masaReleKg:0} kg {Aplicado} (cuenta para el lanzador)\n" +
                  $"Enlace {orb.velocidadEnlaceMBs:0} MB/s con contacto {orb.fraccionContacto * 100f:0} % del tiempo " +
                  $"→ efectivo {resumen.enlaceEfectivoMBs:0.#} MB/s {Aplicado}");
            Prioridad(Rol.Comunicaciones);

            // ---- Piezas de la NAVE tripulada (catálogo propio, no componentes de la sonda)
            CatalogoNave cn = catalogo.catalogoNave;
            if (cn != null && cn.motores != null && cn.motores.Count > 0)
            {
                nueva.motorNave = Seccion("6. MOTOR DE LA NAVE TRIPULADA", Rol.Ingenieria, Nombres(cn.motores, m => m.nombre), eleccion.motorNave);
                OpcionMotorNave mot = resumen.motorNave;
                if (mot != null)
                {
                    Texto(mot.queCambia);
                    Datos($"Coste {mot.costeMUSD:0.#} M USD · Masa {mot.masaKg:0} kg (+{cn.MasaExtraMotor(mot):0} kg sobre el estándar: cuenta en el margen de carga) {Aplicado}\n" +
                          $"Empuje {mot.empujeN / 1000f:0} kN · consumo {mot.consumoKgS:0.0} kg/s a pleno empuje" +
                          (resumenNave != null ? $" → aceleración de maniobra {resumenNave.aceleracionInicialMS2:0.00} m/s² {Aplicado} (esquivar escombros)" : ""));
                }

                if (cn.tanques != null && cn.tanques.Count > 0)
                {
                    nueva.tanqueNave = Seccion("7. TANQUE DE COMBUSTIBLE DE LA NAVE", Rol.Ingenieria, Nombres(cn.tanques, t => t.nombre), eleccion.tanqueNave);
                    OpcionTanqueNave tan = resumen.tanqueNave;
                    if (tan != null)
                    {
                        Texto(tan.queCambia);
                        Datos($"Coste {tan.costeMUSD:0.#} M USD · Masa vacío {tan.masaVaciaKg:0} kg (+{cn.MasaExtraTanque(tan):0} kg sobre el estándar: cuenta en el margen de carga) {Aplicado}\n" +
                              $"Capacidad {tan.capacidadKg:0} kg" +
                              (resumenNave != null
                                  ? $" → combustible al partir {resumenNave.combustibleInicialKg:0} kg {Aplicado}" +
                                    (resumenNave.combustibleNoCabeKg > 0f ? $" (el margen daría {resumenNave.combustibleNoCabeKg:0} kg más, pero no caben)" : "")
                                  : "") +
                              "\n<size=15>Combustible = 300 kg base + margen de carga sobrante (1 kg por kg), con el tope del tanque. Se usa en maniobras y en la frenada final.</size>");
                    }
                }
            }
            else
                Datos("<color=#FFB040>Falta enlazar el CatalogoNave en el CatalogoMision: no hay piezas de la nave.</color>");

            GUILayout.Space(10);
            GUILayout.Label("Modelo simplificado: el lanzador y la órbita no se simulan físicamente; " +
                            "solo cambian coste, masa y velocidad de transmisión. Las prioridades de los roles " +
                            "son orientativas: no bloquean el inicio, pero aparecen en el informe.", sPequeno);

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // ---------------- Columna derecha: resumen
            GUILayout.BeginArea(new Rect(1190, 25, 690, 1035));
            GUILayout.Label("RESUMEN", sTitulo);

            // Vista exterior: el motor elegido cambia la pieza visible; el lanzador se separa en el despegue.
            Rect rv = GUILayoutUtility.GetRect(650f, 200f, GUILayout.ExpandWidth(true));
            DibujarVista(rv);

            Barra("Coste total", resumen.costeTotalMUSD, resumen.presupuestoMUSD, "M USD");
            Barra("Margen de carga configurable del lanzador", resumen.masaLanzamientoKg, resumen.capacidadLanzadorKg, "kg");
            Datos($"Carga científica {resumen.cargaCientificaKg:0} kg + piezas de la nave no estándar {resumen.masaVariantesNaveKg:0} kg. " +
                  "El vehículo tripulado base se considera incluido en el lanzador (simplificación).");
            if (resumenNave != null)
                Texto($"Nave: masa total inicial {resumenNave.masaInicialKg:0} kg " +
                      $"<size=15>(seca {resumenNave.masaSecaKg:0} + carga científica {resumenNave.cargaCientificaKg:0} + combustible {resumenNave.combustibleInicialKg:0})</size> {Aplicado}");

            GUILayout.Space(6);
            Texto($"Masa de la sonda: {resumen.masaSondaKg:0} kg {Aplicado}\n<size=15>Más masa = la sonda acelera menos.</size>");
            Texto($"Energía de la batería: {resumen.energiaWh:0} Wh {Aplicado}");
            Texto($"Energía estimada para escanear: {resumen.energiaEscaneosWh:0.#} Wh <size=15>(estimación)</size>");
            Texto($"Datos previstos: {resumen.datosMisionMB:0} MB de {resumen.memoriaMB:0} MB de memoria {Aplicado}");
            string tx = float.IsInfinity(resumen.segundosTransmision)
                ? "sin enlace"
                : $"≈ {resumen.segundosTransmision:0} s reales <size=15>(media, contando las ventanas sin contacto)</size>";
            Texto($"Transmitir todos los datos: {tx} {Aplicado}");
            Texto($"Valor científico por escaneo: {resumen.valorPorEscaneo} puntos {Aplicado}");

            GUILayout.Space(8);
            foreach (string p in resumen.problemas) GUILayout.Label("[X] " + p, sProblema);
            foreach (string a in resumen.avisos) GUILayout.Label("[!] " + a, sAviso);
            if (resumen.problemas.Count == 0 && resumen.avisos.Count == 0)
                GUILayout.Label("[OK] La configuración cumple todos los límites.", sTexto);

            GUILayout.FlexibleSpace();

            if (ModoEquipo.EnEquipo)
                aprobacionCambiada = DibujarAprobaciones();

            bool todosAprueban = TodosAprueban();
            bool habilitado = GUI.enabled;
            GUI.enabled = resumen.PuedeIniciar && todosAprueban;
            string textoBoton = !resumen.PuedeIniciar ? "NO SE PUEDE INICIAR · revisa los límites"
                              : !todosAprueban ? "FALTAN APROBACIONES: " + Pendientes()
                              : "INICIAR MISIÓN";
            if (GUILayout.Button(textoBoton, sBotonGrande, GUILayout.Height(64)))
                iniciar = true;
            GUI.enabled = habilitado;

            GUILayout.Space(8);
            GUILayout.Label(textoUnidades, sPequeno);
            GUILayout.Space(10);
            GUILayout.EndArea();

            GUI.matrix = Matrix4x4.identity;

            // ---------------- Aplicar cambios al final del evento
            bool cambioEquipo = false;
            if (nuevosJugadores > 0 && nuevosJugadores != ModoEquipo.Jugadores)
            {
                ModoEquipo.EstablecerJugadores(nuevosJugadores);
                turno = 0;
                cambioEquipo = true;
            }
            if (rolReasignado >= 0)
            {
                ModoEquipo.AsignarResponsable((Rol)rolReasignado, jugadorReasignado);
                cambioEquipo = true;
            }
            if (nuevoTurno >= 0) turno = Mathf.Clamp(nuevoTurno, 0, ModoEquipo.Jugadores - 1);
            if (aprobacionCambiada >= 0) aprobado[aprobacionCambiada] = !aprobado[aprobacionCambiada];

            if (!nueva.Equals(eleccion))
            {
                eleccion = nueva;
                Recalcular();
                AnularAprobaciones();   // cualquier cambio obliga a volver a aprobar
            }
            if (cambioEquipo) AnularAprobaciones();
            if (verMisiones) mostrandoMisiones = true;

            if (iniciar && resumen.PuedeIniciar && TodosAprueban())
            {
                MisionActual.Guardar(eleccion);
                EstadoViaje.Reiniciar();
                SceneManager.LoadScene(EstadoViaje.EscenaDeInicio(pasarPorNave, escenaNave, escenaExploracion));
            }
        }

        // ---------------------------------------------------------------- Misión y vista

        /// <summary>Selección de misión: una completa y las demás como «Próximamente» (sin botón).</summary>
        bool DibujarMisiones()
        {
            bool elegir = false;
            GUI.Label(new Rect(60f, 30f, 1200f, 60f), "SELECCIÓN DE MISIÓN", sTitulo);
            GUI.Label(new Rect(60f, 80f, 1500f, 30f), "Valores y escenario de DEMOSTRACIÓN: misión ficticia, no presentada como una misión real.", sPequeno);

            Rect tarjeta = new Rect(60f, 130f, 1000f, 850f);
            Rellenar(tarjeta, new Color(0.05f, 0.16f, 0.2f));
            Rellenar(new Rect(tarjeta.x, tarjeta.y, 6f, tarjeta.height), new Color(1f, 0.55f, 0.15f));
            GUILayout.BeginArea(new Rect(tarjeta.x + 30f, tarjeta.y + 20f, tarjeta.width - 60f, tarjeta.height - 40f));
            GUILayout.Label(catalogo.nombreMision.ToUpperInvariant(), sTitulo);
            if (catalogo.destinos != null && catalogo.destinos.Count > 0)
            {
                // Reutiliza la tarjeta de misión: un botón por destino. Cada partida va a un solo destino.
                int actual = Ajustar(eleccion.destino, catalogo.destinos);
                Texto("<b>Destino</b> (elige uno; cada partida va a un solo destino):");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < catalogo.destinos.Count; i++)
                {
                    bool marcado = i == actual;
                    if (GUILayout.Button((marcado ? "● " : "○ ") + catalogo.destinos[i].nombre, sBoton, GUILayout.Height(46)) && !marcado)
                        destinoPedido = i;
                }
                GUILayout.EndHorizontal();
                DefinicionDestino d = catalogo.destinos[actual];
                Datos(d.diferenciaJugable);
                Datos($"Ruta {d.distanciaRutaM / 1000f:0.#} km · zona de aproximación ±{d.semiZonaM:0} m · captura a ≤ {d.velocidadCapturaMS:0} m/s · " +
                      $"gravedad experimental: caída de {d.velocidadCaidaMS:0} m/s. Estas diferencias se juegan con la navegación longitudinal " +
                      "activada; con el viaje clásico solo cambian el nombre y el aspecto.");
            }
            else Texto($"<b>Destino:</b> {catalogo.destinoMision}");
            Texto($"<b>Objetivo:</b> {catalogo.objetivoMision}");
            Texto($"<b>Presupuesto:</b> {catalogo.presupuestoMUSD:0} M USD");
            Texto($"<b>Dificultad:</b> {catalogo.dificultadMision}. Condiciones concretas:");
            if (catalogo.condicionesMision != null)
                foreach (string c in catalogo.condicionesMision) Texto("   • " + c);
            GUILayout.Space(8);
            Datos("Tú controlas la nave; la nave tiene puestos para cuatro tripulantes. La exploración con la sonda es una fase posterior opcional.");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("ELEGIR ESTA MISIÓN", sBotonGrande, GUILayout.Height(64))) elegir = true;
            GUILayout.EndArea();

            float y = 130f;
            if (catalogo.misionesProximamente != null)
                foreach (string m in catalogo.misionesProximamente)
                {
                    Rect r = new Rect(1100f, y, 760f, 200f);
                    Rellenar(r, new Color(0.08f, 0.1f, 0.12f));
                    GUI.Label(new Rect(r.x + 24f, r.y + 20f, r.width - 48f, 70f), m, sSeccion);
                    GUI.Label(new Rect(r.x + 24f, r.y + 120f, r.width - 48f, 40f), "<color=#8A9AA5><b>PRÓXIMAMENTE</b> · no disponible en esta demostración</color>", sTexto);
                    y += 230f;
                }
            return elegir;
        }

        void DibujarVista(Rect r)
        {
            Rellenar(r, new Color(0.03f, 0.09f, 0.12f));
            if (vista != null)
            {
                vista.Mostrar(resumen.motorNave, resumen.tanqueNave, 1f + 0.25f * eleccion.lanzador);
                if (vista.Textura != null) GUI.DrawTexture(r, vista.Textura, ScaleMode.ScaleAndCrop);
            }
            string motor = resumen.motorNave != null ? resumen.motorNave.nombre : "sin datos";
            string tanque = resumen.tanqueNave != null ? resumen.tanqueNave.nombre : "sin datos";
            GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 20f, 26f), $"<b>Motor:</b> {motor} · <b>Tanque:</b> {tanque}", sPequeno);
            GUI.Label(new Rect(r.x + 10f, r.yMax - 28f, r.width / 2f, 26f), $"LANZADOR ({resumen.lanzador.nombre}) · se separa", sPequeno);
            GUI.Label(new Rect(r.x + r.width / 2f, r.yMax - 28f, r.width / 2f - 10f, 26f), "VEHÍCULO TRIPULADO · continúa", sPequeno);
        }

        // ---------------------------------------------------------------- Equipo

        void DibujarEquipo(ref int nuevosJugadores, ref int nuevoTurno, ref int rolReasignado, ref int jugadorReasignado)
        {
            GUILayout.Space(10);
            GUILayout.Label("EQUIPO (en este PC)", sSeccion);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Jugadores:", sTexto, GUILayout.Width(160));
            for (int n = 1; n <= ModoEquipo.MaxJugadores; n++)
            {
                if (BotonOpcion(n.ToString(), n == ModoEquipo.Jugadores, 70))
                    nuevosJugadores = n;
            }
            GUILayout.EndHorizontal();

            if (!ModoEquipo.EnEquipo)
            {
                Datos("Un jugador decide todo. Con 2 a 4 jugadores, cada uno tiene uno o más roles y todos deben aprobar.");
                return;
            }

            for (int r = 0; r < ModoEquipo.MaxJugadores; r++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(ModoEquipo.NombresRol[r] + ":", sTexto, GUILayout.Width(160));
                for (int j = 0; j < ModoEquipo.Jugadores; j++)
                {
                    if (BotonOpcion(ModoEquipo.NombreJugador(j), ModoEquipo.Responsable((Rol)r) == j, 70))
                    {
                        rolReasignado = r;
                        jugadorReasignado = j;
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Turno:", sTexto, GUILayout.Width(160));
            for (int j = 0; j < ModoEquipo.Jugadores; j++)
            {
                if (BotonOpcion(ModoEquipo.NombreJugador(j), j == turno, 70))
                    nuevoTurno = j;
            }
            GUILayout.EndHorizontal();

            string roles = ModoEquipo.RolesDe(turno);
            Datos($"Turno de {ModoEquipo.NombreJugador(turno)} ({(roles.Length > 0 ? roles : "sin roles")}): " +
                  "solo puede cambiar las secciones de sus roles. Cada cambio anula las aprobaciones.");
        }

        int DibujarAprobaciones()
        {
            int cambiada = -1;
            GUILayout.Label("APROBACIONES DEL EQUIPO", sSeccion);
            GUILayout.BeginHorizontal();
            for (int j = 0; j < ModoEquipo.Jugadores; j++)
            {
                string texto = ModoEquipo.NombreJugador(j) + (aprobado[j] ? ": APROBADO" : ": aprobar");
                Color previo = GUI.backgroundColor;
                GUI.backgroundColor = aprobado[j] ? new Color(0.4f, 1f, 0.55f) : new Color(0.55f, 0.6f, 0.65f);
                if (GUILayout.Button(texto, sBoton, GUILayout.Height(44)))
                    cambiada = j;
                GUI.backgroundColor = previo;
            }
            GUILayout.EndHorizontal();
            Datos("Cada jugador aprueba cuando está de acuerdo. Si alguien cambia algo, hay que volver a aprobar.");
            GUILayout.Space(6);
            return cambiada;
        }

        bool BotonOpcion(string texto, bool elegido, float ancho)
        {
            Color previo = GUI.backgroundColor;
            GUI.backgroundColor = elegido ? new Color(0.3f, 0.9f, 1f) : new Color(0.55f, 0.6f, 0.65f);
            bool pulsado = GUILayout.Button((elegido ? "• " : "") + texto, sBoton, GUILayout.Height(36), GUILayout.Width(ancho));
            GUI.backgroundColor = previo;
            return pulsado;
        }

        void Prioridad(Rol rol)
        {
            EstadoPrioridad e = ModoEquipo.Evaluar(rol, resumen, catalogo);
            Datos($"Prioridad de {ModoEquipo.NombreRol(rol)} ({ModoEquipo.PrioridadesRol[(int)rol]}): " +
                  $"{(e.cumple ? Cumple : NoCumple)} · {e.detalle}");
        }

        // ---------------------------------------------------------------- Bloques

        int Seccion(string encabezado, Rol rol, string[] nombres, int actual)
        {
            GUILayout.Space(14);
            string responsable = ModoEquipo.EnEquipo
                ? $"   · {ModoEquipo.NombreRol(rol)} ({ModoEquipo.NombreJugador(ModoEquipo.Responsable(rol))})"
                : $"   · {ModoEquipo.NombreRol(rol)}";
            GUILayout.Label(encabezado + responsable, sSeccion);

            bool habilitado = GUI.enabled;
            GUI.enabled = PuedeEditar(rol);
            GUILayout.BeginHorizontal();
            int resultado = actual;
            for (int i = 0; i < nombres.Length; i++)
            {
                bool elegido = i == actual;
                Color previo = GUI.backgroundColor;
                GUI.backgroundColor = elegido ? new Color(0.3f, 0.9f, 1f) : new Color(0.55f, 0.6f, 0.65f);
                if (GUILayout.Button((elegido ? "• " : "") + nombres[i], sBoton, GUILayout.Height(44)))
                    resultado = i;
                GUI.backgroundColor = previo;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = habilitado;
            return resultado;
        }

        void Texto(string t) => GUILayout.Label(t, sTexto);
        void Datos(string t) => GUILayout.Label(t, sPequeno);

        void Barra(string etiqueta, float valor, float limite, string unidad)
        {
            bool excedido = valor > limite;
            GUILayout.Label($"{etiqueta}: {valor:0} / {limite:0} {unidad}" + (excedido ? "  <color=#FF6B5B><b>EXCEDIDO</b></color>" : ""), sTexto);
            Rect r = GUILayoutUtility.GetRect(600, 22, GUILayout.ExpandWidth(true));
            Rellenar(r, new Color(1f, 1f, 1f, 0.12f));
            float f = limite > 0f ? Mathf.Clamp01(valor / limite) : 1f;
            Rellenar(new Rect(r.x, r.y, r.width * f, r.height),
                     excedido ? new Color(1f, 0.35f, 0.3f) : f > 0.85f ? new Color(1f, 0.8f, 0.2f) : new Color(0.3f, 0.9f, 1f));
            GUILayout.Space(6);
        }

        static int Ajustar<T>(int indice, List<T> lista)
        {
            if (lista == null || lista.Count == 0) return 0;
            return Mathf.Clamp(indice, 0, lista.Count - 1);
        }

        static string[] Nombres<T>(List<T> lista, Func<T, string> nombre)
        {
            if (lista == null) return new string[0];
            string[] r = new string[lista.Count];
            for (int i = 0; i < lista.Count; i++) r[i] = nombre(lista[i]);
            return r;
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

            sTitulo = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, richText = true };
            sTitulo.normal.textColor = new Color(0.75f, 0.95f, 1f);

            sSeccion = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            sSeccion.normal.textColor = new Color(0.55f, 0.85f, 0.95f);

            sTexto = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, richText = true };
            sTexto.normal.textColor = Color.white;

            sPequeno = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = true };
            sPequeno.normal.textColor = new Color(0.78f, 0.88f, 0.93f);

            sBoton = new GUIStyle(GUI.skin.button) { fontSize = 19, wordWrap = true };

            sBotonGrande = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };

            sProblema = new GUIStyle(sTexto);
            sProblema.normal.textColor = new Color(1f, 0.45f, 0.4f);

            sAviso = new GUIStyle(sTexto);
            sAviso.normal.textColor = new Color(1f, 0.82f, 0.35f);
        }
    }
}
