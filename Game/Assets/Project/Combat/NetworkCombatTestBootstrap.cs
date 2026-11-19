using TemporalContract.Networking;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Combat
{
    /// <summary>Test-scene-only VIP spawner; production match rounds own their own spawn/reset lifecycle.</summary>
    public sealed class NetworkCombatTestBootstrap : MonoBehaviour
    {
        private const string VipResourcePath = "NetworkCore/Vip";
        private GameObject vipPrefab;

        private void Awake()
        {
            vipPrefab = Resources.Load<GameObject>(VipResourcePath);
        }

        private void Update()
        {
            NetworkManager manager = NetworkManager.Singleton;
            LobbyNetworkState lobby = LobbyNetworkState.Instance;
            if (manager == null || !manager.IsListening || !manager.IsServer || lobby == null
                || !lobby.MatchStarted || VipHealth.Instance != null || vipPrefab == null)
            {
                return;
            }

            GameObject vip = Instantiate(vipPrefab, new Vector3(0f, 0f, 4f), Quaternion.identity);
            NetworkObject networkObject = vip.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            else
            {
                Destroy(vip);
                Debug.LogError("NetworkCore VIP prefab must have a root NetworkObject.");
            }
        }
    }
}
