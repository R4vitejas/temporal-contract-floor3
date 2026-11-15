using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TemporalContract.Interactions.Testing
{
    /// <summary>TEST-ONLY Host/Client buttons for the interaction test scene (not the shared menu).</summary>
    public sealed class TestNetworkMenu : MonoBehaviour
    {
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        private void OnGUI()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.IsListening)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 20f, 260f, 200f), GUI.skin.box);
            GUILayout.Label("Interaction test scene");
            address = GUILayout.TextField(address);
            if (GUILayout.Button("Host", GUILayout.Height(30f)))
            {
                Configure(manager);
                manager.StartHost();
            }

            if (GUILayout.Button("Join as client", GUILayout.Height(30f)))
            {
                Configure(manager);
                manager.StartClient();
            }

            GUILayout.EndArea();
        }

        private void Configure(NetworkManager manager)
        {
            var transport = manager.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(address, port);
            }
        }
    }
}
