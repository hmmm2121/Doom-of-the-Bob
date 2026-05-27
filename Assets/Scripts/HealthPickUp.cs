using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public int healthAmount = 30;
    private bool hasBeenPickedUp = false;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenPickedUp) return;

        if (other.CompareTag("Player"))
        {
            hasBeenPickedUp = true;

            //Insert Health class reference here
            Destroy(gameObject);
        }
    }
}