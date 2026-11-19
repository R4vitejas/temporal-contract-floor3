using System;
using System.Linq;
using TemporalContract.Combat;
using TemporalContract.Interactions;
using TemporalContract.Networking;
using TemporalContract.Players;
using TemporalContract.Rounds;
using TemporalContract.UI.Board;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.UI.Hud
{
    /// <summary>Reads production state and routes UI intent to the existing authority.</summary>
    public sealed class ProductionPresentationAdapter : MonoBehaviour, IPresentationProvider, IPresentationActions, IPracticeActions
    {
        public LanSessionController session;
        public KeycardPickup keycard;
        public HudSessionUi hud;
        public FloorMapPanel map;
        public event Action StateChanged;
        private PresentationState previous;
        private NetworkFpsPlayer capturedPlayer;
        private SessionPhase capturedPhase;
        private bool capturedAlive;
        private float nextRefresh;
        private bool hasPrevious;

        private void Awake() { session.ExternalPresentation=true; }
        private void OnEnable() { session.ExternalPresentation=true;hud.Bind(this,this);hasPrevious=false; }
        private void OnDisable()
        {
            if(capturedPlayer)capturedPlayer.SetGameplayInputCaptured(this,false);
            capturedPlayer=null;
            if(session)session.ExternalPresentation=false;
        }
        private void Update()
        {
            if(Time.unscaledTime<nextRefresh)return;
            nextRefresh=Time.unscaledTime+.05f;
            TryGetState(out var state);
            var player=NetworkFpsPlayer.LocalOwner;
            if(player!=capturedPlayer || !hasPrevious || state.phase!=capturedPhase || state.alive!=capturedAlive)
            {
                if(capturedPlayer && capturedPlayer!=player)capturedPlayer.SetGameplayInputCaptured(this,false);
                capturedPlayer=player;capturedPhase=state.phase;capturedAlive=state.alive;
                if((state.phase!=SessionPhase.ActiveRound||!state.alive) && map)map.ForceClose();
                if(player)player.SetGameplayInputCaptured(this,state.phase!=SessionPhase.ActiveRound||!state.alive);
            }
            if(!hasPrevious || !state.Equals(previous)) {previous=state;hasPrevious=true;StateChanged?.Invoke();}
        }
        public bool TryGetState(out PresentationState state)
        {
            var manager=NetworkManager.Singleton;
            var lobby=LobbyNetworkState.Instance;
            var round=RoundCoordinator.Instance;
            var player=NetworkFpsPlayer.LocalOwner;
            var health=player?player.GetComponent<NetworkPlayerHealth>():null;
            var weapon=player?player.GetComponent<NetworkHitscanWeapon>():null;
            ulong local=manager?manager.LocalClientId:0;
            var remote=FindObjectsByType<NetworkFpsPlayer>(FindObjectsSortMode.None).FirstOrDefault(p=>p.OwnerClientId!=local);
            state=new PresentationState {
                phase=SessionPhase.Disconnected, statusMessage=session?session.StatusMessage:"Session unavailable",
                errorMessage=session?session.ErrorMessage:"", isHost=manager&&manager.IsHost,
                connectedPlayers=manager&&manager.IsServer?manager.ConnectedClientsIds.Count:FindObjectsByType<NetworkFpsPlayer>(FindObjectsSortMode.None).Length,
                localReady=lobby&&lobby.IsReady(local),remoteReady=lobby&&remote&&lobby.IsReady(remote.OwnerClientId),
                alive=health&&health.IsAlive,health=health?health.CurrentHealth:0,maxHealth=health?health.MaximumHealth:100,
                ammo=weapon?weapon.Ammunition:0,magazineSize=weapon?weapon.MagazineSize:12,reloading=weapon&&weapon.IsReloading,
                reloadProgress=weapon&&weapon.IsReloading?Mathf.Clamp01(1-(float)weapon.ReloadSecondsRemaining/weapon.ReloadDuration):0,
                isPractice=round&&round.IsPractice, timeRemainingSeconds=round?(float)Math.Ceiling(round.TimeRemaining):-1,
                roundNumber=round?round.RoundNumber:0, keycardKnown=keycard!=null,hasKeycard=keycard&&keycard.IsHeldBy(local),
                role=round&&lobby&&lobby.MatchStarted?(round.AttackerClientId==local?PlayerRole.Attacker:PlayerRole.Defender):PlayerRole.None,
                roundResult=round?round.GetRoundResultText():"",matchResult=round?round.GetMatchResultText():"",
                localScore=round?round.AttackWinsFor(local):0,remoteScore=round&&remote?round.AttackWinsFor(remote.OwnerClientId):0,
                localRematchReady=round&&round.IsRematchReady(local),remoteRematchReady=round&&remote&&round.IsRematchReady(remote.OwnerClientId)
            };
            state.objective=state.role==PlayerRole.Attacker?"Eliminate the VIP in Staff Cabin.\nKeycard: west corridor.\nLMB fire · R reload · E interact · F1 menu":"Protect the VIP until time expires.\nLMB fire · R reload · E interact · F1 menu";
            if(!string.IsNullOrEmpty(state.errorMessage))state.phase=SessionPhase.Error;
            else if(manager && manager.IsListening)
            {
                state.phase=!manager.IsServer&&!manager.IsConnectedClient?SessionPhase.Connecting:SessionPhase.Lobby;
                if(round && lobby && lobby.MatchStarted)
                    state.phase=round.Phase==MatchPhase.ActiveRound?SessionPhase.ActiveRound:
                        round.Phase==MatchPhase.RoundResult?SessionPhase.RoundEnd:
                        round.Phase==MatchPhase.MatchResult?SessionPhase.MatchEnd:SessionPhase.Lobby;
            }
            if(state.isPractice)state.matchResult=round&&round.LastRoundOutcome==RoundOutcome.AttackersWon?"PRACTICE COMPLETE — VIP ELIMINATED":"PRACTICE ENDED";
            else if(round&&round.Phase==MatchPhase.MatchResult)
                state.matchResult=round.MatchWinnerClientId==ulong.MaxValue?"MATCH DRAW":round.MatchWinnerClientId==local?"VICTORY":"DEFEAT";
            if(round&&round.Phase==MatchPhase.RoundResult&&!state.isPractice)
                state.roundResult=((state.role==PlayerRole.Attacker)==(round.LastRoundOutcome==RoundOutcome.AttackersWon))?"ROUND WON":"ROUND LOST";
            if(state.phase==SessionPhase.Lobby)state.statusMessage=(state.isHost?"HOST  ":"CONNECTED  ")+session.ConnectionLabel;
            return true;
        }
        public void Host(ushort port) {session.ConfigureEndpoint("127.0.0.1",port);session.StartHost();nextRefresh=0;}
        public void Join(string address,ushort port) {session.ConfigureEndpoint(address,port);session.StartClient();nextRefresh=0;}
        public void StartPractice(ushort port) {session.ConfigureEndpoint("127.0.0.1",port);session.StartPractice();nextRefresh=0;}
        public void SetReady(bool ready) => LobbyNetworkState.Instance?.SetLocalReady(ready);
        public void StartMatch() => LobbyNetworkState.Instance?.RequestStart();
        public void Disconnect() {session.Disconnect();nextRefresh=0;}
        public void RequestRematch() => RoundCoordinator.Instance?.RequestRematch();
    }
}
