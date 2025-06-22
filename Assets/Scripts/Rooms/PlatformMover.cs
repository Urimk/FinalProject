using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveDistance = 3f;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private bool _startFromPositive = false;
    [SerializeField] private bool _isVertical = true;
    [SerializeField] private bool _isNegative = false;

    [Header("Attachment Settings")]
    [SerializeField] private string _playerTag = "Player";

    [Tooltip("Distance threshold for attaching when player is on ground and close enough.")]
    [SerializeField] private float _attachDistance = 2f;
    [Tooltip("LayerMask to filter ground/platform checks (should include this platform's layer).")]

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private bool _movingPositive;
    private GameObject _player;
    private Transform _playerTransform;
    private Transform _currentPlatformParent;

    private Rigidbody2D _rigidbody2D;

    private void Start()
    {
        // Movement setup
        _startPos = transform.position;
        Vector3 dir = _isVertical ? Vector3.up : Vector3.right;

        // Apply negative direction if _isNegative is true
        if (_isNegative)
            dir = -dir;

        _targetPos = _startFromPositive ? _startPos - dir * _moveDistance : _startPos + dir * _moveDistance;
        _movingPositive = !_startFromPositive;

        // Rigidbody setup for trigger/collision events
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;

        // Find the player and its feet check
        _player = GameObject.FindGameObjectWithTag(_playerTag);
        if (_player != null)
        {
            _playerTransform = _player.transform;
        }
        else
        {
            Debug.LogWarning("Player with tag '" + _playerTag + "' not found in scene.");
        }
    }

    private void Update()
    {
        // Platform movement
        float step = _moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, step);

        if (Vector3.Distance(transform.position, _targetPos) < 0.01f)
        {
            _movingPositive = !_movingPositive;
            Vector3 dir = _isVertical ? Vector3.up : Vector3.right;

            // Apply negative direction if _isNegative is true
            if (_isNegative)
                dir = -dir;

            _targetPos = _movingPositive ? _startPos + dir * _moveDistance : _startPos - dir * _moveDistance;
        }

        // Attachment logic
        HandleAttachment();
    }

    private void HandleAttachment()
    {
        if (_playerTransform == null)
            return;

        // Calculate distance between player and platform centers (or choose axis-specific)
        float distanceToPlayer = Vector3.Distance(_playerTransform.position, transform.position);

        // Check if close enough and player is on ground
        var pm = _player.GetComponent<PlayerMovement>();
        if (distanceToPlayer <= _attachDistance && _playerTransform.position.y > (transform.position.y + 1.2f) && pm != null && pm.IsGrounded())
        {
            // Create or find an intermediate parent with neutral scale
            Transform intermediateParent = transform.Find("PlayerAttachPoint");
            if (intermediateParent == null)
            {
                GameObject attachPoint = new GameObject("PlayerAttachPoint");
                attachPoint.transform.SetParent(transform);
                attachPoint.transform.localPosition = Vector3.zero;
                attachPoint.transform.localRotation = Quaternion.identity;
                attachPoint.transform.localScale = Vector3.one;
                intermediateParent = attachPoint.transform;
            }

            _playerTransform.SetParent(intermediateParent, true);
            _currentPlatformParent = transform;
        }
        else if (_currentPlatformParent == transform && pm != null && !pm.IsGrounded())
        {
            // No longer close or on ground, detach
            _playerTransform.SetParent(null);
            _currentPlatformParent = null;
        }
    }
}
