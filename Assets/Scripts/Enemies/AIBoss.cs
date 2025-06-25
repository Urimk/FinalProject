using System.Collections;

using UnityEngine;
using UnityEngine.UI;

// This script handles the boss's mechanics, state, and execution of actions requested by the Q-learning agent.
public class AIBoss : EnemyDamage, IBoss // Assuming EnemyDamage handles health or similar
{
    [Header("Boss Parameters")]
    [SerializeField] private float _movementSpeed = 3.0f; // Adjusted speed example
    [SerializeField] private bool _doRestHealth;
    [SerializeField] private float _attackRange = 10f;

    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Health _playerHealth;
    [SerializeField] private Slider _energySlider;
    [SerializeField] private Animator _anim;
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] public BossRewardManager rewardManager; // CRUCIAL - Assign in Inspector!
    [SerializeField] private BossHealth _bossHealth; // Reference to the BossHealth script
    [SerializeField] private Transform _firepoint;
    [SerializeField] private Transform _fireballHolder; // Parent for inactive fireballs
    [SerializeField] private GameObject[] _fireballs; // Pool of fireballs
    [SerializeField] private GameObject _flame; // Flame trap object
    [SerializeField] private GameObject _areaMarkerPrefab; // Marker for flame trap
    [SerializeField] private Transform _leftWall; // Boundary for flame trap
    [SerializeField] private Transform _rightWall; // Boundary for flame trap
    [SerializeField] private GameObject _targetIconPrefab; // For dash attack

    [Header("Attack Parameters")]
    [SerializeField] public float attackCooldown = 4f; // Fireball cooldown
    [SerializeField] private int _fireballDamage = 1;
    [SerializeField] private float _projectileSpeed = 5f;
    [SerializeField] private float _projectileSize = 0.3f;
    [SerializeField] private AudioClip _fireballSound;
    [Tooltip("How far ahead (in seconds) to predict player movement for aiming fireballs.")]
    [SerializeField] private float _predictionTime = 0.3f; // Adjust based on projectile speed & player speed

    [Header("Flame Attack Parameters")]
    [SerializeField] public float fireAttackCooldown = 4f;

    [Header("Charge Dash Attack Parameters")]
    [SerializeField] private float _dashChargeTime = 2f; // Slightly faster charge example
    [SerializeField] private float _dashSpeed = 10f; // Slightly faster dash example
    [SerializeField] public float dashCooldown = 8f;
    [SerializeField] private AudioClip _chargeSound;
    [SerializeField] private AudioClip _dashSound;

    [Header("Energy System (Optional - Enable Checks Below)")]
    [SerializeField] private float _maxEnergy = 100f;
    [SerializeField] private float _energyRegenRate = 5f; // Energy per second
    [SerializeField] private float _fireballEnergyCost = 10f;
    [SerializeField] private float _flameTrapEnergyCost = 25;
    [SerializeField] private float _dashEnergyCost = 35f;
    private float _currentEnergy;

    // --- Internal State ---

    //CHANGE!
    private bool _isPhase2 = false;
    private Vector3 _initialBossPosition;
    private bool _detectedPlayer;
    private bool _isChargingDash = false;
    private bool _isDashing = false; // Added to differentiate charging phase from movement phase
    private bool _dashMissed = true;
    public bool flameMissed = true; // Public so BossFlameAttack can set it to false on hit
    private Vector2 _dashTarget;
    private GameObject _targetIconInstance;
    private bool _isDead = false;
    private bool _isFlameDeactivationCanceled = false;
    private PlayerMovement _playerMovement; // Cached reference

    // --- Cooldown Timers ---
    private float _cooldownTimer = Mathf.Infinity;
    private float _fireAttackTimer = Mathf.Infinity;
    private float _dashCooldownTimer = Mathf.Infinity;

    // Add these serialized fields to your AIBoss script if they aren't already there
    [Header("Movement & Targeting")]
    [SerializeField] private Vector3 _arenaCenterPosition = Vector3.zero; // Set in Inspector
    [SerializeField] private float _actionDistanceOffset = 3.0f; // Matches QLearning script parameter
    [SerializeField] private float _flameTrapGroundYLevel = -11.5f; // TODO: Get dynamically or configure
    [SerializeField] private float _dashDistance = 6.0f; // Distance for Dash_AwayFromPlayer action

    private Coroutine _dashCoroutine;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _initialBossPosition = gameObject.transform.position;

        if (_player == null)
        {
            Debug.LogError("[AIBoss] Player Transform not assigned!");
            // Try finding player by tag if not assigned
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
            else this.enabled = false; // Disable if player missing
        }
        if (_player != null)
        {
            _playerMovement = _player.GetComponent<PlayerMovement>();
            // Optional: Check if playerMovement is null and log warning
        }

        if (rewardManager == null)
        {
            Debug.LogError("[AIBoss] BossRewardManager (rewardManager) not assigned! Learning will fail.");
            this.enabled = false;
        }
        if (_bossHealth == null) _bossHealth = GetComponent<BossHealth>();
        if (_bossHealth == null)
        {
            Debug.LogError("[AIBoss] BossHealth component not found!");
            this.enabled = false;
        }
        if (_playerHealth != null)
        {
            _playerHealth.OnDamaged += HandlePlayerDeath;
        }
        _currentEnergy = _maxEnergy; // Start with full energy
    }

    private void Start()
    {
        // Initialize timers to be ready at the start of the first episode
        _cooldownTimer = attackCooldown;
        _fireAttackTimer = fireAttackCooldown;
        _dashCooldownTimer = dashCooldown;

        // Initial state setup (can also be part of ResetState)
        _isPhase2 = false;
        _isChargingDash = false;
        _isDashing = false;
        _isDead = false;
        UpdateEnergyBar();
    }

    public void HandlePlayerDeath(float idc)
    {
        if (_playerHealth != null && _playerHealth.currentHealth <= 0)
        {
            ResetState();
        }
    }

    // This method is called by EpisodeManager to reset the boss state for a new episode
    public void ResetState()
    {
        Debug.Log("[BossEnemy] Resetting BossEnemy state.");
        _detectedPlayer = false;

        // Stop any running coroutines related to attacks or movement
        StopAllCoroutines(); // Stop all coroutines on this script

        // Reset flags and state variables
        _isDead = false;



        if (_doRestHealth)
        {
            _isPhase2 = false; // Reset phase to 1 
        }
        _isChargingDash = false;
        _isDashing = false;
        // isFlameDeactivationCanceled = false; // Reset if used

        // Reset timers to be ready for new attacks
        ResetAbilityStates();

        // Reset Rigidbody state
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.isKinematic = false; // Ensure physics are enabled
        }

        gameObject.transform.position = _initialBossPosition;

        // Reset health (EpisodeManager also calls BossHealth.ResetHealth, but good to be safe)
        if (_bossHealth != null && _doRestHealth) _bossHealth.ResetHealth();

        // Deactivate any active visual markers or effects
        DeactivateFlameAndWarning(); // Ensure this also cleans up the dash target icon

        // Reset animator triggers/states if necessary
        if (_anim != null)
        {
            _anim.Rebind(); // Resets the animator to its default state
            _anim.Update(0f); // Forces an update to apply the rebind
            // You might need to set specific animation states if Rebind() isn't enough
            // anim.SetBool("IsMoving", false);
            // anim.SetBool("IsChargingDash", false); // If you have these bools
        }

        // Ensure the GameObject is active and enabled
        gameObject.SetActive(true);
        this.enabled = true;
    }


    private void Update()
    {
        if (_isDead || _player == null) return; // Ensure player exists

        if (!_detectedPlayer)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
            if (distanceToPlayer <= _attackRange && _playerHealth.currentHealth > 0)
                _detectedPlayer = true;
        }

        // Keep non-decision-making logic
        transform.rotation = Quaternion.Euler(0, 0, 0);

        // Update Cooldown Timers
        _cooldownTimer += Time.deltaTime;
        _fireAttackTimer += Time.deltaTime;
        _dashCooldownTimer += Time.deltaTime;

        // --- Regenerate Energy (if using) ---
        if (_currentEnergy < _maxEnergy)
        {
            _currentEnergy = Mathf.Min(_maxEnergy, _currentEnergy + _energyRegenRate * Time.deltaTime);
            UpdateEnergyBar();
        }

        // Phase transition logic
        HandlePhaseTransition();

        // Sprite flipping (only if not charging or dashing)
        if (!_isChargingDash && !_isDashing)
        {
            HandleSpriteFlip();
        }
    }

    private void LateUpdate()
    {
        if (_energySlider != null)
        {
            // Follow the boss's position
            _energySlider.transform.position = gameObject.transform.position + new Vector3(0, 2.5f, 0);

            // Keep rotation fixed
            _energySlider.transform.rotation = Quaternion.identity;
        }
    }

    private void HandlePhaseTransition()
    {
        if (!_isPhase2 && _bossHealth != null)
        {
            float healthPercentage = _bossHealth.GetHealthPercentage();
            if (healthPercentage <= 0.5f) // Enter phase 2 at 50% health
            {
                EnterPhase2();
            }
        }
    }

    private void HandleSpriteFlip()
    {
        if (_player == null || _isChargingDash) return; // Don't flip during dash
        if (_player.position.x < transform.position.x)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        // Ensure fireball holder matches boss direction if needed
        if (_fireballHolder != null)
        {
            _fireballHolder.localScale = transform.localScale;
        }
    }

    /// <summary>
    /// Handles movement requests based on the learned action type.
    /// </summary>
    /// <param name="moveAction">The specific movement action chosen by Q-Learning.</param>
    /// <param name="playerPos">Current player position.</param>
    /// <param name="bossPos">Current boss position (transform.position).</param>
    /// <param name="arenaCenterPos">Center of the arena.</param>
    /// <param name="offsetDistance">General distance parameter for offset/flank moves.</param>
    public void AIRequestMove(BossQLearning.ActionType moveAction, Vector2 playerPos, Vector2 bossPos, float offsetDistance)
    {
        // Basic checks: boss isn't busy with non-interruptible action or dead
        // (IsCurrentlyChargingOrDashing should cover this)
        if (_player == null || !_detectedPlayer || _isChargingDash || _isDashing || _isDead) return; // Assuming these states block movement

        Vector2 targetPosition;
        Vector2 moveDirection = Vector2.zero; // Default to no movement

        switch (moveAction)
        {
            case BossQLearning.ActionType.Move_TowardsPlayer:
                targetPosition = playerPos;
                moveDirection = (targetPosition - bossPos).normalized;
                break;

            case BossQLearning.ActionType.Move_AwayFromPlayer:
                targetPosition = bossPos + (bossPos - playerPos).normalized * offsetDistance;
                moveDirection = (targetPosition - bossPos).normalized; // Direction away from player
                break;

            case BossQLearning.ActionType.Move_StrafeLeft:
                // Calculate a direction perpendicular to the boss-player line, pointing left
                Vector2 dirToPlayer_L = (playerPos - bossPos).normalized;
                moveDirection = new Vector2(-dirToPlayer_L.y, dirToPlayer_L.x); // Perpendicular vector
                targetPosition = bossPos + moveDirection * offsetDistance; // Target a point to the left
                break;

            case BossQLearning.ActionType.Move_StrafeRight:
                // Calculate a direction perpendicular to the boss-player line, pointing right
                Vector2 dirToPlayer_R = (playerPos - bossPos).normalized;
                moveDirection = new Vector2(dirToPlayer_R.y, -dirToPlayer_R.x); // Perpendicular vector
                targetPosition = bossPos + moveDirection * offsetDistance; // Target a point to the right
                break;

            case BossQLearning.ActionType.Move_StrafeUp: // Assuming Up is World Up (Y+)
                moveDirection = Vector2.up;
                targetPosition = bossPos + moveDirection * offsetDistance; // Target a point above
                break;

            case BossQLearning.ActionType.Move_StrafeDown: // Assuming Down is World Down (Y-)
                moveDirection = Vector2.down;
                targetPosition = bossPos + moveDirection * offsetDistance; // Target a point below
                break;

            case BossQLearning.ActionType.Move_ToArenaCenter:
                targetPosition = _arenaCenterPosition;
                moveDirection = (targetPosition - bossPos).normalized;
                break;

            case BossQLearning.ActionType.Move_ToPlayerFlank:
                // Decide which flank (left/right) dynamically - e.g., based on which side has more space
                // Or simpler: always pick one side, or alternate. Let's just pick left flank for simplicity.
                Vector2 dirToPlayer_Flank = (playerPos - bossPos).normalized;
                // Get a perpendicular vector (left flank relative to player direction)
                Vector2 flankDirection = new Vector2(-dirToPlayer_Flank.y, dirToPlayer_Flank.x);
                targetPosition = playerPos + flankDirection * offsetDistance; // Target a point to the side of the player
                moveDirection = (targetPosition - bossPos).normalized; // Direction towards that flank point
                break;

            default:
                Debug.LogWarning("[AIBoss] Received unexpected move action: " + moveAction);
                AIRequestIdle(); // Default to Idle movement
                return; // Exit method
        }

        // --- Apply Movement ---
        // Use the calculated moveDirection or logic to move towards the targetPosition
        // This basic implementation just moves towards the calculated targetPosition
        // Avoid jittering when very close to the target
        if ((targetPosition - bossPos).sqrMagnitude > 0.25f) // Use squared magnitude for performance (0.5f squared)
        {
            // Could also use moveDirection directly: rb.velocity = moveDirection * movementSpeed;
            _rb.velocity = (targetPosition - bossPos).normalized * _movementSpeed; // Move towards the target position
            if (_anim != null) _anim.SetBool("IsMoving", true);
        }
        else
        {
            // Reached target or very close, stop
            _rb.velocity = Vector2.zero;
            if (_anim != null) _anim.SetBool("IsMoving", false);
        }
    }

    /// <summary>
    /// Attempts to execute the ranged fireball attack with specific learned aiming.
    /// </summary>
    /// <param name="aimAction">The specific aiming action chosen by Q-Learning.</param>
    /// <param name="playerPos">Current player position.</param>
    /// <param name="playerVel">Current player velocity.</param>
    /// <param name="offsetDistance">Distance parameter for offset aiming.</param>
    /// <returns>True if the attack was successfully initiated, false otherwise.</returns>
    public bool AIRequestRangedAttack(BossQLearning.ActionType aimAction, Vector2 playerPos, Vector2 playerVel, float offsetDistance)
    {
        // Basic checks: is ability ready, not busy with non-interruptible action, or dead, player exists
        if (!IsFireballReady() || !_detectedPlayer || _isChargingDash || _isDashing || _isDead || _player == null) return false;

        // --- Optional: Energy Check ---
        if (_currentEnergy < _fireballEnergyCost) return false;

        Vector2 targetPosition;
        Vector2 bossPos = transform.position; // Get boss position locally

        // Use predictionTime and projectileSpeed from your AIBoss configuration
        float predictionTime = (playerPos - bossPos).magnitude / _projectileSpeed; // Time for projectile to reach player
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
                // Assuming Left relative to player's X axis in 2D top-down
                targetPosition = playerPos + Vector2.left * offsetDistance;
                break;

            case BossQLearning.ActionType.Fireball_OffsetRight:
                // Assuming Right relative to player's X axis in 2D top-down
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
                // Aim in boss's current facing direction (assuming horizontal facing in 2D)
                // You'll need boss facing direction if not axis-aligned default
                Vector2 forwardDirection = (_rb.velocity.x > 0.01f) ? Vector2.right : (_rb.velocity.x < -0.01f ? Vector2.left : (_fireballHolder != null ? (_fireballHolder.localScale.x > 0 ? Vector2.right : Vector2.left) : Vector2.right)); // Basic facing guess
                if (forwardDirection == Vector2.zero) forwardDirection = Vector2.right; // Default if not moving
                targetPosition = bossPos + forwardDirection * 100f; // Aim far away in that direction
                break;

            case BossQLearning.ActionType.Fireball_RelativeUp: // Assuming Up is World Up (Y+)
                targetPosition = bossPos + Vector2.up * 100f; // Aim far up
                break;

            case BossQLearning.ActionType.Fireball_RelativeDown: // Assuming Down is World Down (Y-)
                targetPosition = bossPos + Vector2.down * 100f; // Aim far down
                break;

            default:
                Debug.LogWarning("[AIBoss] Received unexpected fireball aim action: " + aimAction);
                targetPosition = playerPos; // Fallback to basic aim
                break;
        }

        // --- Execute Fireball ---
        // Use your existing fireball spawning/pooling logic
        int fireballIndex = FindFireball(); // Assuming this finds an available projectile
        if (fireballIndex == -1) return false; // No fireballs available

        // --- Consume Resource ---
        if (_currentEnergy < _fireballEnergyCost) return false; // Double check or move up

        GameObject projectile = _fireballs[fireballIndex];
        projectile.transform.position = _firepoint.position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.parent = null; // Unparent if needed

        BossProjectile bossProjectile = projectile.GetComponent<BossProjectile>();
        if (bossProjectile == null) { Debug.LogError("[AIBoss] Fireball prefab missing BossProjectile script!"); return false; }

        // Pass necessary references/data to the projectile
        bossProjectile.rewardManager = this.rewardManager; // Ensure projectile can report misses
        bossProjectile.SetDamage(_fireballDamage);
        bossProjectile.SetSize(_projectileSize);
        bossProjectile.Launch(_firepoint.position, targetPosition, _projectileSpeed); // Launch towards calculated target!

        if (_anim != null) _anim.SetTrigger("Attack"); // Use the correct attack trigger
        if (_fireballSound != null) SoundManager.instance.PlaySound(_fireballSound, gameObject); // Assuming SoundManager is correctly set up

        // Only reset cooldown if the launch was successful
        _cooldownTimer = 0f; // Reset cooldown *after* successful launch
        _currentEnergy -= _fireballEnergyCost; // Consume resource *after* successful launch
        UpdateEnergyBar();

        return true; // Action initiated successfully
    }

    /// <summary>
    /// Attempts to place the flame trap attack based on the learned action type.
    /// </summary>
    /// <param name="placeAction">The specific placement action chosen by Q-Learning.</param>
    /// <param name="playerPos">Current player position.</param>
    /// <param name="bossPos">Current boss position.</param>
    /// <param name="playerVel">Current player velocity.</param>
    /// <param name="offsetDistance">Distance parameter for placement offsets.</param>
    /// <returns>True if the attack was successfully initiated, false otherwise.</returns>
    public bool AIRequestFlameAttack(BossQLearning.ActionType placeAction, Vector2 playerPos, Vector2 bossPos, Vector2 playerVel, float offsetDistance)
    {
        // Basic checks: is ability ready, not busy with non-interruptible action, or dead, flame prefab exists, player exists
        if (!IsFlameTrapReady() || !_detectedPlayer || _isChargingDash || _isDashing || _isDead || _flame == null || _player == null) return false;

        // --- Optional: Energy Check ---
        if (_currentEnergy < _flameTrapEnergyCost) return false;

        Vector2 placementPosition;

        // --- Calculate placement position based on action type ---
        switch (placeAction)
        {
            case BossQLearning.ActionType.FlameTrap_AtPlayer:
                // Place at player's X position, on the ground level
                placementPosition = new Vector2(playerPos.x, _flameTrapGroundYLevel);
                break;

            case BossQLearning.ActionType.FlameTrap_NearBoss:
                // Place near boss's X position, on the ground level
                placementPosition = new Vector2(bossPos.x, _flameTrapGroundYLevel); // Or bossPos.x +/- offsetDistance
                break;

            case BossQLearning.ActionType.FlameTrap_BetweenBossAndPlayer:
                // Place at the midpoint X between boss and player, on the ground level
                placementPosition = new Vector2(Vector2.Lerp(bossPos, playerPos, 0.5f).x, _flameTrapGroundYLevel);
                break;

            case BossQLearning.ActionType.FlameTrap_BehindPlayer:
                // Place behind the player based on their current velocity, on the ground level
                Vector2 behindPlayerPos = playerPos - playerVel.normalized * offsetDistance; // Position offset behind player
                placementPosition = new Vector2(behindPlayerPos.x, _flameTrapGroundYLevel);
                break;

            default:
                Debug.LogWarning("[AIBoss] Received unexpected flame trap action: " + placeAction);
                placementPosition = new Vector2(playerPos.x, _flameTrapGroundYLevel); // Fallback
                break;
        }

        // Apply clamping between walls (using your existing logic)
        if (_leftWall != null && _rightWall != null)
        {
            placementPosition.x = Mathf.Clamp(placementPosition.x, _leftWall.position.x + 2f, _rightWall.position.x - 2f); // Add buffer from wall edge
        }

        // --- Execute Flame Trap ---
        StartCoroutine(MarkAreaAndSpawnFire(placementPosition)); // Assuming this coroutine exists and uses the position
        if (_anim != null) _anim.SetTrigger("CastSpell"); // Use the correct trigger
        // Only reset cooldown if the execution was successful
        _fireAttackTimer = 0f; // Reset cooldown
        _currentEnergy -= _flameTrapEnergyCost; // Consume resource *after* successful initiation
        UpdateEnergyBar();

        return true; // Action initiated successfully
    }

    /// <summary>
    /// Attempts to initiate the dash attack based on the learned action type.
    /// </summary>
    /// <param name="dashAction">The specific dash action chosen by Q-Learning.</param>
    /// <param name="playerPos">Current player position.</param>
    /// <param name="bossPos">Current boss position.</param>
    /// <param name="offsetDistance">Distance parameter for flank dash.</param>
    /// <returns>True if the attack was successfully initiated, false otherwise.</returns>
    public bool AIRequestDashAttack(BossQLearning.ActionType dashAction, Vector2 playerPos, Vector2 bossPos, float offsetDistance)
    {
        // Basic checks: is ability ready, not busy (already dashing/charging), or dead, player exists
        if (!IsDashReady() || !_detectedPlayer || _isChargingDash || _isDashing || _isDead || _player == null) return false;

        // --- Optional: Energy Check ---
        if (_currentEnergy < _dashEnergyCost) return false;

        Vector2 dashTargetCalculated;

        // --- Calculate dash target based on action type ---
        switch (dashAction)
        {
            case BossQLearning.ActionType.Dash_TowardsPlayer:
                // Dash directly towards the player's position at the time of call
                dashTargetCalculated = playerPos;
                break;

            case BossQLearning.ActionType.Dash_AwayFromPlayer:
                // Dash away from the player by a configured dashDistance
                dashTargetCalculated = bossPos + (bossPos - playerPos).normalized * _dashDistance; // Use the dashDistance field
                break;

            case BossQLearning.ActionType.Dash_ToPlayerFlank:
                // Calculate a flank position relative to player/boss
                Vector2 dirToPlayer_Flank = (playerPos - bossPos).normalized;
                // Get a perpendicular vector (e.g., for left flank relative to player direction)
                Vector2 flankDirection = new Vector2(-dirToPlayer_Flank.y, dirToPlayer_Flank.x);
                // Add the flank direction offset from the player's position
                dashTargetCalculated = playerPos + flankDirection * offsetDistance; // Use the offsetDistance parameter
                // You might want to add logic here to choose left/right flank based on state or alternate
                break;

            default:
                Debug.LogWarning("[AIBoss] Received unexpected dash action: " + dashAction);
                dashTargetCalculated = playerPos; // Fallback
                break;
        }

        // --- Initiate Dash ---
        _isChargingDash = true; // Enter charging state
        _isDashing = false;     // Not yet moving
        _dashMissed = true;     // Reset miss flag
        _rb.velocity = Vector2.zero; // Stop current movement

        // Set the calculated target for the dash sequence
        _dashTarget = dashTargetCalculated;

        // Activate target marker (using your existing logic)
        if (_targetIconPrefab != null)
        {
            if (_targetIconInstance == null)
            {
                _targetIconInstance = Instantiate(_targetIconPrefab, _dashTarget, Quaternion.identity);
                _targetIconInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
            else
            {
                _targetIconInstance.transform.position = _dashTarget;
                _targetIconInstance.SetActive(true);
            }
        }

        if (_anim != null) _anim.SetTrigger("ChargeDash"); // Use the correct trigger
        if (_chargeSound != null) SoundManager.instance.PlaySound(_chargeSound, gameObject); // Assuming SoundManager

        // Start the sequence that performs the charge and dash
        StartCoroutine(PerformDashAttack()); // Assuming this coroutine exists

        // Only reset cooldown if the initiation was successful
        _dashCooldownTimer = 0f; // Reset cooldown
        _currentEnergy -= _dashEnergyCost; // Consume resource *after* successful initiation
        UpdateEnergyBar();

        return true; // Action initiated successfully
    }

    /// <summary>
    /// Stops current movement and sets animation to idle, based on QL request.
    /// </summary>
    public void AIRequestIdle()
    {
        // Only go idle if not busy with a non-interruptible action
        if (_isChargingDash || _isDashing || _isDead) return;

        _rb.velocity = Vector2.zero;
        if (_anim != null) _anim.SetBool("IsMoving", false);
    }


    // --- Ability Readiness Checks (Used by BossQLearningRefactored) ---
    public bool IsFireballReady()
    {
        // Optional: Energy Check: && currentEnergy >= fireballEnergyCost;
        return _cooldownTimer >= attackCooldown;
    }

    public bool IsFlameTrapReady()
    {
        // Optional: Energy Check: && currentEnergy >= flameTrapEnergyCost;
        return _fireAttackTimer >= fireAttackCooldown;
    }

    public bool IsDashReady()
    {
        // Optional: Energy Check: && currentEnergy >= dashEnergyCost;
        // Add phase check? return isPhase2 && dashCooldownTimer >= dashCooldown;
        return _dashCooldownTimer >= dashCooldown;
    }

    /// <summary>
    /// Checks if the boss is currently in the charge-up phase OR the movement phase of the dash.
    /// </summary>
    public bool IsCurrentlyChargingOrDashing()
    {
        return _isChargingDash || _isDashing;
    }


    // --- State Information Providers (Used by BossQLearningRefactored) ---

    /// <summary>
    /// Gets the boss's current energy level, normalized between 0 and 1.
    /// </summary>
    /// <returns>Normalized energy (0.0 to 1.0).</returns>
    public float GetCurrentEnergyNormalized()
    {
        if (_maxEnergy <= 0) return 1.0f; // Avoid division by zero if energy system not used
        return Mathf.Clamp01(_currentEnergy / _maxEnergy);
    }

    /// <summary>
    /// Checks if the player is currently considered grounded.
    /// Requires access to the PlayerMovement script.
    /// </summary>
    /// <returns>True if the player is grounded, false otherwise.</returns>
    public bool IsPlayerGrounded()
    {
        if (_playerMovement != null)
        {
            return _playerMovement.IsGrounded(); // Assuming PlayerMovement has this method
        }
        // Fallback if playerMovement reference is missing
        // Maybe try a physics check? e.g., Physics2D.OverlapCircle below player? Less reliable.
        Debug.LogWarning("[AIBoss] Cannot check player grounded status: PlayerMovement reference missing.");
        return true; // Default assumption if check fails
    }


    // --- Coroutines and Internal Logic ---
    private int FindFireball() // Helper for object pooling
    {
        for (int i = 0; i < _fireballs.Length; i++)
        {
            // Check if the GameObject itself is active in the hierarchy
            if (!_fireballs[i].activeInHierarchy)
            {
                return i;
            }
        }
        return -1; // No inactive fireball found
    }

    private IEnumerator MarkAreaAndSpawnFire(Vector2 targetPosition)
    { /* ... slight modification needed ... */
        if (_isDead) yield break;

        GameObject marker = null;
        if (_areaMarkerPrefab != null)
        {
            marker = Instantiate(_areaMarkerPrefab, targetPosition, Quaternion.identity);
            // Removed marker.SetActive(true); - Instantiate already makes it active
        }

        yield return new WaitForSeconds(1.5f);

        // Check if boss died *during* the wait
        if (_isDead)
        {
            if (marker != null) Destroy(marker); // Clean up marker if boss died
            yield break;
        }

        if (marker != null) Destroy(marker); // Destroy normally if boss alive

        // Spawn flame only if boss is still alive after the wait
        if (_flame != null)
        {
            _flame.transform.position = targetPosition;
            _flame.SetActive(true);

            BossFlameAttack flameAttack = _flame.GetComponent<BossFlameAttack>();
            if (flameAttack != null)
            {
                flameAttack.Activate(targetPosition);
                StartCoroutine(DeactivateAfterDuration(_flame, 3f));
            }
            else
            {
                Debug.LogError("[AIBoss] Flame object missing BossFlameAttack script!");
                _flame.SetActive(false); // Deactivate if script missing
            }
        }
        else
        {
            Debug.LogError("[AIBoss] Flame prefab reference not set!");
        }
    }


    // Coroutine to deactivate the flame trap after a duration and report miss if needed
    private IEnumerator DeactivateAfterDuration(GameObject obj, float delay)
    {
        _isFlameDeactivationCanceled = false; // Reset cancel flag for this instance
        float timer = 0f;
        while (timer < delay)
        {
            if (_isDead) // If boss dies during the flame duration
            {
                if (obj != null) obj.SetActive(false); // Immediately deactivate
                _isFlameDeactivationCanceled = true; // Mark as canceled due to death
                yield break; // Exit coroutine
            }
            timer += Time.deltaTime;
            yield return null;
        }
        // Timer finished, check if not canceled and object still exists
        if (!_isFlameDeactivationCanceled && obj != null)
        {
            obj.SetActive(false); // Deactivate normally
            if (rewardManager != null)
            {
                if (flameMissed) // Check if the flameMissed flag (set by BossFlameAttack) indicates a miss
                {
                    rewardManager.ReportAttackMissed(); // Report the miss
                }
                // Reset flag for the next attack regardless of hit/miss status
                flameMissed = true;
            }
        }
    }

    private IEnumerator PerformDashAttack()
    {
        _isChargingDash = true; // redundant? already set before calling
        _isDashing = false;

        yield return new WaitForSeconds(_dashChargeTime);

        // --- Charge finished ---
        _isChargingDash = false; // No longer charging
        if (_targetIconInstance != null) _targetIconInstance.SetActive(false);

        if (_isDead) yield break; // Check if died during charge

        // --- Start Dash Movement ---
        _isDashing = true; // Now actually moving
        if (_anim != null) _anim.SetTrigger("Dash");
        if (_dashSound != null) SoundManager.instance.PlaySound(_dashSound, gameObject);

        Vector2 direction = (_dashTarget - (Vector2)transform.position).normalized;
        if (direction == Vector2.zero) direction = (_player.position - transform.position).normalized; // Prevent zero direction if already at target
        _rb.velocity = direction * _dashSpeed;

        // Collision check loop (simplified check)
        float dashTimer = 0f;
        float maxDashDuration = 1.5f; // Allow slightly longer dash travel time

        while (dashTimer < maxDashDuration)
        {
            if (_isDead || !_isDashing) break; // Exit loop if died or flag externally reset (e.g., hit player)

            // Check if dash hit player (OnTriggerEnter2D sets dashMissed = false and might set isDashing=false)
            if (!_dashMissed) break;

            // Optional: More robust obstacle check if needed
            // RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.6f, LayerMask.GetMask("Ground", "Walls"));
            // if (hit.collider != null) break;

            // Check if approximately reached target
            if (Vector2.Distance(transform.position, _dashTarget) < 1.0f) break;


            dashTimer += Time.deltaTime;
            yield return null;
        }

        // --- Dash Finished (hit player, obstacle, reached target, or timed out) ---
        _isDashing = false; // Finished dashing movement phase
        _rb.velocity = Vector2.zero;

        // Report miss ONLY if the dash finished, the 'dashMissed' flag is still true, AND boss isn't dead
        if (rewardManager != null && _dashMissed && !_isDead)
        {
            rewardManager.ReportAttackMissed();
        }

        // Reset flag for the next dash attempt
        _dashMissed = true;
    }

    // --- Phase Logic ---
    private void EnterPhase2() { /* ... as before ... */ }


    // --- Collision Handling ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;

        if (other.CompareTag("Player"))
        {
            // Apply damage to player
            Health playerHealth = other.GetComponent<Health>();
            // Use 'damage' field inherited from EnemyDamage base class
            if (playerHealth != null) playerHealth.TakeDamage(_damage);

            // Report direct collision hit to reward manager
            if (rewardManager != null) rewardManager.ReportHitPlayer();

            // Check if this collision happened during the dash movement phase
            if (_isDashing) // Check isDashing, not isChargingDash
            {
                //Debug.Log("[AIBoss] Dash connected with player during movement!");
                _dashMissed = false;   // Mark dash as successful hit
                _isDashing = false;    // Stop dash movement immediately on hit
                _rb.velocity = Vector2.zero; // Stop immediately
                // Optionally add knockback to player here
            }
            // isColliding flag seems unused, can be removed if not needed elsewhere
            // isColliding = true;
        }
        // Add checks for hitting walls/ground during dash if needed
        else if (_isDashing && (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Walls")))
        {
            // Stop dash if hitting obstacles during movement phase
            //Debug.Log("[AIBoss] Dash hit obstacle: " + other.name);
            _isDashing = false;
            _rb.velocity = Vector2.zero;
        }
    }

    // OnTriggerExit2D likely not needed if isColliding is removed

    // --- Death Handling ---
    public void Die() // Override if EnemyDamage has a virtual Die method
    {
        if (_isDead) return;
        _isDead = true;
        //Debug.Log("[AIBoss] Die() method called.");

        StopAllCoroutines();
        DeactivateFlameAndWarning(); // Ensure cleanup

        _rb.velocity = Vector2.zero;
        _rb.isKinematic = true;
        // Safely disable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (_anim != null) _anim.SetTrigger("Die");

        // Optional: Delay disabling/destroying to allow animation
        // StartCoroutine(DestroyAfterDelay(2.0f)); // Example delay
        this.enabled = false; // Disable script logic immediately
    }
    // Optional Helper for delayed destruction
    // private IEnumerator DestroyAfterDelay(float delay) {
    //     yield return new WaitForSeconds(delay);
    //     Destroy(gameObject);
    // }


    // Utility to clean up flame visuals immediately
    public void DeactivateFlameAndWarning()
    {
        if (_flame != null) _flame.SetActive(false);

        // Find and destroy any active area markers by tag
        GameObject[] markers = GameObject.FindGameObjectsWithTag("FlameWarningMarker"); // Ensure markers have this tag
        foreach (GameObject marker in markers)
        {
            Destroy(marker);
        }
    }

    public void ResetAbilityStates()
    {
        attackCooldown = 4f;
        fireAttackCooldown = 4f;
        dashCooldown = 8f;
        _cooldownTimer = attackCooldown;
        _fireAttackTimer = fireAttackCooldown;
        _dashCooldownTimer = dashCooldown;
    }
    private void UpdateEnergyBar()
    {
        if (_energySlider != null)
        {
            _energySlider.value = _currentEnergy / _maxEnergy;
        }
    }
}
