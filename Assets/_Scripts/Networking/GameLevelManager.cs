using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative game level controller.
/// Handles player spawning, door tracking, team respawn, level completion,
/// disconnect toasts, and pause integration.
/// </summary>
public class GameLevelManager : NetworkBehaviour
{
    public static GameLevelManager Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private GameObject  _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Level")]
    [SerializeField] private int _levelIndex = 1;

    private readonly HashSet<ulong>               _playersInDoor  = new();
    private readonly Dictionary<ulong, NetworkObject> _playerObjects = new();
    private bool _levelComplete;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _levelIndex = PlayerPrefs.GetInt("teamtilt_current_level", 1);
            Debug.Log($"[GameLevelManager] Level {_levelIndex} started.");

            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
                SpawnPlayerForClient(id);

            NetworkManager.Singleton.OnClientConnectedCallback    += OnLateClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback   += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnLateClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // ── Spawning ────────────────────────────────────────────────

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("[GameLevelManager] Player prefab not assigned!");
            return;
        }

        int     idx      = _playerObjects.Count;
        int     si       = idx % Mathf.Max(1, _spawnPoints.Length);
        Vector3 pos      = _spawnPoints is { Length: > 0 }
            ? _spawnPoints[si].position
            : Vector3.right * idx * 1.1f;

        var go     = Instantiate(_playerPrefab, pos, Quaternion.identity);
        var netObj = go.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
        _playerObjects[clientId] = netObj;
        Debug.Log($"[GameLevelManager] Spawned client {clientId} at {pos}");
    }

    private void OnLateClientConnected(ulong id)
    {
        if (!_playerObjects.ContainsKey(id)) SpawnPlayerForClient(id);
    }

    private void OnClientDisconnected(ulong id)
    {
        if (!IsServer) return;

        // Clean up their spawn tracking
        _playerObjects.Remove(id);
        _playersInDoor.Remove(id);

        string msg = $"Player {id} left the game.";
        Debug.Log($"[GameLevelManager] {msg}");

        // Toast all remaining clients
        PlayerLeftToastClientRpc(id);

        // Re-check level complete in case only 1 player was left
        CheckLevelComplete();
    }

    // ── Door Tracking ────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    public void PlayerEnteredDoorServerRpc(ulong clientId)
    {
        _playersInDoor.Add(clientId);
        Debug.Log($"[GameLevelManager] {clientId} in door ({_playersInDoor.Count}/{_playerObjects.Count})");
        CheckLevelComplete();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerExitedDoorServerRpc(ulong clientId)
    {
        _playersInDoor.Remove(clientId);
    }

    private void CheckLevelComplete()
    {
        if (_levelComplete || _playerObjects.Count == 0) return;
        if (_playersInDoor.Count >= _playerObjects.Count)
        {
            _levelComplete = true;
            Debug.Log("[GameLevelManager] LEVEL COMPLETE!");
            LevelProgressManager.Instance?.UnlockNext(_levelIndex);
            // Server loads scene — NGO propagates to all clients automatically
            LevelCompleteClientRpc();
        }
    }

    [ClientRpc]
    private void LevelCompleteClientRpc()
    {
        Debug.Log("[GameLevelManager] Level complete — returning to LevelSelect.");
        // Show toast before transitioning
        ToastManager.Show("🎉 Level Complete! Great teamwork!", 2.5f);

        // Only server triggers the actual scene load (NGO propagates it)
        if (NetworkManager.Singleton.IsServer)
        {
            // Small delay so toast is visible
            Invoke(nameof(LoadLevelSelect), 2f);
        }
    }

    private void LoadLevelSelect()
    {
        GameManager.Instance?.ChangeState(GameState.LevelSelect);
    }

    [ClientRpc]
    private void PlayerLeftToastClientRpc(ulong clientId)
    {
        ToastManager.Show($"⚠️ A player disconnected.", 3f);
    }

    // ── Respawn ──────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    public void RequestRespawnServerRpc()
    {
        if (_levelComplete) return;
        RespawnAll();
    }

    private void RespawnAll()
    {
        _playersInDoor.Clear();

        int idx = 0;
        foreach (var kvp in _playerObjects)
        {
            int     si  = idx % Mathf.Max(1, _spawnPoints.Length);
            Vector3 pos = _spawnPoints is { Length: > 0 }
                ? _spawnPoints[si].position
                : Vector3.right * idx * 1.1f;

            TeleportPlayerClientRpc(kvp.Key, pos);
            idx++;
        }
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(ulong clientId, Vector3 position)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;
        var player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        player?.GetComponent<PlayerController>()?.Teleport(position);
    }
}
