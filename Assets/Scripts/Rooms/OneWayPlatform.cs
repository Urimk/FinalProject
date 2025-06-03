using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [SerializeField] private bool isAIControlled;
    [SerializeField] private bool isFallable = true;
    private PlatformEffector2D effector;
    private EdgeCollider2D edgeCollider;
    public float waitTime = 0.4f; // Delay before enabling collision again
    public Transform player; // Assign player GameObject in the Inspector
    private PlayerMovement playerMovement; // Reference to the PlayerMovement script


    private void Start()
    {
        effector = GetComponent<PlatformEffector2D>();
        edgeCollider = GetComponent<EdgeCollider2D>();
        playerMovement = player.GetComponent<PlayerMovement>();

    }

    private void Update()
    {
        if (!isAIControlled)
        {
            if (Input.GetKeyDown(KeyCode.S) && isFallable) // Press 'S' to fall through
            {
                FallThroughPlatform();
            }
        }

        // Enable or disable collider based on player's position
        CheckPlayerPosition();
    }

    // AI can trigger falling through the platform
    public void SetAIFallThrough(bool shouldFall)
    {
        if (isAIControlled && shouldFall && isFallable)
        {
            FallThroughPlatform();
        }
    }

/*    private bool IsPlayerStandingOnMe()
    {
        if (playerMovement == null || playerMovement.boxCollider == null) return false;

        // Define a small area at the bottom of the player's collider
        Bounds playerBounds = playerMovement.boxCollider.bounds;
        Vector2 checkOrigin = (Vector2)playerBounds.center + Vector2.down * playerBounds.extents.y; // Bottom center of player's collider
        Vector2 checkSize = new Vector2(playerBounds.size.x * 0.9f, checkDistance); // Check a box slightly less wide than player, height = checkDistance

        // Perform OverlapBox
        // IMPORTANT: We only want to check against THIS platform's layer
        LayerMask thisPlatformLayer = 1 << gameObject.layer; // Create a layer mask with only this object's layer

        Collider2D hit = Physics2D.OverlapBox(
            checkOrigin + Vector2.down * checkSize.y / 2, // Center of the check box (offset downwards by half its height)
            checkSize, // Size of the check box
            0f, // Angle
            thisPlatformLayer // Check only against THIS platform's layer mask
        );

        // Return true if we hit something AND that something is THIS platform's collider
        return hit != null && hit == edgeCollider;
    }*/
    // --- END NEW CHECK METHOD ---

    // Common method for both player and AI
    private void FallThroughPlatform()
    {
        effector.rotationalOffset = 180f; // Disable collision
        playerMovement.ResetCoyoteCounter(); // Reset coyote counter to 0
        playerMovement.GroundLayer = 0; // Prevent isGrounded() from detecting collision
        Invoke("ResetPlatform", waitTime); // Re-enable after delay
    }

    private void ResetPlatform()
    {
        effector.rotationalOffset = 0f; // Restore normal collision
        playerMovement.GroundLayer = LayerMask.GetMask("Ground"); // Restore ground detection
    }

    private void CheckPlayerPosition()
    {
        if (player != null)
        {
            float platformTop = edgeCollider.bounds.max.y + 1.1f; // Get the top Y of the platform

            float playerY = player.position.y;

            // Disable collider if player is outside the platform's width
            edgeCollider.enabled = (playerY > platformTop);
        }
    }
}
