using UnityEngine;

public class EnemyProjectile : EnemyDamage
{
    [SerializeField] private float _speed;
    [SerializeField] private float _size;
    [SerializeField] private float _resetTime;
    private float _lifeTime;
    private Animator _anim;
    private bool _hit;
    private BoxCollider2D _collid;
    private Vector2 _fullColliderSize;
    private bool _isComingOut = false;
    private float _colliderGrowTime = 0.3f;
    private float _growTimer = 0f;
    private BoxCollider2D _boxCollider;
    private Vector2 _direction = Vector2.right;
    private bool _useCustomDirection = false;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _collid = GetComponent<BoxCollider2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _fullColliderSize = _boxCollider.size;
    }

    public void ActivateProjectile()
    {
        _hit = false;
        _lifeTime = 0;
        gameObject.SetActive(true);
        _collid.enabled = true;
        if (_isComingOut)
        {
            _boxCollider.size = Vector2.zero;
            _boxCollider.offset = Vector2.zero;
            _colliderGrowTime = _speed / 2.25f;
            _growTimer = 0f;
        }
        else
        {
            _boxCollider.size = _fullColliderSize;
        }
    }

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

    public void SetComingOut(bool value)
    {
        _isComingOut = value;
    }

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
            if (t >= 1f)
            {
                _isComingOut = false;
                _boxCollider.size = _fullColliderSize;
                _boxCollider.offset = Vector2.zero;
            }
        }
        transform.localScale = new Vector3(_size, _size, 1);
        if (_useCustomDirection)
        {
            transform.Translate(_direction * _speed * Time.deltaTime, Space.World);
        }
        else
        {
            transform.Translate(Vector2.right * _speed * Time.deltaTime);
        }
        _lifeTime += Time.deltaTime;
        if (_lifeTime > _resetTime)
        {
            gameObject.SetActive(false);
            _useCustomDirection = false;
            transform.rotation = Quaternion.identity;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "NoCollision" || collision.gameObject.tag == "Enemy")
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
        _useCustomDirection = false;
        transform.rotation = Quaternion.identity;
    }
}
