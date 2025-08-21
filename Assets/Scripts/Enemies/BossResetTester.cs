using UnityEngine;

/// <summary>
/// Simple test script to verify boss reset functionality.
/// Attach this to any GameObject in the scene to test boss reset.
/// </summary>
public class BossResetTester : MonoBehaviour
{
    [Header("Test Controls")]
    [Tooltip("Press this key to test boss reset")]
    [SerializeField] private KeyCode testResetKey = KeyCode.R;
    
    [Tooltip("Press this key to check boss state")]
    [SerializeField] private KeyCode checkStateKey = KeyCode.C;
    
    [Header("Boss Reference")]
    [Tooltip("Reference to the BossEnemy to test")]
    [SerializeField] private BossEnemy bossToTest;
    
    private void Update()
    {
        // Test reset
        if (Input.GetKeyDown(testResetKey))
        {
            TestBossReset();
        }
        
        // Check state
        if (Input.GetKeyDown(checkStateKey))
        {
            CheckBossState();
        }
    }
    
    /// <summary>
    /// Tests the boss reset functionality.
    /// </summary>
    private void TestBossReset()
    {
        if (bossToTest == null)
        {
            Debug.LogError("[BossResetTester] No boss assigned for testing!");
            return;
        }
        
        Debug.Log("[BossResetTester] Testing boss reset...");
        
        // Check state before reset
        Debug.Log($"[BossResetTester] Before reset - Phase2: {bossToTest.IsPhase2}");
        
        // Perform reset
        bossToTest.ResetState();
        
        // Check state after reset
        Debug.Log($"[BossResetTester] After reset - Phase2: {bossToTest.IsPhase2}");
        
        Debug.Log("[BossResetTester] Reset test complete!");
    }
    
    /// <summary>
    /// Checks the current boss state.
    /// </summary>
    private void CheckBossState()
    {
        if (bossToTest == null)
        {
            Debug.LogError("[BossResetTester] No boss assigned for testing!");
            return;
        }
        
        Debug.Log($"[BossResetTester] Current boss state:");
        Debug.Log($"  - Phase2: {bossToTest.IsPhase2}");
        Debug.Log($"  - IsFireballReady: {bossToTest.IsFireballReady()}");
        Debug.Log($"  - IsFlameTrapReady: {bossToTest.IsFlameTrapReady()}");
        Debug.Log($"  - IsDashReady: {bossToTest.IsDashReady()}");
        Debug.Log($"  - IsCurrentlyChargingOrDashing: {bossToTest.IsCurrentlyChargingOrDashing()}");
    }
    
    /// <summary>
    /// Auto-find boss if not assigned.
    /// </summary>
    private void Start()
    {
        if (bossToTest == null)
        {
            bossToTest = FindObjectOfType<BossEnemy>();
            if (bossToTest != null)
            {
                Debug.Log($"[BossResetTester] Auto-found boss: {bossToTest.name}");
            }
            else
            {
                Debug.LogWarning("[BossResetTester] No BossEnemy found in scene!");
            }
        }
        
        Debug.Log($"[BossResetTester] Test controls:");
        Debug.Log($"  - Press {testResetKey} to test boss reset");
        Debug.Log($"  - Press {checkStateKey} to check boss state");
    }
}
