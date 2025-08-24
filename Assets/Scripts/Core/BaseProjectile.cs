using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Base class for all projectiles in the game. Handles common functionality like movement, 
/// collision detection, lifetime management, and visual orientation.
/// </summary>
public abstract class BaseProjectile : MonoBehaviour
{
    // ==================== Constants ====================
    protected const string NoCollisionTag = "NoCollision";
    protected const string PlayerTag = "Player";
    protected const string EnemyTag = "Enemy";
    protected const string DashTargetIndicatorTag = "DashTargetIndicator";
    protected const string FlameWarningMarkerTag = "FlameWarningMarker";
    protected const string CheckPointTag = "Checkpoint";
    protected const float IsComingOutRatio = 0.526f;

    // ==================== Serialized Fields ====================
    [Header("Projectile Settings")]
    [Tooltip("Movement speed of the projectile.")]
    [SerializeField] protected float _speed = 5f;
    
    [Tooltip("Visual scale of the projectile.")]
    [SerializeField] protected float _size = 1f;
    
    [Tooltip("Time in seconds before the projectile is automatically destroyed.")]
    [SerializeField] protected float _lifetime = 5f;
    
    [Tooltip("Whether the projectile should rotate to face its movement direction.")]
    [SerializeField] protected bool _rotateToDirection = true;

    // ==================== Protected Fields ====================
    protected Vector2 _direction = Vector2.right;
    protected float _currentLifetime;
    protected bool _isActive = false;
    protected bool _hasHit = false;
    protected Animator _animator;
    protected Collider2D _collider;
    protected SpriteRenderer _spriteRenderer;
    
    // Coming out effect fields
    protected bool _isComingOut = false;
    protected float _colliderGrowTime = 0.3f;
    protected float _growTimer = 0f;
    protected Vector2 _fullColliderSize;
    protected BoxCollider2D _boxCollider;
    protected const float LerpComplete = 1f;

    // ==================== Properties ====================
    public bool IsActive => _isActive;
    public Vector2 Direction => _direction;

    // ==================== Unity Lifecycle ====================
    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Initialize coming out effect components
        _boxCollider = GetComponent<BoxCollider2D>();
        if (_boxCollider != null)
        {
            _fullColliderSize = _boxCollider.size;
        }
    }

    protected virtual void Update()
    {
        if (!_isActive || _hasHit) return;

        UpdateLifetime();
        UpdateMovement();
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Activates the projectile and resets its state.
    /// </summary>
    public virtual void Activate()
    {
        _isActive = true;
        _hasHit = false;
        _currentLifetime = 0f;
        gameObject.SetActive(true);
        
        // Apply the size/scale
        transform.localScale = Vector3.one * _size;
        
        // Reset sprite flip
        if (_spriteRenderer != null)
        {
            _spriteRenderer.flipX = false;
        }
        
        if (_collider != null)
            _collider.enabled = true;
            
        // Handle coming out effect
        if (_isComingOut && _boxCollider != null)
        {
            transform.localScale = new Vector3(transform.localScale.y * IsComingOutRatio, transform.localScale.y, transform.localScale.z);
            _boxCollider.size = Vector2.zero;
            _boxCollider.offset = Vector2.zero;
            _colliderGrowTime = _speed / 2.25f;
            _growTimer = 0f;
        }
        else if (_boxCollider != null)
        {
            _boxCollider.size = _fullColliderSize;
            _boxCollider.offset = Vector2.zero;
        }
    }

    /// <summary>
    /// Sets the projectile's direction and optionally rotates it to face that direction.
    /// </summary>
    /// <param name="direction">Normalized direction vector</param>
    /// <param name="rotateToDirection">Whether to rotate the projectile to face the direction</param>
    public virtual void SetDirection(Vector2 direction, bool rotateToDirection = true)
    {
        _direction = direction.normalized;
        
        if (rotateToDirection && _rotateToDirection)
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Handle sprite flipping for left direction to prevent visual issues
            if (_spriteRenderer != null)
            {
                // If moving left (negative X), flip the sprite to maintain proper orientation
                if (_direction.x < 0)
                {
                    _spriteRenderer.flipX = true;
                }
                else
                {
                    _spriteRenderer.flipX = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Sets whether the projectile is in the 'coming out' state.
    /// </summary>
    /// <param name="value">True to enable coming out effect, false to disable</param>
    public virtual void SetComingOut(bool value)
    {
        _isComingOut = value;
    }

    /// <summary>
    /// Launches the projectile from a start position toward a target position.
    /// </summary>
    /// <param name="startPosition">Starting position of the projectile</param>
    /// <param name="targetPosition">Target position to aim at</param>
    /// <param name="speed">Optional speed override</param>
    public virtual void Launch(Vector2 startPosition, Vector2 targetPosition, float speed = -1f)
    {
        // Unparent the projectile to prevent it from moving with its parent
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        transform.position = startPosition;
        
        if (speed > 0f)
            _speed = speed;
            
        // Apply the size/scale
        transform.localScale = Vector3.one * _size;
            
        Vector2 direction = (targetPosition - startPosition).normalized;
        SetDirection(direction);
        
        Activate();
    }

    /// <summary>
    /// Launches the projectile in a specific direction from its current position.
    /// </summary>
    /// <param name="direction">Direction to move</param>
    /// <param name="speed">Optional speed override</param>
    public virtual void LaunchInDirection(Vector2 direction, float speed = -1f)
    {
        // Unparent the projectile to prevent it from moving with its parent
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        if (speed > 0f)
            _speed = speed;
            
        // Apply the size/scale
        transform.localScale = Vector3.one * _size;
            
        SetDirection(direction);
        Activate();
    }

    /// <summary>
    /// Launches the projectile from a specific position in a specific direction.
    /// </summary>
    /// <param name="position">Starting position</param>
    /// <param name="direction">Direction to move</param>
    /// <param name="speed">Optional speed override</param>
    public virtual void LaunchFromPosition(Vector2 position, Vector2 direction, float speed = -1f)
    {
        LaunchFromPosition(position, direction, speed, false);
    }

    /// <summary>
    /// Launches the projectile from a specific position in a specific direction with coming out effect.
    /// </summary>
    /// <param name="position">Starting position</param>
    /// <param name="direction">Direction to move</param>
    /// <param name="speed">Optional speed override</param>
    /// <param name="comingOut">Whether to enable the coming out effect</param>
    public virtual void LaunchFromPosition(Vector2 position, Vector2 direction, float speed, bool comingOut)
    {
        // Unparent the projectile to prevent it from moving with its parent
        if (transform.parent != null)
        {
            Debug.Log($"[BaseProjectile] Unparenting {gameObject.name} from {transform.parent.name}");
            transform.SetParent(null);
        }
        
        transform.position = position;
        
        if (speed > 0f)
            _speed = speed;
            
        // Apply the size/scale
        transform.localScale = Vector3.one * _size;
        
        // Set coming out state before activation
        SetComingOut(comingOut);
            
        SetDirection(direction);
        Activate();
        
        Debug.Log($"[BaseProjectile] Launched {gameObject.name} from {position} in direction {direction} (speed: {_speed}, comingOut: {comingOut})");
    }

    // ==================== Protected Methods ====================
    /// <summary>
    /// Updates the projectile's lifetime and destroys it if expired.
    /// </summary>
    protected virtual void UpdateLifetime()
    {
        _currentLifetime += Time.deltaTime;
        if (_currentLifetime >= _lifetime)
        {
            OnLifetimeExpired();
        }
    }

    /// <summary>
    /// Updates the projectile's movement.
    /// </summary>
    protected virtual void UpdateMovement()
    {
        // Handle coming out effect
        if (_isComingOut && _boxCollider != null)
        {
            _growTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_growTimer / _colliderGrowTime);
            
            Vector2 newSize;
            Vector2 newOffset;
            
            if (Mathf.Abs(_direction.x) > Mathf.Abs(_direction.y))
            {
                newSize = new Vector2(_fullColliderSize.x * t, _fullColliderSize.y);
                newOffset = new Vector2(_direction.x > 0 ? (newSize.x - _fullColliderSize.x) / 2f : (_fullColliderSize.x - newSize.x) / 2f, 0);
            }
            else
            {
                newSize = new Vector2(_fullColliderSize.x, _fullColliderSize.y * t);
                newOffset = new Vector2(0, _direction.y > 0 ? (newSize.y - _fullColliderSize.y) / 2f : (_fullColliderSize.y - newSize.y) / 2f);
            }
            
            _boxCollider.size = newSize;
            _boxCollider.size = newSize;
            _boxCollider.offset = newOffset;
            
            if (t >= LerpComplete)
            {
                _isComingOut = false;
                _boxCollider.size = _fullColliderSize;
                _boxCollider.offset = Vector2.zero;
            }
        }
        
        Vector3 movement = (Vector3)(_direction * _speed * Time.deltaTime);
        transform.position += movement;
    }

    /// <summary>
    /// Called when the projectile's lifetime expires.
    /// </summary>
    protected virtual void OnLifetimeExpired()
    {
        Debug.Log($"[BaseProjectile] Lifetime expired for {gameObject.name}, deactivating.");
        Deactivate();
    }

    /// <summary>
    /// Called when the projectile hits something.
    /// </summary>
    /// <param name="collision">The collision that occurred</param>
    protected virtual void OnHit(Collider2D collision)
    {
        Debug.Log($"[BaseProjectile] {gameObject.name} hit {collision.gameObject.name} (tag: {collision.tag})");
        _hasHit = true;
        
        if (_collider != null)
            _collider.enabled = false;
            
        if (_animator != null)
        {
            // Check if animator has explosion trigger
            if (HasAnimatorTrigger("explosion"))
            {
                _animator.SetTrigger("explosion");
            }
            // Check if animator has fade trigger
            else if (HasAnimatorTrigger("fade"))
            {
                _animator.SetTrigger("fade");
            }
            // If neither trigger exists, deactivate immediately
            else
            {
                Deactivate();
            }
        }
        else
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Deactivates the projectile and resets its state.
    /// </summary>
    protected virtual void Deactivate()
    {
        _isActive = false;
        _hasHit = false;
        _currentLifetime = 0f;
        transform.rotation = Quaternion.identity;
        gameObject.SetActive(false);
        
        // Reset coming out effect
        _isComingOut = false;
        if (_boxCollider != null)
        {
            _boxCollider.size = _fullColliderSize;
            _boxCollider.offset = Vector2.zero;
        }
    }

    /// <summary>
    /// Checks if the collision should be ignored.
    /// </summary>
    /// <param name="collision">The collision to check</param>
    /// <returns>True if the collision should be ignored</returns>
    protected virtual bool ShouldIgnoreCollision(Collider2D collision)
    {
        string tag = collision.tag;
        return tag == NoCollisionTag || 
               tag == DashTargetIndicatorTag || 
               tag == FlameWarningMarkerTag || 
               tag == CheckPointTag ||
               ShouldIgnoreCollisionByTag(tag);
    }

    /// <summary>
    /// Override this method to add custom collision tag filtering.
    /// </summary>
    /// <param name="tag">The tag to check</param>
    /// <returns>True if the collision with this tag should be ignored</returns>
    protected virtual bool ShouldIgnoreCollisionByTag(string tag)
    {
        return false;
    }

    // ==================== Collision Detection ====================
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (ShouldIgnoreCollision(collision))
            return;

        OnHit(collision);
    }

    // ==================== Utility Methods ====================
    /// <summary>
    /// Sets the projectile's damage value (if applicable).
    /// </summary>
    public virtual void SetDamage(float damage)
    {
        // Override in derived classes that support damage
    }

    /// <summary>
    /// Sets the projectile's speed.
    /// </summary>
    public virtual void SetSpeed(float speed)
    {
        _speed = speed;
    }

    /// <summary>
    /// Sets the projectile's size/scale and applies it immediately.
    /// </summary>
    /// <param name="size">The new size value</param>
    public virtual void SetSize(float size)
    {
        _size = size;
        transform.localScale = Vector3.one * size;
    }
    
    /// <summary>
    /// Checks if the animator has a specific trigger parameter.
    /// </summary>
    /// <param name="triggerName">Name of the trigger parameter to check</param>
    /// <returns>True if the trigger parameter exists</returns>
    protected virtual bool HasAnimatorTrigger(string triggerName)
    {
        if (_animator == null) return false;
        
        // Get all parameters from the animator
        AnimatorControllerParameter[] parameters = _animator.parameters;
        
        // Check if the trigger parameter exists
        foreach (var parameter in parameters)
        {
            if (parameter.name == triggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }
        
        return false;
    }
}
