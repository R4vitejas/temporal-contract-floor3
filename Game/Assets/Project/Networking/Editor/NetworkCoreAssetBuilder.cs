using TemporalContract.Networking;
using TemporalContract.Players;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TemporalContract.EditorTools
{
    public static class NetworkCoreAssetBuilder
    {
        private const string PlayerPrefabPath = "Assets/Project/Prefabs/Resources/NetworkCore/NetworkPlayer.prefab";
        private const string LobbyPrefabPath = "Assets/Project/Prefabs/Resources/NetworkCore/LobbyState.prefab";
        private const string TestScenePath = "Assets/Project/Scenes/NetworkCoreTest.unity";

        [MenuItem("Temporal Contract/Build Network Core Assets")]
        public static void BuildNetworkCoreAssets()
        {
            EnsureFolder("Assets/Project/Prefabs/Resources");
            EnsureFolder("Assets/Project/Prefabs/Resources/NetworkCore");
            BuildPlayerPrefab();
            BuildLobbyPrefab();
            BuildTestScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Network core prefabs and the LAN test scene are ready.");
        }

        private static void BuildPlayerPrefab()
        {
            GameObject player = new GameObject("NetworkPlayer");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = Vector3.up;
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            player.AddComponent<CharacterController>();
            player.GetComponent<CharacterController>().center = Vector3.up;
            player.AddComponent<NetworkObject>();
            player.AddComponent<OwnerNetworkTransform>();
            NetworkFpsPlayer controller = player.AddComponent<NetworkFpsPlayer>();
            InteractionRouter router = player.AddComponent<InteractionRouter>();

            GameObject view = new GameObject("OwnerCamera");
            view.transform.SetParent(player.transform, false);
            view.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            view.tag = "MainCamera";
            view.SetActive(false);
            Camera camera = view.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            view.AddComponent<AudioListener>();
            router.Configure(camera, controller, controller);

            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
        }

        private static void BuildLobbyPrefab()
        {
            GameObject lobby = new GameObject("LobbyState");
            lobby.AddComponent<NetworkObject>();
            lobby.AddComponent<LobbyNetworkState>();
            PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefabPath);
            Object.DestroyImmediate(lobby);
        }

        private static void BuildTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Test Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);

            GameObject directionalLight = new GameObject("Test Light");
            Light light = directionalLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject session = new GameObject("Network Core Session (Opt In)");
            session.AddComponent<UnityTransport>();
            session.AddComponent<NetworkManager>();
            session.AddComponent<LanSessionController>();

            EditorSceneManager.SaveScene(scene, TestScenePath);
            EditorBuildSettingsScene testScene = new EditorBuildSettingsScene(TestScenePath, true);
            EditorBuildSettingsScene sampleScene = new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true);
            EditorBuildSettings.scenes = new[] { testScene, sampleScene };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
