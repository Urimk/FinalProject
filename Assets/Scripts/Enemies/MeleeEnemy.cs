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
    [SerializeField] private float _attackCooldown;
    [SerializeField] private int _damage;
    [SerializeField] private float _range;

    [Header("Collider Parameters")]
    [SerializeField] private float _colliderDistance;
    [SerializeField] private BoxCollider2D _boxCollider;

    [Header("Player Layer")]
    [SerializeField] private LayerMask _playerLayer;

    [Header("Attack Sound")]
    [SerializeField] private AudioClip _attackSound;

    // ==================== Private Fields ====================
    private float _cooldownTimer = Mathf.Infinity;
    private Health _playerHealth;
    private EnemyPatrol _enemyPatrol;
    private Animator _animator;

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
            if (_cooldownTimer >= _attackCooldown && _playerHealth.currentHealth > 0)
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
