using System.Collections;

using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
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
    private PlayerAI _playerAI; // Reference to PlayerAI
    private PlayerMovement _player; // Reference to PlayerAI
    [Header("Score")]
    [SerializeField] private int scoreValue = 100;



    private float _maxDamageThisFrame;
    private bool _isDamageQueued;

    private void Awake()
    {
        currentHealth = startingHealth;
        _playerAI = GetComponent<PlayerAI>(); // Get PlayerAI component
        _player = GetComponent<PlayerMovement>(); // Get PlayerAI component
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

    public void TakeDamage(float damage)
    {
        if (invulnerable)
        {
            return;
        }
        // Only queue the highest damage this frame
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
                OnDamaged?.Invoke(damage); // Notify AI of damage
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
                OnDamaged?.Invoke(damage); // Notify AI of damage
                SoundManager.instance.PlaySound(_deathSound, gameObject);
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
        for (int i = 0; i < _numberOfFlashes; i++)
        {
            _spriteRenderer.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(_iFramesDuration / (_numberOfFlashes * 2));
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(_iFramesDuration / (_numberOfFlashes * 2));
        }
        Physics2D.IgnoreLayerCollision(8, 9, false);
        invulnerable = false;
    }
    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

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
