using UnityEngine;

/// <summary>
/// Manages a countdown timer for the game, providing start, stop, and time retrieval functionality.
/// Implements a singleton pattern for global access.
/// </summary>
public class TimerManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultStartingTime = 600f; // 10 minutes (600 seconds)

    // ==================== Singleton ====================
    /// <summary>
    /// The global instance of the TimerManager.
    /// </summary>
    public static TimerManager Instance { get; private set; }

    // ==================== Serialized Fields ====================
    [Header("Timer Settings")]
    [Tooltip("The starting time for the timer in seconds.")]
    [SerializeField] private float _startingTime = DefaultStartingTime;

    // ==================== Private Fields ====================
    private float _currentTime;
    private bool _isRunning = false;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Awake callback. Sets up the singleton instance.
    /// </summary>
    private void Awake()
    {
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

    /// <summary>
    /// Unity Start callback. Initializes the timer.
    /// </summary>
    private void Start()
    {
        _currentTime = _startingTime;
        _isRunning = true;
    }

    /// <summary>
    /// Unity Update callback. Decrements the timer if running.
    /// </summary>
    private void Update()
    {
        if (_isRunning && _currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
        }
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Gets the remaining time on the timer (never less than zero).
    /// </summary>
    /// <returns>The remaining time in seconds.</returns>
    public float GetRemainingTime()
    {
        return Mathf.Max(0, _currentTime);
    }

    /// <summary>
    /// Sets the remaining time on the timer.
    /// </summary>
    /// <param name="time">The new time in seconds.</param>
    public void SetRemainingTime(float time)
    {
        _currentTime = time;
    }

    /// <summary>
    /// Stops the timer from counting down.
    /// </summary>
    public void StopTimer()
    {
        _isRunning = false;
    }
}
