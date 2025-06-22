using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance;

    [SerializeField] private float startingTime = 600f; // 10 minutes (600 seconds)
    private float currentTime;
    private bool isRunning = false;

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
        currentTime = startingTime;
        isRunning = true;
    }

    private void Update()
    {
        if (isRunning && currentTime > 0)
        {
            currentTime -= Time.deltaTime;
        }
    }

    public float GetRemainingTime()
    {
        return Mathf.Max(0, currentTime);
    }

    public void SetRemainingTime(float time)
    {
        currentTime = time;
    }

    public void StopTimer()
    {
        isRunning = false;
    }
}
