using System.Collections;

using UnityEngine;
using UnityEngine.Serialization;

    /// <summary>
    /// Handles health, damage, invulnerability, and death logic for a character.
    /// Supports both normal gameplay and AI training modes with different respawn behaviors.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
{
    // ==================== Constants ====================
    private const int IgnoreLayerA = 8;
    private const int IgnoreLayerB = 9;
    private static readonly Color HurtColor = new Color(1, 0, 0, 0.5f);
    private const int DefaultScoreValue = 100;

    // ==================== Serialized Fields ====================
    [SerializeField] private Health mainPlayer;
    [Header("Managers")]
    [Tooltip("Reference to the ground manager for player death handling.")]
    [FormerlySerializedAs("groundManager")]
    [SerializeField] private GroundManager _groundManager;

    [Header("Player Attack")]
    [Tooltip("Reference to the PlayerAttack component.")]
    [FormerlySerializedAs("playerAttack")]
    [SerializeField] private PlayerAttack _playerAttack;

    [Header("Health Settings")]
    [Tooltip("Starting health value for this character.")]
    [FormerlySerializedAs("startingHealth")]
    [SerializeField] private float _startingHealth;

    [Tooltip("Current health value for this character.")]
    [FormerlySerializedAs("currentHealth")]
    [SerializeField] private float _currentHealth;

    [Header("iFrames (Invulnerability)")]
    [Tooltip("Duration of invulnerability frames after taking damage.")]
    [FormerlySerializedAs("iFramesDuration")]
    [SerializeField] private float _iFramesDuration;
    [Tooltip("Number of flashes during invulnerability.")]
    [FormerlySerializedAs("numberOfFlashes")]

    [SerializeField] private int _numberOfFlashes;

    [Header("Components")]
    [Tooltip("Array of components to disable on death.")]
    [FormerlySerializedAs("components")]
    [SerializeField] private Behaviour[] _components;

    [Header("Sounds")]
    [Tooltip("Sound to play on death.")]
    [FormerlySerializedAs("deathSound")]
    [SerializeField] private AudioClip _deathSound;
    [Tooltip("Sound to play when hurt.")]
    [FormerlySerializedAs("hurtSound")]

    [SerializeField] private AudioClip _hurtSound;

    [Header("Score")]
    [Tooltip("Score value awarded for defeating this character.")]
    [FormerlySerializedAs("scoreValue")]
    [SerializeField] private int _scoreValue = DefaultScoreValue;

    // ==================== Private Fields ====================
    private float _maxDamageThisFrame;
    private bool _isDamageQueued;
    private Color _normalColor;

    // ==================== Properties ====================
    /// <summary>Current health of the character.</summary>
    public float CurrentHealth { get => _currentHealth; set => _currentHealth = value; }
    /// <summary>Starting health of the character.</summary>
    public float StartingHealth { get => _startingHealth; set => _startingHealth = value; }
    /// <summary>Score value for this character.</summary>
    public int ScoreValue => _scoreValue;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private bool _dead;
    private bool _invulnerable;
    private int _isFirstHealth = 1;
    private PlayerAI _playerAI;
    private PlayerMovement _player;
    private bool _isInTrainingMode = false;

    /// <summary>
    /// True if the character is dead.
    /// </summary>
    public bool Dead
    {
        get => _dead;
        private set => _dead = value;
    }

    /// <summary>
    /// True if the character is currently invulnerable.
    /// </summary>
    public bool Invulnerable
    {
        get => _invulnerable;
        private set => _invulnerable = value;
    }

    /// <summary>
    /// Initializes health and component references.
    /// </summary>
    private void Awake()
    {
        CurrentHealth = _startingHealth;
        _playerAI = GetComponent<PlayerAI>();
        _player = GetComponent<PlayerMovement>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _normalColor = _spriteRenderer.color;
        
        // Check if we're in training mode (AI-controlled player)
        _isInTrainingMode = _playerAI != null && _player != null && _player.IsAIControlled && _playerAI.IsTraining;
        
        // Debug logging for training mode detection
        if (_isInTrainingMode)
        {
            Debug.Log("[Health] Training mode detected - death will be handled by ML-Agents");
        }
    }

    private void LateUpdate()
    {
        if (_isDamageQueued)
        {
            ApplyDamage(_maxDamageThisFrame);
            _maxDamageThisFrame = 0;
            _isDamageQueued = false;
        }
        if (mainPlayer != null)
        {
            if (mainPlayer.CurrentHealth <= 0)
            {
                ApplyDamage(int.MaxValue);
            }
        }
    }

    /// <summary>
    /// Queues damage to be applied, respecting invulnerability.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (damage == float.MaxValue)
        {
            ApplyDamage(damage);
            return;
        }
        if (Invulnerable)
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
        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, StartingHealth);

        if (_player != null)
        {
            _player.LosePowerUp();
        }
        if (CurrentHealth > 0)
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
            if (!Dead)
            {
                Dead = true;
                
                // Handle death differently based on training mode
                if (_isInTrainingMode)
                {
                    HandleTrainingModeDeath();
                    // Don't play death animation in training mode - it has Deactivate() event
                }
                else
                {
                    _animator.SetTrigger("die");
                    HandleNormalModeDeath();
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
        if (CurrentHealth == StartingHealth || Dead)
        {
            return false;
        }
        CurrentHealth = Mathf.Clamp(CurrentHealth + health, 0, StartingHealth);
        return true;
    }

    /// <summary>
    /// Handles invulnerability frames and visual feedback.
    /// </summary>
    private IEnumerator Invulnerability()
    {
        Invulnerable = true;
        Physics2D.IgnoreLayerCollision(IgnoreLayerA, IgnoreLayerB, true);
        for (int i = 0; i < _numberOfFlashes; i++)
        {
            _spriteRenderer.color = HurtColor;
            yield return new WaitForSeconds(_iFramesDuration / (_numberOfFlashes * 2));
            _spriteRenderer.color = _normalColor;
            yield return new WaitForSeconds(_iFramesDuration / (_numberOfFlashes * 2));
        }
        Physics2D.IgnoreLayerCollision(IgnoreLayerA, IgnoreLayerB, false);
        Invulnerable = false;
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
        Dead = false;
        AddHealth(StartingHealth);
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
        CurrentHealth = StartingHealth;
        Dead = false;
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
        _scoreValue = 0;
    }

    public float GetHealth()
    {
        return CurrentHealth;
    }
    
    /// <summary>
    /// Handles death in training mode - doesn't disable components or trigger normal respawn.
    /// The ML-Agents system will handle episode reset and respawn.
    /// </summary>
    private void HandleTrainingModeDeath()
    {
        // In training mode, we don't disable components or trigger normal respawn
        // The ML-Agents system (PlayerAI) will handle episode reset and respawn
        // We just need to mark the player as dead so the AI knows to end the episode
        
        Debug.Log("[Health] Player died in training mode - ML-Agents will handle respawn");
        
        // Don't disable components - let ML-Agents handle the reset
        // Don't call GroundManager.OnPlayerDeath() - that's for normal gameplay
        // Don't unequip weapon - will be handled during episode reset
        
        // IMPORTANT: Don't play the death animation in training mode
        // The death animation has an animation event that calls Deactivate(),
        // which would set gameObject.SetActive(false) and destroy the player
        // Instead, just mark as dead and let ML-Agents handle the reset
        
        // The PlayerAI.HandlePlayerDamaged() method will be called via OnDamaged event
        // and it will end the episode and trigger OnEpisodeBegin for respawn
    }
    
    /// <summary>
    /// Handles death in normal gameplay mode - disables components and triggers respawn.
    /// </summary>
    private void HandleNormalModeDeath()
    {
        // Normal gameplay death handling
        if (_groundManager != null)
        {
            _groundManager.OnPlayerDeath();
        }
        
        foreach (Behaviour component in _components)
        {
            component.enabled = false;
        }
        
        if (CompareTag("Enemy"))
        {
            ScoreManager.Instance.AddScore(ScoreValue);
        }
        
        if (_player != null)
        {
            if (_playerAttack != null)
            {
                _playerAttack.UnequipWeapon();
            }
        }
    }
}
