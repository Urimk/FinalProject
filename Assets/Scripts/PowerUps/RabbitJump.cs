using UnityEngine;

public class RabbitJump : MonoBehaviour
{
    [SerializeField] private int bonusJumps = 1;
    [SerializeField] private float bonusJumpPower = 1f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float hoverHeight = 0.2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // Create a smooth up-and-down motion
        transform.position = startPos + new Vector3(0, Mathf.Sin(Time.time * hoverSpeed) * hoverHeight, 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerMovement player = collision.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.ActivatePowerUp(bonusJumps, bonusJumpPower);
                gameObject.SetActive(false);
            }
        }
    }
}
