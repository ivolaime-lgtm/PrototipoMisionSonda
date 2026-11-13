using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrototipoMision
{
    /// <summary>Una baliza de la prueba: nombre, posición fija en el mundo (m) y color provisional.</summary>
    [Serializable]
    public class DefinicionBaliza
    {
        public string nombre = "Baliza";
        [Tooltip("Posición en el mundo (m). La nave empieza en (0, 0, 0) mirando a +z.")]
        public Vector3 posicionM;
        [Tooltip("Dirección aproximada vista desde la salida (solo texto de ayuda).")]
        public string direccion = "";
        public Color color = Color.cyan;
    }

    /// <summary>Cuerpo de referencia sin gravedad (V1): solo sirve para orientarse. Posición fija, radio en metros.</summary>
    [Serializable]
    public class CuerpoReferencia
    {
        public string nombre = "Cuerpo";
        public Vector3 posicionM;
        public float radioM = 1000f;
        public Color color = Color.gray;
    }

    /// <summary>
    /// Objetivo de la prueba V1 (SOLO LÓGICA): visitar balizas en orden y detenerse junto a cada una.
    /// «Detenerse junto» = estar a menos de «distanciaLlegadaM» con una velocidad de como mucho «velocidadMaximaMS»
    /// durante «permanenciaS» segundos SEGUIDOS (si se sale, se reinicia la cuenta).
    /// </summary>
    public class ObjetivoBalizas
    {
        [Serializable]
        public struct Ajustes
        {
            [Tooltip("Distancia a la baliza que cuenta como «junto a ella» (m).")]
            public float distanciaLlegadaM;
            [Tooltip("Velocidad máxima respecto a la baliza para contar como detenida (m/s).")]
            public float velocidadMaximaMS;
            [Tooltip("Segundos seguidos cumpliendo las dos condiciones.")]
            public float permanenciaS;
            [Tooltip("Índices (en la lista de balizas) que hay que visitar, en orden.")]
            public int[] orden;

            public static Ajustes PorDefecto => new Ajustes
            {
                distanciaLlegadaM = 50f, velocidadMaximaMS = 1f, permanenciaS = 2f, orden = new[] { 0, 1 }
            };
        }

        public enum Situacion { Lejos, DemasiadoRapido, Manteniendo, Completado }

        readonly Ajustes a;
        readonly List<DefinicionBaliza> balizas;

        public ObjetivoBalizas(Ajustes ajustes, List<DefinicionBaliza> balizas)
        {
            a = ajustes;
            this.balizas = balizas ?? new List<DefinicionBaliza>();
            Reiniciar();
        }

        public Ajustes Configuracion => a;
        public int Pasos => a.orden != null ? a.orden.Length : 0;
        public int PasoActual { get; private set; }
        public bool Completado => PasoActual >= Pasos;
        public float PermanenciaS { get; private set; }
        public float TiempoS { get; private set; }
        public float TiempoFinalS { get; private set; } = -1f;
        public Situacion SituacionActual { get; private set; }
        public float DistanciaM { get; private set; }
        /// <summary>Velocidad con la que la nave se acerca a la baliza (m/s; negativa si se aleja).</summary>
        public float AcercamientoMS { get; private set; }
        /// <summary>Velocidad total respecto a la baliza (m/s). Las balizas están quietas: es la rapidez de la nave.</summary>
        public float VelocidadRelativaMS { get; private set; }
        /// <summary>Parte de la velocidad que NO acerca ni aleja: desplazamiento lateral respecto a la baliza (m/s).</summary>
        public float LateralMS => Mathf.Sqrt(Mathf.Max(0f, VelocidadRelativaMS * VelocidadRelativaMS - AcercamientoMS * AcercamientoMS));

        public void Reiniciar()
        {
            PasoActual = 0;
            PermanenciaS = 0f;
            TiempoS = 0f;
            TiempoFinalS = -1f;
            SituacionActual = Pasos > 0 ? Situacion.Lejos : Situacion.Completado;
        }

        /// <summary>Índice de la baliza objetivo actual en la lista (−1 si no hay).</summary>
        public int IndiceActual
        {
            get
            {
                if (Completado) return -1;
                int i = a.orden[PasoActual];
                return i >= 0 && i < balizas.Count ? i : -1;
            }
        }

        public DefinicionBaliza Actual => IndiceActual >= 0 ? balizas[IndiceActual] : null;

        /// <summary>True si la baliza i ya se visitó en esta prueba.</summary>
        public bool Visitada(int i)
        {
            for (int p = 0; p < PasoActual && p < Pasos; p++)
                if (a.orden[p] == i) return true;
            return false;
        }

        /// <summary>Avanza el objetivo. Devuelve true en el fotograma en que se completa una visita.</summary>
        public bool Actualizar(Vector3d posicion, Vector3d velocidad, float dt)
        {
            if (Completado) { SituacionActual = Situacion.Completado; return false; }
            TiempoS += dt;
            DefinicionBaliza b = Actual;
            if (b == null) { PasoActual++; return false; }

            Vector3d hacia = Vector3d.De(b.posicionM) - posicion;
            double d = hacia.Magnitud;
            DistanciaM = (float)d;
            Vector3 dir = d > 1e-6 ? (hacia * (1.0 / d)).AVector3() : Vector3.zero;
            AcercamientoMS = Vector3.Dot(velocidad.AVector3(), dir);
            float rapidez = (float)velocidad.Magnitud;
            VelocidadRelativaMS = rapidez;

            bool cerca = DistanciaM <= a.distanciaLlegadaM;
            bool lenta = rapidez <= a.velocidadMaximaMS;
            if (cerca && lenta)
            {
                SituacionActual = Situacion.Manteniendo;
                PermanenciaS += dt;
                if (PermanenciaS >= a.permanenciaS)
                {
                    PasoActual++;
                    PermanenciaS = 0f;
                    if (Completado)
                    {
                        TiempoFinalS = TiempoS;
                        SituacionActual = Situacion.Completado;
                    }
                    else SituacionActual = Situacion.Lejos;
                    return true;
                }
            }
            else
            {
                PermanenciaS = 0f;
                SituacionActual = cerca ? Situacion.DemasiadoRapido : Situacion.Lejos;
            }
            return false;
        }
    }
}
