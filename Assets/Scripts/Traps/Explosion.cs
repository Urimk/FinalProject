using UnityEngine;

/// <summary>
/// Handles explosion behavior, including damaging the player and self-destruction after a set lifetime.
/// </summary>
public class Explosion : MonoBehaviour
{
    private const float DefaultDamage = 1f;
    private const float DefaultLifetime = 0.5f;

    [SerializeField] private float _damage = DefaultDamage;
    [SerializeField] private float _lifetime = DefaultLifetime;

    /// <summary>
    /// Destroys the explosion object after its lifetime expires.
    /// </summary>
    private void Start()
    {
        Destroy(gameObject, _lifetime);
    }

    /// <summary>
    /// Damages the player if they enter the explosion's trigger.
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
