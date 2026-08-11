using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Placed on the exit door.
/// Notifies GameLevelManager when a player enters/exits.
/// When ALL players are inside simultaneously the level is cleared.
/// </summary>
public class DoorTrigger : MonoBehaviour
{
    private Collider2D _trigger;
    private bool _playerInside;

    private void Awake()
    {
        _trigger = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        Debug.Log($"[DoorTrigger] Player {netObj.OwnerClientId} entered door.");
        GameLevelManager.Instance?.PlayerEnteredDoorServerRpc(netObj.OwnerClientId);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        Debug.Log($"[DoorTrigger] Player {netObj.OwnerClientId} exited door.");
        GameLevelManager.Instance?.PlayerExitedDoorServerRpc(netObj.OwnerClientId);
    }

    /// <summary>
    /// Fallback for the kinematic mover: direct Rigidbody2D position writes can skip
    /// Unity's trigger callbacks, so we manually overlap-check each frame as a safety net.
    /// </summary>
    private void FixedUpdate()
    {
        if (_trigger == null) return;

        // Only the local player's controller matters for this fallback; the authoritative
        // RPC calls are made by the owner, so check via the player's collider overlap.
        var player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        var netObj = player.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        var playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null) return;

        bool inside = _trigger.OverlapPoint(player.transform.position) ||
                      _trigger.bounds.Contains(playerCollider.bounds.center);

        if (inside && !_playerInside)
        {
            _playerInside = true;
            Debug.Log($"[DoorTrigger] (manual) Player {netObj.OwnerClientId} inside door.");
            GameLevelManager.Instance?.PlayerEnteredDoorServerRpc(netObj.OwnerClientId);
        }
        else if (!inside && _playerInside)
        {
            _playerInside = false;
            Debug.Log($"[DoorTrigger] (manual) Player {netObj.OwnerClientId} left door.");
            GameLevelManager.Instance?.PlayerExitedDoorServerRpc(netObj.OwnerClientId);
        }
    }
}