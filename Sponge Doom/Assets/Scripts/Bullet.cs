using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Damage Settings")]
    public float maxDamage = 20f;
    public float minDamage = 5f;

    [Header("Range Settings")]
    public float maxLifetime = 0.5f;
    public float maxRange = 15f;

    private Vector3 startPosition;

    private void Start()
    {
        // Store the initial position of the bullet to calculate distance traveled later.
        startPosition = transform.position;

        // order the bullet to be destroyed after maxLifetime seconds.
        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        // Calculate the distance the bullet has traveled from its starting position and if it exceeds maxRange, destroy the bullet.
        float distanceTravelled = Vector3.Distance(startPosition, transform.position);

        if (distanceTravelled >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    // Delas with the target hit logic.
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has the "Target" tag. If not, ignore the collision.
        if (!collision.gameObject.CompareTag("Target"))
        {
            return;
        }

        // Calculate the distance traveled by the bullet and determine the damage based on that distance.
        float distanceTravelled = Vector3.Distance(startPosition, transform.position);
        float t = Mathf.Clamp01(distanceTravelled / maxRange);
        float currentDamage = Mathf.Lerp(maxDamage, minDamage, t);

        Debug.Log("Hit " + collision.gameObject.name + " for " + currentDamage + " damage");

        TargetHealth target = collision.gameObject.GetComponent<TargetHealth>();
        if (target != null)
        {
            target.TakeDamage(currentDamage);
        }

        Destroy(gameObject);
    }
}