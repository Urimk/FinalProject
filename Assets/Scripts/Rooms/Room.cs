
using UnityEngine;
public class Room : MonoBehaviour
{
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private CameraController cam;
    [SerializeField] private PlayerMovement pm;
    [SerializeField] private PlayerRespawn pr;
    private Vector3[] initialPositions;

    [Header("Room Settings")]
    public bool freezeCamX = false;
    public float xFreezeValue;
    public float yOffsetValue = 2f;
    public float gravScaleValue = 2f;
    public float maxFallSpeedValue = 100;
    public bool isChase = false;
    public float chaseSpeed = 5f;
    public bool followPlayerY = false;

    public void EnterRoom()
    {
        // 1) Camera
        cam.MoveToNewRoom(transform);
        cam.SetCameraXFreeze(freezeCamX, xFreezeValue);
        cam.SetChaseMode(isChase);
        cam.SetChaseSpeed(chaseSpeed);

        // 2) Y-offset
        cam.SetFollowPlayerY(followPlayerY, yOffsetValue);


        // 3) Gravity
        pm.normalGrav = gravScaleValue;
        pm.maxFallSpeed = maxFallSpeedValue;

        // 4) Activate rooms
        pr.SetCurrentRoom(transform);
    }
    
    private void Awake()
    {
        initialPositions = new Vector3[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                initialPositions[i] = enemies[i].transform.position;
            }

        }
    }
    public void ActivateRoom(bool _status)
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].SetActive(_status);

                if (_status)
                {
                    // Reset enemy position when activating the room
                    enemies[i].transform.position = initialPositions[i];
                }
                else
                {
                    // Deactivate all projectiles associated with the enemy
                    EnemyProjectile[] projectiles = enemies[i].GetComponentsInChildren<EnemyProjectile>(true);
                    foreach (var projectile in projectiles)
                    {
                        projectile.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}


