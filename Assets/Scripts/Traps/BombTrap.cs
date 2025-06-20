using UnityEngine;
// BombTrap.cs
public class BombTrap : MonoBehaviour
{
    public GameObject explosionPrefab;
    public float delayBeforeExplosion = 1.5f;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            GetComponent<Animator>().SetTrigger("TriggerBomb");
        }
    }

    void Explode()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        GetComponent<Animator>().SetTrigger("hide");
        gameObject.SetActive(false);
        triggered = false;
    }
}
