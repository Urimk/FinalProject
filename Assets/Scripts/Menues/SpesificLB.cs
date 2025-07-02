using System.Collections.Generic;

using PlayFab;
using PlayFab.ClientModels;

using TMPro;

using UnityEngine;

/// <summary>
/// Handles display and loading of a specific leaderboard's top 10 entries.
/// </summary>
public class SpesificLB : MonoBehaviour
{
    private const int MaxResultsCount = 10;
    private const int LeaderboardNameParts = 2;
    private const int DisplayNamePart = 0;
    private const int DifficultyPart = 1;
    private const string LevelPrefix = "Level";
    private const string LevelDisplayPrefix = "Level ";
    private const string UnknownPlayerName = "Unknown";
    private const string NoScoreText = "No score";
    private const string LeaderboardNotFoundText = "Leaderboard not found or empty";
    private const string ErrorLoadingLeaderboardText = "Error loading leaderboard";

    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField] private GameObject _leaderboards;
    [SerializeField] private GameObject _spesificLB;
    [SerializeField] private TMP_Text[] _top10Texts;
    [SerializeField] private TMP_Text _leaderboardTitle;

    private string _leaderboardName;

    /// <summary>
    /// Sets the leaderboard to display and updates the title.
    /// </summary>
    public void SetLeaderboard(string name)
    {
        _leaderboardName = name;
        string[] parts = _leaderboardName.Split('_');
        if (parts.Length == LeaderboardNameParts)
        {
            string level = parts[DisplayNamePart].Replace(LevelPrefix, LevelDisplayPrefix);
            string difficulty = parts[DifficultyPart];
            _leaderboardTitle.text = $"{level} - {difficulty}";
        }
        LoadTop10Players();
    }

    /// <summary>
    /// Returns to the main leaderboards menu.
    /// </summary>
    public void BackToLeadearboards()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _spesificLB.SetActive(false);
        _leaderboards.SetActive(true);
    }

    /// <summary>
    /// Loads and displays the top 10 players for the current leaderboard.
    /// </summary>
    private void LoadTop10Players()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = _leaderboardName,
            StartPosition = 0,
            MaxResultsCount = MaxResultsCount
        };

        PlayFabClientAPI.GetLeaderboard(request, result =>
        {
            if (result.Leaderboard == null || result.Leaderboard.Count == 0)
            {
                Debug.LogError($"Leaderboard '{_leaderboardName}' not found or has no entries.");
                foreach (var text in _top10Texts)
                {
                    text.text = LeaderboardNotFoundText;
                }
                return;
            }

            for (int i = 0; i < _top10Texts.Length; i++)
            {
                if (i < result.Leaderboard.Count)
                {
                    var entry = result.Leaderboard[i];
                    string rawName = entry.DisplayName ?? UnknownPlayerName;
                    string displayName = rawName.Split('_')[0];
                    int score = entry.StatValue;
                    _top10Texts[i].text = $"{i + 1}. {displayName}: {score}";
                }
                else
                {
                    _top10Texts[i].text = (i + 1) + ". " + NoScoreText;
                }
            }
        },
        error =>
        {
            Debug.LogError($"Error fetching leaderboard {_leaderboardName}: {error.GenerateErrorReport()}");
            foreach (var text in _top10Texts)
            {
                text.text = ErrorLoadingLeaderboardText;
            }
        });
    }
}
