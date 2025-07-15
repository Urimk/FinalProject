using System.Collections;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Controls the egg-laying bird enemy, including egg laying and crash behavior.
/// </summary>
public class EggLayingBird : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DisabledLayCooldown = 100f;
    private const float CrashMoveX = 1f;
    private const float CrashMoveY = -2f;
    private const float CrashRotZ = 100f;
    private const float CrashDuration = 0.5f;

    // ==================== Serialized Fields ====================
    [Header("Egg Laying Parameters")]
    [Tooltip("Cooldown time between egg lays.")]
    [FormerlySerializedAs("layCooldown")]

    [SerializeField] private float _layCooldown = 2f;
    [Tooltip("Transform where eggs are dropped from.")]
    [FormerlySerializedAs("eggDropPoint")]

    [SerializeField] private Transform _eggDropPoint;
    [Tooltip("Array of egg prefabs for pooling.")]
    [FormerlySerializedAs("eggPrefabs")]

    [SerializeField] private GameObject[] _eggPrefabs;

    [Header("Sound")]
    [Tooltip("Sound played when an egg is dropped.")]
    [FormerlySerializedAs("eggDropSound")]

    [SerializeField] private AudioClip _eggDropSound;

    // ==================== Private Fields ====================
    private float _cooldownTimer = Mathf.Infinity;
    private Animator _anim;

    /// <summary>
    /// Initializes the animator reference.
    /// </summary>
    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Handles egg laying cooldown and triggers egg laying.
    /// </summary>
    private void Update()
    {
        _cooldownTimer += Time.deltaTime;

        if (_cooldownTimer >= _layCooldown && _layCooldown != DisabledLayCooldown)
        {
            _cooldownTimer = 0f;
            LayEgg();
            //anim.SetTrigger("layEgg");
        }
    }

    /// <summary>
    /// Lays an egg by activating an inactive egg prefab at the drop point.
    /// </summary>
    private void LayEgg()
    {
        SoundManager.instance.PlaySound(_eggDropSound, gameObject);

        GameObject egg = _eggPrefabs[FindInactiveEgg()];
        egg.transform.position = _eggDropPoint.position;
        egg.GetComponent<EnemyProjectile>().SetDirection(Vector2.down);
        egg.transform.rotation = Quaternion.identity;
        egg.GetComponent<EnemyProjectile>().ActivateProjectile();
    }

    /// <summary>
    /// Finds the index of an inactive egg prefab in the pool.
    /// </summary>
    private int FindInactiveEgg()
    {
        for (int i = 0; i < _eggPrefabs.Length; i++)
        {
            if (!_eggPrefabs[i].activeInHierarchy)
            {
                return i;
            }
        }
        return 0; // fallback
    }

    /// <summary>
    /// Initiates the crash coroutine.
    /// </summary>
    public void PerformCrash()
    {
        StartCoroutine(Crash());
    }

    /// <summary>
    /// Handles the bird's crash movement and rotation.
    /// </summary>
    private IEnumerator Crash()
    {
        Vector3 startPos = transform.position;
        bool movingRight = transform.localScale.x > 0f;
        Vector3 endPos = startPos + new Vector3(movingRight ? CrashMoveX : -CrashMoveX, CrashMoveY, 0f);

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, movingRight ? -CrashRotZ : CrashRotZ);

        float elapsedTime = 0f;
        float duration = CrashDuration;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Lerp(startRot, endRot, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final position and rotation are exactly at end values
        transform.position = endPos;
        transform.rotation = endRot;
    }

}
