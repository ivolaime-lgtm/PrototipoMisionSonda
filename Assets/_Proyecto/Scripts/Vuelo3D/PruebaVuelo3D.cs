using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace PrototipoMision
{
    /// <summary>
    /// Prueba V1 de vuelo 3D SIN gravedad (escena VueloPrueba). Lee los mandos, avanza la lógica y coordina la vista y el HUD.
    ///   · Lógica: EstadoVuelo3D (nave) y ObjetivoBalizas (objetivo).
    ///   · Dibujo: VistaVuelo3D (mundo, nave, cámaras) y HudVuelo3D (indicadores). Se crean en tiempo de ejecución.
    /// No usa ni modifica la escena Nave ni su lógica. Valores de DEMOSTRACIÓN.
    /// </summary>
    public class PruebaVuelo3D : MonoBehaviour
    {
        [Header("Catálogo (masa, combustible y motor principal)")]
        [Tooltip("Si faltan, se usan los valores estándar del catálogo actual copiados en DatosCatalogoVuelo.Estandar.")]
        [SerializeField] CatalogoMision catalogoMision;
        [SerializeField] CatalogoNave catalogoNave;

        [Header("Propulsores de maniobra — PROVISIONALES")]
        [SerializeField] AjustesManiobra maniobra = AjustesManiobra.PorDefecto;

        [Header("Asistencia de frenado (opcional, tecla B) — PROVISIONAL")]
        [SerializeField] AsistenteFrenado.Ajustes frenado = AsistenteFrenado.Ajustes.PorDefecto;

        [Header("Objetivo")]
        [SerializeField] ObjetivoBalizas.Ajustes objetivo = ObjetivoBalizas.Ajustes.PorDefecto;
        [SerializeField] List<DefinicionBaliza> balizas = new List<DefinicionBaliza>
        {
            new DefinicionBaliza { nombre = "Baliza 1", posicionM = new Vector3(400f, 150f, 1500f), direccion = "delante, arriba a la derecha", color = new Color(0.35f, 0.85f, 1f) },
            new DefinicionBaliza { nombre = "Baliza 2", posicionM = new Vector3(-1200f, -300f, 600f), direccion = "a la izquierda y abajo", color = new Color(1f, 0.7f, 0.25f) },
            new DefinicionBaliza { nombre = "Baliza 3", posicionM = new Vector3(300f, 900f, -1100f), direccion = "detrás y arriba", color = new Color(0.75f, 0.55f, 1f) }
        };

        [Header("Referencias fijas, SIN gravedad (solo para orientarse)")]
        [SerializeField] List<CuerpoReferencia> cuerpos = new List<CuerpoReferencia>
        {
            new CuerpoReferencia { nombre = "Planeta", posicionM = new Vector3(-30000f, -12000f, 70000f), radioM = 10000f, color = new Color(0.35f, 0.55f, 0.8f) },
            new CuerpoReferencia { nombre = "Satélite", posicionM = new Vector3(25000f, 15000f, 45000f), radioM = 2500f, color = new Color(0.75f, 0.73f, 0.7f) }
        };
        [Tooltip("Distancia a la superficie de un cuerpo que termina la prueba como colisión (m).")]
        [SerializeField] float margenColisionM = 300f;
        [Tooltip("Radio de la zona de prueba (m). Salir solo da un aviso.")]
        [SerializeField] float radioZonaPruebaM = 20000f;

        [Header("Solo para pruebas")]
        [Tooltip("SOLO PRUEBAS. Activado: la nave empieza con «Combustible Prueba Kg». Desactivado por defecto.")]
        [SerializeField] bool combustiblePruebaActivo;
        [SerializeField] float combustiblePruebaKg = 20f;

        public enum Resultado { EnCurso, Completada, Colision }

        public EstadoVuelo3D Estado { get; private set; }
        public ObjetivoBalizas Objetivo { get; private set; }
        public AsistenteFrenado Asistente { get; private set; }
        public IReadOnlyList<DefinicionBaliza> Balizas => balizas;
        public IReadOnlyList<CuerpoReferencia> Cuerpos => cuerpos;
        public Resultado ResultadoActual { get; private set; }
        public string MotivoFin { get; private set; } = "";
        public bool VistaExterior { get; private set; }
        public bool AyudaVisible { get; private set; } = true;
        public string Aviso { get; private set; } = "";
        public float RadioZonaPruebaM => radioZonaPruebaM;
        public VistaVuelo3D Vista { get; private set; }

        float avisoHasta;
        bool avisoSinCombustible, avisoFueraZona;

        void Start()
        {
            DatosCatalogoVuelo datos = LeerCatalogo();
            if (combustiblePruebaActivo)
            {
                datos.combustibleKg = Mathf.Max(0f, combustiblePruebaKg);
                datos.origen += " · COMBUSTIBLE DE PRUEBA";
            }
            Estado = new EstadoVuelo3D(datos, maniobra);
            Objetivo = new ObjetivoBalizas(objetivo, balizas);
            Asistente = new AsistenteFrenado(frenado);

            Vista = gameObject.AddComponent<VistaVuelo3D>();
            Vista.Construir(this);
            gameObject.AddComponent<HudVuelo3D>();

            Reiniciar();
            Debug.Log($"[VueloPrueba] Datos de la nave: {datos.origen}. Masa seca {datos.masaSecaKg:0} kg, combustible {datos.combustibleKg:0.0} kg, " +
                      $"motor principal {datos.empujePrincipalN:0} N y {datos.consumoPrincipalKgS:0.00} kg/s (catálogo). " +
                      $"Maniobra PROVISIONAL: traslación {maniobra.empujeTraslacionN:0} N y {maniobra.consumoTraslacionKgS:0.00} kg/s por eje; " +
                      $"giro {maniobra.aceleracionAngularGradS2.x:0}/{maniobra.aceleracionAngularGradS2.y:0}/{maniobra.aceleracionAngularGradS2.z:0} °/s², " +
                      $"{maniobra.consumoOrientacionKgS:0.00} kg/s por eje.", this);
        }

        DatosCatalogoVuelo LeerCatalogo()
        {
            if (catalogoMision == null || catalogoNave == null) return DatosCatalogoVuelo.Estandar;
            ResumenMision mision = catalogoMision.Calcular(MisionActual.Eleccion);
            ResumenNave nave = catalogoNave.Calcular(mision);
            return new DatosCatalogoVuelo
            {
                masaSecaKg = nave.masaInicialKg - nave.combustibleInicialKg,
                combustibleKg = nave.combustibleInicialKg,
                empujePrincipalN = nave.empujeN,
                consumoPrincipalKgS = nave.consumoKgS,
                origen = MisionActual.Configurada ? "catálogo, configuración elegida" : "catálogo, primeras opciones (escena abierta sin configurar)"
            };
        }

        /// <summary>Vuelve al punto de salida: nave quieta, sin giro, combustible inicial y objetivo desde el principio.</summary>
        public void Reiniciar()
        {
            Estado.Reiniciar();
            Objetivo.Reiniciar();
            Asistente.Cancelar("");
            ResultadoActual = Resultado.EnCurso;
            MotivoFin = "";
            avisoSinCombustible = avisoFueraZona = false;
            Avisar("Prueba iniciada. Visita la Baliza 1 y después la Baliza 2, y detente junto a cada una. H: ayuda.", 6f);
        }

        public void Avisar(string texto, float segundos)
        {
            Aviso = texto;
            avisoHasta = Time.time + segundos;
        }

        public bool AvisoVisible => Aviso.Length > 0 && Time.time < avisoHasta;

        static float Eje(KeyControl positivo, KeyControl negativo) =>
            (positivo.isPressed ? 1f : 0f) - (negativo.isPressed ? 1f : 0f);

        void Update()
        {
            if (Estado == null) return;
            Keyboard k = Keyboard.current;
            float dt = Time.deltaTime;
            EntradaVuelo e = default;

            if (k != null)
            {
                if (k.f5Key.wasPressedThisFrame) { Reiniciar(); return; }
                if (k.hKey.wasPressedThisFrame) AyudaVisible = !AyudaVisible;
                if (k.vKey.wasPressedThisFrame)
                {
                    VistaExterior = !VistaExterior;
                    Avisar(VistaExterior ? "Cámara exterior." : "Vista desde el asiento.", 2f);
                }
                if (k.tKey.wasPressedThisFrame)
                {
                    Estado.Estabilizacion = !Estado.Estabilizacion;
                    Avisar(Estado.Estabilizacion
                        ? "Estabilización asistida ACTIVADA: frena el giro de los ejes que no mandas (gasta combustible)."
                        : "Estabilización asistida DESACTIVADA: la nave sigue girando hasta que la frenes tú.", 3f);
                }

                if (k.bKey.wasPressedThisFrame)
                {
                    if (Asistente.Activa)
                    {
                        Asistente.Cancelar("Frenado asistido CANCELADO (B). Vuelves a tener todo el control.");
                        Avisar(Asistente.Motivo, 3f);
                    }
                    else IntentarActivarAsistencia();
                }

                if (ResultadoActual != Resultado.Colision)
                {
                    // Orientación: W/S cabeceo, A/D guiñada, Z/C alabeo. Traslación: I/K, J/L, U/O. Motor: Espacio.
                    e.giro = new Vector3(Eje(k.wKey, k.sKey), Eje(k.dKey, k.aKey), Eje(k.zKey, k.cKey));
                    e.traslacion = new Vector3(Eje(k.lKey, k.jKey), Eje(k.uKey, k.oKey), Eje(k.iKey, k.kKey));
                    e.motorPrincipal = k.spaceKey.isPressed;
                }
            }

            if (ResultadoActual == Resultado.Colision) { Asistente.Cancelar(""); return; }

            // Asistencia de frenado: cualquier mando manual la cancela; si sigue activa, da ella los mandos.
            bool manual = e.giro != Vector3.zero || e.traslacion != Vector3.zero || e.motorPrincipal;
            if (Asistente.Activa && manual)
            {
                Asistente.Cancelar("Frenado asistido CANCELADO por un mando manual.");
                Avisar(Asistente.Motivo, 3f);
            }
            bool asistida = Asistente.Activa;
            if (asistida)
            {
                e = Asistente.Calcular(Estado, dt);
                if (!Asistente.Activa) Avisar(Asistente.Motivo, 5f);
            }

            float combustibleAntes = Estado.Combustible;
            Estado.Paso(e, dt);
            if (asistida) Asistente.GastoKg += combustibleAntes - Estado.Combustible;

            if (Objetivo.Actualizar(Estado.Posicion, Estado.Velocidad, dt))
            {
                if (Objetivo.Completado)
                {
                    ResultadoActual = Resultado.Completada;
                    Avisar($"PRUEBA COMPLETADA en {Objetivo.TiempoFinalS:0} s. Puedes seguir volando o reiniciar con F5.", 8f);
                    Debug.Log($"[VueloPrueba] Completada en {Objetivo.TiempoFinalS:0.0} s. Combustible: motor {Estado.GastoMotorKg:0.0} kg, " +
                              $"traslación {Estado.GastoTraslacionKg:0.0} kg, orientación {Estado.GastoOrientacionKg:0.0} kg, " +
                              $"quedan {Estado.Combustible:0.0} kg.", this);
                }
                else
                    Avisar($"Visita registrada. Siguiente objetivo: {Objetivo.Actual.nombre}.", 4f);
            }

            if (!avisoSinCombustible && Estado.Combustible <= 0f)
            {
                avisoSinCombustible = true;
                Avisar("SIN COMBUSTIBLE: ningún propulsor responde. La nave sigue con su velocidad y su giro actuales. F5 reinicia.", 6f);
            }

            ComprobarColisiones();

            bool fuera = Estado.Posicion.Magnitud > radioZonaPruebaM;
            if (fuera && !avisoFueraZona)
                Avisar($"Fuera de la zona de prueba (más de {radioZonaPruebaM / 1000f:0} km de la salida). F5 reinicia.", 5f);
            avisoFueraZona = fuera;
        }

        void IntentarActivarAsistencia()
        {
            AsistenteFrenado.Ajustes a = Asistente.Configuracion;
            if (ResultadoActual == Resultado.Colision) return;
            if (Estado.Rapidez <= a.velocidadFinalMS)
            {
                Avisar($"La nave ya está prácticamente quieta ({Estado.Rapidez:0.00} m/s): no hace falta frenar.", 3f);
                return;
            }
            if (Estado.Combustible <= 0f)
            {
                Avisar("Sin combustible: la asistencia de frenado no puede empujar.", 4f);
                return;
            }
            float necesario = Estado.Rapidez > a.umbralMotorMS
                ? Mathf.Min(AsistenteFrenado.ConMotor(Estado).combustibleKg, AsistenteFrenado.SoloTraslacion(Estado).combustibleKg)
                : AsistenteFrenado.SoloTraslacion(Estado).combustibleKg;
            Asistente.Activar();
            Avisar("Frenado asistido ACTIVADO: " +
                   (Estado.Rapidez > a.umbralMotorMS ? "girará la proa contra el movimiento y frenará con el motor; " : "frenará con traslación; ") +
                   "B o cualquier mando lo cancela." +
                   (necesario > Estado.Combustible ? $" AVISO: puede no bastar el combustible (≈ {necesario:0} kg necesarios, quedan {Estado.Combustible:0.0})." : ""),
                   5f);
        }

        void ComprobarColisiones()
        {
            foreach (CuerpoReferencia c in cuerpos)
            {
                double d = (Estado.Posicion - Vector3d.De(c.posicionM)).Magnitud;
                if (d < c.radioM + margenColisionM)
                {
                    ResultadoActual = Resultado.Colision;
                    MotivoFin = $"Colisión: la nave ha entrado a menos de {margenColisionM:0} m de la superficie de {c.nombre}.";
                    Avisar(MotivoFin + " F5 reinicia.", 30f);
                    return;
                }
            }
        }
    }
}
