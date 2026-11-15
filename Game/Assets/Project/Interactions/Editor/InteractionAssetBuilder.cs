using System.IO;
using TemporalContract.Interactions.Testing;
using TemporalContract.Networking;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TemporalContract.Interactions.EditorTools
{
    /// <summary>
    /// Regenerates the interaction prefabs and the standalone test scene. Run from the menu or
    /// headless: Unity -batchmode -executeMethod TemporalContract.Interactions.EditorTools.InteractionAssetBuilder.BuildAll
    /// </summary>
    public static class InteractionAssetBuilder
    {
        private const string Root = "Assets/Project/Interactions";
        private const string PrefabDir = Root + "/Prefabs";
        private const string MaterialDir = Root + "/Materials";
        private const string SceneDir = Root + "/Scenes";

        [MenuItem("Temporal Contract/Interactions/Build Prefabs And Test Scene")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MaterialDir);
            Directory.CreateDirectory(SceneDir);
            AssetDatabase.Refresh();

            GameObject door = BuildDoor("Door_Ordinary", false, new Color(0.45f, 0.3f, 0.18f));
            GameObject restricted = BuildDoor("Door_Restricted", true, new Color(0.6f, 0.1f, 0.1f));
            GameObject keycard = BuildKeycard();
            GameObject panel = BuildPanel();
            GameObject wallSwitch = BuildSwitch();
            GameObject chair = BuildCover("Cover_Chair", new Vector3(0.5f, 0.9f, 0.5f), new Color(0.3f, 0.3f, 0.6f));
            GameObject desk = BuildCover("Cover_Desk", new Vector3(1.2f, 0.75f, 0.6f), new Color(0.5f, 0.4f, 0.25f));
            GameObject player = BuildTestPlayer();

            BuildScene(door, restricted, keycard, panel, wallSwitch, chair, desk, player);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Interactions] Prefabs and test scene built.");
        }

        // ---------- prefabs ----------

        private static GameObject BuildDoor(string name, bool locked, Color color)
        {
            var root = new GameObject(name);
            root.AddComponent<NetworkObject>();
            var door = root.AddComponent<NetworkDoor>();

            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = new Vector3(-0.5f, 0f, 0f);

            GameObject slab = Cube("Slab", hinge, new Vector3(0.5f, 1.05f, 0f), new Vector3(1f, 2.1f, 0.08f), color, true);

            var clearance = new GameObject("DoorwayClearance");
            clearance.transform.SetParent(root.transform, false);
            var box = clearance.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 1.05f, 0f);
            box.size = new Vector3(1.2f, 2.1f, 0.8f);

            SerializedObject so = new SerializedObject(door);
            so.FindProperty("hinge").objectReferenceValue = hinge;
            so.FindProperty("lockedByDefault").boolValue = locked;
            so.FindProperty("doorwayClearance").objectReferenceValue = box;
            so.FindProperty("interactionPoint").objectReferenceValue = slab.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(root);
        }

        private static GameObject BuildKeycard()
        {
            var root = new GameObject("Keycard");
            root.AddComponent<NetworkObject>();
            var pickup = root.AddComponent<KeycardPickup>();
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.05f, 0f);
            box.size = new Vector3(0.3f, 0.1f, 0.3f);
            GameObject visual = Cube("Visual", root.transform, new Vector3(0f, 0.05f, 0f),
                new Vector3(0.12f, 0.015f, 0.08f), new Color(0.1f, 0.8f, 0.3f), false);

            SerializedObject so = new SerializedObject(pickup);
            so.FindProperty("visualRoot").objectReferenceValue = visual;
            so.FindProperty("pickupCollider").objectReferenceValue = box;
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(root);
        }

        private static GameObject BuildPanel()
        {
            var root = new GameObject("AccessPanel");
            root.AddComponent<NetworkObject>();
            root.AddComponent<AccessPanel>();
            Cube("Face", root.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.3f, 0.4f, 0.05f),
                new Color(0.1f, 0.4f, 0.8f), true);
            return Save(root);
        }

        private static GameObject BuildSwitch()
        {
            var root = new GameObject("WallSwitch");
            root.AddComponent<NetworkObject>();
            root.AddComponent<WallSwitch>();
            Cube("Plate", root.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.12f, 0.18f, 0.03f),
                new Color(0.9f, 0.9f, 0.9f), true);
            return Save(root);
        }

        private static GameObject BuildCover(string name, Vector3 size, Color color)
        {
            var root = new GameObject(name);
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>(); // server-authoritative by default
            var cover = root.AddComponent<MovableCover>();
            GameObject body = Cube("Body", root.transform, new Vector3(0f, size.y * 0.5f, 0f), size, color, true);

            SerializedObject so = new SerializedObject(cover);
            so.FindProperty("footprint").objectReferenceValue = body.GetComponent<BoxCollider>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(root);
        }

        private static GameObject BuildTestPlayer()
        {
            var root = new GameObject("TestPlayer");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            root.AddComponent<NetworkObject>();
            var netTransform = root.AddComponent<NetworkTransform>();
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            var test = root.AddComponent<TestPlayerController>();
            var router = root.AddComponent<InteractionRouter>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            visual.GetComponent<Renderer>().sharedMaterial = Mat("PlayerBody", new Color(0.8f, 0.8f, 0.2f));

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            AudioListener listener = camGo.AddComponent<AudioListener>();

            SerializedObject routerSo = new SerializedObject(router);
            routerSo.FindProperty("playerCamera").objectReferenceValue = cam;
            routerSo.FindProperty("actorComponent").objectReferenceValue = test;
            routerSo.FindProperty("inputCaptureComponent").objectReferenceValue = test;
            routerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject testSo = new SerializedObject(test);
            testSo.FindProperty("playerCamera").objectReferenceValue = cam;
            testSo.FindProperty("listener").objectReferenceValue = listener;
            testSo.FindProperty("router").objectReferenceValue = router;
            testSo.ApplyModifiedPropertiesWithoutUndo();
            return Save(root);
        }

        // ---------- test scene ----------

        private static void BuildScene(GameObject door, GameObject restricted, GameObject keycard, GameObject panel,
            GameObject wallSwitch, GameObject chair, GameObject desk, GameObject player)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.28f, 0.3f);

            Color wallColor = new Color(0.75f, 0.75f, 0.78f);
            Cube("Floor", null, new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 20f), new Color(0.35f, 0.35f, 0.38f), true);
            // outer walls
            Cube("Wall_N", null, new Vector3(0f, 1.5f, 10f), new Vector3(20f, 3f, 0.2f), wallColor, true);
            Cube("Wall_S", null, new Vector3(0f, 1.5f, -10f), new Vector3(20f, 3f, 0.2f), wallColor, true);
            Cube("Wall_E", null, new Vector3(10f, 1.5f, 0f), new Vector3(0.2f, 3f, 20f), wallColor, true);
            Cube("Wall_W", null, new Vector3(-10f, 1.5f, 0f), new Vector3(0.2f, 3f, 20f), wallColor, true);
            // divider at z=0 with a 1m doorway at x=0 (ordinary door)
            Cube("Divider_A", null, new Vector3(-5.25f, 1.5f, 0f), new Vector3(9.5f, 3f, 0.2f), wallColor, true);
            Cube("Divider_B", null, new Vector3(5.25f, 1.5f, 0f), new Vector3(9.5f, 3f, 0.2f), wallColor, true);
            Cube("DoorHeader", null, new Vector3(0f, 2.55f, 0f), new Vector3(1f, 0.9f, 0.2f), wallColor, true);
            // divider at x=6 (z>0) with a 1m doorway at z=5 (restricted door)
            Cube("Restricted_A", null, new Vector3(6f, 1.5f, 2.25f), new Vector3(0.2f, 3f, 4.5f), wallColor, true);
            Cube("Restricted_B", null, new Vector3(6f, 1.5f, 7.75f), new Vector3(0.2f, 3f, 4.5f), wallColor, true);
            Cube("RestrictedHeader", null, new Vector3(6f, 2.55f, 5f), new Vector3(0.2f, 0.9f, 1f), wallColor, true);

            var cam = new GameObject("FallbackCamera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            cam.transform.position = new Vector3(0f, 6f, -9f);
            cam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

            NetworkDoor ordinary = Place<NetworkDoor>(door, new Vector3(0f, 0f, 0f), Quaternion.identity);
            NetworkDoor restrictedDoor = Place<NetworkDoor>(restricted, new Vector3(6f, 0f, 5f), Quaternion.Euler(0f, 90f, 0f));
            KeycardPickup card = Place<KeycardPickup>(keycard, new Vector3(-5f, 0.8f, -6f), Quaternion.identity);
            AccessPanel accessPanel = Place<AccessPanel>(panel, new Vector3(5.82f, 0f, 3.6f), Quaternion.Euler(0f, 90f, 0f));
            WallSwitch lightSwitch = Place<WallSwitch>(wallSwitch, new Vector3(1.6f, 0f, -0.14f), Quaternion.identity);
            Place<MovableCover>(chair, new Vector3(-3f, 0f, -3f), Quaternion.identity);
            Place<MovableCover>(desk, new Vector3(3f, 0f, -5f), Quaternion.identity);

            // room A lights controlled by the wall switch
            Light l1 = PointLight("RoomA_Light1", new Vector3(-5f, 2.6f, -5f));
            Light l2 = PointLight("RoomA_Light2", new Vector3(5f, 2.6f, -5f));
            SerializedObject switchSo = new SerializedObject(lightSwitch);
            SerializedProperty lights = switchSo.FindProperty("lights");
            lights.arraySize = 2;
            lights.GetArrayElementAtIndex(0).objectReferenceValue = l1;
            lights.GetArrayElementAtIndex(1).objectReferenceValue = l2;
            switchSo.FindProperty("dimIntensityScale").floatValue = 0.1f;
            switchSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject panelSo = new SerializedObject(accessPanel);
            panelSo.FindProperty("keycard").objectReferenceValue = card;
            panelSo.FindProperty("restrictedDoor").objectReferenceValue = restrictedDoor;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            // network manager + test menu
            var nmGo = new GameObject("NetworkManager");
            var nm = nmGo.AddComponent<NetworkManager>();
            var transport = nmGo.AddComponent<UnityTransport>();
            nm.NetworkConfig.NetworkTransport = transport;
            nm.NetworkConfig.PlayerPrefab = player;
            var prefabList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            prefabList.Add(new NetworkPrefab { Prefab = player });
            string listPath = Root + "/InteractionTestPrefabs.asset";
            AssetDatabase.DeleteAsset(listPath);
            AssetDatabase.CreateAsset(prefabList, listPath);
            nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabList);
            nmGo.AddComponent<TestNetworkMenu>();

            EditorSceneManager.SaveScene(scene, SceneDir + "/InteractionsTest.unity");
            _ = ordinary;
        }

        // ---------- helpers ----------

        private static T Place<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance.GetComponent<T>();
        }

        private static Light PointLight(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 25f;
            light.shadows = LightShadows.None;
            return light;
        }

        private static GameObject Save(GameObject root)
        {
            string path = $"{PrefabDir}/{root.name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 localPosition, Vector3 scale,
            Color color, bool withCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            if (parent != null)
            {
                cube.transform.SetParent(parent, false);
            }

            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = Mat(color.ToString(), color);
            if (!withCollider)
            {
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            return cube;
        }

        private static Material Mat(string key, Color color)
        {
            string safe = key.Replace("(", string.Empty).Replace(")", string.Empty).Replace(",", "_").Replace(" ", string.Empty).Replace(".", "p");
            string path = $"{MaterialDir}/{safe}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
