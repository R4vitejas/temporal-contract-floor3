using System;
using TemporalContract.Players;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TemporalContract.Environment.EditorTools
{
    public static class PlayableMvpBuilder
    {
        [MenuItem("Temporal Contract/Floor/Build Playable MVP Scene")]
        public static void Build()
        {
            const string path="Assets/Project/Prefabs/Resources/NetworkCore/NetworkPlayer.prefab";
            var player=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var camera=player.GetComponentInChildren<Camera>(true);
                var view=camera.GetComponent<LocalWeaponView>();if(!view)view=camera.gameObject.AddComponent<LocalWeaponView>();
                view.bodyMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Project/Environment/Materials/Floor3_Metal.mat");
                view.flashMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Project/Feedback/Materials/Test_E6E64C.mat");
                if(!view.bodyMaterial||!view.flashMaterial)throw new InvalidOperationException("Missing weapon presentation materials.");
                var body=player.GetComponent<CharacterController>();body.height=1.8f;body.radius=.3f;body.center=Vector3.up*.9f;
                body.skinWidth=.03f;body.stepOffset=.05f;body.minMoveDistance=0;
                PrefabUtility.SaveAsPrefabAsset(player,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(player);}
            FullFloorAssemblyBuilder.BuildMain();
            Debug.Log("PLAYABLE_MVP_SCENE_PREPARED: real HUD + weapon + scene poses; explicit solo practice and standard LAN entry.");
        }
        public static void EnableMain()
        {
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(FullFloorAssemblyBuilder.MainPath,true)};
            EditorSceneManager.OpenScene(FullFloorAssemblyBuilder.MainPath);
        }
        public static void RunRegression()
        {
            EditorSceneManager.OpenScene(FullFloorAssemblyBuilder.MainPath);
            EditorApplication.EnterPlaymode();
        }
    }
}
