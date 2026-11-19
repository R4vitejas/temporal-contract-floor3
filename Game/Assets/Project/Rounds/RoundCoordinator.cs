using System;
using System.Linq;
using System.Reflection;
using TemporalContract.Combat;
using TemporalContract.Core;
using TemporalContract.Players;
using TemporalContract.Networking;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Rounds
{
    public enum MatchPhase : byte
    {
        WaitingForStart,
        ActiveRound,
        RoundResult,
        MatchResult
    }

    public enum RoundOutcome : byte
    {
        None,
        AttackersWon,
        DefendersWon
    }

    /// <summary>Host-authoritative two-round match state, timer, role swaps, reset, and rematch.</summary>
    public sealed class RoundCoordinator : NetworkBehaviour
    {
        public const double RoundDurationSeconds = 90d;
        private const double ResultDisplaySeconds = 3d;
        private const string VipResourcePath = "NetworkCore/Vip";
        private const ulong DrawClientId = ulong.MaxValue;

        private readonly NetworkVariable<MatchPhase> phase = new NetworkVariable<MatchPhase>(
            MatchPhase.WaitingForStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> roundIndex = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<ulong> attackerClientId = new NetworkVariable<ulong>(
            0UL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<double> roundEndsAt = new NetworkVariable<double>(
            0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<RoundOutcome> roundOutcome = new NetworkVariable<RoundOutcome>(
            RoundOutcome.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<ulong> firstClientId = new NetworkVariable<ulong>(
            0UL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<ulong> secondClientId = new NetworkVariable<ulong>(
            0UL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> firstAttackWins = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> secondAttackWins = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<ulong> matchWinnerClientId = new NetworkVariable<ulong>(
            DrawClientId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<ulong> matchId = new NetworkVariable<ulong>(
            0UL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> practice = new NetworkVariable<bool>(false);
        private readonly NetworkList<ulong> rematchReadyClientIds = new NetworkList<ulong>();

        private static RoundCoordinator instance;
        private GameObject vipPrefab;
        private bool pendingTerminalEvaluation;
        private double resultEndsAt;
        private bool matchBegun;

        public static RoundCoordinator Instance => instance;
        public MatchPhase Phase => phase.Value;
        public int RoundNumber => roundIndex.Value + 1;
        public ulong AttackerClientId => attackerClientId.Value;
        public RoundOutcome LastRoundOutcome => roundOutcome.Value;
        public ulong MatchWinnerClientId => matchWinnerClientId.Value;
        public ulong MatchId => matchId.Value;
        public bool IsGameplayActive => phase.Value == MatchPhase.ActiveRound;
        public bool IsPractice => practice.Value;
        public double TimeRemaining => IsGameplayActive && NetworkManager != null
            ? Math.Max(0d, roundEndsAt.Value - NetworkManager.ServerTime.Time) : 0d;
        public event Action StateChanged;
        public event Action<ulong> PlayerDied;
        public event Action<ulong> VipKilled;

        private void Awake()
        {
            vipPrefab = Resources.Load<GameObject>(VipResourcePath);
        }

        public override void OnNetworkSpawn()
        {
            instance = this;
            phase.OnValueChanged += OnStateValueChanged;
            roundIndex.OnValueChanged += OnStateValueChanged;
            attackerClientId.OnValueChanged += OnStateValueChanged;
            roundEndsAt.OnValueChanged += OnTimerChanged;
            roundOutcome.OnValueChanged += OnStateValueChanged;
            firstAttackWins.OnValueChanged += OnScoreChanged;
            secondAttackWins.OnValueChanged += OnScoreChanged;
            matchWinnerClientId.OnValueChanged += OnStateValueChanged;
            rematchReadyClientIds.OnListChanged += OnRematchReadyChanged;
            if(IsServer) InvokeInteractionLifecycle("BindActorEligibility",new Func<ulong,bool>(CanActorInteract));
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            phase.OnValueChanged -= OnStateValueChanged;
            roundIndex.OnValueChanged -= OnStateValueChanged;
            attackerClientId.OnValueChanged -= OnStateValueChanged;
            roundEndsAt.OnValueChanged -= OnTimerChanged;
            roundOutcome.OnValueChanged -= OnStateValueChanged;
            firstAttackWins.OnValueChanged -= OnScoreChanged;
            secondAttackWins.OnValueChanged -= OnScoreChanged;
            matchWinnerClientId.OnValueChanged -= OnStateValueChanged;
            rematchReadyClientIds.OnListChanged -= OnRematchReadyChanged;
            if (instance == this)
            {
                instance = null;
            }
            TryBindInteractionRoleProvider(null);
            InvokeInteractionLifecycle("BindActorEligibility",null);
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            if (phase.Value == MatchPhase.ActiveRound && NetworkManager.ServerTime.Time >= roundEndsAt.Value)
            {
                pendingTerminalEvaluation = true;
            }
            else if (phase.Value == MatchPhase.RoundResult && NetworkManager.ServerTime.Time >= resultEndsAt)
            {
                AdvanceAfterRound();
            }
        }

        private void LateUpdate()
        {
            if (!IsServer || !IsSpawned || !pendingTerminalEvaluation || phase.Value != MatchPhase.ActiveRound)
            {
                return;
            }

            pendingTerminalEvaluation = false;
            NetworkPlayerHealth attackerHealth = GetPlayerHealth(attackerClientId.Value);
            // Evaluate after all damage RPCs for this frame so attacker death wins a simultaneous VIP kill.
            if (attackerHealth == null || !attackerHealth.IsAlive)
            {
                EndRound(RoundOutcome.DefendersWon);
            }
            else if (VipHealth.Instance == null || !VipHealth.Instance.IsAlive)
            {
                EndRound(RoundOutcome.AttackersWon);
            }
            else if (NetworkManager.ServerTime.Time >= roundEndsAt.Value)
            {
                EndRound(RoundOutcome.DefendersWon);
            }
        }

        public bool IsAttacker(ulong clientId) => phase.Value == MatchPhase.ActiveRound
            && attackerClientId.Value == clientId;
        private bool CanActorInteract(ulong id) => IsServer && IsSpawned && IsGameplayActive && GetPlayerHealth(id)?.IsAlive==true;

        public int AttackWinsFor(ulong clientId)
        {
            if (clientId == firstClientId.Value) return firstAttackWins.Value;
            if (clientId == secondClientId.Value) return secondAttackWins.Value;
            return 0;
        }

        public bool IsRematchReady(ulong clientId) => rematchReadyClientIds.Contains(clientId);

        public string GetRoundResultText()
        {
            return roundOutcome.Value == RoundOutcome.AttackersWon
                ? "Attacker wins the round — VIP eliminated."
                : roundOutcome.Value == RoundOutcome.DefendersWon
                    ? "Defender wins the round."
                    : string.Empty;
        }

        public string GetMatchResultText()
        {
            if (phase.Value != MatchPhase.MatchResult) return string.Empty;
            if (matchWinnerClientId.Value == DrawClientId) return "Match draw.";
            return $"Client {matchWinnerClientId.Value} wins the match.";
        }

        /// <summary>Called only by the server after the lobby's two-player Ready/Start gate passes.</summary>
        public void BeginMatch()
        {
            if (!IsServer || matchBegun || NetworkManager.ConnectedClientsIds.Count != 2)
            {
                return;
            }

            ulong[] clients = NetworkManager.ConnectedClientsIds.OrderBy(id => id).ToArray();
            firstClientId.Value = clients[0];
            secondClientId.Value = clients[1];
            firstAttackWins.Value = 0;
            secondAttackWins.Value = 0;
            matchWinnerClientId.Value = DrawClientId;
            matchId.Value = NewMatchId();
            rematchReadyClientIds.Clear();
            roundIndex.Value = 0;
            attackerClientId.Value = NetworkManager.ServerClientId;
            LobbyNetworkState.Instance?.SetAttackerClientId(attackerClientId.Value);
            matchBegun = true;
            practice.Value=false;
            EnsureVipSpawned();
            BeginRound();
        }

        public void BeginPracticeMatch()
        {
            if(!IsServer || matchBegun || NetworkManager.ConnectedClientsIds.Count!=1)return;
            firstClientId.Value=NetworkManager.LocalClientId;secondClientId.Value=DrawClientId;
            attackerClientId.Value=firstClientId.Value;firstAttackWins.Value=0;secondAttackWins.Value=0;
            roundIndex.Value=0;matchId.Value=NewMatchId();matchBegun=true;practice.Value=true;
            rematchReadyClientIds.Clear();EnsureVipSpawned();BeginRound();
        }

        public void RequestRematch()
        {
            if (!IsSpawned || phase.Value != MatchPhase.MatchResult)
            {
                return;
            }

            if (IsServer)
            {
                AddRematchReady(NetworkManager.LocalClientId);
            }
            else
            {
                RequestRematchRpc();
            }
        }

        public void NotifyPlayerDied(ulong clientId)
        {
            if (!IsServer || phase.Value != MatchPhase.ActiveRound)
            {
                return;
            }

            PlayerDied?.Invoke(clientId);
            InvokeInteractionLifecycle("OnPlayerDied", clientId);
            if (clientId == attackerClientId.Value)
            {
                pendingTerminalEvaluation = true;
            }
        }

        public void NotifyVipDied(ulong killerClientId)
        {
            if (!IsServer || phase.Value != MatchPhase.ActiveRound || killerClientId != attackerClientId.Value)
            {
                return;
            }

            VipKilled?.Invoke(killerClientId);
            pendingTerminalEvaluation = true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestRematchRpc(RpcParams rpcParams = default)
        {
            AddRematchReady(rpcParams.Receive.SenderClientId);
        }

        private void AddRematchReady(ulong clientId)
        {
            if (!IsServer || phase.Value != MatchPhase.MatchResult
                || !NetworkManager.ConnectedClientsIds.Contains(clientId)
                || rematchReadyClientIds.Contains(clientId))
            {
                return;
            }

            rematchReadyClientIds.Add(clientId);
            if (rematchReadyClientIds.Count == (practice.Value?1:2))
            {
                StartRematch();
            }
        }

        private void StartRematch()
        {
            matchId.Value = NewMatchId();
            firstAttackWins.Value = 0;
            secondAttackWins.Value = 0;
            matchWinnerClientId.Value = DrawClientId;
            roundIndex.Value = 0;
            attackerClientId.Value = firstClientId.Value;
            LobbyNetworkState.Instance?.SetAttackerClientId(attackerClientId.Value);
            rematchReadyClientIds.Clear();
            matchBegun = true;
            BeginRound();
        }

        private void BeginRound()
        {
            roundOutcome.Value = RoundOutcome.None;
            pendingTerminalEvaluation = false;
            resultEndsAt = 0d;
            ResetRoundParticipants();
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                NetworkFpsPlayer player = GetPlayer(clientId);
                player?.PlaceAtSpawn(clientId == attackerClientId.Value);
            }
            TryBindInteractionRoleProvider(IsAttacker);
            roundEndsAt.Value = NetworkManager.ServerTime.Time + RoundDurationSeconds;
            phase.Value = MatchPhase.ActiveRound;
            StateChanged?.Invoke();
        }

        private void ResetRoundParticipants()
        {
            var context = new RoundResetContext((uint)(roundIndex.Value + 1), matchId.Value);
            bool interactionsHandled = TryResetInteractions(context);
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (!(behaviour is IRoundResetParticipant participant)
                    || IsInteractionBehaviour(behaviour) && interactionsHandled)
                {
                    continue;
                }
                if (behaviour is NetworkBehaviour networkBehaviour && !networkBehaviour.IsSpawned)
                {
                    continue;
                }
                participant.ResetForRound(context);
            }
        }

        private bool TryResetInteractions(RoundResetContext context)
        {
            return InvokeInteractionLifecycle("ResetAll", context);
        }

        private void AdvanceAfterRound()
        {
            if (roundIndex.Value == 0 && !practice.Value)
            {
                roundIndex.Value = 1;
                attackerClientId.Value = attackerClientId.Value == firstClientId.Value
                    ? secondClientId.Value : firstClientId.Value;
                LobbyNetworkState.Instance?.SetAttackerClientId(attackerClientId.Value);
                BeginRound();
                return;
            }

            int firstWins = firstAttackWins.Value;
            int secondWins = secondAttackWins.Value;
            matchWinnerClientId.Value = firstWins == secondWins
                ? DrawClientId : firstWins > secondWins ? firstClientId.Value : secondClientId.Value;
            phase.Value = MatchPhase.MatchResult;
            TryBindInteractionRoleProvider(null);
            StateChanged?.Invoke();
        }

        private void EndRound(RoundOutcome outcome)
        {
            if (phase.Value != MatchPhase.ActiveRound)
            {
                return;
            }

            roundOutcome.Value = outcome;
            if (outcome == RoundOutcome.AttackersWon)
            {
                if (attackerClientId.Value == firstClientId.Value) firstAttackWins.Value++;
                else if (attackerClientId.Value == secondClientId.Value) secondAttackWins.Value++;
            }
            resultEndsAt = NetworkManager.ServerTime.Time + ResultDisplaySeconds;
            phase.Value = MatchPhase.RoundResult;
            pendingTerminalEvaluation = false;
            StateChanged?.Invoke();
        }

        private void EnsureVipSpawned()
        {
            if (VipHealth.Instance != null || vipPrefab == null)
            {
                return;
            }

            var pose=GameplaySceneLayout.VipPose;
            GameObject vip = Instantiate(vipPrefab, pose.position, pose.rotation);
            NetworkObject networkObject = vip.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            else
            {
                Destroy(vip);
            }
        }

        private NetworkFpsPlayer GetPlayer(ulong clientId)
        {
            if (NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client)
                && client.PlayerObject != null)
            {
                return client.PlayerObject.GetComponent<NetworkFpsPlayer>();
            }
            return null;
        }

        private NetworkPlayerHealth GetPlayerHealth(ulong clientId) => GetPlayer(clientId)?.GetComponent<NetworkPlayerHealth>();

        private static ulong NewMatchId() => unchecked((ulong)DateTime.UtcNow.Ticks);

        private static bool IsInteractionBehaviour(MonoBehaviour behaviour) =>
            behaviour.GetType().Namespace == "TemporalContract.Interactions";

        private static Type FindInteractionLifecycleType()
        {
            return Type.GetType("TemporalContract.Interactions.InteractionLifecycle, Assembly-CSharp");
        }

        private static bool InvokeInteractionLifecycle(string methodName, object argument)
        {
            Type type = FindInteractionLifecycleType();
            MethodInfo method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                return false;
            }
            try
            {
                method.Invoke(null, new[] { argument });
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
                return false;
            }
            catch (ArgumentException exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private static void TryBindInteractionRoleProvider(Func<ulong, bool> provider)
        {
            Type type = FindInteractionLifecycleType();
            MethodInfo method = type?.GetMethod("BindRoleProvider", BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, new object[] { provider });
        }

        private void OnStateValueChanged<T>(T _, T __) => StateChanged?.Invoke();
        private void OnTimerChanged(double _, double __) => StateChanged?.Invoke();
        private void OnScoreChanged(int _, int __) => StateChanged?.Invoke();
        private void OnRematchReadyChanged(NetworkListEvent<ulong> _) => StateChanged?.Invoke();
    }
}
