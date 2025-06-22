using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private bool isAIControlled;
    [SerializeField] private float attackCooldown;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] fireballs;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform swordHitPoint;
    [SerializeField] private float swordRange = 1.0f;
    [SerializeField] private float swordDamage = 2f;
    [SerializeField] private LayerMask enemyLayer;



    private GameObject _equippedWeaponObject;
    private enum WeaponType { Fireball, Sword }
    private WeaponType _currentWeapon = WeaponType.Fireball;
    private bool _hasSword = false;

    private Animator _animator;
    private PlayerMovement _playerMovement;
    private SpriteRenderer _playerSpriteRenderer;
    private float _cooldownTimer = Mathf.Infinity;

    // For Testing
    public bool IsGamePausedForTest { get; set; }
    public float AttackCooldown
    {
        get => attackCooldown;
        set => attackCooldown = value;
    }

    public bool HasSword
    {
        get => _hasSword;
        set => _hasSword = value;
    }

    public Transform FirePoint
    {
        get => firePoint;
        set => firePoint = value;
    }
    public GameObject[] Fireballs
    {
        get => fireballs;
        set => fireballs = value;
    }
    public float CooldownTimer
    {
        get => _cooldownTimer;
        set => _cooldownTimer = value;
    }

    public void TestAttack()
    {
        if (_cooldownTimer >= attackCooldown)
        {
            // Attack logic here
            fireballs[FindFireballs()].transform.position = firePoint.position;
            fireballs[FindFireballs()].GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
            _cooldownTimer = 0; // Reset cooldown after attack
        }
    }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (UIManager.instance.IsGamePaused())
        {
            return;
        }

        if (!isAIControlled)
        {
            if (Input.GetKey(KeyCode.LeftControl) && _cooldownTimer > attackCooldown && _playerMovement.CanAttack())
            {
                Attack();
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftShift) && _hasSword)
        {
            ToggleWeapon();
        }
        UpdateSwordPosition();
        _cooldownTimer += Time.deltaTime;
    }

    // AI can trigger attacks using this method
    public void SetAIAttack(bool attackPressed)
    {
        if (isAIControlled && attackPressed && _cooldownTimer > attackCooldown && _playerMovement.CanAttack())
        {
            Attack();
        }
    }


    private void Attack()
    {

        _cooldownTimer = 0;
        if (_currentWeapon == WeaponType.Fireball)
        {
            SoundManager.instance.PlaySound(fireballSound, gameObject);
            _animator.SetTrigger("attack");
            int idx = FindFireballs();
            if (idx < 0) return;  // pool exhausted—skip shot (or expand pool)
            fireballs[idx].transform.position = firePoint.position;
            fireballs[idx].GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
        }
        else if (_currentWeapon == WeaponType.Sword)
        {
            // Sword swing logic (melee)
            // You could do an animation trigger + detect enemies in a short radius
            SwingSword();
        }
    }
    private void SwingSword()
    {
        StartCoroutine(AnimateSwordSwing());

        Collider2D[] hits = Physics2D.OverlapCircleAll(swordHitPoint.position, swordRange, enemyLayer);

        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();

        foreach (Collider2D hit in hits)
        {
            GameObject obj = hit.gameObject;

            if (damagedObjects.Contains(obj))
                continue;

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(swordDamage);
                damagedObjects.Add(obj); // Mark as damaged
            }
        }

    }

    private IEnumerator AnimateSwordSwing()
    {
        float swingDuration = 0.15f;
        float returnDuration = 0.3f;

        Vector3 startPos = weaponHolder.localPosition;
        Quaternion startRot = weaponHolder.localRotation;

        Vector3 endPos = new Vector3(startPos.x, -1.1f, startPos.z); // swing lower
        Quaternion endRot = Quaternion.Euler(0f, 0f, -130f); // swing angle

        // First: Swing down
        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            float t = elapsed / swingDuration;
            weaponHolder.localPosition = Vector3.Lerp(startPos, endPos, t);
            weaponHolder.localRotation = Quaternion.Lerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Then: Return back
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            float t = elapsed / returnDuration;
            weaponHolder.localPosition = Vector3.Lerp(endPos, startPos, t);
            weaponHolder.localRotation = Quaternion.Lerp(endRot, startRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Just to be exact
        weaponHolder.localPosition = startPos;
        weaponHolder.localRotation = startRot;
    }



    private void OnDrawGizmosSelected()
    {
        if (swordHitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(swordHitPoint.position, swordRange);
        }
    }



    private int FindFireballs()
    {
        for (int i = 0; i < fireballs.Length; i++)
        {
            if (!fireballs[i].activeInHierarchy)
            {
                return i;
            }
        }
        return -1;
    }

    public void EquipWeapon()
    {
        _currentWeapon = WeaponType.Sword;

        // Destroy any existing weapon object
        if (_equippedWeaponObject != null)
        {
            Destroy(_equippedWeaponObject);
        }

        // Instantiate the sword in the weapon holder
        _equippedWeaponObject = Instantiate(swordPrefab, weaponHolder);
        _equippedWeaponObject.transform.localPosition = Vector3.zero; // adjust if needed
    }

    public void UnequipWeapon()
    {
        _currentWeapon = WeaponType.Fireball;

        // Destroy the equipped weapon object
        if (_equippedWeaponObject != null)
        {
            Destroy(_equippedWeaponObject);
            _equippedWeaponObject = null;
        }
    }

    private void ToggleWeapon()
    {
        if (_currentWeapon == WeaponType.Fireball)
        {
            EquipWeapon();
        }
        else
        {
            UnequipWeapon();
        }
    }

    private void UpdateSwordPosition()
    {
        if (hasSword && _playerSpriteRenderer.sprite != null)
        {
            string spriteName = _playerSpriteRenderer.sprite.name;
            Vector3 newPosition;

            if (spriteName == "walk_04")
            {
                newPosition = new Vector3(0.3f, -0.6f, 0f);
            }
            else if (spriteName == "walk_03" || spriteName == "walk_05")
            {
                newPosition = new Vector3(0.45f, -0.6f, 0f);
            }
            else
            {
                newPosition = new Vector3(0.6f, -0.6f, 0f);
            }

            weaponHolder.localPosition = newPosition;
        }
    }

    public float IsAttackReady()
    {
        if (cooldownTimer >= attackCooldown)
            return 1.0f;
        else
            return 0.0f;
    }

    public void ResetCooldown()
    {
        cooldownTimer = 0;
    }



}
