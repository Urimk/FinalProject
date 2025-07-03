using UnityEngine;

/// <summary>
/// Handles the logic for a health collectable that restores player health on pickup.
/// </summary>
public class HealthCollectable : MonoBehaviour
{
    // ==================== Constants ====================
    private const string PlayerTag = "Player";

    // ==================== Inspector Fields ====================
    [Header("Collectable Info")]
    [Tooltip("Amount of health restored by this collectable.")]
    [SerializeField] private float _healthValue;

    [Header("Sound")]
    [Tooltip("Sound to play when the collectable is picked up.")]
    [SerializeField] private AudioClip _collectSound;

    // ==================== Private Fields ====================
    private bool _healthAdded;

    // ==================== Unity Events ====================
    /// <summary>
    /// Restores health to the player and plays a sound when collected.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == PlayerTag)
        {
            _healthAdded = collision.GetComponent<Health>().AddHealth(_healthValue);
            if (_healthAdded)
            {
                SoundManager.instance.PlaySound(_collectSound, gameObject);
                gameObject.SetActive(false);
            }
        }
    }
}
