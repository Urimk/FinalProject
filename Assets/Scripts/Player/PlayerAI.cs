using System.Collections.Generic;
using System.Linq;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

using UnityEngine;

public class PlayerAI : Agent
{
    // Note: After these variable name changes, you may need to re-assign some references
    // in the Unity Inspector for the PlayerAI component.

    // ... (Keep existing fields: References, Rewards, Observations, State) ...
    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerAttack _playerAttack;
    [SerializeField] private Health _playerHealth;
    [SerializeField] private OneWayPlatform _oneWayPlatform;
    [SerializeField] private BossHealth _bossHealth;
    [SerializeField] private AIBoss _aiBoss;

    [SerializeField] private Transform _boss;
    [SerializeField] private Transform _flames;
    [SerializeField] private GameObject[] _bossFireballs;
    [SerializeField] private GameObject[] _playerFireballs;

    // Remove RayPerceptionSensor reference
    // [Header("Sensors")]
    // [SerializeField] private RayPerceptionSensor rayPerceptionSensor; // REMOVED


    // --- Manual Raycast Observation Settings ---
    [Header("Manual Raycast Perception")]
    [SerializeField] private float _raycastDistance = 10f; // Max distance for rays
    [SerializeField] private LayerMask _environmentLayerMask; // Layers to detect (Ground, Platform, Wall, etc.)

    // Define ray angles relative to the agent's forward direction (transform.right)
    // and one fixed world-down ray. Angles in degrees.
    private readonly float[] _rayAngles = {
        0f,     // Forward
     //   45f,    // Forward-Up
    //    90f,    // Up (relative to player's orientation)
       -45f,    // Forward-Down
      180f,    // Backward
     // -135f,   // Backward-Down (relative to facing)
       // Note: A world-down ray will be added separately
    };

    // Define relevant environment layers for observation
    // You NEED to set these layer numbers correctly in the Inspector!
    [Header("Environment Layers (for Observation)")]
    [SerializeField] private int _groundLayer = 3; // Example layer number
    [SerializeField] private int _platformLayer = 7; // Example layer number
    [SerializeField] private int _wallLayer = 6; // Example layer number

    private int _numRaycasts; // Calculated from _rayAngles + 1 (for world down)
    private int _numEnvironmentLayersToObserve; // Number of layers defined above

    // --- REMOVED: EpisodeManager reference is accessed via Singleton ---
    // [SerializeField] private EpisodeManager episodeManager; // Use Instance instead

    [Header("Reward Settings")]
    [SerializeField] private float _rewardWin = 1.0f;
    [SerializeField] private float _penaltyLose = -1.0f;
    [SerializeField] private float _rewardDamageBoss = 0.75f;
    [SerializeField] private float _penaltyTakeDamage = -3f;
    // Step penalty can be set in Behavior Parameters directly (Negative Reward)
    [SerializeField] private float _penaltyPerStep = -0.0001f;

    [Header("Observation Settings")]
    [SerializeField] private int _maxBossFireballsToObserve = 1;
    [Header("Physics Detection")]
    [SerializeField] private float _hazardDetectionRadius = 15f; // How far around the player to check for indicators/hazards
    [SerializeField] private LayerMask _hazardDetectionLayerMask = -1; // Set in inspector to layers containing hazards/indicators (-1 = Everything)
    private Collider2D[] _overlapResults = new Collider2D[10]; // Pre-allocate buffer for OverlapCircleNonAlloc


    // --- STATE ---
    private Rigidbody2D _rigidbody2D;
    private bool _isBossDefeated = false;
    private bool _isPlayerDead = false;
    private const float MaxJumpHoldDuration = 0.6f;


    // --- INITIALIZATION ---
    public override void Initialize()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();

        // Remove RayPerceptionSensor check
        // if (rayPerceptionSensor == null)
        // {
        //      rayPerceptionSensor = GetComponent<RayPerceptionSensor>();
        // }
        // if (rayPerceptionSensor == null)
        // {
        //       Debug.LogError("RayPerceptionSensor2D component not found on PlayerAI GameObject!", this);
        // }

        // Calculate raycast parameters
        _numRaycasts = _rayAngles.Length + 1; // +1 for the world-down ray
        // Count the number of environment layers we are explicitly observing
        // This assumes we list layer numbers we care about. Add more if needed.
        _numEnvironmentLayersToObserve = 3; // For Ground, Platform, Wall. Update if you add more.


        // --- Event Connections --- (Ensure these are set up in Health/BossHealth)
        if (_playerHealth != null) { _playerHealth.OnDamaged += HandlePlayerDamaged; }
        else Debug.LogError("Player Health component not found!", this);
        if (_bossHealth != null)
        {
            _bossHealth.OnBossDamaged += HandleBossDamaged;
            _bossHealth.OnBossDied += HandleBossDied;
        }
        else Debug.LogError("Boss Health component not found!", this);
        // --- End Event Connections ---
    }

    // --- EPISODE LIFECYCLE (ML-Agents Driven) ---
    public override void OnEpisodeBegin()
    {
        _isBossDefeated = false;
        _isPlayerDead = false;

        transform.position = EpisodeManager.Instance != null ? EpisodeManager.Instance.initialPlayerPosition : Vector3.zero;
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

        Debug.Log($"[PlayerAI] OnEpisodeBegin completed.");
    }

    private void ClearProjectiles(GameObject[] projectileArray)
    {
        if (projectileArray == null) return;
        foreach (var proj in projectileArray)
        {
            if (proj != null) proj.SetActive(false);
        }
    }

    // --- OBSERVATIONS ---
    public override void CollectObservations(VectorSensor sensor)
    {
        // --- Agent's Own State ---
        sensor.AddObservation((Vector2)transform.localPosition); // Now 2 floats
        sensor.AddObservation(_rigidbody2D.velocity);           // 2 floats (Vector2)
        sensor.AddObservation(_playerHealth != null ? _playerHealth.currentHealth / _playerHealth.startingHealth : 0f); // 1 float
        sensor.AddObservation(_playerMovement != null && _playerMovement.IsGrounded()); // 1 bool (as float)
        sensor.AddObservation(_playerHealth != null && _playerHealth.invulnerable); // 1 bool (as float)
        sensor.AddObservation(_playerMovement != null && _playerMovement.OnWall());     // 1 bool (as float)
        sensor.AddObservation(_playerMovement != null ? _playerMovement.GetFacingDirection() : 1f); // 1 float
        sensor.AddObservation(_playerAttack != null ? _playerAttack.IsAttackReady() : 1f);       // 1 float

        // --- Boss State ---
        bool bossActive = _boss != null && _boss.gameObject.activeInHierarchy;
        Vector2 relativeBossPos = Vector2.zero;
        Vector2 bossVelocity = Vector2.zero;
        float bossHealthNormalized = 0f;

        if (bossActive)
        {
            relativeBossPos = _boss.localPosition - transform.localPosition;
            Rigidbody2D bossRb = _boss.GetComponent<Rigidbody2D>();
            bossVelocity = bossRb != null ? bossRb.velocity : Vector2.zero;
            bossHealthNormalized = _bossHealth != null ? _bossHealth.currentHealth / _bossHealth.maxHealth : 0f;
        }
        sensor.AddObservation(relativeBossPos); // 2 floats
        sensor.AddObservation(bossVelocity);    // 2 floats
        sensor.AddObservation(bossHealthNormalized); // 1 float


        // --- Manual Raycast Observations ---
        Vector2 origin = transform.position;
        float facingDirection = _playerMovement != null ? _playerMovement.GetFacingDirection() : 1f; // 1 for right, -1 for left

        // Rays relative to facing direction
        foreach (float angle in _rayAngles)
        {
            // Calculate direction vector based on facing direction and angle
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            Vector2 direction = rotation * (Vector2)(facingDirection > 0 ? Vector2.right : Vector2.left); // Rotate facing direction

            DrawRay(origin, direction * _raycastDistance, Color.blue); // Optional: Draw rays in scene view

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, _raycastDistance, _environmentLayerMask);

            // Add observations for this ray
            //sensor.AddObservation(hit.collider != null); // Hit? (1 float)
            // sensor.AddObservation(hit.collider != null ? hit.distance / _raycastDistance : 1f); // Distance (normalized, 1 if no hit)

            // Add one-hot encoding for detected layers
            //sensor.AddObservation(hit.collider != null && hit.collider.gameObject.layer == _groundLayer); // Hit Ground? (1 float)
            //sensor.AddObservation(hit.collider != null && hit.collider.gameObject.layer == _platformLayer); // Hit Platform? (1 float)
            //sensor.AddObservation(hit.collider != null && hit.collider.gameObject.layer == _wallLayer); // Hit Wall? (1 float)
            // Add more layers here if needed, updating _numEnvironmentLayersToObserve

            sensor.AddObservation(hit.collider != null); // Hit? (1 float)
            sensor.AddObservation(hit.collider != null ? hit.distance / _raycastDistance : 1f); // Distance (normalized)
        }

        // --- Add a specific World Down ray ---
        Vector2 worldDownDirection = Vector2.down;
        DrawRay(origin, worldDownDirection * _raycastDistance, Color.cyan); // Optional: Draw ray

        RaycastHit2D worldDownHit = Physics2D.Raycast(origin, worldDownDirection, _raycastDistance, _environmentLayerMask);

        sensor.AddObservation(worldDownHit.collider != null); // Hit? (1 float)
        sensor.AddObservation(worldDownHit.collider != null ? worldDownHit.distance / _raycastDistance : 1f); // Distance (normalized, 1 if no hit)

        sensor.AddObservation(worldDownHit.collider != null && worldDownHit.collider.gameObject.layer == _groundLayer); // Hit Ground? (1 float)
        sensor.AddObservation(worldDownHit.collider != null && worldDownHit.collider.gameObject.layer == _platformLayer); // Hit Platform? (1 float)
        sensor.AddObservation(worldDownHit.collider != null && worldDownHit.collider.gameObject.layer == _wallLayer); // Hit Wall? (1 float)
                                                                                                                      // Add more layers here if needed


        // --- Physics-Based Hazard/Indicator Detection (Keep this) ---
        // Use OverlapCircleNonAlloc for efficiency - finds colliders within radius
        // This is for specific, localized hazards/indicators, distinct from general environment rays

        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, _hazardDetectionRadius, _overlapResults, _hazardDetectionLayerMask);

        // --- Dash Target Indicator Observation ---
        bool dashIndicatorFound = false;
        Vector2 relativeDashIndicatorPos = Vector2.zero;
        float closestDashIndicatorDist = float.MaxValue;

        // --- Flame Warning Marker Observation ---
        bool flameMarkerFound = false;
        Vector2 relativeFlameMarkerPos = Vector2.zero;
        float closestFlameMarkerDist = float.MaxValue;

        // --- Active Flame Hazard Observation (using the '_flames' Transform) ---
        bool isFlameHazardActive = _flames != null && _flames.gameObject.activeInHierarchy;
        Vector2 relativeFlamePosObs = Vector2.zero;

        if (isFlameHazardActive)
        {
            relativeFlamePosObs = (Vector2)_flames.position - (Vector2)transform.position;
            // You might also want to check if it's within a certain range if it's not always relevant
        }
        sensor.AddObservation(isFlameHazardActive);     // 1 float (is it currently active?)
        sensor.AddObservation(relativeFlamePosObs);     // 2 floats (its relative position)
        float closestActiveFlameDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _overlapResults[i];
            if (hit == null) continue;
            float dist = Vector2.Distance(transform.position, hit.transform.position);

            if (hit.CompareTag("AreaMarker"))
            {
                if (dist < closestDashIndicatorDist)
                {
                    dashIndicatorFound = true;
                    relativeDashIndicatorPos = (Vector2)hit.transform.position - (Vector2)transform.position;
                    closestDashIndicatorDist = dist;
                }
            }
            else if (hit.CompareTag("FlameWarningMarker"))
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


        // --- Closest Boss Fireballs --- (Keep existing logic)
        ObserveClosestProjectiles(sensor, _bossFireballs, _maxBossFireballsToObserve);

        // --- Manual Raycast Observations Explanation ---
        // We've added the raycast observations above within this method.
        // Each ray contributes 1 (hit?) + 1 (normalized distance) + _numEnvironmentLayersToObserve (one-hot layer flags)
        // Total raycast observations: _numRaycasts * (2 + _numEnvironmentLayersToObserve)
    }

    // Optional: Helper to draw rays in the Scene view for debugging
    private void DrawRay(Vector3 start, Vector3 dir, Color color)
    {
        Debug.DrawRay(start, dir, color);
    }

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
            Debug.LogWarning("[PlayerAI] Projectiles array is null in ObserveClosestProjectiles.");
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


    // --- ACTIONS ---
    public override void OnActionReceived(ActionBuffers actions)
    {

        AddReward(_penaltyPerStep);
        int moveAction = actions.DiscreteActions[0];
        float rawJumpAction = actions.ContinuousActions[0];
        int fallThroughAction = actions.DiscreteActions[1];
        int attackAction = actions.DiscreteActions[2];

        float moveDirection = (moveAction == 1) ? -1f : (moveAction == 2) ? 1f : 0f;
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
    }

    // --- REWARDS & ENDING EPISODE ---
    public void HandlePlayerDamaged(float damage)
    {
        if (_isPlayerDead || _isBossDefeated) return;
        AddReward(_penaltyTakeDamage);

        if (_playerHealth != null && _playerHealth.currentHealth <= 0)
        {
            _isPlayerDead = true;
            Debug.Log("[PlayerAI] Player Died! Ending Episode.");
            AddReward(_penaltyLose);
            EpisodeManager.Instance?.RecordEndOfEpisode(bossWon: true);
            EndEpisode();
        }
    }

    private void HandleBossDamaged(float damage)
    {
        if (_isPlayerDead || _isBossDefeated) return;
        AddReward(_rewardDamageBoss);
    }

    private void HandleBossDied()
    {
        if (_isPlayerDead || _isBossDefeated) return;
        _isBossDefeated = true;
        Debug.Log("[PlayerAI] Boss Defeated! Ending Episode.");
        AddReward(_rewardWin);
        EpisodeManager.Instance?.RecordEndOfEpisode(bossWon: false);
        EndEpisode();
    }

    // --- HEURISTICS ---
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

    // --- CLEANUP ---
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
