using System.Collections;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Platform that falls when the player stands on it, then resets after a delay.
/// </summary>
public class FallingPlatform : MonoBehaviour
{
    // ==================== Constants ====================
    private const string PlayerTag = "Player";

    // ==================== Inspector Fields ====================
    [Tooltip("How far the platform falls.")]
    [FormerlySerializedAs("fallDistance")]
    [SerializeField] private float _fallDistance = 10f;
    [Tooltip("Time it takes to reach the lowest point.")]
    [FormerlySerializedAs("fallTime")]
    [SerializeField] private float _fallTime = 5f;
    [Tooltip("Time the platform stays before deactivating.")]
    [FormerlySerializedAs("stayTime")]
    [SerializeField] private float _stayTime = 2f;
    [Tooltip("Reference to the GroundManager for sorting.")]
    [FormerlySerializedAs("groundManager")]
    [SerializeField] private GroundManager _groundManager;
    [Tooltip("Tag of the player GameObject.")]
    [FormerlySerializedAs("playerTag")]
    [SerializeField] private string _playerTag = PlayerTag;

    // ==================== Private Fields ====================
    private Vector3 _originalPosition;
    private bool _isFalling = false;
    private Transform _playerOnPlatform = null;
    private Vector3 _lastPlatformPosition;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Start callback. Stores the original position.
    /// </summary>
    private void Start()
    {
        _originalPosition = transform.position;
    }

    // ==================== Platform Logic ====================
    /// <summary>
    /// Handles player entering the platform trigger.
    /// </summary>
    /// <param name="collision">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(_playerTag))
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
    /// <param name="collision">The collider that exited the trigger.</param>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(_playerTag) && collision.transform == _playerOnPlatform)
        {
            _playerOnPlatform = null;
        }
    }

    /// <summary>
    /// Coroutine for falling and resetting the platform.
    /// </summary>
    private IEnumerator FallRoutine()
    {
        _isFalling = true;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = _originalPosition + Vector3.down * _fallDistance;
        float elapsedTime = 0f;
        _lastPlatformPosition = startPosition;
        while (elapsedTime < _fallTime)
        {
            Vector3 currentTargetPos = Vector3.Lerp(startPosition, targetPosition, elapsedTime / _fallTime);
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
        yield return new WaitForSeconds(_stayTime);
        if (_groundManager != null)
        {
            _groundManager.StartSorting();
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
