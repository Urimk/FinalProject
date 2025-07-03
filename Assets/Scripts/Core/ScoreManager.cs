using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player's score, updates the UI, and provides methods to modify and retrieve the score.
/// Implements a singleton pattern for global access.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const string ScorePrefix = "SCORE: ";

    // ==================== Singleton ====================
    /// <summary>
    /// The global instance of the ScoreManager.
    /// </summary>
    public static ScoreManager Instance { get; private set; }

    // ==================== Private Fields ====================
    private int _score = 0;

    // ==================== Serialized Fields ====================
    [Header("UI Reference")]
    [Tooltip("Text component used to display the player's score.")]
    [SerializeField] private Text _scoreText;

    // ==================== Unity Lifecycle ====================
    private void Awake()
    {
        // Ensure there's only one instance of ScoreManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreText();
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Adds the specified amount to the current score and updates the UI.
    /// </summary>
    /// <param name="amount">The amount to add to the score.</param>
    public void AddScore(int amount)
    {
        _score += amount;
        UpdateScoreText();
    }

    /// <summary>
    /// Gets the current score.
    /// </summary>
    public int GetScore()
    {
        return _score;
    }

    /// <summary>
    /// Sets the score to a specific value and updates the UI.
    /// </summary>
    /// <param name="points">The new score value.</param>
    public void SetScore(int points)
    {
        _score = points;
        UpdateScoreText();
    }

    // ==================== Private Methods ====================
    /// <summary>
    /// Updates the score text UI element with the current score.
    /// </summary>
    private void UpdateScoreText()
    {
        if (_scoreText != null)
        {
            _scoreText.text = ScorePrefix + _score;
        }
    }
}
