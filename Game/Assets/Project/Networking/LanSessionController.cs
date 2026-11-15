using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TemporalContract.Networking;
using TemporalContract.Players;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalContract.Networking
{
    /// <summary>Temporary LAN menu and connection owner, kept separate from gameplay systems.</summary>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class LanSessionController : MonoBehaviour
    {
        private const ushort DefaultPort = 7777;
        private const int MaximumPlayers = 2;
        private const int WindowId = 7321;
        private const string PlayerResourcePath = "NetworkCore/NetworkPlayer";
        private const string LobbyResourcePath = "NetworkCore/LobbyState";

        private static LanSessionController instance;
        private NetworkManager networkManager;
        private UnityTransport transport;
        private GameObject lobbyPrefab;
        private string joinAddress = "127.0.0.1";
        private string portText = DefaultPort.ToString();
        private string status = "Ready to host or join a LAN session.";
        private string errorMessage;
        private Rect window = new Rect(24f, 24f, 380f, 330f);
        private bool shutdownRequested;
        private bool menuVisible = true;
        private bool observedMatchStarted;
        private readonly HashSet<ulong> connectedSessionClients = new HashSet<ulong>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSessionController()
        {
            if (FindFirstObjectByType<LanSessionController>() != null)
            {
                return;
            }

            NetworkManager manager = FindFirstObjectByType<NetworkManager>();
            GameObject host = manager != null ? manager.gameObject : new GameObject("LAN Session");
            if (host.GetComponent<UnityTransport>() == null)
            {
                host.AddComponent<UnityTransport>();
            }
            if (host.GetComponent<NetworkManager>() == null)
            {
                host.AddComponent<NetworkManager>();
            }
            host.AddComponent<LanSessionController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            networkManager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            lobbyPrefab = Resources.Load<GameObject>(LobbyResourcePath);

            GameObject playerPrefab = Resources.Load<GameObject>(PlayerResourcePath);
            if (playerPrefab == null || lobbyPrefab == null)
            {
                errorMessage = "Network prefabs are missing. Run Temporal Contract > Build Network Core Assets.";
                status = errorMessage;
                return;
            }

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = lobbyPrefab });
            networkManager.ConnectionApprovalCallback = ApproveConnection;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.ConnectionApprovalCallback = null;
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            if (instance == this)
            {
                instance = null;
            }
        }

        public void StartHost()
        {
            if (!CanStartNetwork() || !TryGetPort(out ushort port))
            {
                return;
            }

            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            if (!networkManager.StartHost())
            {
                status = "Host could not start. Check the port and network log.";
                return;
            }

            NetworkObject lobby = Instantiate(lobbyPrefab).GetComponent<NetworkObject>();
            lobby.Spawn();
            status = $"Hosting on UDP port {port}. Waiting for one player.";
            SetLocalPlayerInputCaptured(true);
        }

        public void StartClient()
        {
            if (!CanStartNetwork() || !TryGetPort(out ushort port))
            {
                return;
            }
            if (!IPAddress.TryParse(joinAddress.Trim(), out IPAddress address)
                || address.AddressFamily != AddressFamily.InterNetwork)
            {
                errorMessage = "Enter a valid IPv4 address.";
                return;
            }

            transport.SetConnectionData(address.ToString(), port);
            if (!networkManager.StartClient())
            {
                status = "Join could not start. Check the IP, port, and firewall.";
                return;
            }
            status = $"Connecting to {address}:{port}…";
            SetLocalPlayerInputCaptured(true);
        }

        public void Disconnect()
        {
            if (networkManager != null && networkManager.IsListening)
            {
                shutdownRequested = true;
                networkManager.Shutdown();
            }
            shutdownRequested = false;
            menuVisible = true;
            observedMatchStarted = false;
            SetLocalPlayerInputCaptured(false);
            status = "Disconnected. You can host or join again.";
        }

        private bool CanStartNetwork()
        {
            errorMessage = string.Empty;
            if (networkManager == null || transport == null || lobbyPrefab == null
                || Resources.Load<GameObject>(PlayerResourcePath) == null)
            {
                errorMessage = "Network setup is incomplete. Close and reopen Play Mode after building network assets.";
                return false;
            }
            if (networkManager.IsListening)
            {
                errorMessage = "A session is already running.";
                return false;
            }
            return true;
        }

        private bool TryGetPort(out ushort port)
        {
            if (!ushort.TryParse(portText, out port) || port == 0)
            {
                errorMessage = "Port must be between 1 and 65535.";
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            LobbyNetworkState lobby = LobbyNetworkState.Instance;
            bool full = networkManager.ConnectedClientsIds.Count >= MaximumPlayers;
            bool late = lobby != null && lobby.MatchStarted;
            response.Approved = !full && !late;
            response.CreatePlayerObject = response.Approved;
            if (!response.Approved)
            {
                response.Reason = late ? "This match has already started." : "This session already has two players.";
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            connectedSessionClients.Add(clientId);
            if (networkManager.IsServer)
            {
                status = networkManager.ConnectedClientsIds.Count == MaximumPlayers
                    ? "Two players connected. Both players must Ready before Start."
                    : "Player connected. Waiting for one more player.";
            }
            else if (clientId == networkManager.LocalClientId)
            {
                status = "Connected to host. Mark Ready when prepared.";
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (shutdownRequested)
            {
                return;
            }

            if (networkManager.IsServer && clientId != networkManager.LocalClientId)
            {
                if (!connectedSessionClients.Remove(clientId))
                {
                    return;
                }
                shutdownRequested = true;
                networkManager.Shutdown();
                shutdownRequested = false;
                status = "The other player disconnected. Session closed; host or join again.";
            }
            else if (!networkManager.IsServer && clientId == networkManager.LocalClientId)
            {
                shutdownRequested = true;
                networkManager.Shutdown();
                shutdownRequested = false;
                string reason = networkManager.DisconnectReason;
                status = string.IsNullOrWhiteSpace(reason)
                    ? "Host disconnected. Session closed; you can join again."
                    : $"Disconnected: {reason}";
            }
        }

        private void OnGUI()
        {
            bool matchStarted = LobbyNetworkState.Instance != null && LobbyNetworkState.Instance.MatchStarted;
            if (matchStarted && !observedMatchStarted)
            {
                observedMatchStarted = true;
                menuVisible = false;
                SetLocalPlayerInputCaptured(false);
            }

            if (matchStarted && Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                menuVisible = !menuVisible;
                SetLocalPlayerInputCaptured(menuVisible);
            }

            if (matchStarted && !menuVisible)
            {
                GUI.Label(new Rect(Screen.width - 160f, 12f, 150f, 24f), "F1: session menu");
                return;
            }

            if (!networkManager.IsListening)
            {
                menuVisible = true;
                observedMatchStarted = false;
            }
            window = GUILayout.Window(WindowId, window, DrawWindow, "Temporal Contract — LAN");
        }

        private static void SetLocalPlayerInputCaptured(bool captured)
        {
            NetworkFpsPlayer.LocalOwner?.SetGameplayInputCaptured(captured);
        }

        private static string GetLocalIpv4()
        {
            try
            {
                foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                    {
                        return address.ToString();
                    }
                }
            }
            catch (SocketException)
            {
                // The session menu remains usable offline; the host can check its adapter settings.
            }
            return "LAN IPv4 unavailable";
        }

        private void DrawWindow(int id)
        {
            LobbyNetworkState lobby = LobbyNetworkState.Instance;
            bool connected = networkManager != null && networkManager.IsListening;
            GUILayout.Label(status, GUILayout.MinHeight(34f));

            if (!connected)
            {
                GUILayout.Space(6f);
                GUILayout.Label($"For another laptop, use this host address: {GetLocalIpv4()}:{portText} (UDP).");
                GUILayout.Label("Allow UDP port 7777 through the host firewall when prompted.");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Port", GUILayout.Width(70f));
                portText = GUILayout.TextField(portText);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host")) StartHost();
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Host IP", GUILayout.Width(70f));
                joinAddress = GUILayout.TextField(joinAddress);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Join")) StartClient();
            }
            else
            {
                GUILayout.Label($"Players: {networkManager.ConnectedClientsIds.Count}/{MaximumPlayers}");
                if (lobby != null)
                {
                    bool ready = lobby.IsReady(networkManager.LocalClientId);
                    bool nextReady = GUILayout.Toggle(ready, ready ? "Ready" : "Mark Ready");
                    if (nextReady != ready) lobby.SetLocalReady(nextReady);
                    GUILayout.Label($"Ready players: {lobby.ReadyCount}/2");
                    if (networkManager.IsServer && GUILayout.Button("Start Match"))
                    {
                        if (!lobby.StartMatch())
                        {
                            status = "Start requires exactly two connected, Ready players.";
                        }
                        else
                        {
                            status = "Match started. Late joining is disabled.";
                            menuVisible = false;
                            SetLocalPlayerInputCaptured(false);
                        }
                    }
                    if (lobby.MatchStarted)
                    {
                        GUILayout.Label("Match is in progress; late joining is disabled.");
                    }
                }
                if (GUILayout.Button("Disconnect")) Disconnect();
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                GUILayout.Label(errorMessage, GUI.skin.box);
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }
    }
}
