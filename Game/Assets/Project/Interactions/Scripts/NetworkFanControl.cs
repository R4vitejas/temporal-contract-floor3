using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>One room control drives its existing ceiling rotors and a quiet spatial motor sound.</summary>
    public sealed class NetworkFanControl : NetworkInteractable
    {
        public Transform[] rotors=System.Array.Empty<Transform>();
        private readonly NetworkVariable<bool> running=new NetworkVariable<bool>(false);
        private Vector3[] pivots,positions;
        private Quaternion[] rotations;
        private AudioSource motor;
        private static AudioClip hum;
        public bool IsRunning=>running.Value;
        private void Awake()
        {
            pivots=new Vector3[rotors.Length];positions=new Vector3[rotors.Length];rotations=new Quaternion[rotors.Length];
            for(int i=0;i<rotors.Length;i++){positions[i]=rotors[i].position;rotations[i]=rotors[i].rotation;pivots[i]=rotors[i].GetComponent<Renderer>().bounds.center;}
            motor=gameObject.AddComponent<AudioSource>();motor.playOnAwake=false;motor.loop=true;motor.spatialBlend=1;motor.volume=.08f;motor.minDistance=1;motor.maxDistance=9;motor.dopplerLevel=0;
            if(!hum){var data=new float[22050];for(int i=0;i<data.Length;i++)data[i]=.3f*Mathf.Sin(2*Mathf.PI*90*i/22050f)+.1f*Mathf.Sin(2*Mathf.PI*180*i/22050f);hum=AudioClip.Create("Ceiling fan motor",data.Length,1,22050,false);hum.SetData(data,0);}motor.clip=hum;
        }
        public override void OnNetworkSpawn(){if(IsServer)running.Value=false;running.OnValueChanged+=Changed;Changed(false,IsRunning);}
        public override void OnNetworkDespawn(){running.OnValueChanged-=Changed;if(motor)motor.Stop();Restore();}
        private void Changed(bool before,bool after){if(after)motor.Play();else motor.Stop();}
        private void Update(){if(!IsSpawned||!IsRunning)return;foreach(var rotor in rotors)if(rotor){int i=System.Array.IndexOf(rotors,rotor);rotor.RotateAround(pivots[i],Vector3.up,Time.deltaTime*280);}}
        private void Restore(){for(int i=0;i<rotors.Length;i++)if(rotors[i]){rotors[i].position=positions[i];rotors[i].rotation=rotations[i];}}
        protected override bool TryBuildPrompt(IInteractionActor actor,out InteractionPrompt prompt){prompt=new InteractionPrompt(IsRunning?"Stop ceiling fans":"Start ceiling fans","E",true);return true;}
        protected override bool ExecuteOnServer(in InteractionRequest request,NetworkObject player){running.Value=!running.Value;BroadcastFeedback(FeedbackKind.LightToggled,IsRunning?1:0);return true;}
        protected override void ResetState(){running.Value=false;Restore();}
    }
}
