using UnityEngine;

public class TargetHealth : MonoBehaviour
{
    public float health = 100f;
    public bool isCriticalHit = false; // tick this on the HEAD collider only
    public TargetHealth mainBody;       // on the head, point this to the main body

    public void TakeDamage(float damage)
    {
        // If this is a critical hit, forward damage to the main body
        if (isCriticalHit && mainBody != null)
        {
            mainBody.TakeDamage(damage);
            return;
        }

        health -= damage;
        Debug.Log(gameObject.name + " health: " + health);
        if (health <= 0f)
        {
            Destroy(gameObject);
        }
    }
}