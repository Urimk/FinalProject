using UnityEngine;
using UnityEngine.UI;

public class LevelEndTrigger : MonoBehaviour
{
    [SerializeField] private GameObject _usernameInputScreen; // Reference to the username input screen
    [SerializeField] private GameObject _winScreen;  // Reference to the WinScreen object
    [SerializeField] private Text _winScoreText;     // Reference to the Text component for the score
    [SerializeField] private Text _healthBonusText;  // Reference to the Text component for health bonus
    [SerializeField] private Text _timeBonusText;    // Reference to the Text component for time bonus
    [SerializeField] private Health _playerHealth;   // Reference to the player's health script
    [SerializeField] private Text _totalScoreText;  // Reference to the total score Text component

    private const int HealthBonusValue = 500; // Points per heart
    private const int TimeBonusValue = 5;     // Points per second left
    private bool _isWinScreenActive = false;   // Track if the win screen is active=
    private int _totalScore; // Store total score

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player touches the end object
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player reached the end!");

            // Stop the timer
            TimerManager.Instance.StopTimer();

            // Activate the WinScreen
            Time.timeScale = 0;
            _winScreen.SetActive(true);
            _isWinScreenActive = true; // Set this so Enter key works

            // Display results
            DisplayResults();
        }
    }

    private void DisplayResults()
    {
        int score = ScoreManager.Instance.score;
        int healthBonus = (int)_playerHealth.currentHealth * _playerHealth.getFirstHealth() * HealthBonusValue;
        int timeBonus = Mathf.FloorToInt(TimerManager.Instance.GetRemainingTime()) * TimeBonusValue;
        _totalScore = score + healthBonus + timeBonus; // Calculate total score

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
            _totalScoreText.text = "Total: " + _totalScore; // Set the total score text
        }
    }

    private void Update()
    {
        if (_isWinScreenActive && Input.GetKeyDown(KeyCode.Return))
        {
            ShowUsernameInputScreen();
        }
    }

    private void ShowUsernameInputScreen()
    {
        _winScreen.SetActive(false);
        _usernameInputScreen.SetActive(true);
        _isWinScreenActive = false;
    }

    public int GetTotalScore()
    {
        return _totalScore;
    }
}
