INFORME DE REVISIÓN DEL RECORRIDO COMPLETO — 8 de noviembre de 2026
===================================================================

Alcance: CentroOperaciones → Configuracion → Exploracion → Resultados → Repetir.
Método: lectura del código y de los archivos de escena, catálogo y prefabs.
NO se ejecutó Unity: nada de esto es una prueba en ejecución.

Copia previa: CopiasSeguridad/Copia_scripts_escenas_docs_antes_revision_2026-11-08.zip
(todos los scripts, escenas, catálogo y documentos, tal como estaban).


1. EVIDENCIA DE PARTIDA
-----------------------
- Los cambios de la sesión anterior compilaron: los ensamblados se regeneraron después de
  esos scripts. Esto NO dice nada de los cambios de esta revisión (ver punto 5).
- Ningún script tenía modificaciones fuera de mis entregas.
- CentroOperaciones se guardó después de aplicar el menú 7 (encuadre).
- Medido por el equipo: 312 FPS en CentroOperaciones (captura con F9).


2. REVISADO SIN ENCONTRAR PROBLEMAS
-----------------------------------
- Referencias del catálogo: correctas en Configuracion (1) y Exploracion (2).
- Referencias de todos los componentes de Exploracion: apuntan a objetos de la escena.
- Cambios de escena: los nombres coinciden con la lista de compilación.
- Conservación de la configuración: se guarda al pulsar INICIAR y la recuperan Exploracion
  (al empezar), Configuracion (al volver con M o desde Resultados) y Repetir.
- Reinicio al repetir: cada misión recarga la escena, así que batería, datos, escaneos y
  puntos empiezan de cero. Los únicos datos estáticos (elección e informe) se reinician al
  pulsar Play y se sustituyen al terminar cada misión.
- Salidas de la misión: objetivo cumplido, batería agotada o T en la estación. Las tres
  generan el informe; el cursor se libera antes de cambiar de escena.
- Valores APLICADOS de verdad (comprobado en el código): capacidad de batería, masa →
  aceleración, presupuesto y carga del lanzador (bloquean), consumo/duración/MB del escaneo,
  memoria, velocidad y ventanas de contacto, consumo de transmisión, escaneos requeridos y
  valor científico. Solo son ESTIMACIÓN la energía para escanear y el tiempo medio de
  transmisión de la pantalla de configuración (rotulados así).
- Rendimiento: ningún método por fotograma busca objetos, crea listas, instancia ni
  recalcula el catálogo. No hay cambios de rendimiento justificados.


3. PROBLEMAS CORREGIDOS
-----------------------
a) Misiones imposibles que se podían iniciar (solo editando el catálogo). Ahora BLOQUEAN el
   inicio, con mensaje: memoria menor que un escaneo; órbita sin enlace; objetivo con 0
   escaneos.                                                   → CatalogoMision.cs
b) Objetivo con más escaneos que puntos en la escena: antes nada lo indicaba. Ahora aviso en
   la Console y en rojo en el panel de datos.            → SistemaDatos.cs, HudCiencia.cs
c) Escala de tiempo duplicada (catálogo y batería): si no coinciden, aviso en la Console.
                                                              → AplicadorConfiguracion.cs
d) Elección guardada fuera de rango si se quitan opciones del catálogo: el botón marcado no
   coincidía con el resumen. Ahora se ajusta.                → PantallaConfiguracion.cs
e) Lista de teclas incompleta en Exploracion (faltaban E, F, T, M, F9). Campo renombrado
   a textoTeclas para que la escena use el texto nuevo.         → HudMinimo.cs
f) "Autonomía al consumo actual" mientras se recarga (contradictorio). Ahora dice
   "recargando en la estación"; fuera, la autonomía indica "s reales".   → HudMinimo.cs
g) Pulsar T con datos sin transmitir los pierde sin avisar. Ahora el aviso de la tecla T
   indica cuántos MB se perderían (más alto, para dos líneas).        → ControlMision.cs
h) Unidades de tiempo ambiguas: la duración del informe y el tiempo de transmisión eran
   segundos reales sin decirlo, en un juego donde 1 s = 1 min de misión. Ahora:
   "m:ss reales (≈ N min de misión)" y "s reales (media…)".
             → InformeMision.cs, ControlMision.cs, PantallaResultados.cs, PantallaConfiguracion.cs
i) Créditos: decían que glTFast "se usa solo en el editor", lo cual no es exacto. Corregido.
                                                                       → CREDITOS.md


4. DOCUMENTACIÓN
----------------
- LEEME_EQUIPO.md: qué bloquea INICIAR, qué masa cuenta dónde, avisos al editar valores,
  campos del catálogo aún no guardados, cómo probar la interfaz (16:9, Scale 1x).
- PRUEBAS_ACEPTACION.md (nuevo): 24 pruebas con pasos y resultados esperados.


5. PENDIENTE (requiere Unity)
-----------------------------
- COMPILACIÓN de los 9 scripts modificados: no verificada. Abre Unity y comprueba la Console.
- Todas las pruebas de PRUEBAS_ACEPTACION.md.
- Legibilidad en ventanas bajas: con 1165×507 (tu captura) los textos pequeños quedan muy
  reducidos porque la interfaz se escala por altura. No lo cambié a ciegas; documentado.
- El satélite en tu captura: con el encuadre nuevo debería verse contra el fondo; en la
  captura no lo distingo con seguridad. Compruébalo en A1.
- Opcional: desmarcar SampleScene en Build Profiles.

Archivos de esta revisión
-------------------------
Modificados: HudMinimo.cs, Ciencia/SistemaDatos.cs, Ciencia/HudCiencia.cs,
Mision/CatalogoMision.cs, Mision/PantallaConfiguracion.cs, Mision/AplicadorConfiguracion.cs,
Mision/ControlMision.cs, Mision/InformeMision.cs, Mision/PantallaResultados.cs,
CREDITOS.md, LEEME_EQUIPO.md.
Nuevos: PRUEBAS_ACEPTACION.md, InformeRevision_2026-11-08.md.
No se tocaron escenas, prefabs, materiales, paquetes ni ajustes. No se borró nada.
