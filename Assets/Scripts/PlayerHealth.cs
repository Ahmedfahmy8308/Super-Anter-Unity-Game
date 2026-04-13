using System.Collections;
using UnityEngine;

public enum PowerUpState { Small, Big, Star }

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Power-Up")]
    [SerializeField] private PowerUpState currentState = PowerUpState.Small;
    [SerializeField] private Vector3 bigScale = new Vector3(1f, 1.5f, 1f);
    [SerializeField] private Vector3 smallScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float growDuration = 0.5f;
    [SerializeField] private float shrinkDuration = 0.3f;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 2f;
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private float starDuration = 10f;

    [Header("Death Feedback")]
    [SerializeField] private ParticleSystem deathExplosionPrefab;
    [SerializeField] private float deathExplosionDestroyDelay = 2f;
    [SerializeField] private float deathKnockbackForce = 9f;
    [SerializeField] private float deathKnockbackUpwardBias = 0.35f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform visualRoot;

    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int BigHash = Animator.StringToHash("Big");
    private static readonly int StarHash = Animator.StringToHash("Star");

    private bool isDead;
    private bool isInvincible;
    private Coroutine invincibilityRoutine;
    private Coroutine starRoutine;
    private Coroutine scaleRoutine;

    public bool IsDead => isDead;
    public bool IsInvincible => isInvincible;
    public PowerUpState CurrentPowerUp => currentState;

    private void Reset()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visualRoot == null) visualRoot = spriteRenderer != null ? spriteRenderer.transform : transform;
    }

    public void TakeDamage(Vector3 damageSource)
    {
        if (isDead || isInvincible) return;

        if (currentState == PowerUpState.Star) return;

        if (currentState == PowerUpState.Big)
        {
            Shrink();
            StartInvincibility(invincibilityDuration);
            AudioManager.Instance?.PlayPowerDown();
            return;
        }

        Vector2 knockbackDir = ((Vector2)transform.position - (Vector2)damageSource).normalized;
        knockbackDir = (knockbackDir + Vector2.up * deathKnockbackUpwardBias).normalized;
        Die(knockbackDir, damageSource);
    }

    public void Die()
    {
        if (isDead) return;
        Die(Vector2.up, transform.position);
    }

    public void Die(Vector2 knockbackDirection, Vector3 impactPosition)
    {
        if (isDead) return;
        isDead = true;

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }
        if (starRoutine != null)
        {
            StopCoroutine(starRoutine);
            starRoutine = null;
        }

        RestoreSpriteVisibility();

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(DeathHash);

        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
            playerController.ResetAirborneVelocity();
        }

        Vector2 finalDirection = knockbackDirection.sqrMagnitude > 0.001f ? knockbackDirection.normalized : Vector2.up;
        finalDirection = (finalDirection + Vector2.up * deathKnockbackUpwardBias).normalized;
        GameManager.Instance?.HandlePlayerDeath(this, finalDirection * deathKnockbackForce, impactPosition);
    }

    public void PowerUp(PowerUpState newState)
    {
        if (isDead) return;

        switch (newState)
        {
            case PowerUpState.Big:
                if (currentState == PowerUpState.Small)
                    Grow();
                break;
            case PowerUpState.Star:
                ActivateStar();
                break;
        }
    }

    private void Grow()
    {
        currentState = PowerUpState.Big;
        SetAnimatorBool(BigHash, true);
        AudioManager.Instance?.PlayPowerUp();

        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleRoutine(bigScale, growDuration));
    }

    private void Shrink()
    {
        currentState = PowerUpState.Small;
        SetAnimatorBool(BigHash, false);

        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleRoutine(smallScale, shrinkDuration));
    }

    private void ActivateStar()
    {
        currentState = PowerUpState.Star;
        isInvincible = true;
        SetAnimatorBool(StarHash, true);
        AudioManager.Instance?.PlayStarMusic();

        if (starRoutine != null) StopCoroutine(starRoutine);
        starRoutine = StartCoroutine(StarRoutine());
    }

    private IEnumerator StarRoutine()
    {
        float elapsed = 0f;
        while (elapsed < starDuration)
        {
            elapsed += Time.deltaTime;

            if (spriteRenderer != null && Time.frameCount % 4 == 0)
                spriteRenderer.color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);

            yield return null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        isInvincible = false;
        if (currentState == PowerUpState.Star)
            currentState = PowerUpState.Small;

        SetAnimatorBool(StarHash, false);
        AudioManager.Instance?.RestoreNormalMusic();
        starRoutine = null;
    }

    private void StartInvincibility(float duration)
    {
        if (invincibilityRoutine != null)
            StopCoroutine(invincibilityRoutine);
        invincibilityRoutine = StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(flashInterval);
        }

        RestoreSpriteVisibility();
        isInvincible = false;
        invincibilityRoutine = null;
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale, float duration)
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (Time.frameCount % 4 == 0)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return null;
        }

        spriteRenderer.enabled = true;

        if (visualRoot != null)
            visualRoot.localScale = targetScale;

        scaleRoutine = null;
    }

    private void RestoreSpriteVisibility()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    private void SetAnimatorBool(int hash, bool value)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetBool(hash, value);
    }

    public void PrepareForDeath()
    {
        if (playerController != null)
            playerController.SetInputEnabled(false);

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ApplyDeathKnockback(Vector2 velocity)
    {
        if (rb == null) return;
        rb.simulated = true;
        rb.linearVelocity = velocity;
    }

    public void FreezeForRespawn()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    public void SpawnDeathExplosion(Vector3 position)
    {
        if (deathExplosionPrefab == null) return;
        ParticleSystem explosion = Instantiate(deathExplosionPrefab, position, Quaternion.identity);
        explosion.Play();
        Destroy(explosion.gameObject, deathExplosionDestroyDelay);
    }

    public void ResetState()
    {
        isDead = false;
        isInvincible = false;
        currentState = PowerUpState.Small;

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }
        if (starRoutine != null)
        {
            StopCoroutine(starRoutine);
            starRoutine = null;
        }
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        RestoreSpriteVisibility();
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        if (visualRoot != null) visualRoot.localScale = smallScale;
        SetAnimatorBool(BigHash, false);
        SetAnimatorBool(StarHash, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        EnemyAI enemy = collision.collider.GetComponentInParent<EnemyAI>();
        if (enemy == null || enemy.IsDead) return;

        PlayerController player = GetComponent<PlayerController>();
        if (player == null) return;

        Vector2 contactNormal = Vector2.up;
        if (collision.contactCount > 0)
            contactNormal = collision.GetContact(0).normal;

        if (currentState == PowerUpState.Star)
        {
            enemy.DieFromShell();
            return;
        }

        if (enemy.TryStomp(player, this, contactNormal))
            return;

        if (enemy.IsShell && !enemy.IsShellMoving)
        {
            float kickDir = transform.position.x < enemy.transform.position.x ? 1f : -1f;
            enemy.KickShell(kickDir);
            return;
        }

        TakeDamage(enemy.transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.GetComponent<DeathZone>() != null)
        {
            Die(Vector2.up, transform.position);
            return;
        }
    }
}
