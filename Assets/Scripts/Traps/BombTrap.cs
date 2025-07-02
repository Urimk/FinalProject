using UnityEngine;

/// <summary>
/// Handles bomb trap logic, triggering an explosion when the player enters the trigger.
/// </summary>
public class BombTrap : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private const string TriggerBombAnim = "TriggerBomb";
    private const string HideAnim = "hide";
    private const float DefaultDelayBeforeExplosion = 1.5f;

    [SerializeField] private GameObject _explosionPrefab;
    [SerializeField] private float _delayBeforeExplosion = DefaultDelayBeforeExplosion;

    private bool _triggered = false;

    /// <summary>
    /// Triggers the bomb when the player enters the trigger.
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
    /// Instantiates the explosion and hides the bomb.
    /// </summary>
    private void Explode()
    {
        Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        GetComponent<Animator>().SetTrigger(HideAnim);
        gameObject.SetActive(false);
        _triggered = false;
    }
}
