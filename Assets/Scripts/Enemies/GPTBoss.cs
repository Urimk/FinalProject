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
    public TMP_InputField playerInput;
    public Health playerHealth;
    private Animator _bossAnimator;
    private Collider2D _bossCollider;
    private GPTManager _gptManager;
    private bool _challengeActive = false;
    public float typingSpeed = 0.05f;
    private int _currentQuestionType = 0;
    private GameObject _currentPlayer;
    private string _currentQuestion;
    private HashSet<string> _askedQuestions = new HashSet<string>();

    // Up to 15 categories
    private readonly string[] categories = new string[] {
        "Mathematics", "Geography", "History", "Science",
        "Literature", "Art", "Music", "Sports",
        "Technology", "Food", "Animals", "Mythology",
        "Movies", "Television", "Video Games"
    };

    private List<string> _availableCategories; // List to track categories that haven't been used

    private void Start()
    {
        _gptManager = FindObjectOfType<GPTManager>();

        playerInput.gameObject.SetActive(false);
        playerInput.characterLimit = 75;
        playerInput.onEndEdit.AddListener(delegate { OnEnterPressed(); });

        _bossAnimator = GetComponent<Animator>();
        _bossCollider = GetComponent<Collider2D>();

        // Initialize the list of available categories
        InitializeAvailableCategories();
    }

    private void InitializeAvailableCategories()
    {
        _availableCategories = new List<string>(categories);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_challengeActive)
        {
            _challengeActive = true;
            _currentPlayer = other.gameObject;
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

    private void CheckAndDisplayQuestion(string question)
    {
        string firstFour = GetFirstFourWords(question);
        if (_askedQuestions.Contains(firstFour))
        {
            Debug.LogWarning($"Duplicate question detected based on first four words: '{firstFour}'. Asking another question.");
            AskQuestion();
            return;
        }
        _askedQuestions.Add(firstFour);
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
            _currentQuestion = question;
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
        playerInput.interactable = false;
        string validationPrompt = $"Question: {_currentQuestion}\nPlayer's answer: {playerAnswer}\n" +
                 "Is the player's answer correct? Respond with only 'yes' or 'no'.";
        StartCoroutine(_gptManager.SendRequest(validationPrompt, ProcessAnswer));
        playerInput.text = string.Empty;
    }

    private void OnEnterPressed()
    {
        if (playerInput.gameObject.activeSelf && playerInput.interactable)
        {
            CheckAnswer();
        }
    }

    private void ProcessAnswer(string response)
    {
        if (response.ToLower().Contains("yes"))
        {
            bossText.text = "Correct!";
            _bossAnimator?.SetTrigger("3_Damaged");
            _currentQuestionType = (_currentQuestionType + 1) % 3;
            if (_currentQuestionType == 0)
                BossDefeated();
            else
                Invoke(nameof(AskQuestion), 2f);
        }
        else
        {
            bossText.text = "Wrong! Try again!";
            playerHealth.TakeDamage(1);
            Invoke(nameof(CheckPlayerDeath), 0.05f);
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
        _bossAnimator?.SetTrigger("4_Death");
        _bossCollider.enabled = false;
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
        if (_currentPlayer == null) return;
        var movement = _currentPlayer.GetComponent<PlayerMovement>();
        var attack = _currentPlayer.GetComponent<PlayerAttack>();
        var rb = _currentPlayer.GetComponent<Rigidbody2D>();
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
    }

    private void EnablePlayerControls()
    {
        if (_currentPlayer == null) return;
        var movement = _currentPlayer.GetComponent<PlayerMovement>();
        var attack = _currentPlayer.GetComponent<PlayerAttack>();
        if (movement != null) movement.enabled = true;
        if (attack != null) attack.enabled = true;
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
