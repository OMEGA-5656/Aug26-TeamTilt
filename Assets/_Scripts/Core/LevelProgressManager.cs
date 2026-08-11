using UnityEngine;

/// <summary>
/// Persists level unlock progress on the local device using PlayerPrefs.
/// Only meaningful on the host's device — clients use the level the host picks.
/// Level 1 is always unlocked. Clearing level N unlocks level N+1.
/// </summary>
public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }

    public const int TotalLevels = 6;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Level 1 is always unlocked
        if (!PlayerPrefs.HasKey(PrefKey(1)))
            PlayerPrefs.SetInt(PrefKey(1), 1);
    }

    public bool IsUnlocked(int level)
    {
        if (level <= 1) return true;
        return PlayerPrefs.GetInt(PrefKey(level), 0) == 1;
    }

    /// <summary>Call this when the current level is cleared.</summary>
    public void UnlockNext(int clearedLevel)
    {
        int next = clearedLevel + 1;
        if (next > TotalLevels) return;
        PlayerPrefs.SetInt(PrefKey(next), 1);
        PlayerPrefs.Save();
        Debug.Log($"[LevelProgress] Level {next} unlocked.");
    }

    /// <summary>Reset all progress (debug/testing).</summary>
    public void ResetProgress()
    {
        for (int i = 1; i <= TotalLevels; i++)
            PlayerPrefs.DeleteKey(PrefKey(i));
        PlayerPrefs.SetInt(PrefKey(1), 1);
        PlayerPrefs.Save();
        Debug.Log("[LevelProgress] Progress reset.");
    }

    private static string PrefKey(int level) => $"teamtilt_level_{level}_unlocked";
}
