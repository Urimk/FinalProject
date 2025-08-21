using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles one-way platform logic, including fall-through for player and AI.
/// </summary>
public class OneWayPlatform : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultWaitTime = 0.4f;
    private const float PlatformTopYOffset = 1.1f;
    private const float EffectorFallThroughRotation = 180f;
    private const float EffectorNormalRotation = 0f;
    private const string PlayerTag = "Player";
    private const string GroundLayerName = "Ground";
    private const KeyCode FallThroughKey = KeyCode.S;

    // ==================== Inspector Fields ====================
    [Tooltip("True if this platform is controlled by AI.")]
    [FormerlySerializedAs("isAIControlled")]
    [SerializeField] private bool _isAIControlled;
    [Tooltip("True if the platform can be fallen through.")]
    [FormerlySerializedAs("isFallable")]
    [SerializeField] private bool _isFallable = true;
    [Tooltip("Wait time before resetting the platform.")]
    [FormerlySerializedAs("waitTime")]
    [SerializeField] private float _waitTime = DefaultWaitTime;
    [Tooltip("Reference to the player Transform.")]
    [FormerlySerializedAs("player")]
    [SerializeField] private Transform _player;

    // ==================== Private Fields ====================
    private PlatformEffector2D _effector;
    private EdgeCollider2D _edgeCollider;
    private PlayerMovement _playerMovement;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Start callback. Initializes platform and player references.
    /// </summary>
    private void Start()
    {
        _effector = GetComponent<PlatformEffector2D>();
        _edgeCollider = GetComponent<EdgeCollider2D>();
        _playerMovement = _player.GetComponent<PlayerMovement>();
    }

    /// <summary>
    /// Unity Update callback. Handles fall-through input and collider state.
    /// </summary>
    private void Update()
    {
        if (!_isAIControlled)
        {
            if (Input.GetKeyDown(FallThroughKey) && _isFallable)
            {
                FallThroughPlatform();
            }
        }
        CheckPlayerPosition();
    }

    // ==================== AI and Platform Logic ====================
    /// <summary>
    /// Allows AI to trigger fall-through.
    /// </summary>
    /// <param name="shouldFall">True if the AI should fall through.</param>
    public void SetAIFallThrough(bool shouldFall)
    {
        if (_isAIControlled && shouldFall && _isFallable)
        {
            FallThroughPlatform();
        }
    }

    /// <summary>
    /// Makes the player fall through the platform.
    /// </summary>
    private void FallThroughPlatform()
    {
        _effector.rotationalOffset = EffectorFallThroughRotation;
        _playerMovement.ResetCoyoteCounter();
        _playerMovement.GroundLayer = 0;
        Invoke(nameof(ResetPlatform), _waitTime);
    }

    /// <summary>
    /// Resets the platform to normal collision.
    /// </summary>
    private void ResetPlatform()
    {
        _effector.rotationalOffset = EffectorNormalRotation;
        _playerMovement.GroundLayer = LayerMask.GetMask(GroundLayerName);
    }

    /// <summary>
    /// Enables or disables the collider based on player position.
    /// </summary>
    private void CheckPlayerPosition()
    {
        if (_player != null)
        {
            float platformTop = _edgeCollider.bounds.max.y + PlatformTopYOffset;
            float playerY = _player.position.y;
            _edgeCollider.enabled = (playerY > platformTop);
        }
    }
    
    /// <summary>
    /// Checks if the player is currently on this platform.
    /// </summary>
    /// <returns>True if the player is on this platform.</returns>
    public bool IsPlayerOnPlatform()
    {
        if (_player == null || _edgeCollider == null) return false;
        
        // Check if player is within the platform's horizontal bounds
        float platformLeft = _edgeCollider.bounds.min.x;
        float platformRight = _edgeCollider.bounds.max.x;
        float playerX = _player.position.x;
        
        bool withinHorizontalBounds = playerX >= platformLeft && playerX <= platformRight;
        
        // Check if player is at the platform's top level
        float platformTop = _edgeCollider.bounds.max.y + PlatformTopYOffset;
        float playerY = _player.position.y;
        float tolerance = 0.1f; // Small tolerance for floating point precision
        
        bool atPlatformLevel = Mathf.Abs(playerY - platformTop) <= tolerance;
        
        return withinHorizontalBounds && atPlatformLevel;
    }
}
