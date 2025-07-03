using System; // Needed for System.DateTime
using System.Collections.Generic;
using System.IO;
using System.Linq; // Needed for Linq methods

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Manages the start and end of training episodes for the Boss vs Player fight.
/// Handles resets, logging, and reward management for ML-Agents training.
/// </summary>
public class EpisodeManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultInitialPlayerX = -10f;
    private const float DefaultInitialPlayerY = -10f;
    private const float DefaultInitialBossX = 10f;
    private const float DefaultInitialBossY = -9f;
    private const float DefaultMaxEpisodeDuration = 30f;
    private const int DefaultLogFrequency = 10;
    private const float TimeoutPenalty = -30f;
    private const string DefaultLogFileName = "BossTrainingLog.txt";
    private const string EpisodeCountKey = "BossEpisodeCount";

    // ==================== Singleton ====================
    /// <summary>
    /// The global instance of the EpisodeManager.
    /// </summary>
    public static EpisodeManager Instance { get; private set; }

    // ==================== Timeout Settings ====================
    [Header("Episode Timeout")]
    [Tooltip("Maximum duration (in seconds) for an episode before timeout is triggered.")]
    [SerializeField] private float _maxEpisodeDuration = DefaultMaxEpisodeDuration;

    // ==================== Scene References ====================
    [Header("Scene References")]
    [Tooltip("Reference to the player GameObject.")]
    [FormerlySerializedAs("_playerObject")]
    [SerializeField] private GameObject _playerObject;
    [Tooltip("Reference to the boss GameObject.")]
    [SerializeField] private GameObject _bossObject;

    [Tooltip("Reference to the AIBoss component.")]
    [SerializeField] private AIBoss _aiBoss;
    [Tooltip("Reference to the BossEnemy component.")]
    [SerializeField] private BossEnemy _boss;
    [Tooltip("Reference to the BossHealth component.")]
    [SerializeField] private BossHealth _bossHealth;
    [Tooltip("Reference to the player Health component.")]
    [SerializeField] private Health _playerHealth;
    [Tooltip("Reference to the PlayerMovement component.")]
    [SerializeField] private PlayerMovement _playerMovement;
    [Tooltip("Reference to the PlayerAttack component.")]
    [SerializeField] private PlayerAttack _playerAttack;
    [Tooltip("Reference to the BossRewardManager component.")]
    [SerializeField] private BossRewardManager _bossRewardManager;
    [Tooltip("Reference to the BossQLearning component (optional for ML-Agents Player training).")]
    [SerializeField] private BossQLearning _bossQLearning;

    // ==================== Episode Start Position ====================
    [Header("Episode Reset Positions")]
    [Tooltip("Initial position for the player at the start of an episode.")]
    [FormerlySerializedAs("initialPlayerPosition")]
    [SerializeField] private Vector3 _initialPlayerPosition = new Vector3(DefaultInitialPlayerX, DefaultInitialPlayerY, 0f);
    [Tooltip("Initial position for the boss at the start of an episode.")]
    [SerializeField] private Vector3 _initialBossPosition = new Vector3(DefaultInitialBossX, DefaultInitialBossY, 0f);

    /// <summary>
    /// Gets the initial player position for episode resets.
    /// </summary>
    public Vector3 InitialPlayerPosition => _initialPlayerPosition;
    /// <summary>
    /// Gets the initial boss position for episode resets.
    /// </summary>
    public Vector3 InitialBossPosition => _initialBossPosition;

    // ==================== Logging Parameters ====================
    [Header("Logging Settings")]
    [Tooltip("File path for episode log output.")]
    [SerializeField] private string _logFilePath = DefaultLogFileName;
    [Tooltip("How often (in episodes) to log statistics.")]
    [SerializeField] private int _logFrequency = DefaultLogFrequency;

    // ==================== Internal State ====================
    [Tooltip("Total number of episodes completed.")]
    [SerializeField, HideInInspector] private int _episodeCount = 0; // Hide in Inspector, but serialize for persistence
    [Tooltip("Average reward over recent episodes.")]
    [SerializeField, HideInInspector] private float _averageReward;
    [Tooltip("Accumulated reward since last log.")]
    [SerializeField, HideInInspector] private float _accumulatedReward = 0f;
    private int _episodesSinceLastLog = 0;

    private float _episodeStartTime;
    private bool _timeoutTriggered = false;
    [FormerlySerializedAs("_episodeCountKey")]
    private string _episodeCountKey = EpisodeCountKey;

    /// <summary>
    /// Gets the total number of episodes completed.
    /// </summary>
    public int EpisodeCount => _episodeCount;
    /// <summary>
    /// Gets the average reward over recent episodes.
    /// </summary>
    public float AverageReward => _averageReward;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Awake callback. Sets up the singleton, validates references, and loads persistent data.
    /// </summary>
    private void Awake()
    {
        //Time.timeScale = 2f;
        //Application.targetFrameRate = -1; // No frame limit

        // --- Singleton Setup ---
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate EpisodeManager found. Destroying this one.");
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if manager needs to persist across scene loads
        }

        // --- Validate References ---
        ValidateReferences();

        // Ensure log file path is persistent
        _logFilePath = Path.Combine(Application.persistentDataPath, _logFilePath);
        Debug.Log($"[EpisodeManager] Training log file path set to: {_logFilePath}");

        // Load episode count from PlayerPrefs
        _episodeCount = PlayerPrefs.GetInt(_episodeCountKey, 0);
        Debug.Log($"[EpisodeManager] Loaded total episode count: {_episodeCount}");

        // Optional: Clear log file on first run or if you want fresh logs each time
        // if (episodeCount == 0 && File.Exists(_logFilePath))
        // {
        //     try { File.Delete(_logFilePath); Debug.Log("[EpisodeManager] Cleared previous log file."); }
        //     catch (Exception ex) { Debug.LogError($"[EpisodeManager] Failed to clear log file: {ex.Message}"); }
        // }
    }

    /// <summary>
    /// Unity Update callback. Checks for episode timeout each frame.
    /// </summary>
    private void Update()
    {
        // Only run the timeout check if an episode is in progress:
        if (!_timeoutTriggered && Time.time - _episodeStartTime >= _maxEpisodeDuration)
        {
            _timeoutTriggered = true;
            HandleEpisodeTimeout();
        }
    }

    /// <summary>
    /// Handles logic when an episode times out (e.g., applies penalty, ends episode).
    /// </summary>
    private void HandleEpisodeTimeout()
    {
        Debug.LogWarning("[EpisodeManager] Episode timed out! Applying penalty and ending episode.");

        // 1. Apply your timeout penalty (e.g. −1 reward)
        //if (_bossRewardManager != null)
        //{
        //    _bossRewardManager.AddCustomReward(TimeoutPenalty);
        //}

        // 2. Tell your ML-Agents agent to end the episode
        //    Assuming your PlayerAI component is on _playerObject:
        var agent = _playerObject.GetComponent<PlayerAI>();
        if (agent != null)
        {
            RecordEndOfEpisode(bossWon: false);
            agent.EndEpisode();
        }
        else
        {
            Debug.LogError("[EpisodeManager] Could not find PlayerAI to EndEpisode on timeout!");
        }
    }

    /// <summary>
    /// Unity callback when the application quits. Saves episode count to PlayerPrefs.
    /// </summary>
    private void OnApplicationQuit()
    {
        // Save episode count on quit
        PlayerPrefs.SetInt(_episodeCountKey, _episodeCount);
        PlayerPrefs.Save(); // Ensure it's written to disk
        Debug.Log($"[EpisodeManager] Saved total episode count: {_episodeCount}");
    }

    /// <summary>
    /// Unity callback when the object is disabled. Saves episode count if this is the active instance.
    /// </summary>
    private void OnDisable()
    {
        // Avoid saving if called during application quit or from duplicates
        if (Instance == this)
        {
            PlayerPrefs.SetInt(_episodeCountKey, _episodeCount);
            PlayerPrefs.Save();
            Debug.Log($"[EpisodeManager] Saved total episode count on disable: {_episodeCount}");
        }
    }

    /// <summary>
    /// Unity Start callback. Validates critical references before starting.
    /// </summary>
    private void Start()
    {
        // Validate critical references one last time before starting
        if (_playerObject == null || _bossObject == null || _bossRewardManager == null) // QL is optional for ML-Agents Player training
        {
            Debug.LogError("[EpisodeManager] Critical references missing for ML-Agents training. Disabling EpisodeManager.");

            this.enabled = false;
            return;
        }

        // When using ML-Agents, the PlayerAI.OnEpisodeBegin is the primary trigger
        // for resetting the environment. We do NOT call StartEpisode() here
        // as it would cause a double reset or conflict with ML-Agents' cycle.
        // The first episode reset will be triggered by ML-Agents calling PlayerAI.OnEpisodeBegin.
    }

    // Removed Update method (no longer managing episode timer here)
    // Removed EndEpisodeAndRestartScene method (no longer managing QL cycle here)


    // --- Method for ML-Agents Player to trigger logging/QL reset WITHOUT restarting scene ---
    /// <summary>
    /// Records the outcome of an episode, handles logging (primarily for QL stats),
    /// and resets Q-Learning state. Intended to be called by PlayerAI when an episode ends.
    /// Does NOT reset the scene environment itself (that's handled by PlayerAI.OnEpisodeBegin
    /// calling ResetEnvironmentForNewEpisode).
    /// </summary>
    /// <param name="bossWon">Did the boss win this episode?</param>
    public void RecordEndOfEpisode(bool bossWon)
    {
        // This method is called by PlayerAI.HandlePlayerDamaged or PlayerAI.HandleBossDied
        // *after* PlayerAI.EndEpisode() has been called by ML-Agents.

        // 1. Report Outcome to Reward Manager (for terminal reward calculation for QL logging)
        // This assumes the reward manager is relevant to QL logging. Adjust if needed.
        if (_bossRewardManager != null)
        {
            if (bossWon) _bossRewardManager.ReportBossWin();
            else _bossRewardManager.ReportBossLoss();
        }
        else { Debug.LogError("[EpisodeManager] BossRewardManager reference missing when reporting win/loss!"); }

        // 2. Get Total Reward for the episode and Accumulate (for QL logging)
        float episodeTotalReward = 0f;
        if (_bossRewardManager != null)
        {
            // Get total reward and clear for next QL cycle's accumulation
            episodeTotalReward = _bossRewardManager.GetEpisodeTotalReward();
        }
        _accumulatedReward += episodeTotalReward;
        _episodesSinceLastLog++;

        // 3. Increment Total Episode Count
        _episodeCount++;
        // Debug.Log($"Episode {episodeCount} ended. Boss {(bossWon ? "WON" : "LOST")}. QL Reward Logged: {episodeTotalReward:F3}");

        // 4. Logging (Handles Q-Learning specific stats)
        if (_logFrequency > 0 && _episodesSinceLastLog >= _logFrequency)
        {
            _averageReward = (_episodesSinceLastLog > 0) ? _accumulatedReward / _episodesSinceLastLog : 0f;

            // Get state counts from the Q-Learning script (if assigned)
            int uniqueStates = _bossQLearning != null ? _bossQLearning.GetUniqueStateCount() : -1;
            int totalVisitedStates = _bossQLearning != null ? _bossQLearning.GetTotalStatesVisitedCount() : -1;
            int revisitedStateCount = _bossQLearning != null ? _bossQLearning.GetRevisitedStateCount() : -1;

            Dictionary<int, int> visitThresholds = _bossQLearning?.GetStateVisitThresholdCounts();

            string visitStats = visitThresholds != null
                ? $"  States visited >100: {visitThresholds[100]}, >200: {visitThresholds[200]}, >500: {visitThresholds[500]}, >1000: {visitThresholds[1000]}"
                : "  State visit thresholds not available.";


            string logMessage = $"--- Batch Summary (Episodes {_episodeCount - _episodesSinceLastLog + 1} to {_episodeCount}) ---" +
                                $"\n  Average QL Reward (last {_episodesSinceLastLog} episodes): {_averageReward:F3}" +
                                $"\n  Total Episodes Trained: {_episodeCount}" +
                                $"\n  Current Epsilon: " + (_bossQLearning != null ? _bossQLearning.Epsilon.ToString("F3") : "-1.000") +
                                $"\n  Unique States in Q-Table: {uniqueStates}" +
                                $"\n  Total Unique States Visited (All Time): {totalVisitedStates}" +
                                $"\n  States Visited More Than Once: {revisitedStateCount}" +
                                $"\n" + visitStats;

            Debug.Log($"[EpisodeManager QL Log]\n{logMessage}");
            LogToFile(logMessage); // Log to file

            // Reset accumulators after logging
            _accumulatedReward = 0f;
            _episodesSinceLastLog = 0;
        }

        // 5. Reset the Q-Learning agent's internal state for its *next* decision cycle
        // This is necessary if the QL boss is active, even if not currently learning.
        if (_bossQLearning != null)
        {
            _bossQLearning.OnEpisodeEnd(episodeTotalReward);
            _bossQLearning.ResetQLearningState();
            _bossQLearning.LogEpisode(_episodeCount, _averageReward, bossWon);
        }
        else
        {
            // Debug.LogWarning("[EpisodeManager] BossQLearning reference missing. Cannot reset QL state.");
        }

        // DO NOT call StartEpisode() here. The ML-Agents system will call PlayerAI.OnEpisodeBegin
        // which will then call ResetEnvironmentForNewEpisode.
    }


    // --- Method for ML-Agents Player to reset only Boss and Environment ---
    /// <summary>
    /// Resets only the Boss state and shared hazards/projectiles.
    /// Intended to be called by PlayerAI.OnEpisodeBegin.
    /// </summary>
    public void ResetEnvironmentForNewEpisode()
    {
        Debug.Log($"[EpisodeManager] ResetEnvironmentForNewEpisode called (likely by PlayerAI).");

        // --- Reset Reward Manager State FIRST ---
        // This ensures rewards start accumulating from zero for the new ML-Agents episode.
        if (_bossRewardManager != null)
        {
            _bossRewardManager.StartNewEpisode(); // Resets timers and pending rewards
        }
        else
        {
            Debug.LogError("[EpisodeManager] Cannot reset environment: BossRewardManager reference missing!");
            // Consider returning or disabling if critical
        }

        // --- Reset Boss ---
        ResetBossState();

        // --- Reset Shared Hazards ---
        ResetSharedHazards();

        // --- NEW: record the time this episode began ---
        _episodeStartTime = Time.time;
        // --- FIX: Reset the timeout trigger for the new episode ---
        _timeoutTriggered = false;
        Debug.Log($"[EpisodeManager] Episode timer started at {_episodeStartTime}");

        // DO NOT reset Player state here - PlayerAI.OnEpisodeBegin handles its own player reset.
        // DO NOT reset QL state here - RecordEndOfEpisode handles that.

        // Debug.Log($"[EpisodeManager] Environment setup complete for new ML-Agents episode.");
    }

    // --- Helper Methods for Resetting Specific Parts ---

    /// <summary>
    /// Resets the boss GameObject, its components, and health for a new episode.
    /// </summary>
    private void ResetBossState()
    {
        if (_bossObject != null)
        {
            Debug.Log("[EpisodeManager] Resetting Boss GameObject state.");
            _bossObject.transform.position = _initialBossPosition;
            Rigidbody2D bossRb = _bossObject.GetComponent<Rigidbody2D>();
            if (bossRb != null) bossRb.velocity = Vector2.zero;

            // Use the AIBoss script for its specific reset logic
            if (_aiBoss != null)
            {
                if (!_aiBoss.enabled) _aiBoss.enabled = true;
                _aiBoss.ResetAbilityStates(); // Assuming this exists
            }
            if (_boss != null)
            {
                if (!_boss.enabled) _boss.enabled = true;
                _boss.ResetState(); // Assuming this exists

            }

            if (_bossHealth != null) _bossHealth.ResetHealth(); // Reset health

            // Ensure collider/physics/visuals are re-enabled if disabled on death
            Collider2D bossCol = _bossObject.GetComponent<Collider2D>();
            if (bossCol != null && !bossCol.enabled) bossCol.enabled = true;
            if (bossRb != null && bossRb.isKinematic) bossRb.isKinematic = false;
            Renderer bossRenderer = _bossObject.GetComponent<Renderer>();
            if (bossRenderer != null) bossRenderer.enabled = true;

        }
        else { Debug.LogError("[EpisodeManager] Boss GameObject reference missing during reset!"); }
    }

    // Removed ResetPlayerState() - PlayerAI handles its own reset in OnEpisodeBegin

    /// <summary>
    /// Resets shared hazards in the environment (e.g., projectiles, traps).
    /// Extend this method based on your game's hazard logic.
    /// </summary>
    private void ResetSharedHazards()
    {
        // Example: Find and deactivate/destroy projectiles, reset traps, etc.
        // This needs to be implemented based on how your hazards work.

        // Assuming PlayerAI already clears boss fireballs in its own reset cycle
        // via the reference array, maybe we don't need extra logic here,
        // unless there are other shared environmental hazards.
        // Example:
        // foreach(var trap in FindObjectsOfType<EnvironmentTrap>()) { trap.ResetTrap(); }

        // AIBoss should deactivate its flames/warnings etc.
        if (_aiBoss != null)
        {
            _aiBoss.DeactivateFlameAndWarning(); // Ensure this covers all necessary boss hazards
        }
        if (_boss != null)
        {
            //_boss.DeactivateFlameAndWarning(); // Ensure this covers all necessary boss hazards
        }
    }


    // --- Logging Helper --- (Keep existing LogToFile method)
    /// <summary>
    /// Appends a message to the episode log file with a timestamp.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void LogToFile(string message)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(_logFilePath, true))
            {
                writer.WriteLine($"--- {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ---");
                writer.WriteLine(message);
                writer.WriteLine("--------------------");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EpisodeManager] Failed to write to log file '{_logFilePath}': {ex.Message}");
        }
    }

    // --- Reference Validation --- (Keep existing ValidateReferences method)
    /// <summary>
    /// Validates that all required scene references are assigned and logs warnings/errors if not.
    /// </summary>
    private void ValidateReferences()
    {
        // Use ?. operator for slightly cleaner checks where appropriate
        if (_playerObject == null) Debug.LogError("[EpisodeManager] Player GameObject not assigned!");
        if (_bossObject == null) Debug.LogError("[EpisodeManager] Boss GameObject not assigned!");

        // Optional/Warning level if component is missing but GO is assigned
        if (_aiBoss == null && _bossObject != null) Debug.LogWarning($"[EpisodeManager] AIBoss component not assigned on {_bossObject.name} (needed for reset).");
        if (_bossHealth == null && _bossObject != null) Debug.LogWarning($"[EpisodeManager] BossHealth component not assigned on {_bossObject.name} (needed for reset).");
        if (_playerHealth == null && _playerObject != null) Debug.LogWarning($"[EpisodeManager] Player Health component not assigned on {_playerObject.name} (needed for reset).");
        if (_playerMovement == null && _playerObject != null) Debug.LogWarning($"[EpisodeManager] PlayerMovement component not assigned on {_playerObject.name} (needed for reset).");
        if (_playerAttack == null && _playerObject != null) Debug.LogWarning($"[EpisodeManager] PlayerAttack component not assigned on {_playerObject.name} (needed for reset).");


        // Critical errors - RewardManager is essential for ML-Agents Player rewards
        if (_bossRewardManager == null) Debug.LogError("[EpisodeManager] BossRewardManager reference not assigned! Rewards/Learning will fail.");
        // QL is optional if only training ML-Agents Player
        if (_bossQLearning == null) Debug.LogWarning("[EpisodeManager] BossQLearning reference not assigned (optional for QL logging).");

    }

    /// <summary>
    /// Gets the total number of episodes completed (for use in other scripts).
    /// </summary>
    /// <returns>The current episode count.</returns>
    public int GetTotalEpisodeCount()
    {
        return _episodeCount; // Return current episode count
    }

}
