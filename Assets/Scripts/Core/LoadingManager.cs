using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    // ==================== Constants ====================
    private const int ReloadSceneIndex = 1;
    private const KeyCode ReloadKey = KeyCode.F1;

    // ==================== Unity Lifecycle ====================
    private void Update()
    {
        if (Input.GetKeyDown(ReloadKey))
        {
            SceneManager.LoadScene(ReloadSceneIndex);
        }
    }
}
