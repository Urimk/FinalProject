using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed;
    private float direction;
    private bool hit;
    private float lifetime;

    private BoxCollider2D boxCollider;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    // 1) In Projectile.cs, let lifetime continue (even when hit),
    //    and/or deactivate immediately on hit:
    private void Update()
    {
        // Always update lifetime so we eventually Despawn()
        lifetime += Time.deltaTime;

        if (lifetime > 5f)
        {
            Deactivate();
            return;
        }

        if (hit)
            return;

        float movement = speed * Time.deltaTime * direction;
        transform.Translate(movement, 0, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name == "Door" || other.gameObject.tag == "NoCollision" || other.gameObject.tag == "Player") return;

        hit = true;
        boxCollider.enabled = false;
        anim.SetTrigger("explosion");

        if (other.TryGetComponent<IDamageable>(out var dmg) && other.gameObject.tag != "Player")
            dmg.TakeDamage(1);

        //deactivates in the animation end
    }


    // common Deactivate helper
    private void Deactivate()
    {
        hit = false;
        boxCollider.enabled = true;
        gameObject.SetActive(false);
    }


    public void SetDirection(float _direction)
    {
        lifetime = 0;
        direction = _direction;
        gameObject.SetActive(true);
        hit = false;
        // Check if the BoxCollider2D exists before enabling it
        if (boxCollider != null)
        {
            boxCollider.enabled = true; // Enable the collider if it exists
        }

        float localScaleX = transform.localScale.x;
        if (Mathf.Sign(localScaleX) != _direction)
        {
            localScaleX = -localScaleX;
        }
        transform.localScale = new Vector3(localScaleX, transform.localScale.y, transform.localScale.z);
    }
}
