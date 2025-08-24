using UnityEngine;

/// <summary>
/// Helper class for spawning and configuring projectiles easily.
/// </summary>
public static class ProjectileSpawner
{
    /// <summary>
    /// Spawns a player projectile at the given position and direction.
    /// </summary>
    /// <param name="prefab">The projectile prefab</param>
    /// <param name="position">Spawn position</param>
    /// <param name="direction">Movement direction</param>
    /// <param name="damage">Damage amount</param>
    /// <param name="speed">Movement speed</param>
    /// <returns>The spawned projectile component</returns>
    public static PlayerProjectile SpawnPlayerProjectile(GameObject prefab, Vector2 position, Vector2 direction, float damage = 1f, float speed = -1f)
    {
        GameObject projectileObj = Object.Instantiate(prefab, position, Quaternion.identity);
        PlayerProjectile projectile = projectileObj.GetComponent<PlayerProjectile>();
        
        if (projectile != null)
        {
            projectile.SetDamage(damage);
            if (speed > 0f) projectile.SetSpeed(speed);
            projectile.LaunchInDirection(direction);
        }
        
        return projectile;
    }

    /// <summary>
    /// Spawns an enemy projectile at the given position and direction.
    /// </summary>
    /// <param name="prefab">The projectile prefab</param>
    /// <param name="position">Spawn position</param>
    /// <param name="direction">Movement direction</param>
    /// <param name="damage">Damage amount</param>
    /// <param name="speed">Movement speed</param>
    /// <param name="applyRecoil">Whether to apply recoil</param>
    /// <returns>The spawned projectile component</returns>
    public static EnemyProjectile SpawnEnemyProjectile(GameObject prefab, Vector2 position, Vector2 direction, float damage = 1f, float speed = -1f, bool applyRecoil = false)
    {
        GameObject projectileObj = Object.Instantiate(prefab, position, Quaternion.identity);
        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
        
        if (projectile != null)
        {
            projectile.SetDamage(damage);
            if (speed > 0f) projectile.SetSpeed(speed);
            projectile.SetRecoil(applyRecoil);
            projectile.LaunchInDirection(direction);
        }
        
        return projectile;
    }

    /// <summary>
    /// Spawns a boss projectile from start position toward target position.
    /// </summary>
    /// <param name="prefab">The projectile prefab</param>
    /// <param name="startPosition">Starting position</param>
    /// <param name="targetPosition">Target position</param>
    /// <param name="damage">Damage amount</param>
    /// <param name="speed">Movement speed</param>
    /// <param name="rewardManager">Boss reward manager for AI training</param>
    /// <returns>The spawned projectile component</returns>
    public static BossProjectile SpawnBossProjectile(GameObject prefab, Vector2 startPosition, Vector2 targetPosition, float damage = 1f, float speed = -1f, BossRewardManager rewardManager = null)
    {
        GameObject projectileObj = Object.Instantiate(prefab, startPosition, Quaternion.identity);
        BossProjectile projectile = projectileObj.GetComponent<BossProjectile>();
        
        if (projectile != null)
        {
            projectile.SetDamage(damage);
            if (speed > 0f) projectile.SetSpeed(speed);
            if (rewardManager != null) projectile.RewardManager = rewardManager;
            projectile.Launch(startPosition, targetPosition, speed);
        }
        
        return projectile;
    }

    /// <summary>
    /// Spawns a projectile from a pool (for better performance).
    /// </summary>
    /// <param name="projectilePool">Array of projectile GameObjects</param>
    /// <param name="position">Spawn position</param>
    /// <param name="direction">Movement direction</param>
    /// <param name="damage">Damage amount</param>
    /// <param name="speed">Movement speed</param>
    /// <returns>The spawned projectile component or null if no available projectile</returns>
    public static BaseProjectile SpawnFromPool(GameObject[] projectilePool, Vector2 position, Vector2 direction, float damage = 1f, float speed = -1f)
    {
        // Find an inactive projectile in the pool
        GameObject projectileObj = null;
        for (int i = 0; i < projectilePool.Length; i++)
        {
            if (projectilePool[i] != null && !projectilePool[i].activeInHierarchy)
            {
                projectileObj = projectilePool[i];
                break;
            }
        }

        if (projectileObj == null) return null;

        // Configure and activate the projectile
        projectileObj.transform.position = position;
        projectileObj.transform.rotation = Quaternion.identity;
        projectileObj.transform.parent = null;

        BaseProjectile projectile = projectileObj.GetComponent<BaseProjectile>();
        if (projectile != null)
        {
            projectile.SetDamage(damage);
            if (speed > 0f) projectile.SetSpeed(speed);
            projectile.LaunchInDirection(direction);
        }

        return projectile;
    }
}
