using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Serialization;

/// <summary>
/// Displays boss training statistics such as curriculum stage, average reward, and win rate.
/// </summary>
public class BossTrainingStatsUI : MonoBehaviour
{
    // === Inspector Fields ===
    [Header("Boss Training Stats Texts")]
    [FormerlySerializedAs("stageText")]
    [Tooltip("UI Text displaying the current curriculum stage.")]
    [SerializeField] private Text _stageText;
    public Text StageText { get => _stageText; set => _stageText = value; }

    [FormerlySerializedAs("avgRewardText")]
    [Tooltip("UI Text displaying the average reward.")]
    [SerializeField] private Text _avgRewardText;
    public Text AvgRewardText { get => _avgRewardText; set => _avgRewardText = value; }

    [FormerlySerializedAs("winRateText")]
    [Tooltip("UI Text displaying the win rate.")]
    [SerializeField] private Text _winRateText;
    public Text WinRateText { get => _winRateText; set => _winRateText = value; }

    /// <summary>
    /// Updates the UI with the latest boss training stats.
    /// </summary>
    /// <param name="stage">Current curriculum stage.</param>
    /// <param name="avgReward">Average reward value.</param>
    /// <param name="winRate">Win rate as a percentage.</param>
    public void UpdateStats(int stage, float avgReward, float winRate)
    {
        _stageText.text = $"Curriculum Stage: {stage}";
        _avgRewardText.text = $"Avg Reward: {avgReward:F2}";
        _winRateText.text = $"Win Rate: {winRate:P1}";
    }
}