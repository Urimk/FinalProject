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
    private const string TestPlayerPrefix = "John_";
    private const string LeaderboardName = "Level1_Easy";
    private const int MinTestScore = 500;
    private const int MaxTestScore = 2000;

    // ==================== Private Fields ====================
    private string _testPlayerPrefix = TestPlayerPrefix;
    private string _leaderboardName = LeaderboardName;

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
    private void LoginWithCustomID(string customId)
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, result =>
        {
            Debug.Log($"Logged in as {customId}");
            SetDisplayName(customId);
            SubmitTestScore();
        }, error =>
        {
            Debug.LogError("Login failed: " + error.GenerateErrorReport());
        });
    }

    /// <summary>
    /// Sets the PlayFab display name to the custom ID.
    /// </summary>
    private void SetDisplayName(string customId)
    {
        string displayName = customId;
        var updateRequest = new UpdateUserTitleDisplayNameRequest { DisplayName = displayName };
        PlayFabClientAPI.UpdateUserTitleDisplayName(updateRequest,
            result => Debug.Log($"Display name set to: {displayName}"),
            error => Debug.LogError("Failed to set display name: " + error.GenerateErrorReport()));
    }

    /// <summary>
    /// Submits a random test score to the leaderboard.
    /// </summary>
    private void SubmitTestScore()
    {
        int randomScore = Random.Range(MinTestScore, MaxTestScore);
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = _leaderboardName, Value = randomScore }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result => Debug.Log("Score submitted successfully!"),
            error => Debug.LogError("Failed to submit score: " + error.GenerateErrorReport()));
    }
}
