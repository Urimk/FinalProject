using UnityEngine;

public class EnemyProjectile : EnemyDamage
{
    [SerializeField] private float speed;
    [SerializeField] private float size;
    [SerializeField] private float resetTime;
    private float lifeTime;
    private Animator anim;
    private bool hit;
    private BoxCollider2D collid;
    private Vector2 fullColliderSize;
    private bool isComingOut = false;
    private float colliderGrowTime = 0.3f; // Adjust based on speed
    private float growTimer = 0f;
    private BoxCollider2D boxCollider;


    // Add field to store direction with default as Vector2.right
    // This maintains backward compatibility with existing enemies
    private Vector2 direction = Vector2.right;

    // Flag to determine if we should use directional movement
    private bool useCustomDirection = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        collid = GetComponent<BoxCollider2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        fullColliderSize = boxCollider.size;
    }

    public void ActivateProjectile()
    {
        hit = false;
        lifeTime = 0;
        gameObject.SetActive(true);
        collid.enabled = true;
        if (isComingOut)
        {
            boxCollider.size = Vector2.zero;
            boxCollider.offset = Vector2.zero;
            colliderGrowTime = speed / 2.25f;
            growTimer = 0f; // Reset grow timer

        }
        else
        {
            boxCollider.size = fullColliderSize;
        }
    }

    // New method to set direction// Updated method to separate sprite rotation from movement direction
    public void SetDirection(Vector2 newDirection, bool invertMovement = false, bool invertVisual = false, bool invertY = false)
    {
        // Normalize direction to ensure correct movement
        newDirection = newDirection.normalized;


        // Flip Y component if needed
        if (invertY)
        {
            newDirection.y = -newDirection.y;
        }

        // Set movement direction (invert if needed)
        direction = invertMovement ? -newDirection : newDirection;
        useCustomDirection = true;

        // Flip ONLY the X component for visual mirroring
        Vector2 visualDirection = new Vector2(invertVisual ? -newDirection.x : newDirection.x, newDirection.y);

        // Rotate sprite to match the visual direction
        float angle = Mathf.Atan2(visualDirection.y, visualDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetComingOut(bool value)
    {
        isComingOut = value;
    }





    private void Update()
    {
        if (hit) return;

        if (isComingOut)
        {
            growTimer += Time.deltaTime;
            float t = Mathf.Clamp01(growTimer / colliderGrowTime);

            // Calculate the new size - grow primarily in the movement direction
            Vector2 newSize;
            Vector2 newOffset;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                // Horizontal movement - grow horizontally, keep full Y size
                newSize = new Vector2(fullColliderSize.x * t, fullColliderSize.y);
                newOffset = new Vector2(direction.x > 0 ? (newSize.x - fullColliderSize.x) / 2f : (fullColliderSize.x - newSize.x) / 2f, 0);
            }
            else
            {
                // Vertical movement - grow vertically, keep full X size
                newSize = new Vector2(fullColliderSize.x, fullColliderSize.y * t);
                newOffset = new Vector2(0, direction.y > 0 ? (newSize.y - fullColliderSize.y) / 2f : (fullColliderSize.y - newSize.y) / 2f);
            }

            boxCollider.size = newSize;

            boxCollider.size = newSize;
            boxCollider.offset = newOffset;

            if (t >= 1f)
            {
                isComingOut = false; // Done growing
                boxCollider.size = fullColliderSize; // Ensure exact full size
                boxCollider.offset = Vector2.zero; // Reset offset
            }
        }


        transform.localScale = new Vector3(size, size, 1);
        // Use either the custom direction or the default right direction
        if (useCustomDirection)
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
        else
        {
            // Original behavior for backward compatibility
            transform.Translate(Vector2.right * speed * Time.deltaTime);
        }

        lifeTime += Time.deltaTime;
        if (lifeTime > resetTime)
        {
            gameObject.SetActive(false);

            // Reset the custom direction flag when deactivated
            useCustomDirection = false;
            transform.rotation = Quaternion.identity;
        }
    }

    // Rest of your methods remain the same
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "NoCollision" || collision.gameObject.tag == "Enemy")
        {
            return;
        }

        hit = true;
        base.OnTriggerStay2D(collision);
        collid.enabled = false;

        if (anim != null)
        {
            anim.SetTrigger("explosion");
            anim.SetTrigger("fade");
        }
        else
        {
            gameObject.SetActive(false);
        }
    }


    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetSize(float newSize)
    {
        size = newSize;
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
        // Reset the custom direction flag when deactivated
        useCustomDirection = false;
        transform.rotation = Quaternion.identity;
    }
}
