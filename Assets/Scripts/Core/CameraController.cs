using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Room Camera
    [SerializeField] private float speed;
    private float currentPosY; // Used as target Y for room camera or initial Y
    private Vector3 velocity = Vector3.zero; // Used by SmoothDamp for Y-axis

    // Follow player X
    [SerializeField] private Transform player;
    [SerializeField] private Health playerHealth;
    [SerializeField] private float aheadDistance;
    [SerializeField] private float cameraSpeed; // Speed for lookAhead Lerp
    private float lookAhead;

    // Freeze camera X
    private bool isXFrozen = false;
    private float frozenX = 0f;


    // Chase mode
    [Header("Chase Mode Settings")]
    [SerializeField] private bool isChase = false;
    [SerializeField] private float chaseSpeed = 5f; // Speed at which camera moves right in chase mode
    private bool playerInMiddle = false;
    private bool playerWasMoving = false;
    private float playerLastXPosition;

    // Player Y Follow Settings
    [Header("Player Y Follow Settings")]
    [SerializeField] private bool followPlayerY = false;
    [SerializeField] private float playerYOffset = 2f; // Vertical offset when following player's Y
    private bool snapYNextFrame = true;

    [Header("Y Offset Transition")]
    [SerializeField] private float offsetTransitionDuration = 0.5f;

    private bool isTransitioningYOffset = false;
    private float transitionStartYOffset;
    private float transitionTargetYOffset;
    private float transitionTimer = 0f;

    private void Start()
    {
        if (followPlayerY)
        {
            // If starting with Y follow, set the camera's Y position directly to the target.
            // SmoothDamp in Update will then keep it following smoothly.
            float initialTargetY = player.position.y + playerYOffset;
            transform.position = new Vector3(transform.position.x, initialTargetY, transform.position.z);
            currentPosY = initialTargetY; // Initialize currentPosY in case mode is toggled
        }
        else
        {
            // Original behavior for room camera:
            // Set initial target Y based on player's start position, with a fixed offset.
            // The camera will SmoothDamp to this Y.
            currentPosY = player.position.y + 2f; // Using the 2f offset from your original Start()
        }

        // Initialize chase mode variables
        playerLastXPosition = player.position.x;
    }

    private void Update()
    {
        HandleYMovement();
        HandleXMovement();
        
        // Update lookAhead for normal follow mode
        if (!isChase && !isXFrozen)
        {
            lookAhead = Mathf.Lerp(lookAhead, (aheadDistance * player.localScale.x), Time.deltaTime * cameraSpeed);
        }
    }

    private void HandleYMovement()
    {
        float yTargetForSmoothDamp;

        if (followPlayerY)
        {
            if (isTransitioningYOffset)
            {
                transitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(transitionTimer / offsetTransitionDuration);
                // Lerp the offset
                playerYOffset = Mathf.Lerp(transitionStartYOffset, transitionTargetYOffset, t);

                if (t >= 1f)
                {
                    // Done transitioning
                    isTransitioningYOffset = false;
                }
            }

            yTargetForSmoothDamp = player.position.y + playerYOffset;

            if (snapYNextFrame)
            {
                // Instantly snap camera Y position and clear velocity
                transform.position = new Vector3(transform.position.x, yTargetForSmoothDamp, transform.position.z);
                velocity = Vector3.zero;
                //snapYNextFrame = false; // Only snap once
            }
            else
            {
                // Smooth follow Y
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    new Vector3(transform.position.x, yTargetForSmoothDamp, transform.position.z),
                    ref velocity,
                    speed
                );
            }
        }
        else
        {
            // Room camera mode
            yTargetForSmoothDamp = currentPosY;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                new Vector3(transform.position.x, yTargetForSmoothDamp, transform.position.z),
                ref velocity,
                speed
            );
        }
    }

    private void HandleXMovement()
    {
        if (isChase)
        {
            HandleChaseMode();
        }
        else if (!isXFrozen)
        {
            // Normal follow mode
            transform.position = new Vector3(player.position.x + lookAhead, transform.position.y, transform.position.z);
        }
        else
        {
            // Frozen mode
            transform.position = new Vector3(frozenX, transform.position.y, transform.position.z);
        }
    }

    private void HandleChaseMode()
    {
        float cameraCurrentX = transform.position.x;
        float playerCurrentX = player.position.x;
        
        // Check if player is moving
        bool isPlayerMoving = Mathf.Abs(playerCurrentX - playerLastXPosition) > 0.01f;
        playerLastXPosition = playerCurrentX;
        
        // Get camera bounds (assuming camera width, you might need to adjust this based on your camera setup)
        Camera cam = GetComponent<Camera>();
        float cameraHalfWidth = cam.orthographicSize * cam.aspect;
        float cameraLeftEdge = cameraCurrentX - cameraHalfWidth;
        float cameraMiddle = cameraCurrentX;
        
        // Check if player fell out of camera on the left side
        if (playerCurrentX < cameraLeftEdge)
        {
            // Player fell out, give damage
            playerHealth.TakeDamage(100);
        }
        
        // Check if player is in the middle of the camera
        playerInMiddle = Mathf.Abs(playerCurrentX - cameraMiddle) < cameraHalfWidth * 0.1f; // 10% of camera width as "middle" zone
        
        if (playerInMiddle && isPlayerMoving && playerCurrentX > cameraMiddle)
        {
            // Player is in middle, moving, and moving right - camera follows player
            playerWasMoving = true;
            transform.position = new Vector3(playerCurrentX, transform.position.y, transform.position.z);
        }
        else if (playerWasMoving && (!isPlayerMoving || playerCurrentX <= cameraMiddle))
        {
            // Player stopped moving or moved back - return to normal chase speed
            playerWasMoving = false;
        }
        
        if (!playerWasMoving)
        {
            // Normal chase mode - move right at constant speed
            transform.position = new Vector3(cameraCurrentX + chaseSpeed * Time.deltaTime, transform.position.y, transform.position.z);
        }
    }

    public void MoveToNewRoom(Transform _newRoom)
    {
        // This sets the target Y for the room camera mode.
        // If followPlayerY is true, this value will be stored but not immediately used
        // for camera positioning until followPlayerY is set to false.
        currentPosY = _newRoom.position.y + 2.38f;
    }

    // Method to freeze or unfreeze the X position of the camera
    public void SetCameraXFreeze(bool freeze, float xPosition = 0f)
    {
        isXFrozen = freeze;
        if (freeze)
        {
            frozenX = xPosition; // Set the X position to freeze the camera at
        }
    }

    public void SetCameraYFreeze(float yPosition = 0f)
    {
        followPlayerY = false;
        frozenX = yPosition; // Set the X position to freeze the camera at
    }

    // Method to enable/disable chase mode
    public void SetChaseMode(bool chase)
    {
        isChase = chase;
        if (chase)
        {
            // Reset chase mode variables
            playerWasMoving = false;
            playerInMiddle = false;
            playerLastXPosition = player.position.x;
        }
    }

    // Optional: Method to toggle Y follow mode and set offset
    public void SetFollowPlayerY(float yOffset = -1f)
    {
        followPlayerY = true;
        if (yOffset != -1f)
        {
            currentPosY = playerYOffset + yOffset;
        }
        else
        {
            snapYNextFrame = true; // Trigger snapping in next Update
        }

    }

    public void SetYOffset(float newOffset)
    {
        // Capture the start/end values
        transitionStartYOffset = playerYOffset;
        transitionTargetYOffset = newOffset;
        transitionTimer = 0f;
        isTransitioningYOffset = true;

        // Ensure we snap once, right after transition completes
        snapYNextFrame = true;
    }

    public float GetPlayerYOffset()
    {
        return playerYOffset;
    }

    // Getter for chase mode state
    public bool IsChaseMode()
    {
        return isChase;
    }

    // Method to set chase speed
    public void SetChaseSpeed(float newChaseSpeed)
    {
        chaseSpeed = newChaseSpeed;
    }
}