using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Placed on the finish door.
/// Notifies GameLevelManager when a player enters/exits.
/// The server hides the player the moment they step inside the door,
/// and the level completes once EVERY player has entered.
/// </summary>
public class DoorTrigger : MonoBehaviour
{
    private Collider2D _trigger;

    // Tracks which players are currently inside (by collider instance id → NetworkObject)
    private readonly Dictionary<int, NetworkObject> _inside = new();

    private void Awake()
    {
        _trigger = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        _inside[other.GetInstanceID()] = netObj;
        Debug.Log($"[DoorTrigger] Player {netObj.OwnerClientId} entered door.");
        GameLevelManager.Instance?.PlayerEnteredDoorServerRpc(netObj.OwnerClientId);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        _inside.Remove(other.GetInstanceID());
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

        // Re-evaluate the local player's collider against the trigger each physics step.
        var localPlayer = FindObjectOfType<PlayerController>();
        if (localPlayer == null) return;

        var netObj = localPlayer.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        var playerCollider = localPlayer.GetComponent<Collider2D>();
        if (playerCollider == null) return;

        bool inside = _trigger.OverlapPoint(localPlayer.transform.position) ||
                      _trigger.bounds.Contains(playerCollider.bounds.center);

        int id = playerCollider.GetInstanceID();
        bool wasInside = _inside.ContainsKey(id);

        if (inside && !wasInside)
        {
            _inside[id] = netObj;
            Debug.Log($"[DoorTrigger] (manual) Player {netObj.OwnerClientId} inside door.");
            GameLevelManager.Instance?.PlayerEnteredDoorServerRpc(netObj.OwnerClientId);
        }
        else if (!inside && wasInside)
        {
            _inside.Remove(id);
            Debug.Log($"[DoorTrigger] (manual) Player {netObj.OwnerClientId} left door.");
            GameLevelManager.Instance?.PlayerExitedDoorServerRpc(netObj.OwnerClientId);
        }
    }
}
