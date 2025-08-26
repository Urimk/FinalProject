using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base abstract class for collectable objects that provides common functionality.
/// Implements the ICollectable interface and provides default behavior.
/// </summary>
public abstract class CollectableBase : MonoBehaviour, ICollectable
{
    // ==================== Protected Fields ====================
    protected bool _isCollected = false;
    protected Collider2D _collider;

    // ==================== Properties ====================
    public bool IsCollected => _isCollected;


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
    }

    public virtual void Reset()
    {
        DebugManager.Log(DebugCategory.Collectable, $"Resetting collectable {gameObject.name}", this);
    
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
            DebugManager.LogError(DebugCategory.Collectable, $"Collectable {gameObject.name} is missing a Collider2D component!", this);
        }

        if (_collider != null && !_collider.isTrigger)
        {
            DebugManager.LogWarning(DebugCategory.Collectable, $"Collectable {gameObject.name} collider should be set to 'Is Trigger' for proper collection behavior.", this);
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