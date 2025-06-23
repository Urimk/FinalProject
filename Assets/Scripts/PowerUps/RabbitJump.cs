using UnityEngine;

public class RabbitJump : MonoBehaviour
{
    [SerializeField] private int _bonusJumps = 1;
    [SerializeField] private float _bonusJumpPower = 1f;
    [SerializeField] private float _hoverSpeed = 2f;
    [SerializeField] private float _hoverHeight = 0.2f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        // Create a smooth up-and-down motion
        transform.position = _startPos + new Vector3(0, Mathf.Sin(Time.time * _hoverSpeed) * _hoverHeight, 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerMovement player = collision.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.ActivatePowerUp(_bonusJumps, _bonusJumpPower);
                gameObject.SetActive(false);
            }
        }
    }
}
