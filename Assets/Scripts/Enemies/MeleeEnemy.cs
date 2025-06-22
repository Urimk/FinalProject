using System.Runtime.CompilerServices;

using UnityEngine;

public class MeleeEnemy : MonoBehaviour
{
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
    private float _cooldownTimer = Mathf.Infinity;
    private Health _playerHealth;
    private EnemyPatrol _enemyPatrol;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _enemyPatrol = GetComponentInParent<EnemyPatrol>();
    }

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

    private bool PlayerInSight()
    {
        RaycastHit2D hit = Physics2D.BoxCast(_boxCollider.bounds.center + transform.right * _range * transform.localScale.x * _colliderDistance,
                                             new Vector2(_boxCollider.bounds.size.x * _range, _boxCollider.bounds.size.y),
                                             0, Vector2.left, 0, _playerLayer);
        if (hit.collider != null)
        {
            _playerHealth = hit.transform.GetComponent<Health>();
        }

        return hit.collider != null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_boxCollider.bounds.center + transform.right * _range * transform.localScale.x * _colliderDistance,
                             new Vector2(_boxCollider.bounds.size.x * _range, _boxCollider.bounds.size.y));
    }

    private void DamagePlayer()
    {
        // Checks if the player is still in sight
        if (PlayerInSight())
        {
            _playerHealth.TakeDamage(_damage);
        }
    }
}
