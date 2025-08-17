# Core Systems Architecture

## Debug System

### Overview
The `DebugManager` provides a centralized way to control debug logging across the entire game. This allows you to:
- Enable/disable debug output globally
- Control debug output by category
- Switch between console and on-screen logging
- Maintain clean production builds

### Usage

#### Basic Debug Logging
```csharp
// Simple debug log
DebugManager.Log(DebugCategory.Collectable, "Diamond collected!", this);

// Warning log
DebugManager.LogWarning(DebugCategory.Sound, "Sound not found", this);

// Error log
DebugManager.LogError(DebugCategory.Player, "Player health is negative", this);
```

#### Debug Categories
- **Collectable**: For collectable-related debug messages
- **Player**: For player movement, health, and state changes
- **Enemy**: For enemy AI, attacks, and behavior
- **Room**: For room transitions and state changes
- **Sound**: For sound system debugging

#### Configuration
In the inspector, you can configure:
- **Global Debug Mode**: Master switch for all debug output
- **Show Console Logs**: Whether to output to Unity console
- **Show On Screen Logs**: Whether to display on screen (future feature)
- **Category Settings**: Individual toggles for each debug category

### Benefits
1. **Performance**: No debug overhead in production builds
2. **Organization**: Categorized logging for easier debugging
3. **Flexibility**: Enable only the categories you need
4. **Consistency**: Standardized logging format across the project

### Integration Example

Here's how the debug system works in a typical collectable:

```csharp
protected override void OnCollect()
{
    // Debug logging (only if collectable debug is enabled)
    DebugManager.Log(DebugCategory.Collectable, $"Diamond {_collectableID} collected!", this);

    // Game logic
    ScoreManager.Instance.AddScore(_scoreValue);
}
```

This approach provides:
- **Clean separation** of concerns
- **Configurable debugging** without code changes
- **Easy maintenance** and updates
