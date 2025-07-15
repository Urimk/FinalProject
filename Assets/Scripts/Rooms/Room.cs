using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.Serialization;
/// <summary>
/// Manages room state, including camera, player, enemies, traps, and collectables.
/// </summary>
public class Room : MonoBehaviour
{
    // ==================== Constants ====================
    private const float DefaultYOffset = 2f;
    private const float DefaultGravScale = 2f;
    private const float DefaultMaxFallSpeed = 100f;
    private const float DefaultChaseSpeed = 5f;
    private const string PlayerTag = "Player";

    // ==================== Inspector Fields ====================
    [Tooltip("Array of enemy GameObjects in this room.")]
    [FormerlySerializedAs("enemies")]
    [SerializeField] private GameObject[] _enemies;
    [Tooltip("Array of trap GameObjects in this room.")]
    [FormerlySerializedAs("traps")]
    [SerializeField] private GameObject[] _traps;
    [Tooltip("Array of collectable GameObjects in this room.")]
    [FormerlySerializedAs("collectables")]
    [SerializeField] private GameObject[] _collectables;
    [Tooltip("Reference to the CameraController.")]
    [FormerlySerializedAs("cameraController")]
    [SerializeField] private CameraController _cameraController;
    [Tooltip("Reference to the PlayerMovement component.")]
    [FormerlySerializedAs("playerMovement")]
    [SerializeField] private PlayerMovement _playerMovement;
    [Tooltip("Reference to the PlayerRespawn component.")]
    [FormerlySerializedAs("playerRespawn")]
    [SerializeField] private PlayerRespawn _playerRespawn;
    [Header("Room Settings")]
    [Tooltip("Freeze camera X axis in this room.")]
    [FormerlySerializedAs("freezeCamX")]
    [SerializeField] private bool _freezeCamX = false;
    [Tooltip("Freeze camera Y axis in this room.")]
    [FormerlySerializedAs("freezeCamY")]
    [SerializeField] private bool _freezeCamY = true;
    [Tooltip("Value to freeze camera X at.")]
    [FormerlySerializedAs("xFreezeValue")]
    [SerializeField] private float _xFreezeValue;
    [Tooltip("Value to freeze camera Y at.")]
    [FormerlySerializedAs("yFreezeValue")]
    [SerializeField] private float _yFreezeValue;
    [Tooltip("YOffset for following player.")]
    [FormerlySerializedAs("yOffsetValue")]
    [SerializeField] private float _yOffsetValue = DefaultYOffset;
    [Tooltip("Gravity scale for the player in this room.")]
    [FormerlySerializedAs("gravScaleValue")]
    [SerializeField] private float _gravScaleValue = DefaultGravScale;
    [Tooltip("Maximum fall speed for the player in this room.")]
    [FormerlySerializedAs("maxFallSpeedValue")]
    [SerializeField] private float _maxFallSpeedValue = DefaultMaxFallSpeed;
    [Tooltip("True if this room is a chase room.")]
    [FormerlySerializedAs("isChase")]
    [SerializeField] private bool _isChase = false;
    [Tooltip("Chase speed for the room.")]
    [FormerlySerializedAs("chaseSpeed")]
    [SerializeField] private float _chaseSpeed = DefaultChaseSpeed;
    [Tooltip("Chase start offset for the room.")]
    [FormerlySerializedAs("chaseStartOffSet")]
    [SerializeField] private float _chaseStartOffSet = 0;
    [Tooltip("True if camera should follow player Y.")]
    [FormerlySerializedAs("followPlayerY")]
    [SerializeField] private bool _followPlayerY = false;

    // ==================== Private Fields ====================
    private Vector3[] _initialEnemyPositions;
    private Quaternion[] _initialEnemyRotations;
    private Vector3[] _initialTrapPositions;
    private Vector3[] _initialCollectablePositions;

    // ==================== Properties ====================
    /// <summary>Freeze camera X axis in this room.</summary>
    public bool FreezeCamX { get => _freezeCamX; set => _freezeCamX = value; }
    /// <summary>Freeze camera Y axis in this room.</summary>
    public bool FreezeCamY { get => _freezeCamY; set => _freezeCamY = value; }
    /// <summary>Value to freeze camera X at.</summary>
    public float XFreezeValue { get => _xFreezeValue; set => _xFreezeValue = value; }
    /// <summary>Value to freeze camera Y at.</summary>
    public float YFreezeValue { get => _yFreezeValue; set => _yFreezeValue = value; }
    /// <summary>YOffset for following player.</summary>
    public float YOffsetValue { get => _yOffsetValue; set => _yOffsetValue = value; }
    /// <summary>Gravity scale for the player in this room.</summary>
    public float GravScaleValue { get => _gravScaleValue; set => _gravScaleValue = value; }
    /// <summary>Maximum fall speed for the player in this room.</summary>
    public float MaxFallSpeedValue { get => _maxFallSpeedValue; set => _maxFallSpeedValue = value; }
    /// <summary>True if this room is a chase room.</summary>
    public bool IsChase { get => _isChase; set => _isChase = value; }
    /// <summary>Chase speed for the room.</summary>
    public float ChaseSpeed { get => _chaseSpeed; set => _chaseSpeed = value; }
    /// <summary>Chase start offset for the room.</summary>
    public float ChaseStartOffSet { get => _chaseStartOffSet; set => _chaseStartOffSet = value; }
    /// <summary>True if camera should follow player Y.</summary>
    public bool FollowPlayerY { get => _followPlayerY; set => _followPlayerY = value; }

    /// <summary>
    /// Called when the player enters the room. Sets camera, gravity, and activates room.
    /// </summary>
    public void EnterRoom()
    {
        _cameraController.MoveToNewRoom(transform);
        _cameraController.SetCameraXFreeze(_freezeCamX, _xFreezeValue);
        _cameraController.SetChaseMode(_isChase);
        _cameraController.SetChaseSpeed(_chaseSpeed);
        _cameraController.SetChaseStart(_chaseStartOffSet);
        if (_freezeCamY)
        {
            _cameraController.SetCameraYFreeze(_yFreezeValue);
        }
        else
        {
            _cameraController.SetFollowPlayerY(_yOffsetValue);
        }
        _playerMovement.NormalGrav = _gravScaleValue;
        _playerMovement.MaxFallSpeed = _maxFallSpeedValue;
        _playerRespawn.SetCurrentRoom(transform);
    }

    /// <summary>
    /// Unity Awake callback. Stores initial positions and rotations for enemies, traps, and collectables.
    /// </summary>
    private void Awake()
    {
        _initialEnemyPositions = new Vector3[_enemies.Length];
        _initialEnemyRotations = new Quaternion[_enemies.Length];
        for (int i = 0; i < _enemies.Length; i++)
        {
            if (_enemies[i] != null)
                _initialEnemyPositions[i] = _enemies[i].transform.position;
            _initialEnemyRotations[i] = _enemies[i].transform.rotation;
        }
        _initialTrapPositions = new Vector3[_traps.Length];
        for (int i = 0; i < _traps.Length; i++)
        {
            if (_traps[i] != null)
                _initialTrapPositions[i] = _traps[i].transform.position;
        }
        _initialCollectablePositions = new Vector3[_collectables.Length];
        for (int i = 0; i < _collectables.Length; i++)
        {
            if (_collectables[i] != null)
                _initialCollectablePositions[i] = _collectables[i].transform.position;
        }
    }

    /// <summary>
    /// Activates or deactivates the room and its enemies.
    /// </summary>
    public void ActivateRoom(bool status)
    {
        for (int i = 0; i < _enemies.Length; i++)
        {
            if (_enemies[i] != null && _enemies[i].GetComponent<Health>().GetHealth() != 0f)
            {
                _enemies[i].SetActive(status);
                if (status)
                {
                    _enemies[i].transform.position = _initialEnemyPositions[i];
                    _enemies[i].transform.rotation = _initialEnemyRotations[i];
                }
                else
                {
                    EnemyProjectile[] projectiles = _enemies[i].GetComponentsInChildren<EnemyProjectile>(true);
                    foreach (var projectile in projectiles)
                    {
                        projectile.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resets the room's enemies, traps, and collectables to their initial state.
    /// </summary>
    public void ResetRoom()
    {
        for (int i = 0; i < _enemies.Length; i++)
        {
            if (_enemies[i] != null)
            {
                _enemies[i].SetActive(true);
                _enemies[i].GetComponent<Health>().ReEnableComponents();
                _enemies[i].GetComponent<Health>().ResetHealth();
                _enemies[i].transform.position = _initialEnemyPositions[i];
                _enemies[i].transform.rotation = _initialEnemyRotations[i];
            }
        }
        for (int i = 0; i < _traps.Length; i++)
        {
            if (_traps[i] != null)
            {
                _traps[i].SetActive(true);
                _traps[i].transform.position = _initialTrapPositions[i];
            }
        }
        for (int i = 0; i < _collectables.Length; i++)
        {
            if (_collectables[i] != null)
            {
                _collectables[i].SetActive(true);
                _collectables[i].transform.position = _initialCollectablePositions[i];
            }
        }
    }

    /// <summary>
    /// Removes collected diamonds from the room's collectables list.
    /// </summary>
    public void RemoveCollectedDiamonds()
    {
        List<GameObject> activeCollectables = new List<GameObject>();
        List<Vector3> activePositions = new List<Vector3>();
        for (int i = 0; i < _collectables.Length; i++)
        {
            if (_collectables[i] != null && _collectables[i].activeInHierarchy)
            {
                activeCollectables.Add(_collectables[i]);
                activePositions.Add(_initialCollectablePositions[i]);
            }
        }
        _collectables = activeCollectables.ToArray();
        _initialCollectablePositions = activePositions.ToArray();
    }
}
