using TemporalContract.Core;
using TemporalContract.Networking;
using TemporalContract.Players;
using TemporalContract.Rounds;
using TemporalContract.UI.Board;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TemporalContract.UI.Hud
{
    /// <summary>Session controls share the existing session capture owner; the authoritative round keeps running.</summary>
    public sealed class GameMenuController : MonoBehaviour
    {
        public LanSessionController session;
        public FloorMapPanel map;
        public GameObject panel;
        public Text title,status,volumeValue,sensitivityValue,fovValue;
        public Button resume,disconnect;
        public Slider volume,sensitivity,fov;
        public Button[] settingsButtons,quitButtons;
        public Button copyAddressButton;
        private float copiedUntil;
        private bool settingsOpen;
        public bool IsOpen=>panel&&panel.activeSelf;
        private void Awake()
        {
            volume.SetValueWithoutNotify(GamePreferences.Volume);sensitivity.SetValueWithoutNotify(GamePreferences.Sensitivity);fov.SetValueWithoutNotify(GamePreferences.FieldOfView);
            volume.onValueChanged.AddListener(SetVolume);sensitivity.onValueChanged.AddListener(SetSensitivity);fov.onValueChanged.AddListener(SetFieldOfView);
            RefreshValues();
            resume.onClick.AddListener(()=>SetOpen(false));disconnect.onClick.AddListener(Disconnect);
            foreach(var button in settingsButtons)button.onClick.AddListener(OpenSettings);
            foreach(var button in quitButtons)button.onClick.AddListener(Quit);
            if(copyAddressButton)copyAddressButton.onClick.AddListener(()=>{GUIUtility.systemCopyBuffer=session.ConnectionAddress;copiedUntil=Time.unscaledTime+1.5f;});
        }
        private void LateUpdate()
        {
            var keys=Keyboard.current;var rounds=RoundCoordinator.Instance;
            bool active=rounds&&rounds.IsGameplayActive;
            if(keys!=null)
            {
                if(keys.f1Key.wasPressedThisFrame && active)SetOpen(!session.IsMenuVisible);
                else if(keys.escapeKey.wasPressedThisFrame)
                {
                    if(map&&map.IsOpen)map.Close();
                    else if(map&&map.LastClosedFrame==Time.frameCount){}
                    else if(settingsOpen||active)SetOpen(!IsOpen);
                }
            }
            bool shown=settingsOpen || (active&&session.IsMenuVisible);
            if(copyAddressButton)copyAddressButton.GetComponentInChildren<Text>().text=Time.unscaledTime<copiedUntil?"COPIED":"COPY ADDRESS";
            if(settingsOpen&&active&&!session.IsMenuVisible)session.SetMenuVisible(true);
            if(panel.activeSelf!=shown)panel.SetActive(shown);
            if(shown)
            {
                title.text=active?"SESSION":"SETTINGS";
                status.text=active?"The round timer continues while this menu is open.":"Audio and view preferences are saved on this computer.";
                if(disconnect)disconnect.gameObject.SetActive(NetworkManager.Singleton&&NetworkManager.Singleton.IsListening);
                resume.GetComponentInChildren<Text>().text=active?"CONTINUE":"BACK";
            }
        }
        public void OpenSettings(){settingsOpen=true;if(NetworkFpsPlayer.LocalOwner)session.SetMenuVisible(true);panel.SetActive(true);}
        public void SetOpen(bool open)
        {
            bool active=RoundCoordinator.Instance&&RoundCoordinator.Instance.IsGameplayActive;
            settingsOpen=open&&!active;
            if(active)
            {
                session.SetMenuVisible(open);
                if(open&&map)map.ForceClose();
            }
            else if(!open&&NetworkFpsPlayer.LocalOwner)session.SetMenuVisible(false);
            panel.SetActive(open);if(!open)GamePreferences.Save();
        }
        public void Disconnect(){settingsOpen=false;panel.SetActive(false);session.Disconnect();GamePreferences.Save();}
        public void Quit()
        {
            GamePreferences.Save();
            if(session&&NetworkManager.Singleton&&NetworkManager.Singleton.IsListening)session.Disconnect();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        public void SetVolume(float value){GamePreferences.Volume=value;RefreshValues();}
        public void SetSensitivity(float value){GamePreferences.Sensitivity=value;NetworkFpsPlayer.LocalOwner?.ApplyViewPreferences();RefreshValues();}
        public void SetFieldOfView(float value){GamePreferences.FieldOfView=value;NetworkFpsPlayer.LocalOwner?.ApplyViewPreferences();RefreshValues();}
        private void RefreshValues(){if(volumeValue)volumeValue.text=Mathf.RoundToInt(GamePreferences.Volume*100)+"%";if(sensitivityValue)sensitivityValue.text=GamePreferences.Sensitivity.ToString("0.00");if(fovValue)fovValue.text=Mathf.RoundToInt(GamePreferences.FieldOfView)+"°";}
        private void OnDisable(){if(panel)panel.SetActive(false);settingsOpen=false;if(session&&NetworkFpsPlayer.LocalOwner)session.SetMenuVisible(false);GamePreferences.Save();}
    }
}
