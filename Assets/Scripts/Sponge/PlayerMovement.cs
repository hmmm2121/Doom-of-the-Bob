namespace Sponge
{
    ﻿using UnityEngine;
    using TMPro;

    public class PlayerMovement : MonoBehaviour
    {
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
        public float dashMomentumWindow = 0.6f;

        [Header("Crouch")]
        public float crouchYScale = 0.5f;
        public KeyCode crouchKey = KeyCode.LeftControl;

        [Header("Slam")]
        public float slamInstantDownVelocity = 40f;
        public float slamAOERadius = 4f;
        public LayerMask slamHitMask;
        public float slamDirectHitRadius = 1.2f;
        public float shockwaveLaunchBase = 12f;
        public float shockwaveLaunchPerSecond = 8f;
        public float shockwaveLaunchMax = 40f;
        public float slamBounceBaseForce = 14f;
        public float slamBounceForcePerSecond = 6f;
        public float slamBounceMaxForce = 32f;
        public float slamBounceWindow = 0.18f;
        [Range(0f, 1f)]
        public float slamLandingMomentumRetain = 0.3f;
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

        WallRide wallRide;

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

        public void RestoreDoubleJump() => hasDoubleJump = true;

        public bool IsSlamming => isSlamming;

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

        void GatherInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            moveDirection = orientation.forward * verticalInput
                            + orientation.right * horizontalInput;

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

        void ApplyDrag()
        {
            if (isDashing) rb.linearDamping = 0f;
            else if (grounded) rb.linearDamping = groundDrag;
            else rb.linearDamping = 0f;
        }

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

        void HandleJump()
        {
            if (isSlamming) return;

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

            jumpRequested = false;
            doubleJumpRequested = false;
        }

        void ResetJump() => readyToJump = true;

        void BetterGravity()
        {
            if (isSlamming) return;
            if (wallRide != null && wallRide.IsWallRunning) return;

            if (rb.linearVelocity.y < 0)
                rb.AddForce(Vector3.down * fallMultiplier, ForceMode.Acceleration);
            else if (rb.linearVelocity.y > 0 && !Input.GetKey(jumpKey))
                rb.AddForce(Vector3.down * lowJumpMultiplier, ForceMode.Acceleration);
        }

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

        // slam

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

            float scaleChange = startYScale - crouchYScale;
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale,
                                               transform.localScale.z);
            transform.position += Vector3.up * (scaleChange * 0.5f);

            OnSlamStart();
        }

        void TriggerSlamImpact()
        {
            isSlamming = false;

            Vector3 impactPoint;
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit,
                                playerHeight, whatIsGround))
            {
                impactPoint = groundHit.point;
            }
            else
            {
                impactPoint = transform.position - Vector3.up * (playerHeight * crouchYScale * 0.5f);
            }

            lastSlamGizmoPos = impactPoint;

            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x * slamLandingMomentumRetain,
                0f,
                rb.linearVelocity.z * slamLandingMomentumRetain
            );

            float scaleChange = startYScale - crouchYScale;
            transform.localScale = new Vector3(transform.localScale.x, startYScale,
                                               transform.localScale.z);
            transform.position += Vector3.up * (scaleChange * 0.5f);

            Collider[] directHits = Physics.OverlapSphere(impactPoint, slamDirectHitRadius, slamHitMask);
            foreach (Collider hit in directHits)
                OnSlamDirectHit(hit, impactPoint);

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

            slamBounceTimer = slamBounceWindow;
        }

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

        // dash

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

        void OnDrawGizmosSelected()
        {
            if (!drawSlamGizmo) return;

            Vector3 origin = lastSlamGizmoPos != Vector3.zero
                ? lastSlamGizmoPos
                : transform.position - Vector3.up * (playerHeight * 0.5f);

            // shockwave AOE
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
            Gizmos.DrawSphere(origin, slamAOERadius);

            // direct hit radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(origin, slamDirectHitRadius);
        }
    }
}
