using UnityEngine.SceneManagement;

/// <summary>
/// Thin static helper so any UI script can navigate scenes regardless of
/// whether SceneLoader (Bootstrap) is present. Falls back to direct SceneManager
/// when SceneLoader.Instance is null (e.g. Developer testing from a non-Bootstrap scene).
/// </summary>
public static class SceneNav
{
    public static void Load(string sceneName)
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
