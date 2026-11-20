using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TemporalContract.Feedback
{
    public sealed class InterfaceAudio : MonoBehaviour
    {
        private readonly List<Button> buttons=new List<Button>();
        private AudioSource source;
        public int ClicksPlayed {get;private set;}
        private void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.45f;}
        private void OnEnable()
        {Rescan();}
        private void Start(){Rescan();}
        private void Rescan()
        {
            foreach(var button in buttons)if(button)button.onClick.RemoveListener(PlayClick);buttons.Clear();
            foreach(var button in transform.parent.GetComponentsInChildren<Button>(true))
            {buttons.Add(button);button.onClick.AddListener(PlayClick);}
        }
        private void OnDisable(){foreach(var button in buttons)if(button)button.onClick.RemoveListener(PlayClick);buttons.Clear();}
        public void PlayClick(){if(source){source.PlayOneShot(GameplayAudioClips.Click);ClicksPlayed++;}}
    }
}
