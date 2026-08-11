using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles all physics and movement for the local player.
/// Reads from PlayerInputHandler. Drives KinematicActor.
/// </summary>
[RequireComponent(typeof(KinematicActor))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    private KinematicActor     _actor;
    private PlayerInputHandler _input;
    private PlayerStats        _stats;
    private PlayerStatsSO      _data;
    private NetworkObject      _netObj;
    private PlayerNetworkSync  _netSync;

    // State for Animator
    public bool IsGrounded => _actor.IsGrounded;
    public bool IsMoving   { get; private set; }
    public bool IsDashing  { get; private set; }

    private float _coyoteTimeCounter;
    private float _coyoteTimeDuration = 0.12f;

    private bool _hasJumpedSinceGrounded = false; // Gates jump to one per ground contact

    private bool  _canDash = true;
    private float _dashCooldownTimer;

    private float _dashTimeCounter;
    private float _dashDuration = 0.2f;

    public int  FacingDirection { get; private set; } = 1;
    private bool _jumpCutApplied = false; // Guard: prevents CutJump from running every Update frame

    private void Awake()
    {
        _actor = GetComponent<KinematicActor>();
        _input = GetComponent<PlayerInputHandler>();
        _stats = GetComponent<PlayerStats>();
        _netObj = GetComponent<NetworkObject>();
        _netSync = GetComponent<PlayerNetworkSync>();
    }

    private void Start()
    {
        _data = _stats.Data;
    }

    private void Update()
    {
        // Only owner or server (to simulate others) should run logic
        if (!_netObj.IsOwner && !NetworkManager.Singleton.IsServer) return;

        bool jumpHeld = _netObj.IsOwner ? _input.JumpHeld : _netSync.JumpHeld;
        Vector2 move = _netObj.IsOwner ? _input.MoveInput : _netSync.MoveInput;

        UpdateCoyoteTime();
        HandleDashCooldown();
        HandleDashState();
        UpdateFacingDirection(move);
        
        // Handle variable jump height (cut ONCE when jump button is released)
        // Note: For now, jump cut is only predicted locally.
        if (jumpHeld)
        {
            _jumpCutApplied = false; // reset when holding jump
        }
        else if (!_jumpCutApplied && _actor.Velocity.y > 0)
        {
            _actor.CutJump();
            _jumpCutApplied = true;
        }
    }

    private void FixedUpdate()
    {
        if (!_netObj.IsOwner && !NetworkManager.Singleton.IsServer) return;

        if (IsDashing) 
        {
             _actor.ApplyGravity = false;
             _actor.SetTargetVelocity(new Vector2(FacingDirection * (_data.DashForce / _actor.maxSpeed), 0));
             _actor.Velocity = new Vector2(FacingDirection * _data.DashForce, 0); 
             return; 
        }

        _actor.ApplyGravity = true;
        Move();

        // Jump processing
        // Note: On the host, RequestJumpServerRpc (sent from Update) executes synchronously and
        // already called Jump(). Consume the buffer so we don't double-jump in the same frame.
        bool shouldJump = false;
        if (_netObj.IsOwner)
        {
            if (CanJump() && _input.JumpBuffered)
            {
                shouldJump = true;
                _input.ConsumeJumpBuffer();
            }
        }
        // Server handles jumped triggered via RPC (which calls Jump() directly)

        if (shouldJump)
        {
            Jump();
        }
        if (_netObj.IsOwner && _input.DashPressed && _canDash)
        {
            Dash();
        }
    }

    private void Move()
    {
        // Read input from local handler if owner, or synced variable if server simulating proxy
        Vector2 input = _netObj.IsOwner ? _input.MoveInput : _netSync.MoveInput;

        // Tell the actor what direction to move
        _actor.SetTargetVelocity(new Vector2(input.x, 0));

        IsMoving = Mathf.Abs(_actor.Velocity.x) > 0.1f;
    }

    public bool CanJump()
    {
        // One jump per ground contact: once we jump, require landing (grounded) again
        // before the next jump is allowed. Coyote time extends the window slightly
        // after leaving a ledge, but never grants a second jump while still airborne.
        if (_hasJumpedSinceGrounded) return false;
        return IsGrounded || _coyoteTimeCounter > 0f;
    }

    public void Jump()
    {
        _coyoteTimeCounter = 0f;
        _jumpCutApplied    = false; // Allow CutJump on the new jump
        _hasJumpedSinceGrounded = true;
        _actor.ForceJump();
    }

    public void Dash()
    {
        IsDashing = true;
        _canDash  = false;
        
        _dashTimeCounter   = _dashDuration;
        _dashCooldownTimer = _data.DashCooldown;

        // Zero out velocity completely before dashing
        _actor.Velocity = Vector2.zero;
        _actor.ApplyGravity = false;
    }

    private void HandleDashState()
    {
        if (IsDashing)
        {
            _dashTimeCounter -= Time.deltaTime;
            if (_dashTimeCounter <= 0f)
            {
                IsDashing = false;
                _actor.Velocity = new Vector2(0f, _actor.Velocity.y); // Stop dash momentum
                _actor.ApplyGravity = true;
            }
        }
    }

    private void UpdateCoyoteTime()
    {
        // Reset coyote time if grounded and moving down or standing still
        if (IsGrounded && _actor.Velocity.y <= 0)
        {
            _coyoteTimeCounter = _coyoteTimeDuration;
            _hasJumpedSinceGrounded = false; // landed again → jump is available
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleDashCooldown()
    {
        if (!_canDash && !IsDashing)
        {
            _dashCooldownTimer -= Time.deltaTime;
            if (_dashCooldownTimer <= 0)
                _canDash = true;
        }
    }

    private void UpdateFacingDirection(Vector2 moveInput)
    {
        if (moveInput.x > 0.01f) FacingDirection = 1;
        else if (moveInput.x < -0.01f) FacingDirection = -1;

        // Flip visual transform
        Vector3 scale = transform.localScale;
        scale.x = FacingDirection * Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    /// <summary>Instantly moves the player to a position and resets physics state. Called by server-driven respawn.</summary>
    public void Teleport(Vector3 position)
    {
        _actor.TeleportTo(position); // syncs both _rb.position and transform.position
        _coyoteTimeCounter  = 0f;
        _jumpCutApplied     = false;
        IsDashing           = false;
        _actor.ApplyGravity = true;
        Debug.Log($"[PlayerController] Teleported to {position}");
    }
}
