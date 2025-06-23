using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] public AudioClip ButtonClickSound;
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private GameObject _levelSelect;
    [SerializeField] private GameObject _optionsMenu;
    [SerializeField] private GameObject _leaderboardMenu;

    public void Play()
    {
        SoundManager.instance.PlaySound(ButtonClickSound, gameObject);
        _mainMenu.SetActive(false);
        _levelSelect.SetActive(true);
    }

    public void OpenOptions()
    {
        SoundManager.instance.PlaySound(ButtonClickSound, gameObject);
        _mainMenu.SetActive(false);
        _optionsMenu.SetActive(true);
    }

    public void OpenLeaderboard()
    {
        SoundManager.instance.PlaySound(ButtonClickSound, gameObject);
        _mainMenu.SetActive(false);
        _leaderboardMenu.SetActive(true);
    }

    public void QuitGame()
    {
        SoundManager.instance.PlaySound(ButtonClickSound, gameObject);
        Application.Quit();
    }
}
