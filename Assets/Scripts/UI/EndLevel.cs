using UnityEngine;
using UnityEngine.UI;

public class LevelEndTrigger : MonoBehaviour
{

    [SerializeField] private GameObject usernameInputScreen; // Reference to the username input screen
    [SerializeField] private GameObject winScreen;  // Reference to the WinScreen object
    [SerializeField] private Text winScoreText;     // Reference to the Text component for the score
    [SerializeField] private Text healthBonusText;  // Reference to the Text component for health bonus
    [SerializeField] private Text timeBonusText;    // Reference to the Text component for time bonus
    [SerializeField] private Health playerHealth;   // Reference to the player's health script
    [SerializeField] private Text totalScoreText;  // Reference to the total score Text component


    private const int HealthBonusValue = 500; // Points per heart
    private const int TimeBonusValue = 5;     // Points per second left
    private bool isWinScreenActive = false;   // Track if the win screen is active=
    private int totalScore; // Store total score


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
            winScreen.SetActive(true);
            isWinScreenActive = true; // Set this so Enter key works


            // Display results
            DisplayResults();
        }
    }

    private void DisplayResults()
    {
        int score = ScoreManager.Instance.score;
        int healthBonus = (int)playerHealth.currentHealth * playerHealth.getFirstHealth() * HealthBonusValue;
        int timeBonus = Mathf.FloorToInt(TimerManager.Instance.GetRemainingTime()) * TimeBonusValue;

        totalScore = score + healthBonus + timeBonus; // Calculate total score

        if (winScoreText != null)
        {
            winScoreText.text = "Score: " + score;
        }

        if (healthBonusText != null)
        {
            healthBonusText.text = "Health Bonus: " + healthBonus;
        }

        if (timeBonusText != null)
        {
            timeBonusText.text = "Time Bonus: " + timeBonus;
        }

        if (totalScoreText != null)
        {
            totalScoreText.text = "Total: " + totalScore; // Set the total score text
        }
    }
    private void Update()
    {
        if (isWinScreenActive && Input.GetKeyDown(KeyCode.Return))
        {
            ShowUsernameInputScreen();
        }
    }

    private void ShowUsernameInputScreen()
    {
        winScreen.SetActive(false);
        usernameInputScreen.SetActive(true);
        isWinScreenActive = false;
    }

    public int GetTotalScore()
    {
        return totalScore;
    }

}
