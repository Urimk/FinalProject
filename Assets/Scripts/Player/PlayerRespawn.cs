using UnityEngine;

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
    private float timeAtCheckpoint;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        uiManager = FindObjectOfType<UIManager>();

        // Set the initial checkpoint to the player's starting position
        GameObject initialCheckpoint = new GameObject("InitialCheckpoint");
        initialCheckpoint.transform.position = transform.position;
        currentRoom = initialRoom;

        // Optionally, set a default room if one exists
        if (currentRoom != null)
        {
            initialCheckpoint.transform.parent = currentRoom; // Assign to starting room
        }
        currentCheckpoint = initialCheckpoint.transform;
    }

    public void SetCurrentRoom(Transform newRoom)
    {
        currentRoom = newRoom;
    }

    public void Respawn()
    {
        playerHealth.setFirstHealth(0);
        if (lives <= 0)
        {
            uiManager.GameOver();
            return;
        }

        // Reduce lives by 1
        //lives--;

        // Check if the checkpoint has a parent (room)
        Transform respawnRoom = currentCheckpoint.parent;


        // Move player to the checkpoint
        transform.position = currentCheckpoint.position;
        var roomComponent = respawnRoom.GetComponent<Room>();
        roomComponent.ResetRoom();
        roomComponent.EnterRoom();

        // Respawn player health
        playerHealth.HRespawn();
        ScoreManager.Instance.SetScore(scoreAtCheckpoint);
        TimerManager.Instance.SetRemainingTime(timeAtCheckpoint);

        if (respawnRoom != null)
        {
            // Move camera to the respawn room
            if (groundManager != null)
            {
                groundManager.OnPlayerRespawn();
            }
            Camera.main.GetComponent<CameraController>().MoveToNewRoom(respawnRoom);

            // Activate and deactivate the appropriate rooms
            if (respawnRoom != currentRoom)
            {
                if (currentRoom != null) currentRoom.GetComponent<Room>().ActivateRoom(false);
                respawnRoom.GetComponent<Room>().ActivateRoom(true);
                currentRoom = respawnRoom; // Update the current room
            }
        }
        else
        {
            Camera.main.GetComponent<CameraController>().MoveToNewRoom(initialRoom);
            // Log a message for the initial checkpoint case
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
            SoundManager.instance.PlaySound(checkpointSound);
            scoreAtCheckpoint = ScoreManager.Instance.GetScore();
            timeAtCheckpoint = TimerManager.Instance.GetRemainingTime();
            Transform respawnRoom = currentCheckpoint.parent;
            var roomComponent = respawnRoom.GetComponent<Room>();
            roomComponent.RemoveCollectedDiamonds();
            collision.GetComponent<Collider2D>().enabled = false;
            collision.GetComponent<Animator>().SetTrigger("appear");
        }
    }
}

}
