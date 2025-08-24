using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the new projectile system.
/// This shows different ways to spawn and configure projectiles.
/// </summary>
public class ProjectileExample : MonoBehaviour
{
    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject _playerProjectilePrefab;
    [SerializeField] private GameObject _enemyProjectilePrefab;
    [SerializeField] private GameObject _bossProjectilePrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _target;
    
    [Header("Projectile Settings")]
    [SerializeField] private float _damage = 1f;
    [SerializeField] private float _speed = 5f;
    
    [Header("Boss Settings")]
    [SerializeField] private BossRewardManager _rewardManager;

    // ==================== Example Methods ====================
    
    /// <summary>
    /// Example: Spawn a player projectile shooting forward
    /// </summary>
    public void SpawnPlayerProjectileForward()
    {
        if (_playerProjectilePrefab == null || _spawnPoint == null) return;
        
        Vector2 direction = transform.right; // Forward direction
        ProjectileSpawner.SpawnPlayerProjectile(
            _playerProjectilePrefab, 
            _spawnPoint.position, 
            direction, 
            _damage, 
            _speed
        );
    }

    /// <summary>
    /// Example: Spawn a player projectile toward a target
    /// </summary>
    public void SpawnPlayerProjectileAtTarget()
    {
        if (_playerProjectilePrefab == null || _spawnPoint == null || _target == null) return;
        
        Vector2 direction = (_target.position - _spawnPoint.position).normalized;
        ProjectileSpawner.SpawnPlayerProjectile(
            _playerProjectilePrefab, 
            _spawnPoint.position, 
            direction, 
            _damage, 
            _speed
        );
    }

    /// <summary>
    /// Example: Spawn an enemy projectile with recoil
    /// </summary>
    public void SpawnEnemyProjectileWithRecoil()
    {
        if (_enemyProjectilePrefab == null || _spawnPoint == null) return;
        
        Vector2 direction = Vector2.left; // Left direction
        ProjectileSpawner.SpawnEnemyProjectile(
            _enemyProjectilePrefab, 
            _spawnPoint.position, 
            direction, 
            _damage, 
            _speed, 
            true // Apply recoil
        );
    }

    /// <summary>
    /// Example: Spawn a boss projectile toward target
    /// </summary>
    public void SpawnBossProjectileAtTarget()
    {
        if (_bossProjectilePrefab == null || _spawnPoint == null || _target == null) return;
        
        ProjectileSpawner.SpawnBossProjectile(
            _bossProjectilePrefab, 
            _spawnPoint.position, 
            _target.position, 
            _damage, 
            _speed, 
            _rewardManager
        );
    }

    /// <summary>
    /// Example: Spawn projectiles in different directions
    /// </summary>
    public void SpawnProjectilesInAllDirections()
    {
        if (_enemyProjectilePrefab == null || _spawnPoint == null) return;
        
        Vector2[] directions = {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1, 1).normalized,
            new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized,
            new Vector2(-1, -1).normalized
        };

        foreach (Vector2 direction in directions)
        {
            ProjectileSpawner.SpawnEnemyProjectile(
                _enemyProjectilePrefab, 
                _spawnPoint.position, 
                direction, 
                _damage, 
                _speed
            );
        }
    }

    // ==================== Manual Spawning Examples ====================
    
    /// <summary>
    /// Example: Manual projectile spawning and configuration
    /// </summary>
    public void SpawnProjectileManually()
    {
        if (_playerProjectilePrefab == null || _spawnPoint == null) return;
        
        // Instantiate the projectile
        GameObject projectileObj = Instantiate(_playerProjectilePrefab, _spawnPoint.position, Quaternion.identity);
        PlayerProjectile projectile = projectileObj.GetComponent<PlayerProjectile>();
        
        if (projectile != null)
        {
            // Configure the projectile
            projectile.SetDamage(_damage);
            projectile.SetSpeed(_speed);
            projectile.SetSize(1.5f); // Make it bigger
            
            // Launch it
            projectile.LaunchInDirection(Vector2.right);
        }
    }

    /// <summary>
    /// Example: Using projectile pooling
    /// </summary>
    public void SpawnFromPool(GameObject[] projectilePool)
    {
        if (projectilePool == null || _spawnPoint == null) return;
        
        Vector2 direction = Random.insideUnitCircle.normalized; // Random direction
        
        BaseProjectile projectile = ProjectileSpawner.SpawnFromPool(
            projectilePool, 
            _spawnPoint.position, 
            direction, 
            _damage, 
            _speed
        );
        
        if (projectile == null)
        {
            Debug.LogWarning("No available projectiles in pool!");
        }
    }
}
