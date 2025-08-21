using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implements Q-Learning logic for the boss, including state/action management and persistence.
/// </summary>
[System.Serializable]
public class CurriculumStage
{
    public int numActions = 10;
    public float positionDiscretization = 4.0f;
    public float velocityThresholdLow = 2.0f;
    public float velocityThresholdHigh = 6.0f;
    public float playerSpeed = 3.0f;
    public int minEpisodes = 100;
    public float minAverageReward = 50.0f;
}

/// <summary>
/// Q-Learning agent for the boss that learns optimal actions based on state and rewards.
/// </summary>
public class BossQLearning : MonoBehaviour
{
    // ==================== Constants ====================
    private const string QTableFileName = "BossQTable.json";
    private const string LogFileName = "BossTrainingLog.csv";
    private const string LogFileHeader = "Episode,Reward,Win,Stage,AverageReward\n";
    private const string PlayerTag = "Player";
    private const int DefaultRecentRewardWindow = 50;
    private const int SaveQTableInterval = 100;
    private const float MinQuantizeThreshold = 0.001f;
    private const float QValueDefault = 0f;
    private const int PlayerHealthBins = 5;
    private const float DefaultEnergy = 1.0f;

    // ==================== Inspector Fields ====================
    [Header("References")]
    [Tooltip("Reference to the player Transform.")]
    [SerializeField] private Transform _player;
    [Tooltip("Reference to the BossAI component.")]
    [SerializeField] private BossAI _bossAI;
    [Tooltip("Reference to the BossRewardManager component.")]
    [SerializeField] private BossRewardManager _rewardManager;
    [Tooltip("Reference to the player's Health component.")]
    [SerializeField] private Health _playerHealth;

    [Header("Q-Learning Parameters")]
    [Tooltip("Learning Rate (alpha): How much new information overrides old information.")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float _learningRate = 0.1f;
    [Tooltip("Discount Factor (gamma): Importance of future rewards.")]
    [Range(0.0f, 0.99f)]
    [SerializeField] private float _discountFactor = 0.95f;
    [Tooltip("Initial Exploration Rate (epsilon): Starting probability of choosing a random action.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] public float Epsilon = 1.0f;
    [Tooltip("Multiplicative decay factor for epsilon each decision step.")]
    [SerializeField] public float EpsilonDecay = 0.9995f;
    [Tooltip("Minimum value epsilon will decay to.")]
    [Range(0.01f, 0.5f)]
    [SerializeField] public float EpsilonMin = 0.1f;

    [Header("Gameplay & State Discretization")]
    [Tooltip("Minimum time between the boss successfully executing a non-Idle ability.")]
    [SerializeField] private float _globalCooldown = 0.5f;
    [Tooltip("Size of the grid cells for discretizing relative positions.")]
    [SerializeField] private float _positionDiscretizationFactor = 2.5f;
    [Tooltip("Thresholds for discretizing velocity into bins.")]
    [SerializeField] private float _velocityThresholdLow = 1.0f;
    [Tooltip("Thresholds for discretizing velocity into bins.")]
    [SerializeField] private float _velocityThresholdHigh = 5.0f;
    [Tooltip("Number of bins to discretize boss energy into.")]
    [SerializeField] private int _energyDiscretizationBins = 5;
    [Tooltip("The boss AI will only be active if the player is within this distance.")]
    [SerializeField] private float _activationRange = 20.0f;
    [Tooltip("Distance used for movement and aiming offset calculations.")]
    [SerializeField] private float _actionDistanceOffset = 3.0f;

    [Header("Curriculum Learning")]
    [Tooltip("Curriculum stages for progressive learning.")]
    [SerializeField] private List<CurriculumStage> _curriculumStages = new List<CurriculumStage>();
    [Tooltip("Whether to use curriculum learning.")]
    [SerializeField] private bool _useCurriculum = false;
    [Tooltip("Current curriculum stage.")]
    [SerializeField] private int _currentCurriculumStage = 0;

    [Header("Penalties")]
    [Tooltip("Penalty for invalid actions.")]
    [SerializeField] private float _penaltyInvalidAction = -1.0f;
    [Tooltip("Penalty for actions blocked by global cooldown.")]
    [SerializeField] private float _penaltyGCDBlocked = -0.5f;

    // ==================== Private Fields ====================
    private Dictionary<string, float[]> _qTable = new Dictionary<string, float[]>();
    private Dictionary<string, int> _stateVisitCounts = new Dictionary<string, int>();
    private Queue<float> _recentRewards = new Queue<float>();
    private string _saveFilePath;
    private string _logFilePath;
    private string _lastState = null;
    private int _lastAction = -1;
    private float _globalCooldownTimer = 0f;
    private int _numActions;
    private int _maxActionIndexForCurrentStage;
    private PlayerMovement _playerMovement;

    // ==================== Enums ====================
    /// <summary>
    /// Defines all possible actions the boss can take in Q-Learning.
    /// </summary>
    public enum ActionType
    {
        Idle = 0,
        // Movement Actions (1-8)
        Move_TowardsPlayer = 1,
        Move_AwayFromPlayer = 2,
        Move_StrafeLeft = 3,
        Move_StrafeRight = 4,
        Move_StrafeUp = 5,
        Move_StrafeDown = 6,
        Move_ToArenaCenter = 7,
        Move_ToPlayerFlank = 8,
        // Fireball Aiming Actions (9-21)
        Fireball_AtCurrentPos = 9,
        Fireball_Predictive = 10,
        Fireball_OffsetUp = 11,
        Fireball_OffsetDown = 12,
        Fireball_OffsetLeft = 13,
        Fireball_OffsetRight = 14,
        Fireball_PredictiveOffsetUp = 15,
        Fireball_PredictiveOffsetDown = 16,
        Fireball_PredictiveOffsetLeft = 17,
        Fireball_PredictiveOffsetRight = 18,
        Fireball_RelativeForward = 19,
        Fireball_RelativeUp = 20,
        Fireball_RelativeDown = 21,
        // FlameTrap Actions (22-25)
        FlameTrap_AtPlayer = 22,
        FlameTrap_NearBoss = 23,
        FlameTrap_BetweenBossAndPlayer = 24,
        FlameTrap_BehindPlayer = 25,
        // Dash Actions (26-28)
        Dash_TowardsPlayer = 26,
        Dash_AwayFromPlayer = 27,
        Dash_ToPlayerFlank = 28
    }

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Initializes Q-table, references, and curriculum.
    /// </summary>
    private void Awake()
    {
        InitializeReferences();
        InitializeQTable();
        SetupPersistence();
        
        if (_useCurriculum && _curriculumStages.Count > 0)
        {
            ApplyCurriculumStage(_currentCurriculumStage);
        }
    }

    /// <summary>
    /// Initializes log file for training.
    /// </summary>
    private void Start()
    {
        _logFilePath = Path.Combine(Application.persistentDataPath, LogFileName);
        if (!File.Exists(_logFilePath))
        {
            File.WriteAllText(_logFilePath, LogFileHeader);
        }
    }

    /// <summary>
    /// Main Q-learning update loop: observes state, updates Q-table, selects and executes actions.
    /// </summary>
    private void Update()
    {
        if (!ValidateReferences()) return;

        // Periodically save Q-table
        if (EpisodeManager.Instance != null && EpisodeManager.Instance.EpisodeCount % SaveQTableInterval == 0)
        {
            SaveQTable();
        }

        // Update global cooldown
        if (_globalCooldownTimer > 0)
        {
            _globalCooldownTimer -= Time.deltaTime;
        }

        // Check activation range
        if (!IsPlayerInRange()) return;

        // Q-Learning Cycle
        string currentState = GetCurrentDiscreteState();
        UpdateStateVisitCount(currentState);

        // Update Q-table based on previous action
        if (_lastState != null && _lastAction != -1)
        {
            float reward = _rewardManager.GetStepRewardAndReset();
            UpdateQTable(_lastState, _lastAction, reward, currentState);
            DecayEpsilon();
        }

        // Select and execute new action
        int currentAction = SelectAction(currentState);
        ExecuteAction(currentAction);

        // Store for next cycle
        _lastState = currentState;
        _lastAction = currentAction;
    }

    // ==================== Initialization ====================
    /// <summary>
    /// Initializes and validates all required references.
    /// </summary>
    private void InitializeReferences()
    {
        _numActions = System.Enum.GetNames(typeof(ActionType)).Length;
        Debug.Log($"[Q-Learning] Initialized with {_numActions} actions.");

        // Auto-find references if not assigned
        if (_player == null) _player = GameObject.FindGameObjectWithTag(PlayerTag)?.transform;
        if (_bossAI == null) _bossAI = GetComponent<BossAI>();
        if (_rewardManager == null) _rewardManager = FindObjectOfType<BossRewardManager>();

        if (_player != null)
        {
            _playerMovement = _player.GetComponent<PlayerMovement>();
        }

        // Validate critical references
        if (_player == null || _bossAI == null || _rewardManager == null)
        {
            Debug.LogError("[Q-Learning] Missing required references! Disabling learning component.");
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Initializes Q-table and state visit counts.
    /// </summary>
    private void InitializeQTable()
    {
        LoadQTable();
    }

    /// <summary>
    /// Sets up persistence paths and event handlers.
    /// </summary>
    private void SetupPersistence()
    {
        _saveFilePath = Path.Combine(Application.persistentDataPath, QTableFileName);
        
        // Setup save on exit
        AppDomain.CurrentDomain.ProcessExit += (s, e) => SaveQTable();
        Console.CancelKeyPress += (sender, args) => { SaveQTable(); args.Cancel = false; };
    }

    // ==================== Validation ====================
    /// <summary>
    /// Validates that all required references are available.
    /// </summary>
    private bool ValidateReferences()
    {
        return _player != null && _bossAI != null && _rewardManager != null && enabled;
    }

    /// <summary>
    /// Checks if the player is within activation range.
    /// </summary>
    private bool IsPlayerInRange()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
        if (distanceToPlayer > _activationRange)
        {
            // Reset state if player is out of range
            if (!_bossAI.IsCurrentlyChargingOrDashing())
            {
                _bossAI.AIRequestIdle();
            }
            _lastState = null;
            _lastAction = -1;
            _globalCooldownTimer = 0f;
            return false;
        }
        return true;
    }

    // ==================== State Management ====================
    /// <summary>
    /// Returns a string representing the current discretized state for Q-learning.
    /// </summary>
    private string GetCurrentDiscreteState()
    {
        Vector2 playerPos = _player.position;
        Vector2 bossPos = transform.position;
        Vector2 playerVel = _playerMovement != null ? _playerMovement.GetVelocity() : Vector2.zero;

        // Discretize continuous values
        Vector2 relativePos = playerPos - bossPos;
        int relPosXBin = Mathf.RoundToInt(relativePos.x / _positionDiscretizationFactor);
        int relPosYBin = Mathf.RoundToInt(relativePos.y / _positionDiscretizationFactor);

        int playerVelXBin = QuantizeFloat(playerVel.x, _velocityThresholdLow, _velocityThresholdHigh);
        int playerVelYBin = QuantizeFloat(playerVel.y, _velocityThresholdLow, _velocityThresholdHigh);

        // Get ability readiness
        bool fireballReady = _bossAI.IsFireballReady();
        bool flameTrapReady = _bossAI.IsFlameTrapReady();
        bool dashReady = _bossAI.IsDashReady();

        // Get energy bin
        int energyBin = 0;
        if (_energyDiscretizationBins > 0)
        {
            float currentEnergy = _bossAI.GetCurrentEnergyNormalized();
            energyBin = Mathf.FloorToInt(Mathf.Clamp01(currentEnergy) * _energyDiscretizationBins);
            energyBin = Mathf.Min(energyBin, _energyDiscretizationBins - 1);
        }

        // Get player health bin
        int playerHealthBin = 0;
        if (_playerHealth != null)
        {
            float playerHealthNormalized = _playerHealth.CurrentHealth / _playerHealth.StartingHealth;
            playerHealthBin = Mathf.FloorToInt(Mathf.Clamp01(playerHealthNormalized) * PlayerHealthBins);
            playerHealthBin = Mathf.Min(playerHealthBin, PlayerHealthBins - 1);
        }

        // Get player grounded status
        bool playerIsGrounded = _bossAI.IsPlayerGrounded();

        // Get player invulnerability status
        int playerInvulnerableState = (_playerHealth != null && _playerHealth.Invulnerable) ? 1 : 0;

        // Combine into state string
        return $"{relPosXBin}_{relPosYBin}_{playerVelXBin}_{playerVelYBin}_{(fireballReady ? 1 : 0)}_{(flameTrapReady ? 1 : 0)}_{(dashReady ? 1 : 0)}_{energyBin}_{(playerIsGrounded ? 1 : 0)}_{playerHealthBin}_{playerInvulnerableState}";
    }

    /// <summary>
    /// Quantizes a float value into bins for state representation.
    /// </summary>
    private int QuantizeFloat(float value, float thresholdLow, float thresholdHigh)
    {
        float absVal = Mathf.Abs(value);
        float validThresholdLow = Mathf.Max(MinQuantizeThreshold, thresholdLow);
        float validThresholdHigh = Mathf.Max(validThresholdLow, thresholdHigh);

        if (absVal < validThresholdLow) return 0;

        int sign = (int)Mathf.Sign(value);
        if (absVal < validThresholdHigh) return sign;
        else return sign * 2;
    }

    /// <summary>
    /// Updates the visit count for the current state.
    /// </summary>
    private void UpdateStateVisitCount(string state)
    {
        if (!_stateVisitCounts.ContainsKey(state))
        {
            _stateVisitCounts[state] = 0;
        }
        _stateVisitCounts[state]++;
    }

    // ==================== Action Selection ====================
    /// <summary>
    /// Selects an action for the given state using an epsilon-greedy policy.
    /// </summary>
    private int SelectAction(string state)
    {
        EnsureStateExists(state);
        List<int> validActions = GetValidActions(state);

        if (validActions.Count == 0)
        {
            Debug.LogWarning($"[Q-Learning] No valid actions found for state {state}! Defaulting to Idle.");
            return (int)ActionType.Idle;
        }

        // Epsilon-Greedy Policy
        if (UnityEngine.Random.value < Epsilon) // Explore
        {
            int randomIndex = UnityEngine.Random.Range(0, validActions.Count);
            return validActions[randomIndex];
        }
        else // Exploit
        {
            return GetBestActionFromValidSet(state, validActions);
        }
    }

    /// <summary>
    /// Returns a list of valid actions for the current state.
    /// </summary>
    private List<int> GetValidActions(string state)
    {
        List<int> validActions = new List<int>();

        // Idle is always valid
        validActions.Add((int)ActionType.Idle);

        // Movement actions (if not busy)
        if (!_bossAI.IsCurrentlyChargingOrDashing())
        {
            for (int i = (int)ActionType.Move_TowardsPlayer; i <= (int)ActionType.Move_ToPlayerFlank; i++)
            {
                validActions.Add(i);
            }
        }

        // Ability actions (if ready)
        if (_bossAI.IsFireballReady())
        {
            for (int i = (int)ActionType.Fireball_AtCurrentPos; i <= (int)ActionType.Fireball_RelativeDown; i++)
            {
                validActions.Add(i);
            }
        }

        if (_bossAI.IsFlameTrapReady())
        {
            for (int i = (int)ActionType.FlameTrap_AtPlayer; i <= (int)ActionType.FlameTrap_BehindPlayer; i++)
            {
                validActions.Add(i);
            }
        }

        if (_bossAI.IsDashReady())
        {
            for (int i = (int)ActionType.Dash_TowardsPlayer; i <= (int)ActionType.Dash_ToPlayerFlank; i++)
            {
                validActions.Add(i);
            }
        }

        // Apply curriculum filtering
        if (_useCurriculum)
        {
            return validActions.Where(action => action < _maxActionIndexForCurrentStage).ToList();
        }

        return validActions;
    }

    /// <summary>
    /// Finds the action with the highest Q-value among valid actions.
    /// </summary>
    private int GetBestActionFromValidSet(string state, List<int> validActions)
    {
        float[] qValues = _qTable[state];
        float maxQ = float.MinValue;
        int bestAction = validActions[0];

        foreach (int actionIndex in validActions)
        {
            if (actionIndex >= 0 && actionIndex < qValues.Length && qValues[actionIndex] > maxQ)
            {
                maxQ = qValues[actionIndex];
                bestAction = actionIndex;
            }
        }

        return bestAction;
    }

    // ==================== Q-Table Management ====================
    /// <summary>
    /// Ensures the Q-table contains an entry for the given state.
    /// </summary>
    private void EnsureStateExists(string state)
    {
        if (!_qTable.ContainsKey(state))
        {
            _qTable[state] = new float[_numActions];
        }
    }

    /// <summary>
    /// Updates the Q-table for a given state-action pair using the Q-learning update rule.
    /// </summary>
    private void UpdateQTable(string state, int action, float reward, string nextState)
    {
        if (action < 0 || action >= _numActions || !_qTable.ContainsKey(state))
        {
            Debug.LogError($"[Q-Learning] Invalid action {action} or state {state} in UpdateQTable.");
            return;
        }

        float oldQ = _qTable[state][action];
        float maxFutureQ = GetMaxQForState(nextState);
        float newQ = oldQ + _learningRate * (reward + _discountFactor * maxFutureQ - oldQ);
        _qTable[state][action] = newQ;
    }

    /// <summary>
    /// Calculates the maximum possible Q-value for the next state.
    /// </summary>
    private float GetMaxQForState(string nextState)
    {
        EnsureStateExists(nextState);
        float[] nextQValues = _qTable[nextState];
        return nextQValues.Max();
    }

    /// <summary>
    /// Decays epsilon for exploration control.
    /// </summary>
    private void DecayEpsilon()
    {
        if (Epsilon > EpsilonMin)
        {
            Epsilon = Mathf.Max(EpsilonMin, Epsilon * EpsilonDecay);
        }
    }

    // ==================== Action Execution ====================
    /// <summary>
    /// Executes the selected action by calling the appropriate BossEnemy method.
    /// </summary>
    private void ExecuteAction(int actionIndex)
    {
        ActionType selectedAction = (ActionType)actionIndex;
        bool abilityExecutedSuccessfully = false;

        Vector2 playerPos = _player.position;
        Vector2 bossPos = transform.position;
        Vector2 playerVel = _playerMovement != null ? _playerMovement.GetVelocity() : Vector2.zero;

        switch (selectedAction)
        {
            case ActionType.Idle:
                if (!_bossAI.IsCurrentlyChargingOrDashing())
                {
                    _bossAI.AIRequestIdle();
                }
                break;

            // Movement Actions
            case ActionType.Move_TowardsPlayer:
            case ActionType.Move_AwayFromPlayer:
            case ActionType.Move_StrafeLeft:
            case ActionType.Move_StrafeRight:
            case ActionType.Move_StrafeUp:
            case ActionType.Move_StrafeDown:
            case ActionType.Move_ToArenaCenter:
            case ActionType.Move_ToPlayerFlank:
                if (!_bossAI.IsCurrentlyChargingOrDashing())
                {
                    _bossAI.AIRequestMove(selectedAction, playerPos, bossPos, _actionDistanceOffset);
                }
                break;

            // Fireball Actions
            case ActionType.Fireball_AtCurrentPos:
            case ActionType.Fireball_Predictive:
            case ActionType.Fireball_OffsetUp:
            case ActionType.Fireball_OffsetDown:
            case ActionType.Fireball_OffsetLeft:
            case ActionType.Fireball_OffsetRight:
            case ActionType.Fireball_PredictiveOffsetUp:
            case ActionType.Fireball_PredictiveOffsetDown:
            case ActionType.Fireball_PredictiveOffsetLeft:
            case ActionType.Fireball_PredictiveOffsetRight:
            case ActionType.Fireball_RelativeForward:
            case ActionType.Fireball_RelativeUp:
            case ActionType.Fireball_RelativeDown:
                if (_globalCooldownTimer <= 0 && !_bossAI.IsCurrentlyChargingOrDashing())
                {
                    abilityExecutedSuccessfully = _bossAI.AIRequestRangedAttack(selectedAction, playerPos, playerVel, _actionDistanceOffset);
                }
                break;

            // Flame Actions
            case ActionType.FlameTrap_AtPlayer:
            case ActionType.FlameTrap_NearBoss:
            case ActionType.FlameTrap_BetweenBossAndPlayer:
            case ActionType.FlameTrap_BehindPlayer:
                if (_globalCooldownTimer <= 0 && !_bossAI.IsCurrentlyChargingOrDashing())
                {
                    abilityExecutedSuccessfully = _bossAI.AIRequestFlameAttack(selectedAction, playerPos, bossPos, playerVel, _actionDistanceOffset);
                }
                break;

            // Dash Actions
            case ActionType.Dash_TowardsPlayer:
            case ActionType.Dash_AwayFromPlayer:
            case ActionType.Dash_ToPlayerFlank:
                if (_globalCooldownTimer <= 0 && !_bossAI.IsCurrentlyChargingOrDashing())
                {
                    abilityExecutedSuccessfully = _bossAI.AIRequestDashAttack(selectedAction, playerPos, bossPos, _actionDistanceOffset);
                }
                break;

            default:
                Debug.LogWarning($"[Q-Learning] Unknown action index: {actionIndex}");
                break;
        }

        // Apply global cooldown if ability was executed
        if (abilityExecutedSuccessfully)
        {
            _globalCooldownTimer = _globalCooldown;
        }
    }

    // ==================== Curriculum Learning ====================
    /// <summary>
    /// Applies the curriculum stage settings for the given stage index.
    /// </summary>
    private void ApplyCurriculumStage(int stageIdx)
    {
        if (stageIdx >= 0 && stageIdx < _curriculumStages.Count)
        {
            var stage = _curriculumStages[stageIdx];
            _maxActionIndexForCurrentStage = stage.numActions;
            _positionDiscretizationFactor = stage.positionDiscretization;
            _velocityThresholdLow = stage.velocityThresholdLow;
            _velocityThresholdHigh = stage.velocityThresholdHigh;
            Debug.Log($"[Q-Learning] Applied curriculum stage {stageIdx}");
        }
    }

    // ==================== Public Interface ====================
    /// <summary>
    /// Resets the internal Q-Learning state for the next episode.
    /// </summary>
    public void ResetQLearningState()
    {
        _lastState = null;
        _lastAction = -1;
        _globalCooldownTimer = 0f;
    }

    /// <summary>
    /// Handles logic for the end of an episode, including curriculum advancement.
    /// </summary>
    public void OnEpisodeEnd(float episodeReward)
    {
        _recentRewards.Enqueue(episodeReward);
        if (_recentRewards.Count > DefaultRecentRewardWindow)
        {
            _recentRewards.Dequeue();
        }

        if (_useCurriculum && _currentCurriculumStage < _curriculumStages.Count - 1)
        {
            if (_recentRewards.Count == DefaultRecentRewardWindow &&
                _recentRewards.Average() > _curriculumStages[_currentCurriculumStage].minAverageReward &&
                EpisodeManager.Instance.EpisodeCount > _curriculumStages[_currentCurriculumStage].minEpisodes)
            {
                _currentCurriculumStage++;
                ApplyCurriculumStage(_currentCurriculumStage);
                Debug.Log($"[Q-Learning] Advanced to curriculum stage {_currentCurriculumStage}");
            }
        }
    }

    /// <summary>
    /// Logs episode statistics to the log file.
    /// </summary>
    public void LogEpisode(int episode, float reward, bool win)
    {
        float avgReward = _recentRewards.Count > 0 ? _recentRewards.Average() : 0f;
        string line = $"{episode},{reward},{(win ? 1 : 0)},{_currentCurriculumStage},{avgReward}\n";
        File.AppendAllText(_logFilePath, line);
    }

    // ==================== Statistics Getters ====================
    /// <summary>
    /// Gets the total count of unique states currently in the Q-Table.
    /// </summary>
    public int GetUniqueStateCount() => _qTable.Count;

    /// <summary>
    /// Gets the total count of unique states that have been visited at least once.
    /// </summary>
    public int GetTotalStatesVisitedCount() => _stateVisitCounts.Count;

    /// <summary>
    /// Gets the count of states that have been visited more than once.
    /// </summary>
    public int GetRevisitedStateCount() => _stateVisitCounts.Count(pair => pair.Value > 1);

    // ==================== Persistence ====================
    /// <summary>
    /// Saves the Q-table and state visits on application quit.
    /// </summary>
    private void OnApplicationQuit()
    {
        SaveQTable();
    }

    /// <summary>
    /// Saves the Q-table and state visit counts to disk.
    /// </summary>
    private void SaveQTable()
    {
        try
        {
            QTableWrapper wrapper = new QTableWrapper(_qTable, _stateVisitCounts);
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(_saveFilePath, json);
            Debug.Log($"[Q-Learning] Q-Table saved successfully. {_qTable.Count} states saved.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Q-Learning] Failed to save Q-Table: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the Q-table and state visit counts from disk.
    /// </summary>
    private void LoadQTable()
    {
        if (File.Exists(_saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(_saveFilePath);
                QTableWrapper wrapper = JsonUtility.FromJson<QTableWrapper>(json);

                if (wrapper.qStates != null && wrapper.qValues != null)
                {
                    _qTable = wrapper.ToDictionary();
                    Debug.Log($"[Q-Learning] Q-Table loaded successfully. {_qTable.Count} states loaded.");
                }

                if (wrapper.visitStates != null && wrapper.visitCounts != null)
                {
                    _stateVisitCounts = wrapper.ToStateVisitDictionary();
                    Debug.Log($"[Q-Learning] State visit counts loaded successfully. {_stateVisitCounts.Count} states tracked.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Q-Learning] Failed to load Q-Table: {ex.Message}");
                _qTable = new Dictionary<string, float[]>();
                _stateVisitCounts = new Dictionary<string, int>();
            }
        }
        else
        {
            Debug.Log("[Q-Learning] No saved Q-Table found. Starting with empty tables.");
            _qTable = new Dictionary<string, float[]>();
            _stateVisitCounts = new Dictionary<string, int>();
        }
    }

    // ==================== Statistics Methods ====================
    /// <summary>
    /// Gets the count of states visited more than specified thresholds.
    /// </summary>
    /// <returns>Dictionary mapping threshold values to counts of states visited more than that threshold.</returns>
    public Dictionary<int, int> GetStateVisitThresholdCounts()
    {
        Dictionary<int, int> thresholdCounts = new Dictionary<int, int>();
        
        // Initialize with common threshold values
        int[] thresholds = { 100, 200, 500, 1000 };
        foreach (int threshold in thresholds)
        {
            thresholdCounts[threshold] = 0;
        }

        // Count states that exceed each threshold
        foreach (var visitCount in _stateVisitCounts.Values)
        {
            foreach (int threshold in thresholds)
            {
                if (visitCount > threshold)
                {
                    thresholdCounts[threshold]++;
                }
            }
        }

        return thresholdCounts;
    }

    // ==================== Serialization Classes ====================
    /// <summary>
    /// Serializable wrapper for saving and loading the Q-table and state visit counts.
    /// </summary>
    [System.Serializable]
    private class QTableWrapper
    {
        public List<string> qStates;
        public List<FloatArrayWrapper> qValues;
        public List<string> visitStates;
        public List<int> visitCounts;

        public QTableWrapper(Dictionary<string, float[]> qTableDict, Dictionary<string, int> visitDict)
        {
            qStates = new List<string>();
            qValues = new List<FloatArrayWrapper>();
            visitStates = new List<string>();
            visitCounts = new List<int>();

            foreach (var kvp in qTableDict)
            {
                qStates.Add(kvp.Key);
                qValues.Add(new FloatArrayWrapper(kvp.Value));
            }

            foreach (var kvp in visitDict)
            {
                visitStates.Add(kvp.Key);
                visitCounts.Add(kvp.Value);
            }
        }

        public Dictionary<string, float[]> ToDictionary()
        {
            Dictionary<string, float[]> dict = new Dictionary<string, float[]>();
            int expectedActionCount = System.Enum.GetNames(typeof(ActionType)).Length;

            for (int i = 0; i < qStates.Count && i < qValues.Count; i++)
            {
                if (!string.IsNullOrEmpty(qStates[i]) && qValues[i]?.array != null)
                {
                    if (qValues[i].array.Length == expectedActionCount)
                    {
                        dict[qStates[i]] = qValues[i].array;
                    }
                }
            }
            return dict;
        }

        public Dictionary<string, int> ToStateVisitDictionary()
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            for (int i = 0; i < visitStates.Count && i < visitCounts.Count; i++)
            {
                if (!string.IsNullOrEmpty(visitStates[i]))
                {
                    dict[visitStates[i]] = visitCounts[i];
                }
            }
            return dict;
        }
    }

    /// <summary>
    /// Helper class for serializing float arrays in lists.
    /// </summary>
    [System.Serializable]
    private class FloatArrayWrapper
    {
        public float[] array;
        public FloatArrayWrapper(float[] arr) { array = arr; }
    }
}

