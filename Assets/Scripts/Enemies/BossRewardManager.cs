using System; // Needed for Mathf.Abs

using UnityEngine;

// Manages reward calculation for the Boss AI.
// Other scripts (Boss Health, Player Health, Projectiles, Episode Manager)
// should call the Report... methods.
// The Q-Learning Agent calls GetTotalRewardAndReset() each step.
public class BossRewardManager : MonoBehaviour
{
    [Header("Terminal Rewards (End of Episode)")]
    [Tooltip("Base reward given when the boss WINS the episode (defeats the player).")]
    [SerializeField] private float rewardBossWinsBase = 200.0f;
    [Tooltip("Base penalty given when the boss LOSES the episode (is defeated or time runs out). Should be negative.")]
    [SerializeField] private float penaltyBossLosesBase = -200.0f;
    [Tooltip("Maximum expected/allowed duration of an episode in seconds. Used for scaling time-based rewards/penalties.")]
    [SerializeField] private float maxEpisodeDuration = 30f; // Ensure this matches your EpisodeManager's episodeDuration
    [Tooltip("Set to true to add a bonus for winning faster, and a larger penalty for losing faster.")]
    [SerializeField] private bool scaleTerminalRewardByTime = true;

    [Header("Step Rewards/Penalties (During Episode)")]
    [Tooltip("Reward for the boss's attack hitting the player.")]
    [SerializeField] private float rewardHitPlayer = 5.0f; // Adjusted magnitude relative to terminal rewards
    [Tooltip("Penalty for the boss taking damage from the player.")]
    [SerializeField] private float penaltyTookDamage = -10.0f; // Adjusted magnitude relative to terminal rewards
    [Tooltip("Penalty for firing an attack that hits nothing (optional, can be 0).")]
    [SerializeField] private float penaltyAttackMissed = -0.5f; // Adjusted magnitude
    [Tooltip("Reward for the player triggering a flame trap placed by the boss (optional).")]
    [SerializeField] private float rewardTrapTriggered = 3.0f; // Adjusted magnitude
    [Tooltip("Tiny penalty per decision step to encourage efficiency (can be 0). Avoid making this too large.")]
    [SerializeField] private float penaltyPerStep = -0.01f; // Slightly increased for more impact

    // --- Deprecated Distance Rewards ---
    // Setting these to 0 effectively disables them. Keeping fields allows easy re-enabling for experiments.
    [Header("Distance Rewards (Generally NOT Recommended with Terminal Rewards)")]
    [Tooltip("Reward for being very close. Set to 0 if using strong terminal rewards.")]
    [SerializeField] private float rewardVeryClose = 0.0f;
    [SerializeField] private float veryCloseDistance = 4.0f;
    [Tooltip("Reward for moderate distance. Set to 0 if using strong terminal rewards.")]
    [SerializeField] private float rewardModerateDistance = 0.0f;
    [SerializeField] private float moderateMinDistance = 4.0f;
    [SerializeField] private float moderateMaxDistance = 8.0f;
    [Tooltip("Penalty for being too far. Set to 0 if using strong terminal rewards.")]
    [SerializeField] private float penaltyTooFar = 0.0f;
    [SerializeField] private float tooFarDistance = 8.0f;


    // --- Internal State ---
    private float _currentAccumulatedStepReward = 0f; // Accumulates rewards *between* Q-learning agent steps
    private float _pendingTerminalReward = 0f; // Stores the calculated reward for the episode end
    private bool _terminalRewardPending = false; // Flag to indicate a terminal reward is set
    private float _currentEpisodeStartTime = 0f;

    // --- Optional References for Deprecated Distance Logic ---
    // Uncomment and assign if you re-enable distance rewards
    //[Header("References (Only for Distance Logic)")]
    //[SerializeField] private Transform bossTransform;
    //[SerializeField] private Transform playerTransform;


    private void Awake()
    {
        // -- Find Transforms only if distance rewards are non-zero --
        // if (rewardVeryClose != 0 || rewardModerateDistance != 0 || penaltyTooFar != 0) {
        //     if (bossTransform == null) bossTransform = transform;
        //     if (playerTransform == null && PlayerMovement.instance != null) { // Assuming PlayerMovement has a static instance
        //         playerTransform = PlayerMovement.instance.transform;
        //     } else if (playerTransform == null) {
        //         Debug.LogWarning("[RewardManager] Player Transform not found/assigned for distance rewards.");
        //     }
        // }
    }

    // Consider adding an Update method if you need to add time-based rewards *per step*
    // (e.g., small positive reward for surviving, small negative for duration)
    // void Update()
    // {
    //     // Add penalty per decision step (handled in GetTotalRewardAndReset)
    //     // Add small survival reward per second (optional)
    //     // currentAccumulatedStepReward += rewardPerSecond * Time.deltaTime;
    // }


    // --- Methods to be called by other game systems (Health, Projectiles, Traps) ---

    public void ReportHitPlayer()
    {
        _currentAccumulatedStepReward += rewardHitPlayer;
    }

    public void ReportTookDamage(float damageAmount) // damageAmount unused, could scale penalty
    {
        _currentAccumulatedStepReward += penaltyTookDamage;
    }

    public void ReportAttackMissed()
    {
        _currentAccumulatedStepReward += penaltyAttackMissed;
    }

    public void ReportTrapTriggered()
    {
        _currentAccumulatedStepReward += rewardTrapTriggered;
    }

    // --- Methods called by the system managing episodes (e.g., EpisodeManager) ---

    /// <summary>
    /// Call this at the beginning of each new training episode.
    /// </summary>
    public void StartNewEpisode()
    {
        //Debug.Log("Reward Manager: Starting New Episode");
        _currentAccumulatedStepReward = 0f;
        _pendingTerminalReward = 0f;
        _terminalRewardPending = false;
        _currentEpisodeStartTime = Time.time; // Record start time for duration calculation
    }


    /// <summary>
    /// Call this EXACTLY ONCE when the boss wins the episode.
    /// Calculates and stores the terminal reward.
    /// </summary>
    public void ReportBossWin()
    {
        if (_terminalRewardPending) return; // Prevent multiple reports

        float duration = Time.time - _currentEpisodeStartTime;
        float finalReward = rewardBossWinsBase; // Starts with the base win reward

        if (scaleTerminalRewardByTime && maxEpisodeDuration > 0)
        {
            // Bonus for finishing faster: scales from 0 (at max time) to `rewardBossWinsBase` (instantly)
            // Time factor is 1 when duration is 0, 0 when duration is maxEpisodeDuration or more.
            float timeFactor = Mathf.Clamp01(1.0f - (duration / maxEpisodeDuration));
            float timeBonus = rewardBossWinsBase * timeFactor; // Bonus is positive
            finalReward += timeBonus; // Add bonus to base reward

            //Debug.Log($"Reward Manager: Boss WIN! Duration: {duration:F2}s. Base: {rewardBossWinsBase}, Time Bonus: {timeBonus:F2}, Total: {finalReward:F2}");
        }
        else
        {
            //Debug.Log($"Reward Manager: Boss WIN! Duration: {duration:F2}s. Final Reward: {finalReward:F2}");
        }

        _pendingTerminalReward = finalReward;
        _terminalRewardPending = true;
    }

    /// <summary>
    /// Call this EXACTLY ONCE when the boss loses the episode.
    /// Calculates and stores the terminal penalty.
    /// </summary>
    public void ReportBossLoss()
    {
        if (_terminalRewardPending) return; // Prevent multiple reports

        float duration = Time.time - _currentEpisodeStartTime;
        float finalPenalty = penaltyBossLosesBase; // Starts with the base loss penalty (negative)

        if (scaleTerminalRewardByTime && maxEpisodeDuration > 0)
        {
            // Additional penalty for losing faster (or less penalty for surviving longer)
            // Time factor is 0 when duration is 0, 1 when duration is maxEpisodeDuration or more.
            float timeFactor = Mathf.Clamp01(duration / maxEpisodeDuration);
            // Additional penalty scales from |Base| (instant loss) down to 0 (survived max time)
            // We subtract a positive value here to make the penalty *more negative* for faster loss.
            // The factor (1.0f - timeFactor) is 1 for instant loss, 0 for max duration.
            float additionalTimePenaltyMagnitude = Mathf.Abs(penaltyBossLosesBase) * (1.0f - timeFactor);
            finalPenalty -= additionalTimePenaltyMagnitude; // Subtract a positive value to make penalty larger magnitude (more negative)

            //Debug.Log($"Reward Manager: Boss LOSS! Duration: {duration:F2}s. Base: {penaltyBossLosesBase}, Add. Time Penalty: {-additionalTimePenaltyMagnitude:F2}, Total: {finalPenalty:F2}"); // Log additional penalty as negative
        }
        else
        {
            //Debug.Log($"Reward Manager: Boss LOSS! Duration: {duration:F2}s. Final Penalty: {finalPenalty:F2}");
        }

        _pendingTerminalReward = finalPenalty;
        _terminalRewardPending = true;
    }

    // --- Method called by Q-Learning Agent ---

    /// <summary>
    /// Gets the reward accumulated since the last call, including any pending terminal reward.
    /// Resets the step reward accumulator. Clears pending terminal reward if returned.
    /// </summary>
    /// <returns>The calculated reward for the agent's last step/transition.</returns>
    public float GetTotalRewardAndReset()
    {
        // Start with event-based rewards accumulated since the last step
        float rewardToReturn = _currentAccumulatedStepReward;

        // Add constant step penalty (applied each time QL requests reward)
        rewardToReturn += penaltyPerStep;

        // Add distance rewards ONLY if they are enabled (non-zero)
        // Note: Recommend keeping these at 0 when using strong terminal rewards.
        if (rewardVeryClose != 0 || rewardModerateDistance != 0 || penaltyTooFar != 0)
        {
            // AddDistanceReward(ref rewardToReturn); // Uncomment if using distance rewards
        }


        // --- CRITICAL: Add terminal reward if it's pending ---
        // This ensures the large terminal reward is only given once at the end of the episode.
        if (_terminalRewardPending)
        {
            //Debug.Log($"Reward Manager: Adding PENDING terminal reward: {_pendingTerminalReward:F2}");
            rewardToReturn += _pendingTerminalReward;
            _pendingTerminalReward = 0f;      // Clear the pending reward once it's been given
            _terminalRewardPending = false;   // Reset the flag
        }

        // --- Reset step accumulator for the next cycle ---
        _currentAccumulatedStepReward = 0f;

        return rewardToReturn;
    }

    /// <summary>
    /// Helper method for the deprecated distance rewards. Call only if needed.
    /// </summary>
    private void AddDistanceReward(ref float currentReward)
    {
        // This requires the bossTransform and playerTransform references to be set.
        // You might need to uncomment the reference finding logic in Awake() if using this.
        // if (bossTransform != null && playerTransform != null)
        // {
        //     float distance = Vector2.Distance(bossTransform.position, playerTransform.position);
        //     if (distance < veryCloseDistance && rewardVeryClose != 0) {
        //         currentReward += rewardVeryClose;
        //     } else if (distance >= moderateMinDistance && distance <= moderateMaxDistance && rewardModerateDistance != 0) {
        //         currentReward += rewardModerateDistance;
        //     } else if (distance > tooFarDistance && penaltyTooFar != 0) {
        //         currentReward += penaltyTooFar;
        //     }
        // }
    }


    // --- Optional: Check if episode is done (used by DQN logic, might be useful here too) ---
    // You might have this logic elsewhere (e.g. GameManager checking health)
    public bool IsEpisodeDone()
    {
        // This reward manager doesn't inherently know the episode state,
        // but the QLearning script might need this for the (s,a,r,s', done) tuple.
        // The terminalRewardPending flag is set *when* the episode ends.
        return _terminalRewardPending; // Returns true only *after* ReportWin/Loss has been called and before GetTotalRewardAndReset clears the flag
    }
    public void AddCustomReward(float reward)
    {
        _currentAccumulatedStepReward += reward;
    }

}
