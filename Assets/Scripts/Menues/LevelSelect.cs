using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

/// <summary>
/// Handles level selection and navigation from the level select menu.
/// </summary>
public class LevelSelect : MonoBehaviour
{
    private const string LevelKey = "SelectedLevel";
    private const string DifficultyKey = "GameDifficulty";
    private const string LeaderboardNameKey = "LeaderboardName";
    private const string DefaultDifficulty = "Normal";
    private const string LevelSceneFormat = "Level {0} - {1}";

    [SerializeField] private AudioClip _buttonClickSound; // Assign the sound effect in the Inspector
    [SerializeField] private GameObject _mainMenu; // Assign the MainMenu GameObject in the Inspector
    [SerializeField] private GameObject _levelSelect; // Assign the LevelSelect GameObject in the Inspector

    /// <summary>
    /// Selects a level, saves preferences, and loads the scene.
    /// </summary>
    public void SelectLevel(int levelIndex)
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);

        // Save the selected level
        PlayerPrefs.SetInt(LevelKey, levelIndex);

        // Get the currently selected difficulty (default to "Normal" if not set)
        string difficulty = PlayerPrefs.GetString(DifficultyKey, DefaultDifficulty);

        // Save the full leaderboard name as "LevelX_Difficulty"
        string leaderboardName = $"Level{levelIndex}_{difficulty}";
        PlayerPrefs.SetString(LeaderboardNameKey, leaderboardName);
        PlayerPrefs.Save(); // Ensure the data is saved

        Debug.Log($"Selected Level: {levelIndex}, Difficulty: {difficulty}, Leaderboard: {leaderboardName}");

        // Load the selected level
        SceneManager.LoadScene(string.Format(LevelSceneFormat, levelIndex, difficulty));
        SoundManager.instance.ChangeMusic(SoundManager.instance.level1Music);
    }

    /// <summary>
    /// Returns to the main menu from the level select menu.
    /// </summary>
    public void BackToMainMenu()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);

        // Deactivate LevelSelect and activate MainMenu
        _levelSelect.SetActive(false);
        _mainMenu.SetActive(true);
    }
}
