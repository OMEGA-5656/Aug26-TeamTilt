using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized scene loader with async loading and event notifications.
/// Supports both single-scene and additive loading.
/// Add a "Loading" scene name in LoadingSceneName to show a loading screen.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Optional loading screen scene name")]
    [SerializeField] private string _loadingSceneName = "";

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
    /// Load a scene by name, replacing the current scene.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName, LoadSceneMode.Single));
    }

    /// <summary>
    /// Load a scene additively (keeps current scene loaded).
    /// Useful for overlaying UI or loading sub-levels.
    /// </summary>
    public void LoadSceneAdditive(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName, LoadSceneMode.Additive));
    }

    /// <summary>
    /// Unload an additively loaded scene.
    /// </summary>
    public void UnloadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode)
    {
        EventBus.Publish(new SceneLoadStartedEvent { SceneName = sceneName });

        // Optionally show loading screen
        if (!string.IsNullOrEmpty(_loadingSceneName))
            SceneManager.LoadScene(_loadingSceneName, LoadSceneMode.Additive);

        var op = SceneManager.LoadSceneAsync(sceneName, mode);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
        yield return op;

        // Unload loading screen
        if (!string.IsNullOrEmpty(_loadingSceneName))
            SceneManager.UnloadSceneAsync(_loadingSceneName);

        EventBus.Publish(new SceneLoadCompletedEvent { SceneName = sceneName });
        Debug.Log($"[SceneLoader] Loaded scene: {sceneName}");
    }

    private IEnumerator UnloadSceneAsync(string sceneName)
    {
        var op = SceneManager.UnloadSceneAsync(sceneName);
        yield return op;
        Debug.Log($"[SceneLoader] Unloaded scene: {sceneName}");
    }
}
