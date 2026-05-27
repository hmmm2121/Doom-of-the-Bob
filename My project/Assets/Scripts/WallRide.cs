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
    public float wallRunSpeed = 15f;
    public float wallStickForce = 18f;
    public float wallUpForce = 4f;
    public float maxWallRunTime = 2f;

    [Header("Wall jump")]
    public float wallJumpSideForce = 12f;
    public float wallJumpUpForce = 10f;

    [Header("Feel")]
    [Tooltip("How quickly the along-wall speed builds up (higher = snappier attach)")]
    public float wallAcceleration = 10f;
    [Tooltip("Max camera tilt angle (degrees) – read by your camera script")]
    public float maxTiltAngle = 12f;

    // ── read-only for other scripts ──────────────────────────────────────────

    public float WallRunTilt { get; private set; }
    public bool IsWallRunning => isWallRunning;

    // ── private state ────────────────────────────────────────────────────────

    Rigidbody rb;
    PlayerMovement playerMovement;

    bool wallLeft, wallRight;
    RaycastHit leftHit, rightHit;
    Vector3 wallNormal;
    bool lastWallLeft, lastWallRight;

    bool isWallRunning;
    float wallTimer;
    bool grounded;

    Vector3 wallForward;

    // ── lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
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
    }

    // ── state machine ────────────────────────────────────────────────────────

    void HandleWallState()
    {
        bool holdingSpace = Input.GetKey(KeyCode.Space);
        bool releasedSpace = Input.GetKeyUp(KeyCode.Space);
        bool nearWall = wallLeft || wallRight;

        // Never wall-ride while slamming
        if (playerMovement != null && playerMovement.IsSlamming)
        {
            if (isWallRunning) StopWallRun(false);
            return;
        }

        // ── attach ───────────────────────────────────────────────────────────
        if (!isWallRunning && !grounded && nearWall && holdingSpace && HasForwardInput())
        {
            StartWallRun();
            return;
        }

        if (!isWallRunning) return;

        // ── while riding ─────────────────────────────────────────────────────

        if ((wallLeft != lastWallLeft) || (wallRight != lastWallRight))
        {
            wallTimer = maxWallRunTime;
            lastWallLeft = wallLeft;
            lastWallRight = wallRight;
        }

        wallTimer -= Time.deltaTime;

        bool shouldDetach = grounded
                         || !nearWall
                         || !holdingSpace
                         || wallTimer <= 0f;

        if (shouldDetach)
        {
            // Lúcio rule: releasing Space while still on the wall = jump off.
            // Time-out or wall-end = just fall (no automatic jump).
            bool doJump = releasedSpace && nearWall && !grounded;
            StopWallRun(doJump);
        }
    }

    void StartWallRun()
    {
        isWallRunning = true;
        wallTimer = maxWallRunTime;
        lastWallLeft = wallLeft;
        lastWallRight = wallRight;

        rb.useGravity = false;

        // Flatten vertical velocity so there's no upward lurch on attach
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    void StopWallRun(bool jumpOff)
    {
        if (!isWallRunning) return;

        isWallRunning = false;
        rb.useGravity = true;
        WallRunTilt = 0f;

        // Restore double jump — wall ride counts as a reset surface
        if (playerMovement != null)
            playerMovement.RestoreDoubleJump();

        if (jumpOff)
            DoWallJump();
    }

    // ── forces ───────────────────────────────────────────────────────────────

    void ApplyWallRunForces()
    {
        // Along-wall direction, oriented toward where the player is looking
        wallForward = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, orientation.forward) < 0f)
            wallForward = -wallForward;

        // ── speed control ─────────────────────────────────────────────────────
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currSpeed = flatVel.magnitude;

        float newSpeed = Mathf.MoveTowards(currSpeed, wallRunSpeed,
                                           wallAcceleration * Time.fixedDeltaTime);

        if (currSpeed > 0.1f)
        {
            Vector3 desiredFlat = wallForward * newSpeed;
            rb.linearVelocity = new Vector3(desiredFlat.x, rb.linearVelocity.y, desiredFlat.z);
        }

        // ── perpendicular stick ───────────────────────────────────────────────
        rb.AddForce(-wallNormal * wallStickForce, ForceMode.Force);

        // ── gravity cancellation + upward bias ────────────────────────────────
        // BetterGravity in PlayerMovement is suppressed while IsWallRunning,
        // so we only need to cancel Physics.gravity here.
        float gravCancel = -Physics.gravity.y;    // ≈ 9.81 with default settings
        rb.AddForce(Vector3.up * (gravCancel + wallUpForce), ForceMode.Acceleration);

        // ── camera tilt signal ────────────────────────────────────────────────
        WallRunTilt = wallRight ? 1f : -1f;
    }

    // ── wall jump ────────────────────────────────────────────────────────────

    void DoWallJump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

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