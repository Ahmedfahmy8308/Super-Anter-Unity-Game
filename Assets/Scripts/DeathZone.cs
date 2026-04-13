using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DeathZone : MonoBehaviour
{
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && !health.IsDead)
        {
            health.Die(Vector2.up, other.transform.position);
            return;
        }

        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null && !enemy.IsDead)
        {
            Destroy(enemy.gameObject);
            return;
        }

        Destroy(other.gameObject);
    }
}
