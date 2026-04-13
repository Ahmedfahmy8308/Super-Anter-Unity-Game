using System.Collections;
using UnityEngine;

public class Flagpole : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private float topY = 5f;
    [SerializeField] private float bottomY = 0f;
    [SerializeField] private int maxScore = 5000;
    [SerializeField] private int minScore = 100;

    [Header("Animation")]
    [SerializeField] private Transform flagTransform;
    [SerializeField] private float flagSlideSpeed = 6f;
    [SerializeField] private float flagBottomY = 0f;
    [SerializeField] private float playerSlideSpeed = 5f;
    [SerializeField] private Transform landingPoint;

    [Header("References")]
    [SerializeField] private Collider2D triggerCollider;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.IsDead) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        triggered = true;

        float playerY = other.transform.position.y;
        float normalizedHeight = Mathf.InverseLerp(bottomY, topY, playerY);
        int score = Mathf.RoundToInt(Mathf.Lerp(minScore, maxScore, normalizedHeight));

        score = RoundToNearestHundred(score);

        GameManager.Instance?.AddScore(score, other.transform.position);
        AudioManager.Instance?.PlayFlagpole();

        StartCoroutine(FlagpoleRoutine(player, health));
    }

    private IEnumerator FlagpoleRoutine(PlayerController player, PlayerHealth health)
    {
        player.SetInputEnabled(false);

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.gravityScale = 0f;
        }

        if (flagTransform != null)
        {
            Vector3 flagTarget = new Vector3(flagTransform.position.x, flagBottomY, flagTransform.position.z);
            while (Vector3.Distance(flagTransform.position, flagTarget) > 0.05f)
            {
                flagTransform.position = Vector3.MoveTowards(flagTransform.position, flagTarget, flagSlideSpeed * Time.deltaTime);

                if (playerRb != null)
                    playerRb.MovePosition(Vector2.MoveTowards(playerRb.position, new Vector2(playerRb.position.x, flagTransform.position.y), playerSlideSpeed * Time.deltaTime));

                yield return null;
            }
        }

        if (playerRb != null)
            playerRb.gravityScale = 3f;

        if (landingPoint != null)
        {
            float elapsed = 0f;
            Vector3 start = player.transform.position;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.5f);
                player.transform.position = Vector3.Lerp(start, landingPoint.position, t);
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.3f);

        GameManager.Instance?.TryCompleteLevel();
    }

    private static int RoundToNearestHundred(int value)
    {
        return Mathf.RoundToInt(value / 100f) * 100;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 top = new Vector3(transform.position.x, topY, transform.position.z);
        Vector3 bottom = new Vector3(transform.position.x, bottomY, transform.position.z);
        Gizmos.DrawLine(top, bottom);
        Gizmos.DrawWireSphere(top, 0.15f);
        Gizmos.DrawWireSphere(bottom, 0.15f);

        if (landingPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(landingPoint.position, 0.2f);
        }
    }
}
