using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Handles projectile movement, collision, and deactivation logic.
/// </summary>
public class Projectile : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultLifetime = 5f;
    private const int DamageAmount = 1;
    private const string DoorObjectName = "Door";
    private const string NoCollisionTag = "NoCollision";
    private const string PlayerTag = "Player";
    private const string AnimatorExplosion = "explosion";
    private const string CheckPointTag = "Checkpoint";
    private const string DashTargetIndicatorTag = "DashTargetIndicator";
    private const string FlameWarningMarkerTag = "FlameWarningMarker";


    // ==================== Inspector Fields ====================
    [Header("Projectile Settings")]
    [Tooltip("Speed of the projectile.")]
    [SerializeField] private float speed;

    // ==================== Private Fields ====================
    private float _direction;
    private bool _hit;
    private float _lifetime;
    private BoxCollider2D _boxCollider;
    private Animator _animator;

    /// <summary>
    /// Unity Awake callback. Initializes components.
    /// </summary>
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// Unity Update callback. Handles movement and lifetime.
    /// </summary>
    private void Update()
    {
        _lifetime += Time.deltaTime;
        if (_lifetime > DefaultLifetime)
        {
            Deactivate();
            return;
        }
        if (_hit)
            return;
        float movement = speed * Time.deltaTime * _direction;
        transform.Translate(movement, 0, 0);
    }

    /// <summary>
    /// Handles collision logic for the projectile.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name == DoorObjectName || other.gameObject.tag == NoCollisionTag || other.gameObject.tag == PlayerTag || other.gameObject.tag == CheckPointTag || other.gameObject.tag == FlameWarningMarkerTag || other.gameObject.tag == DashTargetIndicatorTag) return;
        _hit = true;
        _boxCollider.enabled = false;
        _animator.SetTrigger(AnimatorExplosion);
        if (other.TryGetComponent<IDamageable>(out var dmg) && other.gameObject.tag != PlayerTag)
            dmg.TakeDamage(DamageAmount);
        // Deactivates in the animation end
    }

    /// <summary>
    /// Deactivates the projectile and resets state.
    /// </summary>
    private void Deactivate()
    {
        _hit = false;
        _boxCollider.enabled = true;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Sets the direction and resets the projectile for reuse.
    /// </summary>
    public void SetDirection(float direction)
    {
        _lifetime = 0;
        _direction = direction;
        gameObject.SetActive(true);
        _hit = false;
        if (_boxCollider != null)
        {
            _boxCollider.enabled = true;
        }
        float localScaleX = transform.localScale.x;
        if (Mathf.Sign(localScaleX) != direction)
        {
            localScaleX = -localScaleX;
        }
        transform.localScale = new Vector3(localScaleX, transform.localScale.y, transform.localScale.z);
    }
}
