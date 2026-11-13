using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    public enum ResultadoExpedicion { EnCurso, Exito, Parcial, Fracaso }
    public enum EstadoBus { Apagado, Alimentado, Protegido }

    /// <summary>Daño de casco en un módulo. Persiste aunque se cambie entre Pilotaje e interior.</summary>
    public class DanoModulo
    {
        public string modulo, nombreModulo, causa;
        public float velocidadMS;
        public bool grave, guionizado;
        public bool reparado;
        /// <summary>Lo actualiza el interior: todas las puertas de aislamiento del módulo están cerradas.</summary>
        public bool aislado;
        public float trabajoS;
    }

    /// <summary>
    /// ESTADO COMPARTIDO de la expedición (SOLO LÓGICA): es el mismo objeto para Pilotaje, el interior, los puestos y los
    /// resultados. Contiene el vuelo (EstadoVuelo3D existente + gravedad del SistemaKora), el tiempo, los recursos (combustible,
    /// energía, soporte vital, kits), los datos científicos, los daños y el progreso de la misión. Nada se reinicia al cambiar
    /// de puesto: quien dibuja (interior o vista de vuelo) solo lee este estado.
    /// </summary>
    public class EstadoExpedicion
    {
        public readonly SistemaKora Sistema;
        public readonly ResumenExpedicion Config;
        public readonly DefinicionMisionMVP Mision;
        public readonly EstadoVuelo3D Vuelo;
        public readonly AsistenteFrenado Frenado;
        readonly float minutosPorSegundo, consumoTransmisionW;

        public double Tiempo;                       // segundos de vuelo simulados
        public readonly List<CuerpoKora> Objetivos = new List<CuerpoKora>();
        public int ObjetivoSel;
        /// <summary>Ayuda de orientación: 0 nada, 1 proa al objetivo, 2 retrógrado respecto a Kora, 3 progrado.</summary>
        public int Apuntar;
        public static readonly string[] NombresApuntar = { "desactivada", "proa al objetivo", "proa RETRÓGRADA (frenar la órbita)", "proa PROGRADA (acelerar la órbita)" };

        // Recursos
        public float EnergiaWh;
        public EstadoBus BusCiencia = EstadoBus.Apagado, BusComunicaciones = EstadoBus.Apagado, BusServicio = EstadoBus.Apagado;
        public bool GeneracionReducida;
        public float SoporteS;
        public float Integridad = 100f;
        public int KitsBodega;
        public bool KitEnMano;
        public int KitsUsados;
        public readonly List<DanoModulo> Danos = new List<DanoModulo>();

        // Ciencia y comunicaciones
        public int Capturas, CapturasTransmitidas;
        public float DatosMB, TransmitidoMB;
        public int capturasPendientes;              // capturas a bordo sin transmitir
        public float CapturaProgresoS = -1f, CapturaDuracionS;
        public string CapturaAbortada = "";
        public bool DiagnosticoHecho, SateliteReparado, SondaDesplegada, InformePendiente, InformeTransmitido;
        public float OrdenProgresoS = -1f;
        public bool Transmitiendo;
        public bool OperacionSolicitada;            // Comunicaciones pidió permiso de salida a la base

        // Misión
        public int Paso;
        public bool PilotajeTomado;
        public ResultadoExpedicion Resultado = ResultadoExpedicion.EnCurso;
        public string MotivoFin = "";
        public bool RegresoAnticipado;
        public int Impactos;
        public bool DanoGuionizadoHecho;
        public float CombustibleInicialKg, EnergiaGastadaWh;
        public readonly List<string> Registro = new List<string>();
        /// <summary>Sucesos nuevos para quien dibuja o hace sonar algo (alarma, impacto…). Se vacía al leerlo.</summary>
        public readonly List<string> Sucesos = new List<string>();
        /// <summary>Solo órbita (fondo de la preparación): no gasta recursos ni avanza la misión.</summary>
        public bool SoloOrbita;

        readonly Dictionary<string, double> ultimoContacto = new Dictionary<string, double>();
        Vector3d gravedadActual;
        bool gravedadLista;
        CuerpoKora cBase, cKora, cK2, cTesel, cT7;

        public EstadoExpedicion(ResumenExpedicion config, CatalogoMision cat)
        {
            Config = config;
            Mision = config.mision;
            Sistema = SistemaKora.Crear();
            minutosPorSegundo = cat != null ? Mathf.Max(0.01f, cat.minutosMisionPorSegundo) : 1f;
            consumoTransmisionW = (cat != null ? cat.consumoTransmisionW : 25f) * BalanceEM4X.FactorConsumoCatalogo;

            DatosCatalogoVuelo datos = new DatosCatalogoVuelo
            {
                masaSecaKg = config.masaSecaKg,
                combustibleKg = config.combustibleKg,
                empujePrincipalN = config.motor.empujeN,
                consumoPrincipalKgS = config.motor.consumoKgS,
                origen = "balance provisional EM-4X (DEMOSTRACIÓN)"
            };
            Vuelo = new EstadoVuelo3D(datos, BalanceEM4X.Maniobra);
            Frenado = new AsistenteFrenado(AsistenteFrenado.Ajustes.PorDefecto);

            cBase = Sistema.Cuerpo("base"); cKora = Sistema.Cuerpo("kora"); cK2 = Sistema.Cuerpo("k2");
            cTesel = Sistema.Cuerpo("tesel"); cT7 = Sistema.Cuerpo("t7");
            foreach (CuerpoKora c in Sistema.cuerpos) if (c.navegable) Objetivos.Add(c);
            Seleccionar(Mision.objetivoInicial);

            // La nave empieza en la misma órbita que la base, 250 m por delante y con su misma velocidad orbital.
            CuerpoKora salida = new CuerpoKora { radioOrbitaM = SistemaKora.RadioOrbitaBaseM, faseInicialRad = 250.0 / SistemaKora.RadioOrbitaBaseM };
            Sistema.Estado(salida, 0.0, out Vector3d p0, out Vector3d v0);
            Vuelo.Posicion = p0;
            Vuelo.Velocidad = v0;
            // Proa hacia el sentido de la marcha, «arriba» de la nave perpendicular al plano orbital.
            Vuelo.Orientacion = Quaternion.LookRotation(v0.AVector3().normalized, Vector3.up);

            EnergiaWh = BalanceEM4X.BateriaWh;
            SoporteS = config.soporteVitalS;
            KitsBodega = config.kits;
            CombustibleInicialKg = config.combustibleKg;
            Anotar("Tripulación a bordo. " + Mision.nombre + ".");
        }

        // ------------------------------------------------------------------ Consultas

        public CuerpoKora Objetivo => Objetivos[Mathf.Clamp(ObjetivoSel, 0, Objetivos.Count - 1)];
        public OpcionInstrumento Instrumento => Config.instrumento;
        public float DuracionCapturaS => Config.instrumento.segundosEscaneo * 3f;
        public float ValorCaptura => Config.instrumento.valorCientifico * (Config.instrumentoRecomendado ? 2 : 1);
        public bool HayBrechaAbierta { get { foreach (DanoModulo d in Danos) if (!d.reparado) return true; return false; } }
        public float SoporteFraccion => Config.soporteVitalS > 0f ? SoporteS / Config.soporteVitalS : 0f;
        public string PasoTexto => Paso < Mision.pasos.Count ? Mision.pasos[Paso] : "Misión completada.";
        public float AltitudKoraM => (float)(Vuelo.Posicion.Magnitud - SistemaKora.RadioPlanetaM);
        public float RadioKoraM => (float)Vuelo.Posicion.Magnitud;

        public void Seleccionar(string id)
        {
            for (int i = 0; i < Objetivos.Count; i++) if (Objetivos[i].id == id) { ObjetivoSel = i; return; }
        }

        public void SiguienteObjetivo()
        {
            ObjetivoSel = (ObjetivoSel + 1) % Objetivos.Count;
            Frenado.Cancelar("Frenado asistido cancelado: ha cambiado la referencia.");
        }

        /// <summary>Distancia (m) al centro de un cuerpo y velocidad relativa (m/s; «acercamiento» positivo si se acerca).</summary>
        public void Relativo(CuerpoKora c, out float distanciaM, out float velocidadMS, out float acercamientoMS)
        {
            Sistema.Estado(c, Tiempo, out Vector3d p, out Vector3d v);
            Vector3d dp = p - Vuelo.Posicion, dv = Vuelo.Velocidad - v;
            distanciaM = (float)dp.Magnitud;
            velocidadMS = (float)dv.Magnitud;
            acercamientoMS = distanciaM > 0.01f ? (float)(Vd.Punto(dv, dp) / dp.Magnitud) : 0f;
        }

        public float Distancia(CuerpoKora c) { Relativo(c, out float d, out float v, out float a); return d; }
        public float VelocidadRelativa(CuerpoKora c) { Relativo(c, out float d, out float v, out float a); return v; }

        /// <summary>Dirección (en el mundo) desde la nave hacia un cuerpo.</summary>
        public Vector3 DireccionA(CuerpoKora c) => (Sistema.Posicion(c, Tiempo) - Vuelo.Posicion).AVector3().normalized;
        public float DesvioProaGrados(CuerpoKora c) => Vector3.Angle(Vuelo.Proa, DireccionA(c));

        public bool EnBase(out string falta)
        {
            Relativo(cBase, out float d, out float v, out float a);
            falta = "";
            if (d > 400f) falta = $"estás a {d:0} m de la base (máximo 400 m)";
            else if (v > 2f) falta = $"vas a {v:0.0} m/s respecto a la base (máximo 2 m/s)";
            return falta.Length == 0;
        }

        /// <summary>Enlace con la base: con el relevo instalado hace falta línea de visión (el planeta la puede tapar).</summary>
        public bool EnlaceDisponible(out string motivo)
        {
            motivo = "";
            if (BusComunicaciones != EstadoBus.Alimentado) { motivo = "el bus de comunicaciones no está alimentado (Ingeniería)"; return false; }
            if (Config.relevo.fraccionContacto >= 0.999f) return true;      // relevo en órbita alta: contacto continuo
            if (SistemaKora.PlanetaTapa(Vuelo.Posicion, Sistema.Posicion(cBase, Tiempo)))
            {
                motivo = "Kora tapa la línea de visión con la base (con el relevo en órbita alta no pasaría)";
                return false;
            }
            return true;
        }

        public void Anotar(string texto)
        {
            int min = (int)(Tiempo / 60.0), seg = (int)(Tiempo % 60.0);
            Registro.Add($"[{min:00}:{seg:00}] {texto}");
        }

        // ------------------------------------------------------------------ Condiciones de las tareas de Ciencia

        /// <summary>¿Se puede capturar ahora? Devuelve qué falta. Depende de la misión y del paso.</summary>
        public bool CondicionesCaptura(out string falta)
        {
            falta = "";
            if (BusCiencia != EstadoBus.Alimentado) { falta = "el bus de ciencia no está alimentado (Ingeniería)"; return false; }
            float giro = Vuelo.VelocidadAngularGradS.magnitude;
            if (Mision.id == 0)
            {
                float r = RadioKoraM;
                if (r < SistemaKora.BandaObservacionMinM || r > SistemaKora.BandaObservacionMaxM)
                    falta = $"fuera de la banda de observación (estás a {r / 1000f:0.0} km del centro; banda 24–28 km)";
                else if (DesvioProaGrados(cKora) > 15f) falta = $"la proa se desvía {DesvioProaGrados(cKora):0}° de Kora (máximo 15°)";
                else if (giro > 1f) falta = $"la nave gira a {giro:0.0}°/s (máximo 1°/s)";
            }
            else if (Mision.id == 1)
            {
                Relativo(cK2, out float d, out float v, out float a);
                if (DiagnosticoHecho) falta = "el diagnóstico ya está hecho";
                else if (d > 300f) falta = $"el satélite está a {d:0} m (máximo 300 m)";
                else if (v > 2f) falta = $"velocidad relativa {v:0.0} m/s (máximo 2 m/s)";
            }
            else
            {
                Relativo(cTesel, out float d, out float v, out float a);
                if (Capturas >= Mision.capturasRequeridas) falta = "los escaneos del cúmulo ya están hechos";
                else if (d < 3000f || d > 6000f) falta = $"estás a {d / 1000f:0.0} km del centro del cúmulo (punto de observación: 3–6 km)";
                else if (v > 5f) falta = $"velocidad relativa al cúmulo {v:0.0} m/s (máximo 5 m/s)";
                else if (DesvioProaGrados(cTesel) > 15f) falta = $"la proa se desvía {DesvioProaGrados(cTesel):0}° del cúmulo (máximo 15°)";
                else if (giro > 1f) falta = $"la nave gira a {giro:0.0}°/s (máximo 1°/s)";
            }
            if (falta.Length == 0 && DatosMB + Config.instrumento.datosPorEscaneoMB > Config.memoriaMB)
                falta = $"memoria llena ({DatosMB:0} de {Config.memoriaMB:0} MB): transmite antes desde Comunicaciones";
            return falta.Length == 0;
        }

        public bool IniciarCaptura(out string mensaje)
        {
            if (CapturaProgresoS >= 0f) { mensaje = "Ya hay una captura en curso."; return false; }
            if (!CondicionesCaptura(out string falta)) { mensaje = "No se puede capturar: " + falta + "."; return false; }
            CapturaProgresoS = 0f;
            CapturaDuracionS = DuracionCapturaS;
            CapturaAbortada = "";
            mensaje = $"Captura iniciada con {Config.instrumento.nombre}: mantén las condiciones {CapturaDuracionS:0} s.";
            return true;
        }

        public bool CondicionesSonda(out string falta)
        {
            falta = "";
            if (Mision.id != 2) { falta = "esta misión no despliega sonda"; return false; }
            if (!Config.sonda) falta = "no hay sonda a bordo";
            else if (SondaDesplegada) falta = "la sonda ya está desplegada";
            else if (Capturas < Mision.capturasRequeridas) falta = "primero hay que escanear el cúmulo";
            else
            {
                Relativo(cT7, out float d, out float v, out float a);
                float superficie = d - cT7.radioM;
                if (superficie > 400f) falta = $"T-7 está a {superficie:0} m (máximo 400 m de su superficie)";
                else if (v > 3f) falta = $"velocidad relativa a T-7 {v:0.0} m/s (máximo 3 m/s)";
                else if (BusCiencia != EstadoBus.Alimentado) falta = "el bus de ciencia no está alimentado (Ingeniería)";
            }
            return falta.Length == 0;
        }

        public bool DesplegarSonda(out string mensaje)
        {
            if (!CondicionesSonda(out string falta)) { mensaje = "No se puede desplegar: " + falta + "."; return false; }
            SondaDesplegada = true;
            DatosMB += 80f;
            capturasPendientes++;
            mensaje = "Sonda desplegada sobre T-7. Datos de la sonda a bordo (80 MB): transmítelos desde Comunicaciones.";
            Anotar("Sonda desplegada sobre el asteroide T-7.");
            return true;
        }

        // ------------------------------------------------------------------ Comunicaciones

        public bool AlternarTransmision(out string mensaje)
        {
            if (Transmitiendo) { Transmitiendo = false; mensaje = "Transmisión detenida."; return true; }
            if (DatosMB <= 0.01f && !InformePendiente) { mensaje = "No hay datos a bordo que transmitir."; return false; }
            if (!EnlaceDisponible(out string motivo)) { mensaje = "Sin enlace: " + motivo + "."; return false; }
            Transmitiendo = true;
            mensaje = $"Transmitiendo a {Config.relevo.velocidadEnlaceMBs:0} MB/s por «{Config.relevo.nombre}».";
            return true;
        }

        public bool CondicionesOrden(out string falta)
        {
            falta = "";
            if (Mision.id != 1) { falta = "esta misión no tiene servicio a un satélite"; return false; }
            Relativo(cK2, out float d, out float v, out float a);
            if (SateliteReparado) falta = "el satélite ya está reiniciado";
            else if (!DiagnosticoHecho) falta = "falta el diagnóstico de Ciencia";
            else if (BusServicio != EstadoBus.Alimentado) falta = "el bus de servicio no está alimentado (Ingeniería)";
            else if (BusComunicaciones != EstadoBus.Alimentado) falta = "el bus de comunicaciones no está alimentado (Ingeniería)";
            else if (d > 300f) falta = $"el satélite está a {d:0} m (máximo 300 m)";
            return falta.Length == 0;
        }

        public bool EnviarOrden(out string mensaje)
        {
            if (OrdenProgresoS >= 0f) { mensaje = "La orden ya se está enviando."; return false; }
            if (!CondicionesOrden(out string falta)) { mensaje = "No se puede enviar la orden: " + falta + "."; return false; }
            OrdenProgresoS = 0f;
            mensaje = "Enviando la orden de reinicio al satélite K-2 (6 s). Mantén la distancia.";
            return true;
        }

        // ------------------------------------------------------------------ Ingeniería

        public bool AlternarBus(ref EstadoBus bus, string nombre, out string mensaje)
        {
            if (bus == EstadoBus.Protegido) { mensaje = nombre + " está PROTEGIDO por una avería: usa «Restablecer» cuando el módulo esté reparado."; return false; }
            if (bus == EstadoBus.Alimentado) { bus = EstadoBus.Apagado; mensaje = nombre + " apagado: deja de consumir."; return true; }
            if (EnergiaWh <= BalanceEM4X.BateriaWh * 0.05f) { mensaje = "Energía insuficiente para alimentar " + nombre + "."; return false; }
            bus = EstadoBus.Alimentado;
            mensaje = nombre + " alimentado.";
            return true;
        }

        /// <summary>Restablece los buses protegidos cuyos módulos ya están reparados.</summary>
        public bool Restablecer(out string mensaje)
        {
            bool cienciaDanada = false, comDanada = false, sistemasDanado = false;
            foreach (DanoModulo d in Danos)
            {
                if (d.reparado || !d.grave) continue;
                if (d.modulo == "CI" || d.modulo == "IN") cienciaDanada = true;
                if (d.modulo == "CO" || d.modulo == "IN") comDanada = true;
                if (d.modulo == "SI") sistemasDanado = true;
            }
            int hechos = 0;
            if (BusCiencia == EstadoBus.Protegido && !cienciaDanada) { BusCiencia = EstadoBus.Apagado; hechos++; }
            if (BusComunicaciones == EstadoBus.Protegido && !comDanada) { BusComunicaciones = EstadoBus.Apagado; hechos++; }
            if (GeneracionReducida && !sistemasDanado) { GeneracionReducida = false; hechos++; }
            if (hechos > 0) { mensaje = $"Restablecidos {hechos} sistema(s). Los buses quedan apagados: aliméntalos de nuevo si los necesitas."; Anotar("Ingeniería restablece sistemas protegidos."); return true; }
            mensaje = cienciaDanada || comDanada || sistemasDanado
                ? "No se puede restablecer: el módulo dañado sigue sin reparar."
                : "No hay sistemas protegidos que restablecer.";
            return false;
        }

        public float ConsumoW
        {
            get
            {
                float w = BalanceEM4X.ConsumoBaseW;
                if (BusCiencia == EstadoBus.Alimentado) w += BalanceEM4X.ConsumoBusCienciaW;
                if (BusComunicaciones == EstadoBus.Alimentado) w += BalanceEM4X.ConsumoBusComunicacionesW;
                if (BusServicio == EstadoBus.Alimentado) w += BalanceEM4X.ConsumoBusServicioW;
                if (CapturaProgresoS >= 0f) w += Config.instrumento.consumoEscaneoW * BalanceEM4X.FactorConsumoCatalogo;
                if (Transmitiendo || OrdenProgresoS >= 0f) w += consumoTransmisionW;
                return w;
            }
        }

        public float GeneracionW => BalanceEM4X.GeneracionW * (GeneracionReducida ? 0.5f : 1f);

        // ------------------------------------------------------------------ Kits y reparación

        public bool RecogerKit(out string mensaje)
        {
            if (KitEnMano) { mensaje = "Ya llevas un kit de reparación."; return false; }
            if (KitsBodega <= 0) { mensaje = "No quedan kits de reparación en la bodega."; return false; }
            KitsBodega--;
            KitEnMano = true;
            mensaje = $"Kit de reparación recogido. Quedan {KitsBodega} en la bodega.";
            return true;
        }

        public DanoModulo DanoDe(string modulo)
        {
            foreach (DanoModulo d in Danos) if (d.modulo == modulo && !d.reparado) return d;
            return null;
        }

        /// <summary>Trabajo de reparación mientras se mantiene la tecla en el punto de reparación del módulo.</summary>
        public bool Reparar(string modulo, float dt, out string mensaje)
        {
            DanoModulo d = DanoDe(modulo);
            if (d == null) { mensaje = "Este módulo no tiene daños que reparar."; return false; }
            if (!KitEnMano) { mensaje = "Necesitas un kit de reparación: recógelo en la Bodega."; return false; }
            d.trabajoS += dt;
            if (d.trabajoS < BalanceEM4X.SegundosReparacion)
            {
                mensaje = $"Reparando la brecha… {d.trabajoS / BalanceEM4X.SegundosReparacion * 100f:0} %";
                return true;
            }
            d.reparado = true;
            KitEnMano = false;
            KitsUsados++;
            Integridad = Mathf.Min(100f, Integridad + BalanceEM4X.IntegridadRecuperadaKit);
            mensaje = "Brecha sellada en " + d.nombreModulo + ". Se ha gastado el kit. El aire perdido no se recupera." +
                      (d.grave ? " Hay sistemas protegidos: restablécelos en Ingeniería." : "");
            Anotar("Brecha reparada en " + d.nombreModulo + ".");
            Sucesos.Add("reparado");
            return true;
        }

        // ------------------------------------------------------------------ Daño

        static readonly string[] ModulosDanables = { "MA", "IN", "CI", "CO", "HA", "SI" };
        static readonly string[] NombresDanables = { "Mando", "Ingeniería", "Ciencia", "Comunicaciones", "Hábitat", "Sistemas" };
        static readonly Vector3[] DireccionesDanables =
        {
            new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -0.7f), new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f),
            new Vector3(0f, 1f, 0.2f), new Vector3(0f, 0.6f, -1f)
        };

        /// <summary>Registra un impacto: severidad por velocidad relativa y módulo por dirección (ejes de la nave).</summary>
        public void RegistrarImpacto(float velocidadMS, Vector3 direccionNave, string causa, bool guionizado)
        {
            if (velocidadMS < BalanceEM4X.VelocidadRoceMS)
            {
                Anotar($"Roce con {causa} a {velocidadMS:0.0} m/s: sin daños.");
                Sucesos.Add("roce");
                return;
            }
            Impactos++;
            if (velocidadMS >= BalanceEM4X.VelocidadCatastroficaMS)
            {
                Integridad = 0f;
                Terminar(ResultadoExpedicion.Fracaso, $"Impacto catastrófico con {causa} a {velocidadMS:0} m/s (límite {BalanceEM4X.VelocidadCatastroficaMS:0} m/s): la nave se pierde.");
                Sucesos.Add("catastrofe");
                return;
            }
            Integridad = Mathf.Max(0f, Integridad - velocidadMS * BalanceEM4X.DanoPorMS);
            int mejor = 0; float mejorPunto = -2f;
            Vector3 dir = direccionNave.sqrMagnitude > 1e-6f ? direccionNave.normalized : Vector3.forward;
            for (int i = 0; i < ModulosDanables.Length; i++)
            {
                float punto = Vector3.Dot(dir, DireccionesDanables[i].normalized);
                if (punto > mejorPunto) { mejorPunto = punto; mejor = i; }
            }
            string nombre = NombresDanables[mejor];
            if (Integridad <= 0f)
            {
                Terminar(ResultadoExpedicion.Fracaso, $"El casco no resiste otro impacto ({causa}, {velocidadMS:0} m/s): integridad agotada.");
                Sucesos.Add("catastrofe");
                return;
            }
            if (velocidadMS < BalanceEM4X.VelocidadBrechaMS)
            {
                Anotar($"Impacto leve con {causa} a {velocidadMS:0.0} m/s en {nombre}: abolladura, sin brecha. Integridad {Integridad:0} %.");
                Sucesos.Add("impacto");
                return;
            }
            DanoModulo previo = DanoDe(ModulosDanables[mejor]);
            bool grave = velocidadMS >= BalanceEM4X.VelocidadGraveMS;
            if (previo != null)
            {
                previo.grave = previo.grave || grave;
                previo.trabajoS = 0f;
            }
            else
                Danos.Add(new DanoModulo { modulo = ModulosDanables[mejor], nombreModulo = nombre, causa = causa, velocidadMS = velocidadMS,
                                           grave = grave, guionizado = guionizado });
            if (grave)
            {
                string m = ModulosDanables[mejor];
                if (m == "CI" || m == "IN") { BusCiencia = EstadoBus.Protegido; CancelarCaptura("avería en el bus de ciencia"); }
                if (m == "CO" || m == "IN") { BusComunicaciones = EstadoBus.Protegido; Transmitiendo = false; }
                if (m == "SI") GeneracionReducida = true;
                if (m == "MA") { Vuelo.Estabilizacion = false; Apuntar = 0; Frenado.Cancelar("Frenado asistido cancelado: avería en Mando."); }
            }
            Anotar((guionizado ? "EVENTO GUIONIZADO DE DEMOSTRACIÓN: " : "") +
                   $"{(grave ? "BRECHA GRAVE" : "Brecha")} en {nombre} por {causa} a {velocidadMS:0.0} m/s. Integridad {Integridad:0} %.");
            Sucesos.Add("alarma");
        }

        void CancelarCaptura(string motivo)
        {
            if (CapturaProgresoS < 0f) return;
            CapturaProgresoS = -1f;
            CapturaAbortada = "Captura interrumpida: " + motivo + ".";
        }

        void Terminar(ResultadoExpedicion r, string motivo)
        {
            if (Resultado != ResultadoExpedicion.EnCurso) return;
            Resultado = r;
            MotivoFin = motivo;
            Anotar(motivo);
        }

        /// <summary>Confirmación del regreso desde Pilotaje (en la zona de la base).</summary>
        public bool ConfirmarRegreso(out string mensaje)
        {
            if (Paso == 0 && !RegresoAnticipado) { mensaje = "Aún no has salido de la base: primero toma Pilotaje y aléjate más de 600 m."; return false; }
            if (!EnBase(out string falta)) { mensaje = "Aún no se puede confirmar el regreso: " + falta + "."; return false; }
            bool objetivos = Paso >= Mision.pasos.Count - 1;
            if (objetivos && !HayBrechaAbierta)
                Terminar(ResultadoExpedicion.Exito, "Regreso a la base con todos los objetivos cumplidos.");
            else if (objetivos)
                Terminar(ResultadoExpedicion.Parcial, "Regreso a la base con los objetivos cumplidos, pero con una brecha sin reparar.");
            else
                Terminar(ResultadoExpedicion.Parcial, "Regreso anticipado a la base: quedaron objetivos sin cumplir (paso " + (Paso + 1) + " de " + Mision.pasos.Count + ").");
            if (objetivos) Paso = Mision.pasos.Count;
            mensaje = MotivoFin;
            return true;
        }

        /// <summary>El jugador abandona: la tripulación pide rescate (misión fallida).</summary>
        public void PedirRescate()
        {
            Terminar(ResultadoExpedicion.Fracaso, "La tripulación pidió rescate: la nave no volvió a la base por sus medios.");
        }

        // ------------------------------------------------------------------ Avance

        /// <summary>
        /// Avanza un paso fijo. «manual» son los mandos del piloto (cero si nadie está en Pilotaje). Las ayudas activas
        /// (estabilización, apuntar al objetivo, frenado asistido) siguen funcionando sin piloto y gastan su combustible.
        /// </summary>
        public void Avanzar(EntradaVuelo manual, float dt)
        {
            if (Resultado != ResultadoExpedicion.EnCurso) return;

            // Referencia explícita de velocidad para la asistencia de frenado: el objetivo seleccionado.
            Sistema.Estado(Objetivo, Tiempo, out Vector3d posObj, out Vector3d velObj);
            Vuelo.VelocidadReferencia = velObj;

            EntradaVuelo e = manual;
            bool hayManual = manual.giro != Vector3.zero || manual.traslacion != Vector3.zero || manual.motorPrincipal;
            float combustibleAntes = Vuelo.Combustible;
            bool asistida = false;
            if (Frenado.Activa)
            {
                if (hayManual) Frenado.Cancelar("Frenado asistido CANCELADO por un mando manual.");
                else { e = Frenado.Calcular(Vuelo, dt); asistida = true; }
            }
            else if (Apuntar > 0 && manual.giro == Vector3.zero && Vuelo.Combustible > 0f)
            {
                Vector3 dir = Apuntar == 1 ? (posObj - Vuelo.Posicion).AVector3().normalized
                            : Vuelo.Velocidad.AVector3().normalized * (Apuntar == 2 ? -1f : 1f);
                if (dir.sqrMagnitude > 0.5f) e.giro = GiroHacia(dir);
            }

            // Gravedad con salto de rana: medio impulso, movimiento (EstadoVuelo3D existente), medio impulso.
            if (!gravedadLista) { gravedadActual = Sistema.Gravedad(Vuelo.Posicion, Tiempo); gravedadLista = true; }
            Vuelo.Velocidad = Vuelo.Velocidad + gravedadActual * (dt * 0.5);
            Vuelo.Paso(e, dt);
            Tiempo += dt;
            gravedadActual = Sistema.Gravedad(Vuelo.Posicion, Tiempo);
            Vuelo.Velocidad = Vuelo.Velocidad + gravedadActual * (dt * 0.5);
            if (asistida) Frenado.GastoKg += combustibleAntes - Vuelo.Combustible;

            if (SoloOrbita) return;
            Colisiones();
            if (Resultado != ResultadoExpedicion.EnCurso) return;
            Sistemas(dt);
            Progreso();
        }

        /// <summary>Mando de giro para llevar la proa hacia una dirección del mundo (misma ley que el frenado asistido).</summary>
        Vector3 GiroHacia(Vector3 direccionMundo)
        {
            Vector3 enNave = Quaternion.Inverse(Vuelo.Orientacion) * direccionMundo;
            Quaternion.FromToRotation(Vector3.forward, enNave).ToAngleAxis(out float angulo, out Vector3 eje);
            if (angulo > 180f) { angulo = 360f - angulo; eje = -eje; }
            Vector3 alfa = Vuelo.Maniobra.aceleracionAngularGradS2 * Mathf.Deg2Rad;
            float alfaMin = Mathf.Max(0.001f, Mathf.Min(alfa.x, alfa.y));
            float rad = angulo * Mathf.Deg2Rad;
            float wDeseada = Mathf.Min(Mathf.Sqrt(2f * alfaMin * rad * 0.7f), 15f * Mathf.Deg2Rad);
            Vector3 objetivo = rad > 2e-3f && eje.sqrMagnitude > 1e-6f ? eje.normalized * wDeseada : Vector3.zero;
            Vector3 w = Vuelo.VelocidadAngular;
            Vector3 u = Vector3.zero;
            for (int i = 0; i < 3; i++)
                if (alfa[i] > 0f) u[i] = Mathf.Clamp((objetivo[i] - w[i]) / (alfa[i] * 0.3f), -1f, 1f);
            // Zona muerta: no gasta combustible por correcciones despreciables.
            for (int i = 0; i < 3; i++) if (Mathf.Abs(u[i]) < 0.02f) u[i] = 0f;
            return u;
        }

        void Colisiones()
        {
            foreach (CuerpoKora c in Sistema.cuerpos)
            {
                if (c.tipo == TipoCuerpo.Cumulo) continue;
                Sistema.Estado(c, Tiempo, out Vector3d p, out Vector3d v);
                Vector3d dp = Vuelo.Posicion - p;
                double limite = c.radioM + BalanceEM4X.RadioNaveM;
                // Descarte rápido por ejes antes de calcular la distancia.
                if (Math.Abs(dp.x) > limite || Math.Abs(dp.y) > limite || Math.Abs(dp.z) > limite) continue;
                double dist = dp.Magnitud;
                if (dist >= limite) continue;

                Vector3d n = dist > 1e-6 ? dp * (1.0 / dist) : new Vector3d(0.0, 1.0, 0.0);
                Vector3d vRel = Vuelo.Velocidad - v;
                double vn = Vd.Punto(vRel, n);                 // negativo = acercándose
                // Sacar la nave a la superficie de contacto: un contacto es UN suceso, no uno por fotograma.
                Vuelo.Posicion = p + n * limite;
                if (vn >= 0.0) continue;
                float velocidad = (float)(-vn);
                bool cuerpoMayor = c.tipo == TipoCuerpo.Planeta || c.tipo == TipoCuerpo.Luna;
                // Rebote amortiguado: se anula la componente de acercamiento y se devuelve un 20 %.
                Vuelo.Velocidad = Vuelo.Velocidad + n * (-vn * 1.2);

                double ultimo;
                if (ultimoContacto.TryGetValue(c.id, out ultimo) && Tiempo - ultimo < 2.0) continue;
                ultimoContacto[c.id] = Tiempo;
                if (cuerpoMayor && velocidad >= BalanceEM4X.VelocidadBrechaMS)
                {
                    Integridad = 0f;
                    Impactos++;
                    Terminar(ResultadoExpedicion.Fracaso, $"La nave se estrelló contra {c.nombre} a {velocidad:0} m/s.");
                    Sucesos.Add("catastrofe");
                    return;
                }
                Vector3 direccionNave = Quaternion.Inverse(Vuelo.Orientacion) * (n * -1.0).AVector3();
                RegistrarImpacto(velocidad, direccionNave, c.nombre, false);
                if (Resultado != ResultadoExpedicion.EnCurso) return;
            }
        }

        void Sistemas(float dt)
        {
            // Energía: misma regla que el resto del prototipo (1 s real de vuelo = N minutos de misión).
            float horas = dt * minutosPorSegundo / 60f;
            float consumo = ConsumoW * horas;
            EnergiaGastadaWh += consumo;
            EnergiaWh = Mathf.Clamp(EnergiaWh + GeneracionW * horas - consumo, 0f, BalanceEM4X.BateriaWh);
            if (EnergiaWh <= 0f)
            {
                bool habia = BusCiencia == EstadoBus.Alimentado || BusComunicaciones == EstadoBus.Alimentado || BusServicio == EstadoBus.Alimentado;
                if (BusCiencia == EstadoBus.Alimentado) BusCiencia = EstadoBus.Apagado;
                if (BusComunicaciones == EstadoBus.Alimentado) BusComunicaciones = EstadoBus.Apagado;
                if (BusServicio == EstadoBus.Alimentado) BusServicio = EstadoBus.Apagado;
                Transmitiendo = false;
                CancelarCaptura("batería agotada");
                OrdenProgresoS = -1f;
                if (habia) { Anotar("Batería agotada: los buses de ciencia, comunicaciones y servicio se han apagado."); Sucesos.Add("alarma"); }
            }

            // Soporte vital: gasto normal + fugas (menores si el módulo dañado está aislado).
            float gasto = 1f;
            foreach (DanoModulo d in Danos)
            {
                if (d.reparado) continue;
                float fuga = d.grave ? BalanceEM4X.FugaGrave : BalanceEM4X.FugaLeve;
                gasto += d.aislado ? fuga * BalanceEM4X.FugaAislada : fuga;
            }
            float antes = SoporteFraccion;
            SoporteS = Mathf.Max(0f, SoporteS - gasto * dt);
            if (antes > BalanceEM4X.SoporteCriticoFraccion && SoporteFraccion <= BalanceEM4X.SoporteCriticoFraccion)
            {
                RegresoAnticipado = true;
                Anotar("SOPORTE VITAL CRÍTICO: hay que volver a la base ya (regreso anticipado).");
                Sucesos.Add("alarma");
            }
            if (SoporteS <= 0f)
            {
                Terminar(ResultadoExpedicion.Fracaso, "Soporte vital agotado antes de volver a la base.");
                Sucesos.Add("catastrofe");
                return;
            }

            // Captura en curso: las condiciones deben mantenerse hasta el final.
            if (CapturaProgresoS >= 0f)
            {
                if (!CondicionesCaptura(out string falta)) CancelarCaptura(falta);
                else
                {
                    CapturaProgresoS += dt;
                    if (CapturaProgresoS >= CapturaDuracionS)
                    {
                        CapturaProgresoS = -1f;
                        Capturas++;
                        capturasPendientes++;
                        DatosMB += Config.instrumento.datosPorEscaneoMB;
                        if (Mision.id == 1) { DiagnosticoHecho = true; InformePendiente = true; }
                        Anotar($"Captura {Capturas} completada con {Config.instrumento.nombre} ({Config.instrumento.datosPorEscaneoMB:0} MB).");
                        Sucesos.Add("captura");
                    }
                }
            }

            // Orden de servicio al satélite.
            if (OrdenProgresoS >= 0f)
            {
                if (!CondicionesOrden(out string faltaOrden)) OrdenProgresoS = -1f;
                else
                {
                    OrdenProgresoS += dt;
                    if (OrdenProgresoS >= 6f)
                    {
                        OrdenProgresoS = -1f;
                        SateliteReparado = true;
                        Anotar("Satélite K-2 reiniciado: vuelve a estar operativo.");
                        Sucesos.Add("captura");
                    }
                }
            }

            // Transmisión a la base.
            if (Transmitiendo)
            {
                if (!EnlaceDisponible(out string motivo))
                {
                    Transmitiendo = false;
                    Anotar("Transmisión interrumpida: " + motivo + ".");
                }
                else
                {
                    float enviado = Mathf.Min(DatosMB, Config.relevo.velocidadEnlaceMBs * dt);
                    DatosMB -= enviado;
                    TransmitidoMB += enviado;
                    if (DatosMB <= 0.01f)
                    {
                        DatosMB = 0f;
                        Transmitiendo = false;
                        CapturasTransmitidas += capturasPendientes;
                        capturasPendientes = 0;
                        if (InformePendiente) { InformePendiente = false; InformeTransmitido = true; }
                        Anotar($"Transmisión completada: {TransmitidoMB:0} MB recibidos en la base.");
                        Sucesos.Add("captura");
                    }
                }
            }
        }

        void Progreso()
        {
            if (Paso >= Mision.pasos.Count) return;
            float dBase = Distancia(cBase);

            // Evento guionizado de la misión 3: al entrar por primera vez en el cúmulo.
            if (Mision.danoGuionizado && !DanoGuionizadoHecho && Distancia(cTesel) < 2500f)
            {
                DanoGuionizadoHecho = true;
                RegistrarImpacto(5f, new Vector3(0f, 1f, 0.2f), "un fragmento del cúmulo", true);
            }

            bool hecho = false;
            int ultimo = Mision.pasos.Count - 1;
            if (Paso == 0) hecho = PilotajeTomado && dBase > 600f;
            else if (Paso == ultimo) hecho = false;       // el regreso se confirma desde Pilotaje
            else if (Mision.id == 0)
            {
                if (Paso == 1) hecho = RadioKoraM >= SistemaKora.BandaObservacionMinM && RadioKoraM <= SistemaKora.BandaObservacionMaxM;
                else if (Paso == 2) hecho = Capturas >= Mision.capturasRequeridas;
                else if (Paso == 3) hecho = CapturasTransmitidas >= Mision.capturasRequeridas;
            }
            else if (Mision.id == 1)
            {
                if (Paso == 1) { Relativo(cK2, out float d, out float v, out float a); hecho = d < 300f && v < 2f; }
                else if (Paso == 2) hecho = DiagnosticoHecho;
                else if (Paso == 3) hecho = BusServicio == EstadoBus.Alimentado;
                else if (Paso == 4) hecho = SateliteReparado;
                else if (Paso == 5) hecho = InformeTransmitido;
            }
            else
            {
                if (Paso == 1) { Relativo(cTesel, out float d, out float v, out float a); hecho = d >= 3000f && d <= 6000f && v < 5f; }
                else if (Paso == 2) hecho = Capturas >= Mision.capturasRequeridas;
                else if (Paso == 3) { Relativo(cT7, out float d, out float v, out float a); hecho = d - cT7.radioM < 400f && v < 3f; }
                else if (Paso == 4) hecho = SondaDesplegada;
                else if (Paso == 5) hecho = DatosMB <= 0.01f && CapturasTransmitidas >= Mision.capturasRequeridas + 1;
            }
            if (hecho)
            {
                Anotar("Paso cumplido: " + Mision.pasos[Paso]);
                Paso++;
                Sucesos.Add("paso");
            }
        }
    }
}
