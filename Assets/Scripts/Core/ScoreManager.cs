using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private int _score = 0;
    [SerializeField] public Text _scoreText;

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

    public void AddScore(int amount)
    {
        _score += amount;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (_scoreText != null)
        {
            _scoreText.text = "SCORE: " + _score;
        }
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
}
