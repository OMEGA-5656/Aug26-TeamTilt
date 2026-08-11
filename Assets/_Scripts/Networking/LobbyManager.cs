using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-authoritative lobby controller.
/// Lives in the Lobby scene on a NetworkObject.
/// Tracks all connected players in a NetworkList so every client sees the list.
/// The host can trigger loading the next scene when ready.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    /// <summary>All players currently in the lobby (server-owned, replicated).</summary>
    public NetworkList<LobbyPlayerData> PlayerList { get; private set; }

    // How many players the host needs before Start Game is unlocked.
    [SerializeField] private int _minPlayersToStart = 1;

    private void Awake()
    {
        Instance = this;
        // NetworkList must be created in Awake (before OnNetworkSpawn).
        PlayerList = new NetworkList<LobbyPlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback   += ServerOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += ServerOnClientDisconnected;

            // Add host itself immediately.
            AddPlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback   -= ServerOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= ServerOnClientDisconnected;
        }
    }

    // ── Server-side player tracking ───────────────────────────

    private void ServerOnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return; // host already added
        AddPlayer(clientId);
    }

    private void ServerOnClientDisconnected(ulong clientId)
    {
        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId)
            {
                PlayerList.RemoveAt(i);
                break;
            }
        }
        EventBus.Publish(new LobbyPlayerLeftEvent { ClientId = clientId });
    }

    private void AddPlayer(ulong clientId)
    {
        string ipLabel = (clientId == NetworkManager.ServerClientId) ? " (Host)" : "";
        var data = new LobbyPlayerData
        {
            ClientId    = clientId,
            DisplayName = $"Player {clientId}{ipLabel}"
        };
        PlayerList.Add(data);
        Debug.Log($"[LobbyManager] Player joined: {data.DisplayName} (id={clientId})");
    }

    // ── Host API ──────────────────────────────────────────────

    public bool CanStartGame() => IsServer && PlayerList.Count >= _minPlayersToStart;

    /// <summary>Host calls this to transition to Level Select for all players.</summary>
    [Rpc(SendTo.Server)]
    public void StartGameServerRpc()
    {
        if (!IsServer) return;
        Debug.Log("[LobbyManager] Starting game — loading LevelSelect…");
        EventBus.Publish(new GameStartedEvent());
        GameManager.Instance?.ChangeState(GameState.LevelSelect);
    }
}
