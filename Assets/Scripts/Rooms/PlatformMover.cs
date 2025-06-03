using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool startFromPositive = false;
    [SerializeField] private bool isVertical = true;
    [SerializeField] private bool isNegative = false;

    [Header("Attachment Settings")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Distance threshold for attaching when player is on ground and close enough.")]
    [SerializeField] private float attachDistance = 2f;
    [Tooltip("LayerMask to filter ground/platform checks (should include this platform's layer).")]

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool movingPositive;
    private GameObject player;
    private Transform playerTransform;
    private Transform currentPlatformParent;

    private Rigidbody2D rb;

    private void Start()
    {
        // Movement setup
        startPos = transform.position;
        Vector3 dir = isVertical ? Vector3.up : Vector3.right;
        
        // Apply negative direction if isNegative is true
        if (isNegative)
            dir = -dir;
        
        targetPos = startFromPositive ? startPos - dir * moveDistance : startPos + dir * moveDistance;
        movingPositive = !startFromPositive;

        // Rigidbody setup for trigger/collision events
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Find the player and its feet check
        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Player with tag '" + playerTag + "' not found in scene.");
        }
    }

    private void Update()
    {
        // Platform movement
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            movingPositive = !movingPositive;
            Vector3 dir = isVertical ? Vector3.up : Vector3.right;
            
            // Apply negative direction if isNegative is true
            if (isNegative)
                dir = -dir;
                
            targetPos = movingPositive ? startPos + dir * moveDistance : startPos - dir * moveDistance;
        }

        // Attachment logic
        HandleAttachment();
    }

     private void HandleAttachment()
    {
        if (playerTransform == null)
            return;

        // Calculate distance between player and platform centers (or choose axis-specific)
        float distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);

        // Check if close enough and player is on ground
        var pm = player.GetComponent<PlayerMovement>();
        if (distanceToPlayer <= attachDistance && playerTransform.position.y > (transform.position.y + 1.2f) && pm != null && pm.isGrounded())
        {
            // Create or find an intermediate parent with neutral scale
                Transform intermediateParent = transform.Find("PlayerAttachPoint");
                if (intermediateParent == null)
                {
                    GameObject attachPoint = new GameObject("PlayerAttachPoint");
                    attachPoint.transform.SetParent(transform);
                    attachPoint.transform.localPosition = Vector3.zero;
                    attachPoint.transform.localRotation = Quaternion.identity;
                    attachPoint.transform.localScale = Vector3.one;
                    intermediateParent = attachPoint.transform;
                }
                
                playerTransform.SetParent(intermediateParent, true);
                currentPlatformParent = transform;
        }
        else if (currentPlatformParent == transform && pm != null && !pm.isGrounded())
        {
            // No longer close or on ground, detach
            playerTransform.SetParent(null);
            currentPlatformParent = null;
        }
    }
}