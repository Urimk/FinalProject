using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles bomb trap logic, triggering an explosion when the player enters the trigger.
/// </summary>
public class BombTrap : MonoBehaviour
{
    // === Constants ===
    private const string PlayerTag = "Player";
    private const string TriggerBombAnim = "TriggerBomb";
    private const string HideAnim = "hide";
    private const float DefaultDelayBeforeExplosion = 1.5f;

    // === Inspector Fields ===
    [Header("Explosion Settings")]
    [Tooltip("Prefab to instantiate when the bomb explodes.")]
    [FormerlySerializedAs("explosionPrefab")]

    [SerializeField] private GameObject _explosionPrefab;

    [Tooltip("Delay in seconds before the bomb explodes after being triggered.")]
    [FormerlySerializedAs("delayBeforeExplosion")]

    [SerializeField] private float _delayBeforeExplosion = DefaultDelayBeforeExplosion;

    // === Private State ===
    private bool _triggered = false;

    /// <summary>
    /// Called by Unity when another collider enters this trigger.
    /// Triggers the bomb if the player enters and it hasn't already been triggered.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_triggered && other.CompareTag(PlayerTag))
        {
            _triggered = true;
            GetComponent<Animator>().SetTrigger(TriggerBombAnim);
        }
    }

    /// <summary>
    /// Instantiates the explosion prefab, hides the bomb, and resets the triggered state.
    /// Should be called by an animation event.
    /// </summary>
    private void Explode()
    {
        Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        GetComponent<Animator>().SetTrigger(HideAnim);
        gameObject.SetActive(false);
        _triggered = false;
    }
}
