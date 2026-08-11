using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-authoritative Level Select controller.
/// Lives in the LevelSelect scene on a NetworkObject.
/// Only the server/host can select a level and load it for all clients.
/// </summary>
public class LevelSelectManager : NetworkBehaviour
{
    public static LevelSelectManager Instance { get; private set; }

    // Maps level index (1-6) to scene name
    private static readonly string[] LevelScenes =
    {
        "",           // index 0 unused
        SceneNames.GameLevel1,
        SceneNames.GameLevel1, // placeholder until more levels exist
        SceneNames.GameLevel1,
        SceneNames.GameLevel1,
        SceneNames.GameLevel1,
        SceneNames.GameLevel1,
    };

    private void Awake() => Instance = this;

    /// <summary>Host calls this when a level tile is clicked.</summary>
    public void HostSelectLevel(int levelIndex)
    {
        if (!IsServer) return;
        if (levelIndex < 1 || levelIndex > LevelProgressManager.TotalLevels) return;
        if (LevelProgressManager.Instance != null && !LevelProgressManager.Instance.IsUnlocked(levelIndex))
        {
            Debug.LogWarning($"[LevelSelectManager] Level {levelIndex} is locked.");
            return;
        }

        Debug.Log($"[LevelSelectManager] Loading level {levelIndex}");

        // Store which level is being played so GameLevelManager can read it
        PlayerPrefs.SetInt("teamtilt_current_level", levelIndex);

        string sceneName = LevelScenes[Mathf.Clamp(levelIndex, 1, LevelScenes.Length - 1)];
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
