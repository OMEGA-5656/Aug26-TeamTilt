using UnityEngine;

public class GameSettingsInit : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSettings()
    {
        // 1. Target Frame Rate for Mobile Pacing
        // Setting it to 61 avoids specific integer division stutter on typical 60Hz displays.
        Application.targetFrameRate = 61;
        
        Debug.Log("[GameSettingsInit] Forced Application.targetFrameRate = 61 for mobile frame pacing optimizations.");
    }
}
