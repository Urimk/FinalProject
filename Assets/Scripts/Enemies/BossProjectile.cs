using UnityEngine;

public class BossProjectile : EnemyDamage
{
    [SerializeField] private float _speed;
    [SerializeField] private float _size;
    [SerializeField] private float _resetTime;
    private float _lifeTime;
    private Animator _anim;
    private bool _hit;
    private BoxCollider2D _collid;
    public BossRewardManager rewardManager;
    private Vector2 _direction;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _collid = GetComponent<BoxCollider2D>();
    }

    public void ActivateProjectile()
    {
        _hit = false;
        _lifeTime = 0;
        gameObject.SetActive(true);
        _collid.enabled = true;
    }

    public void Launch(Vector2 startPosition, Vector2 targetPosition, float speed)
    {
        _hit = false;
        _lifeTime = 0;
        transform.localScale = new Vector3(_size, _size, 1f);
        transform.position = startPosition;
        _speed = speed;
        _direction = (targetPosition - startPosition).normalized;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        gameObject.SetActive(true);
        _collid.enabled = true;
    }

    private void Update()
    {
        if (_hit) return;
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        _lifeTime += Time.deltaTime;
        if (_lifeTime > _resetTime)
        {
            if (rewardManager != null)
            {
                rewardManager.ReportAttackMissed();
            }
            Deactivate();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "NoCollision" || collision.gameObject.tag == "Enemy")
        {
            return;
        }
        bool hitPlayer = collision.tag == "Player";
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
        if (_anim != null)
        {
            _anim.SetTrigger("explosion");
        }
        else
        {
            Deactivate();
        }
    }

    public void SetDamage(int newDamage)
    {
        _damage = newDamage;
    }

    public void SetSpeed(float newSpeed)
    {
        _speed = newSpeed;
    }

    public void SetSize(float newSize)
    {
        _size = newSize;
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
        transform.rotation = Quaternion.identity;
    }
}
