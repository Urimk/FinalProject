using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles explosion behavior, including damaging the player and self-destruction after a set lifetime.
/// </summary>
public class Explosion : MonoBehaviour
{
    // === Constants ===
    private const float DefaultDamage = 1f;
    private const float DefaultLifetime = 0.3f;

    // === Inspector Fields ===
    [Header("Explosion Settings")]
    [Tooltip("Amount of damage dealt to the player.")]
    [FormerlySerializedAs("damage")]

    [SerializeField] private float _damage = DefaultDamage;

    [Tooltip("Lifetime of the explosion in seconds before it is destroyed.")]
    [FormerlySerializedAs("lifetime")]

    [SerializeField] private float _lifetime = DefaultLifetime;

    /// <summary>
    /// Called by Unity when the object is instantiated.
    /// Schedules the destruction of the explosion object after its lifetime expires.
    /// </summary>
    private void Start()
    {
        Destroy(gameObject, _lifetime);
    }

    /// <summary>
    /// Called by Unity when another collider enters this trigger.
    /// Damages the player if they enter the explosion's trigger area.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<Health>().TakeDamage(_damage);
        }
    }
}
