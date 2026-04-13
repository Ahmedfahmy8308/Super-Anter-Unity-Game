using UnityEngine;

public class KoopaEnemy : MonoBehaviour
{
    public Sprite shellSprite;
    public float shellSpeed = 12f;
    [SerializeField] private int scoreValue = 100;

    private bool shelled;
    private bool pushed;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (shelled) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
        if (player == null) return;

        bool stomp = IsStompCollision(collision);
        if (!stomp && collision.transform.DotTest(transform, Vector2.down))
            stomp = true;

        if (stomp)
        {
            EnterShell();
            AudioManager.Instance?.PlayEnemyStomp();
            GameManager.Instance?.AddStompScore(scoreValue, transform.position);

            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 10f);
        }
        else
        {
            if (!player.IsDead)
                player.Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (shelled && other.CompareTag("Player"))
        {
            if (!pushed)
            {
                Vector2 dir = new Vector2(transform.position.x - other.transform.position.x, 0f);
                PushShell(dir);
                AudioManager.Instance?.PlayKick();
            }
            else
            {
                PlayerHealth player = other.GetComponent<PlayerHealth>();
                if (player != null && !player.IsDead)
                    player.Die();
            }
        }
        else if (!shelled && other.gameObject.layer == LayerMask.NameToLayer("Shell"))
        {
            Hit();
        }
    }

    private void EnterShell()
    {
        shelled = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && shellSprite != null) sr.sprite = shellSprite;

        AnimatedSprite anim = GetComponent<AnimatedSprite>();
        if (anim != null) anim.enabled = false;

        MarioEntityMovement movement = GetComponent<MarioEntityMovement>();
        if (movement != null) movement.enabled = false;
    }

    private void PushShell(Vector2 direction)
    {
        pushed = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

        MarioEntityMovement movement = GetComponent<MarioEntityMovement>();
        if (movement != null)
        {
            movement.direction = direction.normalized;
            movement.speed = shellSpeed;
            movement.enabled = true;
        }

        gameObject.layer = LayerMask.NameToLayer("Shell");
    }

    private void Hit()
    {
        AnimatedSprite anim = GetComponent<AnimatedSprite>();
        if (anim != null) anim.enabled = false;

        DeathAnimation da = GetComponent<DeathAnimation>();
        if (da != null)
            da.enabled = true;
        else
            Destroy(gameObject);

        GameManager.Instance?.AddScore(scoreValue, transform.position);
        Destroy(gameObject, 3f);
    }

    private void OnBecameInvisible()
    {
        if (pushed)
            Destroy(gameObject);
    }

    private static bool IsStompCollision(Collision2D collision)
    {
        if (collision.contactCount == 0) return false;
        return collision.GetContact(0).normal.y > 0.35f;
    }
}
