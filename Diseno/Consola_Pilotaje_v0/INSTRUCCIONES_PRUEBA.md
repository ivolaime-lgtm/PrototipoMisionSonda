# Prueba de ida y vuelta: Blender → FBX → copia de Unity

Objetivo: comprobar que el **modelo base** llega a Unity con la escala, la orientación, los pivotes y los anclajes
correctos **antes** de rediseñarlo. El modelo es de bloques y no hay que conservar sus formas; sí sus nombres,
pivotes y anclajes, salvo que se acuerde cambiarlos.

Requisitos: Blender 3.6 o superior y una **copia** del proyecto de Unity (o al menos una copia de la escena Nave).

## 1. Abrir en Blender sin sobrescribir trabajo

1. Descomprime el ZIP en una carpeta nueva, **fuera** del proyecto de Unity.
2. En Blender: *Archivo → Nuevo → General*. Usa un archivo nuevo, nunca uno de trabajo existente. Puedes borrar el
   cubo inicial.
3. *Archivo → Guardar como* → `consola_pilotaje_v0_base.blend`, en la carpeta descomprimida.
4. Pestaña *Scripting* → *Abrir* → `crear_consola_pilotaje_v0.py` → *Ejecutar* (▶).
   - Crea la colección `Consola_Pilotaje_v0`. Si ya existe, crea otra con sufijo; no borra nada.
   - **Ejecútalo una sola vez por archivo.** Una segunda vez duplica los objetos con nombres `.001`.
5. Guarda (Ctrl+S). Este archivo queda como **base de referencia: no lo modifiques**.

## 2. Revisar escala, orientación, pivotes y anclajes

- **Unidades:** en *Propiedades de escena → Unidades*: sistema métrico, escala 1,0, longitud en metros.
- **Medidas** (herramienta *Medir*), comparadas con el README:
  - Ancho máximo de la consola: unos 2,43 m en la parte trasera (1,87 m en el borde del piloto).
  - Altura: 1,05 m.
  - Cojín del asiento: 0,48 m.
- **Orientación en Blender:**
  - La flecha amarilla `AYUDA_FlechaProa` apunta hacia **+Y** (la ventana).
  - El cuadrado azul está en **+X**.
  - La palanca queda a la **derecha** del piloto.
- **Pivotes:** selecciona `…_Palanca` y `…_TapaEtapa`. Su origen debe estar en el eje de giro y en la bisagra.
  `NAV_Pilotaje_Asiento_A` tiene el origen en el suelo.
- **Anclajes:** deben existir 8 vacíos `ANC_…`. Sus posiciones están en el README y en `anclajes.json`. Recuerda que en
  Blender los ejes Y y Z están intercambiados respecto a Unity.

## 3. Guardar el editable de trabajo

*Archivo → Guardar como* → `consola_pilotaje_v0_trabajo.blend`. Los cambios de rediseño se hacen en este archivo o
en sus versiones siguientes; `…_base.blend` no se toca.

## 4. Exportar una prueba (procedimiento provisional, sin validar)

Desde `…_base.blend`, sin cambios:

1. Selecciona todos los objetos de la colección `Consola_Pilotaje_v0`, incluida la flecha de ayuda: clic derecho
   en la colección → *Seleccionar objetos*.
2. *Archivo → Exportar → FBX* → `consola_pilotaje_v0_prueba.fbx` con:
   - *Limitar a:* objetos seleccionados.
   - *Tipos de objeto:* Vacío y Malla.
   - *Escala:* 1,00; *Aplicar escalado:* «FBX todo» (*FBX All*).
   - *Adelante:* Z; *Arriba:* Y.
   - *Aplicar transformación:* activado.
   - *Añadir huesos de hoja:* desactivado.

Estos ajustes son una propuesta. Si el resultado sale girado o espejado en Unity, anota cómo sale y prueba de nuevo
con *Adelante: −Z*. No modifiques el script ni el modelo para compensarlo.

## 5. Importar en una copia de Unity

1. En la **copia** del proyecto, crea `Assets/_Proyecto/Pruebas/Consola_v0/` y arrastra el FBX.
2. En el inspector del FBX: *Scale Factor* 1 y *Convert Units* activado. Pulsa *Apply*.
3. Arrastra el modelo a una copia de la escena Nave (por ejemplo `Nave_PruebaConsola.unity`) y colócalo en (0; 0; 3,30)
   sin giro.
4. Comprueba:
   - La flecha amarilla apunta a **+Z** (la ventana).
   - El cuadrado azul queda en **+X**.
   - La palanca queda a la derecha del piloto.
   - Las medidas: en *Scale* debe aparecer 1, y la estructura debe medir unos 2,43 m de ancho.
   - Los 8 `ANC_…` aparecen como objetos vacíos en las posiciones del README.
   - Los pivotes de la palanca y de la tapa están donde deben.
5. Sin tocar scripts ni la escena original: en Play, ¿se ve la ventana por encima de las pantallas desde la vista
   de Pilotaje?

## 6. Qué devolver

- Capturas: Blender con la colección y la flecha; Unity con la flecha y el cuadrado azul; Unity desde la vista
  sentada.
- Si algo no coincide, los valores reales de posición, giro y escala de la raíz, de la palanca y de `ANC_Vista`
  en Unity.
- Los ajustes de exportación que funcionaron, si fueron distintos de los propuestos.

Con eso se corrige solo lo necesario (ejes, escala, pivotes o anclajes) en el script y en el README.
