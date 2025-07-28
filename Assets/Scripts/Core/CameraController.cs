using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Controls the camera's movement and behavior, including following the player, chase mode, and room transitions.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultYOffset = 2f;
    private const float RoomYOffset = 2.38f;
    private const float PlayerMoveThreshold = 0.01f;
    private const float CameraMiddleZonePercent = 0.1f;
    private const int PlayerFallDamage = 100;

    // ==================== Dependencies ====================
    [Header("Dependencies")]
    [Tooltip("Reference to the player Transform.")]
    [FormerlySerializedAs("player")]
    [SerializeField] private Transform _player;
    [Tooltip("Reference to the player's Health component.")]
    [FormerlySerializedAs("playerHealth")]

    [SerializeField] private Health _playerHealth;

    // ==================== General Camera Movement ====================
    [Header("General Movement")]
    [Tooltip("Camera smoothing speed for Y movement.")]
    [FormerlySerializedAs("speed")]
    [SerializeField] private float _speed;
    private float _currentPosY;
    private Vector3 _velocity = Vector3.zero;

    // ==================== Player X Follow ====================
    [Header("X Follow Settings")]
    [Tooltip("Target X offset for the camera relative to the player.")]
    [FormerlySerializedAs("_aheadDistance")]
    [SerializeField] private float _targetXOffset;
    [Tooltip("Camera smoothing speed for X movement.")]
    [FormerlySerializedAs("cameraSpeed")]
    [SerializeField] private float _cameraSpeed;
    private float _currentXOffset;

    // ==================== X Freeze ====================
    [Header("X Freeze Settings")]
    private bool _isXFrozen = false;
    private float _frozenX = 0f;

    // ==================== Chase Mode ====================
    [Header("Chase Mode Settings")]
    [Tooltip("Enable chase mode for the camera.")]
    [FormerlySerializedAs("isChase")]
    [SerializeField] private bool _isChase = false;
    [Tooltip("Speed at which the camera chases the player.")]
    [FormerlySerializedAs("chaseSpeed")]
    [SerializeField] private float _chaseSpeed = 5f;
    private bool _playerInMiddle = false;
    private bool _playerIsMoving = false;
    private float _playerLastXPosition;

    // ==================== Y Follow ====================
    [Header("Y Follow Settings")]
    [Tooltip("Should the camera follow the player's Y position?")]
    [FormerlySerializedAs("followPlayerY")]
    [SerializeField] private bool _followPlayerY = false;
    [Tooltip("Y offset for the camera relative to the player.")]
    [FormerlySerializedAs("playerYOffset")]
    [SerializeField] private float _playerYOffset = DefaultYOffset;
    private bool _snapYNextFrame = true;

    // ==================== Y Offset Transition ====================
    [Header("Y Offset Transition")]
    [Tooltip("Duration for Y offset transitions.")]
    [FormerlySerializedAs("offsetTransitionDuration")]
    [SerializeField] private float _offsetTransitionDuration = 0.5f;
    private bool _isTransitioningYOffset = false;
    private float _transitionStartYOffset;
    private float _transitionTargetYOffset;
    private float _transitionTimer = 0f;

    /// <summary>
    /// Unity Start callback. Initializes camera position and state.
    /// </summary>
    private void Start()
    {
        if (_followPlayerY)
        {
            float initialTargetY = _player.position.y + _playerYOffset;
            transform.position = new Vector3(transform.position.x, initialTargetY, transform.position.z);
            _currentPosY = initialTargetY;
        }
        else
        {
            _currentPosY = _player.position.y + DefaultYOffset;
        }
        _playerLastXPosition = _player.position.x;
    }

    /// <summary>
    /// Unity Update callback. Handles camera movement each frame.
    /// </summary>
    private void Update()
    {
        HandleYMovement();
        HandleXMovement();
        if (!_isChase && !_isXFrozen)
        {
            _currentXOffset = Mathf.Lerp(_currentXOffset, _targetXOffset * _player.localScale.x, Time.deltaTime * _cameraSpeed);
        }
    }

    // ==================== Y Movement ====================
    /// <summary>
    /// Handles vertical (Y axis) camera movement, including smooth following and Y offset transitions.
    /// </summary>
    private void HandleYMovement()
    {
        // 1) Always advance any ongoing Y‑offset transition
        if (_isTransitioningYOffset)
        {
            _transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_transitionTimer / _offsetTransitionDuration);
            _playerYOffset = Mathf.Lerp(_transitionStartYOffset, _transitionTargetYOffset, t);
            if (t >= 1f)
                _isTransitioningYOffset = false;
        }

        // 2) Decide your target Y
        float yTarget = _followPlayerY
            ? _player.position.y + _playerYOffset
            : _currentPosY;

        // 3) Snap if requested
        if (_snapYNextFrame)
        {
            transform.position = new Vector3(transform.position.x, yTarget, transform.position.z);
            _velocity = Vector3.zero;
            _snapYNextFrame = false;
        }
        else
        {
            // 4) SmoothDamp toward that target
            transform.position = Vector3.SmoothDamp(
                transform.position,
                new Vector3(transform.position.x, yTarget, transform.position.z),
                ref _velocity,
                _speed
            );
        }
    }


    // ==================== X Movement ====================
    /// <summary>
    /// Handles horizontal (X axis) camera movement, including chase and freeze logic.
    /// </summary>
    private void HandleXMovement()
    {
        if (_isChase)
        {
            HandleChaseMode();
        }
        else if (!_isXFrozen)
        {
            transform.position = new Vector3(_player.position.x + _currentXOffset, transform.position.y, transform.position.z);
        }
        else
        {
            transform.position = new Vector3(_frozenX, transform.position.y, transform.position.z);
        }
    }

    // ==================== Chase Mode Methods ====================
    /// <summary>
    /// Handles camera behavior in chase mode, including player tracking and fall damage.
    /// </summary>
    private void HandleChaseMode()
    {
        float cameraCurrentX = transform.position.x;
        float playerCurrentX = _player.position.x;
        bool isPlayerMoving = Mathf.Abs(playerCurrentX - _playerLastXPosition) > PlayerMoveThreshold;
        _playerLastXPosition = playerCurrentX;
        Camera cam = GetComponent<Camera>();
        float cameraHalfWidth = cam.orthographicSize * cam.aspect;
        float cameraLeftEdge = cameraCurrentX - cameraHalfWidth;
        float cameraMiddle = cameraCurrentX;
        if (playerCurrentX < cameraLeftEdge)
        {
            _playerHealth.TakeDamage(PlayerFallDamage);
        }
        _playerInMiddle = Mathf.Abs(playerCurrentX - cameraMiddle) < cameraHalfWidth * CameraMiddleZonePercent;
        if (_playerInMiddle && isPlayerMoving && playerCurrentX > cameraMiddle)
        {
            _playerIsMoving = true;
            transform.position = new Vector3(playerCurrentX, transform.position.y, transform.position.z);
        }
        else if (_playerIsMoving && (!isPlayerMoving || playerCurrentX <= cameraMiddle))
        {
            _playerIsMoving = false;
        }
        if (!_playerIsMoving)
        {
            transform.position = new Vector3(cameraCurrentX + _chaseSpeed * Time.deltaTime, transform.position.y, transform.position.z);
        }
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Moves the camera to a new room, updating the Y position offset.
    /// </summary>
    /// <param name="newRoom">The transform of the new room.</param>
    public void MoveToNewRoom(Transform newRoom)
    {
        _currentPosY = newRoom.position.y + RoomYOffset;
    }

    /// <summary>
    /// Freezes or unfreezes the camera's X position.
    /// </summary>
    /// <param name="freeze">Whether to freeze the X position.</param>
    /// <param name="xPosition">The X position to freeze at (if freezing).</param>
    public void SetCameraXFreeze(bool freeze, float xPosition = 0f)
    {
        _isXFrozen = freeze;
        if (freeze)
        {
            _frozenX = xPosition;
        }
    }

    /// <summary>
    /// Freezes the camera's Y position at a specific value.
    /// </summary>
    /// <param name="yPosition">The Y position to freeze at.</param>
    public void SetCameraYFreeze(float yPosition = 0f)
    {
        _followPlayerY = false;
        SetYOffset(yPosition, false);
    }

    /// <summary>
    /// Enables or disables chase mode for the camera.
    /// </summary>
    /// <param name="chase">Whether to enable chase mode.</param>
    public void SetChaseMode(bool chase)
    {
        _isChase = chase;
        if (chase)
        {
            _playerIsMoving = false;
            _playerInMiddle = false;
            _playerLastXPosition = _player.position.x;
        }
    }

    /// <summary>
    /// Enables following the player's Y position, with optional Y offset.
    /// </summary>
    /// <param name="yOffset">The Y offset to use (if not -1).</param>
    public void SetFollowPlayerY(float yOffset = -1f)
    {
        _followPlayerY = true;
        if (yOffset != -1f)
        {
            SetYOffset(yOffset, false);
        }
        else
        {
            _snapYNextFrame = true;
        }
    }

    /// <summary>
    /// Sets the camera's Y offset, with optional instant snap.
    /// </summary>
    /// <param name="newOffset">The new Y offset value.</param>
    /// <param name="instantSnap">Whether to snap instantly or transition smoothly.</param>
    public void SetYOffset(float newOffset, bool instantSnap = false)
    {
        if (instantSnap)
        {
            _playerYOffset = newOffset;
            _currentPosY = newOffset;
            _snapYNextFrame = true;
            _isTransitioningYOffset = false;
        }
        else
        {
            _isTransitioningYOffset = true;
            _transitionStartYOffset = _playerYOffset;
            _transitionTargetYOffset = newOffset;
            _currentPosY = newOffset;
            _transitionTimer = 0f;
        }
    }

    /// <summary>
    /// Gets the current Y offset of the camera relative to the player.
    /// </summary>
    /// <returns>The current Y offset value.</returns>
    public float GetPlayerYOffset()
    {
        return _playerYOffset;
    }

    /// <summary>
    /// Returns whether the camera is currently in chase mode.
    /// </summary>
    /// <returns>True if in chase mode, false otherwise.</returns>
    public bool IsChaseMode()
    {
        return _isChase;
    }

    /// <summary>
    /// Sets the speed at which the camera chases the player in chase mode.
    /// </summary>
    /// <param name="newChaseSpeed">The new chase speed value.</param>
    public void SetChaseSpeed(float newChaseSpeed)
    {
        _chaseSpeed = newChaseSpeed;
    }

    /// <summary>
    /// Sets the starting Y position for the camera in chase mode.
    /// </summary>
    /// <param name="offSet">The Y offset to start at.</param>
    public void SetChaseStart(float offSet)
    {
        _currentPosY = offSet;
    }
}
