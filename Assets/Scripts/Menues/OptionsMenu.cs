using UnityEngine;

/// <summary>
/// Handles the options menu, including difficulty selection and navigation.
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    // ==================== Constants ====================
    private const string DifficultyKey = "GameDifficulty";
    private const string EasyDifficulty = "Easy";
    private const string NormalDifficulty = "Normal";
    private const string HardDifficulty = "Hard";

    // ==================== Inspector Fields ====================
    [Tooltip("Sound effect to play when a button is clicked.")]
    [SerializeField] private AudioClip _buttonClickSound;
    [Tooltip("Reference to the MainMenu GameObject.")]
    [SerializeField] private GameObject _mainMenu;
    [Tooltip("Reference to the OptionsMenu GameObject.")]
    [SerializeField] private GameObject _optionsMenu;

    // ==================== Menu Navigation ====================
    /// <summary>
    /// Returns to the main menu from the options menu.
    /// </summary>
    public void BackToMainMenu()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _optionsMenu.SetActive(false);
        _mainMenu.SetActive(true);
    }

    /// <summary>
    /// Sets the game difficulty to Easy.
    /// </summary>
    public void SetEasyDifficulty()
    {
        SetDifficulty(EasyDifficulty);
    }

    /// <summary>
    /// Sets the game difficulty to Normal.
    /// </summary>
    public void SetNormalDifficulty()
    {
        SetDifficulty(NormalDifficulty);
    }

    /// <summary>
    /// Sets the game difficulty to Hard.
    /// </summary>
    public void SetHardDifficulty()
    {
        SetDifficulty(HardDifficulty);
    }

    // ==================== Utility ====================
    /// <summary>
    /// Sets the game difficulty and saves it in PlayerPrefs.
    /// </summary>
    /// <param name="difficulty">The difficulty to set.</param>
    private void SetDifficulty(string difficulty)
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        PlayerPrefs.SetString(DifficultyKey, difficulty);
        PlayerPrefs.Save();
        Debug.Log("Difficulty set to: " + difficulty);
    }
}
