using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class KinematicActor : MonoBehaviour
{
    [Header("Movement Stats")]
    public float maxSpeed = 8f;
    public float acceleration = 50f;
    public float groundFriction = 50f;
    public float airFriction = 20f;
    [Tooltip("Maximum fall speed")]
    public float terminalVelocity = 20f;

    [Header("Jump Stats")]
    public float maxJumpHeight = 3f;
    public float timeToJumpApex = 0.4f;
    [Tooltip("Gravity is multiplied by this when falling to make jumps feel weighty")]
    public float fallGravityMultiplier = 1.6f;

    // Physics State
    public Vector2 Velocity;
    private Vector2 _targetVelocity;
    public bool ApplyGravity = true;

    public bool IsGrounded { get; private set; }
    private BoxCollider2D _collider;
    private Rigidbody2D _rb;
    private NetworkObject _netObj;

    [Header("Collision Layers")]
    [Tooltip("Layers this actor should collide with. Do NOT include the Player layer.")]
    public LayerMask collisionMask;

    // Derived physics stats
    private float _gravity;
    private float _jumpVelocity;

    // Standard buffer for all kinematic math. 
    // We stay at least this far away from surfaces to avoid precision overlaps.
    private const float SkinWidth = 0.015f; 

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _rb = GetComponent<Rigidbody2D>();
        _netObj = GetComponent<NetworkObject>();

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.useFullKinematicContacts = true;
        // This mover writes _rb.position directly (custom kinematic movement), which bypasses
        // Rigidbody2D interpolation. Leaving Interpolate enabled makes the renderer fight between
        // the interpolated and raw positions every frame → visible mini-teleports when moving.
        // Interpolation must be None for direct position writes to render smoothly.
        _rb.interpolation = RigidbodyInterpolation2D.None;
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        CalculateGravity();
    }

    private void OnValidate() { CalculateGravity(); }

    private void CalculateGravity()
    {
        _gravity = -(2f * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2f);
        _jumpVelocity = Mathf.Abs(_gravity) * timeToJumpApex;
    }

    private void OnEnable() { if (KinematicPhysicsManager.Instance != null) KinematicPhysicsManager.Instance.RegisterActor(this); }
    private void Start() { if (KinematicPhysicsManager.Instance != null) KinematicPhysicsManager.Instance.RegisterActor(this); }
    private void OnDisable() { if (KinematicPhysicsManager.Instance != null) KinematicPhysicsManager.Instance.UnregisterActor(this); }

    public void SetTargetVelocity(Vector2 target) => _targetVelocity = target;

    public void ForceJump()
    {
        Velocity.y = _jumpVelocity;
        IsGrounded = false;
    }

    public void CutJump()
    {
        if (Velocity.y > 0f)
            Velocity.y *= 0.5f;
    }

    public void TeleportTo(Vector2 position)
    {
        _rb.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        Velocity = Vector2.zero;
        IsGrounded = false;
    }

    public void CalculateVelocity(float dt)
    {
        if (!_netObj.IsOwner && !NetworkManager.Singleton.IsServer) return;

        if (ApplyGravity)
        {
            float g = (Velocity.y < 0f) ? _gravity * fallGravityMultiplier : _gravity;
            Velocity.y = Mathf.Max(Velocity.y + g * dt, -terminalVelocity);
        }

        float friction = IsGrounded ? groundFriction : airFriction;
        float rate = (Mathf.Abs(_targetVelocity.x) > 0.01f) ? acceleration : friction;
        Velocity.x = Mathf.MoveTowards(Velocity.x, _targetVelocity.x * maxSpeed, rate * dt);
    }

    public void MoveActor(float dt)
    {
        if (!_netObj.IsOwner && !NetworkManager.Singleton.IsServer) return;

        if (_collider == null || _rb == null) return;

        // 1. Setup dimensions correctly for scale
        Vector2 lossyScale = transform.lossyScale;
        Vector2 absScale = new Vector2(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
        
        Vector2 colOffset = Vector2.Scale(_collider.offset, lossyScale); 
        Vector2 colSize   = Vector2.Scale(_collider.size, absScale);
        
        // Cast box is slightly inset to avoid "edge catching" which causes jitter on vertical seams
        Vector2 castSize = colSize - (Vector2.one * SkinWidth * 2.5f);
        castSize.x = Mathf.Max(castSize.x, 0.01f);
        castSize.y = Mathf.Max(castSize.y, 0.01f);

        Vector2 pos = _rb.position;

        // 2. Direct Depenetration (Resolve Overlaps using current pos)
        // Physics2D.OverlapBox is more reliable than Rigidbody-synced OverlapCollider
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(pos + colOffset, colSize, 0f, collisionMask);
        foreach (var over in overlaps)
        {
            if (over == null || over.isTrigger || over.transform.IsChildOf(transform)) continue;
            
            ColliderDistance2D d = _collider.Distance(over);
            if (d.isValid && d.isOverlapped && d.distance < -0.0001f)
            {
                // normal is surface normal on 'over'. distance is negative overlap.
                // Subtract normal * distance to move Player AWAY from 'over'.
                pos += d.normal * d.distance;
            }
        }
        
        // Sync RB position IMMEDIATELY to origin for following casts
        _rb.position = pos;

        IsGrounded = false;

        // 3. Horizontal Movement with "Already Sticking" guards
        float moveX = Velocity.x * dt;
        if (Mathf.Abs(moveX) > 0.0001f)
        {
            Vector2 dirX = new Vector2(Mathf.Sign(moveX), 0);
            float dist   = Mathf.Abs(moveX);
            
            // Cast slightly further than move to ensure we hit even if currently exactly flush
            RaycastHit2D hit = Physics2D.BoxCast(pos + colOffset, castSize, 0, dirX, dist + SkinWidth, collisionMask);
            if (IsValidHit(hit))
            {
                // If we hit something closer than our intended travel dist, cap it
                float allowedMove = Mathf.Max(0, hit.distance - SkinWidth);
                pos.x += dirX.x * allowedMove;
                Velocity.x = 0;
            }
            else
            {
                pos.x += moveX;
            }
            _rb.position = pos; // Sync between axes
        }

        // 4. Vertical Movement
        float moveY = Velocity.y * dt;
        if (Mathf.Abs(moveY) > 0.0001f)
        {
            Vector2 dirY = new Vector2(0, Mathf.Sign(moveY));
            float dist   = Mathf.Abs(moveY);

            RaycastHit2D hit = Physics2D.BoxCast(pos + colOffset, castSize, 0, dirY, dist + SkinWidth, collisionMask);
            if (IsValidHit(hit))
            {
                // Surface detection for grounding
                if (Velocity.y < 0 && hit.normal.y > 0.7f) IsGrounded = true;

                float allowedMove = Mathf.Max(0, hit.distance - SkinWidth);
                pos.y += dirY.y * allowedMove;
                Velocity.y = 0;
            }
            else
            {
                pos.y += moveY;
            }
            _rb.position = pos;
        }

        // 5. Improved Ground Snapping
        if (Velocity.y <= 0)
        {
            RaycastHit2D hit = Physics2D.BoxCast(pos + colOffset, castSize, 0, Vector2.down, SkinWidth * 2f, collisionMask);
            if (IsValidHit(hit) && hit.normal.y > 0.7f)
            {
                IsGrounded = true;
                // Snap to surface with SkinWidth gap
                pos.y = hit.point.y + (colSize.y * 0.5f) - colOffset.y + SkinWidth;
                Velocity.y = 0;
            }
        }

        // 6. MovePosition to sync physics engine internals
        _rb.MovePosition(pos);
    }

    private bool IsValidHit(RaycastHit2D hit)
    {
        return hit.collider != null && !hit.collider.isTrigger && !hit.collider.transform.IsChildOf(transform);
    }
}

