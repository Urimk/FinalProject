using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Advanced camera controller with three movement modes per axis: Static, Follow, and Moving.
/// Supports smooth transitions, directional offsets, and chase mechanics.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ==================== Enums ====================
    /// <summary>
    /// Camera movement modes for each axis.
    /// </summary>
    public enum CameraMode
    {
        Static,     // Fixed position
        Follow,     // Follows player
        Moving      // Independent movement (chase, scrolling)
    }

    // ==================== Constants ====================
    private const float PlayerMoveThreshold = 0.01f;
    private const float CameraMiddleZonePercent = 0.1f;
    private const int PlayerFallDamage = 100;
    private const float DefaultDirectionalOffset = 6f;
    private const float DefaultDirectionalSmoothing = 4f;
    private const float DefaultFollowSmoothing = 5f;
    private const float DefaultMovingSpeed = 5f;
    private const float DefaultModeTransitionDuration = 0.5f;
    private const float InstantFollowSmoothing = 100f;
    private const float DefaultFollowYOffset = 2f;
    private const float DefaultFollowXOffset = 0f;
    
    // ==================== Dependencies ====================
    [Header("Dependencies")]
    [Tooltip("Reference to the player Transform.")]
    [FormerlySerializedAs("player")]
    [SerializeField] private Transform _player;
    [Tooltip("Reference to the player's Health component.")]
    [FormerlySerializedAs("playerHealth")]
    [SerializeField] private Health _playerHealth;

    // ==================== Camera Modes ====================
    [Header("Camera Modes")]
    [Tooltip("Current X-axis movement mode.")]
    [SerializeField] private CameraMode _xMode = CameraMode.Follow;
    [Tooltip("Current Y-axis movement mode.")]
    [SerializeField] private CameraMode _yMode = CameraMode.Static;

    // ==================== Follow Settings ====================
    [Header("Follow Settings")]
    [Tooltip("Base X offset when following the player.")]
    [SerializeField] private float _followXOffset = DefaultFollowXOffset;
    [Tooltip("Y offset when following the player.")]
    [SerializeField] private float _followYOffset = DefaultFollowYOffset;
    [Tooltip("Smoothing speed for following the player.")]
    [SerializeField] private float _followSmoothing = DefaultFollowSmoothing;

    [Header("Directional Offset")]
    [Tooltip("Additional X offset when player is facing right (shows more ahead).")]
    [SerializeField] private float _directionalOffset = DefaultDirectionalOffset;
    [Tooltip("Smoothing speed for directional offset changes.")]
    [SerializeField] private float _directionalSmoothing = DefaultDirectionalSmoothing;

    // ==================== Moving Settings ====================
    [Header("Moving Settings")]
    [Tooltip("Speed for moving camera mode (chase, scrolling, etc.).")]
    [SerializeField] private float _movingSpeed = DefaultMovingSpeed;
    [Tooltip("Target position for moving camera mode.")]
    [SerializeField] private Vector3 _movingTarget = Vector3.zero;

    // ==================== Transition Settings ====================
    [Header("Transition Settings")]
    [Tooltip("Duration for mode transitions (when switching between Static/Follow/Moving).")]
    [SerializeField] private float _modeTransitionDuration = DefaultModeTransitionDuration;

    // ==================== Private Fields ====================
    private Vector3 _targetPosition;
    private Vector2 _velocity = Vector2.zero;
    
    // Moving mode fields
    private bool _playerInMiddle = false;
    private bool _playerIsMoving = false;
    private float _playerLastXPosition;

    // Offset fields
    private float _currentDirectionalOffset = 0f;
    private float _targetDirectionalOffset = 0f;
    private float _currentFollowXOffset = 0f;
    private float _currentFollowYOffset = 0f;
    private float _targetFollowXOffset = 0f;
    private float _targetFollowYOffset = 0f;
    private bool _isOffsetTransitioning = false;
    
    // Static position fields
    private float _staticXPosition = 0f;
    private float _staticYPosition = 0f;
    
    // Transition fields
    private float _xTransitionTimer = 0f;
    private float _yTransitionTimer = 0f;
    private bool _isInXTransition = false;
    private bool _isInYTransition = false;

    // ==================== Unity Lifecycle ====================
    private void Start()
    {
        // Initialize first room
        Room firstRoom = _player.parent.GetComponent<Room>();
        firstRoom.EnterRoom();

        // Initialize positions and offsets
        _targetPosition = transform.position;
        _currentFollowXOffset = _followXOffset;
        _currentFollowYOffset = _followYOffset;
        _targetFollowXOffset = _followXOffset;
        _targetFollowYOffset = _followYOffset;
    }

    private void Update()
    {
        UpdateDirectionalOffset();
        UpdateOffsetTransitions();
        UpdateModeTransitionTimer();
        
        // Handle Moving Mode directly (bypasses target position system)
        if (_xMode == CameraMode.Moving)
        {
            HandleMovingModeX();
        }
        
        UpdateTargetPosition();
        UpdateCameraPosition();
    }

    // ==================== Core Movement Logic ====================
    /// <summary>
    /// Updates the target position based on current camera modes.
    /// </summary>
    private void UpdateTargetPosition()
    {
        float targetX = CalculateTargetX();
        float targetY = CalculateTargetY();
        _targetPosition = new Vector3(targetX, targetY, transform.position.z);
    }

    /// <summary>
    /// Calculates target X position based on current X mode.
    /// </summary>
    private float CalculateTargetX()
    {
        return _xMode switch
        {
            CameraMode.Static => _staticXPosition,
            CameraMode.Follow => _player.position.x + _currentFollowXOffset,
            CameraMode.Moving => transform.position.x, // Handled directly in Update
            _ => transform.position.x
        };
    }

    /// <summary>
    /// Calculates target Y position based on current Y mode.
    /// </summary>
    private float CalculateTargetY()
    {
        return _yMode switch
        {
            CameraMode.Static => _staticYPosition,
            CameraMode.Follow => _player.position.y + _currentFollowYOffset,
            CameraMode.Moving => _movingTarget.y,
            _ => transform.position.y
        };
    }

    /// <summary>
    /// Handles Moving Mode (chase logic) - bypasses target position system.
    /// </summary>
    private void HandleMovingModeX()
    {
        float cameraX = transform.position.x;
        float playerX = _player.position.x;
        bool isPlayerMoving = Mathf.Abs(playerX - _playerLastXPosition) > PlayerMoveThreshold;
        _playerLastXPosition = playerX;

        Camera cam = GetComponent<Camera>();
        float cameraHalfWidth = cam.orthographicSize * cam.aspect;
        float cameraLeftEdge = cameraX - cameraHalfWidth;
        float cameraMiddle = cameraX;

        // Check for player falling off screen
        if (playerX < cameraLeftEdge)
        {
            _playerHealth.TakeDamage(PlayerFallDamage);
        }

        // Update chase logic
        _playerInMiddle = Mathf.Abs(playerX - cameraMiddle) < cameraHalfWidth * CameraMiddleZonePercent;

        if (_playerInMiddle && isPlayerMoving && playerX > cameraMiddle)
        {
            _playerIsMoving = true;
            transform.position = new Vector3(playerX, transform.position.y, transform.position.z);
        }
        else if (_playerIsMoving && (!isPlayerMoving || playerX <= cameraMiddle))
        {
            _playerIsMoving = false;
        }

        if (!_playerIsMoving)
        {
            float newX = cameraX + _movingSpeed * Time.deltaTime;
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }
    }

    // ==================== Offset Systems ====================
    /// <summary>
    /// Updates directional offset based on player's facing direction.
    /// </summary>
    private void UpdateDirectionalOffset()
    {
        if (_xMode != CameraMode.Follow) return;

        float playerScaleX = _player.localScale.x;
        _targetDirectionalOffset = playerScaleX > 0 ? _directionalOffset : -_directionalOffset;
        _currentDirectionalOffset = Mathf.Lerp(_currentDirectionalOffset, _targetDirectionalOffset, Time.deltaTime * _directionalSmoothing);
    }

    /// <summary>
    /// Updates offset transitions for smooth offset changes.
    /// </summary>
    private void UpdateOffsetTransitions()
    {
        if (!_isOffsetTransitioning) return;

        _currentFollowXOffset = Mathf.Lerp(_currentFollowXOffset, _targetFollowXOffset, Time.deltaTime * _followSmoothing);
        _currentFollowYOffset = Mathf.Lerp(_currentFollowYOffset, _targetFollowYOffset, Time.deltaTime * _followSmoothing);
        
        // Check if transitions are complete
        if (Mathf.Abs(_currentFollowXOffset - _targetFollowXOffset) < 0.01f && 
            Mathf.Abs(_currentFollowYOffset - _targetFollowYOffset) < 0.01f)
        {
            _currentFollowXOffset = _targetFollowXOffset;
            _currentFollowYOffset = _targetFollowYOffset;
            _isOffsetTransitioning = false;
        }
    }

    // ==================== Transition System ====================
    /// <summary>
    /// Updates mode transition timers for brief smoothing after mode changes.
    /// </summary>
    private void UpdateModeTransitionTimer()
    {
        UpdateAxisTransitionTimer(true);  // X-axis
        UpdateAxisTransitionTimer(false); // Y-axis
    }

    /// <summary>
    /// Updates transition timer for a specific axis.
    /// </summary>
    /// <param name="isXAxis">True for X-axis, false for Y-axis.</param>
    private void UpdateAxisTransitionTimer(bool isXAxis)
    {
        bool isTransitioning = isXAxis ? _isInXTransition : _isInYTransition;
        float timer = isXAxis ? _xTransitionTimer : _yTransitionTimer;

        if (isTransitioning)
        {
            timer += Time.deltaTime;
            if (timer >= _modeTransitionDuration)
            {
                if (isXAxis)
                {
                    _isInXTransition = false;
                    _xTransitionTimer = 0f;
                }
                else
                {
                    _isInYTransition = false;
                    _yTransitionTimer = 0f;
                }
            }
            else
            {
                if (isXAxis)
                    _xTransitionTimer = timer;
                else
                    _yTransitionTimer = timer;
            }
        }
    }

    /// <summary>
    /// Updates the camera's actual position with appropriate smoothing.
    /// </summary>
    private void UpdateCameraPosition()
    {
        // Apply directional offset and calculate final target
        float finalTargetX = _targetPosition.x + _currentDirectionalOffset;
        Vector3 targetPosition3D = new Vector3(finalTargetX, _targetPosition.y, transform.position.z);
        
        // Handle X and Y axes independently
        float currentX = transform.position.x;
        float currentY = transform.position.y;
        
        // X-axis movement
        if (_xMode == CameraMode.Moving)
        {
            // X-axis handled directly in HandleMovingModeX(), keep current X
            currentX = transform.position.x;
        }
        else if (_isInXTransition)
        {
            // Smooth X transition
            float smoothing = 1f / _modeTransitionDuration;
            currentX = Mathf.SmoothDamp(currentX, targetPosition3D.x, ref _velocity.x, 1f / smoothing);
        }
        else if (_xMode == CameraMode.Follow)
        {
            // Instant X following
            currentX = targetPosition3D.x;
        }
        else
        {
            // Smooth X movement for other modes
            float smoothing = _followSmoothing;
            currentX = Mathf.SmoothDamp(currentX, targetPosition3D.x, ref _velocity.x, 1f / smoothing);
        }
        
        // Y-axis movement
        if (_yMode == CameraMode.Moving)
        {
            // Y-axis moving mode (if implemented)
            currentY = _movingTarget.y;
        }
        else if (_isInYTransition)
        {
            // Smooth Y transition
            float smoothing = 1f / _modeTransitionDuration;
            currentY = Mathf.SmoothDamp(currentY, targetPosition3D.y, ref _velocity.y, 1f / smoothing);
        }
        else if (_yMode == CameraMode.Follow)
        {
            // Instant Y following
            currentY = targetPosition3D.y;
        }
        else
        {
            // Smooth Y movement for other modes
            float smoothing = _followSmoothing;
            currentY = Mathf.SmoothDamp(currentY, targetPosition3D.y, ref _velocity.y, 1f / smoothing);
        }
        
        // Apply final position
        transform.position = new Vector3(currentX, currentY, transform.position.z);
    }



    // ==================== Public API ====================
    /// <summary>
    /// Sets camera mode for X axis with optional transition.
    /// </summary>
    public void SetXMode(CameraMode mode, bool transition = true)
    {
        SetAxisMode(true, mode, transition);
    }

    /// <summary>
    /// Sets camera mode for Y axis with optional transition.
    /// </summary>
    public void SetYMode(CameraMode mode, bool transition = true)
    {
        SetAxisMode(false, mode, transition);
    }

    /// <summary>
    /// Sets camera mode for a specific axis with optional transition.
    /// </summary>
    /// <param name="isXAxis">True for X-axis, false for Y-axis.</param>
    /// <param name="mode">The new camera mode.</param>
    /// <param name="transition">Whether to transition smoothly.</param>
    private void SetAxisMode(bool isXAxis, CameraMode mode, bool transition = true)
    {
        CameraMode currentMode = isXAxis ? _xMode : _yMode;
        if (currentMode == mode) return;

        if (isXAxis)
        {
            _xMode = mode;
        }
        else
        {
            _yMode = mode;
        }

        if (transition)
        {
            StartAxisTransitionSmoothing(isXAxis);
        }
        else
        {
            if (isXAxis)
                _velocity.x = 0f;
            else
                _velocity.y = 0f;
        }

        // Reset moving mode state if switching away from X Moving mode
        if (isXAxis && mode != CameraMode.Moving)
        {
            _playerIsMoving = false;
            _playerInMiddle = false;
        }
    }

    /// <summary>
    /// Sets both X and Y modes simultaneously.
    /// </summary>
    public void SetModes(CameraMode xMode, CameraMode yMode, bool transition = true)
    {
        SetAxisMode(true, xMode, transition);
        SetAxisMode(false, yMode, transition);
    }

    /// <summary>
    /// Sets follow offset for specified axis with optional transition.
    /// </summary>
    public void SetFollowOffset(char axis, float offset, bool transition = true)
    {
        if (axis == 'X' || axis == 'x')
        {
            _followXOffset = offset;
            if (transition)
            {
                _targetFollowXOffset = offset;
                _isOffsetTransitioning = true;
            }
            else
            {
                _currentFollowXOffset = offset;
                _targetFollowXOffset = offset;
            }
        }
        else if (axis == 'Y' || axis == 'y')
        {
            _followYOffset = offset;
            if (transition)
            {
                _targetFollowYOffset = offset;
                _isOffsetTransitioning = true;
            }
            else
            {
                _currentFollowYOffset = offset;
                _targetFollowYOffset = offset;
            }
        }
    }

    /// <summary>
    /// Sets directional offset amount.
    /// </summary>
    public void SetDirectionalOffset(float offset) => _directionalOffset = offset;

    /// <summary>
    /// Sets directional offset smoothing speed.
    /// </summary>
    public void SetDirectionalSmoothing(float smoothing) => _directionalSmoothing = smoothing;

    /// <summary>
    /// Sets moving target position.
    /// </summary>
    public void SetMovingTarget(Vector3 target) => _movingTarget = target;

    /// <summary>
    /// Sets moving speed.
    /// </summary>
    public void SetMovingSpeed(float speed) => _movingSpeed = speed;

    /// <summary>
    /// Sets mode transition duration.
    /// </summary>
    public void SetModeTransitionDuration(float duration) => _modeTransitionDuration = duration;

    /// <summary>
    /// Sets static X position for Static mode.
    /// </summary>
    public void SetStaticXPosition(float xPosition) => SetStaticPosition(true, xPosition);

    /// <summary>
    /// Sets static Y position for Static mode.
    /// </summary>
    public void SetStaticYPosition(float yPosition) => SetStaticPosition(false, yPosition);

    /// <summary>
    /// Sets both static X and Y positions for Static mode.
    /// </summary>
    public void SetStaticPosition(float xPosition, float yPosition)
    {
        SetStaticPosition(true, xPosition);
        SetStaticPosition(false, yPosition);
    }

    /// <summary>
    /// Sets static position for a specific axis.
    /// </summary>
    /// <param name="isXAxis">True for X-axis, false for Y-axis.</param>
    /// <param name="position">The static position value.</param>
    private void SetStaticPosition(bool isXAxis, float position)
    {
        if (isXAxis)
            _staticXPosition = position;
        else
            _staticYPosition = position;
    }

    // ==================== Private Helpers ====================
    /// <summary>
    /// Starts brief smoothing period after mode change for a specific axis.
    /// </summary>
    /// <param name="isXAxis">True for X-axis, false for Y-axis.</param>
    private void StartAxisTransitionSmoothing(bool isXAxis)
    {
        if (isXAxis)
        {
            _isInXTransition = true;
            _xTransitionTimer = 0f;
        }
        else
        {
            _isInYTransition = true;
            _yTransitionTimer = 0f;
        }
    }
}
