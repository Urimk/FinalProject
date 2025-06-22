using UnityEngine;

public class HealthCollectable : MonoBehaviour
{
    [SerializeField] private float healthValue;

    [Header("Sound")]
    [SerializeField] private AudioClip collectSound;
    private bool healthAdded;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            healthAdded = collision.GetComponent<Health>().AddHealth(healthValue);
            if (healthAdded)
            {
                SoundManager.instance.PlaySound(collectSound, gameObject);
                gameObject.SetActive(false);
            }
        }
    }
}
