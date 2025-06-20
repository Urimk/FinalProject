
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public class Room : MonoBehaviour
{
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private GameObject[] Traps;
    [SerializeField] private GameObject[] Collectables;
    [SerializeField] private CameraController cam;
    [SerializeField] private PlayerMovement pm;
    [SerializeField] private PlayerRespawn pr;
    private Vector3[] initialEnemyPositions;
    private Quaternion[] initialEnemyRotations;
    private Vector3[] initialTrapPositions;
    private Vector3[] initialCollectablePositions;


    [Header("Room Settings")]
    public bool freezeCamX = false;
    public bool freezeCamY = true;
    public float xFreezeValue;
    public float yFreezeValue;
    public float yOffsetValue = 2f;
    public float gravScaleValue = 2f;
    public float maxFallSpeedValue = 100;
    public bool isChase = false;
    public float chaseSpeed = 5f;
    public float chaseStartOffSet = 0;
    public bool followPlayerY = false;

    public void EnterRoom()
    {
        // 1) Camera
        cam.MoveToNewRoom(transform);
        cam.SetCameraXFreeze(freezeCamX, xFreezeValue);
        cam.SetChaseMode(isChase);
        cam.SetChaseSpeed(chaseSpeed);
        cam.SetChaseStart(chaseStartOffSet);

        // 2) Y-offset
        if (freezeCamY)
        {
            cam.SetCameraYFreeze(yFreezeValue);
        }
        else
        {
            cam.SetFollowPlayerY(yOffsetValue);
        }



        // 3) Gravity
        pm.normalGrav = gravScaleValue;
        pm.maxFallSpeed = maxFallSpeedValue;

        // 4) Activate rooms
        pr.SetCurrentRoom(transform);
    }

    private void Awake()
    {
        // Enemies
        initialEnemyPositions = new Vector3[enemies.Length];
        initialEnemyRotations = new Quaternion[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                initialEnemyPositions[i] = enemies[i].transform.position;
                initialEnemyRotations[i] = enemies[i].transform.rotation;

        }

        // Traps
        initialTrapPositions = new Vector3[Traps.Length];
        for (int i = 0; i < Traps.Length; i++)
        {
            if (Traps[i] != null)
                initialTrapPositions[i] = Traps[i].transform.position;
        }

        // Collectables
        initialCollectablePositions = new Vector3[Collectables.Length];
        for (int i = 0; i < Collectables.Length; i++)
        {
            if (Collectables[i] != null)
                initialCollectablePositions[i] = Collectables[i].transform.position;
        }
    }

    public void ActivateRoom(bool _status)
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].GetComponent<Health>().getHealth() != 0f)
            {
                enemies[i].SetActive(_status);

                if (_status)
                {
                    // Reset enemy position when activating the room
                    enemies[i].transform.position = initialEnemyPositions[i];
                    enemies[i].transform.rotation = initialEnemyRotations[i];

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

    public void ResetRoom()
    {
        // Reset enemies
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].SetActive(true);
                enemies[i].GetComponent<Health>().reEnableComponents();
                enemies[i].GetComponent<Health>().ResetHealth();
                enemies[i].transform.position = initialEnemyPositions[i];
                enemies[i].transform.rotation = initialEnemyRotations[i];
            }
        }

        // Reset traps
        for (int i = 0; i < Traps.Length; i++)
        {
            if (Traps[i] != null)
            {
                Traps[i].SetActive(true);
                Traps[i].transform.position = initialTrapPositions[i];
            }
        }

        // Reset collectables
        for (int i = 0; i < Collectables.Length; i++)
        {
            if (Collectables[i] != null)
            {
                Collectables[i].SetActive(true);
                Collectables[i].transform.position = initialCollectablePositions[i];
            }
        }
    }

    public void RemoveCollectedDiamonds()
    {
        List<GameObject> activeCollectables = new List<GameObject>();
        List<Vector3> activePositions = new List<Vector3>();

        for (int i = 0; i < Collectables.Length; i++)
        {
            if (Collectables[i] != null && Collectables[i].activeInHierarchy)
            {
                activeCollectables.Add(Collectables[i]);
                activePositions.Add(initialCollectablePositions[i]);
            }
        }

        Collectables = activeCollectables.ToArray();
        initialCollectablePositions = activePositions.ToArray();
}


}