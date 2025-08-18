using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles player respawn logic, checkpoint management, and room resets.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    // ==================== Constants ====================
    private const int DefaultLives = 3;
    private const float DefaultTimeAtCheckpoint = 600f;
    private const string InitialCheckpointName = "InitialCheckpoint";
    private const string CheckpointTag = "Checkpoint";
    private const string AnimatorAppear = "appear";

    // ==================== Inspector Fields ====================
    [Header("Checkpoint Settings")]
    [Tooltip("Sound to play when a checkpoint is activated.")]
    [FormerlySerializedAs("checkpointSound")]
    [SerializeField] private AudioClip _checkpointSound;
    [Tooltip("Reference to the GroundManager for respawn events.")]
    [FormerlySerializedAs("groundManager")]
    [SerializeField] private GroundManager _groundManager;
    [Tooltip("Number of lives the player starts with.")]
    [FormerlySerializedAs("lives")]
    [SerializeField] private int _lives = DefaultLives;
    [Tooltip("Reference to the initial room Transform.")]
    [FormerlySerializedAs("initialRoom")]
    [SerializeField] private Transform _initialRoom;

    // ==================== Private Fields ====================
    private Transform _currentCheckpoint;
    private Health _playerHealth;
    private Transform _currentRoom;
    private UIManager _uiManager;
    private int _scoreAtCheckpoint;
    private float _timeAtCheckpoint = DefaultTimeAtCheckpoint;
    private List<Transform> _roomsSinceCheckpoint = new List<Transform>();

    // ==================== Unity Lifecycle ====================
    /// <summary>
    /// Unity Awake callback. Initializes checkpoint and references.
    /// </summary>
    private void Awake()
    {
        _playerHealth = GetComponent<Health>();
        _uiManager = FindObjectOfType<UIManager>();
        GameObject initialCheckpoint = new GameObject(InitialCheckpointName);
        initialCheckpoint.transform.position = transform.position;
        _currentRoom = _initialRoom;
        if (_currentRoom != null)
        {
            initialCheckpoint.transform.parent = _currentRoom;
        }
        _currentCheckpoint = initialCheckpoint.transform;
    }

    // ==================== Checkpoint and Respawn Logic ====================
    /// <summary>
    /// Sets the current room and tracks rooms entered since the last checkpoint.
    /// </summary>
    /// <param name="newRoom">The new room Transform.</param>
    public void SetCurrentRoom(Transform newRoom)
    {
        _currentRoom = newRoom;
        if (_currentCheckpoint != null && newRoom != _currentCheckpoint.parent)
        {
            if (!_roomsSinceCheckpoint.Contains(newRoom))
                _roomsSinceCheckpoint.Add(newRoom);
        }
    }

    /// <summary>
    /// Respawns the player at the last checkpoint, resets rooms, and restores state.
    /// </summary>
    public void Respawn()
    {
        Respawn(false); // Default to instant respawn
    }

    /// <summary>
    /// Respawns the player at the last checkpoint, resets rooms, and restores state.
    /// </summary>
    /// <param name="useTransitions">Whether to use smooth transitions for camera movement.</param>
    public void Respawn(bool useTransitions)
    {
        _playerHealth.SetFirstHealth(0);
        if (_lives <= 0)
        {
            if (_checkpointSound != null)
            {
                _uiManager.GameOver();
                return;
            }
            // Disable the GameObject that has the PlayerHealth component
            Destroy(_playerHealth.gameObject);
            return;
        }

        Transform respawnRoom = _currentCheckpoint.parent;
        transform.position = _currentCheckpoint.position;

        // Reset all rooms since last checkpoint
        foreach (var roomTransform in _roomsSinceCheckpoint)
        {
            var roomComp = roomTransform.GetComponent<Room>();
            if (roomComp != null)
                roomComp.ResetRoom();
        }

        // Reset checkpoint room
        if (respawnRoom != null)
        {
            var checkpointRoomComp = respawnRoom.GetComponent<Room>();
            if (checkpointRoomComp != null)
                checkpointRoomComp.ResetRoom();
        }

        _roomsSinceCheckpoint.Clear();

        // Update player state
        _lives--;
        _playerHealth.Respawn();
        ScoreManager.Instance.SetScore(_scoreAtCheckpoint);
        TimerManager.Instance.SetRemainingTime(_timeAtCheckpoint);

        // Handle room activation and camera movement
        if (respawnRoom != null)
        {
            _groundManager?.OnPlayerRespawn();
            
            // Deactivate current room if different
            if (_currentRoom != respawnRoom)
            {
                _currentRoom?.GetComponent<Room>()?.ActivateRoom(false);
                respawnRoom.GetComponent<Room>().ActivateRoom(true);
                _currentRoom = respawnRoom;
            }

            // Enter the respawn room with proper camera setup
            respawnRoom.GetComponent<Room>().EnterRoom(useTransitions);
        }
        else
        {
            // Fallback to initial room
            Debug.LogWarning("Respawn room is null. Using initial room.");
            _currentRoom = _initialRoom;
            _initialRoom.GetComponent<Room>().EnterRoom(useTransitions);
        }
    }

    // ==================== Unity Events ====================
    /// <summary>
    /// Handles checkpoint activation and updates checkpoint state.
    /// </summary>
    /// <param name="collision">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(CheckpointTag))
        {
            Checkpoint checkpoint = collision.GetComponent<Checkpoint>();
            if (checkpoint != null && !checkpoint.IsActivated)
            {
                checkpoint.IsActivated = true;
                _currentCheckpoint = collision.transform;
                SoundManager.instance.PlaySound(_checkpointSound, gameObject);
                _scoreAtCheckpoint = ScoreManager.Instance.GetScore();
                _timeAtCheckpoint = TimerManager.Instance.GetRemainingTime();
                _roomsSinceCheckpoint.Clear();
                Transform respawnRoom = _currentCheckpoint.parent;
                var roomComponent = respawnRoom.GetComponent<Room>();
                roomComponent.RemoveCollectedDiamonds();
                collision.GetComponent<Collider2D>().enabled = false;
                collision.GetComponent<Animator>().SetTrigger(AnimatorAppear);
            }
        }
    }
}
