using TemporalContract.Players;
using UnityEngine;

namespace TemporalContract.Feedback
{
    /// <summary>Presentation inferred from replicated travel; teleports and airborne motion do not emit steps.</summary>
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        private NetworkFpsPlayer player;
        private AudioSource source;
        private Vector3 previous;
        private float travelled;
        private int foot;
        public int PlayedSteps {get;private set;}
        private void Awake()
        {
            player=GetComponent<NetworkFpsPlayer>();source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.spatialBlend=1;source.minDistance=1;source.maxDistance=15;source.dopplerLevel=0;source.volume=.45f;
            source.rolloffMode=AudioRolloffMode.Logarithmic;previous=transform.position;
        }
        private void OnEnable(){previous=transform.position;travelled=0;}
        private void Update()
        {
            var position=transform.position;float distance=Vector2.Distance(new Vector2(position.x,position.z),new Vector2(previous.x,previous.z));previous=position;
            if(!player||!player.IsSpawned||distance>2f){travelled=0;return;}
            if(distance<.001f)return;
            if(!Physics.Raycast(position+Vector3.up*.15f,Vector3.down,.4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){travelled=0;return;}
            travelled+=distance;
            if(travelled<1.35f)return;travelled%=1.35f;source.pitch=(foot++%2==0)?1.03f:.97f;source.PlayOneShot(GameplayAudioClips.Footstep);PlayedSteps++;
        }
    }
}
