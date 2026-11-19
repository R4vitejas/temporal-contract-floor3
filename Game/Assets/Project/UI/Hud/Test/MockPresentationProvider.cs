using System;
using System.Collections.Generic;
using UnityEngine;

namespace TemporalContract.UI.Hud.Testing
{
    /// <summary>
    /// TEST-ONLY provider and actions for HudMockTest.unity and the regression. It fabricates state to exercise every
    /// screen. It is not the authoritative session or round code and must never be placed in Main.
    /// </summary>
    public sealed class MockPresentationProvider : MonoBehaviour, IPresentationProvider, IPresentationActions
    {
        public event Action StateChanged;

        private PresentationState state;
        private bool hasState;

        public readonly List<string> Calls = new List<string>();

        public PresentationState State => state;

        public bool TryGetState(out PresentationState result)
        {
            result = state;
            return hasState;
        }

        public void SetState(PresentationState newState)
        {
            state = newState;
            hasState = true;
            StateChanged?.Invoke();
        }

        public void ClearState()
        {
            hasState = false;
            StateChanged?.Invoke();
        }

        // ---------- named presets covering every UI state ----------

        public static readonly string[] PresetNames =
        {
            "No state", "Disconnected", "Connecting", "Lobby: waiting", "Lobby: ready", "Active: attacker",
            "Active: defender low health", "Active: defeated", "Active: reloading", "Round end", "Match end: win",
            "Match end: draw", "Match end: rematch waiting", "Error"
        };

        public void ApplyPreset(int index)
        {
            PresentationState s = Base();
            switch (index)
            {
                case 0: ClearState(); return;
                case 1: s.phase = SessionPhase.Disconnected; s.statusMessage = "Disconnected: the host left"; break;
                case 2: s.phase = SessionPhase.Connecting; break;
                case 3: s.phase = SessionPhase.Lobby; s.isHost = true; s.connectedPlayers = 1; break;
                case 4: s.phase = SessionPhase.Lobby; s.isHost = true; s.connectedPlayers = 2; s.localReady = true; s.remoteReady = true; break;
                case 5: s.phase = SessionPhase.ActiveRound; s.role = PlayerRole.Attacker; s.alive = true; s.health = 100; s.maxHealth = 100; s.ammo = 12; s.magazineSize = 12; s.timeRemainingSeconds = 90f; s.roundNumber = 1; s.objective = "Reach the VIP room and eliminate the VIP"; s.keycardKnown = true; s.hasKeycard = false; break;
                case 6: s.phase = SessionPhase.ActiveRound; s.role = PlayerRole.Defender; s.alive = true; s.health = 20; s.maxHealth = 100; s.ammo = 3; s.magazineSize = 12; s.timeRemainingSeconds = 37.2f; s.roundNumber = 2; s.objective = "Protect the VIP until time runs out"; break;
                case 7: s.phase = SessionPhase.ActiveRound; s.role = PlayerRole.Attacker; s.alive = false; s.health = 0; s.maxHealth = 100; s.ammo = 0; s.magazineSize = 12; s.timeRemainingSeconds = 12f; s.roundNumber = 1; s.objective = "You were eliminated"; break;
                case 8: s.phase = SessionPhase.ActiveRound; s.role = PlayerRole.Attacker; s.alive = true; s.health = 75; s.maxHealth = 100; s.ammo = 0; s.magazineSize = 12; s.reloading = true; s.timeRemainingSeconds = 60f; s.roundNumber = 1; s.keycardKnown = true; s.hasKeycard = true; break;
                case 9: s.phase = SessionPhase.RoundEnd; s.roundResult = "Defender wins: time expired"; s.localScore = 0; s.remoteScore = 1; break;
                case 10: s.phase = SessionPhase.MatchEnd; s.matchResult = "VICTORY"; s.localScore = 2; s.remoteScore = 1; break;
                case 11: s.phase = SessionPhase.MatchEnd; s.matchResult = "DRAW"; s.localScore = 1; s.remoteScore = 1; break;
                case 12: s.phase = SessionPhase.MatchEnd; s.matchResult = "DEFEAT"; s.localScore = 0; s.remoteScore = 1; s.localRematchReady = true; s.remoteRematchReady = false; break;
                default: s.phase = SessionPhase.Error; s.errorMessage = "Connection failed: host unreachable at 192.168.1.20:7777"; break;
            }

            SetState(s);
        }

        private static PresentationState Base() => new PresentationState { timeRemainingSeconds = -1f };

        // ---------- actions: record the call and move the mock state like the real flow would ----------

        public void Host(ushort port)
        {
            Calls.Add($"Host {port}");
            PresentationState s = Base();
            s.phase = SessionPhase.Lobby; s.isHost = true; s.connectedPlayers = 1;
            SetState(s);
        }

        public void Join(string address, ushort port)
        {
            Calls.Add($"Join {address}:{port}");
            PresentationState s = Base();
            s.phase = SessionPhase.Lobby; s.isHost = false; s.connectedPlayers = 2;
            SetState(s);
        }

        public void SetReady(bool ready)
        {
            Calls.Add($"Ready {ready}");
            PresentationState s = state;
            s.localReady = ready;
            SetState(s);
        }

        public void StartMatch() => Calls.Add("Start");

        public void Disconnect()
        {
            Calls.Add("Disconnect");
            PresentationState s = Base();
            s.phase = SessionPhase.Disconnected;
            SetState(s);
        }

        public void RequestRematch()
        {
            Calls.Add("Rematch");
            PresentationState s = state;
            s.localRematchReady = true;
            SetState(s);
        }
    }
}
