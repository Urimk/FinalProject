using System.Collections;

using UnityEngine;

/// <summary>
/// Trap enemy that charges at the player in four directions and can self-deactivate.
/// </summary>
public class SpikeHead : EnemyDamage
{
    // === Constants ===
    private const float DefaultChargeDuration = 0.6f;
    private const float DefaultChargeBackDistance = 1f;
    private const float DefaultAttackSpeedMultiplier = 2f;
    private const float DefaultDeactivateDelay = 10f;
    private const float TinyAttackDelay = 0.05f;
    private const float RecoilDistance = 0.3f;
    private const float RecoilDuration = 0.1f;
    private const float ReducedChargeBackDistance = 0.1f;
    private const string PlayerTag = "Player";
    private const string NoCollisionTag = "NoCollision";
    private const string PlayerLayerName = "Player";

    // === Serialized Fields ===
    [Header("Spikehead Attributes")]
    [Tooltip("Base movement speed of the spike head.")]
    [SerializeField] private float _speed;
    [Tooltip("Detection range for the player.")]
    [SerializeField] private float _range;
    [Tooltip("Delay between player detection checks.")]
    [SerializeField] private float _checkDelay;
    [Tooltip("Duration of the charge back before attacking.")]
    [SerializeField] private float _chargeDuration = DefaultChargeDuration;
    [Tooltip("Distance to move back before charging forward.")]
    [SerializeField] private float _chargeBackDistance = DefaultChargeBackDistance;
    [Tooltip("Multiplier applied to speed during attack.")]
    [SerializeField] private float _attackSpeedMultiplier = DefaultAttackSpeedMultiplier;
    [Tooltip("Layer mask used to detect the player.")]
    [SerializeField] private LayerMask _playerLayer;
    [Header("Sound")]
    [Tooltip("Sound to play when the spike head crashes.")]
    [SerializeField] private AudioClip _crashSound;
    [Header("Self-Deactivation")]
    [Tooltip("If true, the spike head will deactivate after charging.")]
    [SerializeField] private bool _deactivateAfterCharge = false;
    [Tooltip("Delay before self-deactivation after charging.")]
    [SerializeField] private float _deactivateDelay = DefaultDeactivateDelay;

    // === Private Fields ===
    private Vector3[] _directions = new Vector3[4];
    private bool _attacking;
    private bool _isCharging;
    private float _checkTimer;
    private Vector3 _destination;
    private Coroutine _selfDeactivationCoroutine = null;

    /// <summary>
    /// Unity Update callback. Handles player detection and attack movement.
    /// </summary>
    private void Update()
    {
        if (!_attacking && !_isCharging)
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer > _checkDelay)
            {
                CheckForPlayer();
            }
        }
        if (_attacking)
        {
            transform.Translate(_destination * Time.deltaTime * _speed * _attackSpeedMultiplier);
        }
    }

    /// <summary>
    /// Unity OnEnable callback. Resets state.
    /// </summary>
    private void OnEnable()
    {
        Stop();
    }

    /// <summary>
    /// Checks for the player in four directions and starts attack if found.
    /// </summary>
    private void CheckForPlayer()
    {
        CalculateDirections();
        for (int i = 0; i < _directions.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _directions[i], _range, _playerLayer);
            if (hit.collider != null && !_attacking && !_isCharging)
            {
                _checkTimer = 0;
                StartCoroutine(ChargeAndAttack(_directions[i]));
                break;
            }
        }
    }

    /// <summary>
    /// Coroutine for charging back, then attacking in a direction.
    /// </summary>
    private IEnumerator ChargeAndAttack(Vector3 direction)
    {
        _isCharging = true;
        Vector3 chargeDirection = -direction.normalized;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + chargeDirection * _chargeBackDistance;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, chargeDirection, _chargeBackDistance, ~_playerLayer);
        bool canChargeBack = hit.collider == null;
        if (canChargeBack)
        {
            float elapsed = 0f;
            while (elapsed < _chargeDuration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / _chargeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPos;
        }
        else
        {
            Vector3 reducedTargetPos = startPos + chargeDirection * ReducedChargeBackDistance;
            float elapsed = 0f;
            while (elapsed < _chargeDuration)
            {
                transform.position = Vector3.Lerp(startPos, reducedTargetPos, elapsed / _chargeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = reducedTargetPos;
        }
        yield return new WaitForSeconds(TinyAttackDelay);
        _destination = direction.normalized;
        _attacking = true;
        _isCharging = false;
        if (_deactivateAfterCharge)
        {
            if (_selfDeactivationCoroutine != null)
            {
                StopCoroutine(_selfDeactivationCoroutine);
            }
            _selfDeactivationCoroutine = StartCoroutine(DeactivateRoutine());
        }
    }

    /// <summary>
    /// Handles collision logic and triggers recoil.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (SoundManager.instance != null && _crashSound != null)
        {
            SoundManager.instance.PlaySound(_crashSound, gameObject);
        }
        base.OnTriggerStay2D(collision);
        if (!collision.CompareTag(PlayerTag) && (collision.gameObject.layer != LayerMask.NameToLayer(PlayerLayerName)) && !collision.CompareTag(NoCollisionTag))
        {
            Vector3 recoilDirection;
            if (_attacking)
            {
                recoilDirection = _destination;
            }
            else
            {
                recoilDirection = (transform.position - collision.transform.position).normalized;
                if (recoilDirection == Vector3.zero)
                {
                    recoilDirection = -_destination;
                    if (recoilDirection == Vector3.zero) recoilDirection = -transform.right;
                }
            }
            StartCoroutine(Recoil(recoilDirection));
            Stop();
        }
    }

    /// <summary>
    /// Calculates the four cardinal directions.
    /// </summary>
    private void CalculateDirections()
    {
        _directions[0] = transform.right;
        _directions[1] = -transform.right;
        _directions[2] = transform.up;
        _directions[3] = -transform.up;
    }

    /// <summary>
    /// Stops all attack and charging actions.
    /// </summary>
    private void Stop()
    {
        _destination = Vector3.zero;
        _attacking = false;
        _isCharging = false;
        if (_selfDeactivationCoroutine != null)
        {
            StopCoroutine(_selfDeactivationCoroutine);
            _selfDeactivationCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine for recoiling after hitting an obstacle.
    /// </summary>
    private IEnumerator Recoil(Vector3 hitDirection)
    {
        _attacking = false;
        _isCharging = false;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos - hitDirection.normalized * RecoilDistance;
        float duration = RecoilDuration;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
    }

    /// <summary>
    /// Coroutine for self-deactivation after a delay.
    /// </summary>
    private IEnumerator DeactivateRoutine()
    {
        yield return new WaitForSeconds(_deactivateDelay);
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        _selfDeactivationCoroutine = null;
    }
}
