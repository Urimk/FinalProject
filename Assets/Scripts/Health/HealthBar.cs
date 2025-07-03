using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player's health bar UI.
/// </summary>
public class HealthBar : MonoBehaviour
{
    // ==================== Constants ====================
    private const float HealthNormalizationDivisor = 10f;

    // ==================== Inspector Fields ====================
    [Tooltip("Reference to the player's Health component.")]
    [SerializeField] private Health _playerHealth;
    [Tooltip("Image representing the total health bar.")]
    [SerializeField] private Image _totalHealthBar;
    [Tooltip("Image representing the current health bar.")]
    [SerializeField] private Image _currentHealthBar;

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Initializes the total health bar fill amount.
    /// </summary>
    private void Start()
    {
        _totalHealthBar.fillAmount = _playerHealth.CurrentHealth / HealthNormalizationDivisor;
    }

    /// <summary>
    /// Updates the current health bar fill amount.
    /// </summary>
    private void Update()
    {
        _currentHealthBar.fillAmount = _playerHealth.CurrentHealth / HealthNormalizationDivisor;
    }
}
