using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultYOffset = 2f;
    private const float RoomYOffset = 2.38f;
    private const float PlayerMoveThreshold = 0.01f;
    private const float CameraMiddleZonePercent = 0.1f;
    private const int PlayerFallDamage = 100;

    // ==================== Dependencies ====================
    [SerializeField] private Transform _player;
    [SerializeField] private Health _playerHealth;

    // ==================== General Camera Movement ====================
    [Header("General Movement")]
    [SerializeField] private float _speed;
    private float _currentPosY;
    private Vector3 _velocity = Vector3.zero;

    // ==================== Player X Follow ====================
    [Header("X Follow Settings")]
    [FormerlySerializedAs("_aheadDistance")]
    [SerializeField] private float _targetXOffset;
    [SerializeField] private float _cameraSpeed;
    private float _currentXOffset;

    // ==================== X Freeze ====================
    [Header("X Freeze Settings")]
    private bool _isXFrozen = false;
    private float _frozenX = 0f;

    // ==================== Chase Mode ====================
    [Header("Chase Mode Settings")]
    [SerializeField] private bool _isChase = false;
    [SerializeField] private float _chaseSpeed = 5f;
    private bool _playerInMiddle = false;
    private bool _playerIsMoving = false;
    private float _playerLastXPosition;

    // ==================== Y Follow ====================
    [Header("Y Follow Settings")]
    [SerializeField] private bool _followPlayerY = false;
    [SerializeField] private float _playerYOffset = DefaultYOffset;
    private bool _snapYNextFrame = true;

    // ==================== Y Offset Transition ====================
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
    private void HandleYMovement()
    {
        float yTargetForSmoothDamp;
        if (_followPlayerY)
        {
            if (_isTransitioningYOffset)
            {
                _transitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_transitionTimer / _offsetTransitionDuration);
                _playerYOffset = Mathf.Lerp(_transitionStartYOffset, _transitionTargetYOffset, t);
                if (t >= 1f)
                {
                    _isTransitioningYOffset = false;
                }
            }
            yTargetForSmoothDamp = _player.position.y + _playerYOffset;
            if (_snapYNextFrame)
            {
                transform.position = new Vector3(transform.position.x, yTargetForSmoothDamp, transform.position.z);
                _velocity = Vector3.zero;
            }
            else
            {
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
            yTargetForSmoothDamp = _currentPosY;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                new Vector3(transform.position.x, yTargetForSmoothDamp, transform.position.z),
                ref _velocity,
                _speed
            );
        }
    }

    // ==================== X Movement ====================
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
    public void MoveToNewRoom(Transform newRoom)
    {
        _currentPosY = newRoom.position.y + RoomYOffset;
    }

    public void SetCameraXFreeze(bool freeze, float xPosition = 0f)
    {
        _isXFrozen = freeze;
        if (freeze)
        {
            _frozenX = xPosition;
        }
    }

    public void SetCameraYFreeze(float yPosition = 0f)
    {
        _followPlayerY = false;
        SetYOffset(yPosition, false);
    }

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

    public bool IsChaseMode()
    {
        return _isChase;
    }

    public void SetChaseSpeed(float newChaseSpeed)
    {
        _chaseSpeed = newChaseSpeed;
    }

    public void SetChaseStart(float offSet)
    {
        _currentPosY = offSet;
    }
}
