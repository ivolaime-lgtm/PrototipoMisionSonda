using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>Un lote de carga que hay que preparar en el hangar (solo lo que se envía desde tierra).</summary>
    public class LoteCarga
    {
        public string id, nombre, detalle;
        public int cantidad;
        /// <summary>Huella de lo elegido: si la selección cambia, la preparación de ESTE lote deja de valer.</summary>
        public string firma;
        public bool preparado;
    }

    /// <summary>
    /// PREPARACIÓN DE CARGA del hangar terrestre (solo lógica). No es otra configuración: los lotes se DERIVAN de la
    /// preparación de la expedición (ExpedicionActual / BalanceEM4X) y solo se guarda qué lotes se han preparado y con qué
    /// selección. Preparar no crea recursos ni cobra nada: confirma la carga ya presupuestada.
    /// </summary>
    public static class CargaHangar
    {
        static readonly Dictionary<string, string> preparados = new Dictionary<string, string>();
        /// <summary>El jugador eligió misión en la terminal (la elección en sí vive en la preparación).</summary>
        public static bool MisionElegida { get; set; }
        /// <summary>Se embarcó: al volver al hangar empieza una expedición nueva (selecciones conservadas, carga por preparar).</summary>
        public static bool Embarcado { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar()
        {
            preparados.Clear();
            MisionElegida = false;
            Embarcado = false;
        }

        public static void Vaciar() => preparados.Clear();

        /// <summary>Lotes que exige la preparación actual (los de cantidad 0 no aparecen).</summary>
        public static List<LoteCarga> Lotes(PreparacionExpedicion p, ResumenExpedicion r)
        {
            List<LoteCarga> l = new List<LoteCarga>();
            if (r == null || r.tanque == null) return l;
            int tramos = Mathf.Clamp(p.tramosCombustible, 0, BalanceEM4X.MaximoTramos(r.tanque));
            int sum = Mathf.Clamp(p.suministrosExtra, 0, BalanceEM4X.MaximoSuministrosExtra);
            int kits = Mathf.Clamp(p.kitsExtra, 0, BalanceEM4X.MaximoKitsExtra);
            void Lote(string id, string nombre, int n, string detalle, string firma)
            {
                if (n <= 0) { preparados.Remove(id); return; }   // al volver a pedirlo hay que prepararlo otra vez
                LoteCarga lo = new LoteCarga { id = id, nombre = nombre, cantidad = n, detalle = detalle, firma = firma };
                string f;
                lo.preparado = preparados.TryGetValue(id, out f) && f == firma;
                l.Add(lo);
            }
            Lote("combustible", "Combustible", tramos, $"{tramos} × {BalanceEM4X.TramoCombustibleKg:0} kg = {tramos * BalanceEM4X.TramoCombustibleKg:0} kg",
                 $"{tramos}|{p.tanque}");
            Lote("suministros", "Suministros", sum, $"{sum} unidades (+{sum * BalanceEM4X.SegundosPorSuministro / 60f:0} min de soporte vital)", sum.ToString());
            Lote("kits", "Kits de reparación", kits, $"{kits} kits (además de los {BalanceEM4X.KitsABordo} que ya hay a bordo)", kits.ToString());
            Lote("sonda", "Sonda de despliegue", p.sonda ? 1 : 0, "1 sonda (sonda base del catálogo)", "1");
            List<string> piezas = new List<string>();
            if (p.motor > 0 && r.motor != null) piezas.Add(r.motor.nombre);
            if (p.tanque > 0 && r.tanque != null) piezas.Add(r.tanque.nombre);
            if (p.instrumento > 0 && r.instrumento != null) piezas.Add(r.instrumento.nombre);
            if (p.relevo > 0 && r.relevo != null) piezas.Add(r.relevo.nombre);
            Lote("componentes", "Componentes de sustitución", piezas.Count, string.Join("\n", piezas),
                 $"{p.motor}|{p.tanque}|{p.instrumento}|{p.relevo}");
            return l;
        }

        /// <summary>Marca el lote como preparado con la selección actual. No cambia cantidades ni presupuesto.</summary>
        public static void Preparar(LoteCarga lo) { if (lo != null) preparados[lo.id] = lo.firma; }

        public static int Pendientes(List<LoteCarga> lotes)
        {
            int n = 0;
            foreach (LoteCarga lo in lotes) if (!lo.preparado) n++;
            return n;
        }
    }
}
