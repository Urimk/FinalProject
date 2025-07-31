using System.Collections.Generic;
using System.Linq;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// ML-Agents agent for player character, handling observations, actions, rewards, and episode lifecycle.
/// </summary>
public class PlayerAI : Agent
{
    // ==================== Constants ====================
    private const float DefaultRaycastDistance = 10f;
    private const int DefaultGroundLayer = 3;
    private const int DefaultPlatformLayer = 7;
    private const int DefaultWallLayer = 6;
    private const int OverlapResultsBufferSize = 10;
    private const float DefaultRewardWin = 1.0f;
    private const float DefaultPenaltyLose = -1.0f;
    private const float DefaultRewardDamageBoss = 0.75f;
    private const float DefaultPenaltyTakeDamage = -3f;
    private const float DefaultPenaltyPerStep = -0.0001f;
    private const int DefaultMaxBossFireballsToObserve = 1;
    private const float DefaultHazardDetectionRadius = 15f;
    private const int DefaultHazardDetectionLayerMask = -1;
    private const float MaxJumpHoldDuration = 0.6f;
    private const string AreaMarkerTag = "AreaMarker";
    private const string FlameWarningMarkerTag = "FlameWarningMarker";
    private const string DebugPlayerDied = "[PlayerAI] Player Died! Ending Episode.";
    private const string DebugBossDefeated = "[PlayerAI] Boss Defeated! Ending Episode.";
    private const string DebugOnEpisodeBegin = "[PlayerAI] OnEpisodeBegin completed.";
    private const string DebugProjectilesNull = "[PlayerAI] Projectiles array is null in ObserveClosestProjectiles.";

    // ==================== Inspector Fields ====================

    [Header("References")]
    [Tooltip("Reference to the PlayerMovement component.")]
    [FormerlySerializedAs("playerMovement")]
    [SerializeField] private PlayerMovement _playerMovement;
    [Tooltip("Reference to the PlayerAttack component.")]
    [FormerlySerializedAs("playerAttack")]
    [SerializeField] private PlayerAttack _playerAttack;
    [Tooltip("Reference to the Health component.")]
    [FormerlySerializedAs("playerHealth")]
    [SerializeField] private Health _playerHealth;
    [Tooltip("Reference to the OneWayPlatform component.")]
    [FormerlySerializedAs("oneWayPlatform")]
    [SerializeField] private OneWayPlatform _oneWayPlatform;
    [Tooltip("Reference to the BossHealth component.")]
    [FormerlySerializedAs("bossHealth")]
    [SerializeField] private BossHealth _bossHealth;
    [Tooltip("Reference to the AIBoss component.")]
    [FormerlySerializedAs("aiBoss")]
    [SerializeField] private AIBoss _aiBoss;
    [Tooltip("Reference to the boss Transform.")]
    [FormerlySerializedAs("boss")]
    [SerializeField] private Transform _boss;
    [Tooltip("Reference to the flames Transform.")]
    [FormerlySerializedAs("flames")]
    [SerializeField] private Transform _flames;
    [Tooltip("Array of boss fireball GameObjects.")]
    [FormerlySerializedAs("bossFireballs")]
    [SerializeField] private GameObject[] _bossFireballs;
    [Tooltip("Array of player fireball GameObjects.")]
    [FormerlySerializedAs("playerFireballs")]
    [SerializeField] private GameObject[] _playerFireballs;
    [SerializeField] private bool isTraining = false;

    [Header("Manual Raycast Perception")]
    [Tooltip("Distance for manual raycasts.")]
    [FormerlySerializedAs("raycastDistance")]
    [SerializeField] private float _raycastDistance = DefaultRaycastDistance;
    [Tooltip("Layer mask for environment raycasts.")]
    [FormerlySerializedAs("environmentLayerMask")]
    [SerializeField] private LayerMask _environmentLayerMask;
    private readonly float[] _rayAngles = { 0f, -45f, 180f };

    [Header("Environment Layers (for Observation)")]
    [Tooltip("Layer index for ground.")]
    [FormerlySerializedAs("groundLayer")]
    [SerializeField] private int _groundLayer = DefaultGroundLayer;
    [Tooltip("Layer index for platforms.")]
    [FormerlySerializedAs("platformLayer")]
    [SerializeField] private int _platformLayer = DefaultPlatformLayer;
    [Tooltip("Layer index for walls.")]
    [FormerlySerializedAs("wallLayer")]
    [SerializeField] private int _wallLayer = DefaultWallLayer;

    [Header("Reward Settings")]
    [Tooltip("Reward for winning.")]
    [FormerlySerializedAs("rewardWin")]
    [SerializeField] private float _rewardWin = DefaultRewardWin;
    [Tooltip("Penalty for losing.")]
    [FormerlySerializedAs("penaltyLose")]
    [SerializeField] private float _penaltyLose = DefaultPenaltyLose;
    [Tooltip("Reward for damaging the boss.")]
    [FormerlySerializedAs("rewardDamageBoss")]
    [SerializeField] private float _rewardDamageBoss = DefaultRewardDamageBoss;
    [Tooltip("Penalty for taking damage.")]
    [FormerlySerializedAs("penaltyTakeDamage")]
    [SerializeField] private float _penaltyTakeDamage = DefaultPenaltyTakeDamage;
    [Tooltip("Penalty per step.")]
    [FormerlySerializedAs("penaltyPerStep")]
    [SerializeField] private float _penaltyPerStep = DefaultPenaltyPerStep;

    [Header("Observation Settings")]
    [Tooltip("Maximum number of boss fireballs to observe.")]
    [FormerlySerializedAs("maxBossFireballsToObserve")]
    [SerializeField] private int _maxBossFireballsToObserve = DefaultMaxBossFireballsToObserve;
    [Header("Physics Detection")]
    [Tooltip("Radius for hazard detection.")]
    [FormerlySerializedAs("hazardDetectionRadius")]
    [SerializeField] private float _hazardDetectionRadius = DefaultHazardDetectionRadius;
    [Tooltip("Layer mask for hazard detection.")]
    [FormerlySerializedAs("hazardDetectionLayerMask")]
    [SerializeField] private LayerMask _hazardDetectionLayerMask = DefaultHazardDetectionLayerMask;
    [SerializeField] private float directionChangeCooldown = 0f; // seconds


    // ==================== Private Fields ====================
    private int _numRaycasts;
    private int _numEnvironmentLayersToObserve;
    private Collider2D[] _overlapResults = new Collider2D[OverlapResultsBufferSize];
    private Rigidbody2D _rigidbody2D;
    private bool _isBossDefeated = false;
    private bool _isPlayerDead = false;
    private float _lastDirectionChangeTime = 0f;

    private float _lastMoveDirection = 0f; // -1, 0, or 1

    /// <summary>
    /// Initializes the agent, sets up event connections, and calculates raycast parameters.
    /// </summary>
    public override void Initialize()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _numRaycasts = _rayAngles.Length + 1;
        _numEnvironmentLayersToObserve = 3;
        if (_playerHealth != null) { _playerHealth.OnDamaged += HandlePlayerDamaged; }
        else Debug.LogError("Player Health component not found!", this);
        if (_bossHealth != null)
        {
            _bossHealth.OnBossDamaged += HandleBossDamaged;
            _bossHealth.OnBossDied += HandleBossDied;
        }
        else Debug.LogError("Boss Health component not found!", this);
    }

    /// <summary>
    /// Called at the beginning of each episode to reset state and environment.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (!isTraining)
        {
            return;
        }
        _isBossDefeated = false;
        _isPlayerDead = false;
        transform.position = EpisodeManager.Instance != null ? EpisodeManager.Instance.InitialPlayerPosition : Vector3.zero;
        transform.rotation = Quaternion.identity;
        _rigidbody2D.velocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
        _playerHealth?.ResetHealth();
        _playerAttack?.ResetCooldown();
        _playerMovement.ResetState();
        if (_playerMovement != null) _playerMovement.enabled = true;
        if (_playerAttack != null) _playerAttack.enabled = true;
        Collider2D playerCol = GetComponent<Collider2D>();
        if (playerCol != null) playerCol.enabled = true;
        EpisodeManager.Instance?.ResetEnvironmentForNewEpisode();
        ClearProjectiles(_bossFireballs);
        ClearProjectiles(_playerFireballs);
        Debug.Log(DebugOnEpisodeBegin);
    }

    /// <summary>
    /// Deactivates all projectiles in the given array.
    /// </summary>
    private void ClearProjectiles(GameObject[] projectileArray)
    {
        if (projectileArray == null) return;
        foreach (var proj in projectileArray)
        {
            if (proj != null) proj.SetActive(false);
        }
    }

    /// <summary>
    /// Collects observations for the agent, including self, boss, environment, hazards, and projectiles.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent's own state
        sensor.AddObservation((Vector2)transform.position);
        sensor.AddObservation(_rigidbody2D.velocity);
        sensor.AddObservation(_playerHealth != null ? _playerHealth.CurrentHealth / _playerHealth.StartingHealth : 0f);
        sensor.AddObservation(_playerMovement != null && _playerMovement.IsGrounded());
        sensor.AddObservation(_playerHealth != null && _playerHealth.Invulnerable);
        sensor.AddObservation(_playerMovement != null && _playerMovement.OnWall());
        sensor.AddObservation(_playerMovement != null ? _playerMovement.GetFacingDirection() : 1f);
        sensor.AddObservation(_playerAttack != null ? _playerAttack.IsAttackReady() : 1f);
        // Boss state
        bool bossActive = _boss != null && _boss.gameObject.activeInHierarchy;
        Vector2 relativeBossPos = Vector2.zero;
        Vector2 bossVelocity = Vector2.zero;
        float bossHealthNormalized = 0f;
        if (bossActive)
        {
            relativeBossPos = _boss.position - transform.position;
            Rigidbody2D bossRb = _boss.GetComponent<Rigidbody2D>();
            bossVelocity = bossRb != null ? bossRb.velocity : Vector2.zero;
            bossHealthNormalized = _bossHealth != null ? _bossHealth.CurrentHealth / _bossHealth.MaxHealth : 0f;
        }
        sensor.AddObservation(relativeBossPos);
        //Debug.Log(relativeBossPos);
        sensor.AddObservation(bossVelocity);
        sensor.AddObservation(bossHealthNormalized);
        // Manual raycast observations
        Vector2 origin = transform.position;
        float facingDirection = _playerMovement != null ? _playerMovement.GetFacingDirection() : 1f;
        foreach (float angle in _rayAngles)
        {
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            Vector2 direction = rotation * (Vector2)(facingDirection > 0 ? Vector2.right : Vector2.left);
            DrawRay(origin, direction * _raycastDistance, Color.blue);
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, _raycastDistance, _environmentLayerMask);
            sensor.AddObservation(hit.collider != null);
            sensor.AddObservation(hit.collider != null ? hit.distance / _raycastDistance : 1f);
        }
        // World down ray
        Vector2 worldDownDirection = Vector2.down;
        DrawRay(origin, worldDownDirection * _raycastDistance, Color.cyan);
        RaycastHit2D worldDownHit = Physics2D.Raycast(origin, worldDownDirection, _raycastDistance, _environmentLayerMask);
        sensor.AddObservation(worldDownHit.collider != null);
        sensor.AddObservation(worldDownHit.collider != null ? worldDownHit.distance / _raycastDistance : 1f);
        sensor.AddObservation(worldDownHit.collider != null && worldDownHit.collider.gameObject.layer == _groundLayer);
        sensor.AddObservation(worldDownHit.collider != null && worldDownHit.collider.gameObject.layer == _platformLayer);
        sensor.AddObservation(worldDownHit.collider != null && worldDownHit.collider.gameObject.layer == _wallLayer);
        // Physics-based hazard/indicator detection
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, _hazardDetectionRadius, _overlapResults, _hazardDetectionLayerMask);
        bool dashIndicatorFound = false;
        Vector2 relativeDashIndicatorPos = Vector2.zero;
        float closestDashIndicatorDist = float.MaxValue;
        bool flameMarkerFound = false;
        Vector2 relativeFlameMarkerPos = Vector2.zero;
        float closestFlameMarkerDist = float.MaxValue;
        bool isFlameHazardActive = _flames != null && _flames.gameObject.activeInHierarchy;
        Vector2 relativeFlamePosObs = Vector2.zero;
        if (isFlameHazardActive)
        {
            relativeFlamePosObs = (Vector2)_flames.position - (Vector2)transform.position;
        }
        sensor.AddObservation(isFlameHazardActive);
        sensor.AddObservation(relativeFlamePosObs);
        float closestActiveFlameDist = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _overlapResults[i];
            if (hit == null) continue;
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (hit.CompareTag(AreaMarkerTag))
            {
                if (dist < closestDashIndicatorDist)
                {
                    dashIndicatorFound = true;
                    relativeDashIndicatorPos = (Vector2)hit.transform.position - (Vector2)transform.position;
                    closestDashIndicatorDist = dist;
                }
            }
            else if (hit.CompareTag(FlameWarningMarkerTag))
            {
                if (dist < closestFlameMarkerDist)
                {
                    flameMarkerFound = true;
                    relativeFlameMarkerPos = (Vector2)hit.transform.position - (Vector2)transform.position;
                    closestFlameMarkerDist = dist;
                }
            }
        }
        for (int i = hitCount; i < _overlapResults.Length; i++)
        {
            _overlapResults[i] = null;
        }
        sensor.AddObservation(dashIndicatorFound);
        sensor.AddObservation(relativeDashIndicatorPos);
        sensor.AddObservation(flameMarkerFound);
        sensor.AddObservation(relativeFlameMarkerPos);
        sensor.AddObservation(isFlameHazardActive);
        sensor.AddObservation(relativeFlamePosObs);
        ObserveClosestProjectiles(sensor, _bossFireballs, _maxBossFireballsToObserve);
    }

    // ==================== Observation and Action Logic ====================
    /// <summary>
    /// Draws a debug ray in the Scene view.
    /// </summary>
    private void DrawRay(Vector3 start, Vector3 dir, Color color)
    {
        Debug.DrawRay(start, dir, color);
    }

    /// <summary>
    /// Observes the closest projectiles and adds their data to the sensor.
    /// </summary>
    /// <param name="sensor">The ML-Agents vector sensor.</param>
    /// <param name="projectiles">Array of projectile GameObjects.</param>
    /// <param name="maxToObserve">Maximum number of projectiles to observe.</param>
    private void ObserveClosestProjectiles(VectorSensor sensor, GameObject[] projectiles, int maxToObserve)
    {
        if (projectiles == null)
        {
            for (int i = 0; i < maxToObserve; i++)
            {
                sensor.AddObservation(Vector2.zero);
                sensor.AddObservation(Vector2.zero);
                sensor.AddObservation(false);
            }
            Debug.LogWarning(DebugProjectilesNull);
            return;
        }
        var activeProjectilesInfo = projectiles
            .Where(p => p != null && p.activeInHierarchy)
            .Select(p => new
            {
                GameObject = p,
                Distance = Vector2.Distance(transform.position, p.transform.position),
                Rigidbody = p.GetComponent<Rigidbody2D>()
            })
            .OrderBy(p => p.Distance)
            .Take(maxToObserve)
            .ToList();
        int observedCount = 0;
        foreach (var projInfo in activeProjectilesInfo)
        {
            sensor.AddObservation((Vector2)(projInfo.GameObject.transform.position - transform.position));
            sensor.AddObservation(projInfo.Rigidbody != null ? projInfo.Rigidbody.velocity : Vector2.zero);
            sensor.AddObservation(true);
            observedCount++;
        }
        for (int i = observedCount; i < maxToObserve; i++)
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(false);
        }
    }

    // ==================== ML-Agents Action Handling ====================
    /// <summary>
    /// Receives actions from the ML-Agents policy and applies them to the player.
    /// </summary>
    /// <param name="actions">ActionBuffers from ML-Agents.</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(_penaltyPerStep);
        int moveAction = actions.DiscreteActions[0];
        float rawJumpAction = actions.ContinuousActions[0];
        int fallThroughAction = actions.DiscreteActions[1];
        int attackAction = actions.DiscreteActions[2];
        float requestedDirection = (moveAction == 1) ? -1f : (moveAction == 2) ? 1f : 0f;
        float moveDirection = _lastMoveDirection; // default to last valid direction
        // Check if direction actually changed
        bool directionChanged = requestedDirection != 0f && requestedDirection != _lastMoveDirection;

        if (!directionChanged || Time.time - _lastDirectionChangeTime > directionChangeCooldown)
        {
            moveDirection = requestedDirection;

            if (directionChanged)
            {
                _lastDirectionChangeTime = Time.time;
                _lastMoveDirection = moveDirection;
            }
        }
        float jumpDuration = 0f;
        if (rawJumpAction > 0f)
        {
            jumpDuration = rawJumpAction * MaxJumpHoldDuration;
        }
        jumpDuration = Mathf.Clamp(jumpDuration, 0f, MaxJumpHoldDuration);
        bool fallThroughPressed = (fallThroughAction == 1);
        bool attackPressed = (attackAction == 1);
        _playerMovement?.SetAIInput(moveDirection);
        _playerMovement?.SetAIJump(jumpDuration);
        _oneWayPlatform?.SetAIFallThrough(fallThroughPressed);
        _playerAttack?.SetAIAttack(attackPressed);
        Debug.Log(attackPressed);
    }

    // ==================== Reward and Episode Logic ====================
    /// <summary>
    /// Handles player damage, applies penalty, and ends episode if dead.
    /// </summary>
    /// <param name="damage">Amount of damage taken.</param>
    public void HandlePlayerDamaged(float damage)
    {
        if (_isPlayerDead || _isBossDefeated) return;
        AddReward(_penaltyTakeDamage);
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0)
        {
            _isPlayerDead = true;
            Debug.Log(damage);
            Debug.Log(DebugPlayerDied);
            AddReward(_penaltyLose);
            EpisodeManager.Instance?.RecordEndOfEpisode(bossWon: true);
            EndEpisode();
        }
    }

    /// <summary>
    /// Handles boss damage, applies reward.
    /// </summary>
    /// <param name="damage">Amount of damage dealt to boss.</param>
    private void HandleBossDamaged(float damage)
    {
        if (_isPlayerDead || _isBossDefeated) return;
        AddReward(_rewardDamageBoss);
    }

    /// <summary>
    /// Handles boss death, applies win reward, and ends episode.
    /// </summary>
    private void HandleBossDied()
    {
        if (_isPlayerDead || _isBossDefeated) return;
        _isBossDefeated = true;
        Debug.Log(DebugBossDefeated);
        AddReward(_rewardWin);
        EpisodeManager.Instance?.RecordEndOfEpisode(bossWon: false);
        EndEpisode();
    }

    /// <summary>
    /// Provides manual control for testing (heuristic mode).
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        var continuousActions = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.A)) discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.D)) discreteActions[0] = 2;
        else discreteActions[0] = 0;
        discreteActions[1] = Input.GetKey(KeyCode.S) ? 1 : 0;
        discreteActions[2] = Input.GetKey(KeyCode.LeftControl) ? 1 : 0;
        continuousActions[0] = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;
    }

    /// <summary>
    /// Cleans up event subscriptions on destroy.
    /// </summary>
    void OnDestroy()
    {
        if (_playerHealth != null) { _playerHealth.OnDamaged -= HandlePlayerDamaged; }
        if (_bossHealth != null)
        {
            _bossHealth.OnBossDamaged -= HandleBossDamaged;
            _bossHealth.OnBossDied -= HandleBossDied;
        }
    }
}
