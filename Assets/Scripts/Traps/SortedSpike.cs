using System.Collections;

using TMPro;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Represents a spike that can be sorted, moved, and reset for sorting puzzles.
/// </summary>
public class SortedSpike : MonoBehaviour
{
    // === Constants ===
    private const float InactiveYOffset = -2f;
    private const float PositionEpsilon = 0.01f;

    // === Inspector Fields ===
    [Header("Spike Components")]
    [Tooltip("TextMeshPro component displaying the spike's index.")]
    [FormerlySerializedAs("indexText")]
    [SerializeField] private TextMeshPro _indexText;
    public TextMeshPro IndexText { get => _indexText; set => _indexText = value; }


    [Tooltip("GameObject representing the spike.")]
    [FormerlySerializedAs("spike")]
    [SerializeField] private GameObject _spike;
    public GameObject Spike { get => _spike; set => _spike = value; }

    // === Private Fields ===
    private int _index;
    private bool _isActive;
    private Vector3 _basePosition; // Store the original base position

    /// <summary>
    /// Unity Start callback. Stores the base position and initial state.
    /// </summary>
    private void Start()
    {
        _isActive = _spike.activeSelf;
        // Store the base position when the spike is first created
        _basePosition = _spike.transform.position;
    }

    /// <summary>
    /// Sets the index and updates the display text.
    /// </summary>
    /// <param name="newIndex">The new index to set.</param>
    public void SetIndex(int newIndex)
    {
        _index = newIndex;
        _indexText.text = _index.ToString(); // Update the text
    }

    /// <summary>
    /// Gets the current index.
    /// </summary>
    /// <returns>The current index value.</returns>
    public int GetIndex()
    {
        return _index;
    }

    /// <summary>
    /// Sets the active state of the spike.
    /// </summary>
    /// <param name="active">Whether the spike should be active.</param>
    public void SetActive(bool active)
    {
        _isActive = active;
    }

    /// <summary>
    /// Returns whether the spike is active.
    /// </summary>
    /// <returns>True if the spike is active; otherwise, false.</returns>
    public bool IsActive()
    {
        return _isActive;
    }

    /// <summary>
    /// Moves the spike vertically by a given offset over a duration.
    /// </summary>
    /// <param name="offset">The vertical offset to move.</param>
    /// <param name="duration">The duration of the movement.</param>
    /// <returns>Coroutine enumerator.</returns>
    public IEnumerator MoveSpikeY(float offset, float duration)
    {
        Vector3 startPos = _spike.transform.position; // Use spike's position, not SortedSpike
        Vector3 targetPos = startPos + new Vector3(0, offset, 0);
        float elapsed = 0;

        while (elapsed < duration)
        {
            _spike.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _spike.transform.position = targetPos; // Final position update
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
            Vector3 targetPos = new Vector3(_spike.transform.position.x, _basePosition.y, _spike.transform.position.z);
            _spike.transform.position = targetPos;
        }
        else
        {
            // If spike should be inactive, make sure it's at base level
            Vector3 targetPos = new Vector3(_spike.transform.position.x, _basePosition.y + InactiveYOffset, _spike.transform.position.z);
            _spike.transform.position = targetPos;
        }
    }

    /// <summary>
    /// Updates the base position to the spike's current position.
    /// </summary>
    public void UpdateBasePosition()
    {
        _basePosition = _spike.transform.position;
    }
}
