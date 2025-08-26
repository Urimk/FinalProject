using System;

using UnityEngine;

/// <summary>
/// Manages reward calculation and reporting for the Boss AI agent.
/// Other scripts (Boss Health, Player Health, Projectiles, Episode Manager)
/// should call the Report... methods. The Q-Learning Agent calls GetStepRewardAndReset() each step.
/// </summary>
public class BossRewardManager : MonoBehaviour
{
    // ==================== Constants ====================
    // Reward and penalty values for various events
    private const float DefaultRewardBossWinsBase = 200.0f;
    private const float DefaultPenaltyBossLosesBase = -200.0f;
    private const float DefaultMaxEpisodeDuration = 45.0f;
    private const float DefaultRewardHitPlayer = 5.0f;
    private const float DefaultPenaltyTookDamage = -10.0f;
    private const float DefaultPenaltyAttackMissed = -0.5f;
    private const float DefaultRewardTrapTriggered = 3.0f;
    private const float DefaultPenaltyPerStep = -0.01f;

    // ==================== Serialized Fields ====================
    [Header("Terminal Rewards (End of Episode)")]
    [Tooltip("Base reward given when the boss WINS the episode (defeats the player).")]
    [SerializeField] private float _rewardBossWinsBase = DefaultRewardBossWinsBase;
    [Tooltip("Base penalty given when the boss LOSES the episode (is defeated or time runs out). Should be negative.")]
    [SerializeField] private float _penaltyBossLosesBase = DefaultPenaltyBossLosesBase;
    [Tooltip("Maximum expected/allowed duration of an episode in seconds. Used for scaling time-based rewards/penalties.")]
    [SerializeField] private float _maxEpisodeDuration = DefaultMaxEpisodeDuration;
    [Tooltip("Set to true to add a bonus for winning faster, and a larger penalty for losing faster.")]
    [SerializeField] private bool _scaleTerminalRewardByTime = true;

    [Header("Step Rewards/Penalties (During Episode)")]
    [Tooltip("Reward for the boss's attack hitting the player.")]
    [SerializeField] private float _rewardHitPlayer = DefaultRewardHitPlayer;
    [Tooltip("Penalty for the boss taking damage from the player.")]
    [SerializeField] private float _penaltyTookDamage = DefaultPenaltyTookDamage;
    [Tooltip("Penalty for firing an attack that hits nothing (optional, can be 0).")]
    [SerializeField] private float _penaltyAttackMissed = DefaultPenaltyAttackMissed;
    [Tooltip("Reward for the player triggering a flame trap placed by the boss (optional).")]
    [SerializeField] private float _rewardTrapTriggered = DefaultRewardTrapTriggered;
    [Tooltip("Tiny penalty per decision step to encourage efficiency (can be 0).")]
    [SerializeField] private float _penaltyPerStep = DefaultPenaltyPerStep;

    // ==================== Private Fields ====================
    private float _currentAccumulatedStepReward = 0f; // Accumulates rewards *between* agent steps. Is reset every step.
    private float _pendingTerminalReward = 0f;        // Stores the calculated reward for the episode end.
    private bool _terminalRewardPending = false;      // Flag to indicate a terminal reward is set.
    private float _currentEpisodeStartTime = 0f;
    private float _totalEpisodeReward = 0f;           // Tracks the total reward for the entire episode.
    
    // Reference to PlayerAI for reporting dodge rewards
    private PlayerAI _playerAI;
    
    // Boss attack tracking
    private int _totalBossAttacks = 0;

    // ==================== Step/Action Reporting Methods ====================
    /// <summary>
    /// Call when the boss hits the player.
    /// </summary>
    public void ReportHitPlayer()
    {
        _currentAccumulatedStepReward += _rewardHitPlayer;
        Debug.Log($"[BossRewardManager] ReportHitPlayer: +{_rewardHitPlayer}, total={_currentAccumulatedStepReward}");
    }

    /// <summary>
    /// Call when the boss takes damage from the player.
    /// </summary>
    /// <param name="damageAmount">Amount of damage taken (not currently used).</param>
    public void ReportTookDamage(float damageAmount)
    {
        _currentAccumulatedStepReward += _penaltyTookDamage;
        Debug.Log($"[BossRewardManager] ReportTookDamage: {_penaltyTookDamage}, total={_currentAccumulatedStepReward}");
    }

    /// <summary>
    /// Call when the boss fires an attack.
    /// </summary>
    public void ReportBossAttack()
    {
        _totalBossAttacks++;
        
        // Report to PlayerAI for tracking
        if (_playerAI != null)
        {
            _playerAI.OnBossAttackFired();
        }
    }
    
    /// <summary>
    /// Call when the boss fires an attack that misses.
    /// </summary>
    public void ReportAttackMissed()
    {
        _currentAccumulatedStepReward += _penaltyAttackMissed;
        Debug.Log($"[BossRewardManager] ReportAttackMissed: {_penaltyAttackMissed}, total={_currentAccumulatedStepReward}");
        
        // Also report to PlayerAI for dodge reward
        if (_playerAI != null)
        {
            _playerAI.OnBossAttackDodged();
        }
    }
    
    /// <summary>
    /// Sets the PlayerAI reference for reporting dodge rewards.
    /// </summary>
    /// <param name="playerAI">Reference to the PlayerAI component.</param>
    public void SetPlayerAI(PlayerAI playerAI)
    {
        _playerAI = playerAI;
    }
    
    /// <summary>
    /// Gets the total number of boss attacks fired this episode.
    /// </summary>
    /// <returns>Total boss attacks fired.</returns>
    public int GetTotalBossAttacks()
    {
        return _totalBossAttacks;
    }

    /// <summary>
    /// Call when the player triggers a flame trap placed by the boss.
    /// </summary>
    public void ReportTrapTriggered() => _currentAccumulatedStepReward += _rewardTrapTriggered;

    // ==================== Episode Management Methods ====================
    /// <summary>
    /// Call at the beginning of each new training episode to reset reward state.
    /// </summary>
    public void StartNewEpisode()
    {
        Debug.Log($"[BossRewardManager] StartNewEpisode called - resetting all reward state");
        _currentAccumulatedStepReward = 0f;
        _pendingTerminalReward = 0f;
        _terminalRewardPending = false;
        _currentEpisodeStartTime = Time.time;
        _totalEpisodeReward = 0f;
        _totalBossAttacks = 0;
        Debug.Log($"[BossRewardManager] StartNewEpisode complete - _totalEpisodeReward={_totalEpisodeReward}");
    }

    /// <summary>
    /// Call EXACTLY ONCE when the boss wins the episode.
    /// </summary>
    public void ReportBossWin()
    {
        if (_terminalRewardPending) return;
        float duration = Time.time - _currentEpisodeStartTime;
        float finalReward = _rewardBossWinsBase;
        if (_scaleTerminalRewardByTime && _maxEpisodeDuration > 0)
        {
            float timeFactor = Mathf.Clamp01(1.0f - (duration / _maxEpisodeDuration));
            finalReward += _rewardBossWinsBase * timeFactor;
        }
        _pendingTerminalReward = finalReward;
        _terminalRewardPending = true;
        Debug.Log($"[BossRewardManager] ReportBossWin: duration={duration:F2}s, finalReward={finalReward}");
    }

    /// <summary>
    /// Call EXACTLY ONCE when the boss loses the episode.
    /// </summary>
    public void ReportBossLoss()
    {
        if (_terminalRewardPending) return;
        float duration = Time.time - _currentEpisodeStartTime;
        float finalPenalty = _penaltyBossLosesBase;
        // Optionally scale penalty by time (currently commented out)
        //if (_scaleTerminalRewardByTime && _maxEpisodeDuration > 0)
        //{
        //    float timeFactor = Mathf.Clamp01(duration / _maxEpisodeDuration);
        //    float additionalTimePenalty = Mathf.Abs(_penaltyBossLosesBase) * (1.0f - timeFactor);
        //    finalPenalty -= additionalTimePenalty;
        //}
        _pendingTerminalReward = finalPenalty;
        _terminalRewardPending = true;
        Debug.Log($"[BossRewardManager] ReportBossLoss: duration={duration:F2}s, finalPenalty={finalPenalty}");
    }

    // ==================== Q-Learning/Reward Methods ====================
    /// <summary>
    /// Gets the reward accumulated since the last call, including per-step penalty and any terminal reward.
    /// Resets the step reward accumulator. Used by the Q-Learning agent each step.
    /// </summary>
    /// <returns>The reward value for this step.</returns>
    public float GetStepRewardAndReset()
    {
        float rewardToReturn = _currentAccumulatedStepReward;
        rewardToReturn += _penaltyPerStep;
        if (_terminalRewardPending)
        {
            rewardToReturn += _pendingTerminalReward;
            _pendingTerminalReward = 0f;
            _terminalRewardPending = false;
        }
        _currentAccumulatedStepReward = 0f;
        _totalEpisodeReward += rewardToReturn;
        
        // Debug logging to track step rewards
        Debug.Log($"[BossRewardManager] GetStepRewardAndReset: stepReward={rewardToReturn}, _totalEpisodeReward={_totalEpisodeReward}");
        
        return rewardToReturn;
    }

    /// <summary>
    /// Gets the total reward for the current episode without resetting anything. Useful for logging.
    /// </summary>
    /// <returns>Total reward for the episode so far.</returns>
    public float GetEpisodeTotalReward()
    {
        float total = _totalEpisodeReward;
        // Add any pending terminal reward and current step rewards
        if (_terminalRewardPending)
        {
            total += _pendingTerminalReward;
        }
        total += _currentAccumulatedStepReward;
        
        // Debug logging to track reward accumulation
        Debug.Log($"[BossRewardManager] GetEpisodeTotalReward: _totalEpisodeReward={_totalEpisodeReward}, _pendingTerminalReward={_pendingTerminalReward}, _terminalRewardPending={_terminalRewardPending}, _currentAccumulatedStepReward={_currentAccumulatedStepReward}, total={total}");
        
        return total;
    }

    // ==================== Utility Methods ====================
    /// <summary>
    /// Returns true if the episode is done (terminal reward is pending).
    /// </summary>
    public bool IsEpisodeDone()
    {
        return _terminalRewardPending;
    }

    //public void AddCustomReward(float reward)
    //{
    //    Debug.Log($"Custom Reward: {reward}");
    //    _currentAccumulatedStepReward += reward;
    //}
}