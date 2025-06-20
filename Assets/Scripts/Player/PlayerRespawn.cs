using UnityEngine;
using System.Collections.Generic;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private AudioClip checkpointSound;
    [SerializeField] private GroundManager groundManager;
    [SerializeField] private int lives = 3; // Set the number of lives in the Inspector
    [SerializeField] private Transform initialRoom;

    private Transform currentCheckpoint;
    private Health playerHealth;
    private Transform currentRoom; // Tracks the currently active room
    private UIManager uiManager;

    private int scoreAtCheckpoint;
    private float timeAtCheckpoint = 600f;

    // List of rooms visited since the last checkpoint was activated
    private List<Transform> roomsSinceCheckpoint = new List<Transform>();

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        uiManager = FindObjectOfType<UIManager>();

        // Set the initial checkpoint to the player's starting position
        GameObject initialCheckpoint = new GameObject("InitialCheckpoint");
        initialCheckpoint.transform.position = transform.position;
        currentRoom = initialRoom;

        // Assign to starting room if provided
        if (currentRoom != null)
        {
            initialCheckpoint.transform.parent = currentRoom;
        }
        currentCheckpoint = initialCheckpoint.transform;
    }

    // Called by Room or CameraController when the player enters a new room
    public void SetCurrentRoom(Transform newRoom)
    {
        currentRoom = newRoom;

        // Track rooms entered since the checkpoint
        if (currentCheckpoint != null && newRoom != currentCheckpoint.parent)
        {
            if (!roomsSinceCheckpoint.Contains(newRoom))
                roomsSinceCheckpoint.Add(newRoom);
        }
    }

    public void Respawn()
    {
        playerHealth.setFirstHealth(0);
        if (lives <= 0)
        {
            uiManager.GameOver();
            return;
        }

        // Move player to the checkpoint
        Transform respawnRoom = currentCheckpoint.parent;
        transform.position = currentCheckpoint.position;

        // Reset all rooms visited since the checkpoint
        foreach (var roomTransform in roomsSinceCheckpoint)
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
        roomsSinceCheckpoint.Clear();

        // Enter the respawn room
        if (respawnRoom != null)
        {
            respawnRoom.GetComponent<Room>().EnterRoom();
        }

        // Respawn player health and restore state
        playerHealth.HRespawn();
        ScoreManager.Instance.SetScore(scoreAtCheckpoint);
        TimerManager.Instance.SetRemainingTime(timeAtCheckpoint);

        // Handle camera and room activation
        if (respawnRoom != null)
        {
            groundManager?.OnPlayerRespawn();
            Camera.main.GetComponent<CameraController>().MoveToNewRoom(respawnRoom);

            if (currentRoom != respawnRoom)
            {
                currentRoom?.GetComponent<Room>()?.ActivateRoom(false);
                respawnRoom.GetComponent<Room>().ActivateRoom(true);
                currentRoom = respawnRoom;
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

                currentCheckpoint = collision.transform;
                SoundManager.instance.PlaySound(checkpointSound, gameObject);
                scoreAtCheckpoint = ScoreManager.Instance.GetScore();
                timeAtCheckpoint = TimerManager.Instance.GetRemainingTime();

                // Clear the list of rooms visited since this new checkpoint
                roomsSinceCheckpoint.Clear();

                Transform respawnRoom = currentCheckpoint.parent;
                var roomComponent = respawnRoom.GetComponent<Room>();
                roomComponent.RemoveCollectedDiamonds();

                collision.GetComponent<Collider2D>().enabled = false;
                collision.GetComponent<Animator>().SetTrigger("appear");
            }
        }
    }
}
