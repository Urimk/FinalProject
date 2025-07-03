using UnityEngine;

/// <summary>
/// Handles boss projectile movement, collision, and reward reporting for hits/misses.
/// </summary>
public class BossProjectile : EnemyDamage
{
    // ==================== Constants ====================
    private const string NoCollisionTag = "NoCollision";
    private const string EnemyTag = "Enemy";
    private const string PlayerTag = "Player";
    private const float ScaleZ = 1f;
    private const float RotationZ = 0f;

    // ==================== Serialized Fields ====================
    [Tooltip("Speed of the projectile.")]
    [SerializeField] private float _speed;
    [Tooltip("Size (scale) of the projectile.")]
    [SerializeField] private float _size;
    [Tooltip("Time before the projectile resets if it doesn't hit anything.")]
    [SerializeField] private float _resetTime;

    // ==================== Private Fields ====================
    private float _lifeTime;
    private Animator _anim;
    private bool _hit;
    private BoxCollider2D _collid;
    public BossRewardManager rewardManager;
    private Vector2 _direction;

    /// <summary>
    /// Initializes animator and collider references.
    /// </summary>
    private void Awake()
    {
        // Cache references for performance
        _anim = GetComponent<Animator>();
        _collid = GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// Activates the projectile and resets its state.
    /// </summary>
    public void ActivateProjectile()
    {
        // Reset hit state and timer
        _hit = false;
        _lifeTime = 0;
        gameObject.SetActive(true);
        _collid.enabled = true;
    }

    /// <summary>
    /// Launches the projectile towards a target position at a given speed.
    /// </summary>
    public void Launch(Vector2 startPosition, Vector2 targetPosition, float speed)
    {
        // Set up projectile for launch
        _hit = false;
        _lifeTime = 0;
        transform.localScale = new Vector3(_size, _size, ScaleZ);
        transform.position = startPosition;
        _speed = speed;
        _direction = (targetPosition - startPosition).normalized;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(RotationZ, RotationZ, angle);
        gameObject.SetActive(true);
        _collid.enabled = true;
    }

    /// <summary>
    /// Handles projectile movement and lifetime.
    /// </summary>
    private void Update()
    {
        // Move the projectile if it hasn't hit anything
        if (_hit) return;
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        _lifeTime += Time.deltaTime;
        // Check if projectile should reset (missed)
        if (_lifeTime > _resetTime)
        {
            if (rewardManager != null)
            {
                rewardManager.ReportAttackMissed();
            }
            Deactivate();
        }
    }

    /// <summary>
    /// Handles collision with other objects, triggers explosion animation, and reports hit/miss.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignore collisions with certain tags
        if (collision.gameObject.tag == NoCollisionTag || collision.gameObject.tag == EnemyTag)
        {
            return;
        }
        bool hitPlayer = collision.tag == PlayerTag;
        // Report hit or miss to reward manager
        if (rewardManager != null)
        {
            if (hitPlayer)
            {
                rewardManager.ReportHitPlayer();
            }
            else
            {
                rewardManager.ReportAttackMissed();
            }
        }
        _hit = true;
        base.OnTriggerStay2D(collision);
        _collid.enabled = false;
        // Play explosion animation if available
        if (_anim != null)
        {
            _anim.SetTrigger("explosion");
        }
        else
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Sets the projectile's damage value.
    /// </summary>
    public void SetDamage(int newDamage)
    {
        _damage = newDamage;
    }

    /// <summary>
    /// Sets the projectile's speed value.
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        _speed = newSpeed;
    }

    /// <summary>
    /// Sets the projectile's size value.
    /// </summary>
    public void SetSize(float newSize)
    {
        _size = newSize;
    }

    /// <summary>
    /// Deactivates the projectile and resets its rotation.
    /// </summary>
    private void Deactivate()
    {
        // Hide and reset the projectile
        gameObject.SetActive(false);
        transform.rotation = Quaternion.identity;
    }
}
