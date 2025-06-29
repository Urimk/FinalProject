using UnityEngine;

public class HealthCollectable : MonoBehaviour
{
    [Header("Collectable Info")]
    [SerializeField] private float _healthValue;

    [Header("Sound")]
    [SerializeField] private AudioClip _collectSound;

    private bool _healthAdded;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            _healthAdded = collision.GetComponent<Health>().AddHealth(_healthValue);
            if (_healthAdded)
            {
                SoundManager.instance.PlaySound(_collectSound, gameObject);
                gameObject.SetActive(false);
            }
        }
    }
}
