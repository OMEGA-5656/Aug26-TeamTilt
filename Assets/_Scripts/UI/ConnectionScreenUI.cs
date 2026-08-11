using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the Connection Screen using UI Toolkit.
/// Assumes a UIDocument is attached pointing to ConnectionScreen.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ConnectionScreenUI : MonoBehaviour
{
    private Button    _hostBtn;
    private Button    _joinBtn;
    private Button    _backBtn;
    private TextField _ipField;
    private Label     _statusLabel;

    private bool _connecting;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _hostBtn     = root.Q<Button>("host-btn");
        _joinBtn     = root.Q<Button>("join-btn");
        _backBtn     = root.Q<Button>("back-btn");
        _ipField     = root.Q<TextField>("ip-field");
        _statusLabel = root.Q<Label>("status-label");

        if (_hostBtn == null)
        {
            Debug.LogError("[ConnectionScreenUI] Missing UXML elements!");
            return;
        }

        _hostBtn.clicked += OnHostClicked;
        _joinBtn.clicked += OnJoinClicked;
        _backBtn.clicked += OnBackClicked;

        SetStatus("Choose to host or join a game.");

        if (ConnectionManager.Instance != null)
            ConnectionManager.Instance.OnConnectedToHost += OnSuccessfullyJoined;
    }

    private void OnDisable()
    {
        if (_hostBtn != null) _hostBtn.clicked -= OnHostClicked;
        if (_joinBtn != null) _joinBtn.clicked -= OnJoinClicked;
        if (_backBtn != null) _backBtn.clicked -= OnBackClicked;

        if (ConnectionManager.Instance != null)
            ConnectionManager.Instance.OnConnectedToHost -= OnSuccessfullyJoined;
    }

    private void OnHostClicked()
    {
        if (_connecting) return;
        _connecting = true;
        SetStatus("Starting host…");
        SetInteractable(false);

        bool ok = ConnectionManager.Instance != null && ConnectionManager.Instance.StartHost();

        if (ok)
        {
            SetStatus("Hosting! Loading lobby…");
        }
        else
        {
            SetStatus("Failed to start host. Check console.");
            SetInteractable(true);
            _connecting = false;
        }
    }

    private void OnJoinClicked()
    {
        if (_connecting) return;
        _connecting = true;

        string ip = string.IsNullOrWhiteSpace(_ipField.value) ? "127.0.0.1" : _ipField.value.Trim();
        SetStatus($"Connecting to {ip}…");
        SetInteractable(false);

        ConnectionManager.Instance?.StartClient(ip);
    }

    private void OnSuccessfullyJoined()
    {
        SetStatus("Connected! Loading lobby…");
    }

    private void OnBackClicked()
    {
        SceneNav.Load(SceneNames.MainMenu);
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null) _statusLabel.text = msg;
        Debug.Log($"[ConnectionScreenUI] {msg}");
    }

    private void SetInteractable(bool on)
    {
        if (_hostBtn != null) _hostBtn.SetEnabled(on);
        if (_joinBtn != null) _joinBtn.SetEnabled(on);
        if (_backBtn != null) _backBtn.SetEnabled(on);
        if (_ipField != null) _ipField.SetEnabled(on);
    }
}
