using UnityEngine;

/// <summary>
/// Centralized debug manager that controls debug logging across the entire game.
/// Provides a single point of control for enabling/disabling debug output.
/// </summary>
public class DebugManager : MonoBehaviour
{
    // ==================== Singleton ====================
    /// <summary>
    /// The global instance of the DebugManager.
    /// </summary>
    public static DebugManager Instance { get; private set; }

    // ==================== Inspector Fields ====================
    [Header("Debug Settings")]
    [Tooltip("Global debug mode. When enabled, debug logs will be displayed.")]
    [SerializeField] private bool _debugMode = false;

    [Tooltip("Whether to show debug logs in the console.")]
    [SerializeField] private bool _showConsoleLogs = true;

    [Header("Category Settings")]
    [Tooltip("Enable debug logs for collectable system.")]
    [SerializeField] private bool _collectableDebug = false;

    [Tooltip("Enable debug logs for player system.")]
    [SerializeField] private bool _playerDebug = false;

    [Tooltip("Enable debug logs for enemy system.")]
    [SerializeField] private bool _enemyDebug = false;

    [Tooltip("Enable debug logs for room system.")]
    [SerializeField] private bool _roomDebug = false;

    [Tooltip("Enable debug logs for sound system.")]
    [SerializeField] private bool _soundDebug = false;

    // ==================== Properties ====================
    /// <summary>
    /// Gets whether global debug mode is enabled.
    /// </summary>
    public static bool IsDebugMode => Instance != null && Instance._debugMode;

    /// <summary>
    /// Gets whether console logging is enabled.
    /// </summary>
    public static bool ShowConsoleLogs => Instance != null && Instance._showConsoleLogs;


    // ==================== Category Properties ====================
    public static bool CollectableDebug => Instance != null && Instance._collectableDebug;
    public static bool PlayerDebug => Instance != null && Instance._playerDebug;
    public static bool EnemyDebug => Instance != null && Instance._enemyDebug;
    public static bool RoomDebug => Instance != null && Instance._roomDebug;
    public static bool SoundDebug => Instance != null && Instance._soundDebug;

    // ==================== Unity Lifecycle ====================
    private void Awake()
    {
        // Ensure there's only one instance of DebugManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==================== Public Methods ====================
    /// <summary>
    /// Logs a debug message if debug mode is enabled for the specified category.
    /// </summary>
    /// <param name="category">The debug category to check.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="context">Optional context object for the log.</param>
    public static void Log(DebugCategory category, string message, Object context = null)
    {
        if (!IsDebugMode) return;

        bool categoryEnabled = IsCategoryEnabled(category);
        if (!categoryEnabled) return;

        if (ShowConsoleLogs)
        {
            Debug.Log($"[{category}] {message}", context);
        }
    }

    /// <summary>
    /// Logs a warning message if debug mode is enabled for the specified category.
    /// </summary>
    /// <param name="category">The debug category to check.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="context">Optional context object for the log.</param>
    public static void LogWarning(DebugCategory category, string message, Object context = null)
    {
        if (!IsDebugMode) return;

        bool categoryEnabled = IsCategoryEnabled(category);
        if (!categoryEnabled) return;

        if (ShowConsoleLogs)
        {
            Debug.LogWarning($"[{category}] {message}", context);
        }
    }

    /// <summary>
    /// Logs an error message if debug mode is enabled for the specified category.
    /// </summary>
    /// <param name="category">The debug category to check.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="context">Optional context object for the log.</param>
    public static void LogError(DebugCategory category, string message, Object context = null)
    {
        if (!IsDebugMode) return;

        bool categoryEnabled = IsCategoryEnabled(category);
        if (!categoryEnabled) return;

        if (ShowConsoleLogs)
        {
            Debug.LogError($"[{category}] {message}", context);
        }

    }

    // ==================== Private Methods ====================
    /// <summary>
    /// Checks if a specific debug category is enabled.
    /// </summary>
    /// <param name="category">The category to check.</param>
    /// <returns>True if the category is enabled, false otherwise.</returns>
    private static bool IsCategoryEnabled(DebugCategory category)
    {
        if (Instance == null) return false;

        return category switch
        {
            DebugCategory.Collectable => CollectableDebug,
            DebugCategory.Player => PlayerDebug,
            DebugCategory.Enemy => EnemyDebug,
            DebugCategory.Room => RoomDebug,
            DebugCategory.Sound => SoundDebug,
            _ => false
        };
    }
}

/// <summary>
/// Enum defining different debug categories for organized logging.
/// </summary>
public enum DebugCategory
{
    Collectable,
    Player,
    Enemy,
    Room,
    Sound
}
