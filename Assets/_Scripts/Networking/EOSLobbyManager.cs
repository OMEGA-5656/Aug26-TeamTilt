using UnityEngine;

/// <summary>
/// Stub for Epic Online Services lobby integration.
/// Implement lobby creation/joining here once EOS SDK is installed.
/// Reference: https://github.com/PlayEveryWare/eos_plugin_for_unity
/// </summary>
public class EOSLobbyManager : MonoBehaviour
{
    public static EOSLobbyManager Instance { get; private set; }

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

    /// <summary>
    /// Create a new EOS lobby and get a join code.
    /// TODO: Implement using Epic.OnlineServices.Lobby
    /// </summary>
    public void CreateLobby(string lobbyName, int maxPlayers)
    {
        Debug.Log($"[EOSLobbyManager] CreateLobby called: {lobbyName} ({maxPlayers} players) — EOS not installed yet.");
        // Example future implementation:
        // var createLobbyOptions = new CreateLobbyOptions { ... };
        // EOSManager.Instance.GetEOSLobbyInterface().CreateLobby(ref createLobbyOptions, null, OnCreateLobby);
    }

    /// <summary>
    /// Join an existing EOS lobby by ID.
    /// TODO: Implement using Epic.OnlineServices.Lobby
    /// </summary>
    public void JoinLobby(string lobbyId)
    {
        Debug.Log($"[EOSLobbyManager] JoinLobby called: {lobbyId} — EOS not installed yet.");
    }

    /// <summary>
    /// Leave the current EOS lobby.
    /// </summary>
    public void LeaveLobby()
    {
        Debug.Log("[EOSLobbyManager] LeaveLobby called.");
    }
}
