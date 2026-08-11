using UnityEngine;

/// <summary>
/// Holds a reference to the player's ScriptableObject stats data.
/// Also tracks runtime-mutable state like CurrentHealth.
/// Fires events via EventBus so UI/networking don't need direct references.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Stats Asset (ScriptableObject)")]
    [SerializeField] private PlayerStatsSO _data;

    public PlayerStatsSO Data => _data;

    public int CurrentHealth { get; private set; }

    private void Start()
    {
        CurrentHealth = _data.MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (CurrentHealth <= 0)
        {
            EventBus.Publish(new PlayerDiedEvent
            {
                ClientId = GetComponent<Unity.Netcode.NetworkObject>()?.OwnerClientId ?? 0
            });
        }
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(_data.MaxHealth, CurrentHealth + amount);
    }
}
