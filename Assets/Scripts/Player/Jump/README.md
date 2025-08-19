# Player Jump System (Merged into PlayerMovement)

## Overview

The jump system has been **merged back into `PlayerMovement.cs`** to maintain tight coupling between movement and jumping mechanics, which is essential for platformer gameplay. The previous separation into `PlayerJumpController` was reverted due to the tight coupling between jump and movement systems.

## Architecture

### Current Structure
- **`PlayerMovement.cs`** - Contains all jump logic integrated with movement
- **`PlayerInputHandler.cs`** - Abstract base class for input handling
- **`PlayerInput.cs`** - Human input implementation
- **`AIInput.cs`** - AI input implementation

### Why the Merge?
1. **Tight Coupling**: Jump and movement are fundamentally intertwined in platformers
2. **Shared State**: Both systems need access to the same physics state, input, and collision detection
3. **Performance**: Eliminates unnecessary component communication overhead
4. **Simplicity**: Reduces debugging complexity and architectural overhead

## Jump Features in PlayerMovement

### Jump Types
- **Ground Jump**: Basic jump from ground
- **Wall Jump**: Jump off walls with directional force
- **Coyote Time**: Extra jump window after leaving ground
- **Multiple Jumps**: Double/triple jump system
- **Air Jump**: Additional jumps while airborne

### Wall Interaction
- **Wall Gravity**: Applied only when actively moving toward wall
- **Wall Detection**: BoxCast-based wall detection
- **Wall Jump Cooldown**: Prevents rapid wall jumping

### State Management
- **Jump Counter**: Tracks available jumps
- **Coyote Counter**: Manages coyote time window
- **Wall Jump State**: Tracks wall jump decay and cooldown

## Key Methods

### Jump Logic
```csharp
public bool TryJump()           // Attempts to perform a jump
private void PerformGroundJump() // Handles ground jump
private void PerformWallJump()   // Handles wall jump
public void AdjustJumpHeight()   // Adjusts jump height on release
```

### Wall Interaction
```csharp
private void HandleWallInteraction() // Manages wall gravity and interaction
private bool CheckOnWall()           // Detects wall contact
```

### State Management
```csharp
public void ResetJumpCounter()       // Resets jumps when grounded
public float GetWallJumpVelocity()   // Gets wall jump decay velocity
```

## Integration with Input System

The jump system integrates with the input system through events:

```csharp
// In PlayerMovement.Awake()
_inputHandler.OnJumpPressed += OnJumpPressed;
_inputHandler.OnJumpReleased += OnJumpReleased;

// Event handlers
private void OnJumpPressed() => TryJump();
private void OnJumpReleased() => AdjustJumpHeight();
```

## Power-Up Integration

Jump power-ups are handled directly in `PlayerMovement`:

```csharp
public void ActivatePowerUp(int bonusJumps, float bonusJumpPower)
{
    _extraJumps += bonusJumps;
    _jumpPower += bonusJumpPower;
}
```

## Benefits of Merged Architecture

### ✅ Advantages
- **Simplified Communication**: No need for inter-component communication
- **Better Performance**: Direct access to shared state
- **Easier Debugging**: All logic in one place
- **Tighter Integration**: Jump and movement work seamlessly together
- **Reduced Complexity**: Fewer components to manage

### ⚠️ Considerations
- **Larger File**: `PlayerMovement.cs` is now a larger file
- **Single Responsibility**: Violates single responsibility principle
- **Testing**: Harder to test jump logic in isolation

## Migration Notes

### From Separate Jump Controller
If you were using the separate `PlayerJumpController`:

1. **Remove References**: Delete any `PlayerJumpController` components
2. **Update Properties**: Use `PlayerMovement.JumpPower` instead of `jumpController.JumpPower`
3. **Update Methods**: Call jump methods directly on `PlayerMovement`
4. **Update Tests**: Modify tests to work with merged architecture

### Example Migration
```csharp
// Before (Separate Controller)
jumpController.JumpPower = 10f;
bool canJump = jumpController.CanJump;

// After (Merged)
playerMovement.JumpPower = 10f;
bool canJump = playerMovement.CanJump;
```

## Best Practices

1. **Keep Input System**: The input system separation is still beneficial
2. **Organize Code**: Use clear sections and comments in `PlayerMovement`
3. **Test Integration**: Test jump and movement together
4. **Monitor Size**: Keep `PlayerMovement` organized and well-documented

## Future Considerations

If `PlayerMovement` becomes too large, consider:
- **Extracting Recoil System**: Move recoil logic to separate component
- **Extracting Power-Up System**: Move power-up logic to separate component
- **Extracting Animation System**: Move animation logic to separate component

But keep jump and movement together as they are fundamentally coupled in platformer gameplay.
