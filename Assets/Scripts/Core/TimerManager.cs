using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance;

    [SerializeField] private float _startingTime = 600f; // 10 minutes (600 seconds)
    private float _currentTime;
    private bool _isRunning = false;

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
