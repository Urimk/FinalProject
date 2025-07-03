using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player movement, jumping, wall interaction, recoil, and power-ups for both player and AI control.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultGroundCheckWidth = 1.0f;
    private const float DefaultGroundCheckHeight = 0.1f;
    private const float DefaultNormalGravity = 2f;
    private const float DefaultMaxFallSpeed = 100f;
    private const float DefaultRecoilForce = 10f;
    private const float DefaultRecoilDuration = 0.3f;
    private const float DefaultRecoilVerticalForce = 5f;
    private const float DefaultGroundedGraceTime = 0.5f;
    private const float WallJumpCooldownDuration = 0.24f;
    private const float WallJumpGravityScale = 6f;
    private const float WallJumpHorizontalForce = 10f;
    private const float WallJumpVerticalForce = 12f;
    private const float WallCheckBoxCastDistance = 0.015f;
    private const float WallCheckBoxCastHeightIncrease = 0.025f;
    private const float WallCheckBoxCastCenterShift = 0.025f / 2f;
    private const float EarsWalkJumpX = 0.15f;
    private const float EarsAttack01X = 0.25f;
    private const float EarsAttack02X = 0.3f;
    private const float EarsAttack02Y = -0.05f;
    private const float EarsDefaultX = 0.05f;
    private const float EarsDefaultY = 0f;
    private const float SpriteFlipThreshold = 0.01f;
    private const float SmallBumpLift = 0.001f;
    private const float MinHorizontalKnockback = 0.3f;
    private const float DefaultHorizontalKnockback = 0.5f;
    private const float DisableMovementDuringRecoil = 0.08f;
    private const float RecoilInfluenceThreshold = 0.7f;
    private const string AnimatorRunning = "running";
    private const string AnimatorGrounded = "grounded";
    private const string AnimatorJump = "jump";
    private const KeyCode JumpKey = KeyCode.Space;

    // ==================== Inspector Fields ====================
    [Tooltip("True if this player is AI controlled.")]
    [FormerlySerializedAs("isAIControlled")]
    [SerializeField] private bool _isAIControlled;
    [Tooltip("Movement speed of the player.")]
    [FormerlySerializedAs("speed")]
    [SerializeField] private float _speed;
    [Tooltip("Base jump power for the player.")]
    [FormerlySerializedAs("baseJumpPower")]
    [SerializeField] private float _baseJumpPower;
    [Tooltip("Layer mask for ground detection.")]
    [FormerlySerializedAs("groundLayer")]
    [SerializeField] private LayerMask _groundLayer;
    [Tooltip("Layer mask for wall detection.")]
    [FormerlySerializedAs("wallLayer")]
    [SerializeField] private LayerMask _wallLayer;
    [Tooltip("Layer mask for obstacle detection.")]
    [FormerlySerializedAs("obstacleLayer")]
    [SerializeField] private LayerMask _obstacleLayer;
    [Tooltip("Layer mask for default detection.")]
    [FormerlySerializedAs("defaultLayer")]
    [SerializeField] private LayerMask _defaultLayer;
    [Tooltip("Transform slot for equipped ears.")]
    [FormerlySerializedAs("earsSlot")]
    [SerializeField] private Transform _earsSlot;
    [Tooltip("Prefab for the ears power-up.")]
    [FormerlySerializedAs("earsPrefab")]
    [SerializeField] private GameObject _earsPrefab;
    [Tooltip("Width of the ground check box.")]
    [FormerlySerializedAs("groundCheckWidth")]
    [SerializeField] private float _groundCheckWidth = DefaultGroundCheckWidth;
    [Tooltip("Height of the ground check box.")]
    [FormerlySerializedAs("groundCheckHeight")]
    [SerializeField] private float _groundCheckHeight = DefaultGroundCheckHeight;
    [Header("Sounds")]
    [Tooltip("Sound to play when jumping.")]
    [FormerlySerializedAs("jumpSound")]
    [SerializeField] private AudioClip _jumpSound;
    [Header("Coyote Time")]
    [Tooltip("Duration of coyote time (extra jump window).")]
    [FormerlySerializedAs("coyoteTime")]
    [SerializeField] private float _coyoteTime;
    [Tooltip("Grace time for being considered grounded.")]
    [FormerlySerializedAs("groundedGraceTime")]
    [SerializeField] private float _groundedGraceTime = DefaultGroundedGraceTime;
    [Header("Multiple Jumps")]
    [Tooltip("Base number of extra jumps allowed.")]
    [FormerlySerializedAs("baseExtraJumps")]
    [SerializeField] private int _baseExtraJumps;
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

    // ==================== Public Properties ====================
    /// <summary>Transform for ground check (was public field).</summary>
    public Transform GroundCheck { get; set; }
    /// <summary>Normal gravity value (was public field).</summary>
    public float NormalGrav { get; set; } = DefaultNormalGravity;
    /// <summary>Maximum fall speed (was public field).</summary>
    public float MaxFallSpeed { get; set; } = DefaultMaxFallSpeed;
    /// <summary>Singleton instance of PlayerMovement.</summary>
    public static PlayerMovement Instance { get; private set; }

    // ==================== Private Fields ====================
    private SpriteRenderer _playerSpriteRenderer;
    private int _facingDirection = 1;
    private GameObject _equippedEars;
    private int _extraJumps;
    private float _jumpPower;
    private bool _hasPowerUp = false;
    private int _jumpCounter;
    private Rigidbody2D _rigidbody2D;
    private Animator _animator;
    private BoxCollider2D _boxCollider;
    private float _horizontalInput;
    private float _coyoteCounter;
    private float _wallJumpCooldown;
    private float _disableMovementTimer;
    private float _timeSinceGrounded;
    private bool _isInRecoil = false;
    private float _recoilTimer = 0f;
    private Vector2 _recoilDirection;
    private Coroutine _currentAIJumpRoutine = null;

    // ==================== Properties ====================
    /// <summary>Movement speed of the player.</summary>
    public float Speed { get => _speed; set => _speed = value; }
    /// <summary>Jump power of the player.</summary>
    public float JumpPower { get => _jumpPower; set => _jumpPower = value; }
    /// <summary>Layer mask for ground detection.</summary>
    public LayerMask GroundLayer { get => _groundLayer; set => _groundLayer = value; }

    /// <summary>
    /// Unity Awake callback. Initializes singleton and components.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        _jumpPower = _baseJumpPower;
        _extraJumps = _baseExtraJumps;
        InitializeComponents();
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
    /// Unity Update callback. Handles all per-frame logic.
    /// </summary>
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

    /// <summary>
    /// Updates the position of the equipped ears based on the current sprite.
    /// </summary>
    private void UpdateEarsPosition()
    {
        if (_playerSpriteRenderer.sprite != null)
        {
            string spriteName = _playerSpriteRenderer.sprite.name;
            Vector3 newPosition;
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

    // ==================== Movement and Input Logic ====================
    /// <summary>
    /// Updates timers for movement and grounded state.
    /// </summary>
    private void UpdateTimers()
    {
        if (_disableMovementTimer > 0)
        {
            _disableMovementTimer -= Time.deltaTime;
        }
        _timeSinceGrounded = IsGrounded() ? 0 : _timeSinceGrounded + Time.deltaTime;
    }

    /// <summary>
    /// Handles player or AI movement input and applies velocity.
    /// </summary>
    private void HandleMovementInput()
    {
        if (!_isAIControlled && _disableMovementTimer <= 0)
        {
            _horizontalInput = Input.GetAxis("Horizontal");
        }
        if (_disableMovementTimer <= 0 && !IsHorizontallyBlocked())
        {
            if (Mathf.Abs(_horizontalInput) > SpriteFlipThreshold)
            {
                transform.position += new Vector3(0f, SmallBumpLift, 0f);
            }
            _rigidbody2D.velocity = new Vector2(_horizontalInput * _speed, _rigidbody2D.velocity.y);
        }
        else if (IsHorizontallyBlocked() && !IsGrounded())
        {
            _rigidbody2D.velocity = new Vector2(0f, _rigidbody2D.velocity.y);
        }
        FlipSprite();
    }

    /// <summary>
    /// Allows AI to set movement input.
    /// </summary>
    /// <param name="moveDirection">The direction to move (-1, 0, 1).</param>
    public void SetAIInput(float moveDirection)
    {
        if (_isAIControlled && _disableMovementTimer <= 0)
        {
            _horizontalInput = moveDirection;
        }
    }

    /// <summary>
    /// Checks if the player is blocked horizontally by an obstacle.
    /// </summary>
    private bool IsHorizontallyBlocked()
    {
        if (_isInRecoil && _recoilTimer > _recoilDuration * RecoilInfluenceThreshold)
        {
            return false;
        }
        if (Mathf.Approximately(_horizontalInput, 0f))
            return false;
        Vector2 checkDirection = _facingDirection == 1 ? Vector2.right : Vector2.left;
        Vector2 size = _boxCollider.bounds.size;
        size.y += WallCheckBoxCastHeightIncrease;
        Vector2 center = _boxCollider.bounds.center + new Vector3(0, WallCheckBoxCastCenterShift, 0);
        RaycastHit2D hit = Physics2D.BoxCast(
            center,
            size,
            0f,
            checkDirection,
            WallCheckBoxCastDistance,
            _obstacleLayer | _wallLayer | _groundLayer
        );
        if (hit.collider != null)
        {
            FallingPlatform fallingPlatform = hit.collider.GetComponent<FallingPlatform>();
            if (fallingPlatform != null)
            {
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Flips the player sprite based on movement direction.
    /// </summary>
    private void FlipSprite()
    {
        if (_horizontalInput > SpriteFlipThreshold)
        {
            transform.localScale = Vector3.one;
            _facingDirection = 1;
        }
        else if (_horizontalInput < -SpriteFlipThreshold)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            _facingDirection = -1;
        }
    }

    /// <summary>
    /// Updates animation parameters for running and grounded states.
    /// </summary>
    private void UpdateAnimationParameters()
    {
        _animator.SetBool(AnimatorRunning, _horizontalInput != 0);
        _animator.SetBool(AnimatorGrounded, IsGrounded());
    }

    /// <summary>
    /// Updates gravity and wall interaction logic.
    /// </summary>
    private void UpdateGravityAndWallInteraction()
    {
        if (_wallJumpCooldown > WallJumpCooldownDuration)
        {
            HandleWallInteraction();
        }
        else
        {
            _wallJumpCooldown += Time.deltaTime;
        }
        if (_rigidbody2D.velocity.y < -MaxFallSpeed)
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, -MaxFallSpeed);
        }
    }

    /// <summary>
    /// Resets extra jumps when grounded.
    /// </summary>
    private void ResetExtraJumps()
    {
        if (IsGrounded() && _rigidbody2D.velocity.y == 0)
        {
            _jumpCounter = _extraJumps + 1;
            _wallJumpCooldown = 0;
        }
    }

    /// <summary>
    /// Handles wall interaction and gravity scaling.
    /// </summary>
    private void HandleWallInteraction()
    {
        _wallJumpCooldown += Time.deltaTime;
        if (OnWall() && _horizontalInput != 0 && !IsGrounded())
        {
            if (_timeSinceGrounded > _groundedGraceTime)
            {
                _rigidbody2D.gravityScale = WallJumpGravityScale;
                _rigidbody2D.velocity = Vector2.zero;
            }
            else
            {
                _rigidbody2D.gravityScale = NormalGrav;
                _rigidbody2D.velocity = new Vector2(0, _rigidbody2D.velocity.y);
            }
        }
        else
        {
            _rigidbody2D.gravityScale = NormalGrav;
            ManageCoyoteTime();
        }
    }

    /// <summary>
    /// Manages coyote time for jump forgiveness.
    /// </summary>
    private void ManageCoyoteTime()
    {
        if (IsGrounded())
        {
            _coyoteCounter = _coyoteTime;
        }
        else if (_coyoteCounter > 0)
        {
            _coyoteCounter -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Handles jump input for player or AI.
    /// </summary>
    private void HandleJumpInput()
    {
        if (!_isAIControlled)
        {
            if (Input.GetKeyDown(JumpKey))
            {
                AttemptJump();
            }
            if (Input.GetKeyUp(JumpKey) && _rigidbody2D.velocity.y > 0)
            {
                AdjustJumpHeight();
            }
        }
    }

    // ==================== Jump, Power-Up, and Recoil Logic ====================
    /// <summary>
    /// Starts an AI jump routine for a given duration.
    /// </summary>
    /// <param name="jumpDuration">Duration to hold the jump (seconds).</param>
    public void SetAIJump(float jumpDuration)
    {
        if (_isAIControlled)
        {
            if (_currentAIJumpRoutine != null || jumpDuration <= 0)
            {
                return;
            }
            _currentAIJumpRoutine = StartCoroutine(AIJumpRoutine(jumpDuration));
        }
    }

    /// <summary>
    /// Coroutine for simulating AI jump hold duration.
    /// </summary>
    /// <param name="jumpDuration">Duration to hold the jump (seconds).</param>
    private IEnumerator AIJumpRoutine(float jumpDuration)
    {
        bool wasGroundJump = AttemptJump();
        if (wasGroundJump)
        {
            yield return new WaitForSeconds(jumpDuration);
            if (_rigidbody2D.velocity.y > 0)
            {
                AdjustJumpHeight();
            }
            _currentAIJumpRoutine = null;
        }
    }

    /// <summary>
    /// Resets state, including stopping AI jump coroutine.
    /// </summary>
    public void ResetState()
    {
        if (_currentAIJumpRoutine != null)
        {
            StopCoroutine(_currentAIJumpRoutine);
            _currentAIJumpRoutine = null;
        }
        // ... other reset logic ...
    }

    /// <summary>
    /// Attempts to perform a jump, returns true if ground jump.
    /// </summary>
    private bool AttemptJump()
    {
        if (UIManager.Instance.IsGamePaused())
        {
            return false;
        }
        bool wasGroundJump = Jump();
        if (IsGrounded())
        {
            SoundManager.instance.PlaySound(_jumpSound, gameObject);
        }
        return wasGroundJump;
    }

    /// <summary>
    /// Adjusts jump height if jump key is released early.
    /// </summary>
    private void AdjustJumpHeight()
    {
        _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _rigidbody2D.velocity.y / 2);
        _coyoteCounter = 0;
    }

    /// <summary>
    /// Handles jump logic, including coyote time and wall jumps.
    /// </summary>
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
        if (OnWall() && !IsGrounded() && _timeSinceGrounded > _groundedGraceTime && _wallJumpCooldown > 0.05f)
        {
            PerformWallJump();
        }
        else if (_coyoteCounter > 0)
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, _jumpPower);
            _coyoteCounter = 0;
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

    /// <summary>
    /// Performs a wall jump.
    /// </summary>
    private void PerformWallJump()
    {
        _wallJumpCooldown = 0;
        _disableMovementTimer = WallJumpCooldownDuration;
        _rigidbody2D.gravityScale = WallJumpGravityScale;
        _horizontalInput = 0;
        _rigidbody2D.velocity = new Vector2(-Mathf.Sign(transform.localScale.x) * WallJumpHorizontalForce, WallJumpVerticalForce);
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
        _facingDirection = -_facingDirection;
        _jumpCounter--;
    }

    // ==================== State and Utility Methods ====================
    /// <summary>
    /// Checks if the player is grounded using an OverlapBox.
    /// </summary>
    public bool IsGrounded()
    {
        Vector2 boxCenter = GroundCheck.position;
        Vector2 boxSize = new Vector2(_groundCheckWidth, _groundCheckHeight);
        return Physics2D.OverlapBox(boxCenter, boxSize, 0f, _groundLayer | _obstacleLayer);
    }

    /// <summary>
    /// Checks if the player is on a wall using a BoxCast.
    /// </summary>
    public bool OnWall()
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

    /// <summary>
    /// Returns true if the player can attack (not on wall).
    /// </summary>
    public bool CanAttack()
    {
        return !OnWall();
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

    /// <summary>
    /// Applies recoil to the player from a source position or direction.
    /// </summary>
    /// <param name="sourcePosition">Position of the source of recoil.</param>
    /// <param name="recoilDirection">Optional direction for recoil.</param>
    public void Recoil(Vector2 sourcePosition, Vector2 recoilDirection = default)
    {
        Vector2 knockbackDirection;
        if (recoilDirection != Vector2.zero)
        {
            knockbackDirection = recoilDirection.normalized;
        }
        else
        {
            Vector2 playerPosition = transform.position;
            knockbackDirection = (playerPosition - sourcePosition).normalized;
        }
        if (Mathf.Abs(knockbackDirection.x) < MinHorizontalKnockback)
        {
            knockbackDirection.x = _facingDirection == 1 ? DefaultHorizontalKnockback : -DefaultHorizontalKnockback;
        }
        StartRecoil(knockbackDirection);
    }

    /// <summary>
    /// Starts the recoil effect with a given direction.
    /// </summary>
    /// <param name="direction">Direction of the recoil.</param>
    private void StartRecoil(Vector2 direction)
    {
        _isInRecoil = true;
        _recoilTimer = _recoilDuration;
        _recoilDirection = direction;
        Vector2 recoilVelocity = new Vector2(
            direction.x * _recoilForce,
            Mathf.Max(direction.y * _recoilForce, _recoilVerticalForce)
        );
        _rigidbody2D.velocity = recoilVelocity;
    }

    /// <summary>
    /// Handles the recoil state and disables movement during recoil.
    /// </summary>
    private void HandleRecoil()
    {
        if (_isInRecoil)
        {
            _recoilTimer -= Time.deltaTime;
            float recoilStrength = _recoilTimer / _recoilDuration;
            if (_recoilTimer <= 0f)
            {
                _isInRecoil = false;
            }
            else
            {
                _disableMovementTimer = DisableMovementDuringRecoil;
                return;
            }
        }
    }
}
