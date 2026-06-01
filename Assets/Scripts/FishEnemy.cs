using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class FishEnemy : MonoBehaviour, IDamageable
{
    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Detection")]
    public float detectRange = 15f;
    public float contactRange = 1.2f;
    public LayerMask sightObstacleMask = ~0;
    public float eyeHeight = 1.1f;
    public float playerHeightOffset = 0.9f;
    public float loseSightGrace = 1.5f;

    [Header("Speeds")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 4f;

    [Header("Patrol")]
    public bool patrolWhenIdle = true;
    public float patrolRadius = 6f;
    public float patrolWaitMin = 1.5f;
    public float patrolWaitMax = 4f;

    [Header("Health")]
    public float maxHealth = 1f;
    public float currentHealth;

    [Header("Contact damage")]
    public float contactDamage = 10f;
    public float contactDamageCooldown = 1.0f;

    [Header("Hit / Death")]
    public float hitStunDuration = 0.35f;
    public float deathDespawnDelay = 2.5f;

    [Header("Repath")]
    public float repathInterval = 0.2f;

    [Header("Animation")]
    public float animBaseRunSpeed = 1.5f;
    public float animSpeedMin = 0.8f;
    public float animSpeedMax = 2.5f;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip sfxDetect;
    public AudioClip sfxAttack;
    public AudioClip sfxHit;
    public AudioClip sfxDeath;

    private Animator _animator;
    private NavMeshAgent _agent;
    private HitFlash _hitFlash;
    private float _repathTimer;
    private float _hitStunTimer;
    private float _damageCdTimer;
    private float _lostSightTimer;
    private float _patrolWaitTimer;
    private Vector3 _patrolHome;
    private bool _isDead;
    private bool _hasSeenPlayer;

    public bool IsDead => _isDead;

    private enum State { Idle, Patrol, Chase, Hit, Dead }
    private State _state = State.Idle;

    static readonly int P_Speed = Animator.StringToHash("Speed");
    static readonly int P_Hit = Animator.StringToHash("Hit");
    static readonly int P_Die = Animator.StringToHash("Die");

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _hitFlash = GetComponent<HitFlash>();
        currentHealth = maxHealth;
        _patrolHome = transform.position;
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

        if (player != null)
        {
            var myCol = GetComponent<Collider>();
            var playerCol = player.GetComponent<Collider>();
            if (myCol != null && playerCol != null) Physics.IgnoreCollision(myCol, playerCol, true);
        }
    }

    void Update()
    {
        if (_isDead) return;

        if (_hitStunTimer > 0f) { _hitStunTimer -= Time.deltaTime; UpdateAnimatorSpeed(); return; }
        if (_damageCdTimer > 0f) _damageCdTimer -= Time.deltaTime;

        bool seesPlayer = CanSeePlayer(out float distToPlayer);

        if (seesPlayer)
        {
            _lostSightTimer = 0f;
            if (!_hasSeenPlayer) { _hasSeenPlayer = true; PlaySfx(sfxDetect); }
        }
        else if (_hasSeenPlayer)
        {
            _lostSightTimer += Time.deltaTime;
            if (_lostSightTimer > loseSightGrace) _hasSeenPlayer = false;
        }

        State next = DecideState();
        if (next != _state) EnterState(next);

        TickState(distToPlayer);
        UpdateAnimatorSpeed();
    }

    State DecideState()
    {
        if (player == null) return patrolWhenIdle ? State.Patrol : State.Idle;
        if (_hasSeenPlayer) return State.Chase;
        return patrolWhenIdle ? State.Patrol : State.Idle;
    }

    void EnterState(State s)
    {
        _state = s;
        switch (s)
        {
            case State.Idle:
                _agent.isStopped = true;
                break;
            case State.Patrol:
                _agent.isStopped = false;
                _agent.speed = patrolSpeed;
                PickPatrolPoint();
                break;
            case State.Chase:
                _agent.isStopped = false;
                _agent.speed = chaseSpeed;
                _agent.stoppingDistance = 0.4f;
                break;
        }
    }

    void TickState(float distToPlayer)
    {
        _repathTimer -= Time.deltaTime;
        switch (_state)
        {
            case State.Chase:
                if (_repathTimer <= 0f) { _repathTimer = repathInterval; _agent.SetDestination(player.position); }
                if (distToPlayer <= contactRange && _damageCdTimer <= 0f) ApplyContactDamage();
                break;

            case State.Patrol:
                if (!_agent.pathPending && _agent.remainingDistance < 0.5f) {
                    _patrolWaitTimer -= Time.deltaTime;
                    _agent.isStopped = true;
                    if (_patrolWaitTimer <= 0f) { _agent.isStopped = false; PickPatrolPoint(); }
                }
                break;
        }
    }

    void UpdateAnimatorSpeed()
    {
        Vector3 v = _agent != null ? _agent.velocity : Vector3.zero;
        float horiz = new Vector2(v.x, v.z).magnitude;
        _animator.SetFloat(P_Speed, horiz, 0.1f, Time.deltaTime);

        float playback = horiz > 0.05f
            ? Mathf.Clamp(horiz / Mathf.Max(0.01f, animBaseRunSpeed), animSpeedMin, animSpeedMax)
            : 1f;
        _animator.speed = playback;
    }

    void ApplyContactDamage()
    {
        if (player == null) return;
        var dmg = player.GetComponent<IDamageable>();
        if (dmg == null || dmg.IsDead) return;
        _damageCdTimer = contactDamageCooldown;
        Vector3 dir = (player.position - transform.position).normalized;
        dmg.TakeDamage(contactDamage, player.position, dir);
        PlaySfx(sfxAttack);
    }

    bool CanSeePlayer(out float dist)
    {
        dist = float.PositiveInfinity;
        if (player == null) return false;
        Vector3 toPlayer = player.position - transform.position;
        dist = toPlayer.magnitude;
        if (dist > detectRange) return false;
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * playerHeightOffset;
        Vector3 dir = (target - eye);
        float rayLen = dir.magnitude;
        if (rayLen < 0.01f) return true;
        if (Physics.Raycast(eye, dir / rayLen, out var hit, rayLen, sightObstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player)) return true;
            return false;
        }
        return true;
    }

    void PickPatrolPoint()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = _patrolHome + new Vector3(rnd.x, 0f, rnd.y);
            if (NavMesh.SamplePosition(candidate, out var navHit, 2f, NavMesh.AllAreas))
            {
                _agent.SetDestination(navHit.position);
                _patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);
                return;
            }
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_isDead) return;
        currentHealth -= amount;
        _hasSeenPlayer = true;
        if (_hitFlash != null) _hitFlash.Flash();
        if (currentHealth <= 0f) { Die(); return; }
        _hitStunTimer = hitStunDuration;
        _agent.isStopped = true;
        _animator.SetTrigger(P_Hit);
        _state = State.Hit;
        PlaySfx(sfxHit);
    }

    void Die()
    {
        _isDead = true;
        _state = State.Dead;
        _agent.isStopped = true;
        _agent.enabled = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        _animator.speed = 1f;
        _animator.SetTrigger(P_Die);
        PlaySfx(sfxDeath);
        Destroy(gameObject, deathDespawnDelay);
    }

    void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, contactRange);
        Gizmos.color = Color.cyan;
        Vector3 home = Application.isPlaying ? _patrolHome : transform.position;
        Gizmos.DrawWireSphere(home, patrolRadius);
    }
#endif
}
