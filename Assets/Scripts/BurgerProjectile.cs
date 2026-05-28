using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BurgerProjectile : MonoBehaviour
{
    public float damage = 15f;
    public float lifetime = 6f;
    public string playerTag = "Player";
    public GameObject hitVfx;
    public AudioClip hitSfx;

    [HideInInspector] public GameObject owner;
    public float armingDelay = 0.1f;

    private float _armTime;
    private Collider _col;
    private Rigidbody _rb;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
        if (_col != null) _col.isTrigger = true;
    }

    void Start()
    {
        _armTime = Time.time + armingDelay;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time < _armTime) return;
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        var dmg = other.GetComponentInParent<IDamageable>();
        bool isOwner = owner != null && dmg != null && (dmg as Component) != null && ((Component)dmg).transform.IsChildOf(owner.transform);
        if (dmg == null || dmg.IsDead || isOwner) return;

        Vector3 hitDir = (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.01f)
            ? _rb.linearVelocity.normalized
            : transform.forward;
        dmg.TakeDamage(damage, transform.position, hitDir);
        if (hitVfx != null) Instantiate(hitVfx, transform.position, Quaternion.identity);
        if (hitSfx != null) AudioSource.PlayClipAtPoint(hitSfx, transform.position);
        Destroy(gameObject);
    }
}
