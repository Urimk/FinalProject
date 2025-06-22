using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

public class LevelSelect : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickSound; // Assign the sound effect in the Inspector
    [SerializeField] private GameObject mainMenu; // Assign the MainMenu GameObject in the Inspector
    [SerializeField] private GameObject levelSelect; // Assign the LevelSelect GameObject in the Inspector

    private const string LevelKey = "SelectedLevel";
    private const string DifficultyKey = "GameDifficulty";

    // Methods for each level button
    public void SelectLevel(int levelIndex)
    {
        SoundManager.instance.PlaySound(buttonClickSound, gameObject);

        // Save the selected level
        PlayerPrefs.SetInt(LevelKey, levelIndex);

        // Get the currently selected difficulty (default to "Normal" if not set)
        string difficulty = PlayerPrefs.GetString(DifficultyKey, "Normal");

        // Save the full leaderboard name as "LevelX_Difficulty"
        string leaderboardName = $"Level{levelIndex}_{difficulty}";
        PlayerPrefs.SetString("LeaderboardName", leaderboardName);
        PlayerPrefs.Save(); // Ensure the data is saved

        Debug.Log($"Selected Level: {levelIndex}, Difficulty: {difficulty}, Leaderboard: {leaderboardName}");

        // Load the selected level
        SceneManager.LoadScene($"Level {levelIndex} - {difficulty}");
        SoundManager.instance.ChangeMusic(SoundManager.instance.level1Music);
    }

    // Back button functionality
    public void BackToMainMenu()
    {
        SoundManager.instance.PlaySound(buttonClickSound, gameObject);

        // Deactivate LevelSelect and activate MainMenu
        levelSelect.SetActive(false);
        mainMenu.SetActive(true);
    }
}
