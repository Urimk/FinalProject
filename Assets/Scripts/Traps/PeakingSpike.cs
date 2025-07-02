using UnityEngine;

/// <summary>
/// Spike trap that moves between base and low positions on a timer.
/// </summary>
public class PeakingSpike : MonoBehaviour
{
    // === Constants ===
    private const float DefaultBaseHoldTime = 2f;
    private const float DefaultLowHoldTime = 1f;
    private const float DefaultMoveSpeed = 5f;
    private const float DefaultInitialDelay = 0f;
    private const float DefaultLowOffset = -1.7f;
    private const float PositionEpsilon = 0.01f;

    // === Serialized Fields ===
    [Header("Movement Settings")]
    public float BasePosition;
    public float LowPosition;
    public float BaseHoldTime = DefaultBaseHoldTime;
    public float LowHoldTime = DefaultLowHoldTime;
    public float MoveSpeed = DefaultMoveSpeed;
    public float InitialDelay = DefaultInitialDelay;

    // === Private Fields ===
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _timer;
    private bool _isAtBase = true;
    private bool _isMoving = false;
    private bool _hasStarted = false;

    /// <summary>
    /// Unity Start callback. Initializes positions and timers.
    /// </summary>
    private void Start()
    {
        _startPos = transform.position;
        transform.position = _startPos;
        BasePosition = _startPos.y;
        LowPosition = BasePosition + DefaultLowOffset;
        _timer = InitialDelay;
    }

    /// <summary>
    /// Unity Update callback. Handles movement and timing.
    /// </summary>
    private void Update()
    {
        if (!_hasStarted)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _hasStarted = true;
                _timer = BaseHoldTime;
            }
            return;
        }
        if (!_isMoving)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                StartMovement();
            }
        }
        else
        {
            MoveToTarget();
        }
    }

    /// <summary>
    /// Starts movement to the next position.
    /// </summary>
    private void StartMovement()
    {
        _isMoving = true;
        if (_isAtBase)
        {
            _targetPos = _startPos;
            _targetPos.y = LowPosition;
        }
        else
        {
            _targetPos = _startPos;
            _targetPos.y = BasePosition;
        }
    }

    /// <summary>
    /// Moves the spike towards the target position.
    /// </summary>
    private void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, MoveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, _targetPos) < PositionEpsilon)
        {
            transform.position = _targetPos;
            _isMoving = false;
            _isAtBase = !_isAtBase;
            _timer = _isAtBase ? BaseHoldTime : LowHoldTime;
        }
    }
}
