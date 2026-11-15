using System;
using System.IO;
using System.Linq;
using TemporalContract.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TemporalContract.Environment.EditorTools
{
    public static class FloorIntegrationBuilder
    {
        private const string MainPath="Assets/Project/Scenes/Main.unity";
        private const string ConfigPath=FloorAssetBuilder.Root+"/Data/FloorAssemblyConfig.asset";
        private const string InteractionRoot="Assets/Project/Interactions/Prefabs/";

        [MenuItem("Temporal Contract/Floor/Prepare Main Scene")]
        public static void PrepareMain()
        {
            if(AssetDatabase.LoadAssetAtPath<FloorAssemblyConfig>(ConfigPath)==null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FloorAssemblyConfig>(),ConfigPath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            FloorAssetBuilder.ConfigureLighting();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FloorAssetBuilder.Root+"/Prefabs/Floor3Environment.prefab");
            if(prefab==null)throw new InvalidOperationException("Build the floor assets first.");
            PrefabUtility.InstantiatePrefab(prefab);
            Directory.CreateDirectory("Assets/Project/Scenes");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),MainPath);
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=MainPath).ToList();
            scenes.Add(new EditorBuildSettingsScene(MainPath,false));EditorBuildSettings.scenes=scenes.ToArray();
            VerifyPreparedMain();
            Debug.Log("FLOOR_MAIN_PREPARED: geometry and anchors only; Main stays disabled in Build Settings until reviewed assembly.");
        }

        public static void PrepareAndVerify()
        {
            FloorAssetBuilder.BuildAll();
            PrepareMain();
            VerifyPendingAssemblyGate();
        }
        public static void VerifyPendingAssemblyGate()
        {
            var config=AssetDatabase.LoadAssetAtPath<FloorAssemblyConfig>(ConfigPath);
            if(config==null||config.subsystemReviewsAccepted)throw new InvalidOperationException("This check requires the unaccepted preparation config.");
            string before=File.ReadAllText(MainPath);
            bool refused=false;
            try{AssembleReviewedSubsystems();}
            catch(InvalidOperationException error)
            {
                if(!error.Message.StartsWith("Accept the subsystem PRs"))throw;
                refused=true;
            }
            if(!refused||before!=File.ReadAllText(MainPath))throw new InvalidOperationException("Unaccepted assembly changed Main.");
            Debug.Log("FLOOR_PENDING_GATE_PASS: assembly refused without changing Main.");
        }

        public static void VerifyPreparedMain()
        {
            var anchors=UnityEngine.Object.FindFirstObjectByType<FloorIntegrationAnchors>();
            if(anchors==null||anchors.rooms.Length!=4||anchors.boardMap==null)throw new InvalidOperationException("Main floor references are incomplete.");
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)!=0)throw new InvalidOperationException("Missing script in prepared Main.");
                    foreach(var behaviour in transform.GetComponents<MonoBehaviour>())
                        if(behaviour.GetType().FullName.Contains(".Testing."))throw new InvalidOperationException("Test-only code in prepared Main.");
                }
            if(EditorBuildSettings.scenes.Any(s=>s.path==MainPath&&s.enabled))throw new InvalidOperationException("Prepared Main must stay out of production builds.");
            Debug.Log("FLOOR_MAIN_PREPARATION_VERIFY_PASS rooms=4 missingScripts=0 testControllers=0 buildEnabled=false");
        }

        [MenuItem("Temporal Contract/Floor/Assemble Reviewed Subsystems")]
        public static void AssembleReviewedSubsystems()
        {
            var config=AssetDatabase.LoadAssetAtPath<FloorAssemblyConfig>(ConfigPath);
            if(config==null||!config.subsystemReviewsAccepted)
                throw new InvalidOperationException("Accept the subsystem PRs and record their source commits in FloorAssemblyConfig before assembly.");
            if(!config.productionCapsuleFitVerified)throw new InvalidOperationException("Verify the actual production player capsule against the source doorways before assembly.");
            if(config.presentationPrefab==null||!config.presentationBindingsVerified)
                throw new InvalidOperationException("Assign Sanjana's reviewed presentation prefab and verify its documented board/HUD/feedback bindings first.");
            if(string.IsNullOrWhiteSpace(config.networkCommit)||string.IsNullOrWhiteSpace(config.interactionCommit)||string.IsNullOrWhiteSpace(config.presentationCommit))
                throw new InvalidOperationException("Record all three reviewed subsystem commit hashes.");
            Type sessionType=Type.GetType("TemporalContract.Networking.LanSessionController, Assembly-CSharp");
            Type roundsType=Type.GetType("TemporalContract.Rounds.RoundCoordinator, Assembly-CSharp");
            if(sessionType==null||roundsType==null)throw new InvalidOperationException("Reviewed network and round code is unavailable.");
            foreach(string name in new[]{"Door_Ordinary","Door_Restricted","Keycard","AccessPanel","WallSwitch","Cover_Chair"})
                if(AssetDatabase.LoadAssetAtPath<GameObject>(InteractionRoot+name+".prefab")==null)throw new InvalidOperationException("Missing interaction prefab: "+name);
            string networkScene="Assets/Project/Scenes/NetworkCoreTest.unity";
            if(!File.Exists(networkScene))throw new InvalidOperationException("Network scene/session setup is unavailable.");
            PrepareMain();
            var scene=SceneManager.GetActiveScene();
            var floor=UnityEngine.Object.FindFirstObjectByType<FloorIntegrationAnchors>();
            PrefabUtility.UnpackPrefabInstance(floor.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            // Copy the owner's opt-in session configuration, without its test arena or test spawners.
            var source=EditorSceneManager.OpenScene(networkScene,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var session=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).First(b=>b.GetType()==sessionType);
            var productionSession=UnityEngine.Object.Instantiate(session.gameObject);
            productionSession.name="Production LAN Session";
            SceneManager.MoveGameObjectToScene(productionSession,scene);
            EditorSceneManager.CloseScene(source,true);
            var interactionGroup=new GameObject("Reviewed Environmental Interactions").transform;
            Component restrictedDoor=null;
            for(int i=0;i<floor.rooms.Length;i++)
            {
                var room=floor.rooms[i];Vector3 hingePosition=room.doorHinge.position;
                UnityEngine.Object.DestroyImmediate(room.doorHinge.gameObject);
                var door=Interaction(room.restricted?"Door_Restricted":"Door_Ordinary",interactionGroup,hingePosition);
                var doorComponent=FindComponent(door,"NetworkDoor");
                var serialized=new SerializedObject(doorComponent);
                var hinge=(Transform)serialized.FindProperty("hinge").objectReferenceValue;
                door.transform.position=hingePosition-hinge.localPosition;
                foreach(Transform child in hinge.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                FloorAssetBuilder.PlaceModel(room.sourceRoom+"_Door",hinge,Vector3.zero);
                var slab=new GameObject("Source Door Collision");slab.transform.SetParent(hinge,false);
                serialized.FindProperty("interactionPoint").objectReferenceValue=slab.transform;serialized.ApplyModifiedPropertiesWithoutUndo();
                var collider=slab.AddComponent<BoxCollider>();collider.center=new Vector3(.46707f,1.005f,0f);collider.size=new Vector3(.93414f,2.01f,.04f);
                var clearance=(BoxCollider)serialized.FindProperty("doorwayClearance").objectReferenceValue;
                clearance.center=new Vector3(hinge.localPosition.x+.46707f,1.005f,0f);clearance.size=new Vector3(1.1f,2.1f,.75f);
                room.doorHinge=hinge;
                if(room.restricted)restrictedDoor=doorComponent;
            }
            var card=Interaction("Keycard",interactionGroup,floor.keycard.position);
            var panel=Interaction("AccessPanel",interactionGroup,floor.accessPanel.position);
            SetReference(FindComponent(panel,"AccessPanel"),"keycard",FindComponent(card,"KeycardPickup"));
            SetReference(FindComponent(panel,"AccessPanel"),"restrictedDoor",restrictedDoor);
            var lightSwitch=Interaction("WallSwitch",interactionGroup,floor.lightSwitch.position);
            var switchSettings=new SerializedObject(FindComponent(lightSwitch,"WallSwitch"));
            var lights=switchSettings.FindProperty("lights");lights.arraySize=1;lights.GetArrayElementAtIndex(0).objectReferenceValue=floor.rooms[0].roomLight;
            switchSettings.FindProperty("dimIntensityScale").floatValue=.1f;switchSettings.ApplyModifiedPropertiesWithoutUndo();
            // Replace the source chair's static preview and its collider with the reviewed single-holder cover.
            foreach(Transform child in floor.movableChair.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var staticChair=floor.transform.Find("Simple Static Colliders/Supplied chair collider");
            if(staticChair!=null)UnityEngine.Object.DestroyImmediate(staticChair.gameObject);
            var cover=Interaction("Cover_Chair",interactionGroup,floor.movableChair.position);
            var coverSettings=new SerializedObject(FindComponent(cover,"MovableCover"));
            var footprint=(BoxCollider)coverSettings.FindProperty("footprint").objectReferenceValue;
            footprint.transform.localScale=Vector3.one;footprint.transform.localPosition=Vector3.zero;
            footprint.center=new Vector3(0f,.45f,0f);footprint.size=new Vector3(.43f,.9f,.67f);
            var placeholder=footprint.GetComponent<Renderer>();if(placeholder!=null)placeholder.enabled=false;
            FloorAssetBuilder.PlaceModel("Floor3Chair",cover.transform,Vector3.zero);
            var presentation=(GameObject)PrefabUtility.InstantiatePrefab(config.presentationPrefab);
            presentation.name="Reviewed Board HUD And Feedback";
            // Sanjana's published binder owns the presentation bindings; do not invent another HUD/controller here.
            EditorUtility.SetDirty(floor);
            EditorSceneManager.SaveScene(scene,MainPath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MainPath,true)};
            ValidateProductionScene();
            Debug.Log("FLOOR_MAIN_ASSEMBLED: reviewed prefab placement complete; run full host/client acceptance before release.");
        }

        private static GameObject Interaction(string name,Transform parent,Vector3 position)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(InteractionRoot+name+".prefab"));
            PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            go.transform.SetParent(parent,false);go.transform.position=position;return go;
        }
        private static Component FindComponent(GameObject go,string name)=>go.GetComponents<Component>().First(c=>c.GetType().Name==name);
        private static void SetReference(Component component,string field,UnityEngine.Object value)
        {
            if(value==null)throw new InvalidOperationException("Missing binding: "+field);
            var settings=new SerializedObject(component);settings.FindProperty(field).objectReferenceValue=value;settings.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void ValidateProductionScene()
        {
            var config=AssetDatabase.LoadAssetAtPath<FloorAssemblyConfig>(ConfigPath);
            if(config==null||!config.subsystemReviewsAccepted||!config.productionCapsuleFitVerified||!config.presentationBindingsVerified||config.presentationPrefab==null)
                throw new InvalidOperationException("Subsystem/presentation acceptance is not recorded.");
            int managers=0,doors=0,cards=0,panels=0,covers=0;
            foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                string type=component.GetType().FullName;
                if(type=="Unity.Netcode.NetworkManager")managers++;
                if(type=="TemporalContract.Interactions.NetworkDoor")doors++;
                if(type=="TemporalContract.Interactions.KeycardPickup")cards++;
                if(type=="TemporalContract.Interactions.AccessPanel")panels++;
                if(type=="TemporalContract.Interactions.MovableCover")covers++;
                if(type.Contains(".Testing.")||type.EndsWith("NetworkCombatTestBootstrap"))throw new InvalidOperationException("Test harness in Main: "+type);
            }
            if(doors!=4||cards!=1||panels!=1||covers!=1)throw new InvalidOperationException("Required interaction instances are missing.");
            if(managers!=1)throw new InvalidOperationException("Main must contain exactly one production NetworkManager.");
            if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Any(a=>a.enabled))
                throw new InvalidOperationException("Main must use only the spawned owner's audio listener.");
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing script: "+t.name);
        }
    }
}
