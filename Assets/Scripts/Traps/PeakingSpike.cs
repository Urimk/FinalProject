using UnityEngine;

public class PeakingSpike : MonoBehaviour
{
    [Header("Movement Settings")]
    public float basePosition;          // Y position when at base
    public float lowPosition;          // Y position when lowered
    public float baseHoldTime = 2f;          // Time to stay at base position
    public float lowHoldTime = 1f;           // Time to stay at low position
    public float moveSpeed = 5f;             // Speed of movement between positions
    public float initialDelay = 0f;          // Delay before starting the loop

    private Vector3 startPos;
    private Vector3 targetPos;
    private float timer;
    private bool isAtBase = true;
    private bool isMoving = false;
    private bool hasStarted = false;

    void Start()
    {
        // Store the initial position and set base position
        startPos = transform.position;
        //startPos.y = basePosition;
        transform.position = startPos;
        basePosition = startPos.y;
        lowPosition = basePosition - 1.7f;

        // Set initial timer to the delay value
        timer = initialDelay;
    }

    void Update()
    {
        if (!hasStarted)
        {
            // Wait for initial delay
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                hasStarted = true;
                timer = baseHoldTime; // Start with base hold time
            }
            return;
        }

        if (!isMoving)
        {
            // Count down the hold timer
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                // Start moving to the other position
                StartMovement();
            }
        }
        else
        {
            // Move towards target position
            MoveToTarget();
        }
    }

    void StartMovement()
    {
        isMoving = true;

        if (isAtBase)
        {
            // Moving from base to low position
            targetPos = startPos;
            targetPos.y = lowPosition;
        }
        else
        {
            // Moving from low to base position
            targetPos = startPos;
            targetPos.y = basePosition;
        }
    }

    void MoveToTarget()
    {
        // Move towards target position
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // Check if we've reached the target
        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            transform.position = targetPos;
            isMoving = false;
            isAtBase = !isAtBase;

            // Set the appropriate hold time
            timer = isAtBase ? baseHoldTime : lowHoldTime;
        }
    }
}
