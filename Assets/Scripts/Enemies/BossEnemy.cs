using System.Collections;

using UnityEngine;

// Manages the Boss's behavior, attacks, and state.
public class BossEnemy : EnemyDamage, IBoss // Assuming EnemyDamage provides base health/damage logic
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private bool _doRestHealth;
    [SerializeField] private Health _playerHealth;

    [Header("Attack Parameters")]
    [SerializeField] private float _attackCooldown = 6f;
    [SerializeField] private int _fireballDamage = 1;
    [SerializeField] private float _projectileSpeed = 5f;
    [SerializeField] private float _projectileSize = 0.3f;
    [SerializeField] private float _attackRange = 10f;
    [SerializeField] private AudioClip _fireballSound;

    [Header("Phase Control")]
    [SerializeField] private BossHealth _bossHealth; // Reference to the BossHealth script
    private bool _isPhase2 = false;

    [Header("Ranged Attack")]
    [SerializeField] private Transform _firepoint;
    [SerializeField] private GameObject _projectilePrefab; // Assuming this is needed for spawning
    [SerializeField] private GameObject[] _fireballs; // Array for pooling fireballs
    [SerializeField] private Transform _player; // Reference to the player's transform
    [SerializeField] private Transform _fireballHolder; // Parent for fireballs (optional)

    [Header("Flame Attack")]
    [SerializeField] private GameObject _flame; // Prefab or instance of the fire attack GameObject
    [SerializeField] private GameObject _areaMarkerPrefab; // Red marker prefab
    [SerializeField] private float _fireAttackCooldown = 6f;

    [Header("Charge Dash Attack")]
    [SerializeField] private float _dashChargeTime = 2f; // Charge time before dash
    [SerializeField] private float _dashSpeed = 10f; // Speed of the dash
    [SerializeField] private float _dashCooldown = 10f; // Cooldown for dash attack
    [SerializeField] private AudioClip _chargeSound;
    [SerializeField] private AudioClip _dashSound;
    [SerializeField] private GameObject _targetIconPrefab; // Prefab for the dash target icon
    private GameObject _targetIconInstance; // Instance of the dash target icon

    // Flags to track boss state
    private bool _isChargingDash = false; // Flag to track if dash is charging
    private bool _detectedPlayer;
    private bool _isDashing = false; // Flag to track if boss is currently dashing
    private Vector2 _dashTarget; // Target position for dash

    // Timers for attack cooldowns
    private float _fireAttackTimer = Mathf.Infinity;
    private float _cooldownTimer = Mathf.Infinity;
    private float _dashCooldownTimer = Mathf.Infinity; // Timer for dash attack cooldown

    private Animator _anim; // Reference to the Animator component
    private Vector3 _initialBossPosition;
    // Flag to track if the boss is dead
    private bool _isDead = false;

    // Flag to cancel flame deactivation coroutine (might not be needed with proper reset)
    // private bool isFlameDeactivationCanceled = false;

    // Reference to the dash coroutine so we can stop it
    private Coroutine _dashCoroutine;


    private void Awake()
    {
        _anim = GetComponent<Animator>();

        _initialBossPosition = gameObject.transform.position;

        // If bossHealth wasn't set in inspector, try to get it
        if (_bossHealth == null)
        {
            _bossHealth = GetComponent<BossHealth>();
            if (_bossHealth == null)
            {
                Debug.LogError("[BossEnemy] BossHealth component not found!");
            }
        }
        _rb = GetComponent<Rigidbody2D>();
        if (_playerHealth != null)
        {
            _playerHealth.OnDamaged += HandlePlayerDeath;
        }

    }

    private void Start()
    {
        // Initialize timers to be ready at the start of the first episode
        _cooldownTimer = _attackCooldown;
        _fireAttackTimer = _fireAttackCooldown;
        _dashCooldownTimer = _dashCooldown;

        // Initial state setup (can also be part of ResetState)
        _isPhase2 = false;
        _isChargingDash = false;
        _isDashing = false;
        _isDead = false;
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
        _attackCooldown = 7f;
        _fireAttackCooldown = 8.5f;
        _dashCooldown = 10f;
        _cooldownTimer = _attackCooldown;
        _fireAttackTimer = _fireAttackCooldown;
        _dashCooldownTimer = _dashCooldown;

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
        if (_isDead) return;  // Skip everything if the boss is dead
        if (!_detectedPlayer)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
            if (distanceToPlayer <= _attackRange && _playerHealth.currentHealth > 0)
                _detectedPlayer = true;
        }


        transform.rotation = Quaternion.Euler(0, 0, 0);
        // Check for phase 2 transition (50% health)
        if (!_isPhase2 && _bossHealth != null)
        {
            float healthPercentage = _bossHealth.GetHealthPercentage();
            if (healthPercentage <= 0.5f) // 50% health
            {
                _isPhase2 = true;
                EnterPhase2();
            }
        }

        // Update all cooldown timers
        if (!_isChargingDash)
        {
            _cooldownTimer += Time.deltaTime;
            _fireAttackTimer += Time.deltaTime;
            _dashCooldownTimer += Time.deltaTime;
        }



        // --- Movement Logic (Hardcoded - Consider removing for Q-Learning) ---
        // In a Q-Learning setup, the QL agent should call AIRequestMove
        // with the desired action type (e.g., Move_TowardsPlayer)
        // This hardcoded logic is here based on your request but may conflict with QL training.
        if (!_isChargingDash && _detectedPlayer) // Only move if not busy and outside attack range
        {
            // Calculate direction towards the player
            Vector3 fixPlayerPosition = _player.position;
            fixPlayerPosition.y -= 0.75f;
            Vector2 directionToPlayer = (fixPlayerPosition - transform.position).normalized;

            // Set Rigidbody velocity to move towards the player
            _rb.velocity = directionToPlayer * _movementSpeed;

            // Update animation parameter
            //if (anim != null) anim.SetBool("IsMoving", true);
        }
        else if (_isChargingDash || _detectedPlayer) // Stop moving if within attack range and not busy
        {
            _rb.velocity = Vector2.zero;
            //if (anim != null) anim.SetBool("IsMoving", false);
        }

        if (_isPhase2 && _detectedPlayer && _dashCooldownTimer >= _dashCooldown && !_isChargingDash)
        {
            _dashCooldownTimer = 0;
            ChargeDashAttack();
        }

        // Flip boss based on player's position
        if (_player.position.x < transform.position.x)
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
        if (_detectedPlayer && _cooldownTimer >= _attackCooldown)
        {
            _cooldownTimer = 0;
            RangedAttack();
        }

        _fireballHolder.localScale = transform.localScale;

        // Flame attack logic
        if (_detectedPlayer && _fireAttackTimer >= _fireAttackCooldown && !_flame.activeInHierarchy)
        {
            _fireAttackTimer = 0;
            SpawnFireAtPlayer();
        }
    }

    // --- Attack Methods (Called by QL Agent or Hardcoded Triggers) ---

    // Method to initiate Ranged Attack (can be called by QL agent)
    public void AIRequestRangedAttack()
    {
        if (_isDead || _isChargingDash || _isDashing || _cooldownTimer < _attackCooldown) return; // Check if boss is busy or attack is on cooldown

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

        GameObject projectile = _fireballs[fireballIndex];
        projectile.transform.position = _firepoint.position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.parent = null; // Unparent if needed

        BossProjectile bossProjectile = projectile.GetComponent<BossProjectile>();
        if (bossProjectile == null) { Debug.LogError("[BossEnemy] Fireball prefab missing BossProjectile script!"); return; }

        // Pass necessary references/data to the projectile
        bossProjectile.SetDamage(_fireballDamage);
        bossProjectile.SetSize(_projectileSize);
        // Ensure player is not null before accessing position
        if (_player != null)
        {
            bossProjectile.Launch(_firepoint.position, _player.transform.position, _projectileSpeed); // Launch towards calculated target!
        }
        else
        {
            Debug.LogWarning("[BossEnemy] Player transform is null, cannot launch fireball.");
            return; // Abort if player is null
        }


        if (_anim != null) _anim.SetTrigger("2_Attack"); // Use the correct attack trigger
        if (_fireballSound != null) SoundManager.instance.PlaySound(_fireballSound, gameObject); // Assuming SoundManager is correctly set up

        // Only reset cooldown if the launch was successful
        _cooldownTimer = 0f; // Reset cooldown *after* successful launch
        // currentEnergy -= fireballEnergyCost; // Consume resource *after* successful launch
    }

    private int FindFireball()
    {
        if (_fireballs == null) return -1; // Added null check for fireballs array
        for (int i = 0; i < _fireballs.Length; i++)
        {
            if (_fireballs[i] != null && !_fireballs[i].activeInHierarchy) // Added null check for array element
            {
                return i;
            }
        }
        return -1;
    }

    // Method to initiate Flame Attack (can be called by QL agent)
    public void AIRequestFlameAttack()
    {
        if (_isDead || _isChargingDash || _isDashing || _fireAttackTimer < _fireAttackCooldown || _flame == null || _flame.activeInHierarchy) return; // Check if boss is busy or attack is on cooldown or flame is active

        SpawnFireAtPlayer(); // Perform the attack logic
    }

    private void SpawnFireAtPlayer()
    {
        // This method contains the logic for spawning the flame attack sequence
        // It's called by AIRequestFlameAttack or the hardcoded trigger in Update

        if (_flame == null || _isDead || _player == null) return; // Don't spawn fire if boss is dead or player is null

        Vector3 targetPosition = new Vector3(_player.position.x, -11.5f, _player.position.z);

        // Stop existing flame coroutine if any (though DeactivateFlameAndWarning should handle cleanup)
        // if (flameCoroutine != null) StopCoroutine(flameCoroutine); // Example if you track it

        StartCoroutine(MarkAreaAndSpawnFire(targetPosition));

        // Only reset cooldown if the spawn was successful
        _fireAttackTimer = 0f;
    }

    private IEnumerator MarkAreaAndSpawnFire(Vector3 targetPosition)
    {
        if (_isDead) yield break; // Skip if the boss is dead

        GameObject marker = Instantiate(_areaMarkerPrefab, targetPosition, Quaternion.identity);
        marker.SetActive(true);
        marker.tag = "FlameWarningMarker"; // <-- Assign specific tag


        yield return new WaitForSeconds(1.5f);

        // Check if boss died during the wait
        if (_isDead)
        {
            Destroy(marker);
            yield break;
        }

        Destroy(marker);

        _flame.transform.position = targetPosition;
        _flame.SetActive(true);

        BossFlameAttack flameAttack = _flame.GetComponent<BossFlameAttack>();
        if (flameAttack != null)
        {
            flameAttack.Activate(targetPosition);
            // Stop existing deactivate coroutine if any
            // if (deactivateFlameCoroutine != null) StopCoroutine(deactivateFlameCoroutine); // Example if you track it
            // deactivateFlameCoroutine = StartCoroutine(DeactivateAfterDuration(flame, 3f)); // Track the coroutine
            StartCoroutine(DeactivateAfterDuration(_flame, 3f));
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
            if (_isDead || obj == null || !obj.activeInHierarchy) // Added obj null/active check
            {
                // If boss dies or object is no longer active, stop the coroutine
                if (obj != null) obj.SetActive(false); // Ensure it's off
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Only deactivate if the boss is not dead and the object is still active
        if (!_isDead && obj != null && obj.activeInHierarchy)
        {
            obj.SetActive(false);
        }
    }


    // Charge Dash Attack sequence
    private void ChargeDashAttack()
    {
        if (_isDead || _isChargingDash || _isDashing) return; // Double check state

        _isChargingDash = true;
        _isDashing = false; // Ensure dashing flag is false during charge

        // Set dash target with the player's position but change the Y to -1 (or adjust as needed)
        if (_player != null)
        {
            _dashTarget = new Vector3(_player.position.x, _player.position.y - 1.2f, _player.position.z);
        }
        else
        {
            Debug.LogWarning("[BossEnemy] Player transform is null, cannot set dash target.");
            _isChargingDash = false; // Cancel charge if player is null
            return;
        }


        // Instantiate or reuse the target icon
        if (_targetIconInstance == null)
        {
            // Instantiate at the calculated dashTarget position (adjust Y for visual)
            _targetIconInstance = Instantiate(_targetIconPrefab, new Vector3(_dashTarget.x, _dashTarget.y + 0.7f), Quaternion.identity); // Adjust Y for visual placement
            _targetIconInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            _targetIconInstance.tag = "DashTargetIndicator"; // <-- Assign specific tag

        }
        else
        {
            // Reuse and set position to the calculated dashTarget (adjust Y for visual)
            _targetIconInstance.transform.position = new Vector3(_dashTarget.x, _dashTarget.y + 0.7f); // Adjust Y for visual placement
            _targetIconInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            _targetIconInstance.SetActive(true);
            _targetIconInstance.tag = "DashTargetIndicator"; // <-- Ensure tag is set even on reuse

        }


        // Start charge-up animation if needed
        if (_anim != null) _anim.SetTrigger("ChargeDash");
        if (_chargeSound != null) SoundManager.instance.PlaySound(_chargeSound, gameObject);

        // Start the coroutine and keep a reference to it
        _dashCoroutine = StartCoroutine(PerformDashAttack());
    }

    private IEnumerator PerformDashAttack()
    {
        // Wait for charge time
        yield return new WaitForSeconds(_dashChargeTime);

        // Check if boss died during the charge time
        if (_isDead)
        {
            _isChargingDash = false;
            // Clean up target icon if it exists
            if (_targetIconInstance != null) _targetIconInstance.SetActive(false);
            yield break; // Stop the coroutine
        }

        // Deactivate the target icon before dashing
        if (_targetIconInstance != null)
        {
            _targetIconInstance.SetActive(false);
        }

        // Start the dash itself
        _isChargingDash = false; // Charge is complete
        _isDashing = true; // Boss is now dashing

        if (_anim != null) _anim.SetTrigger("Dash");
        if (_dashSound != null) SoundManager.instance.PlaySound(_dashSound, gameObject);

        Vector2 startPosition = transform.position;
        // Direction should be based on the calculated dashTarget
        Vector2 direction = (_dashTarget - startPosition).normalized;
        float dashDuration = 0.8f; // Dash duration (time to reach the target)
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            // Check if boss died during the dash
            if (_isDead)
            {
                _isDashing = false; // Stop dashing state
                // Stop movement immediately
                if (_rb != null) _rb.velocity = Vector2.zero;
                yield break; // Stop the coroutine
            }

            // Move the boss using Lerp for smooth movement to the target
            transform.position = Vector2.Lerp(startPosition, _dashTarget, (elapsedTime / dashDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure boss reaches the exact target position
        transform.position = _dashTarget;

        // Dash is complete
        _isDashing = false;

        // Reset dash cooldown after the dash is finished
        _dashCooldownTimer = 0f;
    }

    // Phase 2 implementation
    private void EnterPhase2()
    {
        Debug.Log("[BossEnemy] Entering Phase 2!");

        // Increase attack frequency in Phase 2
        _attackCooldown -= 0.5f; // Reduce attack cooldowns
        _fireAttackCooldown -= 1f; // Reduce flame attack cooldown

        // Ensure cooldowns are not negative
        _attackCooldown = Mathf.Max(0.1f, _attackCooldown);
        _fireAttackCooldown = Mathf.Max(0.1f, _fireAttackCooldown);


        // Visual indicator of phase change (optional)
        // You could change the boss color, play a special animation, etc.

        // Inform the player about phase change (optional)
        // You could show a UI element, play a sound, etc.
    }

    // This method is called when the boss dies (e.g., by BossHealth)
    public void Die()
    {
        if (_isDead) return;

        _isDead = true; // Set the boss as dead
        Debug.Log("[BossEnemy] Boss Died!");

        // Stop any running coroutines related to attacks or movement
        StopAllCoroutines(); // Stop all coroutines on this script

        // Deactivate any active visual markers or effects
        DeactivateFlameAndWarning(); // Ensure this also cleans up the dash target icon

        // Disable physics and collider
        if (_rb != null) _rb.velocity = Vector2.zero;
        Collider2D bossCol = GetComponent<Collider2D>();
        if (bossCol != null) bossCol.enabled = false;
        if (_rb != null) _rb.isKinematic = true; // Make kinematic to stop physics interactions

        // Play death animation
        if (_anim != null) _anim.SetTrigger("4_Death"); // Assuming you have a die animation trigger

        // Disable the script itself so Update doesn't run
        this.enabled = false;

        // Optionally, you can destroy the boss GameObject after a delay:
        Destroy(gameObject, 2f); // Adjust the time to fit your death animation length
    }

    // Method to deactivate flame and warning markers
    public void DeactivateFlameAndWarning()
    {
        if (_flame != null)
        {
            _flame.SetActive(false);  // Deactivate the flame attack
        }

        // Find and destroy any active area markers (including dash target icon if it has the tag)
        GameObject[] markers = GameObject.FindGameObjectsWithTag("AreaMarker"); // Assuming target icon has this tag
        foreach (GameObject marker in markers)
        {
            marker.SetActive(false);
        }

        // Explicitly deactivate target icon instance if it exists and wasn't destroyed by tag search
        if (_targetIconInstance != null)
        {
            _targetIconInstance.SetActive(false);
        }
    }

    // Public getters for AI observation (assuming AIBoss inherits from this or this is AIBoss)
    public bool IsCurrentlyChargingOrDashing()
    {
        return _isChargingDash || _isDashing;
    }

    public bool IsFireballReady()
    {
        return _cooldownTimer >= _attackCooldown;
    }

    public bool IsFlameTrapReady()
    {
        return _fireAttackTimer >= _fireAttackCooldown && (_flame == null || !_flame.activeInHierarchy);
    }

    public bool IsDashReady()
    {
        return _dashCooldownTimer >= _dashCooldown && _isPhase2; // Dash is only ready in phase 2
    }

    // Assuming PlayerMovement has a GetFacingDirection() method
    // Assuming PlayerMovement has isGrounded() and onWall() methods
    // Assuming PlayerAttack has IsAttackReady() method
    // Assuming PlayerMovement has ResetState() method
}
