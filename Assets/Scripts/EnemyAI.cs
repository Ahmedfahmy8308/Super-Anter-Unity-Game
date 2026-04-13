using System.Collections;
using UnityEngine;

public enum EnemyType { Goomba, Koopa }

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Goomba;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitAtWaypoint = 0.35f;
    [SerializeField] private float arriveDistance = 0.05f;
    [SerializeField] private bool useCustomStartDirection = false;
    [SerializeField] private bool startMovingRight = true;

    [Header("Spawn Landing")]
    [SerializeField] private bool waitUntilGroundedBeforePatrol = true;
    [SerializeField] private float groundedProbeDistance = 0.2f;

    [Header("Fall Rescue")]
    [SerializeField] private bool enableFallRescue = true;
    [SerializeField] private float rescueIfBelowY = -10f;
    [SerializeField] private float maxAirTimeBeforeRescue = 1.75f;
    [SerializeField] private float rescueRayStartY = 24f;
    [SerializeField] private float rescueRayDistance = 80f;

    [Header("Stomp Detection")]
    [SerializeField] private float stompDotThreshold = 0.55f;
    [SerializeField] private float stompBounceForce = 14f;
    [SerializeField] private int stompScoreValue = 100;

    [Header("Koopa Shell")]
    [SerializeField] private float shellSpeed = 12f;
    [SerializeField] private float shellKickDelay = 0.3f;

    [Header("Edge Detection")]
    [SerializeField] private bool detectEdges = true;
    [SerializeField] private Transform edgeCheckPoint;
    [SerializeField] private float edgeRayLength = 0.6f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Detection")]
    [SerializeField] private Transform wallCheckPoint;
    [SerializeField] private float wallRayLength = 0.3f;

    [Header("Barrier Constraint")]
    [SerializeField] private bool constrainToLevelBarriers = true;
    [SerializeField] private float barrierInset = 0.05f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D mainCollider;
    [SerializeField] private GameData gameData;

    [Header("Death Feedback")]
    [SerializeField] private ParticleSystem deathExplosionPrefab;
    [SerializeField] private float deathExplosionDestroyDelay = 1.5f;
    [SerializeField] private float flattenScaleY = 0.3f;
    [SerializeField] private float flattenDuration = 0.5f;

    private static readonly int WalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int FlatHash = Animator.StringToHash("Flat");
    private static readonly int ShellHash = Animator.StringToHash("Shell");

    private int currentWaypointIndex;
    private float waitTimer;
    private bool movingRight = true;
    private bool isDead;

    private bool isShell;
    private bool isShellMoving;
    private float shellKickTimer;
    private float shellDirection;
    private bool barrierBoundsSynced;
    private float leftBarrierLimitX;
    private float rightBarrierLimitX;
    private bool hasLandedOnce;
    private float airborneTimer;

    public bool IsDead => isDead;
    public bool IsShell => isShell;
    public bool IsShellMoving => isShellMoving;
    public EnemyType Type => enemyType;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        mainCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (mainCollider == null) mainCollider = GetComponent<Collider2D>();

        if (gameData != null)
        {
            moveSpeed = gameData.enemyMoveSpeed;
            waitAtWaypoint = gameData.enemyWaitAtWaypoint;
        }

        InitializeRuntimeState();
    }

    public void ApplyRuntimeData(GameDataRuntimeData data)
    {
        if (data == null || data.enemy == null) return;
        moveSpeed = data.enemy.moveSpeed;
        waitAtWaypoint = data.enemy.waitAtWaypoint;
    }

    private void OnEnable()
    {
        InitializeRuntimeState();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        UpdateAirborneTimer();

        if (enableFallRescue)
            TryRescueFromEndlessFall();

        if (!barrierBoundsSynced)
            TrySyncBarrierBounds();

        if (waitUntilGroundedBeforePatrol && !hasLandedOnce)
        {
            if (IsStandingOnGround())
            {
                hasLandedOnce = true;
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                UpdateAnimator(false);
                ApplyBarrierConstraintAndTurn();
                return;
            }
        }

        if (isShell)
        {
            HandleShellMovement();
            ApplyBarrierConstraintAndTurn();
            return;
        }

        if (waypoints == null || waypoints.Length < 2)
        {
            HandleFreeRoamPatrol();
            ApplyBarrierConstraintAndTurn();
            return;
        }

        HandleWaypointPatrol();
        ApplyBarrierConstraintAndTurn();
    }

    private void HandleWaypointPatrol()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            UpdateAnimator(false);
            return;
        }

        if (ShouldTurnAround())
        {
            waitTimer = Mathf.Max(waitAtWaypoint * 0.5f, 0.05f);
            AdvanceWaypoint();
            UpdateAnimator(false);
            return;
        }

        Transform target = waypoints[currentWaypointIndex];
        if (target == null)
        {
            AdvanceWaypoint();
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(rb.position, target.position, moveSpeed * Time.fixedDeltaTime);
        Vector2 delta = nextPosition - rb.position;

        if (Mathf.Abs(delta.x) > 0.001f)
            SetFacing(delta.x > 0f);

        rb.MovePosition(nextPosition);
        UpdateAnimator(true);

        if (Vector2.Distance(rb.position, target.position) <= arriveDistance)
        {
            waitTimer = waitAtWaypoint;
            AdvanceWaypoint();
        }
    }

    private void HandleFreeRoamPatrol()
    {
        if (constrainToLevelBarriers && barrierBoundsSynced)
        {
            if (movingRight && rb.position.x >= rightBarrierLimitX - 0.02f)
                SetFacing(false);
            else if (!movingRight && rb.position.x <= leftBarrierLimitX + 0.02f)
                SetFacing(true);
        }

        if (ShouldTurnAround())
        {
            movingRight = !movingRight;
            SetFacing(movingRight);
        }

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        UpdateAnimator(true);
    }

    private void HandleShellMovement()
    {
        if (!isShellMoving)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (ShouldTurnAround())
            shellDirection = -shellDirection;

        rb.linearVelocity = new Vector2(shellDirection * shellSpeed, rb.linearVelocity.y);
    }

    private bool ShouldTurnAround()
    {
        if (detectEdges && !DetectGround())
            return true;

        if (DetectWall())
            return true;

        return false;
    }

    private bool DetectGround()
    {
        Vector3 origin = edgeCheckPoint != null ? edgeCheckPoint.position : transform.position;
        return RayHitsGround(origin, Vector2.down, edgeRayLength);
    }

    private bool DetectWall()
    {
        Vector2 direction = movingRight ? Vector2.right : Vector2.left;
        if (isShell && isShellMoving)
            direction = shellDirection > 0 ? Vector2.right : Vector2.left;

        Vector3 origin = wallCheckPoint != null ? wallCheckPoint.position : transform.position;
        return RayHitsGround(origin, direction, wallRayLength);
    }

    public bool TryStomp(PlayerController player, PlayerHealth playerHealth, Vector2 contactNormal)
    {
        if (isDead) return false;

        float dot = Vector2.Dot(contactNormal, Vector2.up);
        bool isStompingFromAbove = dot >= stompDotThreshold;

        if (!isStompingFromAbove)
            return false;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null && playerRb.linearVelocity.y > 0.1f)
            return false;

        OnStomped(player);
        return true;
    }

    private void OnStomped(PlayerController player)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounceForce);

        AudioManager.Instance?.PlayEnemyStomp();
        GameManager.Instance?.AddScore(stompScoreValue, transform.position);

        switch (enemyType)
        {
            case EnemyType.Goomba:
                StartCoroutine(GoombaFlattenRoutine());
                break;
            case EnemyType.Koopa:
                if (!isShell)
                    EnterShell();
                else if (!isShellMoving)
                    KickShell(player.transform.position.x < transform.position.x ? 1f : -1f);
                else
                    StopShell();
                break;
        }
    }

    private IEnumerator GoombaFlattenRoutine()
    {
        isDead = true;
        UpdateAnimator(false);
        SetAnimatorBool(FlatHash, true);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (mainCollider != null)
            mainCollider.enabled = false;

        Vector3 originalScale = transform.localScale;
        Vector3 flatScale = new Vector3(originalScale.x * 1.2f, originalScale.y * flattenScaleY, originalScale.z);

        float elapsed = 0f;
        float squashTime = 0.08f;
        while (elapsed < squashTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / squashTime);
            transform.localScale = Vector3.Lerp(originalScale, flatScale, t);
            yield return null;
        }
        transform.localScale = flatScale;

        yield return new WaitForSeconds(flattenDuration);
        Destroy(gameObject);
    }

    private void EnterShell()
    {
        isShell = true;
        isShellMoving = false;
        shellKickTimer = shellKickDelay;
        UpdateAnimator(false);
        SetAnimatorBool(ShellHash, true);

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        BoxCollider2D box = mainCollider as BoxCollider2D;
        if (box != null)
        {
            box.size = new Vector2(box.size.x, box.size.y * 0.6f);
            box.offset = new Vector2(box.offset.x, box.offset.y - box.size.y * 0.15f);
        }
    }

    public void KickShell(float direction)
    {
        if (!isShell) return;
        isShellMoving = true;
        shellDirection = Mathf.Sign(direction);
        AudioManager.Instance?.PlayEnemyStomp();
    }

    private void StopShell()
    {
        isShellMoving = false;
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void DieFromShell()
    {
        if (isDead) return;
        isDead = true;
        AudioManager.Instance?.PlayEnemyStomp();
        GameManager.Instance?.AddScore(stompScoreValue, transform.position);

        if (spriteRenderer != null)
            spriteRenderer.flipY = true;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = new Vector2(0f, 8f);
            rb.gravityScale = 3f;
        }

        if (mainCollider != null)
            mainCollider.enabled = false;

        Destroy(gameObject, 2f);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        UpdateAnimator(false);

        if (deathExplosionPrefab != null)
        {
            ParticleSystem explosion = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
            explosion.Play();
            Destroy(explosion.gameObject, deathExplosionDestroyDelay);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Destroy(gameObject, deathExplosionDestroyDelay);
    }

    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
    }

    private void AdvanceWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private void SetFacing(bool faceRight)
    {
        movingRight = faceRight;
        if (spriteRenderer != null)
            spriteRenderer.flipX = !movingRight;
    }

    private void UpdateAnimator(bool walking)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetBool(WalkingHash, walking);
    }

    private void SetAnimatorBool(int hash, bool value)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetBool(hash, value);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        TryReverseOnObstacleCollision(collision);

        if (isShell && isShellMoving)
        {
            EnemyAI otherEnemy = collision.collider.GetComponentInParent<EnemyAI>();
            if (otherEnemy != null && otherEnemy != this && !otherEnemy.IsDead)
            {
                otherEnemy.DieFromShell();
                return;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        TryReverseOnObstacleCollision(collision);
    }

    private void TryReverseOnObstacleCollision(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
            return;

        // Ignore player collisions so stomp/side-hit interactions stay consistent.
        if (collision.collider.GetComponentInParent<PlayerController>() != null)
            return;

        if (collision.contactCount <= 0)
            return;

        bool hasSideContact = false;
        float sideNormalX = 0f;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (Mathf.Abs(contact.normal.x) >= 0.25f)
            {
                hasSideContact = true;
                sideNormalX = contact.normal.x;
                break;
            }
        }

        if (!hasSideContact)
            return;

        bool obstacleOnRight = sideNormalX < 0f;

        if (isShell && isShellMoving)
        {
            shellDirection = obstacleOnRight ? -1f : 1f;
            return;
        }

        movingRight = !obstacleOnRight;
        SetFacing(movingRight);
    }

    private void TrySyncBarrierBounds()
    {
        if (!constrainToLevelBarriers)
            return;

        BoxCollider2D[] colliders = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Exclude);

        bool foundLeft = false;
        bool foundRight = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            BoxCollider2D c = colliders[i];
            if (c == null) continue;

            if (c.name == "LeftBarrier")
            {
                leftBarrierLimitX = c.bounds.max.x + barrierInset;
                foundLeft = true;
            }
            else if (c.name == "RightBarrier")
            {
                rightBarrierLimitX = c.bounds.min.x - barrierInset;
                foundRight = true;
            }
        }

        barrierBoundsSynced = foundLeft && foundRight && leftBarrierLimitX < rightBarrierLimitX;
    }

    private void InitializeRuntimeState()
    {
        if (rb == null)
            return;

        if (rb.bodyType != RigidbodyType2D.Dynamic)
            rb.bodyType = RigidbodyType2D.Dynamic;

        if (rb.gravityScale <= 0f)
            rb.gravityScale = 3f;

        if (mainCollider != null)
            mainCollider.isTrigger = false;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.WakeUp();

        hasLandedOnce = !waitUntilGroundedBeforePatrol;
        airborneTimer = 0f;
        bool initialRight = useCustomStartDirection ? startMovingRight : true;
        movingRight = initialRight;
        if (spriteRenderer != null)
            spriteRenderer.flipX = !movingRight;
    }

    private void UpdateAirborneTimer()
    {
        if (IsStandingOnGround())
            airborneTimer = 0f;
        else
            airborneTimer += Time.fixedDeltaTime;
    }

    private void TryRescueFromEndlessFall()
    {
        bool belowAllowedWorld = rb.position.y < rescueIfBelowY;
        bool airborneTooLong = airborneTimer > maxAirTimeBeforeRescue && rb.linearVelocity.y <= 0f;
        if (!belowAllowedWorld && !airborneTooLong)
            return;

        if (!TryFindGroundYAtX(rb.position.x, out float groundY))
            return;

        float halfHeight = 0.5f;
        if (mainCollider != null)
            halfHeight = Mathf.Max(0.1f, mainCollider.bounds.extents.y);

        rb.position = new Vector2(rb.position.x, groundY + halfHeight + 0.03f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        hasLandedOnce = true;
        airborneTimer = 0f;
    }

    private bool TryFindGroundYAtX(float worldX, out float groundY)
    {
        groundY = 0f;
        LayerMask mask = groundLayer.value != 0 ? groundLayer : Physics2D.AllLayers;
        Vector3 origin = new Vector3(worldX, rescueRayStartY, transform.position.z);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, rescueRayDistance, mask);

        bool found = false;
        float highestY = float.MinValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger)
                continue;

            if (mainCollider != null && c == mainCollider)
                continue;

            if (c.transform.IsChildOf(transform))
                continue;

            if (c.name == "LeftBarrier" || c.name == "RightBarrier")
                continue;

            if (hits[i].point.y > highestY)
            {
                highestY = hits[i].point.y;
                found = true;
            }
        }

        if (!found)
            return false;

        groundY = highestY;
        return true;
    }

    private void ApplyBarrierConstraintAndTurn()
    {
        if (!constrainToLevelBarriers || !barrierBoundsSynced)
            return;

        Vector2 pos = rb.position;
        if (pos.x < leftBarrierLimitX)
        {
            pos.x = leftBarrierLimitX;
            rb.position = pos;

            if (isShell && isShellMoving)
            {
                shellDirection = 1f;
                rb.linearVelocity = new Vector2(shellSpeed, rb.linearVelocity.y);
            }
            else
            {
                if (waypoints != null && waypoints.Length > 1)
                    AdvanceWaypoint();
                movingRight = true;
                SetFacing(true);
                rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
            }
        }
        else if (pos.x > rightBarrierLimitX)
        {
            pos.x = rightBarrierLimitX;
            rb.position = pos;

            if (isShell && isShellMoving)
            {
                shellDirection = -1f;
                rb.linearVelocity = new Vector2(-shellSpeed, rb.linearVelocity.y);
            }
            else
            {
                if (waypoints != null && waypoints.Length > 1)
                    AdvanceWaypoint();
                movingRight = false;
                SetFacing(false);
                rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y);
            }
        }
    }

    private bool IsStandingOnGround()
    {
        if (mainCollider == null)
            return RayHitsGround(transform.position, Vector2.down, groundedProbeDistance);

        Bounds b = mainCollider.bounds;
        float y = b.min.y + 0.02f;
        Vector3 center = new Vector3(b.center.x, y, transform.position.z);
        Vector3 left = new Vector3(b.min.x + 0.05f, y, transform.position.z);
        Vector3 right = new Vector3(b.max.x - 0.05f, y, transform.position.z);

        return RayHitsGround(center, Vector2.down, groundedProbeDistance)
            || RayHitsGround(left, Vector2.down, groundedProbeDistance)
            || RayHitsGround(right, Vector2.down, groundedProbeDistance);
    }

    private bool RayHitsGround(Vector3 origin, Vector2 direction, float distance)
    {
        LayerMask mask = groundLayer.value != 0 ? groundLayer : Physics2D.AllLayers;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger)
                continue;

            if (mainCollider != null && c == mainCollider)
                continue;

            if (c.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform current = waypoints[i];
                if (current == null) continue;
                Gizmos.DrawWireSphere(current.position, 0.12f);
                Transform next = waypoints[(i + 1) % waypoints.Length];
                if (next != null)
                    Gizmos.DrawLine(current.position, next.position);
            }
        }

        if (edgeCheckPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(edgeCheckPoint.position, edgeCheckPoint.position + Vector3.down * edgeRayLength);
        }

        if (wallCheckPoint != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 dir = movingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheckPoint.position, wallCheckPoint.position + dir * wallRayLength);
        }
    }
}
