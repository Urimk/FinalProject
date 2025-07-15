using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Handles boss health, damage, death, and health bar UI logic.
/// </summary>
public class BossHealth : MonoBehaviour, IDamageable
{
    // ==================== Constants ====================
    /// <summary>Offset for the health bar above the boss.</summary>
    private static readonly Vector3 HealthBarOffset = new Vector3(0, 2.6f, 0);
    /// <summary>Time to wait after death animation before destroying the boss.</summary>
    private const float DeathAnimationWait = 2f;
    /// <summary>Divisor for normalizing health bar value.</summary>
    private const float HealthNormalizationDivisor = 1f;

    // ==================== Inspector Fields ====================
    [Header("Boss Health Settings")]
    [Tooltip("Maximum health of the boss.")]
    [FormerlySerializedAs("maxHealth")]
    [SerializeField] private float _maxHealth = 10f;

    [Tooltip("Current health of the boss.")]
    [FormerlySerializedAs("currentHealth")]
    [SerializeField] private float _currentHealth = 10f;

    [Header("UI References")]
    [Tooltip("Slider component for the boss health bar.")]
    [SerializeField] private Slider _healthSlider;

    [Header("Boss References")]
    [Tooltip("SPUM prefab GameObject for animation control.")]
    [SerializeField] private GameObject _spumPrefabObject;
    [Tooltip("Transform of the boss for positioning the health bar.")]
    [SerializeField] private Transform _boss;
    [Tooltip("Boss reward manager for reporting damage.")]
    [SerializeField] private BossRewardManager _rm;
    [Tooltip("True if boss is in training mode.")]
    [SerializeField] private bool _isTraining;
    [Tooltip("Trophy GameObject to activate on boss defeat.")]
    [SerializeField] private GameObject _trophy;
    [Tooltip("List of MonoBehaviours implementing IBoss for boss logic.")]
    [SerializeField] private List<MonoBehaviour> _bossScriptObjects;

    // ==================== Events ====================
    /// <summary>Invoked when the boss takes damage. Passes the damage amount.</summary>
    public event Action<float> OnBossDamaged;
    /// <summary>Invoked when the boss dies.</summary>
    public event Action OnBossDied;

    // ==================== Properties ====================
    /// <summary>Current health of the boss.</summary>
    public float CurrentHealth { get => _currentHealth; private set => _currentHealth = value; }
    /// <summary>Maximum health of the boss.</summary>
    public float MaxHealth => _maxHealth;

    // ==================== Private Fields ====================
    private SPUM_Prefabs _spumPrefabs;
    private bool _isDying = false;
    private List<IBoss> _bossScripts = new List<IBoss>();

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Initializes SPUM prefab reference.
    /// </summary>
    private void Awake()
    {
        if (_spumPrefabObject != null)
        {
            _spumPrefabs = _spumPrefabObject.GetComponent<SPUM_Prefabs>();
        }
        else
        {
            Debug.LogError("No SPUM Prefab GameObject assigned in the Inspector!");
        }
    }

    /// <summary>
    /// Updates the health bar position and rotation to follow the boss.
    /// </summary>
    private void LateUpdate()
    {
        if (_healthSlider != null && _boss != null)
        {
            _healthSlider.transform.position = _boss.transform.position + HealthBarOffset;
            _healthSlider.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Initializes health and boss script references.
    /// </summary>
    private void Start()
    {
        CurrentHealth = _maxHealth;
        UpdateHealthBar();
        foreach (var obj in _bossScriptObjects)
        {
            if (obj is IBoss boss)
                _bossScripts.Add(boss);
            else
                Debug.LogError($"{obj.name} does not implement IBoss!");
        }
    }

    // ==================== Boss Health Logic ====================
    /// <summary>
    /// Applies damage to the boss and updates health bar.
    /// </summary>
    /// <param name="damage">Amount of damage to apply.</param>
    public void TakeDamage(float damage)
    {
        _rm.ReportTookDamage(damage);
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, _maxHealth);
        UpdateHealthBar();

        // Only play damaged animation if all boss scripts are idle and not dying
        bool allIdle = _bossScripts.TrueForAll(b => !b.IsCurrentlyChargingOrDashing());
        if (_spumPrefabs != null && !_isDying && allIdle)
        {
            _spumPrefabs.PlayAnimation(PlayerState.DAMAGED, 0);
            OnBossDamaged?.Invoke(damage);
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Returns the boss's current health as a percentage (0.0 to 1.0).
    /// </summary>
    public float GetHealthPercentage()
    {
        return CurrentHealth / MaxHealth;
    }

    /// <summary>
    /// Updates the health bar UI to reflect current health.
    /// </summary>
    private void UpdateHealthBar()
    {
        if (_healthSlider != null)
        {
            _healthSlider.value = CurrentHealth / MaxHealth / HealthNormalizationDivisor;
        }
    }

    /// <summary>
    /// Handles boss death logic, animation, and cleanup.
    /// </summary>
    private void Die()
    {
        Debug.Log("Boss Defeated!");

        if (!_isDying)
        {
            _isDying = true;

            if (_spumPrefabs != null)
            {
                _spumPrefabs.PlayAnimation(PlayerState.DEATH, 0);
            }

            if (_healthSlider != null)
            {
                _healthSlider.gameObject.SetActive(false);
            }
            OnBossDied?.Invoke();
            if (!_isTraining)
            {
                foreach (var boss in _bossScripts)
                {
                    boss.Die();
                }
                StartCoroutine(DestroyAfterAnimation());
            }
        }
        if (_trophy != null)
        {
            _trophy.SetActive(true);
        }
    }

    /// <summary>
    /// Waits for the death animation, then destroys the boss GameObject.
    /// </summary>
    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(DeathAnimationWait);
        Destroy(gameObject);
    }

    /// <summary>
    /// Resets the boss's health and health bar for reuse.
    /// </summary>
    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        if (_healthSlider != null)
            _healthSlider.gameObject.SetActive(true);
        _isDying = false;
    }
}
