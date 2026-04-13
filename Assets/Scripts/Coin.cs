using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int coinCount = 1;

    [Header("VFX")]
    [SerializeField] private ParticleSystem pickupVfxPrefab;
    [SerializeField] private float vfxDestroyDelay = 1.5f;

    [Header("Audio")]
    [SerializeField] private bool playPickupSfx = true;

    [Header("Float Animation")]
    [SerializeField] private bool floatAnimation = true;
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotateSpeed = 0f;

    private bool collected;
    private Vector3 startPosition;
    private float animationOffset;

    private void Reset()
    {
        Collider2D coinCollider = GetComponent<Collider2D>();
        coinCollider.isTrigger = true;
    }

    private void Start()
    {
        startPosition = transform.position;
        animationOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (collected || !floatAnimation) return;

        float yOffset = Mathf.Sin((Time.time + animationOffset) * floatSpeed) * floatAmplitude;
        transform.position = startPosition + Vector3.up * yOffset;

        if (rotateSpeed > 0f)
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    public void ApplyRuntimeData(GameDataRuntimeData data)
    {
        if (data == null || data.score == null) return;
        coinCount = data.score.coinValue;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponentInParent<PlayerHealth>() == null) return;

        collected = true;
        GameManager.Instance?.AddCoin(coinCount, transform.position);

        if (playPickupSfx)
            AudioManager.Instance?.PlayCoin();

        if (pickupVfxPrefab != null)
        {
            ParticleSystem vfx = Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfxDestroyDelay);
        }

        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Vector3 start = transform.position;
        Vector3 peak = start + Vector3.up * 0.8f;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, peak, t);

            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - t;
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
