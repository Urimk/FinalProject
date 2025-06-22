using System.Collections;

using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] protected float damage;
    [SerializeField] protected bool isRecoil = false;

    protected void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Apply damage
            collision.GetComponent<Health>().TakeDamage(damage);

            if (isRecoil)
            {
                Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 recoilDir = transform.up;
                    collision.GetComponent<PlayerMovement>().Recoil(this.transform.position, recoilDir);
                }
            }




        }
    }

}
