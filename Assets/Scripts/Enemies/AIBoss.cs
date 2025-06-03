using System.Collections;
using UnityEngine;

// This script handles the boss's mechanics, state, and execution of actions requested by the Q-learning agent.
public class AIBoss : EnemyDamage // Assuming EnemyDamage handles health or similar
{
    [Header("Boss Parameters")]
    [SerializeField] private float movementSpeed = 3.0f; // Adjusted speed example

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator anim;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] public BossRewardManager rm; // CRUCIAL - Assign in Inspector!
    [SerializeField] private BossHealth bossHealth; // Reference to the BossHealth script
    [SerializeField] private Transform firepoint;
    [SerializeField] private Transform fireballHolder; // Parent for inactive fireballs
    [SerializeField] private GameObject[] fireballs; // Pool of fireballs
    [SerializeField] private GameObject flame; // Flame trap object
    [SerializeField] private GameObject areaMarkerPrefab; // Marker for flame trap
    [SerializeField] private Transform leftWall; // Boundary for flame trap
    [SerializeField] private Transform rightWall; // Boundary for flame trap
    [SerializeField] private GameObject targetIconPrefab; // For dash attack

    [Header("Attack Parameters")]
    [SerializeField] public float attackCooldown = 2f; // Fireball cooldown
    [SerializeField] private int fireballDamage = 1;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float projectileSize = 1.5f;
    [SerializeField] private AudioClip fireballSound;
    [Tooltip("How far ahead (in seconds) to predict player movement for aiming fireballs.")]
    [SerializeField] private float predictionTime = 0.3f; // Adjust based on projectile speed & player speed

    [Header("Flame Attack Parameters")]
    [SerializeField] public float fireAttackCooldown = 8f;

    [Header("Charge Dash Attack Parameters")]
    [SerializeField] private float dashChargeTime = 1.5f; // Slightly faster charge example
    [SerializeField] private float dashSpeed = 12f; // Slightly faster dash example
    [SerializeField] public float dashCooldown = 10f;
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioClip dashSound;

    [Header("Energy System (Optional - Enable Checks Below)")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float energyRegenRate = 5f; // Energy per second
    [SerializeField] private float fireballEnergyCost = 10f;
    [SerializeField] private float flameTrapEnergyCost = 30f;
    [SerializeField] private float dashEnergyCost = 40f;
    private float currentEnergy;

    // --- Internal State ---

    //CHANGE!
    private bool isPhase2 = true;
    private bool isChargingDash = false;
    private bool isDashing = false; // Added to differentiate charging phase from movement phase
    private bool dashMissed = true;
    public bool flameMissed = true; // Public so BossFlameAttack can set it to false on hit
    private Vector2 dashTarget;
    private GameObject targetIconInstance;
    private bool isDead = false;
    private bool isFlameDeactivationCanceled = false;
    private PlayerMovement playerMovement; // Cached reference

    // --- Cooldown Timers ---
    private float cooldownTimer = Mathf.Infinity;
    private float fireAttackTimer = Mathf.Infinity;
    private float dashCooldownTimer = Mathf.Infinity;

    // Add these serialized fields to your AIBoss script if they aren't already there
    [Header("Movement & Targeting")]
    [SerializeField] private Vector3 arenaCenterPosition = Vector3.zero; // Set in Inspector
    [SerializeField] private float actionDistanceOffset = 3.0f; // Matches QLearning script parameter
    [SerializeField] private float flameTrapGroundYLevel = -11.5f; // TODO: Get dynamically or configure
    [SerializeField] private float dashDistance = 6.0f; // Distance for Dash_AwayFromPlayer action



    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (player == null) {
            Debug.LogError("[AIBoss] Player Transform not assigned!");
            // Try finding player by tag if not assigned
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if(playerObj != null) player = playerObj.transform;
            else this.enabled = false; // Disable if player missing
        }
        if (player != null) {
            playerMovement = player.GetComponent<PlayerMovement>();
            // Optional: Check if playerMovement is null and log warning
        }

        if (rm == null) {
            Debug.LogError("[AIBoss] BossRewardManager (rm) not assigned! Learning will fail.");
            this.enabled = false;
        }
        if (bossHealth == null) bossHealth = GetComponent<BossHealth>();
        if (bossHealth == null) {
            Debug.LogError("[AIBoss] BossHealth component not found!");
             this.enabled = false;
        }

        currentEnergy = maxEnergy; // Start with full energy
    }

    // Start can be removed if empty


    private void Update()
    {
        if (isDead || player == null) return; // Ensure player exists

        // Keep non-decision-making logic
        transform.rotation = Quaternion.Euler(0, 0, 0);

        // Update Cooldown Timers
        cooldownTimer += Time.deltaTime;
        fireAttackTimer += Time.deltaTime;
        dashCooldownTimer += Time.deltaTime;

        // --- Regenerate Energy (if using) ---
        // if (currentEnergy < maxEnergy)
        // {
        //     currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenRate * Time.deltaTime);
        // }

        // Phase transition logic
        HandlePhaseTransition();

        // Sprite flipping (only if not charging or dashing)
        if (!isChargingDash && !isDashing) {
            HandleSpriteFlip();
        }
    }

   private void HandlePhaseTransition()
    {
        if (!isPhase2 && bossHealth != null)
        {
            float healthPercentage = bossHealth.GetHealthPercentage();
            if (healthPercentage <= 0.5f) // Enter phase 2 at 50% health
            {
                EnterPhase2();
            }
        }
    }

    private void HandleSpriteFlip()
    {
        if (player == null || isChargingDash) return; // Don't flip during dash
        if (player.position.x < transform.position.x)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        // Ensure fireball holder matches boss direction if needed
        if (fireballHolder != null)
        {
            fireballHolder.localScale = transform.localScale;
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
        if (player == null || isChargingDash || isDashing || isDead) return; // Assuming these states block movement

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
                targetPosition = arenaCenterPosition;
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
            rb.velocity = (targetPosition - bossPos).normalized * movementSpeed; // Move towards the target position
            if (anim != null) anim.SetBool("IsMoving", true);
        }
        else
        {
            // Reached target or very close, stop
            rb.velocity = Vector2.zero;
            if (anim != null) anim.SetBool("IsMoving", false);
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
        if (!IsFireballReady() || isChargingDash || isDashing || isDead || player == null) return false;

        // --- Optional: Energy Check ---
        // if (currentEnergy < fireballEnergyCost) return false;

        Vector2 targetPosition;
        Vector2 bossPos = transform.position; // Get boss position locally

        // Use predictionTime and projectileSpeed from your AIBoss configuration
        float predictionTime = (playerPos - bossPos).magnitude / projectileSpeed; // Time for projectile to reach player
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
                Vector2 forwardDirection = (rb.velocity.x > 0.01f) ? Vector2.right : (rb.velocity.x < -0.01f ? Vector2.left : (transform.localScale.x > 0 ? Vector2.right : Vector2.left)); // Basic facing guess
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
        // if (currentEnergy < fireballEnergyCost) return false; // Double check or move up

        GameObject projectile = fireballs[fireballIndex];
        projectile.transform.position = firepoint.position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.parent = null; // Unparent if needed

        BossProjectile bossProjectile = projectile.GetComponent<BossProjectile>();
        if (bossProjectile == null) { Debug.LogError("[AIBoss] Fireball prefab missing BossProjectile script!"); return false; }

        // Pass necessary references/data to the projectile
        bossProjectile.rm = this.rm; // Ensure projectile can report misses
        bossProjectile.SetDamage(fireballDamage);
        bossProjectile.SetSize(projectileSize);
        bossProjectile.Launch(firepoint.position, targetPosition, projectileSpeed); // Launch towards calculated target!

        if (anim != null) anim.SetTrigger("Attack"); // Use the correct attack trigger
        if (fireballSound != null) SoundManager.instance.PlaySound(fireballSound); // Assuming SoundManager is correctly set up

        // Only reset cooldown if the launch was successful
        cooldownTimer = 0f; // Reset cooldown *after* successful launch
        // currentEnergy -= fireballEnergyCost; // Consume resource *after* successful launch

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
        if (!IsFlameTrapReady() || isChargingDash || isDashing || isDead || flame == null || player == null) return false;

        // --- Optional: Energy Check ---
        // if (currentEnergy < flameTrapEnergyCost) return false;

        Vector2 placementPosition;

        // --- Calculate placement position based on action type ---
        switch (placeAction)
        {
            case BossQLearning.ActionType.FlameTrap_AtPlayer:
                // Place at player's X position, on the ground level
                placementPosition = new Vector2(playerPos.x, flameTrapGroundYLevel);
                break;

            case BossQLearning.ActionType.FlameTrap_NearBoss:
                // Place near boss's X position, on the ground level
                placementPosition = new Vector2(bossPos.x, flameTrapGroundYLevel); // Or bossPos.x +/- offsetDistance
                break;

            case BossQLearning.ActionType.FlameTrap_BetweenBossAndPlayer:
                // Place at the midpoint X between boss and player, on the ground level
                placementPosition = new Vector2(Vector2.Lerp(bossPos, playerPos, 0.5f).x, flameTrapGroundYLevel);
                break;

            case BossQLearning.ActionType.FlameTrap_BehindPlayer:
                // Place behind the player based on their current velocity, on the ground level
                Vector2 behindPlayerPos = playerPos - playerVel.normalized * offsetDistance; // Position offset behind player
                placementPosition = new Vector2(behindPlayerPos.x, flameTrapGroundYLevel);
                break;

            default:
                Debug.LogWarning("[AIBoss] Received unexpected flame trap action: " + placeAction);
                placementPosition = new Vector2(playerPos.x, flameTrapGroundYLevel); // Fallback
                break;
        }

        // Apply clamping between walls (using your existing logic)
        if (leftWall != null && rightWall != null) {
            placementPosition.x = Mathf.Clamp(placementPosition.x, leftWall.position.x + 2f, rightWall.position.x - 2f); // Add buffer from wall edge
        }

        // --- Execute Flame Trap ---
        StartCoroutine(MarkAreaAndSpawnFire(placementPosition)); // Assuming this coroutine exists and uses the position
        if (anim != null) anim.SetTrigger("CastSpell"); // Use the correct trigger
        // Only reset cooldown if the execution was successful
        fireAttackTimer = 0f; // Reset cooldown
        // currentEnergy -= flameTrapEnergyCost; // Consume resource *after* successful initiation

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
        if (!IsDashReady() || isChargingDash || isDashing || isDead || player == null) return false;

        // --- Optional: Energy Check ---
        // if (currentEnergy < dashEnergyCost) return false;

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
                dashTargetCalculated = bossPos + (bossPos - playerPos).normalized * dashDistance; // Use the dashDistance field
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
        isChargingDash = true; // Enter charging state
        isDashing = false;     // Not yet moving
        dashMissed = true;     // Reset miss flag
        rb.velocity = Vector2.zero; // Stop current movement

        // Set the calculated target for the dash sequence
        dashTarget = dashTargetCalculated;

        // Activate target marker (using your existing logic)
        if (targetIconPrefab != null) {
            if (targetIconInstance == null) {
                targetIconInstance = Instantiate(targetIconPrefab, dashTarget, Quaternion.identity);
                targetIconInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            } else {
                targetIconInstance.transform.position = dashTarget;
                targetIconInstance.SetActive(true);
            }
        }

        if (anim != null) anim.SetTrigger("ChargeDash"); // Use the correct trigger
        if (chargeSound != null) SoundManager.instance.PlaySound(chargeSound); // Assuming SoundManager

        // Start the sequence that performs the charge and dash
        StartCoroutine(PerformDashAttack()); // Assuming this coroutine exists

        // Only reset cooldown if the initiation was successful
        dashCooldownTimer = 0f; // Reset cooldown
        // currentEnergy -= dashEnergyCost; // Consume resource *after* successful initiation

        return true; // Action initiated successfully
    }

    /// <summary>
    /// Stops current movement and sets animation to idle, based on QL request.
    /// </summary>
    public void AIRequestIdle()
    {
        // Only go idle if not busy with a non-interruptible action
        if (isChargingDash || isDashing || isDead) return;

        rb.velocity = Vector2.zero;
        if (anim != null) anim.SetBool("IsMoving", false);
    }


    // --- Ability Readiness Checks (Used by BossQLearningRefactored) ---
    public bool IsFireballReady()
    {
        // Optional: Energy Check: && currentEnergy >= fireballEnergyCost;
        return cooldownTimer >= attackCooldown;
    }

    public bool IsFlameTrapReady()
    {
         // Optional: Energy Check: && currentEnergy >= flameTrapEnergyCost;
        return fireAttackTimer >= fireAttackCooldown;
    }

    public bool IsDashReady()
    {
        // Optional: Energy Check: && currentEnergy >= dashEnergyCost;
        // Add phase check? return isPhase2 && dashCooldownTimer >= dashCooldown;
        return dashCooldownTimer >= dashCooldown;
    }

    /// <summary>
    /// Checks if the boss is currently in the charge-up phase OR the movement phase of the dash.
    /// </summary>
    public bool IsCurrentlyChargingOrDashing()
    {
        return isChargingDash || isDashing;
    }


    // --- State Information Providers (Used by BossQLearningRefactored) ---

    /// <summary>
    /// Gets the boss's current energy level, normalized between 0 and 1.
    /// </summary>
    /// <returns>Normalized energy (0.0 to 1.0).</returns>
    public float GetCurrentEnergyNormalized()
    {
        if (maxEnergy <= 0) return 1.0f; // Avoid division by zero if energy system not used
        return Mathf.Clamp01(currentEnergy / maxEnergy);
    }

    /// <summary>
    /// Checks if the player is currently considered grounded.
    /// Requires access to the PlayerMovement script.
    /// </summary>
    /// <returns>True if the player is grounded, false otherwise.</returns>
    public bool IsPlayerGrounded()
    {
        if (playerMovement != null)
        {
            return playerMovement.isGrounded(); // Assuming PlayerMovement has this method
        }
        // Fallback if playerMovement reference is missing
        // Maybe try a physics check? e.g., Physics2D.OverlapCircle below player? Less reliable.
        Debug.LogWarning("[AIBoss] Cannot check player grounded status: PlayerMovement reference missing.");
        return true; // Default assumption if check fails
    }


    // --- Coroutines and Internal Logic ---
    private int FindFireball() // Helper for object pooling
    {
        for (int i = 0; i < fireballs.Length; i++)
        {
            // Check if the GameObject itself is active in the hierarchy
            if (!fireballs[i].activeInHierarchy)
            {
                return i;
            }
        }
        return -1; // No inactive fireball found
    }

    private IEnumerator MarkAreaAndSpawnFire(Vector2 targetPosition) { /* ... slight modification needed ... */
        if (isDead) yield break;

        GameObject marker = null;
        if (areaMarkerPrefab != null) {
            marker = Instantiate(areaMarkerPrefab, targetPosition, Quaternion.identity);
            // Removed marker.SetActive(true); - Instantiate already makes it active
        }

        yield return new WaitForSeconds(1.5f);

        // Check if boss died *during* the wait
        if (isDead) {
             if (marker != null) Destroy(marker); // Clean up marker if boss died
             yield break;
        }

        if (marker != null) Destroy(marker); // Destroy normally if boss alive

        // Spawn flame only if boss is still alive after the wait
        if (flame != null) {
             flame.transform.position = targetPosition;
             flame.SetActive(true);

             BossFlameAttack flameAttack = flame.GetComponent<BossFlameAttack>();
             if (flameAttack != null) {
                 flameAttack.Activate(targetPosition);
                 StartCoroutine(DeactivateAfterDuration(flame, 3f));
             } else {
                 Debug.LogError("[AIBoss] Flame object missing BossFlameAttack script!");
                 flame.SetActive(false); // Deactivate if script missing
             }
        } else {
            Debug.LogError("[AIBoss] Flame prefab reference not set!");
        }
    }


    // Coroutine to deactivate the flame trap after a duration and report miss if needed
    private IEnumerator DeactivateAfterDuration(GameObject obj, float delay)
    {
        isFlameDeactivationCanceled = false; // Reset cancel flag for this instance
        float timer = 0f;
        while (timer < delay)
        {
            if (isDead) // If boss dies during the flame duration
            {
                if (obj != null) obj.SetActive(false); // Immediately deactivate
                isFlameDeactivationCanceled = true; // Mark as canceled due to death
                yield break; // Exit coroutine
            }
            timer += Time.deltaTime;
            yield return null;
        }
        // Timer finished, check if not canceled and object still exists
        if (!isFlameDeactivationCanceled && obj != null)
        {
            obj.SetActive(false); // Deactivate normally
            if (rm != null)
            {
                if (flameMissed) // Check if the flameMissed flag (set by BossFlameAttack) indicates a miss
                {
                    rm.ReportAttackMissed(); // Report the miss
                }
                // Reset flag for the next attack regardless of hit/miss status
                flameMissed = true;
            }
        }
    }

    private IEnumerator PerformDashAttack()
    {
        isChargingDash = true; // redundant? already set before calling
        isDashing = false;

        yield return new WaitForSeconds(dashChargeTime);

        // --- Charge finished ---
        isChargingDash = false; // No longer charging
        if (targetIconInstance != null) targetIconInstance.SetActive(false);

        if (isDead) yield break; // Check if died during charge

        // --- Start Dash Movement ---
        isDashing = true; // Now actually moving
        if (anim != null) anim.SetTrigger("Dash");
        if (dashSound != null) SoundManager.instance.PlaySound(dashSound);

        Vector2 direction = (dashTarget - (Vector2)transform.position).normalized;
        if (direction == Vector2.zero) direction = (player.position - transform.position).normalized; // Prevent zero direction if already at target
        rb.velocity = direction * dashSpeed;

        // Collision check loop (simplified check)
        float dashTimer = 0f;
        float maxDashDuration = 1.5f; // Allow slightly longer dash travel time

        while (dashTimer < maxDashDuration)
        {
            if (isDead || !isDashing) break; // Exit loop if died or flag externally reset (e.g., hit player)

            // Check if dash hit player (OnTriggerEnter2D sets dashMissed = false and might set isDashing=false)
            if (!dashMissed) break;

            // Optional: More robust obstacle check if needed
            // RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.6f, LayerMask.GetMask("Ground", "Walls"));
            // if (hit.collider != null) break;

            // Check if approximately reached target
             if (Vector2.Distance(transform.position, dashTarget) < 1.0f) break;


            dashTimer += Time.deltaTime;
            yield return null;
        }

        // --- Dash Finished (hit player, obstacle, reached target, or timed out) ---
        isDashing = false; // Finished dashing movement phase
        rb.velocity = Vector2.zero;

        // Report miss ONLY if the dash finished, the 'dashMissed' flag is still true, AND boss isn't dead
        if (rm != null && dashMissed && !isDead)
        {
            rm.ReportAttackMissed();
        }

        // Reset flag for the next dash attempt
        dashMissed = true;
    }

    // --- Phase Logic ---
    private void EnterPhase2() { /* ... as before ... */ }


    // --- Collision Handling ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Player"))
        {
            // Apply damage to player
            Health playerHealth = other.GetComponent<Health>();
            // Use 'damage' field inherited from EnemyDamage base class
            if (playerHealth != null) playerHealth.TakeDamage(damage);

            // Report direct collision hit to reward manager
            if (rm != null) rm.ReportHitPlayer();

            // Check if this collision happened during the dash movement phase
            if (isDashing) // Check isDashing, not isChargingDash
            {
                //Debug.Log("[AIBoss] Dash connected with player during movement!");
                dashMissed = false;   // Mark dash as successful hit
                isDashing = false;    // Stop dash movement immediately on hit
                rb.velocity = Vector2.zero; // Stop immediately
                // Optionally add knockback to player here
            }
            // isColliding flag seems unused, can be removed if not needed elsewhere
            // isColliding = true;
        }
        // Add checks for hitting walls/ground during dash if needed
        else if (isDashing && (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Walls")))
        {
             // Stop dash if hitting obstacles during movement phase
             //Debug.Log("[AIBoss] Dash hit obstacle: " + other.name);
             isDashing = false;
             rb.velocity = Vector2.zero;
        }
    }

    // OnTriggerExit2D likely not needed if isColliding is removed

    // --- Death Handling ---
    public void Die() // Override if EnemyDamage has a virtual Die method
    {
        if (isDead) return;
        isDead = true;
        //Debug.Log("[AIBoss] Die() method called.");

        StopAllCoroutines();
        DeactivateFlameAndWarning(); // Ensure cleanup

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        // Safely disable collider
        Collider2D col = GetComponent<Collider2D>();
        if(col != null) col.enabled = false;

        if (anim != null) anim.SetTrigger("Die");

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
        if (flame != null) flame.SetActive(false);

        // Find and destroy any active area markers by tag
        GameObject[] markers = GameObject.FindGameObjectsWithTag("FlameWarningMarker"); // Ensure markers have this tag
        foreach (GameObject marker in markers)
        {
            Destroy(marker);
        }
    }

    public void ResetAbilityStates()
    {
        cooldownTimer = 0;
        dashCooldownTimer = 0;
        fireAttackTimer = 0;
    }
}