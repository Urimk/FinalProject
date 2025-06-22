using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class GPTBoss : MonoBehaviour
{
    [SerializeField] private GameObject trophy;
    public Text bossText;
    private Animator bossAnimator;
    private Collider2D bossCollider;
    public TMP_InputField playerInput;
    private GPTManager gptManager;
    private bool challengeActive = false;
    public float typingSpeed = 0.05f;
    private int currentQuestionType = 0;
    public Health playerHealth;
    private GameObject currentPlayer;
    private string currentQuestion;
    private HashSet<string> askedQuestions = new HashSet<string>();

    // Up to 15 categories
    private readonly string[] categories = new string[] {
    "Mathematics", "Geography", "History", "Science",
    "Literature", "Art", "Music", "Sports",
    "Technology", "Food", "Animals", "Mythology",
    "Movies", "Television", "Video Games"
  };

    private List<string> availableCategories; // List to track categories that haven't been used

    private void Start()
    {
        gptManager = FindObjectOfType<GPTManager>();

        playerInput.gameObject.SetActive(false);
        playerInput.characterLimit = 75;
        playerInput.onEndEdit.AddListener(delegate { OnEnterPressed(); });

        bossAnimator = GetComponent<Animator>();
        bossCollider = GetComponent<Collider2D>();

        // Initialize the list of available categories
        InitializeAvailableCategories();
    }

    private void InitializeAvailableCategories()
    {
        availableCategories = new List<string>(categories);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !challengeActive)
        {
            challengeActive = true;
            currentPlayer = other.gameObject;
            StartVerbalChallenge();
        }
    }

    private void StartVerbalChallenge()
    {
        DisablePlayerControls();
        StartCoroutine(TypeText("Prepare to face my challenge! Answer correctly, and you will survive.", () =>
        {
            Invoke(nameof(AskQuestion), 2f);
        }));
    }

    private void AskQuestion()
    {
        // If all categories have been used, reset the available categories
        if (availableCategories.Count == 0)
        {
            Debug.Log("All categories used. Resetting available categories.");
            InitializeAvailableCategories();
        }

        // pick a random category from the available ones
        int randomIndex = UnityEngine.Random.Range(0, availableCategories.Count);
        string category = availableCategories[randomIndex];

        // Remove the chosen category from the available list
        availableCategories.RemoveAt(randomIndex);

        string basePrompt = GetPromptForType(currentQuestionType);
        string prompt = $"{basePrompt} Category: {category}.";

        StartCoroutine(gptManager.SendRequest(prompt, CheckAndDisplayQuestion));
    }

    private void CheckAndDisplayQuestion(string question)
    {
        string firstFour = GetFirstFourWords(question);
        if (askedQuestions.Contains(firstFour))
        {
            Debug.LogWarning($"Duplicate question detected based on first four words: '{firstFour}'. Asking another question.");
            AskQuestion();
            return;
        }

        askedQuestions.Add(firstFour);
        DisplayQuestion(question);
    }

    private string GetFirstFourWords(string question)
    {
        var words = question.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int count = Mathf.Min(4, words.Length);
        return string.Join(" ", words.Take(count));
    }

    private string GetPromptForType(int type)
    {
        switch (type)
        {
            case 0:
                return "Generate only a general knowledge question without any introduction or explanation. Start directly with the question. The question should be a yes or no question.";
            case 1:
                return "Generate only a general knowledge question without any introduction or explanation. Start directly with the question. Give 4 options for answers, only 1 is correct.";
            case 2:
            default:
                return "Generate only an EASY general knowledge question without any introduction or explanation. Start directly with the question.";
        }
    }

    private void DisplayQuestion(string question)
    {
        StartCoroutine(TypeText(question, () =>
        {
            currentQuestion = question;
            playerInput.gameObject.SetActive(true);
            playerInput.interactable = true;
            playerInput.text = string.Empty;
            playerInput.ActivateInputField();
        }));
    }

    private void CheckAnswer()
    {
        string playerAnswer = playerInput.text.Trim();
        if (string.IsNullOrEmpty(playerAnswer)) return;

        playerInput.interactable = false; // Disable input field while checking answer

        string validationPrompt = $"Question: {currentQuestion}\nPlayer's answer: {playerAnswer}\n" +
                 "Is the player's answer correct? Respond with only 'yes' or 'no'.";
        StartCoroutine(gptManager.SendRequest(validationPrompt, ProcessAnswer));

        playerInput.text = string.Empty;
    }

    private void OnEnterPressed()
    {
        // Only process if the input field is active and interactable
        if (playerInput.gameObject.activeSelf && playerInput.interactable)
        {
            // This listener fires on "Deselect" or "EndEdit".
            // We only want to check the answer when Enter/Return is pressed.
            // A more robust solution would be to check for the key press directly,
            // or use a separate button, but for this example, we'll rely on EndEdit.
            // Note: Relying solely on OnEndEdit might cause unexpected behavior
            // if the player clicks outside the input field.
            CheckAnswer();
        }
    }

    private void ProcessAnswer(string response)
    {
        if (response.ToLower().Contains("yes"))
        {
            bossText.text = "Correct!";
            bossAnimator?.SetTrigger("3_Damaged");
            currentQuestionType = (currentQuestionType + 1) % 3;

            if (currentQuestionType == 0) // Cycle through question types, boss defeated after completing a cycle
                BossDefeated();
            else
                Invoke(nameof(AskQuestion), 2f);
        }
        else
        {
            bossText.text = "Wrong! Try again!";
            playerHealth.TakeDamage(1);
            Invoke(nameof(CheckPlayerDeath), 0.05f); // Check for death very soon after taking damage
        }
    }

    private void CheckPlayerDeath()
    {
        if (playerHealth.dead)
            PlayerDefeated();
        else
            Invoke(nameof(AskQuestion), 2f);
    }

    private void BossDefeated()
    {
        bossText.text = "You have defeated me!";
        playerInput.gameObject.SetActive(false);
        EnablePlayerControls();
        bossAnimator?.SetTrigger("4_Death");
        bossCollider.enabled = false;
        Invoke(nameof(DisableBoss), 2f);
    }

    private void DisableBoss()
    {
        gameObject.SetActive(false);
        bossText.text = string.Empty;
        if (trophy != null)
        {
            trophy.SetActive(true);
        }
    }

    private void PlayerDefeated()
    {
        bossText.text = "You have lost!";
        playerInput.gameObject.SetActive(false);
        EnablePlayerControls();
        Invoke(nameof(ClearBossText), 2f);
    }

    private void ClearBossText() => bossText.text = string.Empty;

    private void DisablePlayerControls()
    {
        if (currentPlayer == null) return;
        var movement = currentPlayer.GetComponent<PlayerMovement>();
        var attack = currentPlayer.GetComponent<PlayerAttack>();
        var rb = currentPlayer.GetComponent<Rigidbody2D>();
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
    }

    private void EnablePlayerControls()
    {
        if (currentPlayer == null) return;
        var movement = currentPlayer.GetComponent<PlayerMovement>();
        var attack = currentPlayer.GetComponent<PlayerAttack>();
        if (movement != null) movement.enabled = true;
        if (attack != null) attack.enabled = true;
        challengeActive = false;
    }

    private IEnumerator TypeText(string message, Action onComplete)
    {
        bossText.text = string.Empty;
        foreach (char c in message)
        {
            bossText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        onComplete?.Invoke();
    }
}
