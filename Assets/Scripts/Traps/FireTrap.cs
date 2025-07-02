using System.Collections;

using UnityEngine;

/// <summary>
/// Trap that activates and damages the player, with optional auto-cycling.
/// </summary>
public class FireTrap : MonoBehaviour
{
    // === Constants ===
    private const string PlayerTag = "Player";
    private const string AnimatorActivated = "activated";
    private static readonly Color WarningColor = Color.red;
    private static readonly Color NormalColor = Color.white;

    // === Serialized Fields ===
    [SerializeField] private int _damage;
    [Header("FireTrap Timers")]
    [SerializeField] private float _activationDelay;
    [SerializeField] private float _activeTime;

    [Header("Auto Cycle Settings")]
    [SerializeField] private bool _alwaysActive = false; // Flag to enable auto-cycling
    [Tooltip("Time to wait between cycles when in always active mode")]
    [SerializeField] private float _cycleWaitTime = 2f;
    [SerializeField] private float _cycleStartDelay = 0f;

    [Header("Sound")]
    [SerializeField] private AudioClip _fireSound;
    private Animator _anim;
    private SpriteRenderer _spriteRend;
    private bool _triggered; // when the trap is triggered (can be still inactive)
    private bool _active;
    private Health _playerHealth;

    /// <summary>
    /// Unity Awake callback. Initializes components.
    /// </summary>
    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _spriteRend = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Unity Start callback. Starts auto-cycle if enabled.
    /// </summary>
    private void Start()
    {
        // If always active mode is enabled, start the auto-cycle after delay
        if (_alwaysActive)
        {
            StartCoroutine(StartAutoCycleWithDelay());
        }
    }

    /// <summary>
    /// Coroutine to delay the start of the auto-cycle.
    /// </summary>
    private IEnumerator StartAutoCycleWithDelay()
    {
        Debug.Log($"Starting cycle delay: {_cycleStartDelay} seconds");
        yield return new WaitForSeconds(_cycleStartDelay);
        Debug.Log("Cycle delay finished, starting auto-cycle");
        StartCoroutine(AutoCycleFireTrap());
    }

    /// <summary>
    /// Unity Update callback. Damages the player if active.
    /// </summary>
    private void Update()
    {
        if (_playerHealth != null && _active)
        {
            _playerHealth.TakeDamage(_damage);
        }
    }

    /// <summary>
    /// Handles player entering the trap trigger.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == PlayerTag)
        {
            _playerHealth = collision.GetComponent<Health>();

            // Only trigger manually if not in always active mode
            if (!_alwaysActive && !_triggered)
            {
                StartCoroutine(ActivateFireTrap());
            }

            if (_active)
            {
                collision.GetComponent<Health>().TakeDamage(_damage);
            }
        }
    }

    /// <summary>
    /// Handles player exiting the trap trigger.
    /// </summary>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == PlayerTag)
        {
            _playerHealth = null;
        }
    }

    /// <summary>
    /// Coroutine to activate the trap, damage the player, and reset.
    /// </summary>
    private IEnumerator ActivateFireTrap()
    {
        // Set the trap as triggered and change color to red as a warning signal
        _triggered = true;
        _spriteRend.color = WarningColor;

        // Wait for the activation delay, then reset color, mark trap as active, and play activation animation
        yield return new WaitForSeconds(_activationDelay);
        SoundManager.instance.PlaySound(_fireSound, gameObject);
        _spriteRend.color = NormalColor;
        _active = true;
        _anim.SetBool(AnimatorActivated, true);

        // Wait for the active duration, then deactivate trap, reset triggered, and stop activation animation
        yield return new WaitForSeconds(_activeTime);
        _active = false;
        _triggered = false;
        _anim.SetBool(AnimatorActivated, false);
    }

    /// <summary>
    /// Coroutine for auto-cycling the trap.
    /// </summary>
    private IEnumerator AutoCycleFireTrap()
    {
        while (_alwaysActive)
        {
            // Warning phase (red color)
            _spriteRend.color = WarningColor;
            yield return new WaitForSeconds(_activationDelay);

            // Active phase
            SoundManager.instance.PlaySound(_fireSound, gameObject);
            _spriteRend.color = NormalColor;
            _active = true;
            _anim.SetBool(AnimatorActivated, true);
            yield return new WaitForSeconds(_activeTime);

            // Inactive phase
            _active = false;
            _anim.SetBool(AnimatorActivated, false);
            yield return new WaitForSeconds(_cycleWaitTime);
        }
    }
}
