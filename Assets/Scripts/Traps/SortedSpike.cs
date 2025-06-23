using System.Collections;

using TMPro;

using UnityEngine;

public class SortedSpike : MonoBehaviour
{
    public TextMeshPro IndexText; // The text showing the index
    public GameObject Spike;
    private int _index;
    private bool _isActive;
    private Vector3 _basePosition; // Store the original base position

    private void Start()
    {
        _isActive = Spike.activeSelf;
        // Store the base position when the spike is first created
        _basePosition = Spike.transform.position;
    }

    public void SetIndex(int newIndex)
    {
        _index = newIndex;
        IndexText.text = _index.ToString(); // Update the text
    }

    public int GetIndex()
    {
        return _index;
    }

    public void SetActive(bool active)
    {
        _isActive = active;
    }

    public bool IsActive()
    {
        return _isActive;
    }

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
            Vector3 targetPos = new Vector3(Spike.transform.position.x, _basePosition.y - 2f, Spike.transform.position.z);
            Spike.transform.position = targetPos;
        }
    }

    // Optional: Method to update base position if spikes are moved permanently
    public void UpdateBasePosition()
    {
        _basePosition = Spike.transform.position;
    }
}
