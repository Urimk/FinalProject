using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles the behavior of a diamond collectable in the game.
/// When the player collides with this object, it plays a sound, adds score, and deactivates itself.
/// Inherits from CollectableBase to follow the collectable pattern.
/// </summary>
public class Diamond : CollectableBase
{
    // ==================== Constants ====================
    /// <summary>
    /// The default score value awarded for collecting a diamond.
    /// </summary>
    private const int DefaultScoreValue = 50;

    // ==================== Inspector Fields ====================
    [Header("Diamond Settings")]
    [Tooltip("Score value awarded to the player when this diamond is collected.")]
    [FormerlySerializedAs("scoreValue")]
    [SerializeField] private int _scoreValue = DefaultScoreValue;

    [Tooltip("Sound effect played when the diamond is collected.")]
    [FormerlySerializedAs("collectSound")]
    [SerializeField] private AudioClip _collectSound;

    // ==================== Properties ====================
    /// <summary>
    /// Gets the score value for this diamond.
    /// </summary>
    public int ScoreValue => _scoreValue;

    // ==================== Protected Methods ====================
    /// <summary>
    /// Called when the diamond is collected. Handles sound, score, and respawning logic.
    /// </summary>
    protected override void OnCollect()
    {
        DebugManager.Log(DebugCategory.Collectable, $"Diamond {_collectableID} collected! Score: {_scoreValue}", this);

        // Play the collection sound if available
        if (_collectSound != null)
        {
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySound(_collectSound, gameObject);
            }
            else
            {
                DebugManager.LogWarning(DebugCategory.Sound, "SoundManager instance not found. Cannot play collection sound.", this);
            }
        }

        // Add score via the ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(_scoreValue);
        }
        else
        {
            DebugManager.LogWarning(DebugCategory.Collectable, "ScoreManager instance not found. Cannot add score.", this);
        }
        // Deactivate the collectible after collecting
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when the diamond is reset. Ensures proper visibility restoration.
    /// </summary>
    protected override void OnReset()
    {
        DebugManager.Log(DebugCategory.Collectable, $"Diamond {_collectableID} reset!", this);

        // Ensure the diamond is visible and collectable
        SetVisibility(true);

        // Stop any ongoing respawn coroutines
        StopAllCoroutines();
    }
}
