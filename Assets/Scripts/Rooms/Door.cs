using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform prevRoom;
    [SerializeField] private Transform nextRoom;
    [SerializeField] private bool isXDoor = false;

    [SerializeField] private PlayerRespawn playerRespawn;


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        bool toNext = isXDoor
            ? collision.transform.position.x > transform.position.x
            : collision.transform.position.y < transform.position.y;

        Transform fromRoom = toNext ? prevRoom : nextRoom;
        Transform intoRoom = toNext ? nextRoom : prevRoom;

        fromRoom.GetComponent<Room>().ActivateRoom(false);
        intoRoom.GetComponent<Room>().ActivateRoom(true);

        playerRespawn.SetCurrentRoom(intoRoom);
        intoRoom.GetComponent<Room>().EnterRoom();
    }
}
