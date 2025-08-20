# EpisodeManager Modular System

## Overview
The `EpisodeManager` has been refactored to support multiple boss modes, making it more flexible and independent. It can now work with:
- **Q-Learning Boss**: Original AI boss with learning capabilities
- **Auto Boss**: Simple boss with predefined behavior (no learning)
- **No Boss**: Training without any boss

## Boss Modes

### **QLearning Mode**
- **Purpose**: Full AI training with Q-learning boss
- **Requirements**: 
  - `BossRewardManager` (required)
  - `BossQLearning` (optional, for logging)
  - `AIBoss` component
  - `BossEnemy` component
- **Features**: 
  - Reward management and logging
  - Q-learning state tracking
  - Episode statistics
  - Full ML-Agents integration

### **Auto Mode**
- **Purpose**: Simple training with predictable boss behavior
- **Requirements**:
  - `AutoBoss` component
  - No reward manager needed
- **Features**:
  - Basic boss attacks (melee, fireball, dash)
  - Simple movement AI
  - No learning or complex state management
  - Perfect for testing player AI without boss complexity

### **None Mode**
- **Purpose**: Training without any boss
- **Requirements**: None
- **Features**:
  - Environment reset only
  - No boss-related components needed
  - Useful for movement/platforming training

## Configuration

### **EpisodeManager Inspector Settings**

#### **Boss Configuration**
```
Boss Mode: [QLearning | Auto | None]
Boss GameObject: Reference to boss GameObject
```

#### **Scene References**
```
Player GameObject: Reference to player
AIBoss: Reference to AIBoss component (QLearning mode)
BossEnemy: Reference to BossEnemy component (QLearning mode)
AutoBoss: Reference to AutoBoss component (Auto mode)
BossHealth: Reference to BossHealth component
PlayerHealth: Reference to player Health component
PlayerMovement: Reference to PlayerMovement component
PlayerAttack: Reference to PlayerAttack component
```

#### **Q-Learning References (Optional)**
```
BossRewardManager: Reference to BossRewardManager (QLearning mode)
BossQLearning: Reference to BossQLearning (QLearning mode)
```

## AutoBoss Component

### **Features**
- **Basic Attacks**: Melee, fireball, dash attacks
- **Simple AI**: Movement towards/away from player
- **Cooldown System**: Configurable attack timings
- **Animation Support**: Triggers appropriate animations
- **Projectile Management**: Handles fireball pooling

### **Configuration**
```
Auto Boss Settings:
- Attack Cooldown: Time between basic attacks
- Fire Attack Cooldown: Time between fire attacks  
- Dash Cooldown: Time between dash attacks
- Attack Range: Range at which boss will attack
- Movement Speed: Boss movement speed

Attack Parameters:
- Fireball Speed: Speed of fireball projectiles
- Dash Speed: Speed of dash attack
- Dash Duration: Duration of dash attack
- Flame Duration: Duration of flame trap
- Warning Duration: Duration of warning before flame trap
```

### **Behavior**
1. **Movement**: Moves towards player if too far, away if too close
2. **Attack Selection**: Randomly chooses between melee, fire, and dash attacks
3. **Cooldowns**: Respects individual cooldowns for each attack type
4. **Range Management**: Only attacks when player is within range

## Migration Guide

### **From Old System to Q-Learning Mode**
1. Set `Boss Mode` to `QLearning`
2. Ensure all Q-learning references are assigned
3. No other changes needed - backward compatible

### **From Old System to Auto Mode**
1. Set `Boss Mode` to `Auto`
2. Add `AutoBoss` component to boss GameObject
3. Configure `AutoBoss` settings in Inspector
4. Remove or disable Q-learning components if not needed

### **To No Boss Mode**
1. Set `Boss Mode` to `None`
2. Remove or disable boss GameObject
3. No boss-related references needed

## Benefits

### **Modularity**
- ✅ **Independent operation**: Each mode works without others
- ✅ **Easy switching**: Change modes via Inspector
- ✅ **Clean separation**: No cross-dependencies between modes

### **Flexibility**
- ✅ **Multiple use cases**: Training, testing, development
- ✅ **Scalable complexity**: Start simple, add complexity as needed
- ✅ **Component reuse**: Share common components across modes

### **Maintainability**
- ✅ **Clear responsibilities**: Each component has a specific role
- ✅ **Easy debugging**: Isolated issues to specific modes
- ✅ **Future-proof**: Easy to add new boss types

## Usage Examples

### **Player AI Training (Auto Mode)**
```csharp
// In EpisodeManager Inspector:
Boss Mode: Auto
AutoBoss: [Reference to AutoBoss component]
// Leave Q-learning references empty
```

### **Full AI Training (QLearning Mode)**
```csharp
// In EpisodeManager Inspector:
Boss Mode: QLearning
BossRewardManager: [Reference to BossRewardManager]
BossQLearning: [Reference to BossQLearning]
AIBoss: [Reference to AIBoss]
BossEnemy: [Reference to BossEnemy]
```

### **Movement Training (None Mode)**
```csharp
// In EpisodeManager Inspector:
Boss Mode: None
// No boss references needed
```

## Troubleshooting

### **"BossRewardManager reference missing"**
- **Cause**: Q-Learning mode requires BossRewardManager
- **Solution**: Either assign BossRewardManager or switch to Auto/None mode

### **"AutoBoss component not assigned"**
- **Cause**: Auto mode requires AutoBoss component
- **Solution**: Add AutoBoss component to boss GameObject

### **"Boss GameObject not assigned"**
- **Cause**: Boss mode selected but no boss GameObject assigned
- **Solution**: Assign boss GameObject or switch to None mode

### **Performance Issues**
- **Auto Mode**: Generally better performance than Q-Learning
- **None Mode**: Best performance, no boss overhead
- **QLearning Mode**: Most complex, may have performance impact

## Future Enhancements

### **Potential New Boss Types**
- **Scripted Boss**: Predefined attack patterns
- **Difficulty Boss**: Adjustable difficulty levels
- **Multi-Boss**: Multiple bosses in one episode
- **Dynamic Boss**: Boss that changes behavior mid-episode

### **Enhanced AutoBoss Features**
- **Attack Patterns**: More sophisticated attack sequences
- **Difficulty Scaling**: Adjustable based on player performance
- **Environmental Interaction**: Use of traps and hazards
- **Phase Transitions**: Different behaviors at different health levels
