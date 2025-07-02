using System.Collections;

using UnityEngine;
using UnityEngine.UI;

// This script handles the boss's mechanics, state, and execution of actions requested by the Q-learning agent.
public class AIBoss : EnemyDamage, IBoss // Assuming EnemyDamage handles health or similar
{
    // ==================== Constants ====================
    private const float TargetReachedThresholdSqr = 0.25f; // 0.5f squared
    private const float DashMarkerScale = 0.5f;
    private const float WallBuffer = 2f;
    private const float DashTargetProximity = 1.0f;
    private const float DashMaxDuration = 1.5f;
    private const float FlameMarkerWait = 1.5f;
    private const float FlameDeactivateDuration = 3f;
    private const float EnergyBarYOffset = 2.5f;
    private const float Phase2HealthPercent = 0.5f;
    private const float FireballAimDistance = 100f;
    private const string FlameWarningTag = "FlameWarningMarker";

    // ==================== Serialized Fields ====================
    [Header("Boss Parameters")]
    [SerializeField] private float _movementSpeed = 3.0f;
    [SerializeField] private bool _doRestHealth;
    [SerializeField] private float _attackRange = 10f;

    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Health _playerHealth;
    [SerializeField] private Slider _energySlider;
    [SerializeField] private Animator _anim;
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] public BossRewardManager rewardManager; // CRUCIAL - Assign in Inspector!
    [SerializeField] private BossHealth _bossHealth;
    [SerializeField] private Transform _firepoint;
    [SerializeField] private Transform _fireballHolder;
    [SerializeField] private GameObject[] _fireballs;
    [SerializeField] private GameObject _flame;
    [SerializeField] private GameObject _areaMarkerPrefab;
    [SerializeField] private Transform _leftWall;
    [SerializeField] private Transform _rightWall;
    [SerializeField] private GameObject _targetIconPrefab;

    [Header("Attack Parameters")]
    [SerializeField] public float attackCooldown = 4f;
    [SerializeField] private int _fireballDamage = 1;
    [SerializeField] private float _projectileSpeed = 5f;
    [SerializeField] private float _projectileSize = 0.3f;
    [SerializeField] private AudioClip _fireballSound;
    [Tooltip("How far ahead (in seconds) to predict player movement for aiming fireballs.")]
    [SerializeField] private float _predictionTime = 0.3f;

    [Header("Flame Attack Parameters")]
    [SerializeField] public float fireAttackCooldown = 4f;

    [Header("Charge Dash Attack Parameters")]
    [SerializeField] private float _dashChargeTime = 2f;
    [SerializeField] private float _dashSpeed = 10f;
    [SerializeField] public float dashCooldown = 8f;
    [SerializeField] private AudioClip _chargeSound;
    [SerializeField] private AudioClip _dashSound;

    [Header("Energy System (Optional - Enable Checks Below)")]
    [SerializeField] private float _maxEnergy = 100f;
    [SerializeField] private float _energyRegenRate = 5f;
    [SerializeField] private float _fireballEnergyCost = 10f;
    [SerializeField] private float _flameTrapEnergyCost = 25;
    [SerializeField] private float _dashEnergyCost = 35f;
    private float _currentEnergy;

    [Header("Movement & Targeting")]
    [SerializeField] private Vector3 _arenaCenterPosition = Vector3.zero;
    [SerializeField] private float _actionDistanceOffset = 3.0f;
    [SerializeField] private float _flameTrapGroundYLevel = -11.5f;
    [SerializeField] private float _dashDistance = 6.0f;

    // ==================== Private Fields ====================
    private bool _isPhase2 = false;
    private Vector3 _initialBossPosition;
    private bool _detectedPlayer;
    private bool _isChargingDash = false;
    private bool _isDashing = false;
    private bool _dashMissed = true;
    public bool flameMissed = true; // Public so BossFlameAttack can set it to false on hit
    private Vector2 _dashTarget;
    private GameObject _targetIconInstance;
    private bool _isDead = false;
    private bool _isFlameDeactivationCanceled = false;
    private PlayerMovement _playerMovement;
    private float _cooldownTimer = Mathf.Infinity;
    private float _fireAttackTimer = Mathf.Infinity;
    private float _dashCooldownTimer = Mathf.Infinity;
    private Coroutine _dashCoroutine;

    // ==================== Unity Lifecycle ====================
    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _initialBossPosition = gameObject.transform.position;
        if (_player == null)
        {
            Debug.LogError("[AIBoss] Player Transform not assigned!");
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
            else this.enabled = false;
        }
        if (_player != null)
        {
            _playerMovement = _player.GetComponent<PlayerMovement>();
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
        _currentEnergy = _maxEnergy;
    }

    private void Start()
    {
        _cooldownTimer = attackCooldown;
        _fireAttackTimer = fireAttackCooldown;
        _dashCooldownTimer = dashCooldown;
        _isPhase2 = false;
        _isChargingDash = false;
        _isDashing = false;
        _isDead = false;
        UpdateEnergyBar();
    }

    // ==================== State Reset & Update ====================
    public void HandlePlayerDeath(float idc)
    {
        if (_playerHealth != null && _playerHealth.currentHealth <= 0)
        {
            ResetState();
        }
    }

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
        ResetAbilityStates();
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
        if (_isDead || _player == null) return;
        if (!_detectedPlayer)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
            if (distanceToPlayer <= _attackRange && _playerHealth.currentHealth > 0)
                _detectedPlayer = true;
        }
        transform.rotation = Quaternion.Euler(0, 0, 0);
        _cooldownTimer += Time.deltaTime;
        _fireAttackTimer += Time.deltaTime;
        _dashCooldownTimer += Time.deltaTime;
        if (_currentEnergy < _maxEnergy)
        {
            _currentEnergy = Mathf.Min(_maxEnergy, _currentEnergy + _energyRegenRate * Time.deltaTime);
            UpdateEnergyBar();
        }
        HandlePhaseTransition();
        if (!_isChargingDash && !_isDashing)
        {
            HandleSpriteFlip();
        }
    }

    private void LateUpdate()
    {
        if (_energySlider != null)
        {
            _energySlider.transform.position = gameObject.transform.position + new Vector3(0, EnergyBarYOffset, 0);
            _energySlider.transform.rotation = Quaternion.identity;
        }
    }

    // ==================== Phase & Sprite Logic ====================
    private void HandlePhaseTransition()
    {
        if (!_isPhase2 && _bossHealth != null)
        {
            float healthPercentage = _bossHealth.GetHealthPercentage();
            if (healthPercentage <= Phase2HealthPercent)
            {
                EnterPhase2();
            }
        }
    }

    private void HandleSpriteFlip()
    {
        if (_player == null || _isChargingDash) return;
        if (_player.position.x < transform.position.x)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        if (_fireballHolder != null)
        {
            _fireballHolder.localScale = transform.localScale;
        }
    }

    // ==================== Q-Learning Action Requests ====================
    public void AIRequestMove(BossQLearning.ActionType moveAction, Vector2 playerPos, Vector2 bossPos, float offsetDistance)
    {
        if (_player == null || !_detectedPlayer || _isChargingDash || _isDashing || _isDead) return;
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
                Debug.LogWarning("[AIBoss] Received unexpected move action: " + moveAction);
                AIRequestIdle();
                return;
        }
        if ((targetPosition - bossPos).sqrMagnitude > TargetReachedThresholdSqr)
        {
            _rb.velocity = (targetPosition - bossPos).normalized * _movementSpeed;
            if (_anim != null) _anim.SetBool("IsMoving", true);
        }
        else
        {
            _rb.velocity = Vector2.zero;
            if (_anim != null) _anim.SetBool("IsMoving", false);
        }
    }

    public bool AIRequestRangedAttack(BossQLearning.ActionType aimAction, Vector2 playerPos, Vector2 playerVel, float offsetDistance)
    {
        if (!IsFireballReady() || !_detectedPlayer || _isChargingDash || _isDashing || _isDead || _player == null) return false;
        if (_currentEnergy < _fireballEnergyCost) return false;
        Vector2 targetPosition;
        Vector2 bossPos = transform.position;
        float predictionTime = (playerPos - bossPos).magnitude / _projectileSpeed;
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
                Vector2 forwardDirection = (_rb.velocity.x > 0.01f) ? Vector2.right : (_rb.velocity.x < -0.01f ? Vector2.left : (_fireballHolder != null ? (_fireballHolder.localScale.x > 0 ? Vector2.right : Vector2.left) : Vector2.right));
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
                Debug.LogWarning("[AIBoss] Received unexpected fireball aim action: " + aimAction);
                targetPosition = playerPos;
                break;
        }
        int fireballIndex = FindFireball();
        if (fireballIndex == -1) return false;
        if (_currentEnergy < _fireballEnergyCost) return false;
        GameObject projectile = _fireballs[fireballIndex];
        projectile.transform.position = _firepoint.position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.parent = null;
        BossProjectile bossProjectile = projectile.GetComponent<BossProjectile>();
        if (bossProjectile == null) { Debug.LogError("[AIBoss] Fireball prefab missing BossProjectile script!"); return false; }
        bossProjectile.rewardManager = this.rewardManager;
        bossProjectile.SetDamage(_fireballDamage);
        bossProjectile.SetSize(_projectileSize);
        bossProjectile.Launch(_firepoint.position, targetPosition, _projectileSpeed);
        if (_anim != null) _anim.SetTrigger("Attack");
        if (_fireballSound != null) SoundManager.instance.PlaySound(_fireballSound, gameObject);
        _cooldownTimer = 0f;
        _currentEnergy -= _fireballEnergyCost;
        UpdateEnergyBar();
        return true;
    }

    public bool AIRequestFlameAttack(BossQLearning.ActionType placeAction, Vector2 playerPos, Vector2 bossPos, Vector2 playerVel, float offsetDistance)
    {
        if (!IsFlameTrapReady() || !_detectedPlayer || _isChargingDash || _isDashing || _isDead || _flame == null || _player == null) return false;
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
                Debug.LogWarning("[AIBoss] Received unexpected flame trap action: " + placeAction);
                placementPosition = new Vector2(playerPos.x, _flameTrapGroundYLevel);
                break;
        }
        if (_leftWall != null && _rightWall != null)
        {
            placementPosition.x = Mathf.Clamp(placementPosition.x, _leftWall.position.x + WallBuffer, _rightWall.position.x - WallBuffer);
        }
        StartCoroutine(MarkAreaAndSpawnFire(placementPosition));
        if (_anim != null) _anim.SetTrigger("CastSpell");
        _fireAttackTimer = 0f;
        _currentEnergy -= _flameTrapEnergyCost;
        UpdateEnergyBar();
        return true;
    }

    public bool AIRequestDashAttack(BossQLearning.ActionType dashAction, Vector2 playerPos, Vector2 bossPos, float offsetDistance)
    {
        if (!IsDashReady() || !_detectedPlayer || _isChargingDash || _isDashing || _isDead || _player == null) return false;
        if (_currentEnergy < _dashEnergyCost) return false;
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
                Debug.LogWarning("[AIBoss] Received unexpected dash action: " + dashAction);
                dashTargetCalculated = playerPos;
                break;
        }
        _isChargingDash = true;
        _isDashing = false;
        _dashMissed = true;
        _rb.velocity = Vector2.zero;
        _dashTarget = dashTargetCalculated;
        if (_targetIconPrefab != null)
        {
            if (_targetIconInstance == null)
            {
                _targetIconInstance = Instantiate(_targetIconPrefab, _dashTarget, Quaternion.identity);
                _targetIconInstance.transform.localScale = new Vector3(DashMarkerScale, DashMarkerScale, DashMarkerScale);
            }
            else
            {
                _targetIconInstance.transform.position = _dashTarget;
                _targetIconInstance.SetActive(true);
            }
        }
        if (_anim != null) _anim.SetTrigger("ChargeDash");
        if (_chargeSound != null) SoundManager.instance.PlaySound(_chargeSound, gameObject);
        StartCoroutine(PerformDashAttack());
        _dashCooldownTimer = 0f;
        _currentEnergy -= _dashEnergyCost;
        UpdateEnergyBar();
        return true;
    }

    public void AIRequestIdle()
    {
        if (_isChargingDash || _isDashing || _isDead) return;
        _rb.velocity = Vector2.zero;
        if (_anim != null) _anim.SetBool("IsMoving", false);
    }

    // ==================== Ability Readiness Checks ====================
    public bool IsFireballReady() { return _cooldownTimer >= attackCooldown; }
    public bool IsFlameTrapReady() { return _fireAttackTimer >= fireAttackCooldown; }
    public bool IsDashReady() { return _dashCooldownTimer >= dashCooldown; }
    public bool IsCurrentlyChargingOrDashing() { return _isChargingDash || _isDashing; }

    // ==================== State Information Providers ====================
    public float GetCurrentEnergyNormalized() { if (_maxEnergy <= 0) return 1.0f; return Mathf.Clamp01(_currentEnergy / _maxEnergy); }
    public bool IsPlayerGrounded() { if (_playerMovement != null) { return _playerMovement.IsGrounded(); } Debug.LogWarning("[AIBoss] Cannot check player grounded status: PlayerMovement reference missing."); return true; }

    // ==================== Coroutines and Internal Logic ====================
    private int FindFireball()
    {
        for (int i = 0; i < _fireballs.Length; i++)
        {
            if (!_fireballs[i].activeInHierarchy)
            {
                return i;
            }
        }
        return -1;
    }

    private IEnumerator MarkAreaAndSpawnFire(Vector2 targetPosition)
    {
        if (_isDead) yield break;
        GameObject marker = null;
        if (_areaMarkerPrefab != null)
        {
            marker = Instantiate(_areaMarkerPrefab, targetPosition, Quaternion.identity);
        }
        yield return new WaitForSeconds(FlameMarkerWait);
        if (_isDead)
        {
            if (marker != null) Destroy(marker);
            yield break;
        }
        if (marker != null) Destroy(marker);
        if (_flame != null)
        {
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
                Debug.LogError("[AIBoss] Flame object missing BossFlameAttack script!");
                _flame.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("[AIBoss] Flame prefab reference not set!");
        }
    }

    private IEnumerator DeactivateAfterDuration(GameObject obj, float delay)
    {
        _isFlameDeactivationCanceled = false;
        float timer = 0f;
        while (timer < delay)
        {
            if (_isDead)
            {
                if (obj != null) obj.SetActive(false);
                _isFlameDeactivationCanceled = true;
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        if (!_isFlameDeactivationCanceled && obj != null)
        {
            obj.SetActive(false);
            if (rewardManager != null)
            {
                if (flameMissed)
                {
                    rewardManager.ReportAttackMissed();
                }
                flameMissed = true;
            }
        }
    }

    private IEnumerator PerformDashAttack()
    {
        _isChargingDash = true;
        _isDashing = false;
        yield return new WaitForSeconds(_dashChargeTime);
        _isChargingDash = false;
        if (_targetIconInstance != null) _targetIconInstance.SetActive(false);
        if (_isDead) yield break;
        _isDashing = true;
        if (_anim != null) _anim.SetTrigger("Dash");
        if (_dashSound != null) SoundManager.instance.PlaySound(_dashSound, gameObject);
        Vector2 direction = (_dashTarget - (Vector2)transform.position).normalized;
        if (direction == Vector2.zero) direction = (_player.position - transform.position).normalized;
        _rb.velocity = direction * _dashSpeed;
        float dashTimer = 0f;
        while (dashTimer < DashMaxDuration)
        {
            if (_isDead || !_isDashing) break;
            if (!_dashMissed) break;
            if (Vector2.Distance(transform.position, _dashTarget) < DashTargetProximity) break;
            dashTimer += Time.deltaTime;
            yield return null;
        }
        _isDashing = false;
        _rb.velocity = Vector2.zero;
        if (rewardManager != null && _dashMissed && !_isDead)
        {
            rewardManager.ReportAttackMissed();
        }
        _dashMissed = true;
    }

    // ==================== Phase Logic ====================
    private void EnterPhase2() { /* ... as before ... */ }

    // ==================== Collision Handling ====================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null) playerHealth.TakeDamage(_damage);
            if (rewardManager != null) rewardManager.ReportHitPlayer();
            if (_isDashing)
            {
                _dashMissed = false;
                _isDashing = false;
                _rb.velocity = Vector2.zero;
            }
        }
        else if (_isDashing && (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Walls")))
        {
            _isDashing = false;
            _rb.velocity = Vector2.zero;
        }
    }

    // ==================== Death Handling ====================
    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        StopAllCoroutines();
        DeactivateFlameAndWarning();
        _rb.velocity = Vector2.zero;
        _rb.isKinematic = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (_anim != null) _anim.SetTrigger("Die");
        this.enabled = false;
    }

    // Utility to clean up flame visuals immediately
    public void DeactivateFlameAndWarning()
    {
        if (_flame != null) _flame.SetActive(false);
        GameObject[] markers = GameObject.FindGameObjectsWithTag(FlameWarningTag);
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
