using System.Collections;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Manages the Boss's behavior, attacks, and state.
/// </summary>
public class BossEnemy : EnemyDamage, IBoss // Assuming EnemyDamage provides base health/damage logic
{
    // ==================== Constants ====================
    private const float DefaultAttackCooldown = 7f;
    private const float DefaultFireAttackCooldown = 8.5f;
    private const float DefaultDashCooldown = 10f;
    private const float Phase2AttackCooldownReduction = 0.5f;
    private const float Phase2FireAttackCooldownReduction = 1f;
    private const float MinCooldown = 0.1f;
    private const float FlameMarkerWait = 1.5f;
    private const float FlameDeactivateDuration = 3f;
    private const float DashMarkerScale = 0.5f;
    private const float DashTargetYOffset = 0.7f;
    private const float DashTargetYAdjust = -1.2f;
    private const float DashDuration = 0.8f;
    private const float DeathDestroyDelay = 2f;
    private const string FlameWarningTag = "FlameWarningMarker";
    private const string AreaMarkerTag = "AreaMarker";
    private const string DashTargetIndicatorTag = "DashTargetIndicator";

    // ==================== Serialized Fields ====================
    [Tooltip("Reference to the boss's Rigidbody2D component.")]
    [FormerlySerializedAs("rb")]
    [SerializeField] private Rigidbody2D _rb;
    [Tooltip("Movement speed of the boss.")]
    [FormerlySerializedAs("movementSpeed")]
    [SerializeField] private float _movementSpeed;
    [Tooltip("Should the boss reset health on state reset?")]
    [FormerlySerializedAs("doRestHealth")]
    [SerializeField] private bool _doRestHealth;
    [Tooltip("Reference to the player's Health component.")]
    [FormerlySerializedAs("playerHealth")]
    [SerializeField] private Health _playerHealth;

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
    [Tooltip("Reference to the BossHealth component.")]
    [FormerlySerializedAs("bossHealth")]
    [SerializeField] private BossHealth _bossHealth;
    private bool _isPhase2 = false;

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
    [Tooltip("Reference to the player Transform.")]
    [FormerlySerializedAs("player")]
    [SerializeField] private Transform _player;
    [Tooltip("Reference to the fireball holder Transform.")]
    [FormerlySerializedAs("fireballHolder")]
    [SerializeField] private Transform _fireballHolder;

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
    [SerializeField] private float _dashChargeTime = 2f;
    [Tooltip("Speed of the dash attack.")]
    [FormerlySerializedAs("dashSpeed")]
    [SerializeField] private float _dashSpeed = 10f;
    [Tooltip("Cooldown time between dash attacks.")]
    [FormerlySerializedAs("dashCooldown")]
    [SerializeField] private float _dashCooldown = 10f;
    [Tooltip("Sound played when charging dash.")]
    [FormerlySerializedAs("chargeSound")]
    [SerializeField] private AudioClip _chargeSound;
    [Tooltip("Sound played when dashing.")]
    [FormerlySerializedAs("dashSound")]
    [SerializeField] private AudioClip _dashSound;
    [Tooltip("Prefab for the target icon.")]
    [FormerlySerializedAs("targetIconPrefab")]
    [SerializeField] private GameObject _targetIconPrefab;

    // ==================== Private Fields ====================
    private GameObject _targetIconInstance;
    private bool _isChargingDash = false;
    private bool _detectedPlayer;
    private bool _isDashing = false;
    private Vector2 _dashTarget;
    private float _fireAttackTimer = Mathf.Infinity;
    private float _cooldownTimer = Mathf.Infinity;
    private float _dashCooldownTimer = Mathf.Infinity;
    private Animator _anim;
    private Vector3 _initialBossPosition;
    private bool _isDead = false;
    private Coroutine _dashCoroutine;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Awake callback. Initializes references and validates required components.
    /// </summary>
    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _initialBossPosition = gameObject.transform.position;
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

    /// <summary>
    /// Unity Start callback. Initializes boss state and timers.
    /// </summary>
    private void Start()
    {
        _cooldownTimer = _attackCooldown;
        _fireAttackTimer = _fireAttackCooldown;
        _dashCooldownTimer = _dashCooldown;
        _isPhase2 = false;
        _isChargingDash = false;
        _isDashing = false;
        _isDead = false;
    }
    // ==================== State Reset & Update ====================
    /// <summary>
    /// Handles logic when the player dies. Resets the boss state if the player's health reaches zero.
    /// </summary>
    /// <param name="idc">Unused parameter (for event compatibility).</param>
    public void HandlePlayerDeath(float idc)
    {
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0)
        {
            ResetState();
        }
    }

    /// <summary>
    /// Resets the boss's state, health, cooldowns, and position for a new encounter.
    /// </summary>
    public void ResetState()
    {
        Debug.Log("[BossEnemy] Resetting BossEnemy state.");
        _detectedPlayer = false;
        StopAllCoroutines();
        _isDead = false;
        if (_doRestHealth)
        {
            _isPhase2 = false;
        }
        _isChargingDash = false;
        _isDashing = false;
        _attackCooldown = DefaultAttackCooldown;
        _fireAttackCooldown = DefaultFireAttackCooldown;
        _dashCooldown = DefaultDashCooldown;
        _cooldownTimer = _attackCooldown;
        _fireAttackTimer = _fireAttackCooldown;
        _dashCooldownTimer = _dashCooldown;
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.isKinematic = false;
        }
        gameObject.transform.position = _initialBossPosition;
        if (_bossHealth != null && _doRestHealth) _bossHealth.ResetHealth();
        DeactivateFlameAndWarning();
        if (_anim != null)
        {
            _anim.Rebind();
            _anim.Update(0f);
        }
        gameObject.SetActive(true);
        this.enabled = true;
    }

    private void Update()
    {
        if (_isDead) return;
        if (!_detectedPlayer)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
            if (distanceToPlayer <= _attackRange && _playerHealth.CurrentHealth > 0)
                _detectedPlayer = true;
        }
        transform.rotation = Quaternion.Euler(0, 0, 0);
        if (!_isPhase2 && _bossHealth != null)
        {
            float healthPercentage = _bossHealth.GetHealthPercentage();
            if (healthPercentage <= 0.5f)
            {
                _isPhase2 = true;
                EnterPhase2();
            }
        }
        if (!_isChargingDash)
        {
            _cooldownTimer += Time.deltaTime;
            _fireAttackTimer += Time.deltaTime;
            _dashCooldownTimer += Time.deltaTime;
        }
        if (!_isChargingDash && _detectedPlayer)
        {
            Vector3 fixPlayerPosition = _player.position;
            fixPlayerPosition.y -= 0.75f;
            Vector2 directionToPlayer = (fixPlayerPosition - transform.position).normalized;
            _rb.velocity = directionToPlayer * _movementSpeed;
        }
        else if (_isChargingDash || _detectedPlayer)
        {
            _rb.velocity = Vector2.zero;
        }
        if (_isPhase2 && _detectedPlayer && _dashCooldownTimer >= _dashCooldown && !_isChargingDash)
        {
            _dashCooldownTimer = 0;
            ChargeDashAttack();
        }
        if (_player.position.x < transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        if (_detectedPlayer && _cooldownTimer >= _attackCooldown)
        {
            _cooldownTimer = 0;
            RangedAttack();
        }
        _fireballHolder.localScale = transform.localScale;
        if (_detectedPlayer && _fireAttackTimer >= _fireAttackCooldown && !_flame.activeInHierarchy)
        {
            _fireAttackTimer = 0;
            SpawnFireAtPlayer();
        }
    }

    // ==================== Attack Methods (Called by QL Agent or Hardcoded Triggers) ====================
    /// <summary>
    /// Requests the boss to perform a ranged attack (e.g., fireball).
    /// </summary>
    public void AIRequestRangedAttack()
    {
        if (_isDead || _isChargingDash || _isDashing || _cooldownTimer < _attackCooldown) return;
        RangedAttack();
    }

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
        if (bossProjectile == null) { Debug.LogError("[BossEnemy] Fireball prefab missing BossProjectile script!"); return; }
        bossProjectile.SetDamage(_fireballDamage);
        bossProjectile.SetSize(_projectileSize);
        if (_player != null)
        {
            bossProjectile.Launch(_firepoint.position, _player.transform.position, _projectileSpeed);
        }
        else
        {
            Debug.LogWarning("[BossEnemy] Player transform is null, cannot launch fireball.");
            return;
        }
        if (_anim != null) _anim.SetTrigger("2_Attack");
        if (_fireballSound != null) SoundManager.instance.PlaySound(_fireballSound, gameObject);
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
    /// Requests the boss to perform a flame attack.
    /// </summary>
    public void AIRequestFlameAttack()
    {
        if (_isDead || _isChargingDash || _isDashing || _fireAttackTimer < _fireAttackCooldown || _flame == null || _flame.activeInHierarchy) return;
        SpawnFireAtPlayer();
    }

    /// <summary>
    /// Spawns a flame attack at the player's position.
    /// </summary>
    private void SpawnFireAtPlayer()
    {
        if (_flame == null || _isDead || _player == null) return;
        Vector3 targetPosition = new Vector3(_player.position.x, -11.5f, _player.position.z);
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
        }
    }

    // ==================== Charge Dash Attack sequence ====================
    /// <summary>
    /// Initiates the charge dash attack sequence.
    /// </summary>
    private void ChargeDashAttack()
    {
        if (_isDead || _isChargingDash || _isDashing) return;
        _isChargingDash = true;
        _isDashing = false;
        if (_player != null)
        {
            _dashTarget = new Vector3(_player.position.x, _player.position.y + DashTargetYAdjust, _player.position.z);
        }
        else
        {
            Debug.LogWarning("[BossEnemy] Player transform is null, cannot set dash target.");
            _isChargingDash = false;
            return;
        }
        if (_targetIconInstance == null)
        {
            _targetIconInstance = Instantiate(_targetIconPrefab, new Vector3(_dashTarget.x, _dashTarget.y + DashTargetYOffset), Quaternion.identity);
            _targetIconInstance.transform.localScale = new Vector3(DashMarkerScale, DashMarkerScale, DashMarkerScale);
            _targetIconInstance.tag = DashTargetIndicatorTag;
        }
        else
        {
            _targetIconInstance.transform.position = new Vector3(_dashTarget.x, _dashTarget.y + DashTargetYOffset);
            _targetIconInstance.transform.localScale = new Vector3(DashMarkerScale, DashMarkerScale, DashMarkerScale);
            _targetIconInstance.SetActive(true);
            _targetIconInstance.tag = DashTargetIndicatorTag;
        }
        if (_anim != null) _anim.SetTrigger("ChargeDash");
        if (_chargeSound != null) SoundManager.instance.PlaySound(_chargeSound, gameObject);
        _dashCoroutine = StartCoroutine(PerformDashAttack());
    }

    /// <summary>
    /// Coroutine to perform the dash attack sequence.
    /// </summary>
    private IEnumerator PerformDashAttack()
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
        if (_anim != null) _anim.SetTrigger("Dash");
        if (_dashSound != null) SoundManager.instance.PlaySound(_dashSound, gameObject);
        Vector2 startPosition = transform.position;
        Vector2 direction = (_dashTarget - startPosition).normalized;
        float elapsedTime = 0f;
        while (elapsedTime < DashDuration)
        {
            if (_isDead)
            {
                _isDashing = false;
                if (_rb != null) _rb.velocity = Vector2.zero;
                yield break;
            }
            transform.position = Vector2.Lerp(startPosition, _dashTarget, (elapsedTime / DashDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = _dashTarget;
        _isDashing = false;
        _dashCooldownTimer = 0f;
    }

    // ==================== Phase 2 implementation ====================
    /// <summary>
    /// Handles the transition to phase 2 of the boss fight.
    /// </summary>
    private void EnterPhase2()
    {
        Debug.Log("[BossEnemy] Entering Phase 2!");
        _attackCooldown -= Phase2AttackCooldownReduction;
        _fireAttackCooldown -= Phase2FireAttackCooldownReduction;
        _attackCooldown = Mathf.Max(MinCooldown, _attackCooldown);
        _fireAttackCooldown = Mathf.Max(MinCooldown, _fireAttackCooldown);
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
        if (_anim != null) _anim.SetTrigger("4_Death");
        this.enabled = false;
        Destroy(gameObject, DeathDestroyDelay);
    }

    /// <summary>
    /// Deactivates the flame and warning markers in the environment.
    /// </summary>
    public void DeactivateFlameAndWarning()
    {
        if (_flame != null)
        {
            _flame.SetActive(false);
        }
        GameObject[] markers = GameObject.FindGameObjectsWithTag(AreaMarkerTag);
        foreach (GameObject marker in markers)
        {
            marker.SetActive(false);
        }
        if (_targetIconInstance != null)
        {
            _targetIconInstance.SetActive(false);
        }
    }

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
}
