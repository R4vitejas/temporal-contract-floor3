using System;
using TemporalContract.Core;
using TemporalContract.Networking;
using Unity.Netcode;
using UnityEngine;
using TemporalContract.Rounds;

namespace TemporalContract.Combat
{
    /// <summary>Stationary, replicated VIP health. Only the current attacker can damage the VIP.</summary>
    public sealed class VipHealth : NetworkBehaviour, IRoundResetParticipant
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;

        private readonly NetworkVariable<int> health = new NetworkVariable<int>(
            100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public static VipHealth Instance { get; private set; }
        public int MaximumHealth => maximumHealth;
        public int CurrentHealth => health.Value;
        public bool IsAlive => health.Value > 0;
        public event Action<VipHealth, int, int> HealthChanged;
        public event Action<VipHealth, int, ulong> DamageReceived;
        public event Action<ulong> Died;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (IsServer)
            {
                health.Value = maximumHealth;
            }
            health.OnValueChanged += OnHealthChanged;
            HealthChanged?.Invoke(this, health.Value, health.Value);
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= OnHealthChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool ApplyDamage(int amount, ulong attackerClientId)
        {
            LobbyNetworkState lobby = LobbyNetworkState.Instance;
            bool active = RoundCoordinator.Instance != null
                ? RoundCoordinator.Instance.IsGameplayActive : lobby != null && lobby.MatchStarted;
            bool attacker = RoundCoordinator.Instance != null
                ? RoundCoordinator.Instance.IsAttacker(attackerClientId)
                : lobby != null && lobby.IsAttacker(attackerClientId);
            if (!IsServer || amount <= 0 || !IsAlive || !active || !attacker)
            {
                return false;
            }

            int previous = health.Value;
            health.Value = Mathf.Max(0, previous - amount);
            DamageReceived?.Invoke(this, amount, attackerClientId);
            DamageReceivedRpc(amount, attackerClientId);
            if (previous > 0 && health.Value == 0)
            {
                Died?.Invoke(attackerClientId);
                RoundCoordinator.Instance?.NotifyVipDied(attackerClientId);
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
            HealthChanged?.Invoke(this, previous, health.Value);
        }

        private void OnHealthChanged(int previous, int current) => HealthChanged?.Invoke(this, previous, current);
    }
}
