using System.Collections.Generic;
using System.Text.RegularExpressions;

using PlayFab;
using PlayFab.ClientModels;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the username input menu, validation, and PlayFab leaderboard submission.
/// </summary>
public class UsernameInputMenu : MonoBehaviour
{
    private const int UsernameCharacterLimit = 12;
    private const int MaxUsernameLength = 25;
    private const string LeaderboardNameKey = "LeaderboardName";
    private const string DefaultLeaderboardName = "Level1_Normal";
    private const string FullUsernameKey = "FullUsername";
    private static readonly Regex ValidCharacters = new Regex("^[a-zA-Z0-9]*$");

    [SerializeField] private GameObject _usernameInputScreen; // The menu itself
    [SerializeField] private TMP_InputField _usernameInputField;
    [SerializeField] private TextMeshProUGUI _errorText; // Display errors if submission fails

    private bool _isSubmitting = false; // Prevent multiple submissions
    private string _leaderboardName;

    /// <summary>
    /// Initializes the username input menu and sets up listeners.
    /// </summary>
    private void Start()
    {
        _leaderboardName = PlayerPrefs.GetString(LeaderboardNameKey, DefaultLeaderboardName);
        Time.timeScale = 0; // Pause game

        // Set the character limit directly in TMP_InputField
        _usernameInputField.characterLimit = UsernameCharacterLimit;

        // Ensure input field is selected and ready to type
        _usernameInputField.Select();
        _usernameInputField.ActivateInputField();

        // Add listener to enforce only letters/numbers
        _usernameInputField.onValueChanged.AddListener(ValidateInput);
    }

    /// <summary>
    /// Validates the input to allow only alphanumeric characters.
    /// </summary>
    private void ValidateInput(string input)
    {
        // Remove invalid characters
        _usernameInputField.text = Regex.Replace(input, "[^a-zA-Z0-9]", "");
    }

    /// <summary>
    /// Handles key input for submission and disables Escape.
    /// </summary>
    private void Update()
    {
        // Disable Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Pause is disabled in username menu.");
        }

        // Check for Enter key submission
        if (Input.GetKeyDown(KeyCode.Return) && !_isSubmitting)
        {
            SubmitScore();
        }
    }

    /// <summary>
    /// Submits the score and username to PlayFab.
    /// </summary>
    private void SubmitScore()
    {
        string username = _usernameInputField.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            _errorText.text = "Username cannot be empty!";
            return;
        }

        // Generate a unique suffix based on time (this is the part that will be truncated if necessary)
        string uniqueSuffix = System.DateTime.UtcNow.Ticks.ToString();  // A unique identifier based on time

        // Ensure the total length doesn't exceed 25 chars
        int totalLength = username.Length + uniqueSuffix.Length + 1; // Total length of username + uniqueSuffix

        // If the combined length exceeds the max, shorten the uniqueSuffix
        string finalSuffix = uniqueSuffix;
        if (totalLength > MaxUsernameLength)
        {
            int availableLengthForSuffix = MaxUsernameLength - username.Length - 1; // Space left for the suffix
            finalSuffix = uniqueSuffix.Substring(0, Mathf.Max(0, availableLengthForSuffix)); // Shorten suffix to fit
        }

        // Combine the username with the unique suffix
        string fullUsername = $"{username}_{finalSuffix}";

        // Store the full unique username locally for use
        PlayerPrefs.SetString(FullUsernameKey, fullUsername);
        PlayerPrefs.Save();

        // Use the full name for PlayFab Custom ID
        string customId = $"{username}_{System.DateTime.UtcNow.Ticks}"; // Unique Custom ID

        int score = FindObjectOfType<LevelEndTrigger>().GetTotalScore();

        // Login with Custom ID
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        },
        result =>
        {
            Debug.Log($"Logged in as {customId}");
            SetDisplayName(fullUsername); // Set the display name
            SubmitToLeaderboard(score);
        },
        error =>
        {
            Debug.LogError("PlayFab Login Failed: " + error.GenerateErrorReport());
            _errorText.text = "Login failed!";
            _isSubmitting = false;
        });
    }

    /// <summary>
    /// Sets the PlayFab display name.
    /// </summary>
    private void SetDisplayName(string username)
    {
        var updateRequest = new UpdateUserTitleDisplayNameRequest { DisplayName = username };
        PlayFabClientAPI.UpdateUserTitleDisplayName(updateRequest,
            result => Debug.Log($"Display name set to: {username}"),
            error => Debug.LogError("Failed to set display name: " + error.GenerateErrorReport()));
    }

    /// <summary>
    /// Submits the score to the PlayFab leaderboard.
    /// </summary>
    private void SubmitToLeaderboard(int score)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = _leaderboardName, Value = score }
            }
        };
        Debug.Log("Submitting score to leaderboard: " + _leaderboardName);


        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result =>
            {
                Debug.Log($"Score {score} submitted successfully!");
                LoadMainMenu();
            },
            error =>
            {
                Debug.LogError("Error submitting score: " + error.GenerateErrorReport());
                _errorText.text = "Submission failed!";
                _isSubmitting = false;
            });
    }

    /// <summary>
    /// Loads the main menu scene.
    /// </summary>
    private void LoadMainMenu()
    {
        Time.timeScale = 1; // Resume time before loading
        SceneManager.LoadScene(0);
    }
}
