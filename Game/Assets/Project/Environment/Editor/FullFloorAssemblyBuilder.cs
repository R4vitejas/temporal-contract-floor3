using System;
using System.IO;
using System.Linq;
using TemporalContract.Environment.Testing;
using TemporalContract.Interactions;
using TemporalContract.Networking;
using TemporalContract.UI.Board;
using TemporalContract.UI.Hud;
using TemporalContract.Core;
using TemporalContract.Feedback;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Places reviewed systems on the full floor without claiming production acceptance.</summary>
    public static class FullFloorAssemblyBuilder
    {
        const string Root = "Assets/Project/Environment";
        public const string ShowcasePath = Root + "/Scenes/Floor3Showcase.unity";
        public const string MainPath = "Assets/Project/Scenes/Main.unity";
        static readonly string[] BoardNames = { "Source_Plane.043", "Source_Plane.044", "Source_Plane.045", "Source_Plane.046", "Source_Plane.049", "Source_Plane.050" };

        [MenuItem("Temporal Contract/Floor/Build Offline Showcase")]
        public static void BuildShowcase()
        {
            RequireEditMode();
            var original = SceneManager.GetActiveScene();
            if (SceneManager.GetSceneByPath(ShowcasePath).isLoaded) throw new InvalidOperationException("Close the showcase before rebuilding it.");
            File.Copy(Root + "/Scenes/FullFloorInspection.unity", ShowcasePath, true);
            AssetDatabase.ImportAsset(ShowcasePath, ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.OpenScene(ShowcasePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var walk = Components<EnvironmentWalkthrough>(scene).Single();
                walk.instructions = "FLOOR 3 · OFFLINE SHOWCASE · WASD / mouse · M map · E classroom board\nEsc cursor / close map · Home respawn · Complete floor and collision demonstration";
                var panelObject = Instantiate("Assets/Project/UI/Board/Prefabs/FloorMapPanel.prefab", null);
                var panel = panelObject.GetComponentInChildren<FloorMapPanel>(true);
                var capture = walk.gameObject.AddComponent<LocalInputCapture>();
                Set(panel, "captureComponent", capture);
                var actor = walk.gameObject.AddComponent<FloorShowcaseActor>();
                actor.walkthrough = walk; actor.capture = capture; actor.map = panel;
                var router = walk.gameObject.AddComponent<InteractionRouter>();
                router.Configure(walk.view, actor, capture); actor.router = router;
                new GameObject("Showcase EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                BindBoards(scene, panel);
                VerifyMeshes(scene);
                if (Components<NetworkManager>(scene).Length != 0) throw new InvalidOperationException("Offline scene must be server independent.");
                EditorSceneManager.SaveScene(scene, ShowcasePath);
                Debug.Log("FLOOR_SHOWCASE_PREPARED: full floor, six classroom boards, full map, no server.");
            }
            finally { SceneManager.SetActiveScene(original); EditorSceneManager.CloseScene(scene, true); }
        }

        [MenuItem("Temporal Contract/Floor/Place Full Floor Production Systems")]
        public static void BuildMain()
        {
            RequireEditMode();
            var original = SceneManager.GetActiveScene();
            if (SceneManager.GetSceneByPath(MainPath).isLoaded) throw new InvalidOperationException("Close Main before rebuilding it.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Scene source = default;
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("Full Floor Production Bindings");
                var bindings = root.AddComponent<FullFloorSceneBindings>();
                bindings.sourceManifest=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/full-floor-manifest.json");
                var floor = Instantiate(Root + "/Prefabs/Floor3FullEnvironment.prefab", root.transform);
                PrefabUtility.UnpackPrefabInstance(floor, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var sun = new GameObject("Floor daylight").AddComponent<Light>();
                sun.transform.SetParent(root.transform); sun.type = LightType.Directional; sun.intensity = .8f;
                sun.color = new Color(1f,.91f,.76f); sun.shadows = LightShadows.None;
                sun.transform.rotation = Quaternion.Euler(55,-35,0);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.57f,.60f,.64f);
                source = EditorSceneManager.OpenScene("Assets/Project/Scenes/NetworkCoreTest.unity", OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var session = UnityEngine.Object.Instantiate(Components<LanSessionController>(source).Single().gameObject);
                session.name = "Production LAN Session (Opt In)";
                // The owner's session calls DontDestroyOnLoad and therefore must remain a scene root.
                SceneManager.MoveGameObjectToScene(session, scene);
                bindings.session=session.GetComponent<LanSessionController>();
                // The session registers its Resources prefabs itself. An independent non-default
                // empty list avoids importing the editor's automatically populated default list twice.
                const string listPath=Root+"/Data/FullFloorNetworkPrefabs.asset";
                var networkPrefabs=AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(listPath);
                if(!networkPrefabs)
                {
                    networkPrefabs=ScriptableObject.CreateInstance<NetworkPrefabsList>();
                    AssetDatabase.CreateAsset(networkPrefabs,listPath);
                }
                if(networkPrefabs.PrefabList.Count!=0)throw new InvalidOperationException("The full-floor runtime registration list must start empty.");
                var networkConfig=session.GetComponent<NetworkManager>().NetworkConfig;
                networkConfig.Prefabs.NetworkPrefabsLists.Clear();networkConfig.Prefabs.NetworkPrefabsLists.Add(networkPrefabs);
                EditorSceneManager.CloseScene(source, true); source = default;
                var presentation = Instantiate(FullFloorPresentationBuilder.PrefabPath, root.transform);
                bindings.map = presentation.GetComponentInChildren<FloorMapPanel>(true);
                bindings.hud = presentation.GetComponentInChildren<HudSessionUi>(true);
                bindings.boards = BindBoards(scene, bindings.map);
                bindings.attackerSpawn = Marker(root.transform, "Attacker spawn - scene adapter required", new Vector3(11.02f,.1f,-7),270);
                bindings.defenderSpawn = Marker(root.transform, "Defender spawn - scene adapter required", new Vector3(-46.15f,.1f,-16.77f),90);
                bindings.vipSpawn = Marker(root.transform, "VIP spawn - scene adapter required", new Vector3(-49.98f,.1f,-14.5f),0);
                var renderers = floor.GetComponentsInChildren<MeshRenderer>(true);
                var doors = renderers.Where(r => r.name.StartsWith("Source_Door.", StringComparison.Ordinal)).OrderBy(r => r.name).ToArray();
                var handles = renderers.Where(r => r.name.StartsWith("Source_Handle_", StringComparison.Ordinal)).ToList();
                bindings.doors = doors.Select(door => BindDoor(door, handles, root.transform, door.name == "Source_Door.004")).ToArray();
                bindings.restrictedDoor = bindings.doors.Single(d => d.name.EndsWith("Source_Door.004",StringComparison.Ordinal));
                bindings.keycard = Interaction<KeycardPickup>("Keycard", root.transform, new Vector3(-18.98f,.12f,-9));
                var restrictedBounds = doors.Single(d => d.name == "Source_Door.004").bounds;
                bindings.accessPanel = Interaction<AccessPanel>("AccessPanel", root.transform,
                    new Vector3(restrictedBounds.max.x + .25f, .05f, restrictedBounds.center.z + .12f));
                Set(bindings.accessPanel,"keycard",bindings.keycard); Set(bindings.accessPanel,"restrictedDoor",bindings.restrictedDoor);
                bindings.lightSwitch = Interaction<WallSwitch>("WallSwitch",root.transform,new Vector3(-3.99f,0,-14.9f));
                bindings.lightSwitch.transform.rotation=Quaternion.Euler(0,90,0);
                var roomLight = new GameObject("Classroom 02 switched fill").AddComponent<Light>();
                roomLight.transform.SetParent(root.transform);roomLight.transform.position = new Vector3(-1,2.7f,-18);
                roomLight.type = LightType.Point; roomLight.intensity = 2; roomLight.range = 8; roomLight.shadows = LightShadows.None;
                var switchSettings = new SerializedObject(bindings.lightSwitch);
                var lights = switchSettings.FindProperty("lights"); lights.arraySize = 1; lights.GetArrayElementAtIndex(0).objectReferenceValue = roomLight;
                switchSettings.FindProperty("dimIntensityScale").floatValue=.1f; switchSettings.ApplyModifiedPropertiesWithoutUndo();
                // A separate gameplay chair preserves every original prop and its tested static collision.
                bindings.movableCover = Interaction<MovableCover>("Cover_Chair",root.transform,new Vector3(-45.3f,.08f,-15.2f));
                var coverSettings=new SerializedObject(bindings.movableCover);
                var footprint=(BoxCollider)coverSettings.FindProperty("footprint").objectReferenceValue;
                footprint.GetComponent<Renderer>().enabled=false;
                footprint.transform.localPosition=Vector3.zero;footprint.transform.localScale=Vector3.one;
                footprint.center=new Vector3(0,.45f,0);footprint.size=new Vector3(.43f,.9f,.67f);
                FloorAssetBuilder.PlaceModel("Floor3Chair",bindings.movableCover.transform,Vector3.zero);
                var layout=root.AddComponent<GameplaySceneLayout>();
                layout.attackerSpawn=bindings.attackerSpawn;layout.defenderSpawn=bindings.defenderSpawn;layout.vipSpawn=bindings.vipSpawn;
                bindings.attackerSpawn.name="Attacker spawn";bindings.defenderSpawn.name="Defender spawn";bindings.vipSpawn.name="VIP spawn";
                var adapter=root.AddComponent<ProductionPresentationAdapter>();
                adapter.session=bindings.session;adapter.hud=bindings.hud;adapter.keycard=bindings.keycard;adapter.map=bindings.map;
                Set(bindings.hud,"providerComponent",adapter);Set(bindings.hud,"actionsComponent",adapter);
                var hudSettings=new SerializedObject(bindings.hud);
                var host=(Button)hudSettings.FindProperty("hostButton").objectReferenceValue;
                var practice=UnityEngine.Object.Instantiate(host.gameObject,host.transform.parent).GetComponent<Button>();
                practice.name="PracticeButton";practice.GetComponentInChildren<Text>(true).text="Solo practice";
                var practiceRect=(RectTransform)practice.transform;practiceRect.anchoredPosition=new Vector2(0,-210);practiceRect.sizeDelta=new Vector2(460,56);
                Set(bindings.hud,"practiceButton",practice);
                var hudRoot=(GameObject)hudSettings.FindProperty("hudRoot").objectReferenceValue;
                var hooks=presentation.GetComponentInChildren<CombatFeedbackHooks>(true);
                var combat=root.AddComponent<ProductionCombatPresenter>();combat.hooks=hooks;
                combat.presenter=presentation.GetComponentInChildren<FeedbackPresenter>(true);
                combat.library=AssetDatabase.LoadAssetAtPath<FeedbackCueLibrary>("Assets/Project/Feedback/FeedbackCueLibrary.asset");
                var hit=new GameObject("Confirmed hit",typeof(RectTransform),typeof(CanvasGroup),typeof(Text));
                hit.transform.SetParent(hudRoot.transform,false);var hitRect=(RectTransform)hit.transform;
                hitRect.anchorMin=hitRect.anchorMax=new Vector2(.5f,.5f);hitRect.sizeDelta=new Vector2(50,50);
                var hitText=hit.GetComponent<Text>();hitText.text="×";hitText.font=host.GetComponentInChildren<Text>(true).font;hitText.fontSize=42;hitText.alignment=TextAnchor.MiddleCenter;hitText.raycastTarget=false;
                hit.GetComponent<CanvasGroup>().alpha=0;hit.GetComponent<CanvasGroup>().blocksRaycasts=false;
                var damage=new GameObject("Damage flash",typeof(RectTransform),typeof(CanvasGroup),typeof(Image));damage.transform.SetParent(hudRoot.transform,false);
                var damageRect=(RectTransform)damage.transform;damageRect.anchorMin=Vector2.zero;damageRect.anchorMax=Vector2.one;damageRect.offsetMin=damageRect.offsetMax=Vector2.zero;
                damage.GetComponent<Image>().color=new Color(.8f,0,0,.35f);damage.GetComponent<Image>().raycastTarget=false;
                damage.GetComponent<CanvasGroup>().alpha=0;damage.GetComponent<CanvasGroup>().blocksRaycasts=false;
                Set(hooks,"hitMarker",hit.GetComponent<CanvasGroup>());Set(hooks,"damageFlash",damage.GetComponent<CanvasGroup>());
                Physics.SyncTransforms();
                VerifyPlacement(scene,bindings);
                ValidateMain(scene);
                EditorSceneManager.SaveScene(scene,MainPath);
                // NGO's own validation needs a saved scene identity; do not retain copied prefab hashes.
                var validateNetworkObject=typeof(NetworkObject).GetMethod("OnValidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                if(validateNetworkObject==null)throw new InvalidOperationException("NetworkObject scene validation is unavailable.");
                foreach(var networkObject in Components<NetworkObject>(scene)) validateNetworkObject.Invoke(networkObject,null);
                EditorSceneManager.SaveScene(scene,MainPath);
                ValidateMain(scene);
                var settings=EditorBuildSettings.scenes.Where(s=>s.path!=MainPath).ToList();
                settings.Add(new EditorBuildSettingsScene(MainPath,false)); EditorBuildSettings.scenes=settings.ToArray();
                Debug.Log("FULL_FLOOR_MAIN_PLACED: full-floor production systems, scene-driven player/VIP poses, real HUD state/actions, practice entry and combat presentation. Runtime acceptance remains separate.");
            }
            finally
            {
                if(source.IsValid() && source.isLoaded) EditorSceneManager.CloseScene(source,true);
                SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);
            }
        }

        static NetworkDoor BindDoor(MeshRenderer mesh, System.Collections.Generic.List<MeshRenderer> handles, Transform parent, bool restricted)
        {
            var bounds=mesh.bounds; bool acrossX=bounds.size.x>bounds.size.z;
            float width=acrossX?bounds.size.x:bounds.size.z;
            var door=Interaction<NetworkDoor>(restricted?"Door_Restricted":"Door_Ordinary",parent,Vector3.zero);
            door.name += " - " + mesh.name;
            door.transform.rotation=Quaternion.Euler(0,acrossX?0:-90,0);
            var settings=new SerializedObject(door);
            var hinge=(Transform)settings.FindProperty("hinge").objectReferenceValue;
            foreach(Transform child in hinge.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var pivot=new Vector3(acrossX?bounds.min.x:bounds.center.x,bounds.min.y,acrossX?bounds.center.z:bounds.min.z);
            door.transform.position += pivot-hinge.position;
            string suffix=mesh.name.Substring("Source_Door".Length);
            // Prefer exact source identities; the seminar export uses a different handle suffix.
            var pieces=handles.Where(h => (h.name.EndsWith(suffix,StringComparison.Ordinal) || bounds.SqrDistance(h.bounds.center)<.06f) && Vector3.Distance(h.bounds.center,bounds.center)<2).ToArray();
            foreach(var h in pieces) { h.transform.SetParent(hinge,true); handles.Remove(h); }
            mesh.transform.SetParent(hinge,true);
            var slab=new GameObject("Source door collision");slab.transform.SetParent(hinge,false);
            var collider=slab.AddComponent<BoxCollider>();collider.center=new Vector3(width*.5f,bounds.size.y*.5f,0);
            collider.size=new Vector3(width,bounds.size.y,Mathf.Max(.034f,acrossX?bounds.size.z:bounds.size.x));
            var origin=new GameObject("Interaction point").transform;origin.SetParent(hinge,false);origin.position=bounds.center;
            settings.FindProperty("interactionPoint").objectReferenceValue=origin;
            var clearance=(BoxCollider)settings.FindProperty("doorwayClearance").objectReferenceValue;
            clearance.center=clearance.transform.InverseTransformPoint(bounds.center);clearance.size=new Vector3(width+.1f,bounds.size.y+.1f,.8f);
            settings.ApplyModifiedPropertiesWithoutUndo();return door;
        }

        public static ClassroomBoard[] BindBoards(Scene scene,FloorMapPanel panel)
        {
            return BoardNames.Select(name =>
            {
                var renderer=Components<MeshRenderer>(scene).Single(r=>r.name==name);
                var target=new GameObject("Floor map target - "+name);target.transform.position=renderer.bounds.center;
                target.transform.SetParent(renderer.transform,true);
                var box=target.AddComponent<BoxCollider>();
                // World-aligned target just outside the static board surface, with the same visible extent.
                box.size=renderer.bounds.size+Vector3.one*.04f;
                var board=target.AddComponent<ClassroomBoard>();Set(board,"panel",panel);return board;
            }).ToArray();
        }
        public static void ValidateMain(Scene scene)
        {
            VerifyMeshes(scene);
            if(Components<NetworkManager>(scene).Length!=1 || Components<NetworkDoor>(scene).Length!=24
                || Components<KeycardPickup>(scene).Length!=1 || Components<AccessPanel>(scene).Length!=1
                || Components<WallSwitch>(scene).Length!=1 || Components<MovableCover>(scene).Length!=1
                || Components<ClassroomBoard>(scene).Length!=6 || Components<EventSystem>(scene).Length!=1)
                throw new InvalidOperationException("Full-floor production composition is incomplete.");
            foreach(var behaviour in Components<MonoBehaviour>(scene))
                if(behaviour.GetType().FullName.Contains(".Testing.") || behaviour is TemporalContract.Combat.NetworkCombatTestBootstrap)
                    throw new InvalidOperationException("Test component in Main: "+behaviour.GetType().Name);
            if(Components<AudioListener>(scene).Any(a=>a.enabled))throw new InvalidOperationException("Main must use the spawned owner's listener.");
            var networkObjects=Components<NetworkObject>(scene);
            var hashes=networkObjects.Select(n=>new SerializedObject(n).FindProperty("GlobalObjectIdHash").longValue).ToArray();
            // Fresh scene objects receive their scene hashes when saved, checked again on the saved scene.
            if(!string.IsNullOrEmpty(scene.path) && (hashes.Any(h=>h==0) || hashes.Distinct().Count()!=hashes.Length))
                throw new InvalidOperationException("Scene NetworkObject hashes must be nonzero and unique.");
            foreach(var transform in Components<Transform>(scene))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)
                    throw new InvalidOperationException("Missing script: "+transform.name);
        }
        public static void VerifySavedScenes()
        {
            RequireEditMode();
            var original=SceneManager.GetActiveScene();
            var reference=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Floor3FullEnvironment.prefab");
            try
            {
                var expectedMeshes=reference.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Source_",StringComparison.Ordinal)).ToDictionary(r=>r.name);
                var expectedCollision=reference.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Baked floor collision");
                foreach(string path in new[]{MainPath,ShowcasePath})
                {
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    try
                    {
                        if(path==MainPath)ValidateMain(scene);else VerifyMeshes(scene);
                        foreach(var mesh in Components<MeshRenderer>(scene).Where(r=>r.name.StartsWith("Source_",StringComparison.Ordinal)))
                        {
                            var expected=expectedMeshes[mesh.name];
                            if(mesh.GetComponent<MeshFilter>().sharedMesh!=expected.GetComponent<MeshFilter>().sharedMesh
                                || !mesh.sharedMaterials.SequenceEqual(expected.sharedMaterials))
                                throw new InvalidOperationException("Source mesh/material changed: "+mesh.name);
                            // The showcase intentionally holds doors open. All other world-space source bounds must match.
                            if(path==MainPath || (!mesh.name.StartsWith("Source_Door.",StringComparison.Ordinal) && !mesh.name.StartsWith("Source_Handle_",StringComparison.Ordinal)))
                                if(Vector3.Distance(mesh.bounds.center,expected.bounds.center)>.001f || Vector3.Distance(mesh.bounds.size,expected.bounds.size)>.001f)
                                    throw new InvalidOperationException("Source bounds changed: "+mesh.name);
                        }
                        var actualCollision=Components<Transform>(scene).Single(t=>t.name=="Baked floor collision");
                        if(!CollisionSignature(expectedCollision).SequenceEqual(CollisionSignature(actualCollision)))
                            throw new InvalidOperationException("The tested static collision bake changed: "+path);
                        if(Components<ClassroomBoard>(scene).Length!=6)throw new InvalidOperationException("Classroom board targets missing.");
                        if(path==MainPath)
                        {
                            var binding=Components<FullFloorSceneBindings>(scene).Single();
                            if(binding.session.transform.parent!=null)throw new InvalidOperationException("LAN session must remain a root for DontDestroyOnLoad.");
                            var lists=binding.session.GetComponent<NetworkManager>().NetworkConfig.Prefabs.NetworkPrefabsLists;
                            if(lists.Count!=1 || lists[0].PrefabList.Count!=0 || new SerializedObject(lists[0]).FindProperty("IsDefault").boolValue)
                                throw new InvalidOperationException("Main must use its non-default runtime registration list.");
                        }
                    }
                    finally {SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
                }
                Debug.Log("FULL_FLOOR_SAVED_SCENES_PASS: all 1767 source mesh/material identities retained; Main source bounds match; static collision bake unchanged in Main/showcase; unique network hashes; six boards; root session; no production test scripts/listeners.");
            }
            finally {PrefabUtility.UnloadPrefabContents(reference);}
        }
        static string[] CollisionSignature(Transform root)
        {
            return root.GetComponentsInChildren<Collider>(true).Select(c=>
            {
                string shape=c is BoxCollider box?box.center.ToString("F5")+box.size.ToString("F5"):
                    c is MeshCollider mesh?AssetDatabase.GetAssetPath(mesh.sharedMesh):c.GetType().FullName;
                return c.name+"/"+c.transform.position.ToString("F5")+c.transform.rotation.ToString("F5")+c.transform.lossyScale.ToString("F5")+"/"+shape+"/"+c.isTrigger+"/"+c.enabled;
            }).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
        }
        static void VerifyPlacement(Scene scene,FullFloorSceneBindings bindings)
        {
            foreach(var marker in new[]{bindings.attackerSpawn,bindings.defenderSpawn,bindings.vipSpawn})
            {
                var p=marker.position;
                if(Physics.OverlapCapsule(p+Vector3.up*.31f,p+Vector3.up*1.49f,.3f,~0,QueryTriggerInteraction.Ignore).Any(c=>c.gameObject.scene==scene))
                    throw new InvalidOperationException("Scene pose intersects geometry: "+marker.name);
            }
            var settings=new SerializedObject(bindings.movableCover);
            var box=(BoxCollider)settings.FindProperty("footprint").objectReferenceValue;
            var hits=Physics.OverlapBox(box.transform.TransformPoint(box.center),Vector3.Scale(box.size*.5f,box.transform.lossyScale)*.97f,box.transform.rotation,~0,QueryTriggerInteraction.Ignore);
            if(hits.Any(c=>c.gameObject.scene==scene && !c.transform.IsChildOf(bindings.movableCover.transform)))
                throw new InvalidOperationException("Gameplay chair placement overlaps the static floor/props.");
        }
        static void VerifyMeshes(Scene scene)
        {
            if(Components<MeshRenderer>(scene).Count(r=>r.name.StartsWith("Source_",StringComparison.Ordinal))!=1767)
                throw new InvalidOperationException("The complete source floor must be retained.");
        }
        static Transform Marker(Transform parent,string name,Vector3 position,float yaw)
        { var go=new GameObject(name);go.transform.SetParent(parent);go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));return go.transform; }
        static T Interaction<T>(string name,Transform parent,Vector3 position) where T:Component
        { var go=Instantiate("Assets/Project/Interactions/Prefabs/"+name+".prefab",parent);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.transform.position=position;return go.GetComponent<T>(); }
        static GameObject Instantiate(string path,Transform parent)
        { var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new InvalidOperationException("Missing prefab: "+path);return (GameObject)PrefabUtility.InstantiatePrefab(prefab,parent); }
        static T[] Components<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Set(Component component,string name,UnityEngine.Object value)
        { var settings=new SerializedObject(component);settings.FindProperty(name).objectReferenceValue=value;settings.ApplyModifiedPropertiesWithoutUndo(); }
        static void RequireEditMode()
        { if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first."); }
    }
}
