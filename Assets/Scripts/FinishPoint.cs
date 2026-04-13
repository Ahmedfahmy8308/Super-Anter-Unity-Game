using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FinishPoint : MonoBehaviour
{
    [SerializeField] private bool loadLevelOnlyOnce = true;
    [SerializeField] private ParticleSystem completionVfxPrefab;

    private bool triggered;

    private void Reset()
    {
        Collider2D finishCollider = GetComponent<Collider2D>();
        finishCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (loadLevelOnlyOnce && triggered) return;
        if (other.GetComponentInParent<PlayerHealth>() == null) return;

        triggered = true;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            player.SetInputEnabled(false);

        if (completionVfxPrefab != null)
        {
            ParticleSystem vfx = Instantiate(completionVfxPrefab, transform.position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, 3f);
        }

        Checkpoint.ClearAll();
        GameManager.Instance?.TryCompleteLevel();
    }
}
