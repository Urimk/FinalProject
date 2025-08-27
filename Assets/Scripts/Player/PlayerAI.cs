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
    
    // Environment Constants
    private const int OverlapResultsBufferSize = 10;
    private const float DefaultHazardDetectionRadius = 15f;
    private const int DefaultHazardDetectionLayerMask = -1;
    
    // Reward Constants - Simplified System
    private const float DefaultRewardWin = 10.0f; // Reward for winning (killing the boss)
    private const float DefaultPenaltyLose = -5.0f; // Penalty for losing (dying)
    private const float DefaultRewardDamageBoss = 3.0f; // Reward for hitting the boss
    private const float DefaultPenaltyTakeDamage = -5.0f; // Penalty for getting hit
    private const float DefaultPenaltyPerStep = -0.001f; // Penalty for time passing
    private const float DefaultRewardAvoidAttack = 1.0f; // Reward for avoiding boss attacks
    
    // Boss Proximity Penalties
    private const float DefaultPenaltyCloseToBossLevel1 = -0.01f; // Penalty for being close to boss (level 1)
    private const float DefaultPenaltyCloseToBossLevel2 = -0.02f; // Penalty for being very close to boss (level 2)
    private const float DefaultSafeDistanceFromBoss = 4.0f; // Safe distance from boss
    private const float DefaultDangerDistanceFromBoss = 1.5f; // Very dangerous distance from boss
    
    // Useless Decision Penalties
    private const float DefaultPenaltyUselessFallThrough = -0.1f; // Penalty for fall through while not on platform
    private const float DefaultPenaltyUselessMove = -0.05f; // Penalty for moving into wall
    private const float DefaultPenaltyUselessAction = -0.05f; // Penalty for action while disabled
    private const float DefaultPenaltyAttackMissed = -0.1f; // Penalty for player attack missing
    
    // Action Constants
    private const float MaxJumpHoldDuration = 0.6f;
    
    // Observation Constants
    private const int DefaultMaxBossFireballsToObserve = 1;
    private const int MaxProjectilesToObserve = 3;
    private const float DistanceNormalizationFactor = 20f;
    
    // Tag Constants
    private const string DashTargetIndicatorTag = "DashTargetIndicator";
    private const string FlameWarningMarkerTag = "FlameWarningMarker";
    
    // Debug Constants
    private const string DebugPlayerDied = "[PlayerAI] Player Died! Ending Episode.";
    private const string DebugBossDefeated = "[PlayerAI] Boss Defeated! Ending Episode.";
    private const string DebugOnEpisodeBegin = "[PlayerAI] OnEpisodeBegin completed.";
    private const string DebugProjectilesNull = "[PlayerAI] Projectiles array is null in ObserveClosestProjectiles.";
    
    // Platform Interaction Constants
    private const float PlatformUsageReward = 0.01f;

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
    [Tooltip("Reference to the BossAI component.")]
    [FormerlySerializedAs("aiBoss")]
    [SerializeField] private BossAI _bossAI;
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
    [Header("Training Mode")]
    [Tooltip("Whether this AI is in training mode (affects respawn behavior).")]
    [SerializeField] private bool isTraining = false;

    [Header("Room Layout References")]
    [Tooltip("Left wall Transform for distance calculations.")]
    [SerializeField] private Transform _leftWall;
    [Tooltip("Right wall Transform for distance calculations.")]
    [SerializeField] private Transform _rightWall;
    [Tooltip("Left platform Transform for distance calculations.")]
    [SerializeField] private Transform _leftPlatform;
    [Tooltip("Right platform Transform for distance calculations.")]
    [SerializeField] private Transform _rightPlatform;
    
    [Header("Platform References")]
    [Tooltip("Array of OneWayPlatform components in the room.")]
    [SerializeField] private OneWayPlatform[] _platforms;
    [SerializeField] private OneWayPlatform _currentPlatform;



    [Header("Reward Settings")]
    [Tooltip("Reward for winning (killing the boss).")]
    [FormerlySerializedAs("rewardWin")]
    [SerializeField] private float _rewardWin = DefaultRewardWin;
    [Tooltip("Penalty for losing (dying).")]
    [FormerlySerializedAs("penaltyLose")]
    [SerializeField] private float _penaltyLose = DefaultPenaltyLose;
    [Tooltip("Reward for hitting the boss.")]
    [FormerlySerializedAs("rewardDamageBoss")]
    [SerializeField] private float _rewardDamageBoss = DefaultRewardDamageBoss;
    [Tooltip("Penalty for getting hit.")]
    [FormerlySerializedAs("penaltyTakeDamage")]
    [SerializeField] private float _penaltyTakeDamage = DefaultPenaltyTakeDamage;
    [Tooltip("Penalty for time passing.")]
    [FormerlySerializedAs("penaltyPerStep")]
    [SerializeField] private float _penaltyPerStep = DefaultPenaltyPerStep;
    [Tooltip("Reward for avoiding boss attacks.")]
    [SerializeField] private float _rewardAvoidAttack = DefaultRewardAvoidAttack;
    
    [Header("Boss Proximity Penalties")]
    [Tooltip("Penalty for being close to boss (level 1 - dangerous).")]
    [SerializeField] private float _penaltyCloseToBossLevel1 = DefaultPenaltyCloseToBossLevel1;
    [Tooltip("Penalty for being very close to boss (level 2 - very dangerous).")]
    [SerializeField] private float _penaltyCloseToBossLevel2 = DefaultPenaltyCloseToBossLevel2;
    [Tooltip("Safe distance from boss (no penalty).")]
    [SerializeField] private float _safeDistanceFromBoss = DefaultSafeDistanceFromBoss;
    [Tooltip("Very dangerous distance from boss (level 2 penalty).")]
    [SerializeField] private float _dangerDistanceFromBoss = DefaultDangerDistanceFromBoss;
    
    [Header("Useless Decision Penalties")]
    [Tooltip("Penalty for fall through while not on platform.")]
    [SerializeField] private float _penaltyUselessFallThrough = DefaultPenaltyUselessFallThrough;
    [Tooltip("Penalty for moving into wall.")]
    [SerializeField] private float _penaltyUselessMove = DefaultPenaltyUselessMove;
    [Tooltip("Penalty for action while disabled (move/fire while can't).")]
    [SerializeField] private float _penaltyUselessAction = DefaultPenaltyUselessAction;
    
    [Tooltip("Penalty for player attack missing.")]
    [SerializeField] private float _penaltyAttackMissed = DefaultPenaltyAttackMissed;

    [Header("Observation Settings")]
    [Tooltip("Maximum number of boss fireballs to observe.")]
    [FormerlySerializedAs("maxBossFireballsToObserve")]
    [SerializeField] private int _maxBossFireballsToObserve = DefaultMaxBossFireballsToObserve;
    
    [Tooltip("Whether to include distance-based observations.")]
    [SerializeField] private bool _includeDistanceObservations = true;
    [Tooltip("Whether to include movement pattern observations.")]
    [SerializeField] private bool _includeMovementPatternObservations = true;
    [Tooltip("Whether to include environmental awareness observations.")]
    [SerializeField] private bool _includeEnvironmentalObservations = true;
    [Header("Physics Detection")]
    [Tooltip("Radius for hazard detection.")]
    [FormerlySerializedAs("hazardDetectionRadius")]
    [SerializeField] private float _hazardDetectionRadius = DefaultHazardDetectionRadius;
    [Tooltip("Layer mask for hazard detection.")]
    [FormerlySerializedAs("hazardDetectionLayerMask")]
    [SerializeField] private LayerMask _hazardDetectionLayerMask = DefaultHazardDetectionLayerMask;
    [Header("Direction Change Cooldown")]
    [Tooltip("Base cooldown time (in seconds) between direction changes to prevent erratic behavior.")]
    [SerializeField] private float directionChangeCooldown = 0.2f; // seconds
    [Tooltip("Whether to apply penalties for rapid direction changes during cooldown.")]
    [SerializeField] private bool penalizeRapidDirectionChanges = true;
    [Tooltip("Penalty applied when direction change is blocked by cooldown.")]
    [SerializeField] private float directionChangePenalty = -0.01f;
    [Tooltip("Whether to provide cooldown information in observations for better AI learning.")]
    [SerializeField] private bool includeCooldownInObservations = true;


    // ==================== Private Fields ====================
    private Collider2D[] _overlapResults = new Collider2D[OverlapResultsBufferSize];
    private Rigidbody2D _rigidbody2D;
    private bool _isBossDefeated = false;
    private bool _isPlayerDead = false;
    private float _lastDirectionChangeTime = 0f;
    private float _lastMoveDirection = 0f; // -1, 0, or 1
    
    // Action tracking for useless decision detection
    private bool _lastMoveAction = false;
    private bool _lastAttackAction = false;
    
    // Statistics tracking for efficiency metrics
    private int _totalDirectionChangesAttempted = 0;
    private int _successfulDirectionChanges = 0;
    private int _totalMovementsAttempted = 0;
    private int _successfulMovements = 0;
    private int _totalFallThroughsAttempted = 0;
    private int _successfulFallThroughs = 0;
    private int _totalAttacksAttempted = 0;
    private int _successfulAttacks = 0;
    private int _totalBossAttacks = 0;
    private int _bossAttacksAvoided = 0;
    private float _totalBossDistance = 0f;
    private int _bossDistanceChecks = 0;
    private float _totalEpisodeRewards = 0f;
    private float _totalEpisodePenalties = 0f;
    
    // Distance tracking for penalty calculation
    private int _timeInSafeDistance = 0;
    private int _timeInLevel1Distance = 0;
    private int _timeInLevel2Distance = 0;
    
    // ==================== Episode Management ====================
    private float _episodeStartTime = 0f;
    private float _episodeDuration = 0f;
    private int _episodeDamageTaken = 0;
    private int _episodeDamageDealt = 0;
    private bool _episodeEnded = false;
    
    // ==================== Direction Change Cooldown Management ====================
    private int _episodeDirectionChanges = 0;
    private int _episodeBlockedDirectionChanges = 0;
    private float _cooldownRemainingTime = 0f;
    private bool _isDirectionChangeBlocked = false;
    
    // ==================== Simplified Tracking ====================
    private Vector2 _lastPosition = Vector2.zero;
    private float _lastDistanceToBoss = 0f;

    /// <summary>
    /// Whether this AI is in training mode.
    /// </summary>
    public bool IsTraining => isTraining;

    /// <summary>
    /// Initializes the agent, sets up event connections, and validates the input system.
    /// 
    /// This method performs the following initialization tasks:
    /// 1. Gets and caches required components
    /// 2. Calculates raycast parameters for observations
    /// 3. Sets up event subscriptions for health and boss events
    /// 4. Validates that the AI input system is properly configured
    /// </summary>
    public override void Initialize()
    {
        // Get and cache required components
        _rigidbody2D = GetComponent<Rigidbody2D>();
        
        // Set up event subscriptions
        if (_playerHealth != null) 
        { 
            _playerHealth.OnDamaged += HandlePlayerDamaged; 
        }
        else 
        {
            Debug.LogError("Player Health component not found!", this);
        }
        
        if (_bossHealth != null)
        {
            _bossHealth.OnBossDamaged += HandleBossDamaged;
            _bossHealth.OnBossDied += HandleBossDied;
        }
        else 
        {
            Debug.LogError("Boss Health component not found!", this);
        }
        
        // Validate AI input system
        ValidateAIInputSystem();
        
        // Validate room layout references
        ValidateRoomLayoutReferences();
        
        // Validate platform references
        ValidatePlatformReferences();
    }
    
    /// <summary>
    /// Validates that the AI input system is properly configured.
    /// 
    /// This method checks that the AIInput component exists and is properly set up
    /// for AI control. It logs warnings if the system is not configured correctly.
    /// </summary>
    private void ValidateAIInputSystem()
    {
        AIInput aiInput = GetComponent<AIInput>();
        if (aiInput == null)
        {
            Debug.LogError("[PlayerAI] AIInput component not found! The AI cannot control the player without this component.");
        }
        else
        {
            Debug.Log("[PlayerAI] AI input system validated successfully.");
        }
        
        // Also check that PlayerMovement is configured for AI control
        if (_playerMovement != null)
        {
            // The PlayerMovement should automatically add AIInput when _isAIControlled is true
            // This is handled in PlayerMovement.InitializeInputSystem()
        }
    }
    
    /// <summary>
    /// Validates that the room layout Transform references are properly configured.
    /// 
    /// This method checks that the wall and platform Transform references exist
    /// and logs warnings if any are missing. Missing references will use fallback values.
    /// </summary>
    private void ValidateRoomLayoutReferences()
    {
        if (_leftWall == null)
        {
            Debug.LogWarning("[PlayerAI] Left wall Transform reference is missing! Distance calculations will use fallback values.");
        }
        
        if (_rightWall == null)
        {
            Debug.LogWarning("[PlayerAI] Right wall Transform reference is missing! Distance calculations will use fallback values.");
        }
        
        if (_leftPlatform == null)
        {
            Debug.LogWarning("[PlayerAI] Left platform Transform reference is missing! Distance calculations will use fallback values.");
        }
        
        if (_rightPlatform == null)
        {
            Debug.LogWarning("[PlayerAI] Right platform Transform reference is missing! Distance calculations will use fallback values.");
        }
        
        if (_leftWall != null && _rightWall != null && _leftPlatform != null && _rightPlatform != null)
        {
            Debug.Log("[PlayerAI] All room layout references validated successfully.");
        }
    }
    
    /// <summary>
    /// Validates that the platform references are properly configured.
    /// 
    /// This method checks that the platform array exists and contains valid OneWayPlatform components.
    /// It logs warnings if any platforms are missing or improperly configured.
    /// </summary>
    private void ValidatePlatformReferences()
    {
        if (_platforms == null || _platforms.Length == 0)
        {
            Debug.LogWarning("[PlayerAI] No platform references found! Platform interaction will be disabled.");
            return;
        }
        
        int validPlatforms = 0;
        for (int i = 0; i < _platforms.Length; i++)
        {
            if (_platforms[i] == null)
            {
                Debug.LogWarning($"[PlayerAI] Platform at index {i} is null!");
            }
            else
            {
                validPlatforms++;
            }
        }
        
        if (validPlatforms > 0)
        {
            //Debug.Log($"[PlayerAI] Platform references validated successfully. Found {validPlatforms} valid platforms.");
        }
        else
        {
            Debug.LogWarning("[PlayerAI] No valid platform references found! Platform interaction will be disabled.");
        }
    }

    /// <summary>
    /// Called at the beginning of each episode to reset state and environment.
    /// 
    /// This method handles the complete reset of the AI player for a new episode:
    /// 1. Resets internal episode state flags and episode tracking
    /// 2. Resets player position and physics
    /// 3. Resets player health and attack cooldowns
    /// 4. Ensures the input system is properly initialized
    /// 5. Resets the environment through EpisodeManager (supports all boss modes)
    /// 6. Clears all projectiles
    /// 7. Initializes episode tracking for performance monitoring
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (!isTraining)
        {
            return;
        }
        
        // Reset episode state flags
        _isBossDefeated = false;
        _isPlayerDead = false;
        _episodeEnded = false;
        
        // Reset episode tracking
        _episodeStartTime = Time.time;
        _episodeDuration = 0f;
        _episodeDamageTaken = 0;
        _episodeDamageDealt = 0;
        
        // Reset direction change cooldown tracking
        _episodeDirectionChanges = 0;
        _episodeBlockedDirectionChanges = 0;
        _cooldownRemainingTime = 0f;
        _isDirectionChangeBlocked = false;
        
        // Reset simplified tracking
        _lastPosition = Vector2.zero;
        _lastDistanceToBoss = 0f;
        
        // Reset action tracking
        _lastMoveAction = false;
        _lastAttackAction = false;
        
        // Reset statistics tracking
        _totalDirectionChangesAttempted = 0;
        _successfulDirectionChanges = 0;
        _totalMovementsAttempted = 0;
        _successfulMovements = 0;
        _totalFallThroughsAttempted = 0;
        _successfulFallThroughs = 0;
        _totalAttacksAttempted = 0;
        _successfulAttacks = 0;
        _totalBossAttacks = 0;
        _bossAttacksAvoided = 0;
        _totalBossDistance = 0f;
        _bossDistanceChecks = 0;
        _totalEpisodeRewards = 0f;
        _totalEpisodePenalties = 0f;
        
        // Reset distance tracking
        _timeInSafeDistance = 0;
        _timeInLevel1Distance = 0;
        _timeInLevel2Distance = 0;
        
        // Reset player position and physics
        Vector3 initialPosition = Vector3.zero;
        if (EpisodeManager.Instance != null)
        {
            initialPosition = EpisodeManager.Instance.InitialPlayerPosition;
            Debug.Log($"[PlayerAI] Episode {EpisodeManager.Instance.EpisodeCount + 1} starting - Boss Mode: {EpisodeManager.Instance.CurrentBossMode}");
        }
        else
        {
            Debug.LogWarning("[PlayerAI] EpisodeManager.Instance is null - using default position");
        }
        
        transform.position = initialPosition;
        transform.rotation = Quaternion.identity;
        _rigidbody2D.velocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
        
        // Reset player components
        _playerHealth?.ResetHealth();
        _playerAttack?.ResetCooldown();
        _playerMovement?.ResetState();
        
        // Ensure components are enabled
        if (_playerMovement != null) _playerMovement.enabled = true;
        if (_playerAttack != null) _playerAttack.enabled = true;
        
        // Enable collider
        Collider2D playerCol = GetComponent<Collider2D>();
        if (playerCol != null) playerCol.enabled = true;
        
        // Ensure AI input system is properly initialized
        EnsureAIInputSystem();
        
        // Reset environment and clear projectiles
        if (EpisodeManager.Instance != null)
        {
            EpisodeManager.Instance.ResetEnvironmentForNewEpisode();
        }
        else
        {
            Debug.LogWarning("[PlayerAI] EpisodeManager.Instance is null - cannot reset environment");
        }
        
        ClearProjectiles(_bossFireballs);
        ClearProjectiles(_playerFireballs);
        
        Debug.Log($"[PlayerAI] Episode {EpisodeManager.Instance?.EpisodeCount ?? 0} initialized successfully");
    }
    
    /// <summary>
    /// Ensures the AI input system is properly initialized for the episode.
    /// 
    /// This method verifies that the AIInput component exists and is properly set up
    /// to handle AI control. If the component is missing, it logs an error.
    /// </summary>
    private void EnsureAIInputSystem()
    {
        AIInput aiInput = GetComponent<AIInput>();
        if (aiInput == null)
        {
            Debug.LogError("[PlayerAI] AIInput component not found! The AI cannot control the player without this component.");
        }
        else
        {
            // Reset the AI input state
            aiInput.ResetInput();
        }
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
        sensor.AddObservation(_playerMovement != null && _playerMovement.IsGrounded);
        sensor.AddObservation(_playerHealth != null && _playerHealth.Invulnerable);
        sensor.AddObservation(_playerMovement != null && _playerMovement.OnWall());
        sensor.AddObservation(_playerMovement != null ? _playerMovement.GetFacingDirection() : 1f);
        sensor.AddObservation(_playerAttack != null ? _playerAttack.IsAttackReady() : 1f);
        
        // Direction change cooldown observations (if enabled)
        if (includeCooldownInObservations)
        {
            sensor.AddObservation(_isDirectionChangeBlocked);
            sensor.AddObservation(_cooldownRemainingTime / directionChangeCooldown); // Normalized cooldown time
            sensor.AddObservation(_lastMoveDirection);
        }

        // Platform interaction observations
        AddPlatformObservations(sensor);
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
            if (hit.CompareTag(DashTargetIndicatorTag))
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
    /// Adds enhanced observations for better AI learning and decision making.
    /// 
    /// This method provides additional context about the player's situation,
    /// including distance-based observations, movement patterns, and environmental awareness.
    /// </summary>
    /// <param name="sensor">The ML-Agents vector sensor.</param>
    private void AddEnhancedObservations(VectorSensor sensor)
    {
        Vector2 currentPosition = transform.position;
        
        // Distance-based observations
        if (_includeDistanceObservations)
        {
            AddDistanceObservations(sensor, currentPosition);
        }
        
        // Movement pattern observations
        if (_includeMovementPatternObservations)
        {
            AddMovementPatternObservations(sensor, currentPosition);
        }
        
        // Environmental awareness observations
        if (_includeEnvironmentalObservations)
        {
            AddEnvironmentalObservations(sensor, currentPosition);
        }
    }
    
    /// <summary>
    /// Adds distance-based observations to help the AI understand spatial relationships.
    /// </summary>
    /// <param name="sensor">The ML-Agents vector sensor.</param>
    /// <param name="currentPosition">Current player position.</param>
    private void AddDistanceObservations(VectorSensor sensor, Vector2 currentPosition)
    {
        // Distance to boss (normalized)
        if (_boss != null && _boss.gameObject.activeInHierarchy)
        {
            float distanceToBoss = Vector2.Distance(currentPosition, _boss.position);
            sensor.AddObservation(distanceToBoss / DistanceNormalizationFactor);
            
            // Store for reward calculations
            _lastDistanceToBoss = distanceToBoss;
            
            // Track boss distance for statistics
            _totalBossDistance += distanceToBoss;
            _bossDistanceChecks++;
        }
        else
        {
            sensor.AddObservation(1f); // Normalized max distance when boss is not available
        }
        
        // Distance to nearest hazard (flames)
        if (_flames != null && _flames.gameObject.activeInHierarchy)
        {
            float distanceToFlames = Vector2.Distance(currentPosition, _flames.position);
            sensor.AddObservation(distanceToFlames / DistanceNormalizationFactor);
        }
        else
        {
            sensor.AddObservation(1f); // No hazard nearby
        }
        
        // Platform awareness - distance to nearest platform
        float nearestPlatformDistance = FindNearestPlatformDistance(currentPosition);
        sensor.AddObservation(nearestPlatformDistance / DistanceNormalizationFactor);
        
        // Wall distances for wall jump planning
        float distanceToLeftWall = _leftWall != null ? Vector2.Distance(currentPosition, _leftWall.position) : DistanceNormalizationFactor;
        float distanceToRightWall = _rightWall != null ? Vector2.Distance(currentPosition, _rightWall.position) : DistanceNormalizationFactor;
        sensor.AddObservation(distanceToLeftWall / DistanceNormalizationFactor);
        sensor.AddObservation(distanceToRightWall / DistanceNormalizationFactor);
    }
    
    /// <summary>
    /// Adds movement pattern observations to help the AI understand its own behavior.
    /// </summary>
    /// <param name="sensor">The ML-Agents vector sensor.</param>
    /// <param name="currentPosition">Current player position.</param>
    private void AddMovementPatternObservations(VectorSensor sensor, Vector2 currentPosition)
    {
        // Movement delta from last position
        Vector2 movementDelta = currentPosition - _lastPosition;
        sensor.AddObservation(movementDelta / DistanceNormalizationFactor);
        
        // Movement speed (normalized)
        float movementSpeed = movementDelta.magnitude / Time.deltaTime;
        sensor.AddObservation(movementSpeed / 10f); // Normalize to reasonable range
        
        // Update last position
        _lastPosition = currentPosition;
    }
    
    /// <summary>
    /// Adds environmental awareness observations to help the AI understand its surroundings.
    /// </summary>
    /// <param name="sensor">The ML-Agents vector sensor.</param>
    /// <param name="currentPosition">Current player position.</param>
    private void AddEnvironmentalObservations(VectorSensor sensor, Vector2 currentPosition)
    {
        // Height from ground (useful for jump timing) - simplified calculation
        float heightFromGround = currentPosition.y - (-4f); // Assuming ground is at y = -4
        sensor.AddObservation(Mathf.Clamp01(heightFromGround / 10f)); // Normalize to 0-1 range
        
        // Available space above player - simplified calculation
        float spaceAbove = 8f - currentPosition.y; // Assuming ceiling is at y = 8
        sensor.AddObservation(Mathf.Clamp01(spaceAbove / 10f)); // Normalize to 0-1 range
        
        // Horizontal space available - use wall distances
        float facingDirection = _playerMovement != null ? _playerMovement.GetFacingDirection() : 1f;
        float horizontalSpace = facingDirection > 0 ? 
            (_rightWall != null ? Vector2.Distance(currentPosition, _rightWall.position) : DistanceNormalizationFactor) : 
            (_leftWall != null ? Vector2.Distance(currentPosition, _leftWall.position) : DistanceNormalizationFactor);
        sensor.AddObservation(horizontalSpace / DistanceNormalizationFactor);
    }
    
    /// <summary>
    /// Finds the distance to the nearest platform for jump planning and navigation.
    /// </summary>
    /// <param name="currentPosition">Current player position.</param>
    /// <returns>Distance to nearest platform, or max distance if no platform found.</returns>
    private float FindNearestPlatformDistance(Vector2 currentPosition)
    {
        // Calculate distances to both platforms
        float distanceToLeftPlatform = _leftPlatform != null ? Vector2.Distance(currentPosition, _leftPlatform.position) : DistanceNormalizationFactor;
        float distanceToRightPlatform = _rightPlatform != null ? Vector2.Distance(currentPosition, _rightPlatform.position) : DistanceNormalizationFactor;
        
        // Return the nearest platform distance
        return Mathf.Min(distanceToLeftPlatform, distanceToRightPlatform);
    }
    
    /// <summary>
    /// Adds platform interaction observations to help the AI understand platform state and usage.
    /// </summary>
    /// <param name="sensor">The ML-Agents vector sensor.</param>
    private void AddPlatformObservations(VectorSensor sensor)
    {
        // Update current platform detection
        UpdateCurrentPlatform();
        
        // Current platform state
        bool isOnPlatform = _currentPlatform != null;
        sensor.AddObservation(isOnPlatform ? 1f : 0f);
    
        
        // Distance to nearest platform
        float nearestPlatformDistance = FindNearestPlatformDistance(transform.position);
        sensor.AddObservation(nearestPlatformDistance / DistanceNormalizationFactor);
        

    }
    
    /// <summary>
    /// Updates the current platform detection based on player position.
    /// </summary>
    private void UpdateCurrentPlatform()
    {
        _currentPlatform = null;
        
        if (_platforms == null || _platforms.Length == 0) return;
        
        Vector2 playerPosition = transform.position;
        
        foreach (var platform in _platforms)
        {
            if (platform == null) continue;
            
            if (platform.IsPlayerOnPlatform(this.transform))
            {
                _currentPlatform = platform;
                break;
            }
        }
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
    /// Receives actions from the ML-Agents policy and applies them to the player using the new input system.
    /// 
    /// This method processes ML-Agents actions and converts them to input commands:
    /// 1. Movement direction (left, right, none) with direction change cooldown
    /// 2. Jump duration (continuous value for variable jump height)
    /// 3. Fall through platform (discrete action)
    /// 4. Attack (discrete action)
    /// 
    /// The direction change cooldown prevents rapid direction switching that could confuse the AI.
    /// </summary>
    /// <param name="actions">ActionBuffers from ML-Agents containing discrete and continuous actions.</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Apply base penalty per step (time passing)
        AddReward(_penaltyPerStep);
        TrackReward(_penaltyPerStep);
        
        // Apply simplified rewards
        ApplySimplifiedRewards();
        
        // Extract actions from ML-Agents
        int moveAction = actions.DiscreteActions[0];
        float rawJumpAction = actions.ContinuousActions[0];
        int fallThroughAction = actions.DiscreteActions[1];
        Debug.Log("fallThroughAction: " + fallThroughAction);
        int attackAction = actions.DiscreteActions[2];
        
        // Store actions for useless decision detection
        _lastMoveAction = (moveAction != 0);
        _lastAttackAction = (attackAction == 1);
        
        // Track attempts for statistics
        if (moveAction != 0) _totalMovementsAttempted++;
        if (fallThroughAction == 1) _totalFallThroughsAttempted++;
        if (attackAction == 1) _totalAttacksAttempted++;
        
        // Process movement direction with cooldown
        float requestedDirection = (moveAction == 1) ? -1f : (moveAction == 2) ? 1f : 0f;
        float moveDirection = ProcessMovementDirection(requestedDirection);
        
        // Process jump duration
        float jumpDuration = ProcessJumpDuration(rawJumpAction);
        
        // Process other actions
        bool fallThroughPressed = (fallThroughAction == 1);
        bool attackPressed = (attackAction == 1);
        
        // Apply actions through the new input system
        ApplyAIActions(moveDirection, jumpDuration, fallThroughPressed, attackPressed);
    }

    public void OnBossAttackDodged()
    {
        _bossAttacksAvoided++;
        AddReward(_rewardAvoidAttack);
        TrackReward(_rewardAvoidAttack);
    }
    
    /// <summary>
    /// Called when the boss fires an attack (for tracking total attacks).
    /// </summary>
    public void OnBossAttackFired()
    {
        _totalBossAttacks++;
    }
    
    /// <summary>
    /// Manually tracks rewards and penalties for statistics.
    /// </summary>
    /// <param name="reward">The reward value to add.</param>
    private void TrackReward(float reward)
    {
        if (reward > 0)
        {
            _totalEpisodeRewards += reward;
        }
        else
        {
            _totalEpisodePenalties += Mathf.Abs(reward);
        }
    }
    
    /// <summary>
    /// Called when a player attack misses (for tracking attack efficiency).
    /// </summary>
    public void OnPlayerAttackMissed()
    {
        AddReward(_penaltyAttackMissed);
        TrackReward(_penaltyAttackMissed);
    }
    
    /// <summary>
    /// Processes movement direction with respect to the direction change cooldown.
    /// 
    /// This enhanced method provides intelligent direction change management:
    /// 1. Tracks direction changes and blocked changes for episode statistics
    /// 2. Applies penalties for rapid direction changes when enabled
    /// 3. Updates cooldown state for observation feedback
    /// 4. Provides better feedback to the AI for learning
    /// </summary>
    /// <param name="requestedDirection">The direction the AI wants to move (-1, 0, 1).</param>
    /// <returns>The processed movement direction that respects the cooldown.</returns>
    private float ProcessMovementDirection(float requestedDirection)
    {
        // Check if direction actually changed
        bool directionChanged = requestedDirection != 0f && requestedDirection != _lastMoveDirection;

        // Update cooldown remaining time
        _cooldownRemainingTime = Mathf.Max(0f, directionChangeCooldown - (Time.time - _lastDirectionChangeTime));
        _isDirectionChangeBlocked = _cooldownRemainingTime > 0f;
        
        // Track direction change attempts
        if (directionChanged) _totalDirectionChangesAttempted++;
        
        // Apply cooldown logic
        if (!directionChanged || !_isDirectionChangeBlocked)
        {
            if (directionChanged)
            {
                _lastDirectionChangeTime = Time.time;
                _lastMoveDirection = requestedDirection;
                _episodeDirectionChanges++;
                _successfulDirectionChanges++;
                
                // Reset cooldown state
                _cooldownRemainingTime = 0f;
                _isDirectionChangeBlocked = false;
            }
            return requestedDirection;
        }
        
        // Direction change blocked by cooldown
        _episodeBlockedDirectionChanges++;
        
        // Apply penalty if enabled
        if (penalizeRapidDirectionChanges)
        {
            AddReward(directionChangePenalty);
            TrackReward(directionChangePenalty);
        }
        
        // Return last valid direction if cooldown is active
        return _lastMoveDirection;
    }
    
    /// <summary>
    /// Processes the raw jump action into a jump duration.
    /// 
    /// Converts the continuous jump action (0-1) into a jump duration
    /// that controls how long the jump button is held down.
    /// </summary>
    /// <param name="rawJumpAction">Raw jump action from ML-Agents (0-1).</param>
    /// <returns>Jump duration in seconds.</returns>
    private float ProcessJumpDuration(float rawJumpAction)
    {
        float jumpDuration = 0f;
        if (rawJumpAction > 0f)
        {
            jumpDuration = rawJumpAction * MaxJumpHoldDuration;
        }
        return Mathf.Clamp(jumpDuration, 0f, MaxJumpHoldDuration);
    }
    
    /// <summary>
    /// Applies simplified rewards based on the new reward system.
    /// </summary>
    private void ApplySimplifiedRewards()
    {
        // Apply boss proximity penalties
        ApplyBossProximityPenalties();
        
        // Apply useless decision penalties
        ApplyUselessDecisionPenalties();
    }
    
    /// <summary>
    /// Applies penalties for being too close to the boss (2 levels of closeness).
    /// </summary>
    private void ApplyBossProximityPenalties()
    {
        if (_boss == null || !_boss.gameObject.activeInHierarchy) return;
        
        float currentDistanceToBoss = Vector2.Distance(transform.position, _boss.position);
        
        // Level 2 penalty: Very close to boss (very dangerous)
        if (currentDistanceToBoss < _dangerDistanceFromBoss)
        {
            AddReward(_penaltyCloseToBossLevel2);
            TrackReward(_penaltyCloseToBossLevel2);
        }
        // Level 1 penalty: Close to boss (dangerous)
        else if (currentDistanceToBoss < _safeDistanceFromBoss)
        {
            AddReward(_penaltyCloseToBossLevel1);
            TrackReward(_penaltyCloseToBossLevel1);
        }
    }
    
    /// <summary>
    /// Applies penalties for making useless decisions.
    /// </summary>
    private void ApplyUselessDecisionPenalties()
    {
     
        // Penalty for moving into wall (will be checked in movement logic)
        if (_lastMoveAction && IsBlockedByWall())
        {
            AddReward(_penaltyUselessMove);
            TrackReward(_penaltyUselessMove);
        }
        
        // Penalty for action while disabled (move/fire while can't)
        if (_lastMoveAction && !CanMove())
        {
            AddReward(_penaltyUselessAction);
            TrackReward(_penaltyUselessAction);
        }
        
        if (_lastAttackAction && !CanAttack())
        {
            AddReward(_penaltyUselessAction);
            TrackReward(_penaltyUselessAction);
        }
    }
    


    
    /// <summary>
    /// Checks if the player is on a platform.
    /// </summary>
    /// <returns>True if the player is on a platform.</returns>
    private bool IsOnPlatform()
    {
        if (_platforms == null) return false;
        
        foreach (var platform in _platforms)
        {
            if (platform != null && platform.IsPlayerOnPlatform(this.transform))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Checks if the player is blocked by a wall.
    /// </summary>
    /// <returns>True if the player is blocked by a wall.</returns>
    private bool IsBlockedByWall()
    {
        // Check if player is trying to move but can't due to wall collision
        if (_playerMovement != null)
        {
            return _playerMovement.IsBlockedByWall();
        }
        return false;
    }
    
    /// <summary>
    /// Checks if the player can move.
    /// </summary>
    /// <returns>True if the player can move.</returns>
    private bool CanMove()
    {
        // Check if direction change cooldown is active
        if (_isDirectionChangeBlocked && _cooldownRemainingTime > 0f)
        {
            return false;
        }
        
        return true; // Can move if no cooldown is active
    }
    
    /// <summary>
    /// Checks if the player can attack.
    /// </summary>
    /// <returns>True if the player can attack.</returns>
    private bool CanAttack()
    {
        if (_playerAttack != null)
        {
            return _playerAttack.CanAttack();
        }
        return true; // Default to true if no attack component
    }
    
    
    /// <summary>
    /// Applies AI actions through the new input system.
    /// 
    /// This method uses the AIInput component (part of the new input system)
    /// to control the player character. The AIInput component handles the
    /// actual input processing and communicates with PlayerMovement.
    /// </summary>
    /// <param name="moveDirection">Movement direction (-1, 0, 1).</param>
    /// <param name="jumpDuration">Duration to hold jump (seconds).</param>
    /// <param name="fallThrough">Whether to fall through platforms.</param>
    /// <param name="attack">Whether to attack.</param>
    private void ApplyAIActions(float moveDirection, float jumpDuration, bool fallThrough, bool attack)
    {
        // Get the AIInput component from the new input system
        AIInput aiInput = GetComponent<AIInput>();
        if (aiInput == null)
        {
            Debug.LogError("[PlayerAI] AIInput component not found! AI cannot control the player.");
            return;
        }
        
        // Apply movement and jump through AIInput
        aiInput.SetMovementInput(moveDirection);
        aiInput.SetJumpInput(jumpDuration);
        
        // Track successful movements (if not blocked by wall or cooldown)
        if (moveDirection != 0f && !IsBlockedByWall() && CanMove())
        {
            _successfulMovements++;
        }
        
        // Apply platform fall-through action
        if (_currentPlatform != null)
        {
            if (fallThrough)
            {
                _currentPlatform.SetAIFallThrough();
                _successfulFallThroughs++;
            }
        }
        else{
            if (fallThrough) {
                AddReward(_penaltyUselessFallThrough);
                TrackReward(_penaltyUselessFallThrough);
            }
        }
        
        // Apply attack action
        _playerAttack?.SetAIAttack(attack);
        if (attack && CanAttack()) _successfulAttacks++;
        
        // Debug logging (can be removed in production)
        if (Debug.isDebugBuild && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            //Debug.Log($"[PlayerAI] Actions applied - Move: {moveDirection}, Jump: {jumpDuration:F2}s, Attack: {attack}");
                }
    }
   
    // ==================== Reward and Episode Logic ====================
    /// <summary>
    /// Handles player damage, applies penalty, and ends episode if dead.
    /// 
    /// This method tracks damage taken during the episode and handles episode termination
    /// when the player dies. It provides detailed logging for episode analysis.
    /// </summary>
    /// <param name="damage">Amount of damage taken.</param>
    public void HandlePlayerDamaged(float damage)
    {
        if (_isPlayerDead || _isBossDefeated || _episodeEnded) return;
        
        // Track episode damage
        _episodeDamageTaken++;
        
        // Apply penalty
        AddReward(_penaltyTakeDamage);
        TrackReward(_penaltyTakeDamage);
        
        // Check if player died
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0)
        {
            _isPlayerDead = true;
            _episodeEnded = true;
            
            // Calculate episode duration
            _episodeDuration = Time.time - _episodeStartTime;
            
            // Log episode outcome
            LogEpisodeOutcome("Player Death", damage, true);
            
            // Apply death penalty and end episode
            AddReward(_penaltyLose);
            TrackReward(_penaltyLose);
            
            // Record episode end with EpisodeManager
            if (EpisodeManager.Instance != null)
            {
                EpisodeManager.Instance.RecordEndOfEpisode(bossWon: true);
            }
            else
            {
                Debug.LogWarning("[PlayerAI] EpisodeManager.Instance is null - cannot record episode end");
            }
            
            EndEpisode();
        }
    }

    /// <summary>
    /// Handles boss damage, applies reward, and tracks episode damage dealt.
    /// 
    /// This method tracks damage dealt to the boss during the episode for performance analysis.
    /// </summary>
    /// <param name="damage">Amount of damage dealt to boss.</param>
    private void HandleBossDamaged(float damage)
    {
        if (_isPlayerDead || _isBossDefeated || _episodeEnded) return;
        
        // Track episode damage dealt
        _episodeDamageDealt++;
        
        // Apply reward
        AddReward(_rewardDamageBoss);
        TrackReward(_rewardDamageBoss);
    }

    /// <summary>
    /// Handles boss death, applies win reward, and ends episode.
    /// 
    /// This method handles the victory condition when the boss is defeated.
    /// It provides detailed logging for episode analysis.
    /// </summary>
    private void HandleBossDied()
    {
        if (_isPlayerDead || _isBossDefeated || _episodeEnded) return;
        
        _isBossDefeated = true;
        _episodeEnded = true;
        
        // Calculate episode duration
        _episodeDuration = Time.time - _episodeStartTime;
        
        // Log episode outcome
        LogEpisodeOutcome("Boss Defeated", 0f);
        
        // Apply win reward and end episode
        AddReward(_rewardWin);
        TrackReward(_rewardWin);
        
        // Record episode end with EpisodeManager
        if (EpisodeManager.Instance != null)
        {
            EpisodeManager.Instance.RecordEndOfEpisode(bossWon: false);
        }
        else
        {
            Debug.LogWarning("[PlayerAI] EpisodeManager.Instance is null - cannot record episode end");
        }
        
        EndEpisode();
    }
    
    /// <summary>
    /// Logs detailed episode outcome information for analysis and debugging.
    /// 
    /// This enhanced method provides comprehensive logging about episode performance,
    /// including duration, damage statistics, direction change behavior, and outcome details.
    /// </summary>
    /// <param name="outcome">The outcome of the episode (e.g., "Player Death", "Boss Defeated").</param>
    /// <param name="finalDamage">The final damage value that triggered the episode end.</param>
    /// <param name="includeEnhancedStats">Whether to include enhanced statistics in the log.</param>
    private void LogEpisodeOutcome(string outcome, float finalDamage, bool includeEnhancedStats = false)
    {
        string bossModeInfo = EpisodeManager.Instance != null ? 
            $"Boss Mode: {EpisodeManager.Instance.CurrentBossMode}" : "Boss Mode: Unknown";
        
        // Calculate direction change efficiency
        float directionChangeEfficiency = _episodeDirectionChanges + _episodeBlockedDirectionChanges > 0 ? 
            (float)(_episodeDirectionChanges) / (float)_episodeDirectionChanges + _episodeBlockedDirectionChanges : 1f;
        
        string episodeInfo = $"[PlayerAI] Episode {EpisodeManager.Instance?.EpisodeCount ?? 0} ended - {outcome}" +
                           $"\n  Duration: {_episodeDuration:F2}s" +
                           $"\n  Damage Taken: {_episodeDamageTaken}" +
                           $"\n  Damage Dealt: {_episodeDamageDealt}" +
                           $"\n  {bossModeInfo}";
        
        // Add enhanced statistics if requested
        if (includeEnhancedStats)
        {
            episodeInfo += "\n\n" + GetEnhancedStatistics();
        }
        
        Debug.Log(episodeInfo);
    }
    
    /// <summary>
    /// Handles episode timeout when the episode exceeds the maximum duration.
    /// 
    /// This method is called by the EpisodeManager when an episode times out.
    /// It provides a graceful way to end episodes that are taking too long.
    /// </summary>
    public void HandleEpisodeTimeout()
    {
        if (_episodeEnded) return;
        
        _episodeEnded = true;
        _episodeDuration = Time.time - _episodeStartTime;
        
        // Log timeout outcome
        LogEpisodeOutcome("Timeout", 0f, true);
        
        // Apply timeout penalty
        AddReward(_penaltyLose);
        TrackReward(_penaltyLose);
        
        // Record episode end with EpisodeManager
        if (EpisodeManager.Instance != null)
        {
            EpisodeManager.Instance.RecordEndOfEpisode(bossWon: true); // Boss wins on timeout
        }
        
        EndEpisode();
    }
    
    /// <summary>
    /// Gets the current episode statistics for monitoring and analysis.
    /// 
    /// This enhanced method provides access to comprehensive episode performance data
    /// including direction change behavior that can be used for debugging, analysis,
    /// or external monitoring systems.
    /// </summary>
    /// <returns>A string containing current episode statistics.</returns>
    public string GetEpisodeStatistics()
    {
        float currentDuration = Time.time - _episodeStartTime;
        string bossModeInfo = EpisodeManager.Instance != null ? 
            $"Boss Mode: {EpisodeManager.Instance.CurrentBossMode}" : "Boss Mode: Unknown";
        
        // Calculate direction change efficiency
        float directionChangeEfficiency = _episodeDirectionChanges > 0 ? 
            (float)(_episodeDirectionChanges - _episodeBlockedDirectionChanges) / _episodeDirectionChanges : 1f;
        
        // Cooldown status
        string cooldownStatus = _isDirectionChangeBlocked ? 
            $"Blocked ({_cooldownRemainingTime:F2}s remaining)" : "Available";
        
        return $"[PlayerAI] Episode Statistics:" +
               $"\n  Duration: {currentDuration:F2}s" +
               $"\n  Damage Taken: {_episodeDamageTaken}" +
               $"\n  Damage Dealt: {_episodeDamageDealt}" +
               $"\n  Direction Changes: {_episodeDirectionChanges} (Blocked: {_episodeBlockedDirectionChanges})" +
               $"\n  Direction Change Efficiency: {directionChangeEfficiency:P1}" +
               $"\n  Current Platform: {(_currentPlatform != null ? "On Platform" : "Not on Platform")}" +
               $"\n  Cooldown Status: {cooldownStatus}" +
               $"\n  Episode Ended: {_episodeEnded}" +
               $"\n  {bossModeInfo}";
    }
    
    /// <summary>
    /// Gets enhanced statistics for the current episode including comprehensive efficiency metrics.
    /// </summary>
    /// <returns>A string containing enhanced episode statistics.</returns>
    public string GetEnhancedStatistics()
    {
        float currentDuration = Time.time - _episodeStartTime;
        
        // Episode length and penalty
        float episodeLengthPenalty = currentDuration * Mathf.Abs(_penaltyPerStep);
        
        // Movement efficiency
        float movementEfficiency = _totalMovementsAttempted > 0 ? 
            (float)_successfulMovements / _totalMovementsAttempted * 100f : 0f;
        float movementPenalty = (_totalMovementsAttempted - _successfulMovements) * _penaltyUselessMove;
        
        // Direction change efficiency
        float directionChangeEfficiency = _totalDirectionChangesAttempted > 0 ? 
            (float)_successfulDirectionChanges / _totalDirectionChangesAttempted * 100f : 0f;
        float directionChangePenalty = (_totalDirectionChangesAttempted - _successfulDirectionChanges) * _penaltyUselessAction;
        
        // Boss distance statistics and penalties
        float averageBossDistance = _bossDistanceChecks > 0 ? _totalBossDistance / _bossDistanceChecks : 0f;
        float bossDistancePenalty = (_timeInLevel1Distance * _penaltyCloseToBossLevel1) + (_timeInLevel2Distance * _penaltyCloseToBossLevel2);
        int totalDistanceChecks = _timeInSafeDistance + _timeInLevel1Distance + _timeInLevel2Distance;
        
        // Platform efficiency
        float platformEfficiency = _totalFallThroughsAttempted > 0 ? 
            (float)_successfulFallThroughs / _totalFallThroughsAttempted * 100f : 0f;
        float platformPenalty = (_totalFallThroughsAttempted - _successfulFallThroughs) * _penaltyUselessFallThrough;
        
        // Attack efficiency
        float attackEfficiency = _totalAttacksAttempted > 0 ? 
            (float)_successfulAttacks / _totalAttacksAttempted * 100f : 0f;
        float attackPenalty = (_totalAttacksAttempted - _successfulAttacks) * _penaltyAttackMissed;
        
        // Boss attack avoidance
        float bossAttackAvoidance = _totalBossAttacks > 0 ? 
            (float)_bossAttacksAvoided / _totalBossAttacks * 100f : 0f;
        float bossAttackReward = _bossAttacksAvoided * _rewardAvoidAttack;
        
        string bossModeInfo = EpisodeManager.Instance != null ? 
            $"Boss Mode: {EpisodeManager.Instance.CurrentBossMode}" : "Boss Mode: Unknown";
        
        return $"[PlayerAI] Enhanced Statistics:" +
               $"\n  Episode Length: {currentDuration:F2}s (Penalty: {episodeLengthPenalty:F3})" +
               $"\n  Movement Efficiency: {movementEfficiency:F1}% ({_successfulMovements}/{_totalMovementsAttempted}) (Penalty: {movementPenalty:F1})" +
               $"\n  Direction Change Efficiency: {directionChangeEfficiency:F1}% ({_successfulDirectionChanges}/{_totalDirectionChangesAttempted}) (Penalty: {directionChangePenalty:F1})" +
               $"\n  Boss Distance: Safe:{_timeInSafeDistance} Level1:{_timeInLevel1Distance} Level2:{_timeInLevel2Distance} (Penalty: {bossDistancePenalty:F3})" +
               $"\n  Platform Efficiency: {platformEfficiency:F1}% ({_successfulFallThroughs}/{_totalFallThroughsAttempted}) (Penalty: {platformPenalty:F1})" +
               $"\n  Attack Efficiency: {attackEfficiency:F1}% ({_successfulAttacks}/{_totalAttacksAttempted}) (Penalty: {attackPenalty:F1})" +
               $"\n  Boss Attack Avoidance: {bossAttackAvoidance:F1}% ({_bossAttacksAvoided}/{_totalBossAttacks}) (Reward: {bossAttackReward:F1})" +
               $"\n  Total Rewards: {_totalEpisodeRewards:F2}" +
               $"\n  Total Penalties: {_totalEpisodePenalties:F2}" +
               $"\n  {bossModeInfo}";
    }
    
    /// <summary>
    /// Gets detailed direction change statistics for the current episode.
    /// 
    /// This method provides specific information about direction change behavior,
    /// useful for analyzing AI movement patterns and learning effectiveness.
    /// </summary>
    /// <returns>A string containing direction change statistics.</returns>
    public string GetDirectionChangeStatistics()
    {
        float directionChangeEfficiency = _episodeDirectionChanges > 0 ? 
            (float)(_episodeDirectionChanges - _episodeBlockedDirectionChanges) / _episodeDirectionChanges : 1f;
        
        float directionChangesPerMinute = _episodeDuration > 0 ? 
            (_episodeDirectionChanges * 60f) / _episodeDuration : 0f;
        
        string cooldownStatus = _isDirectionChangeBlocked ? 
            $"Blocked ({_cooldownRemainingTime:F2}s remaining)" : "Available";
        
        return $"[PlayerAI] Direction Change Statistics:" +
               $"\n  Total Direction Changes: {_episodeDirectionChanges}" +
               $"\n  Blocked Direction Changes: {_episodeBlockedDirectionChanges}" +
               $"\n  Direction Change Efficiency: {directionChangeEfficiency:P1}" +
               $"\n  Direction Changes per Minute: {directionChangesPerMinute:F1}" +
               $"\n  Current Cooldown Status: {cooldownStatus}" +
               $"\n  Cooldown Duration: {directionChangeCooldown:F2}s" +
               $"\n  Penalty for Rapid Changes: {(penalizeRapidDirectionChanges ? "Enabled" : "Disabled")}" +
               $"\n  Cooldown in Observations: {(includeCooldownInObservations ? "Enabled" : "Disabled")}";
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
