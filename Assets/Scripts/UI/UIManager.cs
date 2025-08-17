using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Manages UI screens for game over, pause, win, and handles related input and sound.
/// </summary>
public class UIManager : MonoBehaviour
{
    // === Constants ===
    private const int MainMenuSceneIndex = 0;
    private const float VolumeStep = 0.2f;

    // === Singleton ===
    /// <summary>
    /// Singleton instance of the UIManager.
    /// </summary>
    public static UIManager Instance;

    // === Inspector Fields ===
    [Header("Game Over")]
    [Tooltip("GameObject for the game over screen.")]
    [FormerlySerializedAs("gameOverScreen")]
    [SerializeField] private GameObject _gameOverScreen;

    [Tooltip("Sound to play when game over occurs.")]
    [FormerlySerializedAs("gameOverSound")]
    [SerializeField] private AudioClip _gameOverSound;

    [Header("Pause")]
    [Tooltip("GameObject for the pause screen.")]
    [FormerlySerializedAs("pauseScreen")]
    [SerializeField] private GameObject _pauseScreen;

    [Tooltip("GameObject for the win screen.")]
    [FormerlySerializedAs("winScreen")]
    [SerializeField] private GameObject _winScreen;

    // === Private State ===
    private bool _isGamePaused = false;

    public bool IsGamePaused
    {
        get { return _isGamePaused; }
        set { _isGamePaused = value; }
    }


    // Properties for testing
    public GameObject GameOverScreen
    {
        get { return _gameOverScreen; }
        set { _gameOverScreen = value; }
    }
    public GameObject PauseScreen
    {
        get { return _pauseScreen; }
        set { _pauseScreen = value; }
    }

    /// <summary>
    /// Initializes singleton and disables all UI screens.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        _gameOverScreen.SetActive(false);
        _pauseScreen.SetActive(false);
        _winScreen.SetActive(false);
    }

    /// <summary>
    /// Handles pause input unless win screen is active.
    /// </summary>
    private void Update()
    {
        if (_winScreen.activeInHierarchy) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_pauseScreen.activeInHierarchy)
            {
                PauseGame(false);
            }
            else
            {
                PauseGame(true);
            }
        }
    }

    #region Game Over
    /// <summary>
    /// Shows the game over screen and plays sound.
    /// </summary>
    public void GameOver()
    {
        _gameOverScreen.SetActive(true);
        SoundManager.instance.PlaySound(_gameOverSound, gameObject);
        Time.timeScale = 0;
    }

    /// <summary>
    /// Restarts the current scene.
    /// </summary>
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;

    }

    /// <summary>
    /// Loads the main menu scene and changes music.
    /// </summary>
    public void MainMenu()
    {
        SoundManager.instance.ChangeMusic(SoundManager.instance.MenuMusic);
        _isGamePaused = false;
        Time.timeScale = 1;
        SceneManager.LoadScene(MainMenuSceneIndex);
    }

    /// <summary>
    /// Quits the application (and stops play mode in editor).
    /// </summary>
    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion

    #region Pause
    /// <summary>
    /// Shows or hides the pause screen and sets time scale.
    /// </summary>
    public void PauseGame(bool status)
    {
        _pauseScreen.SetActive(status);
        _isGamePaused = status;
        Time.timeScale = status ? 0 : 1;
    }

    /// <summary>
    /// Changes the sound volume by a fixed step.
    /// </summary>
    public void SoundVolume()
    {
        SoundManager.instance.ChangeSoundVolume(VolumeStep);
    }

    /// <summary>
    /// Changes the music volume by a fixed step.
    /// </summary>
    public void MusicVolume()
    {
        SoundManager.instance.ChangeMusicVolume(VolumeStep);
    }
    #endregion
}
