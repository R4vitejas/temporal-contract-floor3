using System;

namespace TemporalContract.UI.Hud
{
    public enum SessionPhase
    {
        Disconnected,
        Connecting,
        Lobby,
        ActiveRound,
        RoundEnd,
        MatchEnd,
        Error
    }

    public enum PlayerRole
    {
        None,
        Attacker,
        Defender
    }

    /// <summary>
    /// Everything the HUD and session screens can display. The UI only renders this; it never owns score,
    /// authoritative time, health or win conditions. A production adapter fills it from the real systems.
    /// </summary>
    [Serializable]
    public struct PresentationState
    {
        public SessionPhase phase;
        public bool isPractice;

        // lobby
        public bool isHost;
        public int connectedPlayers;
        public bool localReady;
        public bool remoteReady;

        // round
        public PlayerRole role;
        public bool alive;
        public int health;
        public int maxHealth;
        public int ammo;
        public int magazineSize;
        public bool reloading;
        /// <summary>Seconds left in the round as reported by the authority; negative = unknown.</summary>
        public float timeRemainingSeconds;
        public int roundNumber;
        public string objective;
        public bool keycardKnown;
        public bool hasKeycard;

        // results
        public string roundResult;
        public int localScore;
        public int remoteScore;
        public string matchResult;
        public bool localRematchReady;
        public bool remoteRematchReady;

        // status
        public string statusMessage;
        public string errorMessage;
    }

    /// <summary>Source of display state. Implemented by a mock for tests and by a production adapter later.</summary>
    public interface IPresentationProvider
    {
        event Action StateChanged;

        /// <summary>False while there is no real state yet (nothing bound, not spawned). The UI shows a neutral placeholder.</summary>
        bool TryGetState(out PresentationState state);
    }

    /// <summary>Callbacks the UI invokes. The authority (session/round code) owns what they do.</summary>
    public interface IPresentationActions
    {
        void Host(ushort port);
        void Join(string address, ushort port);
        void SetReady(bool ready);
        void StartMatch();
        void Disconnect();
        void RequestRematch();
    }
    public interface IPracticeActions
    {
        void StartPractice(ushort port);
    }
}
