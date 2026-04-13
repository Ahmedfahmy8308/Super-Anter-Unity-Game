using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Flow")]
    [Tooltip("When set, scene order and indices come from the catalog (add new levels here instead of editing build indices).")]
    [SerializeField] private LevelCatalog levelCatalog;
    [SerializeField] private int mainMenuBuildIndex = 0;
    [SerializeField] private int firstLevelBuildIndex = 1;
    [SerializeField] private int lastLevelBuildIndex = 2;
    [SerializeField] private float sceneFadeDuration = 0.25f;

    [Header("Death Flow")]
    [SerializeField] private float hitStopDuration = 0.1f;
    [SerializeField] private float deathSlowMoScale = 0.5f;
    [SerializeField] private float deathSlowMoDuration = 0.2f;
    [SerializeField] private float restartDelay = 1.0f;
    [SerializeField] private float deathPitchMultiplier = 0.8f;
    [SerializeField] private float deathShakeForce = 1.1f;

    [Header("Lives")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int maxLives = 99;
    [SerializeField] private int coinsFor1UP = 100;

    [Header("Score")]
    [SerializeField] private int startingScore = 0;
    [SerializeField] private GameData gameData;

    [Header("Timer")]
    [SerializeField] private float levelTimeLimit = 300f;
    [SerializeField] private float timeWarningThreshold = 60f;
    [SerializeField] private int timeUpScorePerSecond = 50;

    [Header("Feedback")]
    [SerializeField] private CameraImpulseEmitter cameraImpulseEmitter;
    [SerializeField] private CameraFollow2D fallbackCameraShake;

    [Header("Combo")]
    [SerializeField] private float comboResetTime = 1.5f;

    private int currentScore;
    private int currentLives;
    private int currentCoins;
    private float currentTimer;
    private bool timerRunning;
    private bool flowLocked;
    private int stompCombo;
    private float comboTimer;
    private bool isPaused;

    private const string PrefUnlockedLevelCount = "SuperAnter_UnlockedLevelCount";
    private const string PrefBestScorePrefix = "SuperAnter_LevelBest_";
    private const string PrefGlobalBestScore = "SuperAnter_GlobalBestScore";

    public int CurrentScore => currentScore;
    public int CurrentLives => currentLives;
    public int CurrentCoins => currentCoins;
    public float CurrentTimer => currentTimer;
    public bool IsPaused => isPaused;
    public int MainMenuBuildIndex => ResolveMainMenuBuildIndex();
    public LevelCatalog LevelCatalog => levelCatalog;

    public void ApplyRuntimeData(GameDataRuntimeData data)
    {
        if (data == null || data.score == null) return;
        startingScore = data.score.startingScore;
        currentScore = startingScore;
        UIManager.Instance?.UpdateScore(currentScore, false);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentScore = gameData != null ? gameData.startingScore : startingScore;
        currentLives = startingLives;
        currentCoins = 0;
        EnsureProgressDefaults();
    }

    private void Start()
    {
        UIManager.Instance?.UpdateScore(currentScore, false);
        UIManager.Instance?.UpdateLives(currentLives);
        UIManager.Instance?.UpdateCoins(currentCoins);

        // Direct play from a level scene (no fade load): timer would never start otherwise.
        if (SceneManager.GetActiveScene().buildIndex != ResolveMainMenuBuildIndex() && !timerRunning)
        {
            LevelFloorSafety.EnsureFloorForGameplayScene(SceneManager.GetActiveScene());
            StartLevelTimer();
        }
    }

    private void Update()
    {
        UpdateTimer();
        UpdateCombo();
        HandlePauseInput();
    }

    private void UpdateTimer()
    {
        if (!timerRunning || isPaused) return;

        currentTimer -= Time.deltaTime;
        UIManager.Instance?.UpdateTimer(currentTimer, currentTimer <= timeWarningThreshold);

        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            timerRunning = false;
            OnTimeUp();
        }
    }

    private void UpdateCombo()
    {
        if (stompCombo > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
                stompCombo = 0;
        }
    }

    private void HandlePauseInput()
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
            pressed = UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = pressed || Input.GetKeyDown(KeyCode.Escape);
#endif
        if (pressed)
        {
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;
            if (sceneIndex != ResolveMainMenuBuildIndex() && !flowLocked)
                TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        UIManager.Instance?.ShowPause(isPaused);
    }

    public void AddScore(int amount)
    {
        AddScore(amount, Vector3.zero);
    }

    public void AddScore(int amount, Vector3 worldPosition)
    {
        currentScore += amount;
        UIManager.Instance?.UpdateScore(currentScore, true);
        UIManager.Instance?.SpawnFloatingText(worldPosition, $"+{amount}", Color.white);
    }

    public void AddStompScore(int baseAmount, Vector3 worldPosition)
    {
        stompCombo++;
        comboTimer = comboResetTime;

        int comboMultiplier = Mathf.Min(stompCombo, 8);
        int[] comboScores = { 100, 200, 400, 500, 800, 1000, 2000, 4000, 8000 };
        int score = comboMultiplier < comboScores.Length ? comboScores[comboMultiplier] : 8000;

        currentScore += score;
        UIManager.Instance?.UpdateScore(currentScore, true);

        string comboText = stompCombo > 1 ? $"+{score} x{stompCombo}" : $"+{score}";
        Color comboColor = stompCombo > 3 ? Color.yellow : Color.white;
        UIManager.Instance?.SpawnFloatingText(worldPosition, comboText, comboColor);
    }

    public void AddCoin(int amount, Vector3 worldPosition)
    {
        currentCoins += amount;
        currentScore += 200;

        UIManager.Instance?.UpdateScore(currentScore, true);
        UIManager.Instance?.UpdateCoins(currentCoins);

        if (currentCoins >= coinsFor1UP)
        {
            currentCoins -= coinsFor1UP;
            AddLife();
            UIManager.Instance?.UpdateCoins(currentCoins);
            UIManager.Instance?.SpawnFloatingText(worldPosition, "1UP!", Color.green);
        }
        else
        {
            UIManager.Instance?.SpawnFloatingText(worldPosition, $"+{amount}", new Color(1f, 0.85f, 0f));
        }
    }

    public void AddLife()
    {
        if (currentLives >= maxLives) return;
        currentLives++;
        UIManager.Instance?.UpdateLives(currentLives);
        AudioManager.Instance?.Play1UP();
    }

    public void ResetScore()
    {
        currentScore = gameData != null ? gameData.startingScore : startingScore;
        UIManager.Instance?.UpdateScore(currentScore, false);
    }

    public void StartNewGame()
    {
        StartLevelAtCatalogIndex(0);
    }

    public void ReturnToMainMenu()
    {
        flowLocked = false;
        Time.timeScale = 1f;
        isPaused = false;
        LoadSceneWithFade(ResolveMainMenuBuildIndex());
    }

    public void RestartCurrentLevel()
    {
        LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        int current = GetCurrentCatalogLevelIndex();
        if (current < 0)
        {
            ReturnToMainMenu();
            return;
        }

        int next = current + 1;
        if (IsLevelUnlocked(next))
            StartLevelAtCatalogIndex(next);
        else
            ReturnToMainMenu();
    }

    public void CompleteLevel()
    {
        if (flowLocked) return;
        flowLocked = true;
        timerRunning = false;
        StartCoroutine(LevelCompleteRoutine());
    }

    public bool TryCompleteLevel()
    {
        int requiredCoins = GetRequiredCoinsForCurrentLevel();
        if (requiredCoins > 0 && currentCoins < requiredCoins)
        {
            int missing = requiredCoins - currentCoins;
            Vector3 popupWorldPos = Vector3.zero;
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
                popupWorldPos = player.transform.position + Vector3.up * 1.2f;

            UIManager.Instance?.SpawnFloatingText(popupWorldPos, $"Anter {currentCoins:D2}/{requiredCoins:D2} (Need {missing:D2})", new Color(1f, 0.45f, 0.45f));
            return false;
        }

        CompleteLevel();
        return true;
    }

    public int GetUnlockedLevelCount()
    {
        int max = Mathf.Max(1, levelCatalog != null ? levelCatalog.LevelCount : 1);
        return Mathf.Clamp(PlayerPrefs.GetInt(PrefUnlockedLevelCount, 1), 1, max);
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex < 0) return false;
        int levelCount = levelCatalog != null ? levelCatalog.LevelCount : 0;
        if (levelCount > 0 && levelIndex >= levelCount) return false;
        return levelIndex < GetUnlockedLevelCount();
    }

    public int GetLevelBestScore(int levelIndex)
    {
        if (levelIndex < 0) return 0;
        return PlayerPrefs.GetInt(PrefBestScorePrefix + levelIndex, 0);
    }

    public int GetRequiredCoinsForCurrentLevel()
    {
        int levelIndex = GetCurrentCatalogLevelIndex();
        return GetRequiredCoinsForLevel(levelIndex);
    }

    public int GetRequiredCoinsForLevel(int levelIndex)
    {
        if (levelCatalog == null) return 0;
        return levelCatalog.GetRequiredCoinsForLevel(levelIndex);
    }

    public void StartLevelAtCatalogIndex(int levelIndex)
    {
        if (levelCatalog == null || !IsLevelUnlocked(levelIndex))
            return;

        int buildIndex = levelCatalog.GetLevelBuildIndex(levelIndex);
        if (buildIndex < 0)
            return;

        flowLocked = false;
        Time.timeScale = 1f;
        isPaused = false;
        ResetScore();
        currentLives = startingLives;
        currentCoins = 0;
        stompCombo = 0;
        UIManager.Instance?.UpdateLives(currentLives);
        UIManager.Instance?.UpdateCoins(currentCoins);
        LoadSceneWithFade(buildIndex);
    }

    public void HandlePlayerDeath(PlayerHealth playerHealth, Vector2 knockbackDirection, Vector3 impactPosition)
    {
        if (flowLocked) return;
        flowLocked = true;
        timerRunning = false;
        StartCoroutine(PlayerDeathRoutine(playerHealth, knockbackDirection, impactPosition));
    }

    public void TriggerCameraImpulse(float force)
    {
        if (cameraImpulseEmitter != null)
        {
            cameraImpulseEmitter.Emit(force);
            return;
        }

        if (fallbackCameraShake == null && Camera.main != null)
            fallbackCameraShake = Camera.main.GetComponent<CameraFollow2D>();

        if (fallbackCameraShake != null)
            fallbackCameraShake.Shake(force * 0.35f, 0.18f);
    }

    public void StartLevelTimer()
    {
        currentTimer = levelTimeLimit;
        timerRunning = true;
        UIManager.Instance?.UpdateTimer(currentTimer, false);
    }

    private void OnTimeUp()
    {
        PlayerHealth player = Object.FindAnyObjectByType<PlayerHealth>();
        if (player != null && !player.IsDead)
            player.Die();
    }

    private IEnumerator PlayerDeathRoutine(PlayerHealth playerHealth, Vector2 knockbackDirection, Vector3 impactPosition)
    {
        AudioManager.Instance?.SetGlobalPitchMultiplier(1f);
        Time.timeScale = 0f;

        TriggerCameraImpulse(deathShakeForce);
        playerHealth?.PrepareForDeath();

        yield return new WaitForSecondsRealtime(hitStopDuration);

        Time.timeScale = deathSlowMoScale;
        AudioManager.Instance?.SetGlobalPitchMultiplier(deathPitchMultiplier);
        AudioManager.Instance?.PlayDeath();

        playerHealth?.ApplyDeathKnockback(knockbackDirection);
        playerHealth?.SpawnDeathExplosion(impactPosition);

        currentLives--;
        UIManager.Instance?.UpdateLives(currentLives);

        if (currentLives <= 0)
        {
            UIManager.Instance?.ShowGameOver(true);
            AudioManager.Instance?.PlayGameOver();
            yield return new WaitForSecondsRealtime(deathSlowMoDuration);
            playerHealth?.FreezeForRespawn();
            Time.timeScale = 0f;
            AudioManager.Instance?.SetGlobalPitchMultiplier(1f);
            flowLocked = false;
        }
        else
        {
            UIManager.Instance?.ShowGameOver(false);
            yield return new WaitForSecondsRealtime(deathSlowMoDuration);
            playerHealth?.FreezeForRespawn();
            Time.timeScale = 1f;
            AudioManager.Instance?.SetGlobalPitchMultiplier(1f);
            yield return new WaitForSecondsRealtime(restartDelay);
            flowLocked = false;
            RestartCurrentLevel();
        }
    }

    private IEnumerator LevelCompleteRoutine()
    {
        Scene active = SceneManager.GetActiveScene();
        int completedLevelIndex = GetCurrentCatalogLevelIndex();
        bool hasNextLevel = ResolveHasNextLevel(active);

        AudioManager.Instance?.PlayLevelComplete();
        UIManager.Instance?.ShowLevelComplete(hasNextLevel);

        yield return StartCoroutine(TimeBonusRoutine());
        SaveProgressAfterLevelComplete(completedLevelIndex);
        yield return new WaitForSecondsRealtime(1.1f);
        ReturnToMainMenu();
        flowLocked = false;
    }

    private IEnumerator TimeBonusRoutine()
    {
        while (currentTimer > 0)
        {
            float deduct = Mathf.Min(currentTimer, 5f);
            currentTimer -= deduct;
            int bonus = Mathf.RoundToInt(deduct) * timeUpScorePerSecond;
            currentScore += bonus;
            UIManager.Instance?.UpdateScore(currentScore, true);
            UIManager.Instance?.UpdateTimer(currentTimer, false);
            AudioManager.Instance?.PlayCoin();
            yield return new WaitForSecondsRealtime(0.03f);
        }

        currentTimer = 0f;
    }

    private void ShowWinScreen()
    {
        Time.timeScale = 0f;
        UIManager.Instance?.ShowWinScreen(currentScore, currentCoins);
    }

    private void LoadSceneWithFade(int buildIndex)
    {
        if (flowLocked)
            flowLocked = false;
        flowLocked = true;
        StartCoroutine(LoadSceneRoutine(buildIndex));
    }

    private IEnumerator LoadSceneRoutine(int buildIndex)
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (UIManager.Instance != null)
            yield return UIManager.Instance.FadeTo(1f, sceneFadeDuration);

        SceneManager.LoadScene(buildIndex);
        yield return null;

        LevelFloorSafety.EnsureFloorForGameplayScene(SceneManager.GetActiveScene());

        UIManager.Instance?.RefreshForCurrentScene();

        if (UIManager.Instance != null)
            yield return UIManager.Instance.FadeTo(0f, sceneFadeDuration);

        if (buildIndex != ResolveMainMenuBuildIndex())
        {
            currentCoins = 0;
            UIManager.Instance?.UpdateCoins(currentCoins);
            StartLevelTimer();
        }

        stompCombo = 0;
        flowLocked = false;
    }

    private int GetCurrentCatalogLevelIndex()
    {
        if (levelCatalog == null) return -1;
        return levelCatalog.GetCatalogIndexForActiveScene();
    }

    private void SaveProgressAfterLevelComplete(int completedLevelIndex)
    {
        if (completedLevelIndex < 0) return;

        string levelKey = PrefBestScorePrefix + completedLevelIndex;
        int oldBest = PlayerPrefs.GetInt(levelKey, 0);
        if (currentScore > oldBest)
            PlayerPrefs.SetInt(levelKey, currentScore);

        int globalBest = PlayerPrefs.GetInt(PrefGlobalBestScore, 0);
        if (currentScore > globalBest)
            PlayerPrefs.SetInt(PrefGlobalBestScore, currentScore);

        int levelCount = levelCatalog != null ? levelCatalog.LevelCount : 0;
        int unlocked = GetUnlockedLevelCount();
        int shouldUnlockCount = Mathf.Min(levelCount, completedLevelIndex + 2);
        if (shouldUnlockCount > unlocked)
            PlayerPrefs.SetInt(PrefUnlockedLevelCount, shouldUnlockCount);

        PlayerPrefs.Save();
    }

    private void EnsureProgressDefaults()
    {
        int levelCount = Mathf.Max(1, levelCatalog != null ? levelCatalog.LevelCount : 1);
        int unlocked = PlayerPrefs.GetInt(PrefUnlockedLevelCount, 1);
        unlocked = Mathf.Clamp(unlocked, 1, levelCount);
        PlayerPrefs.SetInt(PrefUnlockedLevelCount, unlocked);
        PlayerPrefs.Save();
    }

    private int ResolveMainMenuBuildIndex()
    {
        if (levelCatalog != null)
        {
            int idx = levelCatalog.GetMainMenuBuildIndex();
            if (idx >= 0) return idx;
        }
        return mainMenuBuildIndex;
    }

    private int ResolveFirstLevelBuildIndex()
    {
        if (levelCatalog != null && levelCatalog.LevelCount > 0)
        {
            int idx = levelCatalog.GetLevelBuildIndex(0);
            if (idx >= 0) return idx;
        }
        return firstLevelBuildIndex;
    }

    private bool ResolveHasNextLevel(Scene active)
    {
        if (levelCatalog != null)
        {
            int cat = levelCatalog.GetCatalogIndexForScenePath(active.path);
            if (cat >= 0)
                return levelCatalog.HasNextLevelAfter(cat);
        }
        return active.buildIndex < lastLevelBuildIndex;
    }

    private int ResolveNextLevelBuildIndexAfter(Scene active)
    {
        if (levelCatalog != null)
        {
            int cat = levelCatalog.GetCatalogIndexForScenePath(active.path);
            if (cat >= 0)
                return levelCatalog.GetNextLevelBuildIndex(cat);
        }
        int next = active.buildIndex + 1;
        if (next <= lastLevelBuildIndex)
            return next;
        return -1;
    }
}
