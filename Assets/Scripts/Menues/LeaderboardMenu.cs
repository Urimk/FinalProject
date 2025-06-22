using System.Collections.Generic;

using PlayFab;
using PlayFab.ClientModels;

using TMPro; // Make sure to import TextMeshPro

using UnityEngine;

public class LeaderboardMenu : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject leaderboardMenu;
    [SerializeField] private GameObject spesificLB;
    [SerializeField] private TMP_Text[] leaderboardTexts; // Array for all 9 leaderboard cells
    private int currentIndex;  // To store the index temporarily


    private readonly string[] leaderboardNames =
    {
        "Level1_Easy", "Level1_Normal", "Level1_Hard",
        "Level2_Easy", "Level2_Normal", "Level2_Hard",
        "Level3_Easy", "Level3_Normal", "Level3_Hard"
    };

    void Start()
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
        SoundManager.instance.PlaySound(buttonClickSound, gameObject);
        leaderboardMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void ChooseLB(int index)
    {
        if (index < 0 || index >= leaderboardNames.Length)
        {
            Debug.LogError("Invalid leaderboard index: " + index);
            return;
        }
        spesificLB.SetActive(false);

        SoundManager.instance.PlaySound(buttonClickSound, gameObject);
        leaderboardMenu.SetActive(false);

        // Find the MainMenuManager and call SetLeaderboard directly
        MainMenuController mainMenuManager = FindObjectOfType<MainMenuController>();
        if (mainMenuManager != null)
        {
            SpesificLB spesificLBComponent = mainMenuManager.GetComponent<SpesificLB>();
            if (spesificLBComponent != null)
            {
                spesificLBComponent.SetLeaderboard(leaderboardNames[index]);
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

        spesificLB.SetActive(true);
    }






    private void LoadTopPlayers()
    {
        for (int i = 0; i < leaderboardNames.Length; i++)
        {
            int index = i; // Prevent closure issues in lambda expressions
            var request = new GetLeaderboardRequest
            {
                StatisticName = leaderboardNames[index],
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
                    leaderboardTexts[index].text = $"{displayName}: {score}";
                }
                else
                {
                    leaderboardTexts[index].text = "No scores yet";
                }
            },
            error =>
            {
                Debug.LogError($"Error fetching leaderboard {leaderboardNames[index]}: {error.GenerateErrorReport()}");
                leaderboardTexts[index].text = "Error loading";
            });
        }
    }
}
