using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const string ScorePrefix = "SCORE: ";

    // ==================== Singleton ====================
    public static ScoreManager Instance;

    // ==================== Private Fields ====================
    private int _score = 0;

    // ==================== Serialized Fields ====================
    [SerializeField] public Text _scoreText;

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
    public void AddScore(int amount)
    {
        _score += amount;
        UpdateScoreText();
    }

    public int GetScore()
    {
        return _score;
    }

    public void SetScore(int points)
    {
        _score = points;
        UpdateScoreText();
    }

    // ==================== Private Methods ====================
    private void UpdateScoreText()
    {
        if (_scoreText != null)
        {
            _scoreText.text = ScorePrefix + _score;
        }
    }
}
