using UnityEngine;

/// <summary>
/// Handles player attachment to moving platforms. Movement is handled by PatrolSystem.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultAttachDistance = 2f;
    private const float PlayerYOffsetThreshold = 1.2f;
    private const string AttachPointName = "PlayerAttachPoint";

    // ==================== Inspector Fields ====================
    [Header("Attachment Settings")]
    [Tooltip("Reference to the player Transform.")]
    [SerializeField] private Transform _playerTransform;
    [Tooltip("Distance threshold for attaching when player is on ground and close enough.")]
    [SerializeField] private float _attachDistance = DefaultAttachDistance;

    // ==================== Private Fields ====================
    private GameObject _player;
    private Transform _currentPlatformParent;

    // ==================== Unity Lifecycle ====================
    private void Start()
    {
        if (_playerTransform != null)
        {
            _player = _playerTransform.gameObject;
        }
    }

    private void Update()
    {
        HandleAttachment();
    }

    // ==================== Private Methods ====================
    /// <summary>
    /// Handles attaching and detaching the player to the platform.
    /// </summary>
    private void HandleAttachment()
    {
        if (_playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(_playerTransform.position, transform.position);
        var playerMovement = _player.GetComponent<PlayerMovement>();

        // Attach player if close enough, on ground, and above platform
        if (distanceToPlayer <= _attachDistance && 
            _playerTransform.position.y > (transform.position.y + PlayerYOffsetThreshold) && 
            playerMovement != null && 
            playerMovement.IsGrounded)
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
        // Detach player if no longer grounded
        else if (_currentPlatformParent == transform && playerMovement != null && !playerMovement.IsGrounded)
        {
            _playerTransform.SetParent(null);
            _currentPlatformParent = null;
        }
    }
}
