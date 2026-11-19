using TemporalContract.Core;
using TemporalContract.Players;
using UnityEngine;

namespace TemporalContract.Feedback
{
    /// <summary>Owns the menu-to-player view/listener handoff, including disconnect and re-host.</summary>
    public sealed class LocalAudioDirector : MonoBehaviour
    {
        public Camera menuCamera;
        public AudioListener menuListener;
        public bool UsingPlayerView {get;private set;}
        private void OnEnable(){NetworkFpsPlayer.LocalOwnerChanged+=OwnerChanged;AudioListener.volume=GamePreferences.Volume;Refresh();}
        private void OnDisable(){NetworkFpsPlayer.LocalOwnerChanged-=OwnerChanged;if(menuCamera)menuCamera.enabled=false;if(menuListener)menuListener.enabled=false;}
        private void OwnerChanged(NetworkFpsPlayer player)=>Refresh();
        private void LateUpdate()=>Refresh();
        public void Refresh()
        {
            var player=NetworkFpsPlayer.LocalOwner;
            var listener=player?player.GetComponentInChildren<AudioListener>(true):null;
            UsingPlayerView=player&&player.IsSpawned&&listener;
            if(menuListener)menuListener.enabled=!UsingPlayerView;
            if(menuCamera)menuCamera.enabled=!UsingPlayerView;
            if(UsingPlayerView)listener.enabled=true;
        }
    }
}
