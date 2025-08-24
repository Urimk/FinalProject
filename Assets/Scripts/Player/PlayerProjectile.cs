using UnityEngine;

/// <summary>
/// Player projectile that deals damage to enemies and other damageable objects.
/// </summary>
public class PlayerProjectile : BaseProjectile
{
    // ==================== Serialized Fields ====================
    [Header("Player Projectile Settings")]
    [Tooltip("Damage dealt by the projectile.")]
    [SerializeField] private float _damage = 1f;
    
    [Tooltip("Whether to flip the sprite based on direction.")]
    [SerializeField] private bool _flipSprite = true;

    // ==================== Override Methods ====================
    protected override bool ShouldIgnoreCollisionByTag(string tag)
    {
        // Player projectiles ignore player and enemy tags
        return tag == PlayerTag || tag == EnemyTag;
    }

    protected override void OnHit(Collider2D collision)
    {
        // Deal damage to damageable objects
        if (collision.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_damage);
        }

        base.OnHit(collision);
    }

    public override void SetDirection(Vector2 direction, bool rotateToDirection = true)
    {
        base.SetDirection(direction, rotateToDirection);
        
        // Handle sprite flipping for player projectiles
        if (_flipSprite && _spriteRenderer != null)
        {
            if (direction.x < 0)
            {
                _spriteRenderer.flipX = true;
            }
            else if (direction.x > 0)
            {
                _spriteRenderer.flipX = false;
            }
        }
    }

    public override void SetDamage(float damage)
    {
        _damage = damage;
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Sets the projectile's direction using a simple float (for backward compatibility).
    /// </summary>
    /// <param name="direction">-1 for left, 1 for right</param>
    public void SetDirection(float direction)
    {
        Vector2 dir = new Vector2(direction, 0);
        SetDirection(dir);
    }
}
