using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform prevRoom;
    [SerializeField] private Transform nextRoom;
    [SerializeField] private bool isHorizontalDoor = false;

    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private bool isOneWay = false;


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        bool toNext = isHorizontalDoor
            ? collision.transform.position.y + 1.7f > transform.position.y
            : collision.transform.position.x > transform.position.x;

        if (!toNext && isOneWay)
        {
            return;
        }

        Transform fromRoom = toNext ? prevRoom : nextRoom;
        Transform intoRoom = toNext ? nextRoom : prevRoom;

        fromRoom.GetComponent<Room>().ActivateRoom(false);
        intoRoom.GetComponent<Room>().ActivateRoom(true);

        playerRespawn.SetCurrentRoom(intoRoom);
        intoRoom.GetComponent<Room>().EnterRoom();
    }
}
