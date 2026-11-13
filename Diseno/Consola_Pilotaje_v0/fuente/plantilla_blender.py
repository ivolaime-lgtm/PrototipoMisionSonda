# Consola de pilotaje v0 - MODELO BASE DE BLOQUES para el equipo de modelado.
# Uso: Blender 3.6 o superior -> pestaña Scripting -> Abrir este archivo -> Ejecutar (Run Script).
# Crea en la escena actual: dos raíces (consola y asiento), cada pieza como objeto separado con su pivote,
# los puntos de conexión como objetos vacíos (ANC_...), materiales provisionales y una flecha de ayuda hacia la proa.
# Ejes en Blender: X = derecha del piloto, +Y = hacia la ventana (proa), Z = arriba. Metros.
# No borra nada de la escena: si ya existe una colección "Consola_Pilotaje_v0", se crea otra con sufijo.
#
# Exportar a Unity: pon EXPORTAR_FBX = True y guarda antes el .blend (el FBX se escribe a su lado),
# o exporta a mano con: Forward = Z, Up = Y, Apply Transform activado, Apply Scalings = FBX All.
# En Unity, la flecha amarilla debe apuntar hacia +Z y el cuadrado azul quedar a la DERECHA (+X).
# Si no es así, avisad: el ajuste de ejes no está comprobado por quien generó este archivo.

import bpy, os

EXPORTAR_FBX = False

OBJETOS = __OBJETOS__
ANCLAJES = __ANCLAJES__
RAICES = __RAICES__
MATERIALES = __MATERIALES__


def material(nombre):
    m = bpy.data.materials.get(nombre)
    if m is not None:
        return m
    color, metalico, rugosidad, emision = MATERIALES[nombre]
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    m.diffuse_color = (color[0], color[1], color[2], 1.0)
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
        bsdf.inputs["Metallic"].default_value = metalico
        bsdf.inputs["Roughness"].default_value = rugosidad
        if emision:
            entrada = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
            if entrada is not None:
                entrada.default_value = (emision[0], emision[1], emision[2], 1.0)
            fuerza = bsdf.inputs.get("Emission Strength")
            if fuerza is not None:
                fuerza.default_value = 2.0
    return m


nombre_col = "Consola_Pilotaje_v0"
i = 1
while bpy.data.collections.get(nombre_col) is not None:
    i += 1
    nombre_col = "Consola_Pilotaje_v0_%d" % i
col = bpy.data.collections.new(nombre_col)
bpy.context.scene.collection.children.link(col)

raices = {}
for nombre, loc in RAICES.items():
    e = bpy.data.objects.new(nombre, None)
    e.empty_display_type = "PLAIN_AXES"
    e.empty_display_size = 0.3
    e.location = loc
    col.objects.link(e)
    raices[nombre] = e

for o in OBJETOS:
    me = bpy.data.meshes.new(o["nombre"])
    me.from_pydata([tuple(v) for v in o["v"]], [], [tuple(f) for f in o["f"]])
    for nombre_mat in o["mats"]:
        me.materials.append(material(nombre_mat))
    for poly, idx in zip(me.polygons, o["fm"]):
        poly.material_index = idx
    me.validate()
    me.update()
    ob = bpy.data.objects.new(o["nombre"], me)
    col.objects.link(ob)
    ob.parent = raices[o["raiz"]]
    ob.location = o["loc"]
    if o["nombre"].endswith("_COL"):
        ob.display_type = "WIRE"
        ob.hide_render = True

for a in ANCLAJES:
    e = bpy.data.objects.new(a["nombre"], None)
    e.empty_display_type = "ARROWS"
    e.empty_display_size = 0.08
    col.objects.link(e)
    e.parent = raices[a["raiz"]]
    e.location = a["loc"]

if EXPORTAR_FBX:
    if not bpy.data.filepath:
        raise RuntimeError("Guarda primero el archivo .blend: el FBX se escribe en la misma carpeta.")
    destino = os.path.join(os.path.dirname(bpy.data.filepath), "consola_pilotaje_v0.fbx")
    bpy.ops.object.select_all(action="DESELECT")
    for ob in col.objects:
        ob.select_set(True)   # incluye la flecha AYUDA para comprobar los ejes en Unity; borradla después
    bpy.ops.export_scene.fbx(filepath=destino, use_selection=True, object_types={"EMPTY", "MESH"},
                             axis_forward="Z", axis_up="Y", bake_space_transform=True,
                             apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False)
    print("FBX exportado en", destino)

print("Consola de pilotaje v0 creada en la colección", nombre_col)
