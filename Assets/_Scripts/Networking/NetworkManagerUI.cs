using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Handles Host/Join UI for starting or connecting to a game session.
/// Attach to a UI GameObject in the MainMenu scene.
/// </summary>
public class NetworkManagerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private TMP_Text _statusText;

    [Header("Settings")]
    [SerializeField] private string _defaultIP = "127.0.0.1";
    [SerializeField] private ushort _port = 7777;

    private void Start()
    {
        _hostButton.onClick.AddListener(OnHostClicked);
        _joinButton.onClick.AddListener(OnJoinClicked);

        EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
    }

    private void OnHostClicked()
    {
        SetStatus("Starting host...");
        ConfigureTransport(_defaultIP);
        NetworkManager.Singleton.StartHost();
        GameManager.Instance?.ChangeState(GameState.Lobby);
    }

    private void OnJoinClicked()
    {
        string ip = string.IsNullOrEmpty(_ipInput.text) ? _defaultIP : _ipInput.text;
        SetStatus($"Connecting to {ip}...");
        ConfigureTransport(ip);
        NetworkManager.Singleton.StartClient();
    }

    private void ConfigureTransport(string ip)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(ip, _port);
        }
    }

    private void OnStateChanged(GameStateChangedEvent evt)
    {
        switch (evt.Current)
        {
            case GameState.Lobby:
                SetStatus("In Lobby — waiting for players...");
                break;
            case GameState.InGame:
                SetStatus("Game started!");
                break;
        }
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
        Debug.Log($"[NetworkManagerUI] {message}");
    }
}
