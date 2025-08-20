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
    
    // Reward Constants
    private const float DefaultRewardWin = 1.0f;
    private const float DefaultPenaltyLose = -1.0f;
    private const float DefaultRewardDamageBoss = 0.75f;
    private const float DefaultPenaltyTakeDamage = -3f;
    private const float DefaultPenaltyPerStep = -0.0001f;
    private const float DefaultRewardSurvival = 0.001f;
    private const float DefaultPenaltyCloseToBoss = -0.005f; // Changed from reward to penalty
    private const float DefaultPenaltyStuck = -0.01f;
    private const float DefaultRewardSafeDistance = 0.002f; // New reward for maintaining safe distance
    
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
    private const float PlatformDetectionRadius = 2f;
    private const float PlatformFallThroughCooldown = 0.5f;
    private const float PlatformUsageReward = 0.01f;
    private const float PlatformFallThroughPenalty = -0.005f;

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
    
    [Header("Enhanced Reward Settings")]
    [Tooltip("Small reward for surviving each step.")]
    [SerializeField] private float _rewardSurvival = DefaultRewardSurvival;
    [Tooltip("Penalty for being too close to the boss (dangerous).")]
    [SerializeField] private float _penaltyCloseToBoss = DefaultPenaltyCloseToBoss;
    [Tooltip("Reward for maintaining safe distance from boss.")]
    [SerializeField] private float _rewardSafeDistance = DefaultRewardSafeDistance;
    [Tooltip("Penalty for being stuck in the same position.")]
    [SerializeField] private float _penaltyStuck = DefaultPenaltyStuck;
    [Tooltip("Whether to enable enhanced reward system.")]
    [SerializeField] private bool _enableEnhancedRewards = true;

    [Header("Observation Settings")]
    [Tooltip("Maximum number of boss fireballs to observe.")]
    [FormerlySerializedAs("maxBossFireballsToObserve")]
    [SerializeField] private int _maxBossFireballsToObserve = DefaultMaxBossFireballsToObserve;
    
    [Header("Enhanced Observation Settings")]
    [Tooltip("Whether to include enhanced observations for better AI learning.")]
    [SerializeField] private bool _enableEnhancedObservations = true;
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
    
    // ==================== Enhanced Tracking ====================
    private Vector2 _lastPosition = Vector2.zero;
    private float _lastDistanceToBoss = 0f;
    private int _stepsWithoutMovement = 0;
    private const int MaxStepsWithoutMovement = 60; // 1 second at 60fps
    private float _episodeStartDistanceToBoss = 0f;
    private bool _hasMovedThisEpisode = false;
    private const float SafeDistanceFromBoss = 3f; // Safe distance to maintain from boss
    
    // ==================== Platform Interaction Tracking ====================
    private OneWayPlatform _currentPlatform = null;
    private float _lastFallThroughTime = 0f;
    private bool _canFallThrough = true;
    private int _platformUsageCount = 0;
    private float _lastPlatformRewardTime = 0f;
    
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
            Debug.Log($"[PlayerAI] Platform references validated successfully. Found {validPlatforms} valid platforms.");
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
        
        // Reset enhanced tracking
        _lastPosition = Vector2.zero;
        _lastDistanceToBoss = 0f;
        _stepsWithoutMovement = 0;
        _episodeStartDistanceToBoss = 0f;
        _hasMovedThisEpisode = false;
        
        // Reset platform interaction tracking
        _currentPlatform = null;
        _lastFallThroughTime = 0f;
        _canFallThrough = true;
        _platformUsageCount = 0;
        _lastPlatformRewardTime = 0f;
        
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
        
        // Enhanced observations (if enabled)
        if (_enableEnhancedObservations)
        {
            AddEnhancedObservations(sensor);
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
            
            // Initialize episode start distance if not set
            if (_episodeStartDistanceToBoss == 0f)
            {
                _episodeStartDistanceToBoss = distanceToBoss;
            }
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
        
        // Stuck detection
        bool isStuck = movementDelta.magnitude < 0.01f;
        sensor.AddObservation(isStuck ? 1f : 0f);
        
        // Update stuck tracking
        if (isStuck)
        {
            _stepsWithoutMovement++;
        }
        else
        {
            _stepsWithoutMovement = 0;
            _hasMovedThisEpisode = true;
        }
        
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
        
        // Can fall through (cooldown check)
        sensor.AddObservation(_canFallThrough ? 1f : 0f);
        
        // Fall through cooldown remaining (normalized)
        float cooldownRemaining = Mathf.Max(0f, PlatformFallThroughCooldown - (Time.time - _lastFallThroughTime));
        sensor.AddObservation(cooldownRemaining / PlatformFallThroughCooldown);
        
        // Platform usage count (normalized)
        sensor.AddObservation(Mathf.Clamp01(_platformUsageCount / 10f)); // Normalize to 0-1 range
        
        // Distance to nearest platform
        float nearestPlatformDistance = FindNearestPlatformDistance(transform.position);
        sensor.AddObservation(nearestPlatformDistance / DistanceNormalizationFactor);
        
        // Platform strategic value (distance to boss from platform)
        if (_boss != null && _boss.gameObject.activeInHierarchy)
        {
            float platformStrategicValue = CalculatePlatformStrategicValue();
            sensor.AddObservation(platformStrategicValue / DistanceNormalizationFactor);
        }
        else
        {
            sensor.AddObservation(1f); // Default value when boss is not available
        }
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
            
            float distance = Vector2.Distance(playerPosition, platform.transform.position);
            if (distance <= PlatformDetectionRadius)
            {
                _currentPlatform = platform;
                break;
            }
        }
    }
    
    /// <summary>
    /// Calculates the strategic value of the current platform position relative to the boss.
    /// </summary>
    /// <returns>Distance from platform to boss, or max distance if no platform.</returns>
    private float CalculatePlatformStrategicValue()
    {
        if (_currentPlatform == null || _boss == null) return DistanceNormalizationFactor;
        
        return Vector2.Distance(_currentPlatform.transform.position, _boss.position);
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
        // Apply base penalty per step to encourage efficient behavior
        AddReward(_penaltyPerStep);
        
        // Apply enhanced rewards if enabled
        if (_enableEnhancedRewards)
        {
            ApplyEnhancedRewards();
        }
        
        // Extract actions from ML-Agents
        int moveAction = actions.DiscreteActions[0];
        float rawJumpAction = actions.ContinuousActions[0];
        int fallThroughAction = actions.DiscreteActions[1];
        Debug.Log("fallThroughAction: " + fallThroughAction);
        int attackAction = actions.DiscreteActions[2];
        
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
        
        // Apply cooldown logic
        if (!directionChanged || !_isDirectionChangeBlocked)
        {
            if (directionChanged)
            {
                _lastDirectionChangeTime = Time.time;
                _lastMoveDirection = requestedDirection;
                _episodeDirectionChanges++;
                
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
    /// Applies enhanced rewards to encourage better AI behavior and learning.
    /// 
    /// This method provides additional reward signals beyond the basic step penalty,
    /// including survival rewards, distance-based rewards, and stuck penalties.
    /// </summary>
    private void ApplyEnhancedRewards()
    {
        // Survival reward - small positive reward for staying alive
        AddReward(_rewardSurvival);
        
        // Boss proximity penalty - discourage being too close to the boss (dangerous)
        if (_boss != null && _boss.gameObject.activeInHierarchy)
        {
            float currentDistanceToBoss = Vector2.Distance(transform.position, _boss.position);
            
            if (currentDistanceToBoss < SafeDistanceFromBoss)
            {
                // Penalty for being too close to the boss
                float proximityPenalty = _penaltyCloseToBoss * (SafeDistanceFromBoss - currentDistanceToBoss) / SafeDistanceFromBoss;
                AddReward(proximityPenalty);
            }
            else
            {
                // Reward for maintaining safe distance
                AddReward(_rewardSafeDistance);
            }
        }
        
        // Stuck penalty - discourage staying in the same position
        if (_stepsWithoutMovement > MaxStepsWithoutMovement)
        {
            AddReward(_penaltyStuck);
        }
        
        // Movement encouragement - small reward for moving
        if (_hasMovedThisEpisode && _lastPosition != Vector2.zero)
        {
            Vector2 movementDelta = (Vector2)transform.position - _lastPosition;
            if (movementDelta.magnitude > 0.01f)
            {
                AddReward(_rewardSurvival * 0.5f); // Small bonus for movement
            }
        }
        
        // Platform usage reward - encourage strategic platform usage
        if (_currentPlatform != null && Time.time - _lastPlatformRewardTime > 1f)
        {
            // Reward for being on a platform (strategic positioning)
            AddReward(PlatformUsageReward);
            _lastPlatformRewardTime = Time.time;
        }
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
        
        // Apply platform fall-through action
        HandlePlatformFallThrough(fallThrough);
        
        // Apply attack action
        _playerAttack?.SetAIAttack(attack);
        
        // Debug logging (can be removed in production)
        if (Debug.isDebugBuild && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[PlayerAI] Actions applied - Move: {moveDirection}, Jump: {jumpDuration:F2}s, Attack: {attack}");
                }
    }
    
    /// <summary>
    /// Handles platform fall-through logic with cooldown and strategic considerations.
    /// </summary>
    /// <param name="shouldFallThrough">Whether the AI wants to fall through the platform.</param>
    private void HandlePlatformFallThrough(bool shouldFallThrough)
    {
        Debug.Log("HandlePlatformFallThrough");
        // Update cooldown state
        _canFallThrough = (Time.time - _lastFallThroughTime) >= PlatformFallThroughCooldown;
        
        // Check if we can and should fall through
        if (shouldFallThrough && _canFallThrough && _currentPlatform != null)
        {
            // Apply fall-through
            _currentPlatform.SetAIFallThrough(true);
            
            // Update tracking
            _lastFallThroughTime = Time.time;
            _canFallThrough = false;
            _platformUsageCount++;
            
            // Apply penalty for fall-through (encourages strategic usage)
            AddReward(PlatformFallThroughPenalty);
            
            Debug.Log($"[PlayerAI] Fall-through executed on platform. Usage count: {_platformUsageCount}");
        }
        else if (shouldFallThrough && !_canFallThrough)
        {
            // Penalty for trying to fall through during cooldown
            AddReward(PlatformFallThroughPenalty * 0.5f);
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
        
        // Check if player died
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0)
        {
            _isPlayerDead = true;
            _episodeEnded = true;
            
            // Calculate episode duration
            _episodeDuration = Time.time - _episodeStartTime;
            
            // Log episode outcome
            LogEpisodeOutcome("Player Death", damage);
            
            // Apply death penalty and end episode
            AddReward(_penaltyLose);
            
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
    private void LogEpisodeOutcome(string outcome, float finalDamage)
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
                           $"\n  Direction Changes: {_episodeDirectionChanges} (Blocked: {_episodeBlockedDirectionChanges})" +
                           $"\n  Direction Change Efficiency: {directionChangeEfficiency:P1}" +
                           $"\n  Platform Usage: {_platformUsageCount}" +
                           $"\n  Final Damage: {finalDamage}" +
                           $"\n  {bossModeInfo}";
        
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
        LogEpisodeOutcome("Timeout", 0f);
        
        // Apply timeout penalty
        AddReward(_penaltyLose);
        
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
               $"\n  Platform Usage: {_platformUsageCount}" +
               $"\n  Current Platform: {(_currentPlatform != null ? "On Platform" : "Not on Platform")}" +
               $"\n  Can Fall Through: {_canFallThrough}" +
               $"\n  Cooldown Status: {cooldownStatus}" +
               $"\n  Enhanced Rewards: {(_enableEnhancedRewards ? "Enabled" : "Disabled")}" +
               $"\n  Enhanced Observations: {(_enableEnhancedObservations ? "Enabled" : "Disabled")}" +
               $"\n  Episode Ended: {_episodeEnded}" +
               $"\n  {bossModeInfo}";
    }
    
    /// <summary>
    /// Gets enhanced statistics for the current episode including movement and learning metrics.
    /// 
    /// This method provides comprehensive information about AI behavior and learning effectiveness,
    /// including movement patterns, reward accumulation, and observation utilization.
    /// </summary>
    /// <returns>A string containing enhanced episode statistics.</returns>
    public string GetEnhancedStatistics()
    {
        float currentDuration = Time.time - _episodeStartTime;
        float movementEfficiency = currentDuration > 0 ? (_episodeDirectionChanges * 60f) / currentDuration : 0f;
        float stuckPercentage = currentDuration > 0 ? (_stepsWithoutMovement / 60f) / currentDuration * 100f : 0f;
        
        string bossModeInfo = EpisodeManager.Instance != null ? 
            $"Boss Mode: {EpisodeManager.Instance.CurrentBossMode}" : "Boss Mode: Unknown";
        
        return $"[PlayerAI] Enhanced Statistics:" +
               $"\n  Episode Duration: {currentDuration:F2}s" +
               $"\n  Movement Efficiency: {movementEfficiency:F1} changes/minute" +
               $"\n  Stuck Time: {stuckPercentage:F1}%" +
               $"\n  Has Moved: {_hasMovedThisEpisode}" +
               $"\n  Distance to Boss: {_lastDistanceToBoss:F2}" +
               $"\n  Safe Distance Maintained: {(_lastDistanceToBoss >= SafeDistanceFromBoss ? "Yes" : "No")}" +
               $"\n  Platform Usage Count: {_platformUsageCount}" +
               $"\n  Current Platform: {(_currentPlatform != null ? "On Platform" : "Not on Platform")}" +
               $"\n  Can Fall Through: {_canFallThrough}" +
               $"\n  Enhanced Rewards: {(_enableEnhancedRewards ? "Enabled" : "Disabled")}" +
               $"\n  Enhanced Observations: {(_enableEnhancedObservations ? "Enabled" : "Disabled")}" +
               $"\n  Distance Observations: {(_includeDistanceObservations ? "Enabled" : "Disabled")}" +
               $"\n  Movement Pattern Observations: {(_includeMovementPatternObservations ? "Enabled" : "Disabled")}" +
               $"\n  Environmental Observations: {(_includeEnvironmentalObservations ? "Enabled" : "Disabled")}" +
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
