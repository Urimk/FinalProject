using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private GroundManager groundManager;
    [SerializeField] private PlayerAttack playerAttack;
    [Header("Health")]
    [SerializeField] public float startingHealth;
    public float currentHealth { get; private set; }
    private Animator anim;
    public bool dead;

    [Header("iFrames")]
    [SerializeField] private float iFramesDuration;
    [SerializeField] private int numberOfFlashes;
    private SpriteRenderer spriteRend;

    [Header("Components")]
    [SerializeField] private Behaviour[] components;

    [Header("Sounds")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip hurtSound;
    public bool invulnerable;
    private int isFirstHealth = 1;
    private PlayerAI playerAI; // Reference to PlayerAI
    private PlayerMovement player; // Reference to PlayerAI
    [Header("Score")]
    [SerializeField] private int scoreValue = 100;



    private float maxDamageThisFrame;
    private bool isDamageQueued;

    private void Awake()
    {
        currentHealth = startingHealth;
        playerAI = GetComponent<PlayerAI>(); // Get PlayerAI component
        player = GetComponent<PlayerMovement>(); // Get PlayerAI component
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (isDamageQueued)
        {
            ApplyDamage(maxDamageThisFrame);
            maxDamageThisFrame = 0;
            isDamageQueued = false;
        }
    }

    public void TakeDamage(float damage)
    {
        if (invulnerable)
        {
            return;
        }
        // Only queue the highest damage this frame
        if (!isDamageQueued || damage > maxDamageThisFrame)
        {
            maxDamageThisFrame = damage;
            isDamageQueued = true;
            StartCoroutine(Invulnerability());
        }
    }
    public event System.Action<float> OnDamaged;
    private void ApplyDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, startingHealth);

        if (player != null)
        {
            player.LosePowerUp();
        }
        if (currentHealth > 0)
        {
            if (playerAI != null)
            {
                OnDamaged?.Invoke(damage); // Notify AI of damage
            }
            anim.SetTrigger("hurt");
            SoundManager.instance.PlaySound(hurtSound, gameObject);

        }
        else
        {
            if (!dead)
            {
                dead = true;
                anim.SetTrigger("die");
                if (groundManager != null)
                {
                    groundManager.OnPlayerDeath();
                }
                foreach (Behaviour component in components)
                {
                    component.enabled = false;
                }
                if (CompareTag("Enemy"))
                {
                    ScoreManager.Instance.AddScore(scoreValue);
                }
                if (player != null)
                {
                    playerAttack.UnequipWeapon();
                }
                OnDamaged?.Invoke(damage); // Notify AI of damage
                SoundManager.instance.PlaySound(deathSound, gameObject);
            }
        }
    }

    public bool AddHealth(float health)
    {
        if (currentHealth == startingHealth || dead)
        {
            return false;
        }
        currentHealth = Mathf.Clamp(currentHealth + health, 0, startingHealth);
        return true;
    }

    private IEnumerator Invulnerability()
    {
        invulnerable = true;
        Physics2D.IgnoreLayerCollision(8, 9, true);
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white;
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }
        Physics2D.IgnoreLayerCollision(8, 9, false);
        invulnerable = false;
    }
    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void HRespawn()
    {
        dead = false;
        AddHealth(startingHealth);
        anim.ResetTrigger("die");
        anim.Play("idle");
        StartCoroutine(Invulnerability());
        foreach (Behaviour component in components)
        {
            component.enabled = true;
        }
    }
    public void ResetHealth()
    {
        currentHealth = startingHealth;
        dead = false;
    }

    public void setFirstHealth(int firstHealth)
    {
        isFirstHealth = firstHealth;
    }

    public int getFirstHealth()
    {
        return isFirstHealth;
    }

    public void reEnableComponents()
    {
        foreach (Behaviour component in components)
        {
            component.enabled = true;
        }
        scoreValue = 0;
    }

    public float getHealth()
    {
        return currentHealth;
    }
}
