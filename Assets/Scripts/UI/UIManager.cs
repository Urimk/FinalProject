using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    private bool _isGamePaused = false;
    [Header("Game Over")]
    [SerializeField] private GameObject _gameOverScreen;
    [SerializeField] private AudioClip _gameOverSound;
    [Header("Pause")]
    [SerializeField] private GameObject _pauseScreen;
    [SerializeField] private GameObject _winScreen; // Reference to WinScreen

    // For testing
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
        _winScreen.SetActive(false); // Make sure WinScreen starts disabled
    }

    private void Update()
    {
        // Only listen for Esc key if WinScreen is not active
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
    public void GameOver()
    {
        _gameOverScreen.SetActive(true);
        SoundManager.instance.PlaySound(_gameOverSound, gameObject);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        SoundManager.instance.ChangeMusic(SoundManager.instance.menuMusic);
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion

    #region Pause
    public void PauseGame(bool status)
    {
        _pauseScreen.SetActive(status);
        _isGamePaused = status;
        if (status)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    public bool IsGamePaused()
    {
        return _isGamePaused;
    }

    public void SoundVolume()
    {
        SoundManager.instance.ChangeSoundVolume(0.2f);
    }

    public void MusicVolume()
    {
        SoundManager.instance.ChangeMusicVolume(0.2f);
    }
    #endregion
}
