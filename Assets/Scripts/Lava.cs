namespace Sponge
{
    using UnityEngine;

    public class Lava : MonoBehaviour
    {
        public float lavaDamage = 9999f;

        private void OnTriggerEnter(Collider other)
        {
            TryKill(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryKill(collision.collider);
        }

        private void TryKill(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null && !dmg.IsDead)
            {
                dmg.TakeDamage(lavaDamage, other.transform.position, Vector3.up);
            }
        }
    }

}
