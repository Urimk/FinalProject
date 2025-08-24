using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Manages the Boss's behavior, attacks, and state with improved organization and reset handling.
/// </summary>
public class BossEnemy : EnemyDamage, IBoss
{
    // ==================== Constants ====================
    private const float DefaultAttackCooldown = 7f;
    private const float DefaultFireAttackCooldown = 8.5f;
    private const float DefaultDashCooldown = 10f;
    private const float Phase2AttackCooldownReduction = 0.5f;
    private const float Phase2FireAttackCooldownReduction = 1f;
    private const float MinCooldown = 0.1f;
    private const float FlameMarkerWait = 1.5f;
    protected const float FlameDeactivateDuration = 3f;
    private const float DashMarkerScale = 0.5f;
    private const float DashTargetYOffset = 0.7f;
    private const float DashTargetYAdjust = -1.2f;
    protected const float DashDuration = 0.8f;
    private const float DeathDestroyDelay = 2f;
    private const string FlameWarningTag = "FlameWarningMarker";
    private const string DashTargetIndicatorTag = "DashTargetIndicator";

    // ==================== Inspector Fields ====================
    [Header("Core References")]
    [Tooltip("Reference to the boss's Rigidbody2D component.")]
    [FormerlySerializedAs("rb")]
    [SerializeField] protected Rigidbody2D _rb;
    [Tooltip("Movement speed of the boss.")]
    [FormerlySerializedAs("movementSpeed")]
    [SerializeField] private float _movementSpeed;
    [Tooltip("Should the boss reset health on state reset?")]
    [FormerlySerializedAs("doRestHealth")]
    [SerializeField] private bool _doRestHealth;
    [Tooltip("Reference to the player's Health component.")]
    [FormerlySerializedAs("playerHealth")]
    [SerializeField] private Health _playerHealth;
    [Tooltip("Reference to the BossHealth component.")]
    [FormerlySerializedAs("bossHealth")]
    [SerializeField] private BossHealth _bossHealth;
    [Tooltip("Reference to the BossRewardManager component.")]
    [FormerlySerializedAs("bossRewardManager")]
    [SerializeField] private BossRewardManager _bossRewardManager;

    [Header("Attack Parameters")]
    [Tooltip("Cooldown time between attacks.")]
    [FormerlySerializedAs("attackCooldown")]
    [SerializeField] private float _attackCooldown = 6f;
    [Tooltip("Damage dealt by fireballs.")]
    [FormerlySerializedAs("fireballDamage")]
    [SerializeField] private int _fireballDamage = 1;
    [Tooltip("Speed of projectile attacks.")]
    [FormerlySerializedAs("projectileSpeed")]
    [SerializeField] private float _projectileSpeed = 5f;
    [Tooltip("Size of projectile attacks.")]
    [FormerlySerializedAs("projectileSize")]
    [SerializeField] private float _projectileSize = 0.3f;
    [Tooltip("Attack range for detecting the player.")]
    [FormerlySerializedAs("attackRange")]
    [SerializeField] private float _attackRange = 10f;
    [Tooltip("Sound played when firing a fireball.")]
    [FormerlySerializedAs("fireballSound")]
    [SerializeField] private AudioClip _fireballSound;

    [Header("Phase Control")]
    [Tooltip("Reference to the enraged visual effect.")]
    [SerializeField] private Transform _enragedEffect;

    [Header("Ranged Attack")]
    [Tooltip("Transform where fireballs are spawned from.")]
    [FormerlySerializedAs("firepoint")]
    [SerializeField] private Transform _firepoint;
    [Tooltip("Projectile prefab for ranged attacks.")]
    [FormerlySerializedAs("projectilePrefab")]
    [SerializeField] private GameObject _projectilePrefab;
    [Tooltip("Array of fireball GameObjects for pooling.")]
    [FormerlySerializedAs("fireballs")]
    [SerializeField] private GameObject[] _fireballs;
    [Tooltip("References to the player Transforms.")]
    [SerializeField] private List<Transform> _players;
    [Tooltip("Reference to the fireball holder Transform.")]
    [FormerlySerializedAs("fireballHolder")]
    [SerializeField] protected Transform _fireballHolder;

    [Header("Flame Attack")]
    [Tooltip("Reference to the flame GameObject.")]
    [FormerlySerializedAs("flame")]
    [SerializeField] private GameObject _flame;
    [Tooltip("Prefab for the area marker.")]
    [FormerlySerializedAs("areaMarkerPrefab")]
    [SerializeField] private GameObject _areaMarkerPrefab;
    [Tooltip("Cooldown time between flame attacks.")]
    [FormerlySerializedAs("fireAttackCooldown")]
    [SerializeField] private float _fireAttackCooldown = 6f;

    [Header("Charge Dash Attack")]
    [Tooltip("Time to charge before dashing.")]
    [FormerlySerializedAs("dashChargeTime")]
    [SerializeField] protected float _dashChargeTime = 2f;
    [Tooltip("Speed of the dash attack.")]
    [FormerlySerializedAs("dashSpeed")]
    [SerializeField] protected float _dashSpeed = 10f;
    [Tooltip("Cooldown time between dash attacks.")]
    [FormerlySerializedAs("dashCooldown")]
    [SerializeField] private float _dashCooldown = 10f;
    [Tooltip("Sound played when charging dash.")]
    [FormerlySerializedAs("chargeSound")]
    [SerializeField] protected AudioClip _chargeSound;
    [Tooltip("Sound played when dashing.")]
    [FormerlySerializedAs("dashSound")]
    [SerializeField] protected AudioClip _dashSound;
    [Tooltip("Prefab for the target icon.")]
    [FormerlySerializedAs("targetIconPrefab")]
    [SerializeField] private GameObject _targetIconPrefab;
    


    // ==================== Private Fields ====================
    protected GameObject _targetIconInstance;
    protected Animator _anim;
    private Vector3 _initialBossPosition;
    private float _initialDamage;
    protected float _initialCooldown;
    protected float _initialFireAttackCooldown;
    protected float _initialDashCooldown;

    // ==================== Protected Properties ====================
    /// <summary>
    /// Gets the list of player transforms.
    /// </summary>
    protected List<Transform> Players { get; set; }

    /// <summary>
    /// Gets whether the player has been detected.
    /// </summary>
    protected bool IsPlayerDetected => _detectedPlayer;

    /// <summary>
    /// Gets whether the boss is currently dashing.
    /// </summary>
    protected bool IsDashing => _isDashing;

    /// <summary>
    /// Gets the boss's movement speed.
    /// </summary>
    protected float MovementSpeed => _movementSpeed;

    /// <summary>
    /// Gets the projectile speed.
    /// </summary>
    protected float ProjectileSpeed => _projectileSpeed;

    /// <summary>
    /// Gets the flame GameObject.
    /// </summary>
    protected GameObject FlameObject => _flame;
    

    
    // ==================== State Management ====================
    protected bool _isDead = false;
    private bool _isPhase2 = false;
    private bool _detectedPlayer = false;
    protected bool _isChargingDash = false;
    protected bool _isDashing = false;
    protected Vector2 _dashTarget;
    protected Coroutine _dashCoroutine;
    
    // ==================== Cooldown Management ====================
    protected float _cooldownTimer = Mathf.Infinity;
    protected float _fireAttackTimer = Mathf.Infinity;
    protected float _dashCooldownTimer = Mathf.Infinity;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Awake callback. Initializes references and validates required components.
    /// </summary>
    protected virtual void Awake()
    {
        InitializeComponents();
        ValidateReferences();
        SetupEventSubscriptions();
    }

    /// <summary>
    /// Unity Start callback. Initializes boss state and timers.
    /// </summary>
    protected virtual void Start()
    {
        InitializeBossState();
    }

    // ==================== Initialization ====================
    /// <summary>
    /// Initializes all required components and caches references.
    /// </summary>
    private void InitializeComponents()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _initialBossPosition = transform.position;
        _initialDamage = _damage;
        _initialCooldown = _attackCooldown;
        _initialFireAttackCooldown = _fireAttackCooldown;
        _initialDashCooldown = _dashCooldown;
        
        // Auto-find BossHealth if not assigned
        if (_bossHealth == null)
        {
            _bossHealth = GetComponent<BossHealth>();
        }
    }

    /// <summary>
    /// Validates that all required references are properly assigned.
    /// </summary>
    private void ValidateReferences()
    {
            if (_bossHealth == null)
            {
                Debug.LogError("[BossEnemy] BossHealth component not found!");
            }
        if (_rb == null)
        {
            Debug.LogError("[BossEnemy] Rigidbody2D component not found!");
        }
        if (_firepoint == null)
        {
            Debug.LogError("[BossEnemy] Firepoint Transform not assigned!");
        }
        if (_flame == null)
        {
            Debug.LogError("[BossEnemy] Flame GameObject not assigned!");
        }
    }

    /// <summary>
    /// Sets up event subscriptions for player health monitoring.
    /// </summary>
    private void SetupEventSubscriptions()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDamaged += HandlePlayerDeath;
        }
    }
    


    /// <summary>
    /// Initializes the boss state for a new episode.
    /// </summary>
    private void InitializeBossState()
    {
        ResetCooldowns();
        ResetPhaseState();
        ResetMovementState();
        ResetDetectionState();
    }

    // ==================== State Reset Methods ====================
    /// <summary>
    /// Resets all cooldown timers to their initial values.
    /// </summary>
    private void ResetCooldowns()
    {
        _cooldownTimer = _attackCooldown;
        _fireAttackTimer = _fireAttackCooldown;
        _dashCooldownTimer = _dashCooldown;
    }

    /// <summary>
    /// Resets the phase state and related visual effects.
    /// </summary>
    private void ResetPhaseState()
    {
        Debug.Log("[BossEnemy] ResetPhaseState - Setting _isPhase2 to false");
        _isPhase2 = false;
        
        // Reset cooldowns to default values
        _attackCooldown = _initialCooldown;
        _fireAttackCooldown = _initialFireAttackCooldown;
        _dashCooldown = _initialDashCooldown;
        
        // Reset damage to initial value
        _damage = _initialDamage;
        
        // Hide enraged effect
        if (_enragedEffect != null)
        {
            _enragedEffect.gameObject.SetActive(false);
        }
        
        Debug.Log("[BossEnemy] ResetPhaseState complete - _isPhase2: " + _isPhase2 + ", Damage: " + _damage);
    }

    /// <summary>
    /// Resets movement and physics state.
    /// </summary>
    private void ResetMovementState()
    {
        _isChargingDash = false;
        _isDashing = false;
        
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.isKinematic = false;
        }
        
        transform.position = _initialBossPosition;
        transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Resets player detection and attack state.
    /// </summary>
    private void ResetDetectionState()
    {
        _detectedPlayer = false;
        _isDead = false;
    }

    // ==================== Public Reset Interface ====================
    /// <summary>
    /// Handles logic when the player dies. Resets the boss state if the player's health reaches zero.
    /// </summary>
    /// <param name="damage">Unused parameter (for event compatibility).</param>
    public void HandlePlayerDeath(float damage)
    {
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0)
        {
            ResetState();
        }
    }



    /// <summary>
    /// Resets ability states (called by EpisodeManager).
    /// </summary>
    public void ResetAbilityStates()
    {
        Debug.Log("[BossEnemy] ResetAbilityStates called - performing complete reset");
        ResetState(); // Call the complete reset instead of just InitializeBossState
    }

    // ==================== Virtual Methods for Inheritance ====================
    /// <summary>
    /// Virtual method for resetting state. Can be overridden by derived classes.
    /// </summary>
    public virtual void ResetState()
    {
        Debug.Log("[BossEnemy] Resetting BossEnemy state.");
        
        // Stop all ongoing coroutines FIRST
        StopAllCoroutines();
        
        // Clear all hazards FIRST (before resetting state)
        DeactivateFlameAndWarning();
        
        // Reset health FIRST (before resetting phase state)
        if (_doRestHealth && _bossHealth != null)
        {
            _bossHealth.ResetHealth();
        }
        
        // Reset all state
        ResetPhaseState();
        ResetMovementState();
        ResetDetectionState();
        ResetCooldowns();
        
        // Reset animation
        if (_anim != null)
        {
            _anim.Rebind();
            _anim.Update(0f);
        }
        
        // Re-enable components
        gameObject.SetActive(true);
        enabled = true;
        
        Collider2D bossCol = GetComponent<Collider2D>();
        if (bossCol != null) bossCol.enabled = true;
        
        // Re-enable Rigidbody2D if it was made kinematic during death
        if (_rb != null && _rb.isKinematic)
        {
            _rb.isKinematic = false;
        }
        
        Debug.Log("[BossEnemy] Reset complete - Phase: " + _isPhase2 + ", Damage: " + _damage);
    }

    /// <summary>
    /// Virtual method for performing ranged attacks. Can be overridden by derived classes.
    /// </summary>
    /// <param name="targetPosition">The target position for the attack.</param>
    /// <returns>True if the attack was performed successfully.</returns>
    protected virtual bool PerformRangedAttack(Vector2 targetPosition)
    {
        int fireballIndex = FindFireball();
        if (fireballIndex == -1) return false;
        
        GameObject projectile = _fireballs[fireballIndex];
        projectile.transform.position = _firepoint.position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.parent = null;
        
        BossProjectile bossProjectile = projectile.GetComponent<BossProjectile>();
        if (bossProjectile == null)
        {
            Debug.LogError("[BossEnemy] Fireball prefab missing BossProjectile script!");
            return false;
        }
        
        bossProjectile.SetDamage(_fireballDamage);
        bossProjectile.SetSize(_projectileSize);
        bossProjectile.Launch(_firepoint.position, targetPosition, _projectileSpeed);
        if (_bossRewardManager != null)
        {
            _bossRewardManager.ReportBossAttack();
        }
        
        if (_anim != null)
        {
            _anim.SetTrigger("2_Attack");
        }
        
        if (_fireballSound != null)
        {
            SoundManager.instance.PlaySound(_fireballSound, gameObject);
        }
        
        _cooldownTimer = 0f;
        return true;
    }

    /// <summary>
    /// Virtual method for performing flame attacks. Can be overridden by derived classes.
    /// </summary>
    /// <param name="placementPosition">The position to place the flame.</param>
    /// <returns>True if the attack was performed successfully.</returns>
    protected virtual bool PerformFlameAttack(Vector2 placementPosition)
    {
        if (_flame == null || _isDead) return false;
        
        StartCoroutine(MarkAreaAndSpawnFire(placementPosition));
        _fireAttackTimer = 0f;
        return true;
    }

    /// <summary>
    /// Virtual method for performing dash attacks. Can be overridden by derived classes.
    /// </summary>
    /// <param name="dashTarget">The target position for the dash.</param>
    /// <returns>True if the attack was performed successfully.</returns>
    protected virtual bool PerformDashAttack(Vector2 dashTarget)
    {
        if (_isDead || _isChargingDash || _isDashing) return false;
        
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
        
        StartCoroutine(PerformDashAttack());
        _dashCooldownTimer = 0f;
        return true;
    }

    /// <summary>
    /// Virtual method for setting velocity. Can be overridden by derived classes.
    /// </summary>
    /// <param name="velocity">The velocity to set.</param>
    protected virtual void SetVelocity(Vector2 velocity)
    {
        if (_rb != null)
        {
            _rb.velocity = velocity;
        }
    }

    /// <summary>
    /// Virtual method for getting velocity. Can be overridden by derived classes.
    /// </summary>
    /// <returns>The current velocity.</returns>
    protected virtual Vector2 GetVelocity()
    {
        return _rb != null ? _rb.velocity : Vector2.zero;
    }

    /// <summary>
    /// Virtual method for stopping dashing. Can be overridden by derived classes.
    /// </summary>
    protected virtual void StopDashing()
    {
        _isDashing = false;
        if (_rb != null) _rb.velocity = Vector2.zero;
    }

    /// <summary>
    /// Virtual method for setting attack cooldown. Can be overridden by derived classes.
    /// </summary>
    /// <param name="cooldown">The cooldown value to set.</param>
    protected virtual void SetAttackCooldown(float cooldown)
    {
        _attackCooldown = cooldown;
    }

    /// <summary>
    /// Virtual method for setting fire attack cooldown. Can be overridden by derived classes.
    /// </summary>
    /// <param name="cooldown">The cooldown value to set.</param>
    protected virtual void SetFireAttackCooldown(float cooldown)
    {
        _fireAttackCooldown = cooldown;
    }

    /// <summary>
    /// Virtual method for setting dash cooldown. Can be overridden by derived classes.
    /// </summary>
    /// <param name="cooldown">The cooldown value to set.</param>
    protected virtual void SetDashCooldown(float cooldown)
    {
        _dashCooldown = cooldown;
    }

    /// <summary>
    /// Virtual method for handling flame deactivation. Can be overridden by derived classes.
    /// </summary>
    /// <param name="flameObject">The flame object that was deactivated.</param>
    protected virtual void OnFlameDeactivated(GameObject flameObject)
    {
        // Base implementation does nothing
    }

    /// <summary>
    /// Virtual method for handling dash completion. Can be overridden by derived classes.
    /// </summary>
    protected virtual void OnDashCompleted()
    {
        // Base implementation does nothing
    }

    /// <summary>
    /// Virtual method for handling trigger collisions. Can be overridden by derived classes.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;
        
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null) playerHealth.TakeDamage(_damage);
        }
    }
    


    // ==================== Update Logic ====================
    /// <summary>
    /// Unity Update callback. Handles boss behavior and state management.
    /// </summary>
    protected virtual void Update()
    {
        if (_isDead) return;
        
        // Fix rotation bug - ensure boss doesn't rotate
        transform.rotation = Quaternion.Euler(0, 0, 0);
        
        UpdatePlayerDetection();
        UpdatePhaseTransition();
        UpdateCooldowns();
        UpdateMovement();
        UpdateFacingDirection();
        UpdateAttackLogic();
    }

    /// <summary>
    /// Updates player detection logic.
    /// </summary>
    protected virtual void UpdatePlayerDetection()
    {
        if (!_detectedPlayer)
        {
            Transform closestPlayer = GetClosestPlayer();
            if (closestPlayer != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, closestPlayer.position);
                if (distanceToPlayer <= _attackRange && _playerHealth.CurrentHealth > 0)
                {
                    _detectedPlayer = true;
            }
        }
        }
    }

    /// <summary>
    /// Updates phase transition logic.
    /// </summary>
    protected virtual void UpdatePhaseTransition()
    {
        if (!_isPhase2 && _bossHealth != null)
        {
            float healthPercentage = _bossHealth.GetHealthPercentage();
            if (healthPercentage <= 0.5f)
            {
                Debug.Log("[BossEnemy] UpdatePhaseTransition - Health percentage: " + healthPercentage + " <= 0.5f, entering Phase 2");
                EnterPhase2();
            }
        }
    }

    /// <summary>
    /// Updates cooldown timers.
    /// </summary>
    protected virtual void UpdateCooldowns()
    {
        if (!_isChargingDash)
        {
            _cooldownTimer += Time.deltaTime;
            _fireAttackTimer += Time.deltaTime;
            _dashCooldownTimer += Time.deltaTime;
        }
    }

    /// <summary>
    /// Updates boss movement logic.
    /// </summary>
    protected virtual void UpdateMovement()
    {
        if (!_isChargingDash && !_isDashing && _detectedPlayer)
        {
            Transform closestPlayer = GetClosestPlayer();
            if (closestPlayer != null)
            {
                Vector3 targetPosition = closestPlayer.position;
                targetPosition.y -= 0.75f;
                Vector2 directionToPlayer = (targetPosition - transform.position).normalized;
            _rb.velocity = directionToPlayer * _movementSpeed;
        }
        }
        else if (_isChargingDash)
        {
            // Only stop movement during charging, not during dashing
            _rb.velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Updates boss facing direction based on player position.
    /// </summary>
    private void UpdateFacingDirection()
    {
        Transform closestPlayer = GetClosestPlayer();
        if (closestPlayer != null)
        {
            bool shouldFaceLeft = closestPlayer.position.x < transform.position.x;
            float scaleX = Mathf.Abs(transform.localScale.x) * (shouldFaceLeft ? 1f : -1f);
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }
        
        // Update fireball holder scale
        if (_fireballHolder != null)
        {
            _fireballHolder.localScale = transform.localScale;
        }
    }

    /// <summary>
    /// Updates attack logic and triggers attacks when ready.
    /// </summary>
    private void UpdateAttackLogic()
    {
        if (!_detectedPlayer) return;
        
        // Trigger dash attack in phase 2
        if (_isPhase2 && _dashCooldownTimer >= _dashCooldown && !_isChargingDash)
        {
            _dashCooldownTimer = 0;
            ChargeDashAttack();
        }
        
        // Trigger ranged attack
        if (_cooldownTimer >= _attackCooldown)
        {
            _cooldownTimer = 0;
            RangedAttack();
        }
        
        // Trigger flame attack
        if (_fireAttackTimer >= _fireAttackCooldown && !_flame.activeInHierarchy)
        {
            _fireAttackTimer = 0;
            SpawnFireAtPlayer();
        }
    }

    // ==================== Player Detection ====================
    /// <summary>
    /// Finds the closest player from the players list.
    /// </summary>
    /// <returns>The closest player Transform, or null if no players found.</returns>
    protected virtual Transform GetClosestPlayer()
    {
        Transform closest = null;
        float shortestDistance = float.MaxValue;

        foreach (Transform player in _players)
        {
            if (player == null) continue;

            float distance = Vector2.Distance(transform.position, player.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closest = player;
            }
        }

        return closest;
    }

    // ==================== Attack Methods ====================

    /// <summary>
    /// Performs the actual ranged attack logic (fireball launch).
    /// </summary>
    private void RangedAttack()
    {
        int fireballIndex = FindFireball();
        if (fireballIndex == -1) return;
        
        GameObject projectile = _fireballs[fireballIndex];
        projectile.transform.position = _firepoint.position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.parent = null;
        
        BossProjectile bossProjectile = projectile.GetComponent<BossProjectile>();
        if (bossProjectile == null)
        {
            Debug.LogError("[BossEnemy] Fireball prefab missing BossProjectile script!");
            return;
        }
        
        bossProjectile.SetDamage(_fireballDamage);
        bossProjectile.SetSize(_projectileSize);
        
        Transform closestPlayer = GetClosestPlayer();
        if (closestPlayer != null)
        {
            bossProjectile.Launch(_firepoint.position, closestPlayer.position, _projectileSpeed);
        }
        else
        {
            Debug.LogWarning("[BossEnemy] Player transform is null, cannot launch fireball.");
            return;
        }
        
        if (_anim != null)
        {
            _anim.SetTrigger("2_Attack");
        }
        
        if (_fireballSound != null)
        {
            SoundManager.instance.PlaySound(_fireballSound, gameObject);
        }
        
        _cooldownTimer = 0f;
    }

    /// <summary>
    /// Finds the index of an inactive fireball in the pool.
    /// </summary>
    /// <returns>The index of an available fireball, or -1 if none are available.</returns>
    private int FindFireball()
    {
        if (_fireballs == null) return -1;
        
        for (int i = 0; i < _fireballs.Length; i++)
        {
            if (_fireballs[i] != null && !_fireballs[i].activeInHierarchy)
            {
                return i;
            }
        }
        return -1;
    }



    /// <summary>
    /// Spawns a flame attack at the player's position.
    /// </summary>
    private void SpawnFireAtPlayer()
    {
        if (_flame == null || _isDead) return;
        
        Transform closestPlayer = GetClosestPlayer();
        if (closestPlayer == null) return;
        
        Vector3 targetPosition = new Vector3(closestPlayer.position.x, -7.45f, closestPlayer.position.z);
        StartCoroutine(MarkAreaAndSpawnFire(targetPosition));
        _fireAttackTimer = 0f;
    }

    /// <summary>
    /// Coroutine to mark an area and spawn fire after a delay.
    /// </summary>
    /// <param name="targetPosition">The position to mark and spawn fire.</param>
    private IEnumerator MarkAreaAndSpawnFire(Vector3 targetPosition)
    {
        if (_isDead) yield break;
        
        GameObject marker = Instantiate(_areaMarkerPrefab, targetPosition, Quaternion.identity);
        marker.SetActive(true);
        marker.tag = FlameWarningTag;
        
        yield return new WaitForSeconds(FlameMarkerWait);
        
        if (_isDead)
        {
            Destroy(marker);
            yield break;
        }
        
        Destroy(marker);
        _flame.transform.position = targetPosition;
        _flame.SetActive(true);
        if (_bossRewardManager != null)
        {
            _bossRewardManager.ReportBossAttack();
        }
        
        BossFlameAttack flameAttack = _flame.GetComponent<BossFlameAttack>();
        if (flameAttack != null)
        {
            flameAttack.Activate(targetPosition);
            StartCoroutine(DeactivateAfterDuration(_flame, FlameDeactivateDuration));
        }
        else
        {
            Debug.LogError("[BossEnemy] Flame object is missing the BossFlameAttack script!");
        }
    }

    /// <summary>
    /// Coroutine to deactivate a GameObject after a specified delay.
    /// </summary>
    /// <param name="obj">The GameObject to deactivate.</param>
    /// <param name="delay">The delay in seconds before deactivation.</param>
    private IEnumerator DeactivateAfterDuration(GameObject obj, float delay)
    {
        float timer = 0f;
        while (timer < delay)
        {
            if (_isDead || obj == null || !obj.activeInHierarchy)
            {
                if (obj != null) obj.SetActive(false);
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (!_isDead && obj != null && obj.activeInHierarchy)
        {
            obj.SetActive(false);
            
            // Call virtual method for derived classes
            OnFlameDeactivated(obj);
        }
    }

    // ==================== Dash Attack ====================
    /// <summary>
    /// Initiates the charge dash attack sequence.
    /// </summary>
    private void ChargeDashAttack()
    {
        if (_isDead || _isChargingDash || _isDashing) return;
        
        _isChargingDash = true;
        _isDashing = false;
        
        Transform closestPlayer = GetClosestPlayer();
        if (closestPlayer != null)
        {
            Vector2 targetPosition = new Vector2(closestPlayer.position.x, 
                                               closestPlayer.position.y + DashTargetYAdjust);
            
            // Set dash target (boundary checking handled by derived classes)
            _dashTarget = targetPosition;
            
            Debug.Log($"[BossEnemy] Dash target set to {_dashTarget}");
        }
        else
        {
            Debug.LogWarning("[BossEnemy] Player transform is null, cannot set dash target.");
            _isChargingDash = false;
            return;
        }
        
        CreateOrUpdateTargetIcon();
        
        if (_anim != null)
        {
            _anim.SetTrigger("ChargeDash");
        }
        
        if (_chargeSound != null)
        {
            SoundManager.instance.PlaySound(_chargeSound, gameObject);
        }
        
        _dashCoroutine = StartCoroutine(PerformDashAttack());
    }

    /// <summary>
    /// Creates or updates the target icon for dash attacks.
    /// </summary>
    protected virtual void CreateOrUpdateTargetIcon()
    {
        Vector3 iconPosition = new Vector3(_dashTarget.x, _dashTarget.y + DashTargetYOffset, 0f);
        Vector3 iconScale = Vector3.one * DashMarkerScale;
        
        if (_targetIconInstance == null)
        {
            _targetIconInstance = Instantiate(_targetIconPrefab, iconPosition, Quaternion.identity);
        }
        
        _targetIconInstance.transform.position = iconPosition;
        _targetIconInstance.transform.localScale = iconScale;
        _targetIconInstance.SetActive(true);
        _targetIconInstance.tag = DashTargetIndicatorTag;
    }

    /// <summary>
    /// Coroutine to perform the dash attack sequence.
    /// </summary>
    protected virtual IEnumerator PerformDashAttack()
    {
        yield return new WaitForSeconds(_dashChargeTime);
        
        if (_isDead)
        {
            _isChargingDash = false;
            if (_targetIconInstance != null) _targetIconInstance.SetActive(false);
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
            _rb.velocity = direction * _dashSpeed;
        }
        
        // Use a timer-based approach like the old AIBoss
        float dashTimer = 0f;
        while (dashTimer < DashDuration)
        {
            if (_isDead || !_isDashing) break;
            
            // Check if we're close to target (like the old AIBoss)
            if (Vector2.Distance(transform.position, _dashTarget) < 1.0f) break;
            
            dashTimer += Time.deltaTime;
            yield return null;
        }
        
        // Stop movement (no teleportation)
        _isDashing = false;
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
        }
        
        _dashCooldownTimer = 0f;
        
        // Call virtual method for derived classes
        OnDashCompleted();
    }

    // ==================== Phase Management ====================
    /// <summary>
    /// Handles the transition to phase 2 of the boss fight.
    /// </summary>
    private void EnterPhase2()
    {
        _isPhase2 = true;
        
        // Reset damage to 1 if it was 0 initially
        if (_damage == 0)
        {
            _damage = 1;
        }
        
        // Show enraged effect
        if (_enragedEffect != null)
        {
            _enragedEffect.gameObject.SetActive(true);
        }
        
        // Reduce cooldowns for phase 2
        _attackCooldown -= Phase2AttackCooldownReduction;
        _fireAttackCooldown -= Phase2FireAttackCooldownReduction;
        _attackCooldown = Mathf.Max(MinCooldown, _attackCooldown);
        _fireAttackCooldown = Mathf.Max(MinCooldown, _fireAttackCooldown);
        
        Debug.Log("[BossEnemy] Entered Phase 2 - Enraged!");
    }

    // ==================== Death Handling ====================
    /// <summary>
    /// Handles the boss's death logic, including animation and disabling the boss.
    /// </summary>
    public void Die()
    {
        if (_isDead) return;
        
        _isDead = true;
        Debug.Log("[BossEnemy] Boss Died!");
        
        StopAllCoroutines();
        DeactivateFlameAndWarning();
        
        if (_rb != null) _rb.velocity = Vector2.zero;
        
        Collider2D bossCol = GetComponent<Collider2D>();
        if (bossCol != null) bossCol.enabled = false;
        
        if (_rb != null) _rb.isKinematic = true;
        
        if (_anim != null)
        {
            _anim.SetTrigger("4_Death");
        }
        
        // Check if we're in training mode (any mode except None)
        bool isInTrainingMode = EpisodeManager.Instance != null && EpisodeManager.Instance.CurrentBossMode != EpisodeManager.BossMode.None;
        
        if (isInTrainingMode)
        {
            Debug.Log("[BossEnemy] Boss died in training mode - recording episode end and resetting environment");
            
            // Notify EpisodeManager that boss died (player won)
            EpisodeManager.Instance.RecordEndOfEpisode(false); // false = boss lost, player won
            
            // Disable the boss temporarily - EpisodeManager will reset it for the next episode
            enabled = false;
            Debug.Log("[BossEnemy] Boss disabled for training mode - will be reset by EpisodeManager. BossMode: " + EpisodeManager.Instance.CurrentBossMode);
            
            // Reset environment for the next episode (after disabling)
            EpisodeManager.Instance.ResetEnvironmentForNewEpisode();
        }
        else
        {
            // Not in training mode, destroy the boss
            enabled = false;
            Destroy(gameObject, DeathDestroyDelay);
            Debug.Log("[BossEnemy] Boss destroyed - not in training mode");
        }
    }

    /// <summary>
    /// Deactivates the flame and warning markers in the environment.
    /// </summary>
    public void DeactivateFlameAndWarning()
    {
        Debug.Log("[BossEnemy] Deactivating all hazards and warnings...");
        
        // Deactivate flame
        if (_flame != null)
        {
            _flame.SetActive(false);
            Debug.Log("[BossEnemy] Deactivated Flame");
        }
        
        // Deactivate all flame warning markers
        GameObject[] flameWarnings = GameObject.FindGameObjectsWithTag(FlameWarningTag);
        foreach (GameObject marker in flameWarnings)
        {
            if (marker != null)
            {
                marker.SetActive(false);
                if (marker.name == "Warning(Clone)" || marker.name == "Target(Clone)") {
                    Destroy(marker);
                }
                Debug.Log("[BossEnemy] Deactivated flame warning marker: " + marker.name);
            }
        }
        
        // Deactivate all dash target indicators
        GameObject[] dashIndicators = GameObject.FindGameObjectsWithTag(DashTargetIndicatorTag);
        foreach (GameObject marker in dashIndicators)
        {
            if (marker != null)
        {
            marker.SetActive(false);
                Debug.Log("[BossEnemy] Deactivated dash indicator: " + marker.name);
        }
        }
        
        // Deactivate target icon instance
        if (_targetIconInstance != null)
        {
            _targetIconInstance.SetActive(false);
            Debug.Log("[BossEnemy] Deactivated target icon instance");
        }
        
        // Force stop any ongoing dash state
        _isChargingDash = false;
        _isDashing = false;
        
        Debug.Log("[BossEnemy] All hazards and warnings deactivated");
    }

    // ==================== Public Interface ====================
    /// <summary>
    /// Returns whether the boss is currently charging or dashing.
    /// </summary>
    public bool IsCurrentlyChargingOrDashing()
    {
        return _isChargingDash || _isDashing;
    }

    /// <summary>
    /// Returns whether the boss's fireball attack is ready (off cooldown).
    /// </summary>
    public bool IsFireballReady()
    {
        return _cooldownTimer >= _attackCooldown;
    }

    /// <summary>
    /// Returns whether the boss's flame trap attack is ready (off cooldown).
    /// </summary>
    public bool IsFlameTrapReady()
    {
        return _fireAttackTimer >= _fireAttackCooldown && (_flame == null || !_flame.activeInHierarchy);
    }

    /// <summary>
    /// Returns whether the boss's dash attack is ready (off cooldown).
    /// </summary>
    public bool IsDashReady()
    {
        return _dashCooldownTimer >= _dashCooldown && _isPhase2;
    }

    /// <summary>
    /// Gets whether the boss is in phase 2.
    /// </summary>
    public bool IsPhase2 => _isPhase2;
    
    /// <summary>
    /// Forces a complete reset of the boss state (for debugging/testing).
    /// </summary>
    public void ForceReset()
    {
        Debug.Log("[BossEnemy] Force reset called");
        ResetState();
    }

}
