using UnityEngine;
using UnityEngine.Serialization;

public class Diamond : MonoBehaviour
{
    private const int DefaultScoreValue = 50; // Default score value for diamonds

    [Header("Collectable Info")]
    [SerializeField] private int _scoreValue = DefaultScoreValue;
    [FormerlySerializedAs("collectableID")]
    [SerializeField] private string _collectableID;

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
