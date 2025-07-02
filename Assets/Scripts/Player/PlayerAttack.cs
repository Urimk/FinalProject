using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Handles player attack logic, including fireball and sword attacks, cooldowns, and weapon switching.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    // === Constants ===
    private const float DefaultSwordRange = 1.0f;
    private const float DefaultSwordDamage = 2f;
    private const float SwordSwingDuration = 0.15f;
    private const float SwordReturnDuration = 0.3f;
    private const float SwordSwingEndY = -1.1f;
    private const float SwordSwingEndZ = -130f;
    private const float SwordPosWalk04X = 0.3f;
    private const float SwordPosWalk03_05X = 0.45f;
    private const float SwordPosDefaultX = 0.6f;
    private const float SwordPosAllY = -0.6f;
    private const string SpriteWalk04 = "walk_04";
    private const string SpriteWalk03 = "walk_03";
    private const string SpriteWalk05 = "walk_05";
    private const string AnimatorAttack = "attack";

    // === Serialized Fields ===
    [SerializeField] private bool isAIControlled;
    [SerializeField] private float attackCooldown;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] fireballs;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform swordHitPoint;
    [SerializeField] private float swordRange = DefaultSwordRange;
    [SerializeField] private float swordDamage = DefaultSwordDamage;
    [SerializeField] private LayerMask enemyLayer;

    // === Private Fields ===
    private GameObject _equippedWeaponObject;
    private enum WeaponType { Fireball, Sword }
    private WeaponType _currentWeapon = WeaponType.Fireball;
    private bool _hasSword = false;
    private Animator _animator;
    private PlayerMovement _playerMovement;
    private SpriteRenderer _playerSpriteRenderer;
    private float _cooldownTimer = Mathf.Infinity;

    // === Properties ===
    public bool IsGamePausedForTest { get; set; }
    public float AttackCooldown { get => attackCooldown; set => attackCooldown = value; }
    public bool HasSword { get => _hasSword; set => _hasSword = value; }
    public Transform FirePoint { get => firePoint; set => firePoint = value; }
    public GameObject[] Fireballs { get => fireballs; set => fireballs = value; }
    public float CooldownTimer { get => _cooldownTimer; set => _cooldownTimer = value; }

    /// <summary>
    /// For testing: triggers an attack if cooldown is ready.
    /// </summary>
    public void TestAttack()
    {
        if (_cooldownTimer >= attackCooldown)
        {
            fireballs[FindFireballs()].transform.position = firePoint.position;
            fireballs[FindFireballs()].GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
            _cooldownTimer = 0;
        }
    }

    /// <summary>
    /// Unity Awake callback. Initializes components.
    /// </summary>
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Unity Update callback. Handles input, cooldown, and weapon switching.
    /// </summary>
    private void Update()
    {
        if (UIManager.Instance.IsGamePaused())
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

    /// <summary>
    /// Allows AI to trigger attacks.
    /// </summary>
    public void SetAIAttack(bool attackPressed)
    {
        if (isAIControlled && attackPressed && _cooldownTimer > attackCooldown && _playerMovement.CanAttack())
        {
            Attack();
        }
    }

    /// <summary>
    /// Handles attack logic for both fireball and sword.
    /// </summary>
    private void Attack()
    {
        _cooldownTimer = 0;
        if (_currentWeapon == WeaponType.Fireball)
        {
            SoundManager.instance.PlaySound(fireballSound, gameObject);
            _animator.SetTrigger(AnimatorAttack);
            int idx = FindFireballs();
            if (idx < 0) return;
            fireballs[idx].transform.position = firePoint.position;
            fireballs[idx].GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
        }
        else if (_currentWeapon == WeaponType.Sword)
        {
            SwingSword();
        }
    }

    /// <summary>
    /// Handles sword swing logic and applies damage to enemies.
    /// </summary>
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
                damagedObjects.Add(obj);
            }
        }
    }

    /// <summary>
    /// Animates the sword swing.
    /// </summary>
    private IEnumerator AnimateSwordSwing()
    {
        Vector3 startPos = weaponHolder.localPosition;
        Quaternion startRot = weaponHolder.localRotation;
        Vector3 endPos = new Vector3(startPos.x, SwordSwingEndY, startPos.z);
        Quaternion endRot = Quaternion.Euler(0f, 0f, SwordSwingEndZ);
        float elapsed = 0f;
        while (elapsed < SwordSwingDuration)
        {
            float t = elapsed / SwordSwingDuration;
            weaponHolder.localPosition = Vector3.Lerp(startPos, endPos, t);
            weaponHolder.localRotation = Quaternion.Lerp(startRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < SwordReturnDuration)
        {
            float t = elapsed / SwordReturnDuration;
            weaponHolder.localPosition = Vector3.Lerp(endPos, startPos, t);
            weaponHolder.localRotation = Quaternion.Lerp(endRot, startRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        weaponHolder.localPosition = startPos;
        weaponHolder.localRotation = startRot;
    }

    /// <summary>
    /// Draws the sword hit area in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (swordHitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(swordHitPoint.position, swordRange);
        }
    }

    /// <summary>
    /// Finds an available fireball in the pool.
    /// </summary>
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

    /// <summary>
    /// Equips the sword weapon.
    /// </summary>
    public void EquipWeapon()
    {
        _currentWeapon = WeaponType.Sword;
        if (_equippedWeaponObject != null)
        {
            Destroy(_equippedWeaponObject);
        }
        _equippedWeaponObject = Instantiate(swordPrefab, weaponHolder);
        _equippedWeaponObject.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Unequips the sword weapon.
    /// </summary>
    public void UnequipWeapon()
    {
        _currentWeapon = WeaponType.Fireball;
        if (_equippedWeaponObject != null)
        {
            Destroy(_equippedWeaponObject);
            _equippedWeaponObject = null;
        }
    }

    /// <summary>
    /// Toggles between fireball and sword weapons.
    /// </summary>
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

    /// <summary>
    /// Updates the sword's position based on the current sprite.
    /// </summary>
    private void UpdateSwordPosition()
    {
        if (HasSword && _playerSpriteRenderer.sprite != null)
        {
            string spriteName = _playerSpriteRenderer.sprite.name;
            Vector3 newPosition;
            if (spriteName == SpriteWalk04)
            {
                newPosition = new Vector3(SwordPosWalk04X, SwordPosAllY, 0f);
            }
            else if (spriteName == SpriteWalk03 || spriteName == SpriteWalk05)
            {
                newPosition = new Vector3(SwordPosWalk03_05X, SwordPosAllY, 0f);
            }
            else
            {
                newPosition = new Vector3(SwordPosDefaultX, SwordPosAllY, 0f);
            }
            weaponHolder.localPosition = newPosition;
        }
    }

    /// <summary>
    /// Returns 1.0 if attack is ready, 0.0 otherwise.
    /// </summary>
    public float IsAttackReady()
    {
        if (_cooldownTimer >= attackCooldown)
            return 1.0f;
        else
            return 0.0f;
    }

    /// <summary>
    /// Resets the attack cooldown timer.
    /// </summary>
    public void ResetCooldown()
    {
        _cooldownTimer = 0;
    }
}
