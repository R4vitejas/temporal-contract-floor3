using System;
using TemporalContract.Core;
using TemporalContract.Interactions;
using TemporalContract.Networking;
using TemporalContract.Players;
using TemporalContract.UI.Board;
using TemporalContract.UI.Hud;
using UnityEngine;

namespace TemporalContract.Environment
{
    /// <summary>Concrete scene references for the production adapters; does not own round/spawn authority.</summary>
    public sealed class FullFloorSceneBindings : MonoBehaviour
    {
        public Transform attackerSpawn, defenderSpawn, vipSpawn;
        public LanSessionController session;
        public TextAsset sourceManifest;
        public NetworkDoor[] doors;
        public NetworkDoor restrictedDoor;
        public KeycardPickup keycard;
        public AccessPanel accessPanel;
        public WallSwitch lightSwitch;
        public MovableCover movableCover;
        public WindowPowerConsole windowPower;
        public SlidingWindow vipWindow;
        public ClassroomBoard[] boards;
        public FloorMapPanel map;
        public HudSessionUi hud;
        private InteractionRouter boundRouter;
        private readonly FeedbackRelay feedback = new FeedbackRelay();
        private NetworkInteractable[] sources;

        private void OnEnable()
        {
            sources = GetComponentsInChildren<NetworkInteractable>(true);
            foreach (var source in sources) source.FeedbackRaised += feedback.Forward;
            hud.BindFeedback(feedback);
        }
        private void Update()
        {
            var player = NetworkFpsPlayer.LocalOwner;
            var router = player != null ? player.GetComponent<InteractionRouter>() : null;
            if (router == boundRouter) return;
            boundRouter = router;
            hud.BindRouter(router);
            if (router == null) map.ForceClose();
        }
        private void OnDisable()
        {
            if (sources != null) foreach (var source in sources) if (source) source.FeedbackRaised -= feedback.Forward;
            hud.BindFeedback(null);
            hud.BindRouter(null);
            boundRouter = null;
            map.ForceClose();
        }
        // Not a scene component: audio discovers the original sources once, without a duplicate relay.
        private sealed class FeedbackRelay : IFeedbackEventSource
        {
            public event Action<FeedbackEvent> FeedbackRaised;
            public void Forward(FeedbackEvent value) => FeedbackRaised?.Invoke(value);
        }
    }
}
