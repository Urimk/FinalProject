using System.Collections.Generic;

using PlayFab;
using PlayFab.ClientModels;

using UnityEngine;

/// <summary>
/// Handles PlayFab test player login, display name, and test score submission for leaderboard testing.
/// </summary>
public class PlayFabTestPlayer : MonoBehaviour
{
    // ==================== Constants ====================
    private const string DefaultTestPlayerPrefix = "John_";
    private const string DefaultLeaderboardName = "Level1_Easy";
    private const int DefaultMinTestScore = 500;
    private const int DefaultMaxTestScore = 2000;

    // ==================== Configurable Fields ====================
    [Header("Test Player Settings")]
    [Tooltip("Prefix for the test player's custom ID.")]
    [SerializeField] private string _testPlayerPrefix = DefaultTestPlayerPrefix;
    [Tooltip("Leaderboard name to submit scores to.")]
    [SerializeField] private string _leaderboardName = DefaultLeaderboardName;
    [Tooltip("Minimum random test score to submit.")]
    [SerializeField] private int _minTestScore = DefaultMinTestScore;
    [Tooltip("Maximum random test score to submit.")]
    [SerializeField] private int _maxTestScore = DefaultMaxTestScore;

    /// <summary>
    /// Logs in with a unique custom ID on start.
    /// </summary>
    private void Start()
    {
        string uniqueId = _testPlayerPrefix + System.DateTime.UtcNow.Ticks;
        LoginWithCustomID(uniqueId);
    }

    /// <summary>
    /// Logs in to PlayFab with a custom ID, creating an account if needed.
    /// </summary>
    /// <param name="customId">The custom ID to use for login.</param>
    private void LoginWithCustomID(string customId)
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, result =>
        {
            DebugManager.Log(DebugCategory.Database, $"Logged in as {customId}");
            SetDisplayName(customId);
            SubmitTestScore();
        }, error =>
        {
            DebugManager.LogError(DebugCategory.Database, "Login failed: " + error.GenerateErrorReport());
        });
    }

    /// <summary>
    /// Sets the PlayFab display name to the custom ID.
    /// </summary>
    /// <param name="customId">The custom ID to set as the display name.</param>
    private void SetDisplayName(string customId)
    {
        string displayName = customId;
        var updateRequest = new UpdateUserTitleDisplayNameRequest { DisplayName = displayName };
        PlayFabClientAPI.UpdateUserTitleDisplayName(updateRequest,
            result => DebugManager.Log(DebugCategory.Database, $"Display name set to: {displayName}"),
            error => DebugManager.LogError(DebugCategory.Database, "Failed to set display name: " + error.GenerateErrorReport()));
    }

    /// <summary>
    /// Submits a random test score to the leaderboard.
    /// </summary>
    private void SubmitTestScore()
    {
        int randomScore = Random.Range(_minTestScore, _maxTestScore);
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = _leaderboardName, Value = randomScore }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result => DebugManager.Log(DebugCategory.Database, "Score submitted successfully!"),
            error => DebugManager.LogError(DebugCategory.Database, "Failed to submit score: " + error.GenerateErrorReport()));
    }
}
