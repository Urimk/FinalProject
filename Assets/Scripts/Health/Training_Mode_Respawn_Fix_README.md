# Training Mode Respawn Fix

## Problem
When the player died during AI training, the normal respawn system was triggered, which:
1. Deducted lives from the player's life counter
2. Eventually destroyed the player when lives ran out
3. Prevented proper ML-Agents episode resets

## Solution
Modified the `Health` component to detect training mode and handle death differently:

### **Training Mode Detection**
The system detects training mode when ALL of these conditions are met:
- Player has a `PlayerAI` component
- Player has a `PlayerMovement` component
- `PlayerMovement.IsAIControlled` is `true`
- `PlayerAI.IsTraining` is `true`

### **Different Death Handling**

#### **Training Mode Death** (`HandleTrainingModeDeath()`)
- ✅ **Does NOT** disable player components
- ✅ **Does NOT** call `GroundManager.OnPlayerDeath()`
- ✅ **Does NOT** unequip weapons
- ✅ **Does NOT** deduct lives
- ✅ **Does NOT** play death animation (prevents `Deactivate()` animation event)
- ✅ **Allows** ML-Agents to handle episode reset and respawn
- ✅ **Triggers** `OnDamaged` event so `PlayerAI.HandlePlayerDamaged()` can end episode

#### **Normal Mode Death** (`HandleNormalModeDeath()`)
- ❌ **Disables** player components
- ❌ **Calls** `GroundManager.OnPlayerDeath()`
- ❌ **Unequips** weapons
- ❌ **Deducts** lives
- ❌ **Triggers** normal respawn system

## Files Modified

### **`Assets/Scripts/Health/Health.cs`**
- Added `_isInTrainingMode` field
- Added training mode detection in `Awake()`
- Split death handling into `HandleTrainingModeDeath()` and `HandleNormalModeDeath()`
- Added debug logging for training mode detection
- Modified death handling to not play death animation in training mode
- Prevents `Deactivate()` animation event from destroying player object

### **`Assets/Scripts/Player/PlayerMovement.cs`**
- Added `IsAIControlled` property to expose `_isAIControlled` field

### **`Assets/Scripts/Player/PlayerAI.cs`**
- Added `IsTraining` property to expose `isTraining` field
- Added header for training mode settings

### **`Assets/Scripts/Player/PlayerRespawn.cs`**
- Added `_isInTrainingMode` field
- Added training mode detection in `Awake()`
- Modified `Respawn()` method to skip normal respawn logic in training mode
- Added debug logging for training mode detection

## How It Works

1. **Player takes damage** → `Health.TakeDamage()` is called
2. **Health reaches 0** → `Health.ApplyDamage()` triggers death
3. **Training mode detected** → `HandleTrainingModeDeath()` is called
4. **Components stay enabled** → Player remains functional for ML-Agents
5. **OnDamaged event fired** → `PlayerAI.HandlePlayerDamaged()` is called
6. **Episode ends** → `PlayerAI.EndEpisode()` is called
7. **Episode resets** → `PlayerAI.OnEpisodeBegin()` resets everything
8. **Player respawns** → Fresh start for next training episode

## Verification Steps

### **1. Check Training Mode Detection**
Look for this message in console when the scene starts:
```
[Health] Training mode detected - death will be handled by ML-Agents
```

### **2. Test Death in Training Mode**
1. Start a training episode
2. Let the AI player die
3. Check console for:
   ```
   [Health] Player died in training mode - ML-Agents will handle respawn
   [PlayerAI] Player Died! Ending Episode.
   [PlayerAI] OnEpisodeBegin completed.
   ```

### **3. Verify No Life Deduction**
- The player should respawn immediately without losing lives
- No "Game Over" screen should appear
- Training should continue seamlessly

### **4. Test Normal Mode**
- Set `PlayerAI.isTraining = false`
- Player death should work normally (deduct lives, trigger respawn system)

## Troubleshooting

### **Player Still Loses Lives in Training**
1. Check that `PlayerAI.isTraining` is set to `true`
2. Check that `PlayerMovement._isAIControlled` is set to `true`
3. Verify both `PlayerAI` and `PlayerMovement` components exist
4. Look for training mode detection message in console

### **Player Doesn't Respawn in Training**
1. Check that `PlayerAI.OnEpisodeBegin()` is being called
2. Verify `EpisodeManager.Instance` exists and is properly configured
3. Check that all required components are enabled after episode reset

### **Normal Mode Death Not Working**
1. Check that `PlayerAI.isTraining` is set to `false`
2. Verify `GroundManager` reference is assigned in `Health` component
3. Check that `PlayerRespawn` component is properly configured

## Benefits

- ✅ **Seamless training**: AI can die and respawn without interruption
- ✅ **No life system**: Training doesn't depend on limited lives
- ✅ **Proper episode management**: ML-Agents handles all episode lifecycle
- ✅ **Backward compatibility**: Normal gameplay is unaffected
- ✅ **Clean separation**: Training and normal modes are clearly separated
