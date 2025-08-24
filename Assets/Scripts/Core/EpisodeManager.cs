using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Manages training episodes for the Boss vs Player fight.
/// Handles resets, logging, and reward management for ML-Agents training.
/// Supports both Q-learning boss and auto boss modes.
/// </summary>
public class EpisodeManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultMaxEpisodeDuration = 30f;
    private const int DefaultLogFrequency = 10;
    private const float TimeoutPenalty = -30f;
    private const string DefaultLogFileName = "BossTrainingLog.txt";
    private const string EpisodeCountKey = "BossEpisodeCount";
    private const float DefaultInitialPlayerX = -5f;
    private const float DefaultInitialPlayerY = -10f;
    private const float DefaultInitialBossX = 5f;
    private const float DefaultInitialBossY = -9f;
    private const string LogFileHeader = "=== Boss Training Episode Log ===\n" +
                                       "This file contains episode statistics and training progress.\n" +
                                       "Format: Episode Batch Summary with Q-Learning statistics.\n" +
                                       "==========================================\n\n";

    // ==================== Boss Mode Enum ====================
    /// <summary>
    /// Different modes the boss can operate in.
    /// </summary>
    public enum BossMode
    {
        /// <summary>Boss uses Q-learning for AI behavior.</summary>
        QLearning,
        /// <summary>Boss uses simple auto behavior (no learning).</summary>
        Auto,
        /// <summary>Boss is disabled or not present.</summary>
        None
    }

    // ==================== Singleton ====================
    /// <summary>
    /// The global instance of the EpisodeManager.
    /// </summary>
    public static EpisodeManager Instance { get; private set; }

    // ==================== Inspector Fields ====================
    [Header("Episode Settings")]
    [Tooltip("Maximum duration (in seconds) for an episode before timeout.")]
    [SerializeField] private float _maxEpisodeDuration = DefaultMaxEpisodeDuration;
    [Tooltip("How often (in episodes) to log statistics.")]
    [SerializeField] private int _logFrequency = DefaultLogFrequency;

    [Header("Boss Configuration")]
    [Tooltip("The mode the boss should operate in.")]
    [SerializeField] private BossMode _bossMode = BossMode.Auto;
    [Tooltip("Reference to the boss GameObject.")]
    [SerializeField] private GameObject _bossObject;

    [Header("Scene References")]
    [Tooltip("Reference to the player GameObject.")]
    [SerializeField] private GameObject _playerObject;
    [Tooltip("Reference to the BossEnemy component.")]
    [SerializeField] private BossEnemy _bossEnemy;
    [Tooltip("Reference to the BossHealth component.")]
    [SerializeField] private BossHealth _bossHealth;
    [Tooltip("Reference to the player Health component.")]
    [SerializeField] private Health _playerHealth;
    [Tooltip("Reference to the PlayerMovement component.")]
    [SerializeField] private PlayerMovement _playerMovement;
    [Tooltip("Reference to the PlayerAttack component.")]
    [SerializeField] private PlayerAttack _playerAttack;

    [Header("Q-Learning References")]
    [Tooltip("Reference to the BossRewardManager component (required for Q-learning mode).")]
    [SerializeField] private BossRewardManager _bossRewardManager;
    [Tooltip("Reference to the BossQLearning component (required for Q-learning mode).")]
    [SerializeField] private BossQLearning _bossQLearning;

    [Header("Episode Reset Positions")]
    [Tooltip("Initial position for the player at the start of an episode.")]
    [SerializeField] private Vector3 _initialPlayerPosition = new Vector3(DefaultInitialPlayerX, DefaultInitialPlayerY, 0f);
    [Tooltip("Initial position for the boss at the start of an episode.")]
    [SerializeField] private Vector3 _initialBossPosition = new Vector3(DefaultInitialBossX, DefaultInitialBossY, 0f);

    [Header("Logging Settings")]
    [Tooltip("File path for episode log output.")]
    [SerializeField] private string _logFilePath = DefaultLogFileName;

    // ==================== Internal State ====================
    [SerializeField, HideInInspector] private int _episodeCount = 0;
    [SerializeField, HideInInspector] private float _averageReward;
    [SerializeField, HideInInspector] private float _accumulatedReward = 0f;
    private int _episodesSinceLastLog = 0;
    private float _episodeStartTime;
    private bool _timeoutTriggered = false;

    // ==================== Public Properties ====================
    /// <summary>
    /// Gets the initial player position for episode resets.
    /// </summary>
    public Vector3 InitialPlayerPosition => _initialPlayerPosition;
    /// <summary>
    /// Gets the initial boss position for episode resets.
    /// </summary>
    public Vector3 InitialBossPosition => _initialBossPosition;
    /// <summary>
    /// Gets the current boss mode.
    /// </summary>
    public BossMode CurrentBossMode => _bossMode;
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
        SetupSingleton();
        ValidateReferences();
        InitializePositions();
        SetupLogging();
        LoadEpisodeCount();
    }

    /// <summary>
    /// Unity Start callback. Validates critical references before starting.
    /// </summary>
    private void Start()
    {
        ValidateCriticalReferences();
        SetupPlayerAIReference();
    }

    /// <summary>
    /// Unity Update callback. Checks for episode timeout each frame.
    /// </summary>
    private void Update()
    {
        CheckEpisodeTimeout();
    }

    /// <summary>
    /// Unity callback when the application quits. Saves episode count to PlayerPrefs.
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveEpisodeCount();
    }

    /// <summary>
    /// Unity callback when the object is disabled. Saves episode count if this is the active instance.
    /// </summary>
    private void OnDisable()
    {
        if (Instance == this)
        {
            SaveEpisodeCount();
        }
    }

    // ==================== Initialization ====================
    /// <summary>
    /// Sets up the singleton pattern for EpisodeManager.
    /// </summary>
    private void SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EpisodeManager] Duplicate EpisodeManager found. Destroying this one.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Validates that all required scene references are assigned.
    /// </summary>
    private void ValidateReferences()
    {
        if (_playerObject == null) Debug.LogError("[EpisodeManager] Player GameObject not assigned!");
        if (_bossObject == null && _bossMode != BossMode.None) Debug.LogError("[EpisodeManager] Boss GameObject not assigned!");
        if (_bossHealth == null && _bossObject != null) Debug.LogWarning("[EpisodeManager] BossHealth component not assigned!");
        if (_playerHealth == null && _playerObject != null) Debug.LogWarning("[EpisodeManager] Player Health component not assigned!");
        if (_playerMovement == null && _playerObject != null) Debug.LogWarning("[EpisodeManager] PlayerMovement component not assigned!");
        if (_playerAttack == null && _playerObject != null) Debug.LogWarning("[EpisodeManager] PlayerAttack component not assigned!");

        // Q-Learning specific validation
        if (_bossMode == BossMode.QLearning)
        {
            if (_bossRewardManager == null) Debug.LogError("[EpisodeManager] BossRewardManager reference not assigned! Required for Q-learning mode.");
            if (_bossQLearning == null) Debug.LogWarning("[EpisodeManager] BossQLearning reference not assigned (optional for QL logging).");
        }
    }

    /// <summary>
    /// Validates critical references that would prevent the system from working.
    /// </summary>
    private void ValidateCriticalReferences()
    {
        if (_playerObject == null)
        {
            Debug.LogError("[EpisodeManager] Player GameObject reference missing. Disabling EpisodeManager.");
            enabled = false;
            return;
        }

        if (_bossMode == BossMode.QLearning && _bossRewardManager == null)
        {
            Debug.LogError("[EpisodeManager] Q-Learning mode requires BossRewardManager. Disabling EpisodeManager.");
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Sets up the PlayerAI reference in BossRewardManager for dodge reward reporting.
    /// </summary>
    private void SetupPlayerAIReference()
    {
        if (_bossRewardManager != null && _playerObject != null)
        {
            PlayerAI playerAI = _playerObject.GetComponent<PlayerAI>();
            if (playerAI != null)
            {
                _bossRewardManager.SetPlayerAI(playerAI);
                Debug.Log("[EpisodeManager] PlayerAI reference set in BossRewardManager for dodge rewards.");
            }
            else
            {
                Debug.LogWarning("[EpisodeManager] PlayerAI component not found on player object.");
            }
        }
    }

    /// <summary>
    /// Initializes positions from scene objects if not manually set.
    /// </summary>
    private void InitializePositions()
    {
        if (_playerObject != null)
        {
            _initialPlayerPosition = _playerObject.transform.position;
        }
        if (_bossObject != null)
        {
            _initialBossPosition = _bossObject.transform.position;
        }
    }

    /// <summary>
    /// Sets up logging file path and ensures it's persistent.
    /// </summary>
    private void SetupLogging()
    {
        _logFilePath = Path.Combine(Application.persistentDataPath, _logFilePath);
        Debug.Log($"[EpisodeManager] Training log file path: {_logFilePath}");
        
        // Check if log file exists, if not reset episode count and create new log
        if (!File.Exists(_logFilePath))
        {
            Debug.Log("[EpisodeManager] Log file not found. Resetting episode count and creating new log file.");
            ResetEpisodeCount();
            CreateNewLogFile();
        }
    }

    /// <summary>
    /// Loads episode count from PlayerPrefs.
    /// </summary>
    private void LoadEpisodeCount()
    {
        _episodeCount = PlayerPrefs.GetInt(EpisodeCountKey, 0);
        Debug.Log($"[EpisodeManager] Loaded episode count: {_episodeCount}");
    }

    /// <summary>
    /// Saves episode count to PlayerPrefs.
    /// </summary>
    private void SaveEpisodeCount()
    {
        PlayerPrefs.SetInt(EpisodeCountKey, _episodeCount);
        PlayerPrefs.Save();
        Debug.Log($"[EpisodeManager] Saved episode count: {_episodeCount}");
    }

    // ==================== Episode Management ====================
    /// <summary>
    /// Checks if the current episode has timed out and handles it.
    /// </summary>
    private void CheckEpisodeTimeout()
    {
        if (!_timeoutTriggered && Time.time - _episodeStartTime >= _maxEpisodeDuration)
        {
            _timeoutTriggered = true;
            HandleEpisodeTimeout();
        }
    }

    /// <summary>
    /// Handles logic when an episode times out.
    /// </summary>
    private void HandleEpisodeTimeout()
    {
        Debug.LogWarning("[EpisodeManager] Episode timed out! Applying penalty and ending episode.");

        var agent = _playerObject.GetComponent<PlayerAI>();
        if (agent != null)
        {
            agent.HandleEpisodeTimeout();
        }
        else
        {
            Debug.LogError("[EpisodeManager] Could not find PlayerAI to handle timeout!");
        }
    }

    /// <summary>
    /// Records the outcome of an episode, handles logging, and resets Q-Learning state.
    /// </summary>
    /// <param name="bossWon">Did the boss win this episode?</param>
    public void RecordEndOfEpisode(bool bossWon)
    {
        // Report outcome to reward manager
        if (_bossMode == BossMode.QLearning && _bossRewardManager != null)
        {
            if (bossWon) _bossRewardManager.ReportBossWin();
            else _bossRewardManager.ReportBossLoss();
        }

        // Get total reward for the episode
        float episodeTotalReward = 0f;
        if (_bossMode == BossMode.QLearning && _bossRewardManager != null)
        {
            episodeTotalReward = _bossRewardManager.GetEpisodeTotalReward();
        }

        // Update statistics
        _accumulatedReward += episodeTotalReward;
        _episodesSinceLastLog++;
        _episodeCount++;

        // Log statistics periodically
        if (_logFrequency > 0 && _episodesSinceLastLog >= _logFrequency)
        {
            LogEpisodeStatistics(bossWon, episodeTotalReward);
        }

        // Reset Q-Learning state
        if (_bossMode == BossMode.QLearning && _bossQLearning != null)
        {
            _bossQLearning.OnEpisodeEnd(episodeTotalReward);
            _bossQLearning.ResetQLearningState();
            _bossQLearning.LogEpisode(_episodeCount, _averageReward, bossWon);
        }
    }

    /// <summary>
    /// Resets the environment for a new episode.
    /// </summary>
    public void ResetEnvironmentForNewEpisode()
    {
        Debug.Log("[EpisodeManager] Resetting environment for new episode.");

        // Reset reward manager state
        if (_bossMode == BossMode.QLearning && _bossRewardManager != null)
        {
            _bossRewardManager.StartNewEpisode();
        }

        // Reset boss state
        if (_bossMode != BossMode.None)
        {
            ResetBossState();
        }

        // Reset shared hazards
        ResetSharedHazards();

        // Start episode timer
        _episodeStartTime = Time.time;
        _timeoutTriggered = false;
    }

    // ==================== Reset Methods ====================
    /// <summary>
    /// Resets the boss GameObject and its components for a new episode.
    /// </summary>
    private void ResetBossState()
    {
        if (_bossObject == null) return;

        Debug.Log("[EpisodeManager] Resetting boss state.");

        // Reset position and physics
        _bossObject.transform.position = _initialBossPosition;
        Rigidbody2D bossRb = _bossObject.GetComponent<Rigidbody2D>();
        if (bossRb != null)
        {
            bossRb.velocity = Vector2.zero;
            bossRb.isKinematic = false;
        }

        // Reset boss based on mode
        if (_bossEnemy != null)
        {
            Debug.Log("[EpisodeManager] Re-enabling BossEnemy component");
            _bossEnemy.enabled = true;
            _bossEnemy.ResetState();
        }

        // Reset health
        if (_bossHealth != null)
        {
            Debug.Log("[EpisodeManager] Calling BossHealth.ResetHealth()");
            _bossHealth.ResetHealth();
        }

        // Re-enable components
        Collider2D bossCol = _bossObject.GetComponent<Collider2D>();
        if (bossCol != null) 
        {
            bossCol.enabled = true;
            Debug.Log("[EpisodeManager] Re-enabled boss collider");
        }

        Renderer bossRenderer = _bossObject.GetComponent<Renderer>();
        if (bossRenderer != null) 
        {
            bossRenderer.enabled = true;
            Debug.Log("[EpisodeManager] Re-enabled boss renderer");
        }
        
        Debug.Log("[EpisodeManager] Boss reset complete - GameObject active: " + _bossObject.activeInHierarchy + ", BossEnemy enabled: " + (_bossEnemy != null ? _bossEnemy.enabled : "null") + ", BossHealth enabled: " + (_bossHealth != null ? _bossHealth.enabled : "null"));
    }

    /// <summary>
    /// Resets shared hazards in the environment.
    /// </summary>
    private void ResetSharedHazards()
    {
        // Boss hazards are handled by BossEnemy.ResetState()
        // Add any additional shared hazards here if needed
    }

    // ==================== Logging ====================
    /// <summary>
    /// Logs episode statistics and resets accumulators.
    /// </summary>
    private void LogEpisodeStatistics(bool bossWon, float episodeTotalReward)
    {
        _averageReward = _accumulatedReward / _episodesSinceLastLog;

        // Get Q-Learning statistics
        string qlStats = GetQLearningStatistics();

        string logMessage = $"--- Episode Batch Summary (Episodes {_episodeCount - _episodesSinceLastLog + 1} to {_episodeCount}) ---" +
                           $"\n  Average Reward (last {_episodesSinceLastLog} episodes): {_averageReward:F3}" +
                           $"\n  Total Episodes: {_episodeCount}" +
                           $"\n  Boss Mode: {_bossMode}" +
                           qlStats;

        Debug.Log($"[EpisodeManager]\n{logMessage}");
        LogToFile(logMessage);

        // Reset accumulators
        _accumulatedReward = 0f;
        _episodesSinceLastLog = 0;
    }

    /// <summary>
    /// Gets Q-Learning specific statistics for logging.
    /// </summary>
    private string GetQLearningStatistics()
    {
        if (_bossMode != BossMode.QLearning || _bossQLearning == null)
        {
            return "";
        }

        int uniqueStates = _bossQLearning.GetUniqueStateCount();
        int totalVisitedStates = _bossQLearning.GetTotalStatesVisitedCount();
        int revisitedStateCount = _bossQLearning.GetRevisitedStateCount();
        Dictionary<int, int> visitThresholds = _bossQLearning.GetStateVisitThresholdCounts();

        string visitStats = visitThresholds != null
            ? $"  States visited >100: {visitThresholds[100]}, >200: {visitThresholds[200]}, >500: {visitThresholds[500]}, >1000: {visitThresholds[1000]}"
            : "  State visit thresholds not available.";

        return $"\n  Current Epsilon: {_bossQLearning.Epsilon:F3}" +
               $"\n  Unique States in Q-Table: {uniqueStates}" +
               $"\n  Total States Visited: {totalVisitedStates}" +
               $"\n  States Visited More Than Once: {revisitedStateCount}" +
               $"\n{visitStats}";
    }

    /// <summary>
    /// Appends a message to the episode log file with a timestamp.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void LogToFile(string message)
    {
        try
        {
            // Ensure log file exists, create it if missing
            if (!File.Exists(_logFilePath))
            {
                Debug.LogWarning("[EpisodeManager] Log file missing during write. Creating new log file.");
                CreateNewLogFile();
            }

            using (StreamWriter writer = new StreamWriter(_logFilePath, true))
            {
                writer.WriteLine($"--- {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---");
                writer.WriteLine(message);
                writer.WriteLine("--------------------");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EpisodeManager] Failed to write to log file: {ex.Message}");
        }
    }

    // ==================== Public Interface ====================
    /// <summary>
    /// Resets the episode count to 0 and clears persistent data.
    /// </summary>
    public void ResetEpisodeCount()
    {
        _episodeCount = 0;
        _accumulatedReward = 0f;
        _episodesSinceLastLog = 0;
        _averageReward = 0f;
        
        PlayerPrefs.SetInt(EpisodeCountKey, 0);
        PlayerPrefs.Save();
        
        Debug.Log("[EpisodeManager] Episode count reset to 0.");
    }

    /// <summary>
    /// Creates a new log file with header.
    /// </summary>
    private void CreateNewLogFile()
    {
        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create new log file with header
            File.WriteAllText(_logFilePath, LogFileHeader);
            Debug.Log($"[EpisodeManager] Created new log file: {_logFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EpisodeManager] Failed to create new log file: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the boss mode and validates the configuration.
    /// </summary>
    /// <param name="newMode">The new boss mode to set.</param>
    public void SetBossMode(BossMode newMode)
    {
        _bossMode = newMode;
        Debug.Log($"[EpisodeManager] Boss mode changed to: {_bossMode}");
        ValidateReferences();
    }

    /// <summary>
    /// Gets the total number of episodes completed.
    /// </summary>
    /// <returns>The current episode count.</returns>
    public int GetTotalEpisodeCount()
    {
        return _episodeCount;
    }
}
