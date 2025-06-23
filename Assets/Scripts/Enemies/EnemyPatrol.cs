using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Points")]
    [SerializeField] private Transform _leftEdge;
    [SerializeField] private Transform _rightEdge;

    [Header("Enemy")]
    [SerializeField] private Transform _enemy;

    [Header("Movement Parameters")]
    [SerializeField] private float _speed;
    private Vector3 _initScale;
    private bool _movingLeft = false;

    [Header("Idle Behaviour")]
    [SerializeField] private float _idleDuration;
    private float _idleTimer;

    [Header("Enemy Animator")]
    [SerializeField] private Animator _anim;

    private void Awake()
    {
        _initScale = _enemy.localScale;
    }
    private void Update()
    {
        if (_movingLeft)
        {
            if (_enemy.position.x >= _leftEdge.position.x)
            {
                MoveInDirection(-1);
            }
            else
            {
                DirectionChange();
            }
        }
        else
        {
            if (_enemy.position.x <= _rightEdge.position.x)
            {
                MoveInDirection(1);
            }
            else
            {
                DirectionChange();
            }
        }
    }

    private void DirectionChange()
    {
        if (_anim != null)
        {
            _anim.SetBool("moving", false);
        }
        _idleTimer += Time.deltaTime;
        if (_idleTimer > _idleDuration)
        {
            _movingLeft = !_movingLeft;
        }
    }
    private void MoveInDirection(int direction)
    {
        _idleTimer = 0;
        if (_anim != null)
        {
            _anim.SetBool("moving", true);
        }
        _enemy.localScale = new Vector3(Mathf.Abs(_initScale.x) * direction, _initScale.y, _initScale.z);
        _enemy.position = new Vector3(_enemy.position.x + Time.deltaTime * direction * _speed, _enemy.position.y, _enemy.position.z);
    }

    private void OnDisable()
    {
        if (_anim != null)
        {
            _anim.SetBool("moving", false);
        }
    }
}
