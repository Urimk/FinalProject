using System.Collections.Generic;
using System.Text.RegularExpressions;

using PlayFab;
using PlayFab.ClientModels;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;

public class UsernameInputMenu : MonoBehaviour
{
    [SerializeField] private GameObject _usernameInputScreen; // The menu itself
    [SerializeField] private TMP_InputField _usernameInputField;
    [SerializeField] private TextMeshProUGUI _errorText; // Display errors if submission fails

    private bool _isSubmitting = false; // Prevent multiple submissions
    private static readonly Regex ValidCharacters = new Regex("^[a-zA-Z0-9]*$"); // Only allow letters & numbers
    private string _leaderboardName;

    private void Start()
    {
        _leaderboardName = PlayerPrefs.GetString("LeaderboardName", "Level1_Normal");
        Time.timeScale = 0; // Pause game

        // Set the character limit directly in TMP_InputField
        _usernameInputField.characterLimit = 12;

        // Ensure input field is selected and ready to type
        _usernameInputField.Select();
        _usernameInputField.ActivateInputField();

        // Add listener to enforce only letters/numbers
        _usernameInputField.onValueChanged.AddListener(ValidateInput);
    }

    private void ValidateInput(string input)
    {
        // Remove invalid characters
        _usernameInputField.text = Regex.Replace(input, "[^a-zA-Z0-9]", "");
    }

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
        int maxUsernameLength = 25;
        int totalLength = username.Length + uniqueSuffix.Length + 1; // Total length of username + uniqueSuffix

        // If the combined length exceeds the max, shorten the uniqueSuffix
        if (totalLength > maxUsernameLength)
        {
            int availableLengthForSuffix = maxUsernameLength - username.Length - 1; // Space left for the suffix
            uniqueSuffix = uniqueSuffix.Substring(0, Mathf.Max(0, availableLengthForSuffix)); // Shorten suffix to fit
        }

        // Combine the username with the unique suffix
        string fullUsername = $"{username}_{uniqueSuffix}";

        // Store the full unique username locally for use
        PlayerPrefs.SetString("FullUsername", fullUsername);
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

    private void SetDisplayName(string username)
    {
        var updateRequest = new UpdateUserTitleDisplayNameRequest { DisplayName = username };
        PlayFabClientAPI.UpdateUserTitleDisplayName(updateRequest,
            result => Debug.Log($"Display name set to: {username}"),
            error => Debug.LogError("Failed to set display name: " + error.GenerateErrorReport()));
    }

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

    private void LoadMainMenu()
    {
        Time.timeScale = 1; // Resume time before loading
        SceneManager.LoadScene(0);
    }
}
