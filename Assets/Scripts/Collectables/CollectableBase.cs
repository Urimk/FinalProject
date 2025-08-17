using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base abstract class for collectable objects that provides common functionality.
/// Implements the ICollectable interface and provides default behavior.
/// </summary>
public abstract class CollectableBase : MonoBehaviour, ICollectable
{
    // ==================== Events ====================
    /// <summary>
    /// Event triggered when this collectable is collected.
    /// </summary>
    [System.Serializable]
    public class CollectableCollectedEvent : UnityEvent<string> { }

    [Header("Base Collectable Settings")]
    [Tooltip("Event triggered when this collectable is collected. Parameter: collectableID")]
    [SerializeField] protected CollectableCollectedEvent _onCollected = new CollectableCollectedEvent();

    [Tooltip("Unique identifier for this collectable (used for save/load or analytics).")]
    [SerializeField] protected string _collectableID;

    [Tooltip("Whether this collectable can be collected multiple times.")]
    [SerializeField] protected bool _canRespawn = false;

    // ==================== Protected Fields ====================
    protected bool _isCollected = false;
    protected Collider2D _collider;

    // ==================== Properties ====================
    public string CollectableID => _collectableID;
    public bool IsCollected => _isCollected;
    public bool CanRespawn => _canRespawn;

    /// <summary>
    /// Gets the event that triggers when this collectable is collected.
    /// </summary>
    public CollectableCollectedEvent OnCollected => _onCollected;

    // ==================== Unity Lifecycle ====================
    protected virtual void Awake()
    {
        _collider = GetComponent<Collider2D>();
        ValidateComponents();
    }

    // ==================== Unity Events ====================
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !_isCollected)
        {
            Collect();
        }
    }

    // ==================== Public Methods ====================
    public virtual void Collect()
    {
        if (_isCollected) return;

        _isCollected = true;
        OnCollect();
        _onCollected?.Invoke(_collectableID);
    }

    public virtual void Reset()
    {
        Debug.Log($"Resetting collectable {_collectableID}");

        _isCollected = false;
        gameObject.SetActive(true);

        // Ensure visibility is restored
        SetVisibility(true);

        // Stop any ongoing coroutines
        StopAllCoroutines();

        OnReset();
    }

    // ==================== Protected Abstract Methods ====================
    /// <summary>
    /// Called when the collectable is collected. Override to add specific behavior.
    /// </summary>
    protected abstract void OnCollect();

    /// <summary>
    /// Called when the collectable is reset. Override to add specific behavior.
    /// </summary>
    protected abstract void OnReset();

    // ==================== Protected Virtual Methods ====================
    /// <summary>
    /// Validates that required components are present.
    /// Override to add additional validation.
    /// </summary>
    protected virtual void ValidateComponents()
    {
        if (_collider == null)
        {
            Debug.LogError($"Collectable {gameObject.name} is missing a Collider2D component!", this);
        }

        if (_collider != null && !_collider.isTrigger)
        {
            Debug.LogWarning($"Collectable {gameObject.name} collider should be set to 'Is Trigger' for proper collection behavior.", this);
        }
    }

    /// <summary>
    /// Sets the visibility of the collectable and its collider.
    /// </summary>
    /// <param name="visible">Whether the collectable should be visible and collectable.</param>
    protected virtual void SetVisibility(bool visible)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        if (_collider != null)
        {
            _collider.enabled = visible;
        }
    }
}