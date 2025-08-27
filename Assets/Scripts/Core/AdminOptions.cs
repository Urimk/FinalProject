using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles admin options for debugging and testing during development.
/// Includes scene reload, player health toggle, and time scale toggle.
/// </summary>
public class AdminOptions : MonoBehaviour
{
    [Header("Reload Settings")]
    [Tooltip("The build index of the scene to reload when the reload key is pressed.")]
    [SerializeField] private int _reloadSceneIndex = 0;

    [Tooltip("The key used to trigger a scene reload.")]
    [SerializeField] private KeyCode _reloadKey = KeyCode.F1;

    [Header("Player Health Settings")]
    [Tooltip("The key used to toggle player health between max and default.")]
    [SerializeField] private KeyCode _healthToggleKey = KeyCode.F2;

    [Tooltip("The maximum health value to set when toggling.")]
    [SerializeField] private int _maxHealthValue = 999;

    [Header("Time Scale Settings")]
    [Tooltip("The key used to toggle time scale between fast and normal.")]
    [SerializeField] private KeyCode _timeScaleToggleKey = KeyCode.F3;

    [Tooltip("The fast time scale value.")]
    [SerializeField] private float _fastTimeScale = 2.5f;

    // ==================== Private Fields ====================
    private bool _isHealthMaxed = false;
    private bool _isTimeFast = false;
    private Health _playerHealth;
    private float _originalMaxHealth;

    // ==================== Unity Lifecycle ====================
    private void Start()
    {
        // Find the player health component on object with Player tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            _playerHealth = playerObject.GetComponent<Health>();
            if (_playerHealth != null)
            {
                _originalMaxHealth = _playerHealth.StartingHealth;
            }
            else
            {
                Debug.LogWarning("Health component not found on Player object!");
            }
        }
        else
        {
            Debug.LogWarning("Player object with 'Player' tag not found!");
        }
    }

    /// <summary>
    /// Checks for admin key presses each frame and executes corresponding actions.
    /// </summary>
    private void Update()
    {
        // Listen for the reload key and reload the scene if pressed
        if (Input.GetKeyDown(_reloadKey))
        {
            SceneManager.LoadScene(_reloadSceneIndex);
        }

        // Listen for health toggle key
        if (Input.GetKeyDown(_healthToggleKey))
        {
            TogglePlayerHealth();
        }

        // Listen for time scale toggle key
        if (Input.GetKeyDown(_timeScaleToggleKey))
        {
            ToggleTimeScale();
        }
    }

    // ==================== Private Methods ====================
    /// <summary>
    /// Toggles player health between max value and original default.
    /// </summary>
    private void TogglePlayerHealth()
    {
        if (_playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth component not found!");
            return;
        }

        if (_isHealthMaxed)
        {
            // Return to original health
            _playerHealth.StartingHealth = _originalMaxHealth;
            _playerHealth.CurrentHealth = _originalMaxHealth;
            _isHealthMaxed = false;
            Debug.Log($"Health restored to default: {_originalMaxHealth}");
        }
        else
        {
            // Set to max health
            _playerHealth.StartingHealth = _maxHealthValue;
            _playerHealth.CurrentHealth = _maxHealthValue;
            _isHealthMaxed = true;
            Debug.Log($"Health set to max: {_maxHealthValue}");
        }
    }

    /// <summary>
    /// Toggles time scale between fast and normal speed.
    /// </summary>
    private void ToggleTimeScale()
    {
        if (_isTimeFast)
        {
            // Return to normal time scale
            Time.timeScale = 1.0f;
            _isTimeFast = false;
            Debug.Log("Time scale restored to normal: 1.0");
        }
        else
        {
            // Set to fast time scale
            Time.timeScale = _fastTimeScale;
            _isTimeFast = true;
            Debug.Log($"Time scale set to fast: {_fastTimeScale}");
        }
    }
}
