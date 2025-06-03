using UnityEngine;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickSound; // Assign the sound effect in the Inspector
    [SerializeField] private GameObject mainMenu; // Assign the MainMenu GameObject in the Inspector
    [SerializeField] private GameObject optionsMenu; // Assign the OptionsMenu GameObject in the Inspector

    private const string DifficultyKey = "GameDifficulty"; // Key for storing difficulty in PlayerPrefs

    // Back button functionality
    public void BackToMainMenu()
    {
        SoundManager.instance.PlaySound(buttonClickSound);

        // Deactivate OptionsMenu and activate MainMenu
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    // Methods to set difficulty
    public void SetEasyDifficulty()
    {
        SetDifficulty("Easy");
    }

    public void SetNormalDifficulty()
    {
        SetDifficulty("Normal");
    }

    public void SetHardDifficulty()
    {
        SetDifficulty("Hard");
    }

    private void SetDifficulty(string difficulty)
    {
        SoundManager.instance.PlaySound(buttonClickSound);
        PlayerPrefs.SetString(DifficultyKey, difficulty); // Save difficulty in PlayerPrefs
        PlayerPrefs.Save(); // Ensure it gets saved
        Debug.Log("Difficulty set to: " + difficulty);
    }
}
