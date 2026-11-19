"""Export every mesh of the supplied floor without saving the source Blender file.

blender --background original.blend --python export_full_floor.py -- --output STAGING
Copy the completed Models, Textures and Data folders into Environment after export.
"""
import argparse
import bpy
import bmesh
import hashlib
import json
from pathlib import Path
import re
import shutil
import sys
from mathutils import Matrix, Vector

p = argparse.ArgumentParser()
p.add_argument('--output', required=True)
p.add_argument('--low-poly', action='store_true', help='Simplify detailed props in the runtime export, preserving every object and all architecture.')
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:])
out = Path(a.output).resolve()
for folder in ('Models', 'Textures', 'Data'):
    (out / folder).mkdir(parents=True, exist_ok=True)
source = Path(bpy.data.filepath)
origin = Vector((38.98, -1, 0))
objects = sorted((o for o in bpy.data.objects if o.type == 'MESH'), key=lambda o: o.name)
materials = sorted({m for o in objects for m in o.data.materials if m}, key=lambda m: m.name)
textures = {}
# Fix three source entrances whose frame openings are smaller than the inspection capsule.
# These changes exist only in the exported working scene; the original .blend is never saved.
entrance_adjustments=[]
original_world={obj:obj.matrix_world.copy() for obj in objects}
for obj in objects:
    obj.parent=None
    obj.matrix_world=original_world[obj]
for name,minimum_width,minimum_height in [('Door.012',1.05,2.2),('Door.014',1.10,2.2),('Door.024',1.3114,2.3)]:
    door=bpy.data.objects.get(name)
    if not door:continue
    corners=[door.matrix_world@Vector(v) for v in door.bound_box]
    lo=Vector([min(v[i] for v in corners) for i in range(3)])
    hi=Vector([max(v[i] for v in corners) for i in range(3)])
    width_axis=0 if hi.x-lo.x>hi.y-lo.y else 1
    depth_axis=1-width_axis
    center=(lo+hi)*.5
    pivot=center.copy();pivot.z=lo.z
    scale=Vector((1,1,1));scale[width_axis]=max(1,minimum_width/(hi[width_axis]-lo[width_axis]));scale.z=max(1,minimum_height/(hi.z-lo.z))
    transform=Matrix.Translation(pivot)@Matrix.Diagonal((*scale,1))@Matrix.Translation(-pivot)
    suffix=name[len('Door'):]
    for obj in objects:
        if obj==door or obj.name in ['Handle_Front'+suffix,'Handle_Back'+suffix]:obj.matrix_world=transform@obj.matrix_world
    cut_lo=lo.copy();cut_hi=hi.copy()
    cut_lo[width_axis]=center[width_axis]-max(minimum_width,hi[width_axis]-lo[width_axis])*.5+.06
    cut_hi[width_axis]=center[width_axis]+max(minimum_width,hi[width_axis]-lo[width_axis])*.5-.06
    depth=.95 if name=='Door.024' else .25
    cut_lo[depth_axis]=center[depth_axis]-depth;cut_hi[depth_axis]=center[depth_axis]+depth
    cut_lo.z=lo.z-.08;cut_hi.z=lo.z+minimum_height-.05
    changed=[]
    for obj in objects:
        if not (obj.name.startswith(('Plane','Wall')) or name=='Door.024' and obj.name=='Cube.391'):continue
        bounds=[obj.matrix_world@Vector(v) for v in obj.bound_box]
        if any(max(v[i] for v in bounds)<cut_lo[i] or min(v[i] for v in bounds)>cut_hi[i] for i in range(3)):continue
        mesh=obj.data.copy();mesh.transform(obj.matrix_world)
        bm=bmesh.new();bm.from_mesh(mesh)
        for axis in range(3):
            normal=Vector((0,0,0));normal[axis]=1
            for coordinate in (cut_lo[axis],cut_hi[axis]):
                point=Vector((0,0,0));point[axis]=coordinate
                bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=point,plane_no=normal,dist=.00001)
        remove=[f for f in bm.faces if abs(f.normal.z)<.5 and all(cut_lo[i]-.00001<=f.calc_center_median()[i]<=cut_hi[i]+.00001 for i in range(3))]
        if remove:
            bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(mesh);mesh.transform(obj.matrix_world.inverted());obj.data=mesh;changed.append(obj.name)
        else:bpy.data.meshes.remove(mesh)
        bm.free()
    entrance_adjustments.append({'door':name,'originalMin':list(lo),'originalMax':list(hi),'runtimeWidth':minimum_width,'runtimeHeight':minimum_height,'wallMeshesRefined':changed})
bpy.context.view_layer.update()

material_records = []
missing = []

def image_path(image):
    if image.name in textures:
        return textures[image.name]
    basename = image.filepath.replace('\\', '/').split('/')[-1]
    original = Path(bpy.path.abspath(image.filepath)) if image.filepath else None
    found = original if original and original.is_file() else next((f for f in (source.parent/'textures').glob('*') if f.name.lower() == basename.lower()), None)
    if not found and not image.packed_file:
        missing.append({'image': image.name, 'originalPath': image.filepath})
        textures[image.name] = None
        return None
    key = hashlib.sha256((str(found) if found else image.name).encode()).hexdigest()[:12]
    filename = key + (found.suffix.lower() if found else '.png')
    target = out/'Textures'/filename
    if found:
        shutil.copyfile(found, target)
        image.filepath = str(target)
        image.reload()
    else:
        image.save_render(str(target))
        image.filepath = str(target)
    textures[image.name] = filename
    return filename

for index, material in enumerate(materials):
    original_name = material.name
    material.name = 'FullFloor_M%04d' % index
    color = list(material.diffuse_color)
    roughness = material.roughness
    texture = None
    texture_source = None
    if material.use_nodes:
        bsdf = next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if bsdf:
            color = list(bsdf.inputs['Base Color'].default_value)
            roughness = bsdf.inputs['Roughness'].default_value
            links = bsdf.inputs['Base Color'].links
            # Direct image links are unambiguous. Complex Blender shaders remain documented.
            if links and links[0].from_node.type == 'TEX_IMAGE' and links[0].from_node.image:
                texture_source = links[0].from_node.image.name
                texture = image_path(links[0].from_node.image)
        for node in material.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image:
                image_path(node.image)
    material_records.append({'id':material.name,'sourceName':original_name,'color':color,'roughness':roughness,'baseTexture':texture,'baseTextureSource':texture_source})

records = []
world_matrices = {obj: obj.matrix_world.copy() for obj in objects}
export_collection = bpy.data.collections.new('FullFloorExport')
bpy.context.scene.collection.children.link(export_collection)
for obj in objects:
    original_name = obj.name
    world = world_matrices[obj]
    corners = [world @ Vector(v) for v in obj.bound_box]
    records.append({'source':original_name,'runtime':'Source_'+original_name,
        'collections':[c.name for c in obj.users_collection],
        'min':[min(v[i] for v in corners) for i in range(3)],
        'max':[max(v[i] for v in corners) for i in range(3)],
        'baseTriangles':sum(len(p.vertices)-2 for p in obj.data.polygons),
        'materials':[m.name if m else None for m in obj.data.materials],
        'modifiers':[{'type':m.type,'viewport':m.show_viewport,'render':m.show_render} for m in obj.modifiers]})
    if a.low_poly:
        for modifier in obj.modifiers:
            if modifier.type == 'SUBSURF':
                modifier.show_viewport = False
                modifier.show_render = False
        triangles = records[-1]['baseTriangles']
        architectural = original_name.lower().startswith(('plane', 'floor', 'roof', 'pathway', 'door')) or original_name=='Cube.391'
        if triangles > 10000 and not architectural:
            size = max(records[-1]['max'][i] - records[-1]['min'][i] for i in range(3))
            target = 1500 if size < 2 else 5000
            modifier = obj.modifiers.new('Runtime prop simplification', 'DECIMATE')
            modifier.decimate_type = 'COLLAPSE'
            modifier.ratio = min(1.0, target / triangles)
            modifier.use_collapse_triangulate = True
            records[-1]['runtimeTriangleTarget'] = target
    # FBX cannot represent shared mesh data with different per-object material layouts reliably.
    slots = [slot.material for slot in obj.material_slots]
    if obj.data.users > 1 or any(slot.link == 'OBJECT' for slot in obj.material_slots):
        obj.data = obj.data.copy()
        material_indices = [polygon.material_index for polygon in obj.data.polygons]
        obj.data.materials.clear()
        for material in slots:
            obj.data.materials.append(material)
        for slot in obj.material_slots:
            slot.link = 'DATA'
        for polygon, material_index in zip(obj.data.polygons, material_indices):
            polygon.material_index = material_index
    export_collection.objects.link(obj)
    obj.parent = None
    obj.hide_select = False
    obj.matrix_world = Matrix.Translation(-origin) @ world
    obj.name = 'Source_' + original_name
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.hide_render = False
# Ensure collection exclusion cannot silently remove a source room or prop.
for collection in bpy.data.collections:
    collection.hide_viewport = False
    collection.hide_render = False

def enable(layer):
    layer.exclude = False
    layer.hide_viewport = False
    for child in layer.children:
        enable(child)
enable(bpy.context.view_layer.layer_collection)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects[0]
print('FULL_FLOOR_EXPORT_START',len(objects),'meshes',flush=True)
bpy.ops.export_scene.fbx(filepath=str(out/'Models'/'Floor3Full.fbx'),use_selection=True,
    object_types={'MESH'},axis_forward='-Z',axis_up='Y',global_scale=1,
    apply_unit_scale=True,bake_space_transform=True,use_mesh_modifiers=True,
    mesh_smooth_type='FACE',use_triangles=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
report={'source':source.name,'sourceOrigin':list(origin),'sourceUnits':'metres',
    'lowPoly':a.low_poly,'entranceAdjustments':entrance_adjustments,
    'meshObjectCount':len(objects),'baseTriangles':sum(r['baseTriangles'] for r in records),
    'objects':records,'materials':material_records,'missingImages':missing,
    'scope':'Entire source floor; no clipping, removal of props or corridor blockers.',
    'unityMapping':'X=sourceX-38.98, Y=sourceZ, Z=sourceY+1; verify FBX orientation in Editor.'}
(out/'Data'/'full-floor-manifest.json').write_text(json.dumps(report,indent=2))
print('FULL_FLOOR_EXPORT_COMPLETE',len(objects), 'meshes',report['baseTriangles'],'base triangles',len(missing),'missing images',flush=True)
