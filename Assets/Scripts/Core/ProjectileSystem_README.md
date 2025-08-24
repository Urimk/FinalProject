# Projectile System Refactor

This document explains the new, simplified projectile system that replaces the old messy projectile scripts.

## Overview

The new system consists of:
- **BaseProjectile**: Abstract base class with all common functionality
- **PlayerProjectile**: For player projectiles (damages enemies)
- **EnemyProjectile**: For enemy projectiles (damages player)
- **BossProjectile**: For boss projectiles (damages player + AI training)
- **ProjectileSpawner**: Helper class for easy projectile spawning

## Key Improvements

### ✅ **Simplified and Organized**
- Clean inheritance hierarchy
- Consistent naming conventions
- Well-documented code
- Separation of concerns

### ✅ **Easy to Use**
- Simple API for spawning projectiles
- Automatic direction handling
- Built-in collision filtering
- Flexible configuration

### ✅ **No More Messy Code**
- No complex sprite flipping logic
- No parent transform dependency issues
- No manual movement calculations
- No scattered collision handling

## How to Use

### 1. Basic Projectile Spawning

```csharp
// Spawn a player projectile
ProjectileSpawner.SpawnPlayerProjectile(
    playerProjectilePrefab,
    spawnPosition,
    Vector2.right, // Direction
    1f, // Damage
    5f  // Speed
);

// Spawn an enemy projectile
ProjectileSpawner.SpawnEnemyProjectile(
    enemyProjectilePrefab,
    spawnPosition,
    Vector2.left, // Direction
    1f, // Damage
    5f, // Speed
    true // Apply recoil
);

// Spawn a boss projectile toward target
ProjectileSpawner.SpawnBossProjectile(
    bossProjectilePrefab,
    startPosition,
    targetPosition,
    1f, // Damage
    5f, // Speed
    rewardManager // For AI training
);
```

### 2. Manual Projectile Configuration

```csharp
// Create and configure manually
GameObject projectileObj = Instantiate(prefab, position, Quaternion.identity);
PlayerProjectile projectile = projectileObj.GetComponent<PlayerProjectile>();

projectile.SetDamage(2f);
projectile.SetSpeed(8f);
projectile.SetSize(1.5f);
projectile.LaunchInDirection(Vector2.up);
```

### 3. Different Direction Examples

```csharp
// Forward (right)
projectile.LaunchInDirection(Vector2.right);

// Backward (left)
projectile.LaunchInDirection(Vector2.left);

// Up
projectile.LaunchInDirection(Vector2.up);

// Diagonal
projectile.LaunchInDirection(new Vector2(1, 1).normalized);

// Toward target
Vector2 direction = (target.position - spawn.position).normalized;
projectile.LaunchInDirection(direction);

// Random direction
projectile.LaunchInDirection(Random.insideUnitCircle.normalized);
```

### 4. Using Projectile Pools

```csharp
// Spawn from pool (better performance)
BaseProjectile projectile = ProjectileSpawner.SpawnFromPool(
    projectilePool,
    spawnPosition,
    direction,
    damage,
    speed
);
```

## Migration Guide

### From Old PlayerProjectile to New PlayerProjectile

**Old way:**
```csharp
projectile.SetDirection(direction);
// Complex sprite flipping logic
// Manual movement handling
```

**New way:**
```csharp
projectile.LaunchInDirection(direction);
// Everything handled automatically!
```

### From Old EnemyProjectile to New EnemyProjectile

**Old way:**
```csharp
projectile.ActivateProjectile();
projectile.SetDirection(direction, invertMovement, invertVisual, invertY);
// Complex parent transform handling
// Manual collider growth logic
```

**New way:**
```csharp
projectile.LaunchInDirection(direction);
// Clean and simple!
```

### From Old BossProjectile to New BossProjectile

**Old way:**
```csharp
projectile.Launch(startPos, targetPos, speed);
// Manual reward reporting
// Complex collision handling
```

**New way:**
```csharp
projectile.Launch(startPos, targetPos, speed);
// Reward reporting handled automatically!
```

## Configuration

### Inspector Settings

Each projectile type has these common settings:
- **Speed**: Movement speed
- **Size**: Visual scale
- **Lifetime**: Time before auto-destroy
- **Rotate To Direction**: Whether to rotate sprite to face movement direction

### PlayerProjectile Specific
- **Damage**: Damage dealt to enemies
- **Flip Sprite**: Whether to flip sprite based on direction

### EnemyProjectile Specific
- **Damage**: Damage dealt to player
- **Apply Recoil**: Whether to apply knockback
- **Recoil Forces**: Horizontal/vertical recoil strength
- **Recoil Duration**: How long recoil lasts
- **Break With Fireball**: Whether destroyed by player projectiles

### BossProjectile Specific
- **Damage**: Damage dealt to player
- **Apply Recoil**: Whether to apply knockback
- **Recoil Forces**: Horizontal/vertical recoil strength
- **Recoil Duration**: How long recoil lasts
- **Reward Manager**: For AI training feedback

## Benefits

1. **Cleaner Code**: No more complex sprite flipping or parent transform logic
2. **Easier to Use**: Simple API for spawning and configuring projectiles
3. **Better Performance**: Built-in pooling support
4. **More Flexible**: Easy to extend and customize
5. **Consistent**: All projectiles work the same way
6. **Well Documented**: Clear examples and documentation

## Examples

See `ProjectileExample.cs` for complete usage examples including:
- Basic spawning
- Direction handling
- Pool usage
- Manual configuration
- Multiple directions
- Target aiming

## Migration Steps

1. Replace old projectile scripts with new ones
2. Update prefab references
3. Replace old spawning code with new API
4. Test and adjust as needed

The new system is much simpler and more maintainable while providing all the same functionality!
