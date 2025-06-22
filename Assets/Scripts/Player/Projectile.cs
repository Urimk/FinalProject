using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed;
    private float _direction;
    private bool _hit;
    private float _lifetime;

    private BoxCollider2D _boxCollider;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();
    }

    // 1) In Projectile.cs, let _lifetime continue (even when hit),
    //    and/or deactivate immediately on hit:
    private void Update()
    {
        // Always update _lifetime so we eventually Despawn()
        _lifetime += Time.deltaTime;

        if (_lifetime > 5f)
        {
            Deactivate();
            return;
        }

        if (_hit)
            return;

        float movement = speed * Time.deltaTime * _direction;
        transform.Translate(movement, 0, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name == "Door" || other.gameObject.tag == "NoCollision" || other.gameObject.tag == "Player") return;

        _hit = true;
        _boxCollider.enabled = false;
        _animator.SetTrigger("explosion");

        if (other.TryGetComponent<IDamageable>(out var dmg) && other.gameObject.tag != "Player")
            dmg.TakeDamage(1);

        //deactivates in the animation end
    }


    // common Deactivate helper
    private void Deactivate()
    {
        _hit = false;
        _boxCollider.enabled = true;
        gameObject.SetActive(false);
    }


    public void SetDirection(float direction)
    {
        _lifetime = 0;
        _direction = direction;
        gameObject.SetActive(true);
        _hit = false;
        // Check if the BoxCollider2D exists before enabling it
        if (_boxCollider != null)
        {
            _boxCollider.enabled = true; // Enable the collider if it exists
        }

        float localScaleX = transform.localScale.x;
        if (Mathf.Sign(localScaleX) != direction)
        {
            localScaleX = -localScaleX;
        }
        transform.localScale = new Vector3(localScaleX, transform.localScale.y, transform.localScale.z);
    }
}
