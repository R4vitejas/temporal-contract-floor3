"""Export repaired seminar bench visuals without saving the supplied .blend.

Run Blender in background with the original floor, this script, and -- --output FILE.
Import FILE using SeminarGeometryRefinement.Import in the open inspection scene.
The accepted collision bake is intentionally independent of this visual repair.
"""
import argparse
import json
import sys
from math import radians
from pathlib import Path

import bmesh
import bpy

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
obj = bpy.data.objects['Cube.384']
mesh = obj.data.copy()
mesh.transform(obj.matrix_world)
bm = bmesh.new()
bm.from_mesh(mesh)
# Face-split source vertices defeat collapse decimation on these thin benches.
# Weld only coincident positions, then dissolve coplanar edges, preserving materials.
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.0001)
bmesh.ops.dissolve_limit(bm, angle_limit=radians(.1), use_dissolve_boundaries=False,
                         verts=list(bm.verts), edges=list(bm.edges), delimit={'MATERIAL'})
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bmesh.ops.triangulate(bm, faces=list(bm.faces))
bm.to_mesh(mesh)
bm.free()
vertices, uvs = [], []
submeshes = [{'triangles': []} for _ in mesh.materials]
uv_layer = mesh.uv_layers.active
for face in mesh.polygons:
    # Blender Z-up to Unity Y-up swaps handedness; reverse the winding.
    for loop_index in reversed(face.loop_indices):
        v = mesh.vertices[mesh.loops[loop_index].vertex_index].co
        uv = uv_layer.data[loop_index].uv if uv_layer else (0, 0)
        submeshes[face.material_index]['triangles'].append(len(vertices))
        vertices.append({'x': v.x - 38.98, 'y': v.z, 'z': v.y + 1})
        uvs.append({'x': uv[0], 'y': uv[1]})
payload = {'source': obj.name, 'vertices': vertices, 'uvs': uvs, 'submeshes': submeshes}
target = Path(args.output)
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text(json.dumps(payload))
print('SEMINAR_BENCH_EXPORT', len(mesh.polygons), 'triangles; source file unchanged', flush=True)
