using UnityEngine;

public class TimerManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultStartingTime = 600f; // 10 minutes (600 seconds)

    // ==================== Singleton ====================
    public static TimerManager Instance;

    // ==================== Serialized Fields ====================
    [SerializeField] private float _startingTime = DefaultStartingTime;

    // ==================== Private Fields ====================
    private float _currentTime;
    private bool _isRunning = false;

    // ==================== Unity Lifecycle ====================
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

    private void Start()
    {
        _currentTime = _startingTime;
        _isRunning = true;
    }

    private void Update()
    {
        if (_isRunning && _currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
        }
    }

    // ==================== Public Methods ====================
    public float GetRemainingTime()
    {
        return Mathf.Max(0, _currentTime);
    }

    public void SetRemainingTime(float time)
    {
        _currentTime = time;
    }

    public void StopTimer()
    {
        _isRunning = false;
    }
}
