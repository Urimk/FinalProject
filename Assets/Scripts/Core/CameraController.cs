using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Room Camera
    [SerializeField] private float _speed;
    private float _currentPosY; // Used as target Y for room camera or initial Y
    private Vector3 _velocity = Vector3.zero; // Used by SmoothDamp for Y-axis

    // Follow player X
    [SerializeField] private Transform _player;
    [SerializeField] private Health _playerHealth;
    [SerializeField] private float _aheadDistance;
    [SerializeField] private float _cameraSpeed; // Speed for lookAhead Lerp
    private float _lookAhead;

    // Freeze camera X
    private bool _isXFrozen = false;
    private float _frozenX = 0f;


    // Chase mode
    [Header("Chase Mode Settings")]
    [SerializeField] private bool _isChase = false;
    [SerializeField] private float _chaseSpeed = 5f; // Speed at which camera moves right in chase mode
    private bool _playerInMiddle = false;
    private bool _playerWasMoving = false;
    private float _playerLastXPosition;

    // Player Y Follow Settings
    [Header("Player Y Follow Settings")]
    [SerializeField] private bool _followPlayerY = false;
    [SerializeField] private float _playerYOffset = 2f; // Vertical offset when following player's Y
    private bool _snapYNextFrame = true;

    [Header("Y Offset Transition")]
    [SerializeField] private float _offsetTransitionDuration = 0.5f;

    private bool _isTransitioningYOffset = false;
    private float _transitionStartYOffset;
    private float _transitionTargetYOffset;
    private float _transitionTimer = 0f;

    private void Start()
    {
        if (_followPlayerY)
        {
            // If starting with Y follow, set the camera's Y position directly to the target.
            // SmoothDamp in Update will then keep it following smoothly.
            float initialTargetY = _player.position.y + _playerYOffset;
            transform.position = new Vector3(transform.position.x, initialTargetY, transform.position.z);
            _currentPosY = initialTargetY; // Initialize currentPosY in case mode is toggled
        }
        else
        {
            // Original behavior for room camera:
            // Set initial target Y based on player's start position, with a fixed offset.
            // The camera will SmoothDamp to this Y.
            _currentPosY = _player.position.y + 2f; // Using the 2f offset from your original Start()
        }

        // Initialize chase mode variables
        _playerLastXPosition = _player.position.x;
    }

    private void Update()
    {
        HandleYMovement();
        HandleXMovement();

        // Update lookAhead for normal follow mode
        if (!_isChase && !_isXFrozen)
        {
            _lookAhead = Mathf.Lerp(_lookAhead, (_aheadDistance * _player.localScale.x), Time.deltaTime * _cameraSpeed);
        }
    }

    private void HandleYMovement()
    {
        float yTargetForSmoothDamp;

        if (_followPlayerY)
        {
            if (_isTransitioningYOffset)
            {
                _transitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_transitionTimer / _offsetTransitionDuration);
                // Lerp the offset
                _playerYOffset = Mathf.Lerp(_transitionStartYOffset, _transitionTargetYOffset, t);

                if (t >= 1f)
                {
                    // Done transitioning
                    _isTransitioningYOffset = false;
                }
            }

            yTargetForSmoothDamp = _player.position.y + _playerYOffset;

            if (_snapYNextFrame)
            {
                // Instantly snap camera Y position and clear velocity
                transform.position = new Vector3(transform.position.x, yTargetForSmoothDamp, transform.position.z);
                _velocity = Vector3.zero;
                //_snapYNextFrame = false; // Only snap once
            }
            else
            {
                // Smooth follow Y
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    new Vector3(transform.position.x, yTargetForSmoothDamp, transform.position.z),
                    ref _velocity,
                    _speed
                );
            }
        }
        else
        {
            // Room camera mode
            yTargetForSmoothDamp = _currentPosY;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                new Vector3(transform.position.x, yTargetForSmoothDamp, transform.position.z),
                ref _velocity,
                _speed
            );
        }
    }

    private void HandleXMovement()
    {
        if (_isChase)
        {
            HandleChaseMode();
        }
        else if (!_isXFrozen)
        {
            // Normal follow mode
            transform.position = new Vector3(_player.position.x + _lookAhead, transform.position.y, transform.position.z);
        }
        else
        {
            // Frozen mode
            transform.position = new Vector3(_frozenX, transform.position.y, transform.position.z);
        }
    }

    private void HandleChaseMode()
    {
        float cameraCurrentX = transform.position.x;
        float playerCurrentX = _player.position.x;

        // Check if player is moving
        bool isPlayerMoving = Mathf.Abs(playerCurrentX - _playerLastXPosition) > 0.01f;
        _playerLastXPosition = playerCurrentX;

        // Get camera bounds (assuming camera width, you might need to adjust this based on your camera setup)
        Camera cam = GetComponent<Camera>();
        float cameraHalfWidth = cam.orthographicSize * cam.aspect;
        float cameraLeftEdge = cameraCurrentX - cameraHalfWidth;
        float cameraMiddle = cameraCurrentX;

        // Check if player fell out of camera on the left side
        if (playerCurrentX < cameraLeftEdge)
        {
            // Player fell out, give damage
            _playerHealth.TakeDamage(100);
        }

        // Check if player is in the middle of the camera
        _playerInMiddle = Mathf.Abs(playerCurrentX - cameraMiddle) < cameraHalfWidth * 0.1f; // 10% of camera width as "middle" zone

        if (_playerInMiddle && isPlayerMoving && playerCurrentX > cameraMiddle)
        {
            // Player is in middle, moving, and moving right - camera follows player
            _playerWasMoving = true;
            transform.position = new Vector3(playerCurrentX, transform.position.y, transform.position.z);
        }
        else if (_playerWasMoving && (!isPlayerMoving || playerCurrentX <= cameraMiddle))
        {
            // Player stopped moving or moved back - return to normal chase speed
            _playerWasMoving = false;
        }

        if (!_playerWasMoving)
        {
            // Normal chase mode - move right at constant speed
            transform.position = new Vector3(cameraCurrentX + _chaseSpeed * Time.deltaTime, transform.position.y, transform.position.z);
        }
    }

    public void MoveToNewRoom(Transform newRoom)
    {
        // This sets the target Y for the room camera mode.
        // If followPlayerY is true, this value will be stored but not immediately used
        // for camera positioning until followPlayerY is set to false.
        _currentPosY = newRoom.position.y + 2.38f;
    }

    // Method to freeze or unfreeze the X position of the camera
    public void SetCameraXFreeze(bool freeze, float xPosition = 0f)
    {
        _isXFrozen = freeze;
        if (freeze)
        {
            _frozenX = xPosition; // Set the X position to freeze the camera at
        }
    }

    public void SetCameraYFreeze(float yPosition = 0f)
    {
        _followPlayerY = false;
        SetYOffset(yPosition, false);
    }

    // Method to enable/disable chase mode
    public void SetChaseMode(bool chase)
    {
        _isChase = chase;
        if (chase)
        {
            // Reset chase mode variables
            _playerWasMoving = false;
            _playerInMiddle = false;
            _playerLastXPosition = _player.position.x;
        }
    }

    // Optional: Method to toggle Y follow mode and set offset
    public void SetFollowPlayerY(float yOffset = -1f)
    {
        _followPlayerY = true;
        if (yOffset != -1f)
        {
            SetYOffset(yOffset, false);

        }
        else
        {
            _snapYNextFrame = true; // Trigger snapping in next Update
        }

    }

    public void SetYOffset(float newOffset, bool instantSnap = false)
    {
        if (instantSnap)
        {
            _playerYOffset = newOffset;
            _snapYNextFrame = true;
            _isTransitioningYOffset = false;
        }
        else
        {
            _isTransitioningYOffset = true;
            _transitionStartYOffset = _playerYOffset;
            _transitionTargetYOffset = newOffset;
            _transitionTimer = 0f;
        }
    }

    public float GetPlayerYOffset()
    {
        return _playerYOffset;
    }

    // Getter for chase mode state
    public bool IsChaseMode()
    {
        return _isChase;
    }

    // Method to set chase speed
    public void SetChaseSpeed(float newChaseSpeed)
    {
        _chaseSpeed = newChaseSpeed;
    }

    public void SetChaseStart(float offSet)
    {
        _currentPosY = offSet;
    }
}
