using UnityEngine;

public class PencilProjectile : MonoBehaviour
{
    public Vector3 moveDirection;
    public float speed = 15f;

    void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }
}