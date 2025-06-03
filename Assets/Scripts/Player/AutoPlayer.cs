using System.Collections;
using UnityEngine;

public class AutoPlayer : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float detectionRange = 10f;
    public float fireballDodgeRange = 2f;
    public float fireRate = 1f;
    
    private Rigidbody2D rb;
    [SerializeField] private Transform boss;
    private float fireTimer;
    private bool isGrounded;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (boss == null) return;
        
        fireTimer += Time.deltaTime;

        MoveAI();
        AvoidFireballs();
        
        if (fireTimer >= fireRate)
        {
            fireTimer = 0;
            ShootBoss();
        }
    }

    void MoveAI()
    {
        float distanceToBoss = Vector2.Distance(transform.position, boss.position);
        float direction = boss.position.x > transform.position.x ? 1 : -1; // Determine direction

        if (distanceToBoss > detectionRange)
        {
            // Move closer to boss to stay in range
            MoveTowards(boss.position.x);
            transform.localScale = new Vector3(direction, 1, 1); // Flip sprite towards boss
        }
        else if (distanceToBoss < 3f)
        {
            // If pinned against a wall, jump TOWARD the boss to escape
            if (IsHorizontallyBlocked())
            {
                JumpOverBoss(direction);
            }
            else
            {
                // Otherwise, move away normally
                MoveAwayFrom(boss.position.x);
                transform.localScale = new Vector3(-direction, 1, 1);
            }
        }
    }

    bool IsHorizontallyBlocked()
    {
        return false;
    }

    void JumpOverBoss(float bossDirection)
    {
        if (isGrounded) // Only jump if on the ground
        {
            float jumpSpeed = 5f;  // Adjust to control jump arc
            float jumpForwardBoost = 2f; // Adjust for forward movement

            rb.velocity = new Vector2(bossDirection * jumpForwardBoost, jumpSpeed); 
            Debug.Log("Jumping over the boss!");
        }
    }

    


    void AvoidFireballs()
    {
        GameObject[] fireballs = GameObject.FindGameObjectsWithTag("Fireball"); // Boss fireballs should have this tag
        foreach (GameObject fireball in fireballs)
        {
            Vector2 fireballPosition = fireball.transform.position;
            Vector2 fireballVelocity = fireball.GetComponent<Rigidbody2D>().velocity;
            float timeToImpact = Mathf.Abs((transform.position.x - fireballPosition.x) / fireballVelocity.x);

            if (timeToImpact > 0 && timeToImpact < 1.5f && Mathf.Abs(transform.position.y - fireballPosition.y) < fireballDodgeRange)
            {
                // Jump if fireball is about to hit
                Jump();
                return;
            }
        }
    }

    void MoveTowards(float targetX)
    {
        float direction = targetX > transform.position.x ? 1 : -1;
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
    }

    void MoveAwayFrom(float targetX)
    {
        float direction = targetX > transform.position.x ? -1 : 1;
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
    }

    void Jump()
    {
        if (isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isGrounded = false;
        }
    }

    void ShootBoss()
    {
        Debug.Log("AI Shooting Boss");
        // Call your shooting function here
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}
