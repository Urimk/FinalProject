using UnityEngine;
using TMPro;
using System.Collections;

public class SortedSpike : MonoBehaviour
{
    public TextMeshPro indexText; // The text showing the index
    public GameObject spike;
    private int index;
    private bool isActive;
    private Vector3 basePosition; // Store the original base position

    void Start()
    {
        isActive = spike.activeSelf;
        // Store the base position when the spike is first created
        basePosition = spike.transform.position;
    }

    public void SetIndex(int newIndex)
    {
        index = newIndex;
        indexText.text = index.ToString(); // Update the text
    }

    public int GetIndex()
    {
        return index;
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }

    public bool IsActive()
    {
        return isActive;
    }

    public IEnumerator MoveSpikeY(float offset, float duration)
    {
        Vector3 startPos = spike.transform.position; // Use spike's position, not SortedSpike
        Vector3 targetPos = startPos + new Vector3(0, offset, 0);
        float elapsed = 0;

        while (elapsed < duration)
        {
            spike.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        spike.transform.position = targetPos; // Final position update
    }

    public void ResetToBasePosition()
    {
        // Reset the spike to its proper state based on whether it should be active
        if (isActive)
        {
            // If spike should be active, make sure it's fully up (y position + 2 from base)
            Vector3 targetPos = new Vector3(spike.transform.position.x, basePosition.y, spike.transform.position.z);
            spike.transform.position = targetPos;
        }
        else
        {
            // If spike should be inactive, make sure it's at base level
            Vector3 targetPos = new Vector3(spike.transform.position.x, basePosition.y - 2f, spike.transform.position.z);
            spike.transform.position = targetPos;
        }
    }

    // Optional: Method to update base position if spikes are moved permanently
    public void UpdateBasePosition()
    {
        basePosition = spike.transform.position;
    }
}