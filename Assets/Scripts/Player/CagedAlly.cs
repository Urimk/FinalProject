using UnityEngine;

public class CagedAlly : MonoBehaviour
{
    [Tooltip("Reference to the ally GameObject to activate.")]
    [SerializeField] private GameObject ally;

    [Tooltip("Optional destruction effect (e.g., particles, sound)")]
    [SerializeField] private GameObject destructionEffect;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerFireball"))
        {
            ActivateAlly();

            if (destructionEffect != null)
                Instantiate(destructionEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

    private void ActivateAlly()
    {
        if (ally != null)
        {
            ally.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No ally assigned to " + gameObject.name);
        }
    }
}
