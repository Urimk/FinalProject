using System.Runtime.CompilerServices;

using UnityEngine;

/// <summary>
/// Handles melee enemy attack logic, including player detection and attack cooldown.
/// </summary>
public class MeleeEnemy : MonoBehaviour
{
    // ==================== Constants ====================
    private const float BoxCastAngle = 0f;
    private const float BoxCastDistance = 0f;
    private static readonly Color GizmoColor = Color.red;

    // ==================== Serialized Fields ====================
    [Header("Attack Parameters")]
    [Tooltip("Cooldown time between attacks in seconds.")]
    [SerializeField] private float _attackCooldown;
    [Tooltip("Damage dealt to the player per attack.")]
    [SerializeField] private int _damage;
    [Tooltip("Attack range for detecting the player.")]
    [SerializeField] private float _range;

    [Header("Collider Parameters")]
    [Tooltip("Distance multiplier for the box cast used in player detection.")]
    [SerializeField] private float _colliderDistance;
    [Tooltip("BoxCollider2D used for player detection.")]
    [SerializeField] private BoxCollider2D _boxCollider;

    [Header("Player Layer")]
    [Tooltip("Layer mask for detecting the player.")]
    [SerializeField] private LayerMask _playerLayer;

    [Header("Attack Sound")]
    [Tooltip("Sound to play when performing a melee attack.")]
    [SerializeField] private AudioClip _attackSound;

    // ==================== Private Fields ====================
    private float _cooldownTimer = Mathf.Infinity;
    private Health _playerHealth;
    private EnemyPatrol _enemyPatrol;
    private Animator _animator;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Initializes animator and patrol references.
    /// </summary>
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _enemyPatrol = GetComponentInParent<EnemyPatrol>();
    }

    /// <summary>
    /// Handles attack cooldown, triggers attack, and manages patrol state.
    /// </summary>
    private void Update()
    {
        _cooldownTimer += Time.deltaTime;
        if (PlayerInSight())
        {
            if (_cooldownTimer >= _attackCooldown && _playerHealth.CurrentHealth > 0)
            {
                _cooldownTimer = 0;
                _animator.SetTrigger("meleeAttack");
                SoundManager.instance.PlaySound(_attackSound, gameObject);
            }
        }
        if (_enemyPatrol != null)
        {
            _enemyPatrol.enabled = !PlayerInSight();
        }
    }

    // ==================== Attack Logic ====================
    /// <summary>
    /// Checks if the player is in sight using a box cast.
    /// </summary>
    private bool PlayerInSight()
    {
        RaycastHit2D hit = Physics2D.BoxCast(_boxCollider.bounds.center + transform.right * _range * transform.localScale.x * _colliderDistance,
                                             new Vector2(_boxCollider.bounds.size.x * _range, _boxCollider.bounds.size.y),
                                             BoxCastAngle, Vector2.left, BoxCastDistance, _playerLayer);
        if (hit.collider != null)
        {
            _playerHealth = hit.transform.GetComponent<Health>();
        }

        return hit.collider != null;
    }

    /// <summary>
    /// Draws the box cast gizmo in the editor for debugging.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawWireCube(_boxCollider.bounds.center + transform.right * _range * transform.localScale.x * _colliderDistance,
                             new Vector2(_boxCollider.bounds.size.x * _range, _boxCollider.bounds.size.y));
    }

    /// <summary>
    /// Damages the player if still in sight.
    /// </summary>
    private void DamagePlayer()
    {
        // Checks if the player is still in sight
        if (PlayerInSight())
        {
            _playerHealth.TakeDamage(_damage);
        }
    }
}
