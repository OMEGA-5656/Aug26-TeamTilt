using UnityEngine;

/// <summary>
/// Defines all tunable stats for a player character.
/// Create via: Assets > Create > TeamTilt > Player Stats
/// Assign to PlayerStats component — no magic numbers in code!
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "TeamTilt/Player Stats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("Movement")]
    public float MoveSpeed = 8f;
    public float JumpForce = 14f;
    public float FallMultiplier = 2.5f;   // Makes falling feel snappier
    public float LowJumpMultiplier = 2f;   // Short-tap jump behavior

    [Header("Health")]
    public int MaxHealth = 3;

    [Header("Physics")]
    public float GroundCheckRadius = 0.1f;
    public LayerMask GroundLayer;

    [Header("Combat")]
    public float DashForce = 12f;
    public float DashCooldown = 1f;
    public float InvincibilityDuration = 0.5f;
}
