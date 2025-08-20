using System.Collections;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles player movement, jumping, wall interaction, recoil, and power-ups for both player and AI control.
/// 
/// This is the main controller for all player movement mechanics in the platformer game. It integrates:
/// - Movement input handling (human and AI)
/// - Jump mechanics (ground, wall, coyote time, multiple jumps)
/// - Wall interaction and gravity scaling
/// - Recoil system for damage feedback
/// - Power-up management
/// - Animation and visual feedback
/// 
/// The class uses a unified approach where jump and movement are tightly coupled,
/// which is essential for responsive platformer gameplay.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    // ==================== Constants ====================
    // Movement and Physics
    private const float DefaultMaxFallSpeed = 100f;           // Maximum fall speed to prevent excessive velocity
    private const float DefaultNormalGravity = 2f;            // Normal gravity scale when not on wall
    private const float SpriteFlipThreshold = 0.01f;          // Minimum input to trigger sprite flipping
    private const float SmallBumpLift = 0.001f;               // Small vertical lift to help climb small obstacles
    
    // Recoil System
    private const float DefaultRecoilForce = 10f;             // Base force applied during recoil
    private const float DefaultRecoilDuration = 0.3f;         // Duration of recoil effect
    private const float DefaultRecoilVerticalForce = 5f;      // Minimum vertical force during recoil
    private const float MinHorizontalKnockback = 0.3f;        // Minimum horizontal knockback to ensure direction
    private const float DefaultHorizontalKnockback = 0.5f;    // Default horizontal knockback when direction is weak
    private const float DisableMovementDuringRecoil = 0.08f;  // Time to disable movement during recoil
    private const float RecoilInfluenceThreshold = 0.7f;      // When recoil stops blocking movement (70% through)
    
    // Wall Jump System
    private const float WallJumpCooldownDuration = 0.33f;     // Cooldown between wall jumps
    private const float WallJumpGravityScale = 6f;            // Gravity scale when sliding on wall
    private const float WallJumpHorizontalForce = 10f;        // Horizontal force applied during wall jump
    private const float WallJumpVerticalForce = 6f;           // Vertical force applied during wall jump
    private const float WallCheckBoxCastDistance = 0.015f;    // Distance to check for walls
    private const float WallCheckBoxCastHeightIncrease = 0.025f; // Extra height for wall detection
    private const float WallCheckBoxCastCenterShift = 0.025f / 2f; // Center shift for wall detection
    private const float DefaultGroundedGraceTime = 0.5f;      // Time after leaving ground before wall sliding
    
    // Animation Parameters
    private const string AnimatorJump = "jump";               // Animator parameter for jump trigger
    private const string AnimatorRunning = "running";         // Animator parameter for running state
    private const string AnimatorGrounded = "grounded";       // Animator parameter for grounded state
    
    // Visual Effects (Ears positioning)
    private const float EarsWalkJumpX = 0.15f;                // X offset for ears during walk/jump
    private const float EarsAttack01X = 0.25f;                // X offset for ears during attack_01
    private const float EarsAttack02X = 0.3f;                 // X offset for ears during attack_02
    private const float EarsAttack02Y = -0.05f;               // Y offset for ears during attack_02
    private const float EarsDefaultX = 0.05f;                 // Default X offset for ears
    private const float EarsDefaultY = 0f;                    // Default Y offset for ears

    // ==================== Inspector Fields ====================
    [Header("Input System")]
    [Tooltip("True if this player is AI controlled.")]
    [FormerlySerializedAs("isAIControlled")]
    [SerializeField] private bool _isAIControlled;
    
    [Tooltip("Movement speed of the player.")]
    [FormerlySerializedAs("speed")]
    [SerializeField] private float _speed;

    [Header("Jump Settings")]
    [Tooltip("Base jump power for the player.")]
    [FormerlySerializedAs("baseJumpPower")]
    [SerializeField] private float _baseJumpPower;
    
    [Tooltip("Base number of extra jumps allowed.")]
    [FormerlySerializedAs("baseExtraJumps")]
    [SerializeField] private int _baseExtraJumps;
    
    [Header("Coyote Time")]
    [Tooltip("Duration of coyote time (extra jump window).")]
    [FormerlySerializedAs("coyoteTime")]
    [SerializeField] private float _coyoteTime;
    
    [Tooltip("Grace time for being considered grounded.")]
    [FormerlySerializedAs("groundedGraceTime")]
    [SerializeField] private float _groundedGraceTime = DefaultGroundedGraceTime;
    
    [Header("Ground Detection")]
    [Tooltip("Width of the ground check box.")]
    [FormerlySerializedAs("groundCheckWidth")]
    [SerializeField] private float _groundCheckWidth = 1.0f;
    
    [Tooltip("Height of the ground check box.")]
    [FormerlySerializedAs("groundCheckHeight")]
    [SerializeField] private float _groundCheckHeight = 0.1f;
    
    [Tooltip("Transform for ground check.")]
    [FormerlySerializedAs("groundCheck")]
    [SerializeField] private Transform _groundCheck;

    [Header("Layer Masks")]
    [Tooltip("Layer mask for obstacle detection.")]
    [FormerlySerializedAs("obstacleLayer")]
    [SerializeField] private LayerMask _obstacleLayer;
    [Tooltip("Layer mask for ground detection.")]
    [FormerlySerializedAs("groundLayer")]
    [SerializeField] private LayerMask _groundLayer;
    [Tooltip("Layer mask for default detection.")]
    [FormerlySerializedAs("defaultLayer")]
    [SerializeField] private LayerMask _defaultLayer;
    [Tooltip("Layer mask for wall detection.")]
    [FormerlySerializedAs("wallLayer")]
    [SerializeField] private LayerMask _wallLayer;

    [Header("Visual")]
    [Tooltip("Transform slot for equipped ears.")]
    [FormerlySerializedAs("earsSlot")]
    [SerializeField] private Transform _earsSlot;
    [Tooltip("Prefab for the ears power-up.")]
    [FormerlySerializedAs("earsPrefab")]
    [SerializeField] private GameObject _earsPrefab;

    [Header("Sounds")]
    [Tooltip("Sound to play when jumping.")]
    [FormerlySerializedAs("jumpSound")]
    [SerializeField] private AudioClip _jumpSound;

    [Header("Recoil Settings")]
    [Tooltip("Force applied during recoil.")]
    [FormerlySerializedAs("recoilForce")]
    [SerializeField] private float _recoilForce = DefaultRecoilForce;
    [Tooltip("Duration of recoil effect.")]
    [FormerlySerializedAs("recoilDuration")]
    [SerializeField] private float _recoilDuration = DefaultRecoilDuration;
    [Tooltip("Vertical force applied during recoil.")]
    [FormerlySerializedAs("recoilVerticalForce")]
    [SerializeField] private float _recoilVerticalForce = DefaultRecoilVerticalForce;

    [Tooltip("Maximum fall speed for the player.")]
    [FormerlySerializedAs("maxFallSpeed")]
    [SerializeField] private float _maxFallSpeed = DefaultMaxFallSpeed;
    [Tooltip("Normal gravity value for the player.")]
    [FormerlySerializedAs("normalGrav")]
    [SerializeField] private float _normalGrav = DefaultNormalGravity;

    // ==================== Public Properties ====================
    /// <summary>Movement speed of the player.</summary>
    public float Speed { get => _speed; set => _speed = value; }
    
    /// <summary>Current jump power of the player.</summary>
    public float JumpPower { get => _jumpPower; set => _jumpPower = value; }
    
    /// <summary>Number of extra jumps available.</summary>
    public int ExtraJumps { get => _extraJumps; set => _extraJumps = value; }
    
    /// <summary>Whether the player is currently grounded.</summary>
    public bool IsGrounded => CheckGrounded();
    
    /// <summary>Layer mask for ground detection.</summary>
    public LayerMask GroundLayer { get => _groundLayer; set => _groundLayer = value; }
    
    /// <summary>Whether the player is currently on a wall.</summary>
    public bool IsOnWall => CheckOnWall();
    
    /// <summary>Transform for ground check (read-only).</summary>
    public float NormalGrav
    {
        get => _normalGrav;
        set => _normalGrav = value;
    }
    /// <summary>Maximum fall speed (read/write).</summary>
    public float MaxFallSpeed
    {
        get => _maxFallSpeed;
        set => _maxFallSpeed = value;
    }
    /// <summary>Whether this player is AI controlled.</summary>
    public bool IsAIControlled => _isAIControlled;
    /// <summary>Singleton instance of PlayerMovement.</summary>
    public static PlayerMovement Instance { get; private set; }

    // ==================== Private Fields ====================
    // Core Components
    private Rigidbody2D _rigidbody2D;                         // Physics body for movement and forces
    private Animator _animator;                               // Controls player animations
    private BoxCollider2D _boxCollider;                       // Collision detection for movement
    private SpriteRenderer _playerSpriteRenderer;             // For sprite-based visual effects
    
    // Movement State
    private int _facingDirection = 1;                         // Current facing direction (1 = right, -1 = left)
    private float _disableMovementTimer;                      // Timer to disable movement input
    
    // Recoil System
    private bool _isInRecoil = false;                         // Whether player is currently in recoil state
    private float _recoilTimer = 0f;                          // Timer for recoil duration
    private float _currentRecoilDuration = 0f;                // Current recoil duration (for custom recoil)
    private Vector2 _recoilDirection;                         // Direction of recoil force
    private bool _afterRecoil;                                // Flag set after recoil ends
    
    // Visual Effects
    private GameObject _equippedEars;                         // Reference to equipped ears power-up
    private bool _hasPowerUp = false;                         // Whether player has active power-up
    
    // ==================== Input System ====================
    private PlayerInputHandler _inputHandler;                  // Abstract input handler (human or AI)
    
    // ==================== Jump System ====================
    // Jump Configuration
    private int _extraJumps;                                   // Number of extra jumps available (double/triple jump)
    private float _jumpPower;                                  // Current jump force applied
    
    // Jump State Management
    private int _jumpCounter;                                  // Current number of jumps remaining
    private float _coyoteCounter;                              // Timer for coyote time (jump forgiveness)
    private float _timeSinceGrounded;                          // Time since player was last grounded
    
    // Wall Jump System
    private float _wallJumpCooldown;                           // Timer for wall jump cooldown
    private float _wallJumpStartTime;                          // When the current wall jump started
    private float _initialWallJumpVelocity;                    // Initial velocity of wall jump for decay calculation
    private bool _afterWallJump;                               // Flag indicating player just performed a wall jump

    // ==================== Properties ====================
    /// <summary>
    /// Whether the player can perform a wall jump.
    /// 
    /// Requirements:
    /// - Player must be on a wall (IsOnWall)
    /// - Player must not be grounded
    /// - Must have been off ground for grace time period
    /// - Wall jump cooldown must have elapsed
    /// </summary>
    public bool CanWallJump => IsOnWall && !IsGrounded && _timeSinceGrounded > _groundedGraceTime && _wallJumpCooldown > 0.05f;
    
    /// <summary>
    /// Whether the player can perform any type of jump.
    /// 
    /// Returns true if any of these conditions are met:
    /// - Coyote time is active (jump forgiveness window)
    /// - Player is on a wall (wall jump available)
    /// - Player has remaining jumps (double/triple jump)
    /// </summary>
    public bool CanJump => _coyoteCounter > 0 || IsOnWall || _jumpCounter > 0;

    /// <summary>
    /// Unity Awake callback. Initializes singleton and all system components.
    /// 
    /// Initialization order:
    /// 1. Set singleton instance for global access
    /// 2. Get and cache required Unity components
    /// 3. Set up input system (human or AI)
    /// 4. Initialize jump state with base values
    /// </summary>
    private void Awake()
    {
        Instance = this;
        InitializeComponents();
        InitializeInputSystem();
        InitializeJumpState();
    }

    /// <summary>
    /// Initializes required components.
    /// </summary>
    private void InitializeComponents()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Initializes jump state with base values.
    /// </summary>
    private void InitializeJumpState()
    {
        _jumpPower = _baseJumpPower;
        _extraJumps = _baseExtraJumps;
        ResetJumpCounter();
    }

    /// <summary>
    /// Initializes the input system based on whether the player is AI controlled.
    /// 
    /// This method:
    /// 1. Removes any existing input handlers to prevent duplicates
    /// 2. Adds the appropriate input handler (AIInput or PlayerInput)
    /// 3. Subscribes to jump events for responsive input handling
    /// 
    /// The input system uses an abstract base class (PlayerInputHandler) to allow
    /// seamless switching between human and AI control without changing movement logic.
    /// </summary>
    private void InitializeInputSystem()
    {
        // Remove existing input handlers to prevent duplicates
        PlayerInputHandler[] existingHandlers = GetComponents<PlayerInputHandler>();
        foreach (var handler in existingHandlers)
        {
            DestroyImmediate(handler);
        }

        // Add appropriate input handler based on control type
        if (_isAIControlled)
        {
            _inputHandler = gameObject.AddComponent<AIInput>();
        }
        else
        {
            _inputHandler = gameObject.AddComponent<PlayerInput>();
        }

        // Subscribe to input events for jump handling
        _inputHandler.OnJumpPressed += OnJumpPressed;
        _inputHandler.OnJumpReleased += OnJumpReleased;
    }

    /// <summary>
    /// Unity Update callback. Handles all per-frame logic in the correct order.
    /// 
    /// Update order is important for proper behavior:
    /// 1. UpdateTimers() - Update all timers first
    /// 2. HandleRecoil() - Process recoil state before movement
    /// 3. HandleWallInteraction() - Update wall gravity and interaction
    /// 4. HandleMovementInput() - Process movement and apply velocity
    /// 5. UpdateAnimationParameters() - Update animator based on current state
    /// 6. UpdateEarsPosition() - Update visual effects
    /// 7. UpdateFallSpeed() - Apply fall speed limits last
    /// </summary>
    private void Update()
    {
        UpdateTimers();
        HandleRecoil();
        HandleWallInteraction();
        ResetJumpCounter();
        HandleMovementInput();
        UpdateAnimationParameters();
        UpdateEarsPosition();
        UpdateFallSpeed();
    }

    /// <summary>
    /// Updates the position of the equipped ears based on the current sprite animation.
    /// 
    /// This method adjusts the ears position to match the player's current animation state,
    /// ensuring the ears appear in the correct position relative to the player's head
    /// during different animations (walking, jumping, attacking).
    /// 
    /// Sprite name patterns:
    /// - "walk*" or "jump*" → EarsWalkJumpX position
    /// - "attack_01" → EarsAttack01X position
    /// - "attack_02" → EarsAttack02X/Y position (with Y offset)
    /// - Default → EarsDefaultX/Y position
    /// </summary>
    private void UpdateEarsPosition()
    {
        if (_playerSpriteRenderer.sprite != null)
        {
            string spriteName = _playerSpriteRenderer.sprite.name;
            Vector3 newPosition;
            
            // Position ears based on current animation
            if (spriteName.StartsWith("walk") || spriteName.StartsWith("jump"))
            {
                newPosition = new Vector3(EarsWalkJumpX, EarsDefaultY, 0f);
            }
            else if (spriteName == "attack_01")
            {
                newPosition = new Vector3(EarsAttack01X, EarsDefaultY, 0f);
            }
            else if (spriteName == "attack_02")
            {
                newPosition = new Vector3(EarsAttack02X, EarsAttack02Y, 0f);
            }
            else
            {
                newPosition = new Vector3(EarsDefaultX, EarsDefaultY, 0f);
            }
            
            _earsSlot.localPosition = newPosition;
        }
    }

    // ==================== Timer Management ====================
    /// <summary>
    /// Updates all timers for movement and jump systems.
    /// </summary>
    private void UpdateTimers()
    {
        // Movement timers
        if (_disableMovementTimer > 0)
        {
            _disableMovementTimer -= Time.deltaTime;
        }
        
        // Jump timers
        _timeSinceGrounded = IsGrounded ? 0 : _timeSinceGrounded + Time.deltaTime;
        UpdateCoyoteTime();
        UpdateWallJumpCooldown();
    }

    /// <summary>
    /// Updates coyote time counter for jump forgiveness.
    /// </summary>
    private void UpdateCoyoteTime()
    {
        if (IsGrounded)
        {
            _coyoteCounter = _coyoteTime;
        }
        else if (_coyoteCounter > 0)
        {
            _coyoteCounter -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Updates wall jump cooldown timer.
    /// </summary>
    private void UpdateWallJumpCooldown()
    {
        if (_wallJumpCooldown > WallJumpCooldownDuration)
        {
            // Wall jump cooldown finished, handle wall interaction
        }
        else
        {
            _wallJumpCooldown += Time.deltaTime;
        }
    }

    // ==================== Movement System ====================
    /// <summary>
    /// Handles movement input and applies velocity using the input system.
    /// 
    /// This method processes movement in the following priority order:
    /// 1. Wall jump movement decay - If timer is active, apply wall jump velocity
    /// 2. After wall jump state - Handle movement restrictions after wall jump
    /// 3. Normal movement - Apply horizontal input with collision detection
    /// 4. Small bump system - Try to help player climb small obstacles
    /// 5. Sprite flipping - Update facing direction based on movement
    /// 
    /// The small bump system lifts the player slightly when blocked by small height differences,
    /// allowing them to climb small obstacles without needing to jump.
    /// </summary>
    private void HandleMovementInput()
    {
        if (_inputHandler == null)
            return;

        float horizontalInput = _inputHandler.HorizontalInput;

        // Priority 1: Handle wall jump movement decay
        if (_disableMovementTimer > 0f)
        {
            if (_isInRecoil)
            {
                return; // Recoil takes priority over wall jump movement
            }
            
            // Apply wall jump velocity decay
            float wallJumpVelocity = GetWallJumpVelocity();
            _rigidbody2D.velocity = new Vector2(wallJumpVelocity, _rigidbody2D.velocity.y);
            return;
        }

        // Priority 2: Handle after wall jump state
        if (_afterWallJump && Mathf.Approximately(horizontalInput, 0f))
        {
            if (IsGrounded)
            {
                _rigidbody2D.velocity = new Vector2(0, _rigidbody2D.velocity.y);
            }
            return;
        }
        
        // Reset wall jump and recoil flags
        _afterWallJump = false;
        _afterRecoil = false;

        // Priority 3: Apply normal movement with collision detection
        if (!IsHorizontallyBlocked())
        {
            // No obstacles - apply full movement
            _rigidbody2D.velocity = new Vector2(horizontalInput * _speed, _rigidbody2D.velocity.y);
        }
        else if (IsHorizontallyBlocked())
        {
            // Priority 4: Try small bump to help with small height differences
            if (Mathf.Abs(horizontalInput) > SpriteFlipThreshold && !UIManager.Instance.IsGamePaused)
            {
                // Apply small vertical lift
                transform.position += new Vector3(0f, SmallBumpLift, 0f);
                
                // Check if bump helped by testing collision again
                if (!IsHorizontallyBlocked())
                {
                    // Bump worked - allow movement
                    _rigidbody2D.velocity = new Vector2(horizontalInput * _speed, _rigidbody2D.velocity.y);
                }
                else
                {
                    // Bump didn't help - stop horizontal movement
                    _rigidbody2D.velocity = new Vector2(0f, _rigidbody2D.velocity.y);
                }
            }
            else
            {
                // No input or game paused - stop horizontal movement
                _rigidbody2D.velocity = new Vector2(0f, _rigidbody2D.velocity.y);
            }
        }
        
        // Priority 5: Update sprite facing direction
        FlipSprite();
    }

    // ==================== Input Event Handlers ====================
    /// <summary>
    /// Handles jump pressed event from input system.
    /// </summary>
    private void OnJumpPressed()
    {
        if (UIManager.Instance.IsGamePaused)
            return;

        bool wasGroundJump = TryJump();
        if (IsGrounded)
        {
            SoundManager.instance.PlaySound(_jumpSound, gameObject);
        }
    }

    /// <summary>
    /// Handles jump released event from input system.
    /// </summary>
    private void OnJumpReleased()
    {
        AdjustJumpHeight();
    }

    // ==================== Jump System ====================
    /// <summary>
    /// Attempts to perform a jump. Returns true if a jump was performed.
    /// </summary>
    /// <returns>True if a jump was performed, false otherwise.</returns>
    public bool TryJump()
    {
        if (!CanJump)
            return false;

        if (IsGrounded)
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

    /// <summary>
    /// Performs a ground jump.
    /// </summary>
    private void PerformGroundJump()
    {
        _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _jumpPower);
        _coyoteCounter = 0;
        _animator.SetTrigger(AnimatorJump);
        _jumpCounter--;
    }

    /// <summary>
    /// Handles wall or air jump logic.
    /// </summary>
    private void PerformWallOrAirJump()
    {
        _afterWallJump = false;
        
        if (CanWallJump)
        {
            PerformWallJump();
        }
        else if (_coyoteCounter > 0)
        {
            PerformCoyoteJump();
        }
        else if (_jumpCounter > 0)
        {
            PerformAirJump();
        }
    }

    /// <summary>
    /// Performs a coyote time jump.
    /// </summary>
    private void PerformCoyoteJump()
    {
        _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _jumpPower);
        _coyoteCounter = 0;
        _jumpCounter--;
    }

    /// <summary>
    /// Performs an air jump (double/triple jump).
    /// </summary>
    private void PerformAirJump()
    {
        _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _jumpPower);
        _jumpCounter--;
    }

    /// <summary>
    /// Performs a wall jump.
    /// </summary>
    private void PerformWallJump()
    {
        _wallJumpCooldown = 0;
        _afterWallJump = true;
        
        // Calculate wall jump velocity
        float horizontalVel = -Mathf.Sign(transform.localScale.x) * WallJumpHorizontalForce;
        _rigidbody2D.velocity = new Vector2(horizontalVel, WallJumpVerticalForce);
        
        // Track for decay
        _wallJumpStartTime = Time.time;
        _initialWallJumpVelocity = horizontalVel;
        
        // Flip the player
        Vector3 scale = transform.localScale;
        scale.x = -scale.x;
        transform.localScale = scale;
        
        _disableMovementTimer = WallJumpCooldownDuration;
    }

    /// <summary>
    /// Adjusts jump height when jump key is released early.
    /// </summary>
    public void AdjustJumpHeight()
    {
        if (_rigidbody2D.velocity.y > 0)
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _rigidbody2D.velocity.y / 2);
            _coyoteCounter = 0;
        }
    }

    // ==================== Wall Interaction ====================
    /// <summary>
    /// Handles wall interaction and gravity scaling.
    /// 
    /// This method manages the wall sliding mechanic:
    /// 1. Always resets gravity to normal first
    /// 2. Checks if player is on a wall and not grounded
    /// 3. Only applies wall gravity if player is actively moving toward the wall
    /// 4. Uses a grace time to prevent immediate wall sliding after leaving ground
    /// 
    /// Wall gravity is only applied when the player is actively pressing toward the wall,
    /// creating a more controlled wall sliding experience.
    /// </summary>
    private void HandleWallInteraction()
    {
        // Always reset to normal gravity first
        _rigidbody2D.gravityScale = _normalGrav;
        
        // Check if wall interaction should be active
        if (IsOnWall && !IsGrounded)
        {
            // Get current horizontal input
            float horizontalInput = _inputHandler?.HorizontalInput ?? 0f;
            
            // Check if player is moving toward the wall (facing direction matches input)
            bool isMovingTowardWall = (transform.localScale.x > 0 && horizontalInput > 0) || 
                                     (transform.localScale.x < 0 && horizontalInput < 0);
            
            // Apply wall gravity only if actively moving toward wall and grace time has passed
            if (isMovingTowardWall && _timeSinceGrounded > _groundedGraceTime)
            {
                _rigidbody2D.gravityScale = WallJumpGravityScale;
                _rigidbody2D.velocity = Vector2.zero; // Stop horizontal movement on wall
            }
        }
    }

    // ==================== Ground and Wall Detection ====================
    /// <summary>
    /// Checks if the player is grounded using an OverlapBox.
    /// </summary>
    /// <returns>True if grounded, false otherwise.</returns>
    private bool CheckGrounded()
    {
        Vector2 boxCenter = _groundCheck.position;
        Vector2 boxSize = new Vector2(_groundCheckWidth, _groundCheckHeight);
        return Physics2D.OverlapBox(boxCenter, boxSize, 0f, _groundLayer | _obstacleLayer);
    }

    /// <summary>
    /// Checks if the player is on a wall using a BoxCast.
    /// </summary>
    /// <returns>True if on wall, false otherwise.</returns>
    private bool CheckOnWall()
    {
        Vector2 size = _boxCollider.bounds.size;
        size.y += WallCheckBoxCastHeightIncrease;
        Vector2 center = _boxCollider.bounds.center + new Vector3(0, WallCheckBoxCastCenterShift, 0);
        RaycastHit2D raycastHit = Physics2D.BoxCast(
            center,
            size,
            0,
            new Vector2(transform.localScale.x, 0),
            WallCheckBoxCastDistance,
            _wallLayer
        );
        return raycastHit.collider != null;
    }

    // ==================== AI Input Methods ====================
    /// <summary>
    /// Allows AI to set movement input.
    /// </summary>
    /// <param name="moveDirection">The direction to move (-1, 0, 1).</param>
    public void SetAIInput(float moveDirection)
    {
        if (_isAIControlled && _inputHandler is AIInput aiInput)
        {
            aiInput.SetMovementInput(moveDirection);
        }
    }

    /// <summary>
    /// Sets AI jump input with duration.
    /// </summary>
    /// <param name="jumpDuration">Duration to hold the jump (seconds).</param>
    public void SetAIJump(float jumpDuration)
    {
        if (_isAIControlled && _inputHandler is AIInput aiInput)
        {
            aiInput.SetJumpInput(jumpDuration);
        }
    }

    /// <summary>
    /// Checks if the player is blocked horizontally by an obstacle.
    /// 
    /// This method performs collision detection in the direction the player is facing:
    /// 1. Skips collision check if player is in recoil (recoil takes priority)
    /// 2. Skips collision check if no horizontal input (no movement attempted)
    /// 3. Uses BoxCast to detect obstacles in the facing direction
    /// 4. Ignores FallingPlatform components (allows walking through them)
    /// 5. Returns true if any other obstacle is detected
    /// 
    /// The BoxCast uses an enlarged size to better detect walls and obstacles,
    /// and checks against obstacle, wall, and ground layers.
    /// </summary>
    private bool IsHorizontallyBlocked()
    {
        // Skip collision check if player is in recoil (recoil takes priority)
        // Use 70% threshold of the current recoil duration
        if (_isInRecoil && _recoilTimer > _currentRecoilDuration * RecoilInfluenceThreshold)
        {
            return false;
        }
        
        // Skip collision check if no horizontal input
        float horizontalInput = _inputHandler?.HorizontalInput ?? 0f;
        if (Mathf.Approximately(horizontalInput, 0f))
            return false;
            
        // Set up collision detection parameters
        Vector2 checkDirection = _facingDirection == 1 ? Vector2.right : Vector2.left;
        Vector2 size = _boxCollider.bounds.size;
        size.y += WallCheckBoxCastHeightIncrease; // Enlarge height for better detection
        Vector2 center = _boxCollider.bounds.center + new Vector3(0, WallCheckBoxCastCenterShift, 0);
        
        // Perform BoxCast to detect obstacles
        RaycastHit2D hit = Physics2D.BoxCast(
            center,
            size,
            0f,
            checkDirection,
            WallCheckBoxCastDistance,
            _obstacleLayer | _wallLayer | _groundLayer
        );
        
        // Process collision result
        if (hit.collider != null)
        {
            // Allow walking through falling platforms
            FallingPlatform fallingPlatform = hit.collider.GetComponent<FallingPlatform>();
            if (fallingPlatform != null)
            {
                return false;
            }
            return true; // Blocked by obstacle
        }
        return false; // Not blocked
    }

    /// <summary>
    /// Flips the player sprite based on movement direction.
    /// </summary>
    private void FlipSprite()
    {
        float horizontalInput = _inputHandler?.HorizontalInput ?? 0f;
        
        if (horizontalInput > SpriteFlipThreshold)
        {
            _facingDirection = 1;
        }
        else if (horizontalInput < -SpriteFlipThreshold)
        {
            _facingDirection = -1;
        }
        else
        {
            return;
        }

        float scale = _isAIControlled ? 0.8f : 1f;

        // Flip immediately for both AI and player (AI flip delay is handled in AIInput)
        if (_disableMovementTimer <= 0)
        {
            transform.localScale = new Vector3(scale * _facingDirection, scale, 1f);
        }
    }

    /// <summary>
    /// Updates animation parameters for running and grounded states.
    /// </summary>
    private void UpdateAnimationParameters()
    {
        float horizontalInput = _inputHandler?.HorizontalInput ?? 0f;
        _animator.SetBool(AnimatorRunning, horizontalInput != 0);
        _animator.SetBool(AnimatorGrounded, IsGrounded);
    }

    /// <summary>
    /// Updates fall speed limit.
    /// </summary>
    private void UpdateFallSpeed()
    {
        if (_rigidbody2D.velocity.y < -MaxFallSpeed)
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, -MaxFallSpeed);
        }
    }

    // ==================== State and Utility Methods ====================
    /// <summary>
    /// Resets state, including stopping AI input.
    /// </summary>
    public void ResetState()
    {
        _inputHandler?.ResetInput();
    }

    /// <summary>
    /// Checks if the player is on a wall.
    /// </summary>
    public bool OnWall()
    {
        return CheckOnWall();
    }

    /// <summary>
    /// Returns true if the player can attack (not on wall).
    /// </summary>
    public bool CanAttack()
    {
        return !IsOnWall;
    }

    /// <summary>
    /// Resets the coyote counter.
    /// </summary>
    public void ResetCoyoteCounter()
    {
        _coyoteCounter = 0;
    }

    /// <summary>
    /// Gets the current velocity of the player.
    /// </summary>
    public Vector2 GetVelocity()
    {
        return _rigidbody2D.velocity;
    }

    /// <summary>
    /// Unity OnDisable callback. Resets animation states.
    /// </summary>
    void OnDisable()
    {
        _animator.SetBool(AnimatorGrounded, true);
        _animator.SetBool(AnimatorRunning, false);
    }

    /// <summary>
    /// Gets the current facing direction (1 or -1).
    /// </summary>
    public int GetFacingDirection()
    {
        return _facingDirection;
    }

    /// <summary>
    /// Gets the current horizontal input value.
    /// </summary>
    /// <returns>Current horizontal input (-1 to 1).</returns>
    public float GetCurrentHorizontalInput()
    {
        return _inputHandler?.HorizontalInput ?? 0f;
    }

    /// <summary>
    /// Resets jump counter when grounded.
    /// </summary>
    public void ResetJumpCounter()
    {
        if (IsGrounded && _rigidbody2D.velocity.y == 0)
        {
            _jumpCounter = _extraJumps + 1;
            _wallJumpCooldown = 0;
        }
    }

    /// <summary>
    /// Gets the current wall jump velocity for movement decay.
    /// 
    /// This method calculates the decaying horizontal velocity after a wall jump.
    /// The velocity starts at the initial wall jump force and gradually decreases
    /// over the wall jump cooldown duration, creating a smooth deceleration effect.
    /// 
    /// The decay curve:
    /// - Starts at 100% of initial velocity
    /// - Ends at 60% of initial velocity
    /// - Uses linear interpolation for smooth transition
    /// 
    /// Returns 0 if the player is not in the after-wall-jump state.
    /// </summary>
    /// <returns>The current wall jump velocity (decaying over time).</returns>
    public float GetWallJumpVelocity()
    {
        if (!_afterWallJump)
            return 0f;

        // Calculate time elapsed since wall jump
        float elapsed = Time.time - _wallJumpStartTime;
        float progress = elapsed / WallJumpCooldownDuration; // 0 to 1

        // Decay from full force to 60% force over the cooldown duration
        return Mathf.Lerp(_initialWallJumpVelocity, _initialWallJumpVelocity * 0.6f, progress);
    }

    // ==================== Power-Up Integration ====================
    /// <summary>
    /// Activates a jump power-up, increasing jumps and jump power.
    /// </summary>
    /// <param name="bonusJumps">Number of extra jumps to add.</param>
    /// <param name="bonusJumpPower">Amount to add to jump power.</param>
    public void ActivatePowerUp(int bonusJumps, float bonusJumpPower)
    {
        if (!_hasPowerUp)
        {
            _extraJumps += bonusJumps;
            _jumpPower += bonusJumpPower;
            _jumpCounter++;
            _hasPowerUp = true;
        }
        if (_earsSlot != null && _earsPrefab != null)
        {
            _equippedEars = Instantiate(_earsPrefab, _earsSlot.position, Quaternion.identity, _earsSlot);
        }
    }

    /// <summary>
    /// Removes the jump power-up and resets jumps and jump power.
    /// </summary>
    public void LosePowerUp()
    {
        if (_hasPowerUp)
        {
            _extraJumps = _baseExtraJumps;
            _jumpPower = _baseJumpPower;
            _hasPowerUp = false;
        }
        if (_equippedEars != null)
        {
            Destroy(_equippedEars);
        }
    }

    // ==================== Recoil System ====================
    /// <summary>
    /// Applies recoil to the player from a source position or direction using default recoil values.
    /// 
    /// This is a convenience overload that uses the default recoil settings.
    /// For custom recoil values, use the overload with force and duration parameters.
    /// </summary>
    /// <param name="sourcePosition">Position of the source of recoil (e.g., enemy position).</param>
    /// <param name="recoilDirection">Optional specific direction for recoil (if not provided, calculated from source).</param>
    public void Recoil(Vector2 sourcePosition, Vector2 recoilDirection = default)
    {
        Recoil(sourcePosition, recoilDirection, _recoilForce, _recoilVerticalForce, _recoilDuration);
    }
    
    /// <summary>
    /// Applies recoil to the player from a source position or direction with custom force and duration.
    /// 
    /// This method calculates the recoil direction and starts the recoil effect:
    /// 1. If a specific recoil direction is provided, use it
    /// 2. Otherwise, calculate direction from source position to player
    /// 3. Ensure minimum horizontal knockback for consistent recoil feel
    /// 4. Start the recoil effect with the calculated direction and custom parameters
    /// 
    /// The recoil system provides visual and gameplay feedback when the player takes damage,
    /// pushing them away from the damage source and temporarily disabling movement input.
    /// Different enemies can specify different recoil forces and durations for varied gameplay.
    /// </summary>
    /// <param name="sourcePosition">Position of the source of recoil (e.g., enemy position).</param>
    /// <param name="recoilDirection">Optional specific direction for recoil (if not provided, calculated from source).</param>
    /// <param name="horizontalForce">Custom horizontal recoil force.</param>
    /// <param name="verticalForce">Custom vertical recoil force.</param>
    /// <param name="duration">Custom duration of the recoil effect.</param>
    public void Recoil(Vector2 sourcePosition, Vector2 recoilDirection, float horizontalForce, float verticalForce, float duration)
    {
        Vector2 knockbackDirection;
        _afterRecoil = true;
        
        // Determine recoil direction
        if (recoilDirection != Vector2.zero)
        {
            // Use provided direction
            knockbackDirection = recoilDirection.normalized;
        }
        else
        {
            // Calculate direction from source to player
            Vector2 playerPosition = transform.position;
            knockbackDirection = (playerPosition - sourcePosition).normalized;
        }
        
        // Ensure minimum horizontal knockback for consistent feel
        if (Mathf.Abs(knockbackDirection.x) < MinHorizontalKnockback)
        {
            knockbackDirection.x = _facingDirection == 1 ? DefaultHorizontalKnockback : -DefaultHorizontalKnockback;
        }
        
        StartRecoil(knockbackDirection, horizontalForce, verticalForce, duration);
    }

    /// <summary>
    /// Starts the recoil effect with a given direction using default recoil values.
    /// </summary>
    /// <param name="direction">Direction of the recoil.</param>
    private void StartRecoil(Vector2 direction)
    {
        StartRecoil(direction, _recoilForce, _recoilVerticalForce, _recoilDuration);
    }
    
    /// <summary>
    /// Starts the recoil effect with a given direction and custom force/duration values.
    /// 
    /// This method initializes the recoil state and applies the recoil velocity to the player.
    /// The recoil effect temporarily disables movement input and applies a knockback force
    /// in the specified direction with the given force values.
    /// </summary>
    /// <param name="direction">Direction of the recoil.</param>
    /// <param name="horizontalForce">Horizontal recoil force to apply.</param>
    /// <param name="verticalForce">Vertical recoil force to apply.</param>
    /// <param name="duration">Duration of the recoil effect.</param>
    private void StartRecoil(Vector2 direction, float horizontalForce, float verticalForce, float duration)
    {
        _isInRecoil = true;
        _recoilTimer = duration;
        _currentRecoilDuration = duration; // Store current duration for threshold calculations
        _recoilDirection = direction;
        _disableMovementTimer = duration; // Disable input until recoil finishes
        
        // Apply recoil velocity using custom forces
        Vector2 recoilVelocity = new Vector2(
            direction.x * horizontalForce,
            Mathf.Max(direction.y * horizontalForce, verticalForce)
        );
        _rigidbody2D.velocity = recoilVelocity;
    }

    /// <summary>
    /// Handles the recoil state and disables movement during recoil.
    /// 
    /// This method updates the recoil timer and manages the recoil state.
    /// The recoil effect automatically ends when the timer reaches zero.
    /// </summary>
    private void HandleRecoil()
    {
        if (_isInRecoil)
        {
            _recoilTimer -= Time.deltaTime;
            
            // End recoil when timer expires
            if (_recoilTimer <= 0f)
            {
                _isInRecoil = false;
            }
        }
    }
}
