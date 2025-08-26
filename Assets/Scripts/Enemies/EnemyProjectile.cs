using UnityEngine;

/// <summary>
/// Enemy projectile that deals damage to the player and can have special effects.
/// </summary>
public class EnemyProjectile : BaseProjectile
{
    // ==================== Serialized Fields ====================
    [Header("Enemy Projectile Settings")]
    [Tooltip("Damage dealt to the player.")]
    [SerializeField] private float _damage = 1f;
    
    [Tooltip("Whether to apply recoil to the player on hit.")]
    [SerializeField] private bool _applyRecoil = false;
    
    [Tooltip("Horizontal recoil force.")]
    [SerializeField] private float _recoilHorizontalForce = 4f;
    
    [Tooltip("Vertical recoil force.")]
    [SerializeField] private float _recoilVerticalForce = 3f;
    
    [Tooltip("Duration of the recoil effect.")]
    [SerializeField] private float _recoilDuration = 0.15f;
    
    [Tooltip("Whether this projectile breaks when hit by player fireballs.")]
    [SerializeField] private bool _breakWithFireball = true;

    // ==================== Override Methods ====================
    protected override bool ShouldIgnoreCollisionByTag(string tag)
    {
        // Enemy projectiles ignore enemy tags
        if (tag == EnemyTag || tag == NoCollisionTag) return true;
        
        // Check if it should break with fireball
        if (!_breakWithFireball && tag == "PlayerFireball") return true;
        
        return false;
    }

    protected override void OnHit(Collider2D collision)
    {
        if (ShouldIgnoreCollisionByTag(collision.tag)) return;
        
        // Deal damage to player
        if (collision.CompareTag(PlayerTag))
        {
            if (collision.TryGetComponent<Health>(out var health))
            {
                health.TakeDamage(_damage);
            }

            // Apply recoil if enabled
            if (_applyRecoil)
            {
                if (collision.TryGetComponent<PlayerMovement>(out var playerMovement))
                {
                    Vector2 recoilDirection = transform.up;
                    playerMovement.Recoil(transform.position, recoilDirection, _recoilHorizontalForce, _recoilVerticalForce, _recoilDuration);
                }
            }
        }

        base.OnHit(collision);
    }

    public override void SetDamage(float damage)
    {
        _damage = damage;
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Sets the recoil parameters for this projectile.
    /// </summary>
    public void SetRecoil(bool applyRecoil, float horizontalForce = 4f, float verticalForce = 3f, float duration = 0.15f)
    {
        _applyRecoil = applyRecoil;
        _recoilHorizontalForce = horizontalForce;
        _recoilVerticalForce = verticalForce;
        _recoilDuration = duration;
    }

    /// <summary>
    /// Sets whether this projectile breaks when hit by player fireballs.
    /// </summary>
    public void SetBreakWithFireball(bool breakWithFireball)
    {
        _breakWithFireball = breakWithFireball;
    }

    // ==================== Legacy Support Methods ====================
    /// <summary>
    /// Legacy method for backward compatibility with old scripts.
    /// </summary>
    public void ActivateProjectile()
    {
        Activate();
    }
}
