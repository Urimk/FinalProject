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
    public Transform groundCheck;
    [SerializeField] private float groundCheckWidth = 1.0f;
    [SerializeField] private float groundCheckHeight = 0.1f;
    private SpriteRenderer _playerSpriteRenderer;
    private int _facingDirection = 1;


    private GameObject _equippedEars;



    [Header("Sounds")]
    [SerializeField] private AudioClip jumpSound;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime;
    [SerializeField] private float groundedGraceTime = 0.5f;

    [Header("Multiple Jumps")]
    [SerializeField] private int baseExtraJumps;
    private int _extraJumps;
    private float _jumpPower;
    private bool _hasPowerUp = false;
    private int _jumpCounter;
    public float normalGrav = 2f;
    public float maxFallSpeed = 100f;

    public static PlayerMovement instance; // Singleton instance
    private Rigidbody2D _rigidbody2D;
    private Animator _animator;
    private BoxCollider2D _boxCollider;

    // Movement and state tracking variables
    private float _horizontalInput;
    private float _coyoteCounter;
    private float _wallJumpCooldown;
    private float _disableMovementTimer;
    private float _timeSinceGrounded;
    // Add these fields to your PlayerMovement class
    [Header("Recoil Settings")]
    public float recoilForce = 10f;
    public float recoilDuration = 0.3f;
    public float recoilVerticalForce = 5f; // Optional upward force during recoil

    private bool _isInRecoil = false;
    private float _recoilTimer = 0f;
    private Vector2 _recoilDirection;

    // For Testing
    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    public float JumpPower
    {
        get => _jumpPower;
        set => _jumpPower = value;
    }

    public LayerMask GroundLayer
    {
        get => groundLayer;
        set => groundLayer = value;
    }

    private void Awake()
    {
        instance = this;
        _jumpPower = baseJumpPower;
        _extraJumps = baseExtraJumps;
        InitializeComponents();
    }



    private void InitializeComponents()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        UpdateTimers();
        HandleRecoil();
        HandleMovementInput();
        UpdateAnimationParameters();
        UpdateEarsPosition();
        UpdateGravityAndWallInteraction();
        ResetExtraJumps();
        HandleJumpInput();
    }


    private void UpdateEarsPosition()
    {
        if (_playerSpriteRenderer.sprite != null)
        {
            string spriteName = _playerSpriteRenderer.sprite.name;

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
        if (_disableMovementTimer > 0)
        {
            _disableMovementTimer -= Time.deltaTime;
        }

        // Track time since player was grounded
        _timeSinceGrounded = IsGrounded() ? 0 : _timeSinceGrounded + Time.deltaTime;
    }

    private void HandleMovementInput()
    {
        if (!isAIControlled && _disableMovementTimer <= 0)
        {
            _horizontalInput = Input.GetAxis("Horizontal");
        }

        // Only allow horizontal movement if _disableMovementTimer is over and not blocked
        if (_disableMovementTimer <= 0 && !IsHorizontallyBlocked())
        {
            if (Mathf.Abs(_horizontalInput) > 0.01f)
            {
                // Slightly lift the player to help with small bumps
                transform.position += new Vector3(0f, 0.001f, 0f);
            }
            _rigidbody2D.velocity = new Vector2(_horizontalInput * speed, _rigidbody2D.velocity.y);
        }
        else if (IsHorizontallyBlocked() && !IsGrounded())
        {
            // If blocked horizontally and not grounded, ensure falling
            _rigidbody2D.velocity = new Vector2(0f, _rigidbody2D.velocity.y);
        }

        // Flips the player sprite when moving left and right
        FlipSprite();
    }

    // AI can set movement input using this method
    public void SetAIInput(float moveDirection)
    {
        if (isAIControlled && _disableMovementTimer <= 0)
        {
            _horizontalInput = moveDirection;
        }
    }

    private bool IsHorizontallyBlocked()
    {
        // Don't check for blocking during recoil (let recoil push through briefly)
        if (_isInRecoil && _recoilTimer > recoilDuration * 0.7f) // Only for first 70% of recoil
        {
            return false;
        }
        // 1) Don't even raycast if you're not trying to move horizontally.
        if (Mathf.Approximately(_horizontalInput, 0f))
            return false;

        // 2) Decide "forward" based on which way the player is facing.
        //    If you already flip your sprite via localScale.x, you can use that:
        Vector2 checkDirection = _facingDirection == 1 ? Vector2.right : Vector2.left;

        Vector2 size = _boxCollider.bounds.size;
        size.y += 0.015f; // increase height a little

        Vector2 center = _boxCollider.bounds.center + new Vector3(0, 0.025f / 2f, 0);

        // 3) Perform the short box-cast in front of the player only.
        RaycastHit2D hit = Physics2D.BoxCast(
            center,
            size,
            0f,
            checkDirection,
            0.015f,
            obstacleLayer | wallLayer | groundLayer
        );

        if (hit.collider != null)
        {

            // 4) If it's a falling platform, ignore it.
            FallingPlatform fallingPlatform = hit.collider.GetComponent<FallingPlatform>();
            if (fallingPlatform != null)
            {
                return false;
            }

            // 5) Otherwise it really is a horizontal block.
            return true;
        }


        return false;
    }

    private void FlipSprite()
    {
        if (_horizontalInput > 0.01f)
        {
            transform.localScale = Vector3.one;
            _facingDirection = 1;
        }
        else if (_horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            _facingDirection = -1;
        }
    }

    private void UpdateAnimationParameters()
    {
        _animator.SetBool("running", _horizontalInput != 0);
        _animator.SetBool("grounded", IsGrounded());
    }

    private void UpdateGravityAndWallInteraction()
    {
        if (_wallJumpCooldown > 0.24f)
        {
            HandleWallInteraction();
        }
        else
        {
            _wallJumpCooldown += Time.deltaTime;
        }
        if (_rigidbody2D.velocity.y < -maxFallSpeed)
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, -maxFallSpeed);
        }
    }

    private void ResetExtraJumps()
    {
        if (IsGrounded() && _rigidbody2D.velocity.y == 0)
        {
            _jumpCounter = _extraJumps + 1;
            //Move frome here
            _wallJumpCooldown = 0;
        }
    }

    private void HandleWallInteraction()
    {
        _wallJumpCooldown += Time.deltaTime;
        if (OnWall() && _horizontalInput != 0 && !IsGrounded())
        {
            if (_timeSinceGrounded > groundedGraceTime)
            {
                // Stick to the wall
                _rigidbody2D.gravityScale = 5;
                _rigidbody2D.velocity = Vector2.zero;
            }
            else
            {
                // Within grace time, apply normal gravity and stop horizontal movement
                _rigidbody2D.gravityScale = normalGrav;
                _rigidbody2D.velocity = new Vector2(0, _rigidbody2D.velocity.y);
            }
        }
        else
        {
            _rigidbody2D.gravityScale = normalGrav;
            ManageCoyoteTime();
        }
    }

    private void ManageCoyoteTime()
    {
        if (IsGrounded())
        {
            _coyoteCounter = coyoteTime;
        }
        else if (_coyoteCounter > 0) // Prevent coyoteCounter from going negative
        {
            _coyoteCounter -= Time.deltaTime;
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

            if (Input.GetKeyUp(KeyCode.Space) && _rigidbody2D.velocity.y > 0)
            {
                AdjustJumpHeight();
            }
        }
    }

    // In PlayerMovement.cs
    private Coroutine _currentAIJumpRoutine = null;

    public void SetAIJump(float jumpDuration)
    {
        if (isAIControlled)
        {
            // Stop any previous jump hold simulation if a new jump command comes
            if (_currentAIJumpRoutine != null || jumpDuration <= 0)
            {
                return; // No jump requested
            }
            // Start the new jump hold simulation
            _currentAIJumpRoutine = StartCoroutine(AIJumpRoutine(jumpDuration));
        }
    }

    private IEnumerator AIJumpRoutine(float jumpDuration)
    {
        bool wasGroundJump = AttemptJump(); // Simulate pressing the jump button
        if (wasGroundJump)
        {
            yield return new WaitForSeconds(jumpDuration); // Simulate holding the button
            // Check velocity *after* the wait
            if (_rigidbody2D.velocity.y > 0)
            {
                AdjustJumpHeight(); // Simulate releasing the jump button early
            }
            _currentAIJumpRoutine = null; // Mark the routine as finished 
        }
    }

    // Add ResetState method if you don't have one, to clear the coroutine reference on episode reset
    public void ResetState()
    {
        if (_currentAIJumpRoutine != null)
        {
            StopCoroutine(_currentAIJumpRoutine);
            _currentAIJumpRoutine = null;
        }
        // ... other reset logic ...
    }

    // Handles jump logic
    private bool AttemptJump()
    {
        if (UIManager.instance.IsGamePaused())
        {
            return false;
        }
        bool wasGroundJump = Jump();

        // Play jump sound when grounded
        if (IsGrounded())
        {
            SoundManager.instance.PlaySound(jumpSound, gameObject);
        }
        return wasGroundJump;
    }


    // Handles adjustable jump height
    private void AdjustJumpHeight()
    {
        _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _rigidbody2D.velocity.y / 2);
        _coyoteCounter = 0;
    }


    private bool Jump()
    {
        if (_coyoteCounter <= 0 && !OnWall() && _jumpCounter <= 0)
        {
            return false;
        }

        if (IsGrounded())
        {
            PerformGroundJump();
            return true;
        }
        else
        {
            PerformWallOrAirJump();
            return false;
        }
    }

    private void PerformGroundJump()
    {
        _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _jumpPower);
        _coyoteCounter = 0;
        _animator.SetTrigger("jump");
        _jumpCounter--;
    }

    private void PerformWallOrAirJump()
    {
        if (OnWall() && !IsGrounded() && _timeSinceGrounded > groundedGraceTime && _wallJumpCooldown > 0.05f)
        {
            PerformWallJump();
        }
        else if (_coyoteCounter > 0) // Fix: Allow coyote jump only if counter is positive
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _jumpPower);
            _coyoteCounter = 0; // Reset after using
            _jumpCounter--;
        }
        else
        {
            if (_jumpCounter > 0)
            {
                _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _jumpPower);
                _jumpCounter--;
            }
        }
    }

    private void PerformWallJump()
    {
        // Start cooldown for wall jump and disable movement
        _wallJumpCooldown = 0;
        _disableMovementTimer = 0.24f;
        _rigidbody2D.gravityScale = 6;

        // Apply wall jump force (away from the wall)
        _horizontalInput = 0;
        _rigidbody2D.velocity = new Vector2(-Mathf.Sign(transform.localScale.x) * 10, 12);
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
        _facingDirection = -_facingDirection;
        _jumpCounter--;
    }
    // HAS 2 BUGS!

    public bool IsGrounded()
    {
        Vector2 boxCenter = groundCheck.position;
        Vector2 boxSize = new Vector2(groundCheckWidth, groundCheckHeight); // e.g., width = 1f, height = 0.1f
        return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer | obstacleLayer);
    }




    public bool OnWall()
    {
        Vector2 size = _boxCollider.bounds.size;
        size.y += 0.025f; // increase height a little

        // Shift the center up by half of the added height
        Vector2 center = _boxCollider.bounds.center + new Vector3(0, 0.025f / 2f, 0);

        RaycastHit2D raycastHit = Physics2D.BoxCast(
            center,
            size,
            0,
            new Vector2(transform.localScale.x, 0),
            0.015f,
            wallLayer
        );
        return raycastHit.collider != null;
    }

    public bool CanAttack()
    {
        return !OnWall();
    }

    public void ResetCoyoteCounter()
    {
        _coyoteCounter = 0;
    }

    public Vector2 GetVelocity()
    {
        return _rigidbody2D.velocity;
    }

    void OnDisable()
    {
        _animator.SetBool("grounded", true);
        _animator.SetBool("running", false);  // Force running to stop
    }

    public int GetFacingDirection()
    {
        return _facingDirection;
    }

    public void ActivatePowerUp(int bonusJumps, float bonusJumpPower)
    {
        if (!_hasPowerUp)
        {
            _extraJumps += bonusJumps;
            _jumpPower += bonusJumpPower;
            _hasPowerUp = true;
        }
        // Spawn the ears and attach to the player
        if (earsSlot != null && earsPrefab != null)
        {
            _equippedEars = Instantiate(earsPrefab, earsSlot.position, Quaternion.identity, earsSlot);
        }
    }

    public void LosePowerUp()
    {
        if (_hasPowerUp)
        {
            _extraJumps = baseExtraJumps;
            _jumpPower = baseJumpPower;
            _hasPowerUp = false;
        }
        // Remove the ears
        if (_equippedEars != null)
        {
            Destroy(_equippedEars);
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
            knockbackDirection.x = _facingDirection == 1 ? 0.5f : -0.5f;
        }

        // Apply recoil
        StartRecoil(knockbackDirection);
    }

    private void StartRecoil(Vector2 direction)
    {
        _isInRecoil = true;
        _recoilTimer = recoilDuration;
        _recoilDirection = direction;

        // Apply immediate force
        Vector2 recoilVelocity = new Vector2(
            direction.x * recoilForce,
            Mathf.Max(direction.y * recoilForce, recoilVerticalForce) // Ensure some upward movement
        );

        _rigidbody2D.velocity = recoilVelocity;
    }

    // Add this to your Update method (or wherever you handle movement)
    private void HandleRecoil()
    {
        if (_isInRecoil)
        {
            _recoilTimer -= Time.deltaTime;

            // Gradually reduce recoil influence
            float recoilStrength = _recoilTimer / recoilDuration;

            if (_recoilTimer <= 0f)
            {
                _isInRecoil = false;
            }
            else
            {
                _disableMovementTimer = 0.08f;
                return; // Exit early to prevent normal movement during recoil
            }
        }
    }
}
