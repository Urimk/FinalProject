using System.Collections;
using UnityEngine;

// Manages the Boss's behavior, attacks, and state.
public class BossEnemy : EnemyDamage // Assuming EnemyDamage provides base health/damage logic
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float movementSpeed;

    [Header("Attack Parameters")]
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private int fireballDamage = 1;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float projectileSize = 0.3f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private AudioClip fireballSound;

    [Header("Phase Control")]
    [SerializeField] private BossHealth bossHealth; // Reference to the BossHealth script
    private bool isPhase2 = false;

    [Header("Ranged Attack")]
    [SerializeField] private Transform firepoint;
    [SerializeField] private GameObject projectilePrefab; // Assuming this is needed for spawning
    [SerializeField] private GameObject[] fireballs; // Array for pooling fireballs
    [SerializeField] private Transform player; // Reference to the player's transform
    [SerializeField] private Transform fireballHolder; // Parent for fireballs (optional)

    [Header("Flame Attack")]
    [SerializeField] private GameObject flame; // Prefab or instance of the fire attack GameObject
    [SerializeField] private GameObject areaMarkerPrefab; // Red marker prefab
    [SerializeField] private float fireAttackCooldown = 8f;

    [Header("Charge Dash Attack")]
    [SerializeField] private float dashChargeTime = 2f; // Charge time before dash
    [SerializeField] private float dashSpeed = 10f; // Speed of the dash
    [SerializeField] private float dashCooldown = 10f; // Cooldown for dash attack
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private GameObject targetIconPrefab; // Prefab for the dash target icon
    private GameObject targetIconInstance; // Instance of the dash target icon

    // Flags to track boss state
    private bool isChargingDash = false; // Flag to track if dash is charging
    private bool isDashing = false; // Flag to track if boss is currently dashing
    private Vector2 dashTarget; // Target position for dash

    // Timers for attack cooldowns
    private float fireAttackTimer = Mathf.Infinity;
    private float cooldownTimer = Mathf.Infinity;
    private float dashCooldownTimer = Mathf.Infinity; // Timer for dash attack cooldown

    private Animator anim; // Reference to the Animator component

    // Flag to track if the boss is dead
    private bool isDead = false;

    // Flag to cancel flame deactivation coroutine (might not be needed with proper reset)
    // private bool isFlameDeactivationCanceled = false;

    // Reference to the dash coroutine so we can stop it
    private Coroutine dashCoroutine;


    private void Awake()
    {
        anim = GetComponent<Animator>();

        // If bossHealth wasn't set in inspector, try to get it
        if (bossHealth == null)
        {
            bossHealth = GetComponent<BossHealth>();
            if (bossHealth == null)
            {
                Debug.LogError("[BossEnemy] BossHealth component not found!");
            }
        }
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Initialize timers to be ready at the start of the first episode
        cooldownTimer = attackCooldown;
        fireAttackTimer = fireAttackCooldown;
        dashCooldownTimer = dashCooldown;

        // Initial state setup (can also be part of ResetState)
        isPhase2 = false;
        isChargingDash = false;
        isDashing = false;
        isDead = false;
    }

    // This method is called by EpisodeManager to reset the boss state for a new episode
    public void ResetState()
    {
        Debug.Log("[BossEnemy] Resetting BossEnemy state.");

        // Stop any running coroutines related to attacks or movement
        StopAllCoroutines(); // Stop all coroutines on this script

        // Reset flags and state variables
        isDead = false;
        isPhase2 = false; // Reset phase to 1
        isChargingDash = false;
        isDashing = false;
        // isFlameDeactivationCanceled = false; // Reset if used

        // Reset timers to be ready for new attacks
        attackCooldown = 3f;
        fireAttackCooldown = 6f;
        dashCooldown = 10f;
        cooldownTimer = attackCooldown;
        fireAttackTimer = fireAttackCooldown;
        dashCooldownTimer = dashCooldown;

        // Reset Rigidbody state
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = false; // Ensure physics are enabled
        }

        // Reset health (EpisodeManager also calls BossHealth.ResetHealth, but good to be safe)
        if (bossHealth != null) bossHealth.ResetHealth();

        // Deactivate any active visual markers or effects
        DeactivateFlameAndWarning(); // Ensure this also cleans up the dash target icon

        // Reset animator triggers/states if necessary
        if (anim != null)
        {
            anim.Rebind(); // Resets the animator to its default state
            anim.Update(0f); // Forces an update to apply the rebind
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
        if (isDead) return;  // Skip everything if the boss is dead

        // Check for phase 2 transition (50% health)
        if (!isPhase2 && bossHealth != null)
        {
            float healthPercentage = bossHealth.GetHealthPercentage();
            if (healthPercentage <= 0.5f) // 50% health
            {
                isPhase2 = true;
                EnterPhase2();
            }
        }

        // Update all cooldown timers
        if (!isChargingDash) {
            cooldownTimer += Time.deltaTime;
            fireAttackTimer += Time.deltaTime;
            dashCooldownTimer += Time.deltaTime;
        }

        

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // --- Movement Logic (Hardcoded - Consider removing for Q-Learning) ---
        // In a Q-Learning setup, the QL agent should call AIRequestMove
        // with the desired action type (e.g., Move_TowardsPlayer)
        // This hardcoded logic is here based on your request but may conflict with QL training.
        if (!isChargingDash && distanceToPlayer < attackRange) // Only move if not busy and outside attack range
        {
            // Calculate direction towards the player
            Vector2 directionToPlayer = (player.position - transform.position).normalized;

            // Set Rigidbody velocity to move towards the player
            rb.velocity = directionToPlayer * movementSpeed;

            // Update animation parameter
            //if (anim != null) anim.SetBool("IsMoving", true);
        }
        else if (isChargingDash || distanceToPlayer > attackRange) // Stop moving if within attack range and not busy
        {
            rb.velocity = Vector2.zero;
            //if (anim != null) anim.SetBool("IsMoving", false);
        }

        if (isPhase2 && distanceToPlayer <= attackRange * 1.5f && dashCooldownTimer >= dashCooldown && !isChargingDash)
        {
            dashCooldownTimer = 0;
            //ChargeDashAttack();
        }

        // Flip boss based on player's position
        if (player.position.x < transform.position.x)
        {
            // Player is to the left of the boss
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            // Player is to the right of the boss
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        // Ranged attack logic
        if (distanceToPlayer <= attackRange && cooldownTimer >= attackCooldown)
        {
            cooldownTimer = 0;
            //RangedAttack();
        }

        fireballHolder.localScale = transform.localScale;

        // Flame attack logic
        if (distanceToPlayer <= attackRange && fireAttackTimer >= fireAttackCooldown && !flame.activeInHierarchy)
        {
            fireAttackTimer = 0;
           // SpawnFireAtPlayer();
        }
    }

    // --- Attack Methods (Called by QL Agent or Hardcoded Triggers) ---

    // Method to initiate Ranged Attack (can be called by QL agent)
    public void AIRequestRangedAttack()
    {
        if (isDead || isChargingDash || isDashing || cooldownTimer < attackCooldown) return; // Check if boss is busy or attack is on cooldown

        RangedAttack(); // Perform the attack logic
    }

    private void RangedAttack()
    {
        // This method contains the actual logic for firing a fireball
        // It's called by AIRequestRangedAttack or the hardcoded trigger in Update

        //Debug.Log("[BossEnemy] Ranged attack initiated!");

        // --- Execute Fireball ---
        // Use your existing fireball spawning/pooling logic
        int fireballIndex = FindFireball(); // Assuming this finds an available projectile
        if (fireballIndex == -1) return; // No fireballs available

        // --- Consume Resource (if applicable) ---
        // if (currentEnergy < fireballEnergyCost) return false; // Double check or move up

        GameObject projectile = fireballs[fireballIndex];
        projectile.transform.position = firepoint.position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.parent = null; // Unparent if needed

        BossProjectile bossProjectile = projectile.GetComponent<BossProjectile>();
        if (bossProjectile == null) { Debug.LogError("[BossEnemy] Fireball prefab missing BossProjectile script!"); return; }

        // Pass necessary references/data to the projectile
        bossProjectile.SetDamage(fireballDamage);
        bossProjectile.SetSize(projectileSize);
        // Ensure player is not null before accessing position
        if (player != null)
        {
            bossProjectile.Launch(firepoint.position, player.transform.position, projectileSpeed); // Launch towards calculated target!
        } else {
            Debug.LogWarning("[BossEnemy] Player transform is null, cannot launch fireball.");
            return; // Abort if player is null
        }


        if (anim != null) anim.SetTrigger("Attack"); // Use the correct attack trigger
        if (fireballSound != null) SoundManager.instance.PlaySound(fireballSound); // Assuming SoundManager is correctly set up

        // Only reset cooldown if the launch was successful
        cooldownTimer = 0f; // Reset cooldown *after* successful launch
        // currentEnergy -= fireballEnergyCost; // Consume resource *after* successful launch
    }

    private int FindFireball()
    {
        if (fireballs == null) return -1; // Added null check for fireballs array
        for (int i = 0; i < fireballs.Length; i++)
        {
            if (fireballs[i] != null && !fireballs[i].activeInHierarchy) // Added null check for array element
            {
                return i;
            }
        }
        return -1;
    }

    // Method to initiate Flame Attack (can be called by QL agent)
    public void AIRequestFlameAttack()
    {
        if (isDead || isChargingDash || isDashing || fireAttackTimer < fireAttackCooldown || flame == null || flame.activeInHierarchy) return; // Check if boss is busy or attack is on cooldown or flame is active

        SpawnFireAtPlayer(); // Perform the attack logic
    }

    private void SpawnFireAtPlayer()
    {
        // This method contains the logic for spawning the flame attack sequence
        // It's called by AIRequestFlameAttack or the hardcoded trigger in Update

        if (flame == null || isDead || player == null) return; // Don't spawn fire if boss is dead or player is null

        Vector3 targetPosition = new Vector3(player.position.x, -11.5f, player.position.z);

        // Stop existing flame coroutine if any (though DeactivateFlameAndWarning should handle cleanup)
        // if (flameCoroutine != null) StopCoroutine(flameCoroutine); // Example if you track it

        StartCoroutine(MarkAreaAndSpawnFire(targetPosition));

        // Only reset cooldown if the spawn was successful
        fireAttackTimer = 0f;
    }

    private IEnumerator MarkAreaAndSpawnFire(Vector3 targetPosition)
    {
        if (isDead) yield break; // Skip if the boss is dead

        GameObject marker = Instantiate(areaMarkerPrefab, targetPosition, Quaternion.identity);
        marker.SetActive(true);
        marker.tag = "FlameWarningMarker"; // <-- Assign specific tag


        yield return new WaitForSeconds(1.5f);

        // Check if boss died during the wait
        if (isDead)
        {
            Destroy(marker);
            yield break;
        }

        Destroy(marker);

        flame.transform.position = targetPosition;
        flame.SetActive(true);

        BossFlameAttack flameAttack = flame.GetComponent<BossFlameAttack>();
        if (flameAttack != null)
        {
            flameAttack.Activate(targetPosition);
            // Stop existing deactivate coroutine if any
            // if (deactivateFlameCoroutine != null) StopCoroutine(deactivateFlameCoroutine); // Example if you track it
            // deactivateFlameCoroutine = StartCoroutine(DeactivateAfterDuration(flame, 3f)); // Track the coroutine
             StartCoroutine(DeactivateAfterDuration(flame, 3f));
        }
        else
        {
            Debug.LogError("[BossEnemy] Flame object is missing the BossFlameAttack script!");
        }
    }

    private IEnumerator DeactivateAfterDuration(GameObject obj, float delay)
    {
        float timer = 0f;

        while (timer < delay)
        {
            if (isDead || obj == null || !obj.activeInHierarchy) // Added obj null/active check
            {
                // If boss dies or object is no longer active, stop the coroutine
                if (obj != null) obj.SetActive(false); // Ensure it's off
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Only deactivate if the boss is not dead and the object is still active
        if (!isDead && obj != null && obj.activeInHierarchy)
        {
            obj.SetActive(false);
        }
    }

    // Method to initiate Charge Dash Attack (can be called by QL agent)
     public void AIRequestChargeDashAttack()
     {
         // Check if boss is dead, already busy, or dash is on cooldown, or not in phase 2
         if (isDead || isChargingDash || isDashing || dashCooldownTimer < dashCooldown || !isPhase2) return;

         ChargeDashAttack(); // Perform the attack logic
     }


    // Charge Dash Attack sequence
    private void ChargeDashAttack()
    {
        if (isDead || isChargingDash || isDashing) return; // Double check state

        isChargingDash = true;
        isDashing = false; // Ensure dashing flag is false during charge

        // Set dash target with the player's position but change the Y to -1 (or adjust as needed)
        if (player != null)
        {
             dashTarget = new Vector3(player.position.x, player.position.y - 1.2f, player.position.z);
        } else {
             Debug.LogWarning("[BossEnemy] Player transform is null, cannot set dash target.");
             isChargingDash = false; // Cancel charge if player is null
             return;
        }


        // Instantiate or reuse the target icon
        if (targetIconInstance == null)
        {
             // Instantiate at the calculated dashTarget position (adjust Y for visual)
             targetIconInstance = Instantiate(targetIconPrefab, new Vector3(dashTarget.x, dashTarget.y + 0.7f), Quaternion.identity); // Adjust Y for visual placement
             targetIconInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
             targetIconInstance.tag = "DashTargetIndicator"; // <-- Assign specific tag

        }
        else
        {
             // Reuse and set position to the calculated dashTarget (adjust Y for visual)
             targetIconInstance.transform.position = new Vector3(dashTarget.x, dashTarget.y + 0.7f); // Adjust Y for visual placement
             targetIconInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
             targetIconInstance.SetActive(true);
            targetIconInstance.tag = "DashTargetIndicator"; // <-- Ensure tag is set even on reuse

        }


        // Start charge-up animation if needed
        if (anim != null) anim.SetTrigger("ChargeDash");
        if (chargeSound != null) SoundManager.instance.PlaySound(chargeSound);

        // Start the coroutine and keep a reference to it
        dashCoroutine = StartCoroutine(PerformDashAttack());
    }

    private IEnumerator PerformDashAttack()
    {
        // Wait for charge time
        yield return new WaitForSeconds(dashChargeTime);

        // Check if boss died during the charge time
        if (isDead)
        {
            isChargingDash = false;
            // Clean up target icon if it exists
            if (targetIconInstance != null) targetIconInstance.SetActive(false);
            yield break; // Stop the coroutine
        }

        // Deactivate the target icon before dashing
        if (targetIconInstance != null)
        {
            targetIconInstance.SetActive(false);
        }

        // Start the dash itself
        isChargingDash = false; // Charge is complete
        isDashing = true; // Boss is now dashing

        if (anim != null) anim.SetTrigger("Dash");
        if (dashSound != null) SoundManager.instance.PlaySound(dashSound);

        Vector2 startPosition = transform.position;
        // Direction should be based on the calculated dashTarget
        Vector2 direction = (dashTarget - startPosition).normalized;
        float dashDuration = 0.8f; // Dash duration (time to reach the target)
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            // Check if boss died during the dash
            if (isDead)
            {
                isDashing = false; // Stop dashing state
                // Stop movement immediately
                if (rb != null) rb.velocity = Vector2.zero;
                yield break; // Stop the coroutine
            }

            // Move the boss using Lerp for smooth movement to the target
            transform.position = Vector2.Lerp(startPosition, dashTarget, (elapsedTime / dashDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure boss reaches the exact target position
        transform.position = dashTarget;

        // Dash is complete
        isDashing = false;

        // Reset dash cooldown after the dash is finished
        dashCooldownTimer = 0f;
    }

    // Phase 2 implementation
    private void EnterPhase2()
    {
        Debug.Log("[BossEnemy] Entering Phase 2!");

        // Increase attack frequency in Phase 2
        attackCooldown -= 0.5f; // Reduce attack cooldowns
        fireAttackCooldown -= 1f; // Reduce flame attack cooldown

        // Ensure cooldowns are not negative
        attackCooldown = Mathf.Max(0.1f, attackCooldown);
        fireAttackCooldown = Mathf.Max(0.1f, fireAttackCooldown);


        // Visual indicator of phase change (optional)
        // You could change the boss color, play a special animation, etc.

        // Inform the player about phase change (optional)
        // You could show a UI element, play a sound, etc.
    }

    // This method is called when the boss dies (e.g., by BossHealth)
    public void Die()
    {
        if (isDead) return;

        isDead = true; // Set the boss as dead
        Debug.Log("[BossEnemy] Boss Died!");

        // Stop any running coroutines related to attacks or movement
        StopAllCoroutines(); // Stop all coroutines on this script

        // Deactivate any active visual markers or effects
        DeactivateFlameAndWarning(); // Ensure this also cleans up the dash target icon

        // Disable physics and collider
        if (rb != null) rb.velocity = Vector2.zero;
        Collider2D bossCol = GetComponent<Collider2D>();
        if (bossCol != null) bossCol.enabled = false;
        if (rb != null) rb.isKinematic = true; // Make kinematic to stop physics interactions

        // Play death animation
        if (anim != null) anim.SetTrigger("Die"); // Assuming you have a die animation trigger

        // Disable the script itself so Update doesn't run
        this.enabled = false;

        // Optionally, you can destroy the boss GameObject after a delay:
        // Destroy(gameObject, 2f); // Adjust the time to fit your death animation length
    }

    // Method to deactivate flame and warning markers
    public void DeactivateFlameAndWarning()
    {
        if (flame != null)
        {
            flame.SetActive(false);  // Deactivate the flame attack
        }

        // Find and destroy any active area markers (including dash target icon if it has the tag)
        GameObject[] markers = GameObject.FindGameObjectsWithTag("AreaMarker"); // Assuming target icon has this tag
        foreach (GameObject marker in markers)
        {
            marker.SetActive(false);
        }

        // Explicitly deactivate target icon instance if it exists and wasn't destroyed by tag search
        if (targetIconInstance != null)
        {
            targetIconInstance.SetActive(false);
        }
    }

    // Public getters for AI observation (assuming AIBoss inherits from this or this is AIBoss)
    public bool IsCurrentlyChargingOrDashing()
    {
        return isChargingDash || isDashing;
    }

    public bool IsFireballReady()
    {
        return cooldownTimer >= attackCooldown;
    }

    public bool IsFlameTrapReady()
    {
        return fireAttackTimer >= fireAttackCooldown && (flame == null || !flame.activeInHierarchy);
    }

    public bool IsDashReady()
    {
        return dashCooldownTimer >= dashCooldown && isPhase2; // Dash is only ready in phase 2
    }

    // Assuming PlayerMovement has a GetFacingDirection() method
    // Assuming PlayerMovement has isGrounded() and onWall() methods
    // Assuming PlayerAttack has IsAttackReady() method
    // Assuming PlayerMovement has ResetState() method
}
