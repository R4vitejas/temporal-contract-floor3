// Review-only host reproduction. Copy into an Editor folder in an isolated checkout; never include in shipped Main.
using System;
using System.IO;
using System.Reflection;
using TemporalContract.Core;
using TemporalContract.Combat;
using TemporalContract.Interactions;
using TemporalContract.Networking;
using TemporalContract.Players;
using TemporalContract.Rounds;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ReviewProductionEligibility
{
    [Serializable] sealed class Result
    {
        public string reviewedCommit=System.Environment.GetEnvironmentVariable("REVIEW_SOURCE_SHA") ?? "record checkout SHA", scope="Isolated host: committed NetworkCoreTest scene/player, real health and server interaction validation; host-only match fixture invokes the real BeginRound/EndRound methods";
        public bool optInStartup,host,playerSpawned,productionEligibilityBound,productionRoleProviderBound,productionHealthyInteractionAccepted,defenderDeathKeepsRoundActive,deadActorAlive,deadInteractionAccepted,betweenRoundGameplayActive,betweenRoundInteractionAccepted,guardedHealthyAccepted,guardedDeadDenied,guardedBetweenRoundDenied;
        public int localCameras,localListeners;
        public float capsuleHeight,capsuleRadius,stepOffset,centerY;
    }
    static int stage,frames;
    static NetworkManager manager;
    static double deadline;
    static readonly Result result=new Result();
    static ReviewProductionEligibility(){if(SessionState.GetBool("ReviewProductionEligibility",false))EditorApplication.update+=Tick;}
    public static void Begin()
    {
        EditorSceneManager.OpenScene("Assets/Project/Scenes/NetworkCoreTest.unity");
        SessionState.SetBool("ReviewProductionEligibility",true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
    }
    static NetworkVariable<T> Variable<T>(object target,string name) => (NetworkVariable<T>)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    static NetworkDoor Door(Vector3 position)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Interactions/Prefabs/Door_Ordinary.prefab");
        if(!manager.NetworkConfig.Prefabs.Contains(prefab))manager.AddNetworkPrefab(prefab);
        var instance=UnityEngine.Object.Instantiate(prefab,position,Quaternion.identity);
        instance.GetComponent<NetworkObject>().Spawn();return instance.GetComponent<NetworkDoor>();
    }
    static bool Request(NetworkDoor door) => door.TryHandleInteraction(new InteractionRequest(manager.LocalClientId,door.NetworkObjectId));
    static void Tick()
    {
        if(!Application.isPlaying)return;
        try
        {
            if(stage==0)
            {
                stage=1;manager=NetworkManager.Singleton;result.optInStartup=manager!=null&&!manager.IsListening;
                var session=UnityEngine.Object.FindFirstObjectByType<LanSessionController>();
                typeof(LanSessionController).GetField("portText",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session,"47966");
                session.StartHost();deadline=EditorApplication.timeSinceStartup+15;return;
            }
            if(stage==1)
            {
                var local=manager.LocalClient;var playerObject=local==null?null:local.PlayerObject;
                if(playerObject==null||RoundCoordinator.Instance==null)
                {
                    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Host/player/round coordinator did not become ready");
                    return;
                }
                if(++frames<3)return;stage=2;
                var round=RoundCoordinator.Instance;var player=playerObject.GetComponent<NetworkFpsPlayer>();var health=playerObject.GetComponent<NetworkPlayerHealth>();
                var body=playerObject.GetComponent<CharacterController>();
                result.host=manager.IsHost;result.playerSpawned=playerObject.IsSpawned;
                result.capsuleHeight=body.height;result.capsuleRadius=body.radius;result.stepOffset=body.stepOffset;result.centerY=body.center.y;
                result.localCameras=Array.FindAll(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None),c=>c.enabled&&c.gameObject.activeInHierarchy).Length;
                result.localListeners=Array.FindAll(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None),c=>c.enabled&&c.gameObject.activeInHierarchy).Length;
                var phase=Variable<MatchPhase>(round,"phase");
                Variable<ulong>(round,"attackerClientId").Value=123; // local host represents the defender
                Variable<ulong>(round,"firstClientId").Value=manager.LocalClientId;
                Variable<ulong>(round,"secondClientId").Value=123;
                typeof(RoundCoordinator).GetField("matchBegun",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(round,true);
                Variable<bool>(LobbyNetworkState.Instance,"matchStarted").Value=true;
                LobbyNetworkState.Instance.SetAttackerClientId(123);
                typeof(RoundCoordinator).GetMethod("BeginRound",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(round,null);
                if(!round.IsGameplayActive)throw new Exception("Fixture could not begin an active round");
                player.SetGameplayInputCaptured(UnityEngine.Object.FindFirstObjectByType<LanSessionController>(),false);
                result.productionEligibilityBound=InteractionRoleGate.IsActorEligible!=null;
                result.productionRoleProviderBound=InteractionRoleGate.IsBound;
                result.productionHealthyInteractionAccepted=Request(Door(player.transform.position+Vector3.forward*2.2f));
                if(!health.ApplyDamage(health.MaximumHealth,123))throw new Exception("Could not establish dead-player scenario");
                result.deadActorAlive=health.IsAlive;result.defenderDeathKeepsRoundActive=round.IsGameplayActive;
                result.deadInteractionAccepted=Request(Door(player.transform.position+Vector3.right*2.2f));
                Variable<int>(health,"health").Value=health.MaximumHealth;
                typeof(RoundCoordinator).GetMethod("EndRound",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(round,new object[]{RoundOutcome.DefendersWon});
                result.betweenRoundGameplayActive=round.IsGameplayActive;
                result.betweenRoundInteractionAccepted=Request(Door(player.transform.position-Vector3.right*2.2f));
                // Control: demonstrate the existing optional gate rejects those states while allowing valid play.
                InteractionLifecycle.BindActorEligibility(id=>id==player.OwnerClientId&&round.IsGameplayActive&&health.IsAlive);
                phase.Value=MatchPhase.ActiveRound;
                var control=Door(player.transform.position-Vector3.forward*2.2f);
                result.guardedHealthyAccepted=Request(control);
                Variable<int>(health,"health").Value=0;
                result.guardedDeadDenied=!Request(control);
                Variable<int>(health,"health").Value=health.MaximumHealth;phase.Value=MatchPhase.RoundResult;
                result.guardedBetweenRoundDenied=!Request(control);
                string output=System.Environment.GetEnvironmentVariable("PRODUCTION_ELIGIBILITY_REVIEW_OUTPUT") ?? Path.Combine(Path.GetTempPath(),"temporal-contract-production-eligibility.json");
                File.WriteAllText(output,JsonUtility.ToJson(result,true));
                Debug.Log("PRODUCTION_ELIGIBILITY_REVIEW "+JsonUtility.ToJson(result));
                Finish(result.deadInteractionAccepted||result.betweenRoundInteractionAccepted||!result.productionHealthyInteractionAccepted||!result.defenderDeathKeepsRoundActive?2:
                    result.guardedHealthyAccepted&&result.guardedDeadDenied&&result.guardedBetweenRoundDenied?0:3);
            }
        }
        catch(Exception error){Debug.LogException(error);Finish(3);}
    }
    static void Finish(int exitCode){SessionState.SetBool("ReviewProductionEligibility",false);EditorApplication.update-=Tick;if(manager)manager.Shutdown();EditorApplication.Exit(exitCode);}
}
