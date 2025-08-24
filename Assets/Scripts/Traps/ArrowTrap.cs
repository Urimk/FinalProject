using System.Collections; // Required for Coroutines

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Controls an arrow trap that periodically fires arrows and can optionally rotate its firepoint.
/// </summary>
public class ArrowTrap : MonoBehaviour
{
    // === Constants ===
    private const string PlayerTag = "Player";
    private const float DefaultSpeed = 5f;
    private const float DefaultRotationAngle = 40f;
    private const float DefaultRotationDuration = 3f;

    // === Inspector Fields ===
    [Header("Attack Settings")]
    [Tooltip("Cooldown time in seconds between arrow attacks.")]
    [FormerlySerializedAs("attackCooldown")]
    [SerializeField] private float _attackCooldown;

    [Header("Firepoint Settings")]
    [Tooltip("Transform from which arrows are fired.")]
    [FormerlySerializedAs("firepoint")]
    [SerializeField] private Transform _firepoint;

    [Tooltip("Array of arrow GameObjects used as a pool for firing.")]
    [FormerlySerializedAs("arrows")]
    [SerializeField] private GameObject[] _arrows;

    [Tooltip("Speed at which arrows are fired.")]
    [FormerlySerializedAs("speed")]
    [SerializeField] private float _speed = DefaultSpeed;

    [Tooltip("If true, arrows are marked as 'coming out' for special behavior.")]
    [FormerlySerializedAs("isComingOut")]
    [SerializeField] private bool _isComingOut = false;

    [Header("Sound Settings")]
    [Tooltip("Sound to play when an arrow is fired.")]
    [FormerlySerializedAs("arrowSound")]
    [SerializeField] private AudioClip _arrowSound;

    [Header("Rotation Settings")]
    [Tooltip("If true, the firepoint will rotate back and forth.")]
    [FormerlySerializedAs("rotateFirepoint")]
    [SerializeField] private bool _rotateFirepoint = false;

    [Tooltip("Maximum angle to rotate the firepoint.")]
    [FormerlySerializedAs("rotationAngle")]
    [SerializeField] private float _rotationAngle = DefaultRotationAngle;

    [Tooltip("Duration in seconds for each rotation segment.")]
    [FormerlySerializedAs("rotationDuration")]
    [SerializeField] private float _rotationDuration = DefaultRotationDuration;
    // === Private State ===
    private float _cooldownTimer;
    private bool _rotatingForward = true;
    private Coroutine _rotationCoroutine;

    /// <summary>
    /// Fires an arrow from the trap and plays the arrow sound.
    /// </summary>
    private void Attack()
    {
        _cooldownTimer = 0;
        if (SoundManager.instance != null && _arrowSound != null)
        {
            SoundManager.instance.PlaySound(_arrowSound, gameObject);
        }
        
        int idx = FindArrow();
        if (idx == -1) return; // No available arrows
        
        GameObject arrowObj = _arrows[idx];
        EnemyProjectile projectile = arrowObj.GetComponent<EnemyProjectile>();
        
        if (projectile != null)
        {
            Vector2 direction = _firepoint.right;
            projectile.SetSpeed(_speed);
            projectile.LaunchFromPosition(_firepoint.position, direction, -1f, _isComingOut);
            
            Debug.Log($"[ArrowTrap] Fired arrow {idx} from position {_firepoint.position} in direction {direction}");
        }
        else
        {
            Debug.LogError($"[ArrowTrap] Arrow {idx} missing EnemyProjectile component!");
        }
    }

    /// <summary>
    /// Finds an available arrow in the pool.
    /// </summary>
    /// <returns>Index of the available arrow, or -1 if none available.</returns>
    private int FindArrow()
    {
        int availableCount = 0;
        for (int i = 0; i < _arrows.Length; i++)
        {
            if (_arrows[i] != null && !_arrows[i].activeInHierarchy)
            {
                availableCount++;
                return i;
            }
        }
        
        if (availableCount == 0)
        {
            Debug.LogWarning($"[ArrowTrap] No available arrows in pool! All {_arrows.Length} arrows are active.");
        }
        
        return -1; // No available arrows
    }

    /// <summary>
    /// Handles cooldown and rotation logic each frame.
    /// </summary>
    private void Update()
    {
        _cooldownTimer += Time.deltaTime;
        if (_cooldownTimer >= _attackCooldown)
        {
            Attack();
        }

        if (_rotateFirepoint)
        {
            if (_rotationCoroutine == null)
            {
                _rotationCoroutine = StartCoroutine(RotateFirepointCoroutine());
            }
        }
        else if (_rotationCoroutine != null)
        {
            StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine to rotate the firepoint back and forth between angles.
    /// </summary>
    private IEnumerator RotateFirepointCoroutine()
    {
        float originalX = _firepoint.localEulerAngles.x;
        float originalY = _firepoint.localEulerAngles.y;
        float slerpOscillationCenterZ = _firepoint.localEulerAngles.z;

        while (_rotateFirepoint)
        {
            bool useAngleLerping = false;
            if (!Mathf.Approximately(_rotationAngle, 0))
            {
                float absAngle = Mathf.Abs(_rotationAngle);
                float remainder = absAngle % 360f;
                if (Mathf.Approximately(remainder, 0) && !Mathf.Approximately(absAngle, 0))
                {
                    useAngleLerping = true;
                }
                else if (Mathf.Approximately(remainder, 180f))
                {
                    useAngleLerping = true;
                }
            }

            if (useAngleLerping)
            {
                float currentAngleZ = _firepoint.localEulerAngles.z;
                float spinAmountThisSegment = _rotatingForward ? _rotationAngle : -_rotationAngle;
                float targetAngleZ = currentAngleZ + spinAmountThisSegment;

                float elapsedTime = 0f;
                while (elapsedTime < _rotationDuration)
                {
                    float newAngleZ = Mathf.Lerp(currentAngleZ, targetAngleZ, elapsedTime / _rotationDuration);
                    _firepoint.localRotation = Quaternion.Euler(originalX, originalY, newAngleZ);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                _firepoint.localRotation = Quaternion.Euler(originalX, originalY, targetAngleZ);
            }
            else
            {
                float targetSlerpAngleZ;
                if (_rotatingForward)
                {
                    targetSlerpAngleZ = slerpOscillationCenterZ + _rotationAngle;
                }
                else
                {
                    targetSlerpAngleZ = slerpOscillationCenterZ - _rotationAngle;
                }

                float elapsedTime = 0f;
                Quaternion initialSlerpRotation = _firepoint.localRotation;
                Quaternion finalSlerpRotation = Quaternion.Euler(originalX, originalY, targetSlerpAngleZ);

                while (elapsedTime < _rotationDuration)
                {
                    _firepoint.localRotation = Quaternion.Slerp(initialSlerpRotation, finalSlerpRotation, elapsedTime / _rotationDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                _firepoint.localRotation = finalSlerpRotation;
            }

            _rotatingForward = !_rotatingForward;
        }
        _rotationCoroutine = null;
    }

    /// <summary>
    /// Stops the rotation coroutine when disabled.
    /// </summary>
    private void OnDisable()
    {
        if (_rotationCoroutine != null)
        {
            StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = null;
        }
    }
}
