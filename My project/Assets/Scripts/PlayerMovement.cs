using UnityEngine;
using TMPro;

/// <summary>
/// PlayerMovement — Ultrakill-style ground slam.
///
/// SLAM
///   • Tap crouch key while airborne → instant max downward velocity, NO shockwave on landing.
///   • Hold crouch key while airborne → instant max downward velocity, shockwave on landing.
///   • Shockwave: launches enemies upward, power scales with air time spent slamming.
///   • Direct hit: 2 damage to any enemy directly below on impact (stub via OnSlamDirectHit).
///   • Slam Bounce: jump immediately after landing → extra height scaled by slam air time.
///   • No air steering while slamming.
///
/// GROUND DETECTION
///   Uses OnCollisionEnter/Exit for precise ground contact.
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

    [Header("Double Jump")]
    public float doubleJumpForce = 10f;
    [Tooltip("Seconds after a dash where double jump preserves full horizontal speed.")]
    public float dashMomentumWindow = 0.6f;

    [Header("Crouch")]
    public float crouchYScale = 0.5f;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Slam")]
    [Tooltip("Downward velocity set instantly when slam starts.")]
    public float slamInstantDownVelocity = 40f;
    [Tooltip("Radius of the shockwave AOE sphere on impact.")]
    public float slamAOERadius = 4f;
    [Tooltip("Layers the shockwave and direct hit can affect.")]
    public LayerMask slamHitMask;
    [Tooltip("Direct-hit sphere radius — enemies directly below you when you land.")]
    public float slamDirectHitRadius = 1.2f;
    [Tooltip("Base upward force applied to enemies hit by the shockwave.")]
    public float shockwaveLaunchBase = 12f;
    [Tooltip("Extra upward force added per second spent slamming (scales the launch).")]
    public float shockwaveLaunchPerSecond = 8f;
    [Tooltip("Max total upward launch force on enemies.")]
    public float shockwaveLaunchMax = 40f;
    [Tooltip("Base extra jump height on a slam bounce.")]
    public float slamBounceBaseForce = 14f;
    [Tooltip("Extra bounce force added per second spent slamming.")]
    public float slamBounceForcePerSecond = 6f;
    [Tooltip("Max slam bounce force.")]
    public float slamBounceMaxForce = 32f;
    [Tooltip("Window after landing during which a jump counts as a slam bounce (seconds).")]
    public float slamBounceWindow = 0.18f;
    [Tooltip("How much horizontal momentum is retained on landing (0 = dead stop).")]
    [Range(0f, 1f)]
    public float slamLandingMomentumRetain = 0.3f;
    [Tooltip("Draw the AOE gizmo in the editor.")]
    public bool drawSlamGizmo = true;

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
    bool isSlamming;
    bool slamHeld;
    float slamAirTime;
    float slamBounceTimer;
    bool inSlamBounce;
    Vector3 lastSlamGizmoPos;

    // dash
    bool readyToDash = true;
    bool isDashing;
    float dashHotTimer;
    bool dashIsHot => dashHotTimer > 0f;

    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;

    // reference to wall ride so we can guard BetterGravity and refresh double jump
    WallRide wallRide;

    // ── lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = transform.localScale.y;
        wallRide = GetComponent<WallRide>();
    }

    void Update()
    {
        if (!grounded)
            coyoteTimer -= Time.deltaTime;

        if (dashHotTimer > 0f)
            dashHotTimer -= Time.deltaTime;

        if (isSlamming)
            slamAirTime += Time.deltaTime;

        if (slamBounceTimer > 0f)
            slamBounceTimer -= Time.deltaTime;

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
    }

    // ── public accessors ─────────────────────────────────────────────────────

    /// <summary>Called by WallRide on detach to restore the double jump.</summary>
    public void RestoreDoubleJump() => hasDoubleJump = true;

    /// <summary>True while a slam is in progress (used by WallRide to stay detached).</summary>
    public bool IsSlamming => isSlamming;

    // ── collision detection ──────────────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & whatIsGround) != 0)
        {
            grounded = true;
            coyoteTimer = coyoteTime;
            hasDoubleJump = true;

            if (isSlamming)
                TriggerSlamImpact();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & whatIsGround) != 0)
            grounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & whatIsGround) != 0)
            grounded = false;
    }

    // ── input ────────────────────────────────────────────────────────────────

    void GatherInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        moveDirection = orientation.forward * verticalInput
                        + orientation.right * horizontalInput;

        // jump — set flags; only CLEAR them in HandleJump when consumed
        if (Input.GetKeyDown(jumpKey))
        {
            jumpRequested = true;
            doubleJumpRequested = true;
        }

        // dash
        if (Input.GetKeyDown(dashKey) && readyToDash)
            Dash();

        // slam: tap vs hold
        if (Input.GetKeyDown(crouchKey))
        {
            if (!grounded && !isSlamming)
            {
                slamHeld = false;
                StartSlam();
            }
            else if (grounded)
            {
                StartCrouch();
            }
        }

        if (isSlamming && Input.GetKey(crouchKey))
            slamHeld = true;

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
        if (isSlamming) return;

        // ── slam bounce (highest priority) ───────────────────────────────────
        // Check grounded too: the bounce timer opens the same frame we land,
        // so grounded will already be true when HandleJump next runs.
        if (jumpRequested && slamBounceTimer > 0f && !inSlamBounce)
        {
            inSlamBounce = true;
            slamBounceTimer = 0f;

            float bounceForce = Mathf.Min(
                slamBounceBaseForce + slamBounceForcePerSecond * slamAirTime,
                slamBounceMaxForce
            );

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);

            jumpRequested = false;
            doubleJumpRequested = false;
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
            return;
        }

        // ── ground / coyote jump ──────────────────────────────────────────────
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

        // ── double jump ───────────────────────────────────────────────────────
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

            jumpRequested = false;
            doubleJumpRequested = false;
            return;
        }

        // Flags not consumed this frame — clear them so they don't persist
        // Only clear jumpRequested if no path above could still use it.
        // Both flags survive until the next GatherInput writes new input.
        jumpRequested = false;
        doubleJumpRequested = false;
    }

    void ResetJump() => readyToJump = true;

    void BetterGravity()
    {
        // Don't fight WallRide's gravity cancellation or the slam's forced descent
        if (isSlamming) return;
        if (wallRide != null && wallRide.IsWallRunning) return;

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
        slamAirTime = 0f;
        inSlamBounce = false;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            -slamInstantDownVelocity,
            rb.linearVelocity.z
        );

        // Shrink the player collider by scaling down; adjust position so the
        // bottom of the capsule stays in place (doesn't clip into the floor).
        float scaleChange = startYScale - crouchYScale;
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale,
                                           transform.localScale.z);
        transform.position += Vector3.up * (scaleChange * 0.5f);

        OnSlamStart();
    }

    void TriggerSlamImpact()
    {
        isSlamming = false;

        // ── impact point: bottom of player, BEFORE we restore scale ──────────
        // At this moment the player is still in crouched scale, so the foot
        // position is transform.position − up*(crouchYScale * 0.5f * originalObjectHeight).
        // The simplest reliable approach: raycast straight down a short distance.
        Vector3 impactPoint;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit,
                            playerHeight, whatIsGround))
        {
            impactPoint = groundHit.point;
        }
        else
        {
            // Fallback: use the base of the (crouched) capsule
            impactPoint = transform.position - Vector3.up * (playerHeight * crouchYScale * 0.5f);
        }

        lastSlamGizmoPos = impactPoint;

        // ── bleed horizontal speed, kill vertical ─────────────────────────────
        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x * slamLandingMomentumRetain,
            0f,
            rb.linearVelocity.z * slamLandingMomentumRetain
        );

        // ── restore scale (AFTER we've captured impactPoint) ─────────────────
        float scaleChange = startYScale - crouchYScale;
        transform.localScale = new Vector3(transform.localScale.x, startYScale,
                                           transform.localScale.z);
        transform.position += Vector3.up * (scaleChange * 0.5f);

        // ── direct hit ────────────────────────────────────────────────────────
        Collider[] directHits = Physics.OverlapSphere(impactPoint, slamDirectHitRadius, slamHitMask);
        foreach (Collider hit in directHits)
            OnSlamDirectHit(hit, impactPoint);

        // ── shockwave (hold only) ─────────────────────────────────────────────
        if (slamHeld)
        {
            float launchForce = Mathf.Min(
                shockwaveLaunchBase + shockwaveLaunchPerSecond * slamAirTime,
                shockwaveLaunchMax
            );

            Collider[] aoeHits = Physics.OverlapSphere(impactPoint, slamAOERadius, slamHitMask);
            foreach (Collider hit in aoeHits)
                OnShockwaveHit(hit, impactPoint, launchForce);

            OnSlamImpact(impactPoint, aoeHits.Length, launchForce);
        }
        else
        {
            OnSlamImpact(impactPoint, 0, 0f);
        }

        // ── open slam bounce window ───────────────────────────────────────────
        slamBounceTimer = slamBounceWindow;
    }

    // ── slam hooks ───────────────────────────────────────────────────────────

    void OnSlamStart()
    {
        Debug.Log("[Slam] Started | held: " + slamHeld);
    }

    void OnSlamDirectHit(Collider hit, Vector3 origin)
    {
        // TODO: hit.GetComponent<HealthComponent>()?.TakeDamage(2, origin);
        Debug.Log($"[Slam] Direct hit: {hit.name}");
    }

    void OnShockwaveHit(Collider hit, Vector3 origin, float launchForce)
    {
        Rigidbody enemyRb = hit.GetComponent<Rigidbody>();
        if (enemyRb != null)
            enemyRb.AddForce(Vector3.up * launchForce, ForceMode.Impulse);

        Debug.Log($"[Slam] Shockwave hit: {hit.name} | launch: {launchForce:F1}");
    }

    void OnSlamImpact(Vector3 point, int hitCount, float launchForce)
    {
        Debug.Log($"[Slam] Impact at {point} | shockwave hits: {hitCount} | launch: {launchForce:F1} | airTime: {slamAirTime:F2}s");
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

        // Use last known impact point; fall back to the foot of the player in editor
        Vector3 origin = lastSlamGizmoPos != Vector3.zero
            ? lastSlamGizmoPos
            : transform.position - Vector3.up * (playerHeight * 0.5f);

        // aoe here vvv
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawSphere(origin, slamAOERadius);

        // direct hit radius
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawSphere(origin, slamDirectHitRadius);
    }
}