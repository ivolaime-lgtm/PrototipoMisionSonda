# Adaptación a requisitos

Guía corta para cambiar valores cuando lleguen los requisitos, **sin rehacer escenas ni buscar números por los scripts**. Versión del 14-11-2026 (incremento AH).

## Cómo funciona

- **Una sola fuente:** `Assets/_Proyecto/Resources/ConfiguracionJuego.asset` (clase `ConfiguracionJuego`, en `Scripts/MVP/ConfiguracionJuego.cs`). El hangar, las secuencias y la misión leen de ahí.
- **Sin archivo, nada cambia.** El juego usa los valores por defecto del código, que son los de la versión validada (AG).
- **Crear el archivo:** *Prototipo → Juego → Configuración → 1. Crear archivo de configuración (valores actuales)*.
  - El archivo nace con los valores actuales.
  - Si ya existe, no se sobrescribe.
  - Se edita en el Inspector y entra en el ejecutable automáticamente, porque está en `Resources`.
- **Comprobar:** *… → 2. Comprobar configuración*.
  - Avisa de choques con el escenario: altura de la L-4 frente a la grúa y la puerta, radio frente a las plataformas, VC-1 frente a la cofia.
  - Revisa también límites, opciones, misiones, planos de cámara y textos.
  - No cambia nada.
- **Misiones editables:** *… → 4. Copiar las tres misiones al archivo* (solo si hay que cambiar sus textos; ver la sección de misiones).
- **Listas vacías = valores del código.** Si una lista del archivo está vacía (opciones, misiones, textos, cámaras), se usan los del código.
- **Cambios en Play:** mientras se juega en el editor el archivo ya está cargado. Para ver un cambio, sal de Play y vuelve a entrar.

## Qué se puede cambiar, dónde y qué repetir

Todas las medidas van en **metros**, los tiempos en **segundos**, el dinero en **M USD**, las masas en **kg** y los ángulos en **grados**.

| Qué | Dónde (en `ConfiguracionJuego`) | Dependencias | Prueba que repetir |
|---|---|---|---|
| **Tarifas y costes:** envío orbital 10, instalación 3, lanzamiento de la tripulación 18, kit 1,5, suministro 1, tramo de combustible 1,2, máximos de extras 4 y 3 | `tarifas` | Presupuesto: `CatalogoMision.presupuestoMUSD`, en `Data/` | AG4 (total) y AE |
| **Límites del VC-1:** 34 000 kg, bodega Ø 7,2 × 23, separación 0,3, 12 módulos | `transporte` | Casco provisional Ø 7,6 × 32,5; bodega de 23 m; el anillo tiene sitio para 12 módulos | AG4, AH1 |
| **Opciones de la EM-4X:** motor y tanque, con coste, masa, empuje, consumo y capacidad | `opciones.motoresEM4X`, `opciones.tanquesEM4X` | Máximo 3 por tipo. El índice 0 es lo instalado. El tamaño de la pieza sale de empuje, consumo y capacidad (`DatosTransporteOrbital.Motor/Tanque`) | AE, AG3, AG4 |
| **Opciones de la L-4:** bodega, tanque de transferencia, refuerzos (coste, masa, capacidad, unidades) | `opciones.bodegasL4`, `tanquesL4`, `refuerzosL4` | Máximo 3. El índice 0 es la L-4 de siempre (coste 0, incluida en la tarifa) | AF, AG4 |
| **Instrumentos y órbitas del relevo** | Asset `Data/CatalogoMision` (ya editable) | Se comparten con otras escenas | AE |
| **Condiciones de las misiones:** salida 600, regreso 400 m y 2 m/s, proa ±15°, giro 1°/s, banda 24–28 km, K-2 300 m y 2 m/s, cúmulo 3–6 km y 5 m/s, T-7 400 m y 3 m/s | `misiones.condiciones` | Los textos de los pasos se recalculan solos si `misiones.lista` está vacía | Misiones 1–3 completas |
| **Nombres, resúmenes y pasos de las misiones** | `misiones.lista` (menú 4) | La lógica conoce las ids 0, 1 y 2 y comprueba los pasos por posición: no se pueden añadir pasos sin código. Con lista, los números de los textos se mantienen a mano | Misiones 1–3 |
| **Proporciones visuales de la L-4** | `lanzadera` | Grúa 40,4 m; puerta 42 m; plataformas a 4,7 m del eje; cabina interior 3,6 × 2,8 m. **No cambian masa, capacidad ni empuje** | AF, AG2, AH4–AH6 |
| **Proporciones visuales del lanzador LC-2** | `lanzadorVC1` | La cofia debe cubrir el VC-1 (32,5 m, Ø 7,6) | AH1 |
| **Secuencias:** activar u omitir partes, velocidad, duraciones, cuenta atrás | `secuencias` | Ver la sección de secuencias | AH7 |
| **Rótulos e indicadores** | `textos` (clave → texto) | Las claves están en `ConfiguracionJuego.TextosPorDefecto` | AH8 |
| **Planos de cámara** | `camaras` (id, ancla, posición, ancla del foco, foco, campo) | Ver la sección de cámaras | Plano correspondiente |
| **Puntos de servicio y atraque** | `puntos` | Ver la sección de puntos | AH2, AH3, AH5 |
| **Modelos y materiales** | Tabla `Data/RecursosVisualesEM4X` (entradas «Escenario» y de la EM-4X) | Puntos de sustitución: `GUIA_MODELADO_EM4X.md` §12–14 | AG8 |

### Secuencias (`secuencias`)

- **Partes que se pueden activar o desactivar:**
  - `mostrarEnvioVC1`
  - `mostrarTripulacionL4`
  - `mostrarEntradaABordo`
  - `mostrarSeparaciones` (cofia del VC-1, refuerzos y etapa de la L-4)
- Desactivar una parte **no cambia nada del estado**: costes, piezas instaladas y punto de inicio son los mismos.
- **Omitir:**
  - `omitirDesdeLaPrimeraVez` permite omitir cada parte desde la primera vez.
  - Por defecto solo se puede omitir después de haberla visto entera en la sesión.
  - Las teclas son Espacio o Intro.
- **Ritmo:**
  - `velocidad` acelera o frena todas las secuencias (por ejemplo, 2 = el doble de rápido).
  - Cada tramo tiene su duración: `vc1…`, `l4…` y `entradaABordo`.
- **Orden fijo:** envío del VC-1 e instalación, después la L-4, después la entrada a bordo. El orden no se configura.

### Puntos (`puntos`, en coordenadas de la EM-4X)

Ejes de la nave: proa en +Z, Ciencia hacia −X, Comunicaciones hacia +X, arriba en +Y.

- **Atraque de la L-4:**
  - `atraqueL4Desfase` se mide respecto al puerto «atraque» del Mando, que está en (0; −3; 11,7) según `NaveModular`.
  - La entrada a bordo pasa siempre por ese puerto.
  - `esperaL4M` es la distancia del punto de alineación bajo el puerto.
- **VC-1:**
  - `aparcamientoVC1` es la popa aparcada; el VC-1 mira hacia +Z con la bodega hacia +Y.
  - `esperaVC1` y `llegadaVC1` son relativos al aparcamiento.
  - `puertoServicioVC1` va en coordenadas del VC-1: popa en z = 0, babor en −x.
- **Plataforma de servicio:**
  - `vigaServicio` y `largoViga`
  - `bandejaRecepcion`
  - `bastidorRetiradas`
  - `tomaServicio`
  - `tomaCombustible`
- **Pasillo seguro:** `pasilloZ`, `pasilloY`, `pasilloX` y `pasilloTanqueY` definen el recorrido de las piezas por detrás de la nave y por fuera de los paneles solares (x 3,5–19; z −35,2 a −25,2).
- **Montajes:** `montaje…` y `giro…`.
  - El relevo va sobre el módulo de Comunicaciones, porque la punta del módulo la ocupa la antena.
- **Cuidado al mover estos puntos:** si un punto se acerca a la nave, una pieza o una manguera puede atravesar un casco. Mira la secuencia entera después de cada cambio.

### Cámaras (`camaras`)

- **Cada plano tiene:**
  - posición, con su ancla;
  - foco, con su propia ancla;
  - campo de visión.
- **Anclas:**
  - `EM4X`: ejes de la nave.
  - `Vehiculo`: el VC-1, el lanzador o la L-4 del plano, desplazado en ejes del mundo.
  - `Plataforma`: la plataforma de tierra del plano, o el punto en el espacio.
- **Ids que usa el juego:**
  - Envío: `vc1.salida`, `vc1.integracion`, `vc1.lanzamiento`, `vc1.espacio`, `vc1.lejos`, `vc1.espera`, `vc1.servicio`.
  - Instalación: `vc1.instalacion.popa`, `vc1.instalacion.ciencia`, `vc1.instalacion.comunicaciones`, `vc1.suministro`.
  - Tripulación: `l4.traslado`, `l4.ascenso`, `l4.espacio`, `l4.lejos`, `l4.alineacion`, `l4.atraque`.
- **Lo que no tiene plano configurable:**
  - Los planos interiores de la cabina y el embarque siguen a la cápsula.
  - El paso a bordo sigue al puerto del Mando.

## Lo que todavía está en el código (no configurable)

- **Distribución del hangar** (`HangarTerrestre.cs`, `Construir…`): zonas, consolas, soportes de C2 y altura de la cubierta.
  - Esto no se pidió cambiar.
  - Si un requisito lo exige, se mueve a la configuración con la misma técnica.
- **Medidas del escenario que comprueba el menú 2** (`ConfiguracionJuegoEditor.cs`, constantes de cabecera):
  - grúa a 42 m (`IntegracionLanzadera.AlturaPuente`);
  - plataformas de trabajo a 4,7 m del eje;
  - puerta de salida de 16 × 42 m.
  - Si cambian en el código, actualiza también esas constantes.
- **Masas, energía, daño, maniobra y combustible a bordo de la EM-4X:** `BalanceEM4X.cs`, constantes con comentario «DEMOSTRACIÓN».
- **Geometría de la EM-4X:** `NaveModular.cs`. No se toca.
- **Partes del VC-1 que siguen fijas:**
  - geometría del casco provisional (`ModeloTransporte`);
  - tamaño de cada componente (`DatosTransporteOrbital.Motor/Tanque/Instrumento/Relevo`).

## «Tubos» de la L-4: qué pieza es

Antes de cambiar nada hay que confirmar **qué pieza** se refiere el mecánico y **qué medida** quiere. Estas son las piezas cilíndricas que podrían ser los «tubos». Las medidas son las actuales, todas provisionales, en metros.

| # | Pieza (nombre en la jerarquía) | Diámetro × alto | Dónde | Campo en `lanzadera` |
|---|---|---|---|---|
| 1 | **Etapa principal** (`Etapa principal (pieza)`) | Ø 5 × 18 | Cuerpo central de abajo | `diametro`, `alturaEtapa` |
| 2 | **Propulsores de refuerzo** (`Propulsores de refuerzo × N`) | Ø 1,8 × 14, con morro de 1,2; eje a 3,55 m del centro | Laterales: 0, 2 o 4 según la opción | `diametroRefuerzo`, `alturaRefuerzo`, `separacionRefuerzo` |
| 3 | **Tanque de transferencia** (`Tanque de transferencia`) | Ø 5 × 3 (1 tramo) o × 5 (2 tramos) | Encima de la etapa | `diametro`, `alturaTanque` |
| 4 | **Bodega de carga** (`Bodega de carga`) | Ø 5 × 2,5 (pequeña) o × 4 (grande) | Encima del tanque | `diametro`, `alturaBodega` |
| 5 | **Toberas** de la etapa | Ø 3,2 × 1 | Base | `diametroToberas` |
| 6 | **Cápsula y adaptador** | Ø 4,4 × 3,2, más 1 de adaptador | Arriba | `diametroCapsula`, `alturaCapsula` |
| 7 | **Torre de escape** | Ø 0,5 × 3 | Punta (solo en tierra) | `alturaTorreEscape` |

Ojo: el `diametro` de la etapa también es el del tanque y el de la bodega.

**Otro «tubo» posible, que no es de la L-4:** el lanzador LC-2 del VC-1, con etapa central de Ø 8 × 40 y aceleradores de Ø 3,2 × 24 (`lanzadorVC1`).

**Altura total de la L-4:**
- de 26,5 m (sin tanque ni bodega) a 35,5 m (con tanque de 2 tramos y bodega grande);
- más la plataforma móvil, de 2 m.

**Si se cambia una proporción:**
- **Solo es visual.** La masa, la capacidad, el empuje y el combustible no cambian: están en `opciones.*L4`. Una proporción nueva no queda validada por cambiarla.
- **Lo que depende de ella:**
  - plataformas de trabajo a 4,7 m del eje (hoy el radio máximo es 4,45);
  - gancho de la grúa hasta 40,4 m;
  - cubierta: 44,8 m sobre los rieles y 52 m en el centro;
  - puerta de 16 × 42 m;
  - plataforma móvil de 12 × 12 m;
  - brazo de acceso de la torre (sigue a la cápsula solo);
  - encuadres de la consola B y de la secuencia (siguen la altura de cada pieza).
- **Después:** ejecuta el menú 2 y repite AF, AG2 y AH4.

**Pregunta pendiente para el mecánico:** ¿qué pieza de la tabla (número) y qué medida final (diámetro y/o alto)? Si tiene una referencia (plano, foto o vehículo de referencia con proporción), basta con esa referencia.
