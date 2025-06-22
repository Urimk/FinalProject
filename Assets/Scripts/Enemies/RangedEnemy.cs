using UnityEngine;

public class RangedEnemy : MonoBehaviour
{
    [Header("Attack Parameters")]
    [SerializeField] private float _attackCooldown;
    [SerializeField] private int _damage;
    [SerializeField] private float _range;

    [Header("Ranged Attack")]
    [SerializeField] private Transform _firepoint;
    [SerializeField] private GameObject[] _fireballs;

    [Header("Collider Parameters")]
    [SerializeField] private float _colliderDistance;
    [SerializeField] private BoxCollider2D _boxCollider;

    [Header("Player Layer")]
    [SerializeField] private LayerMask _playerLayer;

    [Header("Fireball Sound")]
    [SerializeField] private AudioClip _fireballSound;
    private float _cooldownTimer = Mathf.Infinity;

    private Animator _animator;
    private EnemyPatrol _enemyPatrol;

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

    private bool PlayerInSight()
    {
        RaycastHit2D hit = Physics2D.BoxCast(_boxCollider.bounds.center + transform.right * _range * transform.localScale.x * _colliderDistance,
                                             new Vector2(_boxCollider.bounds.size.x * _range, _boxCollider.bounds.size.y),
                                             0, Vector2.left, 0, _playerLayer);

        return hit.collider != null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_boxCollider.bounds.center + transform.right * _range * transform.localScale.x * _colliderDistance,
                             new Vector2(_boxCollider.bounds.size.x * _range, _boxCollider.bounds.size.y));
    }

    private void RangedAttack()
    {
        SoundManager.instance.PlaySound(_fireballSound, gameObject);
        _cooldownTimer = 0;
        _fireballs[FindFireball()].transform.position = _firepoint.position;
        _fireballs[FindFireball()].GetComponent<EnemyProjectile>().ActivateProjectile();
    }

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
