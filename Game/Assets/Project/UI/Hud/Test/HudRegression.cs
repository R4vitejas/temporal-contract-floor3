using System.Collections;
using TemporalContract.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TemporalContract.UI.Hud.Testing
{
    /// <summary>
    /// TEST-ONLY headless Play Mode regression for the HUD/session UI. Prints HUD_REGRESSION PASS/FAIL lines and
    /// exits 0 on pass, 2 on failure. Button presses call Button.onClick, the same path a click takes.
    /// </summary>
    public sealed class HudRegression : MonoBehaviour
    {
        [SerializeField] private HudSessionUi ui;
        [SerializeField] private MockPresentationProvider provider;

        private bool failed;
        private int errorLogs;

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errorLogs++;
            }
        }

        private IEnumerator Start()
        {
            yield return null;

            // no provider at all: neutral placeholder, no exception
            ui.Bind(null, null);
            Check("no provider shows the neutral placeholder", Active("NoStatePanel") && !Active("HudRoot") && !Active("MainMenuPanel"));
            ui.Bind(provider, provider);
            provider.ApplyPreset(0);
            Check("provider without state shows the neutral placeholder", Active("NoStatePanel"));

            // every state
            provider.ApplyPreset(1);
            Check("disconnected shows menu with status", Active("MainMenuPanel") && T("MenuStatusText").Contains("host left"));
            provider.ApplyPreset(2);
            Check("connecting disables host/join", Active("MainMenuPanel") && !B("HostButton").interactable
                && !B("JoinButton").interactable && T("MenuStatusText") == "Connecting...");
            provider.ApplyPreset(3);
            Check("lobby waiting: 1/2 players, start disabled", Active("LobbyPanel") && T("LobbyPlayersText") == "Players: 1/2"
                && B("StartButton").gameObject.activeSelf && !B("StartButton").interactable);
            provider.ApplyPreset(4);
            Check("lobby both ready: host can start", B("StartButton").interactable && T("LobbyReadyText").Contains("READY"));
            provider.ApplyPreset(5);
            Check("active attacker HUD", Active("HudRoot") && T("RoleText") == "ATTACKER" && T("HealthText") == "100/100"
                && T("AmmoText") == "12/12" && T("TimerText") == "01:30" && T("RoundText") == "Round 1/2"
                && T("KeycardText") == "Keycard: not held" && T("ObjectiveText").Contains("VIP") && !Active("DefeatedOverlay"));
            provider.ApplyPreset(6);
            Check("defender low health and timer rounding", T("RoleText") == "DEFENDER" && T("HealthText") == "20/100"
                && Mathf.Approximately(Fill("HealthFill").fillAmount, 0.2f) && T("TimerText") == "00:38");
            provider.ApplyPreset(7);
            Check("defeated overlay shown", Active("HudRoot") && Active("DefeatedOverlay"));
            provider.ApplyPreset(8);
            Check("reloading shown", T("AmmoText") == "RELOADING" && T("KeycardText") == "Keycard: HELD");
            provider.ApplyPreset(9);
            Check("round end shows result and score", Active("RoundEndPanel") && T("RoundResultText").Contains("Defender wins")
                && T("RoundScoreText").Contains("0") && T("RoundScoreText").Contains("1"));
            provider.ApplyPreset(10);
            Check("match end victory", Active("MatchEndPanel") && T("MatchResultText") == "VICTORY" && B("RematchButton").interactable);
            provider.ApplyPreset(11);
            Check("match end draw", T("MatchResultText") == "DRAW");
            provider.ApplyPreset(12);
            Check("rematch waiting disables the button", !B("RematchButton").interactable
                && T("RematchButtonText") == "Waiting for opponent...");
            provider.ApplyPreset(13);
            Check("error state shows message", Active("ErrorPanel") && T("ErrorText").Contains("unreachable"));

            // callbacks and input validation
            provider.ApplyPreset(1);
            provider.Calls.Clear();
            SetInput("PortInput", "abc");
            B("HostButton").onClick.Invoke();
            Check("invalid port is rejected with a message", provider.Calls.Count == 0 && T("MenuStatusText").Contains("port"));
            SetInput("PortInput", "7777");
            SetInput("AddressInput", "not-an-ip");
            B("JoinButton").onClick.Invoke();
            Check("invalid address is rejected", provider.Calls.Count == 0 && T("MenuStatusText").Contains("IPv4"));
            SetInput("AddressInput", "192.168.1.20");
            B("JoinButton").onClick.Invoke();
            Check("join passes address and port", provider.Calls.Contains("Join 192.168.1.20:7777") && Active("LobbyPanel"));
            provider.ApplyPreset(1);
            B("HostButton").onClick.Invoke();
            Check("host passes port and enters the lobby", provider.Calls.Contains("Host 7777") && Active("LobbyPanel"));
            B("ReadyButton").onClick.Invoke();
            Check("ready toggles on", provider.Calls.Contains("Ready True") && T("ReadyButtonText") == "Cancel Ready");
            B("ReadyButton").onClick.Invoke();
            Check("ready toggles off", provider.Calls.Contains("Ready False") && T("ReadyButtonText") == "Ready");
            provider.ApplyPreset(4);
            B("StartButton").onClick.Invoke();
            Check("start is forwarded", provider.Calls.Contains("Start"));
            B("LobbyDisconnectButton").onClick.Invoke();
            Check("disconnect is forwarded and returns to the menu", provider.Calls.Contains("Disconnect") && Active("MainMenuPanel"));
            provider.ApplyPreset(10);
            B("RematchButton").onClick.Invoke();
            Check("rematch is forwarded and disables the button", provider.Calls.Contains("Rematch") && !B("RematchButton").interactable);

            // missing actions: ignored politely
            ui.Bind(provider, null);
            provider.ApplyPreset(1);
            SetInput("PortInput", "7777");
            int callsBefore = provider.Calls.Count;
            B("HostButton").onClick.Invoke();
            Check("missing actions are ignored without error", provider.Calls.Count == callsBefore && Active("MessageText"));
            ui.Bind(provider, provider);

            // runtime bindings must survive disable/enable (review of #22)
            provider.ApplyPreset(4);
            ui.enabled = false;
            ui.enabled = true;
            Check("state survives disable/enable", Active("LobbyPanel"));
            provider.ApplyPreset(1);
            Check("provider updates still reach the HUD after re-enable", Active("MainMenuPanel") && !Active("LobbyPanel"));
            provider.Calls.Clear();
            SetInput("PortInput", "7777");
            B("HostButton").onClick.Invoke();
            Check("button callbacks still work after re-enable", provider.Calls.Contains("Host 7777"));
            ui.BindRouter(null);

            // prompt and short messages
            ui.SetPrompt(new InteractionPrompt("Open door", "E", true));
            Check("prompt shows label and hint", Active("PromptText") && T("PromptText").Contains("Open door") && T("PromptText").Contains("E"));
            ui.SetPrompt(default);
            Check("prompt clears when the target is lost", !Active("PromptText"));
            ui.ShowMessage("Access granted");
            Check("message shows", Active("MessageText") && T("MessageText") == "Access granted");
            yield return new WaitForSecondsRealtime(2.8f);
            Check("message expires", !Active("MessageText"));

            // legibility at 1280x720
            var scaler = ui.GetComponent<CanvasScaler>();
            Check("canvas scales from a 1280x720 reference", scaler != null
                && scaler.referenceResolution == new Vector2(1280f, 720f));
            bool legible = true;
            foreach (Text text in ui.GetComponentsInChildren<Text>(true))
            {
                if (text.fontSize < 18 && text.GetComponentInParent<InputField>() == null)
                {
                    legible = false;
                    Debug.Log($"HUD_REGRESSION small text: {text.name} {text.fontSize}");
                }
            }

            Check("all text is at least 18 px on the 720p reference", legible);
            Check("no error or exception was logged", errorLogs == 0);
            Finish();
        }

        // ---------- helpers ----------

        private Transform Find(string name)
        {
            foreach (Transform t in ui.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        private bool Active(string name)
        {
            Transform t = Find(name);
            return t != null && t.gameObject.activeSelf;
        }

        private string T(string name) => Find(name)?.GetComponent<Text>()?.text ?? string.Empty;
        private Button B(string name) => Find(name).GetComponent<Button>();
        private Image Fill(string name) => Find(name).GetComponent<Image>();
        private void SetInput(string name, string value) => Find(name).GetComponent<InputField>().text = value;

        private void Check(string name, bool ok)
        {
            if (!ok)
            {
                failed = true;
            }

            Debug.Log($"HUD_REGRESSION {(ok ? "PASS" : "FAIL")}: {name}");
        }

        private void Finish()
        {
            Debug.Log($"HUD_REGRESSION RESULT: {(failed ? "FAIL" : "PASS")}");
            int code = failed ? 2 : 0;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
