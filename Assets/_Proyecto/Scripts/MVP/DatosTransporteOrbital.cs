using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    // =====================================================================================================================
    // TRANSPORTE ORBITAL NO TRIPULADO «VC-1» (envío orbital separado) — SOLO DATOS Y REGLAS.
    //
    // El envío orbital ya existía como TARIFA (10 M, BalanceEM4X.TarifaTransporteOrbitalMUSD) sin vehículo. Ahora tiene
    // vehículo propio, distinto de la L-4 y sin tripulación. La tarifa NO cambia: el vehículo, la animación y el
    // lanzamiento no se cobran aparte.
    //
    // Límites de DEMOSTRACIÓN (reglas de juego, no ingeniería validada), comprobados con lo que va en el envío:
    //  · Masa útil máxima: 34 000 kg (componentes de la EM-4X + combustible + carga pequeña que no va en la L-4).
    //  · Bodega principal (componentes): Ø 7,2 m × 23 m. Cada componente debe caber en el diámetro y la suma de los largos
    //    (más 0,3 m de separación antes, entre y después de cada pieza) en los 23 m. Con las piezas actuales TODAS caben: el
    //    caso más largo (tanque ampliado 13 + motor de alta eficiencia 5 + sensor de diagnóstico 2,1 + relevo 1,2 + 1,5 de
    //    separaciones) ocupa 22,8 m. Es la MASA la que limita los envíos más pesados.
    //  · Anillo de carga menor: hasta 12 módulos de combustible (uno por tramo de 3000 kg) y los contenedores pequeños.
    //
    // Las medidas de cada componente son las de su representación provisional a escala real (ModeloComponentes); el
    // holograma azul es esquemático y no se usa para medir. Si algo no cabe, el resumen tiene un PROBLEMA y la
    // preparación no se puede cerrar: no se despacha. Nunca se reduce una pieza ni se cambia un catálogo para que quepa.
    // =====================================================================================================================

    public struct MedidasTransporte
    {
        public float diametro, largo;
        public MedidasTransporte(float d, float l) { diametro = d; largo = l; }
    }

    public enum TipoComponente { Motor, Tanque, Instrumento, Relevo }

    public class ElementoTransporte
    {
        public string nombre;
        public TipoComponente tipo;
        public int indice;
        public float masaKg;
        public MedidasTransporte medidas;
        public bool cabeDiametro;
    }

    public class ResumenTransporte
    {
        public bool necesario;
        public readonly List<ElementoTransporte> componentes = new List<ElementoTransporte>();
        public int modulosCombustible;
        public float combustibleKg, cargaPequenaKg;
        public readonly List<string> contenedores = new List<string>();
        public float masaComponentesKg, masaTotalKg, largoUsadoM;
        public readonly List<string> motivos = new List<string>();
        public bool Cabe => motivos.Count == 0;
    }

    public static class DatosTransporteOrbital
    {
        public const string Nombre = "VC-1 · transporte orbital no tripulado";
        public const string NombreLanzador = "Lanzador provisional LC-2";
        public const float MasaMaximaKg = 34000f;
        public const float DiametroBodegaM = 7.2f;
        public const float LargoBodegaM = 23f;
        /// <summary>Separación antes, entre y después de las piezas de la bodega (la misma que usa ModeloTransporte).</summary>
        public const float SeparacionM = 0.3f;
        public const int ModulosCombustibleMax = 12;

        /// <summary>Medidas a escala real del motor (provisionales): tobera mayor con más empuje, más larga cuanto más eficiente.</summary>
        public static MedidasTransporte Motor(OpcionMotorNave o) =>
            new MedidasTransporte(4.5f * Mathf.Sqrt(o.empujeN / 400000f), 4f * (o.empujeN / Mathf.Max(1f, o.consumoKgS)) / 4000f);

        /// <summary>Tanque: Ø 4,6 m; largo proporcional a la capacidad (36 000 kg = 13 m, lo que cabe en la etapa de propulsión).</summary>
        public static MedidasTransporte Tanque(OpcionTanqueNave o) =>
            new MedidasTransporte(4.6f, 13f * Mathf.Clamp01(o.capacidadKg / 36000f));

        public static MedidasTransporte Instrumento(OpcionInstrumento o)
        {
            if (o.nombre.StartsWith("Cámara")) return new MedidasTransporte(1.1f, 1.5f);
            if (o.nombre.StartsWith("Espectr")) return new MedidasTransporte(1.7f, 1.5f);
            return new MedidasTransporte(1.1f, 2.1f);     // sensor de diagnóstico: esfera + mástil
        }

        public static MedidasTransporte Relevo(OpcionOrbita o) => new MedidasTransporte(1.2f, 1.2f);

        /// <summary>Calcula qué lleva el VC-1 y si cabe. «rl» dice qué no viaja en la L-4.</summary>
        public static ResumenTransporte Evaluar(PreparacionExpedicion p, ResumenExpedicion r, ResumenLanzadera rl, float masaSondaKg)
        {
            ResumenTransporte t = new ResumenTransporte();
            void Comp(TipoComponente tipo, int i, string nombre, float masa, MedidasTransporte m)
            {
                if (i <= 0) return;           // 0 = equipo ya instalado en órbita: no se compra ni se envía
                t.componentes.Add(new ElementoTransporte { tipo = tipo, indice = i, nombre = nombre, masaKg = masa, medidas = m,
                                                           cabeDiametro = m.diametro <= DiametroBodegaM + 0.001f });
            }
            Comp(TipoComponente.Motor, p.motor, r.motor.nombre, r.motor.masaKg, Motor(r.motor));
            Comp(TipoComponente.Tanque, p.tanque, r.tanque.nombre, r.tanque.masaVaciaKg, Tanque(r.tanque));
            Comp(TipoComponente.Instrumento, p.instrumento, r.instrumento.nombre, r.instrumento.masaKg, Instrumento(r.instrumento));
            if (r.relevo != null) Comp(TipoComponente.Relevo, p.relevo, r.relevo.nombre, r.relevo.masaReleKg, Relevo(r.relevo));

            if (rl != null && !rl.combustibleEnLanzadera && rl.combustibleEncargadoKg > 0f)
            {
                t.combustibleKg = rl.combustibleEncargadoKg;
                t.modulosCombustible = Mathf.RoundToInt(t.combustibleKg / BalanceEM4X.TramoCombustibleKg);
            }
            if (rl != null && !rl.cargaPequenaEnLanzadera && rl.cargaPequenaKg > 0f)
            {
                t.cargaPequenaKg = rl.cargaPequenaKg;
                if (p.suministrosExtra > 0) t.contenedores.Add("suministros");
                if (p.kitsExtra > 0) t.contenedores.Add("kits");
                if (p.sonda) t.contenedores.Add("sonda");
            }
            t.necesario = t.componentes.Count > 0 || t.modulosCombustible > 0 || t.contenedores.Count > 0;

            foreach (ElementoTransporte e in t.componentes) { t.masaComponentesKg += e.masaKg; t.largoUsadoM += e.medidas.largo + SeparacionM; }
            if (t.componentes.Count > 0) t.largoUsadoM += SeparacionM;
            t.masaTotalKg = t.masaComponentesKg + t.combustibleKg + t.cargaPequenaKg;
            if (!t.necesario) return t;

            foreach (ElementoTransporte e in t.componentes)
                if (!e.cabeDiametro)
                    t.motivos.Add($"{e.nombre} mide Ø {e.medidas.diametro:0.0} m y la bodega del VC-1 solo admite Ø {DiametroBodegaM:0.0} m.");
            if (t.largoUsadoM > LargoBodegaM + 0.001f)
                t.motivos.Add($"Los componentes ocupan {t.largoUsadoM:0.0} m de bodega y el VC-1 tiene {LargoBodegaM:0} m.");
            if (t.masaTotalKg > MasaMaximaKg + 0.5f)
                t.motivos.Add($"El envío pesa {t.masaTotalKg:0} kg y el VC-1 admite {MasaMaximaKg:0} kg (componentes {t.masaComponentesKg:0} + combustible {t.combustibleKg:0} + carga {t.cargaPequenaKg:0}).");
            if (t.modulosCombustible > ModulosCombustibleMax)
                t.motivos.Add($"El anillo de carga admite {ModulosCombustibleMax} módulos de combustible y hacen falta {t.modulosCombustible}.");
            return t;
        }
    }
}
