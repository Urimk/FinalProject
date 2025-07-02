using System.Collections;

using UnityEngine;

/// <summary>
/// Handles health, damage, invulnerability, and death logic for a character.
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    // ==================== Constants ====================
    private const int IgnoreLayerA = 8;
    private const int IgnoreLayerB = 9;
    private static readonly Color HurtColor = new Color(1, 0, 0, 0.5f);
    private static readonly Color NormalColor = Color.white;
    private const int DefaultScoreValue = 100;

    // ==================== Serialized Fields ====================
    [SerializeField] private GroundManager groundManager;
    [SerializeField] private PlayerAttack _playerAttack;
    [Header("Health")]
    [SerializeField] public float startingHealth;
    public float currentHealth { get; private set; }
    private Animator _animator;
    public bool dead;

    [Header("iFrames")]
    [SerializeField] private float _iFramesDuration;
    [SerializeField] private int _numberOfFlashes;
    private SpriteRenderer _spriteRenderer;

    [Header("Components")]
    [SerializeField] private Behaviour[] _components;

    [Header("Sounds")]
    [SerializeField] private AudioClip _deathSound;
    [SerializeField] private AudioClip _hurtSound;
    public bool invulnerable;
    private int _isFirstHealth = 1;
    private PlayerAI _playerAI;
    private PlayerMovement _player;
    [Header("Score")]
    [SerializeField] private int scoreValue = DefaultScoreValue;

    // ==================== Private Fields ====================
    private float _maxDamageThisFrame;
    private bool _isDamageQueued;

    /// <summary>
    /// Initializes health and component references.
    /// </summary>
    private void Awake()
    {
        currentHealth = startingHealth;
        _playerAI = GetComponent<PlayerAI>();
        _player = GetComponent<PlayerMovement>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (_isDamageQueued)
        {
            ApplyDamage(_maxDamageThisFrame);
            _maxDamageThisFrame = 0;
            _isDamageQueued = false;
        }
    }

    /// <summary>
    /// Queues damage to be applied, respecting invulnerability.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (invulnerable)
        {
            return;
        }
        if (!_isDamageQueued || damage > _maxDamageThisFrame)
        {
            _maxDamageThisFrame = damage;
            _isDamageQueued = true;
            StartCoroutine(Invulnerability());
        }
    }
    public event System.Action<float> OnDamaged;
    private void ApplyDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, startingHealth);

        if (_player != null)
        {
            _player.LosePowerUp();
        }
        if (currentHealth > 0)
        {
            if (_playerAI != null)
            {
                OnDamaged?.Invoke(damage);
            }
            _animator.SetTrigger("hurt");
            SoundManager.instance.PlaySound(_hurtSound, gameObject);
        }
        else
        {
            if (!dead)
            {
                dead = true;
                _animator.SetTrigger("die");
                if (groundManager != null)
                {
                    groundManager.OnPlayerDeath();
                }
                foreach (Behaviour component in _components)
                {
                    component.enabled = false;
                }
                if (CompareTag("Enemy"))
                {
                    ScoreManager.Instance.AddScore(scoreValue);
                }
                if (_player != null)
                {
                    _playerAttack.UnequipWeapon();
                }
                OnDamaged?.Invoke(damage);
                SoundManager.instance.PlaySound(_deathSound, gameObject);
            }
        }
    }

    /// <summary>
    /// Adds health to the character, up to the starting value.
    /// </summary>
    public bool AddHealth(float health)
    {
        if (currentHealth == startingHealth || dead)
        {
            return false;
        }
        currentHealth = Mathf.Clamp(currentHealth + health, 0, startingHealth);
        return true;
    }

    /// <summary>
    /// Handles invulnerability frames and visual feedback.
    /// </summary>
    private IEnumerator Invulnerability()
    {
        invulnerable = true;
        Physics2D.IgnoreLayerCollision(IgnoreLayerA, IgnoreLayerB, true);
        for (int i = 0; i < _numberOfFlashes; i++)
        {
            _spriteRenderer.color = HurtColor;
            yield return new WaitForSeconds(_iFramesDuration / (_numberOfFlashes * 2));
            _spriteRenderer.color = NormalColor;
            yield return new WaitForSeconds(_iFramesDuration / (_numberOfFlashes * 2));
        }
        Physics2D.IgnoreLayerCollision(IgnoreLayerA, IgnoreLayerB, false);
        invulnerable = false;
    }
    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Respawns the character, restoring health and enabling components.
    /// </summary>
    public void Respawn()
    {
        dead = false;
        AddHealth(startingHealth);
        _animator.ResetTrigger("die");
        _animator.Play("idle");
        StartCoroutine(Invulnerability());
        foreach (Behaviour component in _components)
        {
            component.enabled = true;
        }
    }
    public void ResetHealth()
    {
        currentHealth = startingHealth;
        dead = false;
    }

    public void SetFirstHealth(int firstHealth)
    {
        _isFirstHealth = firstHealth;
    }

    public int GetFirstHealth()
    {
        return _isFirstHealth;
    }

    public void ReEnableComponents()
    {
        foreach (Behaviour component in _components)
        {
            component.enabled = true;
        }
        scoreValue = 0;
    }

    public float GetHealth()
    {
        return currentHealth;
    }
}
