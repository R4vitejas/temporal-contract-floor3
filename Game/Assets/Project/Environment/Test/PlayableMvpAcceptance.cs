using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TemporalContract.Combat;
using TemporalContract.Core;
using TemporalContract.Interactions;
using TemporalContract.Networking;
using TemporalContract.Players;
using TemporalContract.Rounds;
using TemporalContract.UI.Hud;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TemporalContract.Environment.Testing
{
    /// <summary>Explicit CLI-only single-host acceptance; never serialized into production Main.</summary>
    public sealed class PlayableMvpAcceptance : MonoBehaviour
    {
        [Serializable] private sealed class Report {public int checks,entrancesPassed;public bool passed;public string scope="Single-host real production Main, UI and gameplay. No two-client certification.";public List<string> failures=new List<string>();}
        private readonly Report report=new Report();
        private FullFloorSceneBindings binding;
        private ProductionPresentationAdapter adapter;
        private NetworkFpsPlayer player;
        private CharacterController body;
        private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(System.Environment.GetCommandLineArgs().Contains("--mvp-smoke-test"))new GameObject("MVP acceptance").AddComponent<PlayableMvpAcceptance>();
        }
        private void Check(bool value,string message)
        {
            report.checks++;if(!value)throw new InvalidOperationException(message);
            Debug.Log("MVP_CHECK_PASS "+message);
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            var routine=Run();
            while(true)
            {
                bool more=false;object current=null;Exception failure=null;
                try{more=routine.MoveNext();if(more)current=routine.Current;}catch(Exception error){failure=error;}
                if(failure!=null){report.failures.Add(failure.ToString());Debug.LogException(failure);break;}
                if(!more){report.passed=true;break;}
                yield return current;
            }
            string output=System.Environment.GetEnvironmentVariable("MVP_ACCEPTANCE_OUTPUT");
            if(!string.IsNullOrEmpty(output))System.IO.File.WriteAllText(output,JsonUtility.ToJson(report,true));
            Debug.Log("PLAYABLE_MVP_ACCEPTANCE "+JsonUtility.ToJson(report));
            if(NetworkManager.Singleton)NetworkManager.Singleton.Shutdown();
#if UNITY_EDITOR
            if(Application.isBatchMode)UnityEditor.EditorApplication.Exit(report.passed?0:2);
            else UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit(report.passed?0:2);
#endif
        }
        private IEnumerator Run()
        {
            yield return null;yield return null;
            binding=FindFirstObjectByType<FullFloorSceneBindings>();adapter=FindFirstObjectByType<ProductionPresentationAdapter>();
            Check(binding&&adapter,"Production binding/provider present");
            Check(binding.hud.CurrentPhase==SessionPhase.Disconnected,"Real main menu appears before opt-in startup");
            Check(!NetworkManager.Singleton.IsListening,"No automatic network startup");
            Capture("01-menu.png");yield return new WaitForSecondsRealtime(.15f);
            var port=FindObjectsByType<InputField>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(f=>f.name=="PortInput");port.text="47979";
            var practice=FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(b=>b.name=="PracticeButton");
            Check(practice.gameObject.activeInHierarchy&&practice.interactable,"Solo practice button available");practice.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            player=NetworkFpsPlayer.LocalOwner;body=player?player.GetComponent<CharacterController>():null;
            var manager=NetworkManager.Singleton;var rounds=RoundCoordinator.Instance;
            Check(manager.IsHost&&player&&rounds&&rounds.IsPractice&&rounds.IsGameplayActive,"Practice starts real host/player/round");
            Check(binding.hud.CurrentPhase==SessionPhase.ActiveRound,"HUD enters gameplay from the actual practice button");
            Check(Mathf.Abs(body.height-1.8f)<.001f&&Mathf.Abs(body.radius-.3f)<.001f&&Mathf.Abs(body.center.y-.9f)<.001f,"Production capsule uses floor dimensions");
            Check(Vector3.Distance(player.Position,binding.attackerSpawn.position)<.2f,"Initial player uses scene spawn");
            Check(VipHealth.Instance&&Vector3.Distance(VipHealth.Instance.transform.position,binding.vipSpawn.position)<.01f,"Actual VIP uses staff-cabin pose");
            Check(binding.doors.All(d=>d.IsSpawned)&&binding.restrictedDoor.IsLocked,"All 24 source doors spawn with cabin locked");
            Check(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled&&l.gameObject.activeInHierarchy)==1,"Exactly one owner listener");
            Check(FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled&&c.gameObject.activeInHierarchy)==1,"Exactly one owner camera");
            var view=player.GetComponentInChildren<LocalWeaponView>(true);
            Check(view&&view.RecoilPivot&&view.RecoilPivot.GetComponentsInChildren<Renderer>(true).Length>=4,"Visible local weapon geometry");
            Capture("02-gameplay.png");yield return new WaitForSecondsRealtime(.15f);
            foreach(var board in binding.boards)
            {
                board.RequestInteraction(new InteractionRequest(player.OwnerClientId,null));
                Check(binding.map.IsOpen&&player.IsGameplayInputCaptured,"Board captures real gameplay: "+board.name);
                for(int i=0;i<11;i++)binding.map.Select(i);
                binding.map.Close();Check(!player.IsGameplayInputCaptured,"Board releases actual player input");
            }
            var weapon=player.GetComponent<NetworkHitscanWeapon>();
            Check(weapon.Ammunition==12,"Real magazine starts full");weapon.RequestFire();
            yield return new WaitForSecondsRealtime(.1f);
            adapter.TryGetState(out var state);Check(weapon.Ammunition==11&&state.ammo==11&&state.health==100,"Authoritative shot updates HUD ammunition/health");
            weapon.RequestReload();yield return new WaitForSecondsRealtime(.1f);
            adapter.TryGetState(out state);Check(weapon.IsReloading&&state.reloading,"Reload state reaches HUD");
            yield return new WaitForSecondsRealtime(1.6f);Check(weapon.Ammunition==12&&!weapon.IsReloading,"Real reload completes");
            Place(binding.keycard.transform.position+Vector3.right*.7f);
            Check(Request(binding.keycard)&&binding.keycard.IsHeldBy(player.OwnerClientId),"Actual attacker collects the scene keycard");
            Place(binding.accessPanel.transform.position+Vector3.forward*.7f);
            Check(Request(binding.accessPanel)&&binding.accessPanel.IsGranted&&!binding.restrictedDoor.IsLocked,"Card grants the scene access panel and unlocks cabin");
            foreach(var door in binding.doors)
            {
                var origin=(Transform)new FieldReference(door).Reference("interactionPoint");
                Place(origin.position+Vector3.forward*.7f);
                Check(Request(door)&&door.IsOpen,"Server opens source door "+door.name);
            }
            yield return new WaitForSecondsRealtime(.7f);
            // Use the exact production movement entry point and controller, not the inspection controller.
            var manifest=JsonUtility.FromJson<ProbeManifest>(ResourcesManifest());
            var move=typeof(NetworkFpsPlayer).GetMethod("MoveHorizontal",Private);
            float speed=(float)typeof(NetworkFpsPlayer).GetField("moveSpeed",Private).GetValue(player);
            int oldLayer=player.gameObject.layer;player.gameObject.layer=2;
            try
            {
                foreach(var door in manifest.objects.Where(d=>d.source.StartsWith("Door.",StringComparison.Ordinal)))
                {
                    Vector3 threshold=new Vector3((door.min[0]+door.max[0])*.5f-38.98f,.08f,(door.min[1]+door.max[1])*.5f+1);
                    Vector3 direction=door.max[0]-door.min[0]>door.max[1]-door.min[1]?Vector3.forward:Vector3.right;
                    float low=door.source=="Door.012"?.2f:.6f,high=door.source=="Door.014"?.2f:.6f;
                    foreach(float sign in new[]{-1f,1f})
                    {
                        var start=threshold+direction*(sign>0?-low:high);var target=threshold+direction*(sign>0?high:-low);
                        start=Ground(start);target=Ground(target);Place(start);
                        player.transform.rotation=Quaternion.LookRotation(direction*sign);
                        for(int i=0;i<30;i++)move.Invoke(player,new object[]{Vector2.up*((low+high)/(30*.016f*speed)),.016f});
                        float error=Vector2.Distance(new Vector2(player.Position.x,player.Position.z),new Vector2(target.x,target.z));
                        Check(error<.12f,"Actual production doorway "+door.source+" direction="+sign+" error="+error);
                        report.entrancesPassed++;
                    }
                }
            }
            finally {player.gameObject.layer=oldLayer;}
            Place(binding.lightSwitch.transform.position+Vector3.right);
            Check(Request(binding.lightSwitch),"Healthy active actor interaction allowed");
            var health=player.GetComponent<NetworkPlayerHealth>();
            binding.map.Open();
            Check(health.ApplyDamage(100,12345),"Actual health death path executes");
            Check(!Request(binding.lightSwitch),"Dead actor is denied on the server");
            yield return new WaitForSecondsRealtime(.15f);
            Check(binding.hud.CurrentPhase==SessionPhase.RoundEnd,"Death drives real round-end HUD");
            Check(!binding.map.IsOpen,"Death closes the board map before results");
            Check(!Request(binding.lightSwitch),"Between-round actor is denied on the server");
            yield return new WaitForSecondsRealtime(3.1f);
            Check(rounds.Phase==MatchPhase.MatchResult&&binding.hud.CurrentPhase==SessionPhase.MatchEnd,"Practice reaches match results without a second client");
            adapter.RequestRematch();yield return new WaitForSecondsRealtime(.2f);
            Check(rounds.IsGameplayActive&&health.IsAlive&&weapon.Ammunition==12,"Actual practice restart restores player and ammunition");
            Check(!binding.keycard.IsCollected&&!binding.accessPanel.IsGranted&&binding.restrictedDoor.IsLocked&&binding.doors.All(d=>!d.IsOpen),"Restart resets card/panel/doors");
            Check(Vector3.Distance(player.Position,binding.attackerSpawn.position)<.2f,"Restart uses scene spawn again");
            // A bounded shooting fixture aims the real host weapon at the real scene VIP.
            var vip=VipHealth.Instance;Place(vip.transform.position+Vector3.forward*2.5f);
            player.transform.rotation=Quaternion.Euler(0,180,0);
            typeof(NetworkFpsPlayer).GetField("pitch",Private).SetValue(player,Mathf.Atan2(.72f,2.5f)*Mathf.Rad2Deg);
            int hits=0;weapon.HitConfirmed+=_=>hits++;
            for(int i=0;i<4;i++){weapon.RequestFire();yield return new WaitForSecondsRealtime(.3f);}
            Check(!vip.IsAlive&&hits==4&&weapon.Ammunition==8,"Real hitscan kills VIP with four confirmed shots");
            Check(rounds.LastRoundOutcome==RoundOutcome.AttackersWon,"VIP damage drives authoritative attacker win");
            yield return new WaitForSecondsRealtime(3.1f);
            Check(binding.hud.CurrentPhase==SessionPhase.MatchEnd,"Winning result reaches HUD");
            Capture("03-results.png");yield return new WaitForSecondsRealtime(.15f);
            adapter.Disconnect();yield return new WaitForSecondsRealtime(.3f);
            Check(!manager.IsListening&&binding.hud.CurrentPhase==SessionPhase.Disconnected,"Disconnect restores the real menu");
        }
        private bool Request(NetworkInteractable target) => target.TryHandleInteraction(new InteractionRequest(player.OwnerClientId,target.NetworkObjectId));
        private void Place(Vector3 value)
        {
            value.y=Mathf.Max(.065f,value.y>1?.065f:value.y);
            body.enabled=false;player.transform.position=value;body.enabled=true;
            typeof(NetworkFpsPlayer).GetField("verticalSpeed",Private).SetValue(player,0f);Physics.SyncTransforms();
        }
        private Vector3 Ground(Vector3 value)
        {
            body.enabled=false;
            if(Physics.Raycast(value+Vector3.up*.8f,Vector3.down,out RaycastHit hit,1.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.normal.y>.8f&&hit.collider.name.StartsWith("Surface -",StringComparison.Ordinal))value.y=hit.point.y+.065f;
            return value;
        }
        [Serializable]private sealed class ProbeObject {public string source;public float[] min,max;}
        [Serializable]private sealed class ProbeManifest {public ProbeObject[] objects;}
        private string ResourcesManifest()
        {
            if(!binding.sourceManifest)throw new InvalidOperationException("Missing full-floor source manifest reference.");
            return binding.sourceManifest.text;
        }
        private void Capture(string name)
        {
            string directory=System.Environment.GetEnvironmentVariable("MVP_SCREENSHOT_DIRECTORY");
            if(!string.IsNullOrEmpty(directory)&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {System.IO.Directory.CreateDirectory(directory);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,name));}
        }
        // Private serialized base fields are inspected only by this opt-in regression.
        private sealed class FieldReference
        {
            private readonly object target;public FieldReference(object value){target=value;}
            public UnityEngine.Object Reference(string name)=> (UnityEngine.Object)typeof(NetworkInteractable).GetField(name,Private).GetValue(target);
        }
    }
}
