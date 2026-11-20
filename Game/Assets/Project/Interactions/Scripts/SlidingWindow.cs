using TemporalContract.Core;
using TemporalContract.Players;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>Server-owned sliding glass; the pane and its shot-blocking collider travel together.</summary>
    public sealed class SlidingWindow : NetworkInteractable
    {
        public WindowPowerConsole power;
        public Transform slidingPane;
        public Vector3 travel=new Vector3(0,0,-1.12f);
        public float duration=.8f;
        private Vector3 closed;
        private readonly NetworkVariable<bool> opened=new NetworkVariable<bool>(false);
        private readonly NetworkVariable<double> movedAt=new NetworkVariable<double>(-1000);
        public bool IsOpen=>opened.Value;
        public bool IsMoving=>IsSpawned&&NetworkManager.ServerTime.Time-movedAt.Value<duration;
        private void Awake(){closed=slidingPane.localPosition;}
        public override void OnNetworkSpawn(){if(IsServer){opened.Value=false;movedAt.Value=-1000;}Apply();}
        private void Update(){if(IsSpawned)Apply();}
        private void Apply()
        {
            float progress=Mathf.Clamp01((float)(NetworkManager.ServerTime.Time-movedAt.Value)/Mathf.Max(.1f,duration));
            progress=progress*progress*(3-2*progress);
            slidingPane.localPosition=closed+travel*(IsOpen?progress:1-progress);
            Physics.SyncTransforms();
        }
        protected override bool TryBuildPrompt(IInteractionActor actor,out InteractionPrompt prompt)
        {
            bool enabled=power&&power.IsPowered;
            prompt=new InteractionPrompt(!enabled?"VIP window — no power":IsMoving?"Window motor moving":IsOpen?"Close VIP window":"Slide VIP window open",!enabled?"Enable motors at corridor power console":"E",enabled&&!IsMoving);return true;
        }
        protected override bool ExecuteOnServer(in InteractionRequest request,NetworkObject player)
        {
            if(!power||!power.IsPowered)return Deny("enable VIP window motors at the corridor power console first");
            if(IsMoving)return Deny("window motor is moving");
            var collider=slidingPane.GetComponent<Collider>();
            var sweep=collider.bounds;sweep.Encapsulate(new Bounds(collider.bounds.center+(IsOpen?-travel:travel),collider.bounds.size));
            foreach(var hit in Physics.OverlapBox(sweep.center,sweep.extents,Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(hit.GetComponentInParent<NetworkFpsPlayer>())return Deny("stand clear of the sliding window");
            opened.Value=!opened.Value;movedAt.Value=NetworkManager.ServerTime.Time;
            BroadcastFeedback(FeedbackKind.DoorMoved,IsOpen?1:0);return true;
        }
        protected override void ResetState(){opened.Value=false;movedAt.Value=-1000;Apply();}
    }
}
