using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles reloading a specific scene when a designated key is pressed.
/// Useful for quick testing and debugging during development.
/// </summary>
public class LoadingManager : MonoBehaviour
{
    [Header("Reload Settings")]
    [Tooltip("The build index of the scene to reload when the reload key is pressed.")]
    [SerializeField] private int _reloadSceneIndex = 1;

    [Tooltip("The key used to trigger a scene reload.")]
    [SerializeField] private KeyCode _reloadKey = KeyCode.F1;

    /// <summary>
    /// Checks for the reload key press each frame and reloads the specified scene if pressed.
    /// </summary>
    private void Update()
    {
        // Listen for the reload key and reload the scene if pressed
        if (Input.GetKeyDown(_reloadKey))
        {
            SceneManager.LoadScene(_reloadSceneIndex);
        }
    }
}
