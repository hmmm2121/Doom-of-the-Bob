using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MomentumController : MonoBehaviour
{
    [Header("Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.08f;
    public float maxPitch = 85f;

    [Header("Ground Move")]
    public float groundAccel = 80f;
    public float groundSpeed = 8f;
    public float groundFriction = 8f;
    public float runMultiplier = 1.5f;

    [Header("Air Move (Quake/Source)")]
    public float airAccel = 60f;
    public float airWishSpeedCap = 1.5f;

    [Header("Jump / Bhop")]
    public float jumpHeight = 1.4f;
    public float gravity = -22f;
    public float bhopWindow = 0.12f;

    [Header("Crouch / Slide")]
    public KeyCode crouchKeyFallback = KeyCode.LeftControl;
    public float standHeight = 1.8f;
    public float crouchHeight = 1.0f;
    public float slideMinSpeed = 6f;
    public float slideFriction = 1.5f;
    public float slideDownAccel = 18f;
    public float slideHopBoost = 1.05f;

    [Header("Wall-run")]
    public LayerMask wallrunMask;
    public float wallCheckDist = 0.7f;
    public float wallrunGravity = -3f;
    public float wallrunMaxTime = 1.4f;
    public float wallrunMinForward = 4f;
    public float wallJumpUp = 7f;
    public float wallJumpOut = 7f;
    public float wallCameraTilt = 14f;

    [Header("Caps")]
    public float maxHorizontalSpeed = 30f;

    CharacterController cc;
    Vector3 velocity;          // full velocity (xz + y)
    float pitch;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    bool wasGroundedLastFrame;

    // slide
    bool sliding;
    float originalCenterY;

    // wallrun
    bool wallrunning;
    Vector3 wallNormal;
    Vector3 wallForward;
    float wallrunStartTime;
    int wallSide; // -1 left, +1 right
    float currentCamRoll;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        cc.height = standHeight;
        originalCenterY = cc.center.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (wallrunMask.value == 0)
            wallrunMask = 1 << LayerMask.NameToLayer("Wallrun");
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        HandleLook(mouse);

        float dt = Time.deltaTime;
        Vector2 wish = ReadWishInput(kb);
        Vector3 wishDir = transform.right * wish.x + transform.forward * wish.y;
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        bool jumpPressed = kb.spaceKey.wasPressedThisFrame;
        if (jumpPressed) lastJumpPressedTime = Time.time;

        bool crouchHeld = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
        bool grounded = cc.isGrounded;
        if (grounded) lastGroundedTime = Time.time;

        // Wall-run tick
        TickWallrun(grounded, wishDir, jumpPressed, dt);

        if (grounded && !wallrunning)
        {
            bool justLanded = !wasGroundedLastFrame;
            bool bhop = justLanded && (Time.time - lastJumpPressedTime) < bhopWindow;

            // Friction (skip on bhop/slide)
            if (!bhop && !sliding) ApplyFriction(groundFriction, dt);

            // Slide enter/exit
            UpdateSlide(crouchHeld, wishDir);

            // Ground accel
            float targetSpeed = (kb.leftShiftKey.isPressed ? groundSpeed * runMultiplier : groundSpeed);
            Accelerate(wishDir, targetSpeed, groundAccel, dt);

            // Slide downhill boost (raycast down for slope)
            if (sliding) ApplySlideSlope(dt);

            // Jump
            if (Time.time - lastJumpPressedTime < bhopWindow)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpPressedTime = -10f;
                if (sliding) ScaleHorizontal(slideHopBoost);
            }
            else if (velocity.y < 0f) velocity.y = -2f;
        }
        else if (!wallrunning)
        {
            // Air-strafe
            Accelerate(wishDir, airWishSpeedCap, airAccel, dt);
            velocity.y += gravity * dt;

            // Try start wall-run
            TryStartWallrun(wishDir);
        }

        ClampHorizontal();
        cc.Move(velocity * dt);

        if (kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        wasGroundedLastFrame = grounded;
    }

    void HandleLook(Mouse mouse)
    {
        if (mouse == null) return;
        Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, -maxPitch, maxPitch);

        // camera roll lerp for wallrun
        float targetRoll = wallrunning ? -wallSide * wallCameraTilt : 0f;
        currentCamRoll = Mathf.Lerp(currentCamRoll, targetRoll, Time.deltaTime * 8f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, currentCamRoll);
    }

    Vector2 ReadWishInput(Keyboard kb)
    {
        float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        return new Vector2(h, v);
    }

    void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
    {
        if (wishDir.sqrMagnitude < 0.0001f) return;
        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        float currentSpeed = Vector3.Dot(horiz, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;
        float accelSpeed = accel * dt * wishSpeed;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;
        velocity.x += wishDir.x * accelSpeed;
        velocity.z += wishDir.z * accelSpeed;
    }

    void ApplyFriction(float friction, float dt)
    {
        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horiz.magnitude;
        if (speed < 0.01f) { velocity.x = velocity.z = 0f; return; }
        float drop = speed * friction * dt;
        float newSpeed = Mathf.Max(speed - drop, 0f);
        float scale = newSpeed / speed;
        velocity.x *= scale;
        velocity.z *= scale;
    }

    void ScaleHorizontal(float k)
    {
        velocity.x *= k;
        velocity.z *= k;
    }

    void ClampHorizontal()
    {
        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        if (horiz.magnitude > maxHorizontalSpeed)
        {
            horiz = horiz.normalized * maxHorizontalSpeed;
            velocity.x = horiz.x;
            velocity.z = horiz.z;
        }
    }

    void UpdateSlide(bool crouchHeld, Vector3 wishDir)
    {
        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horiz.magnitude;
        bool wantSlide = crouchHeld && speed >= slideMinSpeed;

        if (wantSlide && !sliding)
        {
            sliding = true;
            cc.height = crouchHeight;
            cc.center = new Vector3(cc.center.x, originalCenterY - (standHeight - crouchHeight) * 0.5f, cc.center.z);
        }
        else if (!wantSlide && sliding)
        {
            sliding = false;
            cc.height = standHeight;
            cc.center = new Vector3(cc.center.x, originalCenterY, cc.center.z);
        }

        if (sliding) ApplyFriction(slideFriction, Time.deltaTime);
    }

    void ApplySlideSlope(float dt)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
        {
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
            float slopeFactor = 1f - Mathf.Clamp01(Vector3.Dot(hit.normal, Vector3.up));
            velocity.x += slopeDir.x * slideDownAccel * slopeFactor * dt;
            velocity.z += slopeDir.z * slideDownAccel * slopeFactor * dt;
        }
    }

    void TryStartWallrun(Vector3 wishDir)
    {
        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        if (horiz.magnitude < wallrunMinForward) return;
        if (Vector3.Dot(wishDir, transform.forward) < 0.3f) return;

        if (Physics.Raycast(transform.position, transform.right, out RaycastHit hitR, wallCheckDist, wallrunMask))
        { BeginWallrun(hitR.normal, +1); return; }
        if (Physics.Raycast(transform.position, -transform.right, out RaycastHit hitL, wallCheckDist, wallrunMask))
        { BeginWallrun(hitL.normal, -1); return; }
    }

    void BeginWallrun(Vector3 normal, int side)
    {
        wallrunning = true;
        wallNormal = normal;
        wallSide = side;
        wallrunStartTime = Time.time;
        // tangent along wall, forward direction
        Vector3 along = Vector3.Cross(normal, Vector3.up);
        if (Vector3.Dot(along, transform.forward) < 0f) along = -along;
        wallForward = along.normalized;
        velocity.y = 0f;
    }

    void TickWallrun(bool grounded, Vector3 wishDir, bool jumpPressed, float dt)
    {
        if (!wallrunning) return;
        if (grounded || Time.time - wallrunStartTime > wallrunMaxTime) { EndWallrun(); return; }

        // re-confirm wall still there
        Vector3 dir = wallSide > 0 ? transform.right : -transform.right;
        if (!Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDist + 0.2f, wallrunMask))
        { EndWallrun(); return; }
        wallNormal = hit.normal;
        Vector3 along = Vector3.Cross(wallNormal, Vector3.up);
        if (Vector3.Dot(along, wallForward) < 0f) along = -along;
        wallForward = along.normalized;

        // glue to wall + move along it
        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        float speed = Mathf.Max(horiz.magnitude, groundSpeed);
        velocity.x = wallForward.x * speed;
        velocity.z = wallForward.z * speed;
        velocity.y += wallrunGravity * dt;

        if (jumpPressed)
        {
            velocity = wallForward * speed + wallNormal * wallJumpOut + Vector3.up * wallJumpUp;
            EndWallrun();
            lastJumpPressedTime = -10f;
        }
    }

    void EndWallrun()
    {
        wallrunning = false;
    }

    public float HorizontalSpeed
    {
        get { var h = new Vector3(velocity.x, 0f, velocity.z); return h.magnitude; }
    }
}
