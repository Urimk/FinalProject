using UnityEngine;

public class SwordPickup : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f; // degrees per second

    private void Update()
    {
        // Constant rotation around the Y axis
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {   
        float distance = Vector2.Distance(transform.position, collision.transform.position);
        if (collision.CompareTag("Player"))
        {
            PlayerAttack playerAttack = collision.GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                playerAttack.HasSword = true; // Call the equip method
                playerAttack.EquipWeapon();
                gameObject.SetActive(false);
            }
        }
    }
}
