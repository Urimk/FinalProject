using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour, IDamageable
{
    [SerializeField] public float maxHealth = 10f;
    public event Action<float> OnBossDamaged; // Event for AI notification

    public float currentHealth { get; private set; }

    [SerializeField] private Slider _healthSlider;
    [SerializeField] private GameObject _spumPrefabObject;
    [SerializeField] private Transform _boss;  // Reference to the boss Transform
    [SerializeField] private BossRewardManager _rm;
    [SerializeField] private bool _isTraining;
    [SerializeField] private GameObject _trophy;

    private SPUM_Prefabs _spumPrefabs;
    private bool _isDying = false;

    // Reference to BossEnemy script to access flame and warning marker
    [SerializeField] private List<MonoBehaviour> _bossScriptObjects; // Drag any boss component here (AIBoss or BossEnemy)
    private List<IBoss> _bossScripts = new List<IBoss>();

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

    void LateUpdate()
    {
        if (_healthSlider != null && _boss != null)
        {
            // Follow the boss's position
            _healthSlider.transform.position = _boss.transform.position + new Vector3(0, 2.6f, 0);

            // Keep rotation fixed
            _healthSlider.transform.rotation = Quaternion.identity;
        }
    }

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

    // New method to get current health percentage (0.0 to 1.0)
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    private void UpdateHealthBar()
    {
        if (_healthSlider != null)
        {
            _healthSlider.value = currentHealth / maxHealth;
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

            // Deactivate the health bar (slider)
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
        yield return new WaitForSeconds(2f);  // Adjust the time to match your animation length
        Destroy(gameObject);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        _healthSlider.gameObject.SetActive(true);
        _isDying = false;
    }
}
