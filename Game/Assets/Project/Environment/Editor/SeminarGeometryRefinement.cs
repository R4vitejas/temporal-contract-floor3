using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Repairs the seminar bench visual without changing the accepted collision bake.</summary>
    public static class SeminarGeometryRefinement
    {
        public const string MeshPath = "Assets/Project/Environment/Models/SeminarBenches.asset";
        [Serializable] public class Submesh { public int[] triangles; }
        [Serializable] public class Export { public string source; public Vector3[] vertices; public Vector2[] uvs; public Submesh[] submeshes; }

        public static void Import(string jsonPath, GameObject floor)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var data = JsonUtility.FromJson<Export>(File.ReadAllText(jsonPath));
            if (data.source != "Cube.384") throw new InvalidOperationException("Unexpected seminar source.");
            var target = floor.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "Source_Cube.384");
            var mesh = new Mesh { name = "Seminar benches - welded planar source", indexFormat = IndexFormat.UInt32 };
            // Keep hard furniture edges. The source positions are exported in floor world coordinates.
            var vertices = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var indices = new int[data.submeshes.Length][];
            for (int s = 0; s < indices.Length; s++)
            {
                indices[s] = new int[data.submeshes[s].triangles.Length];
                for (int i = 0; i < indices[s].Length; i++)
                {
                    indices[s][i] = vertices.Count;
                    int sourceIndex = data.submeshes[s].triangles[i];
                    vertices.Add(target.transform.InverseTransformPoint(data.vertices[sourceIndex]));
                    uvs.Add(data.uvs != null ? data.uvs[sourceIndex] : Vector2.zero);
                }
            }
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.subMeshCount = indices.Length;
            for (int s = 0; s < indices.Length; s++) mesh.SetTriangles(indices[s], s);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var oldBounds = target.GetComponent<MeshRenderer>().bounds;
            var newBounds = new Bounds(target.transform.TransformPoint(vertices[0]), Vector3.zero);
            foreach (var v in vertices) newBounds.Encapsulate(target.transform.TransformPoint(v));
            if (Vector3.Distance(oldBounds.min, newBounds.min) > .02f || Vector3.Distance(oldBounds.max, newBounds.max) > .02f)
                throw new InvalidOperationException("Refined bench bounds differ from the supplied layout.");
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); }
            else AssetDatabase.CreateAsset(mesh, MeshPath);
            AssetDatabase.SaveAssets(); Apply(floor);
        }

        public static void Apply(GameObject floor)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (!mesh) return;
            var target = floor.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "Source_Cube.384");
            target.sharedMesh = mesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }
    }
}
