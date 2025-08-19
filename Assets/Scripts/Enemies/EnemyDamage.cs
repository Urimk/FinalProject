using System.Collections;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Base class for enemy damage logic. Handles damaging the player and optional recoil.
/// 
/// This class provides a flexible damage system where each enemy can specify:
/// - Damage amount
/// - Whether to apply recoil
/// - Custom recoil force (horizontal and vertical)
/// - Custom recoil duration
/// 
/// The recoil system allows different enemies to have unique knockback effects,
/// creating varied gameplay experiences.
/// </summary>
public class EnemyDamage : MonoBehaviour
{

    private const float InstantDeath = -666;

    // ==================== Serialized Fields ====================
    [Header("Damage Parameters")]
    [Tooltip("Amount of damage dealt to the player.")]
    [FormerlySerializedAs("damage")]
    [SerializeField] protected float _damage;
    
    [Header("Recoil Settings")]
    [Tooltip("If true, applies recoil to the player on hit.")]
    [FormerlySerializedAs("isRecoil")]
    [SerializeField] protected bool _isRecoil = false;
    
    [Tooltip("Horizontal recoil force applied to the player.")]
    [SerializeField] protected float _recoilHorizontalForce = 4f;
    
    [Tooltip("Vertical recoil force applied to the player.")]
    [SerializeField] protected float _recoilVerticalForce = 3f;
    
    [Tooltip("Duration of the recoil effect.")]
    [SerializeField] protected float _recoilDuration = 0.15f;

    // ==================== Damage Logic ====================
    /// <summary>
    /// Applies damage to the player and triggers recoil if enabled when the player stays in the trigger.
    /// </summary>
    /// <param name="collision">Collider of the object staying in the trigger.</param>
    protected void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Apply damage
            if (_damage > 0)
            {
                collision.GetComponent<Health>().TakeDamage(_damage);
            }
            else
            {
                if (_damage == InstantDeath)
                {
                    collision.GetComponent<Health>().TakeDamage(float.MaxValue);
                }
            }

            if (_isRecoil)
            {
                Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 recoilDir = transform.up;
                    PlayerMovement playerMovement = collision.GetComponent<PlayerMovement>();
                    if (playerMovement != null)
                    {
                        playerMovement.Recoil(this.transform.position, recoilDir, _recoilHorizontalForce, _recoilVerticalForce, _recoilDuration);
                    }
                }
            }
        }
    }
}
