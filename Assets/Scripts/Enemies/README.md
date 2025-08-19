# Enemy Damage and Recoil System

## Overview

The `EnemyDamage` class provides a flexible damage system that allows each enemy to specify custom recoil parameters, creating varied gameplay experiences.

## Features

### Damage System
- **Damage Amount**: Each enemy can deal different amounts of damage
- **Instant Death**: Special damage value (-666) for instant kill mechanics
- **Health Integration**: Works with the `Health` component system

### Recoil System
- **Customizable Recoil**: Each enemy can specify unique recoil behavior
- **Horizontal Force**: Controls how far the player is knocked back horizontally
- **Vertical Force**: Controls the upward knockback force
- **Duration**: How long the recoil effect lasts
- **Direction**: Recoil direction can be customized (defaults to enemy's up direction)

## Configuration

### Inspector Fields

#### Damage Parameters
- **Damage**: Amount of damage dealt to the player
- **Is Recoil**: Enable/disable recoil for this enemy

#### Recoil Settings
- **Recoil Horizontal Force**: Horizontal knockback force (default: 10f)
- **Recoil Vertical Force**: Vertical knockback force (default: 5f)
- **Recoil Duration**: Duration of recoil effect (default: 0.3f)

## Usage Examples

### Weak Enemy (Small Knockback)
```
Damage: 10
Is Recoil: true
Recoil Horizontal Force: 5
Recoil Vertical Force: 3
Recoil Duration: 0.2
```

### Strong Enemy (Heavy Knockback)
```
Damage: 25
Is Recoil: true
Recoil Horizontal Force: 15
Recoil Vertical Force: 8
Recoil Duration: 0.5
```

### Boss Enemy (Massive Knockback)
```
Damage: 50
Is Recoil: true
Recoil Horizontal Force: 20
Recoil Vertical Force: 12
Recoil Duration: 0.8
```

## Technical Details

### How It Works
1. When a player enters the enemy's trigger area, `OnTriggerStay2D` is called
2. The enemy applies damage to the player's `Health` component
3. If recoil is enabled, the enemy calls `PlayerMovement.Recoil()` with custom parameters
4. The player's movement system handles the recoil effect

### Integration with PlayerMovement
The `PlayerMovement` class now has two `Recoil` method overloads:
- `Recoil(sourcePosition, recoilDirection)` - Uses default recoil values
- `Recoil(sourcePosition, recoilDirection, horizontalForce, verticalForce, duration)` - Uses custom values

### Recoil Effect
During recoil:
- Player movement input is disabled
- Knockback force is applied in the specified direction
- Collision detection is modified to allow recoil movement
- Effect automatically ends after the specified duration

## Best Practices

1. **Balance Recoil Values**: Stronger enemies should have stronger recoil, but not so strong that it becomes frustrating
2. **Consider Level Design**: High recoil values can push players into hazards or off platforms
3. **Test Different Values**: Experiment with different force and duration combinations
4. **Use Direction Wisely**: Consider the enemy's orientation and the desired knockback direction

## Extending the System

To create custom enemy types with unique recoil patterns:

1. Inherit from `EnemyDamage`
2. Override `OnTriggerStay2D` if you need custom logic
3. Set the recoil parameters in the inspector or via code
4. Consider adding visual effects or sound feedback

Example custom enemy:
```csharp
public class ExplosiveEnemy : EnemyDamage
{
    [SerializeField] private float explosionRadius = 3f;
    
    protected override void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Custom explosion logic
            float distance = Vector2.Distance(transform.position, collision.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius);
            
            // Apply scaled damage and recoil
            collision.GetComponent<Health>().TakeDamage(_damage * damageMultiplier);
            
            if (_isRecoil)
            {
                Vector2 direction = (collision.transform.position - transform.position).normalized;
                collision.GetComponent<PlayerMovement>().Recoil(
                    transform.position, 
                    direction, 
                    _recoilHorizontalForce * damageMultiplier,
                    _recoilVerticalForce * damageMultiplier,
                    _recoilDuration
                );
            }
        }
    }
}
```
