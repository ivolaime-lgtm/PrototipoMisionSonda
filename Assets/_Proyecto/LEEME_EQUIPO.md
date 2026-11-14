# Prototipo de misión — guía rápida del equipo

> **ENTRADA DEL EQUIPO (13-11-2026)**
> - **Para jugar:** menú *Prototipo → Juego → 3. Abrir HangarExpedicion (JUGAR)* → Play. Es la entrada del juego; al embarcar se pasa sola a ExpedicionMVP.
> - **MVP sin hangar (pruebas):** *Prototipo → Juego → 4. Abrir ExpedicionMVP*.
> - **Para importar modelos:** *Prototipo → Arte y pruebas → 4. Abrir ModeloPrueba (IMPORTAR MODELOS)* (si aún no existe: *Arte y pruebas → 2. Crear escena ModeloPrueba*).
> - **Ejecutable:** *Prototipo → Juego → 6. Generar el ejecutable completo (Windows)*.
> - **Zonas del hangar:** A · estación de configuración de la EM-4X (holograma con sus consolas alrededor) · B · integración de la lanzadera L-4 (edificio a la derecha, consola B) · C · carga (carro, estanterías, combustible) · C2 · transporte orbital no tripulado VC-1 (nave anexa, al fondo a la izquierda) · D · embarque (al fondo).
> - Los menús se reorganizaron: la tabla de equivalencias (números antiguos → nuevos) está en «Escenas del proyecto y menú Prototipo», al final.

Nombre del proyecto provisional: `PrototipoMisionSonda`.
Motor: Unity 6.3 LTS (6000.3.28f1) · URP · Windows · Input System (New).

## Qué hay hasta ahora

- **Incremento 1:** sonda en primera persona, estación que recarga, energía, interfaz mínima.
- **Incremento 2:** pantalla de configuración (objetivo, instrumento, batería, lanzador,
  órbita de relevo) con límites de presupuesto y de masa. La batería y la masa elegidas
  se aplican en la exploración.
- **Incremento 3:** escáner (tecla E), 3 puntos científicos con fichas, memoria de datos y
  transmisión desde la estación según la órbita elegida.
- **Centro de operaciones:** escena inicial con la estación orbital propia, una luna de fondo
  y el satélite de relevo. Solo visual: no cambia las mecánicas.
- **Incremento 4:** informe de resultados (éxito, parcial o fracaso) con explicaciones,
  puntuación de demostración y botones para reconfigurar o repetir.

## Menús de creación (Prototipo)

| Menú | Qué hace |
|---|---|
| 1. Crear escena de exploración | Crea `Scenes/Exploracion` (pregunta antes de sustituir) |
| 2. Crear escena de configuración | Crea `Data/CatalogoMision` (si no existe) y `Scenes/Configuracion` |
| 3. Conectar configuración a Exploracion | **Añade** el objeto AplicadorConfiguracion a Exploracion |
| 4. Añadir ciencia a Exploracion | Crea fichas y el prefab PuntoCientifico; **añade** SistemaCiencia y 3 puntos |
| 5. Crear resultados y conectar Exploracion | Crea `Scenes/Resultados`; **añade** ControlMision a Exploracion |
| 6. Crear centro de operaciones | Crea `Scenes/CentroOperaciones` y la pone primera en la compilación |
| 7. Ajustar encuadre del centro de operaciones | Mueve estación, luna, satélite y Sol a la disposición calculada. **No guarda**: Ctrl+S para guardar, Ctrl+Z para deshacer |
| 8. Crear escena Nave | Crea `Data/CatalogoNave` (si no existe) y `Scenes/Nave`, y la pone en la compilación después de Configuracion |

Orden del juego: **CentroOperaciones → Configuracion → Nave → Exploracion → Resultados**.
Si la escena Nave no existe o no está en la compilación, Configuracion va directamente a Exploracion.

Recursos externos: solo los de `Externos/NASA`. Sus créditos y condiciones están en
`CREDITOS.md`. **Antes de añadir otro recurso externo, regístralo ahí.**

Para jugar el recorrido completo: abre `Scenes/Configuracion` y pulsa **Play**.
Si pulsas Play directamente en Exploracion, se usan las primeras opciones del catálogo.

## Tareas sin programar

| Quiero… | Dónde |
|---|---|
| Cambiar presupuesto, costes, masas, baterías, instrumentos… | `Data/CatalogoMision` |
| Añadir una opción (p. ej. un tercer instrumento) | `Data/CatalogoMision` → botón **+** de la lista |
| Cambiar o añadir objetivos científicos | `Data/CatalogoMision` → lista **Objetivos** |
| Cambiar el texto o la imagen de una ficha | `Data/Fichas` → selecciona la ficha |
| Crear una ficha nueva | Clic derecho en `Data/Fichas` → **Create → Prototipo → Ficha científica** |
| Añadir un punto escaneable | Arrastra `Prefabs/PuntoCientifico` a la escena y asígnale una ficha |
| Mover un punto científico | Objeto **PuntosCientificos** de la escena → selecciona el punto y muévelo |
| Cambiar alcance del escáner o ventanas de contacto | Objeto **SistemaCiencia** → **Escaner** y **Sistema Datos** |
| Cambiar bonos de la puntuación | `Data/CatalogoMision` → **Puntuación del informe** |
| Cambiar esperas antes del informe o textos de fin | Objeto **ControlMision** de Exploracion |
| Cambiar títulos del informe | Escena Resultados → **PantallaResultados** |
| Cambiar textos de la pantalla inicial | Escena CentroOperaciones → **CentroOperaciones** |
| Mover la luna, la estación o el satélite | Escena CentroOperaciones; la velocidad está en **Movimiento Visual** |
| Cambiar el aspecto de la estación orbital | Doble clic en `Prefabs/EstacionOrbital` |
| Cambiar los consumos de la sonda o la recarga | `Prefabs/Sonda` → **Sistema Energia** |
| Cambiar velocidad, masa de referencia o ratón | `Prefabs/Sonda` → **Controlador Sonda** |
| Mover la estación | Selecciónala en la escena y muévela (herramienta Move, tecla W) |
| Cambiar el aspecto de la estación | Doble clic en `Prefabs/Estacion` |
| Cambiar textos de la interfaz | **Interfaz → Hud Minimo** y **PantallaConfiguracion** |
| Cambiar colores | Carpeta `Materials` → campo **Base Map** |
| Mover rocas o plantas | Objetos **Rocas** y **PlantasLuminosas** de la escena |

**Atención:** la capacidad de la batería ahora la decide el catálogo. El campo
*Capacidad Wh* de Sistema Energia solo se usa si falta el AplicadorConfiguracion.

Pasa el ratón sobre cada campo del Inspector para ver su explicación y sus unidades.

## Qué decisión afecta a qué

| Decisión | Efecto | Estado |
|---|---|---|
| Batería | Capacidad de energía (Wh) | APLICADO |
| Instrumento + batería | Masa de la sonda → aceleración | APLICADO |
| Todo | Coste total frente al presupuesto | APLICADO (bloquea) |
| Instrumento + batería + órbita | Carga frente a la capacidad del lanzador | APLICADO (bloquea) |
| Instrumento | Consumo, duración y MB de cada escaneo | APLICADO |
| Sonda base | Memoria: si se llena, no se puede escanear | APLICADO |
| Órbita | Velocidad de transmisión y ventanas de contacto | APLICADO |
| Objetivo | Escaneos que hay que transmitir | APLICADO |
| Objetivo + instrumento | Valor científico (×2 con el instrumento recomendado) | APLICADO |
| Todo | Informe final con explicación y puntuación | APLICADO |

El lanzador y la órbita **no** se simulan físicamente: solo cambian coste, masa y
velocidad de transmisión.

### Qué bloquea el botón INICIAR MISIÓN

1. Coste total mayor que el presupuesto.
2. Carga (sonda + satélite de relevo) mayor que la capacidad del lanzador.
3. Memoria menor que los datos de un solo escaneo del instrumento elegido.
4. Órbita sin enlace (velocidad o fracción de contacto en 0).
5. Objetivo con menos de 1 escaneo requerido.

Las reglas 3 a 5 solo pueden aparecer si se editan los valores del catálogo.
Avisos que **no** bloquean: instrumento no recomendado, datos previstos mayores que la
memoria (habrá que volver a transmitir) y escanear más de la mitad de la batería.

### Masa: qué cuenta dónde

- **Carga en el lanzador** = sonda + satélite de relevo. Se compara con la capacidad del lanzador.
- **Masa de la sonda** = estructura + instrumento + batería. Es la que cambia la aceleración
  en la exploración (referencia: 170 kg en *Controlador Sonda*).

### Avisos al editar valores

- La escala de tiempo está en dos sitios: *Minutos Mision Por Segundo* del catálogo (solo para
  estimaciones) y de *Sistema Energia* en el prefab Sonda (la que usa el juego). Deben
  coincidir; si no, la Console lo avisa al empezar la exploración.
- Si el objetivo pide más escaneos que puntos científicos hay en la escena, la Console y el
  panel de datos lo avisan: el objetivo sería imposible.
- El catálogo guardado aún no incluye los campos añadidos después de crearlo (consumo de
  transmisión, multiplicador del instrumento recomendado y bonos de puntuación). Unity usa
  sus valores por defecto (25 W, ×2, +10 y 0,5). Se ven en el Inspector al seleccionar el
  catálogo y se escribirán en el archivo la próxima vez que se modifique y guarde.

## Escena Nave (primera persona)

Nave tripulada **ficticia** y viaje **comprimido**: DEMOSTRACIÓN. Los escombros son un peligro
ficticio localizado, no una descripción del cinturón de asteroides.

Recorrido: **preparación → despegue → viaje con eventos → llegada y entrada en órbita → despliegue de la sonda**.

| Puesto | Qué se hace | Rol del modo equipo (para el futuro) |
|---|---|---|
| Pilotaje (frente, ventana) | Despegar; esquivar escombros con WASD; frenar para entrar en órbita (Espacio en la zona verde) | Lanzamiento |
| Ingeniería (izquierda) | Revisar los datos de la nave; reparar la avería: aislar circuito (1/2/3) → reserva (R) → restablecer (Espacio) | Ingeniería |
| Comunicaciones (derecha) | Recibir y confirmar las instrucciones del centro (Espacio) | Comunicaciones |
| Ciencia (fondo) | Revisar la sonda; desplegarla en órbita | Ciencia |

Controles: **WASD** caminar · **ratón** mirar · **E** usar el puesto que miras · **Q** salir del puesto ·
**Espacio** acción principal del puesto · clic para recuperar el ratón.
La luz de cada puesto indica: ámbar = tarea pendiente, roja = alarma, verde = correcto.

Consecuencias visibles: impactos (destello rojo, vibración, casco −25 %), avería (luces rojas,
pérdida de energía), combustible gastado en maniobras y frenada. La misión **se aborta** si el casco
llega a 0 %, si la energía de la nave llega a 0 o si no queda combustible para frenar.

**Datos de la nave separados de la sonda** (asset `Data/CatalogoNave`). Reglas de demostración:
1. La carga científica (sonda + relevo) viaja a bordo.
2. La capacidad sobrante del lanzador se llena con combustible extra (1 kg por kg).
3. Masa de la nave = masa seca + combustible + carga científica.
4. Aceleración de maniobra = empuje / masa.
5. Combustible para frenar = base (180 kg) × masa actual / masa de referencia, más hasta un 40 % si se frena fuera de tiempo.
La batería de la sonda sigue siendo solo de la sonda; la nave tiene su propia energía.

**Consecuencias del viaje** (evaluación al entrar en órbita, antes de desplegar; DEMOSTRACIÓN):

| Acción | Resultado | Consecuencia |
|---|---|---|
| Reparar la avería | energía de la nave perdida ≤ 25 % / ≤ 50 % / más | +5 / +2 / 0 puntos de viaje |
| Escombros | 0 impactos / 1 / 2 o más | +6 / +3 / 0 puntos de viaje |
| Separar la etapa (Pilotaje, durante el ascenso) | dentro de la ventana verde / antes / sin pulsar | +5 / 0 (temprana) / 0 (el ordenador la separa tarde). Siempre se continúa. Procedimiento simplificado: solo cuenta en la evaluación |
| Frenar para entrar en órbita | zona verde / fuera | **sin puntos**: la sonda empieza con hasta un 10 % menos de carga (regla simplificada: descenso prolongado). La capacidad máxima no cambia y la estación recarga igual |

Los puntos de viaje solo cuentan si la sonda se despliega. El presupuesto no cambia: se valida solo en
Configuracion y su bono se mantiene igual. Resultados muestra por separado ciencia, objetivo,
presupuesto, viaje y total. Umbrales y puntos: asset `CatalogoNave` (sección *Puntos de viaje*).

La sonda empieza en (0, 2, 0), **dentro** de la zona de recarga de la estación (esfera de 6 m):
la carga perdida en el descenso se recupera en unos 2 s si la sonda se queda allí (300 W de recarga).

**Separación de etapa**: el panel de Pilotaje la explica antes de la cuenta atrás. Durante el ascenso
aparece una barra; pulsa Espacio cuando el marcador esté en la zona verde (al menos 2,5 s, también en
modo de prueba). El ascenso espera a que se resuelva. El panel muestra el lanzador elegido, la carga y
el combustible extra. Ajustes en ControlNave: *Inicio Ventana Etapa*, *Ventana Etapa Minima S*, *Puntos Separacion*.

**Transición de llegada**: tras desplegar la sonda, tres tarjetas (llegada, sistema de acceso, descenso
al océano) con un corte esquemático animado. Espacio: siguiente · Intro: omitir. Aclara que el paso por
el hielo está simplificado. Es solo visual (`TransicionDescenso.cs`, la crea ControlNave en tiempo de
ejecución; no hay objetos nuevos en la escena). Se desactiva con *Mostrar Transicion* en ControlNave.

**Modo de prueba**: en el objeto ControlNave (escena Nave), casilla *Modo Prueba*. Usa los
*Tiempos Prueba* (cuenta atrás 3 s, viaje 40 s) en lugar de los *Tiempos Normales* (10 s, 150 s).
Los momentos de la avería y de los escombros también se editan ahí.

## Modo equipo (1 a 4 jugadores en el mismo PC)

Estado: código conservado; su ampliación está en pausa mientras se construye la nave.

En la pantalla de configuración, bloque **EQUIPO**:

- **Jugadores**: 1 a 4. Con 1 jugador todo funciona como antes.
- **Roles**: cada rol tiene un responsable (J1…J4) y se puede reasignar.
  | Rol | Decide | Prioridad (umbral en el catálogo, DEMOSTRACIÓN) |
  |---|---|---|
  | Ciencia | Objetivo e instrumento | Usar el instrumento recomendado |
  | Ingeniería | Batería | Margen tras escanear ≥ *Margen Energia Minimo Wh* (100) |
  | Lanzamiento | Lanzador | Reserva de presupuesto ≥ *Reserva Presupuesto Minima MUSD* (20) |
  | Comunicaciones | Órbita del relevo | Transmitir todo en ≤ *Tiempo Transmision Maximo S* (20 s) |
- Reparto inicial: 2 jugadores → J1 Ciencia + Comunicaciones, J2 Ingeniería + Lanzamiento;
  3 → J2 lleva Ingeniería y Lanzamiento; 4 → un rol cada uno.
- **Turno**: el jugador que tiene el teclado. Solo puede cambiar las secciones de sus roles.
- **Aprobaciones**: para iniciar, todos deben pulsar su botón. Cualquier cambio de opción,
  de jugadores o de reparto anula las aprobaciones.
- Las prioridades **no bloquean** el inicio. Con los valores actuales ninguna configuración
  válida cumple las cuatro a la vez: el equipo tiene que negociar cuál ceder.
- El informe final muestra, por rol, si el plan cumplía su prioridad y qué pasó de verdad.

Por qué en el mismo PC: en clase o en el evento el equipo comparte una pantalla y discute en
voz alta. El juego reparte la responsabilidad y obliga a llegar a un acuerdo; no hay red.

Archivos: `Scripts/Equipo/ModoEquipo.cs` (roles y prioridades), cambios en
`PantallaConfiguracion`, `ControlMision`, `InformeMision`, `PantallaResultados`,
`AplicadorConfiguracion` y tres campos nuevos en `CatalogoMision`.

## Controles

| Tecla | Acción |
|---|---|
| WASD / Espacio / Ctrl o C / Shift | Mover, subir, bajar, turbo |
| Ratón | Mirar |
| E (mantener) | Escanear el punto apuntado |
| F | Cerrar la ficha |
| T (en la estación) | Terminar la misión y ver el informe |
| M | Volver a la configuración |
| F9 | Mostrar u ocultar el medidor de rendimiento (FPS), en cualquier escena y en el ejecutable |
| Esc / clic | Liberar / recuperar el cursor |

**Probar la interfaz:** usa la vista Game con resolución fija 16:9 (por ejemplo 1920×1080) y
*Scale* en 1x. La interfaz se escala según la altura de la ventana: en ventanas muy bajas
(unos 500 px) los textos pequeños dejan de ser legibles.

**Pruebas de aceptación:** ver `PRUEBAS_ACEPTACION.md`.

## Unidades

- **M USD:** millones de dólares.
- **kg:** masa.
- **W (vatio):** cuánta energía se gasta en cada instante.
- **Wh (vatio-hora):** cuánta energía hay guardada. 1 Wh = 1 W durante 1 hora.
- **MB / MB/s:** cantidad de datos y velocidad de transmisión.
- **Datos recogidos:** guardados en la memoria de la sonda. **Datos transmitidos:** enviados al
  satélite de relevo desde la estación. Solo los transmitidos cuentan para el objetivo.
- **Escala de tiempo:** 1 segundo real = 1 minuto de misión (editable).

Todos los valores son de **DEMOSTRACIÓN**. No proceden de ninguna misión real.

## Licencias (pendiente de decidir y verificar)

| Tipo | Elemento | Estado |
|---|---|---|
| Código propio | `Scripts/` y `Editor/` | Licencia OSI por elegir (p. ej. MIT o Apache-2.0) |
| Dependencias del motor | Unity Editor, URP, Input System | Términos de Unity; confirmar cada paquete en el Package Manager |
| Recursos de terceros | Textura de Europa y modelo TDRS (NASA 3D Resources) | Ver `CREDITOS.md` |
| Datos científicos | Ninguno hasta ahora; las fichas son textos ficticios marcados DEMOSTRACIÓN | Si se añaden datos reales, anotar la fuente en cada ficha |
| Valores de demostración | Catálogo de misión, consumos, recarga, escala de tiempo | Marcados como DEMOSTRACIÓN |

Pendiente de verificar con la organización: si depender de un motor no OSI
(Unity) es aceptable según las reglas de entrega de NASA Space Apps 2026.

## Muestra visual: sector delantero de la nave (en evaluación)

Estilo: exploración de ciencia ficción, azul y turquesa con acentos naranja cálido. Formas, texturas y
materiales generados por el propio proyecto (sin recursos de otros juegos). Las formas simples siguen
siendo provisionales fuera de este sector.

- Aplicar: escena **Nave** abierta, fuera de Play → *Prototipo → Muestra visual → 1. Aplicar: sector delantero de la nave*. Después, Ctrl+S.
- Quitar: *… → 1. Quitar: sector delantero de la nave*. Restaura lo ocultado, la luz ambiental y el posprocesado de la cámara. Después, Ctrl+S.
- Todo cuelga de `VISUAL_MUESTRA_NAVE`; ninguna pieza tiene colisionador. Se ocultan (no se borran) la consola y la pantalla de pilotaje (sus colisionadores siguen) y los listones antiguos de la ventana.
- Recursos en `Assets/_Proyecto/Visual/` (texturas, materiales `MV_…`, perfil `PerfilCabina`). Se crean solo si no existen: se pueden retocar en el Inspector sin que el menú los sobrescriba.
- Scripts visuales (`Scripts/Visual/`): `PantallaDatosNave` (las pantallas muestran datos de ControlNave, solo lectura), `LucesReactivasVisual` (las tiras de luz siguen el color de las luces de alerta), `CapaVisual` (registro para quitar la muestra).

## Demostración tripulada (recorrido principal)

Configuracion → Nave (preparación, despegue, viaje, llegada) → **Resultados**. Al entrar en órbita y
confirmar el mensaje "Objetivo alcanzado", la misión termina. La exploración con la sonda queda como
fase posterior opcional (botón en Resultados).

Evaluación de la misión tripulada (DEMOSTRACIÓN, separada de la de exploración): llegada al destino
(+10, *Puntos Llegada* en ControlNave) + presupuesto sobrante (0,5 por M USD, solo si se llega) +
puntos de viaje (separación, reparación, escombros). **No incluye ciencia**: no se dan por hechos los escaneos.

El panel de la nave muestra siempre el destino y la distancia restante. La distancia es simulada
(*Distancia Simulada Mkm*): el progreso del viaje es guionizado.

Para volver al recorrido con exploración: desactiva *Terminar Al Llegar* en ControlNave.

## Selección de misión y piezas de la nave

- La primera vez, Configuracion muestra la **selección de misión** (una misión completa y dos "Próximamente").
  Datos en `CatalogoMision` → *Misión seleccionable*.
- **Motor de la nave** (sección 6): datos en `CatalogoNave` → *Motores* (el primero es el estándar). Coste al
  presupuesto; la masa **de más** sobre el estándar cuenta en el **margen de carga configurable** del lanzador
  (el vehículo tripulado base se considera incluido: simplificación). El empuje y el consumo se usan en las maniobras.
- **Tanque de la nave** (sección 7): datos en `CatalogoNave` → *Tanques*. Coste al presupuesto; la masa en vacío
  de más sobre el estándar cuenta en el margen de carga. Pone el **tope** del combustible.
- Masas de la nave: seca (*Masa Casco Kg* 5450 + motor + tanque vacío) + carga científica (sonda + relevo) +
  combustible = masa total inicial. Combustible = mínimo(capacidad del tanque, 300 base + margen sobrante, 1 kg por kg):
  el tanque no añade combustible por otra vía. La aceleración usa la masa total actual.
- `CatalogoMision` enlaza `CatalogoNave` (campo *Catalogo Nave*). Si falta, no se muestran piezas de la nave.
- Vista exterior: `Scripts/Visual/VistaNaveConfiguracion.cs`, creada en tiempo de ejecución (no hay objetos en la escena).

## Sectores de la cabina (versión reducida para la demostración)

`Scripts/Nave/SectoresNave.cs`, creado por ControlNave al empezar (casilla *Mostrar Sectores*). Rótulos, bordes
luminosos en el suelo, tabiques bajos y una luz por sector. No mueve puestos, vistas ni colisionadores y no añade
colisionadores. Sectores: 1 Pilotaje · 2 Ingeniería · 3 Comunicaciones y navegación · 4 Ciencia / control de misión.
Un jugador controla la nave; hay puestos para cuatro tripulantes (sin multijugador). La nave grande queda pendiente.

## Nave ampliada (menú 9) y restauración (menú 10)

Plano (vista desde arriba, z hacia la ventana; interior de 10,4 × 19 m, 3 m de alto):

```
              z=+5   ┌──────── VENTANA ────────┐
                     │      1  PILOTAJE        │  x ±4
              z=+1,5 └────────┐     ┌──────────┘
 ┌──────────────┐             │     │             ┌──────────────┐
 │ 4  CIENCIA / ├── puerta ───┤  P  ├── puerta ───┤ 3 COMUNIC. Y │  salas 4 × 4 m
 │ CONTROL      │             │  A  │             │ NAVEGACIÓN   │  (z −3,5 … +0,5)
 └──────────────┘             │  S  │             └──────────────┘
                              │  I  │  pasillo 2,4 m
              z=−9   ┌────────┘  L  └──────────┐
                     │      2  INGENIERÍA      │  x ±3,5
                     │  ═══ barandilla ═══     │
                     │  (tanque)     (motor)   │
              z=−14  └─────────────────────────┘
```

- **Aplicar**: escena Nave abierta, fuera de Play → *Prototipo → 9. Ampliar nave (4 sectores y pasillo)* → Ctrl+S.
  Copia la escena en `CopiasSeguridad/` antes de cambiarla. Si ya está ampliada, pregunta y la rehace.
- Desactiva (no borra) las paredes laterales y trasera originales; añade paredes, suelos y techos CON colisionador;
  mueve Ingeniería, Comunicaciones y Ciencia (con su vista); añade 4 luces a las alertas de ControlNave; sube el
  aviso de escombros del modo de prueba de 4 a 8 s. Pilotaje y la ventana no cambian.
- **Volver**: *Prototipo → 10. Volver a la cabina original* → Ctrl+S. Restaura todo lo anterior y borra NAVE_AMPLIADA.
- Escombros: se evalúan una vez en la proa (z = 5,3) y desaparecen; no atraviesan las salas.
- **No uses** el menú 8 (regenera la escena) ni la muestra visual antigua (pensada para la cabina pequeña).

## Acabado de la cabina de pilotaje (menú 11)

Capa visual reversible sobre la nave ampliada: *Prototipo → 11. Acabado: cabina de pilotaje (aplicar / quitar)*,
fuera de Play y con la escena Nave abierta; después Ctrl+S. Todo cuelga de `ACABADO_CABINA_PILOTAJE`, sin
colisionadores. Reutiliza los generadores de texturas y materiales (`Assets/_Proyecto/Visual/`, se crean la primera
vez) y la consola de `MuestraVisual`. Oculta (no borra) la consola y pantalla antiguas de pilotaje, los listones de
la ventana y el rótulo flotante "PILOTAJE". No cambia la luz ambiental; activa un posprocesado moderado.
La muestra antigua (*Muestra visual → 1.*) ya no se aplica sobre la nave ampliada.


## Despegue visto desde fuera

`CinematicaDespegue` (la añade ControlNave al empezar si *Mostrar Despegue Exterior* está activado; no está en la
escena). En los últimos 3 s de la cuenta atrás cambia a una cámara exterior: plataforma, encendido, humo, ascenso con
el cielo oscureciéndose, separación del lanzador (la orden sigue siendo Espacio en Pilotaje, sin cambios de reglas),
alejamiento con el planeta debajo y fundido de vuelta a la cabina. Intro vuelve antes. El decorado se crea lejos de la
cabina (y = −20 000) y se destruye al terminar. La nave usa el motor y el tanque elegidos con las mismas formas que la
vista de Configuracion. Planeta, altitud y velocidad son ficticios e ilustrativos (DEMOSTRACIÓN). Sonido generado por
código (sin archivos ni licencias): rumor del motor y golpe de separación; se puede sustituir más adelante por sonidos
con licencia verificada. Mientras dura la toma, HudNave oculta sus paneles laterales.

## Observación de aproximación (bonus aparte)

Durante el crucero se abre una ventana (por defecto entre el 40 % y el 56 % del viaje). En **Ciencia**, mantener
Espacio captura datos (UD) y gasta energía de la **nave** (21 Wh por UD; 12 UD como máximo ≈ 252 Wh, una cuarta parte
de la energía inicial de la nave, `CatalogoNave → energiaNaveWh` = 1000 Wh; la batería de la sonda no se toca). En
**Comunicaciones**, mantener Espacio envía los datos; solo cuentan los enviados antes de llegar. Da un bonus aparte de
0 a 6 que **no suma** a la puntuación principal (sobre 40) ni cambia sus criterios. La energía gastada en observar no
cuenta como pérdida de la avería.

- Lógica: `Scripts/Nave/ObservacionAproximacion.cs` (sin dibujo). HUD, paneles e informe solo leen su estado
  (`ControlNave.Observacion`), así que las pantallas de los modelos nuevos pueden mostrar lo mismo sin tocar la lógica.
- Ajustes en ControlNave → *Observacion*: activa, inicio y duración de la ventana (fracciones del viaje), máximo de UD,
  captura y envío por segundo, coste en Wh por UD y puntos del bonus. Valores provisionales de prueba.
- Desactivado: sin ventana, avisos, consumos ni interacción; el juego queda como antes.
- Pruebas: sección Q de PRUEBAS_ACEPTACION.

## Corrección de trayectoria antes del frenado (opcional)

Al final del crucero, con la avería reparada y los escombros terminados (desde el 85 % del viaje), se abre una ventana
de duración fija (22 s; 18 s en Modo Prueba). Delante aparece un anillo: el corredor de entrada, a 5–9 m de la
posición de la nave. En Pilotaje, con WASD (los mismos propulsores y el mismo gasto que con los escombros), hay que
centrarlo y permanecer 2 s seguidos dentro de 1,5 m. Si se completa en la primera mitad de la ventana, el marcador de
la inserción orbital va más lento (periodo 3,3 s); en la segunda mitad, 2,75 s; sin corregir, 2,2 s como siempre. No da
puntos ni cambia la puntuación principal ni el bonus de observación. La llegada espera a que termine la ventana.

- Lógica: `Scripts/Nave/CorredorAproximacion.cs` (sin dibujo). Vista: `Scripts/Nave/VistaCorredor.cs` (anillo de
  balizas que se coloca como los escombros: corredor menos desplazamiento lateral). Sustituible por un modelo.
- Ajustes en ControlNave → *Correccion* (todos provisionales). Desactivado: sin anillo, avisos, consumos ni demora.
- El combustible gastado en maniobras durante la ventana se mide y se muestra; no hay consumo nuevo.
- Pruebas: sección R de PRUEBAS_ACEPTACION.

## Navegación longitudinal (opcional, desactivada por defecto)

Con *ControlNave → Navegacion → Activa*, el crucero deja de ser un progreso guionizado: la nave sale del ascenso a
20 m/s hacia el destino, vuela por inercia (sin gravedad ni rozamiento) y el jugador acelera, gira y frena desde
**Pilotaje**. Desactivada, el juego es exactamente el de antes.

- **Mandos (solo en Pilotaje y solo en crucero):** Espacio mantenido = motor principal (empuja hacia donde apunta la
  proa); R = girar 180° (3 s, el motor queda bloqueado y la velocidad no cambia); WASD = maniobras laterales, como
  siempre. Espacio conserva la cuenta atrás, la separación y la inserción: al empezar el crucero hay que soltarlo antes
  de que encienda el motor, y en la llegada también. R sigue siendo la reparación en Ingeniería.
- **Velocidad cero:** si el motor frena la nave hasta 0, la velocidad se queda en 0 y el motor se apaga hasta soltar
  Espacio. La orientación nunca cambia sola: si se vuelve a encender, la nave se mueve hacia donde apunta la proa.
- **Unidades y escala:** metros, segundos, m/s y kg; tiempo real (sin compresión temporal). La ruta es **corta y
  comprimida** (8000 m; 2500 m en Modo Prueba): es una ruta de juego, no la distancia real a una luna, y la distancia
  decorativa de «M km» no se usa. Aceleración = empuje / masa, con el motor y el consumo del catálogo (unos 3,6 m/s² y
  2,5 kg/s con el motor estándar); no se ha cambiado ninguna masa, empuje, capacidad ni consumo.
- **Llegada:** zona de aproximación de ±250 m alrededor del final de la ruta. La inserción se autoriza dentro de la zona
  a ≤ 15 m/s y con la avería, los escombros y la corrección de trayectoria terminados. Si algo está pendiente, la nave
  **no se detiene**: se bloquea la autorización y el panel dice por qué. Si sale de la zona antes de la inserción, la
  inserción deja de estar disponible hasta volver. **Pasarse de largo** = salir de la zona por el lado del destino; la
  luna se dibuja al lado de la ruta, así que nunca se atraviesa.
- **Combustible:** tres categorías en el libro: maniobras laterales, motor principal e inserción. La inserción es el
  frenado de captura desde la zona y no depende de lo que se frenó antes con el motor principal (eso ya está cobrado).
- **Fallo:** la misión se aborta cuando la llegada **ya no puede completarse**: el combustible que queda no basta para
  llevar la nave a la zona a ≤ 15 m/s con el motor principal (si se aleja: frenar y volver a 1 m/s) **más** una
  inserción perfecta. El motivo da las cifras. No se aborta por llegar a 0 kg (siempre ocurre antes).
- **Eventos:** se colocan por la fracción **máxima** de ruta alcanzada; retroceder no repite avería, escombros,
  observación ni corrección.
- **Ruta viable de referencia** (cálculo con la regla del juego, no medido en Unity): acelerar 8 s hasta unos 50 m/s,
  volar por inercia, girar y frenar a 15 m/s antes de la zona → unos 160 s y unos 44 kg de motor principal; con la
  inserción (unos 195 kg) y las maniobras cabe en el tanque estándar de 350 kg.
- **Combustible de prueba:** *Navegacion → Combustible Prueba Activo* + *Combustible Prueba Kg* (240 por defecto).
  Solo para pruebas y solo con la navegación activada; no cambia el catálogo ni los tanques. El HUD y los Resultados lo
  marcan como «COMBUSTIBLE DE PRUEBA».
- **Vista:** esquema de la ruta abajo, marca del destino (en pantalla o en el borde; «DETRÁS» si queda a la espalda) y,
  al girar, todo el exterior (estrellas y luna) gira alrededor de la nave. Los escombros y el anillo de corrección
  siguen fijos a la proa (simplificación).
- Lógica: `Scripts/Nave/NavegacionLongitudinal.cs` (sin dibujo). Pruebas: sección S de PRUEBAS_ACEPTACION.

## Destinos configurables (11 de noviembre de 2026)

Los destinos están en *CatalogoMision → Destinos* (`Scripts/Mision/DestinoMision.cs`, solo datos). Cada uno define:
nombre, diferencia jugable (texto), aspecto provisional (tinte y tamaño del cuerpo de la ventana), longitud de la ruta
(normal y Modo Prueba), zona de aproximación, velocidad de captura y parámetros de la gravedad experimental. Se elige en
la tarjeta de la selección de misión (dos botones; cada partida va a un solo destino).

| | Luna helada (ficticia) | Planetoide denso (ficticio) |
| --- | --- | --- |
| Ruta | 8000 m (prueba 2500) | 11 000 m (prueba 3200) |
| Zona de aproximación | ±250 m | ±150 m |
| Captura | ≤ 15 m/s | ≤ 8 m/s |
| Gravedad experimental | caída 18 m/s, paso a 150 m, seguridad 100 m | caída 28 m/s, paso a 220 m, seguridad 160 m |
| Aspecto | el de siempre | anaranjado, 30 % mayor |

- La Luna conserva exactamente los valores de la navegación probada.
- La diferencia jugable se nota **con la navegación activada**. En el viaje clásico solo cambian el nombre y el aspecto.
- No cambian motores, masas, tanques, puntuación ni el coste de la inserción.
- *Navegacion → Usar Destino Elegido* (activado por defecto): desactivado, la ruta y la llegada salen del inspector,
  como en la versión probada.

## Gravedad experimental (un eje, desactivada por defecto)

*ControlNave → Navegacion → Gravedad Experimental*. Solo actúa con la navegación activada.

- **Qué representa:** la componente **a lo largo de la ruta** de la atracción de un cuerpo cuyo centro está a una
  distancia de paso *b* del final de la ruta (la ruta pasa a su lado): a(x) = −μ·x / (x² + b²)^(3/2), con x la posición
  respecto al punto de paso. μ se deduce de la «velocidad de caída»: la que ganaría la nave cayendo desde muy lejos
  hasta el punto de paso, v² = 2μ/b.
- **Qué no representa:** la componente lateral (se supone compensada), órbitas, asistencias gravitatorias, varios
  cuerpos ni escala real. No es una órbita: si no se inserta, la nave oscila a lo largo de la ruta alrededor del punto
  de paso.
- **Sin singularidad:** como b > 0, la atracción es finita en todas partes (máxima a |x| = b/√2, cero en el punto de paso).
- **Límite de seguridad:** la nave nunca se acerca al centro a menos de *b*, y *b* nunca es menor que el radio de
  seguridad del cuerpo (si se configura menor, se corrige y se avisa en la Consola). No puede llegar a la superficie.
- **Captura con gravedad:** cuenta la velocidad que la nave tendría en el borde de la zona con su energía actual (la
  gravedad la acelera hacia el centro, pero esa referencia no cambia sin motor, así que la autorización no parpadea).
- **Llegada imposible con gravedad:** se usa un mínimo teórico por energía (frenar donde la nave va más rápido). Puede
  avisar tarde, pero nunca aborta antes de tiempo.
- **Por separado:** Pilotaje muestra la aceleración de la gravedad y la del motor; el esquema dibuja la gravedad en
  azul; Resultados da el cambio de velocidad total de cada uno. La gravedad no gasta combustible.
- **Bloqueo para ir más lejos:** órbitas o asistencias gravitatorias necesitan mover la nave en dos o tres dimensiones.
  No se ha migrado; queda como incremento aparte.

## Claridad de la navegación (11 de noviembre de 2026)

Solo textos y dibujo; las reglas no cambian.

- Pilotaje empieza con «AHORA:», la acción recomendada en verde, ámbar o rojo, y después filas fijas: MOVIMIENTO
  (velocidad y si se acerca o se aleja), PROA (hacia dónde apunta y qué hace Espacio ahora: ACELERA o FRENA), MOTOR,
  GRAVEDAD (si está activa), LLEGADA y COMBUSTIBLE (mínimo para llegar y margen).
- Avisos únicos: «MOMENTO DE FRENAR» y margen de combustible por debajo de 30 kg y de 10 kg.
- Llegada bloqueada: dice qué la bloquea y cómo resolverlo, y aconseja detenerse dentro de la zona mientras tanto.
- La marca del destino no se pone debajo del panel del puesto y dice si la nave se acerca o se aleja.
- Resultados: desglose completo del combustible con navegación (al partir − maniobras − motor principal − inserción =
  al final, y si cuadra). Sin navegación, el informe es el de siempre.
- Para salir de la llegada hay un margen de 2 m y 0,5 m/s que evita el parpadeo de la autorización en el borde.

## Vuelo 3D · prueba V1 (escena VueloPrueba, 11 de noviembre de 2026)

Prueba aparte, sin gravedad. No usa ni modifica la escena Nave ni su lógica: el viaje actual sigue igual.

- **Crear la escena (una sola vez):** menú *Prototipo → 12. Crear escena VueloPrueba (vuelo 3D V1)*. Crea
  `Scenes/VueloPrueba.unity` con un único objeto `PruebaVuelo3D`; si ya existe, no la sustituye. No toca otras escenas
  ni la lista de compilación. Después: abrir `VueloPrueba` y pulsar Play.
- **Código** (`Scripts/Vuelo3D/`): `EstadoVuelo3D` y `ObjetivoBalizas` (solo lógica), `PruebaVuelo3D` (mandos y
  coordinación), `VistaVuelo3D` (mundo, nave y cámaras) y `HudVuelo3D` (indicadores). Todo se crea al pulsar Play.
- **Controles:** W/S cabeceo (W baja el morro) · A/D guiñada · Z/C alabeo · T estabilización asistida · I/K, J/L, U/O
  traslación · Espacio motor principal · V cámara (asiento o exterior) · botón derecho del ratón: mirar · H ayuda ·
  F5 reiniciar.
- **Valores del catálogo:** masa seca, combustible, empuje y consumo del motor principal (configuración elegida o, si
  la escena se abre sola, las primeras opciones).
- **Valores provisionales** (inspector de `PruebaVuelo3D`, *Maniobra*): traslación 1500 N y 0,6 kg/s por eje; giro
  8/8/12 °/s² y 0,08 kg/s por eje; límite de comodidad 45 °/s. No copian el motor principal.
- **Física simplificada:** sin gravedad ni rozamiento; empuje y consumo constantes; una aceleración angular fija por
  eje en lugar de par e inercia; posición y velocidad en doble precisión; la nave se dibuja en el origen y el mundo se
  coloca alrededor sin girarlo. Los cuerpos lejanos se dibujan a 9 km con su tamaño aparente real. Sin colisiones con
  las balizas; acercarse a menos de 300 m de la superficie de un cuerpo termina la prueba.
- **Objetivo:** Baliza 1 y después Baliza 2; detenerse a menos de 50 m, a 1 m/s o menos, durante 2 s seguidos.
  La Baliza 3 sirve de referencia detrás de la salida.

### Vuelo 3D V1 · frenado más accesible

- **Dos ayudas distintas:** *estabilización de giro* (T) solo frena la rotación; *frenado asistido* (B) frena el
  desplazamiento. El HUD lo dice en cada fila.
- **Guía:** la marca X («FRENAR: lleva la proa aquí») está en la dirección contraria al movimiento. El panel FRENADO da
  el desvío entre la proa y la X, si Espacio frena o aceleraría, y estimaciones de tiempo, distancia y combustible para
  parar con el motor o solo con traslación (empuje constante; el giro, desde parado).
- **Balizas:** distancia restante, velocidad total respecto a la baliza, acercamiento o alejamiento y velocidad lateral.
  Cuenta como detenida solo con la velocidad total.
- **Asistencia (B), opcional:** no pilota hacia ningún sitio, solo para la nave. Por encima de 2 m/s gira la proa
  progresivamente contra el movimiento (sin enderezar el alabeo) y frena con el motor; por debajo, frena con
  traslación de forma proporcional; a 0,05 m/s corta el empuje, frena el giro que quede y se apaga. Usa los mismos
  mandos que el jugador (respeta fuerzas, límite de giro y consumo) y muestra lo que gasta. B o cualquier mando la
  cancela; también se detiene sin combustible o a los 240 s. Ajustes en *PruebaVuelo3D → Frenado* (provisionales).
- Lógica: `Scripts/Vuelo3D/AsistenteFrenado.cs`. Pruebas: sección V de PRUEBAS_ACEPTACION.

## Hangar de prueba · nave base y bloque interior ampliado (escena HangarPrueba, 11 de noviembre de 2026)

Escena **aparte**: no cambia Nave, VueloPrueba ni Configuración, ni la lista de escenas del build. Vuelo 3D V1 **no**
está integrado aquí. Sin despegue, sin embarque completo, sin flotación, sin bots, sin red y sin gravedad exterior.

**Cómo abrirla:** menú *Prototipo → 13. Crear escena HangarPrueba* (solo la crea si no existe y le asigna
`Data/CatalogoMision.asset`) → abrir `Scenes/HangarPrueba.unity` → Play.

**Qué se ve:** una única base, *Expedición E-4* (cuatro tripulantes), en un hangar con marcas cada 5 m y una pasarela
provisional (el embarque no está implementado). La lista de bases admite más, pero las otras tres **no** están
definidas ni modeladas.

**Cambiar componentes:** botones ◀ ▶ del panel izquierdo (motor y tanque). Usan los catálogos y la validación de
siempre (`CatalogoMision.Calcular` + `CatalogoNave.Calcular`) y guardan la elección en `MisionActual`, la misma que leen
Configuración y Nave. Cada pieza va en su anclaje (`ANC_I2_Motor`, módulo de servicio tras `ANC_I3`), separada del casco;
al cambiar se destruye la anterior antes de construir la nueva (el panel muestra cuántas piezas hay en cada anclaje:
debe ser 1). No hay tercer módulo: la carga va en la bodega, que sigue pendiente.

**Masas:** el panel separa estructura (casco del catálogo), componentes instalados (motor, tanque vacío),
combustible y carga científica. **PROVISIONAL:** el bloque ampliado todavía no tiene balance de masa; se mantiene la masa
del casco del catálogo. No se ha cambiado ninguna masa, coste, capacidad ni empuje.

**Inspeccionar el interior:** **I** alterna exterior / cámara de inspección. La cámara de inspección es
**PROVISIONAL**: no tiene colisiones y **no** es la mecánica final de movimiento interior. WASD, Espacio/Ctrl, Mayús,
botón derecho para mirar; **1–4** sentado en Pilotaje, Ingeniería, Ciencia y Comunicaciones; **0** en la esclusa.
**C** corte (oculta techo y cubierta para ver el interior desde fuera); **L** colores de estado de las medidas
(verde conservada · azul propuesta · ámbar pendiente de prueba · naranja reserva); **H** ayuda.

**Tripulación:** seis figuras de 1,75 m (cuatro con el nombre de su rol junto a su asiento y dos maniquíes de medida en
el pasillo: uno de pie y otro en horizontal). Son **referencias de tamaño**, no bots ni jugadores conectados.

**Medidas del bloque (m; ejes: x a estribor, y arriba, z a proa; origen en la consola de referencia del diseño):**

| Elemento | Medida | Estado |
|---|---|---|
| Envolvente del casco | 20,3 × 11,6 m (z −14,8…5,5; x ±5,8) | Conservada |
| Tramos del casco | proa ±4,6 (z 1,3…5,5) · salas ±5,8 (z −4,5…1,3) · popa ±4,1 (z −14,8…−9) | Conservada |
| Tramo central | ±4,4 (z −9…−4,5), antes ±4,1 | Propuesta |
| Altura libre interior | 3,4 m (antes 3,0); casco y −1,0…3,9 (antes 3,7) | Propuesta |
| Pilotaje | x ±4,0, z 1,5…5,0; consola v0 en (0; 0; 3,30), 2,43 × 1,05 × 0,85; asiento z 2,95 | Conservada |
| Pasillo | 3,0 m de ancho libre (x ±1,5), z −9…1,5 (antes 2,4 m) | Propuesta |
| Comunicaciones / Ciencia | 3,7 × 4,8 m cada una (x ±1,7…±5,4, z −4,1…0,7) | Propuesta |
| Puertas interiores | 2,0 × 2,4 m | Propuesta |
| Esclusa provisional | 2,3 × 2,5 m (x 1,7…4,0, z −7,6…−5,1); escotilla exterior 1,4 × 2,0 a estribor | Pendiente de prueba |
| Reserva de carga interior | 2,3 × 2,5 m (x −4,0…−1,7, z −7,6…−5,1), acceso abierto 2,5 m | Reserva |
| Ingeniería | x ±3,7, z −14,2…−9,2 | Conservada |
| Puestos Ingeniería / Ciencia / Comunicaciones | consola sustituta 0,8 m de fondo; asiento a 0,9 m de ella | Propuesta |
| Mamparo I4 | z −14,8, Ø 4,0 | Conservada |
| Bodega | Ø 4,0 × 2,5 (z −14,8…−17,3) | Reserva (pendiente) |
| Módulo de servicio | Ø 4,0, largo = tanque + 1,0 (4,2 m; 5,4 m con el ampliado) | Conservada |
| Motor | envolvente Ø 3,2 × 2,5 en `ANC_I2_Motor` (z −21,5 / −22,7) | Conservada |
| Unión al lanzador | 3,0 m tras el motor | Reserva (pendiente) |

Planta y perfil: documento de equipo «Nave base E-4 · planta y perfil del bloque provisional». **No** encargar acabado
definitivo hasta validar el espacio en esta escena.

**Archivos:** `Scripts/Hangar/NaveBase.cs` (datos de la base, sin visual), `PruebaHangar.cs` (configuración y teclas),
`VistaHangar.cs` (solo visual, bloques y materiales simples), `HudHangar.cs` (paneles), `Editor/CrearEscenaHangarPrueba.cs`
(menú 13). Pruebas: sección W de `PRUEBAS_ACEPTACION.md`.

## Nave de expedición modular EM-4X · bloque provisional (escena ExpedicionPrueba, 12 de noviembre de 2026)

Escena **aparte** para recorrer y juzgar tamaño, distribución y sensación de espacio. **No** está integrada en la
partida y **no** es una nave con balance de vuelo: no usa los catálogos ni la masa del casco de la E-4 y no cambia
masas, motores, combustible ni límites del lanzador. Sin atraque, ensamblaje, lanzamientos, gravedad, bots ni red.
HangarPrueba, Nave, VueloPrueba, Configuración, la lista de escenas del build y los paquetes no se han tocado.
Copia previa: `CopiasSeguridad/Copia_antes_EM4X_2026-11-12.zip` (junto con `Copia_funcional_V1_frenado_2026-11-11.zip`).

**Cómo abrirla:** menú *Prototipo → 14. Crear escena ExpedicionPrueba (nave modular EM-4X)* (solo la crea si no
existe) → abrir `Scenes/ExpedicionPrueba.unity` → Play. Todo se construye al pulsar Play.

**Controles (cámara de inspección PROVISIONAL, sin colisiones; no valida por sí sola el movimiento real de personajes):**
- Exterior: botón derecho del ratón para girar alrededor, rueda para acercar.
- **I**: exterior / inspección. En inspección: W/S/A/D, Espacio/Ctrl (según la cámara), **Q/E** girar sobre sí misma,
  botón derecho para mirar, Mayús rápido, **R** nivelar.
- **1** Pilotaje · **2** Ingeniería · **3** Ciencia · **4** Comunicaciones · **0** esclusa · **5** zona común · **6** lanzadera.
- **C** corte (horizontal, longitudinal, transversal por el nodo, sin corte) · **T** rótulos · **X** esquema de
  conexiones (naranja con escotilla, gris estructural) y recorridos (rojo) · **H** ocultar paneles.

**Distribución:** nodo central de Ø 10 m (zona común con mesa y pantalla de misión) con Mando/Pilotaje a proa,
Ingeniería a popa, Ciencia a babor, Comunicaciones a estribor, Hábitat (4 camarotes) arriba y Bodega abajo. Detrás de
Ingeniería, un nodo de popa con Sistemas arriba, Esclusa abajo, anclaje de la sonda y puerto de suministro. Etapa de
propulsión solo exterior. La lanzadera L-4 (bloque) atraca bajo el mando. «Arriba» y «abajo» son referencias de la nave.

**Medidas resultantes (PROVISIONALES):** 57,9 × 29,0 × 25,2 m (largo con lanzadera y motor · ancho con antena ·
alto). Módulos habitables: 6,0 m exteriores, 5,4 interiores, paso libre 3,8 × 3,8 (octógonos de bloques; diámetros
entre caras). Escotillas 1,4 × 1,4; atraque 1,0 × 1,0. Unos 2263 m³ presurizados sin la lanzadera. Recorridos entre
puestos de 17,3 a 22,4 m. La escena muestra la revisión de distribución (solapes, conexiones, escotillas, equipos).

**Datos:** todo es provisional de DEMOSTRACIÓN. Pendientes: balance de masa, propulsión y combustible, recursos y
energía, transporte de los módulos a la órbita, atraque. Las figuras de 1,75 m son referencias, no bots ni jugadores.
No encargar acabado definitivo hasta recorrer el bloque y aceptar medidas. Documento de equipo: «EM-4X · bloque
provisional de la nave de expedición» (planta, perfil, sección, esquema y tablas); copia del plano en
`Documentacion/em4x_planos.png`.

**Archivos:** `Scripts/Expedicion/NaveModular.cs` (datos), `RevisionDistribucion.cs` (revisión, volúmenes,
recorridos), `VistaExpedicion.cs` (geometría), `InspeccionExpedicion.cs` (cámara y teclas), `HudExpedicion.cs`
(paneles), `PruebaExpedicion.cs` (entrada), `Editor/CrearEscenaExpedicionPrueba.cs` (menú 14). Pruebas: sección X de
`PRUEBAS_ACEPTACION.md`.

## Dirección del juego y preparación (decisiones registradas el 12 de noviembre de 2026)

**Estado de la EM-4X:** aspecto general, tamaño y distribución aceptados como base de desarrollo. No es una nave
técnicamente terminada. Siguen pendientes: balance de masa (no vale la masa de 5450 kg del casco anterior), propulsión y
combustible, recursos y energía, transporte y ensamblaje de los módulos en órbita (el **nodo central de 10 m no tiene
transporte resuelto**: se conserva para probar la jugabilidad; su construcción o transporte es una decisión pendiente),
atraque y acabado definitivo.

**Flujo de partida (dirección, no implementado todavía):**
1. Preparación de la expedición antes del despegue terrestre.
2. Lanzamiento de la tripulación en la lanzadera.
3. Llegada a la EM-4X, ya ensamblada en órbita.
4. Salida de la expedición y cumplimiento de objetivos.
5. Regreso de la EM-4X a la base orbital.
6. Retorno de la tripulación a superficie cuando corresponda.
Misiones tripuladas de ida y vuelta como regla principal, con uno o varios objetivos intermedios. Una sonda puede quedarse
en el destino mientras la tripulación regresa. El ensamblaje con varios lanzamientos es contexto del mundo, no una campaña.

**Preparación, piezas y presupuesto:**
- La EM-4X se configura antes del lanzamiento terrestre en una **vista de preparación orbital** que la muestra en órbita.
  Desde tierra se encargan los cambios; la nave grande **no** está en el hangar terrestre.
- El **hangar terrestre es de la lanzadera**. Ambas vistas pueden estar en la misma fase, pero siempre se indica qué
  vehículo y qué lugar se muestra. HangarPrueba se conserva como prototipo recuperable; no se convierte en un hangar que
  contiene la EM-4X.
- Las piezas nuevas se transportan e instalan en órbita; al principio, con costes y una transición (sin jugar los vuelos
  de carga).
- El presupuesto distingue: equipamiento ya instalado · compras y sustituciones · combustible y suministros · transporte
  orbital (solo de lo que se envía desde tierra) · instalación y preparación · lanzamiento de la tripulación. No se cobra
  como nueva una pieza ya instalada.
- La configuración elegida es exactamente la que se encuentra al llegar y la que usa la misión. Durante la planificación
  se comparan opciones; al confirmar, queda **cerrada**. Cambiarla después es explícito y recalcula costes y requisitos
  sin duplicar cobros. Durante el viaje no se compran ni cambian motores o tanques desde ese menú (reparaciones y repuestos
  son otra mecánica).
- La validación futura contempla la misión completa, con el regreso.
- La lanzadera tiene configuración fija por ahora, con datos y recursos separados de los de la EM-4X.
- Se reutilizan configuración, catálogos, validación y presupuesto existentes donde sirvan, sin cambiar sus reglas en silencio.

**Integración que necesitará (más adelante, por etapas):** separar en los datos «instalado» de «pedido»; partidas nuevas
del presupuesto (transporte orbital, instalación, lanzamiento de la tripulación); estado «configuración cerrada»; datos
propios de la lanzadera; vista de preparación orbital; validación con regreso; balance de la EM-4X.

**Siguiente y aplazado**
- Ahora: pasadizos, puertas básicas acordadas y movimiento interior de un personaje con colisiones (escena
  CirculacionPrueba, escrita; pendiente de la prueba del equipo).
- Después, por etapas: preparación y configuración → vuelo → objetivos → regreso.
- Se conservan sin integrar todavía: VueloPrueba (vuelo y frenado), HangarPrueba, ExpedicionPrueba.
- Aplazado: separación o eyección de módulos, presión y oxígeno, atraque funcional, despliegue de la sonda, arte
  definitivo, bots y multijugador.
- Arte en paralelo: el modelador puede avanzar con dirección visual y piezas independientes usando la EM-4X como
  referencia; los accesos y conexiones siguen provisionales hasta comprobar la circulación. Sin encargo de acabado definitivo.

## Circulación interior: personaje, pasadizos y puertas (escena CirculacionPrueba, 12 de noviembre de 2026)

Escena **aparte** para probar un personaje provisional con colisiones dentro de la EM-4X aceptada. Reutiliza sin
modificarla la EM-4X (añade el componente `PruebaExpedicion` al iniciar: mismos datos, geometría, revisión, cámara de
inspección y panel). No toca ExpedicionPrueba, HangarPrueba, Nave, VueloPrueba, Configuración, la lista de escenas del
build ni los paquetes. Copia previa: `CopiasSeguridad/Copia_antes_circulacion_2026-11-12.zip` (y la de la EM-4X aceptada).

**Cómo abrirla:** menú *Prototipo → 15. Crear escena CirculacionPrueba (personaje, pasadizos y puertas)* → abrir
`Scenes/CirculacionPrueba.unity` → Play.

**Controles del personaje (con colisiones):** ratón mirar (gira el cuerpo; sin arriba global) · W/S/A/D moverse ·
Espacio/Ctrl subir/bajar según la vista · Z/X girar sobre sí mismo · Mayús rápido · R nivelar con el «arriba» del
módulo · **E** usar el panel de una puerta · T rótulos · F5 volver a la zona común · F1 ocultar el panel · clic para
recuperar el ratón. **Tab**: cámara de inspección (sin colisiones; herramienta aparte, con sus teclas de siempre).

**Pasos acordados (12-11-2026):**
- Abiertos, sin puertas: Nodo central ↔ Mando, Ingeniería, Ciencia, Comunicaciones y Hábitat; Ingeniería ↔ Nodo de popa;
  Nodo de popa ↔ Sistemas.
- Con puertas: Mando ↔ Lanzadera y Nodo central ↔ Bodega (una puerta a cada lado de la unión, anillo amarillo de unión
  separable); Nodo de popa ↔ Esclusa (puerta interior).
- Cerrados o sin paso: escotilla exterior de la esclusa (cerrada, sin interacción), puerto de suministro (cerrado),
  reserva de la sonda (volumen exterior), etapa de propulsión (unión estructural).
- Intención futura (sin función ni botones): lanzadera separable, bodega intercambiable en la base orbital, etapa de
  propulsión sustituible.

**Puertas provisionales:** hojas deslizantes dentro de cada módulo, junto a su escotilla; se abren en 1,2 s y quedan
fuera del corredor de la escotilla. Todas las puertas de una unión se mueven juntas desde cualquiera de sus paneles
(hay un panel en cada lado), así nunca hace falta cruzar una puerta cerrada para alcanzar el control. Protección: no
cierran si hay alguien en la zona de las hojas o entre las dos puertas, y si alguien entra mientras cierran, vuelven a
abrir. **Ajuste necesario:** en el mando la cara del suelo mide 2,24 m y una hoja por lado no cabe abierta (faltan
7 cm); se usa una puerta **telescópica** (dos hojas por lado), que cabe con 17 cm de margen. No se ha cambiado ninguna
medida de la nave.

**Personaje:** cápsula de 1,75 × 0,5 m tumbada a lo largo de la mirada (se pasa de cabeza por las escotillas), con
Rigidbody sin gravedad. De `JugadorNave` se reutiliza la forma de usar con E (rayo desde los ojos) y la captura del
ratón; su CharacterController no sirve porque su cápsula siempre queda vertical. Al soltar las teclas se frena solo
(ayuda provisional de la prueba). Indica el módulo en que estás, lo que tocas y si te bloqueas.

**Comprobaciones automáticas (no son la prueba del equipo):** revisión de distribución de la EM-4X (sin cambios);
revisión de pasos, puertas y paneles sobre los datos; y, al iniciar, comprobación con las colisiones reales (pasadizos
libres, puertas que bloquean cerradas y dejan pasar abiertas, cierres al exterior, recorridos a los puestos y módulos, y
paneles alcanzables). El panel debe mostrar 0 problemas en las tres.

**Provisional:** todo el aspecto de puertas, paneles y personaje; el frenado automático; la comprobación de colisiones
no cubre todos los giros del cuerpo. Sin presión, oxígeno, separación, atraque, sonda, bots ni red. Pendientes: balance
de masa, propulsión, recursos, transporte orbital (nodo de 10 m incluido), atraque y acabado.

**Archivos:** `Scripts/Circulacion/DatosCirculacion.cs` (pasos, puertas, paneles y su geometría),
`RevisionCirculacion.cs` (revisión de datos), `ComprobacionPasos.cs` (comprobación con física), `ColisionesExpedicion.cs`
(colisiones sobre los bloques visibles), `PuertasUnion.cs` (puertas y paneles), `PersonajeFlotante.cs` (personaje),
`HudCirculacion.cs` (panel), `PruebaCirculacion.cs` (entrada y cambio de modo), `Editor/CrearEscenaCirculacionPrueba.cs`
(menú 15). Pruebas: sección Y de `PRUEBAS_ACEPTACION.md`.

## Ajuste de espacio: pasadizos permanentes más amplios (12 de noviembre de 2026)

Ajuste nuevo y acotado (no corrige un fallo). Se aplica a la **EM-4X común**: ExpedicionPrueba y CirculacionPrueba
muestran la misma nave. Copia recuperable previa: `CopiasSeguridad/Copia_antes_pasadizos_amplios_2026-11-12.zip`
(datos compartidos, scripts de Expedicion y Circulacion con sus .meta, menús 14 y 15, las dos escenas, NaveBase.cs,
plano y documentos).

- **Pasadizos permanentes** (Nodo central ↔ Mando, Ingeniería, Ciencia, Comunicaciones y Hábitat; Ingeniería ↔ Nodo de
  popa): **2,4 × 2,4 m libres, 1,2 m de largo**, paredes de 0,2 m (2,8 m por fuera). **Nodo de popa ↔ Sistemas:
  2,2 × 2,2 m** (la cara del nodo de popa no admite más sin agrandarlo).
- Huecos de los módulos: de 1,4 a 2,4 m (2,2 hacia Sistemas), del mismo tamaño que el pasadizo, sin escalón.
- **Sin cambios:** uniones con puertas (Mando ↔ Lanzadera, Nodo central ↔ Bodega, Nodo de popa ↔ Esclusa) con sus
  escotillas, adaptadores de 0,6 m y puertas; cierres al exterior y elementos sin paso.
- **Desplazamientos:** Mando (con la lanzadera), Ingeniería, Ciencia, Comunicaciones y Hábitat, 0,6 m hacia fuera; nodo
  de popa, Sistemas, Esclusa, propulsión y paneles, 1,2 m hacia popa; Sistemas además 0,6 m hacia arriba. La Bodega no
  se mueve. Las puertas y los paneles van fijados a su módulo y se mueven con él.
- **Medidas resultantes:** 59,7 × 30,2 × 25,8 m (largo con lanzadera y motor · ancho con antena · alto); 56,2 m de largo
  sin la lanzadera; 28,4 m de ancho de módulos. Unos 2293 m³ presurizados sin la lanzadera.
- **Recorridos:** Pilotaje → Ingeniería 23,6 m; Ciencia → Comunicaciones 20,3 m; Pilotaje → Ciencia 19,7 m; Pilotaje →
  Comunicaciones 19,2 m; Ciencia → Ingeniería 19,0 m; Comunicaciones → Ingeniería 18,5 m.
- **Revisión ampliada:** además de lo anterior, comprueba que cada pasadizo cabe en la cara o tapa del módulo y deja
  pared a los lados del hueco, y que ninguna conexión invade otro módulo, un elemento exterior u otra conexión. El
  número de comprobaciones cambia; lo que importa es que siga en 0 problemas. En CirculacionPrueba, la comprobación al
  iniciar también recorre cada pasadizo amplio pegado a sus paredes y esquinas.
- Plano actualizado: `Documentacion/em4x_planos.png`.
- Siguen pendientes: balance de masa, propulsión y combustible, recursos y energía, transporte y ensamblaje (nodo de
  10 m incluido), atraque y acabado.

## Puertas de aislamiento normalmente abiertas (12 de noviembre de 2026)

Sustituye la idea de limitar las puertas a los módulos separables: un módulo permanente también puede tener una puerta
para aislarlo en una emergencia. Copia previa: `CopiasSeguridad/Copia_antes_puertas_aislamiento_2026-11-12.zip`.

- **Una puerta por conexión permanente** (Nodo central ↔ Mando, Ingeniería, Ciencia, Comunicaciones y Hábitat;
  Ingeniería ↔ Nodo de popa; Nodo de popa ↔ Sistemas). Empiezan **abiertas** y siguen así en el uso normal; se cierran y
  abren a mano con **E** desde un panel a cada lado. Mismo mecanismo que las demás puertas (hojas que se deslizan,
  colisiones, protección de cierre).
- **Dónde van:** en el nodo central (hacia Mando y hacia Ingeniería), en el nodo de popa (hacia Ingeniería) y en la tapa
  de entrada de Ciencia, Comunicaciones, Hábitat y Sistemas. Ciencia, Comunicaciones y Hábitat llevan puertas
  **telescópicas** (dos hojas por lado): una hoja por lado no cabe abierta en esas tapas, y en las caras del nodo chocaría
  con la pantalla de misión y las taquillas. **No se ha cambiado ninguna medida** ni se ha estrechado ningún pasadizo:
  abiertas, las hojas quedan fuera del paso de 2,4 m (2,2 m hacia Sistemas).
- Se conservan las puertas de Lanzadera, Bodega y Esclusa. Escotilla exterior de la esclusa y suministro: cerrados.
  Reserva de la sonda: exterior.
- **Intención futura (no implementada):** impacto o avería → alarma → salida de la tripulación → aislamiento del módulo →
  reparación o decisión de regreso. Aislar no es separar ni repara nada. Sin impactos nuevos, presión, oxígeno,
  descompresión, cierres automáticos, daños estructurales, separación ni reparaciones nuevas; las averías existentes no
  cambian.
- **Revisiones:** la de datos comprueba que cada pasadizo tiene su puerta y un panel a cada lado, que las hojas caben,
  no chocan con equipos ni con otras puertas y abiertas no invaden el paso. La de colisiones al iniciar comprueba que
  cerradas bloquean, abiertas dejan pasar (también pegado a paredes y esquinas) y que los paneles se alcanzan; después
  deja las de aislamiento abiertas.
- Archivos cambiados: `Scripts/Circulacion/DatosCirculacion.cs`, `RevisionCirculacion.cs`, `ComprobacionPasos.cs`,
  `PuertasUnion.cs`, `HudCirculacion.cs`. Pruebas: sección AA de `PRUEBAS_ACEPTACION.md`.

---

## MVP de la expedición (13-11-2026) — escena ExpedicionMVP

**Estado: ESCRITO y revisado por lectura. NO compilado ni ejecutado por quien lo escribió (no hay Unity ni compilador en su entorno). Ninguna prueba está superada hasta que el equipo la confirme.**

Copia previa: `CopiasSeguridad/Copia_antes_MVP_2026-11-13.zip`.

### Cómo abrirlo
1. Menú **Prototipo → 16. Crear escena ExpedicionMVP (juego completo)**. Crea la escena nueva y tres materiales en `Materials/MVP`. No toca otras escenas, paquetes ni la lista del build.
2. Abrir `Scenes/ExpedicionMVP` y pulsar Play.
3. Ejecutable: **Prototipo → 18** compila solo esa escena en `Ejecutable/ExpedicionEM4X/`. El menú **17** (opcional y explícito) pone ExpedicionMVP como primera escena del build y guarda la lista anterior en `CopiasSeguridad/`.

### Qué hay
Portada → preparación con presupuesto → tres mensajes (envío e instalación, lanzamiento de la tripulación, a bordo) → interior de la EM-4X → puestos → regreso → resultados → nueva expedición.

### Archivos (propiedad y separación)
| Archivo | Qué es |
|---|---|
| `Scripts/MVP/BalanceEM4X.cs` | DATOS: balance provisional, tarifas, preparación, presupuesto, definición de las tres misiones |
| `Scripts/MVP/SistemaKora.cs` | DATOS y LÓGICA del sistema ficticio: cuerpos, órbitas, gravedad, predicción |
| `Scripts/MVP/EstadoExpedicion.cs` | LÓGICA: estado compartido (vuelo, recursos, ciencia, daños, progreso) |
| `Scripts/MVP/JuegoMVP.cs` | INTEGRADOR: fases, modos, mandos, puntos de interacción |
| `Scripts/MVP/VistaVueloMVP.cs` | VISUAL del pilotaje |
| `Scripts/MVP/DetalleInterior.cs` | VISUAL del interior (rótulos, kits, alarmas), sin colisiones |
| `Scripts/MVP/HudMVP.cs` | PANTALLAS |
| `Editor/CrearEscenaExpedicionMVP.cs` | Menús 16, 17 y 18 |

Cambios en archivos existentes (mínimos y compatibles): `Vuelo3D/EstadoVuelo3D.cs` añade `VelocidadReferencia` (cero por defecto) y `Vuelo3D/AsistenteFrenado.cs` frena la velocidad relativa a esa referencia. Con referencia cero (VueloPrueba) el cálculo es el mismo que antes. Nave, HangarPrueba, Configuración, CirculacionPrueba, ExpedicionPrueba, catálogos y paquetes: sin cambios.

### Interior y vuelo: un solo estado
`EstadoExpedicion` es el único estado. El interior (la EM-4X construida en el origen) y la vista de pilotaje son dos representaciones suyas. Al dejar Pilotaje (Q) el vuelo **sigue**: motor apagado, la nave continúa su órbita y las ayudas activas (estabilización, orientación automática, frenado asistido) siguen trabajando y gastando combustible; el panel del interior lo indica. La aceleración del tiempo (×4, ×16) solo existe en Pilotaje y sin mandos. La vista de pilotaje dibuja el exterior real de la EM-4X y gira el resto del sistema alrededor.

### Sistema ficticio «Kora» (DEMOSTRACIÓN, escala reducida)
Unidades: m, s, kg. Planeta en el origen, órbitas en el plano XZ. Atracción hacia el centro del planeta y de la luna, a = mu/d². Paso fijo de 0,02 s (salto de rana).

| Cuerpo | Radio | mu (m³/s²) | Órbita |
|---|---|---|---|
| Kora (planeta) | 10 km | 6,0·10⁸ | — |
| Nix (satélite natural) | 3 km | 8,0·10⁶ | 200 km, circular |
| Base orbital | — | no atrae | 30 km, 141 m/s, 22 min |
| Satélite K-2 | — | no atrae | 31,5 km, 12° por delante al empezar |
| Cúmulo Tesel (35 asteroides) | 30–140 m | no atraen | 34 km, 25° por delante al empezar |

Base, K-2 y el cúmulo van «sobre raíles» en su órbita circular (no notan la luna). La nave sí: sin motor se separa de la base unos 300 m por órbita. No son distancias ni masas reales.

### Balance provisional de la EM-4X (DEMOSTRACIÓN)
- Estructura 96 000 kg (no usa los 5450 kg del casco antiguo).
- Motor estándar EM-4X: 3000 kg, 400 kN, 100 kg/s. Alto empuje: 3600 kg, 560 kN, 155 kg/s.
- Tanque estándar: 2500 kg vacío, 24 000 kg de capacidad. Ampliado: 3600 kg, 36 000 kg.
- Los nombres y precios del motor y tanque no estándar se leen de `CatalogoNave` (8 y 6 M). Sus empujes y masas no sirven para una nave de 100 t, así que las versiones EM-4X están en `BalanceEM4X` con la misma estructura. `CatalogoNave` no se modifica.
- Maniobra: 60 kN y 20 kg/s por eje; giro 6/6/8 °/s², 1,5 kg/s por eje.
- Energía: batería 12 kWh, paneles 4 kW, consumo base 3,6 kW; buses de ciencia 0,8, comunicaciones 0,6 y servicio 1,5 kW. Instrumento y transmisión: consumo del catálogo × 100. Regla de tiempo del catálogo (1 s de vuelo = 1 min de misión).
- Soporte vital: 20 min de vuelo por suministro (2 a bordo). Kits: 1 a bordo, 40 kg cada uno.
- Daño por velocidad relativa: < 2 m/s roce; 2–4 abolladura; 4–10 brecha; 10–25 brecha grave (protege un bus o reduce paneles); ≥ 25 catastrófico. 1,6 % de casco por m/s. Contra Kora o Nix, desde 4 m/s es catastrófico.
- Con la configuración por defecto (18 t de combustible): 120 t, 3,3 m/s², ≈ 650 m/s de cambio de velocidad.

### Presupuesto (100 M del catálogo, sin cambiarlo)
Tarifas de demostración en `BalanceEM4X`: transporte orbital 10 M (fijo, una vez, solo si se envía algo), instalación 3 M por componente sustituido, lanzamiento de la tripulación 18 M, tramo de 3000 kg de combustible 1,2 M, suministro 1 M, kit 1,5 M. Sonda: precio de la sonda base del catálogo (30 M). Instrumentos y relevos: precios del catálogo.

Ya instalado y sin coste: motor y tanque estándar, Cámara multiespectral, Relevo en órbita baja, 6000 kg de combustible, 1 kit, 2 suministros.

- Ejemplo A (misión 1 por defecto): combustible 12 t 4,8 + kit 1,5 + transporte 10 + tripulación 18 = **34,3 M**.
- Ejemplo B (misión 3 con sonda y Espectrómetro): 34,3 + sonda 30 + Espectrómetro 28 + instalación 3 = **95,3 M**.

El presupuesto es previsto mientras se planifica; se calcula entero cada vez. Confirmar cierra la configuración; reabrir la recalcula sin acumular.

**Lanzador de la sonda.** En el código existente `OpcionLanzador` es transporte desde tierra (capacidad de carga para sonda y relevo, y coste). En el flujo orbital ese papel lo cubre el cargo fijo de transporte, así que el MVP no pide lanzador y no lo cobra. No se ha renombrado ni tocado: la escena Configuración lo sigue usando igual.

**Informe.** `InformeMision` y `PantallaResultados` están atados a la sonda submarina y a la escena Exploración; el MVP muestra sus propios resultados en `HudMVP` y no los modifica.

### Pendiente
- Compilación, ejecución y pruebas del equipo (sección AB de PRUEBAS_ACEPTACION).
- Validación de pasadizos (Z) y puertas de aislamiento (AA): siguen pendientes.
- Balance: es provisional; las estimaciones de combustible salen de un modelo en Python con guiado idealizado, no de partidas reales.
- Transporte y construcción del nodo de 10 m: sin resolver.
- Sin EVA, atraque físico, vuelos de carga, red ni varios jugadores.

---

## Arte de la EM-4X: sustitución visual y guía de modelado (13-11-2026)

**Estado: ESCRITO y revisado por lectura. No compilado ni ejecutado por quien lo escribió.**

Copia previa: `CopiasSeguridad/Copia_antes_sustitucion_visual_2026-11-13.zip` (JuegoMVP, menú 16, documentos y escena ExpedicionMVP).

- **Guía para modeladores:** `Documentacion/GUIA_MODELADO_EM4X.md`, con las láminas `em4x_vistas_actuales.png` y `em4x_modulo_ciencia.png`. Las medidas se generan leyendo `NaveModular.cs` y `DatosCirculacion.cs`. Hay copia compartible en Claude Docs: «EM-4X · guía de modelado».
- **Punto de sustitución:**
  - `Scripts/ArteEM4X/RecursosVisualesEM4X.cs` contiene los datos: la tabla de modelos.
  - `Scripts/ArteEM4X/SustitucionVisualEM4X.cs` es solo visual. Coloca cada modelo sobre la pieza provisional; esta deja de dibujarse (`forceRenderingOff`) pero conserva su colisión.
  - Se aplica DESPUÉS de colisiones, puertas y detalle. Quita las colisiones o la física que traiga el modelo.
  - Si falta un modelo, se ve lo provisional.
- **Prueba de importación:** `Scripts/ArteEM4X/PruebaModelos.cs`, en la escena ModeloPrueba. Reutiliza CirculacionPrueba sin cambiarla.
  - Teclas: F2 informe · F3 modelos/provisional · F4 ir a Ciencia · F12 capturas en `Capturas/`.
- **Menús** (`Editor/HerramientasArteEM4X.cs`):
  - **19** crea las carpetas `Assets/_Arte/…` y la tabla `Data/RecursosVisualesEM4X.asset`, y la asigna a la escena abierta.
  - **20** crea la escena ModeloPrueba.
  - **21** comprueba lo entregado y escribe `Informes/ComprobacionArte_<fecha>.txt`.
  - Ninguno sustituye escenas ni archivos existentes.
- **Cambios mínimos:**
  - `JuegoMVP` tiene un campo nuevo `recursosVisuales` y aplica la sustitución tras construir el detalle.
  - `DetalleInterior` expone su raíz.
  - El menú 16 asigna la tabla si existe.
  - Lógica, física, balance y misiones sin cambios.
- **Riesgo de sobrescritura:** la EM-4X se construye al pulsar Play a partir de los datos y los modelos viven en `_Arte`, así que recrear una escena no los toca. Los menús antiguos 8 y «Muestra visual» siguen siendo peligrosos para la escena Nave: no usarlos.
- **Limitación:** el modo «corte» de la cámara de inspección no recorta los modelos importados, solo la geometría provisional.
- **Hangar terrestre:** todavía no implementado. Se hará después de corregir los fallos que encontréis en el MVP, en una escena nueva e independiente; HangarPrueba no se toca.

---

## Hangar terrestre jugable (13-11-2026) — escena HangarExpedicion

**Estado: ESCRITO y revisado por lectura. No compilado ni ejecutado por quien lo escribió.** Las pruebas del MVP y de modelos siguen pendientes.

Copia previa: `CopiasSeguridad/Copia_antes_hangar_terrestre_2026-11-13.zip` (BalanceEM4X, JuegoMVP, HudMVP y documentos).

**Cómo abrirlo:** menú **Prototipo → 22. Hangar terrestre: crear o abrir la escena HangarExpedicion**. La crea si no existe y si ya existe solo la abre. Después, Play.

**Recorrido:** entrada → 1 Misión → 2 Configuración orbital (con el holograma de la EM-4X) → 3 Carga (manifiesto, estanterías y zona de envío) → 4 Revisión final → 5 Embarque. Desde ahí se pasa a ExpedicionMVP, que empieza en los mensajes de transición y lleva al interior de la EM-4X con esa configuración. Al terminar la misión hay un botón «Volver al hangar terrestre».

**Datos: no hay una segunda configuración.**
- Las elecciones son la misma `PreparacionExpedicion` de `ExpedicionActual` que usa el MVP. Presupuesto, tarifas, problemas y avisos salen de `BalanceEM4X.Calcular`.
- La carga (`CargaHangar`) se deriva de esa preparación: combustible, suministros, kits, sonda y componentes de sustitución.
  - Preparar un lote solo lo marca como listo con la selección actual. No crea recursos ni se cobra.
  - Si cambia la selección de un lote, solo ese lote vuelve a quedar por preparar.
- **Cerrar la preparación:** con la preparación cerrada no se puede cambiar nada. Reabrirla conserva las selecciones.
- **Embarque:** exige misión elegida, configuración válida, presupuesto suficiente, carga preparada y preparación cerrada.
- **Acceso directo al MVP:** sin cambios. Si abres ExpedicionMVP directamente, sale la portada y la preparación de siempre.

**Controles:** W/A/S/D caminar · ratón mirar · Mayús correr · E usar · Esc libera el cursor (o cierra la terminal). Siempre visibles en pantalla.

**Archivos:**
- Nuevos:
  - `Scripts/HangarTerrestre/HangarTerrestre.cs`, `CaminanteHangar.cs`, `CargaHangar.cs` y `HudHangarTerrestre.cs`;
  - `Scripts/MVP/CargadorEscenas.cs`;
  - `Editor/CrearEscenaHangarExpedicion.cs`.
- Modificados:
  - `Scripts/MVP/BalanceEM4X.cs`: solo `ExpedicionActual`, con borrador, embarque desde el hangar y origen.
  - `Scripts/MVP/JuegoMVP.cs`: entrada desde el hangar y vuelta al hangar.
  - `Scripts/MVP/HudMVP.cs`: botón de volver al hangar.
- Sin cambios: EM-4X, vuelo, gravedad, balance, HangarPrueba, escena de entrada, lista del build y paquetes.

**Cambio de escena sin tocar la lista del build:** en el editor se carga por ruta. En un ejecutable hará falta añadir HangarExpedicion y ExpedicionMVP a la lista; si faltan, el juego lo dice en pantalla en lugar de fallar.

**No incluido en esta versión:** lanzadera detallada, despegue, grúas, cajas con física, vuelos de carga y modelos del equipo de arte.

## Preparación dinámica: alternativas, holograma, carro, combustible y despegue (13-11-2026)

Sobre el hangar validado (AD1–AD12, copia `CopiasSeguridad/Copia_funcional_hangar_validado_2026-11-13.zip`). No cambia la EM-4X, el vuelo, la gravedad ni el balance de vuelo. Estado: **escrito y revisado por lectura; sin compilar ni ejecutar en Unity.** Pruebas: sección AE de `PRUEBAS_ACEPTACION.md`.

### Tres alternativas por pieza (valores efectivos de la EM-4X · DEMOSTRACIÓN)
Los catálogos originales (`CatalogoNave`, `CatalogoMision`) no se tocan; las alternativas se definen en `BalanceEM4X`. Presupuesto (100 M) y tarifas (transporte 10 M, instalación 3 M por componente, tripulación 18 M) sin cambios.

| Pieza | Alternativa | Datos | Precio |
|---|---|---|---|
| Motor | Estándar (instalado) | 400 kN · 100 kg/s · 3000 kg | 0 |
| Motor | Alto empuje | 560 kN · 155 kg/s · 3600 kg | del catálogo (8 M si falta) |
| Motor | **Alta eficiencia (nuevo)** | 280 kN · 56 kg/s · 3400 kg | 10 M |
| Tanque | Estándar (instalado) | 24 000 kg · 2500 kg vacío | 0 |
| Tanque | Ampliado | 36 000 kg · 3600 kg vacío | del catálogo (6 M si falta) |
| Tanque | **Ligero (nuevo)** | 15 000 kg · 1500 kg vacío · máx. 3 tramos | 3 M |
| Instrumento | Cámara multiespectral / Espectrómetro | los del catálogo, sin cambios | catálogo |
| Instrumento | **Sensor de diagnóstico (nuevo)** | 10 W×factor · 40 MB · captura 6 s · valor 3 · 12 kg | 10 M |

El sensor de diagnóstico es recomendado (×2) en la inspección del satélite K-2 además del espectrómetro; en las otras misiones vale ×1 y aparece el aviso. Las incompatibilidades (por ejemplo, el tanque ligero limita los tramos de combustible, o un instrumento cuyos datos no caben) se muestran como aviso o problema antes de encargar.

### Holograma interactivo (estación 2, centro del hangar)
E delante de la mesa → vista propia del holograma (botón derecho + arrastrar o flechas: girar; rueda fuera del panel: acercar; Esc: salir). Pestañas MOTOR / TANQUE / INSTRUMENTO resaltan la pieza; la lista muestra sus tres alternativas con datos, utilidad y advertencias. Pulsar una alternativa la **previsualiza** con geometría provisional y una animación de separación y encaje (no cambia nada); la comparación muestra coste, Δv, aceleración, masa y combustible frente a la actual. «ENCARGAR ESTE CAMBIO» aplica el mismo cambio que la terminal 2. Es una previsualización: la EM-4X sigue en órbita. Distribución y medidas de los módulos, sin cambios (escala 1:15 de `NaveModular`).

### Carga con carro (reutilizable) y puesto de combustible
- Cuatro lotes, un contenedor cada uno: suministros, kits, sonda y componentes de sustitución (solo si su cantidad es mayor que 0).
- Carro junto a las estanterías (base en x −12, z 24): E en el asa para empujarlo; delante de una estantería, E fija el contenedor al carro; E en otro sitio lo suelta. Al entrar en la zona amarilla de envío el lote queda **LISTO** y el contenedor se coloca allí.
- No crea carga ni cobra: confirma lo presupuestado. Si cambias la selección, solo el lote afectado vuelve a su estantería (también si iba en el carro).
- **R** devuelve el carro a su base, vacío (si se atasca o se pierde).
- **Puesto 3b · Combustible** (derecha, z 30): 1) verificar alimentación, 2) autorizar el llenado de los tramos ya presupuestados. Marca el lote de combustible como preparado; sin él no se puede embarcar.

### Despegue (lanzadera L-4, terrestre, no pilotable)
Al embarcar con todo listo: acceso por la pasarela, vista desde el asiento, cierre de compuerta, comprobación, cuenta atrás, encendido y ascenso, fundido y llegada al interior de la EM-4X (orbital). Tras verla una vez en la sesión, **Espacio o Intro** la omiten: omitirla solo acorta la animación (los requisitos ya se comprobaron, la preparación confirmada y los costes no cambian). Si la escena no se pudo abrir, se vuelve al hangar sin embarque pendiente.

### Archivos
Nuevos: `Scripts/HangarTerrestre/HologramaInteractivo.cs`, `CarroCarga.cs`, `SecuenciaDespegue.cs`, `Editor/MenuEscenas.cs`.
Modificados: `HangarTerrestre.cs`, `HudHangarTerrestre.cs`, `CaminanteHangar.cs` (factor de velocidad), `MVP/BalanceEM4X.cs` (alternativas, `Variante`, `Recomendado`, embarque con despegue), `MVP/HudMVP.cs` (lista de instrumentos y texto de llegada), `Editor/CrearEscenaExpedicionMVP.cs` (ejecutable), rutas de menú en todos los `Editor/*.cs`.

## Escenas del proyecto y menú Prototipo (13-11-2026)

Revisado: escenas en `Assets/_Proyecto/Scenes`, rutas en código (`CargadorEscenas`, constantes `Ruta…` de los menús, `PruebaHangar` → Configuracion, flujo antiguo con `SceneManager.LoadScene` por nombre), menús de creación y apertura y la lista del build. **Las escenas no se han movido**: muchos menús de creación usan rutas fijas y moverlas podría hacer que un menú creara una escena duplicada. La clasificación es por menú y documentación.

| Grupo | Escenas | Uso |
|---|---|---|
| **Juego** | `HangarExpedicion` (ENTRADA), `ExpedicionMVP` | Las únicas que se cargan durante la partida. |
| **Arte y pruebas** | `ModeloPrueba` (importar modelos; se crea con su menú), `VueloPrueba` (regresión del vuelo), `CirculacionPrueba`, `ExpedicionPrueba`, `HangarPrueba` | Pruebas útiles; no van al ejecutable. |
| **Prototipos antiguos** | `CentroOperaciones`, `Configuracion`, `Nave`, `Exploracion`, `Resultados` | Flujo anterior. Se conservan; no usar sus menús sin copia. |

**Candidatas a eliminación (solo para revisar; no se ha borrado nada):** `Assets/Scenes/SampleScene.unity` (plantilla, desactivada en el build), `Assets/Prueba.cs` (script suelto de pruebas), y las escenas del grupo «Prototipos antiguos» cuando el equipo decida que ya no se necesitan como referencia.

**Ejecutable:** *Juego → 6* compila con su propia lista: HangarExpedicion (entrada) + ExpedicionMVP, en `Ejecutable/ExpedicionEM4X_Completo/`. *Juego → 7* conserva la configuración anterior (solo ExpedicionMVP, `Ejecutable/ExpedicionEM4X/`), descrita en `CopiasSeguridad/ConfiguracionEjecutable_anterior.txt`. *Juego → 5* (opcional) pone esas dos escenas al principio de la lista del editor y guarda la lista anterior en `CopiasSeguridad/ListaEscenasBuild_antes_hangar_<fecha>.txt`.

| Antes | Ahora |
|---|---|
| 22 | Juego → 1. Hangar terrestre: crear o abrir HangarExpedicion |
| 16 | Juego → 2. Crear escena ExpedicionMVP |
| — | Juego → 3 / 4. Abrir HangarExpedicion / ExpedicionMVP |
| 17 | Juego → 5. Preparar la lista de escenas del build |
| 18 | Juego → 6. Generar el ejecutable completo (y 7: el anterior) |
| 19, 20, 21 | Arte y pruebas → 1, 2, 3 |
| — | Arte y pruebas → 4. Abrir ModeloPrueba; Abrir escenas de prueba/… |
| 12, 13, 14, 15 | Arte y pruebas → Crear escenas de prueba/… |
| 1–11 y Muestra visual | Prototipos antiguos (no usar sin copia)/… (mismos nombres y números) |

## Lanzadera L-4 configurable y edificio de integración (13-11-2026)

Ampliación sobre la versión entregada (copia: `CopiasSeguridad/Copia_entregada_preparacion_dinamica_2026-11-13.zip`). El holograma azul de la EM-4X, la carga, el combustible, el presupuesto y la secuencia de despegue se conservan. Estado: **escrito y revisado por lectura; sin compilar ni ejecutar.** Pruebas: sección AF de `PRUEBAS_ACEPTACION.md` (AE1–AE9 siguen pendientes).

**Qué representaba la lanzadera antes:** no tenía datos propios. Solo existían la tarifa fija «Lanzamiento de la tripulación» (18 M) y un bloque visual en el despegue. La carga desde tierra viaja en un **envío orbital sin tripulación** (10 M si se envía algo).

**Ahora (zona B, edificio de integración junto al hangar, misma escena):** tres tipos de pieza con tres alternativas. Todos los valores son de DEMOSTRACIÓN (reglas de juego, no prestaciones validadas). La opción 0 de cada tipo es la L-4 de siempre, incluida en los 18 M: sin tocar la lanzadera, los costes son exactamente los de antes.

| Pieza | Alternativas | Efecto jugable |
|---|---|---|
| Bodega de carga | sin · pequeña (300 kg, pesa 200 kg, 1,5 M) · grande (800 kg, pesa 400 kg, 3 M) | Si TODA la carga pequeña (suministros extra, kits extra y sonda) cabe, viaja en la L-4 y se entrega con el carro en el **muelle** (zona B), no en la zona de envío. |
| Tanque de transferencia | sin · 1 tramo (3000 kg, pesa 500 kg, 2 M) · 2 tramos (6000 kg, pesa 800 kg, 3,5 M) | Si TODO el combustible encargado cabe, viaja en la L-4 (el puesto de combustible lo indica). |
| Propulsores de refuerzo | sin (1000 kg extra) · 2 (4000 kg, 4 M) · 4 (8000 kg, 8 M) | Masa extra = bodega y tanque (aunque vayan vacíos) + lo que llevan. Si se supera, **no se puede cerrar la preparación**, y en la consola B no se deja confirmar una pieza que la supere. |

Reglas de dinero: el envío orbital (10 M) solo se cobra si queda algo que no viaja en la L-4. Los componentes de la EM-4X (motor, tanque, instrumento, relevo) van **siempre** en el envío orbital. Las opciones de la L-4 se cobran en su propia partida. Las tarifas y el presupuesto no cambian. Hay avisos para las opciones que no aportan nada: una bodega vacía, carga que no cabe, refuerzos de más o un envío que sigue haciendo falta. **Sin efecto en el vuelo:** la L-4 no cambia el Δv, la masa ni el combustible de la EM-4X.

**Interacción:**
- Entra por el paso de la pared derecha del hangar (zona B) y pulsa E en la consola B.
- Pestañas BODEGA / TANQUE TRANSF. / REFUERZOS: resaltan la pieza.
- Pulsar una alternativa la previsualiza: el puente grúa retira la pieza y coloca la nueva, guiado y sin física. El panel compara el coste total, la masa extra frente a la capacidad y qué viaja dónde.
- «CONFIRMAR ESTA PIEZA» cambia la configuración. Esc descarta la previsualización.
- Las piezas grandes se cambian solo aquí. El carro sigue siendo para los lotes pequeños.

**Despegue:** usa la configuración CONFIRMADA (el mismo `ModeloLanzadera` que la integración). Orden: traslado desde el edificio de integración a la plataforma → acceso y embarque → cierre de compuerta → comprobación → cuenta atrás → encendido → ascenso → llegada a la EM-4X. Se puede omitir igual que antes.

**Archivos:**
- Nuevos: `Scripts/MVP/DatosLanzadera.cs` (datos y reglas), `Scripts/HangarTerrestre/ModeloLanzadera.cs` (geometría provisional), `Scripts/HangarTerrestre/IntegracionLanzadera.cs` (vista, grúa, previsualización).
- Modificados: `BalanceEM4X.cs` (configuración de la lanzadera separada dentro de la preparación, con copia completa; cálculo, partidas y avisos), `HudMVP.cs`, `CargaHangar.cs` (destino de cada lote), `HangarTerrestre.cs` (edificio, paso en la pared este, muelle, consola B, carteles A–D), `HudHangarTerrestre.cs` (panel de integración), `SecuenciaDespegue.cs` (lanzadera configurada y traslado).
- Guía de modelado: apartado 12 (lanzadera y edificio). El encargo de Ciencia no cambia.

No hace falta escena nueva: el edificio está en HangarExpedicion, así que el recorrido y el ejecutable no cambian.

## Recorrido visual: estación A compacta, cubierta abovedada, transporte no tripulado VC-1 y llegada sin tarjetas (13-11-2026)

Sobre la versión validada (AE1–AE9 y AF1–AF4; copia: `CopiasSeguridad/Copia_validada_AE_AF_2026-11-13.zip`). Estado: **escrito y revisado por lectura; sin compilar ni ejecutar.** Pruebas: sección AG de `PRUEBAS_ACEPTACION.md`. Sigue habiendo tres vehículos distintos: **EM-4X** (en órbita, la configura el holograma), **L-4** (tripulación y carga asignada) y **VC-1** (nuevo, transporte orbital sin tripulación). No hay escenas nuevas: todo está en HangarExpedicion, así que el ejecutable no cambia.

**Estación A:**
- Alrededor de la mesa del holograma están las consolas MOTOR, TANQUE e INSTRUMENTO (abren el holograma con esa pieza elegida), la consola A2 «Comparar y encargar» (este) y la terminal 2 (oeste).
- Detrás, bajas, hay maquetas identificadas de la pieza elegida: motor y tanque a 1:10, instrumento a 1:2. La pieza real, a escala 1:1, aparece en su soporte de la zona C2.
- El paso queda libre a ambos lados.

**Cubierta de la L-4:**
- Bóveda de perfil ovalado: paredes hasta 36 m y clave a 52 m.
- Se conservan los arcos de estructura, las plataformas, las pasarelas, la grúa (ahora de 30 m, entre rieles a z 5 y 35) y la luz de trabajo.
- Hay una puerta de salida de 16 × 42 m al este.
- Gálibo comprobado por cálculo (no en Unity): sección 13 B de la guía.

**Estados de un componente encargado:**
- **Previsualización:** solo en el holograma. No genera carga ni compras.
- **Encargo confirmado:** la pieza aparece a escala real en su soporte de C2, con el destino «Transporte orbital no tripulado».
- **Preparación:** E en el soporte.
- **Carga:** el puente grúa la lleva a su hueco de la bodega del VC-1.
- **Despacho:** solo al embarcar, con la preparación cerrada y validada.
- Cambiar o cancelar un componente con la preparación abierta retira su representación e invalida solo su lote.
- El equipo de serie (opción 0) no aparece como compra ni envío.

**Carga:**
- Cada componente de la EM-4X es su propio lote y va siempre en el VC-1.
- Los lotes pequeños siguen con el carro: a la L-4 (muelle B) o a la entrega al VC-1 (amarilla, dentro de C2), según las reglas de la lanzadera.
- El combustible conserva su destino calculado.
- Los rótulos dicen «L-4: tripulación y carga» o «Transporte orbital no tripulado».
- El VC-1, la animación y su lanzamiento no se cobran: la tarifa del envío orbital (10 M) ya era ese servicio.

**Límites de demostración del VC-1** (`DatosTransporteOrbital`):
- Masa útil: 34 000 kg.
- Bodega: Ø 7,2 × 23 m.
- Anillo: 12 módulos de combustible.

Contraste con las piezas actuales:

| Envío | Masa | Largo de bodega | ¿Cabe? |
|---|---|---|---|
| Tanque ampliado + 10 tramos | 33 600 kg | 13,6 m | sí (por poco) |
| Tanque ampliado + motor de alto empuje + 10 tramos | 37 200 kg | 17,5 m | **no**: masa |
| Tanque ampliado + motor de alta eficiencia + sensor + relevo | ~7 000 kg | 22,8 m | sí (el caso más largo) |
| Cualquier pieza sola | — | — | sí: todas caben en Ø 7,2 m |

Si no cabe, el motivo aparece en la revisión y en el VC-1, la preparación no se puede cerrar y no se despacha. Nunca se reduce una pieza ni se cambia un catálogo. **Cambio de comportamiento:** las combinaciones muy pesadas que antes se podían cerrar ahora se bloquean por la masa del VC-1.

**Recorrido al embarcar** (planos breves con modelos provisionales, sin física):
1. **Envío** (solo si hace falta; si no, no se prepara ni se lanza ningún transporte):
   - El VC-1 sale de la nave C2 con la bodega abierta y cierra sus compuertas.
   - Se integra en el lanzador LC-2, en la plataforma exterior, y despega tras la cuenta atrás.
   - Llega a la zona de servicio orbital. Por cada pieza: recepción, retirada de la de serie (queda en el soporte de piezas retiradas), colocación y comprobación.
   - Se transfiere el combustible y la carga pequeña entra por el nodo de popa: «EM-4X PREPARADA».
2. **Tripulación:**
   - Carga de la bodega de la L-4, traslado desde el edificio, embarque, compuerta, comprobación, cuenta atrás y ascenso.
   - La L-4 llega y atraca bajo el Mando, y su carga pasa a la EM-4X.
   - Se entra en ExpedicionMVP ya a bordo, con el control del personaje.

**Sin tarjetas narrativas** al llegar desde el hangar. **F2**, a bordo, abre un resumen opcional (qué subió en cada vehículo, qué se instaló y el coste ya cobrado). El acceso directo a ExpedicionMVP (pruebas, sin hangar) conserva sus tarjetas, porque allí no hay secuencias. Cada parte se puede omitir con Espacio/Intro después de haberla visto entera; el resultado es el mismo.

**Sustitución de modelos:**
- Las piezas provisionales del hangar, la L-4, el VC-1, el lanzador y el servicio orbital son *puntos de sustitución* con id.
- Se usa la misma tabla de la EM-4X, con entradas de tipo **Escenario**.
- *Prototipo → Arte y pruebas → 5. Conectar la tabla de sustitución al hangar* la asigna a la escena (con confirmación).
- Detalles en la guía, sección 13.

**Archivos:**
- Nuevos: `MVP/DatosTransporteOrbital.cs`, `HangarTerrestre/ModeloComponentes.cs`, `ModeloTransporte.cs`, `ZonaTransporteOrbital.cs`, `EscenarioOrbital.cs`, `GeometriaProvisional.cs`, `SecuenciaEnvioOrbital.cs` y `ArteEM4X/SustitucionEscenario.cs`.
- Modificados: `BalanceEM4X.cs`, `JuegoMVP.cs`, `HudMVP.cs`, `CargaHangar.cs`, `HangarTerrestre.cs`, `HudHangarTerrestre.cs`, `SecuenciaDespegue.cs`, `ModeloLanzadera.cs`, `IntegracionLanzadera.cs`, `RecursosVisualesEM4X.cs` (tipo Escenario, al final de la lista), `SustitucionVisualEM4X.cs` (ignora las entradas Escenario) y `Editor/CrearEscenaHangarExpedicion.cs`.

