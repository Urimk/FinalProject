using System.Collections.Generic;

using PlayFab;
using PlayFab.ClientModels;

using UnityEngine;

public class PlayFabTestPlayers : MonoBehaviour
{
    private string _testPlayerPrefix = "John_"; // Prefix for test players
    private string _leaderboardName = "Level1_Easy";

    private void Start()
    {
        string uniqueId = _testPlayerPrefix + System.DateTime.UtcNow.Ticks; // Unique ID
        LoginWithCustomID(uniqueId);
    }

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
            SetDisplayName(customId); // Set the display name after login
            SubmitTestScore();
        }, error =>
        {
            Debug.LogError("Login failed: " + error.GenerateErrorReport());
        });
    }

    private void SetDisplayName(string customId)
    {
        string displayName = customId;

        var updateRequest = new UpdateUserTitleDisplayNameRequest { DisplayName = displayName };
        PlayFabClientAPI.UpdateUserTitleDisplayName(updateRequest,
            result => Debug.Log($"Display name set to: {displayName}"),
            error => Debug.LogError("Failed to set display name: " + error.GenerateErrorReport()));
    }

    private void SubmitTestScore()
    {
        int randomScore = Random.Range(500, 2000); // Random test score

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
