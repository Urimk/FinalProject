/// <summary>
/// Interface for objects that can take damage.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Applies damage to the object.
    /// </summary>
    /// <param name="damage">Amount of damage to apply.</param>
    void TakeDamage(float damage);
}
