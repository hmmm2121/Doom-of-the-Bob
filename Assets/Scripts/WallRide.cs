using UnityEngine;

public class WallRide : MonoBehaviour
{
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
    public float wallAcceleration = 10f;
    public float maxTiltAngle = 12f;

    public float WallRunTilt { get; private set; }
    public bool IsWallRunning => isWallRunning;

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

    void HandleWallState()
    {
        bool holdingSpace = Input.GetKey(KeyCode.Space);
        bool releasedSpace = Input.GetKeyUp(KeyCode.Space);
        bool nearWall = wallLeft || wallRight;

        if (playerMovement != null && playerMovement.IsSlamming)
        {
            if (isWallRunning) StopWallRun(false);
            return;
        }

        if (!isWallRunning && !grounded && nearWall && holdingSpace && HasForwardInput())
        {
            StartWallRun();
            return;
        }

        if (!isWallRunning) return;

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

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    void StopWallRun(bool jumpOff)
    {
        if (!isWallRunning) return;

        isWallRunning = false;
        rb.useGravity = true;
        WallRunTilt = 0f;

        if (playerMovement != null)
            playerMovement.RestoreDoubleJump();

        if (jumpOff)
            DoWallJump();
    }

    void ApplyWallRunForces()
    {
        wallForward = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, orientation.forward) < 0f)
            wallForward = -wallForward;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currSpeed = flatVel.magnitude;

        float newSpeed = Mathf.MoveTowards(currSpeed, wallRunSpeed,
                                           wallAcceleration * Time.fixedDeltaTime);

        if (currSpeed > 0.1f)
        {
            Vector3 desiredFlat = wallForward * newSpeed;
            rb.linearVelocity = new Vector3(desiredFlat.x, rb.linearVelocity.y, desiredFlat.z);
        }

        rb.AddForce(-wallNormal * wallStickForce, ForceMode.Force);

        float gravCancel = -Physics.gravity.y;
        rb.AddForce(Vector3.up * (gravCancel + wallUpForce), ForceMode.Acceleration);

        WallRunTilt = wallRight ? 1f : -1f;
    }

    void DoWallJump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 jumpDir = wallNormal.normalized * wallJumpSideForce
                        + Vector3.up * wallJumpUpForce;
        rb.AddForce(jumpDir, ForceMode.Impulse);
    }

    bool HasForwardInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"),
                           Input.GetAxisRaw("Vertical")).sqrMagnitude > 0.04f;
    }
}
