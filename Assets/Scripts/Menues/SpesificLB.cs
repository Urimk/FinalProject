using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class SpesificLB : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private GameObject leaderboards;
    [SerializeField] private GameObject spesificLB; // Assign the spesificLB GameObject in the Inspector
    [SerializeField] private TMP_Text[] top10Texts;
    [SerializeField] private TMP_Text leaderboardTitle; // Reference to the title TMP_Text

    
    private string leaderboardName;

    public void SetLeaderboard(string name)
    {
        leaderboardName = name;

    // Format the leaderboard name to "Level X - Difficulty"
    string[] parts = leaderboardName.Split('_');
    if (parts.Length == 2)
    {
        string level = parts[0].Replace("Level", "Level ");  // Ensure proper formatting (Level 1, Level 2, etc.)
        string difficulty = parts[1];  // Easy, Normal, Hard
        leaderboardTitle.text = $"{level} - {difficulty}";  // Update the title text
    }

        LoadTop10Players();
    }


    public void BackToLeadearboards()
    {
        SoundManager.instance.PlaySound(buttonClickSound);
        spesificLB.SetActive(false);
        leaderboards.SetActive(true);
    }

    private void LoadTop10Players()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = leaderboardName,
            StartPosition = 0,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, result =>
        {
            if (result.Leaderboard == null || result.Leaderboard.Count == 0)
            {
                Debug.LogError($"Leaderboard '{leaderboardName}' not found or has no entries.");
                foreach (var text in top10Texts)
                {
                    text.text = "Leaderboard not found or empty";
                }
                return;
            }

            for (int i = 0; i < top10Texts.Length; i++)
            {
                if (i < result.Leaderboard.Count)
                {
                    var entry = result.Leaderboard[i];
                    string rawName = entry.DisplayName ?? "Unknown";
                    string displayName = rawName.Split('_')[0]; 
                    int score = entry.StatValue;
                    top10Texts[i].text = $"{i + 1}. {displayName}: {score}";
                }
                else
                {
                    top10Texts[i].text = (i + 1) + ". No score";
                }
            }
        },
        error =>
        {
            Debug.LogError($"Error fetching leaderboard {leaderboardName}: {error.GenerateErrorReport()}");
            foreach (var text in top10Texts)
            {
                text.text = "Error loading leaderboard";
            }
        });
    }
}
