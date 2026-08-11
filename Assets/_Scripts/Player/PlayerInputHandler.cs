using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Reads player input from the New Input System (keyboard/gamepad)
/// AND from virtual buttons injected by TouchControlsUI on mobile.
/// PlayerController reads the final combined values.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Asset")]
    [SerializeField] private InputActionAsset _actionsAsset;

    // ── Exposed state (PlayerController reads these) ──────────
    public Vector2 MoveInput     { get; private set; }
    public bool    JumpPressed   { get; private set; }
    public bool    JumpHeld      { get; private set; }
    public bool    DashPressed   { get; private set; }
    public bool    PausePressed  { get; private set; }
    public bool    JumpBuffered  { get; private set; }

    // ── Virtual input (set by TouchControlsUI) ─────────────────
    private Vector2 _virtualMove;
    private bool    _virtualJump;
    private bool    _virtualJumpHeld;
    private bool    _virtualDash;

    [SerializeField] private float _jumpBufferDuration = 0.1f;
    private float _jumpBufferTimer;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;
    private InputAction _pauseAction;
    private NetworkObject _netObj;
    private PlayerNetworkSync _netSync;

    private bool _actionsValid;

    private void Awake()
    {
        _netObj = GetComponent<NetworkObject>();
        _netSync = GetComponent<PlayerNetworkSync>();
        if (_actionsAsset == null)
        {
            Debug.LogError("[PlayerInputHandler] FATAL: No InputActionAsset assigned! Keyboard/Gamepad will NOT work. Please assign 'TeamTiltActions' in the inspector.");
            return;
        }

        var playerMap = _actionsAsset.FindActionMap("Player", throwIfNotFound: false);
        if (playerMap == null)
        {
            Debug.LogError("[PlayerInputHandler] 'Player' action map not found!");
            return;
        }

        _moveAction  = playerMap.FindAction("Move",  throwIfNotFound: false);
        _jumpAction  = playerMap.FindAction("Jump",  throwIfNotFound: false);
        _dashAction  = playerMap.FindAction("Dash",  throwIfNotFound: false);
        _pauseAction = playerMap.FindAction("Pause", throwIfNotFound: false);
        _actionsValid = _moveAction != null && _jumpAction != null;

        if (!_actionsValid)
            Debug.LogError("[PlayerInputHandler] FATAL: Move or Jump action not found in 'Player' map! Did the action names change?");
        else
            Debug.Log("[PlayerInputHandler] Successfully bound Player Input Actions.");
    }

    private void OnEnable()
    {
        if (!_actionsValid) return;
        _moveAction?.Enable();
        _jumpAction?.Enable();
        _dashAction?.Enable();
        _pauseAction?.Enable();
        _jumpAction.performed  += OnJumpPerformed;
        _jumpAction.canceled   += OnJumpCanceled;
        _dashAction.performed  += OnDashPerformed;
        _pauseAction.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (!_actionsValid) return;
        _jumpAction.performed  -= OnJumpPerformed;
        _jumpAction.canceled   -= OnJumpCanceled;
        _dashAction.performed  -= OnDashPerformed;
        _pauseAction.performed -= OnPausePerformed;
        _moveAction?.Disable();
        _jumpAction?.Disable();
        _dashAction?.Disable();
        _pauseAction?.Disable();
    }

    private void Update()
    {
        // Skip reading input if this is a network proxy
        if (_netObj != null && !_netObj.IsOwner)
        {
             MoveInput = Vector2.zero;
             JumpPressed = false;
             JumpHeld = false;
             DashPressed = false;
             PausePressed = false;
             return;
        }

        // Combine hardware + virtual move
        Vector2 hwMove = _actionsValid ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        MoveInput = hwMove + _virtualMove;

        // Clamp so diagonal doesn't exceed magnitude 1
        if (MoveInput.sqrMagnitude > 1f) MoveInput = MoveInput.normalized;

        // Combine jump held
        JumpHeld = (_actionsValid && _jumpAction.IsPressed()) || _virtualJumpHeld;

        // Sync to server
        if (_netSync != null) _netSync.UpdateInput(MoveInput, JumpHeld);

        // Jump buffer countdown
        if (JumpBuffered)
        {
            _jumpBufferTimer -= Time.deltaTime;
            if (_jumpBufferTimer <= 0f) JumpBuffered = false;
        }
    }

    private void LateUpdate()
    {
        // Clear one-frame pulses
        JumpPressed  = false;
        DashPressed  = false;
        PausePressed = false;
        _virtualJump = false;
        _virtualDash = false;
    }

    // ── Virtual input API (called by TouchControlsUI) ──────────

    /// <summary>Set the virtual joystick direction (−1..1 on x/y).</summary>
    public void SetVirtualMove(Vector2 dir) => _virtualMove = dir;

    /// <summary>Call when touch jump button is pressed down.</summary>
    public void VirtualJumpDown()
    {
        _virtualJumpHeld = true;
        _virtualJump     = true;
        JumpPressed      = true;
        JumpBuffered     = true;
        _jumpBufferTimer = _jumpBufferDuration;

        if (_netSync != null) _netSync.RequestJumpServerRpc();
    }

    /// <summary>Call when touch jump button is released.</summary>
    public void VirtualJumpUp() => _virtualJumpHeld = false;

    /// <summary>Call when touch dash button is pressed.</summary>
    public void VirtualDash()
    {
        _virtualDash = true;
        DashPressed  = true;
        
        if (_netSync != null) _netSync.RequestDashServerRpc();
    }

    public void ConsumeJumpBuffer()
    {
        JumpBuffered     = false;
        _jumpBufferTimer = 0f;
    }

    // ── Hardware callbacks ─────────────────────────────────────

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (_netObj != null && !_netObj.IsOwner) return;
        JumpPressed      = true;
        JumpHeld         = true;
        JumpBuffered     = true;
        _jumpBufferTimer = _jumpBufferDuration;

        if (_netSync != null) _netSync.RequestJumpServerRpc();
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx) 
    {
        if (_netObj != null && !_netObj.IsOwner) return;
        JumpHeld = false;
    }

    private void OnDashPerformed(InputAction.CallbackContext ctx) 
    {
        if (_netObj != null && !_netObj.IsOwner) return;
        DashPressed = true;

        if (_netSync != null) _netSync.RequestDashServerRpc();
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx) 
    {
        if (_netObj != null && !_netObj.IsOwner) return;
        PausePressed = true;
    }
}
