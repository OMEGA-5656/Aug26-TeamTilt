using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the Main Menu using UI Toolkit.
/// Attach to a GameObject that also has a UIDocument component.
/// Settings are handled by a sibling SettingsUI component in the same scene.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuUI : MonoBehaviour
{
    private Button     _playBtn;
    private Button     _settingsBtn;
    private Button     _quitBtn;
    private SettingsUI _settings;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _playBtn     = root.Q<Button>("play-btn");
        _settingsBtn = root.Q<Button>("settings-btn");
        _quitBtn     = root.Q<Button>("quit-btn");

        // Find the SettingsUI in the scene (sibling or anywhere)
        _settings = FindFirstObjectByType<SettingsUI>();

        if (_playBtn == null || _quitBtn == null)
        {
            Debug.LogError("[MainMenuUI] Could not find buttons in UXML. Check element names.");
            return;
        }

        _playBtn.clicked += OnPlayClicked;
        _quitBtn.clicked += OnQuitClicked;

        if (_settingsBtn != null)
            _settingsBtn.clicked += OnSettingsClicked;
    }

    private void OnDisable()
    {
        if (_playBtn     != null) _playBtn.clicked     -= OnPlayClicked;
        if (_settingsBtn != null) _settingsBtn.clicked -= OnSettingsClicked;
        if (_quitBtn     != null) _quitBtn.clicked     -= OnQuitClicked;
    }

    private void OnPlayClicked()
    {
        // Close settings if open before navigating
        _settings?.Close();
        SceneNav.Load(SceneNames.ConnectionScreen);
    }

    private void OnSettingsClicked()
    {
        if (_settings != null)
            _settings.Open();
        else
            Debug.LogWarning("[MainMenuUI] SettingsUI not found in scene.");
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
