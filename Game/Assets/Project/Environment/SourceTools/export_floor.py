"""Run with Blender --background <original.blend> --python this_file -- --output <Environment>.
The source file is read only. Runtime meshes use simplified, named material slots.
"""
import argparse
import bmesh
import bpy
import json
import os
import sys
from mathutils import Matrix, Vector

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
out = os.path.abspath(args.output)
os.makedirs(os.path.join(out, 'Models'), exist_ok=True)
os.makedirs(os.path.join(out, 'Data'), exist_ok=True)
rooms = ['ClassRoom02', 'ClassRoom03', 'ClassRoom05', 'ClassRoom06']
origin = Vector((38.98, -1.0, 0.0))
source_file = os.path.basename(bpy.data.filepath)
original_objects = list(bpy.data.objects)
exported = []
provenance = []
# Named flat materials intentionally replace unavailable machine-specific source textures.
colors = {'Wall': (.73,.70,.62,1), 'Floor': (.35,.39,.40,1), 'Wood': (.29,.14,.065,1),
          'Board': (.035,.12,.105,1), 'Metal': (.16,.20,.23,1), 'Glass': (.30,.48,.57,1),
          'Ceiling': (.77,.79,.78,1), 'Accent': (.23,.29,.36,1)}
materials = {}
for name, color in colors.items():
    m = bpy.data.materials.new('Floor3_' + name)
    m.diffuse_color = color
    m.use_nodes = True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .8
    materials[name] = m

def material_kind(name):
    n = name.lower()
    if 'wall' in n: return 'Wall'
    if 'tile' in n or 'granite' in n or 'marble' in n: return 'Floor'
    if 'chalk' in n or 'board' in n or 'screen' in n: return 'Board'
    if 'roof' in n or 'light' in n: return 'Ceiling'
    if 'glass' in n: return 'Glass'
    if 'wood' in n or 'door' in n: return 'Wood'
    if 'black' in n or 'metal' in n or 'cage' in n or 'frame' in n or 'handle' in n: return 'Metal'
    return 'Accent'

def copy_mesh(source, name=None, local_origin=origin):
    # Evaluate modifiers, including procedural door frames, before exporting.
    evaluated = source.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = source.data.copy() if source.name.startswith('Handle_') else bpy.data.meshes.new_from_object(evaluated)
    if not mesh.polygons:
        bpy.data.meshes.remove(mesh)
        return None
    transform = Matrix.Translation(-local_origin) @ source.matrix_world
    mesh.transform(transform)
    obj = bpy.data.objects.new(name or ('Source_' + source.name), mesh)
    bpy.context.scene.collection.objects.link(obj)
    original_materials = list(source.data.materials)
    kinds = [material_kind(m.name) if m else 'Accent' for m in original_materials] or ['Accent']
    mesh.materials.clear()
    for kind in kinds: mesh.materials.append(materials[kind])
    for p in mesh.polygons: p.material_index = min(p.material_index, len(kinds)-1)
    mesh.update()
    provenance.append({'source': source.name, 'runtime': obj.name, 'triangles': sum(len(p.vertices)-2 for p in mesh.polygons)})
    return obj

# Keep four source rooms and their own low-poly classroom benches/platforms.
for source in original_objects:
    if source.type != 'MESH': continue
    groups = [c.name for c in source.users_collection]
    keep = any(c in rooms for c in groups)
    if any(c in ['Desks.002','Desks.003','Desks.005','Desks.006'] for c in groups): keep = True
    if not keep: continue
    # Movable doors and handles are exported separately with hinge pivots.
    if source.name.startswith(('Door.', 'Handle_')): continue
    obj = copy_mesh(source)
    if obj: exported.append(obj)

pathway = bpy.data.objects.get('Pathway')
obj = copy_mesh(pathway, 'SourceCorridor')
if obj:
    bm = bmesh.new(); bm.from_mesh(obj.data)
    for point, normal in [((34.48-origin.x,0,0),(1,0,0)), ((52.1-origin.x,0,0),(-1,0,0)),
                          ((0,-14.72-origin.y,0),(0,1,0)), ((0,.2-origin.y,0),(0,-1,0))]:
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces), dist=.00001,
                              plane_co=point, plane_no=normal, clear_inner=True, clear_outer=False)
    bm.to_mesh(obj.data);bm.free();exported.append(obj)

def export_fbx(name, objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects: o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=os.path.join(out,'Models',name+'.fbx'), use_selection=True,
        object_types={'MESH'}, axis_forward='-Z', axis_up='Y', global_scale=1.0,
        apply_unit_scale=True, bake_space_transform=True, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False, path_mode='STRIP')

export_fbx('Floor3Section', exported)
# All interactive door slabs keep their actual source dimensions, expressed from left hinge/base.
doors = []
for source_name, room in [('Door.007','ClassRoom02'),('Door.008','ClassRoom03'),('Door.009','ClassRoom06'),('Door.010','ClassRoom05')]:
    source = bpy.data.objects[source_name]
    corners = [source.matrix_world @ Vector(p) for p in source.bound_box]
    lo=Vector([min(p[i] for p in corners) for i in range(3)])
    hi=Vector([max(p[i] for p in corners) for i in range(3)])
    pivot=Vector((lo.x,(lo.y+hi.y)*.5,lo.z))
    slab=copy_mesh(source, 'SourceDoorSlab', pivot)
    handle_suffix=source_name.split('.')[1]
    pieces=[slab]
    for prefix in ['Handle_Front.','Handle_Back.']:
        s=bpy.data.objects.get(prefix+handle_suffix)
        if s:
            p=copy_mesh(s, 'SourceHandle', pivot)
            if p: pieces.append(p)
    export_fbx(room+'_Door',pieces)
    doors.append({'room':room,'source':source_name,'sourceHinge':list(pivot),'sourceSize':list(hi-lo)})
# A genuine low-poly chair from the supplied Staff Room; retain base pivot.
chair_source=bpy.data.objects['Cube.386']
corners=[chair_source.matrix_world @ Vector(p) for p in chair_source.bound_box]
lo=Vector([min(p[i] for p in corners) for i in range(3)]);hi=Vector([max(p[i] for p in corners) for i in range(3)])
chair=copy_mesh(chair_source,'SourceChair',Vector(((lo.x+hi.x)*.5,(lo.y+hi.y)*.5,lo.z)))
export_fbx('Floor3Chair',[chair])
report={'source':source_file,'sourceUnits':'metres','sourceOrigin':list(origin),
        'roomCollections':rooms,'doors':doors,'objects':provenance,
        'sourceChair':'Cube.386','sourceTriangleCount':10958379,
        'notes':['Original files remain unchanged.','Lab OBJ: 576510843 bytes, 3016929 vertices, 4766537 faces; excluded.',
                 'Missing source image paths replaced by eight named material colors.',
                 'Pathway clipped to selected corridor; room shell, board, windows and benches retained.']}
with open(os.path.join(out,'Data','floor-source-manifest.json'),'w') as f: json.dump(report,f,indent=2)
print('FLOOR_EXPORT_COMPLETE',sum(p['triangles'] for p in provenance),'triangles',out)
