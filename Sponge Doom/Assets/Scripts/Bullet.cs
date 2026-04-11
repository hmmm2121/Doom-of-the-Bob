using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            print("Hit" + collision.gameObject.name +"!");
            Destroy(collision.gameObject);
        }
    }
}
