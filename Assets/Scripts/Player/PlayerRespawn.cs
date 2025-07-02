using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Handles player respawn logic, checkpoint management, and room resets.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    // === Constants ===
    private const int DefaultLives = 3;
    private const float DefaultTimeAtCheckpoint = 600f;
    private const string InitialCheckpointName = "InitialCheckpoint";
    private const string CheckpointTag = "Checkpoint";
    private const string AnimatorAppear = "appear";

    // === Serialized Fields ===
    [SerializeField] private AudioClip checkpointSound;
    [SerializeField] private GroundManager groundManager;
    [SerializeField] private int lives = DefaultLives;
    [SerializeField] private Transform initialRoom;

    // === Private Fields ===
    private Transform _currentCheckpoint;
    private Health _playerHealth;
    private Transform _currentRoom;
    private UIManager _uiManager;
    private int _scoreAtCheckpoint;
    private float _timeAtCheckpoint = DefaultTimeAtCheckpoint;
    private List<Transform> _roomsSinceCheckpoint = new List<Transform>();

    /// <summary>
    /// Unity Awake callback. Initializes checkpoint and references.
    /// </summary>
    private void Awake()
    {
        _playerHealth = GetComponent<Health>();
        _uiManager = FindObjectOfType<UIManager>();
        GameObject initialCheckpoint = new GameObject(InitialCheckpointName);
        initialCheckpoint.transform.position = transform.position;
        _currentRoom = initialRoom;
        if (_currentRoom != null)
        {
            initialCheckpoint.transform.parent = _currentRoom;
        }
        _currentCheckpoint = initialCheckpoint.transform;
    }

    /// <summary>
    /// Sets the current room and tracks rooms entered since the last checkpoint.
    /// </summary>
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
        _playerHealth.SetFirstHealth(0);
        if (lives <= 0)
        {
            _uiManager.GameOver();
            return;
        }
        Transform respawnRoom = _currentCheckpoint.parent;
        transform.position = _currentCheckpoint.position;
        foreach (var roomTransform in _roomsSinceCheckpoint)
        {
            var roomComp = roomTransform.GetComponent<Room>();
            if (roomComp != null)
                roomComp.ResetRoom();
        }
        if (respawnRoom != null)
        {
            var checkpointRoomComp = respawnRoom.GetComponent<Room>();
            if (checkpointRoomComp != null)
                checkpointRoomComp.ResetRoom();
        }
        _roomsSinceCheckpoint.Clear();
        if (respawnRoom != null)
        {
            respawnRoom.GetComponent<Room>().EnterRoom();
        }
        _playerHealth.Respawn();
        ScoreManager.Instance.SetScore(_scoreAtCheckpoint);
        TimerManager.Instance.SetRemainingTime(_timeAtCheckpoint);
        if (respawnRoom != null)
        {
            groundManager?.OnPlayerRespawn();
            Camera.main.GetComponent<CameraController>().MoveToNewRoom(respawnRoom);
            if (_currentRoom != respawnRoom)
            {
                _currentRoom?.GetComponent<Room>()?.ActivateRoom(false);
                respawnRoom.GetComponent<Room>().ActivateRoom(true);
                _currentRoom = respawnRoom;
            }
        }
        else
        {
            Camera.main.GetComponent<CameraController>().MoveToNewRoom(initialRoom);
            Debug.LogWarning("Respawn room is null. Skipping room changes for the initial checkpoint.");
        }
    }

    /// <summary>
    /// Handles checkpoint activation and updates checkpoint state.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(CheckpointTag))
        {
            Checkpoint checkpoint = collision.GetComponent<Checkpoint>();
            if (checkpoint != null && !checkpoint.IsActivated)
            {
                checkpoint.IsActivated = true;
                _currentCheckpoint = collision.transform;
                SoundManager.instance.PlaySound(checkpointSound, gameObject);
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
