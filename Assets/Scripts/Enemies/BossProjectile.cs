using UnityEngine;

/// <summary>
/// Boss projectile that deals damage to the player and reports hits/misses for AI training.
/// </summary>
public class BossProjectile : BaseProjectile
{
    // ==================== Serialized Fields ====================
    [Header("Boss Projectile Settings")]
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
    
    [Tooltip("Reference to the boss reward manager for AI training.")]
    [SerializeField] private BossRewardManager _rewardManager;

    // ==================== Properties ====================
    public BossRewardManager RewardManager 
    { 
        get => _rewardManager; 
        set => _rewardManager = value; 
    }

    // ==================== Override Methods ====================
    protected override bool ShouldIgnoreCollisionByTag(string tag)
    {
        // Boss projectiles ignore enemy tags
        return tag == EnemyTag;
    }

    protected override void OnHit(Collider2D collision)
    {
        bool hitPlayer = collision.CompareTag(PlayerTag);
        
        // Report to reward manager for AI training
        if (_rewardManager != null)
        {
            if (hitPlayer)
            {
                _rewardManager.ReportHitPlayer();
            }
            else
            {
                _rewardManager.ReportAttackMissed();
            }
        }

        // Deal damage to player
        if (hitPlayer)
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

    protected override void OnLifetimeExpired()
    {
        // Report missed attack to reward manager
        if (_rewardManager != null)
        {
            _rewardManager.ReportAttackMissed();
        }

        base.OnLifetimeExpired();
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
}
