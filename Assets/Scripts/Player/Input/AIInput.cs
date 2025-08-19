using System.Collections;
using UnityEngine;

/// <summary>
/// Handles AI input for the player.
/// Implements the PlayerInputHandler interface for AI-controlled input.
/// </summary>
public class AIInput : PlayerInputHandler
{
    // ==================== Constants ====================
    private const float FlipDelay = 0.2f;
    private const float MaxJumpHoldDuration = 1.0f;

    // ==================== Private Fields ====================
    private float _horizontalInput;
    private bool _isInputEnabled = true;
    private bool _wasJumpPressed = false;
    private float _flipTimer = 0f;
    private int _desiredFacingDirection = 1;
    private Coroutine _currentJumpRoutine = null;

    // ==================== Properties ====================
    /// <summary>
    /// Current horizontal input value.
    /// </summary>
    public override float HorizontalInput => _horizontalInput;
    
    /// <summary>
    /// Whether the input is currently active and enabled.
    /// </summary>
    public override bool IsInputActive => _isInputEnabled;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Update callback. Handles AI input processing each frame.
    /// </summary>
    private void Update()
    {
        if (!_isInputEnabled)
            return;

        UpdateFlipTimer();
    }

    // ==================== AI Input Methods ====================
    /// <summary>
    /// Sets the AI movement direction.
    /// </summary>
    /// <param name="moveDirection">The direction to move (-1, 0, 1).</param>
    public void SetMovementInput(float moveDirection)
    {
        if (!_isInputEnabled)
            return;

        _horizontalInput = Mathf.Clamp(moveDirection, -1f, 1f);
        UpdateDesiredFacingDirection();
    }

    /// <summary>
    /// Sets the AI jump input with duration.
    /// </summary>
    /// <param name="jumpDuration">Duration to hold the jump (seconds).</param>
    public void SetJumpInput(float jumpDuration)
    {
        if (!_isInputEnabled || jumpDuration <= 0)
            return;

        // Stop any existing jump routine
        if (_currentJumpRoutine != null)
        {
            StopCoroutine(_currentJumpRoutine);
        }

        // Start new jump routine
        _currentJumpRoutine = StartCoroutine(JumpRoutine(jumpDuration));
    }

    /// <summary>
    /// Updates the desired facing direction based on movement.
    /// </summary>
    private void UpdateDesiredFacingDirection()
    {
        if (_horizontalInput > 0.01f)
        {
            _desiredFacingDirection = 1;
        }
        else if (_horizontalInput < -0.01f)
        {
            _desiredFacingDirection = -1;
        }
        else
        {
            _flipTimer = 0f; // Reset timer when not moving
        }
    }

    /// <summary>
    /// Updates the flip timer for delayed AI flipping.
    /// </summary>
    private void UpdateFlipTimer()
    {
        if (_desiredFacingDirection != GetCurrentFacingDirection())
        {
            _flipTimer += Time.deltaTime;
            if (_flipTimer >= FlipDelay)
            {
                // Flip would be handled by the movement system
                _flipTimer = 0f;
            }
        }
        else
        {
            _flipTimer = 0f;
        }
    }

    /// <summary>
    /// Coroutine for simulating AI jump hold duration.
    /// </summary>
    /// <param name="jumpDuration">Duration to hold the jump (seconds).</param>
    private IEnumerator JumpRoutine(float jumpDuration)
    {
        // Trigger jump pressed
        if (!_wasJumpPressed)
        {
            TriggerJumpPressed();
            _wasJumpPressed = true;
        }

        // Hold jump for duration
        yield return new WaitForSeconds(jumpDuration);

        // Trigger jump released
        if (_wasJumpPressed)
        {
            TriggerJumpReleased();
            _wasJumpPressed = false;
        }

        _currentJumpRoutine = null;
    }

    /// <summary>
    /// Gets the current facing direction from the transform.
    /// </summary>
    /// <returns>1 for right, -1 for left.</returns>
    private int GetCurrentFacingDirection()
    {
        return transform.localScale.x > 0 ? 1 : -1;
    }

    // ==================== Public API ====================
    /// <summary>
    /// Enables AI input processing.
    /// </summary>
    public override void EnableInput()
    {
        _isInputEnabled = true;
    }

    /// <summary>
    /// Disables AI input processing.
    /// </summary>
    public override void DisableInput()
    {
        _isInputEnabled = false;
        _horizontalInput = 0f;
        
        // Stop any ongoing jump routine
        if (_currentJumpRoutine != null)
        {
            StopCoroutine(_currentJumpRoutine);
            _currentJumpRoutine = null;
        }
    }

    /// <summary>
    /// Resets AI input state.
    /// </summary>
    public override void ResetInput()
    {
        _horizontalInput = 0f;
        _wasJumpPressed = false;
        _flipTimer = 0f;
        _desiredFacingDirection = 1;
        
        if (_currentJumpRoutine != null)
        {
            StopCoroutine(_currentJumpRoutine);
            _currentJumpRoutine = null;
        }
    }
}
