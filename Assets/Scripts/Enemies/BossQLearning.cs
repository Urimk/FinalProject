using System; // Needed for Linq methods like Count()
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Implements Q-Learning logic for the boss, including state/action management, curriculum, and persistence.
/// </summary>
[System.Serializable]
public class CurriculumStage
{
    public int numActions = 10; // e.g., only Idle + 3 moves + 3 fireballs + 3 dashes;
    public float positionDiscretization = 4.0f;
    public float velocityThresholdLow = 2.0f;
    public float velocityThresholdHigh = 6.0f;
    public float playerSpeed = 3.0f; // If you want to slow the player
    public int minEpisodes = 100; // Minimum episodes before advancing
    public float minAverageReward = 50.0f; // Threshold to advance
}

// This script acts as the "Brain" of the boss, using Q-Learning to decide which action to take.
// It learns based on state information and rewards provided by other components.
// Movement and Aiming logic are now determined by the learned Q-values.
public class BossQLearning : MonoBehaviour
{
    // ==================== Constants ====================
    private const string QTableFileName = "BossQTable_PosVelCooldownsEnergyPlayerState.json";
    private const string LogFileName = "BossTrainingLog.csv";
    private const string LogFileHeader = "Episode,Reward,Win,Stage,AverageReward\n";
    private const string PlayerTag = "Player";
    private const int DefaultRecentRewardWindow = 50;
    private const int DefaultLastAction = -1;
    private const float DefaultGlobalCooldownTimer = 0f;
    private const int SaveQTableInterval = 100;
    private const int IdleActionIndex = 0;
    private const int InvalidActionIndex = -1;
    private const int PlayerHealthBins = 5;
    private const float DefaultEnergy = 1.0f;
    private const float MinQuantizeThreshold = 0.001f;
    private const string StateErrorReferencesMissing = "STATE_ERROR_REFERENCES_MISSING";
    private const float QValueInitMin = float.MinValue;
    private const float QValueDefault = 0f;
    private const int DefaultActionIndex = 0;
    private const int StateVisitThreshold100 = 100;
    private const int StateVisitThreshold200 = 200;
    private const int StateVisitThreshold500 = 500;
    private const int StateVisitThreshold1000 = 1000;

    // ==================== Serialized Fields ====================
    [Header("References")]
    [Tooltip("Reference to the player Transform.")]
    [SerializeField] private Transform _player;
    [Tooltip("Reference to the AIBoss component.")]
    [SerializeField] private AIBoss _aiBoss;
    [Tooltip("Reference to the BossRewardManager component.")]
    [SerializeField] private BossRewardManager _rewardManager;
    [Tooltip("Reference to the player's Health component.")]
    [SerializeField] private Health _playerHealth;

    [Header("Q-Learning Parameters")]
    [Tooltip("Learning Rate (alpha): How much new information overrides old information (0=no learning, 1=only use new).")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float _learningRate = 0.1f;
    [Tooltip("Discount Factor (gamma): Importance of future rewards (0=only immediate, 1=future rewards are as important as immediate). Should be < 1.")]
    [Range(0.0f, 0.99f)]
    [SerializeField] private float _discountFactor = 0.95f;
    [Tooltip("Initial Exploration Rate (epsilon): Starting probability of choosing a random action (1=always random, 0=never random).")]
    [Range(0.0f, 1.0f)]
    [SerializeField] public float Epsilon = 1.0f;
    [Tooltip("Multiplicative decay factor for epsilon each decision step (e.g., 0.9995 means it retains 99.95% of its value). Lower value = faster decay.")]
    [SerializeField] public float EpsilonDecay = 0.9995f;
    [Tooltip("Minimum value epsilon will decay to, ensuring some exploration always occurs.")]
    [Range(0.01f, 0.5f)]
    [SerializeField] public float EpsilonMin = 0.1f;

    [Header("Gameplay & State Discretization")]
    [Tooltip("Minimum time between the boss *successfully executing* a non-Idle ability.")]
    [SerializeField] private float _globalCooldown = 0.5f;
    [Tooltip("Size of the grid cells for discretizing relative positions (larger value = coarser grid, fewer states). Adjust based on arena size.")]
    [SerializeField] private float _positionDiscretizationFactor = 2.5f;
    [Tooltip("Thresholds for discretizing velocity into bins: [-High, -Low, 0, Low, High]. Adjust based on typical player speeds.")]
    [SerializeField] private float _velocityThresholdLow = 1.0f;
    [Tooltip("Thresholds for discretizing velocity into bins: [-High, -Low, 0, Low, High]. Adjust based on typical player speeds.")]
    [SerializeField] private float _velocityThresholdHigh = 5.0f;
    [Tooltip("Number of bins to discretize boss energy into (e.g., 5 bins: 0-19%, 20-39%, ..., 80-100%). Set to 1 or less if not using energy.")]
    [SerializeField] private int _energyDiscretizationBins = 5;
    [Tooltip("The boss AI will only be active (learn/attack) if the player is within this distance.")]
    [SerializeField] private float _activationRange = 20.0f;
    [Tooltip("Distance used for movement and aiming offset calculations within AIBoss.")]
    [SerializeField] private float _actionDistanceOffset = 3.0f;

    [Tooltip("Curriculum stages for progressive learning.")]
    [SerializeField] private List<CurriculumStage> curriculumStages = new List<CurriculumStage>();
    [Header("RL Penalties")]
    [Tooltip("Penalty for invalid actions.")]
    [SerializeField] private float penaltyInvalidAction = -1.0f;
    [Tooltip("Penalty for actions blocked by global cooldown.")]
    [SerializeField] private float penaltyGCDBlocked = -0.5f;

    // ==================== Private Fields ====================
    private int currentCurriculumStage = 0;
    private Queue<float> recentRewards = new Queue<float>();
    private int recentRewardWindow = DefaultRecentRewardWindow;
    private Dictionary<string, float[]> _qTable = new Dictionary<string, float[]>();
    private string _saveFilePath;
    private string _lastState = null;
    private int _lastAction = DefaultLastAction;
    private Dictionary<string, int> _stateVisitCounts = new Dictionary<string, int>();
    private float _globalCooldownTimer = DefaultGlobalCooldownTimer;
    private string logFilePath;
    private int _maxActionIndexForCurrentStage;
    private int _numActions;
    private PlayerMovement _playerMovement;

    // ==================== Enums ====================
    /// <summary>
    /// Defines all possible actions the boss can take in Q-Learning.
    /// </summary>
    public enum ActionType
    {
        Idle = 0,
        // Movement Actions (Indices 1-8)
        Move_TowardsPlayer = 1,
        Move_AwayFromPlayer = 2,
        Move_StrafeLeft = 3,
        Move_StrafeRight = 4,
        Move_StrafeUp = 5,
        Move_StrafeDown = 6,
        Move_ToArenaCenter = 7,
        Move_ToPlayerFlank = 8,
        // Fireball Aiming Actions (Indices 9-21)
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
        // FlameTrap Actions (Indices 22-25)
        FlameTrap_AtPlayer = 22,
        FlameTrap_NearBoss = 23,
        FlameTrap_BetweenBossAndPlayer = 24,
        FlameTrap_BehindPlayer = 25,
        // Dash Actions (Indices 26-28)
        Dash_TowardsPlayer = 26,
        Dash_AwayFromPlayer = 27,
        Dash_ToPlayerFlank = 28
    }

    /// <summary>
    /// Initializes Q-table, references, and curriculum.
    /// </summary>
    private void Awake()
    {
        // --- Action count setup ---
        _numActions = System.Enum.GetNames(typeof(ActionType)).Length;
        Debug.Log($"[Q-Learning] Initialized with {_numActions} actions.");

        // --- Reference Validation and Setup ---
        if (_player == null) _player = GameObject.FindGameObjectWithTag(PlayerTag)?.transform;
        if (_aiBoss == null) _aiBoss = GetComponent<AIBoss>();
        if (_rewardManager == null) _rewardManager = FindObjectOfType<BossRewardManager>();

        if (_player != null)
        {
            _playerMovement = _player.GetComponent<PlayerMovement>();
            if (_playerMovement == null)
            {
                Debug.LogWarning("[Q-Learning] PlayerMovement component not found directly on the player Transform root. Velocity state might be inaccurate.");
            }
        }

        // --- Reference error handling ---
        if (_player == null || _aiBoss == null || _rewardManager == null)
        {
            Debug.LogError("[Q-Learning] Missing required references! Disabling learning component.");
            this.enabled = false;
            // Even if disabled, try loading in case we just want to inspect the loaded table
            LoadQTable();
            return;
        }

        // --- Q-Table Persistence Path ---
        _saveFilePath = Path.Combine(Application.persistentDataPath, QTableFileName);
        LoadQTable(); // Load Q-table and stateVisitCounts
        // This should work in headless mode
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            Debug.Log("ProcessExit called, saving Q-table...");
            SaveQTable();
        };

        Console.CancelKeyPress += (sender, args) =>
        {
            Debug.Log("CancelKeyPress (Ctrl+C) detected, saving Q-table...");
            SaveQTable();
            args.Cancel = false; // Let the process continue to shut down
        };

        ApplyCurriculumStage(currentCurriculumStage);
    }

    /// <summary>
    /// Initializes log file for training.
    /// </summary>
    private void Start()
    {
        logFilePath = Path.Combine(Application.persistentDataPath, LogFileName);
        if (!File.Exists(logFilePath))
            File.WriteAllText(logFilePath, LogFileHeader);
    }

    // --- Main Decision Loop (Update) ---
    /// <summary>
    /// Main Q-learning update loop: observes state, updates Q-table, selects and executes actions.
    /// </summary>
    void Update()
    {
        // Essential reference checks
        if (_player == null || _aiBoss == null || _rewardManager == null || !this.enabled) return;

        // Periodically save Q-table
        if (EpisodeManager.Instance.EpisodeCount % SaveQTableInterval == 0)
        {
            SaveQTable();
        }

        // Decrement global cooldown timer
        if (_globalCooldownTimer > 0)
        {
            _globalCooldownTimer -= Time.deltaTime;
        }

        // --- Activation Range Check ---
        float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
        if (distanceToPlayer > _activationRange)
        {
            // Pause learning and reset state if player is out of range
            if (_aiBoss != null && !_aiBoss.IsCurrentlyChargingOrDashing())
            {
                _aiBoss.AIRequestIdle();
            }
            if (_lastState != null)
            {
                _lastState = null;
                _lastAction = InvalidActionIndex;
            }
            _globalCooldownTimer = DefaultGlobalCooldownTimer;
            return;
        }

        // --- Q-Learning Cycle ---
        // 1. Observe Current State
        string currentState = GetCurrentDiscreteState();

        // Increment visit count for the current state
        if (!_stateVisitCounts.ContainsKey(currentState))
        {
            _stateVisitCounts[currentState] = 0;
        }
        _stateVisitCounts[currentState]++;

        // 2. Get Reward & Update Q-Table (Based on the *previous* action's outcome)
        if (_lastState != null && _lastAction != InvalidActionIndex)
        {
            float reward = _rewardManager.GetStepRewardAndReset();
            UpdateQTable(_lastState, _lastAction, reward, currentState);
            // Decay Epsilon after a learning step
            if (Epsilon > EpsilonMin)
            {
                Epsilon = Mathf.Max(EpsilonMin, Epsilon * EpsilonDecay);
            }
        }

        // 3. Select New Action (Based on the *current* state)
        int currentAction = SelectAction(currentState);

        // 4. Execute Newly Selected Action
        ExecuteAction(currentAction, currentState);

        // 5. Prepare for Next Cycle: Store current state and the chosen action
        _lastState = currentState;
        _lastAction = currentAction;
    }

    /// <summary>
    /// Resets the internal Q-Learning state for the next episode (called by EpisodeManager).
    /// </summary>
    public void ResetQLearningState()
    {
        _lastState = null;
        _lastAction = InvalidActionIndex;
        _globalCooldownTimer = DefaultGlobalCooldownTimer;
        // Do NOT reset stateVisitCounts or qTable here.
        // Do NOT increment episodeCount here (EpisodeManager handles it).
        // Do NOT log counts here (EpisodeManager handles logging interval).
    }

    // --- State Representation ---
    /// <summary>
    /// Returns a string representing the current discretized state for Q-learning.
    /// </summary>
    private string GetCurrentDiscreteState()
    {
        if (_player == null || _aiBoss == null) return StateErrorReferencesMissing;

        Vector2 playerPos = _player.position;
        Vector2 bossPos = transform.position;

        // Player velocity
        Vector2 playerVel = Vector2.zero;
        if (_playerMovement != null)
        {
            playerVel = _playerMovement.GetVelocity();
        }

        // Player status (grounded/airborne)
        bool playerIsGrounded = true; // Replace with actual check

        // Boss energy (if using)
        float currentEnergy = DefaultEnergy; // Placeholder

        // Discretize continuous values
        Vector2 relativePos = playerPos - bossPos;
        int relPosXBin = Mathf.RoundToInt(relativePos.x / _positionDiscretizationFactor);
        int relPosYBin = Mathf.RoundToInt(relativePos.y / _positionDiscretizationFactor);

        int playerVelXBin = QuantizeFloat(playerVel.x, _velocityThresholdLow, _velocityThresholdHigh);
        int playerVelYBin = QuantizeFloat(playerVel.y, _velocityThresholdLow, _velocityThresholdHigh);

        // Discretize energy (if using)
        int energyBin = 0;
        if (_energyDiscretizationBins > 0 && _aiBoss != null)
        {
            // currentEnergy = aiBoss.GetCurrentEnergyNormalized();
            energyBin = Mathf.FloorToInt(Mathf.Clamp01(currentEnergy) * _energyDiscretizationBins);
            if (energyBin >= _energyDiscretizationBins) energyBin = _energyDiscretizationBins - 1;
        }

        // Get discrete ability cooldown status from AIBoss
        bool fireballReady = _aiBoss != null && _aiBoss.IsFireballReady();
        bool flameTrapReady = _aiBoss != null && _aiBoss.IsFlameTrapReady();
        bool dashReady = _aiBoss != null && _aiBoss.IsDashReady();

        // Player health
        int playerHealthBin = 0;
        if (_playerHealth != null)
        {
            float playerHealthNormalized = _playerHealth.currentHealth / _playerHealth.startingHealth;
            playerHealthBin = Mathf.FloorToInt(Mathf.Clamp01(playerHealthNormalized) * PlayerHealthBins);
            if (playerHealthBin >= PlayerHealthBins) playerHealthBin = PlayerHealthBins - 1;
        }
        else { Debug.LogWarning("[Q-Learning] Player Health reference missing for state!"); }

        // Player invulnerability status
        int playerInvulnerableState = 0;
        if (_playerHealth != null && _playerHealth.invulnerable)
        {
            playerInvulnerableState = 1;
        }

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

    // --- Action Selection Logic ---
    /// <summary>
    /// Selects an action for the given state using an epsilon-greedy policy.
    /// </summary>
    private int SelectAction(string state)
    {
        EnsureStateExists(state); // Make sure the state is in the Q-table

        List<int> currentlyValidActions = GetValidActions(state);

        if (currentlyValidActions.Count == 0)
        {
            // This should ideally not happen if Idle and basic movement are always possible.
            Debug.LogWarning($"[Q-Learning] No valid actions found for state {state}! Defaulting to Idle.");
            return (int)ActionType.Idle; // Default to Idle if somehow no valid actions (shouldn't happen)
        }

        // --- Epsilon-Greedy Policy ---
        if (UnityEngine.Random.value < Epsilon) // Explore
        {
            int randomIndex = UnityEngine.Random.Range(0, currentlyValidActions.Count);
            return currentlyValidActions[randomIndex];
        }
        else // Exploit
        {
            int bestAction = GetBestActionFromValidSet(state, currentlyValidActions);
            return bestAction;
        }
    }

    /// <summary>
    /// Returns a list of valid actions for the current state.
    /// </summary>
    private List<int> GetValidActions(string state)
    {
        List<int> validActions = new List<int>();

        // Idle is always considered a valid choice for the Q-learning agent's decision.
        validActions.Add((int)ActionType.Idle);

        // --- Check Movement Actions ---
        // Assume movement is valid unless the AIBoss is in a busy state (like dashing or charging an ability)
        if (_aiBoss != null && !_aiBoss.IsCurrentlyChargingOrDashing())
        {
            validActions.Add((int)ActionType.Move_TowardsPlayer);
            validActions.Add((int)ActionType.Move_AwayFromPlayer);
            validActions.Add((int)ActionType.Move_StrafeLeft);
            validActions.Add((int)ActionType.Move_StrafeRight);
            validActions.Add((int)ActionType.Move_StrafeUp);
            validActions.Add((int)ActionType.Move_StrafeDown);
            validActions.Add((int)ActionType.Move_ToArenaCenter);
            validActions.Add((int)ActionType.Move_ToPlayerFlank);
        }

        // --- Check Ability Actions ---
        // Assume AIBoss methods check individual cooldowns AND potentially resource costs.
        // Global cooldown is NOT checked here. Execution handles GCD.
        if (_aiBoss != null)
        {
            if (_aiBoss.IsFireballReady(/* potentially pass requiredEnergy */))
            {
                // If Fireball is ready, all its aiming variants are potential choices
                validActions.Add((int)ActionType.Fireball_AtCurrentPos);
                validActions.Add((int)ActionType.Fireball_Predictive);
                validActions.Add((int)ActionType.Fireball_OffsetUp);
                validActions.Add((int)ActionType.Fireball_OffsetDown);
                validActions.Add((int)ActionType.Fireball_OffsetLeft);
                validActions.Add((int)ActionType.Fireball_OffsetRight);
                validActions.Add((int)ActionType.Fireball_PredictiveOffsetUp);
                validActions.Add((int)ActionType.Fireball_PredictiveOffsetDown);
                validActions.Add((int)ActionType.Fireball_PredictiveOffsetLeft);
                validActions.Add((int)ActionType.Fireball_PredictiveOffsetRight);
                validActions.Add((int)ActionType.Fireball_RelativeForward);
                validActions.Add((int)ActionType.Fireball_RelativeUp);
                validActions.Add((int)ActionType.Fireball_RelativeDown);
            }
            if (_aiBoss.IsFlameTrapReady(/* potentially pass requiredEnergy */))
            {
                // If FlameTrap is ready, all its placement variants are potential choices
                validActions.Add((int)ActionType.FlameTrap_AtPlayer);
                validActions.Add((int)ActionType.FlameTrap_NearBoss);
                validActions.Add((int)ActionType.FlameTrap_BetweenBossAndPlayer);
                validActions.Add((int)ActionType.FlameTrap_BehindPlayer);
            }
            // Dash actions require Dash to be ready and not currently dashing/charging
            if (_aiBoss.IsDashReady(/* potentially pass requiredEnergy */))
            { // IsDashReady should include !IsCurrentlyChargingOrDashing check
                // If Dash is ready, all its dash variants are potential choices
                validActions.Add((int)ActionType.Dash_TowardsPlayer);
                validActions.Add((int)ActionType.Dash_AwayFromPlayer);
                validActions.Add((int)ActionType.Dash_ToPlayerFlank);
            }
        }
        else
        {
            Debug.LogWarning("[Q-Learning] AIBoss reference missing when getting valid actions.");
        }
        // Only allow actions up to the current curriculum stage
        return validActions.Where(action => action < _maxActionIndexForCurrentStage).ToList();
    }

    /// <summary>
    /// Finds the action with the highest Q-value among a provided list of valid actions for a given state.
    /// </summary>
    private int GetBestActionFromValidSet(string state, List<int> validActions)
    {
        float[] qValues = _qTable[state];
        float maxQ = QValueInitMin;
        int bestAction = validActions.Count > 0 ? validActions[0] : DefaultActionIndex;
        if (validActions.Count > 0) maxQ = qValues[bestAction];

        foreach (int actionIndex in validActions)
        {
            // Basic safety check
            if (actionIndex >= 0 && actionIndex < qValues.Length)
            {
                if (qValues[actionIndex] > maxQ)
                {
                    maxQ = qValues[actionIndex];
                    bestAction = actionIndex;
                }
            }
            else
            {
                Debug.LogError($"[Q-Learning] Invalid action index {actionIndex} encountered in GetBestActionFromValidSet for state {state}!");
            }
        }
        return bestAction;
    }

    /// <summary>
    /// Calculates the maximum possible Q-value for the next state (used in the Q-update).
    /// </summary>
    private float GetMaxQForState(string nextState)
    {
        EnsureStateExists(nextState);
        float[] nextQValues = _qTable[nextState];
        float maxQ = QValueInitMin;
        for (int i = 0; i < _numActions; i++)
        {
            if (nextQValues[i] > maxQ)
            {
                maxQ = nextQValues[i];
            }
        }
        if (maxQ == QValueInitMin)
        {
            return QValueDefault;
        }
        return maxQ;
    }

    // --- Q-Table Management ---
    /// <summary>
    /// Ensures the Q-table contains an entry for the given state.
    /// </summary>
    private void EnsureStateExists(string state)
    {
        if (!_qTable.ContainsKey(state))
        {
            _qTable[state] = new float[_numActions]; // Initialize with zeros for all actions
        }
    }

    /// <summary>
    /// Updates the Q-table for a given state-action pair using the Q-learning update rule.
    /// </summary>
    private void UpdateQTable(string state, int action, float reward, string nextState)
    {
        if (action < 0 || action >= _numActions)
        {
            Debug.LogError($"[Q-Learning] Invalid action index {action} provided to UpdateQTable for state {state}. Aborting update.");
            return;
        }
        // Ensure the state we're updating exists (should always be true due to logic flow)
        if (!_qTable.ContainsKey(state))
        {
            Debug.LogError($"[Q-Learning] Attempted to update Q-Table for non-existent state {state}! This should not happen. Aborting update.");
            return;
        }

        // --- Q-Learning Update Rule ---
        // Q(s,a) = Q(s,a) + alpha * [R + gamma * max Q(s',a') - Q(s,a)]
        float oldQ = _qTable[state][action];
        float maxFutureQ = GetMaxQForState(nextState); // Best expected value *from* the next state

        // Calculate the updated Q-value
        float newQ = oldQ + _learningRate * (reward + _discountFactor * maxFutureQ - oldQ);
        _qTable[state][action] = newQ;

        // Optional: Log the update details (consider logging only significant changes or periodically)
        // if (Mathf.Abs(newQ - oldQ) > 0.01f || reward != 0) // Example condition
        //   Debug.Log($"[Q-Update] State={state} | Action={(ActionType)action} | Reward={reward:F3} | FutureQ={maxFutureQ:F3} | OldQ={oldQ:F3} -> NewQ={newQ:F3}");
    }

    // --- Public Getters for State Info (Used by EpisodeManager for Logging) ---
    /// <summary>
    /// Gets the total count of unique states currently in the Q-Table.
    /// </summary>
    public int GetUniqueStateCount()
    {
        return _qTable.Count;
    }

    /// <summary>
    /// Gets the total count of unique states that have been visited at least once.
    /// </summary>
    public int GetTotalStatesVisitedCount()
    {
        return _stateVisitCounts.Count;
    }

    /// <summary>
    /// Gets the count of states that have been visited more than once.
    /// </summary>
    public int GetRevisitedStateCount()
    {
        return _stateVisitCounts.Count(pair => pair.Value > 1);
    }

    // --- Action Execution ---
    /// <summary>
    /// Translates the selected action index into a command for AIBoss and handles execution logic.
    /// </summary>
    private void ExecuteAction(int actionIndex, string currentStateInfo)
    {
        ActionType selectedAction = (ActionType)actionIndex;
        bool abilityExecutedSuccessfully = false; // Flag to track if GCD should be applied

        // Ensure AIBoss reference is valid before calling methods
        if (_aiBoss == null)
        {
            Debug.LogError("[Q-Learning] AIBoss reference missing during action execution!");
            return;
        }

        // --- Prepare contextual data for AIBoss calls ---
        Vector2 playerPos = _player.position;
        Vector2 bossPos = transform.position;
        Vector2 playerVel = Vector2.zero;
        if (_playerMovement != null) playerVel = _playerMovement.GetVelocity();

        // --- Action Dispatch ---
        switch (selectedAction)
        {
            case ActionType.Idle:
                // Idle is always executable unless AIBoss is busy with a long ability animation (handled by AIBoss itself)
                if (!_aiBoss.IsCurrentlyChargingOrDashing())
                {
                    _aiBoss.AIRequestIdle();
                }
                // Idle does not trigger GCD
                break;

            // --- Movement Actions ---
            // Movement is allowed regardless of Global Cooldown, but NOT if AIBoss is busy
            case ActionType.Move_TowardsPlayer:
            case ActionType.Move_AwayFromPlayer:
            case ActionType.Move_StrafeLeft:
            case ActionType.Move_StrafeRight:
            case ActionType.Move_StrafeUp:
            case ActionType.Move_StrafeDown:
            case ActionType.Move_ToArenaCenter:
            case ActionType.Move_ToPlayerFlank:
                if (!_aiBoss.IsCurrentlyChargingOrDashing())
                {
                    _aiBoss.AIRequestMove(selectedAction, playerPos, bossPos, _actionDistanceOffset);
                }
                break;

            // --- Ability Actions ---
            // Ability execution requires Global Cooldown to be ready AND boss not busy
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
                if (_globalCooldownTimer <= 0 && !_aiBoss.IsCurrentlyChargingOrDashing())
                {
                    if (_aiBoss.AIRequestRangedAttack(selectedAction, playerPos, playerVel, _actionDistanceOffset))
                    {
                        abilityExecutedSuccessfully = true;
                    }
                }
                break;

            case ActionType.FlameTrap_AtPlayer:
            case ActionType.FlameTrap_NearBoss:
            case ActionType.FlameTrap_BetweenBossAndPlayer:
            case ActionType.FlameTrap_BehindPlayer:
                if (_globalCooldownTimer <= 0 && !_aiBoss.IsCurrentlyChargingOrDashing())
                {
                    if (_aiBoss.AIRequestFlameAttack(selectedAction, playerPos, bossPos, playerVel, _actionDistanceOffset))
                    {
                        abilityExecutedSuccessfully = true;
                    }
                }
                break;

            case ActionType.Dash_TowardsPlayer:
            case ActionType.Dash_AwayFromPlayer:
            case ActionType.Dash_ToPlayerFlank:
                if (_globalCooldownTimer <= 0 && !_aiBoss.IsCurrentlyChargingOrDashing())
                {
                    if (_aiBoss.AIRequestDashAttack(selectedAction, playerPos, bossPos, _actionDistanceOffset))
                    {
                        abilityExecutedSuccessfully = true;
                    }
                }
                break;

            default:
                Debug.LogWarning($"[Q-Learning] Unknown action index: {actionIndex}");
                break;
        }

        // --- Apply Global Cooldown ---
        // Apply GCD only if a non-Idle ability was successfully initiated by AIBoss.
        // Movement actions do NOT trigger GCD.
        if (abilityExecutedSuccessfully)
        {
            _globalCooldownTimer = _globalCooldown;
        }
        // Optional: Log if an ability action was attempted but failed
        else if (IsAbilityAction(selectedAction) && _globalCooldownTimer <= 0 && !_aiBoss.IsCurrentlyChargingOrDashing())
        {
            Debug.LogWarning($"[Q-Learning] Ability Action {selectedAction} chosen & conditions met (GCD ready, not busy), but AIBoss.AIRequest failed (internal readiness/resource/constraint issue).");
        }
    }

    /// <summary>
    /// Returns true if the action is a movement action.
    /// </summary>
    private bool IsMovementAction(ActionType action)
    {
        // Check if the action's integer value falls within the movement range in the enum
        return (int)action >= (int)ActionType.Move_TowardsPlayer && (int)action <= (int)ActionType.Move_ToPlayerFlank;
    }

    /// <summary>
    /// Returns true if the action is an ability action (not Idle or Movement).
    /// </summary>
    private bool IsAbilityAction(ActionType action)
    {
        return action != ActionType.Idle && !IsMovementAction(action);
    }

    // --- Persistence (Saving/Loading Q-Table and State Visits) ---
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
            // Create wrapper that includes Q-Table and State Visit Counts
            QTableWrapper wrapper = new QTableWrapper(_qTable, _stateVisitCounts);
            string json = JsonUtility.ToJson(wrapper, true); // Use 'true' for pretty print (debugging)
            File.WriteAllText(_saveFilePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Q-Learning] Failed to save Q-Table: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
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

                // Load Q-Table
                if (wrapper.qStates != null && wrapper.qValues != null)
                {
                    _qTable = wrapper.ToDictionary();
                    Debug.Log($"[Q-Learning] Q-Table loaded successfully from {_saveFilePath}. {_qTable.Count} states loaded. Loaded num actions: {wrapper.qValues?.FirstOrDefault()?.array?.Length ?? 0}");
                    // Log a warning if the loaded number of actions doesn't match the current script's expectation
                    if (_qTable.Count > 0 && _qTable.First().Value.Length != _numActions)
                    {
                        Debug.LogWarning($"[Q-Learning Load] Loaded Q-Table has {_qTable.First().Value.Length} actions per state, but current script expects {_numActions}. This may lead to incorrect behavior or errors if the action space definition changed. Consider clearing save file if action space changed significantly.");
                    }
                }
                else
                {
                    Debug.LogWarning("[Q-Learning] Q-Table data in save file is null or incomplete. Starting with empty Q-table.");
                    _qTable = new Dictionary<string, float[]>();
                }

                // Load State Visit Counts
                if (wrapper.visitStates != null && wrapper.visitCounts != null)
                {
                    _stateVisitCounts = wrapper.ToStateVisitDictionary();
                    Debug.Log($"[Q-Learning] State Visit Counts loaded successfully. {_stateVisitCounts.Count} states tracked.");
                }
                else
                {
                    Debug.LogWarning("[Q-Learning] State Visit data in save file is null or incomplete. Starting state visit counts fresh.");
                    _stateVisitCounts = new Dictionary<string, int>();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Q-Learning] Failed to load or parse Q-Table from {_saveFilePath}: {ex.GetType().Name} - {ex.Message}. Starting with empty tables.\n{ex.StackTrace}");
                _qTable = new Dictionary<string, float[]>(); // Start fresh on error
                _stateVisitCounts = new Dictionary<string, int>(); // Start fresh on error
            }
        }
        else
        {
            Debug.Log("[Q-Learning] No saved Q-Table found. Starting with empty tables.");
            _qTable = new Dictionary<string, float[]>();
            _stateVisitCounts = new Dictionary<string, int>();
        }
    }

    // --- JSON Serialization Helper Classes (Updated to include visit counts) ---
    /// <summary>
    /// Serializable wrapper for saving and loading the Q-table and state visit counts.
    /// </summary>
    [System.Serializable]
    private class QTableWrapper
    {
        // For Q-Table
        public List<string> qStates;
        public List<FloatArrayWrapper> qValues;

        // For State Visit Counts
        public List<string> visitStates;
        public List<int> visitCounts;

        /// <summary>
        /// Constructs a wrapper from Q-table and visit count dictionaries for serialization.
        /// </summary>
        public QTableWrapper(Dictionary<string, float[]> qTableDict, Dictionary<string, int> visitDict)
        {
            qStates = new List<string>();
            qValues = new List<FloatArrayWrapper>();
            visitStates = new List<string>();
            visitCounts = new List<int>();

            if (qTableDict != null)
            {
                foreach (var kvp in qTableDict)
                {
                    qStates.Add(kvp.Key);
                    qValues.Add(new FloatArrayWrapper(kvp.Value));
                }
            }

            if (visitDict != null)
            {
                foreach (var kvp in visitDict)
                {
                    visitStates.Add(kvp.Key);
                    visitCounts.Add(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Converts the wrapper's Q-table data back into a dictionary.
        /// </summary>
        public Dictionary<string, float[]> ToDictionary()
        {
            Dictionary<string, float[]> dict = new Dictionary<string, float[]>();
            int expectedActionCount = System.Enum.GetNames(typeof(ActionType)).Length;

            if (qStates != null && qValues != null && qStates.Count == qValues.Count)
            {
                for (int i = 0; i < qStates.Count; i++)
                {
                    if (!string.IsNullOrEmpty(qStates[i]) && qValues[i] != null && qValues[i].array != null)
                    {
                        // Ensure the loaded array size matches the current number of actions
                        if (qValues[i].array.Length == expectedActionCount)
                        {
                            dict[qStates[i]] = qValues[i].array;
                        }
                        else
                        {
                            Debug.LogWarning($"[Q-Learning Load] Skipping Q-Table entry for state '{qStates[i]}' due to action count mismatch ({qValues[i].array.Length} vs {expectedActionCount}).");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Q-Learning Load] Skipping invalid entry during Q-Table loading at index {i}. State: '{(qStates != null && i < qStates.Count ? qStates[i] : "N/A")}', Values null? {(qValues != null && i < qValues.Count ? qValues[i] == null : true)}");
                    }
                }
            }
            return dict;
        }

        /// <summary>
        /// Converts the wrapper's state visit data back into a dictionary.
        /// </summary>
        public Dictionary<string, int> ToStateVisitDictionary()
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            if (visitStates != null && visitCounts != null && visitStates.Count == visitCounts.Count)
            {
                for (int i = 0; i < visitStates.Count; i++)
                {
                    if (!string.IsNullOrEmpty(visitStates[i]))
                    {
                        dict[visitStates[i]] = visitCounts[i];
                    }
                    else
                    {
                        Debug.LogWarning($"[Q-Learning Load] Skipping invalid state visit entry at index {i}. State: '{(visitStates != null && i < visitStates.Count ? visitStates[i] : "N/A")}'");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[Q-Learning Load] State visit state and count lists mismatch or are null during deserialization. Starting visit counts fresh.");
            }
            return dict;
        }
    }

    /// <summary>
    /// Helper class for serializing float arrays in lists (required by Unity's JsonUtility).
    /// </summary>
    [System.Serializable]
    private class FloatArrayWrapper
    {
        public float[] array;
        public FloatArrayWrapper(float[] arr) { this.array = arr; }
    }

    // --- Curriculum and Logging Utilities ---

    /// <summary>
    /// Applies the curriculum stage settings for the given stage index.
    /// </summary>
    private void ApplyCurriculumStage(int stageIdx)
    {
        var stage = curriculumStages[stageIdx];
        _maxActionIndexForCurrentStage = stage.numActions;
        _positionDiscretizationFactor = stage.positionDiscretization;
    }

    /// <summary>
    /// Handles logic for the end of an episode, including curriculum advancement.
    /// </summary>
    public void OnEpisodeEnd(float episodeReward)
    {
        recentRewards.Enqueue(episodeReward);
        Debug.Log($"Average Over the last 50 Episodes: {recentRewards.Average()}");
        if (recentRewards.Count > recentRewardWindow)
            recentRewards.Dequeue();

        if (currentCurriculumStage < curriculumStages.Count - 1)
        {
            if (recentRewards.Count == recentRewardWindow &&
                recentRewards.Average() > curriculumStages[currentCurriculumStage].minAverageReward &&
                EpisodeManager.Instance.EpisodeCount > curriculumStages[currentCurriculumStage].minEpisodes)
            {
                currentCurriculumStage++;
                ApplyCurriculumStage(currentCurriculumStage);
                Debug.Log($"[Curriculum] Advanced to stage {currentCurriculumStage}");
            }
        }
    }

    /// <summary>
    /// Logs episode statistics to the log file.
    /// </summary>
    public void LogEpisode(int episode, float reward, bool win)
    {
        float avgReward = recentRewards.Count > 0 ? recentRewards.Average() : 0f;
        string line = $"{episode},{reward},{(win ? 1 : 0)},{currentCurriculumStage},{avgReward}\n";
        File.AppendAllText(logFilePath, line);
    }

    /// <summary>
    /// Returns a dictionary of state visit counts above certain thresholds.
    /// </summary>
    public Dictionary<int, int> GetStateVisitThresholdCounts()
    {
        var result = new Dictionary<int, int>
        {
            [StateVisitThreshold100] = 0,
            [StateVisitThreshold200] = 0,
            [StateVisitThreshold500] = 0,
            [StateVisitThreshold1000] = 0
        };

        foreach (var count in _stateVisitCounts.Values)
        {
            if (count > StateVisitThreshold100) result[StateVisitThreshold100]++;
            if (count > StateVisitThreshold200) result[StateVisitThreshold200]++;
            if (count > StateVisitThreshold500) result[StateVisitThreshold500]++;
            if (count > StateVisitThreshold1000) result[StateVisitThreshold1000]++;
        }

        return result;
    }
}

