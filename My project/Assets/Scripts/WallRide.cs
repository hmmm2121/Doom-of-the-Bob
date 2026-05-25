using UnityEngine;

/// <summary>
/// Lúcio-style wall ride.
/// • Hold Space near a wall  → attach and run along it.
/// • Release Space           → wall-jump automatically (no button press needed).
/// • Touch the ground        → detach normally.
/// </summary>
public class WallRide : MonoBehaviour
{
    // ── inspector ────────────────────────────────────────────────────────────

    [Header("References")]
    public Transform orientation;

    [Header("Wall detection")]
    public LayerMask whatIsWall;
    public float wallCheckDistance = 0.7f;
    public LayerMask whatIsGround;
    public float playerHeight = 2f;

    [Header("Wall run")]
    public float wallRunSpeed = 15f;   // target horizontal speed along wall
    public float wallStickForce = 18f;   // perpendicular force pressing you into the wall
    public float wallUpForce = 4f;    // counter-gravity while riding
    public float maxWallRunTime = 2f;    // hard time limit per continuous ride

    [Header("Wall jump")]
    public float wallJumpSideForce = 12f;
    public float wallJumpUpForce = 10f;

    [Header("Feel")]
    [Tooltip("How quickly the along-wall speed builds up (higher = snappier attach)")]
    public float wallAcceleration = 10f;
    [Tooltip("Max camera tilt angle (degrees) – read by your camera script")]
    public float maxTiltAngle = 12f;

    // ── read-only for other scripts ──────────────────────────────────────────

    /// <summary>Camera tilt target: −1 (left wall) … 0 … +1 (right wall).</summary>
    public float WallRunTilt { get; private set; }
    public bool IsWallRunning => isWallRunning;

    // ── private state ────────────────────────────────────────────────────────

    Rigidbody rb;

    bool wallLeft, wallRight;
    RaycastHit leftHit, rightHit;
    Vector3 wallNormal;
    bool lastWallLeft, lastWallRight;   // track wall switches for timer reset

    bool isWallRunning;
    float wallTimer;
    bool grounded;

    // cached per-frame along-wall direction
    Vector3 wallForward;

    // ── lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        GroundCheck();
        CheckForWall();
        HandleWallState();
    }

    void FixedUpdate()
    {
        if (isWallRunning)
            ApplyWallRunForces();
    }

    // ── wall detection ───────────────────────────────────────────────────────

    void GroundCheck()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down,
                       playerHeight * 0.5f + 0.3f, whatIsGround);
    }

    void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right,
                        out rightHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right,
                        out leftHit, wallCheckDistance, whatIsWall);

        if (wallRight) wallNormal = rightHit.normal;
        else if (wallLeft) wallNormal = leftHit.normal;
        // keep last normal if no wall is detected so jump still works
    }

    // ── state machine ────────────────────────────────────────────────────────

    void HandleWallState()
    {
        bool holdingSpace = Input.GetKey(KeyCode.Space);
        bool releasedSpace = Input.GetKeyUp(KeyCode.Space);
        bool nearWall = wallLeft || wallRight;

        // ── attach ───────────────────────────────────────────────────────────
        if (!isWallRunning && !grounded && nearWall && holdingSpace && HasForwardInput())
        {
            StartWallRun();
            return;
        }

        if (!isWallRunning) return;

        // ── while riding ─────────────────────────────────────────────────────

        // switched to the other wall? reset the timer so you don't get cut short
        if ((wallLeft != lastWallLeft) || (wallRight != lastWallRight))
        {
            wallTimer = maxWallRunTime;
            lastWallLeft = wallLeft;
            lastWallRight = wallRight;
        }

        wallTimer -= Time.deltaTime;

        bool shouldDetach = grounded           // landed
                         || !nearWall          // ran off the wall's edge
                         || !holdingSpace      // player released Space
                         || wallTimer <= 0f;   // time limit hit

        if (shouldDetach)
        {
            bool doJump = releasedSpace && !grounded;  // Lúcio: release = jump
            StopWallRun(doJump);
        }
    }

    void StartWallRun()
    {
        isWallRunning = true;
        wallTimer = maxWallRunTime;
        lastWallLeft = wallLeft;
        lastWallRight = wallRight;

        rb.useGravity = false;   // kill gravity immediately – no blending needed

        // Flatten vertical velocity on attach so there's no upward lurch
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    void StopWallRun(bool jumpOff)
    {
        if (!isWallRunning) return;

        isWallRunning = false;
        rb.useGravity = true;   // gravity back on instantly
        WallRunTilt = 0f;

        if (jumpOff)
            DoWallJump();
    }

    // ── forces ───────────────────────────────────────────────────────────────

    void ApplyWallRunForces()
    {
        // along-wall direction, oriented toward where the player is looking
        wallForward = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, orientation.forward) < 0f)
            wallForward = -wallForward;

        // ── speed control ─────────────────────────────────────────────────
        // Work only on horizontal speed to avoid fighting the up-force below.
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currSpeed = flatVel.magnitude;

        // Smoothly accelerate toward target speed
        float targetSpeed = wallRunSpeed;
        float newSpeed = Mathf.MoveTowards(currSpeed, targetSpeed,
                                wallAcceleration * Time.fixedDeltaTime);

        // Only redirect horizontal velocity along the wall; don't kill vertical
        if (currSpeed > 0.1f)
        {
            Vector3 desiredFlat = wallForward * newSpeed;
            rb.linearVelocity = new Vector3(desiredFlat.x, rb.linearVelocity.y, desiredFlat.z);
        }

        // ── perpendicular stick ───────────────────────────────────────────
        rb.AddForce(-wallNormal * wallStickForce, ForceMode.Force);

        // ── counter gravity ───────────────────────────────────────────────
        // Apply just enough to cancel gravity, plus a tiny upward bias
        float gravCancel = -Physics.gravity.y;          // ~9.81 on standard settings
        rb.AddForce(Vector3.up * (gravCancel + wallUpForce), ForceMode.Acceleration);

        // ── camera tilt signal ────────────────────────────────────────────
        WallRunTilt = wallRight ? 1f : -1f;
    }

    // ── wall jump ────────────────────────────────────────────────────────────

    void DoWallJump()
    {
        // Zero out vertical velocity for a clean, predictable arc
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Push away from wall + up
        Vector3 jumpDir = wallNormal.normalized * wallJumpSideForce
                        + Vector3.up * wallJumpUpForce;
        rb.AddForce(jumpDir, ForceMode.Impulse);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    bool HasForwardInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"),
                           Input.GetAxisRaw("Vertical")).sqrMagnitude > 0.04f;
    }
}