using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles the behavior of a diamond collectable in the game.
/// When the player collides with this object, it plays a sound, adds score, and deactivates itself.
/// </summary>
public class Diamond : MonoBehaviour
{
    // ==================== Constants ====================
    /// <summary>
    /// The default score value awarded for collecting a diamond.
    /// </summary>
    private const int DefaultScoreValue = 50;

    // ==================== Inspector Fields ====================
    [Header("Collectable Info")]
    [Tooltip("Score value awarded to the player when this diamond is collected.")]
    [SerializeField] private int _scoreValue = DefaultScoreValue;

    [Tooltip("Unique identifier for this collectable (used for save/load or analytics).")]
    [FormerlySerializedAs("collectableID")]
    [SerializeField] private string _collectableID;

    [Header("Sound")]
    [Tooltip("Sound effect played when the diamond is collected.")]
    [SerializeField] private AudioClip _collectSound;

    // ==================== Properties ====================
    /// <summary>
    /// Gets the score value for this diamond.
    /// </summary>
    public int ScoreValue => _scoreValue;

    /// <summary>
    /// Gets the unique collectable ID.
    /// </summary>
    public string CollectableID => _collectableID;

    // ==================== Unity Events ====================
    /// <summary>
    /// Triggered when another collider enters this object's trigger collider.
    /// Handles collection logic if the player collides.
    /// </summary>
    /// <param name="collision">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the colliding object is the player
        if (collision.CompareTag("Player"))
        {
            // Play the collection sound at this object's location
            SoundManager.instance.PlaySound(_collectSound, gameObject);
            // Add score via the ScoreManager
            ScoreManager.Instance.AddScore(_scoreValue);
            // Deactivate the collectible after collecting
            gameObject.SetActive(false);
        }
    }
}
