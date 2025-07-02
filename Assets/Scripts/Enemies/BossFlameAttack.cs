using System.Collections;

using UnityEngine;

/// <summary>
/// Handles the boss's flame attack, including warning, activation, and player damage/reward logic.
/// </summary>
public class BossFlameAttack : MonoBehaviour
{
    // ==================== Constants ====================
    private static readonly Color WarningColor = Color.red;
    private static readonly Color FireColor = Color.white;
    private const string PlayerTag = "Player";
    private const float DefaultRewardCooldown = 1.55f;
    private const float TimerReset = 0f;

    // ==================== Serialized Fields ====================
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _warningTime = 1.5f;
    [SerializeField] private float _fireActiveTime = 3f;
    [SerializeField] private AudioClip _fireSound;

    // ==================== Private Fields ====================
    private SpriteRenderer _spriteRenderer;
    private bool _active = false;
    private float _timeSinceLastReward = 0f;
    private float _rewardCooldown = DefaultRewardCooldown;

    /// <summary>
    /// Initializes the sprite renderer reference.
    /// </summary>
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Activates the flame attack at the given position.
    /// </summary>
    public void Activate(Vector3 position)
    {
        transform.position = position;
        StartCoroutine(FireSequence());
    }

    /// <summary>
    /// Handles the warning and active fire sequence.
    /// </summary>
    private IEnumerator FireSequence()
    {
        // Show warning color
        _spriteRenderer.color = WarningColor;

        yield return new WaitForSeconds(_warningTime);

        // Ignite the fire
        SoundManager.instance.PlaySound(_fireSound, gameObject);
        _spriteRenderer.color = FireColor;
        _active = true;

        yield return new WaitForSeconds(_fireActiveTime);

        // Destroy the fire effect after duration
        //Destroy(gameObject);
    }

    /// <summary>
    /// Damages the player and reports reward if the flame is active and the player is in the trigger.
    /// </summary>
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (_active && collision.CompareTag(PlayerTag))
        {
            collision.GetComponent<Health>()?.TakeDamage(_damage);
            AIBoss boss = FindObjectOfType<AIBoss>();
            if (boss != null)
            {
                boss.flameMissed = false;
                _timeSinceLastReward += Time.deltaTime;
                if (_timeSinceLastReward >= _rewardCooldown)
                {
                    if (boss.rewardManager != null)
                    {
                        boss.rewardManager.ReportHitPlayer();
                    }
                    _timeSinceLastReward = TimerReset;
                }
            }
        }
    }
}
