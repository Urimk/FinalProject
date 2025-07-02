using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Displays boss training statistics such as curriculum stage, average reward, and win rate.
/// </summary>
public class BossTrainingStatsUI : MonoBehaviour
{
    public Text stageText, avgRewardText, winRateText;

    /// <summary>
    /// Updates the UI with the latest boss training stats.
    /// </summary>
    public void UpdateStats(int stage, float avgReward, float winRate)
    {
        stageText.text = $"Curriculum Stage: {stage}";
        avgRewardText.text = $"Avg Reward: {avgReward:F2}";
        winRateText.text = $"Win Rate: {winRate:P1}";
    }
}