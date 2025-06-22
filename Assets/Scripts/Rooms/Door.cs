using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform _previousRoom;
    [SerializeField] private Transform _nextRoom;
    [SerializeField] private bool _isHorizontalDoor = false;

    [SerializeField] private PlayerRespawn _playerRespawn;
    [SerializeField] private bool _isOneWay = false;


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        bool toNext = _isHorizontalDoor
            ? collision.transform.position.y + 1.7f > transform.position.y
            : collision.transform.position.x > transform.position.x;

        if (!toNext && _isOneWay)
        {
            return;
        }

        Transform fromRoom = toNext ? _previousRoom : _nextRoom;
        Transform intoRoom = toNext ? _nextRoom : _previousRoom;

        fromRoom.GetComponent<Room>().ActivateRoom(false);
        intoRoom.GetComponent<Room>().ActivateRoom(true);

        _playerRespawn.SetCurrentRoom(intoRoom);
        intoRoom.GetComponent<Room>().EnterRoom();
    }
}
