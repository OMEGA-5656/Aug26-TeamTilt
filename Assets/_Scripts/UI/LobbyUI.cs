using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the Lobby Screen using UI Toolkit.
/// Uses a simple poll-every-frame approach so it never misses player list updates,
/// regardless of network spawn timing.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LobbyUI : MonoBehaviour
{
    private Label      _statusLabel;
    private ScrollView _playerScrollView;
    private Button     _startBtn;
    private Button     _leaveBtn;

    private bool _uiReady;
    private bool _subscribed;
    private int  _lastCount = -1;

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _statusLabel      = root.Q<Label>("status-label");
        _playerScrollView = root.Q<ScrollView>("player-scroll-view");
        _startBtn         = root.Q<Button>("start-btn");
        _leaveBtn         = root.Q<Button>("leave-btn");

        if (_startBtn != null)
        {
            _startBtn.clicked += OnStartClicked;
            _leaveBtn.clicked += OnLeaveClicked;
            _uiReady = true;
            Debug.Log("[LobbyUI] Awake — UI elements found.");
        }
        else
        {
            Debug.LogError("[LobbyUI] Awake — 'start-btn' NOT FOUND in UXML!");
        }
    }

    private void Update()
    {
        if (!_uiReady) return;
        if (LobbyManager.Instance == null) return;
        if (LobbyManager.Instance.PlayerList == null) return;

        // Subscribe once
        if (!_subscribed)
        {
            _subscribed = true;
            Debug.Log($"[LobbyUI] Subscribed. IsServer={NetworkManager.Singleton?.IsServer}");
            LobbyManager.Instance.PlayerList.OnListChanged += OnListChanged;
        }

        // Poll for changes
        int c = LobbyManager.Instance.PlayerList.Count;
        if (c != _lastCount)
        {
            _lastCount = c;
            Debug.Log($"[LobbyUI] Count changed → {c}");
            RefreshUI();
        }
    }

    private void OnListChanged(NetworkListEvent<LobbyPlayerData> e)
    {
        Debug.Log($"[LobbyUI] OnListChanged fired: Type={e.Type}");
        RefreshUI();
    }

    private void OnDisable()
    {
        if (_startBtn != null) _startBtn.clicked -= OnStartClicked;
        if (_leaveBtn != null) _leaveBtn.clicked -= OnLeaveClicked;

        if (_subscribed && LobbyManager.Instance?.PlayerList != null)
            LobbyManager.Instance.PlayerList.OnListChanged -= OnListChanged;
        _subscribed = false;
        _lastCount = -1;
    }

    private void RefreshUI()
    {
        if (_playerScrollView == null) return;

        var players = LobbyManager.Instance?.PlayerList;
        if (players == null) return;

        _playerScrollView.Clear();

        for (int i = 0; i < players.Count; i++)
        {
            var p      = players[i];
            bool local = NetworkManager.Singleton != null &&
                         p.ClientId == NetworkManager.Singleton.LocalClientId;

            var row = new VisualElement();
            row.AddToClassList("player-row");

            var name = new Label(p.DisplayName.ToString());
            name.AddToClassList("player-name");

            var tag = new Label(local ? "YOU" : "READY");
            tag.AddToClassList("player-ready-status");

            row.Add(name);
            row.Add(tag);
            _playerScrollView.Add(row);
        }

        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
        {
            _startBtn.style.display = DisplayStyle.Flex;
            _startBtn.SetEnabled(LobbyManager.Instance.CanStartGame());
            if (_statusLabel != null)
                _statusLabel.text = $"Hosting — {players.Count} player(s)";
        }
        else
        {
            _startBtn.style.display = DisplayStyle.None;
            if (_statusLabel != null)
                _statusLabel.text = $"Waiting for host… {players.Count} player(s)";
        }
    }

    private void OnStartClicked()
    {
        if (NetworkManager.Singleton?.IsServer == true)
            LobbyManager.Instance?.StartGameServerRpc();
    }

    private void OnLeaveClicked()
    {
        Debug.Log("[LobbyUI] Leave clicked.");
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.Disconnect();
        }
        else
        {
            // Fallback: manually shut down and go to main menu
            Debug.LogWarning("[LobbyUI] ConnectionManager.Instance is null — shutting down directly.");
            NetworkManager.Singleton?.Shutdown();
            SceneNav.Load(SceneNames.MainMenu);
        }
    }
}
