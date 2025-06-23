using System.Collections.Generic;

using PlayFab;
using PlayFab.ClientModels;

using TMPro; // Make sure to import TextMeshPro

using UnityEngine;

public class LeaderboardMenu : MonoBehaviour
{
    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _leaderboardMenu;
    [SerializeField] private GameObject _spesificLB;
    [SerializeField] private TMP_Text[] _leaderboardTexts; // Array for all 9 leaderboard cells
    private int _currentIndex;  // To store the index temporarily

    private readonly string[] _leaderboardNames =
    {
        "Level1_Easy", "Level1_Normal", "Level1_Hard",
        "Level2_Easy", "Level2_Normal", "Level2_Hard",
        "Level3_Easy", "Level3_Normal", "Level3_Hard"
    };

    private void Start()
    {
        // Log in anonymously before accessing leaderboards
        LoginAnonymously();
    }

    private void LoginAnonymously()
    {
        string customId = "TestPlayer_" + System.DateTime.UtcNow.Ticks;

        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        },
        result =>
        {
            Debug.Log("Logged in anonymously");
            LoadTopPlayers();
        },
        error =>
        {
            Debug.LogError("Error logging in anonymously: " + error.GenerateErrorReport());
        });
    }

    public void BackToMainMenu()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _leaderboardMenu.SetActive(false);
        _mainMenu.SetActive(true);
    }

    public void ChooseLB(int index)
    {
        if (index < 0 || index >= _leaderboardNames.Length)
        {
            Debug.LogError("Invalid leaderboard index: " + index);
            return;
        }
        _spesificLB.SetActive(false);

        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _leaderboardMenu.SetActive(false);

        // Find the MainMenuManager and call SetLeaderboard directly
        MainMenuController mainMenuManager = FindObjectOfType<MainMenuController>();
        if (mainMenuManager != null)
        {
            SpesificLB spesificLBComponent = mainMenuManager.GetComponent<SpesificLB>();
            if (spesificLBComponent != null)
            {
                spesificLBComponent.SetLeaderboard(_leaderboardNames[index]);
            }
            else
            {
                Debug.LogError("SpesificLB component not found on MainMenuManager.");
            }
        }
        else
        {
            Debug.LogError("MainMenuController not found.");
        }

        _spesificLB.SetActive(true);
    }

    private void LoadTopPlayers()
    {
        for (int i = 0; i < _leaderboardNames.Length; i++)
        {
            int index = i; // Prevent closure issues in lambda expressions
            var request = new GetLeaderboardRequest
            {
                StatisticName = _leaderboardNames[index],
                StartPosition = 0,
                MaxResultsCount = 1 // Get only the top player
            };

            PlayFabClientAPI.GetLeaderboard(request, result =>
            {
                if (result.Leaderboard.Count > 0)
                {
                    var topEntry = result.Leaderboard[0];
                    string rawName = topEntry.DisplayName ?? "Unknown";
                    string displayName = rawName.Split('_')[0]; // Remove unique suffix
                    int score = topEntry.StatValue;
                    _leaderboardTexts[index].text = $"{displayName}: {score}";
                }
                else
                {
                    _leaderboardTexts[index].text = "No scores yet";
                }
            },
            error =>
            {
                Debug.LogError($"Error fetching leaderboard {_leaderboardNames[index]}: {error.GenerateErrorReport()}");
                _leaderboardTexts[index].text = "Error loading";
            });
        }
    }
}
