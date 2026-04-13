using System.Collections;
using UnityEngine;

public enum BlockContent { Coin, Mushroom, Star, MultiCoin, OneUp }

public class QuestionBlock : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private BlockContent content = BlockContent.Coin;
    [SerializeField] private int multiCoinCount = 5;

    [Header("Spawning")]
    [SerializeField] private GameObject mushroomPrefab;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject oneUpPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Animation")]
    [SerializeField] private float bumpHeight = 0.35f;
    [SerializeField] private float bumpDuration = 0.15f;
    [SerializeField] private Sprite usedSprite;

    [Header("Coin Pop")]
    [SerializeField] private GameObject coinPopPrefab;
    [SerializeField] private float coinPopHeight = 2f;
    [SerializeField] private float coinPopDuration = 0.4f;

    private SpriteRenderer spriteRenderer;
    private bool isUsed;
    private int remainingCoins;
    private bool isBumping;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        remainingCoins = multiCoinCount;
    }

    public void Hit(PlayerController player)
    {
        if (isBumping) return;

        if (content == BlockContent.MultiCoin)
        {
            if (remainingCoins <= 0) return;
            remainingCoins--;
            SpawnCoinPop();

            if (remainingCoins <= 0)
                MarkUsed();

            StartCoroutine(BumpRoutine());
            return;
        }

        if (isUsed) return;
        MarkUsed();
        StartCoroutine(BumpRoutine());
        SpawnContent(player);
    }

    private void SpawnContent(PlayerController player)
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * 0.6f;

        switch (content)
        {
            case BlockContent.Coin:
                SpawnCoinPop();
                break;
            case BlockContent.Mushroom:
                SpawnItem(mushroomPrefab, pos);
                break;
            case BlockContent.Star:
                SpawnItem(starPrefab, pos);
                break;
            case BlockContent.OneUp:
                SpawnItem(oneUpPrefab, pos);
                break;
        }
    }

    private void SpawnCoinPop()
    {
        GameManager.Instance?.AddCoin(1, transform.position + Vector3.up);
        AudioManager.Instance?.PlayCoin();

        if (coinPopPrefab != null)
        {
            GameObject coin = Instantiate(coinPopPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            StartCoroutine(CoinPopRoutine(coin));
        }
    }

    private IEnumerator CoinPopRoutine(GameObject coin)
    {
        if (coin == null) yield break;

        Vector3 start = coin.transform.position;
        Vector3 peak = start + Vector3.up * coinPopHeight;
        float elapsed = 0f;
        float halfDuration = coinPopDuration * 0.5f;

        while (elapsed < halfDuration && coin != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            coin.transform.position = Vector3.Lerp(start, peak, eased);
            yield return null;
        }

        if (coin == null) yield break;

        elapsed = 0f;
        Vector3 end = start + Vector3.up * 0.2f;
        while (elapsed < halfDuration && coin != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            coin.transform.position = Vector3.Lerp(peak, end, t);
            yield return null;
        }

        if (coin != null)
            Destroy(coin);
    }

    private void SpawnItem(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        GameObject item = Instantiate(prefab, position, Quaternion.identity);
        StartCoroutine(ItemRiseRoutine(item, position));
    }

    private IEnumerator ItemRiseRoutine(GameObject item, Vector3 startPos)
    {
        if (item == null) yield break;

        Vector3 endPos = startPos + Vector3.up * 0.8f;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration && item != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            item.transform.position = Vector3.Lerp(startPos, endPos, eased);
            yield return null;
        }
    }

    private void MarkUsed()
    {
        isUsed = true;
        if (spriteRenderer != null && usedSprite != null)
            spriteRenderer.sprite = usedSprite;
    }

    private IEnumerator BumpRoutine()
    {
        isBumping = true;
        Vector3 originalPos = transform.position;
        Vector3 bumpPos = originalPos + Vector3.up * bumpHeight;

        float elapsed = 0f;
        float halfDuration = bumpDuration * 0.5f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.position = Vector3.Lerp(originalPos, bumpPos, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float eased = t * t;
            transform.position = Vector3.Lerp(bumpPos, originalPos, eased);
            yield return null;
        }

        transform.position = originalPos;
        isBumping = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (collision.contactCount <= 0) return;
        Vector2 normal = collision.GetContact(0).normal;

        // Hit from below: contact normal on the block's bottom points downward (negative Y in Unity 2D).
        if (normal.y < -0.3f)
        {
            AudioManager.Instance?.PlayBlockBump();
            Hit(player);
        }
    }
}
