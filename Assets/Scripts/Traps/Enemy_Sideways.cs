using UnityEngine;

public class Enemy_Sideways : MonoBehaviour
{
    [SerializeField] private float _movementDistance;
    [SerializeField] private float _speed;
    [SerializeField] private float _damage;
    [SerializeField] private bool _moveVertically;

    public bool MovingNegative = true;
    private float _minEdge;
    private float _maxEdge;

    private void Awake()
    {
        float currentPos = _moveVertically ? transform.position.y : transform.position.x;
        _minEdge = currentPos - _movementDistance;
        _maxEdge = currentPos + _movementDistance;
    }

    private void Update()
    {
        if (MovingNegative)
        {
            if ((_moveVertically && transform.position.y > _minEdge) ||
                (!_moveVertically && transform.position.x > _minEdge))
            {
                Move(-1);
            }
            else
            {
                MovingNegative = false;
            }
        }
        else
        {
            if ((_moveVertically && transform.position.y < _maxEdge) ||
                (!_moveVertically && transform.position.x < _maxEdge))
            {
                Move(1);
            }
            else
            {
                MovingNegative = true;
            }
        }
    }

    private void Move(int direction)
    {
        Vector3 position = transform.position;
        if (_moveVertically)
            position.y += direction * _speed * Time.deltaTime;
        else
            position.x += direction * _speed * Time.deltaTime;
        transform.position = position;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<Health>().TakeDamage(_damage);
        }
    }
}
