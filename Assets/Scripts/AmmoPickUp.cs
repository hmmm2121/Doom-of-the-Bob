using UnityEngine;

public class AmmoPackPickup : MonoBehaviour
{
    public int bulletAmount = 8;
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
            //Weapon weapon = other.GetComponent<Weapon>();

            /*if (weapon != null)
            {
                hasBeenPickedUp = true;
                weapon.currentReserveAmmo += bulletAmount;
                Destroy(gameObject);
            }*/
        }
    }
}