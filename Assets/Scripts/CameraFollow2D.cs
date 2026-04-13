using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Framing")]
    [SerializeField] private Vector2 offset = new Vector2(2f, 0f);
    [SerializeField] private float height = 3f;
    [SerializeField] private float undergroundHeight = -12f;
    [SerializeField] private bool useSceneStartYAsHeight = true;
    [SerializeField] private bool followHighJumps = true;
    [SerializeField] private float highJumpFollowOffsetY = 0.5f;

    [Header("Horizontal bounds")]
    [Tooltip("Clamp camera X so the view stays over the level. Uses world X of the outermost ground tiles.")]
    [SerializeField] private bool useHorizontalBounds = true;
    [SerializeField] private float levelLeftEdgeWorld = -14f;
    [SerializeField] private float levelRightEdgeWorld = 41f;
    [SerializeField] private bool autoSyncRightBoundToFinishPoint = true;
    [SerializeField] private float finishPointRightPadding = 3f;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float zDistance = -10f;

    [Header("Shake")]
    [SerializeField] private float shakeReturnSpeed = 18f;

    private Camera cam;
    private Vector3 shakeOffset;
    private float shakeTimer;
    private float shakeDuration;
    private float shakeIntensity;
    private Vector2 shakeSeed;
    private Vector3 velocity;
    private bool underground;
    private bool boundsSynced;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void Shake(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f) return;
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimer = Mathf.Max(shakeTimer, duration);
        shakeSeed = Random.insideUnitCircle * 100f;
    }

    public void SetUnderground(bool isUnderground)
    {
        underground = isUnderground;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (useSceneStartYAsHeight)
            height = transform.position.y;

        ResolveTargetIfNeeded();
        TrySyncBoundsWithFinishPoint();
    }

    private void LateUpdate()
    {
        if (!boundsSynced)
            TrySyncBoundsWithFinishPoint();

        if (!target)
            ResolveTargetIfNeeded();
        if (!target) return;

        Vector3 currentPosition = transform.position;
        float baseY = underground ? undergroundHeight : height;
        float targetY = baseY;

        if (followHighJumps && !underground)
            targetY = Mathf.Max(baseY, target.position.y + highJumpFollowOffsetY);

        float targetX = target.position.x + offset.x;

        if (useHorizontalBounds && cam != null && cam.orthographic)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            float minCamX = levelLeftEdgeWorld + halfWidth;
            float maxCamX = levelRightEdgeWorld - halfWidth;
            if (minCamX <= maxCamX)
                targetX = Mathf.Clamp(targetX, minCamX, maxCamX);
        }

        UpdateShake();

        Vector3 desiredPosition = new Vector3(targetX, targetY, zDistance) + shakeOffset;
        transform.position = Vector3.SmoothDamp(currentPosition, desiredPosition, ref velocity, smoothTime);
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float elapsed = Mathf.Max(0.0001f, shakeDuration - shakeTimer);
            float decay = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            float noiseX = (Mathf.PerlinNoise(shakeSeed.x, Time.unscaledTime * 30f) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(shakeSeed.y, Time.unscaledTime * 30f + 13.37f) - 0.5f) * 2f;
            shakeOffset = new Vector3(noiseX, noiseY, 0f) * shakeIntensity * decay;
            return;
        }

        shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.unscaledDeltaTime * shakeReturnSpeed);
        shakeIntensity = Mathf.Lerp(shakeIntensity, 0f, Time.unscaledDeltaTime * shakeReturnSpeed);
        shakeDuration = Mathf.Lerp(shakeDuration, 0f, Time.unscaledDeltaTime * shakeReturnSpeed);
    }

    private void ResolveTargetIfNeeded()
    {
        if (target != null) return;
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) target = player.transform;
    }

    private void TrySyncBoundsWithFinishPoint()
    {
        if (!autoSyncRightBoundToFinishPoint)
            return;

        Flagpole flagpole = Object.FindAnyObjectByType<Flagpole>();
        if (flagpole != null)
        {
            levelRightEdgeWorld = Mathf.Max(levelRightEdgeWorld, flagpole.transform.position.x + finishPointRightPadding);
            boundsSynced = true;
            return;
        }

        FinishPoint finish = Object.FindAnyObjectByType<FinishPoint>();
        if (finish == null)
            return;

        levelRightEdgeWorld = Mathf.Max(levelRightEdgeWorld, finish.transform.position.x + finishPointRightPadding);
        boundsSynced = true;
    }
}
