using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BossTrainingStatsUI : MonoBehaviour
{
    public Text stageText, avgRewardText, winRateText;

    public void UpdateStats(int stage, float avgReward, float winRate)
    {
        stageText.text = $"Curriculum Stage: {stage}";
        avgRewardText.text = $"Avg Reward: {avgReward:F2}";
        winRateText.text = $"Win Rate: {winRate:P1}";
    }
}