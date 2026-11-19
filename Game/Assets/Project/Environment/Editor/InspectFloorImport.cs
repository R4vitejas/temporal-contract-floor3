using UnityEditor;
using UnityEngine;
namespace TemporalContract.Environment.EditorTools
{
    public static class InspectFloorImport
    {
        public static void Run()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Environment/Models/Floor3Section.fbx");
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Source_Plane.024" || r.name == "Source_Plane.036" || r.name == "SourceCorridor")
                    Debug.Log($"FLOOR_MODEL {r.name} center={r.bounds.center:F4} size={r.bounds.size:F4} rootScale={root.transform.localScale} rootRotation={root.transform.eulerAngles}");
            var door = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Environment/Models/ClassRoom02_Door.fbx");
            foreach (var r in door.GetComponentsInChildren<Renderer>(true)) Debug.Log($"FLOOR_DOOR {r.name} center={r.bounds.center:F4} size={r.bounds.size:F4}");
        }
    }
}
