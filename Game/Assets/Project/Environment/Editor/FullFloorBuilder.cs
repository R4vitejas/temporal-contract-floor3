using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TemporalContract.Environment.Testing;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Imports the complete source layout into an isolated inspection scene.</summary>
    public static class FullFloorBuilder
    {
        const string Root = "Assets/Project/Environment";
        const string Model = Root + "/Models/Floor3Full.fbx";
        [Serializable] public class SourceObject { public string source, runtime; public string[] collections; public float[] min, max; public int baseTriangles; }
        [Serializable] public class SourceMaterial { public string id, sourceName, baseTexture, baseTextureSource; public float[] color; public float roughness; }
        [Serializable] public class Manifest { public int meshObjectCount, baseTriangles; public SourceObject[] objects; public SourceMaterial[] materials; }

        [MenuItem("Temporal Contract/Floor/Build Full Floor Inspection")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before generating the full floor inspection.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/Data/full-floor-manifest.json"));
            Directory.CreateDirectory(Root + "/Materials/FullFloor");
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(Model);
            if (importer == null) throw new InvalidOperationException("Import Floor3Full.fbx first.");
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.addCollider = false; importer.isReadable = false;
            // All source materials map into the agreed retro palette; geometry remains separate.
            foreach (var group in manifest.materials.GroupBy(RetroFloorStyle.Key))
            {
                var mat=RetroFloorStyle.Material(group.Key);
                foreach(var original in group)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),original.id),mat);
            }
            AssetDatabase.SaveAssets();
            importer.SaveAndReimport();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.57f,.60f,.64f);
            RenderSettings.fog = false;
            var sun = new GameObject("Inspection daylight").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f,.91f,.76f); sun.intensity = .8f; sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(55f,-35f,0f);
            var root = new GameObject("Floor 3 - Entire supplied floor");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
            model.transform.SetParent(root.transform, false);
            var renderers = model.GetComponentsInChildren<MeshRenderer>(true);
            var a = renderers.First(r => r.name == "Source_Plane.024");
            var b = renderers.First(r => r.name == "Source_Plane.036");
            if (b.bounds.center.z < a.bounds.center.z) model.transform.rotation = Quaternion.Euler(0f,180f,0f);
            var lookup = manifest.objects.ToDictionary(o => o.runtime);
            int meshColliders = 0;
            var wallMaterial = RetroFloorStyle.Material("Wall");
            var floorMaterial = RetroFloorStyle.Material("Floor");
            var furnitureCollision = new GameObject("Furniture collision").transform;
            furnitureCollision.SetParent(root.transform,false);
            foreach (var renderer in renderers)
            {
                SourceObject source;
                if (!lookup.TryGetValue(renderer.name, out source)) throw new InvalidOperationException("Unknown imported mesh " + renderer.name);
                string n = source.source.ToLowerInvariant();
                if (n == "pathway" || n.StartsWith("floor"))
                    renderer.sharedMaterials = Enumerable.Repeat(floorMaterial, renderer.sharedMaterials.Length).ToArray();
                else if(renderer.sharedMaterials.Any(m => m == null))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m ? m : wallMaterial).ToArray();
                // Architecture keeps its exact openings. High-detail fixtures have no collision in this first inspection.
                if ((n.StartsWith("plane") || n.StartsWith("floor") || n == "pathway" || n.StartsWith("roof")) && source.baseTriangles < 20000)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter && filter.sharedMesh) { renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh; meshColliders++; }
                }
                var bounds=renderer.bounds;
                bool furniture = !n.StartsWith("plane") && !n.StartsWith("floor") && !n.StartsWith("roof") && !n.StartsWith("door") &&
                    !n.StartsWith("handle") && !n.Contains("socket") && !n.Contains("window") && n!="pathway" &&
                    bounds.min.y>=-.15f && bounds.min.y<=.15f && bounds.max.y>.3f && bounds.max.y<2.5f &&
                    bounds.size.x>.15f && bounds.size.z>.15f && Mathf.Max(bounds.size.x,bounds.size.z)<8f;
                bool window=renderer.sharedMaterials.Any(m=>m && m.name=="Retro_Glass") && bounds.size.y>.2f;
                if(furniture || window)
                {
                    var obstacle=new GameObject("Collision - "+source.source);
                    obstacle.transform.SetParent(furnitureCollision,false);obstacle.transform.position=bounds.center;
                    var box=obstacle.AddComponent<BoxCollider>();box.size=new Vector3(Mathf.Max(.03f,bounds.size.x),Mathf.Max(.03f,bounds.size.y),Mathf.Max(.03f,bounds.size.z));
                }
            }
            var player = new GameObject("Full floor inspection player");
            player.transform.position = new Vector3(-2.5f,.1f,-8f);
            var body = player.AddComponent<CharacterController>();
            body.height = 1.8f; body.radius = .3f; body.center = new Vector3(0f,.9f,0f);
            // Default 0.3 m step-up lifts the capsule into the original 2.01 m door lintels.
            body.stepOffset = .05f;
            body.skinWidth = .03f;
            var camera = new GameObject("Inspection Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = .05f; camera.farClipPlane = 150f;
            camera.transform.SetParent(player.transform,false); camera.transform.localPosition = new Vector3(0f,1.65f,0f);
            camera.gameObject.AddComponent<AudioListener>();
            var walk = player.AddComponent<EnvironmentWalkthrough>(); walk.view = camera;
            walk.homePosition = player.transform.position;
            var probes=manifest.objects.Where(o=>o.source.StartsWith("Door.",StringComparison.Ordinal)&&o.collections.Any(c=>c.StartsWith("ClassRoom",StringComparison.Ordinal))).ToArray();
            walk.doorwayProbes=probes.Select(d=>new Vector3((d.min[0]+d.max[0])*.5f-38.98f,.08f,(d.min[1]+d.max[1])*.5f+1f)).ToArray();
            walk.doorwayDirections=probes.Select(d=>d.max[0]-d.min[0]>d.max[1]-d.min[1]?Vector3.forward:Vector3.right).ToArray();
            walk.instructions = "FULL FLOOR INSPECTION · WASD / mouse · Esc cursor · Home respawn\nRetro floor preview · doors held open · multiplayer interactions are tested separately.";
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Floor3FullEnvironment.prefab");
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            OpenInspectionDoors(renderers);
            EditorSceneManager.SaveScene(scene,Root + "/Scenes/FullFloorInspection.unity");
            var sourceNames = new HashSet<string>(manifest.objects.Select(o => o.runtime));
            var importedNames = new HashSet<string>(renderers.Select(r => r.name));
            if (!sourceNames.SetEquals(importedNames)) throw new InvalidOperationException("Full-floor mesh preservation check failed.");
            long triangles = model.GetComponentsInChildren<MeshFilter>().Sum(f => Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(i => (long)f.sharedMesh.GetIndexCount(i) / 3));
            Debug.Log($"FULL_FLOOR_INSPECTION_PASS: {renderers.Length}/{manifest.meshObjectCount} source meshes retained, {triangles} imported triangles, {meshColliders} architecture colliders. No cropping or corridor blockers. Gameplay/prop collision/performance pending.");
            AssetDatabase.SaveAssets();

        }
        [MenuItem("Temporal Contract/Floor/Prepare Full Floor Main")]
        public static void PrepareMain()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Stop Play Mode and save the current scene first.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Floor3FullEnvironment.prefab");
            if(!prefab)throw new InvalidOperationException("Build the full floor first.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(prefab);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.57f,.60f,.64f);
            var sun=new GameObject("Floor daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.8f;sun.color=new Color(1f,.91f,.76f);sun.shadows=LightShadows.None;sun.transform.rotation=Quaternion.Euler(55f,-35f,0f);
            var markers=new GameObject("Proposed gameplay locations - production binding pending").transform;
            FloorAssetBuilder.Marker(markers,"Attacker spawn proposal",new Vector3(11.02f,.1f,-7f));
            FloorAssetBuilder.Marker(markers,"Defender spawn proposal",new Vector3(-44.98f,.1f,-15f));
            FloorAssetBuilder.Marker(markers,"VIP staff cabin proposal",new Vector3(-49.98f,.1f,-14.5f));
            FloorAssetBuilder.Marker(markers,"Keycard proposal",new Vector3(-18.98f,.8f,-9f));
            const string path="Assets/Project/Scenes/Main.unity";
            EditorSceneManager.SaveScene(scene,path);
            var settings=EditorBuildSettings.scenes.Where(s=>s.path!=path).ToList();settings.Add(new EditorBuildSettingsScene(path,false));EditorBuildSettings.scenes=settings.ToArray();
            Debug.Log("FULL_FLOOR_MAIN_PREPARED: complete geometry, retro materials, collision and proposed locations. Production systems not assembled; Main remains disabled in builds.");
        }

        private static void OpenInspectionDoors(MeshRenderer[] renderers)
        {
            foreach(var door in renderers.Where(r => r.name.StartsWith("Source_Door.",StringComparison.Ordinal)))
            {
                var bounds=door.bounds;
                bool acrossX=bounds.size.x>bounds.size.z;
                var pivot=new GameObject("Inspection hinge - "+door.name).transform;
                pivot.SetParent(door.transform.parent,true);
                pivot.position=new Vector3(acrossX?bounds.min.x:bounds.center.x,bounds.min.y,acrossX?bounds.center.z:bounds.min.z);
                string suffix=door.name.Substring("Source_Door".Length);
                foreach(var piece in renderers.Where(r => r==door || r.name=="Source_Handle_Front"+suffix || r.name=="Source_Handle_Back"+suffix))
                    piece.transform.SetParent(pivot,true);
                pivot.Rotate(Vector3.up,90f,Space.World);
            }
        }

    }
}
