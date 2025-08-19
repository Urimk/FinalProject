# Player Input System

This directory contains the refactored input system for the player, providing a clean separation between input logic and movement logic.

## Architecture

### `PlayerInputHandler.cs`
Abstract base class that defines the contract for all input systems:
- **Events**: `OnJumpPressed`, `OnJumpReleased`
- **Properties**: `HorizontalInput`, `IsInputActive`
- **Methods**: `EnableInput()`, `DisableInput()`, `ResetInput()`

### `PlayerInput.cs`
Human input implementation that handles keyboard/gamepad input:
- Processes horizontal input with smoothing
- Handles jump key press/release
- Respects game pause state
- Provides input smoothing and snap-to-zero functionality

### `AIInput.cs`
AI input implementation for AI-controlled players:
- Accepts movement direction from AI systems
- Handles jump duration simulation
- Provides delayed flip logic for AI
- Manages jump coroutines for realistic AI behavior

## Usage

### For Human Players
The system automatically creates a `PlayerInput` component when `_isAIControlled = false`.

### For AI Players
The system automatically creates an `AIInput` component when `_isAIControlled = true`.

### AI Control Methods
```csharp
// Set movement direction (-1 to 1)
playerMovement.SetAIInput(moveDirection);

// Set jump with duration
playerMovement.SetAIJump(jumpDuration);
```

## Benefits

1. **Separation of Concerns**: Input logic is completely separated from movement logic
2. **Testability**: Easy to mock input for testing
3. **Flexibility**: Easy to add new input types (e.g., touch, gamepad)
4. **Maintainability**: Cleaner, more focused classes
5. **Reusability**: Input systems can be reused across different projects

## Integration

The `PlayerMovement` class now:
- Uses `_inputHandler.HorizontalInput` instead of `_horizontalInput`
- Subscribes to jump events instead of polling input
- Delegates AI input to the appropriate input handler
- Maintains all existing functionality while being cleaner

## Migration Notes

- Removed `_horizontalInput` field from `PlayerMovement`
- Removed `_currentAIJumpRoutine` and related coroutine logic
- Removed `HandleJumpInput()` method
- Simplified `FlipSprite()` logic
- Updated all input-dependent methods to use the input system
