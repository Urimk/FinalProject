# Collectable System Architecture

## Overview

The collectable system has been refactored to follow a clean, extensible architecture pattern. This document explains the new structure and how to use it.

## Architecture Components

### 1. ICollectable Interface
- **Purpose**: Defines the contract for all collectable objects
- **Key Methods**: `Collect()`, `Reset()`
- **Key Properties**: `CollectableID`, `IsCollected`, `CanRespawn`

### 2. CollectableBase Abstract Class
- **Purpose**: Provides common functionality for all collectables
- **Features**:
  - Automatic collision detection with player
  - Component validation
  - Event system integration
  - Visibility management
  - Respawn functionality

### 3. Diamond Class (Example Implementation)
- **Purpose**: Specific implementation for diamond collectables
- **Features**:
  - Score awarding
  - Sound effects
  - Configurable respawn timing

## Key Improvements Made

### 1. **Better Error Handling**
- Null checks for manager instances
- Component validation with helpful error messages
- Graceful degradation when dependencies are missing

### 2. **Event System Integration**
- UnityEvents for external system integration
- Decoupled architecture allowing other systems to listen for collection events

### 3. **Respawn System**
- Configurable respawn functionality
- Visual feedback during respawn process
- Coroutine-based timing system

### 4. **Code Reusability**
- Base class eliminates code duplication
- Interface allows for polymorphic behavior
- Easy to extend for new collectable types

### 5. **Better Documentation**
- Comprehensive XML documentation
- Clear separation of concerns
- Inline comments explaining complex logic

## Usage Examples

### Creating a New Collectable Type

```csharp
public class Gem : CollectableBase
{
    [SerializeField] private int _gemValue = 100;
    [SerializeField] private AudioClip _gemSound;

    protected override void OnCollect()
    {
        // Add gem-specific collection logic
        ScoreManager.Instance.AddScore(_gemValue);
        SoundManager.instance.PlaySound(_gemSound, gameObject);
        
        // Optional: Add particle effects, animations, etc.
    }

    protected override void OnReset()
    {
        // Add gem-specific reset logic if needed
    }
}
```

### Listening to Collection Events

```csharp
public class CollectionTracker : MonoBehaviour
{
    private void Start()
    {
        // Find all diamonds in the scene
        var diamonds = FindObjectsOfType<Diamond>();
        
        foreach (var diamond in diamonds)
        {
            diamond.OnCollected.AddListener(OnDiamondCollected);
        }
    }

    private void OnDiamondCollected(string collectableID)
    {
        Debug.Log($"Diamond {collectableID} was collected!");
        // Add your custom logic here
    }
}
```

## Inspector Configuration

### Base Collectable Settings
- **Collectable ID**: Unique identifier for save/load systems
- **Can Respawn**: Whether the collectable can be collected multiple times
- **On Collected Event**: UnityEvent that triggers when collected

### Diamond-Specific Settings
- **Score Value**: Points awarded when collected
- **Collect Sound**: Audio clip to play on collection
- **Respawn Time**: Delay before respawning (if enabled)

## Best Practices

1. **Always inherit from CollectableBase** for new collectable types
2. **Use the interface** when you need polymorphic behavior
3. **Implement OnCollect()** for collection-specific logic
4. **Use events** for loose coupling between systems
5. **Validate components** in the base class Awake method
6. **Provide meaningful error messages** when dependencies are missing

## Migration Guide

If you have existing collectable scripts:

1. **Inherit from CollectableBase** instead of MonoBehaviour
2. **Move collection logic** to the `OnCollect()` method
3. **Remove duplicate code** that's now handled by the base class
4. **Update inspector fields** to use the new structure
5. **Test thoroughly** to ensure behavior remains the same

## Integration with Room System

The collectable system has been updated to work properly with the existing Room system. The Room script now:

1. **Uses the ICollectable interface** to reset collectables properly
2. **Provides fallback support** for legacy collectables that don't use the new system
3. **Includes helper methods** for working with collectables

### Room Integration Changes

The `Room.ResetRoom()` method now properly resets collectables using the new system:

```csharp
// New behavior - uses ICollectable interface
var collectable = _collectables[i].GetComponent<ICollectable>();
if (collectable != null)
{
    collectable.Reset(); // Properly resets state and visibility
}
else
{
    // Fallback for legacy collectables
    _collectables[i].SetActive(true);
}
```

### Debugging Integration Issues

If you're experiencing issues with collectable reset functionality:

1. **Use the CollectableDebugger script** to test and diagnose issues
2. **Check the console logs** for detailed information about collectable states
3. **Verify that collectables have proper IDs** set in the inspector
4. **Ensure colliders are set to "Is Trigger"** for proper collection behavior

### Common Issues and Solutions

**Issue**: Collectables don't reset properly when rooms reset
- **Solution**: Make sure collectables inherit from `CollectableBase` and implement the `ICollectable` interface

**Issue**: Collectables disappear after collection and don't respawn
- **Solution**: Check the `CanRespawn` setting and `RespawnTime` values in the inspector

**Issue**: Collectables are invisible after reset
- **Solution**: The system now automatically restores visibility, but check that SpriteRenderer components are present

## Future Enhancements

Potential improvements for the collectable system:

1. **Save/Load System**: Integration with persistent data
2. **Animation System**: Built-in collection animations
3. **Particle Effects**: Configurable visual feedback
4. **Audio Pooling**: Better sound management
5. **Collection Tracking**: Analytics and achievement systems 