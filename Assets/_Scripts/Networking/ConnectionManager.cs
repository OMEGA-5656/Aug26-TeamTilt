using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Abstracts the underlying transport mechanism (LAN / direct connect via UTP).
/// Swap this class to support EOS or Relay without touching other scripts.
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    [Header("Transport Settings")]
    [SerializeField] private ushort _port = 7777;

    public bool IsConnected => NetworkManager.Singleton != null &&
                               (NetworkManager.Singleton.IsHost ||
                                NetworkManager.Singleton.IsClient ||
                                NetworkManager.Singleton.IsServer);

    /// <summary>Fires on the connecting client when it successfully joins a host.</summary>
    public event Action OnConnectedToHost;

    /// <summary>Fires on host/server when any client disconnects.</summary>
    public event Action<ulong> OnClientLeft;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        NetworkManager.Singleton.OnClientConnectedCallback   += OnClientConnectedInternal;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedInternal;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback   -= OnClientConnectedInternal;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedInternal;
    }

    // ── Public API ────────────────────────────────────────────

    /// <summary>
    /// Start hosting on all local interfaces (LAN).
    /// Returns true if the host started successfully.
    /// </summary>
    public bool StartHost()
    {
        SetTransportAddress("0.0.0.0", _port);
        bool ok = NetworkManager.Singleton.StartHost();
        if (ok)
        {
            GameManager.Instance?.ChangeState(GameState.Lobby);
            Debug.Log("[ConnectionManager] Host started.");
        }
        else
        {
            Debug.LogError("[ConnectionManager] Failed to start host.");
        }
        return ok;
    }

    /// <summary>
    /// Connect as a client to the given LAN IP.
    /// The OnConnectedToHost event fires on success.
    /// </summary>
    public void StartClient(string ip)
    {
        SetTransportAddress(ip, _port);
        NetworkManager.Singleton.StartClient();
        Debug.Log($"[ConnectionManager] Connecting to {ip}:{_port}…");
    }

    private bool _disconnecting;

    public void Disconnect()
    {
        if (_disconnecting) return;
        _disconnecting = true;

        Debug.Log("[ConnectionManager] Disconnecting…");
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        GameManager.Instance?.ChangeState(GameState.MainMenu);
        _disconnecting = false;
    }

    // ── Internal callbacks ────────────────────────────────────

    private void OnClientConnectedInternal(ulong clientId)
    {
        Debug.Log($"[ConnectionManager] Client connected: {clientId}");

        // Host path: StartHost() already called GameManager.ChangeState(Lobby) which
        //            triggers NGO SceneManager.LoadScene — no need to do it again here.
        // Client path: NGO SceneManager automatically replicates the scene to clients
        //              when the server calls LoadScene — do NOT call ChangeState for clients.
        if (!NetworkManager.Singleton.IsServer &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Pure client — scene is handled by NGO, just fire the event.
            OnConnectedToHost?.Invoke();
        }

        EventBus.Publish(new LobbyPlayerJoinedEvent { ClientId = clientId });
    }

    private void OnClientDisconnectedInternal(ulong clientId)
    {
        Debug.Log($"[ConnectionManager] Client disconnected: {clientId}");
        OnClientLeft?.Invoke(clientId);

        EventBus.Publish(new LobbyPlayerLeftEvent { ClientId = clientId });

        if (NetworkManager.Singleton == null) return;

        bool iAmClient = !NetworkManager.Singleton.IsServer;

        // Return to main menu if:
        // (a) we ourselves disconnected, OR
        // (b) the server/host (clientId == 0) dropped — means host left/crashed.
        bool myOwnDisconnect    = clientId == NetworkManager.Singleton.LocalClientId;
        bool serverDisconnected = clientId == NetworkManager.ServerClientId;

        if (iAmClient && (myOwnDisconnect || serverDisconnected))
        {
            Debug.Log("[ConnectionManager] Host or self disconnected — returning to MainMenu.");
            GameManager.Instance?.ChangeState(GameState.MainMenu);
        }
    }

    // ── Helpers ──────────────────────────────────────────────

    private void SetTransportAddress(string ip, ushort port)
    {
        var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(ip, port);
    }
}
