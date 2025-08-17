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
    // ==================== Constants ====================
    private const int MaxResultsCount = 10;
    private const int LeaderboardNameParts = 2;
    private const int DisplayNamePart = 0;
    private const int DifficultyPart = 1;
    private const string LevelPrefix = "Level";
    private const string LevelDisplayPrefix = "Level ";
    private const string UnknownPlayerName = "Unknown";
    private const string NoScoreText = "No score";
    private const string LeaderboardNotFoundText = "Leaderboard not found";
    private const string ErrorLoadingLeaderboardText = "Error loading leaderboard";

    // ==================== Inspector Fields ====================
    [Tooltip("Sound effect to play when a button is clicked.")]
    [SerializeField] private AudioClip _buttonClickSound;
    [Tooltip("Reference to the Leaderboards GameObject.")]
    [SerializeField] private GameObject _leaderboards;
    [Tooltip("Reference to the SpesificLB GameObject.")]
    [SerializeField] private GameObject _spesificLB;
    [Tooltip("Text fields for the top 10 leaderboard entries.")]
    [SerializeField] private TMP_Text[] _top10Texts;
    [Tooltip("Text field for the leaderboard title.")]
    [SerializeField] private TMP_Text _leaderboardTitle;

    // ==================== Private Fields ====================
    private string _leaderboardName;

    // ==================== Leaderboard Logic ====================
    /// <summary>
    /// Sets the leaderboard to display and updates the title.
    /// </summary>
    /// <param name="name">The name of the leaderboard to display.</param>
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

    // ==================== Menu Navigation ====================
    /// <summary>
    /// Returns to the main leaderboards menu.
    /// </summary>
    public void BackToLeadearboards()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _spesificLB.SetActive(false);
        _leaderboards.SetActive(true);
    }

    // ==================== PlayFab Logic ====================
    /// <summary>
    /// Loads and displays the top 10 players for the current leaderboard.
    /// </summary>
    private void LoadTop10Players()
    {
        // Show a temporary "Loading..." state
        for (int i = 0; i < _top10Texts.Length; i++)
        {
            _top10Texts[i].text = $"{i + 1}. Loading...";
        }
        var request = new GetLeaderboardRequest
        {
            StatisticName = _leaderboardName,
            StartPosition = 0,
            MaxResultsCount = MaxResultsCount
        };

        PlayFabClientAPI.GetLeaderboard(request, result =>
        {
            if (result.Leaderboard == null)
            {
                Debug.LogError($"Leaderboard '{_leaderboardName}' not found.");
                foreach (var text in _top10Texts)
                {
                    text.text = LeaderboardNotFoundText;
                }
                return;
            }

            if (result.Leaderboard.Count == 0)
            {
                Debug.Log($"Leaderboard '{_leaderboardName}' found but has no entries.");
                for (int i = 0; i < _top10Texts.Length; i++)
                {
                    _top10Texts[i].text = $"{i + 1}. {NoScoreText}";
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
