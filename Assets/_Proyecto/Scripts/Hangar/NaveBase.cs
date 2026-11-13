using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>Estado de una medida del bloque provisional (para la planta, el perfil y los colores de la escena).</summary>
    public enum EstadoMedida
    {
        /// <summary>Acordada antes (documento de diseño, consola v0, pila de popa): no se cambia.</summary>
        Conservada,
        /// <summary>Cambio propuesto en este bloque; hay que validarlo antes del acabado.</summary>
        Propuesta,
        /// <summary>Medida que hay que comprobar en Unity con las referencias de tripulantes.</summary>
        PendientePrueba,
        /// <summary>Solo reserva de volumen: el diseño está sin decidir (bodega, unión al lanzador).</summary>
        Reserva
    }

    public enum TipoZona { Pilotaje, Ingenieria, Ciencia, Comunicaciones, Pasillo, Esclusa, ReservaCarga }

    /// <summary>Espacio libre interior de una zona (sin paredes), en el sistema de la nave: x derecha, y arriba, z proa (m).</summary>
    [Serializable]
    public class ZonaInterior
    {
        public string nombre;
        public TipoZona tipo;
        public float xMin, xMax, zMin, zMax;
        public EstadoMedida estado;
        [TextArea(1, 2)] public string nota;
        public float Ancho => xMax - xMin;
        public float Largo => zMax - zMin;
        public Vector3 Centro(float y) => new Vector3((xMin + xMax) / 2f, y, (zMin + zMax) / 2f);
    }

    /// <summary>Hueco en un muro: tramo [desde, hasta] medido a lo largo del muro desde su punto a, y altura del hueco.</summary>
    [Serializable]
    public class HuecoMuro
    {
        public string nombre;
        public float desde, hasta, alto;
        public EstadoMedida estado;
        public float Ancho => hasta - desde;
    }

    /// <summary>Muro interior: línea central de a a b (en x/z), grosor común y huecos de puerta.</summary>
    [Serializable]
    public class MuroInterior
    {
        public string nombre;
        public Vector2 a, b;
        public List<HuecoMuro> huecos = new List<HuecoMuro>();
    }

    /// <summary>Tramo del casco exterior: caja envolvente de una parte de la nave.</summary>
    [Serializable]
    public class TramoCasco
    {
        public string nombre;
        public float semiAncho, zMin, zMax;
        public EstadoMedida estado;
    }

    /// <summary>Puesto de un rol: consola (sustituto provisional salvo la de pilotaje, que sigue la consola v0) y asiento con sujeción.</summary>
    [Serializable]
    public class PuestoBase
    {
        public string rol;
        public Vector3 centroConsola, tamanoConsola;
        public Vector3 asiento;
        [Tooltip("Hacia dónde mira quien se sienta (grados alrededor de y; 0 = proa).")]
        public float orientacionGrados;
        public EstadoMedida estado;
        public Color color = Color.white;
    }

    /// <summary>
    /// Definición de una NAVE BASE (solo datos). La lista de bases admite varias; en esta prueba hay una sola,
    /// «Expedición E-4», PROVISIONAL. Las masas, costes y capacidades NO están aquí: siguen en CatalogoNave
    /// (casco, motores, tanques) y CatalogoMision (carga). Esta clase solo describe geometría y puntos de montaje.
    /// </summary>
    [Serializable]
    public class DefinicionNaveBase
    {
        public string nombre = "Expedición E-4 (provisional)";
        [TextArea(2, 4)] public string descripcion = "";
        public int tripulantes = 4;

        [Header("Alturas (m, suelo interior en y = 0)")]
        public float alturaLibreM = 3.4f;
        public float cascoInferiorY = -1.0f, cascoSuperiorY = 3.9f;
        public float grosorMuroM = 0.2f;
        public EstadoMedida estadoAltura = EstadoMedida.Propuesta;

        public List<TramoCasco> casco = new List<TramoCasco>();
        public List<ZonaInterior> zonas = new List<ZonaInterior>();
        public List<MuroInterior> muros = new List<MuroInterior>();
        public List<PuestoBase> puestos = new List<PuestoBase>();
        [Tooltip("Escotilla exterior de la esclusa, en el costado derecho del casco.")]
        public float escotillaZ = -6.35f, escotillaAncho = 1.4f, escotillaAlto = 2.0f;

        [Header("Pila de popa (medidas del documento de diseño)")]
        [Tooltip("Mamparo de popa y anillo I4 (pivote del casco tripulado).")]
        public float zMamparoI4 = -14.8f;
        public float diametroAnilloM = 4.0f;
        [Tooltip("Reserva de bodega entre I4 e I3: PENDIENTE, no es un diseño.")]
        public float largoBodegaM = 2.5f;
        [Tooltip("Carcasa del módulo de servicio = largo del tanque + este margen (4,2 m con el estándar, 5,4 m con el ampliado).")]
        public float margenCarcasaTanqueM = 1.0f;
        [Tooltip("Altura del eje de empuje en el sistema de la nave (centro de la envolvente). Provisional.")]
        public float ejeEmpujeY = 1.45f;
        public float largoMaximoMotorM = 2.5f, diametroMaximoMotorM = 3.2f;
        [Tooltip("Reserva de volumen detrás del motor para la unión al lanzador: PENDIENTE.")]
        public float largoReservaLanzadorM = 3.0f;

        // ------------------------------------------------------------------ Pila (depende del tanque elegido)

        public float ZAnclajeI3 => zMamparoI4 - largoBodegaM;
        public float LargoModuloServicio(OpcionTanqueNave t) => (t != null ? Mathf.Max(1f, t.largo) : 3.2f) + margenCarcasaTanqueM;
        public float ZAnclajeMotor(OpcionTanqueNave t) => ZAnclajeI3 - LargoModuloServicio(t);
        public float LargoExteriorM => (casco.Count > 0 ? MaxZ() : 0f) - zMamparoI4;

        float MaxZ()
        {
            float m = float.MinValue;
            foreach (TramoCasco c in casco) m = Mathf.Max(m, c.zMax);
            return m;
        }

        public float SemiAnchoMaximo
        {
            get
            {
                float m = 0f;
                foreach (TramoCasco c in casco) m = Mathf.Max(m, c.semiAncho);
                return m;
            }
        }

        // ------------------------------------------------------------------ Base de esta prueba

        static HuecoMuro Puerta(string n, float desde, float hasta, float alto, EstadoMedida e) =>
            new HuecoMuro { nombre = n, desde = desde, hasta = hasta, alto = alto, estado = e };

        static MuroInterior Muro(string n, float ax, float az, float bx, float bz, params HuecoMuro[] huecos)
        {
            MuroInterior m = new MuroInterior { nombre = n, a = new Vector2(ax, az), b = new Vector2(bx, bz) };
            m.huecos.AddRange(huecos);
            return m;
        }

        /// <summary>
        /// Bloque provisional para cuatro tripulantes. Conserva la cabina de pilotaje (consola v0 en z 3,30), Ingeniería,
        /// la envolvente de 20,3 × 11,6 m y la pila de popa del documento de diseño. Cambios propuestos: pasillo de 3,0 m,
        /// altura libre de 3,4 m, puertas de 2,0 m, salas laterales mayores, esclusa y reserva de carga interior.
        /// </summary>
        public static DefinicionNaveBase ExpedicionE4()
        {
            DefinicionNaveBase b = new DefinicionNaveBase
            {
                nombre = "Expedición E-4 (provisional)",
                descripcion = "Única base de esta prueba. Bloque sin acabado para comprobar espacio, accesos y montaje de módulos. " +
                              "Masa estructural: la del casco del catálogo; la ampliación del bloque aún no tiene balance de masa (PROVISIONAL)."
            };

            b.casco.Add(new TramoCasco { nombre = "Proa (pilotaje)", semiAncho = 4.6f, zMin = 1.3f, zMax = 5.5f, estado = EstadoMedida.Conservada });
            b.casco.Add(new TramoCasco { nombre = "Salas laterales", semiAncho = 5.8f, zMin = -4.5f, zMax = 1.3f, estado = EstadoMedida.Conservada });
            b.casco.Add(new TramoCasco { nombre = "Centro (esclusa y carga)", semiAncho = 4.4f, zMin = -9.0f, zMax = -4.5f, estado = EstadoMedida.Propuesta });
            b.casco.Add(new TramoCasco { nombre = "Popa (ingeniería)", semiAncho = 4.1f, zMin = -14.8f, zMax = -9.0f, estado = EstadoMedida.Conservada });

            b.zonas.Add(new ZonaInterior { nombre = "Pilotaje", tipo = TipoZona.Pilotaje, xMin = -4.0f, xMax = 4.0f, zMin = 1.5f, zMax = 5.0f,
                estado = EstadoMedida.Conservada, nota = "Cabina actual; consola v0 en (0; 0; 3,30) y asiento en z 2,95." });
            b.zonas.Add(new ZonaInterior { nombre = "Pasillo", tipo = TipoZona.Pasillo, xMin = -1.5f, xMax = 1.5f, zMin = -9.0f, zMax = 1.5f,
                estado = EstadoMedida.Propuesta, nota = "Antes 2,4 m de ancho; propuesto 3,0 m para cruzarse flotando." });
            b.zonas.Add(new ZonaInterior { nombre = "Comunicaciones", tipo = TipoZona.Comunicaciones, xMin = 1.7f, xMax = 5.4f, zMin = -4.1f, zMax = 0.7f,
                estado = EstadoMedida.Propuesta, nota = "Antes 3,8 × 4,0 m; propuesto 3,7 × 4,8 m dentro de la envolvente." });
            b.zonas.Add(new ZonaInterior { nombre = "Ciencia", tipo = TipoZona.Ciencia, xMin = -5.4f, xMax = -1.7f, zMin = -4.1f, zMax = 0.7f,
                estado = EstadoMedida.Propuesta, nota = "Simétrica de Comunicaciones." });
            b.zonas.Add(new ZonaInterior { nombre = "Esclusa", tipo = TipoZona.Esclusa, xMin = 1.7f, xMax = 4.0f, zMin = -7.6f, zMax = -5.1f,
                estado = EstadoMedida.PendientePrueba, nota = "Nueva. Escotilla exterior de 1,4 × 2,0 m en el costado derecho." });
            b.zonas.Add(new ZonaInterior { nombre = "Reserva de carga", tipo = TipoZona.ReservaCarga, xMin = -4.0f, xMax = -1.7f, zMin = -7.6f, zMax = -5.1f,
                estado = EstadoMedida.Reserva, nota = "Solo volumen reservado para estiba interior; abierta al pasillo." });
            b.zonas.Add(new ZonaInterior { nombre = "Ingeniería", tipo = TipoZona.Ingenieria, xMin = -3.7f, xMax = 3.7f, zMin = -14.2f, zMax = -9.2f,
                estado = EstadoMedida.Conservada, nota = "Sala actual; la abertura al pasillo pasa a 3,0 m." });

            const float alto = 2.4f;   // altura propuesta de las puertas
            b.muros.Add(Muro("Pilotaje proa", -4.1f, 5.1f, 4.1f, 5.1f));
            b.muros.Add(Muro("Pilotaje derecha", 4.1f, 1.4f, 4.1f, 5.1f));
            b.muros.Add(Muro("Pilotaje izquierda", -4.1f, 1.4f, -4.1f, 5.1f));
            b.muros.Add(Muro("Pilotaje fondo derecha", 1.6f, 1.4f, 4.1f, 1.4f));
            b.muros.Add(Muro("Pilotaje fondo izquierda", -4.1f, 1.4f, -1.6f, 1.4f));
            // Pasillo (a = popa, b = proa; los huecos se miden desde a)
            b.muros.Add(Muro("Pasillo derecha", 1.6f, -9.1f, 1.6f, 1.4f,
                Puerta("Puerta esclusa", 1.75f, 3.75f, alto, EstadoMedida.PendientePrueba),
                Puerta("Puerta Comunicaciones", 6.5f, 8.5f, alto, EstadoMedida.PendientePrueba)));
            b.muros.Add(Muro("Pasillo izquierda", -1.6f, -9.1f, -1.6f, 1.4f,
                Puerta("Acceso a la reserva de carga", 1.5f, 4.0f, 3.4f, EstadoMedida.Reserva),
                Puerta("Puerta Ciencia", 6.5f, 8.5f, alto, EstadoMedida.PendientePrueba)));
            b.muros.Add(Muro("Comunicaciones frente", 1.6f, 0.8f, 5.5f, 0.8f));
            b.muros.Add(Muro("Comunicaciones fondo", 1.6f, -4.2f, 5.5f, -4.2f));
            b.muros.Add(Muro("Comunicaciones exterior", 5.5f, -4.2f, 5.5f, 0.8f));
            b.muros.Add(Muro("Ciencia frente", -5.5f, 0.8f, -1.6f, 0.8f));
            b.muros.Add(Muro("Ciencia fondo", -5.5f, -4.2f, -1.6f, -4.2f));
            b.muros.Add(Muro("Ciencia exterior", -5.5f, -4.2f, -5.5f, 0.8f));
            b.muros.Add(Muro("Esclusa proa", 1.6f, -5.0f, 4.1f, -5.0f));
            b.muros.Add(Muro("Esclusa popa", 1.6f, -7.7f, 4.1f, -7.7f));
            b.muros.Add(Muro("Esclusa exterior", 4.1f, -7.7f, 4.1f, -5.0f,
                Puerta("Escotilla exterior", 0.65f, 2.05f, 2.0f, EstadoMedida.PendientePrueba)));
            b.muros.Add(Muro("Reserva proa", -4.1f, -5.0f, -1.6f, -5.0f));
            b.muros.Add(Muro("Reserva popa", -4.1f, -7.7f, -1.6f, -7.7f));
            b.muros.Add(Muro("Reserva exterior", -4.1f, -7.7f, -4.1f, -5.0f));
            b.muros.Add(Muro("Ingeniería frente derecha", 1.6f, -9.1f, 3.8f, -9.1f));
            b.muros.Add(Muro("Ingeniería frente izquierda", -3.8f, -9.1f, -1.6f, -9.1f));
            b.muros.Add(Muro("Ingeniería derecha", 3.8f, -14.3f, 3.8f, -9.1f));
            b.muros.Add(Muro("Ingeniería izquierda", -3.8f, -14.3f, -3.8f, -9.1f));
            b.muros.Add(Muro("Ingeniería popa", -3.8f, -14.3f, 3.8f, -14.3f));

            b.puestos.Add(new PuestoBase { rol = "Pilotaje", centroConsola = new Vector3(0f, 0.525f, 3.725f), tamanoConsola = new Vector3(2.43f, 1.05f, 0.85f),
                asiento = new Vector3(0f, 0f, 2.95f), orientacionGrados = 0f, estado = EstadoMedida.Conservada, color = new Color(0.95f, 0.55f, 0.16f) });
            b.puestos.Add(new PuestoBase { rol = "Ingeniería", centroConsola = new Vector3(3.25f, 0.5f, -11.2f), tamanoConsola = new Vector3(0.7f, 1.0f, 1.6f),
                asiento = new Vector3(2.35f, 0f, -11.2f), orientacionGrados = 90f, estado = EstadoMedida.PendientePrueba, color = new Color(0.95f, 0.8f, 0.25f) });
            b.puestos.Add(new PuestoBase { rol = "Ciencia", centroConsola = new Vector3(-5.0f, 0.5f, -1.7f), tamanoConsola = new Vector3(0.7f, 1.0f, 1.6f),
                asiento = new Vector3(-4.1f, 0f, -1.7f), orientacionGrados = -90f, estado = EstadoMedida.PendientePrueba, color = new Color(0.45f, 0.85f, 0.5f) });
            b.puestos.Add(new PuestoBase { rol = "Comunicaciones", centroConsola = new Vector3(5.0f, 0.5f, -1.7f), tamanoConsola = new Vector3(0.7f, 1.0f, 1.6f),
                asiento = new Vector3(4.1f, 0f, -1.7f), orientacionGrados = 90f, estado = EstadoMedida.PendientePrueba, color = new Color(0.4f, 0.75f, 1f) });
            return b;
        }
    }

    /// <summary>
    /// Desglose de masa a partir de los datos EXISTENTES (ResumenNave del CatalogoNave). No calcula nada nuevo:
    /// solo separa estructura, componentes instalados, combustible y carga.
    /// </summary>
    public struct DesgloseMasa
    {
        public float estructuralKg, motorKg, tanqueVacioKg, combustibleKg, cargaKg, totalKg;
        public float ComponentesKg => motorKg + tanqueVacioKg;

        public static DesgloseMasa De(ResumenNave n)
        {
            if (n == null) return default;
            return new DesgloseMasa
            {
                estructuralKg = n.masaCascoKg,
                motorKg = n.masaMotorKg,
                tanqueVacioKg = n.masaTanqueKg,
                combustibleKg = n.combustibleInicialKg,
                cargaKg = n.cargaCientificaKg,
                totalKg = n.masaInicialKg
            };
        }
    }
}
