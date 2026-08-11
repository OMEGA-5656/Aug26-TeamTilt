using UnityEngine;

/// <summary>
/// Clamps the local player's position within the camera's current visible boundary
/// when the camera is at its maximum zoom level.
/// Attach to the Player prefab alongside PlayerController.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBoundaryEnforcer : MonoBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] private float _margin = 0.3f; // half-width of the player sprite

    private void Awake() => _rb = GetComponent<Rigidbody2D>();

    private void FixedUpdate()
    {
        if (PicoParkCamera.Instance == null) return;
        if (!IsLocalPlayer()) return;

        Rect bounds = PicoParkCamera.Instance.CurrentBounds;
        Vector2 pos = _rb.position;

        float clampedX = Mathf.Clamp(pos.x, bounds.xMin + _margin, bounds.xMax - _margin);
        float clampedY = Mathf.Clamp(pos.y, bounds.yMin + _margin, bounds.yMax - _margin * 0.5f);

        if (clampedX != pos.x || clampedY != pos.y)
        {
            _rb.position = new Vector2(clampedX, clampedY);
            _rb.linearVelocity = new Vector2(
                clampedX != pos.x ? 0f : _rb.linearVelocity.x,
                clampedY != pos.y ? 0f : _rb.linearVelocity.y
            );
        }
    }

    private bool IsLocalPlayer()
    {
        var netObj = GetComponent<Unity.Netcode.NetworkObject>();
        return netObj == null || netObj.IsOwner;
    }
}
