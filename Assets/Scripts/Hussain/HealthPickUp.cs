namespace Official
{
    using UnityEngine;

    public class HealthPackPickup : MonoBehaviour
    {
        public float healthAmount = 30f;
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
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

                if (playerHealth != null && !playerHealth.IsDead)
                {
                    hasBeenPickedUp = true;
                    playerHealth.Heal(healthAmount);
                    Destroy(gameObject);
                }
            }
        }
    }
}
