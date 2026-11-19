using System;
using TemporalContract.Core;
using TemporalContract.Networking;
using TemporalContract.Players;
using TemporalContract.Rounds;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Combat
{
    /// <summary>Server-owned player health. Damage values and victim identity are resolved by the host.</summary>
    public sealed class NetworkPlayerHealth : NetworkBehaviour, IRoundResetParticipant
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;

        private readonly NetworkVariable<int> health = new NetworkVariable<int>(
            100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int MaximumHealth => maximumHealth;
        public int CurrentHealth => health.Value;
        public bool IsAlive => health.Value > 0;
        public event Action<NetworkPlayerHealth, int, int> HealthChanged;
        public event Action<NetworkPlayerHealth, int, ulong> DamageReceived;
        public event Action<ulong> Died;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                health.Value = maximumHealth;
            }
            health.OnValueChanged += OnHealthChanged;
            NetworkFpsPlayer player = GetComponent<NetworkFpsPlayer>();
            player?.SetAliveState(IsAlive);
            HealthChanged?.Invoke(this, health.Value, health.Value);
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= OnHealthChanged;
        }

        public bool ApplyDamage(int amount, ulong attackerClientId)
        {
            if (!IsServer || amount <= 0 || !IsAlive || !IsGameplayActive())
            {
                return false;
            }

            int previous = health.Value;
            health.Value = Mathf.Max(0, previous - amount);
            DamageReceived?.Invoke(this, amount, attackerClientId);
            DamageReceivedRpc(amount, attackerClientId);
            if (health.Value == 0 && previous > 0)
            {
                Died?.Invoke(OwnerClientId);
                RoundCoordinator.Instance?.NotifyPlayerDied(OwnerClientId);
            }
            return true;
        }

        [Rpc(SendTo.NotServer)]
        private void DamageReceivedRpc(int amount, ulong attackerClientId)
        {
            DamageReceived?.Invoke(this, amount, attackerClientId);
        }

        public void ResetForRound(in RoundResetContext context)
        {
            if (!IsServer)
            {
                return;
            }

            int previous = health.Value;
            health.Value = maximumHealth;
            GetComponent<NetworkFpsPlayer>()?.SetAliveState(true);
            HealthChanged?.Invoke(this, previous, health.Value);
        }

        private void OnHealthChanged(int previous, int current)
        {
            GetComponent<NetworkFpsPlayer>()?.SetAliveState(current > 0);
            HealthChanged?.Invoke(this, previous, current);
        }

        private static bool IsGameplayActive() => RoundCoordinator.Instance != null
            ? RoundCoordinator.Instance.IsGameplayActive
            : LobbyNetworkState.Instance != null && LobbyNetworkState.Instance.MatchStarted;
    }
}
