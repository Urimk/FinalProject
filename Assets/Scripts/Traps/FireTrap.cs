using System.Collections;

using UnityEngine;

public class FireTrap : MonoBehaviour
{

    [SerializeField] private int damage;
    [Header("FireTrap Timers")]
    [SerializeField] private float activationDelay;
    [SerializeField] private float activeTime;

    [Header("Auto Cycle Settings")]
    [SerializeField] private bool alwaysActive = false; // Flag to enable auto-cycling
    [Tooltip("Time to wait between cycles when in always active mode")]
    [SerializeField] private float cycleWaitTime = 2f;
    [SerializeField] private float cycleStartDelay = 0f;

    [Header("Sound")]
    [SerializeField] private AudioClip fireSound;
    private Animator anim;
    private SpriteRenderer spriteRend;
    private bool triggered; // when the trap is triggered (can be still inactive)
    private bool active;
    private Health playerHealth;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // If always active mode is enabled, start the auto-cycle after delay
        if (alwaysActive)
        {
            StartCoroutine(StartAutoCycleWithDelay());
        }
    }

    private IEnumerator StartAutoCycleWithDelay()
    {
        Debug.Log($"Starting cycle delay: {cycleStartDelay} seconds");
        yield return new WaitForSeconds(cycleStartDelay);
        Debug.Log("Cycle delay finished, starting auto-cycle");
        StartCoroutine(AutoCycleFireTrap());
    }

    private void Update()
    {
        if (playerHealth != null && active)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            playerHealth = collision.GetComponent<Health>();

            // Only trigger manually if not in always active mode
            if (!alwaysActive && !triggered)
            {
                StartCoroutine(ActivateFireTrap());
            }

            if (active)
            {
                collision.GetComponent<Health>().TakeDamage(damage);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            playerHealth = null;
        }
    }

    private IEnumerator ActivateFireTrap()
    {
        // Set the trap as triggered and change color to red as a warning signal
        triggered = true;
        spriteRend.color = Color.red;

        // Wait for the activation delay, then reset color, mark trap as active, and play activation animation
        yield return new WaitForSeconds(activationDelay);
        SoundManager.instance.PlaySound(fireSound, gameObject);
        spriteRend.color = Color.white;
        active = true;
        anim.SetBool("activated", true);

        // Wait for the active duration, then deactivate trap, reset triggered, and stop activation animation
        yield return new WaitForSeconds(activeTime);
        active = false;
        triggered = false;
        anim.SetBool("activated", false);
    }

    private IEnumerator AutoCycleFireTrap()
    {
        while (alwaysActive)
        {
            // Warning phase (red color)
            spriteRend.color = Color.red;
            yield return new WaitForSeconds(activationDelay);

            // Active phase
            SoundManager.instance.PlaySound(fireSound, gameObject);
            spriteRend.color = Color.white;
            active = true;
            anim.SetBool("activated", true);
            yield return new WaitForSeconds(activeTime);

            // Inactive phase
            active = false;
            anim.SetBool("activated", false);
            yield return new WaitForSeconds(cycleWaitTime);
        }
    }
}
