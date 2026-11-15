using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TemporalContract.Environment.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Builds only scene-owned environment assets. Subsystem code is never regenerated here.</summary>
    public static class FloorAssetBuilder
    {
        public const string Root = "Assets/Project/Environment";
        private const float OriginX = 38.98f;
        private const float OriginY = -1f;
        private static readonly string[] RoomIds = { "ClassRoom02", "ClassRoom03", "ClassRoom06", "ClassRoom05" };
        private static readonly float[] DoorX = {35.2808f,51.2307f,35.3970f,51.2121f};
        private static readonly float[] DoorY = {-14.8251f,-14.8247f,.1991f,.1756f};
        // Model orientation is checked against two known source room shells before scene generation.
        private static Quaternion modelCorrection = Quaternion.identity;
        public static Vector3 Source(float x, float y, float z = 0f) => new Vector3(x-OriginX,z,y-OriginY);

        [MenuItem("Temporal Contract/Floor/Build Environment Assets")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Root+"/Materials");
            Directory.CreateDirectory(Root+"/Prefabs");
            Directory.CreateDirectory(Root+"/Scenes");
            AssetDatabase.Refresh();
            ConfigureModels();
            DetectOrientation();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureLighting();
            var floor = CreateFloor();
            var prefab = PrefabUtility.SaveAsPrefabAsset(floor,Root+"/Prefabs/Floor3Environment.prefab");
            UnityEngine.Object.DestroyImmediate(floor);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            OpenDoors(instance.transform);
            AddWalkthrough();
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/EnvironmentTest.unity");
            AssetDatabase.SaveAssets();
            Verify();
            Debug.Log("FLOOR_BUILD_PASS: environment prefab and isolated walkthrough scene generated.");
        }

        private static void ConfigureModels()
        {
            var palette = new Dictionary<string, Color> {
                {"Wall",new Color(.73f,.70f,.62f)}, {"Floor",new Color(.35f,.39f,.40f)},
                {"Wood",new Color(.29f,.14f,.065f)}, {"Board",new Color(.035f,.12f,.105f)},
                {"Metal",new Color(.16f,.20f,.23f)}, {"Glass",new Color(.30f,.48f,.57f)},
                {"Ceiling",new Color(.77f,.79f,.78f)}, {"Accent",new Color(.23f,.29f,.36f)} };
            foreach(var pair in palette)
            {
                string path=Root+"/Materials/Floor3_"+pair.Key+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null)
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material,path);
                }
                material.SetColor("_BaseColor",pair.Value);
                material.SetFloat("_Smoothness",.15f);
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            foreach(string path in Directory.GetFiles(Root+"/Models","*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importCameras=false; importer.importLights=false; importer.importAnimation=false;
                importer.addCollider=false; importer.isReadable=false;
                foreach(var material in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                {
                    string key=palette.Keys.FirstOrDefault(k=>material.name.StartsWith("Floor3_"+k,StringComparison.Ordinal));
                    if(key!=null) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),
                        AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Floor3_"+key+".mat"));
                }
                importer.SaveAndReimport();
            }
        }

        private static void DetectOrientation()
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/Floor3Section.fbx");
            var a=asset.GetComponentsInChildren<Renderer>().First(r=>r.name=="Source_Plane.024");
            var b=asset.GetComponentsInChildren<Renderer>().First(r=>r.name=="Source_Plane.036");
            modelCorrection=b.bounds.center.z>a.bounds.center.z ? Quaternion.identity : Quaternion.Euler(0f,180f,0f);
            Debug.Log("FLOOR_ORIENTATION: correction="+modelCorrection.eulerAngles);
        }

        public static GameObject CreateFloor()
        {
            DetectOrientation();
            var root=new GameObject("Floor 3 - Supplied Classroom Section");
            var anchors=root.AddComponent<FloorIntegrationAnchors>();
            var geometry=PlaceModel("Floor3Section",root.transform,Vector3.zero);
            geometry.name="Supplied Architecture";
            // Clear one source bench from the documented VIP anchor, retaining the other classroom furniture.
            var vipBench=geometry.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Source_Shorter.029");
            if(vipBench!=null)vipBench.gameObject.SetActive(false);
            foreach(var child in geometry.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(child.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
            var collision=new GameObject("Simple Static Colliders").transform;collision.SetParent(root.transform,false);
            Box(collision,"Corridor floor",Source(43.29f,-7.25f,-.065f),new Vector3(17.62f,.1f,14.94f));
            anchors.rooms=new FloorIntegrationAnchors.Room[4];
            for(int i=0;i<4;i++)
            {
                bool west=i==0||i==2; bool north=i>=2;
                float x0=west?34.55f:43.23f,x1=west?43.23f:52.03f;
                float y0=north?.2f:-24.24f,y1=north?9.64f:-14.82f;
                float centerX=(x0+x1)*.5f, centerY=(y0+y1)*.5f;
                Box(collision,RoomIds[i]+" floor",Source(centerX,centerY,-.055f),new Vector3(x1-x0,.1f,y1-y0));
                Box(collision,RoomIds[i]+" outer wall",Source(centerX,north?y1:y0,1.5f),new Vector3(x1-x0,3f,.12f));
                Box(collision,RoomIds[i]+" west wall",Source(x0,centerY,1.5f),new Vector3(.12f,3f,y1-y0));
                Box(collision,RoomIds[i]+" east wall",Source(x1,centerY,1.5f),new Vector3(.12f,3f,y1-y0));
                float doorEdge=north?y0:y1, left=DoorX[i]-.52f, right=DoorX[i]+.52f;
                Box(collision,RoomIds[i]+" door wall left",Source((x0+left)/2,doorEdge,1.5f),new Vector3(left-x0,3f,.12f));
                Box(collision,RoomIds[i]+" door wall right",Source((right+x1)/2,doorEdge,1.5f),new Vector3(x1-right,3f,.12f));
                Box(collision,RoomIds[i]+" lintel",Source(DoorX[i],doorEdge,2.55f),new Vector3(1.04f,.9f,.12f));
                Box(collision,RoomIds[i]+" ceiling",Source(centerX,centerY,3.1f),new Vector3(x1-x0,.1f,y1-y0));
                var hinge=new GameObject("Door_"+RoomIds[i]).transform;hinge.SetParent(root.transform,false);
                hinge.position=Source(DoorX[i]-.46707f,DoorY[i],.004f);
                var visual=PlaceModel(RoomIds[i]+"_Door",hinge,Vector3.zero);
                Box(hinge,"Door slab collider",new Vector3(.46707f,1.005f,0f),new Vector3(.93414f,2.01f,.04f));
                var lightObject=new GameObject("Light_"+RoomIds[i]);lightObject.transform.SetParent(root.transform,false);
                lightObject.transform.position=Source(centerX,centerY,2.6f);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=12f;light.intensity=2.3f;light.shadows=LightShadows.None;
                string boardName= i==0?"Source_Plane.043": i==1?"Source_Plane.044": i==2?"Source_Plane.046":"Source_Plane.045";
                var board=geometry.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name==boardName);
                anchors.rooms[i]=new FloorIntegrationAnchors.Room{sourceRoom=RoomIds[i],label=i==2?"Classroom 06 - VIP":RoomIds[i].Replace("ClassRoom","Classroom "),doorHinge=hinge,board=board,roomLight=light,restricted=i==2};
                Sign(root.transform,anchors.rooms[i].label,Source(DoorX[i],DoorY[i]+(north?-.13f:.13f),2.4f),north?0f:180f,.12f);
            }
            // Bench/platform bounds are simple boxes; windows are blocked by room perimeter boxes.
            foreach(var renderer in geometry.GetComponentsInChildren<Renderer>())
            {
                if(renderer.name.StartsWith("Source_Longer")||renderer.name.StartsWith("Source_Shorter")||renderer.name.StartsWith("Source_Cube."))
                    Box(collision,"Furniture_"+renderer.name,renderer.bounds.center,renderer.bounds.size);
            }
            Barrier(root.transform,"West route closed",Source(34.48f,-7.25f,1.3f),new Vector3(.18f,2.6f,14.94f));
            Barrier(root.transform,"East route closed",Source(52.1f,-7.25f,1.3f),new Vector3(.18f,2.6f,14.94f));
            anchors.attackerSpawn=Marker(root.transform,"AttackerSpawn",new Vector3(-2.5f,.05f,0f));
            anchors.defenderSpawn=Marker(root.transform,"DefenderSpawn",new Vector3(2.5f,.05f,0f));anchors.defenderSpawn.rotation=Quaternion.Euler(0,180,0);
            anchors.vip=Marker(root.transform,"VIPAnchor",new Vector3(0f,0f,4f));
            anchors.keycard=Marker(root.transform,"KeycardAnchor",Source(45f,-8f,.85f));
            anchors.accessPanel=Marker(root.transform,"AccessPanelAnchor",Source(36.5f,-.02f));
            anchors.lightSwitch=Marker(root.transform,"SwitchAnchor",Source(37f,-.02f));
            anchors.movableChair=Marker(root.transform,"CoverAnchor",Source(46f,-5f));
            var chairVisual=PlaceModel("Floor3Chair",anchors.movableChair,Vector3.zero);
            var chairBounds=chairVisual.GetComponentInChildren<Renderer>().bounds;
            Box(collision,"Supplied chair collider",chairBounds.center,chairBounds.size);
            anchors.boardMap=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Data/Floor3Map.png");
            return root;
        }

        public static GameObject PlaceModel(string name,Transform parent,Vector3 position)
        {
            DetectOrientation();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+".fbx");
            if(asset==null)throw new InvalidOperationException("Missing exported model: "+name);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset);instance.transform.SetParent(parent,false);
            instance.transform.localPosition=position;instance.transform.localRotation=modelCorrection;return instance;
        }
        public static Transform Marker(Transform parent,string name,Vector3 position)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
        }
        private static void Box(Transform parent,string name,Vector3 position,Vector3 size)
        {
            if(size.x<=0||size.y<=0||size.z<=0)throw new InvalidOperationException("Invalid collision box: "+name);
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var box=go.AddComponent<BoxCollider>();box.size=size;go.isStatic=true;
        }
        private static void Barrier(Transform parent,string name,Vector3 position,Vector3 size)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Floor3_Wood.mat");
            bool west=name.StartsWith("West",StringComparison.Ordinal);
            Sign(parent,name.ToUpperInvariant(),position+Vector3.up*.4f+(west?Vector3.right:Vector3.left)*(size.x*.5f+.02f),west?-90f:90f,.13f);
        }
        private static void Sign(Transform parent,string text,Vector3 position,float yaw,float scale)
        {
            var go=new GameObject("Sign_"+text);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.transform.localRotation=Quaternion.Euler(0f,yaw,0f);
            var mesh=go.AddComponent<TextMesh>();mesh.text=text;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;
            mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;
            mesh.fontSize=64;mesh.characterSize=scale;mesh.color=new Color(.1f,.15f,.17f);
        }
        public static void ConfigureLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.53f,.57f);
            var sun=new GameObject("Environment Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1f;
            sun.transform.rotation=Quaternion.Euler(45f,-25f,0f);sun.shadows=LightShadows.None;
        }
        private static void OpenDoors(Transform root)
        {
            foreach(var room in root.GetComponent<FloorIntegrationAnchors>().rooms)room.doorHinge.localRotation=Quaternion.Euler(0f,95f,0f);
        }
        private static void AddWalkthrough()
        {
            var go=new GameObject("ENVIRONMENT TEST ONLY - Walkthrough");go.transform.position=new Vector3(-2.5f,.05f,0f);
            var body=go.AddComponent<CharacterController>();body.height=1.8f;body.radius=.3f;body.center=new Vector3(0f,.9f,0f);body.stepOffset=.25f;
            var walkthrough=go.AddComponent<EnvironmentWalkthrough>();
            var camera=new GameObject("Environment Test Camera");camera.transform.SetParent(go.transform,false);camera.transform.localPosition=new Vector3(0f,1.62f,0f);
            walkthrough.view=camera.AddComponent<Camera>();walkthrough.view.nearClipPlane=.05f;camera.AddComponent<AudioListener>();
        }
        [MenuItem("Temporal Contract/Floor/Verify Environment")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene(Root+"/Scenes/EnvironmentTest.unity");
            Physics.SyncTransforms();
            var anchors=UnityEngine.Object.FindFirstObjectByType<FloorIntegrationAnchors>();
            if(anchors==null||anchors.rooms.Length!=4)throw new InvalidOperationException("Missing floor room anchors.");
            if(anchors.boardMap==null)throw new InvalidOperationException("Board map is missing.");
            if(UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Any(t=>t.font==null))throw new InvalidOperationException("A room/route sign has no font.");
            foreach(var room in anchors.rooms)
            {
                if(room.board==null||room.roomLight==null)throw new InvalidOperationException("Missing board/light: "+room.sourceRoom);
                Vector3 door=room.doorHinge.position+new Vector3(.46707f,0f,0f);
                if(!Physics.Raycast(door+Vector3.up,Vector3.down,out var hit,2f)||hit.point.y<-.1f)
                    throw new InvalidOperationException("Door threshold has no supporting floor: "+room.sourceRoom);
                // An adult-sized capsule must fit through each source doorway with the test door open.
                Vector3 feet=door+Vector3.up*.03f;
                var overlaps=Physics.OverlapCapsule(feet+Vector3.up*.32f,feet+Vector3.up*1.48f,.29f);
                if(overlaps.Any(c=>!(c is CharacterController)))
                    throw new InvalidOperationException("Doorway capsule blocked: "+room.sourceRoom+" by "+string.Join(",",overlaps.Select(c=>c.name)));
            }
            foreach(var spawn in new[]{anchors.attackerSpawn,anchors.defenderSpawn,anchors.vip})
            {
                if(!Physics.Raycast(spawn.position+Vector3.up,Vector3.down,2f))throw new InvalidOperationException("Unsupported spawn: "+spawn.name);
                var occupied=Physics.OverlapCapsule(spawn.position+Vector3.up*.35f,spawn.position+Vector3.up*1.45f,.3f);
                if(occupied.Any(c=>!(c is CharacterController)))throw new InvalidOperationException("Occupied spawn: "+spawn.name);
            }
            int missing=0;foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if(missing!=0)throw new InvalidOperationException("Missing scripts in floor scene.");
            long triangles=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.sharedMesh!=null).Sum(m=>Enumerable.Range(0,m.sharedMesh.subMeshCount).Sum(i=>(long)m.sharedMesh.GetIndexCount(i)/3));
            Debug.Log($"FLOOR_VERIFY_PASS rooms=4 clearDoorways=4 supportedAnchors=3 missingScripts={missing} triangles={triangles} colliders={UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length}");
        }
    }
}
