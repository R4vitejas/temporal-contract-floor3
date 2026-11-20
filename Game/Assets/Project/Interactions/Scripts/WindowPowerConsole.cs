using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>A shared, round-scoped prerequisite for the VIP window motor.</summary>
    public sealed class WindowPowerConsole : NetworkInteractable
    {
        public Renderer indicator;
        private readonly NetworkVariable<bool> powered = new NetworkVariable<bool>(false);
        public bool IsPowered => powered.Value;
        public override void OnNetworkSpawn(){if(IsServer)powered.Value=false;powered.OnValueChanged+=Changed;Paint();}
        public override void OnNetworkDespawn(){powered.OnValueChanged-=Changed;}
        private void Changed(bool before,bool after)=>Paint();
        private void Paint()
        {
            if(!indicator)return;
            var block=new MaterialPropertyBlock();var color=IsPowered?new Color(.2f,1,.55f):new Color(1,.33f,.08f);
            block.SetColor("_BaseColor",color);block.SetColor("_Color",color);block.SetColor("_EmissionColor",color*2);indicator.SetPropertyBlock(block);
        }
        protected override bool TryBuildPrompt(IInteractionActor actor,out InteractionPrompt prompt)
        {prompt=new InteractionPrompt(IsPowered?"VIP window power enabled":"Enable VIP window motors",IsPowered?"Staff Cabin · west wing":"E",!IsPowered);return true;}
        protected override bool ExecuteOnServer(in InteractionRequest request,NetworkObject player)
        {
            if(IsPowered)return Deny("window power already enabled");
            powered.Value=true;BroadcastFeedback(FeedbackKind.InteractionAccepted);return true;
        }
        protected override void ResetState(){powered.Value=false;}
    }
}
