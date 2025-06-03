using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour, IDamageable
{
    [SerializeField] public float maxHealth = 10f;
    public event Action<float> OnBossDamaged; // Event for AI notification

    public float currentHealth { get; private set; }

    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject spumPrefabObject;
    [SerializeField] private Transform boss;  // Reference to the boss Transform
    [SerializeField] private BossRewardManager rm;

    private SPUM_Prefabs spumPrefabs;
    private bool isDying = false;

    // Reference to BossEnemy script to access flame and warning marker
    [SerializeField] private AIBoss bossEnemyScript; 

    private void Awake()
    {
        if (spumPrefabObject != null)
        {
            spumPrefabs = spumPrefabObject.GetComponent<SPUM_Prefabs>();
        }
        else
        {
            Debug.LogError("No SPUM Prefab GameObject assigned in the Inspector!");
        }
    }

    void LateUpdate()
    {
        if (healthSlider != null && boss != null)
        {
            // Follow the boss’s position
            healthSlider.transform.position = boss.transform.position + new Vector3(0, 2.6f, 0);

            // Keep rotation fixed
            healthSlider.transform.rotation = Quaternion.identity;
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(float damage)
    {
        rm.ReportTookDamage(damage);
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();

        if (spumPrefabs != null && !isDying && !bossEnemyScript.IsCurrentlyChargingOrDashing())
        {
            spumPrefabs.PlayAnimation(PlayerState.DAMAGED, 0);
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
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
    }
    public event System.Action OnBossDied;
    private void Die()
    {
        Debug.Log("Boss Defeated!");

        if (!isDying)
        {
            isDying = true;

            if (spumPrefabs != null)
            {
                spumPrefabs.PlayAnimation(PlayerState.DEATH, 0);
            }

            // Deactivate the health bar (slider)
            if (healthSlider != null)
            {
                healthSlider.gameObject.SetActive(false);
            }
            if (bossEnemyScript != null)
            {
                bossEnemyScript.Die();
            }
            OnBossDied?.Invoke();

            //StartCoroutine(DestroyAfterAnimation());
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
        isDying = false;
    }
}