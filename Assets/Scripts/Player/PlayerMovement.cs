using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private bool isAIControlled;
    [SerializeField] private float speed;
    [SerializeField] private float baseJumpPower;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask obstacleLayer;  // New layer for obstacles\
    [SerializeField] private LayerMask defaultLayer;  // New layer for obstacles
    [SerializeField] private Transform earsSlot; // drag the child transform in the inspector
    [SerializeField] private GameObject earsPrefab; // drag the ears prefab here
    private SpriteRenderer playerSpriteRenderer;
    private int facingDirecton = 1;


    private GameObject equippedEars;



    [Header("Sounds")]
    [SerializeField] private AudioClip jumpSound;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime;
    [SerializeField] private float groundedGraceTime = 0.2f;

    [Header("Multiple Jumps")]
    [SerializeField] private int baseExtraJumps;
    private int extraJumps;
    private float jumpPower;
    private bool hasPowerUp = false;
    private int jumpCounter;
    public float normalGrav = 2f;
    public float maxFallSpeed = 100f;

    public static PlayerMovement instance; // Singleton instance
    private Rigidbody2D body;
    private Animator anim;
    private BoxCollider2D boxCollider;

    // Movement and state tracking variables
    private float horizontalInput;
    private float coyoteCounter;
    private float wallJumpCooldown;
    private float disableMovementTimer;
    private float timeSinceGrounded;
    // Add these fields to your PlayerMovement class
    [Header("Recoil Settings")]
    public float recoilForce = 10f;
    public float recoilDuration = 0.3f;
    public float recoilVerticalForce = 5f; // Optional upward force during recoil

    private bool isInRecoil = false;
    private float recoilTimer = 0f;
    private Vector2 recoilDirection;

    // For Testing
    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    public float JumpPower
    {
        get => jumpPower;
        set => jumpPower = value;
    }

    public LayerMask GroundLayer
    {
        get => groundLayer;
        set => groundLayer = value;
    }

    private void Awake()
    {
        instance = this;
        jumpPower = baseJumpPower;
        extraJumps = baseExtraJumps;
        InitializeComponents();
    }



    private void InitializeComponents()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        UpdateTimers();
        HandleRecoil();
        HandleMovementInput();
        UpdateAnimationParameters();
        updateEarsPosition();
        UpdateGravityAndWallInteraction();
        resetExtraJumps();
        HandleJumpInput();
    }

    private void updateEarsPosition()
    {
        if (playerSpriteRenderer.sprite != null)
        {
            string spriteName = playerSpriteRenderer.sprite.name;

            Vector3 newPosition;

            if (spriteName.StartsWith("walk") || spriteName.StartsWith("jump"))
            {
                newPosition = new Vector3(0.15f, 0f, 0f);
            }
            else if (spriteName == "attack_01")
            {
                newPosition = new Vector3(0.25f, 0f, 0f);
            }
            else if (spriteName == "attack_02")
            {
                newPosition = new Vector3(0.3f, -0.05f, 0f);
            }
            else
            {
                newPosition = new Vector3(0.05f, 0f, 0f);
            }

            earsSlot.localPosition = newPosition;
        }
    }
    private void UpdateTimers()
    {
        // Reduce disable movement timer
        if (disableMovementTimer > 0)
        {
            disableMovementTimer -= Time.deltaTime;
        }

        // Track time since player was grounded
        timeSinceGrounded = isGrounded() ? 0 : timeSinceGrounded + Time.deltaTime;
    }

    private void HandleMovementInput()
    {
        if (!isAIControlled)
        {
            horizontalInput = Input.GetAxis("Horizontal");
        }

        // Only allow horizontal movement if disableMovementTimer is over and not blocked
        if (disableMovementTimer <= 0 && !IsHorizontallyBlocked())
        {
            body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
        }
        else if (IsHorizontallyBlocked() && !isGrounded())
        {
            // If blocked horizontally and not grounded, ensure falling
            body.velocity = new Vector2(0, body.velocity.y);
        }

        // Flips the player sprite when moving left and right
        FlipSprite();
    }

    // AI can set movement input using this method
    public void SetAIInput(float moveDirection)
    {
        if (isAIControlled)
        {
            horizontalInput = moveDirection;
        }
    }

    private bool IsHorizontallyBlocked()
    {
        // Don't check for blocking during recoil (let recoil push through briefly)
        if (isInRecoil && recoilTimer > recoilDuration * 0.7f) // Only for first 70% of recoil
        {
            return false;
        }
        // 1) Don’t even raycast if you're not trying to move horizontally.
        if (Mathf.Approximately(horizontalInput, 0f))
            return false;

        // 2) Decide “forward” based on which way the player is facing.
        //    If you already flip your sprite via localScale.x, you can use that:
        Vector2 checkDirection = facingDirecton == 1 ? Vector2.right : Vector2.left;

        // 3) Perform the short box-cast in front of the player only.
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0f,
            checkDirection,
            0.1f,
            obstacleLayer
        );

        if (hit.collider != null)
        {
            // 4) If it’s a falling platform, ignore it.
            if (hit.collider.GetComponent<FallingPlatform>() != null)
                return false;

            // 5) Otherwise it really is a horizontal block.
            return true;
        }

        return false;
    }

    private void FlipSprite()
    {
        if (horizontalInput > 0.01f)
        {
            transform.localScale = Vector3.one;
            facingDirecton = 1;
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            facingDirecton = -1;
        }
    }

    private void UpdateAnimationParameters()
    {
        anim.SetBool("running", horizontalInput != 0);
        anim.SetBool("grounded", isGrounded());
    }

    private void UpdateGravityAndWallInteraction()
    {
        if (wallJumpCooldown > 0.24f)
        {
            HandleWallInteraction();
        }
        else
        {
            wallJumpCooldown += Time.deltaTime;
        }
        if (body.velocity.y < -maxFallSpeed)
        {
            body.velocity = new Vector2(body.velocity.x, -maxFallSpeed);
        }
    }

    private void resetExtraJumps()
    {
        if (isGrounded())
        {
            jumpCounter = extraJumps;
        }
    }

    private void HandleWallInteraction()
    {
        if (onWall() && horizontalInput != 0 && !isGrounded())
        {
            if (timeSinceGrounded > groundedGraceTime)
            {
                // Stick to the wall
                body.gravityScale = 5;
                body.velocity = Vector2.zero;
            }
            else
            {
                // Within grace time, apply normal gravity and stop horizontal movement
                body.gravityScale = normalGrav;
                body.velocity = new Vector2(0, body.velocity.y);
            }
        }
        else
        {
            body.gravityScale = normalGrav;
            ManageCoyoteTime();
        }
    }

    private void ManageCoyoteTime()
    {
        if (isGrounded())
        {
            coyoteCounter = coyoteTime;
        }
        else if (coyoteCounter > 0) // Prevent coyoteCounter from going negative
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    private void HandleJumpInput()
    {
        if (!isAIControlled)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AttemptJump();
            }

            if (Input.GetKeyUp(KeyCode.Space) && body.velocity.y > 0)
            {
                AdjustJumpHeight();
            }
        }
    }

    // In PlayerMovement.cs
    private Coroutine currentAIJumpRoutine = null;

    public void SetAIJump(float jumpDuration)
    {
        if (isAIControlled)
        {
            // Stop any previous jump hold simulation if a new jump command comes
            if (currentAIJumpRoutine != null)
            {
                StopCoroutine(currentAIJumpRoutine);
                currentAIJumpRoutine = null;
                // Optional: Decide if releasing the previous jump early should trigger AdjustJumpHeight here
                // if (body.velocity.y > 0) { AdjustJumpHeight(); }
            }

            if (jumpDuration <= 0) // Use <= 0 for safety
            {
                return; // No jump requested
            }
            // Start the new jump hold simulation
            currentAIJumpRoutine = StartCoroutine(AIJumpRoutine(jumpDuration));
        }
    }

    private IEnumerator AIJumpRoutine(float jumpDuration)
    {
        AttemptJump(); // Simulate pressing the jump button
        yield return new WaitForSeconds(jumpDuration); // Simulate holding the button

        // Check velocity *after* the wait
        if (body.velocity.y > 0)
        {
            AdjustJumpHeight(); // Simulate releasing the jump button early
        }
        currentAIJumpRoutine = null; // Mark the routine as finished
    }

    // Add ResetState method if you don't have one, to clear the coroutine reference on episode reset
    public void ResetState()
    {
        if (currentAIJumpRoutine != null)
        {
            StopCoroutine(currentAIJumpRoutine);
            currentAIJumpRoutine = null;
        }
        // ... other reset logic ...
    }

    // Handles jump logic
    private void AttemptJump()
    {
        if (UIManager.instance.IsGamePaused())
        {
            return;
        }
        Jump();

        // Play jump sound when grounded
        if (isGrounded())
        {
            SoundManager.instance.PlaySound(jumpSound);
        }
    }


    // Handles adjustable jump height
    private void AdjustJumpHeight()
    {
        body.velocity = new Vector2(body.velocity.x, body.velocity.y / 2);
        coyoteCounter = 0;
    }


    private void Jump()
    {
        if (coyoteCounter <= 0 && !onWall() && jumpCounter <= 0)
        {
            return;
        }

        if (isGrounded())
        {
            PerformGroundJump();
        }
        else
        {
            PerformWallOrAirJump();
        }
    }

    private void PerformGroundJump()
    {
        body.velocity = new Vector2(body.velocity.x, jumpPower);
        coyoteCounter = 0;
        anim.SetTrigger("jump");
    }

    private void PerformWallOrAirJump()
    {
        if (onWall() && !isGrounded() && timeSinceGrounded > groundedGraceTime)
        {
            PerformWallJump();
        }
        else if (coyoteCounter > 0) // Fix: Allow coyote jump only if counter is positive
        {
            body.velocity = new Vector2(body.velocity.x, jumpPower);
            coyoteCounter = 0; // Reset after using
        }
        else
        {
            if (jumpCounter > 0)
            {
                body.velocity = new Vector2(body.velocity.x, jumpPower);
                jumpCounter--;
            }
        }
    }

    private void PerformWallJump()
    {
        // Start cooldown for wall jump and disable movement
        wallJumpCooldown = 0;
        disableMovementTimer = 0.24f;
        body.gravityScale = 6;

        // Apply wall jump force (away from the wall)
        body.velocity = new Vector2(-Mathf.Sign(transform.localScale.x) * 10, 12);
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
        facingDirecton = -facingDirecton;
    }

    public bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            Vector2.down,
            0.05f,
            groundLayer | obstacleLayer
        );
        return raycastHit.collider != null;
    }

    public bool onWall()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            new Vector2(transform.localScale.x, 0),
            0.01f,
            wallLayer
        );
        return raycastHit.collider != null;
    }

    public bool canAttack()
    {
        return !onWall();
    }

    public void ResetCoyoteCounter()
    {
        coyoteCounter = 0;
    }

    public Vector2 GetVelocity()
    {
        return GetComponent<Rigidbody2D>().velocity;
    }

    void OnDisable()
    {
        anim.SetBool("grounded", true);
        anim.SetBool("running", false);  // Force running to stop
    }

    public int GetFacingDirection()
    {
        return facingDirecton;
    }

    public void ActivatePowerUp(int bonusJumps, float bonusJumpPower)
    {
        if (!hasPowerUp)
        {
            extraJumps += bonusJumps;
            jumpPower += bonusJumpPower;
            hasPowerUp = true;
        }
        // Spawn the ears and attach to the player
        if (earsSlot != null && earsPrefab != null)
        {
            equippedEars = Instantiate(earsPrefab, earsSlot.position, Quaternion.identity, earsSlot);
        }
    }

    public void LosePowerUp()
    {
        if (hasPowerUp)
        {
            extraJumps = baseExtraJumps;
            jumpPower = baseJumpPower;
            hasPowerUp = false;
        }
        // Remove the ears
        if (equippedEars != null)
        {
            Destroy(equippedEars);
        }
    }
    
    // Method to be called from the damage dealer
public void Recoil(Vector2 sourcePosition, Vector2 recoilDirection = default)
{
    Vector2 knockbackDirection;
    
    // If a specific recoil direction is provided, use it
    if (recoilDirection != Vector2.zero)
    {
        knockbackDirection = recoilDirection.normalized;
    }
    else
    {
        // Calculate direction from damage source to player
        Vector2 playerPosition = transform.position;
        knockbackDirection = (playerPosition - sourcePosition).normalized;
    }
    
    // Ensure minimum horizontal knockback (prevent getting stuck)
    if (Mathf.Abs(knockbackDirection.x) < 0.3f)
    {
        knockbackDirection.x = facingDirecton == 1 ? 0.5f : -0.5f;
    }
    
    // Apply recoil
    StartRecoil(knockbackDirection);
}

private void StartRecoil(Vector2 direction)
{
    isInRecoil = true;
    recoilTimer = recoilDuration;
    recoilDirection = direction;
    
    // Apply immediate force
    Vector2 recoilVelocity = new Vector2(
        direction.x * recoilForce,
        Mathf.Max(direction.y * recoilForce, recoilVerticalForce) // Ensure some upward movement
    );
    
    body.velocity = recoilVelocity;
}

// Add this to your Update method (or wherever you handle movement)
private void HandleRecoil()
{
    if (isInRecoil)
    {
        recoilTimer -= Time.deltaTime;
        
        // Gradually reduce recoil influence
        float recoilStrength = recoilTimer / recoilDuration;
        
        if (recoilTimer <= 0f)
        {
            isInRecoil = false;
        }
        else
        {
            disableMovementTimer = 0.08f;
            return; // Exit early to prevent normal movement during recoil
        }
    }
}
}