using System.Collections;
using UnityEngine;

public class EggLayingBird : MonoBehaviour
{
    [Header("Egg Laying Parameters")]
    [SerializeField] private float layCooldown = 2f;
    [SerializeField] private Transform eggDropPoint;
    [SerializeField] private GameObject[] eggPrefabs;

    [Header("Sound")]
    [SerializeField] private AudioClip eggDropSound;

    private float cooldownTimer = Mathf.Infinity;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;

        if (cooldownTimer >= layCooldown && layCooldown != 100)
        {
            cooldownTimer = 0f;
            LayEgg();
            //anim.SetTrigger("layEgg");
        }
    }

    // Called by animation event
    private void LayEgg()
    {
        SoundManager.instance.PlaySound(eggDropSound, gameObject);

        GameObject egg = eggPrefabs[FindInactiveEgg()];
        egg.transform.position = eggDropPoint.position;
        egg.GetComponent<EnemyProjectile>().SetDirection(Vector2.down);
        egg.transform.rotation = Quaternion.identity;  // keep the sprite upright
        egg.GetComponent<EnemyProjectile>().ActivateProjectile();
        

    }

    private int FindInactiveEgg()
    {
        for (int i = 0; i < eggPrefabs.Length; i++)
        {
            if (!eggPrefabs[i].activeInHierarchy)
            {
                return i;
            }
        }
        return 0; // fallback
    }

    public void PerformCrash() {
        StartCoroutine(Crash());
    }
    private IEnumerator Crash() {
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
