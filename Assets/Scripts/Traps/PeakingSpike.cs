using UnityEngine;

public class PeakingSpike : MonoBehaviour
{
    [Header("Movement Settings")]
    public float BasePosition;          // Y position when at base
    public float LowPosition;          // Y position when lowered
    public float BaseHoldTime = 2f;          // Time to stay at base position
    public float LowHoldTime = 1f;           // Time to stay at low position
    public float MoveSpeed = 5f;             // Speed of movement between positions
    public float InitialDelay = 0f;          // Delay before starting the loop

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _timer;
    private bool _isAtBase = true;
    private bool _isMoving = false;
    private bool _hasStarted = false;

    private void Start()
    {
        // Store the initial position and set base position
        _startPos = transform.position;
        //_startPos.y = BasePosition;
        transform.position = _startPos;
        BasePosition = _startPos.y;
        LowPosition = BasePosition - 1.7f;

        // Set initial timer to the delay value
        _timer = InitialDelay;
    }

    private void Update()
    {
        if (!_hasStarted)
        {
            // Wait for initial delay
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _hasStarted = true;
                _timer = BaseHoldTime; // Start with base hold time
            }
            return;
        }

        if (!_isMoving)
        {
            // Count down the hold timer
            _timer -= Time.deltaTime;

            if (_timer <= 0f)
            {
                // Start moving to the other position
                StartMovement();
            }
        }
        else
        {
            // Move towards target position
            MoveToTarget();
        }
    }

    private void StartMovement()
    {
        _isMoving = true;

        if (_isAtBase)
        {
            // Moving from base to low position
            _targetPos = _startPos;
            _targetPos.y = LowPosition;
        }
        else
        {
            // Moving from low to base position
            _targetPos = _startPos;
            _targetPos.y = BasePosition;
        }
    }

    private void MoveToTarget()
    {
        // Move towards target position
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, MoveSpeed * Time.deltaTime);

        // Check if we've reached the target
        if (Vector3.Distance(transform.position, _targetPos) < 0.01f)
        {
            transform.position = _targetPos;
            _isMoving = false;
            _isAtBase = !_isAtBase;

            // Set the appropriate hold time
            _timer = _isAtBase ? BaseHoldTime : LowHoldTime;
        }
    }
}
