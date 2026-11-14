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
        /// <summary>true = viaja en la lanzadera L-4 (se entrega en el muelle de integración); false = envío orbital.</summary>
        public bool enLanzadera;
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
            ResumenLanzadera rl = r.lanzadera;
            void Lote(string id, string nombre, int n, string detalle, string firma, bool enLanzadera)
            {
                if (n <= 0) { preparados.Remove(id); return; }   // al volver a pedirlo hay que prepararlo otra vez
                // El destino forma parte de la huella: si cambia la lanzadera y el lote cambia de vehículo, hay que volver a prepararlo.
                firma += enLanzadera ? "|L" : "|E";
                detalle += enLanzadera ? "\nviaja en la lanzadera L-4" : "\nviaja en el envío orbital";
                LoteCarga lo = new LoteCarga { id = id, nombre = nombre, cantidad = n, detalle = detalle, firma = firma, enLanzadera = enLanzadera };
                string f;
                lo.preparado = preparados.TryGetValue(id, out f) && f == firma;
                l.Add(lo);
            }
            Lote("combustible", "Combustible", tramos, $"{tramos} × {BalanceEM4X.TramoCombustibleKg:0} kg = {tramos * BalanceEM4X.TramoCombustibleKg:0} kg",
                 $"{tramos}|{p.tanque}", rl != null && rl.combustibleEnLanzadera);
            bool pequena = rl != null && rl.cargaPequenaEnLanzadera;
            Lote("suministros", "Suministros", sum, $"{sum} unidades (+{sum * BalanceEM4X.SegundosPorSuministro / 60f:0} min de soporte vital)", sum.ToString(), pequena);
            Lote("kits", "Kits de reparación", kits, $"{kits} kits (además de los {BalanceEM4X.KitsABordo} que ya hay a bordo)", kits.ToString(), pequena);
            Lote("sonda", "Sonda de despliegue", p.sonda ? 1 : 0, "1 sonda (sonda base del catálogo)", "1", pequena);
            List<string> piezas = new List<string>();
            if (p.motor > 0 && r.motor != null) piezas.Add(r.motor.nombre);
            if (p.tanque > 0 && r.tanque != null) piezas.Add(r.tanque.nombre);
            if (p.instrumento > 0 && r.instrumento != null) piezas.Add(r.instrumento.nombre);
            if (p.relevo > 0 && r.relevo != null) piezas.Add(r.relevo.nombre);
            Lote("componentes", "Componentes de sustitución", piezas.Count, string.Join("\n", piezas),
                 $"{p.motor}|{p.tanque}|{p.instrumento}|{p.relevo}", false);   // componentes: siempre envío orbital
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
