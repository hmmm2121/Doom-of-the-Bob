using UnityEngine;

// Slam shockwave VFX: a radial debris particle burst. All look/feel values are
// exposed for the Inspector so it can be retuned without code. PlayerMovement sets
// `radius` from its slam AOE; with scaleSpeedToRadius the debris reach the AOE edge.
public class SlamImpactEffect : MonoBehaviour
{
    [Header("Slam Area")]
    public Color color = new Color(1f, 0.2f, 0.15f, 1f);
    [Range(0f, 3f)] public float intensity = 1f;   // alpha / brightness multiplier
    public float radius = 2.5f;                     // overridden by PlayerMovement to match slam AOE
    public bool scaleSpeedToRadius = true;          // debris reach the AOE edge

    [Header("Particles")]
    public int particleCount = 50;
    public float particleSpeed = 9f;                // used when scaleSpeedToRadius is false
    public float particleSize = 0.25f;
    public float particleLifetime = 0.5f;

    ParticleSystem ps;

    void Awake()
    {
        ps = GetComponentInChildren<ParticleSystem>();
    }

    void Start()
    {
        if (ps != null)
        {
            var main = ps.main;
            main.startLifetime = particleLifetime;
            main.startSize = particleSize;
            main.startSpeed = scaleSpeedToRadius
                ? radius / Mathf.Max(0.05f, particleLifetime)
                : particleSpeed;
            Color pc = color; pc.a *= Mathf.Clamp01(intensity);
            main.startColor = pc;
            if (particleCount > main.maxParticles) main.maxParticles = particleCount;

            var em = ps.emission;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)particleCount) });

            ps.Play();
        }

        Destroy(gameObject, particleLifetime + 0.5f);
    }
}
