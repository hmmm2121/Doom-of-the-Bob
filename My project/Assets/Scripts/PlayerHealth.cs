using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Invulnerability")]
    public float iFrames = 0.4f;

    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    private float _iTimer;
    private bool _isDead;

    public bool IsDead => _isDead;
    public float HealthFraction => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (_iTimer > 0f) _iTimer -= Time.deltaTime;
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_isDead || _iTimer > 0f || amount <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        _iTimer = iFrames;
        onDamaged?.Invoke();
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        _isDead = true;
        onDeath?.Invoke();
    }
}