/// <summary>
/// Interface defining the contract for all collectable objects in the game.
/// Provides a consistent pattern for different types of collectables.
/// </summary>
public interface ICollectable
{

    /// <summary>
    /// Gets whether this collectable has been collected.
    /// </summary>
    bool IsCollected { get; }

    /// <summary>
    /// Manually triggers the collection of this collectable.
    /// </summary>
    void Collect();

    /// <summary>
    /// Resets this collectable to its collectable state.
    /// </summary>
    void Reset();
}
