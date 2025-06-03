using UnityEngine;

public class BossProjectile : EnemyDamage {
    [SerializeField] private float speed;
    [SerializeField] private float size;
    [SerializeField] private float resetTime;
    private float lifeTime;
    private Animator anim;
    private bool hit;
    private BoxCollider2D collid;
    public BossRewardManager rm;


    private Vector2 direction; // Stores movement direction

    private void Awake() {
        anim = GetComponent<Animator>();
        collid = GetComponent<BoxCollider2D>();
    }

    public void ActivateProjectile() {
        hit = false;
        lifeTime = 0;
        gameObject.SetActive(true);
        collid.enabled = true;
    }

    public void Launch(Vector2 startPosition, Vector2 targetPosition, float speed)
    {

        // Reset state variables
        hit = false;
        lifeTime = 0;

        transform.localScale = new Vector3(size, size, 1f); 
        transform.position = startPosition;
        this.speed = speed;
        direction = (targetPosition - startPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Activate projectile
        gameObject.SetActive(true);
        collid.enabled = true;
    }

    private void Update() {
        if (hit) return;
        // Move the projectile
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        
        // Handle lifetime
        lifeTime += Time.deltaTime;
        if (lifeTime > resetTime) {
            if (rm != null)
            {
                rm.ReportAttackMissed();
            }
            Deactivate();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        
        if (collision.gameObject.tag == "NoCollision" || collision.gameObject.tag == "Enemy") {
            return;
        }
        bool hitPlayer = collision.tag == "Player";

        // Notify the boss that the fireball hit something
        if (rm != null)
        {
            if (hitPlayer) {
                rm.ReportHitPlayer(); // Pass hit result
            } else {
                rm.ReportAttackMissed(); // Pass hit result
            }
        }
        hit = true;
        base.OnTriggerStay2D(collision);
        collid.enabled = false;

        if (anim != null) {
            anim.SetTrigger("explosion");
        } else {
            Deactivate();
        }
    }

    public void SetDamage(int newDamage) {
        damage = newDamage;
    }

    public void SetSpeed(float newSpeed) {
        speed = newSpeed;
    }

    public void SetSize(float newSize) {
        size = newSize;
    }

    private void Deactivate() {
        gameObject.SetActive(false);
        transform.rotation = Quaternion.identity;
    }
}
