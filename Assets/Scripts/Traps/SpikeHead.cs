using UnityEngine;
using System.Collections;

public class SpikeHead : EnemyDamage
{
    [Header("Spikehead Attributes")]
    [SerializeField] private float speed;
    [SerializeField] private float range;
    [SerializeField] private float checkDelay;
    [SerializeField] private float chargeDuration = 0.6f;
    [SerializeField] private float chargeBackDistance = 1f;
    [SerializeField] private float attackSpeedMultiplier = 2f;

    [SerializeField] private LayerMask playerLayer;

    [Header("Sound")]
    [SerializeField] private AudioClip crashSound;

    [Header("Self-Deactivation")]
    [SerializeField] private bool deactivateAfterCharge = false;
    [SerializeField] private float deactivateDelay = 10f;

    private Vector3[] directions = new Vector3[4];
    private bool attacking;
    private bool isCharging;
    private float checkTimer;
    private Vector3 destination;

    private Coroutine selfDeactivationCoroutine = null; // Added to manage deactivation

    private void Update()
    {
        if (!attacking && !isCharging)
        {
            checkTimer += Time.deltaTime;
            if (checkTimer > checkDelay)
            {
                CheckForPlayer();
            }
        }

        if (attacking)
        {
            transform.Translate(destination * Time.deltaTime * speed * attackSpeedMultiplier);
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
        for (int i = 0; i < directions.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directions[i], range, playerLayer);
            if (hit.collider != null && !attacking && !isCharging)
            {
                checkTimer = 0;
                StartCoroutine(ChargeAndAttack(directions[i]));
                break; // only attack in one direction
            }
        }
    }

    private IEnumerator ChargeAndAttack(Vector3 direction)
    {
        isCharging = true;

        Vector3 chargeDirection = -direction.normalized;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + chargeDirection * chargeBackDistance;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, chargeDirection, chargeBackDistance, ~playerLayer);
        bool canChargeBack = hit.collider == null;

        if (canChargeBack)
        {
            float elapsed = 0f;
            while (elapsed < chargeDuration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / chargeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPos;
        }
        else
        {
            Vector3 reducedTargetPos = startPos + chargeDirection * 0.1f; // Small distance
            float elapsed = 0f;
            while (elapsed < chargeDuration)
            {
                transform.position = Vector3.Lerp(startPos, reducedTargetPos, elapsed / chargeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = reducedTargetPos;
        }

        yield return new WaitForSeconds(0.05f); // Tiny delay before attack starts

        destination = direction.normalized;
        attacking = true;
        isCharging = false;

        // --- Start of Self-Deactivation Logic ---
        if (deactivateAfterCharge)
        {
            // If a deactivation routine was already running (e.g., from a very rapid re-trigger, though unlikely), stop it.
            if (selfDeactivationCoroutine != null)
            {
                StopCoroutine(selfDeactivationCoroutine);
            }
            selfDeactivationCoroutine = StartCoroutine(DeactivateRoutine());
        }
        // --- End of Self-Deactivation Logic ---
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (SoundManager.instance != null && crashSound != null)
        {
            SoundManager.instance.PlaySound(crashSound, gameObject);
        }
        base.OnTriggerStay2D(collision); // Assuming EnemyDamage has OnTriggerStay2D

        // Check if the collision is with something that should stop the spike head
        // Ensure "NoCollision" tag exists or adjust logic as needed
        if (!collision.CompareTag("Player") && (collision.gameObject.layer != LayerMask.NameToLayer("Player")) && !collision.CompareTag("NoCollision"))
        {
            // Determine recoil direction based on current state
            Vector3 recoilDirection;
            if (attacking) // If was moving forward
            {
                recoilDirection = destination; // Recoil opposite to attack direction
            }
            else // If hit during charge back or other states (less likely for this specific call point)
            {
                // A more general recoil if not actively attacking forward
                recoilDirection = (transform.position - collision.transform.position).normalized;
                if (recoilDirection == Vector3.zero) // Avoid zero vector if positions are identical
                {
                    recoilDirection = -destination; // Fallback
                    if (recoilDirection == Vector3.zero) recoilDirection = -transform.right; // Further fallback
                }
            }
            StartCoroutine(Recoil(recoilDirection)); // Recoil now uses the determined direction
            Stop(); // Stop all actions, including pending deactivation if Stop handles it
        }
    }


    private void CalculateDirections()
    {
        directions[0] = transform.right;
        directions[1] = -transform.right;
        directions[2] = transform.up;
        directions[3] = -transform.up;
    }

    private void Stop()
    {
        destination = Vector3.zero;
        attacking = false;
        isCharging = false;

        // --- Cancel pending self-deactivation if Stop is called ---
        if (selfDeactivationCoroutine != null)
        {
            StopCoroutine(selfDeactivationCoroutine);
            selfDeactivationCoroutine = null;
        }
        // --- End of cancellation logic ---
    }

    private IEnumerator Recoil(Vector3 hitDirection)
    {
        attacking = false; // Stop attack movement during recoil
        isCharging = false; // Ensure not in charging state

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
        yield return new WaitForSeconds(deactivateDelay);

        // Check if the GameObject is still active before trying to deactivate it
        // (it might have been deactivated by other means)
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        selfDeactivationCoroutine = null; // Clear the reference
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