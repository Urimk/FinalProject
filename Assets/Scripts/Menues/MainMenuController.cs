using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] public AudioClip buttonClickSound; // Assign the sound effect in the Inspector
    [SerializeField] private GameObject mainMenu; // Assign the MainMenu GameObject in the Inspector
    [SerializeField] private GameObject levelSelect; // Assign the LevelSelect GameObject in the Inspector
    [SerializeField] private GameObject optionsMenu; // Assign the optionsMenu GameObject in the Inspector
    [SerializeField] private GameObject leaderboardMenu; // Assign the lbMenu GameObject in the Inspector

    public void Play()
    {
        SoundManager.instance.PlaySound(buttonClickSound, gameObject);
        // Deactivate MainMenu and activate LevelSelect
        mainMenu.SetActive(false);
        levelSelect.SetActive(true);
    }

    public void OpenOptions()
    {
        SoundManager.instance.PlaySound(buttonClickSound, gameObject);
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void OpenLeaderboard()
    {
        SoundManager.instance.PlaySound(buttonClickSound, gameObject);
        mainMenu.SetActive(false);
        leaderboardMenu.SetActive(true);
    }

    public void QuitGame()
    {
        SoundManager.instance.PlaySound(buttonClickSound, gameObject);
        Application.Quit();
    }
}
