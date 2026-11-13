CRÉDITOS Y RECURSOS DE TERCEROS
================================

Prototipo de juego creado por un equipo universitario para un reto de NASA Space Apps.
La ambientación es FICTICIA: una luna helada inventada con un océano bajo el hielo.
No representa ninguna misión real y no está aprobada ni respaldada por NASA.
No se usa el logotipo de NASA como marca del juego.

Fecha de consulta de las fuentes: 8 de noviembre de 2026.


1. TEXTURA DE LA LUNA FICTICIA
------------------------------
- Archivo en el proyecto: Assets/_Proyecto/Externos/NASA/Jupiter - Europa.jpg
- Archivo descargado: ModelosDescargados/Images and Textures/Jupiter - Europa/Jupiter - Europa.jpg
- Procedencia: repositorio NASA 3D Resources (github.com/nasa/NASA-3D-Resources),
  enlazado desde la biblioteca oficial NASA 3D Resources.
- Página de origen: https://science.nasa.gov/3d-resources/jupiter-europa
- Crédito indicado en la página: USGS, JPL & Caltech (mosaico con imágenes Voyager,
  de la base de datos JPL/Caltech de mapas planetarios).
- Cuerpo que representa originalmente: Europa, luna de Júpiter.
- Uso en el juego: solo como aspecto visual de una luna FICTICIA. El juego no simula Europa.
- Condiciones aplicables: la biblioteca NASA 3D Resources indica que sus recursos son
  gratuitos para descargar y usar, sujetos a las pautas de imágenes y medios de NASA.
  La ficha no muestra avisos de derechos de terceros ni restricciones particulares.
- Atribución que usamos: "Textura: USGS, JPL & Caltech, vía NASA 3D Resources".
- Dudas pendientes: ninguna detectada en la ficha. El crédito nombra también a USGS y
  Caltech; si la organización del reto pide algo más, se añadirá aquí.


2. SATÉLITE DE RELEVO (MODELO 3D)
---------------------------------
- Archivo en el proyecto: Assets/_Proyecto/Externos/NASA/Tracking and Data Relay Satellites (TDRS) (A).glb
- Archivo descargado: ModelosDescargados/3D Models/Tracking and Data Relay Satellites (TDRS) (A)/
- Procedencia: repositorio NASA 3D Resources, enlazado desde la biblioteca oficial.
- Página de origen: https://science.nasa.gov/3d-resources/tracking-and-data-relay-satellites-tdrs-a/
- Crédito indicado en la página: NASA/Ames Research Center.
- Uso en el juego: representación VISUAL del satélite de relevo de la escena de presentación.
  No implica que TDRS se use en una luna lejana. Las reglas de comunicación del juego
  (velocidad de enlace y ventanas de contacto) no dependen de este modelo.
- Condiciones aplicables: las mismas pautas de NASA 3D Resources indicadas arriba.
  La ficha no muestra avisos de derechos de terceros. El modelo no contiene texturas
  ni logotipos (solo materiales de color).
- Atribución que usamos: "Modelo TDRS: NASA/Ames Research Center, vía NASA 3D Resources".
- Dudas pendientes: ninguna detectada.


3. RECURSO EVALUADO Y NO INTEGRADO: GATEWAY
-------------------------------------------
- Archivo: ModelosDescargados/3D Models/Gateway/Gateway Core.glb (no copiado al proyecto).
- Contribuidor según la biblioteca oficial: NASA/JSC/Gateway Program Office.
  No se localizó la ficha individual del modelo.
- Motivo de dejarlo PENDIENTE: una textura incluye la marca "Canada" (identificador del
  Gobierno de Canadá, de un tercero) y otra un rótulo pequeño; además, es una estación real
  muy reconocible y podría sugerir una misión real.
- Alternativa usada: estación modular propia (Prefabs/EstacionOrbital), hecha con
  geometría simple por el equipo.


4. HERRAMIENTAS
---------------
- Unity glTFast (com.unity.cloud.gltfast), versión 6.15.1, licencia Apache 2.0.
  Importa el modelo .glb en el editor. Como paquete del proyecto, su código y los
  sombreadores de sus materiales pueden incluirse en el ejecutable (los materiales del
  modelo TDRS los usan).
  https://github.com/Unity-Technologies/com.unity.cloud.gltfast


5. NOTAS SOBRE LICENCIAS
------------------------
- Pautas de NASA consultadas: https://www.nasa.gov/nasa-brand-center/images-and-media
  En resumen: el material de NASA no suele tener copyright en EE. UU.; se debe reconocer
  a NASA como fuente; no se pueden usar sus insignias o logotipos sin autorización
  ni dar a entender respaldo; el material de terceros que aparezca en él conserva sus derechos.
- El archivo meta.json del repositorio menciona "NASA Open Source Agreement 1.3".
  NO lo aplicamos automáticamente a estos recursos: documentamos las pautas de cada uno.
- Código propio del equipo: licencia de código abierto pendiente de elegir.
