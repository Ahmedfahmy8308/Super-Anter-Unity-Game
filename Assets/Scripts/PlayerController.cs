using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 70f;
    [SerializeField] private float deceleration = 90f;
    [SerializeField] private float airAcceleration = 50f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("Sprint")]
    [SerializeField] private float sprintMultiplier = 1.45f;

    [Header("Jump (height 5, time 1)")]
    [SerializeField] private float maxJumpHeight = 5f;
    [SerializeField] private float maxJumpTime = 1f;
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallMultiplier = 2f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float jumpCutMultiplier = 0.45f;

    [Header("Screen Clamp")]
    [SerializeField] private bool clampToScreen = false;

    [Header("Wall Slide & Jump")]
    [SerializeField] private bool enableWallMechanics = true;
    [SerializeField] private Transform wallCheckPoint;
    [SerializeField] private float wallCheckDistance = 0.25f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallSlideSpeed = 2.5f;
    [SerializeField] private float wallJumpForceX = 12f;
    [SerializeField] private float wallJumpForceY = 16f;
    [SerializeField] private float wallJumpLockTime = 0.15f;
    [SerializeField] private float wallCoyoteTime = 0.08f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.18f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual Feedback")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Vector3 jumpStretchScale = new Vector3(0.92f, 1.12f, 1f);
    [SerializeField] private Vector3 landingSquashScale = new Vector3(1.18f, 0.82f, 1f);
    [SerializeField] private float squashInTime = 0.06f;
    [SerializeField] private float squashHoldTime = 0.04f;
    [SerializeField] private float squashOutTime = 0.12f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem jumpDustPrefab;
    [SerializeField] private ParticleSystem landingImpactPrefab;
    [SerializeField] private ParticleSystem wallSlideDustPrefab;
    [SerializeField] private Transform jumpEffectPoint;
    [SerializeField] private Transform landingEffectPoint;
    [SerializeField] private float landingImpactVelocityThreshold = 6f;
    [SerializeField] private float landingShakeForce = 0.45f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameData gameData;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int WallSlideHash = Animator.StringToHash("WallSlide");
    private static readonly int SprintHash = Animator.StringToHash("Sprint");

    private float moveInput;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpQueued;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpCutApplied;
    private bool inputEnabled = true;
    private bool facingRight = true;
    private float lastAirborneVelocityY;
    private Vector3 groundedVisualScale;
    private Coroutine squashRoutine;

    private bool isTouchingWall;
    private bool isWallSliding;
    private int wallDirection;
    private float wallCoyoteTimer;
    private float wallJumpLockTimer;
    private bool isSprinting;
    private ParticleSystem activeWallDust;

    public bool IsGrounded => isGrounded;
    public bool IsWallSliding => isWallSliding;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        visualRoot = spriteRenderer != null ? spriteRenderer.transform : transform;
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visualRoot == null) visualRoot = spriteRenderer != null ? spriteRenderer.transform : transform;

        groundedVisualScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

        if (gameData != null)
        {
            moveSpeed = gameData.moveSpeed;
            acceleration = gameData.acceleration;
            deceleration = gameData.deceleration;
            coyoteTime = gameData.coyoteTime;
            jumpBufferTime = gameData.jumpBufferTime;
        }

        jumpForce = (2f * maxJumpHeight) / (maxJumpTime / 2f);
        rb.freezeRotation = true;
        isGrounded = CheckGrounded();
        wasGrounded = isGrounded;
        coyoteTimer = isGrounded ? coyoteTime : 0f;
    }

    public void ApplyRuntimeData(GameDataRuntimeData data)
    {
        if (data == null || data.player == null) return;
        moveSpeed = data.player.moveSpeed;
        acceleration = data.player.acceleration;
        deceleration = data.player.deceleration;
        coyoteTime = data.player.coyoteTime;
        jumpBufferTime = data.player.jumpBufferTime;
        // Keep jump arc consistent with maxJumpHeight / maxJumpTime (JSON jumpForce often mismatches and feels "off").
        jumpForce = (2f * maxJumpHeight) / (maxJumpTime / 2f);
    }

    private void Update()
    {
        UpdateGroundedState();
        UpdateWallState();

        if (!inputEnabled)
        {
            UpdateAnimator();
            return;
        }

        moveInput = ReadHorizontalInput();
        isSprinting = IsSprintHeld() && Mathf.Abs(moveInput) > 0.01f;

        if (WasJumpPressed())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        if (wallJumpLockTimer > 0f)
            wallJumpLockTimer -= Time.deltaTime;

        if (Mathf.Abs(moveInput) > 0.01f && wallJumpLockTimer <= 0f)
            SetFacing(moveInput > 0f);

        QueueJumpIfPossible();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (!inputEnabled) return;

        if (!isGrounded)
            lastAirborneVelocityY = Mathf.Min(lastAirborneVelocityY, rb.linearVelocity.y);

        ApplyHorizontalMovement();
        ApplyJumpAndAirControl();
        ApplyWallSlide();
        ClampFallSpeed();
        ClampToScreen();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!inputEnabled)
        {
            moveInput = 0f;
            jumpQueued = false;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            isWallSliding = false;
        }
    }

    private void ApplyHorizontalMovement()
    {
        if (wallJumpLockTimer > 0f) return;

        float targetSpeed = moveInput * moveSpeed;
        if (isSprinting) targetSpeed *= sprintMultiplier;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f
            ? (isGrounded ? acceleration : airAcceleration)
            : deceleration;

        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    private void ApplyJumpAndAirControl()
    {
        if (jumpQueued)
        {
            if (isWallSliding || wallCoyoteTimer > 0f)
            {
                PerformWallJump();
            }
            else
            {
                PerformJump();
            }

            jumpQueued = false;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            wallCoyoteTimer = 0f;
            jumpCutApplied = false;
            return;
        }

        if (rb.linearVelocity.y < 0f)
        {
            float extraGravity = Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity += new Vector2(0f, extraGravity * rb.gravityScale);
        }
        else if (rb.linearVelocity.y > 0f && !IsJumpHeld())
        {
            if (!jumpCutApplied)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                jumpCutApplied = true;
            }

            float extraGravity = Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity += new Vector2(0f, extraGravity * rb.gravityScale);
        }
    }

    private void ApplyWallSlide()
    {
        if (!isWallSliding) return;

        if (rb.linearVelocity.y < -wallSlideSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
    }

    private void ClampFallSpeed()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    private void UpdateGroundedState()
    {
        wasGrounded = isGrounded;
        isGrounded = CheckGrounded();

        if (!wasGrounded && isGrounded)
            OnLanded();

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            wallCoyoteTimer = 0f;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
            coyoteTimer = Mathf.Max(coyoteTimer, 0f);
        }
    }

    private void UpdateWallState()
    {
        if (!enableWallMechanics || isGrounded)
        {
            SetWallSliding(false);
            wallCoyoteTimer = 0f;
            return;
        }

        bool wasTouchingWall = isTouchingWall;
        isTouchingWall = CheckWall(out int dir);
        wallDirection = dir;

        bool shouldSlide = isTouchingWall && !isGrounded && rb.linearVelocity.y <= 0f
                           && Mathf.Abs(moveInput) > 0.01f
                           && ((moveInput > 0f && wallDirection > 0) || (moveInput < 0f && wallDirection < 0));

        SetWallSliding(shouldSlide);

        if (wasTouchingWall && !isTouchingWall && !isGrounded)
            wallCoyoteTimer = wallCoyoteTime;
        else
            wallCoyoteTimer -= Time.deltaTime;

        wallCoyoteTimer = Mathf.Max(wallCoyoteTimer, 0f);
    }

    private void SetWallSliding(bool sliding)
    {
        if (isWallSliding == sliding) return;
        isWallSliding = sliding;

        if (sliding)
        {
            if (wallSlideDustPrefab != null && activeWallDust == null)
            {
                Vector3 pos = wallCheckPoint != null ? wallCheckPoint.position : transform.position;
                activeWallDust = Instantiate(wallSlideDustPrefab, pos, Quaternion.identity, transform);
                activeWallDust.Play();
            }
        }
        else
        {
            if (activeWallDust != null)
            {
                activeWallDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(activeWallDust.gameObject, 1f);
                activeWallDust = null;
            }
        }
    }

    private LayerMask EffectiveGroundMask =>
        groundLayer.value != 0 ? groundLayer : (LayerMask)(1 << 0);

    private bool CheckGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, EffectiveGroundMask);
    }

    private bool CheckWall(out int direction)
    {
        direction = 0;
        if (wallCheckPoint == null && groundCheck == null) return false;

        Vector3 origin = wallCheckPoint != null ? wallCheckPoint.position : transform.position;
        LayerMask mask = wallLayer.value != 0 ? wallLayer : EffectiveGroundMask;

        RaycastHit2D rightHit = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, mask);
        RaycastHit2D leftHit = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, mask);

        if (rightHit.collider != null) { direction = 1; return true; }
        if (leftHit.collider != null) { direction = -1; return true; }
        return false;
    }

    private void QueueJumpIfPossible()
    {
        if (jumpBufferTimer <= 0f) return;

        bool canNormalJump = coyoteTimer > 0f;
        bool canWallJump = enableWallMechanics && (isWallSliding || wallCoyoteTimer > 0f);

        if (canNormalJump || canWallJump)
        {
            jumpQueued = true;
            jumpBufferTimer = 0f;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(GroundedHash, isGrounded);
        animator.SetFloat(VerticalSpeedHash, rb.linearVelocity.y);
        animator.SetBool(WallSlideHash, isWallSliding);
        animator.SetBool(SprintHash, isSprinting && isGrounded);
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastAirborneVelocityY = jumpForce;
        AudioManager.Instance?.PlayJump();
        SpawnVfx(jumpDustPrefab, jumpEffectPoint);
        PlaySquashStretch(jumpStretchScale, 0.04f, 0.05f, 0.08f);
    }

    private void PerformWallJump()
    {
        int jumpDir = isWallSliding ? -wallDirection : (wallCoyoteTimer > 0f ? -wallDirection : (facingRight ? -1 : 1));
        rb.linearVelocity = new Vector2(jumpDir * wallJumpForceX, wallJumpForceY);
        wallJumpLockTimer = wallJumpLockTime;
        SetFacing(jumpDir > 0);
        SetWallSliding(false);
        lastAirborneVelocityY = wallJumpForceY;
        AudioManager.Instance?.PlayJump();
        SpawnVfx(jumpDustPrefab, jumpEffectPoint);
        PlaySquashStretch(jumpStretchScale, 0.04f, 0.05f, 0.08f);
    }

    private void OnLanded()
    {
        jumpCutApplied = false;
        SetWallSliding(false);

        if (lastAirborneVelocityY > -landingImpactVelocityThreshold)
        {
            lastAirborneVelocityY = 0f;
            return;
        }

        SpawnVfx(landingImpactPrefab, landingEffectPoint);
        PlaySquashStretch(landingSquashScale, squashInTime, squashHoldTime, squashOutTime);
        GameManager.Instance?.TriggerCameraImpulse(landingShakeForce);
        lastAirborneVelocityY = 0f;
    }

    private void PlaySquashStretch(Vector3 targetScale, float inTime, float holdTime, float outTime)
    {
        if (visualRoot == null) return;
        if (squashRoutine != null) StopCoroutine(squashRoutine);
        squashRoutine = StartCoroutine(SquashStretchRoutine(targetScale, inTime, holdTime, outTime));
    }

    private IEnumerator SquashStretchRoutine(Vector3 targetScale, float inTime, float holdTime, float outTime)
    {
        yield return ScaleTo(targetScale, inTime);
        if (holdTime > 0f) yield return new WaitForSecondsRealtime(holdTime);
        yield return ScaleTo(groundedVisualScale, outTime);
        visualRoot.localScale = groundedVisualScale;
        squashRoutine = null;
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        if (visualRoot == null) yield break;
        Vector3 startScale = visualRoot.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            visualRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }
        visualRoot.localScale = targetScale;
    }

    private void SpawnVfx(ParticleSystem prefab, Transform point)
    {
        if (prefab == null) return;
        Vector3 position = point != null ? point.position : transform.position;
        ParticleSystem effect = Instantiate(prefab, position, Quaternion.identity);
        effect.Play();
        float lifespan = effect.main.duration + effect.main.startLifetime.constantMax + 0.25f;
        Destroy(effect.gameObject, lifespan);
    }

    public void ResetAirborneVelocity()
    {
        lastAirborneVelocityY = 0f;
        jumpCutApplied = false;
    }

    private void ClampToScreen()
    {
        if (!clampToScreen) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        // ScreenToWorldPoint needs distance along the camera forward axis; z=0 snaps to the lens and breaks bounds.
        float depth = Mathf.Abs(cam.transform.position.z - transform.position.z);
        if (depth < 0.01f) depth = 10f;

        Vector3 bl = cam.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 br = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0f, depth));
        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, bl.x + 0.5f, br.x - 0.5f);
        rb.position = pos;
    }

    private void SetFacing(bool faceRight)
    {
        if (facingRight == faceRight) return;
        facingRight = faceRight;
        transform.eulerAngles = faceRight ? Vector3.zero : new Vector3(0f, 180f, 0f);
    }

    public void ForceFlip(bool faceRight)
    {
        facingRight = faceRight;
        transform.eulerAngles = faceRight ? Vector3.zero : new Vector3(0f, 180f, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheckPoint != null || groundCheck != null)
        {
            Gizmos.color = Color.green;
            Vector3 origin = wallCheckPoint != null ? wallCheckPoint.position : transform.position;
            Gizmos.DrawLine(origin, origin + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(origin, origin + Vector3.left * wallCheckDistance);
        }
    }

    private static float ReadHorizontalInput()
    {
        float x = 0f;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        }
        if (Mathf.Abs(x) < 0.01f && Gamepad.current != null)
            x = Gamepad.current.leftStick.ReadValue().x;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Mathf.Abs(x) < 0.01f)
            x = Input.GetAxisRaw("Horizontal");
#endif
        return Mathf.Clamp(x, -1f, 1f);
    }

    private static bool WasJumpPressed()
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        pressed = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame))
                  || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = pressed || Input.GetButtonDown("Jump");
#endif
        return pressed;
    }

    private static bool IsJumpHeld()
    {
        bool held = false;
#if ENABLE_INPUT_SYSTEM
        held = (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed))
               || (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        held = held || Input.GetButton("Jump");
#endif
        return held;
    }

    private static bool IsSprintHeld()
    {
        bool held = false;
#if ENABLE_INPUT_SYSTEM
        held = (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
               || (Gamepad.current != null && Gamepad.current.buttonWest.isPressed);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        held = held || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        return held;
    }
}
