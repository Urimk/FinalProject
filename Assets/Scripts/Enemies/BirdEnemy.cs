using System.Collections;

using UnityEngine;

public class EggLayingBird : MonoBehaviour
{
    [Header("Egg Laying Parameters")]
    [SerializeField] private float _layCooldown = 2f;
    [SerializeField] private Transform _eggDropPoint;
    [SerializeField] private GameObject[] _eggPrefabs;

    [Header("Sound")]
    [SerializeField] private AudioClip _eggDropSound;

    private float _cooldownTimer = Mathf.Infinity;
    private Animator _anim;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void Update()
    {
        _cooldownTimer += Time.deltaTime;

        if (_cooldownTimer >= _layCooldown && _layCooldown != 100)
        {
            _cooldownTimer = 0f;
            LayEgg();
            //anim.SetTrigger("layEgg");
        }
    }

    // Called by animation event
    private void LayEgg()
    {
        SoundManager.instance.PlaySound(_eggDropSound, gameObject);

        GameObject egg = _eggPrefabs[FindInactiveEgg()];
        egg.transform.position = _eggDropPoint.position;
        egg.GetComponent<EnemyProjectile>().SetDirection(Vector2.down);
        egg.transform.rotation = Quaternion.identity;  // keep the sprite upright
        egg.GetComponent<EnemyProjectile>().ActivateProjectile();


    }

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

    public void PerformCrash()
    {
        StartCoroutine(Crash());
    }
    private IEnumerator Crash()
    {
        Vector3 startPos = transform.position;
        bool movingRight = transform.localScale.x > 0f;
        Vector3 endPos = startPos + new Vector3(movingRight ? 1f : -1f, -2f, 0f);

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, movingRight ? -100f : 100f);

        float elapsedTime = 0f;
        float duration = 0.5f;

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
