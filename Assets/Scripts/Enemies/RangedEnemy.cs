using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles ranged enemy attack logic, including player detection, fireball attack, and cooldown.
/// </summary>
public class RangedEnemy : MonoBehaviour
{
    // ==================== Constants ====================
    private const float BoxCastAngle = 0f;
    private const float BoxCastDistance = 0f;
    private static readonly Color GizmoColor = Color.red;

    // ==================== Serialized Fields ====================
    [Header("Attack Parameters")]
    [Tooltip("Cooldown time between attacks in seconds.")]
    [FormerlySerializedAs("attackCooldown")]
    [SerializeField] private float _attackCooldown;

    [Tooltip("Damage dealt to the player per attack.")]
    [FormerlySerializedAs("damage")]
    [SerializeField] private int _damage;

    [Tooltip("Attack range for detecting the player.")]
    [FormerlySerializedAs("range")]
    [SerializeField] private float _range;

    [Header("Ranged Attack")]
    [Tooltip("Transform from which fireballs are spawned.")]
    [FormerlySerializedAs("firepoint")]
    [SerializeField] private Transform _firepoint;

    [Tooltip("Pool of fireball GameObjects for reuse.")]
    [FormerlySerializedAs("fireballs")]
    [SerializeField] private GameObject[] _fireballs;

    [Header("Collider Parameters")]
    [Tooltip("Distance multiplier for the box cast used in player detection.")]
    [FormerlySerializedAs("colliderDistance")]
    [SerializeField] private float _colliderDistance;

    [Tooltip("BoxCollider2D used for player detection.")]
    [FormerlySerializedAs("boxCollider")]
    [SerializeField] private BoxCollider2D _boxCollider;

    [Header("Player Layer")]
    [Tooltip("Layer mask for detecting the player.")]
    [FormerlySerializedAs("playerLayer")]
    [SerializeField] private LayerMask _playerLayer;

    [Header("Fireball Sound")]
    [Tooltip("Sound to play when firing a fireball.")]
    [FormerlySerializedAs("fireballSound")]
    [SerializeField] private AudioClip _fireballSound;

    // ==================== Private Fields ====================
    private float _cooldownTimer = Mathf.Infinity;
    private Animator _animator;
    private EnemyPatrol _enemyPatrol;

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
            if (_cooldownTimer >= _attackCooldown)
            {
                _cooldownTimer = 0;
                _animator.SetTrigger("rangedAttack");
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
    /// Plays the fireball sound, resets cooldown, and activates a fireball.
    /// </summary>
    private void RangedAttack()
    {
        SoundManager.instance.PlaySound(_fireballSound, gameObject);
        _cooldownTimer = 0;
        _fireballs[FindFireball()].transform.position = _firepoint.position;
        _fireballs[FindFireball()].GetComponent<EnemyProjectile>().ActivateProjectile();
    }

    /// <summary>
    /// Finds the index of an inactive fireball in the pool.
    /// </summary>
    private int FindFireball()
    {
        for (int i = 0; i < _fireballs.Length; i++)
        {
            if (!_fireballs[i].activeInHierarchy)
            {
                return i;
            }
        }
        return 0;
    }
}
