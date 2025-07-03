using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Controls an enemy that moves sideways (horizontally or vertically) and damages the player on contact.
/// </summary>
public class Enemy_Sideways : MonoBehaviour
{
    // === Inspector Fields ===
    [Header("Movement Settings")]
    [Tooltip("Distance the enemy moves from its starting position.")]
    [SerializeField] private float _movementDistance;

    [Tooltip("Speed at which the enemy moves.")]
    [SerializeField] private float _speed;

    [Tooltip("Amount of damage dealt to the player on contact.")]
    [SerializeField] private float _damage;

    [Tooltip("If true, the enemy moves vertically; otherwise, horizontally.")]
    [SerializeField] private bool _moveVertically;

    // === State ===
    [FormerlySerializedAs("MovingNegative")]
    [Tooltip("Indicates if the enemy is currently moving in the negative direction.")]
    [SerializeField] private bool _movingNegative = true;
    /// <summary>
    /// Gets or sets whether the enemy is currently moving in the negative direction.
    /// </summary>
    public bool MovingNegative
    {
        get => _movingNegative;
        set => _movingNegative = value;
    }

    private float _minEdge;
    private float _maxEdge;

    /// <summary>
    /// Initializes the movement edges based on the starting position.
    /// </summary>
    private void Awake()
    {
        float currentPos = _moveVertically ? transform.position.y : transform.position.x;
        _minEdge = currentPos - _movementDistance;
        _maxEdge = currentPos + _movementDistance;
    }

    /// <summary>
    /// Handles movement logic each frame.
    /// </summary>
    private void Update()
    {
        if (_movingNegative)
        {
            if ((_moveVertically && transform.position.y > _minEdge) ||
                (!_moveVertically && transform.position.x > _minEdge))
            {
                Move(-1);
            }
            else
            {
                _movingNegative = false;
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
                _movingNegative = true;
            }
        }
    }

    /// <summary>
    /// Moves the enemy in the specified direction.
    /// </summary>
    /// <param name="direction">-1 for negative, 1 for positive direction.</param>
    private void Move(int direction)
    {
        Vector3 position = transform.position;
        if (_moveVertically)
            position.y += direction * _speed * Time.deltaTime;
        else
            position.x += direction * _speed * Time.deltaTime;
        transform.position = position;
    }

    /// <summary>
    /// Damages the player if they stay in the enemy's trigger.
    /// </summary>
    /// <param name="collision">The collider staying in the trigger.</param>
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<Health>().TakeDamage(_damage);
        }
    }
}
