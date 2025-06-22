using System.Collections;

using UnityEngine;

public class BossFlameAttack : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float warningTime = 1.5f;
    [SerializeField] private float fireActiveTime = 3f;
    [SerializeField] private AudioClip fireSound;

    private SpriteRenderer spriteRenderer;
    private bool active = false;

    private float timeSinceLastReward = 0f; // Track time since last reward
    private float rewardCooldown = 1.55f; // 1 second cooldown for giving the reward

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Activate(Vector3 position)
    {
        transform.position = position;
        StartCoroutine(FireSequence());
    }

    private IEnumerator FireSequence()
    {
        // Show warning color
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(warningTime);

        // Ignite the fire
        SoundManager.instance.PlaySound(fireSound, gameObject);
        spriteRenderer.color = Color.white;
        active = true;

        yield return new WaitForSeconds(fireActiveTime);

        // Destroy the fire effect after duration
        //Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (active && collision.CompareTag("Player"))
        {
            collision.GetComponent<Health>()?.TakeDamage(damage);
            AIBoss boss = FindObjectOfType<AIBoss>(); // Reference to the boss
            if (boss != null)
            {
                boss.flameMissed = false;
                // Update the timer
                timeSinceLastReward += Time.deltaTime;

                // Only give the reward if 1 second has passed since the last reward
                if (timeSinceLastReward >= rewardCooldown)
                {
                    if (boss.rm != null)
                    {
                        boss.rm.ReportHitPlayer();
                    }

                    // Reset the timer after rewarding
                    timeSinceLastReward = 0f;
                }
            }
        }
    }
}
