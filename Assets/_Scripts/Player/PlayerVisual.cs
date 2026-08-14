using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Assigns one of 8 distinct colors to each player based on their ClientId.
/// Swap the color array or the sprite in the inspector to change player visuals.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisual : NetworkBehaviour
{
    [Header("Player Colors — easily swappable")]
    [SerializeField] private Color[] _playerColors = new Color[]
    {
        new Color(0.95f, 0.26f, 0.21f), // 0 Red
        new Color(0.13f, 0.59f, 0.95f), // 1 Blue
        new Color(0.30f, 0.69f, 0.31f), // 2 Green
        new Color(1.00f, 0.76f, 0.03f), // 3 Yellow
        new Color(0.61f, 0.15f, 0.69f), // 4 Purple
        new Color(0.00f, 0.74f, 0.83f), // 5 Cyan
        new Color(1.00f, 0.60f, 0.00f), // 6 Orange
        new Color(0.91f, 0.12f, 0.39f), // 7 Pink
    };

    private SpriteRenderer _sr;

    private void Awake() => _sr = GetComponent<SpriteRenderer>();

    /// <summary>
    /// Toggles the player's sprite visibility (used by the finish door to "absorb" players).
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_sr == null) return;
        _sr.enabled = visible;
    }

    public override void OnNetworkSpawn()
    {
        if (_sr == null) return;
        int colorIdx = (int)(OwnerClientId % (ulong)_playerColors.Length);
        _sr.color = _playerColors[colorIdx];
        Debug.Log($"[PlayerVisual] Client {OwnerClientId} → color index {colorIdx}");
    }
}
