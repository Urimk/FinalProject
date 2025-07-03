using System.Collections;

using UnityEngine;

/// <summary>
/// Base class for enemy damage logic. Handles damaging the player and optional recoil.
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    // ==================== Serialized Fields ====================
    [Header("Damage Parameters")]
    [Tooltip("Amount of damage dealt to the player.")]
    [SerializeField] protected float _damage;
    [Tooltip("If true, applies recoil to the player on hit.")]
    [SerializeField] protected bool _isRecoil = false;

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
            collision.GetComponent<Health>().TakeDamage(_damage);

            if (_isRecoil)
            {
                Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 recoilDir = transform.up;
                    collision.GetComponent<PlayerMovement>().Recoil(this.transform.position, recoilDir);
                }
            }
        }
    }
}
