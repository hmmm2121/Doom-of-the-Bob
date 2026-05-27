using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth health;
    public Image damageVignette;
    public Image healthFill;
    public string playerTag = "Player";

    [Header("Damage vignette")]
    public Color vignetteColor = new Color(0.6f, 0f, 0f, 1f);
    [Tooltip("Alpha added by a single hit.")]
    public float flashOnHit = 0.6f;
    [Tooltip("Alpha units faded per second after a hit.")]
    public float flashDecay = 2f;

    [Header("Low-health sustain")]
    [Tooltip("Below this health fraction the screen stays tinted red.")]
    public float lowHealthThreshold = 0.5f;
    public float maxLowHealthAlpha = 0.55f;
    [Tooltip("Heartbeat pulse magnitude added when near death.")]
    public float pulseAmplitude = 0.12f;
    public float pulseSpeed = 4f;

    [Header("Health bar")]
    public Color fullHealthColor = new Color(0.2f, 0.85f, 0.25f, 1f);
    public Color lowHealthColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    float _flash;
    bool _dead;

    void OnEnable()
    {
        if (health == null)
        {
            var p = GameObject.FindWithTag(playerTag);
            if (p != null) health = p.GetComponent<PlayerHealth>();
        }
        if (health == null) return;

        health.onDamaged.AddListener(OnDamaged);
        health.onHealthChanged.AddListener(OnHealthChanged);
        health.onDeath.AddListener(OnDeath);

        if (damageVignette != null) SetVignetteAlpha(0f);
    }

    // Init bar in Start so PlayerHealth.Awake has already set currentHealth = maxHealth.
    void Start()
    {
        if (health != null) OnHealthChanged(health.currentHealth, health.maxHealth);
    }

    void OnDisable()
    {
        if (health == null) return;
        health.onDamaged.RemoveListener(OnDamaged);
        health.onHealthChanged.RemoveListener(OnHealthChanged);
        health.onDeath.RemoveListener(OnDeath);
    }

    void OnDamaged() => _flash = Mathf.Max(_flash, flashOnHit);

    void OnHealthChanged(float current, float max)
    {
        if (healthFill == null) return;
        float frac = max > 0f ? current / max : 0f;
        healthFill.fillAmount = frac;
        healthFill.color = Color.Lerp(lowHealthColor, fullHealthColor, frac);
    }

    void OnDeath()
    {
        _dead = true;
        if (damageVignette != null) SetVignetteAlpha(1f);
    }

    void Update()
    {
        if (damageVignette == null || health == null) return;
        if (_dead) { SetVignetteAlpha(1f); return; }

        _flash = Mathf.MoveTowards(_flash, 0f, flashDecay * Time.deltaTime);

        float frac = health.HealthFraction;
        float lowTint = 0f;
        if (frac < lowHealthThreshold)
        {
            float t = Mathf.InverseLerp(lowHealthThreshold, 0f, frac); // 0..1 as HP drops
            lowTint = t * maxLowHealthAlpha;
            lowTint += Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * pulseAmplitude * t;
        }

        SetVignetteAlpha(Mathf.Clamp01(Mathf.Max(_flash, lowTint)));
    }

    void SetVignetteAlpha(float a)
    {
        var c = vignetteColor;
        c.a = a;
        damageVignette.color = c;
    }
}
