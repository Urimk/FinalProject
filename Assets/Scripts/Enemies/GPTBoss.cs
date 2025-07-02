using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the GPT-powered boss challenge, including question/answer logic and player interaction.
/// </summary>
public class GPTBoss : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultTypingSpeed = 0.05f;
    private const float QuestionDelay = 2f;
    private const float PlayerDeathCheckDelay = 0.05f;
    private const int WrongAnswerDamage = 1;
    private const int QuestionTypeCount = 3;
    private const int DuplicateWordCount = 4;
    private const string PlayerTag = "Player";
    private const string CorrectMessage = "Correct!";
    private const string WrongMessage = "Wrong! Try again!";
    private const string DefeatedMessage = "You have defeated me!";
    private const string LostMessage = "You have lost!";
    private const string ChallengeIntro = "Prepare to face my challenge! Answer correctly, and you will survive.";

    // ==================== Serialized Fields ====================
    [SerializeField] private GameObject trophy;

    // ==================== Public Fields ====================
    public Text bossText;
    public TMP_InputField playerInput;
    public Health playerHealth;
    public float typingSpeed = DefaultTypingSpeed;

    // ==================== Private Fields ====================
    private Animator _bossAnimator;
    private Collider2D _bossCollider;
    private GPTManager _gptManager;
    private bool _challengeActive = false;
    private int _currentQuestionType = 0;
    private GameObject _currentPlayer;
    private string _currentQuestion;
    private HashSet<string> _askedQuestions = new HashSet<string>();
    private readonly string[] categories = new string[] {
        "Mathematics", "Geography", "History", "Science",
        "Literature", "Art", "Music", "Sports",
        "Technology", "Food", "Animals", "Mythology",
        "Movies", "Television", "Video Games"
    };
    private List<string> _availableCategories;

    /// <summary>
    /// Initializes references and sets up the challenge.
    /// </summary>
    private void Start()
    {
        _gptManager = FindObjectOfType<GPTManager>();
        playerInput.gameObject.SetActive(false);
        playerInput.characterLimit = 75;
        playerInput.onEndEdit.AddListener(delegate { OnEnterPressed(); });
        _bossAnimator = GetComponent<Animator>();
        _bossCollider = GetComponent<Collider2D>();
        InitializeAvailableCategories();
    }

    /// <summary>
    /// Initializes the list of available categories.
    /// </summary>
    private void InitializeAvailableCategories()
    {
        _availableCategories = new List<string>(categories);
    }

    /// <summary>
    /// Starts the challenge when the player enters the trigger.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag) && !_challengeActive)
        {
            _challengeActive = true;
            _currentPlayer = other.gameObject;
            StartVerbalChallenge();
        }
    }

    /// <summary>
    /// Begins the verbal challenge sequence.
    /// </summary>
    private void StartVerbalChallenge()
    {
        DisablePlayerControls();
        StartCoroutine(TypeText(ChallengeIntro, () =>
        {
            Invoke(nameof(AskQuestion), QuestionDelay);
        }));
    }

    /// <summary>
    /// Asks a new question from a random category.
    /// </summary>
    private void AskQuestion()
    {
        if (_availableCategories.Count == 0)
        {
            Debug.Log("All categories used. Resetting available categories.");
            InitializeAvailableCategories();
        }
        int randomIndex = UnityEngine.Random.Range(0, _availableCategories.Count);
        string category = _availableCategories[randomIndex];
        _availableCategories.RemoveAt(randomIndex);
        string basePrompt = GetPromptForType(_currentQuestionType);
        string prompt = $"{basePrompt} Category: {category}.";
        StartCoroutine(_gptManager.SendRequest(prompt, CheckAndDisplayQuestion));
    }

    /// <summary>
    /// Checks for duplicate questions and displays the new question.
    /// </summary>
    private void CheckAndDisplayQuestion(string question)
    {
        string firstFour = GetFirstNWords(question, DuplicateWordCount);
        if (_askedQuestions.Contains(firstFour))
        {
            Debug.LogWarning($"Duplicate question detected based on first {DuplicateWordCount} words: '{firstFour}'. Asking another question.");
            AskQuestion();
            return;
        }
        _askedQuestions.Add(firstFour);
        DisplayQuestion(question);
    }

    /// <summary>
    /// Gets the first N words of a question for duplicate detection.
    /// </summary>
    private string GetFirstNWords(string question, int n)
    {
        var words = question.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int count = Mathf.Min(n, words.Length);
        return string.Join(" ", words.Take(count));
    }

    /// <summary>
    /// Returns the prompt for the current question type.
    /// </summary>
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

    /// <summary>
    /// Displays the question and enables player input.
    /// </summary>
    private void DisplayQuestion(string question)
    {
        StartCoroutine(TypeText(question, () =>
        {
            _currentQuestion = question;
            playerInput.gameObject.SetActive(true);
            playerInput.interactable = true;
            playerInput.text = string.Empty;
            playerInput.ActivateInputField();
        }));
    }

    /// <summary>
    /// Checks the player's answer by sending it to the GPT API for validation.
    /// </summary>
    private void CheckAnswer()
    {
        string playerAnswer = playerInput.text.Trim();
        if (string.IsNullOrEmpty(playerAnswer)) return;
        playerInput.interactable = false;
        string validationPrompt = $"Question: {_currentQuestion}\nPlayer's answer: {playerAnswer}\n" +
                 "Is the player's answer correct? Respond with only 'yes' or 'no'.";
        StartCoroutine(_gptManager.SendRequest(validationPrompt, ProcessAnswer));
        playerInput.text = string.Empty;
    }

    /// <summary>
    /// Called when the player presses enter in the input field.
    /// </summary>
    private void OnEnterPressed()
    {
        if (playerInput.gameObject.activeSelf && playerInput.interactable)
        {
            CheckAnswer();
        }
    }

    /// <summary>
    /// Processes the GPT API's answer validation response.
    /// </summary>
    private void ProcessAnswer(string response)
    {
        if (response.ToLower().Contains("yes"))
        {
            bossText.text = CorrectMessage;
            _bossAnimator?.SetTrigger("3_Damaged");
            _currentQuestionType = (_currentQuestionType + 1) % QuestionTypeCount;
            if (_currentQuestionType == 0)
                BossDefeated();
            else
                Invoke(nameof(AskQuestion), QuestionDelay);
        }
        else
        {
            bossText.text = WrongMessage;
            playerHealth.TakeDamage(WrongAnswerDamage);
            Invoke(nameof(CheckPlayerDeath), PlayerDeathCheckDelay);
        }
    }

    /// <summary>
    /// Checks if the player is dead after a wrong answer.
    /// </summary>
    private void CheckPlayerDeath()
    {
        if (playerHealth.dead)
            PlayerDefeated();
        else
            Invoke(nameof(AskQuestion), QuestionDelay);
    }

    /// <summary>
    /// Handles boss defeat logic and disables the boss.
    /// </summary>
    private void BossDefeated()
    {
        bossText.text = DefeatedMessage;
        playerInput.gameObject.SetActive(false);
        EnablePlayerControls();
        _bossAnimator?.SetTrigger("4_Death");
        _bossCollider.enabled = false;
        Invoke(nameof(DisableBoss), QuestionDelay);
    }

    /// <summary>
    /// Disables the boss and shows the trophy if available.
    /// </summary>
    private void DisableBoss()
    {
        gameObject.SetActive(false);
        bossText.text = string.Empty;
        if (trophy != null)
        {
            trophy.SetActive(true);
        }
    }

    /// <summary>
    /// Handles player defeat logic.
    /// </summary>
    private void PlayerDefeated()
    {
        bossText.text = LostMessage;
        playerInput.gameObject.SetActive(false);
        EnablePlayerControls();
        Invoke(nameof(ClearBossText), QuestionDelay);
    }

    /// <summary>
    /// Clears the boss text.
    /// </summary>
    private void ClearBossText() => bossText.text = string.Empty;

    /// <summary>
    /// Disables player movement and attack controls.
    /// </summary>
    private void DisablePlayerControls()
    {
        if (_currentPlayer == null) return;
        var movement = _currentPlayer.GetComponent<PlayerMovement>();
        var attack = _currentPlayer.GetComponent<PlayerAttack>();
        var rb = _currentPlayer.GetComponent<Rigidbody2D>();
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
    }

    /// <summary>
    /// Enables player movement and attack controls.
    /// </summary>
    private void EnablePlayerControls()
    {
        if (_currentPlayer == null) return;
        var movement = _currentPlayer.GetComponent<PlayerMovement>();
        var attack = _currentPlayer.GetComponent<PlayerAttack>();
        if (movement != null) movement.enabled = true;
        if (attack != null) attack.enabled = true;
    }

    /// <summary>
    /// Types out the given message character by character, then invokes onComplete.
    /// </summary>
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
