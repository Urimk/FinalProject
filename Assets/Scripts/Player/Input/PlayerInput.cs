using UnityEngine;

/// <summary>
/// Handles human input (keyboard/gamepad) for the player.
/// Implements the PlayerInputHandler interface for human-controlled input.
/// </summary>
public class PlayerInput : PlayerInputHandler
{
    // ==================== Constants ====================
    private const KeyCode JumpKey = KeyCode.Space;
    private const float InputSmoothingSpeed = 20f; // Increased for more responsive input
    private const float InputSnapThreshold = 0.05f;

    // ==================== Private Fields ====================
    private float _rawHorizontalInput;
    private float _smoothedHorizontalInput;
    private bool _isInputEnabled = true;
    private bool _wasJumpPressed = false;

    // ==================== Properties ====================
    /// <summary>
    /// Current horizontal input value with smoothing applied.
    /// </summary>
    public override float HorizontalInput => _smoothedHorizontalInput;
    
    /// <summary>
    /// Whether the input is currently active and enabled.
    /// </summary>
    public override bool IsInputActive => _isInputEnabled;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Update callback. Handles input processing each frame.
    /// </summary>
    private void Update()
    {
        if (!_isInputEnabled || UIManager.Instance.IsGamePaused)
            return;

        ProcessHorizontalInput();
        ProcessJumpInput();
    }

    // ==================== Input Processing ====================
    /// <summary>
    /// Processes horizontal input with smoothing.
    /// </summary>
    private void ProcessHorizontalInput()
    {
        // Get raw input
        _rawHorizontalInput = Input.GetAxisRaw("Horizontal");
        
        // Apply smoothing
        _smoothedHorizontalInput = Mathf.Lerp(_smoothedHorizontalInput, _rawHorizontalInput, InputSmoothingSpeed * Time.deltaTime);
        
        // Snap to zero if within threshold
        if (Mathf.Abs(_smoothedHorizontalInput) < InputSnapThreshold)
        {
            _smoothedHorizontalInput = 0f;
        }
    }

    /// <summary>
    /// Processes jump input and triggers events.
    /// </summary>
    private void ProcessJumpInput()
    {
        bool isJumpPressed = Input.GetKey(JumpKey);
        
        // Jump pressed event
        if (isJumpPressed && !_wasJumpPressed)
        {
            TriggerJumpPressed();
        }
        
        // Jump released event
        if (!isJumpPressed && _wasJumpPressed)
        {
            TriggerJumpReleased();
        }
        
        _wasJumpPressed = isJumpPressed;
    }

    // ==================== Public API ====================
    /// <summary>
    /// Enables input processing.
    /// </summary>
    public override void EnableInput()
    {
        _isInputEnabled = true;
    }

    /// <summary>
    /// Disables input processing.
    /// </summary>
    public override void DisableInput()
    {
        _isInputEnabled = false;
        _smoothedHorizontalInput = 0f;
        _rawHorizontalInput = 0f;
    }

    /// <summary>
    /// Resets input state.
    /// </summary>
    public override void ResetInput()
    {
        _smoothedHorizontalInput = 0f;
        _rawHorizontalInput = 0f;
        _wasJumpPressed = false;
    }
}
