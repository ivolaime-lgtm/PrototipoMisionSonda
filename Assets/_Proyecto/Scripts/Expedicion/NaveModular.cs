using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    // =====================================================================================================================
    // DATOS de la nave de expedición modular EM-4X (solo datos y geometría de referencia; nada visual ni de controles).
    //
    // Bloque PROVISIONAL de DEMOSTRACIÓN para validar tamaño, distribución y sensación de espacio. NO es una nave con
    // balance de vuelo: no tiene masas, motores, combustible, energía ni transporte a la órbita (todo PENDIENTE). No usa
    // los catálogos ni la masa del casco de la E-4.
    //
    // Ejes de la nave: x = estribor, y = «arriba» LOCAL de la nave (no un arriba del espacio), z = proa. Metros.
    // Cada módulo tiene su propio marco: z local = su eje (de 0 a «largo»), y local = su «arriba», x local = su derecha.
    // Secciones octogonales (aproximación por bloques de un cilindro): los diámetros se miden ENTRE CARAS.
    // Los números de ExpedicionEM4X() salen del mismo modelo con el que se revisó la distribución (planta, perfil,
    // volúmenes y recorridos del documento de equipo).
    // Ajuste acordado el 12-11-2026: pasadizos permanentes de 2,4 × 2,4 m libres (2,2 hacia Sistemas) y 1,2 m de largo;
    // los módulos se desplazaron lo necesario. Las uniones con puertas (lanzadera, bodega, esclusa) no cambian.
    // =====================================================================================================================

    public enum EjeNave { MasX, MenosX, MasY, MenosY, MasZ, MenosZ }
    public enum TipoModuloNave { Nodo, Habitable, Esclusa, Propulsion, Lanzadera }
    public enum TipoEquipo { Bastidor, Consola, Asiento, Camarote, Carga, Mesa, Pantalla, Otro }

    /// <summary>Equipo dentro de un módulo: caja en coordenadas LOCALES del módulo (centro y tamaño).</summary>
    [Serializable]
    public class EquipoModulo
    {
        public string nombre;
        public TipoEquipo tipo;
        public Vector3 centro;
        public Vector3 tamano;
        public Vector3 Min => centro - tamano * 0.5f;
        public Vector3 Max => centro + tamano * 0.5f;
    }

    /// <summary>Puerto de unión en la superficie de un módulo (local). «lado» = escotilla cuadrada (m).</summary>
    [Serializable]
    public class PuertoModulo
    {
        public string id;
        public Vector3 posicion;
        public Vector3 normal;
        public float lado;
        [Tooltip("Falso = unión estructural sin paso (no se puede atravesar).")]
        public bool escotilla = true;
        public bool EnTapa => Mathf.Abs(normal.z) > 0.5f;
    }

    [Serializable]
    public class ModuloNave
    {
        public string id;
        public string nombre;
        public string funcion;
        public TipoModuloNave tipo;
        public EstadoMedida estado;
        [Tooltip("Centro de la cara de inicio (z local = 0), en coordenadas de la nave.")]
        public Vector3 origen;
        public EjeNave eje;
        public EjeNave arriba;
        public float largo;
        [Tooltip("Entre caras exteriores del octógono (m).")]
        public float diametroExterior;
        [Tooltip("Entre caras interiores (m).")]
        public float diametroInterior;
        [Tooltip("Paso libre entre equipos (ancho, alto). Cero = sin bastidores (nodo).")]
        public Vector2 pasoLibre;
        [Tooltip("Tramo del eje (z local desde, hasta) donde se exige ese paso libre.")]
        public Vector2 tramoPaso;
        [Tooltip("Solo lanzadera: caja exterior (ancho, alto, largo).")]
        public Vector3 caja;
        public List<PuertoModulo> puertos = new List<PuertoModulo>();
        public List<EquipoModulo> equipos = new List<EquipoModulo>();

        public bool EsCaja => tipo == TipoModuloNave.Lanzadera;
        public bool Presurizado => tipo != TipoModuloNave.Propulsion;
        public float Grosor => (diametroExterior - diametroInterior) * 0.5f;
        public Vector3 Adelante => EjeNaveUtil.Vector(eje);
        public Vector3 Arriba => EjeNaveUtil.Vector(arriba);
        public Vector3 Derecha => Vector3.Cross(Arriba, Adelante);
        public Quaternion Rotacion => Quaternion.LookRotation(Adelante, Arriba);
        public Vector3 AMundo(Vector3 local) => origen + Derecha * local.x + Arriba * local.y + Adelante * local.z;
        public Vector3 DireccionAMundo(Vector3 d) => Derecha * d.x + Arriba * d.y + Adelante * d.z;
        public Vector3 Centro => AMundo(new Vector3(0f, 0f, largo * 0.5f));

        public PuertoModulo Puerto(string pid)
        {
            foreach (PuertoModulo p in puertos) if (p.id == pid) return p;
            return null;
        }

        /// <summary>Medio ancho exterior en x e y locales (octógono: medio diámetro entre caras).</summary>
        public Vector2 MedioExterior => EsCaja ? new Vector2(caja.x * 0.5f, caja.y * 0.5f) : new Vector2(diametroExterior * 0.5f, diametroExterior * 0.5f);

        /// <summary>Caja alineada con los ejes de la nave que envuelve el módulo (exacta: los ejes son ±X, ±Y, ±Z).</summary>
        public Bounds Limites
        {
            get
            {
                Vector2 h = MedioExterior;
                Bounds b = new Bounds(AMundo(new Vector3(-h.x, -h.y, 0f)), Vector3.zero);
                for (int i = 0; i < 8; i++)
                    b.Encapsulate(AMundo(new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? 0f : largo)));
                return b;
            }
        }

        /// <summary>Interior de la lanzadera (bloque): x ±4,2, suelo a −1,45, altura 3,4, z 0,45..11,55.</summary>
        public static readonly Vector3 LanzaderaInteriorMin = new Vector3(-4.2f, -1.45f, 0.45f);
        public static readonly Vector3 LanzaderaInteriorMax = new Vector3(4.2f, 1.95f, 11.55f);
    }

    public static class EjeNaveUtil
    {
        public static Vector3 Vector(EjeNave e)
        {
            switch (e)
            {
                case EjeNave.MasX: return Vector3.right;
                case EjeNave.MenosX: return Vector3.left;
                case EjeNave.MasY: return Vector3.up;
                case EjeNave.MenosY: return Vector3.down;
                case EjeNave.MasZ: return Vector3.forward;
                default: return Vector3.back;
            }
        }
    }

    [Serializable]
    public class ConexionModulos
    {
        public string moduloA, puertoA, moduloB, puertoB;
        [Tooltip("Pasadizo permanente (sección cuadrada del tamaño del hueco). Falso = adaptador octogonal corto (uniones con puertas, estructural).")]
        public bool pasadizo;
        [Tooltip("Largo de la conexión (m). 0 = el del adaptador (largoAdaptador).")]
        public float largo;
    }

    /// <summary>Puesto de trabajo: punto donde está el tripulante (local del módulo) y hacia dónde mira.</summary>
    [Serializable]
    public class PuestoExpedicion
    {
        public string rol;
        public string modulo;
        public Vector3 posicion;
        public Vector3 mirada;
        public Color color;
    }

    /// <summary>Vista interior extra para la cámara de inspección (esclusa, nodo, lanzadera).</summary>
    [Serializable]
    public class VistaInterior
    {
        public string nombre;
        public string modulo;
        public Vector3 posicion;
        public Vector3 mirada;
    }

    /// <summary>Elemento exterior sin interior (antena, motor, reserva de la sonda…), unido a un módulo.</summary>
    [Serializable]
    public class ElementoExterior
    {
        public string nombre;
        public string unidoA;
        public Vector3 min, max;
    }

    public class DefinicionNaveModular
    {
        public string nombre = "EM-4X · nave de expedición modular (bloque provisional)";
        /// <summary>Largo de los adaptadores entre módulos (m).</summary>
        public float largoAdaptador = 0.6f;
        public List<ModuloNave> modulos = new List<ModuloNave>();
        public List<ConexionModulos> conexiones = new List<ConexionModulos>();
        public List<PuestoExpedicion> puestos = new List<PuestoExpedicion>();
        public List<VistaInterior> vistas = new List<VistaInterior>();
        public List<ElementoExterior> exteriores = new List<ElementoExterior>();
        [Tooltip("Paneles solares y radiadores desplegados (a cada lado, x desde-hasta y z desde-hasta). Fuera del ancho medido.")]
        public Vector2 panelesX, panelesZ;

        public static float InteriorAdaptador(float lado) => lado + 0.8f;
        public static float ExteriorAdaptador(float lado) => lado + 1.2f;

        /// <summary>Paredes de los pasadizos permanentes (m): exterior = libre + 2 × grosor.</summary>
        public const float GrosorPasadizo = 0.2f;
        public static float ExteriorPasadizo(float libre) => libre + 2f * GrosorPasadizo;

        /// <summary>Largo real de una conexión (pasadizo o adaptador).</summary>
        public float LargoConexion(ConexionModulos c) => c != null && c.largo > 0f ? c.largo : largoAdaptador;

        /// <summary>Ancho exterior de la conexión unida a un puerto de lado «lado».</summary>
        public static float ExteriorConexion(ConexionModulos c, float lado) => c != null && c.pasadizo ? ExteriorPasadizo(lado) : ExteriorAdaptador(lado);

        /// <summary>Conexión que usa un puerto (null si no tiene módulo al otro lado).</summary>
        public ConexionModulos ConexionDe(string modulo, string puerto)
        {
            foreach (ConexionModulos c in conexiones)
                if ((c.moduloA == modulo && c.puertoA == puerto) || (c.moduloB == modulo && c.puertoB == puerto)) return c;
            return null;
        }

        public ModuloNave Agregar(ModuloNave m) { modulos.Add(m); return m; }

        public ModuloNave Modulo(string id)
        {
            foreach (ModuloNave m in modulos) if (m.id == id) return m;
            return null;
        }

        /// <summary>La distribución propuesta EM-4X. Medidas PROVISIONALES (ver el documento de equipo).</summary>
        public static DefinicionNaveModular ExpedicionEM4X()
        {
            ModuloNave m;
            var d = new DefinicionNaveModular();

            // Nodo central · zona común
            m = d.Agregar(new ModuloNave { id = "NC", nombre = "Nodo central · zona común", funcion = "Reunión de los cuatro, distribución a todos los módulos, mesa y pantalla de misión",
                tipo = TipoModuloNave.Nodo, estado = EstadoMedida.Propuesta, origen = new Vector3(0f, 0f, -5f),
                eje = EjeNave.MasZ, arriba = EjeNave.MasY, largo = 10f, diametroExterior = 10f, diametroInterior = 9.4f,
                pasoLibre = Vector2.zero, tramoPaso = Vector2.zero, caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "popa", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 2.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "proa", posicion = new Vector3(0f, 0f, 10f), normal = new Vector3(0f, 0f, 1f), lado = 2.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "estribor", posicion = new Vector3(5f, 0f, 6.5f), normal = new Vector3(1f, 0f, 0f), lado = 2.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "babor", posicion = new Vector3(-5f, 0f, 6.5f), normal = new Vector3(-1f, 0f, 0f), lado = 2.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "dorsal", posicion = new Vector3(0f, 5f, 6.5f), normal = new Vector3(0f, 1f, 0f), lado = 2.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "ventral", posicion = new Vector3(0f, -5f, 6.5f), normal = new Vector3(0f, -1f, 0f), lado = 1.4f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Mesa de misión", tipo = TipoEquipo.Mesa, centro = new Vector3(0f, -3.15f, 2.8f), tamano = new Vector3(2.4f, 0.1f, 2.4f) });
            m.equipos.Add(new EquipoModulo { nombre = "Pie de la mesa", tipo = TipoEquipo.Mesa, centro = new Vector3(0f, -3.95f, 2.8f), tamano = new Vector3(0.3f, 1.5f, 0.3f) });
            m.equipos.Add(new EquipoModulo { nombre = "Pantalla de misión", tipo = TipoEquipo.Pantalla, centro = new Vector3(-4.6f, 0.3f, 2.8f), tamano = new Vector3(0.15f, 1.5f, 2.6f) });
            m.equipos.Add(new EquipoModulo { nombre = "Taquillas comunes", tipo = TipoEquipo.Bastidor, centro = new Vector3(4.35f, 0f, 2.6f), tamano = new Vector3(0.7f, 2.2f, 3f) });

            // Mando · Pilotaje
            m = d.Agregar(new ModuloNave { id = "MA", nombre = "Mando · Pilotaje", funcion = "Pilotaje (consola v0) y atraque de la lanzadera",
                tipo = TipoModuloNave.Habitable, estado = EstadoMedida.Propuesta, origen = new Vector3(0f, 0f, 6.2f),
                eje = EjeNave.MasZ, arriba = EjeNave.MasY, largo = 9f, diametroExterior = 6f, diametroInterior = 5.4f,
                pasoLibre = new Vector2(3.8f, 3.8f), tramoPaso = new Vector2(0f, 4.6f), caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 2.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "atraque", posicion = new Vector3(0f, -3f, 5.5f), normal = new Vector3(0f, -1f, 0f), lado = 1f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de navegación (estribor)", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 2.6f), tamano = new Vector3(0.7f, 2.2f, 4f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de navegación (babor)", tipo = TipoEquipo.Bastidor, centro = new Vector3(-2.35f, 0f, 2.6f), tamano = new Vector3(0.7f, 2.2f, 4f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de guiado (techo)", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, 2.35f, 2.6f), tamano = new Vector3(2.2f, 0.7f, 4f) });
            m.equipos.Add(new EquipoModulo { nombre = "Plataforma de pilotaje", tipo = TipoEquipo.Otro, centro = new Vector3(0f, -2.55f, 7.6f), tamano = new Vector3(2f, 0.3f, 2.2f) });
            m.equipos.Add(new EquipoModulo { nombre = "Consola v0 (del compañero)", tipo = TipoEquipo.Consola, centro = new Vector3(0f, -1.875f, 8f), tamano = new Vector3(2.43f, 1.05f, 0.85f) });
            m.equipos.Add(new EquipoModulo { nombre = "Asiento de pilotaje", tipo = TipoEquipo.Asiento, centro = new Vector3(0f, -1.9f, 7f), tamano = new Vector3(0.6f, 1f, 0.6f) });

            // Ciencia
            m = d.Agregar(new ModuloNave { id = "CI", nombre = "Ciencia", funcion = "Puesto de Ciencia, instrumentos y muestras",
                tipo = TipoModuloNave.Habitable, estado = EstadoMedida.Propuesta, origen = new Vector3(-6.2f, 0f, 1.5f),
                eje = EjeNave.MenosX, arriba = EjeNave.MasY, largo = 9f, diametroExterior = 6f, diametroInterior = 5.4f,
                pasoLibre = new Vector2(3.8f, 3.8f), tramoPaso = new Vector2(0f, 9f), caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 2.4f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Consola de Ciencia", tipo = TipoEquipo.Consola, centro = new Vector3(2.35f, 0f, 4f), tamano = new Vector3(0.7f, 1.2f, 2f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de instrumentos 1", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 1.6f), tamano = new Vector3(0.7f, 2.2f, 2f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de instrumentos 2", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 6.9f), tamano = new Vector3(0.7f, 2.2f, 3f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores de laboratorio (pared)", tipo = TipoEquipo.Bastidor, centro = new Vector3(-2.35f, 0f, 4.8f), tamano = new Vector3(0.7f, 2.2f, 7.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores (techo)", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, 2.35f, 4.8f), tamano = new Vector3(2.2f, 0.7f, 7.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores (suelo)", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, -2.35f, 4.8f), tamano = new Vector3(2.2f, 0.7f, 7.8f) });

            // Comunicaciones
            m = d.Agregar(new ModuloNave { id = "CO", nombre = "Comunicaciones", funcion = "Puesto de Comunicaciones, enlace y datos; antena exterior",
                tipo = TipoModuloNave.Habitable, estado = EstadoMedida.Propuesta, origen = new Vector3(6.2f, 0f, 1.5f),
                eje = EjeNave.MasX, arriba = EjeNave.MasY, largo = 7f, diametroExterior = 6f, diametroInterior = 5.4f,
                pasoLibre = new Vector2(3.8f, 3.8f), tramoPaso = new Vector2(0f, 7f), caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 2.4f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Consola de Comunicaciones", tipo = TipoEquipo.Consola, centro = new Vector3(2.35f, 0f, 3.5f), tamano = new Vector3(0.7f, 1.2f, 2f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de radio 1", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 1.4f), tamano = new Vector3(0.7f, 2.2f, 1.6f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de radio 2", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 5.6f), tamano = new Vector3(0.7f, 2.2f, 1.6f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores de datos (pared)", tipo = TipoEquipo.Bastidor, centro = new Vector3(-2.35f, 0f, 3.8f), tamano = new Vector3(0.7f, 2.2f, 5.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores (techo)", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, 2.35f, 3.8f), tamano = new Vector3(2.2f, 0.7f, 5.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores (suelo)", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, -2.35f, 3.8f), tamano = new Vector3(2.2f, 0.7f, 5.8f) });

            // Ingeniería
            m = d.Agregar(new ModuloNave { id = "IN", nombre = "Ingeniería", funcion = "Puesto de Ingeniería: energía, motor y averías",
                tipo = TipoModuloNave.Habitable, estado = EstadoMedida.Propuesta, origen = new Vector3(0f, 0f, -6.2f),
                eje = EjeNave.MenosZ, arriba = EjeNave.MasY, largo = 9f, diametroExterior = 6f, diametroInterior = 5.4f,
                pasoLibre = new Vector2(3.8f, 3.8f), tramoPaso = new Vector2(0f, 9f), caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 2.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "fin", posicion = new Vector3(0f, 0f, 9f), normal = new Vector3(0f, 0f, 1f), lado = 2.4f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Consola de Ingeniería", tipo = TipoEquipo.Consola, centro = new Vector3(2.35f, 0f, 4f), tamano = new Vector3(0.7f, 1.2f, 2f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de motor", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 1.6f), tamano = new Vector3(0.7f, 2.2f, 2f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de energía", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 6.9f), tamano = new Vector3(0.7f, 2.2f, 3f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores de control (pared)", tipo = TipoEquipo.Bastidor, centro = new Vector3(-2.35f, 0f, 4.5f), tamano = new Vector3(0.7f, 2.2f, 7.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores (techo)", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, 2.35f, 4.5f), tamano = new Vector3(2.2f, 0.7f, 7.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidores (suelo)", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, -2.35f, 4.5f), tamano = new Vector3(2.2f, 0.7f, 7.8f) });

            // Hábitat
            m = d.Agregar(new ModuloNave { id = "HA", nombre = "Hábitat", funcion = "Cuatro camarotes (sujeción y descanso), cocina, higiene y ejercicio",
                tipo = TipoModuloNave.Habitable, estado = EstadoMedida.Propuesta, origen = new Vector3(0f, 6.2f, 1.5f),
                eje = EjeNave.MasY, arriba = EjeNave.MasZ, largo = 8f, diametroExterior = 6f, diametroInterior = 5.4f,
                pasoLibre = new Vector2(3.8f, 3.8f), tramoPaso = new Vector2(0f, 8f), caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 2.4f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Cocina y mesa", tipo = TipoEquipo.Bastidor, centro = new Vector3(-2.35f, 0f, 1.8f), tamano = new Vector3(0.7f, 2.2f, 2.4f) });
            m.equipos.Add(new EquipoModulo { nombre = "Higiene", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 1.8f), tamano = new Vector3(0.7f, 2.2f, 2.4f) });
            m.equipos.Add(new EquipoModulo { nombre = "Ejercicio", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, 2.35f, 1.8f), tamano = new Vector3(2.2f, 0.7f, 2.4f) });
            m.equipos.Add(new EquipoModulo { nombre = "Víveres", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, -2.35f, 1.8f), tamano = new Vector3(2.2f, 0.7f, 2.4f) });
            m.equipos.Add(new EquipoModulo { nombre = "Camarote 1 (sujeción y descanso)", tipo = TipoEquipo.Camarote, centro = new Vector3(2.3f, 0f, 5.8f), tamano = new Vector3(0.8f, 2.2f, 3f) });
            m.equipos.Add(new EquipoModulo { nombre = "Camarote 2 (sujeción y descanso)", tipo = TipoEquipo.Camarote, centro = new Vector3(-2.3f, 0f, 5.8f), tamano = new Vector3(0.8f, 2.2f, 3f) });
            m.equipos.Add(new EquipoModulo { nombre = "Camarote 3 (sujeción y descanso)", tipo = TipoEquipo.Camarote, centro = new Vector3(0f, 2.3f, 5.8f), tamano = new Vector3(2.2f, 0.8f, 3f) });
            m.equipos.Add(new EquipoModulo { nombre = "Camarote 4 (sujeción y descanso)", tipo = TipoEquipo.Camarote, centro = new Vector3(0f, -2.3f, 5.8f), tamano = new Vector3(2.2f, 0.8f, 3f) });

            // Bodega
            m = d.Agregar(new ModuloNave { id = "BO", nombre = "Bodega", funcion = "Carga presurizada: repuestos, víveres y material de la misión",
                tipo = TipoModuloNave.Habitable, estado = EstadoMedida.Reserva, origen = new Vector3(0f, -5.6f, 1.5f),
                eje = EjeNave.MenosY, arriba = EjeNave.MasZ, largo = 6f, diametroExterior = 6f, diametroInterior = 5.4f,
                pasoLibre = new Vector2(3.8f, 3.8f), tramoPaso = new Vector2(0f, 6f), caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 1.4f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de carga", tipo = TipoEquipo.Carga, centro = new Vector3(2.3f, 0f, 3f), tamano = new Vector3(0.8f, 2.2f, 4.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de carga", tipo = TipoEquipo.Carga, centro = new Vector3(-2.3f, 0f, 3f), tamano = new Vector3(0.8f, 2.2f, 4.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de carga", tipo = TipoEquipo.Carga, centro = new Vector3(0f, 2.3f, 3f), tamano = new Vector3(2.2f, 0.8f, 4.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Bastidor de carga", tipo = TipoEquipo.Carga, centro = new Vector3(0f, -2.3f, 3f), tamano = new Vector3(2.2f, 0.8f, 4.8f) });

            // Nodo de popa
            m = d.Agregar(new ModuloNave { id = "N2", nombre = "Nodo de popa", funcion = "Distribución a sistemas y esclusa; anclaje de la sonda y puerto de suministro",
                tipo = TipoModuloNave.Nodo, estado = EstadoMedida.Propuesta, origen = new Vector3(0f, 0f, -16.4f),
                eje = EjeNave.MenosZ, arriba = EjeNave.MasY, largo = 5f, diametroExterior = 6.6f, diametroInterior = 6f,
                pasoLibre = Vector2.zero, tramoPaso = Vector2.zero, caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 2.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "fin", posicion = new Vector3(0f, 0f, 5f), normal = new Vector3(0f, 0f, 1f), lado = 1.4f, escotilla = false });
            m.puertos.Add(new PuertoModulo { id = "dorsal", posicion = new Vector3(0f, 3.3f, 2.5f), normal = new Vector3(0f, 1f, 0f), lado = 2.2f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "ventral", posicion = new Vector3(0f, -3.3f, 2.5f), normal = new Vector3(0f, -1f, 0f), lado = 1.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "suministro", posicion = new Vector3(-3.3f, 0f, 2.5f), normal = new Vector3(-1f, 0f, 0f), lado = 1.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "sonda", posicion = new Vector3(3.3f, 0f, 2.5f), normal = new Vector3(1f, 0f, 0f), lado = 1.4f, escotilla = false });

            // Sistemas
            m = d.Agregar(new ModuloNave { id = "SI", nombre = "Sistemas", funcion = "Soporte vital, energía y control térmico (mantenimiento)",
                tipo = TipoModuloNave.Habitable, estado = EstadoMedida.Propuesta, origen = new Vector3(0f, 4.5f, -18.9f),
                eje = EjeNave.MasY, arriba = EjeNave.MasZ, largo = 6f, diametroExterior = 6f, diametroInterior = 5.4f,
                pasoLibre = new Vector2(3.8f, 3.8f), tramoPaso = new Vector2(0f, 6f), caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 2.2f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Soporte vital: aire y agua", tipo = TipoEquipo.Bastidor, centro = new Vector3(2.35f, 0f, 3f), tamano = new Vector3(0.7f, 2.2f, 4.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Energía y baterías", tipo = TipoEquipo.Bastidor, centro = new Vector3(-2.35f, 0f, 3f), tamano = new Vector3(0.7f, 2.2f, 4.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Control térmico", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, 2.35f, 3f), tamano = new Vector3(2.2f, 0.7f, 4.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Repuestos", tipo = TipoEquipo.Bastidor, centro = new Vector3(0f, -2.35f, 3f), tamano = new Vector3(2.2f, 0.7f, 4.8f) });

            // Esclusa
            m = d.Agregar(new ModuloNave { id = "ES", nombre = "Esclusa", funcion = "Salidas al exterior (EVA): trajes y escotilla exterior",
                tipo = TipoModuloNave.Esclusa, estado = EstadoMedida.PendientePrueba, origen = new Vector3(0f, -3.9f, -18.9f),
                eje = EjeNave.MenosY, arriba = EjeNave.MasZ, largo = 4.5f, diametroExterior = 4.4f, diametroInterior = 3.8f,
                pasoLibre = new Vector2(2.4f, 2.4f), tramoPaso = new Vector2(0f, 4.5f), caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 1.4f, escotilla = true });
            m.puertos.Add(new PuertoModulo { id = "exterior", posicion = new Vector3(0f, 0f, 4.5f), normal = new Vector3(0f, 0f, 1f), lado = 1.4f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Traje EVA 1", tipo = TipoEquipo.Bastidor, centro = new Vector3(1.45f, 0f, 2.25f), tamano = new Vector3(0.5f, 1.9f, 1.2f) });
            m.equipos.Add(new EquipoModulo { nombre = "Traje EVA 2", tipo = TipoEquipo.Bastidor, centro = new Vector3(-1.45f, 0f, 2.25f), tamano = new Vector3(0.5f, 1.9f, 1.2f) });

            // Etapa de propulsión
            m = d.Agregar(new ModuloNave { id = "PR", nombre = "Etapa de propulsión", funcion = "Tanques y motor (solo exterior; sin balance)",
                tipo = TipoModuloNave.Propulsion, estado = EstadoMedida.Reserva, origen = new Vector3(0f, 0f, -22f),
                eje = EjeNave.MenosZ, arriba = EjeNave.MasY, largo = 15f, diametroExterior = 6f, diametroInterior = 0f,
                pasoLibre = Vector2.zero, tramoPaso = Vector2.zero, caja = Vector3.zero });
            m.puertos.Add(new PuertoModulo { id = "inicio", posicion = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), lado = 1.4f, escotilla = false });

            // Lanzadera L-4
            m = d.Agregar(new ModuloNave { id = "LZ", nombre = "Lanzadera L-4", funcion = "Despega desde tierra con los cuatro; atraca bajo el mando",
                tipo = TipoModuloNave.Lanzadera, estado = EstadoMedida.Conservada, origen = new Vector3(0f, -6.05f, 6.7f),
                eje = EjeNave.MasZ, arriba = EjeNave.MasY, largo = 12f, diametroExterior = 0f, diametroInterior = 0f,
                pasoLibre = Vector2.zero, tramoPaso = Vector2.zero, caja = new Vector3(9.2f, 4.9f, 12f) });
            m.puertos.Add(new PuertoModulo { id = "atraque", posicion = new Vector3(0f, 2.45f, 5f), normal = new Vector3(0f, 1f, 0f), lado = 1f, escotilla = true });
            m.equipos.Add(new EquipoModulo { nombre = "Consola de la lanzadera", tipo = TipoEquipo.Consola, centro = new Vector3(0f, -0.9f, 10.9f), tamano = new Vector3(2.4f, 1.1f, 0.8f) });
            m.equipos.Add(new EquipoModulo { nombre = "Asiento de lanzamiento 1", tipo = TipoEquipo.Asiento, centro = new Vector3(-2.4f, -0.95f, 9.3f), tamano = new Vector3(0.6f, 1f, 0.6f) });
            m.equipos.Add(new EquipoModulo { nombre = "Asiento de lanzamiento 2", tipo = TipoEquipo.Asiento, centro = new Vector3(-0.8f, -0.95f, 9.3f), tamano = new Vector3(0.6f, 1f, 0.6f) });
            m.equipos.Add(new EquipoModulo { nombre = "Asiento de lanzamiento 3", tipo = TipoEquipo.Asiento, centro = new Vector3(0.8f, -0.95f, 9.3f), tamano = new Vector3(0.6f, 1f, 0.6f) });
            m.equipos.Add(new EquipoModulo { nombre = "Asiento de lanzamiento 4", tipo = TipoEquipo.Asiento, centro = new Vector3(2.4f, -0.95f, 9.3f), tamano = new Vector3(0.6f, 1f, 0.6f) });

            d.conexiones.Add(new ConexionModulos { moduloA = "NC", puertoA = "proa", moduloB = "MA", puertoB = "inicio", pasadizo = true, largo = 1.2f });
            d.conexiones.Add(new ConexionModulos { moduloA = "NC", puertoA = "popa", moduloB = "IN", puertoB = "inicio", pasadizo = true, largo = 1.2f });
            d.conexiones.Add(new ConexionModulos { moduloA = "NC", puertoA = "babor", moduloB = "CI", puertoB = "inicio", pasadizo = true, largo = 1.2f });
            d.conexiones.Add(new ConexionModulos { moduloA = "NC", puertoA = "estribor", moduloB = "CO", puertoB = "inicio", pasadizo = true, largo = 1.2f });
            d.conexiones.Add(new ConexionModulos { moduloA = "NC", puertoA = "dorsal", moduloB = "HA", puertoB = "inicio", pasadizo = true, largo = 1.2f });
            d.conexiones.Add(new ConexionModulos { moduloA = "NC", puertoA = "ventral", moduloB = "BO", puertoB = "inicio" });
            d.conexiones.Add(new ConexionModulos { moduloA = "IN", puertoA = "fin", moduloB = "N2", puertoB = "inicio", pasadizo = true, largo = 1.2f });
            d.conexiones.Add(new ConexionModulos { moduloA = "N2", puertoA = "dorsal", moduloB = "SI", puertoB = "inicio", pasadizo = true, largo = 1.2f });
            d.conexiones.Add(new ConexionModulos { moduloA = "N2", puertoA = "ventral", moduloB = "ES", puertoB = "inicio" });
            d.conexiones.Add(new ConexionModulos { moduloA = "N2", puertoA = "fin", moduloB = "PR", puertoB = "inicio" });
            d.conexiones.Add(new ConexionModulos { moduloA = "MA", puertoA = "atraque", moduloB = "LZ", puertoB = "atraque" });

            d.puestos.Add(new PuestoExpedicion { rol = "Pilotaje", modulo = "MA", posicion = new Vector3(0f, -0.5f, 7f), mirada = new Vector3(0f, -0.15f, 1f), color = new Color(0.23f, 0.49f, 0.85f) });
            d.puestos.Add(new PuestoExpedicion { rol = "Ingeniería", modulo = "IN", posicion = new Vector3(1.2f, -0.4f, 4f), mirada = new Vector3(1f, 0f, 0f), color = new Color(0.85f, 0.51f, 0.23f) });
            d.puestos.Add(new PuestoExpedicion { rol = "Ciencia", modulo = "CI", posicion = new Vector3(1.2f, -0.4f, 4f), mirada = new Vector3(1f, 0f, 0f), color = new Color(0.23f, 0.7f, 0.42f) });
            d.puestos.Add(new PuestoExpedicion { rol = "Comunicaciones", modulo = "CO", posicion = new Vector3(1.2f, -0.4f, 3.5f), mirada = new Vector3(1f, 0f, 0f), color = new Color(0.64f, 0.35f, 0.85f) });
            d.vistas.Add(new VistaInterior { nombre = "Esclusa", modulo = "ES", posicion = new Vector3(0f, 0f, 1.2f), mirada = new Vector3(0f, 0f, 1f) });
            d.vistas.Add(new VistaInterior { nombre = "Nodo central · mesa de misión", modulo = "NC", posicion = new Vector3(0f, 0.6f, 7.6f), mirada = new Vector3(0f, -0.35f, -1f) });
            d.vistas.Add(new VistaInterior { nombre = "Lanzadera · asientos", modulo = "LZ", posicion = new Vector3(0f, 0.9f, 7.2f), mirada = new Vector3(0f, -0.1f, 1f) });
            d.exteriores.Add(new ElementoExterior { nombre = "Antena de alta ganancia", unidoA = "CO", min = new Vector3(13.2f, -1.5f, 0f), max = new Vector3(15f, 1.5f, 3f) });
            d.exteriores.Add(new ElementoExterior { nombre = "Motor", unidoA = "PR", min = new Vector3(-2.25f, -2.25f, -41f), max = new Vector3(2.25f, 2.25f, -37f) });
            d.exteriores.Add(new ElementoExterior { nombre = "Reserva de la sonda (exterior)", unidoA = "N2", min = new Vector3(-6.9f, -1.5f, -20.4f), max = new Vector3(-3.9f, 1.5f, -17.4f) });
            d.exteriores.Add(new ElementoExterior { nombre = "Tapa del puerto de suministro", unidoA = "N2", min = new Vector3(3.3f, -1.3f, -20.2f), max = new Vector3(3.9f, 1.3f, -17.6f) });
            d.panelesX = new Vector2(3.5f, 19f);
            d.panelesZ = new Vector2(-35.2f, -25.2f);
            return d;
        }
    }
}
