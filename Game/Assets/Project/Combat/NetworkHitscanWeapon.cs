using System;
using TemporalContract.Core;
using TemporalContract.Networking;
using TemporalContract.Players;
using TemporalContract.Rounds;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.Combat
{
    /// <summary>Owner-triggered, host-validated hitscan weapon with replicated ammo and reload state.</summary>
    public sealed class NetworkHitscanWeapon : NetworkBehaviour, IRoundResetParticipant
    {
        [SerializeField, Min(1)] private int magazineSize = 12;
        [SerializeField, Min(1)] private int damage = 25;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.25f;
        [SerializeField, Min(0.1f)] private float reloadDuration = 1.5f;
        [SerializeField, Min(1f)] private float range = 100f;
        [SerializeField, Min(0f)] private float maximumAimPitch = 85f;

        private readonly NetworkVariable<int> ammunition = new NetworkVariable<int>(
            12, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> reloading = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<double> reloadEndsAt = new NetworkVariable<double>(
            0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<uint> shotSequence = new NetworkVariable<uint>(
            0u, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkFpsPlayer player;
        private NetworkPlayerHealth health;
        private double nextAllowedShotAt;

        public int MagazineSize => magazineSize;
        public int Ammunition => ammunition.Value;
        public bool IsReloading => reloading.Value;
        public double ReloadSecondsRemaining => reloading.Value && NetworkManager != null
            ? Math.Max(0d, reloadEndsAt.Value - NetworkManager.ServerTime.Time) : 0d;
        public event Action StateChanged;
        public event Action<NetworkHitscanWeapon> ShotFired;

        public override void OnNetworkSpawn()
        {
            player = GetComponent<NetworkFpsPlayer>();
            health = GetComponent<NetworkPlayerHealth>();
            if (IsServer)
            {
                ammunition.Value = magazineSize;
            }
            ammunition.OnValueChanged += OnAmmoChanged;
            reloading.OnValueChanged += OnReloadChanged;
            shotSequence.OnValueChanged += OnShotSequenceChanged;
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            ammunition.OnValueChanged -= OnAmmoChanged;
            reloading.OnValueChanged -= OnReloadChanged;
            shotSequence.OnValueChanged -= OnShotSequenceChanged;
        }

        private void Update()
        {
            if (IsServer && reloading.Value && NetworkManager.ServerTime.Time >= reloadEndsAt.Value)
            {
                ammunition.Value = magazineSize;
                reloadEndsAt.Value = 0d;
                reloading.Value = false;
            }

            if (!IsOwner || player == null || player.IsGameplayInputCaptured || !Application.isFocused
                || Mouse.current == null || Keyboard.current == null)
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                FireRpc(player.CameraPitch);
            }
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                RequestReloadRpc();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void FireRpc(float requestedPitch, RpcParams rpcParams = default)
        {
            if (!IsServer || rpcParams.Receive.SenderClientId != OwnerClientId || !IsAliveAndActive()
                || reloading.Value || ammunition.Value <= 0 || float.IsNaN(requestedPitch)
                || float.IsInfinity(requestedPitch))
            {
                return;
            }

            double now = NetworkManager.ServerTime.Time;
            if (now < nextAllowedShotAt)
            {
                return;
            }

            float pitch = Mathf.Clamp(requestedPitch, -maximumAimPitch, maximumAimPitch);
            Quaternion aim = Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
            Vector3 origin = transform.position + Vector3.up * 1.62f;
            Vector3 direction = aim * Vector3.forward;
            ammunition.Value--;
            nextAllowedShotAt = now + fireInterval;
            shotSequence.Value++;

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            NetworkPlayerHealth targetPlayer = hit.collider.GetComponentInParent<NetworkPlayerHealth>();
            if (targetPlayer != null && targetPlayer.OwnerClientId != OwnerClientId)
            {
                targetPlayer.ApplyDamage(damage, OwnerClientId);
                return;
            }

            VipHealth targetVip = hit.collider.GetComponentInParent<VipHealth>();
            if (targetVip != null && (RoundCoordinator.Instance != null
                ? RoundCoordinator.Instance.IsAttacker(OwnerClientId)
                : LobbyNetworkState.Instance != null && LobbyNetworkState.Instance.IsAttacker(OwnerClientId)))
            {
                targetVip.ApplyDamage(damage, OwnerClientId);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestReloadRpc(RpcParams rpcParams = default)
        {
            if (!IsServer || rpcParams.Receive.SenderClientId != OwnerClientId || !IsAliveAndActive()
                || reloading.Value || ammunition.Value >= magazineSize)
            {
                return;
            }

            reloading.Value = true;
            reloadEndsAt.Value = NetworkManager.ServerTime.Time + reloadDuration;
        }

        public void ResetForRound(in RoundResetContext context)
        {
            if (!IsServer)
            {
                return;
            }

            ammunition.Value = magazineSize;
            reloading.Value = false;
            reloadEndsAt.Value = 0d;
            nextAllowedShotAt = 0d;
            StateChanged?.Invoke();
        }

        private bool IsAliveAndActive() => health != null && health.IsAlive
            && (RoundCoordinator.Instance != null ? RoundCoordinator.Instance.IsGameplayActive
                : LobbyNetworkState.Instance != null && LobbyNetworkState.Instance.MatchStarted);

        private void OnAmmoChanged(int _, int __) => StateChanged?.Invoke();
        private void OnReloadChanged(bool _, bool __) => StateChanged?.Invoke();
        private void OnShotSequenceChanged(uint _, uint __) => ShotFired?.Invoke(this);
    }
}
