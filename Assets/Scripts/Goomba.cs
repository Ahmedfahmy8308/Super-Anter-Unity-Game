using UnityEngine;

public class Goomba : MonoBehaviour
{
    public Sprite flatSprite;
    [SerializeField] private int scoreValue = 100;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
        if (player == null) return;

        bool stomp = IsStompCollision(collision);
        if (!stomp && collision.transform.DotTest(transform, Vector2.down))
            stomp = true;

        if (stomp)
        {
            Flatten();
            AudioManager.Instance?.PlayEnemyStomp();
            GameManager.Instance?.AddStompScore(scoreValue, transform.position);

            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                    playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 10f);
            }
        }
        else
        {
            if (!player.IsDead)
                player.Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Shell"))
            Hit();
    }

    private void Flatten()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        MarioEntityMovement movement = GetComponent<MarioEntityMovement>();
        if (movement != null) movement.enabled = false;

        AnimatedSprite anim = GetComponent<AnimatedSprite>();
        if (anim != null) anim.enabled = false;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && flatSprite != null) sr.sprite = flatSprite;

        Destroy(gameObject, 0.5f);
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

    private static bool IsStompCollision(Collision2D collision)
    {
        if (collision.contactCount == 0) return false;
        // Normal points out of the surface hit; landing on the enemy's top gives an upward normal.
        float ny = collision.GetContact(0).normal.y;
        return ny > 0.35f;
    }
}
