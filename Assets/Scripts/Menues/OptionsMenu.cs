using UnityEngine;

/// <summary>
/// Handles the options menu, including difficulty selection and navigation.
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    private const string DifficultyKey = "GameDifficulty";
    private const string EasyDifficulty = "Easy";
    private const string NormalDifficulty = "Normal";
    private const string HardDifficulty = "Hard";

    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _optionsMenu;

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

    /// <summary>
    /// Sets the game difficulty and saves it in PlayerPrefs.
    /// </summary>
    private void SetDifficulty(string difficulty)
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        PlayerPrefs.SetString(DifficultyKey, difficulty);
        PlayerPrefs.Save();
        Debug.Log("Difficulty set to: " + difficulty);
    }
}
