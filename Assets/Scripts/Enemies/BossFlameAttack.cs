using System.Collections;

using UnityEngine;

public class BossFlameAttack : MonoBehaviour
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _warningTime = 1.5f;
    [SerializeField] private float _fireActiveTime = 3f;
    [SerializeField] private AudioClip _fireSound;

    private SpriteRenderer _spriteRenderer;
    private bool _active = false;
    private float _timeSinceLastReward = 0f; // Track time since last reward
    private float _rewardCooldown = 1.55f; // 1 second cooldown for giving the reward

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Activate(Vector3 position)
    {
        transform.position = position;
        StartCoroutine(FireSequence());
    }

    private IEnumerator FireSequence()
    {
        // Show warning color
        _spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(_warningTime);

        // Ignite the fire
        SoundManager.instance.PlaySound(_fireSound, gameObject);
        _spriteRenderer.color = Color.white;
        _active = true;

        yield return new WaitForSeconds(_fireActiveTime);

        // Destroy the fire effect after duration
        //Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (_active && collision.CompareTag("Player"))
        {
            collision.GetComponent<Health>()?.TakeDamage(_damage);
            AIBoss boss = FindObjectOfType<AIBoss>(); // Reference to the boss
            if (boss != null)
            {
                boss.flameMissed = false;
                // Update the timer
                _timeSinceLastReward += Time.deltaTime;

                // Only give the reward if 1 second has passed since the last reward
                if (_timeSinceLastReward >= _rewardCooldown)
                {
                    if (boss.rm != null)
                    {
                        boss.rm.ReportHitPlayer();
                    }

                    // Reset the timer after rewarding
                    _timeSinceLastReward = 0f;
                }
            }
        }
    }
}
