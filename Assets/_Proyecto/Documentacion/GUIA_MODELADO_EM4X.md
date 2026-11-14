# Guía de modelado · EM-4X (encargo para el equipo de arte)

13-11-2026 · Las medidas de esta guía se han **leído del código actual** (`Scripts/Expedicion/NaveModular.cs` y `Scripts/Circulacion/DatosCirculacion.cs`). No son propuestas antiguas. Si el código cambia, esta guía se regenera.

> **Menús renombrados (13-11-2026, solo cambia la ruta):** 19 → *Prototipo → Arte y pruebas → 1. Preparar carpetas de arte*; 20 → *Arte y pruebas → 2. Crear escena ModeloPrueba*; 21 → *Arte y pruebas → 3. Comprobar recursos de arte*; para abrir la escena de importación: *Arte y pruebas → 4. Abrir ModeloPrueba*. 16 → *Juego → 2. Crear escena ExpedicionMVP*. Los menús antiguos 8 y «Muestra visual» están en *Prototipos antiguos*.

## 1. Estado de lo que se modela

- **Aceptado por el equipo:** aspecto general, tamaño y distribución de la EM-4X.
- **Pendiente de validar en Unity:**
  - Pasadizos de 2,4 m (2,2 m hacia Sistemas).
  - Las siete puertas de aislamiento.
  - Todo el MVP.
  - Que el modelado de **pasadizos y puertas** pueda cambiar si la prueba falla.
- **No es definitivo:** balance de masas, transporte del nodo de 10 m, lanzadera (bloque provisional) y etapa de propulsión (solo exterior).
- **No se cambian:** distribución, tamaños, posiciones de puertas, puestos ni puntos de interacción. Si una medida no os encaja, se avisa al integrador; no se corrige en el modelo.

## 2. Primer encargo: solo un módulo de prueba

**Versión sencilla del módulo de Ciencia (CI), con piezas separadas**, para probar importación, escala y circulación antes de la nave completa. La **puerta sigue siendo la provisional** hasta que se valide su paso: no se modela ni se detalla todavía. Ver `em4x_modulo_ciencia.png`.

| Recurso | Nombre de archivo | Medida a respetar | Pivote | Orientación |
|---|---|---|---|---|
| Casco de Ciencia (sencillo) | `EM4X_CI_Casco_v01` | Octógono de 6,0 m entre caras exteriores y 5,4 m entre interiores (pared 0,3). Largo 9,0 m. Hueco cuadrado de 2,4 × 2,4 m centrado en la tapa de entrada, sin puerta | Centro de la cara de ENTRADA (z = 0) | +Z hacia el fondo, +Y arriba |
| Consola de Ciencia (sencilla) | `EM4X_CI_ConsolaCiencia_v01` | Dentro de la caja 0,7 (x) × 1,2 (y) × 2,0 (z) m | Centro de la caja | Ejes del módulo; la cara útil mira a −X (hacia el pasillo) |

Casco y consola van en archivos distintos; dentro del casco, los asideros en mallas llamadas «Asidero». Hoja y panel de puerta quedan fuera de este encargo. La zona de la puerta de Ciencia, es decir, el plano entre 0,08 y 0,14 m por dentro de la tapa de entrada y el recorrido de las hojas a cada lado del hueco, debe quedar libre en el casco.

Cómo comprobarlo (lo hace el integrador):
1. Menú **Prototipo → 19** (carpetas y tabla) y **Prototipo → 20** (escena ModeloPrueba).
2. Arrastrar cada prefab a su entrada en `Data/RecursosVisualesEM4X`.
3. Play en ModeloPrueba.

El informe de la izquierda indica tamaño, pivote, materiales no URP y si algo invade el interior o el paso libre. Con F3 se compara con la geometría provisional y con F4 se va a la puerta de Ciencia. Después hay que cruzar la puerta abierta, cerrarla con E desde los dos lados y acercarse a la consola.

**Criterio de aceptación de la prueba:**
- El informe no muestra avisos de tamaño ni de pivote.
- No hay vértices dentro del paso libre ni del interior.
- No hay materiales rosas.
- Se circula igual que con la geometría provisional.

## 3. Escala, ejes, pivotes y exportación

- **Unidades:** 1 unidad = 1 metro. Modelar a tamaño real. En Unity, *Scale Factor* = 1 y *Convert Units* activado; el menú 21 lo comprueba.
- **Ejes de la nave:** x = estribor, y = «arriba» de la nave, z = proa. Origen = centro del nodo central en z = 0 (su cara de popa está en z = −5).
- **Ejes locales de cada módulo:** +z a lo largo del módulo, desde su cara de entrada hacia el fondo. +y es el «arriba» local de la tabla y +x la derecha. Cada módulo se modela **en sus propios ejes**: el juego lo coloca y gira.
- **Pivotes:**
  - Casco: centro de la cara de entrada.
  - Equipos: centro de su caja.
  - Hojas y paneles: centro.
  - Utilería: centro de la caja.
- **Blender → FBX:** exportar con *Forward: −Z Forward*, *Up: Y Up*, *Apply Transform* y *Apply Scalings: FBX Units Scale*. Aplicar escala y rotación antes de exportar (Ctrl+A). Confirmar siempre en ModeloPrueba: el informe indica si el pivote o la escala no cuadran.

## 4. Formato y materiales

- **Formato:**
  - Preferido: **FBX**.
  - También se acepta **.glb**, porque el proyecto tiene glTFast 6.15 instalado.
  - Sin cámaras, luces, animaciones ni colisiones.
- **Render pipeline:** el proyecto usa **URP 17.3** (Unity 6000.3.28f1, recurso `PC_RPAsset`).
  - Material: **Universal Render Pipeline/Lit**, flujo *Metallic*.
  - Los shaders *Standard* del pipeline integrado se ven **rosas**.
  - Pantallas y luces de estado: *Emission* del mismo Lit.
- **Texturas:**
  - Formato PNG.
  - Nombres `T_<Pieza>_BaseColor`, `_Normal`, `_MetallicSmoothness` y `_Emission`.
  - Tamaño orientativo: 2048 para cascos y 1024 para equipos y utilería.
- **Tamaño de las mallas (orientativo, a ajustar tras la prueba):**
  - Casco de un módulo: hasta unos 30 000 triángulos.
  - Consola: hasta unos 10 000.
  - Utilería: hasta unos 3000.
- **Asideros:** en mallas aparte llamadas «Asidero» dentro del casco; el informe no los cuenta como invasión.
- **Lectura y escritura:** activar *Read/Write* en el importador solo durante la prueba; así el informe puede comprobar las invasiones vértice a vértice.

## 5. Convención de nombres y carpetas

- **Recursos de la nave:** `EM4X_<Bloque>_<Pieza>_v<NN>`.
  - `<Bloque>` es el id del módulo (NC, MA, CI, CO, IN, HA, BO, N2, SI, ES, PR, LZ) o bien `Puerta`, `Pasadizo` o `Util`.
  - Ejemplos: `EM4X_MA_Casco_v01`, `EM4X_MA_AsientoPilotaje_v01`, `EM4X_Util_Kit_v01`.
- **Recursos del hangar:** `HGR_<Zona>_<Pieza>_v<NN>`, por ejemplo `HGR_Terminal_Mision_v01`.
- **Materiales:** `M_<Pieza>`. **Texturas:** `T_<Pieza>_<Mapa>`.
- **Carpetas de entrega:**
  - Ruta: `Assets/_Arte/<Nave|Interior|Utileria|Hangar>/<Modelos|Materiales|Texturas|Prefabs>`, que crea el menú 19.
  - Cada bloque tiene un responsable y nadie escribe en la carpeta de otro.
  - **Los modeladores no tocan escenas, scripts ni `Data/`.** Solo el integrador asigna modelos en la tabla.

## 6. Piezas que deben ser objetos separados

- **Casco de cada módulo:** uno por módulo. La lanzadera y la bodega también van aparte, porque en el futuro serán separables.
- **Pasadizos y adaptadores:** separados de los cascos, porque su medida está pendiente de validar.
- **Hojas de puerta:** se mueven, así que van sueltas. Basta **una hoja genérica**; el juego la estira a cada tamaño.
- **Guías de las puertas:** fijas.
- **Paneles de puerta:** interactivos. La luz de estado (verde o roja) la pone el juego.
- **Consolas, asientos y mesa de misión:** cada uno es un equipo propio.
- **Kits:** se cuentan y desaparecen al recogerlos.
- **Balizas de reparación:** aparecen solo con avería.
- **Sonda e instrumentos.**
- **Elementos exteriores:** antena, paneles solares y radiadores, motor, reserva de la sonda y tapa de suministro.

## 7. Espacios que deben quedar libres

- **Paso libre de cada módulo habitable:** 3,8 × 3,8 m centrado, en el tramo z de la tabla. La esclusa tiene 2,4 × 2,4 m.
  - Ninguna pieza visible puede entrar en él.
  - Fuera de ese paso, los equipos deben quedarse dentro de **su** caja.
- **Pasadizos:** 2,4 × 2,4 m libres (2,2 m hacia Sistemas), con 1,2 m de largo y paredes de 0,2 m. *Pendiente de validar.*
- **Escotillas:** el hueco cuadrado de cada puerto debe quedar abierto. El marco (0,1 m) va por fuera del hueco.
- **Puertas:**
  - Las hojas se deslizan en un plano situado entre 0,08 y 0,14 m por dentro de la cara interior.
  - Abiertas, llegan hasta medio hueco + 0,15 m + el ancho de una hoja a cada lado.
  - Ahí no puede haber geometría: ni nervios, ni tuberías, ni marcos gruesos.
- **Paneles y puestos:** hay que dejar libre 1 m delante de cada panel y entre el puesto y su consola.
- **Interior del casco:** la cara interior es el límite. Detalles en relieve de pocos centímetros, como máximo; lo que sobresalga más debe ir en un equipo.

## 8. Medidas actuales

Las coordenadas de equipos, puertos, paneles y puntos son **locales del módulo** (x derecha, y arriba local, z a lo largo). Para pasarlas a la nave: `origen + derecha·x + arriba·y + eje·z`.

### Módulos

| Id | Módulo | Tipo | Centro de la cara de entrada (nave) | Eje (+z local) | Arriba local | Largo | Ø ext / Ø int (entre caras) | Pared | Paso libre (tramo z) |
|---|---|---|---|---|---|---|---|---|---|
| NC | Nodo central · zona común | Nodo | (0 ; 0 ; -5) | +Z | +Y | 10 | 10 / 9,4 | 0,3 | — |
| MA | Mando · Pilotaje | Habitable | (0 ; 0 ; 6,2) | +Z | +Y | 9 | 6 / 5,4 | 0,3 | 3,8 × 3,8 (0–4,6) |
| CI | Ciencia | Habitable | (-6,2 ; 0 ; 1,5) | −X | +Y | 9 | 6 / 5,4 | 0,3 | 3,8 × 3,8 (0–9) |
| CO | Comunicaciones | Habitable | (6,2 ; 0 ; 1,5) | +X | +Y | 7 | 6 / 5,4 | 0,3 | 3,8 × 3,8 (0–7) |
| IN | Ingeniería | Habitable | (0 ; 0 ; -6,2) | −Z | +Y | 9 | 6 / 5,4 | 0,3 | 3,8 × 3,8 (0–9) |
| HA | Hábitat | Habitable | (0 ; 6,2 ; 1,5) | +Y | +Z | 8 | 6 / 5,4 | 0,3 | 3,8 × 3,8 (0–8) |
| BO | Bodega | Habitable | (0 ; -5,6 ; 1,5) | −Y | +Z | 6 | 6 / 5,4 | 0,3 | 3,8 × 3,8 (0–6) |
| N2 | Nodo de popa | Nodo | (0 ; 0 ; -16,4) | −Z | +Y | 5 | 6,6 / 6 | 0,3 | — |
| SI | Sistemas | Habitable | (0 ; 4,5 ; -18,9) | +Y | +Z | 6 | 6 / 5,4 | 0,3 | 3,8 × 3,8 (0–6) |
| ES | Esclusa | Esclusa | (0 ; -3,9 ; -18,9) | −Y | +Z | 4,5 | 4,4 / 3,8 | 0,3 | 2,4 × 2,4 (0–4,5) |
| PR | Etapa de propulsión | Propulsion | (0 ; 0 ; -22) | −Z | +Y | 15 | 6 / sin interior | — | — |
| LZ | Lanzadera L-4 | Lanzadera | (0 ; -6,05 ; 6,7) | +Z | +Y | 12 | caja 9,2 × 4,9 × 12 | 0,3 (techo 0,5) | — |

### Conexiones y escotillas

| Conexión | Puerto A (local) | Puerto B (local) | Escotilla | Unión | Largo |
|---|---|---|---|---|---|
| NC.proa ↔ MA.inicio | (0 ; 0 ; 10) | (0 ; 0 ; 0) | 2,4 × 2,4 | PASADIZO cuadrado 2,4 × 2,4 libre, paredes 0,2 (ext. 2,8) | 1,2 |
| NC.popa ↔ IN.inicio | (0 ; 0 ; 0) | (0 ; 0 ; 0) | 2,4 × 2,4 | PASADIZO cuadrado 2,4 × 2,4 libre, paredes 0,2 (ext. 2,8) | 1,2 |
| NC.babor ↔ CI.inicio | (-5 ; 0 ; 6,5) | (0 ; 0 ; 0) | 2,4 × 2,4 | PASADIZO cuadrado 2,4 × 2,4 libre, paredes 0,2 (ext. 2,8) | 1,2 |
| NC.estribor ↔ CO.inicio | (5 ; 0 ; 6,5) | (0 ; 0 ; 0) | 2,4 × 2,4 | PASADIZO cuadrado 2,4 × 2,4 libre, paredes 0,2 (ext. 2,8) | 1,2 |
| NC.dorsal ↔ HA.inicio | (0 ; 5 ; 6,5) | (0 ; 0 ; 0) | 2,4 × 2,4 | PASADIZO cuadrado 2,4 × 2,4 libre, paredes 0,2 (ext. 2,8) | 1,2 |
| NC.ventral ↔ BO.inicio | (0 ; -5 ; 6,5) | (0 ; 0 ; 0) | 1,4 × 1,4 | adaptador octogonal int. 2,2 / ext. 2,6 | 0,6 |
| IN.fin ↔ N2.inicio | (0 ; 0 ; 9) | (0 ; 0 ; 0) | 2,4 × 2,4 | PASADIZO cuadrado 2,4 × 2,4 libre, paredes 0,2 (ext. 2,8) | 1,2 |
| N2.dorsal ↔ SI.inicio | (0 ; 3,3 ; 2,5) | (0 ; 0 ; 0) | 2,2 × 2,2 | PASADIZO cuadrado 2,2 × 2,2 libre, paredes 0,2 (ext. 2,6) | 1,2 |
| N2.ventral ↔ ES.inicio | (0 ; -3,3 ; 2,5) | (0 ; 0 ; 0) | 1,4 × 1,4 | adaptador octogonal int. 2,2 / ext. 2,6 | 0,6 |
| N2.fin ↔ PR.inicio | (0 ; 0 ; 5) | (0 ; 0 ; 0) | 1,4 × 1,4 | estructural, SIN paso | 0,6 |
| MA.atraque ↔ LZ.atraque | (0 ; -3 ; 5,5) | (0 ; 2,45 ; 5) | 1 × 1 | adaptador octogonal int. 1,8 / ext. 2,2 | 0,6 |
| N2.suministro (sin módulo al otro lado) | (-3,3 ; 0 ; 2,5) | — | 1,4 × 1,4 | tapa CERRADA (exterior) | — |
| N2.sonda (sin módulo al otro lado) | (3,3 ; 0 ; 2,5) | — | 1,4 × 1,4 | sin escotilla (reserva o estructural) | — |
| ES.exterior (sin módulo al otro lado) | (0 ; 0 ; 4,5) | — | 1,4 × 1,4 | tapa CERRADA (exterior) | — |

### Puertas y paneles

| Unión | Tipo | Puerta en (módulo.puerto) | Hojas | Hoja (ancho × alto × grosor) | Paneles (módulo: posición local → mira hacia) |
|---|---|---|---|---|---|
| lanzadera | Mando ↔ Lanzadera | MA.atraque | 4 (telescópica, 2 por lado) | 0,28 × 1,1 × 0,04 | MA: (0 ; -2,65 ; 4,3) → +Y; LZ: (0 ; 1,9 ; 3,9) → −Y |
| lanzadera | Mando ↔ Lanzadera | LZ.atraque | 2 (1 por lado) | 0,52 × 1,1 × 0,04 | MA: (0 ; -2,65 ; 4,3) → +Y; LZ: (0 ; 1,9 ; 3,9) → −Y |
| bodega | Nodo central ↔ Bodega | NC.ventral | 2 (1 por lado) | 0,72 × 1,5 × 0,04 | NC: (0 ; -4,65 ; 7,8) → +Y; BO: (2,05 ; 0 ; 0,33) → +Z |
| bodega | Nodo central ↔ Bodega | BO.inicio | 2 (1 por lado) | 0,72 × 1,5 × 0,04 | NC: (0 ; -4,65 ; 7,8) → +Y; BO: (2,05 ; 0 ; 0,33) → +Z |
| esclusa | Nodo de popa ↔ Esclusa | ES.inicio | 2 (1 por lado) | 0,72 × 1,5 × 0,04 | N2: (0 ; -2,95 ; 3,6) → +Y; ES: (1,74 ; 0 ; 0,33) → +Z |
| ais_mando | aislamiento (normalmente ABIERTA) | NC.proa | 2 (1 por lado) | 1,22 × 2,5 × 0,04 | NC: (0 ; 2,08 ; 9,67) → −Z; MA: (0 ; 2,08 ; 0,33) → +Z |
| ais_ingenieria | aislamiento (normalmente ABIERTA) | NC.popa | 2 (1 por lado) | 1,22 × 2,5 × 0,04 | NC: (0 ; 2,08 ; 0,33) → +Z; IN: (0 ; 2,08 ; 0,33) → +Z |
| ais_ciencia | aislamiento (normalmente ABIERTA) | CI.inicio | 4 (telescópica, 2 por lado) | 0,63 × 2,5 × 0,04 | CI: (0 ; 2,08 ; 0,33) → +Z; NC: (-4,65 ; 0 ; 8,2) → +X |
| ais_comunicaciones | aislamiento (normalmente ABIERTA) | CO.inicio | 4 (telescópica, 2 por lado) | 0,63 × 2,5 × 0,04 | CO: (0 ; 2,08 ; 0,33) → +Z; NC: (4,65 ; 0 ; 8,2) → −X |
| ais_habitat | aislamiento (normalmente ABIERTA) | HA.inicio | 4 (telescópica, 2 por lado) | 0,63 × 2,5 × 0,04 | HA: (0 ; 2,08 ; 0,33) → +Z; NC: (0 ; 4,65 ; 8,2) → −Y |
| ais_popa | aislamiento (normalmente ABIERTA) | N2.inicio | 2 (1 por lado) | 1,22 × 2,5 × 0,04 | N2: (0 ; 2,08 ; 0,33) → +Z; IN: (0 ; 2,08 ; 8,67) → −Z |
| ais_sistemas | aislamiento (normalmente ABIERTA) | SI.inicio | 2 (1 por lado) | 1,12 × 2,3 × 0,04 | SI: (0 ; 2,08 ; 0,33) → +Z; N2: (0 ; 2,95 ; 4,2) → −Y |

### Equipos interiores

| Módulo | Pieza | Tipo | Centro (local) | Tamaño x × y × z (local) |
|---|---|---|---|---|
| NC | Mesa de misión | Mesa | (0 ; -3,15 ; 2,8) | 2,4 × 0,1 × 2,4 |
| NC | Pie de la mesa | Mesa | (0 ; -3,95 ; 2,8) | 0,3 × 1,5 × 0,3 |
| NC | Pantalla de misión | Pantalla | (-4,6 ; 0,3 ; 2,8) | 0,15 × 1,5 × 2,6 |
| NC | Taquillas comunes | Bastidor | (4,35 ; 0 ; 2,6) | 0,7 × 2,2 × 3 |
| MA | Bastidor de navegación (estribor) | Bastidor | (2,35 ; 0 ; 2,6) | 0,7 × 2,2 × 4 |
| MA | Bastidor de navegación (babor) | Bastidor | (-2,35 ; 0 ; 2,6) | 0,7 × 2,2 × 4 |
| MA | Bastidor de guiado (techo) | Bastidor | (0 ; 2,35 ; 2,6) | 2,2 × 0,7 × 4 |
| MA | Plataforma de pilotaje | Otro | (0 ; -2,55 ; 7,6) | 2 × 0,3 × 2,2 |
| MA | Consola v0 (del compañero) | Consola | (0 ; -1,88 ; 8) | 2,43 × 1,05 × 0,85 |
| MA | Asiento de pilotaje | Asiento | (0 ; -1,9 ; 7) | 0,6 × 1 × 0,6 |
| CI | Consola de Ciencia | Consola | (2,35 ; 0 ; 4) | 0,7 × 1,2 × 2 |
| CI | Bastidor de instrumentos 1 | Bastidor | (2,35 ; 0 ; 1,6) | 0,7 × 2,2 × 2 |
| CI | Bastidor de instrumentos 2 | Bastidor | (2,35 ; 0 ; 6,9) | 0,7 × 2,2 × 3 |
| CI | Bastidores de laboratorio (pared) | Bastidor | (-2,35 ; 0 ; 4,8) | 0,7 × 2,2 × 7,8 |
| CI | Bastidores (techo) | Bastidor | (0 ; 2,35 ; 4,8) | 2,2 × 0,7 × 7,8 |
| CI | Bastidores (suelo) | Bastidor | (0 ; -2,35 ; 4,8) | 2,2 × 0,7 × 7,8 |
| CO | Consola de Comunicaciones | Consola | (2,35 ; 0 ; 3,5) | 0,7 × 1,2 × 2 |
| CO | Bastidor de radio 1 | Bastidor | (2,35 ; 0 ; 1,4) | 0,7 × 2,2 × 1,6 |
| CO | Bastidor de radio 2 | Bastidor | (2,35 ; 0 ; 5,6) | 0,7 × 2,2 × 1,6 |
| CO | Bastidores de datos (pared) | Bastidor | (-2,35 ; 0 ; 3,8) | 0,7 × 2,2 × 5,8 |
| CO | Bastidores (techo) | Bastidor | (0 ; 2,35 ; 3,8) | 2,2 × 0,7 × 5,8 |
| CO | Bastidores (suelo) | Bastidor | (0 ; -2,35 ; 3,8) | 2,2 × 0,7 × 5,8 |
| IN | Consola de Ingeniería | Consola | (2,35 ; 0 ; 4) | 0,7 × 1,2 × 2 |
| IN | Bastidor de motor | Bastidor | (2,35 ; 0 ; 1,6) | 0,7 × 2,2 × 2 |
| IN | Bastidor de energía | Bastidor | (2,35 ; 0 ; 6,9) | 0,7 × 2,2 × 3 |
| IN | Bastidores de control (pared) | Bastidor | (-2,35 ; 0 ; 4,5) | 0,7 × 2,2 × 7,8 |
| IN | Bastidores (techo) | Bastidor | (0 ; 2,35 ; 4,5) | 2,2 × 0,7 × 7,8 |
| IN | Bastidores (suelo) | Bastidor | (0 ; -2,35 ; 4,5) | 2,2 × 0,7 × 7,8 |
| HA | Cocina y mesa | Bastidor | (-2,35 ; 0 ; 1,8) | 0,7 × 2,2 × 2,4 |
| HA | Higiene | Bastidor | (2,35 ; 0 ; 1,8) | 0,7 × 2,2 × 2,4 |
| HA | Ejercicio | Bastidor | (0 ; 2,35 ; 1,8) | 2,2 × 0,7 × 2,4 |
| HA | Víveres | Bastidor | (0 ; -2,35 ; 1,8) | 2,2 × 0,7 × 2,4 |
| HA | Camarote 1 (sujeción y descanso) | Camarote | (2,3 ; 0 ; 5,8) | 0,8 × 2,2 × 3 |
| HA | Camarote 2 (sujeción y descanso) | Camarote | (-2,3 ; 0 ; 5,8) | 0,8 × 2,2 × 3 |
| HA | Camarote 3 (sujeción y descanso) | Camarote | (0 ; 2,3 ; 5,8) | 2,2 × 0,8 × 3 |
| HA | Camarote 4 (sujeción y descanso) | Camarote | (0 ; -2,3 ; 5,8) | 2,2 × 0,8 × 3 |
| BO | Bastidor de carga | Carga | (2,3 ; 0 ; 3) | 0,8 × 2,2 × 4,8 |
| BO | Bastidor de carga | Carga | (-2,3 ; 0 ; 3) | 0,8 × 2,2 × 4,8 |
| BO | Bastidor de carga | Carga | (0 ; 2,3 ; 3) | 2,2 × 0,8 × 4,8 |
| BO | Bastidor de carga | Carga | (0 ; -2,3 ; 3) | 2,2 × 0,8 × 4,8 |
| SI | Soporte vital: aire y agua | Bastidor | (2,35 ; 0 ; 3) | 0,7 × 2,2 × 4,8 |
| SI | Energía y baterías | Bastidor | (-2,35 ; 0 ; 3) | 0,7 × 2,2 × 4,8 |
| SI | Control térmico | Bastidor | (0 ; 2,35 ; 3) | 2,2 × 0,7 × 4,8 |
| SI | Repuestos | Bastidor | (0 ; -2,35 ; 3) | 2,2 × 0,7 × 4,8 |
| ES | Traje EVA 1 | Bastidor | (1,45 ; 0 ; 2,25) | 0,5 × 1,9 × 1,2 |
| ES | Traje EVA 2 | Bastidor | (-1,45 ; 0 ; 2,25) | 0,5 × 1,9 × 1,2 |
| LZ | Consola de la lanzadera | Consola | (0 ; -0,9 ; 10,9) | 2,4 × 1,1 × 0,8 |
| LZ | Asiento de lanzamiento 1 | Asiento | (-2,4 ; -0,95 ; 9,3) | 0,6 × 1 × 0,6 |
| LZ | Asiento de lanzamiento 2 | Asiento | (-0,8 ; -0,95 ; 9,3) | 0,6 × 1 × 0,6 |
| LZ | Asiento de lanzamiento 3 | Asiento | (0,8 ; -0,95 ; 9,3) | 0,6 × 1 × 0,6 |
| LZ | Asiento de lanzamiento 4 | Asiento | (2,4 ; -0,95 ; 9,3) | 0,6 × 1 × 0,6 |

### Puestos y puntos interactivos

| Punto | Módulo | Posición local | Notas |
|---|---|---|---|
| Puesto de Pilotaje (ojos del tripulante) | MA | (0 ; -0,5 ; 7) | mira hacia (0 ; -0,15 ; 1); E a menos de 1,7 m |
| Puesto de Ingeniería (ojos del tripulante) | IN | (1,2 ; -0,4 ; 4) | mira hacia (1 ; 0 ; 0); E a menos de 1,7 m |
| Puesto de Ciencia (ojos del tripulante) | CI | (1,2 ; -0,4 ; 4) | mira hacia (1 ; 0 ; 0); E a menos de 1,7 m |
| Puesto de Comunicaciones (ojos del tripulante) | CO | (1,2 ; -0,4 ; 3,5) | mira hacia (1 ; 0 ; 0); E a menos de 1,7 m |
| Mesa de misión (uso) | NC | (0 ; -1,9 ; 2,8) | punto del MVP (JuegoMVP.CrearPuntos); E a menos de 1,7 m |
| Armario de kits | BO | (0 ; 0 ; 3) | punto del MVP (JuegoMVP.CrearPuntos); E a menos de 1,7 m |
| Reparación | MA | (0 ; 0 ; 3) | punto del MVP (JuegoMVP.CrearPuntos); la baliza solo se ve con una brecha en ese módulo |
| Reparación | IN | (0 ; 0 ; 2,2) | punto del MVP (JuegoMVP.CrearPuntos); la baliza solo se ve con una brecha en ese módulo |
| Reparación | CI | (0 ; 0 ; 2,2) | punto del MVP (JuegoMVP.CrearPuntos); la baliza solo se ve con una brecha en ese módulo |
| Reparación | CO | (0 ; 0 ; 2,2) | punto del MVP (JuegoMVP.CrearPuntos); la baliza solo se ve con una brecha en ese módulo |
| Reparación | HA | (0 ; 0 ; 4) | punto del MVP (JuegoMVP.CrearPuntos); la baliza solo se ve con una brecha en ese módulo |
| Reparación | SI | (0 ; 0 ; 3) | punto del MVP (JuegoMVP.CrearPuntos); la baliza solo se ve con una brecha en ese módulo |
| Cámara del asiento de Pilotaje | MA | (0 ; 0,2 ; 7) | puesto + 0,7 m arriba (VistaVueloMVP) |
## 9. Vistas

- `em4x_vistas_actuales.png`: planta, perfil y alzado dibujados con estas mismas medidas. Incluye puertas, paneles, puestos y puntos de reparación.
- `em4x_modulo_ciencia.png`: ficha del módulo de prueba (sección transversal y planta).

**Capturas desde Unity** (las hace el integrador): en la escena ModeloPrueba, Play y **F12**. Se guardan planta, perfil, alzado y perspectiva en `Capturas/` (fuera de Assets), a 2400 × 1400 px. La consola indica cuántos metros cubre cada imagen. Hay que hacerlas con F3 en modelos y en provisional para comparar.

## 10. Cómo se integra un modelo (solo el integrador)

1. El modelador entrega el prefab en su carpeta de `_Arte` y avisa.
2. El integrador ejecuta el menú **21** (comprobación de nombres, escala, colisiones y materiales); el informe queda en `Informes/ComprobacionArte_<fecha>.txt`.
3. Asigna el prefab en `Data/RecursosVisualesEM4X` y prueba en **ModeloPrueba**.
4. Cuando todo cuadra, juega ExpedicionMVP con la misma tabla. La tabla se asigna con el menú 19 con la escena abierta y luego hay que guardar la escena.

**Qué hace el juego con el modelo:**
- Lo coloca como **visual**. La pieza provisional deja de dibujarse, pero **conserva su colisión**.
- Puertas, paneles (E), aislamiento, diagnóstico, reparación, cámaras, asiento, estado compartido y balance no cambian.
- Si el modelo trae colisiones o física, se quitan.
- Si falta un modelo, se ve la pieza provisional.

**Recrear la escena no borra nada:** la nave se construye al pulsar Play a partir de los datos y los modelos viven en `_Arte`. Los menús 16, 19 y 20 nunca sustituyen una escena existente.

**Peligro conocido:** los menús antiguos 8 y «Muestra visual» rehacen la escena *Nave*. No hay que usarlos.

## 11. Reparto del trabajo

Por prioridad. El orden es el de cada lista; «Independiente» indica si se puede empezar sin esperar a nadie.

**A · Nave (casco y módulos)**
1. `EM4X_CI_Casco_v01`, el módulo de prueba. **Independiente**: es lo primero.
2. Cascos MA, IN y CO, después de que se acepte la prueba de CI. Dependen de la prueba 1.
3. NC (Ø 10) y N2 (Ø 6,6), HA y SI. Dependen de la prueba 1.
4. BO, ES, PR y elementos exteriores (antena, paneles solares, motor). Independientes de lo interior, pero menos urgentes.
5. Pasadizos y adaptadores: **esperan** a que el equipo valide los pasadizos.

**B · Interior**
1. `EM4X_CI_ConsolaCiencia_v01` (prueba 1). **Independiente.** Hoja y panel de puerta: después de validar el paso.
2. Consola de Pilotaje (caja 2,43 × 1,05 × 0,85) y asiento de Pilotaje (0,6 × 1 × 0,6). Independientes; los usa el MVP.
3. Consolas de Comunicaciones e Ingeniería y mesa de misión. Independientes.
4. Baliza de reparación y marcas de puesto. Independientes.
5. Bastidores, camarotes y cocina: al final. **No retrasan las pruebas.**

**C · Utilería**
1. `EM4X_Util_Kit_v01`, caja de unos 0,25 × 0,18 × 0,35 m. Independiente; el juego la usa ya.
2. Sonda de despliegue, instrumentos (cámara multiespectral, espectrómetro) y cajas de suministros. Independientes.

**D · Hangar terrestre** (escena nueva, todavía no implementada)
1. Estructura básica, terminal de misión y estanterías de equipos y suministros. Independientes, pero sin medidas cerradas: **solo bocetos o bloques** hasta que exista la versión jugable provisional.
2. Zona de embarque y maqueta u holograma de la EM-4X: después de la versión provisional.

Las piezas que usan las misiones son las de B1–B4 y C1, y van antes que la decoración.

## 12. Apartado separado · Lanzadera L-4 y edificio de integración (13-11-2026)

**No cambia el encargo actual:** el módulo de Ciencia (secciones 2–10) sigue siendo la prioridad y sus medidas no cambian. Este apartado es un encargo **posterior y separado**. La lanzadera L-4 es el vehículo **terrestre** que sube a la tripulación. No es la EM-4X, que está en órbita.

**Estado:** el juego ya tiene una versión con **geometría provisional** (escena HangarExpedicion, zona B). Las medidas están **leídas del código** (`Scripts/HangarTerrestre/ModeloLanzadera.cs` y `HangarTerrestre.cs`, `ConstruirIntegracion`). Los modelos se guardan en `Assets/_Arte/Hangar/Lanzadera/` y `Assets/_Arte/Hangar/Integracion/`. **Actualización 13-11:** ya hay puntos de sustitución (ids `L4/EtapaPrincipal`, `L4/Refuerzos/2` o `/4`, `L4/TanqueTransferencia/1` o `/2`, `L4/Bodega/1` o `/2`, `L4/Capsula`; ver sección 13). La cubierta del edificio ahora es abovedada (sección 13 B). Hasta validar AG, solo bocetos o bloques.

**Referencia visual:** la imagen del edificio de montaje vertical (vehículo de pie, nave industrial alta, estructuras metálicas, plataformas, pasarelas, iluminación de trabajo y escala) es solo de **ambiente y composición**. No se copian el vehículo, sus marcas ni sus medidas. **Sin logotipos ni marcas reales** (ni de NASA ni de otras agencias).

### Lanzadera L-4 · piezas separadas (Y arriba, metros, pivote en el centro de la base de cada pieza)

La lanzadera es modular porque el jugador cambia tres tipos de pieza. Cada alternativa es un objeto **aparte**.

| Pieza | Nombre de archivo | Medidas provisionales | Notas |
|---|---|---|---|
| Etapa principal | `L4_EtapaPrincipal_v01` | cilindro Ø 5 × 18 m; toberas Ø 3,2 × 1 m bajo la base | Origen = base de la etapa. Siempre presente. |
| Propulsor de refuerzo | `L4_Refuerzo_v01` | Ø 1,8 × 14 m + morro Ø 1,3 × 1,2 m + tobera Ø 1,2 × 0,8 m | Uno solo; el juego lo coloca 0, 2 (a los lados, eje ±Z) o 4 veces (a 45°), a 3,55 m del eje. |
| Tanque de transferencia (1 tramo) | `L4_TanqueTransf_1_v01` | Ø 5 × 3 m | Encima de la etapa. |
| Tanque de transferencia (2 tramos) | `L4_TanqueTransf_2_v01` | Ø 5 × 5 m | Encima de la etapa. |
| Bodega pequeña | `L4_Bodega_Pequena_v01` | Ø 5 × 2,5 m, compuerta 2,4 m de ancho en +Z | Encima del tanque (o de la etapa si no hay tanque). |
| Bodega grande | `L4_Bodega_Grande_v01` | Ø 5 × 4 m, compuerta igual | Igual que la pequeña. |
| Adaptador | `L4_Adaptador_v01` | Ø 4,4 × 1 m | Entre la pila y la cápsula. |
| Cápsula | `L4_Capsula_v01` | cuerpo Ø 4,4 × 3,2 m + hombro Ø 3,2 × 0,8 m + morro Ø 2 × 0,5 m | Escotilla 1,2 × 1,8 m en **−Z** (lado del brazo de acceso); ventanas en +Z. |
| Torre de escape | `L4_TorreEscape_v01` | Ø 0,5 × 3 m | Encima de la cápsula. |
| Cabina interior | `L4_Cabina_v01` | caja interior 3,6 × 2,8 × 2,8 m, un asiento, consola, compuerta lateral | Se ve desde el asiento en la secuencia de despegue; ventana en el techo. |

Alturas: la pila se apila sin huecos, así que la altura total va de **26,5 m** (sin tanque ni bodega) a **35,5 m** (tanque de 2 tramos y bodega grande). Las piezas no deben sobresalir de Ø 5 m, salvo los refuerzos.

### Edificio de integración (zona B del hangar) · piezas

| Pieza | Nombre | Medidas provisionales |
|---|---|---|
| Nave del edificio | `INT_Nave_v01` | interior 32 × 36 m, 46 m de alto. Paso desde el hangar de 10 m de ancho × 9 m de alto en la pared oeste. |
| Plataforma de la lanzadera | `INT_Plataforma_v01` | 12 × 12 × 2 m. El vehículo apoya encima. |
| Torre de servicio | `INT_Torre_v01` | 3 × 3 × 40 m. Hay dos, a 8,5 m del eje del vehículo (norte y sur). |
| Plataforma de trabajo | `INT_PlataformaTrabajo_v01` | 6 m de ancho, a 10, 18, 26 y 34 m. **Debe acabar a 4,7 m del eje** para dejar sitio a los refuerzos. |
| Pasarela de acceso | `INT_Pasarela_v01` | 1,6 m de ancho, a 18 y 26 m, de la pared oeste a la torre sur |
| Puente grúa | `INT_PuenteGrua_v01` | viga 1,2 × 1,4 × 34 m a 42 m de altura, con carro y gancho separados (se mueven en la animación). |
| Focos de trabajo | `INT_Foco_v01` | pequeños; luz cálida |
| Consola B | `INT_Consola_v01` | como las terminales del hangar (pedestal 1,2 × 1,1 × 0,6 m) |
| Muelle de la bodega | `INT_Muelle_v01` | 6 × 6 m en el suelo; ahí se dejan los contenedores que viajan en la L-4 |

**Espacios libres:** alrededor del vehículo, un cilindro de 4,7 m de radio sin estructuras (refuerzos y animación de la grúa). Sobre el vehículo, el recorrido del gancho hasta 42 m. En el suelo, el paso del hangar y el camino del carro hasta el muelle.

**Prioridad del apartado:** 1) cápsula y etapa principal; 2) un refuerzo, las dos bodegas y los dos tanques; 3) torre, plataforma y plataformas de trabajo; 4) puente grúa, pasarelas y nave del edificio. Todo **después** del módulo de Ciencia.

## 13. Fichas separadas · estación del holograma, edificio de la L-4, transporte VC-1 y servicio orbital (13-11-2026)

**No cambia el encargo de Ciencia** (secciones 2–10): sus medidas siguen igual. **Estado de estas fichas: posiciones y medidas PROVISIONALES**, leídas del código de esta versión y **todavía no comprobadas en Unity** (pruebas AG pendientes). Hasta que se validen, **solo bocetos o bloques; no arte final.** Unidades: metros, Y arriba. Las coordenadas son del hangar (x a la derecha desde la entrada, z hacia el fondo) salvo en D, que usa las de la EM-4X.

**Cómo se sustituye un bloque:** cada pieza provisional sustituible es un *punto de sustitución* con un id (columna «Id»). El integrador añade en la misma tabla de la EM-4X (`Data/RecursosVisualesEM4X.asset`) una entrada de tipo **Escenario** con «Nombre» = id y el modelo. Después ejecuta una vez *Prototipo → Arte y pruebas → 5. Conectar la tabla de sustitución al hangar*. El modelo se pone como hijo del punto y sigue sus animaciones. El bloque deja de verse, pero sus colisiones, puntos de carga y lógica se quedan. Pivote del modelo = pivote del punto. No se cambia masa ni balance por el arte.

### A · Estación del holograma (hangar, zona A)

| Pieza | Id | Medidas | Posición y orientación | Pivote |
|---|---|---|---|---|
| Mesa y proyector | `A/MesaHolograma` | 5 × 0,9 × 5 | centro (0; 0,45; 22) | centro de la caja |
| Consola de pieza (×3: motor, tanque, instrumento) | `A/Consola` | pedestal 1,2 × 1,1 × 0,6; pantalla 1,1 × 0,7 a 1,45 m | (−2,8; 0; 18,2), (0; 0; 17,6), (2,8; 0; 18,2); la pantalla mira hacia fuera de la mesa | base, centro |
| Consola A2 · comparar y encargar | `A/Consola` | igual | (4,6; 0; 22), mira a +X | base, centro |
| Terminal 2 · configuración orbital | — (terminal común) | igual | (−4,6; 0; 22), mira a −X | base, centro |
| Pedestal de maqueta (×3) | `A/PedestalMaqueta` | 1,6 × 0,9 × 1,6 | (−3,4; 0,45; 26,4), (0; 0,45; 26,7), (3,4; 0,45; 26,4) | centro |
| Maquetas | (las piezas de C, reducidas) | motor y tanque 1:10, instrumento 1:2; **altura máxima 1,6 m** | encima de los pedestales | — |
| Tarima de la estación | — | 12 × 11, suelo | centro (0; 22) | — |

**Espacio libre:** sobre la mesa, de 1 a 4,5 m de alto, el holograma (unos 4 m de largo); nada lo tapa. A los dos lados de la estación (|x| > 6) queda el paso. Los rótulos de las consolas están a 1,95 m, por debajo del holograma. **Elementos móviles:** ninguno (el holograma lo genera el juego). **Prioridad:** 1.

### B · Edificio de la L-4 (zona B) · cubierta curva

| Pieza | Id | Medidas y posición |
|---|---|---|
| Cubierta abovedada | `B/Cubierta` | semielipse sobre z (2–38): alero a 36 m, clave a 52 m; corre a lo largo de X (20,5–52,5). Perfil ovalado en los hastiales oeste y este. |
| Arcos de estructura | (provisionales) | uno cada 6 m en x (22,5; 28,5; …), 0,6 m de ancho, 0,7 m bajo la cubierta |
| Paredes | — | hasta 36 m; hastiales hasta la bóveda |
| Puerta de salida | `B/PuertaSalida` | pared este, z 12–28, 42 m de alto; dos hojas de 8 m (cerradas) |
| Puente grúa | `B/PuenteGrua` | viga 1,2 × 1,4 × 30 a 42 m, entre los rieles de z 5 y z 35 (a 42,6 m); el carro y el gancho se mueven |
| Torres, plataformas, pasarelas, focos, muelle | ver sección 12 | sin cambios |
| Edificio visto desde fuera | `B/EdificioExterior` | misma forma (32 × 36 m, alero 36, clave 52), puerta de 16 × 42 m hacia la plataforma |

**Espacios que no se invaden:**
- El gálibo de la lanzadera: cilindro de 4,7 m de radio hasta 37,5 m de alto en (36,5; 20).
- El recorrido de salida: franja z 14–26, desde x 30,5 hasta la puerta, 40 m de alto.
- El puente grúa: z 5–35 a 42 m. La bóveda pasa de 44,8 m allí.

**Pivotes:** la cubierta tiene el pivote en el centro de la planta, en el suelo; cada hoja de puerta, en el centro de su base. **Prioridad:** 3.

### C · Transporte orbital no tripulado VC-1 (zona C2, nave anexa detrás del hangar)

| Pieza | Id | Medidas | Notas |
|---|---|---|---|
| Módulo de servicio | `VC1/ModuloServicio` | Ø 7,6 × 4 (z 0–4) + 4 toberas de maniobra Ø 1 y paneles solares plegados | pivote: centro de la popa (z = 0) |
| Bodega principal | `VC1/Bodega` | Ø 7,6 exterior / 7,2 útil × 23 (z 4–27), abierta por arriba | **2 compuertas móviles** con bisagra en los bordes superiores, giran 110° hacia fuera |
| Anillo de carga menor | `VC1/Anillo` | Ø 7,6 × 4 (z 27–31) | 12 módulos de combustible (3 × 2 × 2) y 3 contenedores |
| Proa y atraque | `VC1/Proa` | Ø 2,2 × 1,5 (z 31–32,5) | — |
| Lanzador provisional LC-2 | `LC2/Lanzador` | etapa central Ø 8 × 40; 2 aceleradores Ø 3,2 × 24 a x ±5,8; cofia Ø 8,6 × 32 (cubre el VC-1) | pivote: base, centro. Elementos móviles: cofia (se cierra) y llama |
| Plataforma exterior | `LC2/Plataforma` | 22 × 22 × 1 | fuera de los edificios |
| Contenedor de carga pequeña | `Carga/suministros`, `Carga/kits`, `Carga/sonda` | 0,75 × 0,7 × 1,1 (sonda 0,8 × 0,9 × 1,2) | pivote: centro de la base |
| Módulo de combustible | `Carga/ModuloCombustible` | Ø 2,4 × 1,4 | uno por tramo de 3000 kg |
| Soportes de preparación (×4) | `C2/Soporte` | 3 × 1 × (13,5 tanque · 5,5 motor · 2,6 instrumento y relevo) | en x −15; z 80 (tanque), 90,5 (motor), 95,5 (instrumento), 99 (relevo) |
| Puente grúa de C2 | `C2/PuenteGrua` | viga 27,5 × 0,9 × 1 a 16 m, recorre z; carro y gancho móviles | levanta las piezas a 12,5 m |
| Nave C2 | `C2/NaveExterior` (vista de fuera) | interior x −32 a −2,5 · z 64,5 a 104 · 18 m de alto; paso desde el hangar de 6 × 6 m (x −9 a −3) | VC-1 tumbado en 4 cunas (6 × 1 × 1,2), eje a 4,6 m, popa en z 70, x −26 |
| Componentes a escala real | `EM4X/Motor/1`, `EM4X/Tanque/2`, `EM4X/Instrumento/2`, `EM4X/Relevo/1`… | motor: Ø 4,5·√(empuje/400 kN) × 4·(velocidad de salida/4000) · tanque: Ø 4,6 × 13·(capacidad/36 000) · cámara Ø 1,1 × 1,5 · espectrómetro 1,2 × 1,2 × 1,5 · sensor Ø 1,1 × 2,1 · relevo 0,8 × 0,8 × 1,2 | pivote: centro de la cara de montaje; el largo sale hacia +Z |

**Espacios que no se invaden:**
- El volumen de la bodega (Ø 7,2) y el recorrido de la grúa, entre los soportes y la bodega a 12,5 m.
- El pasillo este de la nave C2 (x −11 a −3) y la entrega de carga pequeña (x −9 a −3, z 66–72).

**Prioridad:** 2.

### D · Servicio orbital (coordenadas de la EM-4X: proa +Z, Ciencia −X, Comunicaciones +X)

| Pieza | Id | Medidas | Posición |
|---|---|---|---|
| Plataforma de servicio | `Orbita/PlataformaServicio` | viga 3 × 1,4 × 44; dos brazos de unión 1 × 8 × 1 | (0; −12; −24), bajo la etapa de propulsión |
| Soporte de recepción | `Orbita/SoporteRecepcion` | bandeja 6 × 0,3 × 6 | (7; −10,5; −30) |
| Soporte de piezas retiradas | `Orbita/SoporteRetiradas` | bastidor 6 × 0,3 × 16, 4 huecos cada 3,4 m | (−8; −10,5; −14). Las piezas retiradas quedan sujetas aquí. |
| Brazo guiado (visual) | (provisional) | base 1,6 × 1,2 × 1,6 en (0; −11; −20); brazo de 0,5 × 0,5 que se estira hasta la pieza | **móvil** |
| Puntos de colocación | — | motor (0; 0; −37) hacia −Z · tanque (0; 0; −23) hacia −Z, dentro de la etapa · instrumento (−15,2; 0; 1,5) hacia −X · relevo (13,2; 0; 1,5) hacia +X | simplificación, no plano de ingeniería |
| Llegada del VC-1 | — | popa en (0; −21; −46), bodega hacia +Y | — |
| Atraque de la L-4 | — | (0; −3,2; 11), bajo el Mando | la cápsula sube hacia +Y |

**Espacios que no se invaden:** los módulos de la EM-4X (su distribución y medidas no cambian) y el volumen de atraque bajo el Mando. **Prioridad:** 4.
