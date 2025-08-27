using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI-controlled boss behavior that inherits from BossEnemy and adds Q-Learning capabilities.
/// </summary>
public class BossAI : BossEnemy
{
    // ==================== AI Mode Constants ====================
    private const float AIAttackCooldown = 4f;
    private const float AIFireAttackCooldown = 4f;
    private const float AIDashCooldown = 8f;
    private const float EnergyBarYOffset = 2.5f;
    private const float TargetReachedThresholdSqr = 0.25f;
    private const float WallBuffer = 3.5f;
    private const float DashMaxDuration = 1.5f;
    private const float FireballAimDistance = 100f;
    private const float DashBoundaryCheckDistance = 0.5f;
    private const float AIDashDuration = 0.4f; // Faster dash for AI
    private const float DashTargetProximity = 1.0f; // Distance to target to stop dashing

    // ==================== AI Mode Inspector Fields ====================
    [Header("AI Mode Settings")]
    [Tooltip("Reference to the BossRewardManager (required for AI mode).")]
    [SerializeField] private BossRewardManager _rewardManager;
    [Tooltip("Reference to the boss's energy UI slider (for AI mode).")]
    [SerializeField] private Slider _energySlider;
    [Tooltip("Reference to the left wall Transform (for AI movement).")]
    [SerializeField] private Transform _leftWall;
    [Tooltip("Reference to the right wall Transform (for AI movement).")]
    [SerializeField] private Transform _rightWall;
    [Tooltip("Reference to the ground Transform (for boundary checking).")]
    [SerializeField] private Transform _ground;
    [Tooltip("Reference to the ceiling Transform (for boundary checking).")]
    [SerializeField] private Transform _ceiling;
    [Tooltip("Center position of the boss arena (for AI movement).")]
    [SerializeField] private Vector3 _arenaCenterPosition = Vector3.zero;
    [Tooltip("Offset distance for boss actions (for AI mode).")]
    [SerializeField] private float _actionDistanceOffset = 3.0f;
    [Tooltip("Y level for placing flame traps (for AI mode).")]
    [SerializeField] private float _flameTrapGroundYLevel = -11.5f;
    [Tooltip("Distance for dash attacks (for AI mode).")]
    [SerializeField] private float _dashDistance = 6.0f;
    [Tooltip("How far ahead (in seconds) to predict player movement for aiming fireballs (AI mode).")]
    [SerializeField] private float _predictionTime = 0.3f;

    [Header("Energy System (AI Mode Only)")]
    [Tooltip("Maximum energy for the boss (AI mode only).")]
    [SerializeField] private float _maxEnergy = 100f;
    [Tooltip("Energy regeneration rate per second (AI mode only).")]
    [SerializeField] private float _energyRegenRate = 5f;
    [Tooltip("Energy cost for firing a fireball (AI mode only).")]
    [SerializeField] private float _fireballEnergyCost = 10f;
    [Tooltip("Energy cost for using a flame trap (AI mode only).")]
    [SerializeField] private float _flameTrapEnergyCost = 25f;
    [Tooltip("Energy cost for dashing (AI mode only).")]
    [SerializeField] private float _dashEnergyCost = 35f;

    // ==================== AI Mode Private Fields ====================
    private float _currentEnergy;
    private bool _dashMissed = true;
    private bool _flameMissed = true;
    private bool _isFlameDeactivationCanceled = false;
    private PlayerMovement _playerMovement;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Awake callback. Initializes AI-specific components.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        InitializeAIComponents();
    }

    /// <summary>
    /// Unity Start callback. Initializes AI state.
    /// </summary>
    protected override void Start()
    {
        base.Start();
        InitializeAIState();
    }

    // ==================== AI Initialization ====================
    /// <summary>
    /// Initializes AI-specific components and references.
    /// </summary>
    private void InitializeAIComponents()
    {
        // Find player if not assigned
        if (Players == null || Players.Count == 0)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Players = new List<Transform> { playerObj.transform };
                _playerMovement = playerObj.GetComponent<PlayerMovement>();
            }
        }
        else if (Players.Count > 0)
        {
            _playerMovement = Players[0].GetComponent<PlayerMovement>();
        }

        // Initialize energy
        _currentEnergy = _maxEnergy;
        UpdateEnergyBar();

        // Set AI mode cooldowns
        SetAttackCooldown(AIAttackCooldown);
        SetFireAttackCooldown(AIFireAttackCooldown);
        SetDashCooldown(AIDashCooldown);
    }

    /// <summary>
    /// Initializes AI-specific state.
    /// </summary>
    private void InitializeAIState()
    {
        ValidateAIReferences();
    }

    /// <summary>
    /// Validates AI mode specific references.
    /// </summary>
    private void ValidateAIReferences()
    {
        if (_rewardManager == null)
        {
            DebugManager.LogError(DebugCategory.Enemy, "BossRewardManager not assigned! Learning will fail.");
        }
        if (Players == null || Players.Count == 0)
        {
            DebugManager.LogError(DebugCategory.Enemy, "No player references found!");
        }
        if (_leftWall == null || _rightWall == null)
        {
            DebugManager.LogWarning(DebugCategory.Enemy, "Wall references not assigned - movement may be limited.");
        }
        if (_ground == null || _ceiling == null)
        {
            DebugManager.LogWarning(DebugCategory.Enemy, "Ground/Ceiling references not assigned - boundary checking may not work properly.");
        }
    }

    // ==================== AI State Management ====================
    /// <summary>
    /// Resets AI mode specific state (energy, attack tracking, etc.).
    /// </summary>
    public override void ResetState()
    {
        base.ResetState();
        ResetAIState();
    }

    /// <summary>
    /// Resets AI mode specific state.
    /// </summary>
    private void ResetAIState()
    {
        // Reset energy
        _currentEnergy = _maxEnergy;
        UpdateEnergyBar();

        // Reset attack tracking
        _dashMissed = true;
        _flameMissed = true;
        _isFlameDeactivationCanceled = false;

        // Reset cooldowns to AI mode values
        SetAttackCooldown(AIAttackCooldown);
        SetFireAttackCooldown(AIFireAttackCooldown);
        SetDashCooldown(AIDashCooldown);

        DebugManager.Log(DebugCategory.Enemy, "AI state reset - Energy: " + _currentEnergy + "/" + _maxEnergy);
    }

    // ==================== AI Update Logic ====================
    /// <summary>
    /// Override Update to prevent automatic player aiming and movement - AI makes its own decisions.
    /// </summary>
    protected override void Update()
    {
        if (_isDead) return;
        
        // Fix rotation bug - ensure boss doesn't rotate
        transform.rotation = Quaternion.Euler(0, 0, 0);
        
        // Only update player detection and phase transition
        // Skip automatic movement, facing direction, and attack logic - AI handles these
        UpdatePlayerDetection();
        UpdatePhaseTransition();
        UpdateCooldowns();
        
        // AI-specific updates
        UpdateAI();
        
        // Ensure no movement during charging (but allow movement during dashing)
        if (_isChargingDash)
        {
            if (_rb != null) _rb.velocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Override to prevent automatic movement - AI controls all movement.
    /// </summary>
    protected override void UpdateMovement()
    {
        // AI controls all movement through AIRequestMove methods
        // No automatic movement here
    }

    /// <summary>
    /// Updates AI-specific logic.
    /// </summary>
    private void UpdateAI()
    {
        // Energy regeneration
        if (_currentEnergy < _maxEnergy)
        {
            _currentEnergy = Mathf.Min(_maxEnergy, _currentEnergy + _energyRegenRate * Time.deltaTime);
            UpdateEnergyBar();
        }
        
        // Update facing direction based on current velocity (AI controls movement)
        UpdateFacingDirection();
    }
    
    /// <summary>
    /// Updates boss facing direction based on current velocity (for AI-controlled movement).
    /// </summary>
    private void UpdateFacingDirection()
    {
        Vector2 currentVelocity = GetVelocity();
        if (Mathf.Abs(currentVelocity.x) > 0.1f)
        {
            bool shouldFaceLeft = currentVelocity.x < 0;
            float scaleX = Mathf.Abs(transform.localScale.x) * (shouldFaceLeft ? 1f : -1f);
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }
        else if (Players != null && Players.Count > 0)
        {
            // If not moving, face the player
            Transform closestPlayer = GetClosestPlayer();
            if (closestPlayer != null)
            {
                bool shouldFaceLeft = closestPlayer.position.x < transform.position.x;
                float scaleX = Mathf.Abs(transform.localScale.x) * (shouldFaceLeft ? 1f : -1f);
                transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            }
        }
        
        // Update fireball holder scale
        if (_fireballHolder != null)
        {
            _fireballHolder.localScale = transform.localScale;
        }
    }

    /// <summary>
    /// Updates the boss's energy bar UI.
    /// </summary>
    private void UpdateEnergyBar()
    {
        if (_energySlider != null)
        {
            _energySlider.value = _currentEnergy / _maxEnergy;
        }
    }

    /// <summary>
    /// LateUpdate for AI mode UI positioning.
    /// </summary>
    private void LateUpdate()
    {
        if (_energySlider != null)
        {
            _energySlider.transform.position = transform.position + new Vector3(0, EnergyBarYOffset, 0);
            _energySlider.transform.rotation = Quaternion.identity;
        }
    }

    // ==================== AI Public Interface ====================
    /// <summary>
    /// Gets the BossRewardManager reference.
    /// </summary>
    public BossRewardManager RewardManager => _rewardManager;

    /// <summary>
    /// Gets the boss's current energy as a normalized value (0 to 1).
    /// </summary>
    public float GetCurrentEnergyNormalized()
    {
        if (_maxEnergy <= 0) return 1.0f;
        return Mathf.Clamp01(_currentEnergy / _maxEnergy);
    }

    /// <summary>
    /// Returns whether the player is currently grounded.
    /// </summary>
    public bool IsPlayerGrounded()
    {
        if (_playerMovement != null)
        {
            return _playerMovement.IsGrounded;
        }
        DebugManager.LogWarning(DebugCategory.Enemy, "Cannot check player grounded status: PlayerMovement reference missing.");
        return true;
    }

    /// <summary>
    /// Gets or sets whether the flame attack missed (for BossFlameAttack interaction).
    /// </summary>
    public bool FlameMissed { get => _flameMissed; set => _flameMissed = value; }

    /// <summary>
    /// Gets whether the boss is currently dead.
    /// </summary>
    public bool IsDead => _isDead;
    
    /// <summary>
    /// Gets whether the boss is ready for any attack (has energy and is not charging/dashing).
    /// </summary>
    public bool IsReadyForAnyAttack()
    {
        return !IsDead && !IsCurrentlyChargingOrDashing() && _currentEnergy > 0;
    }

    // ==================== AI Action Requests ====================
    /// <summary>
    /// Requests the boss to move according to the specified Q-learning action.
    /// </summary>
    /// <param name="moveAction">The movement action type.</param>
    /// <param name="playerPos">The player's position.</param>
    /// <param name="bossPos">The boss's position.</param>
    /// <param name="offsetDistance">The offset distance for movement.</param>
    public void AIRequestMove(BossQLearning.ActionType moveAction, Vector2 playerPos, Vector2 bossPos, float offsetDistance)
    {
        if (Players == null || Players.Count == 0 || !IsPlayerDetected || IsCurrentlyChargingOrDashing() || IsDead) return;

        Vector2 targetPosition;
        Vector2 moveDirection = Vector2.zero;

        switch (moveAction)
        {
            case BossQLearning.ActionType.Move_TowardsPlayer:
                targetPosition = playerPos;
                moveDirection = (targetPosition - bossPos).normalized;
                break;
            case BossQLearning.ActionType.Move_AwayFromPlayer:
                targetPosition = bossPos + (bossPos - playerPos).normalized * offsetDistance;
                moveDirection = (targetPosition - bossPos).normalized;
                break;
            case BossQLearning.ActionType.Move_StrafeLeft:
                Vector2 dirToPlayer_L = (playerPos - bossPos).normalized;
                moveDirection = new Vector2(-dirToPlayer_L.y, dirToPlayer_L.x);
                targetPosition = bossPos + moveDirection * offsetDistance;
                break;
            case BossQLearning.ActionType.Move_StrafeRight:
                Vector2 dirToPlayer_R = (playerPos - bossPos).normalized;
                moveDirection = new Vector2(dirToPlayer_R.y, -dirToPlayer_R.x);
                targetPosition = bossPos + moveDirection * offsetDistance;
                break;
            case BossQLearning.ActionType.Move_StrafeUp:
                moveDirection = Vector2.up;
                targetPosition = bossPos + moveDirection * offsetDistance;
                break;
            case BossQLearning.ActionType.Move_StrafeDown:
                moveDirection = Vector2.down;
                targetPosition = bossPos + moveDirection * offsetDistance;
                break;
            case BossQLearning.ActionType.Move_ToArenaCenter:
                targetPosition = _arenaCenterPosition;
                moveDirection = (targetPosition - bossPos).normalized;
                break;
            case BossQLearning.ActionType.Move_ToPlayerFlank:
                Vector2 dirToPlayer_Flank = (playerPos - bossPos).normalized;
                Vector2 flankDirection = new Vector2(-dirToPlayer_Flank.y, dirToPlayer_Flank.x);
                targetPosition = playerPos + flankDirection * offsetDistance;
                moveDirection = (targetPosition - bossPos).normalized;
                break;
            default:
                DebugManager.LogWarning(DebugCategory.Enemy, "Received unexpected move action: " + moveAction);
                AIRequestIdle();
                return;
        }

        if ((targetPosition - bossPos).sqrMagnitude > TargetReachedThresholdSqr)
        {
            SetVelocity((targetPosition - bossPos).normalized * MovementSpeed);
        }
        else
        {
            SetVelocity(Vector2.zero);
        }
    }

    /// <summary>
    /// Requests the boss to perform a ranged attack according to the specified Q-learning action.
    /// </summary>
    /// <param name="aimAction">The aiming action type.</param>
    /// <param name="playerPos">The player's position.</param>
    /// <param name="playerVel">The player's velocity.</param>
    /// <param name="offsetDistance">The offset distance for aiming.</param>
    /// <returns>True if the attack was performed, false otherwise.</returns>
    public bool AIRequestRangedAttack(BossQLearning.ActionType aimAction, Vector2 playerPos, Vector2 playerVel, float offsetDistance)
    {
        if (!IsFireballReady() || !IsPlayerDetected || IsCurrentlyChargingOrDashing() || IsDead || Players == null || Players.Count == 0) return false;
        if (_currentEnergy < _fireballEnergyCost) return false;

        Vector2 targetPosition;
        Vector2 bossPos = transform.position;
        float predictionTime = (playerPos - bossPos).magnitude / ProjectileSpeed;
        Vector2 predictedPlayerPos = playerPos + (playerVel * predictionTime);

        switch (aimAction)
        {
            case BossQLearning.ActionType.Fireball_AtCurrentPos:
                targetPosition = playerPos;
                break;
            case BossQLearning.ActionType.Fireball_Predictive:
                targetPosition = predictedPlayerPos;
                break;
            case BossQLearning.ActionType.Fireball_OffsetUp:
                targetPosition = playerPos + Vector2.up * offsetDistance;
                break;
            case BossQLearning.ActionType.Fireball_OffsetDown:
                targetPosition = playerPos + Vector2.down * offsetDistance;
                break;
            case BossQLearning.ActionType.Fireball_OffsetLeft:
                targetPosition = playerPos + Vector2.left * offsetDistance;
                break;
            case BossQLearning.ActionType.Fireball_OffsetRight:
                targetPosition = playerPos + Vector2.right * offsetDistance;
                break;
            case BossQLearning.ActionType.Fireball_PredictiveOffsetUp:
                targetPosition = predictedPlayerPos + Vector2.up * offsetDistance;
                break;
            case BossQLearning.ActionType.Fireball_PredictiveOffsetDown:
                targetPosition = predictedPlayerPos + Vector2.down * offsetDistance;
                break;
            case BossQLearning.ActionType.Fireball_PredictiveOffsetLeft:
                targetPosition = predictedPlayerPos + Vector2.left * offsetDistance;
                break;
            case BossQLearning.ActionType.Fireball_PredictiveOffsetRight:
                targetPosition = predictedPlayerPos + Vector2.right * offsetDistance;
                break;
            case BossQLearning.ActionType.Fireball_RelativeForward:
                Vector2 forwardDirection = (GetVelocity().x > 0.01f) ? Vector2.right : (GetVelocity().x < -0.01f ? Vector2.left : Vector2.right);
                if (forwardDirection == Vector2.zero) forwardDirection = Vector2.right;
                targetPosition = bossPos + forwardDirection * FireballAimDistance;
                break;
            case BossQLearning.ActionType.Fireball_RelativeUp:
                targetPosition = bossPos + Vector2.up * FireballAimDistance;
                break;
            case BossQLearning.ActionType.Fireball_RelativeDown:
                targetPosition = bossPos + Vector2.down * FireballAimDistance;
                break;
            default:
                Debug.LogWarning("[BossAI] Received unexpected fireball aim action: " + aimAction);
                targetPosition = playerPos;
                break;
        }

        bool success = PerformRangedAttack(targetPosition);
        if (success)
        {
            _currentEnergy -= _fireballEnergyCost;
            UpdateEnergyBar();
        }
        return success;
    }

    /// <summary>
    /// Requests the boss to perform a flame attack according to the specified Q-learning action.
    /// </summary>
    /// <param name="placeAction">The flame placement action type.</param>
    /// <param name="playerPos">The player's position.</param>
    /// <param name="bossPos">The boss's position.</param>
    /// <param name="playerVel">The player's velocity.</param>
    /// <param name="offsetDistance">The offset distance for placement.</param>
    /// <returns>True if the attack was performed, false otherwise.</returns>
    public bool AIRequestFlameAttack(BossQLearning.ActionType placeAction, Vector2 playerPos, Vector2 bossPos, Vector2 playerVel, float offsetDistance)
    {
        if (!IsFlameTrapReady() || !IsPlayerDetected || IsCurrentlyChargingOrDashing() || IsDead || Players == null || Players.Count == 0) return false;
        if (_currentEnergy < _flameTrapEnergyCost) return false;

        Vector2 placementPosition;
        switch (placeAction)
        {
            case BossQLearning.ActionType.FlameTrap_AtPlayer:
                placementPosition = new Vector2(playerPos.x, _flameTrapGroundYLevel);
                break;
            case BossQLearning.ActionType.FlameTrap_NearBoss:
                placementPosition = new Vector2(bossPos.x, _flameTrapGroundYLevel);
                break;
            case BossQLearning.ActionType.FlameTrap_BetweenBossAndPlayer:
                placementPosition = new Vector2(Vector2.Lerp(bossPos, playerPos, 0.5f).x, _flameTrapGroundYLevel);
                break;
            case BossQLearning.ActionType.FlameTrap_BehindPlayer:
                Vector2 behindPlayerPos = playerPos - playerVel.normalized * offsetDistance;
                placementPosition = new Vector2(behindPlayerPos.x, _flameTrapGroundYLevel);
                break;
            default:
                Debug.LogWarning("[BossAI] Received unexpected flame trap action: " + placeAction);
                placementPosition = new Vector2(playerPos.x, _flameTrapGroundYLevel);
                break;
        }

        if (_leftWall != null && _rightWall != null)
        {
            placementPosition.x = Mathf.Clamp(placementPosition.x, _leftWall.position.x + WallBuffer, _rightWall.position.x - WallBuffer);
        }

        bool success = PerformFlameAttack(placementPosition);
        if (success)
        {
            _currentEnergy -= _flameTrapEnergyCost;
            UpdateEnergyBar();
        }
        return success;
    }

    /// <summary>
    /// Requests the boss to perform a dash attack according to the specified Q-learning action.
    /// </summary>
    /// <param name="dashAction">The dash action type.</param>
    /// <param name="playerPos">The player's position.</param>
    /// <param name="bossPos">The boss's position.</param>
    /// <param name="offsetDistance">The offset distance for dashing.</param>
    /// <returns>True if the attack was performed, false otherwise.</returns>
    public bool AIRequestDashAttack(BossQLearning.ActionType dashAction, Vector2 playerPos, Vector2 bossPos, float offsetDistance)
    {
        if (!IsDashReady() || !IsPlayerDetected || IsCurrentlyChargingOrDashing() || IsDead || Players == null || Players.Count == 0)
        {
            return false;
        }
        if (_currentEnergy < _dashEnergyCost)
        {
            return false;
        }

        Vector2 dashTargetCalculated;
        switch (dashAction)
        {
            case BossQLearning.ActionType.Dash_TowardsPlayer:
                dashTargetCalculated = playerPos;
                break;
            case BossQLearning.ActionType.Dash_AwayFromPlayer:
                dashTargetCalculated = bossPos + (bossPos - playerPos).normalized * _dashDistance;
                break;
            case BossQLearning.ActionType.Dash_ToPlayerFlank:
                Vector2 dirToPlayer_Flank = (playerPos - bossPos).normalized;
                Vector2 flankDirection = new Vector2(-dirToPlayer_Flank.y, dirToPlayer_Flank.x);
                dashTargetCalculated = playerPos + flankDirection * offsetDistance;
                break;
            default:
                Debug.LogWarning("[BossAI] Received unexpected dash action: " + dashAction);
                dashTargetCalculated = playerPos;
                break;
        }

        // Clamp dash target to room boundaries
        dashTargetCalculated = ClampPositionToBoundaries(dashTargetCalculated);

        bool success = PerformDashAttack(dashTargetCalculated);
        
        if (success)
        {
            _currentEnergy -= _dashEnergyCost;
            UpdateEnergyBar();
        }
        
        return success;
    }

    /// <summary>
    /// Requests the boss to idle (do nothing) for this step.
    /// </summary>
    public void AIRequestIdle()
    {
        if (IsCurrentlyChargingOrDashing() || IsDead) return;
        SetVelocity(Vector2.zero);
    }

    // ==================== AI Collision Handling ====================
    /// <summary>
    /// Handles AI-specific collision logic.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if (other.CompareTag("Player"))
        {
            // AI Mode: Report hit player
            if (_rewardManager != null)
            {
                _rewardManager.ReportHitPlayer();
            }

            // AI Mode: Report successful dash hit
            if (IsDashing)
            {
                _dashMissed = false;
                StopDashing();
            }
        }
        else if (IsDashing && (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Walls")))
        {
            StopDashing();
        }
    }

    // ==================== AI Flame Attack Handling ====================
    /// <summary>
    /// Handles flame attack deactivation for AI mode.
    /// </summary>
    /// <param name="flameObject">The flame object that was deactivated.</param>
    protected override void OnFlameDeactivated(GameObject flameObject)
    {
        base.OnFlameDeactivated(flameObject);

        // AI Mode: Report attack missed if flame attack
        if (_rewardManager != null && flameObject == FlameObject)
        {
            if (_flameMissed)
            {
                _rewardManager.ReportAttackMissed();
            }
            _flameMissed = true;
        }
    }

    // ==================== AI Dash Attack Handling ====================
    /// <summary>
    /// Override to stop the dash coroutine when dashing is stopped.
    /// </summary>
    protected override void StopDashing()
    {
        base.StopDashing();
        
        // Stop the dash coroutine if it's running
        if (_dashCoroutine != null)
        {
            StopCoroutine(_dashCoroutine);
            _dashCoroutine = null;
        }
    }

    /// <summary>
    /// Handles dash attack completion for AI mode.
    /// </summary>
    protected override void OnDashCompleted()
    {
        base.OnDashCompleted();

        // AI Mode: Report dash missed if no collision occurred
        if (_rewardManager != null && _dashMissed && !IsDead)
        {
            _rewardManager.ReportAttackMissed();
        }
        _dashMissed = true;
        
        // Clear the coroutine reference
        _dashCoroutine = null;
    }

    /// <summary>
    /// Override to use AI's own coroutine management and add boundary checking.
    /// </summary>
    protected override bool PerformDashAttack(Vector2 dashTarget)
    {
        if (_isDead || _isChargingDash || _isDashing)
        {
            return false;
        }
        
        _isChargingDash = true;
        _isDashing = false;
        _dashTarget = dashTarget;
        
        CreateOrUpdateTargetIcon();
        
        if (_anim != null)
        {
            _anim.SetTrigger("ChargeDash");
        }
        
        if (_chargeSound != null)
        {
            SoundManager.instance.PlaySound(_chargeSound, gameObject);
        }
        
        // Use AI's own coroutine management
        _dashCoroutine = StartCoroutine(PerformDashAttackCoroutine());
        _dashCooldownTimer = 0f;
        
        return true;
    }

    /// <summary>
    /// AI-specific dash attack coroutine with boundary checking.
    /// </summary>
    private IEnumerator PerformDashAttackCoroutine()
    {
        yield return new WaitForSeconds(_dashChargeTime);
        
        if (_isDead)
        {
            _isChargingDash = false;
            if (_targetIconInstance != null) _targetIconInstance.SetActive(false);
            _dashCoroutine = null;
            yield break;
        }
        
        if (_targetIconInstance != null)
        {
            _targetIconInstance.SetActive(false);
        }
        
        _isChargingDash = false;
        _isDashing = true;
        
        if (_anim != null)
        {
            _anim.SetTrigger("Dash");
        }
        
        if (_dashSound != null)
        {
            SoundManager.instance.PlaySound(_dashSound, gameObject);
        }
        
        // Set velocity once and let physics handle the movement (like the old AIBoss)
        Vector2 direction = (_dashTarget - (Vector2)transform.position).normalized;
        
        if (direction == Vector2.zero && Players != null && Players.Count > 0)
        {
            direction = (Players[0].position - transform.position).normalized;
        }
        
        if (_rb != null)
        {
            Vector2 targetVelocity = direction * _dashSpeed;
            _rb.velocity = targetVelocity;
        }
        else
        {
            Debug.LogError("[BossAI] Rigidbody2D is null!");
        }
        
        // Use a timer-based approach like the old AIBoss
        float dashTimer = 0f;
        
        while (dashTimer < AIDashDuration)
        {
            if (_isDead || !_isDashing)
            {
                break;
            }
            
            // Check for collision ahead
            if (IsCollisionAhead(transform.position, direction, DashBoundaryCheckDistance))
            {
                break;
            }
            
            // Check if we're close to target (like the old AIBoss)
            float distanceToTarget = Vector2.Distance(transform.position, _dashTarget);
            if (distanceToTarget < DashTargetProximity)
            {
                break;
            }
            
            dashTimer += Time.deltaTime;
            yield return null;
        }
        
        // Stop movement
        _isDashing = false;
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
        }
        
        _dashCooldownTimer = 0f;
        
        // Call virtual method for derived classes
        OnDashCompleted();
    }
    
    /// <summary>
    /// Clamps a position to be within the room boundaries.
    /// </summary>
    /// <param name="position">The position to clamp.</param>
    /// <returns>The clamped position within boundaries.</returns>
    private Vector2 ClampPositionToBoundaries(Vector2 position)
    {
        if (_leftWall == null || _rightWall == null || _ground == null || _ceiling == null)
        {
            Debug.LogWarning("[BossAI] Boundary references missing - cannot clamp position");
            return position; // Return original if boundaries not set
        }
        
        float leftBound = _leftWall.position.x + WallBuffer;
        float rightBound = _rightWall.position.x - WallBuffer;
        float bottomBound = _ground.position.y + WallBuffer;
        float topBound = _ceiling.position.y - WallBuffer;
        
        float clampedX = Mathf.Clamp(position.x, leftBound, rightBound);
        float clampedY = Mathf.Clamp(position.y, bottomBound, topBound);
        
        return new Vector2(clampedX, clampedY);
    }

    
    /// <summary>
    /// Checks if there's a collision ahead in the dash direction.
    /// </summary>
    /// <param name="startPosition">The starting position of the dash.</param>
    /// <param name="direction">The direction of the dash.</param>
    /// <param name="distance">The distance to check.</param>
    /// <returns>True if there's a collision ahead, false otherwise.</returns>
    private bool IsCollisionAhead(Vector2 startPosition, Vector2 direction, float distance)
    {
        // Check for walls and ground/ceiling
        LayerMask boundaryLayers = LayerMask.GetMask("Ground", "Walls");
        
        RaycastHit2D hit = Physics2D.Raycast(startPosition, direction, distance, boundaryLayers);
        
        return hit.collider != null;
    }
}
