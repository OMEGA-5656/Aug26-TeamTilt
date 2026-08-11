using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Settings screen UI. Accessible from the Main Menu.
/// Allows toggling touch controls. Defers UI setup to avoid async UIDocument null issues.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SettingsUI : MonoBehaviour
{
    private Toggle        _touchToggle;
    private Button        _backBtn;
    private VisualElement _panel;
    private bool          _ready;
    private bool          _pendingOpen;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc?.rootVisualElement == null) return;
        // UIDocument builds its visual tree asynchronously — schedule setup
        doc.rootVisualElement.schedule.Execute(Setup).ExecuteLater(60);
    }

    private void Setup()
    {
        var doc = GetComponent<UIDocument>();
        if (doc?.rootVisualElement == null) return;
        var root = doc.rootVisualElement;

        _panel       = root.Q<VisualElement>("root");
        _touchToggle = root.Q<Toggle>("touch-toggle");
        _backBtn     = root.Q<Button>("back-btn");

        if (_touchToggle != null)
        {
            _touchToggle.value = TouchControlsUI.GetTouchControlsEnabled();
            _touchToggle.RegisterValueChangedCallback(evt =>
                TouchControlsUI.SetTouchControlsEnabled(evt.newValue));
        }
        if (_backBtn != null) _backBtn.clicked += Close;

        _ready = true;

        if (_pendingOpen) { _pendingOpen = false; ShowPanel(); }
        else Hide();
    }

    private void OnDisable()
    {
        if (_backBtn != null) _backBtn.clicked -= Close;
        _ready = false;
    }

    // ── Public API ─────────────────────────────────────────────

    public void Open()
    {
        if (_ready) ShowPanel();
        else _pendingOpen = true; // will show once Setup() completes
    }

    public void Close()
    {
        _pendingOpen = false;
        Hide();
    }

    private void ShowPanel()
    {
        if (_panel != null) _panel.style.display = DisplayStyle.Flex;
    }

    private void Hide()
    {
        if (_panel != null) _panel.style.display = DisplayStyle.None;
    }
}
