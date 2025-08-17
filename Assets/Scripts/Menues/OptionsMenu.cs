using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private GameObject[] _outlines;

    private void Start()
    {
        string savedDifficulty = PlayerPrefs.GetString(DifficultyKey, NormalDifficulty); // Default to Normal

        switch (savedDifficulty)
        {
            case EasyDifficulty:
                UpdateDifficultyOutline(0);
                break;
            case NormalDifficulty:
                UpdateDifficultyOutline(1);
                break;
            case HardDifficulty:
                UpdateDifficultyOutline(2);
                break;
            default:
                Debug.LogWarning("Unknown saved difficulty: " + savedDifficulty);
                UpdateDifficultyOutline(1); // Fallback to Normal
                break;
        }
    }


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
        UpdateDifficultyOutline(0);

    }

    /// <summary>
    /// Sets the game difficulty to Normal.
    /// </summary>
    public void SetNormalDifficulty()
    {
        SetDifficulty(NormalDifficulty);
        UpdateDifficultyOutline(1);

    }

    /// <summary>
    /// Sets the game difficulty to Hard.
    /// </summary>
    public void SetHardDifficulty()
    {
        SetDifficulty(HardDifficulty);
        UpdateDifficultyOutline(2);

    }

    /// <summary>
    /// Enables only the outline at the given index and disables the others.
    /// </summary>
    /// <param name="selectedIndex">The index of the selected difficulty (0 = Easy, 1 = Normal, 2 = Hard)</param>
    private void UpdateDifficultyOutline(int selectedIndex)
    {
        for (int i = 0; i < _outlines.Length; i++)
        {
            Outline outline = _outlines[i].GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = (i == selectedIndex);
            }
        }
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
