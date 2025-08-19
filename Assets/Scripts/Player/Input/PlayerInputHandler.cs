using UnityEngine;

/// <summary>
/// Abstract interface for handling player input, whether from human or AI.
/// Provides a clean separation between input logic and movement logic.
/// </summary>
public abstract class PlayerInputHandler : MonoBehaviour
{
    // ==================== Events ====================
    /// <summary>
    /// Event triggered when jump input is detected.
    /// </summary>
    public event System.Action OnJumpPressed;
    
    /// <summary>
    /// Event triggered when jump input is released.
    /// </summary>
    public event System.Action OnJumpReleased;

    // ==================== Properties ====================
    /// <summary>
    /// Current horizontal input value (-1 to 1).
    /// </summary>
    public abstract float HorizontalInput { get; }
    
    /// <summary>
    /// Whether the input is currently active.
    /// </summary>
    public abstract bool IsInputActive { get; }

    // ==================== Protected Methods ====================
    /// <summary>
    /// Triggers the jump pressed event.
    /// </summary>
    protected void TriggerJumpPressed()
    {
        OnJumpPressed?.Invoke();
    }

    /// <summary>
    /// Triggers the jump released event.
    /// </summary>
    protected void TriggerJumpReleased()
    {
        OnJumpReleased?.Invoke();
    }

    // ==================== Abstract Methods ====================
    /// <summary>
    /// Called when the input handler should be enabled.
    /// </summary>
    public abstract void EnableInput();
    
    /// <summary>
    /// Called when the input handler should be disabled.
    /// </summary>
    public abstract void DisableInput();
    
    /// <summary>
    /// Resets the input handler state.
    /// </summary>
    public abstract void ResetInput();
}
