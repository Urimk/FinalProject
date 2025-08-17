using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles enemy projectile movement, collision, and animation logic.
/// </summary>
public class EnemyProjectile : EnemyDamage
{
    // ==================== Constants ====================
    private const float ColliderGrowDivisor = 2.25f;
    private const float DefaultColliderGrowTime = 0.3f;
    private const float LerpComplete = 1f;
    private const string NoCollisionTag = "NoCollision";
    private const string EnemyTag = "Enemy";
    private const string DashTargetIndicatorTag = "DashTargetIndicator";
    private const string FlameWarningMarkerTag = "FlameWarningMarker";
    private const string FireballTag = "PlayerFireball";

    // ==================== Serialized Fields ====================
    [Header("Projectile Parameters")]
    [Tooltip("Movement speed of the projectile.")]
    [FormerlySerializedAs("speed")]

    [SerializeField] private float _speed;
    [Tooltip("Visual and collision size of the projectile.")]
    [FormerlySerializedAs("size")]

    [SerializeField] private float _size;
    [Tooltip("Time in seconds before the projectile is automatically reset.")]
    [FormerlySerializedAs("resetTime")]

    [SerializeField] private float _resetTime;
    [SerializeField] private bool breakWithFireball = true;

    // ==================== Private Fields ====================
    private float _lifeTime;
    private Animator _anim;
    private bool _hit;
    private BoxCollider2D _collid;
    private Vector2 _fullColliderSize;
    private bool _isComingOut = false;
    private float _colliderGrowTime = DefaultColliderGrowTime;
    private float _growTimer = 0f;
    private BoxCollider2D _boxCollider;
    private Vector2 _direction = Vector2.right;
    private bool _useCustomDirection = false;
    private Vector3 _storedWorldPosition;
    private bool _useStoredPosition = false;
    private Vector3 _initialParentLossyScale;
    private bool _isFlipped = false;



    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Initializes references and collider size.
    /// </summary>
    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _collid = GetComponent<BoxCollider2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _fullColliderSize = _boxCollider.size;
    }

    public void StoreInitialWorldPosition()
    {
        _storedWorldPosition = transform.position;
        _initialParentLossyScale = transform.parent.lossyScale; // Store parent's initial scale
        _useStoredPosition = true;
        _isFlipped = false;

    }

    // ==================== Projectile Activation & Direction ====================
    /// <summary>
    /// Activates the projectile and resets its state.
    /// </summary>
    public void ActivateProjectile()
    {
        _hit = false;
        _lifeTime = 0;
        if (_useStoredPosition)
        {
            float globalXDirection = Mathf.Sign(transform.parent.lossyScale.x); // +1 for right, -1 for left
            _direction = new Vector2(globalXDirection, 0);
        }
        gameObject.SetActive(true);
        _collid.enabled = true;
        if (_isComingOut)
        {
            _boxCollider.size = Vector2.zero;
            _boxCollider.offset = Vector2.zero;
            _colliderGrowTime = _speed / ColliderGrowDivisor;
            _growTimer = 0f;
        }
        else
        {
            _boxCollider.size = _fullColliderSize;
        }
    }

    /// <summary>
    /// Sets the projectile's direction and visual orientation.
    /// </summary>
    public void SetDirection(Vector2 newDirection, bool invertMovement = false, bool invertVisual = false, bool invertY = false)
    {
        newDirection = newDirection.normalized;
        if (invertY)
        {
            newDirection.y = -newDirection.y;
        }
        _direction = invertMovement ? -newDirection : newDirection;
        _useCustomDirection = true;
        Vector2 visualDirection = new Vector2(invertVisual ? -newDirection.x : newDirection.x, newDirection.y);
        float angle = Mathf.Atan2(visualDirection.y, visualDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Sets whether the projectile is in the 'coming out' state.
    /// </summary>
    public void SetComingOut(bool value)
    {
        _isComingOut = value;
    }

    // ==================== Movement, Collision, and Lifetime ====================
    /// <summary>
    /// Handles projectile movement, collider growth, and lifetime.
    /// </summary>
    private void Update()
    {
        if (_hit) return;
        if (_isComingOut)
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
        transform.localScale = new Vector3(_size, _size, 1);
        if (_useStoredPosition)
        {
            // Check if parent scale changed and compensate
            Vector3 currentParentScale = transform.parent.lossyScale;
            if (currentParentScale.x != _initialParentLossyScale.x && !_isFlipped)
            {
                // Parent flipped, rotate the fireball 180 degrees
                transform.Rotate(0, 0, 180);
                _isFlipped = true;
                _initialParentLossyScale = currentParentScale;

                // Update stored parent scale
                _initialParentLossyScale = currentParentScale;
            }
            // Move from stored world position, ignoring parent transforms
            _storedWorldPosition += (Vector3)(_direction * _speed * Time.deltaTime);
            transform.position = _storedWorldPosition;
        }
        else
        {
            if (_useCustomDirection || _useStoredPosition)
            {
                transform.Translate(_direction * _speed * Time.deltaTime, Space.World);
            }
            else
            {
                transform.Translate(_direction * _speed * Time.deltaTime);
            }
        }
        _lifeTime += Time.deltaTime;
        if (_lifeTime > _resetTime)
        {
            gameObject.SetActive(false);
            _useCustomDirection = false;
            transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Handles collision with other objects, triggers explosion/fade animation, and disables the projectile.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == NoCollisionTag || collision.gameObject.tag == EnemyTag || collision.gameObject.tag == FlameWarningMarkerTag || collision.gameObject.tag == DashTargetIndicatorTag)
        {
            return;
        }
        if (!breakWithFireball && collision.gameObject.tag == FireballTag)
        {
            return;
        }
        _hit = true;
        base.OnTriggerStay2D(collision);
        _collid.enabled = false;
        if (_anim != null)
        {
            _anim.SetTrigger("explosion");
            _anim.SetTrigger("fade");
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ==================== Utility ====================
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
    /// Deactivates the projectile and resets its state.
    /// </summary>
    private void Deactivate()
    {
        gameObject.SetActive(false);
        _useCustomDirection = false;
        transform.rotation = Quaternion.identity;
    }
}