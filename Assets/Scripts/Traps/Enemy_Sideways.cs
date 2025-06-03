using UnityEngine;

public class Enemy_Sideways : MonoBehaviour
{
    [SerializeField] private float movementDistance;
    [SerializeField] private float speed;
    [SerializeField] private float damage;
    [SerializeField] private bool moveVertically; // New parameter

    public bool movingNegative = true; // Replaces movingLeft
    private float minEdge;
    private float maxEdge;

    private void Awake()
    {
        float currentPos = moveVertically ? transform.position.y : transform.position.x;
        minEdge = currentPos - movementDistance;
        maxEdge = currentPos + movementDistance;
    }

    private void Update()
    {
        if (movingNegative)
        {
            if ((moveVertically && transform.position.y > minEdge) ||
                (!moveVertically && transform.position.x > minEdge))
            {
                Move(-1);
            }
            else
            {
                movingNegative = false;
            }
        }
        else
        {
            if ((moveVertically && transform.position.y < maxEdge) ||
                (!moveVertically && transform.position.x < maxEdge))
            {
                Move(1);
            }
            else
            {
                movingNegative = true;
            }
        }
    }

    private void Move(int direction)
    {
        Vector3 position = transform.position;

        if (moveVertically)
            position.y += direction * speed * Time.deltaTime;
        else
            position.x += direction * speed * Time.deltaTime;

        transform.position = position;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<Health>().TakeDamage(damage);
        }
    }
}
