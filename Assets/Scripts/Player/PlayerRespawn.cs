using System.Collections.Generic;

using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private AudioClip checkpointSound;
    [SerializeField] private GroundManager groundManager;
    [SerializeField] private int lives = 3; // Set the number of lives in the Inspector
    [SerializeField] private Transform initialRoom;

    private Transform _currentCheckpoint;
    private Health _playerHealth;
    private Transform _currentRoom; // Tracks the currently active room
    private UIManager _uiManager;

    private int _scoreAtCheckpoint;
    private float _timeAtCheckpoint = 600f;

    // List of rooms visited since the last checkpoint was activated
    private List<Transform> _roomsSinceCheckpoint = new List<Transform>();

    private void Awake()
    {
        _playerHealth = GetComponent<Health>();
        _uiManager = FindObjectOfType<UIManager>();

        // Set the initial checkpoint to the player's starting position
        GameObject initialCheckpoint = new GameObject("InitialCheckpoint");
        initialCheckpoint.transform.position = transform.position;
        _currentRoom = initialRoom;

        // Assign to starting room if provided
        if (_currentRoom != null)
        {
            initialCheckpoint.transform.parent = _currentRoom;
        }
        _currentCheckpoint = initialCheckpoint.transform;
    }

    // Called by Room or CameraController when the player enters a new room
    public void SetCurrentRoom(Transform newRoom)
    {
        _currentRoom = newRoom;

        // Track rooms entered since the checkpoint
        if (_currentCheckpoint != null && newRoom != _currentCheckpoint.parent)
        {
            if (!_roomsSinceCheckpoint.Contains(newRoom))
                _roomsSinceCheckpoint.Add(newRoom);
        }
    }

    public void Respawn()
    {
        _playerHealth.SetFirstHealth(0);
        if (lives <= 0)
        {
            _uiManager.GameOver();
            return;
        }

        // Move player to the checkpoint
        Transform respawnRoom = _currentCheckpoint.parent;
        transform.position = _currentCheckpoint.position;

        // Reset all rooms visited since the checkpoint
        foreach (var roomTransform in _roomsSinceCheckpoint)
        {
            var roomComp = roomTransform.GetComponent<Room>();
            if (roomComp != null)
                roomComp.ResetRoom();
        }

        // Also reset the checkpoint room itself
        if (respawnRoom != null)
        {
            var checkpointRoomComp = respawnRoom.GetComponent<Room>();
            if (checkpointRoomComp != null)
                checkpointRoomComp.ResetRoom();
        }

        // Clear the visited rooms list
        _roomsSinceCheckpoint.Clear();

        // Enter the respawn room
        if (respawnRoom != null)
        {
            respawnRoom.GetComponent<Room>().EnterRoom();
        }

        // Respawn player health and restore state
        _playerHealth.Respawn();
        ScoreManager.Instance.SetScore(_scoreAtCheckpoint);
        TimerManager.Instance.SetRemainingTime(_timeAtCheckpoint);

        // Handle camera and room activation
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Checkpoint"))
        {
            Checkpoint checkpoint = collision.GetComponent<Checkpoint>();
            if (checkpoint != null && !checkpoint.isActivated)
            {
                checkpoint.isActivated = true;

                _currentCheckpoint = collision.transform;
                SoundManager.instance.PlaySound(checkpointSound, gameObject);
                _scoreAtCheckpoint = ScoreManager.Instance.GetScore();
                _timeAtCheckpoint = TimerManager.Instance.GetRemainingTime();

                // Clear the list of rooms visited since this new checkpoint
                _roomsSinceCheckpoint.Clear();

                Transform respawnRoom = _currentCheckpoint.parent;
                var roomComponent = respawnRoom.GetComponent<Room>();
                roomComponent.RemoveCollectedDiamonds();

                collision.GetComponent<Collider2D>().enabled = false;
                collision.GetComponent<Animator>().SetTrigger("appear");
            }
        }
    }
}
