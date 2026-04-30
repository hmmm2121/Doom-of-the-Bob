using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;
    bool isCrouching;

    [Header("Jump Consistency")]
    public float coyoteTime = 0.15f;
    private float coyoteTimer;

    private bool jumpRequested;

    [Header("Gravity")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [HideInInspector] public float walkSpeed;
    [HideInInspector] public float sprintSpeed;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Crouch")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    [HideInInspector] public TextMeshProUGUI text_speed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;

        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);



        if (grounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (Input.GetKeyDown(jumpKey))
            jumpRequested = true;

        MyInput();
        SpeedControl();

        // handle drag
        if (grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();

        if (jumpRequested && readyToJump && coyoteTimer > 0f)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);

            jumpRequested = false;
        }

        HandleBetterGravity();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if (Input.GetKey(jumpKey))
        {
            jumpRequested = true;
        }

        if (Input.GetKeyDown(crouchKey))
        {
            isCrouching = true;
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        }
        if (Input.GetKeyUp(crouchKey))
        {
            isCrouching = false;
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        float currentSpeed = isCrouching ? moveSpeed * 0.5f : moveSpeed;

        // on ground
        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float currentSpeed = isCrouching ? moveSpeed * 0.5f : moveSpeed;

        // limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }

        text_speed.SetText("Speed: " + flatVel.magnitude);
    }

    private void HandleBetterGravity()
    {
        // fall
        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.down * fallMultiplier, ForceMode.Acceleration);
        }
        // going up but player released jump early
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(jumpKey))
        {
            rb.AddForce(Vector3.down * lowJumpMultiplier, ForceMode.Acceleration);
        }
    }

    private void Jump()
    {
        // reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }
}