using UnityEngine;
using System.Collections;

/// <summary>
/// Handles one-way platform logic using collider disable/enable instead of platform effector.
/// Supports multiple players, where each player controls a different edge collider.
/// </summary>
public class OneWayPlatform : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultFallThroughDuration = 0.3f;
    private const float PlatformTopYOffset = 1.1f;
    private const string PlayerTag = "Player";
    private const KeyCode FallThroughKey = KeyCode.S;

    // ==================== Inspector Fields ====================
    [Header("Platform Settings")]
    [Tooltip("Duration the collider stays disabled when falling through.")]
    [SerializeField] private float _fallThroughDuration = DefaultFallThroughDuration;
    [Tooltip("Reference to the first player Transform.")]
    [SerializeField] private Transform _player1;
    [Tooltip("Reference to the second player Transform (optional).")]
    [SerializeField] private Transform _player2;

    // ==================== Private Fields ====================
    private EdgeCollider2D[] _edgeColliders;
    private bool _isManuallyDisabled1 = false;
    private bool _isManuallyDisabled2 = false;

    // ==================== Unity Lifecycle ====================
    private void Start()
    {
        _edgeColliders = GetComponents<EdgeCollider2D>();
        if (_edgeColliders == null || _edgeColliders.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] OneWayPlatform requires at least one EdgeCollider2D component!");
        }
    }

    private void Update()
    {
        HandlePlayerInput();
        UpdateColliderStates();
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Allows AI to trigger fall-through for the first player.
    /// </summary>
    public void SetAIFallThrough()
    {
        StartCoroutine(DisableColliderTemporarily(1));
    }

    /// <summary>
    /// Allows AI to trigger fall-through for the second player.
    /// </summary>
    public void SetAIFallThrough2()
    {
        StartCoroutine(DisableColliderTemporarily(2));
    }

    /// <summary>
    /// Checks if a specific player is currently on this platform.
    /// </summary>
    /// <param name="playerTransform">The player transform to check.</param>
    /// <returns>True if the player is on this platform.</returns>
    public bool IsPlayerOnPlatform(Transform playerTransform)
    {
        Debug.Log("IsPlayerOnPlatform: " + playerTransform.name);
        if (playerTransform == null || _edgeColliders == null || _edgeColliders.Length == 0) return false;
        
        // Use the first collider for platform detection
        EdgeCollider2D collider = _edgeColliders[0];
        
        // Check if player is within the platform's horizontal bounds
        float platformLeft = collider.bounds.min.x;
        float platformRight = collider.bounds.max.x;
        float playerX = playerTransform.position.x;
        
        bool withinHorizontalBounds = playerX >= platformLeft && playerX <= platformRight;
        
        // Check if player is at the platform's top level
        float platformTop = collider.bounds.max.y + PlatformTopYOffset;
        float playerY = playerTransform.position.y;
        float tolerance = 0.5f;
        
        Debug.Log("Player Y: " + playerY + " Platform top: " + platformTop);
        bool atPlatformLevel = Mathf.Abs(playerY - platformTop) <= tolerance;
        
        Debug.Log("Within horizontal bounds: " + withinHorizontalBounds + " At platform level: " + atPlatformLevel);
        return withinHorizontalBounds && atPlatformLevel;
    }

    // ==================== Private Methods ====================
    private void HandlePlayerInput()
    {
        // Handle first player input
        if (_player1 != null && IsPlayerOnPlatform(_player1) && Input.GetKeyDown(FallThroughKey))
        {
            Debug.Log("Player 1 pressed S");
            StartCoroutine(DisableColliderTemporarily(1));
        }

        // Handle second player input (if exists)
        if (_player2 != null && IsPlayerOnPlatform(_player2) && Input.GetKeyDown(FallThroughKey))
        {
            Debug.Log("Player 2 pressed S");
            StartCoroutine(DisableColliderTemporarily(2));
        }
    }

    private void UpdateColliderStates()
    {
        if (_edgeColliders == null || _edgeColliders.Length == 0) return;

        // Update first collider (controlled by player 1)
        if (_edgeColliders.Length > 0)
        {
            bool shouldDisableCollider1 = _isManuallyDisabled1;
            
            if (_player1 != null)
            {
                float platformTop = _edgeColliders[0].bounds.max.y + PlatformTopYOffset;
                if (_player1.position.y < platformTop || _player1.GetComponent<Rigidbody2D>().velocity.y > 0)
                {
                    shouldDisableCollider1 = true;
                }
            }
            
            _edgeColliders[0].enabled = !shouldDisableCollider1;
        }

        // Update second collider (controlled by player 2, if exists)
        if (_edgeColliders.Length > 1)
        {
            bool shouldDisableCollider2 = _isManuallyDisabled2;
            
            if (_player2 != null)
            {
                float platformTop = _edgeColliders[1].bounds.max.y + PlatformTopYOffset;
                if (_player2.position.y < platformTop || _player2.GetComponent<Rigidbody2D>().velocity.y > 0)
                {
                    shouldDisableCollider2 = true;
                }
            }
            
            _edgeColliders[1].enabled = !shouldDisableCollider2;
        }
    }

    private IEnumerator DisableColliderTemporarily(int playerNumber)
    {
        if (playerNumber == 1)
        {
            _isManuallyDisabled1 = true;
            yield return new WaitForSeconds(_fallThroughDuration);
            _isManuallyDisabled1 = false;
        }
        else if (playerNumber == 2)
        {
            _isManuallyDisabled2 = true;
            yield return new WaitForSeconds(_fallThroughDuration);
            _isManuallyDisabled2 = false;
        }
    }
}

