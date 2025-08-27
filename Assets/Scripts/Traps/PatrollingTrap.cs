using UnityEngine;

/// <summary>
/// Example trap that uses the PatrolSystem for movement.
/// This demonstrates how traps can use the new patrol system.
/// </summary>
public class PatrollingTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    [Tooltip("Damage dealt to the player")]
    [SerializeField] private int _damage = 1;
    
    [Tooltip("Whether the trap should destroy itself after hitting the player")]
    [SerializeField] private bool _destroyOnHit = false;
    
    [Tooltip("Sound to play when the trap hits the player")]
    [SerializeField] private AudioClip _hitSound;

    [Header("Patrol Integration")]
    [Tooltip("Whether to stop patrolling when the trap is triggered")]
    [SerializeField] private bool _stopPatrolOnTrigger = false;

    private PatrolSystem _patrolSystem;
    private bool _hasHitPlayer = false;

    private void Awake()
    {
        _patrolSystem = GetComponent<PatrolSystem>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHitPlayer) return;

        // Check if the collider is the player
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Deal damage to the player
                playerHealth.TakeDamage(_damage);
                
                // Play hit sound
                if (_hitSound != null)
                {
                    SoundManager.instance.PlaySound(_hitSound, gameObject);
                }

                _hasHitPlayer = true;

                // Stop patrolling if configured
                if (_stopPatrolOnTrigger && _patrolSystem != null)
                {
                    _patrolSystem.StopPatrol();
                }

                // Destroy the trap if configured
                if (_destroyOnHit)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Reset the trap (useful for respawning)
    /// </summary>
    public void ResetTrap()
    {
        _hasHitPlayer = false;
        
        if (_patrolSystem != null)
        {
            _patrolSystem.StartPatrol();
        }
    }
}
