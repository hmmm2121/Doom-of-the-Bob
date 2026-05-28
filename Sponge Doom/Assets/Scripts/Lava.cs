using UnityEngine;

public class Lava : MonoBehaviour
{
    [Tooltip("Damage applied per touch. Set high enough to insta-kill so DeathReset fires.")]
    public float lavaDamage = 9999f;

    private void OnTriggerEnter(Collider other)
    {
        TryKill(other);
    }

    // Belt-and-braces: also handle non-trigger overlaps. Lava commonly has a
    // non-trigger MeshCollider for traversal physics and a trigger BoxCollider on top.
    private void OnCollisionEnter(Collision collision)
    {
        TryKill(collision.collider);
    }

    private void TryKill(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        // Hunt up the parent chain so a child capsule routes damage to the PlayerObj.
        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null && !dmg.IsDead)
        {
            dmg.TakeDamage(lavaDamage, other.transform.position, Vector3.up);
        }
    }
}
