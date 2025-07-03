using UnityEngine;

/// <summary>
/// Handles the pickup and rotation logic for the sword collectible.
/// </summary>
public class SwordPickup : MonoBehaviour
{
    // === Constants ===
    private const float DefaultRotationSpeed = 90f; // degrees per second
    private const string PlayerTag = "Player";

    // === Inspector Fields ===
    [Header("Sword Pickup Settings")]
    [Tooltip("Rotation speed of the sword in degrees per second.")]
    [SerializeField] private float _rotationSpeed = DefaultRotationSpeed;

    /// <summary>
    /// Rotates the sword around the Y axis every frame.
    /// </summary>
    private void Update()
    {
        transform.Rotate(Vector3.up * _rotationSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Equips the sword to the player when they collide with the pickup.
    /// </summary>
    /// <param name="collision">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(PlayerTag))
        {
            PlayerAttack playerAttack = collision.GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                playerAttack.HasSword = true;
                playerAttack.EquipWeapon();
                gameObject.SetActive(false);
            }
        }
    }
}
