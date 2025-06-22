using System.Collections;

using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    public float fallDistance = 10f; // How far the platform falls
    public float fallTime = 5f;      // Time it takes to reach the lowest point
    public float stayTime = 2f;      // Time it stays before deactivating
    public GroundManager groundManager;
    public string playerTag = "Player"; // Tag of the player GameObject

    private Vector3 originalPosition;
    private bool isFalling = false;
    private Transform playerOnPlatform = null;
    private Vector3 lastPlatformPosition;

    void Start()
    {
        originalPosition = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            playerOnPlatform = collision.transform;

            if (!isFalling)
            {
                StartCoroutine(FallRoutine());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag) && collision.transform == playerOnPlatform)
        {
            playerOnPlatform = null;
        }
    }

    IEnumerator FallRoutine()
    {
        isFalling = true;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = originalPosition + Vector3.down * fallDistance;
        float elapsedTime = 0f;

        lastPlatformPosition = startPosition;

        while (elapsedTime < fallTime)
        {
            Vector3 currentTargetPos = Vector3.Lerp(startPosition, targetPosition, elapsedTime / fallTime);
            Vector3 deltaMovement = currentTargetPos - lastPlatformPosition;

            transform.position = currentTargetPos;

            if (playerOnPlatform != null)
            {
                playerOnPlatform.position += deltaMovement;
            }

            lastPlatformPosition = transform.position;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Vector3 finalDelta = targetPosition - lastPlatformPosition;
        transform.position = targetPosition;

        if (playerOnPlatform != null)
        {
            playerOnPlatform.position += finalDelta;
        }

        yield return new WaitForSeconds(stayTime);

        if (groundManager != null)
        {
            groundManager.StartSorting();
        }

        gameObject.SetActive(false);
    }

    // ✅ Add this method to reset the platform
    public void ResetPlatform()
    {
        isFalling = false;
        playerOnPlatform = null;
        transform.position = originalPosition;
        gameObject.SetActive(true);
    }
}
