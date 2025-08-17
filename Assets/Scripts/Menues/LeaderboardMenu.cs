using System.Collections.Generic;

using PlayFab;
using PlayFab.ClientModels;

using TMPro; // Make sure to import TextMeshPro

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles the leaderboard menu, including login, navigation, and displaying top players for each leaderboard.
/// </summary>
public class LeaderboardMenu : MonoBehaviour
{
    // ==================== Constants ====================
    private const int LeaderboardCells = 9;
    private const int MaxResultsCount = 1;
    private const string CustomIdPrefix = "TestPlayer_";
    private const string UnknownPlayerName = "Unknown";
    private const string NoScoresYetText = "No scores yet";
    private const string ErrorLoadingText = "Error loading";
    private const string InvalidLeaderboardIndexError = "Invalid leaderboard index: ";
    private const string SpesificLBComponentNotFoundError = "SpesificLB component not found on MainMenuManager.";
    private const string MainMenuControllerNotFoundError = "MainMenuController not found.";

    // ==================== Inspector Fields ====================
    [Tooltip("Sound effect to play when a button is clicked.")]
    [SerializeField] private AudioClip _buttonClickSound;
    [Tooltip("Reference to the MainMenu GameObject.")]
    [SerializeField] private GameObject _mainMenu;
    [Tooltip("Reference to the LeaderboardMenu GameObject.")]
    [SerializeField] private GameObject _leaderboardMenu;
    [Tooltip("Reference to the SpesificLB GameObject.")]
    [SerializeField] private GameObject _spesificLB;
    [Tooltip("Text fields for all 9 leaderboard cells.")]
    [FormerlySerializedAs("leaderboardTexts")]

    [SerializeField] private TMP_Text[] _leaderboardTexts;

    // ==================== Private Fields ====================
    private int _currentIndex;
    private readonly string[] _leaderboardNames =
    {
        "Level1_Easy", "Level1_Normal", "Level1_Hard",
        "Level2_Easy", "Level2_Normal", "Level2_Hard",
        "Level3_Easy", "Level3_Normal", "Level3_Hard"
    };

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Logs in anonymously to PlayFab and loads top players for all leaderboards.
    /// </summary>
    private void Start()
    {
        LoginAnonymously();
    }

    // ==================== Menu Navigation ====================
    /// <summary>
    /// Returns to the main menu from the leaderboard menu.
    /// </summary>
    public void BackToMainMenu()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _leaderboardMenu.SetActive(false);
        _mainMenu.SetActive(true);
    }

    /// <summary>
    /// Opens a specific leaderboard by index.
    /// </summary>
    /// <param name="index">Leaderboard index to open.</param>
    public void ChooseLB(int index)
    {
        if (index < 0 || index >= _leaderboardNames.Length)
        {
            Debug.LogError(InvalidLeaderboardIndexError + index);
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
                Debug.LogError(SpesificLBComponentNotFoundError);
            }
        }
        else
        {
            Debug.LogError(MainMenuControllerNotFoundError);
        }

        _spesificLB.SetActive(true);
    }

    // ==================== PlayFab Logic ====================
    /// <summary>
    /// Logs in anonymously using a custom ID.
    /// </summary>
    private void LoginAnonymously()
    {
        string customId = CustomIdPrefix + System.DateTime.UtcNow.Ticks;

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

    /// <summary>
    /// Loads and displays the top player for each leaderboard.
    /// </summary>
    private void LoadTopPlayers()
    {
        for (int i = 0; i < _leaderboardNames.Length; i++)
        {
            int index = i; // Prevent closure issues in lambda expressions
            var request = new GetLeaderboardRequest
            {
                StatisticName = _leaderboardNames[index],
                StartPosition = 0,
                MaxResultsCount = MaxResultsCount
            };

            PlayFabClientAPI.GetLeaderboard(request, result =>
            {
                if (result.Leaderboard.Count > 0)
                {
                    var topEntry = result.Leaderboard[0];
                    string rawName = topEntry.DisplayName ?? UnknownPlayerName;
                    string displayName = rawName.Split('_')[0]; // Remove unique suffix
                    int score = topEntry.StatValue;
                    _leaderboardTexts[index].text = $"{displayName}: {score}";
                }
                else
                {
                    _leaderboardTexts[index].text = NoScoresYetText;
                }
            },
            error =>
            {
                Debug.LogError($"Error fetching leaderboard {_leaderboardNames[index]}: {error.GenerateErrorReport()}");
                _leaderboardTexts[index].text = ErrorLoadingText;
            });
        }
    }
}
