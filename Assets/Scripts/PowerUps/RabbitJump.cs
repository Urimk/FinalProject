using UnityEngine;

/// <summary>
/// Power-up that grants the player extra jumps and jump power, with a hovering animation.
/// </summary>
public class RabbitJump : MonoBehaviour
{
    // === Constants ===
    private const string PlayerTag = "Player";

    // === Serialized Fields ===
    [SerializeField] private int _bonusJumps = 1;
    [SerializeField] private float _bonusJumpPower = 1f;
    [SerializeField] private float _hoverSpeed = 2f;
    [SerializeField] private float _hoverHeight = 0.2f;

    // === Private Fields ===
    private Vector3 _startPos;

    /// <summary>
    /// Unity Start callback. Stores the initial position.
    /// </summary>
    private void Start()
    {
        _startPos = transform.position;
    }

    /// <summary>
    /// Unity Update callback. Animates the power-up with a hovering effect.
    /// </summary>
    private void Update()
    {
        // Create a smooth up-and-down motion
        transform.position = _startPos + new Vector3(0, Mathf.Sin(Time.time * _hoverSpeed) * _hoverHeight, 0);
    }

    /// <summary>
    /// Grants the power-up to the player on collision.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(PlayerTag))
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
