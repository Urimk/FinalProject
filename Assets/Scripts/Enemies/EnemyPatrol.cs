using UnityEngine;

/// <summary>
/// Handles enemy patrol movement between two points, including idle and animation logic.
/// </summary>
public class EnemyPatrol : MonoBehaviour
{
    // Direction constants for clarity
    private const int DirectionLeft = -1;
    private const int DirectionRight = 1;

    [Header("Patrol Points")]
    [SerializeField] private Transform _leftEdge;
    [SerializeField] private Transform _rightEdge;

    [Header("Enemy")]
    [SerializeField] private Transform _enemy;

    [Header("Movement Parameters")]
    [SerializeField] private float _speed;

    [Header("Idle Behaviour")]
    [SerializeField] private float _idleDuration;

    [Header("Enemy Animator")]
    [SerializeField] private Animator _anim;

    // Private fields
    private Vector3 _initScale;
    private bool _movingLeft = false;
    private float _idleTimer;

    /// <summary>
    /// Initializes the enemy's scale.
    /// </summary>
    private void Awake()
    {
        _initScale = _enemy.localScale;
    }

    /// <summary>
    /// Handles patrol movement and direction changes each frame.
    /// </summary>
    private void Update()
    {
        if (_movingLeft)
        {
            if (_enemy.position.x >= _leftEdge.position.x)
            {
                MoveInDirection(DirectionLeft);
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
                MoveInDirection(DirectionRight);
            }
            else
            {
                DirectionChange();
            }
        }
    }

    /// <summary>
    /// Handles the idle timer and toggles direction after idling.
    /// </summary>
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

    /// <summary>
    /// Moves the enemy in the specified direction and updates animation/scale.
    /// </summary>
    /// <param name="direction">Direction constant (DirectionLeft or DirectionRight)</param>
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

    /// <summary>
    /// Resets the moving animation when the object is disabled.
    /// </summary>
    private void OnDisable()
    {
        if (_anim != null)
        {
            _anim.SetBool("moving", false);
        }
    }
}
