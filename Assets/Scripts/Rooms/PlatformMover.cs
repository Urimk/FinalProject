using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Moves a platform back and forth, and handles player attachment/detachment.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlatformMover : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultMoveDistance = 3f;
    private const float DefaultMoveSpeed = 2f;
    private const float DefaultAttachDistance = 2f;
    private const float PlayerYOffsetThreshold = 1.2f;
    private const float PositionEpsilon = 0.01f;
    private const string PlayerTag = "Player";
    private const string AttachPointName = "PlayerAttachPoint";

    // ==================== Inspector Fields ====================
    [Header("Movement Settings")]
    [Tooltip("Distance the platform moves from its start position.")]
    [FormerlySerializedAs("moveDistance")]
    [SerializeField] private float _moveDistance = DefaultMoveDistance;
    [Tooltip("Speed at which the platform moves.")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] private float _moveSpeed = DefaultMoveSpeed;
    [Tooltip("If true, platform starts from the positive direction.")]
    [FormerlySerializedAs("startFromPositive")]
    [SerializeField] private bool _startFromPositive = false;
    [Tooltip("If true, platform moves vertically.")]
    [FormerlySerializedAs("isVertical")]
    [SerializeField] private bool _isVertical = true;
    [Tooltip("If true, platform moves in the negative direction.")]
    [FormerlySerializedAs("isNegative")]
    [SerializeField] private bool _isNegative = false;
    [Header("Attachment Settings")]
    [SerializeField] private Transform _playerTransform;
    [Tooltip("Distance threshold for attaching when player is on ground and close enough.")]
    [FormerlySerializedAs("attachDistance")]
    [SerializeField] private float _attachDistance = DefaultAttachDistance;

    // ==================== Private Fields ====================
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private bool _movingPositive;
    private GameObject _player;
    private Transform _currentPlatformParent;
    private Rigidbody2D _rigidbody2D;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Start callback. Initializes movement and player references.
    /// </summary>
    private void Start()
    {
        _startPos = transform.position;
        Vector3 dir = _isVertical ? Vector3.up : Vector3.right;
        if (_isNegative)
            dir = -dir;
        _targetPos = _startFromPositive ? _startPos - dir * _moveDistance : _startPos + dir * _moveDistance;
        _movingPositive = !_startFromPositive;
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _player = _playerTransform.gameObject;
    }

    /// <summary>
    /// Unity Update callback. Handles platform movement and player attachment.
    /// </summary>
    private void Update()
    {
        float step = _moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _targetPos, step);
        if (Vector3.Distance(transform.position, _targetPos) < PositionEpsilon)
        {
            _movingPositive = !_movingPositive;
            Vector3 dir = _isVertical ? Vector3.up : Vector3.right;
            if (_isNegative)
                dir = -dir;
            _targetPos = _movingPositive ? _startPos + dir * _moveDistance : _startPos - dir * _moveDistance;
        }
        HandleAttachment();
    }

    // ==================== Platform Logic ====================
    /// <summary>
    /// Handles attaching and detaching the player to the platform.
    /// </summary>
    private void HandleAttachment()
    {
        if (_playerTransform == null)
            return;
        float distanceToPlayer = Vector3.Distance(_playerTransform.position, transform.position);
        var pm = _player.GetComponent<PlayerMovement>();

        if (distanceToPlayer <= _attachDistance && _playerTransform.position.y > (transform.position.y + PlayerYOffsetThreshold) && pm != null && pm.IsGrounded())
        {
            Transform intermediateParent = transform.Find(AttachPointName);
            if (intermediateParent == null)
            {
                GameObject attachPoint = new GameObject(AttachPointName);
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
            _playerTransform.SetParent(null);
            _currentPlatformParent = null;
        }
    }
}
