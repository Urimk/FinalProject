using UnityEngine;
using System.Collections; // Required for Coroutines

public class ArrowTrap : MonoBehaviour
{
    [SerializeField] private float attackCooldown;
    [SerializeField] private Transform firepoint;
    [SerializeField] private GameObject[] arrows;

    [Header("Sound")]
    [SerializeField] private AudioClip arrowSound;

    [Header("Rotation")]
    [SerializeField] private bool rotateFirepoint = false;
    [SerializeField] private float rotationAngle = 40f; // Angle to rotate
    [SerializeField] private float rotationDuration = 3f; // Duration of one rotation segment

    private float cooldownTimer;
    private bool rotatingForward = true; // To track current rotation direction
    private Coroutine rotationCoroutine;

    private void Attack()
    {
        cooldownTimer = 0;
        if (SoundManager.instance != null && arrowSound != null) // Basic null check
        {
            SoundManager.instance.PlaySound(arrowSound);
        }
        int idx = FindArrow();
        arrows[idx].transform.position = firepoint.position;

        // Original direction logic: shoots along local -X, then flips world Y.
        // Ensure this aligns with your arrow sprites and game orientation.
        // Vector2 direction = -firepoint.right.normalized; 
        // direction.y *= -1; // This Y flip is unusual, ensure it's intended.

        // A more common 2D setup might be to shoot along firepoint.up (if sprite points "up")
        // or firepoint.right (if sprite points "right" and is visually oriented for that).
        // For this example, I'll assume firepoint.right is the intended "forward" for the arrow.
        // If your arrow needs to shoot "out" from where the firepoint is "looking" (e.g. its local X+):
        Vector2 direction = firepoint.right; // Shoots along the firepoint's local X+ axis
        // If firepoint is visually oriented so its "up" is the shooting direction:
        // Vector2 direction = firepoint.up;

        arrows[idx].GetComponent<EnemyProjectile>().SetDirection(direction.normalized);
        arrows[idx].GetComponent<EnemyProjectile>().ActivateProjectile();
    }

    private int FindArrow()
    {
        for (int i = 0; i < arrows.Length; i++)
        {
            if (!arrows[i].activeInHierarchy)
            {
                return i;
            }
        }
        return 0; // Default to reusing the first arrow if all are active
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;
        if (cooldownTimer >= attackCooldown)
        {
            Attack();
        }

        if (rotateFirepoint)
        {
            if (rotationCoroutine == null)
            {
                rotationCoroutine = StartCoroutine(RotateFirepointCoroutine());
            }
        }
        else if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    private IEnumerator RotateFirepointCoroutine()
    {
        // Store original X and Y Euler angles to preserve them during Z-axis rotation
        float originalX = firepoint.localEulerAngles.x;
        float originalY = firepoint.localEulerAngles.y;

        // This is the center of oscillation for the Slerp method (non-180/360 angles)
        float slerpOscillationCenterZ = firepoint.localEulerAngles.z;

        while (rotateFirepoint)
        {
            bool useAngleLerping = false;
            if (!Mathf.Approximately(rotationAngle, 0)) // Exclude zero rotation
            {
                float absAngle = Mathf.Abs(rotationAngle);
                float remainder = absAngle % 360f;

                // Check if it's a multiple of 180 degrees
                // (e.g., 180, 360, 540, 720)
                if (Mathf.Approximately(remainder, 0) && !Mathf.Approximately(absAngle,0)) // Effectively 360, 720 etc. (was a multiple of 360)
                {
                     useAngleLerping = true;
                }
                else if (Mathf.Approximately(remainder, 180f)) // Effectively 180, 540 etc.
                {
                    useAngleLerping = true;
                }
            }

            if (useAngleLerping)
            {
                // Continuous spin logic using Mathf.Lerp on Euler angles for 180/360 degree multiples
                float currentAngleZ = firepoint.localEulerAngles.z;
                
                // Determine the amount to spin in this segment based on rotationAngle and direction
                float spinAmountThisSegment = rotatingForward ? rotationAngle : -rotationAngle;
                float targetAngleZ = currentAngleZ + spinAmountThisSegment;

                float elapsedTime = 0f;
                while (elapsedTime < rotationDuration)
                {
                    float newAngleZ = Mathf.Lerp(currentAngleZ, targetAngleZ, elapsedTime / rotationDuration);
                    firepoint.localRotation = Quaternion.Euler(originalX, originalY, newAngleZ);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                // Set final angle accurately
                firepoint.localRotation = Quaternion.Euler(originalX, originalY, targetAngleZ);
            }
            else // Original Slerp-based oscillation for other angles (e.g., 40, 90)
            {
                float targetSlerpAngleZ;
                // For Slerp, targets are relative to the initial center of oscillation
                if (rotatingForward)
                {
                    targetSlerpAngleZ = slerpOscillationCenterZ + rotationAngle;
                }
                else
                {
                    targetSlerpAngleZ = slerpOscillationCenterZ - rotationAngle;
                }

                float elapsedTime = 0f;
                Quaternion initialSlerpRotation = firepoint.localRotation;
                Quaternion finalSlerpRotation = Quaternion.Euler(originalX, originalY, targetSlerpAngleZ);

                while (elapsedTime < rotationDuration)
                {
                    firepoint.localRotation = Quaternion.Slerp(initialSlerpRotation, finalSlerpRotation, elapsedTime / rotationDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                // Ensure the rotation is exact
                firepoint.localRotation = finalSlerpRotation;
            }

            rotatingForward = !rotatingForward; // Reverse direction for the next cycle
        }
        rotationCoroutine = null; // Clear the reference when the coroutine exits its loop
    }

    private void OnDisable()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }
}