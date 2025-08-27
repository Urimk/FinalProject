using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Flexible patrol system that can be attached to any object for movement between points.
/// Supports conditional triggering, custom paths, and destruction timers.
/// </summary>
public class PatrolSystem : MonoBehaviour
{
    [System.Serializable]
    public enum PatrolMode
    {
        Horizontal,      // Only move on X axis
        Vertical,        // Only move on Y axis
        PointToPoint,    // Move between two 3D points
        CustomPath       // Move along a custom path (future feature)
    }

    [System.Serializable]
    public enum PatrolPointType
    {
        Coordinates,     // Use Vector3 coordinates
        WorldObjects,    // Use Transform objects in the world
        Range           // Use a range from current position
    }

    [System.Serializable]
    public enum TriggerType
    {
        Always,         // Patrol immediately
        OnTrigger,      // Start patrol when triggered
        OnProximity,    // Start patrol when player is nearby
        OnTimer,        // Start patrol after a delay
        Manual          // Start patrol manually via code
    }

    // ==================== Serialized Fields ====================
    [Header("Patrol Configuration")]
    [SerializeField] private PatrolMode _patrolMode = PatrolMode.Horizontal;
    [SerializeField] private TriggerType _triggerType = TriggerType.Always;
    [SerializeField] private bool _destroyAfterPatrol = false;
    [SerializeField] private float _destroyDelay = 0f;
    [SerializeField] private bool _flipSprite = true;

    [Header("Patrol Points")]
    [SerializeField] private PatrolPointType _patrolPointType = PatrolPointType.Coordinates;
    [SerializeField] private Vector3 _startPoint;
    [SerializeField] private Vector3 _endPoint;
    [SerializeField] private bool _useWorldCoordinates = false;

    [Header("World Objects")]
    [SerializeField] private Transform _leftPoint;
    [SerializeField] private Transform _rightPoint;

    [Header("Range Settings")]
    [SerializeField] private float _patrolRange = 3f;
    [SerializeField] private bool _patrolDirectionFlip = false; // false = go to right/up first, true = go to left/down first

    [Header("Movement Parameters")]
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _idleDuration = 1f;
    [SerializeField] private bool _loopPatrol = true;
    [SerializeField] private bool _idleBeforeStart = false;

    [Header("Trigger Parameters")]
    [SerializeField] private float _proximityDistance = 5f;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private LayerMask _proximityLayerMask = 1;

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _movementParameter = "moving";

    [Header("Events")]
    [SerializeField] private UnityEvent _onPatrolStart;
    [SerializeField] private UnityEvent _onPatrolEnd;
    [SerializeField] private UnityEvent _onDestroy;

    // ==================== Private Fields ====================
    private Vector3 _originalPosition;
    private bool _isPatrolling = false;
    private bool _isTriggered = false;
    private float _idleTimer = 0f;
    private bool _movingToEnd = true;
    private Transform _playerTransform;
    private Vector3 _initScale;
    private bool _isIdlingBeforeStart = false;

    // ==================== Properties ====================
    public bool IsPatrolling => _isPatrolling;
    public bool IsTriggered => _isTriggered;

    // ==================== Unity Lifecycle ====================
    private void Awake()
    {
        _originalPosition = transform.position;
        _initScale = transform.localScale;
        _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Set initial direction based on patrol direction
        _movingToEnd = !_patrolDirectionFlip;
    }

    private void Start()
    {
        if (_triggerType == TriggerType.Always)
            StartPatrol();
        else if (_triggerType == TriggerType.OnTimer)
            StartCoroutine(StartPatrolAfterDelay(_startDelay));
    }

    private void Update()
    {
        if (!_isPatrolling) return;

        if (_triggerType == TriggerType.OnProximity && !_isTriggered)
            CheckProximityTrigger();

        if (_isPatrolling && _isTriggered)
        {
            if (_isIdlingBeforeStart)
                HandleIdleBeforeStart();
            else
                HandlePatrolMovement();
        }
    }

    // ==================== Public Methods ====================
    public void StartPatrol()
    {
        if (_isPatrolling) return;
        
        _isPatrolling = true;
        _isTriggered = true;
        _onPatrolStart?.Invoke();
        
        if (_idleBeforeStart)
        {
            _isIdlingBeforeStart = true;
            _idleTimer = 0f;
            if (_animator != null)
                _animator.SetBool(_movementParameter, false);
        }
        else
        {
            if (_animator != null)
                _animator.SetBool(_movementParameter, true);
        }
    }

    public void StopPatrol()
    {
        _isPatrolling = false;
        _isTriggered = false;
        _onPatrolEnd?.Invoke();
        
        if (_animator != null)
            _animator.SetBool(_movementParameter, false);
    }

    public void SetPatrolPoints(Vector3 start, Vector3 end)
    {
        _startPoint = start;
        _endPoint = end;
    }

    public void SetSpeed(float speed) => _speed = speed;

    public void SetPatrolObjects(Transform leftPoint, Transform rightPoint)
    {
        _patrolPointType = PatrolPointType.WorldObjects;
        _leftPoint = leftPoint;
        _rightPoint = rightPoint;
    }

    public void SetPatrolRange(float range)
    {
        _patrolPointType = PatrolPointType.Range;
        _patrolRange = range;
    }

    public PatrolPointType GetPatrolPointType() => _patrolPointType;

    // ==================== Private Methods ====================
    private void HandleIdleBeforeStart()
    {
        _idleTimer += Time.deltaTime;
        
        if (_animator != null)
            _animator.SetBool(_movementParameter, false);
        
        if (_idleTimer >= _idleDuration)
        {
            _isIdlingBeforeStart = false;
            _idleTimer = 0f;
            if (_animator != null)
                _animator.SetBool(_movementParameter, true);
        }
    }

    private void HandlePatrolMovement()
    {
        GetPatrolPoints(out Vector3 leftPoint, out Vector3 rightPoint);
        
        if (_patrolMode == PatrolMode.Horizontal)
        {
            HandleHorizontalMovement(leftPoint, rightPoint);
        }
        else if (_patrolMode == PatrolMode.Vertical)
        {
            HandleVerticalMovement(leftPoint, rightPoint);
        }
        else
        {
            HandlePointToPointMovement(leftPoint, rightPoint);
        }
    }

    private void HandleHorizontalMovement(Vector3 leftPoint, Vector3 rightPoint)
    {
        if (_movingToEnd && transform.position.x >= rightPoint.x) // Passed right point
        {
            HandleTargetReached();
        }
        else if (!_movingToEnd && transform.position.x <= leftPoint.x) // Passed left point
        {
            HandleTargetReached();
        }
        else
        {
            Vector3 target = _movingToEnd ? rightPoint : leftPoint;
            MoveTowardsTarget(target);
        }
    }

    private void HandleVerticalMovement(Vector3 leftPoint, Vector3 rightPoint)
    {
        if (_movingToEnd && transform.position.y >= rightPoint.y) // Passed up point
        {
            HandleTargetReached();
        }
        else if (!_movingToEnd && transform.position.y <= leftPoint.y) // Passed down point
        {
            HandleTargetReached();
        }
        else
        {
            Vector3 target = _movingToEnd ? rightPoint : leftPoint;
            MoveTowardsTarget(target);
        }
    }

    private void HandlePointToPointMovement(Vector3 leftPoint, Vector3 rightPoint)
    {
        bool isVertical = IsVerticalMovement();
        float currentPos = isVertical ? transform.position.y : transform.position.x;
        float targetPos = isVertical ? 
            (_movingToEnd ? rightPoint.y : leftPoint.y) : 
            (_movingToEnd ? rightPoint.x : leftPoint.x);
        
        if (Mathf.Abs(currentPos - targetPos) > 0.5f)
        {
            Vector3 target = _movingToEnd ? rightPoint : leftPoint;
            MoveTowardsTarget(target);
        }
        else
        {
            HandleTargetReached();
        }
    }

    private bool IsVerticalMovement()
    {
        return _patrolMode == PatrolMode.Vertical;
    }

    private void MoveTowardsTarget(Vector3 target)
    {
        Vector3 direction = GetMovementDirection();
        Vector3 movement = direction * _speed * Time.deltaTime;
        transform.position += movement;
        
        // Update sprite direction
        if (_flipSprite && direction.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(_initScale.x) * Mathf.Sign(direction.x), _initScale.y, _initScale.z);
        }
        
        if (_animator != null)
            _animator.SetBool(_movementParameter, true);
    }

    private Vector3 GetMovementDirection()
    {
        if (_patrolMode == PatrolMode.Horizontal)
            return _movingToEnd ? Vector3.right : Vector3.left;
        
        if (_patrolMode == PatrolMode.Vertical)
            return _movingToEnd ? Vector3.up : Vector3.down;
        
        return (_movingToEnd ? _endPoint : _startPoint - transform.position).normalized;
    }

    private void GetPatrolPoints(out Vector3 leftPoint, out Vector3 rightPoint)
    {
        switch (_patrolPointType)
        {
            case PatrolPointType.Coordinates:
                leftPoint = _useWorldCoordinates ? _startPoint : _originalPosition + _startPoint;
                rightPoint = _useWorldCoordinates ? _endPoint : _originalPosition + _endPoint;
                break;
                
            case PatrolPointType.WorldObjects:
                if (_leftPoint == null || _rightPoint == null)
                {
                    Debug.LogWarning($"[{gameObject.name}] World objects not assigned!");
                    leftPoint = rightPoint = _originalPosition;
                }
                else
                {
                    // Determine which point is actually left/right based on X position
                    bool leftIsLeft = _leftPoint.position.x <= _rightPoint.position.x;
                    leftPoint = leftIsLeft ? _leftPoint.position : _rightPoint.position;
                    rightPoint = leftIsLeft ? _rightPoint.position : _leftPoint.position;
                }
                break;
                
            case PatrolPointType.Range:
                if (_patrolMode == PatrolMode.Horizontal)
                {
                    leftPoint = _originalPosition + Vector3.left * _patrolRange;
                    rightPoint = _originalPosition + Vector3.right * _patrolRange;
                }
                else
                {
                    leftPoint = _originalPosition + Vector3.down * _patrolRange;
                    rightPoint = _originalPosition + Vector3.up * _patrolRange;
                }
                break;
                
            default:
                leftPoint = rightPoint = _originalPosition;
                break;
        }
    }

    private void HandleTargetReached()
    {
        _idleTimer += Time.deltaTime;
        
        if (_animator != null)
            _animator.SetBool(_movementParameter, false);
        
        if (_idleTimer >= _idleDuration)
        {
            _idleTimer = 0f;
            
            if (_loopPatrol)
            {
                _movingToEnd = !_movingToEnd;
            }
            else
            {
                StopPatrol();
                if (_destroyAfterPatrol)
                    StartCoroutine(DestroyAfterDelay(_destroyDelay));
            }
        }
    }

    private void CheckProximityTrigger()
    {
        if (_playerTransform == null) return;
        
        if (Vector3.Distance(transform.position, _playerTransform.position) <= _proximityDistance)
            StartPatrol();
    }

    private IEnumerator StartPatrolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartPatrol();
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _onDestroy?.Invoke();
        Destroy(gameObject);
    }

    // ==================== Trigger Methods ====================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggerType == TriggerType.OnTrigger && !_isTriggered)
            StartPatrol();
    }

    // ==================== Gizmos ====================
    private void OnDrawGizmosSelected()
    {
        GetPatrolPoints(out Vector3 leftPoint, out Vector3 rightPoint);
        
        // Draw patrol path
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(leftPoint, rightPoint);
        
        // Draw patrol points
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(leftPoint, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(rightPoint, 0.3f);
        
        // Draw proximity trigger
        if (_triggerType == TriggerType.OnProximity)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _proximityDistance);
        }
        
        // Draw range indicator
        if (_patrolPointType == PatrolPointType.Range)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _patrolRange);
        }
    }
}
