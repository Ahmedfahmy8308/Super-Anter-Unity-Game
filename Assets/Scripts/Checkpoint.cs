using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool isActivated;
    [SerializeField] private Sprite activatedSprite;
    [SerializeField] private ParticleSystem activateVfxPrefab;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private static Checkpoint activeCheckpoint;

    public static Vector3? GetActiveCheckpointPosition()
    {
        if (activeCheckpoint != null)
            return activeCheckpoint.transform.position;
        return null;
    }

    public static void ClearAll()
    {
        activeCheckpoint = null;
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;
        if (other.GetComponentInParent<PlayerHealth>() == null) return;

        Activate();
    }

    private void Activate()
    {
        if (activeCheckpoint != null && activeCheckpoint != this)
            activeCheckpoint.Deactivate();

        isActivated = true;
        activeCheckpoint = this;

        if (spriteRenderer != null && activatedSprite != null)
            spriteRenderer.sprite = activatedSprite;

        if (activateVfxPrefab != null)
        {
            ParticleSystem vfx = Instantiate(activateVfxPrefab, transform.position + Vector3.up, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, 2f);
        }

        AudioManager.Instance?.PlayCoin();
    }

    private void Deactivate()
    {
        isActivated = false;
    }
}
