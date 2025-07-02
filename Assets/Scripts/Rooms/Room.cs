using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
/// <summary>
/// Manages room state, including camera, player, enemies, traps, and collectables.
/// </summary>
public class Room : MonoBehaviour
{
    // === Constants ===
    private const float DefaultYOffset = 2f;
    private const float DefaultGravScale = 2f;
    private const float DefaultMaxFallSpeed = 100f;
    private const float DefaultChaseSpeed = 5f;
    private const string PlayerTag = "Player";

    // === Serialized Fields ===
    [SerializeField] private GameObject[] _enemies;
    [SerializeField] private GameObject[] _traps;
    [SerializeField] private GameObject[] _collectables;
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerRespawn _playerRespawn;
    [Header("Room Settings")]
    public bool freezeCamX = false;
    public bool freezeCamY = true;
    public float xFreezeValue;
    public float yFreezeValue;
    public float yOffsetValue = DefaultYOffset;
    public float gravScaleValue = DefaultGravScale;
    public float maxFallSpeedValue = DefaultMaxFallSpeed;
    public bool isChase = false;
    public float chaseSpeed = DefaultChaseSpeed;
    public float chaseStartOffSet = 0;
    public bool followPlayerY = false;

    // === Private Fields ===
    private Vector3[] _initialEnemyPositions;
    private Quaternion[] _initialEnemyRotations;
    private Vector3[] _initialTrapPositions;
    private Vector3[] _initialCollectablePositions;

    /// <summary>
    /// Called when the player enters the room. Sets camera, gravity, and activates room.
    /// </summary>
    public void EnterRoom()
    {
        _cameraController.MoveToNewRoom(transform);
        _cameraController.SetCameraXFreeze(freezeCamX, xFreezeValue);
        _cameraController.SetChaseMode(isChase);
        _cameraController.SetChaseSpeed(chaseSpeed);
        _cameraController.SetChaseStart(chaseStartOffSet);
        if (freezeCamY)
        {
            _cameraController.SetCameraYFreeze(yFreezeValue);
        }
        else
        {
            _cameraController.SetFollowPlayerY(yOffsetValue);
        }
        _playerMovement.normalGrav = gravScaleValue;
        _playerMovement.maxFallSpeed = maxFallSpeedValue;
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
