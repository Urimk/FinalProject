using UnityEngine;

public class FireballHolder : MonoBehaviour
{
    [SerializeField] private Transform _enemy;

    private void Update()
    {
        transform.localScale = _enemy.localScale;
    }
}
