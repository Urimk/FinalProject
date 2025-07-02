using System.Collections;

using TMPro;

using UnityEngine;

/// <summary>
/// Represents a spike that can be sorted, moved, and reset for sorting puzzles.
/// </summary>
public class SortedSpike : MonoBehaviour
{
    // === Constants ===
    private const float InactiveYOffset = -2f;
    private const float PositionEpsilon = 0.01f;

    // === Serialized Fields ===
    public TextMeshPro IndexText; // The text showing the index
    public GameObject Spike;

    // === Private Fields ===
    private int _index;
    private bool _isActive;
    private Vector3 _basePosition; // Store the original base position

    /// <summary>
    /// Unity Start callback. Stores the base position and initial state.
    /// </summary>
    private void Start()
    {
        _isActive = Spike.activeSelf;
        // Store the base position when the spike is first created
        _basePosition = Spike.transform.position;
    }

    /// <summary>
    /// Sets the index and updates the display text.
    /// </summary>
    public void SetIndex(int newIndex)
    {
        _index = newIndex;
        IndexText.text = _index.ToString(); // Update the text
    }

    /// <summary>
    /// Gets the current index.
    /// </summary>
    public int GetIndex()
    {
        return _index;
    }

    /// <summary>
    /// Sets the active state of the spike.
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;
    }

    /// <summary>
    /// Returns whether the spike is active.
    /// </summary>
    public bool IsActive()
    {
        return _isActive;
    }

    /// <summary>
    /// Moves the spike vertically by a given offset over a duration.
    /// </summary>
    public IEnumerator MoveSpikeY(float offset, float duration)
    {
        Vector3 startPos = Spike.transform.position; // Use spike's position, not SortedSpike
        Vector3 targetPos = startPos + new Vector3(0, offset, 0);
        float elapsed = 0;

        while (elapsed < duration)
        {
            Spike.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Spike.transform.position = targetPos; // Final position update
    }

    /// <summary>
    /// Resets the spike to its base position based on active state.
    /// </summary>
    public void ResetToBasePosition()
    {
        // Reset the spike to its proper state based on whether it should be active
        if (_isActive)
        {
            // If spike should be active, make sure it's fully up (y position + 2 from base)
            Vector3 targetPos = new Vector3(Spike.transform.position.x, _basePosition.y, Spike.transform.position.z);
            Spike.transform.position = targetPos;
        }
        else
        {
            // If spike should be inactive, make sure it's at base level
            Vector3 targetPos = new Vector3(Spike.transform.position.x, _basePosition.y + InactiveYOffset, Spike.transform.position.z);
            Spike.transform.position = targetPos;
        }
    }

    /// <summary>
    /// Updates the base position to the spike's current position.
    /// </summary>
    public void UpdateBasePosition()
    {
        _basePosition = Spike.transform.position;
    }
}
