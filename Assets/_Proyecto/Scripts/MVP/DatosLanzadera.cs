using System;
using System.Collections.Generic;

namespace PrototipoMision
{
    // =====================================================================================================================
    // LANZADERA L-4 (vehículo TERRESTRE que sube a la tripulación a órbita) — SOLO DATOS. Separada de la EM-4X.
    //
    // Antes de este incremento la L-4 no tenía datos propios: solo la tarifa fija «Lanzamiento de la tripulación» (18 M)
    // y un bloque visual en la secuencia de despegue. La carga desde tierra viaja en un ENVÍO ORBITAL aparte, sin
    // tripulación (tarifa de 10 M si se envía algo).
    //
    // Ahora se puede configurar de forma LIMITADA: tres tipos de pieza, tres alternativas cada uno. La opción 0 de cada
    // tipo es la L-4 de siempre y está incluida en la tarifa de 18 M (con todo en 0, los costes son exactamente los de
    // antes). Todas las cifras son de DEMOSTRACIÓN: reglas de juego explícitas, NO prestaciones validadas.
    //
    // Reglas (las aplica BalanceEM4X.Calcular y se muestran en pantalla):
    //  1. Carga pequeña = suministros extra + kits extra + sonda. Si su masa cabe ENTERA en la bodega, viaja en la
    //     lanzadera (se entrega con el carro en el muelle de integración); si no cabe, va toda en el envío orbital.
    //  2. Combustible encargado: si TODOS los tramos caben en el tanque de transferencia, viaja en la lanzadera; si no,
    //     va todo en el envío orbital.
    //  3. Los componentes de la EM-4X (motor, tanque, instrumento, relevo) van SIEMPRE en el envío orbital.
    //  4. El envío orbital (10 M) solo se cobra si queda algo que no viaja en la lanzadera.
    //  5. Masa extra de la L-4 = bodega y tanque instalados (aunque vayan vacíos) + lo que llevan. Si supera la capacidad
    //     que dan los refuerzos, la preparación no se puede cerrar.
    // =====================================================================================================================

    /// <summary>Configuración de la lanzadera L-4 (índices sobre DatosLanzadera). Independiente de la de la EM-4X.</summary>
    [Serializable]
    public class ConfiguracionLanzadera
    {
        public int bodega;       // 0 = sin bodega (L-4 de siempre)
        public int tanque;       // 0 = sin tanque de transferencia
        public int refuerzos;    // 0 = sin propulsores de refuerzo

        public ConfiguracionLanzadera Copia() => (ConfiguracionLanzadera)MemberwiseClone();
    }

    public enum PiezaLanzadera { Bodega, Tanque, Refuerzos }

    public class OpcionLanzadera
    {
        public string nombre;
        /// <summary>Qué hace (texto corto).</summary>
        public string queHace;
        /// <summary>Consecuencia jugable en el flujo actual (o que no la tiene).</summary>
        public string consecuencia;
        public float costeMUSD;
        /// <summary>Masa propia de la pieza (cuenta contra la capacidad aunque vaya vacía).</summary>
        public float masaKg;
        /// <summary>Bodega: carga que admite · Tanque: combustible que admite · Refuerzos: masa extra que puede subir la L-4.</summary>
        public float capacidadKg;
        /// <summary>Solo refuerzos: cuántos propulsores (para la representación).</summary>
        public int unidades;
    }

    /// <summary>Resultado de la lanzadera dentro de un ResumenExpedicion (qué viaja dónde, masas y capacidad).</summary>
    public class ResumenLanzadera
    {
        public ConfiguracionLanzadera eleccion;
        public OpcionLanzadera bodega, tanque, refuerzos;
        public float cargaPequenaKg, combustibleEncargadoKg;
        public bool cargaPequenaEnLanzadera, combustibleEnLanzadera;
        public float masaExtraKg, capacidadKg;
        public bool envioOrbital;
        public readonly List<string> enLanzadera = new List<string>();
        public readonly List<string> enEnvio = new List<string>();
        public float costeOpcionesMUSD;
        public bool CapacidadSuperada => masaExtraKg > capacidadKg + 0.5f;
    }

    public static class DatosLanzadera
    {
        public const string Nombre = "Lanzadera L-4";

        public static List<OpcionLanzadera> Bodegas() => new List<OpcionLanzadera>
        {
            new OpcionLanzadera { nombre = "Sin bodega (L-4 de siempre)", queHace = "Solo cápsula: la carga pequeña va en el envío orbital.",
                consecuencia = "Incluida en la tarifa de lanzamiento.", costeMUSD = 0f, masaKg = 0f, capacidadKg = 0f },
            new OpcionLanzadera { nombre = "Bodega pequeña", queHace = "Admite 300 kg de carga pequeña (suministros, kits, sonda).",
                consecuencia = "Si toda la carga pequeña cabe, viaja con la tripulación y se entrega en el muelle de integración. Pesa 200 kg.",
                costeMUSD = 1.5f, masaKg = 200f, capacidadKg = 300f },
            new OpcionLanzadera { nombre = "Bodega grande", queHace = "Admite 800 kg de carga pequeña: cabe el máximo que se puede encargar.",
                consecuencia = "Igual que la pequeña con más sitio, pero pesa 400 kg: con carga suele necesitar refuerzos.",
                costeMUSD = 3f, masaKg = 400f, capacidadKg = 800f }
        };

        public static List<OpcionLanzadera> Tanques() => new List<OpcionLanzadera>
        {
            new OpcionLanzadera { nombre = "Sin tanque de transferencia", queHace = "El combustible para la EM-4X va en el envío orbital.",
                consecuencia = "Incluido en la tarifa de lanzamiento.", costeMUSD = 0f, masaKg = 0f, capacidadKg = 0f },
            new OpcionLanzadera { nombre = "Tanque de transferencia · 1 tramo", queHace = "Lleva hasta 3000 kg de combustible para la EM-4X.",
                consecuencia = "Si se encarga 1 tramo o ninguno, el combustible viaja en la lanzadera. Pesa 500 kg vacío.",
                costeMUSD = 2f, masaKg = 500f, capacidadKg = 3000f },
            new OpcionLanzadera { nombre = "Tanque de transferencia · 2 tramos", queHace = "Lleva hasta 6000 kg de combustible para la EM-4X.",
                consecuencia = "Hasta 2 tramos viajan en la lanzadera. Pesa 800 kg vacío: lleno necesita 4 refuerzos.",
                costeMUSD = 3.5f, masaKg = 800f, capacidadKg = 6000f }
        };

        public static List<OpcionLanzadera> Refuerzos() => new List<OpcionLanzadera>
        {
            new OpcionLanzadera { nombre = "Sin refuerzos (L-4 de siempre)", queHace = "Sube la cápsula y hasta 1000 kg extra.",
                consecuencia = "Incluido en la tarifa de lanzamiento.", costeMUSD = 0f, capacidadKg = 1000f, unidades = 0 },
            new OpcionLanzadera { nombre = "2 propulsores de refuerzo", queHace = "Sube hasta 4000 kg extra (bodega, tanque y su contenido).",
                consecuencia = "Solo sirven si hay bodega o tanque que subir; si no, es un gasto sin efecto.", costeMUSD = 4f, capacidadKg = 4000f, unidades = 2 },
            new OpcionLanzadera { nombre = "4 propulsores de refuerzo", queHace = "Sube hasta 8000 kg extra.",
                consecuencia = "Necesarios para el tanque de 2 tramos lleno. Caros: compara con la tarifa del envío orbital (10 M).",
                costeMUSD = 8f, capacidadKg = 8000f, unidades = 4 }
        };

        public static List<OpcionLanzadera> Opciones(PiezaLanzadera t) =>
            t == PiezaLanzadera.Bodega ? Bodegas() : t == PiezaLanzadera.Tanque ? Tanques() : Refuerzos();

        public static int Indice(ConfiguracionLanzadera c, PiezaLanzadera t) =>
            t == PiezaLanzadera.Bodega ? c.bodega : t == PiezaLanzadera.Tanque ? c.tanque : c.refuerzos;

        public static void Fijar(ConfiguracionLanzadera c, PiezaLanzadera t, int i)
        {
            if (t == PiezaLanzadera.Bodega) c.bodega = i;
            else if (t == PiezaLanzadera.Tanque) c.tanque = i;
            else c.refuerzos = i;
        }

        public static string NombrePieza(PiezaLanzadera t) =>
            t == PiezaLanzadera.Bodega ? "BODEGA DE CARGA" : t == PiezaLanzadera.Tanque ? "TANQUE DE TRANSFERENCIA" : "PROPULSORES DE REFUERZO";

        /// <summary>Ajusta los índices a las listas (para preparaciones antiguas o valores fuera de rango).</summary>
        public static void Limitar(ConfiguracionLanzadera c)
        {
            c.bodega = Math.Max(0, Math.Min(c.bodega, Bodegas().Count - 1));
            c.tanque = Math.Max(0, Math.Min(c.tanque, Tanques().Count - 1));
            c.refuerzos = Math.Max(0, Math.Min(c.refuerzos, Refuerzos().Count - 1));
        }

        public static string Resumen(ConfiguracionLanzadera c) =>
            $"{Bodegas()[c.bodega].nombre} · {Tanques()[c.tanque].nombre} · {Refuerzos()[c.refuerzos].nombre}";
    }
}
