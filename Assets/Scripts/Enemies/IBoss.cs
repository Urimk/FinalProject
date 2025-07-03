/// <summary>
/// Interface for boss enemy behaviors.
/// </summary>
public interface IBoss
{
    /// <summary>
    /// Returns true if the boss is currently charging or dashing.
    /// </summary>
    bool IsCurrentlyChargingOrDashing();

    /// <summary>
    /// Triggers the boss's death behavior.
    /// </summary>
    void Die();
}
