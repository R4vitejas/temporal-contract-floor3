using System;
using System.Linq;
using Unity.Netcode;

namespace TemporalContract.Networking
{
    /// <summary>Server-owned lobby readiness and match-start gate replicated to both players.</summary>
    public sealed class LobbyNetworkState : NetworkBehaviour
    {
        private readonly NetworkList<ulong> readyClientIds = new NetworkList<ulong>();
        private readonly NetworkVariable<bool> matchStarted = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public static LobbyNetworkState Instance { get; private set; }
        public bool MatchStarted => matchStarted.Value;
        public int ReadyCount => readyClientIds.Count;
        public event Action StateChanged;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            readyClientIds.OnListChanged += OnReadyListChanged;
            matchStarted.OnValueChanged += OnMatchStartedChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            readyClientIds.OnListChanged -= OnReadyListChanged;
            matchStarted.OnValueChanged -= OnMatchStartedChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool IsReady(ulong clientId) => readyClientIds.Contains(clientId);

        public void SetLocalReady(bool ready)
        {
            if (!IsSpawned || matchStarted.Value)
            {
                return;
            }
            if (IsServer)
            {
                ApplyReady(NetworkManager.LocalClientId, ready);
            }
            else
            {
                SetReadyRpc(ready);
            }
        }

        public bool CanStartMatch => IsServer && !matchStarted.Value
            && NetworkManager.ConnectedClientsIds.Count == 2
            && readyClientIds.Count == 2;

        public bool StartMatch()
        {
            if (!CanStartMatch)
            {
                return false;
            }
            matchStarted.Value = true;
            StateChanged?.Invoke();
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetReadyRpc(bool ready, RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            if (senderClientId != NetworkManager.ServerClientId && !matchStarted.Value)
            {
                ApplyReady(senderClientId, ready);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestStartRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId)
            {
                StartMatch();
            }
        }

        public void RequestStart()
        {
            if (IsServer)
            {
                StartMatch();
            }
            else
            {
                RequestStartRpc();
            }
        }

        private void ApplyReady(ulong clientId, bool ready)
        {
            if (!NetworkManager.ConnectedClientsIds.Contains(clientId))
            {
                return;
            }
            int index = readyClientIds.IndexOf(clientId);
            if (ready && index < 0)
            {
                readyClientIds.Add(clientId);
            }
            else if (!ready && index >= 0)
            {
                readyClientIds.RemoveAt(index);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            int index = readyClientIds.IndexOf(clientId);
            if (index >= 0)
            {
                readyClientIds.RemoveAt(index);
            }
        }

        private void OnReadyListChanged(NetworkListEvent<ulong> _) => StateChanged?.Invoke();
        private void OnMatchStartedChanged(bool _, bool __) => StateChanged?.Invoke();
    }
}
