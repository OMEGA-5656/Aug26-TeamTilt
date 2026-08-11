using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game state machine. Manages transitions between all game states.
/// Uses a singleton pattern with DontDestroyOnLoad.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Current State (read-only)")]
    [SerializeField] private GameState _currentState = GameState.MainMenu;

    public GameState CurrentState => _currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() => EnterState(_currentState);

    public void ChangeState(GameState newState)
    {
        if (_currentState == newState) return;
        ExitState(_currentState);
        var evt = new GameStateChangedEvent { Previous = _currentState, Current = newState };
        _currentState = newState;
        EnterState(newState);
        EventBus.Publish(evt);
    }

    private void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                Debug.Log("[GameManager] Entered MainMenu");
                SceneNav.Load(SceneNames.MainMenu);
                break;

            case GameState.Lobby:
                Debug.Log("[GameManager] Entered Lobby");
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    Debug.Log("[GameManager] Loading Lobby via NGO SceneManager");
                    NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.Lobby, LoadSceneMode.Single);
                }
                else
                {
                    SceneNav.Load(SceneNames.Lobby);
                }
                break;

            case GameState.LevelSelect:
                Debug.Log("[GameManager] Entered LevelSelect");
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.LevelSelect, LoadSceneMode.Single);
                }
                else
                {
                    SceneNav.Load(SceneNames.LevelSelect);
                }
                break;

            case GameState.InGame:
                Debug.Log("[GameManager] Entered InGame");
                // Scene loading handled by LevelSelectManager when host picks a level
                break;

            case GameState.Loading:
                Debug.Log("[GameManager] Entered Loading");
                break;

            case GameState.GameOver:
                Debug.Log("[GameManager] Entered GameOver");
                break;
        }
    }

    private void ExitState(GameState state) { /* cleanup per state */ }
}

/// <summary>All possible high-level game states.</summary>
public enum GameState
{
    MainMenu,
    Lobby,
    LevelSelect,
    Loading,
    InGame,
    GameOver
}
