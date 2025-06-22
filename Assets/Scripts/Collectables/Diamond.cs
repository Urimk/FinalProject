using UnityEngine;

public class Diamond : MonoBehaviour
{
    [SerializeField] private int _scoreValue = 50;
    public string collectableID;

    [Header("Sound")]
    [SerializeField] private AudioClip _collectSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            SoundManager.instance.PlaySound(_collectSound, gameObject);
            // Add score via the ScoreManager
            ScoreManager.Instance.AddScore(_scoreValue);

            // Deactivate the collectible after collecting
            gameObject.SetActive(false);
        }
    }
}
