using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;


using UnityEngine;

/// <summary>
/// Handles player attack logic, including fireball and sword attacks, cooldowns, and weapon switching.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    // ==================== Constants ====================
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

    // ==================== Inspector Fields ====================
    [Tooltip("True if this player is AI controlled.")]
    [FormerlySerializedAs("isAIControlled")]
    [SerializeField] private bool _isAIControlled;
    [Tooltip("Cooldown time between attacks.")]
    [FormerlySerializedAs("attackCooldown")]
    [SerializeField] private float _attackCooldown;
    [Tooltip("Transform where fireballs are spawned.")]
    [FormerlySerializedAs("firePoint")]
    [SerializeField] private Transform _firePoint;
    [Tooltip("Array of fireball GameObjects.")]
    [FormerlySerializedAs("fireballs")]
    [SerializeField] private GameObject[] _fireballs;
    [Tooltip("Sound to play when firing a fireball.")]
    [FormerlySerializedAs("fireballSound")]
    [SerializeField] private AudioClip _fireballSound;
    [SerializeField] private AudioClip _swordSound;
    [Tooltip("Prefab for the sword weapon.")]
    [FormerlySerializedAs("swordPrefab")]
    [SerializeField] private GameObject _swordPrefab;
    [Tooltip("Transform holding the equipped weapon.")]
    [FormerlySerializedAs("weaponHolder")]
    [SerializeField] private Transform _weaponHolder;
    [Tooltip("Transform for the sword's hit point.")]
    [FormerlySerializedAs("swordHitPoint")]
    [SerializeField] private Transform _swordHitPoint;
    [Tooltip("Range of the sword attack.")]
    [FormerlySerializedAs("swordRange")]
    [SerializeField] private float _swordRange = DefaultSwordRange;
    [Tooltip("Damage dealt by the sword.")]
    [FormerlySerializedAs("swordDamage")]
    [SerializeField] private float _swordDamage = DefaultSwordDamage;
    [Tooltip("Layer mask for enemy detection.")]
    [FormerlySerializedAs("enemyLayer")]
    [SerializeField] private LayerMask _enemyLayer;

    // ==================== Private Fields ====================
    private GameObject _equippedWeaponObject;
    private enum WeaponType { Fireball, Sword }
    private WeaponType _currentWeapon = WeaponType.Fireball;
    private bool _hasSword = false;
    private Animator _animator;
    private PlayerMovement _playerMovement;
    private SpriteRenderer _playerSpriteRenderer;
    private float _cooldownTimer = Mathf.Infinity;
    private bool _isAttacking = false;

    // ==================== Properties ====================
    /// <summary>True if the game is paused for testing.</summary>
    public bool IsGamePausedForTest { get; set; }
    /// <summary>Cooldown time between attacks.</summary>
    public float AttackCooldown { get => _attackCooldown; set => _attackCooldown = value; }
    /// <summary>True if the player has a sword equipped.</summary>
    public bool HasSword { get => _hasSword; set => _hasSword = value; }
    /// <summary>Transform where fireballs are spawned.</summary>
    public Transform FirePoint { get => _firePoint; set => _firePoint = value; }
    /// <summary>Array of fireball GameObjects.</summary>
    public GameObject[] Fireballs { get => _fireballs; set => _fireballs = value; }
    /// <summary>Current attack cooldown timer.</summary>
    public float CooldownTimer { get => _cooldownTimer; set => _cooldownTimer = value; }
    /// <summary>True if the player is currently attacking.</summary>
    public bool IsAttacking => _isAttacking;

    /// <summary>
    /// For testing: triggers an attack if cooldown is ready.
    /// </summary>
    public void TestAttack()
    {
        if (_cooldownTimer >= _attackCooldown)
        {
            _fireballs[FindFireballs()].transform.position = _firePoint.position;
            _fireballs[FindFireballs()].GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
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
        if (UIManager.Instance.IsGamePaused)
        {
            return;
        }
        if (!_isAIControlled)
        {
            if (Input.GetKey(KeyCode.LeftControl) && _cooldownTimer > _attackCooldown && _playerMovement.CanAttack())
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
        if (_isAIControlled && attackPressed && _cooldownTimer > _attackCooldown && _playerMovement.CanAttack())
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
        _isAttacking = true;
        
        if (_currentWeapon == WeaponType.Fireball)
        {
            SoundManager.instance.PlaySound(_fireballSound, gameObject);
            _animator.SetTrigger(AnimatorAttack);
            int idx = FindFireballs();
            if (idx < 0) return;
            _fireballs[idx].transform.position = _firePoint.position;
            _fireballs[idx].GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
        }
        else if (_currentWeapon == WeaponType.Sword)
        {
            SwingSword();
        }
        
        // Reset attacking state after a short delay
        StartCoroutine(ResetAttackingState());
    }

    /// <summary>
    /// Handles sword swing logic and applies damage to enemies.
    /// </summary>
    private void SwingSword()
    {
        StartCoroutine(AnimateSwordSwing());
        SoundManager.instance.PlaySound(_swordSound, gameObject);
        Collider2D[] hits = Physics2D.OverlapCircleAll(_swordHitPoint.position, _swordRange, _enemyLayer);
        HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
        foreach (Collider2D hit in hits)
        {
            GameObject obj = hit.gameObject;
            if (damagedObjects.Contains(obj))
                continue;
            // If the object has a specific tag, call a special function
            if (obj.CompareTag("CagedAlly")) // Replace "SpecialEnemy" with your actual tag
            {
                // Call your custom function (make sure it exists on the right component)
                if (obj.TryGetComponent<CagedAlly>(out var cage))
                {
                    cage.ActivateAlly();
                    Destroy(cage.gameObject);
                }
            }
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_swordDamage);
                damagedObjects.Add(obj);
            }
        }
    }

    /// <summary>
    /// Animates the sword swing.
    /// </summary>
    private IEnumerator AnimateSwordSwing()
    {
        Vector3 startPos = _weaponHolder.localPosition;
        Quaternion startRot = _weaponHolder.localRotation;
        Vector3 endPos = new Vector3(startPos.x, SwordSwingEndY, startPos.z);
        Quaternion endRot = Quaternion.Euler(0f, 0f, SwordSwingEndZ);
        float elapsed = 0f;
        while (elapsed < SwordSwingDuration)
        {
            float t = elapsed / SwordSwingDuration;
            _weaponHolder.localPosition = Vector3.Lerp(startPos, endPos, t);
            _weaponHolder.localRotation = Quaternion.Lerp(startRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < SwordReturnDuration)
        {
            float t = elapsed / SwordReturnDuration;
            _weaponHolder.localPosition = Vector3.Lerp(endPos, startPos, t);
            _weaponHolder.localRotation = Quaternion.Lerp(endRot, startRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _weaponHolder.localPosition = startPos;
        _weaponHolder.localRotation = startRot;
    }

    /// <summary>
    /// Draws the sword hit area in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_swordHitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_swordHitPoint.position, _swordRange);
        }
    }

    // ==================== Attack and Weapon Logic ====================
    /// <summary>
    /// Finds an available fireball in the pool.
    /// </summary>
    private int FindFireballs()
    {
        for (int i = 0; i < _fireballs.Length; i++)
        {
            if (!_fireballs[i].activeInHierarchy)
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
        _equippedWeaponObject = Instantiate(_swordPrefab, _weaponHolder);
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
            _weaponHolder.localPosition = newPosition;
        }
    }

    // ==================== Utility ====================
    /// <summary>
    /// Returns 1.0 if attack is ready, 0.0 otherwise.
    /// </summary>
    public float IsAttackReady()
    {
        if (_cooldownTimer >= _attackCooldown)
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
    
    /// <summary>
    /// Coroutine to reset the attacking state after a short delay.
    /// </summary>
    private IEnumerator ResetAttackingState()
    {
        yield return new WaitForSeconds(0.1f); // Short delay to indicate attack is happening
        _isAttacking = false;
    }
}
