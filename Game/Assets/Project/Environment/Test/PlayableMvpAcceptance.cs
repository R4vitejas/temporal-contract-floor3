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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TemporalContract.Feedback;

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
        private Keyboard keyboard;
        private float originalVolume,originalSensitivity,originalFov;
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
            originalVolume=GamePreferences.Volume;originalSensitivity=GamePreferences.Sensitivity;originalFov=GamePreferences.FieldOfView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>("ProductionAcceptanceKeyboard");
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
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            GamePreferences.Volume=originalVolume;GamePreferences.Sensitivity=originalSensitivity;GamePreferences.FieldOfView=originalFov;GamePreferences.Save();
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
            var audio=FindFirstObjectByType<LocalAudioDirector>();var guard=FindFirstObjectByType<SingleAudioListenerGuard>();
            Check(audio&&audio.menuListener.enabled&&audio.menuCamera.enabled&&guard.Check()==1,"Menu has exactly one explicit listener and view");
            var controls=FindFirstObjectByType<GameMenuController>();var uiSound=FindFirstObjectByType<InterfaceAudio>();
            int clicks=uiSound.ClicksPlayed;controls.settingsButtons[0].onClick.Invoke();yield return null;
            Check(controls.IsOpen&&uiSound.ClicksPlayed==clicks+1,"Settings button opens controls and plays one UI cue");
            controls.volume.value=.35f;controls.sensitivity.value=.16f;controls.fov.value=78;
            Check(Mathf.Abs(AudioListener.volume-.35f)<.001f&&Mathf.Abs(GamePreferences.Sensitivity-.16f)<.001f,"Real audio and sensitivity controls apply");
            controls.volume.value=originalVolume;controls.sensitivity.value=originalSensitivity;controls.fov.value=originalFov;
            controls.resume.onClick.Invoke();yield return null;Check(!controls.IsOpen,"Settings returns to main menu");
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
            Check(audio.UsingPlayerView&&!audio.menuListener.enabled&&!audio.menuCamera.enabled&&guard.Check()==1,"Menu audio/view hands off to the actual owner without duplicates");
            controls.OpenSettings();yield return null;
            Check(controls.IsOpen&&player.IsGameplayInputCaptured,"Session controls capture movement and fire");
            controls.fov.value=80;Check(Mathf.Abs(player.GetComponentInChildren<Camera>().fieldOfView-80)<.01f,"Field-of-view control changes the actual owner camera");
            controls.fov.value=originalFov;controls.resume.onClick.Invoke();yield return null;
            Check(!controls.IsOpen&&!player.IsGameplayInputCaptured,"Continue releases the existing session capture owner");
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
            binding.map.Open();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
            Check(!binding.map.IsOpen&&!controls.IsOpen&&!player.IsGameplayInputCaptured,"Escape closes the board without also opening the session menu");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
            Check(controls.IsOpen&&player.IsGameplayInputCaptured,"Escape opens the real session menu once");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F1));yield return null;yield return null;
            Check(!controls.IsOpen&&!player.IsGameplayInputCaptured,"F1 closes the real session menu once");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            var playerMove=typeof(NetworkFpsPlayer).GetMethod("MoveHorizontal",Private);int beforeSteps=player.GetComponent<PlayerFootsteps>().PlayedSteps;
            for(int i=0;i<25;i++){playerMove.Invoke(player,new object[]{Vector2.up,.016f});yield return null;}
            Check(player.GetComponent<PlayerFootsteps>().PlayedSteps>beforeSteps,"Actual grounded travel produces spatial foot-contact audio");
            PlaceNear(binding.movableCover);
            Check(Request(binding.movableCover)&&binding.movableCover.IsHeldBy(player.OwnerClientId),"Actual cover pickup records the host-owned holder");
            Place(binding.attackerSpawn.position);player.transform.rotation=binding.attackerSpawn.rotation;yield return null;yield return null;
            var router=player.GetComponent<InteractionRouter>();
            Check(ReferenceEquals(typeof(InteractionRouter).GetField("currentTarget",Private).GetValue(router),binding.movableCover),"Held cover remains the placement target above the camera ray");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return null;yield return null;
            Check(!binding.movableCover.IsHeld,"E places the held cover through the production router");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            var weapon=player.GetComponent<NetworkHitscanWeapon>();
            Check(weapon.Ammunition==12,"Real magazine starts full");weapon.RequestFire();
            yield return new WaitForSecondsRealtime(.1f);
            adapter.TryGetState(out var state);Check(weapon.Ammunition==11&&state.ammo==11&&state.health==100,"Authoritative shot updates HUD ammunition/health");
            weapon.RequestReload();yield return new WaitForSecondsRealtime(.1f);
            adapter.TryGetState(out state);Check(weapon.IsReloading&&state.reloading,"Reload state reaches HUD");
            yield return new WaitForSecondsRealtime(1.6f);Check(weapon.Ammunition==12&&!weapon.IsReloading,"Real reload completes");
            PlaceNear(binding.keycard);
            Check(Request(binding.keycard)&&binding.keycard.IsHeldBy(player.OwnerClientId),"Actual attacker collects the scene keycard");
            PlaceNear(binding.accessPanel);
            Check(Request(binding.accessPanel)&&binding.accessPanel.IsGranted&&!binding.restrictedDoor.IsLocked,"Card grants the scene access panel and unlocks cabin");
            foreach(var door in binding.doors)
            {
                PlaceNear(door);
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
            PlaceNear(binding.lightSwitch);
            Check(Request(binding.lightSwitch),"Healthy active actor interaction allowed");
            var point=binding.lightSwitch.GetComponentsInChildren<Collider>().First(c=>!c.isTrigger).bounds.center;
            var eye=player.GetComponentInChildren<Camera>().transform.position;
            var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.name="Regression sight barrier";barrier.transform.position=(eye+point)*.5f;barrier.transform.localScale=new Vector3(.16f,.8f,.16f);Physics.SyncTransforms();
            bool wasOn=binding.lightSwitch.IsOn;
            Check(!Request(binding.lightSwitch)&&binding.lightSwitch.IsOn==wasOn,"Server rejects a real interaction through intervening geometry");
            Destroy(barrier);yield return null;Physics.SyncTransforms();
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
            Check(!NetworkFpsPlayer.LocalOwner&&audio.menuListener.enabled&&audio.menuCamera.enabled&&guard.Check()==1,"Disconnect restores one menu listener with no stale owner");
            practice.onClick.Invoke();yield return new WaitForSecondsRealtime(.4f);
            Check(manager.IsHost&&RoundCoordinator.Instance.IsGameplayActive&&NetworkFpsPlayer.LocalOwner,"A fresh practice session starts again without Editor restart");
            Check(guard.Check()==1&&audio.UsingPlayerView,"Re-host keeps exactly one owner listener");
            adapter.Disconnect();yield return new WaitForSecondsRealtime(.3f);Check(guard.Check()==1&&!audio.UsingPlayerView,"Second disconnect returns audio ownership to the menu");
        }
        private bool Request(NetworkInteractable target) => target.TryHandleInteraction(new InteractionRequest(player.OwnerClientId,target.NetworkObjectId));
        private void PlaceNear(NetworkInteractable target)
        {
            var origin=(Transform)new FieldReference(target).Reference("interactionPoint");
            var collider=target.GetComponentsInChildren<Collider>().FirstOrDefault(c=>c.enabled&&!c.isTrigger);
            Vector3 point=origin?origin.position:collider?collider.bounds.center:target.transform.position;
            body.enabled=false;
            foreach(float distance in new[]{.8f,1.1f,.5f,.4f})
                foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left,new Vector3(1,0,1).normalized,new Vector3(-1,0,-1).normalized})
                {
                    Vector3 candidate=point+direction*distance;candidate.y=.065f;candidate=Ground(candidate);
                    if(Physics.OverlapCapsule(candidate+Vector3.up*.31f,candidate+Vector3.up*1.49f,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Length!=0)continue;
                    Vector3 eye=candidate+Vector3.up*1.62f,delta=point-eye;
                    if(Physics.Raycast(eye,delta.normalized,out var hit,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&!hit.collider.transform.IsChildOf(target.transform))continue;
                    Place(candidate);return;
                }
            throw new InvalidOperationException("No clear interaction approach for "+target.name);
        }
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
