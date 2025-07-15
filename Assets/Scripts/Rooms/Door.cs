using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles room transitions when the player exits through a door.
/// </summary>
public class Door : MonoBehaviour
{
    // ==================== Constants ====================
    private const float HorizontalDoorYOffset = 1.7f;
    private const string PlayerTag = "Player";

    // ==================== Inspector Fields ====================
    [Tooltip("Reference to the previous room Transform.")]
    [FormerlySerializedAs("previousRoom")]
    [SerializeField] private Transform _previousRoom;
    [Tooltip("Reference to the next room Transform.")]
    [FormerlySerializedAs("nextRoom")]
    [SerializeField] private Transform _nextRoom;
    [Tooltip("True if this door is horizontal (vertical otherwise).")]
    [FormerlySerializedAs("isHorizontalDoor")]
    [SerializeField] private bool _isHorizontalDoor = false;
    [Tooltip("Reference to the PlayerRespawn component.")]
    [FormerlySerializedAs("playerRespawn")]
    [SerializeField] private PlayerRespawn _playerRespawn;
    [Tooltip("True if this door is one-way only.")]
    [FormerlySerializedAs("isOneWay")]
    [SerializeField] private bool _isOneWay = false;

    // ==================== Room Transition Logic ====================
    /// <summary>
    /// Handles player exiting the door and triggers room transitions.
    /// </summary>
    /// <param name="collision">The collider that exited the trigger.</param>
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
