using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player's health bar UI.
/// </summary>
public class HealthBar : MonoBehaviour
{
    private const float HealthNormalizationDivisor = 10f;

    [SerializeField] private Health _playerHealth;
    [SerializeField] private Image _totalHealthBar;
    [SerializeField] private Image _currentHealthBar;

    /// <summary>
    /// Initializes the total health bar fill amount.
    /// </summary>
    private void Start()
    {
        _totalHealthBar.fillAmount = _playerHealth.currentHealth / HealthNormalizationDivisor;
    }

    /// <summary>
    /// Updates the current health bar fill amount.
    /// </summary>
    private void Update()
    {
        _currentHealthBar.fillAmount = _playerHealth.currentHealth / HealthNormalizationDivisor;
    }
}
