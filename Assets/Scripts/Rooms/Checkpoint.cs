using UnityEngine;

/// <summary>
/// Represents a checkpoint in the level that can be activated by the player.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    // ==================== Fields ====================
    private bool _isActivated = false;

    // ==================== Properties ====================
    /// <summary>
    /// True if the checkpoint has been activated by the player.
    /// </summary>
    public bool IsActivated
    {
        get => _isActivated;
        set => _isActivated = value;
    }
}
