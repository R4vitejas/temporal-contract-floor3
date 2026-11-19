using System;
using System.Net;
using System.Net.Sockets;
using TemporalContract.Core;
using TemporalContract.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace TemporalContract.UI.Hud
{
    /// <summary>
    /// HUD and session/result screens. It renders a <see cref="PresentationState"/> from an
    /// <see cref="IPresentationProvider"/> and invokes <see cref="IPresentationActions"/> callbacks; it owns no
    /// score, authoritative time, health or win condition. Missing state or actions never throw: the UI shows a
    /// neutral placeholder and logs once.
    /// </summary>
    public sealed class HudSessionUi : MonoBehaviour
    {
        [Header("Bindings (assign a provider and actions; a mock is in Test/)")]
        [SerializeField] private MonoBehaviour providerComponent;
        [SerializeField] private MonoBehaviour actionsComponent;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject roundEndPanel;
        [SerializeField] private GameObject matchEndPanel;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private GameObject defeatedOverlay;
        [SerializeField] private GameObject noStatePanel;

        [Header("Main menu")]
        [SerializeField] private InputField addressInput;
        [SerializeField] private InputField portInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button practiceButton;
        [SerializeField] private Text menuStatusText;

        [Header("Lobby")]
        [SerializeField] private Text lobbyPlayersText;
        [SerializeField] private Text lobbyReadyText;
        [SerializeField] private Button readyButton;
        [SerializeField] private Text readyButtonText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button lobbyDisconnectButton;

        [Header("HUD")]
        [SerializeField] private Text roleText;
        [SerializeField] private Text healthText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text keycardText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text messageText;

        [Header("Results")]
        [SerializeField] private Text roundResultText;
        [SerializeField] private Text roundScoreText;
        [SerializeField] private Text matchResultText;
        [SerializeField] private Text matchScoreText;
        [SerializeField] private Text rematchStatusText;
        [SerializeField] private Button rematchButton;
        [SerializeField] private Text rematchButtonText;
        [SerializeField] private Button matchDisconnectButton;

        [Header("Error")]
        [SerializeField] private Text errorText;
        [SerializeField] private Button errorDisconnectButton;

        [Header("Look")]
        [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.25f, 0.2f);
        [SerializeField] private Color healthColor = new Color(0.35f, 0.85f, 0.4f);
        [SerializeField, Min(0.5f)] private float messageSeconds = 2.5f;

        private IPresentationProvider provider;
        private IPresentationActions actions;
        private PresentationState lastState;
        private bool hasState;
        private bool warnedNoProvider;
        private bool warnedNoActions;
        private float messageUntil;
        private InteractionRouter boundRouter;
        private IFeedbackEventSource boundFeedback;

        public SessionPhase? CurrentPhase => hasState ? lastState.phase : (SessionPhase?)null;

        private void Awake()
        {
            Wire(hostButton, OnHost);
            Wire(joinButton, OnJoin);
            Wire(practiceButton, OnPractice);
            Wire(readyButton, OnReady);
            Wire(startButton, () => Invoke(a => a.StartMatch()));
            Wire(lobbyDisconnectButton, () => Invoke(a => a.Disconnect()));
            Wire(rematchButton, () => Invoke(a => a.RequestRematch()));
            Wire(matchDisconnectButton, () => Invoke(a => a.Disconnect()));
            Wire(errorDisconnectButton, () => Invoke(a => a.Disconnect()));
            BindComponents();
        }

        private static void Wire(Button button, Action handler)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => handler());
            }
        }

        private void OnEnable()
        {
            BindComponents();
            if (provider != null)
            {
                provider.StateChanged -= Refresh;
                provider.StateChanged += Refresh;
            }

            // Runtime router/feedback bindings survive a disable/enable cycle.
            if (boundRouter != null)
            {
                boundRouter.PromptChanged -= SetPrompt;
                boundRouter.PromptChanged += SetPrompt;
            }

            if (boundFeedback != null)
            {
                boundFeedback.FeedbackRaised -= OnFeedback;
                boundFeedback.FeedbackRaised += OnFeedback;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (provider != null)
            {
                provider.StateChanged -= Refresh;
            }

            // Unsubscribe only; keep the references so OnEnable can re-subscribe.
            if (boundRouter != null)
            {
                boundRouter.PromptChanged -= SetPrompt;
            }

            if (boundFeedback != null)
            {
                boundFeedback.FeedbackRaised -= OnFeedback;
            }
        }

        private void BindComponents()
        {
            // Inspector references are only a default; a runtime Bind() always wins.
            if (provider == null)
            {
                provider = providerComponent as IPresentationProvider;
            }

            if (actions == null)
            {
                actions = actionsComponent as IPresentationActions;
            }
        }

        /// <summary>Attach or replace the provider/actions at runtime (production adapter or a mock).</summary>
        public void Bind(IPresentationProvider newProvider, IPresentationActions newActions)
        {
            if (provider != null)
            {
                provider.StateChanged -= Refresh;
            }

            provider = newProvider;
            actions = newActions;
            if (provider != null && isActiveAndEnabled)
            {
                provider.StateChanged += Refresh;
            }

            Refresh();
        }

        // ---------- prompt and short messages ----------

        public void BindRouter(InteractionRouter router)
        {
            if (boundRouter != null)
            {
                boundRouter.PromptChanged -= SetPrompt;
            }

            boundRouter = router;
            if (boundRouter != null)
            {
                boundRouter.PromptChanged += SetPrompt;
            }
        }

        /// <summary>Shows the interaction prompt; an empty label (target lost) clears it.</summary>
        public void SetPrompt(InteractionPrompt prompt)
        {
            if (promptText == null)
            {
                return;
            }

            bool has = !string.IsNullOrEmpty(prompt.Label);
            promptText.gameObject.SetActive(has);
            if (has)
            {
                string hint = string.IsNullOrEmpty(prompt.InputHint) ? string.Empty : $"  [{prompt.InputHint}]";
                promptText.text = prompt.Label + hint;
                promptText.color = prompt.CanInteract ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }
        }

        public void BindFeedback(IFeedbackEventSource source)
        {
            if (boundFeedback != null)
            {
                boundFeedback.FeedbackRaised -= OnFeedback;
            }

            boundFeedback = source;
            if (boundFeedback != null)
            {
                boundFeedback.FeedbackRaised += OnFeedback;
            }
        }

        private void OnFeedback(FeedbackEvent feedback)
        {
            switch (feedback.Kind)
            {
                case FeedbackKind.InteractionAccepted:
                    ShowMessage("Access granted");
                    break;
                case FeedbackKind.InteractionDenied:
                    ShowMessage("Can't do that");
                    break;
                case FeedbackKind.KeycardCollected:
                    ShowMessage("Keycard collected");
                    break;
            }
        }

        public void ShowMessage(string text)
        {
            if (messageText == null)
            {
                return;
            }

            messageText.text = text;
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            messageUntil = Time.unscaledTime + messageSeconds;
        }

        private void Update()
        {
            if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime >= messageUntil)
            {
                messageText.gameObject.SetActive(false);
            }
        }

        // ---------- rendering ----------

        public void Refresh()
        {
            if (provider == null)
            {
                WarnNoProvider("No presentation provider is bound.");
                ShowOnly(noStatePanel);
                hasState = false;
                return;
            }

            if (!provider.TryGetState(out PresentationState state))
            {
                WarnNoProvider("The presentation provider has no state yet.");
                ShowOnly(noStatePanel);
                hasState = false;
                return;
            }

            lastState = state;
            hasState = true;
            switch (state.phase)
            {
                case SessionPhase.Disconnected:
                case SessionPhase.Connecting:
                    RenderMenu(state);
                    break;
                case SessionPhase.Lobby:
                    RenderLobby(state);
                    break;
                case SessionPhase.ActiveRound:
                    RenderHud(state);
                    break;
                case SessionPhase.RoundEnd:
                    RenderRoundEnd(state);
                    break;
                case SessionPhase.MatchEnd:
                    RenderMatchEnd(state);
                    break;
                default:
                    RenderError(state);
                    break;
            }
        }

        private void WarnNoProvider(string message)
        {
            if (!warnedNoProvider)
            {
                warnedNoProvider = true;
                Debug.LogWarning($"[HUD] {message} Showing a neutral placeholder.", this);
            }
        }

        private void ShowOnly(GameObject active)
        {
            foreach (GameObject panel in new[] { mainMenuPanel, lobbyPanel, hudRoot, roundEndPanel, matchEndPanel, errorPanel, noStatePanel })
            {
                if (panel != null)
                {
                    panel.SetActive(panel == active);
                }
            }

            if (defeatedOverlay != null)
            {
                defeatedOverlay.SetActive(false);
            }
        }

        private void RenderMenu(PresentationState state)
        {
            ShowOnly(mainMenuPanel);
            bool connecting = state.phase == SessionPhase.Connecting;
            SetInteractable(hostButton, !connecting);
            SetInteractable(joinButton, !connecting);
            if(practiceButton) { practiceButton.gameObject.SetActive(actions is IPracticeActions);practiceButton.interactable=!connecting; }
            SetText(menuStatusText, connecting ? "Connecting..." : Fallback(state.statusMessage, "Not connected"));
        }

        private void RenderLobby(PresentationState state)
        {
            ShowOnly(lobbyPanel);
            SetText(lobbyPlayersText, $"Players: {state.connectedPlayers}/2");
            SetText(lobbyReadyText,
                $"You: {(state.localReady ? "READY" : "not ready")}    Opponent: {(state.remoteReady ? "READY" : "not ready")}");
            SetText(readyButtonText, state.localReady ? "Cancel Ready" : "Ready");
            if (startButton != null)
            {
                startButton.gameObject.SetActive(state.isHost);
                startButton.interactable = state.isHost && state.connectedPlayers == 2 && state.localReady && state.remoteReady;
            }
        }

        private void RenderHud(PresentationState state)
        {
            ShowOnly(hudRoot);
            SetText(roleText, state.role == PlayerRole.None ? string.Empty : state.role.ToString().ToUpperInvariant());
            SetText(healthText, $"{Mathf.Max(0, state.health)}/{Mathf.Max(1, state.maxHealth)}");
            if (healthFill != null)
            {
                float fraction = Mathf.Clamp01(state.health / (float)Mathf.Max(1, state.maxHealth));
                healthFill.fillAmount = fraction;
                healthFill.color = fraction <= 0.25f ? lowHealthColor : healthColor;
            }

            SetText(ammoText, state.reloading ? "RELOADING" : $"{Mathf.Max(0, state.ammo)}/{Mathf.Max(0, state.magazineSize)}");
            SetText(timerText, FormatTime(state.timeRemainingSeconds));
            SetText(roundText, state.isPractice ? "SOLO PRACTICE" : state.roundNumber > 0 ? $"Round {state.roundNumber}/2" : string.Empty);
            SetText(objectiveText, Fallback(state.objective, string.Empty));
            SetText(keycardText, !state.keycardKnown ? string.Empty : state.hasKeycard ? "Keycard: HELD" : "Keycard: not held");
            if (defeatedOverlay != null)
            {
                defeatedOverlay.SetActive(!state.alive);
            }
        }

        private void RenderRoundEnd(PresentationState state)
        {
            ShowOnly(roundEndPanel);
            SetText(roundResultText, Fallback(state.roundResult, "Round over"));
            SetText(roundScoreText, state.isPractice ? "Preparing practice results..." : $"You {state.localScore}  -  {state.remoteScore} Opponent");
        }

        private void RenderMatchEnd(PresentationState state)
        {
            ShowOnly(matchEndPanel);
            SetText(matchResultText, Fallback(state.matchResult, "MATCH OVER"));
            SetText(matchScoreText, state.isPractice ? "Solo practice · " + (state.localScore>0?"Objective completed":"Try again") : $"Successful attacks: You {state.localScore}  -  {state.remoteScore} Opponent");
            SetText(rematchButtonText, state.isPractice ? "Practice again" : state.localRematchReady ? "Waiting for opponent..." : "Ready for rematch");
            SetInteractable(rematchButton, !state.localRematchReady);
            SetText(rematchStatusText, state.remoteRematchReady ? "Opponent is ready for a rematch" : string.Empty);
        }

        private void RenderError(PresentationState state)
        {
            ShowOnly(errorPanel);
            SetText(errorText, Fallback(state.errorMessage, "Something went wrong"));
        }

        // ---------- callbacks ----------

        private void OnHost()
        {
            if (!TryParsePort(out ushort port))
            {
                SetText(menuStatusText, "Enter a port between 1 and 65535");
                return;
            }

            Invoke(a => a.Host(port));
        }

        private void OnJoin()
        {
            if (!TryParsePort(out ushort port))
            {
                SetText(menuStatusText, "Enter a port between 1 and 65535");
                return;
            }

            string address = addressInput != null ? addressInput.text.Trim() : string.Empty;
            if (!IPAddress.TryParse(address, out IPAddress ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            {
                SetText(menuStatusText, "Enter the host's IPv4 address, for example 192.168.1.20");
                return;
            }

            Invoke(a => a.Join(address, port));
        }

        private void OnReady() => Invoke(a => a.SetReady(!(hasState && lastState.localReady)));
        private void OnPractice()
        {
            if(TryParsePort(out ushort port) && actions is IPracticeActions practice)practice.StartPractice(port);
        }

        private bool TryParsePort(out ushort port)
        {
            port = 0;
            return portInput != null && ushort.TryParse(portInput.text.Trim(), out port) && port > 0;
        }

        private void Invoke(Action<IPresentationActions> call)
        {
            if (actions == null)
            {
                if (!warnedNoActions)
                {
                    warnedNoActions = true;
                    Debug.LogWarning("[HUD] No presentation actions bound; button presses are ignored.", this);
                }

                ShowMessage("Not available yet");
                return;
            }

            call(actions);
        }

        // ---------- helpers ----------

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void SetInteractable(Button button, bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private static string Fallback(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : value;

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f)
            {
                return "--:--";
            }

            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
