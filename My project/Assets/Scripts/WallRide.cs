using UnityEngine;

public class WallRide : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    private Rigidbody rb;

    [Header("Wall Detection")]
    public LayerMask whatIsWall;
    public float wallCheckDistance = 0.8f;

    private bool wallLeft;
    private bool wallRight;

    private RaycastHit leftHit;
    private RaycastHit rightHit;

    private Vector3 wallNormal;

    [Header("Wall Movement")]
    public float wallRunForce = 18f;
    public float wallStickForce = 8f;
    public float wallUpForce = 3f;
    public float maxWallRunTime = 1.5f;

    private float wallTimer;
    private bool isWallRunning;

    [Header("Wall Jump")]
    public float wallJumpForce = 10f;
    public float wallJumpUpForce = 6f;

    [Header("Ground Check")]
    public LayerMask whatIsGround;
    public float playerHeight = 2f;

    [Header("Wall Run Control")]
    public float minInputToWallRun = 0.2f;

    private bool grounded;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        GroundCheck();
        CheckForWall();
        HandleWallState();

        if (Input.GetKeyDown(KeyCode.Space) && isWallRunning)
        {
            WallJump();
        }
    }

    private void FixedUpdate()
    {
        if (isWallRunning)
            WallRunMovement();
    }

    void GroundCheck()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);
    }

    void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftHit, wallCheckDistance, whatIsWall);

        if (wallRight)
            wallNormal = rightHit.normal;
        else if (wallLeft)
            wallNormal = leftHit.normal;
    }

    void HandleWallState()
    {
        float inputMagnitude = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude;

        bool canWallRun = !grounded &&
                          (wallLeft || wallRight) &&
                          inputMagnitude > minInputToWallRun;

        if (canWallRun)
        {
            if (!isWallRunning)
                StartWallRun();

            wallTimer -= Time.deltaTime;

            if (wallTimer <= 0)
                StopWallRun();
        }
        else
        {
            StopWallRun();
        }
    }

    void StartWallRun()
    {
        isWallRunning = true;
        wallTimer = maxWallRunTime;

        rb.useGravity = false;
    }

    void StopWallRun()
    {
        if (!isWallRunning) return;

        isWallRunning = false;
        rb.useGravity = true;
    }

    void WallRunMovement()
    {
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

        if (Vector3.Dot(wallForward, orientation.forward) < 0)
            wallForward = -wallForward;

        // REMOVE velocity INTO wall (IMPORTANT FIX)
        Vector3 currentVel = rb.linearVelocity;
        Vector3 intoWall = Vector3.Project(currentVel, -wallNormal);

        rb.linearVelocity -= intoWall;

        // optional: keep movement smooth on wall plane
        Vector3 alongWallVel = Vector3.ProjectOnPlane(currentVel, wallNormal);
        rb.linearVelocity = new Vector3(alongWallVel.x, rb.linearVelocity.y, alongWallVel.z);

        // forward along wall
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        // reduced stick force (prevents "gluing")
        float inputMagnitude = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude;

        // scale stick force based on player intention
        float stickMultiplier = Mathf.Clamp(inputMagnitude, 0.2f, 1f);

        rb.AddForce(-wallNormal * wallStickForce * stickMultiplier, ForceMode.Force);

        rb.AddForce(-wallNormal * (wallStickForce * 0.3f), ForceMode.Force);

        // slight upward lift
        rb.AddForce(Vector3.up * wallUpForce, ForceMode.Force);
    }

    void WallJump()
    {
        StopWallRun();

        Vector3 force =
            wallNormal * wallJumpForce +
            Vector3.up * wallJumpUpForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(force, ForceMode.Impulse);
    }
}