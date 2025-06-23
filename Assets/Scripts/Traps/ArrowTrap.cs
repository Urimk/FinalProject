using System.Collections; // Required for Coroutines

using UnityEngine;

public class ArrowTrap : MonoBehaviour
{
    [SerializeField] private float _attackCooldown;
    [SerializeField] private Transform _firepoint;
    [SerializeField] private GameObject[] _arrows;
    [SerializeField] private float _speed = 5f;
    [SerializeField] private bool _isComingOut = false;

    [Header("Sound")]
    [SerializeField] private AudioClip _arrowSound;

    [Header("Rotation")]
    [SerializeField] private bool _rotateFirepoint = false;
    [SerializeField] private float _rotationAngle = 40f;
    [SerializeField] private float _rotationDuration = 3f;

    private float _cooldownTimer;
    private bool _rotatingForward = true;
    private Coroutine _rotationCoroutine;

    private void Attack()
    {
        _cooldownTimer = 0;
        if (SoundManager.instance != null && _arrowSound != null)
        {
            SoundManager.instance.PlaySound(_arrowSound, gameObject);
        }
        int idx = FindArrow();
        _arrows[idx].transform.position = _firepoint.position;
        Vector2 direction = _firepoint.right;
        var projectile = _arrows[idx].GetComponent<EnemyProjectile>();
        projectile.GetComponent<EnemyProjectile>().SetDirection(direction.normalized);
        projectile.SetSpeed(_speed);
        projectile.SetComingOut(_isComingOut);
        projectile.GetComponent<EnemyProjectile>().ActivateProjectile();
    }

    private int FindArrow()
    {
        for (int i = 0; i < _arrows.Length; i++)
        {
            if (!_arrows[i].activeInHierarchy)
            {
                return i;
            }
        }
        return 0;
    }

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

    private void OnDisable()
    {
        if (_rotationCoroutine != null)
        {
            StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = null;
        }
    }
}
