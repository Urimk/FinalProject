using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles boss health, damage, death, and health bar UI logic.
/// </summary>
public class BossHealth : MonoBehaviour, IDamageable
{
    // ==================== Constants ====================
    private static readonly Vector3 HealthBarOffset = new Vector3(0, 2.6f, 0);
    private const float DeathAnimationWait = 2f;
    private const float HealthNormalizationDivisor = 1f;

    // ==================== Serialized Fields ====================
    [SerializeField] public float maxHealth = 10f;
    public event Action<float> OnBossDamaged;
    public float currentHealth { get; private set; }
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private GameObject _spumPrefabObject;
    [SerializeField] private Transform _boss;
    [SerializeField] private BossRewardManager _rm;
    [SerializeField] private bool _isTraining;
    [SerializeField] private GameObject _trophy;
    [SerializeField] private List<MonoBehaviour> _bossScriptObjects;

    // ==================== Private Fields ====================
    private SPUM_Prefabs _spumPrefabs;
    private bool _isDying = false;
    private List<IBoss> _bossScripts = new List<IBoss>();

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
        currentHealth = maxHealth;
        UpdateHealthBar();
        foreach (var obj in _bossScriptObjects)
        {
            if (obj is IBoss boss)
                _bossScripts.Add(boss);
            else
                Debug.LogError($"{obj.name} does not implement IBoss!");
        }
    }

    /// <summary>
    /// Applies damage to the boss and updates health bar.
    /// </summary>
    public void TakeDamage(float damage)
    {
        _rm.ReportTookDamage(damage);
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();

        bool allIdle = _bossScripts.TrueForAll(b => !b.IsCurrentlyChargingOrDashing());
        if (_spumPrefabs != null && !_isDying && allIdle)
        {
            _spumPrefabs.PlayAnimation(PlayerState.DAMAGED, 0);
            OnBossDamaged?.Invoke(damage);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Returns the boss's current health as a percentage (0.0 to 1.0).
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    private void UpdateHealthBar()
    {
        if (_healthSlider != null)
        {
            _healthSlider.value = currentHealth / maxHealth / HealthNormalizationDivisor;
        }
    }
    public event System.Action OnBossDied;
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

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(DeathAnimationWait);
        Destroy(gameObject);
    }

    /// <summary>
    /// Resets the boss's health and health bar.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        _healthSlider.gameObject.SetActive(true);
        _isDying = false;
    }
}
