using System.Collections;

using UnityEngine;

public class SpikeHead : EnemyDamage
{
    [Header("Spikehead Attributes")]
    [SerializeField] private float _speed;
    [SerializeField] private float _range;
    [SerializeField] private float _checkDelay;
    [SerializeField] private float _chargeDuration = 0.6f;
    [SerializeField] private float _chargeBackDistance = 1f;
    [SerializeField] private float _attackSpeedMultiplier = 2f;

    [SerializeField] private LayerMask _playerLayer;

    [Header("Sound")]
    [SerializeField] private AudioClip _crashSound;

    [Header("Self-Deactivation")]
    [SerializeField] private bool _deactivateAfterCharge = false;
    [SerializeField] private float _deactivateDelay = 10f;

    private Vector3[] _directions = new Vector3[4];
    private bool _attacking;
    private bool _isCharging;
    private float _checkTimer;
    private Vector3 _destination;

    private Coroutine _selfDeactivationCoroutine = null; // Added to manage deactivation

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

    private void OnEnable()
    {
        // Stop() will also cancel any pending deactivation, resetting the state
        Stop();
        // Reset check timer to allow immediate checks if needed, or keep its existing value
        // checkTimer = 0; // Optional: uncomment if you want checkDelay to apply immediately on enable
    }

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
                break; // only attack in one direction
            }
        }
    }

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
            Vector3 reducedTargetPos = startPos + chargeDirection * 0.1f; // Small distance
            float elapsed = 0f;
            while (elapsed < _chargeDuration)
            {
                transform.position = Vector3.Lerp(startPos, reducedTargetPos, elapsed / _chargeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = reducedTargetPos;
        }

        yield return new WaitForSeconds(0.05f); // Tiny delay before attack starts

        _destination = direction.normalized;
        _attacking = true;
        _isCharging = false;

        // --- Start of Self-Deactivation Logic ---
        if (_deactivateAfterCharge)
        {
            // If a deactivation routine was already running (e.g., from a very rapid re-trigger, though unlikely), stop it.
            if (_selfDeactivationCoroutine != null)
            {
                StopCoroutine(_selfDeactivationCoroutine);
            }
            _selfDeactivationCoroutine = StartCoroutine(DeactivateRoutine());
        }
        // --- End of Self-Deactivation Logic ---
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (SoundManager.instance != null && _crashSound != null)
        {
            SoundManager.instance.PlaySound(_crashSound, gameObject);
        }
        base.OnTriggerStay2D(collision); // Assuming EnemyDamage has OnTriggerStay2D

        // Check if the collision is with something that should stop the spike head
        // Ensure "NoCollision" tag exists or adjust logic as needed
        if (!collision.CompareTag("Player") && (collision.gameObject.layer != LayerMask.NameToLayer("Player")) && !collision.CompareTag("NoCollision"))
        {
            // Determine recoil direction based on current state
            Vector3 recoilDirection;
            if (_attacking) // If was moving forward
            {
                recoilDirection = _destination; // Recoil opposite to attack direction
            }
            else // If hit during charge back or other states (less likely for this specific call point)
            {
                // A more general recoil if not actively attacking forward
                recoilDirection = (transform.position - collision.transform.position).normalized;
                if (recoilDirection == Vector3.zero) // Avoid zero vector if positions are identical
                {
                    recoilDirection = -_destination; // Fallback
                    if (recoilDirection == Vector3.zero) recoilDirection = -transform.right; // Further fallback
                }
            }
            StartCoroutine(Recoil(recoilDirection)); // Recoil now uses the determined direction
            Stop(); // Stop all actions, including pending deactivation if Stop handles it
        }
    }


    private void CalculateDirections()
    {
        _directions[0] = transform.right;
        _directions[1] = -transform.right;
        _directions[2] = transform.up;
        _directions[3] = -transform.up;
    }

    private void Stop()
    {
        _destination = Vector3.zero;
        _attacking = false;
        _isCharging = false;

        // --- Cancel pending self-deactivation if Stop is called ---
        if (_selfDeactivationCoroutine != null)
        {
            StopCoroutine(_selfDeactivationCoroutine);
            _selfDeactivationCoroutine = null;
        }
        // --- End of cancellation logic ---
    }

    private IEnumerator Recoil(Vector3 hitDirection)
    {
        _attacking = false; // Stop attack movement during recoil
        _isCharging = false; // Ensure not in charging state

        Vector3 startPos = transform.position;
        // Recoil slightly in the opposite direction of hitDirection
        Vector3 targetPos = startPos - hitDirection.normalized * 0.3f;
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
        // After recoil, it will go back to checking for player due to Stop() being called previously.
    }

    // --- New Coroutine for Self-Deactivation ---
    private IEnumerator DeactivateRoutine()
    {
        yield return new WaitForSeconds(_deactivateDelay);

        // Check if the GameObject is still active before trying to deactivate it
        // (it might have been deactivated by other means)
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        _selfDeactivationCoroutine = null; // Clear the reference
    }
    // --- End of New Coroutine ---

    // OnDisable is called when the GameObject becomes inactive.
    // Coroutines are automatically stopped by Unity.
    // Explicitly nullifying selfDeactivationCoroutine here is optional but can be good for clarity
    // if you have complex logic that might check this reference from elsewhere after deactivation.
    // However, since OnEnable calls Stop(), which nullifies it, this is generally covered.
    /*
    private void OnDisable()
    {
        selfDeactivationCoroutine = null;
    }
    */
}
