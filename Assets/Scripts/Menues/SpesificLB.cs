using System.Collections.Generic;

using PlayFab;
using PlayFab.ClientModels;

using TMPro;

using UnityEngine;

public class SpesificLB : MonoBehaviour
{
    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField] private GameObject _leaderboards;
    [SerializeField] private GameObject _spesificLB;
    [SerializeField] private TMP_Text[] _top10Texts;
    [SerializeField] private TMP_Text _leaderboardTitle;

    private string _leaderboardName;

    public void SetLeaderboard(string name)
    {
        _leaderboardName = name;
        string[] parts = _leaderboardName.Split('_');
        if (parts.Length == 2)
        {
            string level = parts[0].Replace("Level", "Level ");
            string difficulty = parts[1];
            _leaderboardTitle.text = $"{level} - {difficulty}";
        }
        LoadTop10Players();
    }

    public void BackToLeadearboards()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _spesificLB.SetActive(false);
        _leaderboards.SetActive(true);
    }

    private void LoadTop10Players()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = _leaderboardName,
            StartPosition = 0,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, result =>
        {
            if (result.Leaderboard == null || result.Leaderboard.Count == 0)
            {
                Debug.LogError($"Leaderboard '{_leaderboardName}' not found or has no entries.");
                foreach (var text in _top10Texts)
                {
                    text.text = "Leaderboard not found or empty";
                }
                return;
            }

            for (int i = 0; i < _top10Texts.Length; i++)
            {
                if (i < result.Leaderboard.Count)
                {
                    var entry = result.Leaderboard[i];
                    string rawName = entry.DisplayName ?? "Unknown";
                    string displayName = rawName.Split('_')[0];
                    int score = entry.StatValue;
                    _top10Texts[i].text = $"{i + 1}. {displayName}: {score}";
                }
                else
                {
                    _top10Texts[i].text = (i + 1) + ". No score";
                }
            }
        },
        error =>
        {
            Debug.LogError($"Error fetching leaderboard {_leaderboardName}: {error.GenerateErrorReport()}");
            foreach (var text in _top10Texts)
            {
                text.text = "Error loading leaderboard";
            }
        });
    }
}
