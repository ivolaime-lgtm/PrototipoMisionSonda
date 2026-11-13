INFORME DE LA SESIÓN AUTÓNOMA — 8 de noviembre de 2026
=======================================================

Qué podía hacer en esta sesión
------------------------------
- SÍ: leer y escribir archivos de la carpeta del proyecto y analizarlos (escenas, .meta,
  paquetes, logs de importación) con mis herramientas.
- NO: ejecutar comandos en el PC, abrir Unity, compilar, pulsar Play, ver la escena ni medir FPS.
- No trabajé en segundo plano: todo se hizo durante una única respuesta.

Antes de empezar existía la copia:
CopiasSeguridad/Copia_Assets_Proyecto_y_Packages_antes_centro_2026-11-08.zip


1. COMPROBADO (con evidencia de archivos, sin ejecutar Unity)
-------------------------------------------------------------
- Compilación: Library/ScriptAssemblies/Assembly-CSharp.dll y Assembly-CSharp-Editor.dll se
  generaron DESPUÉS de la última modificación de los scripts. Unity solo los escribe si
  la compilación termina sin errores. (Los scripts nuevos de esta sesión aún no se han
  compilado: ver "Pendiente".)
- glTFast: instalado en la versión 6.15.1. Comparando Packages/manifest.json y
  packages-lock.json con la copia anterior, el ÚNICO cambio es glTFast; ningún otro
  paquete cambió de versión.
- Referencias del catálogo de misión: correctas en Configuracion (PantallaConfiguracion) y en
  Exploracion (AplicadorConfiguracion y ControlMision); todas apuntan a Data/CatalogoMision.asset.
- Resto de referencias: revisé todos los campos de nuestros componentes en las 4 escenas.
  Ninguno está vacío ni apunta a un objeto o archivo inexistente.
- Recorrido de escenas: los nombres usados para cambiar de escena coinciden con la lista de
  compilación (CentroOperaciones → Configuracion → Exploracion → Resultados; M y
  "Volver a configurar" llevan a Configuracion).
- TDRS: la escena contiene una instancia del MODELO (no el marcador provisional), escalado
  a unos 7 m. Su .meta sigue diciendo "DefaultImporter" porque Unity no lo reescribió; no
  parece afectar, pero conviene reimportarlo (ver lista final).


2. PROBLEMA ENCONTRADO Y CORREGIDO (pendiente de que lo apliques)
-----------------------------------------------------------------
Encuadre del centro de operaciones. Calculé con la misma proyección de la cámara de Unity
(60°, 16:9) dónde cae cada objeto; el cálculo coincide aproximadamente con tu captura.
- Antes: la estación ocupaba el centro de la pantalla (~465 px de radio) delante de la luna
  y el satélite quedaba DETRÁS de la estación en las 24 posiciones de su órbita: por eso no
  se veía.
- Ahora (calculado): estación abajo a la derecha del panel (~220 px), luna arriba a la
  derecha (~300 px) y el satélite contra el espacio en 16–24 de 24 posiciones durante todo
  el vaivén de la cámara. El Sol se gira para iluminar la estación de frente.
- Cómo aplicarlo SIN sustituir la escena: menú Prototipo → 7 (solo mueve 5 objetos y no guarda).


3. ARCHIVOS DE ESTA SESIÓN
--------------------------
Nuevos:
- Assets/_Proyecto/Editor/AjustarEncuadreCentro.cs      (menú 7)
- Assets/_Proyecto/Scripts/Diagnostico/MedidorRendimiento.cs  (F9: FPS en editor y ejecutable)
- InformeSesion_2026-11-08.md (este archivo, fuera de Assets)
Modificados:
- Assets/_Proyecto/Editor/ConstructorCentroOperaciones.cs  (solo valores por defecto del encuadre)
- Assets/_Proyecto/LEEME_EQUIPO.md  (menú 7 y tecla F9)
No se tocaron escenas, mecánicas, paquetes ni ajustes del proyecto. No se borró nada.


4. RENDIMIENTO (revisión estática, NO es la prueba 6)
-----------------------------------------------------
- Exploracion: ~123 objetos visibles, 1 luz direccional con sombras y 5 luces sin sombras.
- CentroOperaciones: muy ligera (estrellas en una sola malla, TDRS ~3.000 triángulos,
  textura de 1440×720).
- No encontré nada que apunte a un problema. Los FPS reales siguen PENDIENTES de medir.


5. PENDIENTE — LO QUE DEBES COMPROBAR AL VOLVER
-----------------------------------------------
1. Abre Unity y espera a que compile. Console sin errores rojos.
2. Reimporta el TDRS: Project → Assets/_Proyecto/Externos/NASA → clic derecho sobre el .glb →
   Reimport. Debe seguir viéndose como modelo (con flecha desplegable).
3. Menú Prototipo → 7. Ajustar encuadre → Continuar. Mira la vista Game (Scale en 1x).
   Si te gusta: Ctrl+S. Si no: Ctrl+Z (o cierra la escena sin guardar).
4. Play en CentroOperaciones: el satélite debe verse pasar contra el fondo negro.
5. Prueba 6 (FPS): pulsa F9 en Play en CentroOperaciones y en Exploracion; apunta FPS y
   "peor ms". Repite en el ejecutable (File → Build Profiles → Build).
   Alternativa en el editor: botón "Stats" en la barra superior derecha de la vista Game
   (junto a "Gizmos"); el dato es "FPS" en la sección Graphics.
6. Recorrido: CentroOperaciones → Configuracion → Exploracion → M (vuelve a Configuracion con
   tu elección marcada) → Iniciar → T en la estación → Resultados → Repetir.
7. Opcional: en Build Profiles, la escena de plantilla Assets/Scenes/SampleScene está
   incluida en la compilación. No molesta, pero puedes desmarcarla para no incluirla.

Interfaz cortada en tu captura: el título aparece recortado arriba y a la izquierda. La causa
más probable es la misma que al principio (el control "Scale" de la vista Game por encima de
1x), aunque también podría ser el recorte de la captura. Compruébalo con Scale en 1x; si sigue
cortado, dímelo y lo corrijo.
