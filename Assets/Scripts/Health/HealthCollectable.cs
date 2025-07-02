using UnityEngine;

/// <summary>
/// Handles the logic for a health collectable that restores player health on pickup.
/// </summary>
public class HealthCollectable : MonoBehaviour
{
    private const string PlayerTag = "Player";

    [Header("Collectable Info")]
    [SerializeField] private float _healthValue;

    [Header("Sound")]
    [SerializeField] private AudioClip _collectSound;

    private bool _healthAdded;

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
