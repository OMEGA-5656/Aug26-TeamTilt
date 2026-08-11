using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// In-game pause menu. Triggered by Escape key (via PlayerInputHandler.PausePressed).
/// Also usable programmatically. Only the local client pauses their own view.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseUI : MonoBehaviour
{
    public static PauseUI Instance { get; private set; }

    private VisualElement _overlay;
    private Button        _resumeBtn, _quitBtn;
    private bool          _isPaused;
    private bool          _ready;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc?.rootVisualElement == null) return;
        doc.rootVisualElement.schedule.Execute(Setup).ExecuteLater(60);
    }

    private void Setup()
    {
        var doc = GetComponent<UIDocument>();
        if (doc?.rootVisualElement == null) return;
        var root = doc.rootVisualElement;

        _overlay   = root.Q<VisualElement>("pause-overlay");
        _resumeBtn = root.Q<Button>("btn-resume");
        _quitBtn   = root.Q<Button>("btn-quit");

        if (_resumeBtn != null) _resumeBtn.clicked += Resume;
        if (_quitBtn   != null) _quitBtn.clicked   += QuitToMenu;

        _ready = true;
        Hide();
    }

    private void OnDisable()
    {
        if (_resumeBtn != null) _resumeBtn.clicked -= Resume;
        if (_quitBtn   != null) _quitBtn.clicked   -= QuitToMenu;
        _ready = false;
    }

    private void Update()
    {
        // Poll PausePressed from any local PlayerInputHandler
        var handlers = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);
        foreach (var h in handlers)
        {
            var net = h.GetComponent<NetworkObject>();
            if (net != null && !net.IsOwner) continue; // only local player
            if (h.PausePressed)
            {
                Toggle();
                break;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────

    public void Toggle() { if (_isPaused) Resume(); else Pause(); }

    public void Pause()
    {
        _isPaused = true;
        if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        _isPaused = false;
        Hide();
        Time.timeScale = 1f;
    }

    private void QuitToMenu()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        // Disconnect cleanly then go to main menu
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
                NetworkManager.Singleton.Shutdown();
            else
                NetworkManager.Singleton.Shutdown();
        }
        GameManager.Instance?.ChangeState(GameState.MainMenu);
    }

    private void Hide()
    {
        if (_overlay != null) _overlay.style.display = DisplayStyle.None;
    }
}
