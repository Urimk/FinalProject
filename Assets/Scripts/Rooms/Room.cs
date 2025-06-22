using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
public class Room : MonoBehaviour
{
    [SerializeField] private GameObject[] _enemies;
    [SerializeField] private GameObject[] _traps;
    [SerializeField] private GameObject[] _collectables;
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerRespawn _playerRespawn;
    private Vector3[] _initialEnemyPositions;
    private Quaternion[] _initialEnemyRotations;
    private Vector3[] _initialTrapPositions;
    private Vector3[] _initialCollectablePositions;


    [Header("Room Settings")]
    public bool freezeCamX = false;
    public bool freezeCamY = true;
    public float xFreezeValue;
    public float yFreezeValue;
    public float yOffsetValue = 2f;
    public float gravScaleValue = 2f;
    public float maxFallSpeedValue = 100;
    public bool isChase = false;
    public float chaseSpeed = 5f;
    public float chaseStartOffSet = 0;
    public bool followPlayerY = false;

    public void EnterRoom()
    {
        // 1) Camera
        _cameraController.MoveToNewRoom(transform);
        _cameraController.SetCameraXFreeze(freezeCamX, xFreezeValue);
        _cameraController.SetChaseMode(isChase);
        _cameraController.SetChaseSpeed(chaseSpeed);
        _cameraController.SetChaseStart(chaseStartOffSet);

        // 2) Y-offset
        if (freezeCamY)
        {
            _cameraController.SetCameraYFreeze(yFreezeValue);
        }
        else
        {
            _cameraController.SetFollowPlayerY(yOffsetValue);
        }



        // 3) Gravity
        _playerMovement.defaultGravityScale = gravScaleValue;
        _playerMovement.maxFallSpeed = maxFallSpeedValue;

        // 4) Activate rooms
        _playerRespawn.SetCurrentRoom(transform);
    }

    private void Awake()
    {
        // Enemies
        _initialEnemyPositions = new Vector3[_enemies.Length];
        _initialEnemyRotations = new Quaternion[_enemies.Length];
        for (int i = 0; i < _enemies.Length; i++)
        {
            if (_enemies[i] != null)
                _initialEnemyPositions[i] = _enemies[i].transform.position;
            _initialEnemyRotations[i] = _enemies[i].transform.rotation;

        }

        // Traps
        _initialTrapPositions = new Vector3[_traps.Length];
        for (int i = 0; i < _traps.Length; i++)
        {
            if (_traps[i] != null)
                _initialTrapPositions[i] = _traps[i].transform.position;
        }

        // Collectables
        _initialCollectablePositions = new Vector3[_collectables.Length];
        for (int i = 0; i < _collectables.Length; i++)
        {
            if (_collectables[i] != null)
                _initialCollectablePositions[i] = _collectables[i].transform.position;
        }
    }

    public void ActivateRoom(bool _status)
    {
        for (int i = 0; i < _enemies.Length; i++)
        {
            if (_enemies[i] != null && _enemies[i].GetComponent<Health>().GetHealth() != 0f)
            {
                _enemies[i].SetActive(_status);

                if (_status)
                {
                    // Reset enemy position when activating the room
                    _enemies[i].transform.position = _initialEnemyPositions[i];
                    _enemies[i].transform.rotation = _initialEnemyRotations[i];

                }
                else
                {
                    // Deactivate all projectiles associated with the enemy
                    EnemyProjectile[] projectiles = _enemies[i].GetComponentsInChildren<EnemyProjectile>(true);
                    foreach (var projectile in projectiles)
                    {
                        projectile.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void ResetRoom()
    {
        // Reset enemies
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

        // Reset traps
        for (int i = 0; i < _traps.Length; i++)
        {
            if (_traps[i] != null)
            {
                _traps[i].SetActive(true);
                _traps[i].transform.position = _initialTrapPositions[i];
            }
        }

        // Reset collectables
        for (int i = 0; i < _collectables.Length; i++)
        {
            if (_collectables[i] != null)
            {
                _collectables[i].SetActive(true);
                _collectables[i].transform.position = _initialCollectablePositions[i];
            }
        }
    }

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
