using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Manages room state, including camera, player, enemies, traps, and collectables.
/// </summary>
public class Room : MonoBehaviour
{
    // ==================== Enums ====================
    /// <summary>
    /// Defines the camera mode for each axis in this room.
    /// </summary>
    public enum RoomCameraMode
    {
        Static,     // Camera stays at a fixed position
        Follow,     // Camera follows the player
        Moving      // Camera moves independently (chase, scrolling, etc.)
    }

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
    [FormerlySerializedAs("Traps")]
    [SerializeField] private GameObject[] _traps;
    [Tooltip("Array of collectable GameObjects in this room.")]
    [FormerlySerializedAs("Collectables")]
    [SerializeField] private GameObject[] _collectables;
    [Tooltip("Reference to the CameraController.")]
    [FormerlySerializedAs("cam")]
    [SerializeField] private CameraController _cameraController;
    [Tooltip("Reference to the PlayerMovement component.")]
    [FormerlySerializedAs("pm")]
    [SerializeField] private PlayerMovement _playerMovement;
    [Tooltip("Reference to the PlayerRespawn component.")]
    [FormerlySerializedAs("pr")]
    [SerializeField] private PlayerRespawn _playerRespawn;
    [Header("Camera Settings")]
    [Tooltip("Camera mode for X axis in this room.")]
    [FormerlySerializedAs("freezeCamX")]
    [SerializeField] private RoomCameraMode _xCameraMode = RoomCameraMode.Follow;
    [Tooltip("Camera mode for Y axis in this room.")]
    [FormerlySerializedAs("freezeCamY")]
    [SerializeField] private RoomCameraMode _yCameraMode = RoomCameraMode.Static;
    [Header("Static Mode Settings")]
    [Tooltip("X position when camera is in Static or Moving mode.")]
    [FormerlySerializedAs("xFreezeValue")]
    [SerializeField] private float _xPosition = 0f;
    [Tooltip("Y position when camera is in Static or Moving mode.")]
    [FormerlySerializedAs("yFreezeValue")]
    [SerializeField] private float _yPosition = 0f;
    [Header("Follow Mode Settings")]
    [Tooltip("Y offset when following the player.")]
    [FormerlySerializedAs("yOffsetValue")]
    [SerializeField] private float _yOffset = DefaultYOffset;
    [Header("Chase Settings")]
    [Tooltip("Speed for Moving camera mode (chase, scrolling, etc.).")]
    [FormerlySerializedAs("chaseSpeed")]
    [SerializeField] private float _movingSpeed = DefaultChaseSpeed;
    [Header("Falling Settings")]
    [Tooltip("Gravity scale for the player in this room.")]
    [FormerlySerializedAs("gravScaleValue")]
    [SerializeField] private float _gravScale = DefaultGravScale;
    [Tooltip("Maximum fall speed for the player in this room.")]
    [FormerlySerializedAs("maxFallSpeedValue")]
    [SerializeField] private float _maxFallSpeed = DefaultMaxFallSpeed;

    // ==================== Private Fields ====================
    private Vector3[] _initialEnemyPositions;
    private Quaternion[] _initialEnemyRotations;
    private Vector3[] _initialTrapPositions;
    private Vector3[] _initialCollectablePositions;

    // ==================== Properties ====================
    /// <summary>Camera mode for X axis in this room.</summary>
    public RoomCameraMode XCameraMode { get => _xCameraMode; set => _xCameraMode = value; }
    /// <summary>Camera mode for Y axis in this room.</summary>
    public RoomCameraMode YCameraMode { get => _yCameraMode; set => _yCameraMode = value; }
    /// <summary>X position when camera is in Static or Moving mode.</summary>
    public float XPosition { get => _xPosition; set => _xPosition = value; }
    /// <summary>Y position when camera is in Static or Moving mode.</summary>
    public float YPosition { get => _yPosition; set => _yPosition = value; }
    /// <summary>Y offset when following the player.</summary>
    public float YOffset { get => _yOffset; set => _yOffset = value; }
    /// <summary>Gravity scale for the player in this room.</summary>
    public float GravScale { get => _gravScale; set => _gravScale = value; }
    /// <summary>Maximum fall speed for the player in this room.</summary>
    public float MaxFallSpeed { get => _maxFallSpeed; set => _maxFallSpeed = value; }
    /// <summary>Speed for Moving camera mode.</summary>
    public float MovingSpeed { get => _movingSpeed; set => _movingSpeed = value; }

    /// <summary>
    /// Called when the player enters the room. Sets camera, gravity, and activates room.
    /// </summary>
    public void EnterRoom()
    {
        EnterRoom(false); // Default to no transitions for instant setup
    }

    /// <summary>
    /// Called when the player enters the room. Sets camera, gravity, and activates room.
    /// </summary>
    /// <param name="useTransitions">Whether to use smooth transitions when setting camera modes.</param>
    public void EnterRoom(bool useTransitions)
    {
        // Set X-axis camera mode
        switch (_xCameraMode)
        {
            case RoomCameraMode.Static:
                _cameraController.SetStaticXPosition(_xPosition);
                _cameraController.SetXMode(CameraController.CameraMode.Static, useTransitions);
                break;
            case RoomCameraMode.Follow:
                _cameraController.SetXMode(CameraController.CameraMode.Follow, useTransitions);
                break;
            case RoomCameraMode.Moving:
                // Set up chase mode
                _cameraController.SetMovingSpeed(_movingSpeed);
                _cameraController.SetMovingTarget(new Vector3(_cameraController.transform.position.x, _yPosition, _cameraController.transform.position.z));
                _cameraController.SetXMode(CameraController.CameraMode.Moving, useTransitions);
                break;
        }
        
        // Set Y-axis camera mode (after room movement)
        switch (_yCameraMode)
        {
            case RoomCameraMode.Static:
                _cameraController.SetStaticYPosition(_yPosition);
                _cameraController.SetYMode(CameraController.CameraMode.Static, useTransitions);
                break;
            case RoomCameraMode.Follow:
                _cameraController.SetFollowOffset('Y', _yOffset, useTransitions);
                _cameraController.SetYMode(CameraController.CameraMode.Follow, useTransitions);
                break;
            case RoomCameraMode.Moving:
                Debug.Log("Moving Y mode");
                _cameraController.SetMovingTarget(new Vector3(_cameraController.transform.position.x, _yPosition, _cameraController.transform.position.z));
                _cameraController.SetYMode(CameraController.CameraMode.Moving, useTransitions);
                break;
        }
        
        // Set player physics
        _playerMovement.NormalGrav = _gravScale;
        _playerMovement.MaxFallSpeed = _maxFallSpeed;
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
                if (!_enemies[i].activeSelf)
                {
                    _enemies[i].SetActive(status);
                    if (status)
                    {
                        _enemies[i].transform.position = _initialEnemyPositions[i];
                        _enemies[i].transform.rotation = _initialEnemyRotations[i];
                    }
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
                // Reset the collectable using the new CollectableBase system
                var collectable = _collectables[i].GetComponent<ICollectable>();
                if (collectable != null)
                {
                    collectable.Reset();
                }
                else
                {
                    // Fallback for legacy collectables that don't use the new system
                _collectables[i].SetActive(true);
                }
                _collectables[i].transform.position = _initialCollectablePositions[i];
            }
        }
    }

    /// <summary>
    /// Removes collected diamonds from the room's collectables list.
    /// Updated to work with the new CollectableBase system.
    /// </summary>
    public void RemoveCollectedDiamonds()
    {
        List<GameObject> activeCollectables = new List<GameObject>();
        List<Vector3> activePositions = new List<Vector3>();

        for (int i = 0; i < _collectables.Length; i++)
        {
            if (_collectables[i] != null)
            {
                // Check if the collectable is active using the new system
                var collectable = _collectables[i].GetComponent<ICollectable>();
                bool isActive = collectable != null ? !collectable.IsCollected : _collectables[i].activeInHierarchy;

                if (isActive)
            {
                activeCollectables.Add(_collectables[i]);
                activePositions.Add(_initialCollectablePositions[i]);
            }
        }
        }

        _collectables = activeCollectables.ToArray();
        _initialCollectablePositions = activePositions.ToArray();
    }

    /// <summary>
    /// Gets all collectables in this room that implement the ICollectable interface.
    /// </summary>
    /// <returns>Array of ICollectable components found in this room.</returns>
    public ICollectable[] GetCollectables()
    {
        List<ICollectable> collectables = new List<ICollectable>();

        for (int i = 0; i < _collectables.Length; i++)
        {
            if (_collectables[i] != null)
            {
                var collectable = _collectables[i].GetComponent<ICollectable>();
                if (collectable != null)
                {
                    collectables.Add(collectable);
                }
            }
        }

        return collectables.ToArray();
    }

    /// <summary>
    /// Resets all collectables in this room to their initial state.
    /// </summary>
    public void ResetAllCollectables()
    {
        for (int i = 0; i < _collectables.Length; i++)
        {
            if (_collectables[i] != null)
            {
                var collectable = _collectables[i].GetComponent<ICollectable>();
                if (collectable != null)
                {
                    collectable.Reset();
                }
                _collectables[i].transform.position = _initialCollectablePositions[i];
            }
        }
    }

    // ==================== Camera Configuration Helpers ====================
    
    /// <summary>
    /// Sets the camera to follow the player on both X and Y axes.
    /// </summary>
    /// <param name="yOffset">Y offset when following player.</param>
    public void SetCameraFollowPlayer(float yOffset = -1f)
    {
        if (yOffset != -1f)
        {
            _yOffset = yOffset;
        }
        _xCameraMode = RoomCameraMode.Follow;
        _yCameraMode = RoomCameraMode.Follow;
    }

    /// <summary>
    /// Sets the camera to follow player on X axis and static on Y axis.
    /// </summary>
    /// <param name="yPosition">Y position to stay at.</param>
    public void SetCameraFollowXStaticY(float yPosition)
    {
        _xCameraMode = RoomCameraMode.Follow;
        _yCameraMode = RoomCameraMode.Static;
        _yPosition = yPosition;
    }

    /// <summary>
    /// Sets the camera to static on X axis and follow player on Y axis.
    /// </summary>
    /// <param name="xPosition">X position to stay at.</param>
    /// <param name="yOffset">Y offset when following player.</param>
    public void SetCameraStaticXFollowY(float xPosition, float yOffset = -1f)
    {
        _xCameraMode = RoomCameraMode.Static;
        _xPosition = xPosition;
        _yCameraMode = RoomCameraMode.Follow;
        if (yOffset != -1f)
        {
            _yOffset = yOffset;
        }
    }

    /// <summary>
    /// Sets the camera to static on both X and Y axes.
    /// </summary>
    /// <param name="xPosition">X position to stay at.</param>
    /// <param name="yPosition">Y position to stay at.</param>
    public void SetCameraStaticBoth(float xPosition, float yPosition)
    {
        _xCameraMode = RoomCameraMode.Static;
        _xPosition = xPosition;
        _yCameraMode = RoomCameraMode.Static;
        _yPosition = yPosition;
    }


    /// <summary>
    /// Sets both X and Y camera modes directly.
    /// </summary>
    /// <param name="xMode">X-axis camera mode.</param>
    /// <param name="yMode">Y-axis camera mode.</param>
    public void SetCameraModes(RoomCameraMode xMode, RoomCameraMode yMode)
    {
        _xCameraMode = xMode;
        _yCameraMode = yMode;
    }

    /// <summary>
    /// Gets the current camera configuration as a readable string.
    /// </summary>
    /// <returns>String describing the current camera setup.</returns>
    public string GetCameraConfiguration()
    {
        string xMode = _xCameraMode switch
        {
            RoomCameraMode.Static => $"Static at {_xPosition}",
            RoomCameraMode.Follow => "Follow Player",
            RoomCameraMode.Moving => $"Moving (Speed: {_movingSpeed})",
            _ => "Unknown"
        };
        
        string yMode = _yCameraMode switch
        {
            RoomCameraMode.Static => $"Static at {_yPosition}",
            RoomCameraMode.Follow => $"Follow Player (Offset: {_yOffset})",
            RoomCameraMode.Moving => $"Moving (Speed: {_movingSpeed})",
            _ => "Unknown"
        };
        
        return $"X: {xMode}, Y: {yMode}";
    }
}

