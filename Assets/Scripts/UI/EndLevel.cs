using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the end-of-level trigger, win screen, and score calculation.
/// </summary>
public class LevelEndTrigger : MonoBehaviour
{
    // === Constants ===
    private const int HealthBonusValue = 500; // Points per heart
    private const int TimeBonusValue = 5;     // Points per second left
    private const string PlayerTag = "Player";

    // === Inspector Fields ===
    [Header("UI Screens")]
    [Tooltip("Reference to the username input screen GameObject.")]
    [SerializeField] private GameObject _usernameInputScreen;

    [Tooltip("Reference to the win screen GameObject.")]
    [SerializeField] private GameObject _winScreen;

    [Header("UI Texts")]
    [Tooltip("Text displaying the win score.")]
    [SerializeField] private Text _winScoreText;

    [Tooltip("Text displaying the health bonus.")]
    [SerializeField] private Text _healthBonusText;

    [Tooltip("Text displaying the time bonus.")]
    [SerializeField] private Text _timeBonusText;

    [Tooltip("Reference to the player's Health component.")]
    [SerializeField] private Health _playerHealth;

    [Tooltip("Text displaying the total score.")]
    [SerializeField] private Text _totalScoreText;

    // === Private State ===
    private bool _isWinScreenActive = false;
    private int _totalScore;

    /// <summary>
    /// Triggers the win screen and displays results when the player reaches the end.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag))
        {
            Debug.Log("Player reached the end!");
            TimerManager.Instance.StopTimer();
            Time.timeScale = 0;
            _winScreen.SetActive(true);
            _isWinScreenActive = true;
            DisplayResults();
        }
    }

    /// <summary>
    /// Calculates and displays the score, health bonus, and time bonus.
    /// </summary>
    private void DisplayResults()
    {
        int score = ScoreManager.Instance.GetScore();
        int healthBonus = (int)_playerHealth.currentHealth * _playerHealth.GetFirstHealth() * HealthBonusValue;
        int timeBonus = Mathf.FloorToInt(TimerManager.Instance.GetRemainingTime()) * TimeBonusValue;
        _totalScore = score + healthBonus + timeBonus;
        if (_winScoreText != null)
        {
            _winScoreText.text = "Score: " + score;
        }
        if (_healthBonusText != null)
        {
            _healthBonusText.text = "Health Bonus: " + healthBonus;
        }
        if (_timeBonusText != null)
        {
            _timeBonusText.text = "Time Bonus: " + timeBonus;
        }
        if (_totalScoreText != null)
        {
            _totalScoreText.text = "Total: " + _totalScore;
        }
    }

    /// <summary>
    /// Handles input to show the username input screen after winning.
    /// </summary>
    private void Update()
    {
        if (_isWinScreenActive && Input.GetKeyDown(KeyCode.Return))
        {
            ShowUsernameInputScreen();
        }
    }

    /// <summary>
    /// Shows the username input screen and hides the win screen.
    /// </summary>
    private void ShowUsernameInputScreen()
    {
        _winScreen.SetActive(false);
        _usernameInputScreen.SetActive(true);
        _isWinScreenActive = false;
    }

    /// <summary>
    /// Returns the total score for leaderboard submission.
    /// </summary>
    public int GetTotalScore()
    {
        return _totalScore;
    }
}
