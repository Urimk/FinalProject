using System.Collections;

using UnityEngine;

/// <summary>
/// Base class for enemy damage logic. Handles damaging the player and optional recoil.
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [SerializeField] protected float _damage;
    [SerializeField] protected bool _isRecoil = false;

    /// <summary>
    /// Applies damage to the player and triggers recoil if enabled when the player stays in the trigger.
    /// </summary>
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
