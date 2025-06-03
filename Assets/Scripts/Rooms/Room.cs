
using UnityEngine;
public class Room : MonoBehaviour
{
    [SerializeField] private GameObject[] enemies;
    private Vector3[] initialPositions;
    
    private void Awake() {
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


