using System.Collections;

using UnityEngine;

/// <summary>
/// Prototype AI for player movement, jumping, and boss avoidance/attack.
/// </summary>
public class AutoPlayer : MonoBehaviour
{
    // === Constants ===
    private const float DefaultMoveSpeed = 5f;
    private const float DefaultJumpForce = 12f;
    private const float DefaultDetectionRange = 10f;
    private const float DefaultFireballDodgeRange = 2f;
    private const float DefaultFireRate = 1f;
    private const float MinBossDistance = 3f;
    private const float JumpSpeed = 5f;
    private const float JumpForwardBoost = 2f;
    private const float TimeToImpactThreshold = 1.5f;
    private const string FireballTag = "Fireball";
    private const string GroundTag = "Ground";

    // === Serialized Fields ===
    [SerializeField] private Transform boss;
    public float moveSpeed = DefaultMoveSpeed;
    public float jumpForce = DefaultJumpForce;
    public float detectionRange = DefaultDetectionRange;
    public float fireballDodgeRange = DefaultFireballDodgeRange;
    public float fireRate = DefaultFireRate;

    // === Private Fields ===
    private Rigidbody2D _rigidbody2D;
    private float _fireTimer;
    private bool _isGrounded;

    /// <summary>
    /// Unity Start callback. Initializes components.
    /// </summary>
    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Unity Update callback. Handles AI movement, dodging, and shooting.
    /// </summary>
    void Update()
    {
        if (boss == null) return;
        _fireTimer += Time.deltaTime;
        MoveAI();
        AvoidFireballs();
        if (_fireTimer >= fireRate)
        {
            _fireTimer = 0;
            ShootBoss();
        }
    }

    /// <summary>
    /// Handles AI movement logic relative to the boss.
    /// </summary>
    void MoveAI()
    {
        float distanceToBoss = Vector2.Distance(transform.position, boss.position);
        float direction = boss.position.x > transform.position.x ? 1 : -1;
        if (distanceToBoss > detectionRange)
        {
            MoveTowards(boss.position.x);
            transform.localScale = new Vector3(direction, 1, 1);
        }
        else if (distanceToBoss < MinBossDistance)
        {
            if (IsHorizontallyBlocked())
            {
                JumpOverBoss(direction);
            }
            else
            {
                MoveAwayFrom(boss.position.x);
                transform.localScale = new Vector3(-direction, 1, 1);
            }
        }
    }

    /// <summary>
    /// Placeholder for horizontal block detection.
    /// </summary>
    bool IsHorizontallyBlocked()
    {
        return false;
    }

    /// <summary>
    /// Makes the AI jump over the boss if grounded.
    /// </summary>
    void JumpOverBoss(float bossDirection)
    {
        if (_isGrounded)
        {
            _rigidbody2D.velocity = new Vector2(bossDirection * JumpForwardBoost, JumpSpeed);
            Debug.Log("Jumping over the boss!");
        }
    }

    /// <summary>
    /// Avoids incoming fireballs by jumping if a collision is imminent.
    /// </summary>
    void AvoidFireballs()
    {
        GameObject[] fireballs = GameObject.FindGameObjectsWithTag(FireballTag);
        foreach (GameObject fireball in fireballs)
        {
            Vector2 fireballPosition = fireball.transform.position;
            Vector2 fireballVelocity = fireball.GetComponent<Rigidbody2D>().velocity;
            float timeToImpact = Mathf.Abs((transform.position.x - fireballPosition.x) / fireballVelocity.x);
            if (timeToImpact > 0 && timeToImpact < TimeToImpactThreshold && Mathf.Abs(transform.position.y - fireballPosition.y) < fireballDodgeRange)
            {
                Jump();
                return;
            }
        }
    }

    /// <summary>
    /// Moves the AI towards a target X position.
    /// </summary>
    void MoveTowards(float targetX)
    {
        float direction = targetX > transform.position.x ? 1 : -1;
        _rigidbody2D.velocity = new Vector2(direction * moveSpeed, _rigidbody2D.velocity.y);
    }

    /// <summary>
    /// Moves the AI away from a target X position.
    /// </summary>
    void MoveAwayFrom(float targetX)
    {
        float direction = targetX > transform.position.x ? -1 : 1;
        _rigidbody2D.velocity = new Vector2(direction * moveSpeed, _rigidbody2D.velocity.y);
    }

    /// <summary>
    /// Makes the AI jump if grounded.
    /// </summary>
    void Jump()
    {
        if (_isGrounded)
        {
            _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, jumpForce);
            _isGrounded = false;
        }
    }

    /// <summary>
    /// Placeholder for shooting at the boss.
    /// </summary>
    void ShootBoss()
    {
        Debug.Log("AI Shooting Boss");
    }

    /// <summary>
    /// Handles collision with the ground to reset grounded state.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(GroundTag))
        {
            _isGrounded = true;
        }
    }
}
