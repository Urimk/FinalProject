using UnityEngine;

/// <summary>
/// Syncs the fireball holder's scale with the enemy's scale for consistent visuals.
/// </summary>
public class FireballHolder : MonoBehaviour
{
    [SerializeField] private Transform _enemy;

    /// <summary>
    /// Updates the holder's scale to match the enemy's scale every frame.
    /// </summary>
    private void Update()
    {
        transform.localScale = _enemy.localScale;
    }
}
