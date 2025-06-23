using UnityEngine;
// BombTrap.cs
public class BombTrap : MonoBehaviour
{
    public GameObject ExplosionPrefab;
    public float DelayBeforeExplosion = 1.5f;

    private bool _triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_triggered && other.CompareTag("Player"))
        {
            _triggered = true;
            GetComponent<Animator>().SetTrigger("TriggerBomb");
        }
    }

    void Explode()
    {
        Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
        GetComponent<Animator>().SetTrigger("hide");
        gameObject.SetActive(false);
        _triggered = false;
    }
}
