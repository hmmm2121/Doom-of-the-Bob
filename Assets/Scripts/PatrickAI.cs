using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class PatrickAI : MonoBehaviour, IDamageable
{
    public enum Phase { Dormant, Ranged, JumpingDown, Melee, Dead }

    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Detection")]
    public float eyeHeight = 1.4f;
    public float playerHeightOffset = 0.9f;

    [Header("Ranged (perch)")]
    public Transform perchPoint;
    public Transform landingPoint;
    public GameObject burgerPrefab;
    public Transform throwOrigin;
    public float throwCooldown = 2.5f;
    public float throwSpeed = 18f;
    public float throwArcBoost = 1f;
    public float throwReleaseDelay = 0.35f;
    public float jumpDownRange = 8f;

    [Header("Jump down")]
    public float jumpDuration = 1.2f;
    public float jumpArcHeight = 4f;
    public float standDelay = 0.5f;

    [Header("Melee")]
    public float chaseSpeed = 3.5f;
    public float meleeRange = 2.5f;
    public float meleeDamage = 18f;
    public float meleeCooldown = 1.8f;

    [Header("Health")]
    public float maxHealth = 250f;
    public float currentHealth;
    [Range(0.05f, 1f)] public float damageTakenMultiplier = 0.5f;

    public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

    [Header("Hit / Death")]
    public float hitStunDuration = 0.3f;
    public float deathDespawnDelay = 3f;

    [Header("Victory / Next Level")]
    public string nextSceneName = "";   // empty -> loads next build index
    public float winLoadDelay = 4f;

    [Header("Animation")]
    public float animBaseRunSpeed = 1.5f;
    public float animSpeedMin = 0.8f;
    public float animSpeedMax = 2.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxThrow;
    public AudioClip sfxHit;
    public AudioClip sfxDeath;
    public AudioClip sfxMelee;

    private Animator _animator;
    private NavMeshAgent _agent;
    private HitFlash _hitFlash;
    private Collider[] _colliders;

    private Phase _phase = Phase.Dormant;
    private float _hitStunTimer;
    private float _throwCdTimer;
    private float _spawnTimer;
    private bool _spawnPending;
    private float _meleeCdTimer;
    private bool _isThrowing;

    public bool IsDead => _phase == Phase.Dead;
    public Phase CurrentPhase => _phase;

    static readonly int P_Speed = Animator.StringToHash("Speed");
    static readonly int P_Throw = Animator.StringToHash("Throw");
    static readonly int P_Hit = Animator.StringToHash("Hit");
    static readonly int P_Die = Animator.StringToHash("Die");
    static readonly int P_JumpDown = Animator.StringToHash("JumpDown");
    static readonly int P_Fighting = Animator.StringToHash("Fighting");

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _hitFlash = GetComponent<HitFlash>();
        _colliders = GetComponentsInChildren<Collider>();
        currentHealth = maxHealth;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag(playerTag);
            if (p == null) p = GameObject.Find("SpongeBob");
            if (p != null) player = p.transform;
        }
        _agent.speed = chaseSpeed;
        _agent.stoppingDistance = 0f;

        if (perchPoint != null) transform.position = perchPoint.position;
        _agent.enabled = false;
    }

    public void BeginFight()
    {
        if (_phase == Phase.Dormant)
        {
            _phase = Phase.Ranged;
            _animator.SetBool(P_Fighting, true);
        }
    }

    void Update()
    {
        if (_phase == Phase.Dead) return;
        if (_hitStunTimer > 0f) { _hitStunTimer -= Time.deltaTime; UpdateAnimatorSpeed(); return; }
        if (_throwCdTimer > 0f) _throwCdTimer -= Time.deltaTime;
        if (_meleeCdTimer > 0f) _meleeCdTimer -= Time.deltaTime;
        if (_spawnPending)
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f) { _spawnPending = false; _isThrowing = false; SpawnBurger(); }
        }

        switch (_phase)
        {
            case Phase.Dormant: FacePlayer(2f); break;
            case Phase.Ranged: TickRanged(); break;
            case Phase.JumpingDown: break;
            case Phase.Melee: TickMelee(); break;
        }
        UpdateAnimatorSpeed();
    }

    void TickRanged()
    {
        if (player == null) return;
        FacePlayer(6f);

        if (!_isThrowing && _throwCdTimer <= 0f)
        {
            _isThrowing = true;
            _throwCdTimer = throwCooldown;
            _spawnPending = true;
            _spawnTimer = throwReleaseDelay;
            _animator.ResetTrigger(P_Throw);
            _animator.SetTrigger(P_Throw);
        }

        Vector3 land = landingPoint != null ? landingPoint.position : transform.position;
        Vector3 toPlayer = player.position - land; toPlayer.y = 0f;
        if (toPlayer.magnitude <= jumpDownRange)
        {
            StartCoroutine(JumpDownRoutine());
        }
    }

    public void SpawnBurger()
    {
        if (burgerPrefab == null || player == null) return;
        Vector3 hand = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * playerHeightOffset;
        Vector3 flat = (target - hand); flat.y = 0f;
        Vector3 dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : transform.forward;
        Vector3 origin = hand + dir * 1.0f + Vector3.up * 0.2f;

        Vector3 vel = SolveArc(origin, target, throwSpeed);

        GameObject burger = Instantiate(burgerPrefab, origin, Quaternion.LookRotation(dir));
        var proj = burger.GetComponent<BurgerProjectile>();
        if (proj != null) proj.owner = gameObject;
        var rb = burger.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = vel;
        var bcol = burger.GetComponent<Collider>();
        if (bcol != null)
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) Physics.IgnoreCollision(bcol, _colliders[i], true);

        if (sfxThrow != null) PlaySfx(sfxThrow);
    }

    Vector3 SolveArc(Vector3 from, Vector3 to, float horizSpeed)
    {
        Vector3 d = to - from;
        Vector3 dXZ = new Vector3(d.x, 0f, d.z);
        float dist = dXZ.magnitude;
        float g = -Physics.gravity.y;
        float t = dist / Mathf.Max(0.01f, horizSpeed);
        Vector3 vXZ = dist > 0.001f ? dXZ / t : Vector3.zero;
        float vY = d.y / t + 0.5f * g * t + throwArcBoost;
        return vXZ + Vector3.up * vY;
    }

    public void EndThrow() { _isThrowing = false; }

    IEnumerator JumpDownRoutine()
    {
        _phase = Phase.JumpingDown;
        _isThrowing = false;
        _animator.ResetTrigger(P_JumpDown);
        _animator.SetTrigger(P_JumpDown);

        Vector3 from = transform.position;
        Vector3 to = landingPoint != null ? landingPoint.position : from;
        if (NavMesh.SamplePosition(to, out var navHit, 5f, NavMesh.AllAreas)) to = navHit.position;

        float t = 0f;
        while (t < jumpDuration)
        {
            float u = t / jumpDuration;
            Vector3 p = Vector3.Lerp(from, to, u);
            p.y += jumpArcHeight * Mathf.Sin(u * Mathf.PI);
            transform.position = p;
            FacePlayer(8f);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = to;
        OnJumpLanded();
    }

    public void OnJumpLanded()
    {
        _agent.enabled = true;
        if (NavMesh.SamplePosition(transform.position, out var navHit, 5f, NavMesh.AllAreas))
            _agent.Warp(navHit.position);
        _agent.isStopped = true;
        StartCoroutine(StandThenChase());
    }

    IEnumerator StandThenChase()
    {
        // stay in JumpingDown phase so Speed stays ~0 and Idle plays (settle on feet)
        yield return new WaitForSeconds(standDelay);
        if (_phase == Phase.Dead) yield break;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        _phase = Phase.Melee;
    }

    void TickMelee()
    {
        if (player == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.SetDestination(player.position);
        FacePlayer(8f);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= meleeRange && _meleeCdTimer <= 0f)
        {
            _meleeCdTimer = meleeCooldown;
            DealMeleeDamage();
        }
    }

    void DealMeleeDamage()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > meleeRange * 1.4f) return;
        var dmg = player.GetComponent<IDamageable>();
        if (dmg != null && !dmg.IsDead)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dmg.TakeDamage(meleeDamage, player.position, dir);
        }
        if (sfxMelee != null) PlaySfx(sfxMelee);
    }

    public void MeleeHit() { }
    public void EndMelee() { }

    void FacePlayer(float turnSpeed)
    {
        if (player == null) return;
        Vector3 to = player.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude < 0.001f) return;
        Quaternion want = Quaternion.LookRotation(to);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, turnSpeed * Time.deltaTime);
    }

    void UpdateAnimatorSpeed()
    {
        Vector3 v = (_agent != null && _agent.enabled && _agent.isOnNavMesh) ? _agent.velocity : Vector3.zero;
        float horiz = new Vector2(v.x, v.z).magnitude;
        _animator.SetFloat(P_Speed, horiz, 0.1f, Time.deltaTime);
        float playback = horiz > 0.05f
            ? Mathf.Clamp(horiz / Mathf.Max(0.01f, animBaseRunSpeed), animSpeedMin, animSpeedMax)
            : 1f;
        _animator.speed = playback;
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_phase == Phase.Dead) return;
        currentHealth -= amount * damageTakenMultiplier;
        if (_hitFlash != null) _hitFlash.Flash();
        if (currentHealth <= 0f) { Die(); return; }

        if (_phase != Phase.JumpingDown)
        {
            _hitStunTimer = hitStunDuration;
            _isThrowing = false;
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
            _animator.ResetTrigger(P_Hit);
            _animator.SetTrigger(P_Hit);
        }
        PlaySfx(sfxHit);
    }

    void Die()
    {
        _phase = Phase.Dead;
        if (_agent.enabled) { _agent.isStopped = true; _agent.enabled = false; }
        for (int i = 0; i < _colliders.Length; i++) if (_colliders[i] != null) _colliders[i].enabled = false;
        _animator.speed = 1f;
        _animator.ResetTrigger(P_Hit);
        _animator.ResetTrigger(P_Throw);
        _animator.ResetTrigger(P_JumpDown);
        _animator.ResetTrigger(P_Die);
        _animator.SetTrigger(P_Die);
        PlaySfx(sfxDeath);
        Invoke(nameof(LoadNextLevel), winLoadDelay);
    }

    void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1);
    }

    void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 land = landingPoint != null ? landingPoint.position : transform.position;
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(land, jumpDownRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
#endif
}
