using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the main menu navigation and actions.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    // ==================== Inspector Fields ====================
    [Tooltip("Sound effect to play when a button is clicked.")]
    [SerializeField] private AudioClip _buttonClickSound;
    [Tooltip("Reference to the MainMenu GameObject.")]
    [SerializeField] private GameObject _mainMenu;
    [Tooltip("Reference to the LevelSelect GameObject.")]
    [SerializeField] private GameObject _levelSelect;
    [Tooltip("Reference to the OptionsMenu GameObject.")]
    [SerializeField] private GameObject _optionsMenu;
    [Tooltip("Reference to the LeaderboardMenu GameObject.")]
    [SerializeField] private GameObject _leaderboardMenu;

    // ==================== Menu Navigation ====================
    /// <summary>
    /// Navigates to the level select menu.
    /// </summary>
    public void Play()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _mainMenu.SetActive(false);
        _levelSelect.SetActive(true);
    }

    /// <summary>
    /// Opens the options menu.
    /// </summary>
    public void OpenOptions()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _mainMenu.SetActive(false);
        _optionsMenu.SetActive(true);
    }

    /// <summary>
    /// Opens the leaderboard menu.
    /// </summary>
    public void OpenLeaderboard()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        _mainMenu.SetActive(false);
        _leaderboardMenu.SetActive(true);
    }

    /// <summary>
    /// Quits the game application.
    /// </summary>
    public void QuitGame()
    {
        SoundManager.instance.PlaySound(_buttonClickSound, gameObject);
        Application.Quit();
    }
}
