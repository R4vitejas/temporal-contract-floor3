using System.IO;
using TemporalContract.Interactions.Testing;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TemporalContract.Interactions.EditorTools
{
    /// <summary>Builds and runs the host Play Mode regression for MovableCover release safety.</summary>
    public static class CoverRegressionRunner
    {
        private const string ScenePath = "Assets/Project/Interactions/Scenes/CoverReleaseRegression.unity";
        private const string CoverPrefab = "Assets/Project/Interactions/Prefabs/Cover_Chair.prefab";

        [MenuItem("Temporal Contract/Interactions/Run Cover Release Regression")]
        public static void RunBatch()
        {
            BuildScene();
            EditorApplication.EnterPlaymode();
        }

        public static void BuildScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(30f, 1f, 30f);

            var coverObject = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(CoverPrefab));
            coverObject.transform.position = Vector3.zero;

            var host = new GameObject("NetworkManager");
            var manager = host.AddComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport = host.AddComponent<UnityTransport>();
            var regression = host.AddComponent<CoverReleaseRegression>();
            var so = new SerializedObject(regression);
            so.FindProperty("floor").objectReferenceValue = floor;
            so.FindProperty("cover").objectReferenceValue = coverObject.GetComponent<MovableCover>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
