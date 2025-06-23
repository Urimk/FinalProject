using UnityEngine;
// Explosion.cs
public class Explosion : MonoBehaviour
{
    public float Damage = 1f;
    public float Lifetime = 0.5f;

    private void Start()
    {
        Destroy(gameObject, Lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<Health>().TakeDamage(Damage);
        }
    }
}
