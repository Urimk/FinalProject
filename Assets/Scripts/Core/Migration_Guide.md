# Migration Guide for Updated Scripts

This guide shows the specific changes made to BirdEnemy, RangedEnemy, and ArrowTrap to work with the new projectile system.

## ✅ **BirdEnemy (EggLayingBird) - Updated**

### **Before (Old Way):**
```csharp
private void LayEgg()
{
    SoundManager.instance.PlaySound(_eggDropSound, gameObject);

    GameObject egg = _eggPrefabs[FindInactiveEgg()];
    egg.transform.position = _eggDropPoint.position;
    egg.GetComponent<EnemyProjectile>().SetDirection(Vector2.down);
    egg.transform.rotation = Quaternion.identity;
    egg.GetComponent<EnemyProjectile>().ActivateProjectile();
}
```

### **After (New Way):**
```csharp
private void LayEgg()
{
    SoundManager.instance.PlaySound(_eggDropSound, gameObject);

    GameObject egg = _eggPrefabs[FindInactiveEgg()];
    EnemyProjectile projectile = egg.GetComponent<EnemyProjectile>();
    
    if (projectile != null)
    {
        projectile.LaunchInDirection(Vector2.down);
    }
}
```

**Changes:**
- ❌ Removed manual position setting
- ❌ Removed manual rotation setting
- ❌ Removed separate `SetDirection()` and `ActivateProjectile()` calls
- ✅ Added single `LaunchInDirection()` call
- ✅ Added null check for safety

---

## ✅ **RangedEnemy - Updated**

### **Before (Old Way):**
```csharp
private void RangedAttack()
{
    SoundManager.instance.PlaySound(_fireballSound, gameObject);
    _cooldownTimer = 0;
    EnemyProjectile fireball = _fireballs[FindFireball()].GetComponent<EnemyProjectile>();
    fireball.transform.position = _firepoint.position;
    fireball.StoreInitialWorldPosition(); // New method
    fireball.ActivateProjectile();
}
```

### **After (New Way):**
```csharp
private void RangedAttack()
{
    SoundManager.instance.PlaySound(_fireballSound, gameObject);
    _cooldownTimer = 0;
    
    GameObject fireballObj = _fireballs[FindFireball()];
    EnemyProjectile fireball = fireballObj.GetComponent<EnemyProjectile>();
    
    if (fireball != null)
    {
        fireball.LaunchInDirection(transform.right);
    }
}
```

**Changes:**
- ❌ Removed manual position setting
- ❌ Removed `StoreInitialWorldPosition()` call
- ❌ Removed `ActivateProjectile()` call
- ✅ Added single `LaunchInDirection()` call
- ✅ Added null check for safety
- ✅ Uses `transform.right` for direction (shoots forward)

---

## ✅ **ArrowTrap - Updated**

### **Before (Old Way):**
```csharp
private void Attack()
{
    _cooldownTimer = 0;
    if (SoundManager.instance != null && _arrowSound != null)
    {
        SoundManager.instance.PlaySound(_arrowSound, gameObject);
    }
    int idx = FindArrow();
    _arrows[idx].transform.position = _firepoint.position;
    Vector2 direction = _firepoint.right;
    var projectile = _arrows[idx].GetComponent<EnemyProjectile>();
    projectile.GetComponent<EnemyProjectile>().SetDirection(direction.normalized);
    projectile.SetSpeed(_speed);
    projectile.SetComingOut(_isComingOut);
    projectile.GetComponent<EnemyProjectile>().ActivateProjectile();
}
```

### **After (New Way):**
```csharp
private void Attack()
{
    _cooldownTimer = 0;
    if (SoundManager.instance != null && _arrowSound != null)
    {
        SoundManager.instance.PlaySound(_arrowSound, gameObject);
    }
    
    int idx = FindArrow();
    GameObject arrowObj = _arrows[idx];
    EnemyProjectile projectile = arrowObj.GetComponent<EnemyProjectile>();
    
    if (projectile != null)
    {
        Vector2 direction = _firepoint.right;
        projectile.SetSpeed(_speed);
        projectile.SetComingOut(_isComingOut);
        projectile.LaunchInDirection(direction);
    }
}
```

**Changes:**
- ❌ Removed manual position setting
- ❌ Removed redundant `GetComponent<>()` calls
- ❌ Removed separate `SetDirection()` and `ActivateProjectile()` calls
- ✅ Added single `LaunchInDirection()` call
- ✅ Added null check for safety
- ✅ Cleaner variable naming

---

## 🎯 **Key Benefits of the Updates**

### **1. Simpler Code**
- **Before:** 4-5 lines of setup code
- **After:** 1 line with `LaunchInDirection()`

### **2. No More Manual Transform Handling**
- **Before:** Manual position and rotation setting
- **After:** Automatic handling by BaseProjectile

### **3. Better Error Handling**
- **Before:** No null checks
- **After:** Safe null checks added

### **4. Consistent API**
- **Before:** Different methods for each script
- **After:** Same `LaunchInDirection()` method everywhere

### **5. Backward Compatibility**
- Legacy methods like `ActivateProjectile()` still work
- No breaking changes to existing prefabs

---

## 🔧 **What You Need to Do**

### **1. Update Prefabs (Optional)**
- The scripts will work with existing prefabs
- You can optionally update prefab components to use the new BaseProjectile settings

### **2. Test the Changes**
- Run the game and test each enemy/trap
- Verify projectiles move correctly
- Check that damage and collision still work

### **3. Remove Old Code (Optional)**
- You can remove the legacy methods from EnemyProjectile if you're not using them elsewhere
- The new system is fully backward compatible

---

## 🚀 **Next Steps**

1. **Test the updated scripts** in your game
2. **Update other projectile-using scripts** using the same pattern
3. **Consider using ProjectileSpawner** for even cleaner code
4. **Remove legacy methods** once all scripts are updated

The new system is much cleaner and easier to maintain!
