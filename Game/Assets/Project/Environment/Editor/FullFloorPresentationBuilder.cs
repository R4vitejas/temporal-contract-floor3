using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TemporalContract.UI.Board;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Composes reviewed presentation pieces; production provider/actions binding remains an owner task.</summary>
    public static class FullFloorPresentationBuilder
    {
        public const string PrefabPath = "Assets/Project/Environment/Prefabs/Floor3Presentation.prefab";
        const string ConfigPath = "Assets/Project/Environment/Data/FloorAssemblyConfig.asset";
        static readonly string[] Pieces = {
            "Assets/Project/Feedback/Prefabs/FeedbackSystem.prefab",
            "Assets/Project/UI/Board/Prefabs/FloorMapPanel.prefab",
            "Assets/Project/UI/Hud/Prefabs/HudSessionUi.prefab"
        };

        [MenuItem("Temporal Contract/Floor/Prepare Full Floor Presentation Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");
            // Resolve every required asset before opening a temporary scene.
            var pieces = Array.ConvertAll(Pieces, path => AssetDatabase.LoadAssetAtPath<GameObject>(path)
                ?? throw new InvalidOperationException("Missing reviewed presentation prefab: " + path));
            var config = AssetDatabase.LoadAssetAtPath<FloorAssemblyConfig>(ConfigPath);
            if (!config) throw new InvalidOperationException("Missing floor assembly configuration.");
            var original = SceneManager.GetActiveScene();
            var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(temporary);
                var root = new GameObject("Floor 3 presentation - production adapter pending");
                foreach (var piece in pieces) PrefabUtility.InstantiatePrefab(piece, root.transform);
                var capture = root.AddComponent<PlayerInputCaptureAdapter>();
                var panel = root.GetComponentInChildren<FloorMapPanel>(true);
                var serialized = new SerializedObject(panel);
                serialized.FindProperty("captureComponent").objectReferenceValue = capture;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
                var events = new GameObject("Presentation EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(root.transform, false);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (!prefab) throw new InvalidOperationException("Presentation prefab save failed.");
                config.presentationPrefab = prefab;
                // Rebuilding this preparation requires the real binder to be verified again.
                config.presentationBindingsVerified = false;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log("FULL_FLOOR_PRESENTATION_PREPARED: reviewed feedback/map/HUD pieces, local capture adapter and one EventSystem. Production state/actions and gameplay binding remain pending.");
            }
            finally
            {
                SceneManager.SetActiveScene(original);
                EditorSceneManager.CloseScene(temporary, true);
            }
        }
    }
}
