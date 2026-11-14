# Pruebas de aceptación del recorrido completo

Recorrido: **CentroOperaciones → Configuracion → Exploracion → Resultados → Repetir**.

Preparación: vista Game con resolución fija 16:9 (por ejemplo 1920×1080) y *Scale* en 1x.
Abre `Scenes/CentroOperaciones` y pulsa Play. Ten abierta la Console.

Configuración de referencia: **Cartografiar · Cámara · Batería ligera · Lanzador pequeño ·
Relevo en órbita baja**. Todos los valores son de DEMOSTRACIÓN.

## A. Recorrido principal

| # | Pasos | Resultado esperado |
|---|---|---|
| A1 | Arranca en CentroOperaciones | Se ven la estación, la luna y el satélite orbitando. Sin errores en la Console |
| A2 | **Configurar misión** | Pantalla de configuración. Con la referencia: coste **72/100 M USD**, carga **190/230 kg**, "[OK]" |
| A3 | Mira "Transmitir todos los datos" | Indica "s reales (media, contando las ventanas sin contacto)" |
| A4 | **Iniciar misión** y completa la nave (sección F) | Exploracion. Panel derecho: 80 Wh, aceleración ×1,00. Abajo, la lista completa de teclas (E, F, T, M, F9…) |
| A5 | Dentro de la estación | Panel de energía: "Autonomía: recargando en la estación". Abajo: "T: terminar la misión…" |
| A6 | Sal, escanea 1 punto (E) y vuelve | En la estación, mientras quedan datos: "…aún hay N MB sin transmitir, se perderían" |
| A7 | Escanea y transmite los 3 puntos | Banner verde y, 4 s después, Resultados **MISIÓN CUMPLIDA** |
| A8 | Lee el informe | Duración "m:ss reales (≈ N min de misión)". Puntuación 12 + 10 + 14 = **36** |
| A9 | **Repetir con la misma configuración** | La nave de nuevo (preparación) y, tras el viaje, Exploracion con batería llena, 0 escaneos, puntos sin escanear, misma configuración |
| A10 | Pulsa **M** | Configuracion con la misma elección marcada |

## B. Finales alternativos

| # | Pasos | Resultado esperado |
|---|---|---|
| B1 | Transmite 1 escaneo y pulsa T en la estación | **PARCIALMENTE CUMPLIDA**. Sin bonos; explica que el sobrante no suma |
| B2 | Pulsa T fuera de la estación | "Solo puedes terminar la misión dentro de la estación". No termina |
| B3 | Catálogo: *Capacidad Wh* de la batería ligera = 15. Sal de la estación y aléjate sin volver (si llegas a un punto, escanéalo) | "ENERGÍA AGOTADA · Generando el informe…" → **MISIÓN FALLIDA** con la distancia a la estación, los MB perdidos si escaneaste y la sugerencia de batería mayor |
| B4 | Desde Resultados, **Volver a configurar** | Configuracion con la última elección |

## C. Reglas de la configuración

| # | Elección, o cambio en el catálogo (fuera de Play) | Resultado esperado |
|---|---|---|
| C1 | Cámara · **Batería alta** · Pequeño · **Órbita alta** | Carga 238/230 EXCEDIDO. Botón bloqueado |
| C2 | Espectrómetro · Ligera · **Mediano** · Baja | Coste 106/100 EXCEDIDO. Botón bloqueado |
| C3 | *Memoria MB* = 100 | Con la Cámara (120 MB por escaneo): bloqueado por memoria. Con el Espectrómetro (60 MB): permitido |
| C4 | *Fracción Contacto* de la órbita baja = 0 | Con esa órbita: bloqueado por "no tiene enlace" |
| C5 | *Escaneos Requeridos* del objetivo = 4 | Permite iniciar. En Exploracion, aviso rojo en el panel de datos y en la Console: solo hay 3 puntos |
| C6 | *Minutos Mision Por Segundo* del catálogo = 2 | Al entrar en Exploracion, aviso amarillo en la Console por escalas de tiempo distintas |

Deshaz los cambios del catálogo al terminar (o restaura desde `CopiasSeguridad`).

## D. Ejecutable y rendimiento

| # | Pasos | Resultado esperado |
|---|---|---|
| D1 | File → Build Profiles: orden de escenas | CentroOperaciones, Configuracion, Exploracion, Resultados (SampleScene, si aparece, se puede desmarcar) |
| D2 | Build en una carpeta **fuera** del proyecto y ejecútalo | Luna con textura, satélite visible, estación con materiales (nada rosa) |
| D3 | F9 en cada escena (editor y ejecutable) | Anota FPS y "peor ms". Referencia del editor en CentroOperaciones: 312 FPS (dato medido por el equipo) |
| D4 | Recorrido A completo en el ejecutable | Igual que en el editor |

## E. Modo equipo

| # | Pasos | Resultado esperado |
|---|---|---|
| E1 | En Configuracion, Jugadores **1** | Sin filas de roles ni aprobaciones. Todas las secciones activas. INICIAR funciona como antes (A2–A4) |
| E2 | Jugadores **2** | Roles: Ciencia J1, Ingeniería J2, Lanzamiento J2, Comunicaciones J1. Turno J1. Batería y Lanzador en gris (no se pueden pulsar) |
| E3 | Turno **J2** | Objetivo, Instrumento y Órbita en gris; Batería y Lanzador activos |
| E4 | Con la referencia, mira las líneas de prioridad | Ciencia SE CUMPLE; Ingeniería NO (margen < 100 Wh); Lanzamiento SE CUMPLE (reserva 28); Comunicaciones SE CUMPLE |
| E5 | Pulsa solo **J1: aprobar** | Botón grande: "FALTAN APROBACIONES: J2", desactivado |
| E6 | Pulsa **J2: aprobar** | "INICIAR MISIÓN" activo |
| E7 | Con ambos aprobados, cambia la batería (turno J2) | Las dos aprobaciones vuelven a "aprobar" |
| E8 | Reasigna Lanzamiento a J1 | Aprobaciones anuladas; en turno J1 el Lanzador queda activo |
| E9 | Jugadores **4** | Un rol por jugador; cuatro botones de aprobación |
| E10 | Inicia y mira el panel de Exploracion | Línea "Equipo: N jugadores", el panel no tapa otros textos |
| E11 | Termina la misión | Resultados: sección "PRIORIDADES DE CADA ROL (N/4)" con [OK]/[X], jugador, plan y resultado real; en POR QUÉ, "El equipo (…) cumplió N de 4 prioridades" |
| E12 | **Volver a configurar** | Mismo número de jugadores y reparto; aprobaciones vacías |
| E13 | Para Play y vuelve a pulsar Play | Jugadores vuelve a 1 |

## F. Nave (activa *Modo Prueba* en ControlNave para acortar los tiempos)

Con la configuración de referencia (lanzador pequeño): combustible 300 + 40 = **340 kg**, masa **6530 kg**.

| # | Pasos | Resultado esperado |
|---|---|---|
| F1 | Configuracion → **Iniciar misión** | Escena Nave en primera persona, cielo azul y plataforma por la ventana. Panel: "Preparación", "[MODO DE PRUEBA]" si está activado |
| F2 | Camina y mira cada consola | Abajo: "E: usar …". Luces ámbar en Comunicaciones, Ingeniería y Ciencia |
| F3 | Pilotaje antes de comprobar nada, Espacio | Aviso "Faltan comprobaciones: …". No despega |
| F4 | Ingeniería (E) | Motor estándar 26 kN, 2,5 kg/s. Masa seca 6000 kg (casco y sistemas 5600 + motor 400), carga científica 190 kg, combustible 340 kg (300 + 40), masa total inicial 6530 kg, aceleración 3,98 m/s², energía 1000 Wh. Espacio: luz verde |
| F5 | Ciencia (E) | Datos de la SONDA: batería 80 Wh, masa 170 kg. Espacio: luz verde |
| F6 | Comunicaciones (E) | Mensaje "Plan de vuelo". Espacio: confirmado, luz verde |
| F7 | Pilotaje, Espacio | Cuenta atrás grande; Q no saca del asiento. Al terminar: vibración, el suelo cae, el cielo se oscurece, aparecen estrellas en movimiento (separación de etapa: sección H) |
| F8 | Tras el ascenso, Q | Puedes caminar por la cabina |
| F9 | Espera la avería (30 % del viaje) | Luces rojas, luz roja en Ingeniería, la energía de la nave baja |
| F10 | Ingeniería: pulsa un número que NO es el circuito en sobrecarga | "Paso equivocado": −40 Wh, vibración. Sigue en el paso 1 |
| F11 | Número correcto → R → Espacio | Palanca roja → ámbar → azul → verde; "Avería reparada"; luces normales |
| F12 | Espera la alerta de escombros | Luces ámbar, luz roja en Pilotaje, aviso con cuenta atrás |
| F13 | Quédate fuera de Pilotaje mientras llegan | Rocas visibles por la ventana; impactos: destello rojo, vibración, casco −25 % |
| F14 | Repite (Repetir) y esquiva desde Pilotaje con WASD | Las rocas se desplazan en la ventana; el combustible baja al empujar y al soltar (estabilizador); cuenta de esquivados |
| F15 | Llegada | La luna crece por la ventana; mensaje en Comunicaciones; Pilotaje pide esperarlo |
| F16 | Confirma el mensaje y vuelve a Pilotaje | Barra con zona verde y marcador. Espacio: "En órbita", combustible restado |
| F17 | Comunicaciones (autorización de despliegue) → Ciencia, Espacio | Cápsula que se aleja por la ventana; después, la transición (sección I) y Exploracion |
| F18 | Termina la exploración | Resultados con sección "VIAJE EN LA NAVE" y explicaciones de impactos, avería, combustible y frenada |
| F19 | Recibe 4 impactos (no esquives) | "MISIÓN ABORTADA" → Resultados **MISIÓN FALLIDA**, "La sonda no llegó a desplegarse" |
| F20 | No repares la avería | Energía de la nave a 0 → misión abortada |
| F21 | Gasta combustible empujando mucho (más de ~35 s a pleno empuje) antes de llegar y frena en el peor momento | Si no alcanza: abortada por falta de combustible. Una sola mala frenada, sin maniobras excesivas, ya no aborta |
| F22 | Referencia con **Lanzador mediano** (coste 90/100) | Ingeniería: combustible **350 kg** (el tope del tanque estándar; 210 kg "no caben en el tanque"), masa total 6540 kg, aceleración 3,98 m/s². *Cambió con el paso del tanque: antes 560 kg* |
| F23 | En el objeto PantallaConfiguracion de Configuracion, desmarca *Pasar Por Nave* | Iniciar misión va directamente a Exploracion, como antes; Resultados sin sección de viaje |

## G. Consecuencias del viaje (Modo Prueba activado; configuración de referencia: batería 80 Wh, coste 72 M USD)

| # | Pasos | Resultado esperado |
|---|---|---|
| G1 | Partida BUENA: separa la etapa en la zona verde, repara la avería rápido y sin errores, esquiva todos los escombros, frena en la zona verde | En órbita: mensaje "Evaluación del viaje" en Comunicaciones: separación +5, reparación +5, escombros +6, puntos de viaje 16; carga de la sonda 80/80 Wh |
| G2 | Antes de desplegar, mira el puesto de Ciencia | Muestra la carga inicial prevista de la sonda |
| G3 | Despliega y mira el panel derecho de Exploracion al empezar | Sin línea "Carga inicial" (no hubo reducción); batería 80/80 |
| G4 | Termina con el objetivo cumplido | PUNTUACIÓN: Ciencia 12 · Objetivo 10 · Presupuesto 14 · Viaje 16 · **TOTAL 52** |
| G5 | Partida CON ERRORES (Repetir): no separes la etapa, tarda en reparar o equivócate dos veces, recibe 2 impactos, frena muy lejos de la zona verde | Evaluación: separación 0, reparación +2 o 0, escombros +0; carga de la sonda menor que 80 y nunca menor que 72 Wh |
| G6 | Mira Ciencia antes de desplegar | "Carga inicial … (−N Wh, N %)" con la explicación de la regla simplificada |
| G7 | Exploracion al empezar | Panel derecho: "Carga inicial: N/80 Wh (descenso prolongado)". El panel de energía empieza en N, la capacidad sigue siendo 80 |
| G8 | Quédate unos 2 s en la estación | La batería sube hasta 80/80 (la recarga funciona igual) |
| G9 | Termina la misión | PUNTUACIÓN con Viaje 0–2; el total no tiene una segunda resta por la carga; VIAJE y POR QUÉ explican la carga inicial aparte |
| G10 | En Exploracion pulsa M → Configuracion → Iniciar y repite el viaje con frenada en la zona verde | Sin reducción: la penalización anterior no se arrastra |
| G11 | Resultados → Repetir con frenada imprecisa y vuelve a mirar el panel de Exploracion | La reducción corresponde solo a la última frenada (no se suma a la anterior) |
| G12 | Pulsa Play directamente en Exploracion | Batería 80/80, sin línea "Carga inicial"; Resultados: "Viaje: 0 (no se pasó por la nave)" |
| G13 | En Configuracion compara antes y después de jugar | Coste 72/100 M USD y carga 190/230 kg iguales; Ingeniería de la nave: combustible 340 kg (sin cambios) |
| G14 | Llegada con frenada en el peor momento y pocas maniobras | Entra en órbita (cuesta unos 250 kg); no aborta |

## H. Separación de etapa (Modo Prueba)

| # | Pasos | Resultado esperado |
|---|---|---|
| H1 | Pilotaje con todo comprobado (antes de la cuenta atrás) | El panel explica el procedimiento: zona verde, Espacio, al menos 2,5 s, "procedimiento simplificado" |
| H2 | Durante la cuenta atrás | El panel muestra el lanzador elegido (p. ej. "Lanzador pequeño"), "Margen de carga configurable: 190 / 230 kg usados (el vehículo tripulado base va incluido)", combustible extra 40 kg, motor y masa total de la nave |
| H3 | Ascenso: espera a la zona verde y pulsa Espacio | Aviso "Separación a tiempo … (+5)", sacudida breve; el ascenso continúa |
| H4 | Repetir: pulsa Espacio antes de la zona verde | "Separación temprana … (0)"; el ascenso continúa |
| H5 | Repetir: no pulses nada | Al cerrarse la ventana: "Sin orden a tiempo: el ordenador separó la etapa tarde … (0)"; el ascenso continúa (se detiene un momento mientras espera) |
| H6 | Llega a la órbita y lee la evaluación | La separación aparece con +5 o +0 según H3–H5 |
| H7 | Resultados | VIAJE: "Despegue: <lanzador> · separación de etapa a tiempo / fuera de la ventana"; POR QUÉ explica el resultado; la línea Viaje la incluye |
| H8 | Configuracion | Coste, masa y presupuesto sin cambios |

## I. Transición de llegada

| # | Pasos | Resultado esperado |
|---|---|---|
| I1 | Despliega la sonda y espera sin pulsar nada | Tres tarjetas: llegada (se ve la luna detrás), sistema de acceso (el módulo baja y se abre el paso en el hielo), descenso (la sonda baja al océano). Después, Exploracion |
| I2 | Repetir y pulsa Espacio en cada tarjeta | Avanza tarjeta a tarjeta |
| I3 | Repetir y pulsa Intro en la primera tarjeta | Pasa directamente a Exploracion |
| I4 | Lee el aviso inferior | Indica que el paso a través del hielo está simplificado y que la luna y la misión son ficticias |
| I5 | Exploracion tras la transición | Igual que antes: carga inicial según la frenada (sección G), sin otros cambios |

## J. Demostración tripulada (ControlNave: *Terminar Al Llegar* activado, valor por defecto)

| # | Pasos | Resultado esperado |
|---|---|---|
| J1 | Recorrido desde CentroOperaciones → Configuracion → Nave | Panel de la nave: "Destino: Luna helada (ficticia)" y "Viaje N % · faltan N M km (simulado: progreso guionizado)" |
| J2 | Llega, frena y confirma "Evaluación del viaje" en Comunicaciones | Llega un mensaje "Objetivo alcanzado" (no la autorización de despliegue) |
| J3 | Confírmalo | Cartel verde "OBJETIVO ALCANZADO"; a los 4 s, Resultados. No se pasa por Exploracion |
| J4 | Resultados (partida buena) | "OBJETIVO ALCANZADO". Evaluación: Llegada 10 · Presupuesto 14 · Viaje 16 · Ciencia "no realizada" · **TOTAL 40**. Secciones NAVE Y RECURSOS y DESEMPEÑO EN EL VIAJE |
| J5 | Puesto de Ciencia en órbita | "La sonda queda a bordo: la exploración es una fase posterior" y Espacio no hace nada |
| J6 | Partida que aborta (4 impactos) | "MISIÓN FALLIDA", todos los puntos a 0, explicación del motivo; sin botón de exploración |
| J7 | Resultados → "EXPLORAR CON LA SONDA (FASE POSTERIOR, OPCIONAL)" | Exploracion con la carga de la sonda según la frenada; al terminar, informe de exploración como antes |
| J8 | Resultados → "REPETIR LA MISIÓN" | Vuelve a la nave en preparación, todo reiniciado |
| J9 | ControlNave: desactiva *Terminar Al Llegar* | Recorrido anterior: despliegue, transición y Exploracion |

## K. Selección de misión (A2) y motor de la nave (B, primera parte)

Configuración estándar de referencia: Cartografiar · Cámara · Batería ligera · Lanzador pequeño · Relevo en órbita baja · **Motor estándar**.

| # | Pasos | Resultado esperado |
|---|---|---|
| K1 | Play en CentroOperaciones → Configurar misión (primera vez) | "SELECCIÓN DE MISIÓN": tarjeta "TRAVESÍA AL HIELO" con destino, objetivo, presupuesto 100 M USD y dificultad Media con 4 condiciones. Dos tarjetas grises "PRÓXIMAMENTE" sin botón |
| K2 | ELEGIR ESTA MISIÓN | Pantalla de configuración; arriba "Misión: Travesía al hielo…" y botón VER MISIÓN |
| K3 | Mira la columna derecha | Vista exterior girando: lanzador a la izquierda ("se separa"), nave a la derecha ("continúa"), motor en el hueco |
| K4 | Referencia (motor estándar) | Coste 72/100; "Margen de carga configurable del lanzador" 190/230 kg; "Nave: masa total inicial 6530 kg (seca 6000 + carga científica 190 + combustible 340)" |
| K5 | Sección 6: **Motor de alto empuje** | La tobera de la vista se hace mayor, con 4 toberas auxiliares y otro color. Coste 80/100; margen 220/230 kg (+30); masa total 6530 kg (seca 6030 + 190 + combustible 310); aceleración 5,51 m/s² |
| K6 | Alto empuje + **Lanzador mediano** | Coste 98/100; margen 220/450 kg; combustible **350 kg** (tope del tanque estándar; 180 kg no caben); masa total 6570 kg; aceleración 5,48 m/s². El lanzador de la vista se alarga. *Cambió con el paso del tanque* |
| K7 | Inicia con alto empuje (lanzador pequeño) y abre Ingeniería | "Motor: Motor de alto empuje · 36 kN · 3,4 kg/s"; masa seca 6030 (casco 5450 + motor 430 + tanque 150); combustible 310; aceleración 5,51 |
| K8 | Escombros: esquiva desde Pilotaje | Responde más rápido que con el estándar; el combustible baja más deprisa al empujar |
| K9 | Resultados | Configuración: "Motor de la nave: Motor de alto empuje · 36 kN · 3,4 kg/s · 8 M USD" y el margen de carga; NAVE: "Masa total inicial … = seca + carga científica + combustible" |
| K10 | Repetir la misión | El motor elegido se conserva |
| K11 | Volver a configurar | Va directo a las piezas (no a la selección); VER MISIÓN vuelve a la tarjeta |

Con el motor y el tanque estándar y el lanzador pequeño, los valores de las pruebas F y G no cambian.

## L. Tanque de la nave (B, segunda parte)

Combustible = mínimo(capacidad del tanque, 300 kg base + margen de carga sobrante). Nunca supera el tanque.

| # | Pasos | Resultado esperado |
|---|---|---|
| L1 | Referencia (lanzador pequeño, motor y tanque estándar) | Igual que antes: coste 72; margen 190/230; combustible 340 de 350; masa total 6530; aceleración 3,98 |
| L2 | Sección 7: **Tanque ampliado** con lanzador pequeño | El tanque de la vista se alarga y ensancha (más bandas naranjas). Margen 240/230 kg: **bloquea INICIAR** ("Margen de carga configurable superado") |
| L3 | Tanque ampliado + **Lanzador mediano** | Coste 96/100; margen 240/450; combustible 510 de 600 kg; masa total 6750 (seca 6050 + 190 + 510); aceleración 3,85 |
| L4 | Tanque ampliado + mediano + **alto empuje** | Coste 104/100: **bloquea INICIAR** por presupuesto |
| L5 | Tanque estándar + mediano | Combustible 350 de 350; texto "el margen daría 210 kg más, pero no caben" |
| L6 | Inicia con L3 y abre Ingeniería | "Tanque: Tanque ampliado · capacidad 600 kg"; masa seca 6050 (casco 5450 + motor 400 + tanque 200); combustible 510 de 600 |
| L7 | Viaje con L3 | Más margen de combustible para maniobrar y frenar que con el estándar |
| L8 | Resultados | "Tanque de la nave: Tanque ampliado · capacidad 600 kg · piezas de la nave 6 M USD" |
| L9 | Repetir / volver a configurar | El tanque elegido se conserva |

## M. Sectores de la cabina (versión reducida; ControlNave → *Mostrar Sectores*)

| # | Pasos | Resultado esperado |
|---|---|---|
| M1 | Entra en la nave | Cuatro rótulos con el número y nombre del sector: 1 PILOTAJE (pared delantera, a la izquierda de la ventana), 2 INGENIERÍA (izquierda), 3 COMUNICACIONES Y NAVEGACIÓN (derecha), 4 CIENCIA / CONTROL DE MISIÓN (fondo) |
| M2 | Mira el suelo y las paredes | Borde luminoso de color en cada sector, tabiques bajos junto a las paredes y una luz del color del sector |
| M3 | Camina entre sectores | Abajo a la izquierda: "Sector N · nombre" o "Pasillo central" y "Nave con puestos para 4 tripulantes · controla 1 jugador" |
| M4 | Desde el asiento de pilotaje | El rótulo y los tabiques no tapan la ventana ni la barra de separación ni la mira |
| M5 | Usa los cuatro puestos (E/Q) | Igual que antes: mismas posiciones, vistas e interacciones |
| M6 | Avería y escombros | Las alertas siguen viéndose (las luces de la cabina se ponen rojas/ámbar; las de sector mantienen su color) |
| M7 | Desmarca *Mostrar Sectores* en ControlNave | La cabina vuelve a verse como antes |

Limitaciones: los tabiques no tienen colisionador (se pueden atravesar si se camina pegado a la pared).
La nave grande con una distribución más amplia queda pendiente.

## N. Nave ampliada (menú 9) — pruebas de cierre

Antes: escena Nave abierta, fuera de Play → *Prototipo → 9. Ampliar nave* → Ctrl+S. ControlNave con *Modo Prueba*.

| # | Pasos | Resultado esperado |
|---|---|---|
| N1 | Recorre las cuatro salas y el pasillo, también pegado a las paredes y por las puertas | No se atraviesa ninguna pared, puerta, consola ni la barandilla de Ingeniería; no hay atascos en las puertas (1,4 m) ni zonas sin suelo; abajo a la izquierda, "Sector N · …" o "Pasillo" |
| N2 | Rótulos | 1 PILOTAJE sobre la entrada de la cabina, 2 INGENIERÍA sobre la entrada de popa, 3 COMUNICACIONES Y NAVEGACIÓN y 4 CIENCIA / CONTROL DE MISIÓN sobre sus puertas (se leen desde el pasillo); borde de color en el suelo de cada sala |
| N3 | Entra y sal (E/Q) de los cuatro puestos | La cámara mira la consola de cada puesto. En Ingeniería se ven de frente la pantalla y las palancas 1, 2, 3 (de izquierda a derecha) |
| N4 | Ingeniería: detrás de la barandilla | Tanque y motor con el tamaño de la configuración (rótulos "TANQUE: …" y "MOTOR: …"); cambian si eliges otras piezas |
| N5 | Desde Pilotaje, espera la avería y ve a Ingeniería a repararla | Se llega en unos 5 s; la reparación sigue dando +5 si no hay errores; las luces de todas las salas se ponen rojas |
| N6 | Tras reparar, vuelve a Pilotaje con el aviso de escombros | Aviso de **8 s** también en modo de prueba; da tiempo a llegar y sentarse |
| N7 | Esquiva y deja que alguna roca impacte | Las rocas se ven por la ventana; cada roca cuenta una sola vez (impacto o esquivada); al impactar: destello, vibración, −25 % de casco; ninguna roca aparece dentro de las salas |
| N8 | Frenada, llegada y Resultados | Igual que antes (J2–J4) |
| N9 | Repetir la misión | La nave sigue ampliada; todo se reinicia |
| N10 | Fuera de Play: *Prototipo → 10. Volver a la cabina original* | Puestos, vistas, paredes, luces de alerta y aviso de prueba (4 s) como antes; desaparece NAVE_AMPLIADA; sin referencias vacías en la lista de luces de ControlNave |
| N11 | Menú 9 dos veces | Pregunta "Rehacer" y no duplica nada |
| N12 | Ejecutable de Windows | El recorrido completo funciona con la nave ampliada |

## O. Acabado de la cabina de pilotaje (menú 11)

Antes: N1–N12 superadas. Escena Nave abierta, fuera de Play → *Prototipo → 11. Acabado: cabina de pilotaje (aplicar)* → Ctrl+S.

| # | Pasos | Resultado esperado |
|---|---|---|
| O1 | Captura A (antes y después): Play, camina hasta la entrada de la cabina (pasillo, z ≈ 0,8) y mira al frente | Paredes con paneles y nervaduras, techo con canal de luz y arcos, suelo de rejilla con líneas de luz, marco de ventana biselado con borde naranja, consola con mesa, mandos, holograma y tres pantallas |
| O2 | Captura B: siéntate en Pilotaje (E) en Preparación | Ventana despejada (sin listón central). Pantallas con fase/viaje, combustible/aceleración y casco/energía. Se leen la barra de separación y los textos del HUD |
| O3 | Captura C: durante la avería, desde la cabina | Las tiras de luz se vuelven rojas con las luces de la nave |
| O4 | Escombros desde Pilotaje | Mira, rocas e impactos visibles igual que antes |
| O5 | Usa los cuatro puestos y recorre la nave | Sin cambios en movimiento, colisiones ni interacciones; el borde del sector 1 sigue visible sobre la rejilla |
| O6 | F9 en la captura A y B, antes y después | Anota FPS y "peor ms" |
| O7 | *… (quitar)* y Ctrl+S | La cabina vuelve a verse como antes; sin posprocesado añadido en la cámara |


## P. Despegue visto desde fuera (ControlNave → *Mostrar Despegue Exterior*)

Antes: ControlNave con *Modo Prueba* activado (también probar una vez sin él). Elige en Configuracion un motor y un tanque y anótalos.

| # | Pasos | Resultado esperado |
|---|---|---|
| P1 | Comprobaciones hechas, siéntate en Pilotaje y pulsa Espacio | En los últimos 3 s de la cuenta atrás la imagen pasa a la vista exterior: plataforma, torre, nave sobre el lanzador. Rótulo "VISTA EXTERIOR (simulada)" arriba a la izquierda; sin paneles laterales del HUD |
| P2 | Captura 1: encendido | Llama naranja bajo el lanzador, luz en la plataforma, humo que se extiende, vibración moderada; sonido grave del motor que sube |
| P3 | Durante el ascenso | La cámara sigue a la nave; el cielo pasa de azul a negro y aparecen estrellas. La barra de separación sigue visible |
| P4 | Captura 2: pulsa Espacio dentro de la ventana | El lanzador se suelta y cae girando, golpe sonoro, se enciende la llama azulada de la nave; el resultado de la separación es el mismo que antes (correcta / temprana / automática) |
| P5 | No pulses nada (otra partida) | El lanzador se separa solo al cerrarse la ventana, igual que antes |
| P6 | Captura 3: al empezar el crucero | La cámara se aleja y se ve el planeta (ficticio) debajo; fundido a negro y vuelta a la cabina en primera persona; se puede levantar con Q |
| P7 | Captura 4: compara con Configuracion | Tanque (largo, diámetro, color) y motor (tobera, toberas auxiliares, color) son los elegidos; el lanzador más grande si se eligió uno mayor. Con más empuje la llama de la nave es más larga |
| P8 | Otra partida: pulsa Intro durante la toma | Vuelve enseguida a la cabina; la misión sigue igual |
| P9 | Desactiva *Mostrar Despegue Exterior* | El despegue se ve como antes, solo desde la cabina |
| P10 | F9 durante la toma exterior | Anota FPS y "peor ms" |

## Q. Observación de aproximación (ControlNave → *Observacion → Activa*)

**Estado: SUPERADA** según confirmación del equipo el 9 de noviembre de 2026 («Las pruebas salieron bien»). La confirmación fue global para esta sección, sin detalle prueba por prueba. Copia de esta versión: `CopiasSeguridad/Copia_funcional_observacion_superada_2026-11-09.zip`.

Bonus APARTE de 0 a 6 (no suma a la puntuación principal). Valores provisionales de prueba (DEMOSTRACIÓN).
Con *Modo Prueba* la captura y el envío se aceleran en la misma proporción que el viaje (×3,75): la ventana dura unos 6 s.
Con tiempos normales: ventana de 60 a 84 s de crucero (progreso 0,40–0,56), entre la avería (45 s) y los escombros (90 s).

| # | Pasos | Resultado esperado |
|---|---|---|
| Q1 | Viaje normal; repara la avería y espera | Al abrirse la ventana: aviso «Ventana de observación abierta (N s)…», luz ámbar en Ciencia e instrucción del HUD con los segundos que quedan |
| Q2 | En Ciencia (E), mantén Espacio | Suben las UD capturadas (1 UD/s; ×3,75 en prueba); la energía de la nave baja 21 Wh por UD; al llegar a 12 UD: «Captura completa» |
| Q3 | Suelta Espacio a mitad | La captura se detiene; no hay otra acción en Ciencia durante el viaje |
| Q4 | Ve a Comunicaciones (E), mantén Espacio | Bajan las «pendientes de envío»; el panel muestra el tiempo hasta la llegada y el bonus actual; luz ámbar en Comunicaciones mientras haya pendientes |
| Q5 | Deja datos sin enviar hasta llegar | Aviso de llegada con «N UD de observación sin enviar: no cuentan»; no suman al bonus |
| Q6 | En órbita, lee la evaluación en Comunicaciones | Línea «Bonus de observación (aparte, no suma): X de 6» |
| Q7 | Resultados | TOTAL igual que sin observación (sobre 40); línea aparte del bonus; explicación con UD capturadas/enviadas y energía usada. Los puntos de reparación NO bajan por la energía de la observación |
| Q8 | Partida ignorando la observación (no pulses Espacio en Ciencia) | Energía, puntos y condiciones iguales que antes; bonus 0; la explicación dice «no se capturaron datos… sin gasto de energía» |
| Q9 | Con un mensaje pendiente en Comunicaciones, mantén Espacio | Solo confirma el mensaje; para enviar datos hay que soltar y volver a pulsar |
| Q10 | Desactiva *Observacion → Activa* | Sin avisos, luces ámbar, textos ni consumos de la observación; Ciencia y Comunicaciones funcionan como antes; Resultados sin la línea del bonus |
| Q11 | (Opcional) Deja la avería sin reparar y captura | Si la energía llega a 0, aborto con motivo «la avería y la observación (N Wh) agotaron la reserva». La captura sola nunca deja la energía a cero |

## R. Corrección de trayectoria antes del frenado (ControlNave → *Correccion → Activa*)

**Estado: R12–R15 SUPERADAS** (confirmación del equipo el 10 de noviembre de 2026: «Las pruebas R12–R15 del combustible están confirmadas»). R1–R11: el equipo indicó el 10 de noviembre que el funcionamiento general es correcto, sin confirmación prueba por prueba; no se registran individualmente como superadas. Copia de esta versión: `CopiasSeguridad/Copia_funcional_combustible_superado_2026-11-10.zip`.

Opcional; no da puntos. Valores provisionales (DEMOSTRACIÓN): ventana de 22 s (18 s en Modo Prueba), que se abre al 85 % del
viaje con la avería reparada y los escombros terminados; corredor a 5–9 m de la posición de la nave; tolerancia 1,5 m;
2 s seguidos dentro; «a tiempo» si se completa en la primera mitad de la ventana. Periodo del marcador de inserción:
2,2 s sin corregir, 3,3 s a tiempo, 2,75 s tarde. Anota en cada prueba el combustible de maniobra que muestra el panel
de Pilotaje durante la ventana (y el de Resultados).

| # | Pasos | Resultado esperado |
|---|---|---|
| R1 | Modo Prueba. Tras los escombros, sin levantarte de Pilotaje, centra el anillo y mantenlo 2 s | Al terminar los escombros: aviso «CORRECCIÓN DE TRAYECTORIA (opcional, 18 s)», anillo cian delante y panel con segundos restantes. Dentro de 1,5 m el anillo y el panel pasan a verde y cuentan la permanencia; a los 2 s: «Trayectoria corregida a tiempo… periodo 3,3 s». Anota los segundos usados |
| R2 | Igual, pero espera a que el panel diga «ya contaría como tarde» antes de completar | «corregida tarde», periodo 2,75 s |
| R3 | Entra en la tolerancia y sal antes de 2 s | La permanencia vuelve a 0; no se completa hasta volver a estar 2 s seguidos dentro |
| R4 | Ignora la tarea (o quédate en otro puesto) | A los 18 s: «Ventana de corrección terminada sin corregir… periodo 2,2 s»; sigue la autorización en Comunicaciones y el frenado de siempre |
| R5 | Termina los escombros con la nave muy desplazada (cerca del límite) | El anillo aparece a 5–9 m de la posición actual, dentro del límite; nunca se completa solo; se puede alcanzar con WASD |
| R6 | Con WASD | El anillo se mueve en el mismo sentido en que se movían las rocas al esquivarlas; no aparecen escombros nuevos |
| R7 | Frenado tras R1 / R2 / R4 | El marcador de la barra de inserción oscila con periodo 3,3 / 2,75 / 2,2 s; el panel de Pilotaje lo indica. El combustible del frenado sigue dependiendo de cuándo pulses Espacio |
| R8 | Resultados tras R1 y R4 | TOTAL y bonus de observación iguales que antes de este cambio; explicación con el resultado de la corrección, el periodo y el combustible de maniobra de la ventana, sin afirmar ahorro |
| R9 | Modo Prueba, empezando la ventana desde Ingeniería (levántate tras los escombros y ve allí antes de que termine el aviso) | Medir si da tiempo a volver a Pilotaje, maniobrar y mantener 2 s dentro de los 18 s. Anotar segundos |
| R10 | Desactiva *Correccion → Activa* | Sin anillo, avisos, textos ni demora: tras los escombros se llega como antes y el marcador va a 2,2 s |
| R11 | Tiempos normales, partida completa con observación: captura, envía parte antes de los escombros y parte después | La ventana de corrección (unos 127–149 s) coincide con el tiempo de envío restante: el jugador elige; la llegada no se retrasa si los escombros terminaron antes del 85 % |
| R12 | En crucero, mira el HUD (barra «Combustible») y el panel de Pilotaje sin pulsar nada; luego mantén W 2 s y suelta | En reposo no hay «▼». Mientras pulsas: «▼ 2,5 kg/s» (motor estándar; 3,4 con alto empuje) y el valor baja con un decimal; al soltar, el estabilizador sigue gastando un momento y luego «▼» desaparece. En 2 s deben bajar unos 5 kg (más lo del estabilizador) |
| R13 | Abre la Console de Unity (filtro «[Combustible]») durante una partida | Líneas: inicio del crucero; apertura y cierre de la ventana de corrección (antes → después y kg de la ventana); antes y después de la inserción con el coste |
| R14 | Antes de pulsar Espacio en la inserción, anota «Combustible ahora» del panel; después, el panel en órbita | Antes − coste = después; el coste está entre el de «perfecta» y el de «peor» mostrados |
| R15 | Al cerrar la misión, última línea «[Combustible] Cierre» | inicial − maniobras − inserción = final; «diferencia 0,000 kg» (no hay cobros dobles). Resultados muestra los mismos valores con un decimal |


## S. Navegación longitudinal (ControlNave → *Navegacion → Activa*)

**Estado: funcionamiento general confirmado por el equipo el 11 de noviembre de 2026** («La navegación ya la probé y funciona
bien»). Los casos S1–S17 **no** se registran uno a uno como superados: no se confirmaron individualmente.
Copia de esa versión: `CopiasSeguridad/Copia_funcional_navegacion_probada_2026-11-11.zip`.

Valores provisionales (DEMOSTRACIÓN): ruta 8000 m (2500 m en Modo Prueba); salida a 20 m/s; zona ±250 m; captura
≤ 15 m/s; giro 3 s. Las cifras «esperadas» salen de la regla del juego con unos 7000–7300 kg de masa y el motor estándar
(unos 3,6 m/s², 2,5 kg/s); anota las reales. Recomendado: Modo Prueba y *Correccion → Activa* desactivada para S3–S10,
salvo que se diga otra cosa. Mira la Console con los filtros «[Navegación]» y «[Combustible]».

| # | Pasos | Resultado esperado |
|---|---|---|
| S1 | *Navegacion → Activa* desactivada; partida normal | Igual que antes: barra «Viaje … M km (simulado)», sin esquema ni marca de destino; Espacio en Pilotaje en crucero no hace nada; última línea «[Combustible] Cierre» sin «motor principal» |
| S2 | Activada. Mantén Espacio desde la separación de etapa hasta después del «En ruta» | El motor NO se enciende hasta soltar Espacio (panel: «MOTOR BLOQUEADO: suelta Espacio…»). La cuenta atrás y la separación funcionan como siempre. En Ingeniería, R sigue siendo el paso 2 de la reparación y no gira la nave |
| S3 | **Aceleración.** En Pilotaje, proa al destino, mantén Espacio 4 s | La velocidad sube unos 14 m/s (≈ 3,6 m/s²); «▼ 2,5 kg/s»; «motor principal» sube unos 10 kg; leve vibración; las estrellas se aceleran |
| S4 | **Inercia.** Suelta Espacio y no toques nada 10 s | Velocidad constante, combustible constante, la distancia al destino baja a ritmo fijo |
| S5 | **Giro.** Pulsa R y, durante el giro, mantén Espacio | Barra «GIRANDO 180°» 3 s; estrellas y luna giran alrededor de la nave; velocidad sin cambios; panel «MOTOR BLOQUEADO: la nave está girando». Al terminar: «proa hacia fuera» y marca «DESTINO DETRÁS». En el esquema el triángulo cambia de ▶ a ◀ |
| S6 | **Frenado.** Tras S5, mantén Espacio sin soltar | La velocidad baja; al llegar a 0 se queda en 0, aviso «Velocidad 0…», el motor se apaga aunque sigas pulsando y la orientación NO cambia. Soltar y volver a pulsar mueve la nave hacia la proa (se aleja del destino) |
| S7 | **Sobrepaso.** Partida nueva: acelera y no frenes | Al pasar 250 m más allá del destino: «TE HAS PASADO DE LARGO». La luna pasa por debajo y a un lado, sin atravesar la nave; en el esquema la nave queda a la derecha de la zona; panel «se aleja» |
| S8 | **Regreso.** Tras S7: R (proa al destino), Espacio hasta 0, suelta, Espacio de nuevo hasta unos 10 m/s | Frena, se detiene, vuelve; «se acerca»; al entrar en la zona a ≤ 15 m/s: fase Llegada y mensaje de inserción. Resultados: «te pasaste de largo 1 vez» |
| S9 | Entra en la zona a más de 15 m/s | Aviso «la inserción exige ≤ 15 m/s. ¡Frena!»; no hay mensaje de inserción hasta frenar dentro de la zona |
| S10 | En Llegada, tarda en confirmar hasta que la nave salga de la zona | «Has salido de la zona…»; vuelve a crucero. Al volver a entrar, la inserción está disponible sin mensaje nuevo |
| S11 | Con *Correccion → Activa*: llega a la zona con la ventana de corrección abierta (o con escombros) | Panel e instrucción: «INSERCIÓN BLOQUEADA: corrección de trayectoria abierta (x s)». La distancia sigue cambiando (la nave no se congela). Al cerrarse la ventana, si sigues en la zona a ≤ 15 m/s, se autoriza |
| S12 | Tras los escombros, gira y retrocede hasta antes del 60 % de la ruta; vuelve | No se repiten avería, escombros, observación ni corrección |
| S13 | **Falta de combustible.** *Combustible Prueba Activo* con 240 kg. Acelera manteniendo Espacio y mira «margen» en el panel | El HUD marca «[COMBUSTIBLE DE PRUEBA]». El margen baja al acelerar; antes de llegar a 0 kg, la misión se aborta con un motivo que da los kg que quedan, los necesarios (motor + inserción perfecta) y para qué |
| S14 | Inserción normal tras S8 | Antes − coste = después, como en R14; el coste no depende de la velocidad a la que entraste (≤ 15 m/s) |
| S15 | «[Combustible] Cierre» al acabar | inicial − maniobras − motor principal − inserción = final; «diferencia 0,000 kg». Resultados: «motor principal x kg» en Nave y recursos |
| S16 | Tiempos normales, partida completa: acelera unos 8 s, vuela, gira y frena antes de la zona | Anota el tiempo de crucero y el combustible del motor principal (referencia calculada: unos 160 s y 44 kg) |
| S17 | Combustible de prueba activado con *Navegacion → Activa* desactivada | Sin efecto: combustible calculado de siempre |

## T. Destinos configurables, gravedad experimental y claridad de la navegación (11 de noviembre de 2026)

**Estado: NO superada — sin ejecutar.** Implementado sin poder compilar ni ejecutar Unity. Nada de esta sección está
validado por el equipo. Las cifras «esperadas» salen de cálculos con la regla del juego (unos 7200 kg, motor estándar).

| # | Pasos | Resultado esperado |
|---|---|---|
| T1 | Abre Unity y espera a que compile | Consola sin errores (`DestinoMision.cs` es nuevo; Unity crea su `.meta`) |
| T2 | Play desde la selección de misión | En la tarjeta de la misión aparecen dos botones de destino: «● Luna helada (ficticia)» y «○ Planetoide denso (ficticio)», con su diferencia jugable y sus cifras debajo. Cambiar de destino no añade pantallas nuevas |
| T3 | Elige la Luna helada, configura y viaja con *Navegacion → Activa* | Igual que la versión probada: ruta 8000 m (2500 en Modo Prueba), zona ±250 m, captura ≤ 15 m/s. La cabecera de configuración y el panel de estado dicen «Luna helada (ficticia)» |
| T4 | Elige el Planetoide denso y viaja con navegación | Ruta 11 000 m (3200 en prueba), zona ±150 m, captura ≤ 8 m/s; el cuerpo de la ventana se ve anaranjado y un 30 % mayor, sin tapar la ruta. Resultados: «destino: Planetoide denso (ficticio)». Referencia calculada: unos 230 s y 49 kg de motor principal |
| T5 | Planetoide sin navegación (viaje clásico) | Cambian solo el nombre y el aspecto; el resto, como siempre |
| T6 | *Navegacion → Usar Destino Elegido* desactivado | Ruta y llegada con los valores del inspector (como la versión probada), sea cual sea el destino |
| T7 | *Navegacion → Gravedad Experimental* activado, Luna | Fila «GRAVEDAD» en Pilotaje con la aceleración de la gravedad y la del motor por separado; flecha azul en el esquema. Lejos del destino la gravedad es casi 0; cerca, unos 0,4 m/s² como máximo. Consola: «[Navegación] Gravedad EXPERIMENTAL…» |
| T8 | Gravedad, Planetoide: detente lejos del destino (velocidad 0) y espera | La nave empieza a moverse sola hacia el destino y se acelera; al llegar al borde de la zona irá más rápido que 8 m/s: hay que frenar cerca de la zona |
| T9 | Gravedad: entra en la zona a velocidad de captura y no insertes | La nave se acelera hacia el centro sin perder la autorización («en el borde llevarías…» no cambia). Al salir por el otro lado vuelve al crucero y la gravedad la frena: en el Planetoide siempre la trae de vuelta; en la Luna, si entraste cerca de 15 m/s, puede escaparse despacio (unos 7–8 m/s) y hay que frenar y volver |
| T10 | Gravedad: pasa por el punto de paso a cualquier velocidad | Nunca hay un salto de velocidad ni la nave «atraviesa» el cuerpo: la ruta pasa a su lado (distancia de paso ≥ radio de seguridad) |
| T11 | En un destino, pon *Distancia Paso M* menor que *Radio Seguridad M* (en el CatalogoMision) | Aviso en Consola: se usa el radio de seguridad como distancia de paso |
| T12 | Pilotaje en crucero con navegación | Primera línea «AHORA:» con la acción recomendada (verde, ámbar o rojo); filas MOVIMIENTO, PROA (con «Espacio ahora ACELERA / FRENA»), MOTOR, LLEGADA y COMBUSTIBLE |
| T13 | Gira con R mientras te acercas | La fila PROA cambia a «◀ proa hacia fuera · Espacio ahora FRENA»; MOVIMIENTO sigue diciendo «se acerca» |
| T14 | Acércate rápido sin frenar | Al llegar a la distancia necesaria: aviso único «MOMENTO DE FRENAR…» y «AHORA: ¡FRENA YA!» en rojo |
| T15 | Combustible de prueba de 240 kg y acelera | Avisos únicos al bajar el margen de 30 kg y de 10 kg; la barra de combustible del HUD muestra el margen en ámbar y en rojo |
| T16 | Con un puesto abierto, deja el destino a la derecha de la pantalla | La marca del destino se queda a la izquierda del panel, no debajo; si queda detrás: «DESTINO DETRÁS · … · te acercas/te alejas» y la pista de girar con R |
| T17 | Llega a la zona con la avería sin reparar o con la corrección abierta | «AHORA: Inserción bloqueada: … Para resolverlo, …» con la acción concreta y el consejo de detenerse dentro de la zona |
| T18 | Resultados de una misión con navegación | Desglose: «Al partir…», «− maniobras laterales · − motor principal · − entrada en órbita», «= al final … (cuadra)». Con gravedad, una línea con el cambio de velocidad del motor y el de la gravedad |

## U. Vuelo 3D · prueba V1 (escena VueloPrueba)

**Estado: funcionamiento general confirmado por el equipo el 11 de noviembre de 2026** («Ya probé V1 y me gustó mucho la
presentación y la sensación de movimiento»; el problema señalado: frenar resulta complicado). Los casos U1–U17 **no** se
registran uno a uno como superados. Copia de esa versión: `CopiasSeguridad/Copia_V1_probada_antes_frenado_2026-11-11.zip`.
Cifras esperadas calculadas con la regla del juego (unos 7300 kg, motor estándar).

| # | Pasos | Resultado esperado |
|---|---|---|
| U1 | Abre Unity y espera a que compile | Consola sin errores. Aparece el menú *Prototipo → 12. Crear escena VueloPrueba (vuelo 3D V1)* |
| U2 | Ejecuta el menú 12 | Se crea `Scenes/VueloPrueba.unity` con un objeto `PruebaVuelo3D` que tiene los dos catálogos asignados. La escena Nave no cambia (fecha de modificación igual). Ejecutarlo otra vez solo avisa |
| U3 | Abre VueloPrueba y pulsa Play | Vista desde el asiento con marco de cabina; estrellas, planeta, satélite y balizas; aviso de inicio; paneles NAVE y OBJETIVO; ayuda abajo. Consola: «[VueloPrueba] Datos de la nave: catálogo…» |
| U4 | Mantén D 1 s y suelta (estabilización activada) | La nave gira a la derecha unos 8° y se detiene sola; la barra de guiñada sube y vuelve a 0 |
| U5 | T (desactivar), D 1 s y suelta | Sigue girando a unos 8 °/s sin parar; A durante 1 s la detiene. Se puede parar en cualquier ángulo |
| U6 | Prueba W/S y Z/C | W baja el morro, S lo sube; Z alabea a la izquierda, C a la derecha. Nada endereza la nave por sí solo. Si algún sentido sale al revés, anótalo |
| U7 | Espacio 4 s mirando al frente | Unos 14 m/s; unos 10 kg de motor principal; la marca O (movimiento) coincide con la + (proa) |
| U8 | Después de U7, gira unos 90° | La velocidad no cambia; O se queda donde estaba y la + se mueve; el panel dice unos 90° entre proa y movimiento |
| U9 | I, J, L, U, O, K por separado | Cada tecla cambia la velocidad en su eje de la nave (panel «En ejes de la nave») a unos 0,2 m/s²; gasta en «traslación», no en «motor principal» |
| U10 | V | Cámara exterior: nave provisional, llama del motor con Espacio y propulsores pequeños que se iluminan al trasladar o girar. V otra vez vuelve al asiento |
| U11 | Botón derecho del ratón y mueve el ratón | Mira alrededor (asiento) u orbita (exterior); al soltar vuelve al frente. No cambia la orientación de la nave |
| U12 | Vuela a la Baliza 1 y quédate a menos de 50 m a 1 m/s o menos | El panel pasa de «Acércate…» a «Frena…» y a «Mantente así»; a los 2 s, «Visita registrada» y el objetivo cambia a la Baliza 2 con sus círculos de zona |
| U13 | Visita la Baliza 2 | «PRUEBA COMPLETADA» con tiempo y combustible; la nave sigue volable |
| U14 | Marca de la baliza objetivo fuera de la vista o detrás | Flecha en el borde con su nombre y «(detrás)» si queda a la espalda |
| U15 | F5 en cualquier momento | Vuelve a la salida: quieta, sin giro, combustible inicial, objetivo en la Baliza 1 |
| U16 | *Combustible Prueba Activo* con 20 kg; acelera con Espacio | A los 8 s aviso «SIN COMBUSTIBLE»; ningún propulsor responde; la nave conserva velocidad y giro |
| U17 | Gastos al terminar | Motor + traslación + orientación + restante = combustible inicial (panel NAVE) |

## V. Vuelo 3D V1 · frenado más accesible

**Estado: funcionamiento general confirmado por el equipo el 11 de noviembre de 2026** («Confirmo que probé V1 y el
ajuste de frenado y quedaron bien»). Los casos V1–V13 **no** se registran uno a uno como superados.
Copia de esta versión: `CopiasSeguridad/Copia_funcional_V1_frenado_2026-11-11.zip`. No se han cambiado masa,
combustible ni motores.

| # | Pasos | Resultado esperado |
|---|---|---|
| V1 | Compila y pulsa Play en VueloPrueba | Sin errores. Panel nuevo «FRENADO DEL DESPLAZAMIENTO»; la fila de la nave dice «ESTABILIZACIÓN DE GIRO (T) · solo frena el GIRO, no la velocidad» |
| V2 | Acelera, suelta y pulsa T varias veces | La velocidad no cambia con T; solo el giro. La ayuda lo explica |
| V3 | Con velocidad, gira la proa hacia la X («FRENAR: lleva la proa aquí») | El panel baja el desvío; por debajo de 10°: «bien orientada: Espacio FRENA» y, bajo la mira, «ALINEADA PARA FRENAR» |
| V4 | Con la proa hacia la marca O (movimiento) | El panel dice «Espacio ACELERARÍA» |
| V5 | Estimaciones | Con motor y solo traslación: tiempo, distancia y combustible para parar; se actualizan al girar y frenar. Referencia: a 30 m/s con la proa al frente, ≈ 28 s y ≈ 460 m con el motor |
| V6 | **Frenar avanzando de frente:** Espacio hasta unos 30 m/s, suelta, B | «ORIENTANDO…» con giro progresivo (unos 180°), después «FRENANDO con el motor principal», después «con traslación» y aviso de terminado a ≤ 0,05 m/s. Referencia de la réplica: ≈ 28 s, ≈ 26 kg |
| V7 | **Frenar con movimiento lateral:** L hasta unos 3 m/s, suelta, B | No gira: frena solo con traslación y termina. Referencia: ≈ 16 s, ≈ 7 kg |
| V8 | **Frenar mirando a otro lado:** avanza a unos 20 m/s, gira la proa unos 90°, B | Gira la proa hasta la X por el camino más corto, frena con el motor y termina con traslación. Referencia: ≈ 21 s, ≈ 19 kg. El alabeo no se endereza respecto a ningún «arriba» |
| V9 | **Cancelar:** durante la asistencia, pulsa B; repite y pulsa cualquier mando | Aviso «CANCELADO»; el empuje de la asistencia se corta en ese momento y la nave conserva su velocidad y su giro |
| V10 | **Poco combustible:** *Combustible Prueba Activo* con 3 kg; acelera un poco y B | Aviso de que puede no bastar; al agotarse, «Asistencia detenida: sin combustible» con la velocidad restante. Lo gastado figura en el panel |
| V11 | **Cerca de una baliza:** acércate a unos 5 m/s y pulsa B a unos 200 m | Se detiene antes de la baliza (la estimación de distancia indica dónde) y no sigue empujando al terminar: la velocidad se queda en ≤ 0,05 m/s y la distancia casi no cambia |
| V12 | Asistencia con la nave ya quieta | «ya está prácticamente quieta»; no se activa |
| V13 | Objetivo: pasa junto a una baliza sin acercarte ni alejarte | El panel dice «ni te acercas ni te alejas» con la velocidad lateral; no cuenta como detenida mientras la velocidad total supere 1 m/s |

## W. Hangar de prueba · nave base y bloque interior (escena HangarPrueba)

**Estado: NO superada — sin ejecutar.** El código se revisó a mano; no se ha compilado ni ejecutado en Unity.

| # | Pasos | Resultado esperado |
|---|---|---|
| W1 | Menú *Prototipo → 13*; abre `HangarPrueba` y pulsa Play | Compila sin errores. Se ve la nave en el hangar y los paneles. Nave, VueloPrueba y Configuración no cambian; la lista de escenas del build tampoco |
| W2 | Pulsa ▶ del motor varias veces | El motor cambia **en la popa** (toberas de otro tamaño y número); el texto del panel cambia con él |
| W3 | Pulsa ▶ del tanque varias veces | El tanque del módulo de servicio cambia de largo y diámetro; con el ampliado el módulo se alarga y el motor se desplaza a su nuevo anclaje |
| W4 | Tras cada cambio, mira MASA, COSTE Y LANZADOR y LÍMITES | Estructura, motor, tanque vacío, combustible, carga y total cambian según el catálogo; coste y margen del lanzador también; los límites coinciden con los de Configuración para la misma elección |
| W5 | Cambia motor y tanque diez veces seguidas | Los contadores de piezas en cada anclaje siguen en 1; en la jerarquía no quedan piezas viejas |
| W6 | **C** (corte) y **L** (colores) en exterior | El interior se ve en su sitio dentro del casco: pasillo, salas, esclusa junto a la escotilla de estribor, reserva de carga, Ingeniería en popa; colores según el estado de cada medida |
| W7 | **I** y recorre el pasillo hasta Ingeniería; entra en cada sala | Banda «CÁMARA DE INSPECCIÓN PROVISIONAL»; puertas de 2,0 m alineadas con las salas; la escotilla de la esclusa da al exterior por el mismo hueco que se ve desde fuera |
| W8 | Teclas **1, 2, 3, 4** | Vista sentada en cada puesto, mirando a su consola, con su figura de referencia al lado; ninguna consola o asiento atraviesa un muro |
| W9 | Maniquíes del pasillo | Uno de pie y otro en horizontal caben a la vez junto a los asideros; la altura libre (3,4 m) supera claramente la figura de 1,75 m |
| W10 | Tecla **0** | Vista en la esclusa: se ven la escotilla exterior (1,4 × 2,0) y la puerta al pasillo |
| W11 | Cambia motor y tanque; pulsa «Recargar hangar» | La escena vuelve a cargarse con el mismo motor y tanque; aviso «Configuración recuperada» |
| W12 | Pulsa «Abrir Configuración» | Configuración muestra el mismo motor y tanque elegidos en el hangar |
| W13 | Panel derecho y etiquetas | Se lee que las figuras son referencias y no bots ni jugadores, y que la pasarela no tiene embarque |

## X. Nave de expedición modular EM-4X (escena ExpedicionPrueba)

**Estado: aceptación global del equipo, 12 de noviembre de 2026** («Ya revisé en Unity la EM-4X y me gusta. Acepto su
aspecto general, tamaño y distribución como base para continuar»). Los casos X1–X14 **no** se registran uno a uno como
superados. Esta aceptación **no** valida el movimiento de personajes con colisiones, el vuelo ni el balance de la nave.
Las 463 comprobaciones del panel son la revisión geométrica y de conexiones del modelo; no equivalen a probar
personajes, puertas, vuelo ni balance. Copia: `CopiasSeguridad/Copia_funcional_EM4X_aceptada_2026-11-12.zip`.
Antes de esta aceptación: código revisado leyéndolo (también por una segunda revisión independiente, que no confirma
compilación) y distribución comprobada con un modelo en Python con los mismos datos.

**Comprobación confirmada por el equipo (12 de noviembre de 2026):** «hice la comprobación que me pediste en Unity y el
resultado está bien». Se registra como la comprobación pedida en la entrega: al pulsar Play, el panel de revisión de
ExpedicionPrueba muestra 0 problemas (caso X2). Solo ese alcance: no se marca ningún otro caso como superado.

| # | Pasos | Resultado esperado |
|---|---|---|
| X1 | Menú *Prototipo → 14*; abre `ExpedicionPrueba` y pulsa Play | Compila sin errores ni avisos nuevos en la consola. Se ve la nave completa. HangarPrueba, Nave, VueloPrueba, Configuración y la lista de escenas no cambian |
| X2 | Panel izquierdo · REVISIÓN | «[OK] 463 comprobaciones correctas» y ningún problema; la consola dice lo mismo |
| X3 | Panel izquierdo · tamaño y volumen | 57,9 × 29,0 × 25,2 m; unos 2263 m³ presurizados sin la lanzadera (2579–2580 con ella) |
| X4 | Recorridos | Seis recorridos entre puestos de 17,3 a 22,4 m |
| X5 | Gira alrededor con el botón derecho | Se identifican módulos arriba, abajo, a los lados, delante y detrás; la lanzadera bajo el mando; rótulos con medidas |
| X6 | **X** (esquema) | Líneas naranja por cada unión con escotilla, gris en la unión con la propulsión, rojas para los recorridos |
| X7 | **C** varias veces | Cuatro estados: horizontal, longitudinal, transversal por el nodo y sin corte; se ve el interior sin piezas sueltas fuera de sitio |
| X8 | **1, 2, 3, 4** | Vista en cada puesto, mirando a su consola, con su figura de referencia; Pilotaje mira por la ventana de proa |
| X9 | **0** y **6** | Esclusa con escotilla exterior y dos trajes; cabina de la lanzadera con 4 asientos |
| X10 | **5** y recorre la zona común | Mesa y pantalla de misión en la mitad de popa; escotillas radiales sin obstáculos; caben los cuatro alrededor de la mesa |
| X11 | Recorre del nodo al hábitat y a la bodega | Se pasa por las escotillas de «techo» y «suelo» sin escaleras; los camarotes no invaden el paso |
| X12 | Recorre todos los módulos | Ningún equipo atraviesa un casco ni tapa una escotilla; los adaptadores tienen asideros |
| X13 | Juicio de espacio (tuyo) | Anota qué medidas aceptas o cambias: diámetro 6,0 / 5,4 / paso 3,8, nodo de 10 m, largos de módulo |
| X14 | Textos en pantalla | Banda «CÁMARA DE INSPECCIÓN PROVISIONAL… no valida… colisiones»; figuras «referencia 1,75 m · no es bot ni jugador» |

## Y. Circulación interior: personaje, pasadizos y puertas (escena CirculacionPrueba)

**Estado: confirmación general del equipo, 12 de noviembre de 2026** («Ya probé CirculacionPrueba y todo lo que comprobé
funciona bien»). Los casos Y1–Y15 **no** se registran uno a uno como superados. El equipo pidió después un ajuste nuevo de
espacio (pasadizos permanentes más amplios), que no corrige un fallo de esta versión. Antes de esta prueba: código revisado
leyéndolo (también en una revisión independiente que no confirma la compilación) y geometría de puertas y paneles
comprobada con el modelo en Python.

| # | Pasos | Resultado esperado |
|---|---|---|
| Y1 | Menú *Prototipo → 15*; abre `CirculacionPrueba` y pulsa Play | Compila sin errores. Aparece el personaje en la zona común. ExpedicionPrueba, HangarPrueba, Nave, VueloPrueba, Configuración y la lista de escenas no cambian |
| Y2 | Panel derecho · REVISIONES AUTOMÁTICAS | Tres líneas [OK] con 0 problemas: distribución EM-4X (463), pasos/puertas/paneles y comprobación con colisiones al iniciar |
| Y3 | Muévete flotando por la zona común | Sin gravedad; se puede girar en cualquier sentido (Z/X) y R nivela con el «arriba» del módulo |
| Y4 | Recorre los 7 pasadizos abiertos | Se cruzan sin puertas; las paredes, los equipos y los bordes de las escotillas detienen al personaje |
| Y5 | Llega a los cuatro puestos | Pilotaje, Ingeniería, Ciencia y Comunicaciones aparecen marcados en «Lugares alcanzados» |
| Y6 | Sube al Hábitat y a Sistemas; baja a la Bodega y a la Esclusa | Se entra flotando, sin escaleras; en la Bodega y la Esclusa hay que abrir sus puertas |
| Y7 | Mira un panel y pulsa **E** | Las hojas se deslizan a los lados (no desaparecen) en unos 1,2 s; la luz pasa de rojo a ámbar y a verde |
| Y8 | Con la puerta cerrada, intenta pasar | La puerta cerrada bloquea el paso |
| Y9 | Abre la unión Mando ↔ Lanzadera desde el mando, entra en la lanzadera y ciérrala desde dentro | Las dos puertas se mueven a la vez; hay panel a cada lado; nadie queda entre las dos puertas cerradas |
| Y10 | Lo mismo con Nodo central ↔ Bodega | Igual que Y9 |
| Y11 | Quédate en medio de una escotilla con puerta y pulsa E para cerrar | No cierra (o vuelve a abrir si ya cerraba) y lo explica en pantalla |
| Y12 | Intenta abrir la escotilla exterior de la esclusa y el puerto de suministro | Están cerrados y no tienen panel |
| Y13 | Empuja contra un obstáculo o intenta girar dentro de un adaptador | Aparece «Bloqueado contra: …» con la pieza; anota dónde te enganchas o el paso se queda corto |
| Y14 | **Tab** | Cambia a la cámara de inspección (sin colisiones, con su panel); Tab vuelve al personaje donde estaba |
| Y15 | Juicio de circulación (tuyo) | Anota qué accesos, puertas o pasos no funcionan o resultan incómodos |

## Z. Pasadizos permanentes amplios (ExpedicionPrueba y CirculacionPrueba)

**Estado: NO superada — sin ejecutar.** Modelo en Python con los mismos datos: 0 problemas (distribución, puertas y
paneles). Código revisado leyéndolo (también en una revisión independiente que no confirma la compilación). **No se ha
compilado ni ejecutado en Unity.**

| # | Pasos | Resultado esperado |
|---|---|---|
| Z1 | Play en ExpedicionPrueba | Compila; panel REVISIÓN con 0 problemas; tamaño 59,7 × 30,2 × 25,8 m; pasadizos con rótulo «pasadizo 2,4 × 2,4 · 1,2 m» |
| Z2 | Play en CirculacionPrueba | Las tres revisiones con 0 problemas |
| Z3 | Pasa con el personaje por los 7 pasadizos permanentes, también pegado a paredes y esquinas | Sin bordes invisibles, huecos ni enganches; anota cualquier sitio donde se frene |
| Z4 | Sube al Hábitat y a Sistemas | Se entra flotando por pasadizos de 2,4 y 2,2 m |
| Z5 | Abre, cruza y cierra las uniones de Lanzadera, Bodega y Esclusa | Las puertas siguen funcionando como antes, en su sitio tras el desplazamiento |
| Z6 | Compara ambas escenas (Tab → inspección en CirculacionPrueba) | Misma distribución y mismas medidas |
| Z7 | Sensación de los pasadizos (tuyo) | Anota si se perciben como corredores y si los recorridos siguen siendo razonables |

## AA. Puertas de aislamiento normalmente abiertas (CirculacionPrueba)

**Estado: NO superada — sin ejecutar.** Geometría comprobada con el modelo en Python (0 problemas). Código revisado
leyéndolo (también en una revisión independiente que no confirma la compilación). **No se ha compilado ni ejecutado en
Unity.** La prueba de los pasadizos ampliados (sección Z) sigue pendiente y se hace junto con esta.

| # | Pasos | Resultado esperado |
|---|---|---|
| AA1 | Play en CirculacionPrueba | Compila; las tres revisiones con 0 problemas; las 7 puertas de aislamiento aparecen abiertas (luz verde) |
| AA2 | Recorre la nave sin tocar paneles | Se pasa por todos los pasadizos sin abrir nada; las hojas abiertas no estrechan el paso |
| AA3 | Cierra y abre cada puerta de aislamiento desde un lado y desde el otro | Las hojas se deslizan (no desaparecen); se controla desde ambos lados sin cruzar la puerta |
| AA4 | Con una cerrada, intenta pasar | Bloquea el paso |
| AA5 | Quédate en el hueco y pulsa E para cerrar | No cierra o vuelve a abrir, con aviso |
| AA6 | Lanzadera, Bodega y Esclusa | Siguen funcionando como antes |
| AA7 | Busca bordes invisibles o enganches junto a las hojas abiertas y las guías | No los hay; anota cualquier sitio donde se frene |

---

## AB. MVP de la expedición (escena ExpedicionMVP) — 13-11-2026

**Estado: NINGUNA SUPERADA. Código escrito y leído; no compilado ni ejecutado por quien lo escribió.** Marcar solo lo que el equipo confirme.

### AB0. Comprobación breve de la EM-4X candidata (vale también para Z y AA)
En `CirculacionPrueba`, Play:
- [ ] Las tres revisiones del panel marcan 0 problemas.
- [ ] Se cruza cada pasadizo ancho por el centro y pegado a una pared sin engancharse (Z).
- [ ] Las 7 puertas de aislamiento empiezan abiertas, se cierran y abren con E desde los dos lados, cerradas bloquean y no cierran sobre el personaje (AA).

### AB1. Arranque
- [ ] El proyecto compila sin errores tras copiar los archivos.
- [ ] El menú 16 crea la escena y Play muestra la portada con la EM-4X en órbita al fondo.

### AB2. Preparación
- [ ] Cambiar opciones actualiza el total sin acumular; el ejemplo A da 34,3 M.
- [ ] Superar 100 M impide confirmar. La misión 3 sin sonda impide confirmar.
- [ ] Confirmar bloquea las opciones; reabrir las desbloquea y el total coincide.
- [ ] El tanque ampliado permite más tramos y baja la aceleración mostrada.

### AB3. Interior y Pilotaje
- [ ] Se llega andando a Pilotaje, Ciencia, Comunicaciones, Ingeniería, Bodega y Mesa, y E abre cada puesto.
- [ ] Al dejar Pilotaje el tiempo de vuelo y la distancia a la base siguen cambiando; al volver, posición, velocidad y combustible no han saltado.
- [ ] Tab abre la cámara de inspección y vuelve.

### AB4. Vuelo con gravedad
- [ ] Órbita de referencia: sin tocar nada, ×16 durante una órbita (22 min de vuelo): la nave sigue cerca de la base (cientos de metros).
- [ ] Una maniobra retrógrada baja el «punto más bajo»; la línea prevista cambia.
- [ ] Frenar mucho da el aviso de trayectoria de impacto y, si no se corrige, la misión termina contra Kora.
- [ ] B anula la velocidad relativa al objetivo seleccionado, no la velocidad orbital.
- [ ] Sin combustible los mandos no responden y la nave sigue su órbita.

### AB5. Misiones
- [ ] Misión 1 completa: banda, 3 capturas, transmisión, regreso, resultados.
- [ ] Misión 2 completa: encuentro, diagnóstico, bus de servicio, orden, informe, regreso.
- [ ] Misión 3 completa: escaneos, T-7, sonda, transmisión, regreso, con la brecha guionizada reparada.
- [ ] Regreso anticipado da éxito parcial; pedir rescate da misión fallida.

### AB6. Daño y reparación
- [ ] Un roce lento no causa brecha; un choque más rápido sí; uno muy rápido termina la misión.
- [ ] Un solo choque registra un solo impacto.
- [ ] La brecha sigue al cambiar entre Pilotaje e interior; alarma y luz roja en el módulo.
- [ ] Cerrar la puerta de aislamiento reduce la fuga y no repara.
- [ ] Con kit, mantener E en la baliza sella la brecha y gasta el kit; sin kit no.
- [ ] Una brecha grave protege un bus y «Restablecer» solo funciona tras reparar.

### AB7. Ejecutable
- [ ] El menú 18 genera el ejecutable.
- [ ] En el ejecutable se ven materiales, rótulos y pantallas (sin objetos rosa).
- [ ] Las tres misiones se completan en el ejecutable.

---

## AC. Sustitución visual e importación de una pieza — 13-11-2026

**Estado: NINGUNA SUPERADA. Escrito y leído; no compilado ni ejecutado por quien lo escribió.**

- [ ] AC1. El proyecto compila con los archivos nuevos.
- [ ] AC2. El menú 19 crea `Assets/_Arte/…` y `Data/RecursosVisualesEM4X.asset`; el menú 20 crea ModeloPrueba.
- [ ] AC3. ModeloPrueba **sin modelos**: el informe dice «sin modelo» en las 4 entradas. Las tres revisiones de circulación dan 0 problemas y se juega igual que CirculacionPrueba.
- [ ] AC4. **Con una pieza de prueba** (puede ser un cubo de 1 m exportado desde Blender como `EM4X_CI_ConsolaCiencia_v01`, asignado con «Escalar a la caja» y después «Tal cual»):
  - aparece en el sitio de la consola;
  - el informe avisa del tamaño cuando no coincide;
  - F3 alterna el modelo y el bloque;
  - se sigue chocando con la caja provisional.
- [ ] AC5. Con un modelo que trae colisiones, el informe dice que se han quitado y no aparecen bloqueos nuevos.
- [ ] AC6. Con un material Standard (pipeline antiguo), el informe lo marca como no URP.
- [ ] AC7. F12 guarda 4 imágenes en `Capturas/` y la planta se corresponde con `em4x_vistas_actuales.png`.
- [ ] AC8. ExpedicionMVP con la tabla asignada: las misiones, puertas, reparación y Pilotaje funcionan igual que sin tabla.

---

## AD. Hangar terrestre (escena HangarExpedicion) — 13-11-2026

**Estado: AD1–AD12 SUPERADAS — confirmadas por el usuario el 13-11-2026 en Unity, sobre la versión guardada en `CopiasSeguridad/Copia_funcional_hangar_validado_2026-11-13.zip`.** Los cambios posteriores (sección AE) necesitan pruebas nuevas.

- [x] AD1. Compila. El menú 22 crea la escena; si se repite, solo la abre.
- [x] AD2. Play: se camina por el suelo con WASD y ratón, sin atravesar paredes, terminales ni estanterías. Los controles se ven en pantalla.
- [x] AD3. Al acercarse a cada estación aparece su nombre y «E: usar» o «E: preparar».
- [x] AD4. Terminal abierta: cursor libre, personaje quieto. Esc o «Cerrar» devuelven el control. Esc fuera de una terminal libera el cursor; Esc o clic lo recuperan.
- [x] AD5. Misión: se ven las tres con objetivo y requisitos; al seleccionar una queda marcada y aparece arriba.
- [x] AD6. Configuración: cambiar motor, tanque, instrumento, relevo o combustible actualiza los valores, el presupuesto y el holograma (módulos en amarillo).
- [x] AD7. Carga: subir y bajar suministros o kits cambia las cajas de la estantería. E en la estantería las lleva a la zona de envío. Al cambiar una cantidad, solo ese lote vuelve a la estantería. No se pueden pasar los máximos.
- [x] AD8. Revisión: la lista coincide con lo que falta. Con un problema (por ejemplo, la misión 3 sin sonda) no se puede cerrar. Cerrar bloquea los cambios y reabrir conserva las selecciones.
- [x] AD9. Embarque: con algo pendiente explica qué falta; con todo listo, «Confirmar la salida y embarcar» abre ExpedicionMVP directamente en los mensajes de transición, sin portada ni otra preparación.
- [x] AD10. En la misión, la configuración (motor, tanque, instrumento, combustible, kits, suministros, sonda) es exactamente la del hangar, y el presupuesto de los resultados coincide.
- [x] AD11. Al terminar, «Volver al hangar terrestre» lleva al hangar con las selecciones conservadas y la carga por preparar.
- [x] AD12. Abrir ExpedicionMVP directamente sigue mostrando la portada y su preparación de siempre.

## AE. Preparación dinámica, holograma, carro, combustible, despegue, escenas y ejecutable — 13-11-2026

**Estado: AE1–AE9 SUPERADAS — confirmadas por el usuario el 13-11-2026 (compilado y probado en Unity) sobre la versión guardada en `CopiasSeguridad/Copia_validada_AE_AF_2026-11-13.zip`.** Los cambios posteriores (sección AG) necesitan pruebas nuevas. Nota: los menús antiguos citados en secciones anteriores (16, 17, 18, 19…) tienen ahora la ruta de la tabla de `LEEME_EQUIPO.md`.

Preparación común: *Prototipo → Juego → 3. Abrir HangarExpedicion* → Play. Primero comprobar que la consola no muestra errores de compilación.

### AE1. Tres alternativas y persistencia de la selección
- [x] Terminal 2: motor, tanque e instrumento muestran 3 opciones cada uno, con empuje/consumo/masa/precio (motor), capacidad/masa/precio (tanque) y consumo/datos/utilidad por misión (instrumento).
- [x] Elegir «Tanque ligero» con 6 tramos: los tramos bajan a 3 y aparece el aviso de cambio de velocidad si no alcanza.
- [x] Elegir el sensor de diagnóstico con la misión K-2: sin aviso de instrumento; con otra misión, aviso de no recomendado.
- [x] Salir y volver a entrar en la terminal: la selección se conserva. Tras embarcar, la llegada muestra esas mismas piezas (AE7).

### AE2. Holograma
- [x] E delante de la mesa central → vista propia; botón derecho + arrastrar y flechas giran; la rueda acerca solo fuera del panel.
- [x] Pestañas MOTOR/TANQUE/INSTRUMENTO resaltan el módulo correspondiente en amarillo.
- [x] Pulsar una alternativa: la pieza actual se separa, entra la nueva (color distinto) y aparece la comparación. El presupuesto de la banda superior NO cambia.
- [x] «Volver a la pieza actual» y Esc descartan la previsualización sin cambiar nada.
- [x] «ENCARGAR ESTE CAMBIO» cambia la selección (la terminal 2 la muestra igual) y el total se recalcula una sola vez.
- [x] Con la preparación cerrada, «Encargar» está desactivado y se indica reabrirla.

### AE3. Costes sin duplicación
- [x] Anotar el total. Encargar motor A → B → A: el total vuelve exactamente al inicial.
- [x] Preparar, llevar y entregar todos los lotes y autorizar el combustible: el total no cambia.
- [x] Cerrar, reabrir y volver a cerrar: el total no cambia y las selecciones se conservan.

### AE4. Carga con carro y cambio posterior
- [x] Con la sonda y kits extra seleccionados, la estantería muestra un contenedor por lote con su cantidad; los lotes a 0 aparecen como «no hace falta».
- [x] E en el asa del carro → se empuja (más despacio). Delante de la estantería de kits, E → el contenedor pasa al carro y el rótulo dice «EN EL CARRO».
- [x] Intentar cargar otro lote con el carro lleno → mensaje, no se carga.
- [x] Entrar con el carro en la zona amarilla → lote LISTO, contenedor colocado en la zona, carro vacío.
- [x] Cambiar después la cantidad de kits → solo el lote de kits vuelve a su estantería y queda por preparar; los demás siguen LISTOS.
- [x] Cambiar la cantidad mientras el contenedor va en el carro → el contenedor vuelve a la estantería.
- [x] Empujar el carro contra una pared → «BLOQUEADO»; R lo devuelve a su base vacío y el lote que llevaba sigue por preparar.
- [x] El recorrido no se vuelve inestable (sin saltos ni objetos que salgan disparados).

### AE5. Puesto de combustible
- [x] Sin tramos: el puesto indica que no hay nada que autorizar y el combustible no aparece como pendiente.
- [x] Con tramos: «Autorizar» está desactivado hasta verificar la alimentación; tras autorizar, el indicador pasa a verde y el lote queda PREPARADO.
- [x] Cambiar los tramos o el tanque después → el combustible vuelve a quedar por autorizar.

### AE6. Embarque bloqueado y despegue
- [x] Con algo pendiente (por ejemplo, combustible sin autorizar), el embarque lo indica y no inicia la secuencia.
- [x] Con todo listo: acceso, asiento, cierre de compuerta, comprobación, cuenta atrás, encendido, ascenso y llegada a la EM-4X. No se puede pilotar.
- [x] La primera vez Espacio no omite. Volver al hangar desde los resultados y embarcar otra vez: Espacio/Intro omiten la secuencia y se llega igual.
- [x] Omitir no cambia el total ni la configuración y no salta requisitos (el embarque sigue bloqueado si falta algo).

### AE7. Llegada con la configuración correcta
- [x] En ExpedicionMVP no aparece otra preparación obligatoria; el informe de llegada lista la misión, piezas, combustible y carga elegidos en el hangar y el mismo total.
- [x] Probar al menos una vez con motor de alta eficiencia + tanque ligero + sensor de diagnóstico.

### AE8. Nueva expedición sin estado residual
- [x] Terminar o volver al hangar: selecciones conservadas, preparación abierta, todos los lotes por preparar, contenedores en sus estanterías, carro en su base, combustible por autorizar.
- [x] Repetir la preparación completa y embarcar: sin cobros dobles ni lotes marcados de la expedición anterior.
- [x] Acceso directo: *Juego → 4. Abrir ExpedicionMVP* → Play sigue mostrando su propia preparación.

### AE9. Menús, escenas y ejecutable
- [x] El menú Prototipo muestra tres grupos: Juego, Arte y pruebas, Prototipos antiguos (con todas las herramientas anteriores dentro).
- [x] Los menús «Abrir…» abren la escena sin modificarla; si no existe (ModeloPrueba antes de crearla), avisan.
- [x] *Juego → 6* genera `Ejecutable/ExpedicionEM4X_Completo/ExpedicionEM4X.exe`; el exe arranca en el hangar y embarcar abre la expedición. Se crea `CopiasSeguridad/ConfiguracionEjecutable_anterior.txt`.
- [x] En el ejecutable, «Volver al hangar terrestre» desde los resultados funciona.
- [x] *Juego → 7* sigue generando el ejecutable anterior (solo ExpedicionMVP).

## AF. Lanzadera L-4 configurable y edificio de integración — 13-11-2026

**Estado: AF1–AF4 SUPERADAS — confirmadas por el usuario el 13-11-2026 (compilado y probado en Unity) sobre la versión guardada en `CopiasSeguridad/Copia_validada_AE_AF_2026-11-13.zip`.** Los cambios posteriores (sección AG) necesitan pruebas nuevas. Abrir: *Prototipo → Juego → 3. Abrir HangarExpedicion* → Play.

### AF1. Bloque visual de la zona industrial
- [x] Desde la entrada, el cartel nombra las zonas A, B, C y D; en la pared derecha hay un paso con el cartel «B · INTEGRACIÓN DE LA LANZADERA L-4 →».
- [x] Se cruza el paso sin caer ni engancharse; el edificio es alto, con la lanzadera de pie sobre su plataforma, dos torres, plataformas, pasarelas, puente grúa y focos.
- [x] Desde el suelo se leen los rótulos BODEGA / TANQUE DE TRANSFERENCIA / PROPULSORES DE REFUERZO con la opción actual.
- [x] Rendimiento aceptable al mirar el edificio (anotar FPS reales; no se han medido).

### AF2. Selección y previsualización
- [x] E en la consola B: vista propia; giro con botón derecho o flechas; la rueda acerca sin atravesar paredes.
- [x] Cada pestaña resalta su pieza en amarillo y muestra 3 alternativas con datos y efecto.
- [x] Pulsar una alternativa: la grúa saca la pieza y coloca la nueva (verde); el presupuesto de la banda superior NO cambia.
- [x] «Volver a la pieza actual» y Esc descartan; al salir la lanzadera vuelve a la configuración confirmada.
- [x] «CONFIRMAR ESTA PIEZA» cambia la representación, el rótulo y el total una sola vez.
- [x] El holograma azul de la EM-4X sigue igual y su selección no cambia al tocar la lanzadera (y al revés).

### AF3. Consecuencias de la configuración
- [x] Sin tocar la lanzadera, el total es el mismo que antes del incremento con la misma selección.
- [x] Misión 3 (sonda) con 1 kit extra y 0 tramos de combustible: bodega pequeña → la sonda y los kits viajan en la L-4, desaparece la línea del envío orbital y el total baja 10 − 1,5 = 8,5 M.
- [x] Con un componente de la EM-4X encargado, el envío orbital sigue cobrándose y aparece el aviso de que la L-4 no ahorra esa tarifa.
- [x] Con 2 tramos encargados y sin refuerzos, previsualizar el tanque de 2 tramos → la comparación marca «SUPERA LA CAPACIDAD» (6800 de 1000 kg) y el botón de confirmar está bloqueado. Con 4 refuerzos confirmados antes, se puede confirmar.
- [x] Con bodega o tanque confirmados, quitar los refuerzos que hacen falta también queda bloqueado (no se puede dejar la lanzadera sin capacidad).
- [x] Con bodega grande sin refuerzos, subir los kits o suministros hasta pasar de 1000 kg → problema de capacidad en la revisión y no se puede cerrar; bajarlos lo resuelve.
- [x] Refuerzos sin bodega ni tanque → aviso de gasto sin efecto.
- [x] Los lotes que viajan en la L-4 se entregan en el muelle verde (zona B); llevarlos a la zona amarilla da un mensaje (una sola vez) y no los marca. Y al revés.
- [x] Con un lote ya entregado, cambiar la bodega de forma que cambie de vehículo → el lote vuelve a quedar por preparar (su contenedor vuelve a la estantería).
- [x] El puesto de combustible indica si el combustible va en la lanzadera o en el envío; cambiar el tanque de transferencia invalida la autorización si cambia el destino.

### AF4. Despegue con la lanzadera preparada
- [x] Al embarcar: traslado desde el edificio a la plataforma con la MISMA lanzadera (mismos refuerzos, tanque y bodega), después acceso por el brazo, compuerta, comprobación (incluye la línea de la L-4), cuenta atrás, encendido, ascenso y llegada.
- [x] Probar al menos con «sin nada» y con «bodega grande + tanque de 2 tramos + 4 refuerzos».
- [x] La omisión (Espacio/Intro, a partir de la segunda vez) sigue funcionando sin cambiar costes ni requisitos.
- [x] En ExpedicionMVP, las tarjetas de llegada dicen qué subió en la L-4 y qué en el envío orbital, y la configuración de la L-4.
- [x] Volver al hangar: la configuración de la lanzadera se conserva y la carga vuelve a estar por preparar.


## AG. Estación A compacta, cubierta abovedada, transporte VC-1 y recorrido visual — 13-11-2026

**Estado: PENDIENTES.** Escrito y revisado por lectura; no compilado ni ejecutado. AE y AF siguen validadas solo para `Copia_validada_AE_AF_2026-11-13.zip`. Abrir: *Prototipo → Juego → 3. Abrir HangarExpedicion* → Play. Antes, comprobar que la consola no muestra errores de compilación.

### AG1. Organización y circulación junto al holograma
- [ ] La estación A se reconoce como una sola: tarima, mesa, consolas MOTOR/TANQUE/INSTRUMENTO delante, A2 al este y 2 al oeste, maquetas detrás y rótulo superior.
- [ ] E en la consola MOTOR abre el holograma ya en la pestaña MOTOR (igual con TANQUE e INSTRUMENTO); A2 lo abre sin pieza fijada.
- [ ] Ni las maquetas ni los rótulos tapan el holograma desde las consolas.
- [ ] Se puede rodear la estación por los dos lados sin chocar.
- [ ] Las maquetas muestran la pieza elegida con «MAQUETA 1:10» o «1:2» y dónde está la pieza real.

### AG2. Cubierta sin interferencias
- [ ] La cubierta del edificio de la L-4 es curva (perfil ovalado visible en los extremos) y conserva arcos, plataformas, pasarelas, grúa y luces.
- [ ] La lanzadera más alta (bodega grande + tanque de 2 tramos) no toca la cubierta; la animación de la grúa en la consola B no atraviesa la cubierta.
- [ ] La cámara de la consola B no se sale por la cubierta ni por las paredes.
- [ ] La puerta de salida (este) se ve y es más ancha y alta que la lanzadera con su plataforma.

### AG3. Previsualización, confirmación, cambio y cancelación
- [ ] Previsualizar en el holograma no crea ninguna pieza en C2 ni cambia el total.
- [ ] Encargar un motor → su pieza a escala real aparece en el soporte del MOTOR (C2) con «Transporte orbital no tripulado»; el total cambia una sola vez (lo que ya costaba).
- [ ] E en el soporte → la grúa la lleva a la bodega del VC-1; el lote queda preparado; nada se cobra.
- [ ] Cambiar a otro motor con la preparación abierta → la pieza anterior desaparece de la bodega, el soporte muestra la nueva y solo el lote del motor queda pendiente.
- [ ] Volver al motor de serie → el soporte queda «sin encargo», no hay lote ni envío por ese motor.
- [ ] Cambiar la selección mientras la grúa trabaja → aviso y la pieza no queda marcada como cargada.
- [ ] Ningún objeto duplicado tras varios cambios (soportes, bodega, maquetas).

### AG4. Destino de cada carga y límites
- [ ] Cada contenedor y lote dice «L-4: tripulación y carga» o «Transporte orbital no tripulado» según las reglas de la lanzadera.
- [ ] Los lotes pequeños que van en el VC-1 se entregan con el carro en la zona amarilla de C2 (se cruza el paso del fondo sin engancharse) y aparecen en el anillo del VC-1.
- [ ] Tanque ampliado + motor de alto empuje + 10 tramos → el VC-1 dice «NO SE PUEDE DESPACHAR» con el motivo (unos 37 200 kg o más, según la carga pequeña, frente a 34 000 kg), la revisión lo marca y no se puede cerrar. Bajando tramos se resuelve.
- [ ] El total y las tarifas son los mismos que en la versión validada con la misma selección (el VC-1 no añade cobros).

### AG5. Lanzamiento no tripulado solo cuando corresponde
- [ ] Sin componentes, sin combustible en el envío y sin carga pequeña en el envío: el VC-1 dice que no hace falta y al embarcar no aparece su secuencia.
- [ ] Con envío: al embarcar se ven, en orden, salida de C2 con la bodega abierta, integración en el lanzador (fuera de los edificios), cuenta atrás, lanzamiento, llegada a la zona de servicio y, por cada pieza, recepción → retirada de la de serie (queda en el soporte) → colocación → comprobación; después el combustible y la carga pequeña y «EM-4X PREPARADA».
- [ ] Las piezas recibidas e instaladas son las confirmadas (mismo tipo y tamaño que en C2).
- [ ] No embarca ningún astronauta en el VC-1.

### AG6. Tripulación, llegada y omisión
- [ ] Después del envío (o directamente si no hay): carga de la bodega de la L-4 (si lleva), traslado, embarque, compuerta, comprobación, cuenta atrás, ascenso, llegada y atraque bajo el Mando con la L-4 confirmada.
- [ ] En ExpedicionMVP se empieza ya a bordo, con control del personaje y sin tarjetas. F2 abre y cierra el resumen.
- [ ] Ver todo y omitirlo (Espacio/Intro, en partes ya vistas) llegan al mismo estado: misma configuración, recursos, costes y objetivo (comparar la mesa de misión y F2 en ambos casos).
- [ ] La instalación se ve siempre antes que la llegada de la tripulación.

### AG7. Nueva expedición y ejecutable
- [ ] Volver al hangar: soportes y VC-1 vacíos, lotes por preparar, ningún objeto de la expedición anterior; las selecciones se conservan.
- [ ] *Juego → 6* genera el ejecutable y el recorrido completo (con y sin envío) funciona en él.
- [ ] Acceso directo a ExpedicionMVP (sin hangar) sigue funcionando con su propia preparación y sus tarjetas.

### AG8. Sustitución de modelos
- [ ] *Arte y pruebas → 5* conecta la tabla a HangarExpedicion sin otros cambios.
- [ ] Con una entrada «Escenario» de prueba (p. ej. `A/MesaHolograma` o `VC1/Bodega` con un cubo), el modelo sustituye al bloque, las interacciones y la grúa siguen funcionando y la consola no da avisos en ExpedicionMVP.
