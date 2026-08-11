using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Placed below the platform.
/// Any player touching it triggers a full team respawn.
/// </summary>
public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return; // only the owner sends the RPC

        Debug.Log($"[DeathZone] Player {netObj.OwnerClientId} fell off!");
        GameLevelManager.Instance?.RequestRespawnServerRpc();
    }
}
