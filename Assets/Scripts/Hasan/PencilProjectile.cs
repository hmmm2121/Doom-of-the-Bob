namespace Official
{
    using UnityEngine;

    public class PencilProjectile : MonoBehaviour
    {
        public Vector3 moveDirection;
        public float speed = 15f;
        public float damage = 10f;

        void Update()
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }

        void OnTriggerEnter(Collider other)
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) return;

            Vector3 dir = moveDirection.normalized;
            damageable.TakeDamage(damage, transform.position, dir);
            Destroy(gameObject);
        }
    }

}
