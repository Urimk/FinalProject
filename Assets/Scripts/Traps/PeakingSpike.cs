using UnityEngine;
using UnityEngine.Serialization;

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

    // === Inspector Fields ===
    [Header("Movement Settings")]
    [FormerlySerializedAs("BasePosition")]
    [Tooltip("Y position where the spike rests at its base.")]
    [SerializeField] private float _basePosition;
    public float BasePosition { get => _basePosition; set => _basePosition = value; }

    [FormerlySerializedAs("LowPosition")]
    [Tooltip("Y position where the spike moves to when lowered.")]
    [SerializeField] private float _lowPosition;
    public float LowPosition { get => _lowPosition; set => _lowPosition = value; }

    [FormerlySerializedAs("BaseHoldTime")]
    [Tooltip("Time in seconds the spike stays at the base position.")]
    [SerializeField] private float _baseHoldTime = DefaultBaseHoldTime;
    public float BaseHoldTime { get => _baseHoldTime; set => _baseHoldTime = value; }

    [FormerlySerializedAs("LowHoldTime")]
    [Tooltip("Time in seconds the spike stays at the low position.")]
    [SerializeField] private float _lowHoldTime = DefaultLowHoldTime;
    public float LowHoldTime { get => _lowHoldTime; set => _lowHoldTime = value; }

    [FormerlySerializedAs("MoveSpeed")]
    [Tooltip("Speed at which the spike moves between positions.")]
    [SerializeField] private float _moveSpeed = DefaultMoveSpeed;
    public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }

    [FormerlySerializedAs("InitialDelay")]
    [Tooltip("Delay before the spike starts moving for the first time.")]
    [SerializeField] private float _initialDelay = DefaultInitialDelay;
    public float InitialDelay { get => _initialDelay; set => _initialDelay = value; }

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
        _basePosition = _startPos.y;
        _lowPosition = _basePosition + DefaultLowOffset;
        _timer = _initialDelay;
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
                _timer = _baseHoldTime;
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
            _targetPos.y = _lowPosition;
        }
        else
        {
            _targetPos = _startPos;
            _targetPos.y = _basePosition;
        }
    }

    /// <summary>
    /// Moves the spike towards the target position.
    /// </summary>
    private void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, _moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, _targetPos) < PositionEpsilon)
        {
            transform.position = _targetPos;
            _isMoving = false;
            _isAtBase = !_isAtBase;
            _timer = _isAtBase ? _baseHoldTime : _lowHoldTime;
        }
    }
}
