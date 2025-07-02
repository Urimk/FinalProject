using System.Collections;

using UnityEngine;

/// <summary>
/// Platform that falls when the player stands on it, then resets after a delay.
/// </summary>
public class FallingPlatform : MonoBehaviour
{
    // === Constants ===
    private const string PlayerTag = "Player";

    // === Serialized Fields ===
    public float fallDistance = 10f; // How far the platform falls
    public float fallTime = 5f;      // Time it takes to reach the lowest point
    public float stayTime = 2f;      // Time it stays before deactivating
    public GroundManager groundManager;
    public string playerTag = PlayerTag; // Tag of the player GameObject

    // === Private Fields ===
    private Vector3 _originalPosition;
    private bool _isFalling = false;
    private Transform _playerOnPlatform = null;
    private Vector3 _lastPlatformPosition;

    /// <summary>
    /// Unity Start callback. Stores the original position.
    /// </summary>
    void Start()
    {
        _originalPosition = transform.position;
    }

    /// <summary>
    /// Handles player entering the platform trigger.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            _playerOnPlatform = collision.transform;

            if (!_isFalling)
            {
                StartCoroutine(FallRoutine());
            }
        }
    }

    /// <summary>
    /// Handles player exiting the platform trigger.
    /// </summary>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag) && collision.transform == _playerOnPlatform)
        {
            _playerOnPlatform = null;
        }
    }

    /// <summary>
    /// Coroutine for falling and resetting the platform.
    /// </summary>
    IEnumerator FallRoutine()
    {
        _isFalling = true;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = _originalPosition + Vector3.down * fallDistance;
        float elapsedTime = 0f;

        _lastPlatformPosition = startPosition;

        while (elapsedTime < fallTime)
        {
            Vector3 currentTargetPos = Vector3.Lerp(startPosition, targetPosition, elapsedTime / fallTime);
            Vector3 deltaMovement = currentTargetPos - _lastPlatformPosition;

            transform.position = currentTargetPos;

            if (_playerOnPlatform != null)
            {
                _playerOnPlatform.position += deltaMovement;
            }

            _lastPlatformPosition = transform.position;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Vector3 finalDelta = targetPosition - _lastPlatformPosition;
        transform.position = targetPosition;

        if (_playerOnPlatform != null)
        {
            _playerOnPlatform.position += finalDelta;
        }

        yield return new WaitForSeconds(stayTime);

        if (groundManager != null)
        {
            groundManager.StartSorting();
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Resets the platform to its original state.
    /// </summary>
    public void ResetPlatform()
    {
        _isFalling = false;
        _playerOnPlatform = null;
        transform.position = _originalPosition;
        gameObject.SetActive(true);
    }
}
