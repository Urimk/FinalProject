using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

/// <summary>
/// Handles level selection and navigation from the level select menu.
/// </summary>
public class LevelSelect : MonoBehaviour
{
    // ==================== Constants ====================
    private const string LevelKey = "SelectedLevel";
    private const string DifficultyKey = "GameDifficulty";
    private const string LeaderboardNameKey = "LeaderboardName";
    private const string DefaultDifficulty = "Normal";
    private const string LevelSceneFormat = "Level {0} - {1}";

    // ==================== Inspector Fields ====================
    [Tooltip("Sound effect to play when a button is clicked.")]
    [SerializeField] private AudioClip _buttonClickSound;
    [Tooltip("Reference to the MainMenu GameObject.")]
    [SerializeField] private GameObject _mainMenu;
    [Tooltip("Reference to the LevelSelect GameObject.")]
    [SerializeField] private GameObject _levelSelect;

    // ==================== Level Selection Logic ====================
    /// <summary>
    /// Selects a level, saves preferences, and loads the scene.
    /// </summary>
    /// <param name="levelIndex">The index of the selected level.</param>
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
        SoundManager.instance.ChangeMusic(SoundManager.instance.Level1Music);
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
