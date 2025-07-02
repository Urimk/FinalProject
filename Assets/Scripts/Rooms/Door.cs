using UnityEngine;

/// <summary>
/// Handles room transitions when the player exits through a door.
/// </summary>
public class Door : MonoBehaviour
{
    // === Constants ===
    private const float HorizontalDoorYOffset = 1.7f;
    private const string PlayerTag = "Player";

    // === Serialized Fields ===
    [SerializeField] private Transform _previousRoom;
    [SerializeField] private Transform _nextRoom;
    [SerializeField] private bool _isHorizontalDoor = false;
    [SerializeField] private PlayerRespawn _playerRespawn;
    [SerializeField] private bool _isOneWay = false;

    /// <summary>
    /// Handles player exiting the door and triggers room transitions.
    /// </summary>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(PlayerTag)) return;
        bool toNext = _isHorizontalDoor
            ? collision.transform.position.y + HorizontalDoorYOffset > transform.position.y
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
