# Consola de pilotaje v0 — modelo base de bloques

**Qué es:** un modelo de bloques (*blockout*) para validar medidas, separación de piezas y puntos de conexión
antes de modelar. **No es el diseño final** ni tiene detalle, UV ni texturas.

**Comprobación:** las piezas son sólidos cerrados con normales hacia fuera, comprobado por código. Las imágenes son
dibujos de los bloques hechos por código, **no capturas de Unity ni de Blender**. Ni el script de Blender ni el
FBX se han ejecutado todavía.

**Jerarquía de referencias:** este README es la fuente de las medidas locales, las piezas, los pivotes y los anclajes
de la consola. Los ejes, las convenciones y los acuerdos generales están en el documento de rediseño (sección
*Referencia vigente*). Ambos se revisaron juntos el 8 de noviembre de 2026; si cambia uno, se actualiza el otro.

## Archivos

| Archivo | Qué contiene | Qué NO contiene |
| --- | --- | --- |
| `crear_consola_pilotaje_v0.py` | Script de Blender (3.6 o superior). Crea cada pieza como objeto separado **con su pivote**, los puntos de conexión como **objetos vacíos** `ANC_…`, materiales provisionales y una flecha de ayuda. Opción `EXPORTAR_FBX` | — |
| `consola_pilotaje_v0.obj` + `.mtl` | Las mismas mallas, para verlas o medirlas en cualquier programa | **Pivotes de las piezas** (todas las coordenadas son absolutas respecto al pivote de la consola) y **puntos de conexión**: el OBJ no guarda objetos vacíos. Están en `anclajes.json` y en la tabla de abajo |
| `anclajes.json` | Coordenadas de Unity de cada punto de conexión y de cada pivote | — |
| `planta_y_perfil.png` | Planta de la cabina con la consola y perfil por el eje con la línea de visión | — |
| `vista_piloto_sentado.png` | Dibujo de lo que ve el piloto sentado | Iluminación, materiales reales, la ventana de la escena |
| `vista_general.png` | Vista de conjunto de piezas y asiento | — |
| `fuente/` | Programas que generan todo lo anterior a partir de una sola definición de medidas | — |

## Ejes y orientación

- **Unity (referencia de este documento):** x = derecha del piloto, y = arriba, z = hacia la ventana. Metros.
  Pivote de la consola: en el suelo, en el centro del borde del lado del piloto.
- **Blender (script):** X = derecha del piloto, +Y = hacia la ventana, Z = arriba.
- **Exportación FBX (procedimiento provisional, sin validar):** Forward = Z, Up = Y, Apply Transform activado,
  Apply Scalings = FBX All. Es una propuesta: queda pendiente de la prueba Blender → Unity y puede cambiar.
- **OBJ:** Y arriba. Escrito para que Unity, al importarlo, deje la ventana en +Z y la palanca a la derecha.

**Sin comprobar:** no he podido abrir Blender ni Unity. Al importar, comprobad:

- La **flecha amarilla** `AYUDA_FlechaProa` debe apuntar a la ventana (+Z).
- El **cuadrado azul** debe quedar a la **derecha** (+X).
- La **palanca** debe quedar a la derecha del piloto.

Si sale girado o espejado, avisad y corrijo los ejes. Borrad la flecha después.

## Medidas principales

| Medida | Valor |
| --- | --- |
| Ancho total | 2,43 m en la parte trasera (lomo) y 1,87 m en el borde del piloto. Tramo central de 1,00 m y dos alas de 0,45 m de largo y 0,85 m de fondo, giradas 20° hacia el piloto sobre su esquina delantera interior. Al girar, la esquina trasera de cada ala se desplaza 0,29 m hacia fuera: 2 × (0,50 + 0,45·cos 20° + 0,85·sin 20°) = 2,43 m |
| Fondo | 0,85 m desde el borde del piloto; las puntas de las alas avanzan 0,14 m hacia el asiento (caja envolvente: 0,99 m) |
| Altura máxima | 1,05 m de estructura; el bisel de la pantalla central llega a 1,053 m |
| Mesa de mandos | 0,70 m en el borde y 0,76 m al fondo (10°); grosor libre para las rodillas: 0,64 m del suelo, 0,34 m de fondo |
| Atril de pantallas | Inclinado 50° desde la horizontal, de 0,76 a 1,05 m de altura |
| Pantalla central (Estado) | 0,60 × 0,30 m de área útil |
| Pantallas laterales | 0,38 × 0,26 m: Propulsión a la izquierda y Casco a la derecha, como ahora |
| Asiento | Centro a 0,35 m del borde del piloto; cojín a 0,48 m; respaldo inclinado 12° |
| Ojos del piloto (`ANC_Vista`) | 1,20 m de altura, 0,50 m por detrás del borde |
| Ángulo de mirada a la pantalla central | Unos 16° hacia abajo |
| Línea de visión | Desde los ojos, por encima del borde superior, se ve la pared de proa a partir de 0,96 m de altura |
| Triángulos | Unos 850 en total (bloques) |

**Cambios respecto a la ficha:**

- La mesa tiene 10° en lugar de 15°.
- Las pantallas pasan a un atril inclinado para no superar 1,05 m.
- El asiento queda más cerca de la consola, para que los mandos estén al alcance.

## Colocación propuesta en la escena Nave

- **Posición:** pivote de la consola en **(0; 0; 3,30)**, sin giro. Así quedan 0,85 m libres hasta la pared de
  proa (z = 5) y 2,79 m a cada lado.
- **Asiento:** en z = 2,95.
- **Ojos:** en z = 2,80.
- **Pendiente de comprobar:**
  - La altura real de la ventana de la escena.
  - Que la consola no choque con el acabado de cabina (menú 11), que debería quitarse u ocultarse en la prueba.

## Piezas separadas

| Objeto | Pivote (Unity, m) | Para qué va aparte |
| --- | --- | --- |
| `NAV_Pilotaje_Consola_A` (vacío raíz) | (0; 0; 0) | Raíz de la consola; lleva `PuestoNave` en Unity |
| `…_Estructura` | (0; 0; 0) | Cuerpo: mesa, atril y lomo de los tres tramos y sus juntas |
| `…_Bisel` | (0; 0; 0) | Marcos de las tres pantallas |
| `…_Pantalla_Estado` | (0; 0,919; 0,463) | Superficie con material propio para mostrar datos |
| `…_Pantalla_Propulsion` | (−0,870; 0,919; 0,358) | Ídem |
| `…_Pantalla_Casco` | (0,870; 0,919; 0,358) | Ídem |
| `…_BordeAcento` | (0; 0; 0) | Borde naranja de la mesa (material de identidad) |
| `…_TiraLuz` | (0; 0; 0) | Tira de luz del zócalo: debe cambiar de color con las alarmas |
| `…_Indicador` | (0,36; 0,749; 0,30) | Luz de estado (`indicador` de `PuestoNave`) |
| `…_Palanca` | (0,814; 0,758; 0,072) | Palanca de empuje; pivote en su eje de giro, por si se anima |
| `…_SelectorEtapa` | (−0,30; 0,742; 0,198) | Base y botón rojo de la separación |
| `…_TapaEtapa` | (−0,30; 0,783; 0,247) | Tapa de seguridad (abierta 70°); pivote en la bisagra, por si se anima |
| `…_Interruptores` | (−0,801; 0,725; 0,061) | Fila de seis interruptores |
| `…_COL` | (0; 0; 0) | Caja de colisión (−1,21…1,21; 0…1,05; −0,14…0,85). No se dibuja |
| `NAV_Pilotaje_Asiento_A` (vacío raíz) | (0; 0; −0,35) | Raíz del asiento |
| `…_Base`, `…_Cojin`, `…_Respaldo`, `…_Reposabrazos` | ver `anclajes.json` | Asiento en cuatro piezas |
| `AYUDA_FlechaProa` | (0; 0; 1,1) | Solo para comprobar ejes; borrar después |

## Puntos de conexión (objetos vacíos en el script; solo coordenadas en el OBJ)

| Punto | Posición (Unity, m) | Notas |
| --- | --- | --- |
| `ANC_Vista` | (0; 1,20; −0,50) | Ojos del piloto. En Unity, orientar hacia +Z con unos 8° hacia abajo; el script no fija la rotación |
| `ANC_Pantalla_Estado` | (0; 0,919; 0,463) | Normal hacia el piloto (0; 0,638; −0,770); arriba de la pantalla (0; 0,770; 0,638) |
| `ANC_Pantalla_Propulsion` | (−0,870; 0,919; 0,358) | Normal (0,264; 0,638; −0,724); arriba (−0,218; 0,770; 0,599) |
| `ANC_Pantalla_Casco` | (0,870; 0,919; 0,358) | Normal (−0,264; 0,638; −0,724); arriba (0,218; 0,770; 0,599) |
| `ANC_Indicador` | (0,36; 0,749; 0,30) | Base de la luz de estado |
| `ANC_Palanca` | (0,814; 0,758; 0,072) | Eje de giro de la palanca |
| `ANC_TapaEtapa` | (−0,30; 0,783; 0,247) | Bisagra de la tapa |
| `ANC_Asiento` | (0; 0; −0,35) | Pivote del asiento en el suelo |

Los vacíos del script no llevan rotación. La orientación de las pantallas va en la tabla: Unity tendrá que girar
los textos para que se lean de frente.

## Materiales provisionales (de la guía visual)

`M_PinturaBlanca` #E6E4DF · `M_Grafito` #3A3F46 · `M_MetalDesnudo` · `M_AcentoNaranja` #F28C28 ·
`M_Pantalla` (emisivo cian #5CC8E0) · `M_TiraLuz` (emisivo) · `M_Indicador` (emisivo verde) · `M_Alerta` #E5484D ·
`M_TelaAsiento` · `M_Colision` y `M_Ayuda` (solo de trabajo).

## Prueba en Unity (en una copia, sin tocar la escena original)

1. Duplicad `Nave.unity` (por ejemplo `Nave_PruebaConsola.unity`) y trabajad solo en la copia.
2. Importad el FBX exportado desde Blender en una carpeta de prueba.
3. Colocad la raíz en (0; 0; 3,30) y comprobad ejes con la flecha y el cuadrado azul.
4. Siéntate en Pilotaje: ¿se ve la ventana por encima de las pantallas? ¿La mira y los escombros siguen visibles?
5. Recorrido: entrar desde el pasillo, sentarse (E) y levantarse (Q) sin quedarse atascado.
6. Anotad FPS con F9.

Para que la consola funcione de verdad hacen falta estos ajustes en Unity, en un paso aparte que se aprueba primero:

- `PuestoNave` en la raíz.
- Un colisionador de caja según `_COL`.
- `vista` en `ANC_Vista`.
- Los textos de `PantallaDatosNave` en las tres pantallas.
- `indicador` en `_Indicador`.
- La tira de luz en la lista de luces reactivas.

## Qué hace el equipo de modelado

- Diseño final a partir de estos bloques: biseles, juntas, paneles, agarres, mandos detallados y tapizado.
- UV y texturas PBR, con materiales compartidos según la guía.
- **Conservar** nombres, pivotes, piezas separadas y puntos de conexión, o proponer cambios antes de modelar.
- Exportar `.fbx` y texturas `.png`, con unos 15 000–25 000 triángulos para todo el puesto.
