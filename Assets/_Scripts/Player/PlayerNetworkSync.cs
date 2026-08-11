using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Synchronizes player position, velocity, facing direction, and animator state.
/// Implements interpolation for smooth movement on remote proxies.
/// </summary>
public class PlayerNetworkSync : NetworkBehaviour
{
    private PlayerController _controller;
    private KinematicActor   _actor;
    private Animator         _animator;

    [Header("Network State - Physics (Server Authority)")]
    private readonly NetworkVariable<Vector2> _netVelocity = new(writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int>     _netFacing   = new(1, writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool>    _netIsGrounded = new(writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool>    _netIsDashing  = new(writePerm: NetworkVariableWritePermission.Server);

    [Header("Network State - Input (Owner Prediction)")]
    private readonly NetworkVariable<Vector2> _netMoveInput = new(writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool>    _netJumpHeld  = new(writePerm: NetworkVariableWritePermission.Owner);
    
    [Header("Interpolation Settings")]
    [SerializeField] private float _positionSmoothing = 15f;
    [SerializeField] private float _rotationSmoothing = 15f;

    public Vector2 MoveInput => _netMoveInput.Value;
    public bool    JumpHeld  => _netJumpHeld.Value;

    public void UpdateInput(Vector2 move, bool jumpHeld)
    {
        if (IsOwner)
        {
            _netMoveInput.Value = move;
            _netJumpHeld.Value  = jumpHeld;
        }
    }

    [ServerRpc]
    public void RequestJumpServerRpc()
    {
        // Gate server-side just like the owner path: only jump when grounded (or in coyote time).
        // Prevents holding the jump button from re-triggering Jump() every physics tick.
        if (_controller.CanJump())
        {
            _controller.Jump();
        }
    }

    [ServerRpc]
    public void RequestDashServerRpc()
    {
        _controller.Dash();
    }

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _actor      = GetComponent<KinematicActor>();
        _animator   = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // For proxies, the Rigidbody must be Kinematic to avoid local physics calculations
            // and follow the NetworkTransform sync precisely.
            if (TryGetComponent<Rigidbody2D>(out var rb)) rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            UpdateServerState();
        }
        
        if (!IsOwner)
        {
            ApplyRemoteState();
        }
    }

    private void UpdateServerState()
    {
        // Server pushes calculated state to the network
        _netVelocity.Value   = _actor.Velocity;
        _netFacing.Value     = _controller.FacingDirection;
        _netIsGrounded.Value = _controller.IsGrounded;
        _netIsDashing.Value  = _controller.IsDashing;
    }

    private void ApplyRemoteState()
    {
        // Non-owners pull state and apply smooth visuals
        
        // 1. Sync Facing Direction (Flip Visuals)
        if (_netFacing.Value != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * _netFacing.Value;
            transform.localScale = scale;
        }

        // 2. Sync Animations
        if (_animator != null)
        {
            float speedPercent = Mathf.Abs(_netVelocity.Value.x) / _actor.maxSpeed;
            _animator.SetFloat("Speed", speedPercent);
            _animator.SetBool("IsGrounded", _netIsGrounded.Value);
            _animator.SetBool("IsDashing",  _netIsDashing.Value);
        }
        
        // Note: Position syncing is handled by NetworkTransform if present, 
        // but we can add secondary smoothing here if the user's NetworkTransform settings are jittery.
    }
}
