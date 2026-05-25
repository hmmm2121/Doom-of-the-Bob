using UnityEngine;
using TMPro;

/// <summary>
/// PlayerMovement — with ground slam replacing slide.
///
/// SLAM
///   • Hold crouch key while airborne → slams straight down.
///   • On impact: dead stop (all velocity zeroed), AOE sphere cast below.
///   • Damage/HP logic is stubbed via OnSlamImpact() — wire it up when ready.
///   • Visual/audio hooks: SlamStart() and SlamImpact() are partial methods
///     you can extend without touching core logic.
///
/// GROUND DETECTION
///   uses OnCollisionEnter/Exit instead of raycast for precise ground contact.
///   jump and double jump reset ONLY when your collider touches the ground.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    // ── inspector ────────────────────────────────────────────────────────────

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float groundDrag = 6f;
    public float airMultiplier = 0.4f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public float jumpCooldown = 0.25f;
    public float coyoteTime = 0.15f;
    public float fallMultiplier = 3f;
    public float lowJumpMultiplier = 2f;

    [Header("double Jump")]
    public float doubleJumpForce = 10f;
    [Tooltip("seconds after a dash where double jump preserves full horizontal speed.")]
    public float dashMomentumWindow = 0.6f;

    [Header("Crouch")]
    public float crouchYScale = 0.5f;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Slam")]
    [Tooltip("downward force applied every FixedUpdate while slamming.")]
    public float slamDownForce = 60f;
    [Tooltip("radius of the AOE sphere on impact.")] // not used yet
    public float slamAOERadius = 4f;
    [Tooltip("layers the AOE hits.")]
    public LayerMask slamHitMask;
    [Tooltip("visual/debug: draw the AOE gizmo in the editor.")]
    public bool drawSlamGizmo = true;
    [Tooltip("how much horizontal momentum carries through on landing (0 = dead stop).")]
    [Range(0f, 1f)]
    public float slamLandingMomentumRetain = 0.4f;

    [Header("Dash")]
    public KeyCode dashKey = KeyCode.Q;
    public float dashSpeed = 26f;
    public float dashCoastDuration = 0.35f;
    public float dashCooldown = 0.8f;
    [Range(0f, 1f)]
    public float dashMomentumBlend = 0.4f;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;

    [Header("References")]
    public Transform orientation;
    [HideInInspector] public TextMeshProUGUI text_speed;

    // ── private state ────────────────────────────────────────────────────────

    Rigidbody rb;

    bool grounded;
    bool readyToJump = true;
    bool jumpRequested;
    bool doubleJumpRequested;
    float coyoteTimer;
    bool hasDoubleJump;

    bool isCrouching;
    float startYScale;

    // slam
    bool isSlamming;          // descending fast toward ground
    bool slamLanded;          // one-frame flag: impact happened this frame
    Vector3 lastSlamGizmoPos;  // for debug gizmo

    // dash
    bool readyToDash = true;
    bool isDashing;
    float dashHotTimer;
    bool dashIsHot => dashHotTimer > 0f;

    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;

    // ── lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = transform.localScale.y;
    }

    void Update()
    {
        // coyote time countdown when not grounded
        if (!grounded)
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (dashHotTimer > 0f) dashHotTimer -= Time.deltaTime;

        GatherInput();
        ApplyDrag();

        if (text_speed != null)
        {
            Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            text_speed.SetText("spd: " + flat.magnitude.ToString("F1"));
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
        HandleJump();
        BetterGravity();

        if (isSlamming)
            rb.AddForce(Vector3.down * slamDownForce, ForceMode.Acceleration);
    }

    // ── collision detection ──────────────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {
        // check if we collided with ground layer
        if (((1 << collision.gameObject.layer) & whatIsGround) != 0)
        {
            grounded = true;
            coyoteTimer = coyoteTime;
            hasDoubleJump = true;

            // ── slam impact ──────────────────────────────────────────────────
            if (isSlamming)
                TriggerSlamImpact();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // maintain grounded state while touching ground
        if (((1 << collision.gameObject.layer) & whatIsGround) != 0)
        {
            grounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // check if we left the ground layer
        if (((1 << collision.gameObject.layer) & whatIsGround) != 0)
        {
            grounded = false;
        }
    }

    // ── input ────────────────────────────────────────────────────────────────

    void GatherInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        moveDirection = orientation.forward * verticalInput
                        + orientation.right * horizontalInput;

        // jump
        if (Input.GetKeyDown(jumpKey))
        {
            jumpRequested = true;
            doubleJumpRequested = true;
        }
        if (Input.GetKey(jumpKey))
            jumpRequested = true;

        // dash
        if (Input.GetKeyDown(dashKey) && readyToDash)
            Dash();

        // crouch (ground) OR slam (air)
        if (Input.GetKeyDown(crouchKey))
        {
            if (!grounded && !isSlamming)
                StartSlam();
            else if (grounded)
                StartCrouch();
        }

        if (Input.GetKeyUp(crouchKey) && isCrouching)
            StopCrouch();
    }

    // ── drag ─────────────────────────────────────────────────────────────────

    void ApplyDrag()
    {
        if (isDashing) rb.linearDamping = 0f;
        else if (grounded) rb.linearDamping = groundDrag;
        else rb.linearDamping = 0f;
    }

    // ── movement ─────────────────────────────────────────────────────────────

    void MovePlayer()
    {
        // no air-steering during a slam — you committed
        if (isSlamming) return;

        float force = moveSpeed * 10f * (grounded ? 1f : airMultiplier);
        rb.AddForce(moveDirection.normalized * force, ForceMode.Force);

        if (!isDashing)
        {
            Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float cap = isCrouching ? moveSpeed * 0.5f : moveSpeed;
            if (flat.magnitude > cap)
            {
                Vector3 capped = flat.normalized * cap;
                rb.linearVelocity = new Vector3(capped.x, rb.linearVelocity.y, capped.z);
            }
        }
    }

    // ── jump ─────────────────────────────────────────────────────────────────

    void HandleJump()
    {
        // cancel a slam if the player somehow wants to jump (safety net)
        if (isSlamming) return;

        // ground / coyote jump
        if (jumpRequested && readyToJump && coyoteTimer > 0f)
        {
            readyToJump = false;
            jumpRequested = false;
            doubleJumpRequested = false;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            Invoke(nameof(ResetJump), jumpCooldown);
            return;
        }

        // double jump
        if (doubleJumpRequested && !grounded && hasDoubleJump)
        {
            doubleJumpRequested = false;
            hasDoubleJump = false;

            if (dashIsHot)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * doubleJumpForce, ForceMode.Impulse);
            }
            else
            {
                Vector3 airDir = moveDirection.normalized;
                if (airDir == Vector3.zero) airDir = orientation.forward;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * doubleJumpForce, ForceMode.Impulse);
                rb.AddForce(airDir * (moveSpeed * 2f), ForceMode.Impulse);
            }
        }

        jumpRequested = false;
        doubleJumpRequested = false;
    }

    void ResetJump() => readyToJump = true;

    void BetterGravity()
    {
        if (isSlamming) return;   // slam force handles descent

        if (rb.linearVelocity.y < 0)
            rb.AddForce(Vector3.down * fallMultiplier, ForceMode.Acceleration);
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(jumpKey))
            rb.AddForce(Vector3.down * lowJumpMultiplier, ForceMode.Acceleration);
    }

    // ── crouch ───────────────────────────────────────────────────────────────

    void StartCrouch()
    {
        isCrouching = true;
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale,
                                           transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    void StopCrouch()
    {
        isCrouching = false;
        transform.localScale = new Vector3(transform.localScale.x, startYScale,
                                           transform.localScale.z);
    }

    // ── slam ─────────────────────────────────────────────────────────────────

    void StartSlam()
    {
        isSlamming = true;

        // keep horizontal momentum so the slam feels like a dive, not a drop 
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);


        float scaleChange = startYScale - crouchYScale;
        transform.position += Vector3.up * (scaleChange * 0.5f);
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale,
                                           transform.localScale.z);

        // hook: play slam wind-up sound / particle here
        OnSlamStart();
    }

    void TriggerSlamImpact()
    {
        isSlamming = false;

        // bleed horizontal momentum into ground drag rather than hard-zeroing 

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x * slamLandingMomentumRetain,
            0f,
            rb.linearVelocity.z * slamLandingMomentumRetain
        );

        float scaleChange = startYScale - crouchYScale;
        transform.position += Vector3.up * (scaleChange * 0.5f);
        transform.localScale = new Vector3(transform.localScale.x, startYScale,
                                           transform.localScale.z);

        Vector3 impactPoint = transform.position - Vector3.up * (playerHeight * 0.5f);
        lastSlamGizmoPos = impactPoint;

        Collider[] hits = Physics.OverlapSphere(impactPoint, slamAOERadius, slamHitMask);

        foreach (Collider hit in hits)
        {
            // ── TODO: wire up damage here when HP system is ready ────────────
            // Example:
            //   HealthComponent hp = hit.GetComponent<HealthComponent>();
            //   if (hp != null) hp.TakeDamage(slamDamage, impactPoint);
            OnSlamHit(hit, impactPoint);
        }

        // hook: play impact camera shake / shockwave VFX / sound here
        OnSlamImpact(impactPoint, hits.Length);
    }

    // ── slam hooks (extend these without touching core logic) ────────────────

    /// <summary>Called the frame the slam initiates. Spawn wind-up VFX here.</summary>
    void OnSlamStart() { }

    /// <summary>Called for each collider caught in the AOE. Apply damage here.</summary>
    void OnSlamHit(Collider hit, Vector3 origin)
    {
        // stub — replace with actual damage call
        Debug.Log($"[Slam] Hit: {hit.name} | dist: {Vector3.Distance(origin, hit.transform.position):F1}m");
    }

    /// <summary>Called once on impact. hitCount = enemies in AOE. Trigger shockwave VFX here.</summary>
    void OnSlamImpact(Vector3 point, int hitCount)
    {
        Debug.Log($"[Slam] Impact at {point} | AOE hits: {hitCount}");
    }

    // ── dash ─────────────────────────────────────────────────────────────────

    void Dash()
    {
        readyToDash = false;
        isDashing = true;

        Vector3 dir = moveDirection.sqrMagnitude > 0.01f
                    ? moveDirection.normalized
                    : orientation.forward;

        Vector3 currentFlat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 blended = currentFlat * dashMomentumBlend + dir * dashSpeed;

        if (blended.magnitude > dashSpeed)
            blended = blended.normalized * dashSpeed;

        rb.linearVelocity = new Vector3(blended.x, rb.linearVelocity.y, blended.z);

        dashHotTimer = dashMomentumWindow;

        Invoke(nameof(EndDashCoast), dashCoastDuration);
        Invoke(nameof(ResetDash), dashCooldown);
    }

    void EndDashCoast() => isDashing = false;
    void ResetDash() => readyToDash = true;

    // ── gizmos ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!drawSlamGizmo) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawSphere(lastSlamGizmoPos == Vector3.zero
                          ? transform.position
                          : lastSlamGizmoPos, slamAOERadius);
    }
}