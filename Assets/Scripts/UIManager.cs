using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Scene Setup")]
    [SerializeField] private int mainMenuBuildIndex = 0;

    [Header("Panels")]
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private CanvasGroup levelSelectGroup;
    [SerializeField] private CanvasGroup settingsGroup;
    [SerializeField] private CanvasGroup hudGroup;
    [SerializeField] private CanvasGroup gameOverGroup;
    [SerializeField] private CanvasGroup levelCompleteGroup;
    [SerializeField] private CanvasGroup winGroup;
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private CanvasGroup pauseGroup;

    [Header("Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI settingsDeveloperText;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject pauseButtonObject;
    [SerializeField] private Vector2 pauseButtonSize = new Vector2(84f, 40f);
    [SerializeField] private Vector2 pauseButtonOffset = new Vector2(-14f, -12f);
    [SerializeField] private bool pauseButtonBringToFront = true;
    [SerializeField] private RectTransform floatingTextRoot;
    [SerializeField] private FloatingTextPopup floatingTextPrefab;
    [SerializeField] private Camera uiCamera;

    [Header("Branding")]
    [SerializeField] private string developerCreditText = "Developed by Ahmed Fahmy";
    [SerializeField] private TextMeshProUGUI mainMenuDeveloperText;
    [SerializeField] private TextMeshProUGUI hudDeveloperText;
    [SerializeField] private Vector2 mainMenuDeveloperPosition = new Vector2(-24f, 18f);
    [SerializeField] private Vector2 settingsDeveloperPosition = new Vector2(0f, -190f);
    [SerializeField] private Vector2 hudDeveloperPosition = new Vector2(-20f, 16f);

    [Header("Level Complete")]
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private GameObject nextLevelButtonObject;
    [SerializeField] private GameObject mainMenuButtonObject;

    [Header("Win Screen")]
    [SerializeField] private TextMeshProUGUI winScoreText;
    [SerializeField] private TextMeshProUGUI winCoinsText;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private GameObject retryButtonObject;
    [SerializeField] private GameObject gameOverMenuButtonObject;

    [Header("Level Select")]
    [SerializeField] private GameObject level1ButtonObject;
    [SerializeField] private GameObject level2ButtonObject;
    [SerializeField] private GameObject level3ButtonObject;
    [SerializeField] private TextMeshProUGUI level1InfoText;
    [SerializeField] private TextMeshProUGUI level2InfoText;
    [SerializeField] private TextMeshProUGUI level3InfoText;

    [Header("Runtime Level Panel Style")]
    [SerializeField] private string runtimePanelTitle = "CHOOSE LEVEL";
    [SerializeField] private Color runtimePanelBackgroundColor = new Color(0.03f, 0.08f, 0.15f, 0.86f);
    [SerializeField] private Vector2 runtimeButtonSize = new Vector2(280f, 60f);
    [SerializeField] private float runtimeLevelButtonsStartY = 80f;
    [SerializeField] private float runtimeLevelButtonsSpacing = 80f;
    [SerializeField] private float runtimeBackButtonY = -170f;

    [Header("Runtime Level Labels")]
    [SerializeField] private string runtimeLevel1Label = "Level 1";
    [SerializeField] private string runtimeLevel2Label = "Level 2";
    [SerializeField] private string runtimeLevel3Label = "Level 3";
    [SerializeField] private string runtimeBackLabel = "Back";
    [SerializeField] private string runtimeLevel1Icon = "L1";
    [SerializeField] private string runtimeLevel2Icon = "L2";
    [SerializeField] private string runtimeLevel3Icon = "L3";
    [SerializeField] private string runtimeBackIcon = "<";

    [Header("Runtime Level Colors")]
    [SerializeField] private Color runtimeLevel1Color = new Color(0.11f, 0.36f, 0.67f, 0.98f);
    [SerializeField] private Color runtimeLevel2Color = new Color(0.10f, 0.46f, 0.59f, 0.98f);
    [SerializeField] private Color runtimeLevel3Color = new Color(0.14f, 0.55f, 0.48f, 0.98f);
    [SerializeField] private Color runtimeBackColor = new Color(0.28f, 0.31f, 0.36f, 0.95f);

    [Header("HUD Style")]
    [SerializeField] private bool useHudLabelFormat = false;
    [SerializeField] private bool autoStyleHudText = true;
    [SerializeField] private float hudScoreFontSize = 22f;
    [SerializeField] private float hudSmallFontSize = 20f;
    [SerializeField] private Color hudPrimaryTextColor = new Color(0.95f, 0.97f, 1f, 1f);
    [SerializeField] private Color hudAccentTextColor = new Color(1f, 0.84f, 0.34f, 1f);

    [Header("Timer Warning")]
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerWarningColor = Color.red;

    private Coroutine scorePopRoutine;
    private Coroutine timerFlashRoutine;
    private Coroutine levelSelectIntroRoutine;
    private CanvasGroup runtimeLevelSelectGroup;
    private readonly List<CanvasGroup> runtimeLevelCards = new List<CanvasGroup>();
    private Vector3 scoreBaseScale = Vector3.one;
    private bool scoreBaseScaleInitialized;
#if UNITY_EDITOR
    private bool editorPauseButtonUpdateQueued;
#endif

    private const string PrefsMusicVol = "SuperAnter_MusicVol";
    private const string PrefsSfxVol = "SuperAnter_SfxVol";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ResolveSceneReferences();
        WireMenuButtons();
        BindSettingsSliders();
        LoadVolumeFromPrefs();
        RefreshForCurrentScene();
        UpdateScore(GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0, false);

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = true;
            fadeGroup.interactable = false;
            StartCoroutine(FadeTo(0f, 0.25f));
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        QueueEditorPauseButtonRefresh();
    }

    private void QueueEditorPauseButtonRefresh()
    {
        if (editorPauseButtonUpdateQueued)
            return;

        editorPauseButtonUpdateQueued = true;
        EditorApplication.delayCall += RefreshEditorPauseButtonDelayed;
    }

    private void RefreshEditorPauseButtonDelayed()
    {
        editorPauseButtonUpdateQueued = false;

        if (this == null || Application.isPlaying)
            return;

        if (hudGroup == null)
            hudGroup = FindGroupByName("HUDPanel");
        if (mainMenuGroup == null)
            mainMenuGroup = FindGroupByName("MainMenuPanel");
        if (settingsGroup == null)
            settingsGroup = FindGroupByName("SettingsPanel");

        EnsureEditorPauseButton();
        EnsureEditorBrandingLabels();
    }
#endif

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scorePopRoutine != null)
        {
            StopCoroutine(scorePopRoutine);
            scorePopRoutine = null;
        }
        if (timerFlashRoutine != null)
        {
            StopCoroutine(timerFlashRoutine);
            timerFlashRoutine = null;
        }

        ResolveSceneReferences();
        WireMenuButtons();
        BindSettingsSliders();
        LoadVolumeFromPrefs();
        RefreshForCurrentScene();

        if (GameManager.Instance != null)
        {
            UpdateScore(GameManager.Instance.CurrentScore, false);
            UpdateLives(GameManager.Instance.CurrentLives);
            UpdateCoins(GameManager.Instance.CurrentCoins);
        }

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
            fadeGroup.gameObject.SetActive(false);
        }
    }

    public void RefreshForCurrentScene()
    {
        ResolveSceneReferences();
        EnsureDeveloperCreditLabels();
        EnsureRuntimePauseButton();
        RemoveLegacyHudDeveloperText();
        ApplyHudVisualStyle();

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        bool isMainMenu = sceneIndex == mainMenuBuildIndex;

        SetGroupVisible(mainMenuGroup, isMainMenu);
        SetGroupVisible(levelSelectGroup, false);
        SetGroupVisible(hudGroup, !isMainMenu);
        SetGroupVisible(pauseGroup, false);
        SetGroupVisible(settingsGroup, false);

        HideTransientPanels();

        if (hudDeveloperText != null)
            hudDeveloperText.gameObject.SetActive(false);

        if (isMainMenu)
            RefreshLevelSelectButtons();
    }

    public void UpdateScore(int score, bool animatePop = false)
    {
        if (scoreText != null)
        {
            scoreText.text = useHudLabelFormat ? $"SCORE  {score:D6}" : score.ToString("D6");
            if (animatePop) PopScoreText();
        }
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null)
            livesText.text = useHudLabelFormat ? $"LIVES  x {lives}" : $"x {lives}";
    }

    public void UpdateCoins(int coins)
    {
        if (coinText != null)
            coinText.text = $"Anter x {coins:D2}";
    }

    public void UpdateTimer(float timeRemaining, bool isWarning)
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timerText.text = useHudLabelFormat ? $"TIME  {seconds:000}" : seconds.ToString();
            timerText.color = isWarning ? timerWarningColor : timerNormalColor;

            if (isWarning && timerFlashRoutine == null)
                timerFlashRoutine = StartCoroutine(TimerFlashRoutine());
            else if (!isWarning && timerFlashRoutine != null)
            {
                StopCoroutine(timerFlashRoutine);
                timerFlashRoutine = null;
                timerText.color = timerNormalColor;
            }
        }
    }

    private IEnumerator TimerFlashRoutine()
    {
        while (timerText != null)
        {
            timerText.enabled = !timerText.enabled;
            yield return new WaitForSecondsRealtime(0.3f);
        }
        timerFlashRoutine = null;
    }

    public void PopScoreText()
    {
        if (scoreText == null) return;

        if (!scoreBaseScaleInitialized)
        {
            scoreBaseScale = scoreText.rectTransform.localScale;
            scoreBaseScaleInitialized = true;
        }

        if (scorePopRoutine != null) StopCoroutine(scorePopRoutine);
        scoreText.rectTransform.localScale = scoreBaseScale;
        scorePopRoutine = StartCoroutine(ScorePopRoutine());
    }

    public void SpawnFloatingText(Vector3 worldPosition, string message, Color color)
    {
        if (floatingTextPrefab == null || floatingTextRoot == null) return;

        Camera cameraToUse = uiCamera != null ? uiCamera : Camera.main;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(cameraToUse, worldPosition);
        FloatingTextPopup popup = Instantiate(floatingTextPrefab, floatingTextRoot);
        popup.Initialize(message, color, screenPosition);
    }

    public void ShowGameOver(bool isPermaDeath)
    {
        HideTransientPanels();
        SetGroupVisible(gameOverGroup, true);

        if (gameOverText != null)
            gameOverText.text = isPermaDeath ? "Anter Game Over" : "Anter Try Again!";

        if (retryButtonObject != null)
            retryButtonObject.SetActive(!isPermaDeath);

        if (gameOverMenuButtonObject != null)
            gameOverMenuButtonObject.SetActive(isPermaDeath);
    }

    public void ShowLevelComplete(bool hasNextLevel)
    {
        HideTransientPanels();
        SetGroupVisible(levelCompleteGroup, true);

        if (levelCompleteText != null)
            levelCompleteText.text = hasNextLevel ? "Anter Course Clear!" : "Anter Final Course Clear!";

        if (nextLevelButtonObject != null)
            nextLevelButtonObject.SetActive(hasNextLevel);

        if (mainMenuButtonObject != null)
            mainMenuButtonObject.SetActive(!hasNextLevel);
    }

    public void ShowWinScreen(int finalScore, int totalCoins)
    {
        HideTransientPanels();
        SetGroupVisible(winGroup, true);
        SetGroupVisible(hudGroup, false);

        if (winScoreText != null)
            winScoreText.text = $"Anter Final Score: {finalScore:D6}";

        if (winCoinsText != null)
            winCoinsText.text = $"Anter Coins: {totalCoins}";
    }

    public void ShowPause(bool paused)
    {
        if (pauseGroup != null && pauseGroup.GetComponent<RectTransform>() != null)
            SetGroupVisible(pauseGroup, paused);

        if (paused)
            SetGroupVisible(hudGroup, false);
        else
        {
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;
            SetGroupVisible(hudGroup, sceneIndex != mainMenuBuildIndex);
        }
    }

    public void ShowMainMenu()
    {
        HideTransientPanels();
        SetGroupVisible(mainMenuGroup, true);
        SetGroupVisible(hudGroup, false);
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeGroup == null) yield break;

        fadeGroup.gameObject.SetActive(true);
        fadeGroup.blocksRaycasts = true;
        fadeGroup.interactable = false;

        float startAlpha = fadeGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (fadeGroup == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : elapsed / duration;
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        if (fadeGroup == null) yield break;
        fadeGroup.alpha = targetAlpha;
        fadeGroup.blocksRaycasts = targetAlpha > 0f;

        if (Mathf.Approximately(targetAlpha, 0f))
            fadeGroup.gameObject.SetActive(false);
    }

    public void OnStartButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex == mainMenuBuildIndex)
            ShowLevelSelect();
        else
            GameManager.Instance?.StartNewGame();
    }

    public void OnLevel1ButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.StartLevelAtCatalogIndex(0);
    }

    public void OnLevel2ButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.StartLevelAtCatalogIndex(1);
    }

    public void OnLevel3ButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.StartLevelAtCatalogIndex(2);
    }

    public void OnRestartButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.RestartCurrentLevel();
    }

    public void OnNextLevelButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.LoadNextLevel();
    }

    public void OnMainMenuButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        Time.timeScale = 1f;
        GameManager.Instance?.ReturnToMainMenu();
    }

    public void OnResumeButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.TogglePause();
    }

    public void OnPauseButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.TogglePause();
    }

    public void OnQuitButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnSettingsButtonPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        SetGroupVisible(mainMenuGroup, false);
        SetGroupVisible(levelSelectGroup, false);
        SetGroupVisible(settingsGroup, true);
    }

    public void OnSettingsBackPressed()
    {
        AudioManager.Instance?.PlayButtonClick();
        SetGroupVisible(settingsGroup, false);
        SetGroupVisible(levelSelectGroup, false);
        SetGroupVisible(runtimeLevelSelectGroup, false);
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex == mainMenuBuildIndex)
            SetGroupVisible(mainMenuGroup, true);
        else
            SetGroupVisible(hudGroup, true);
    }

    /// <summary>
    /// UICanvas buttons use missing persistent targets (fileID 0). Wire them at runtime to this UIManager (on Systems).
    /// </summary>
    private void WireMenuButtons()
    {
        WireButtonsNamed("StartButton", OnStartButtonPressed);
        WireButtonsNamed("Level1Button", OnLevel1ButtonPressed);
        WireButtonsNamed("Level2Button", OnLevel2ButtonPressed);
        WireButtonsNamed("Level3Button", OnLevel3ButtonPressed);
        WireButtonsNamed("SettingsButton", OnSettingsButtonPressed);
        WireButtonsNamed("BackButton", OnSettingsBackPressed);
        WireButtonsNamed("RetryButton", OnRestartButtonPressed);
        WireButtonsNamed("RestartButton", OnRestartButtonPressed);
        WireButtonsNamed("ResumeButton", OnResumeButtonPressed);
        WireButtonsNamed("PauseButton", OnPauseButtonPressed);
        WireButtonsNamed("QuitButton", OnQuitButtonPressed);
        WireButtonsNamed("NextLevelButton", OnNextLevelButtonPressed);
        WireButtonsNamed("MainMenuButton", OnMainMenuButtonPressed);
        WireButtonsNamed("GameOverMenuButton", OnMainMenuButtonPressed);
        WireButtonsNamed("PauseMenuButton", OnMainMenuButtonPressed);
    }

    private void WireButtonsNamed(string objectName, UnityAction handler)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
            WireButtonsUnderTransform(roots[r].transform, objectName, handler);
    }

    private static void WireButtonsUnderTransform(Transform parent, string objectName, UnityAction handler)
    {
        if (parent.name == objectName)
        {
            Button b = parent.GetComponent<Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(handler);
            }
        }
        for (int i = 0; i < parent.childCount; i++)
            WireButtonsUnderTransform(parent.GetChild(i), objectName, handler);
    }

    private void BindSettingsSliders()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);
        }
    }

    private void OnMusicVolumeSliderChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
        PlayerPrefs.SetFloat(PrefsMusicVol, value);
        PlayerPrefs.Save();
    }

    private void OnSfxVolumeSliderChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
        PlayerPrefs.SetFloat(PrefsSfxVol, value);
        PlayerPrefs.Save();
    }

    private void LoadVolumeFromPrefs()
    {
        float music = PlayerPrefs.GetFloat(PrefsMusicVol, 0.75f);
        float sfx = PlayerPrefs.GetFloat(PrefsSfxVol, 1f);
        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(music);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(sfx);
        AudioManager.Instance?.SetMusicVolume(music);
        AudioManager.Instance?.SetSfxVolume(sfx);
    }

    private void HideTransientPanels()
    {
        SetGroupVisible(gameOverGroup, false);
        SetGroupVisible(levelCompleteGroup, false);
        SetGroupVisible(winGroup, false);
        SetGroupVisible(pauseGroup, false);
        SetGroupVisible(settingsGroup, false);
        SetGroupVisible(levelSelectGroup, false);
        SetGroupVisible(runtimeLevelSelectGroup, false);
    }

    private void ShowLevelSelect()
    {
        EnsureRuntimeLevelSelectPanel();

        CanvasGroup activeLevelGroup = levelSelectGroup != null ? levelSelectGroup : runtimeLevelSelectGroup;

        SetGroupVisible(mainMenuGroup, false);
        SetGroupVisible(settingsGroup, false);
        SetGroupVisible(activeLevelGroup, true);
        RefreshLevelSelectButtons();

        if (activeLevelGroup == runtimeLevelSelectGroup)
        {
            if (levelSelectIntroRoutine != null)
                StopCoroutine(levelSelectIntroRoutine);
            levelSelectIntroRoutine = StartCoroutine(AnimateRuntimeLevelSelectIn());
        }
    }

    private void RefreshLevelSelectButtons()
    {
        EnsureRuntimeLevelSelectPanel();
        ApplyLevelButtonState(0, level1ButtonObject, level1InfoText);
        ApplyLevelButtonState(1, level2ButtonObject, level2InfoText);
        ApplyLevelButtonState(2, level3ButtonObject, level3InfoText);
    }

    private void EnsureRuntimeLevelSelectPanel()
    {
        if (levelSelectGroup != null || runtimeLevelSelectGroup != null)
            return;

        Canvas rootCanvas = Object.FindAnyObjectByType<Canvas>();
        if (rootCanvas == null)
            return;

        GameObject panel = new GameObject("RuntimeLevelSelectPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panel.transform.SetParent(rootCanvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        bg.color = runtimePanelBackgroundColor;

        GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(panel.transform, false);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(360f, 360f);
        frameRect.anchoredPosition = new Vector2(0f, -6f);
        Image frameImage = frame.GetComponent<Image>();
        frameImage.color = new Color(1f, 1f, 1f, 0.06f);

        runtimeLevelSelectGroup = panel.GetComponent<CanvasGroup>();
        runtimeLevelSelectGroup.alpha = 0f;
        runtimeLevelSelectGroup.interactable = false;
        runtimeLevelSelectGroup.blocksRaycasts = false;

        CreateRuntimeTitle(panel.transform, runtimePanelTitle, new Vector2(0f, 165f));

        float y1 = runtimeLevelButtonsStartY;
        float y2 = runtimeLevelButtonsStartY - runtimeLevelButtonsSpacing;
        float y3 = runtimeLevelButtonsStartY - (runtimeLevelButtonsSpacing * 2f);

        level1ButtonObject = CreateRuntimeLevelButton(panel.transform, "Level1Button", runtimeLevel1Label, runtimeLevel1Icon, new Vector2(0f, y1), OnLevel1ButtonPressed, runtimeLevel1Color, runtimeButtonSize);
        level2ButtonObject = CreateRuntimeLevelButton(panel.transform, "Level2Button", runtimeLevel2Label, runtimeLevel2Icon, new Vector2(0f, y2), OnLevel2ButtonPressed, runtimeLevel2Color, runtimeButtonSize);
        level3ButtonObject = CreateRuntimeLevelButton(panel.transform, "Level3Button", runtimeLevel3Label, runtimeLevel3Icon, new Vector2(0f, y3), OnLevel3ButtonPressed, runtimeLevel3Color, runtimeButtonSize);
        CreateRuntimeLevelButton(panel.transform, "BackButton", runtimeBackLabel, runtimeBackIcon, new Vector2(0f, runtimeBackButtonY), OnSettingsBackPressed, runtimeBackColor, runtimeButtonSize);

        level1InfoText = CreateRuntimeInfoText(panel.transform, "Level1InfoText", new Vector2(0f, y1 - 35f));
        level2InfoText = CreateRuntimeInfoText(panel.transform, "Level2InfoText", new Vector2(0f, y2 - 35f));
        level3InfoText = CreateRuntimeInfoText(panel.transform, "Level3InfoText", new Vector2(0f, y3 - 35f));
    }

    private void EnsureRuntimePauseButton()
    {
        if (hudGroup == null || hudGroup.transform == null)
            return;

        if (pauseButtonObject != null)
        {
            ConfigurePauseButtonLayout(pauseButtonObject);
            return;
        }

        Transform existing = FindTransformByName("PauseButton");
        if (existing != null)
        {
            pauseButtonObject = existing.gameObject;
            ConfigurePauseButtonLayout(pauseButtonObject);
            return;
        }

        GameObject buttonGo = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        buttonGo.transform.SetParent(hudGroup.transform, false);
        ConfigurePauseButtonLayout(buttonGo);

        Image image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.09f, 0.12f, 0.18f, 0.88f);

        Outline outline = buttonGo.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textGo.GetComponent<TextMeshProUGUI>();
        label.text = "Pause";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 23f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.95f, 0.97f, 1f, 1f);

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(OnPauseButtonPressed);

        pauseButtonObject = buttonGo;
    }

    private void EnsureDeveloperCreditLabels()
    {
        if (!string.IsNullOrWhiteSpace(developerCreditText))
        {
            if (mainMenuGroup != null)
            {
                if (mainMenuDeveloperText == null)
                    mainMenuDeveloperText = FindTextByName("MainMenuDeveloperText");

                if (mainMenuDeveloperText == null)
                {
                    Transform mainMenuParent = mainMenuGroup.transform;
                    Canvas canvas = mainMenuGroup.GetComponentInParent<Canvas>();
                    if (canvas != null)
                        mainMenuParent = canvas.transform;

                    mainMenuDeveloperText = CreateDeveloperCreditText(mainMenuParent, "MainMenuDeveloperText", mainMenuDeveloperPosition, anchorBottomRight: true);
                }

                if (mainMenuDeveloperText != null)
                {
                    mainMenuDeveloperText.text = developerCreditText;
                    mainMenuDeveloperText.rectTransform.SetAsLastSibling();
                }
            }

            if (settingsGroup != null)
            {
                if (settingsDeveloperText == null)
                    settingsDeveloperText = FindTextByName("SettingsDeveloperText");

                if (settingsDeveloperText == null)
                    settingsDeveloperText = CreateDeveloperCreditText(settingsGroup.transform, "SettingsDeveloperText", settingsDeveloperPosition);

                if (settingsDeveloperText != null)
                    settingsDeveloperText.text = developerCreditText;
            }

        }
    }

    private void RemoveLegacyHudDeveloperText()
    {
        Transform legacy = FindTransformByName("HudDeveloperText");
        if (legacy == null)
            return;

        if (Application.isPlaying)
            Destroy(legacy.gameObject);
        else
            DestroyImmediate(legacy.gameObject);

        hudDeveloperText = null;
    }

    private static TextMeshProUGUI CreateDeveloperCreditText(Transform parent, string objectName, Vector2 anchoredPos, bool anchorBottomRight = false)
    {
        if (parent == null)
            return null;

        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        if (anchorBottomRight)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
        }
        rect.sizeDelta = new Vector2(760f, 42f);
        rect.anchoredPosition = anchoredPos;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.alignment = anchorBottomRight ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.Center;
        text.fontSize = 26f;
        text.color = new Color(0.93f, 0.96f, 1f, 0.95f);
        text.fontStyle = FontStyles.Bold;
        text.text = string.Empty;

        return text;
    }

#if UNITY_EDITOR
    private void EnsureEditorBrandingLabels()
    {
        if (string.IsNullOrWhiteSpace(developerCreditText))
            return;

        if (mainMenuGroup != null)
        {
            bool createdMainMenuLabel = false;
            if (mainMenuDeveloperText == null)
                mainMenuDeveloperText = FindTextByName("MainMenuDeveloperText");

            if (mainMenuDeveloperText == null)
            {
                Transform mainMenuParent = mainMenuGroup.transform;
                Canvas canvas = mainMenuGroup.GetComponentInParent<Canvas>();
                if (canvas != null)
                    mainMenuParent = canvas.transform;

                TextMeshProUGUI created = CreateDeveloperCreditText(mainMenuParent, "MainMenuDeveloperText", mainMenuDeveloperPosition, anchorBottomRight: true);
                if (created != null)
                {
                    Undo.RegisterCreatedObjectUndo(created.gameObject, "Create Main Menu Developer Text");
                    mainMenuDeveloperText = created;
                    createdMainMenuLabel = true;
                }
            }

            if (mainMenuDeveloperText != null)
            {
                mainMenuDeveloperText.text = developerCreditText;
                RectTransform rt = mainMenuDeveloperText.rectTransform;
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);

                if (createdMainMenuLabel)
                    rt.anchoredPosition = mainMenuDeveloperPosition;
                else
                    mainMenuDeveloperPosition = rt.anchoredPosition;
            }
        }

        if (settingsGroup != null)
        {
            bool createdSettingsLabel = false;
            if (settingsDeveloperText == null)
                settingsDeveloperText = FindTextByName("SettingsDeveloperText");

            if (settingsDeveloperText == null)
            {
                TextMeshProUGUI created = CreateDeveloperCreditText(settingsGroup.transform, "SettingsDeveloperText", settingsDeveloperPosition);
                if (created != null)
                {
                    Undo.RegisterCreatedObjectUndo(created.gameObject, "Create Settings Developer Text");
                    settingsDeveloperText = created;
                    createdSettingsLabel = true;
                }
            }

            if (settingsDeveloperText != null)
            {
                settingsDeveloperText.text = developerCreditText;

                if (createdSettingsLabel)
                    settingsDeveloperText.rectTransform.anchoredPosition = settingsDeveloperPosition;
                else
                    settingsDeveloperPosition = settingsDeveloperText.rectTransform.anchoredPosition;
            }
        }

        EditorUtility.SetDirty(this);
    }

    private void EnsureEditorPauseButton()
    {
        if (hudGroup == null || hudGroup.transform == null)
            return;

        if (pauseButtonObject == null)
        {
            Transform existing = FindTransformByName("PauseButton");
            if (existing != null)
                pauseButtonObject = existing.gameObject;
        }

        if (pauseButtonObject == null)
        {
            GameObject buttonGo = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create Pause Button");
            buttonGo.transform.SetParent(hudGroup.transform, false);

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.09f, 0.12f, 0.18f, 0.88f);

            Outline outline = buttonGo.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.4f);
            outline.effectDistance = new Vector2(1f, -1f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(textGo, "Create Pause Button Text");
            textGo.transform.SetParent(buttonGo.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textGo.GetComponent<TextMeshProUGUI>();
            label.text = "Pause";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 23f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.95f, 0.97f, 1f, 1f);

            pauseButtonObject = buttonGo;
        }

        ConfigurePauseButtonLayout(pauseButtonObject);
        EditorUtility.SetDirty(this);
    }
#endif

    private void ConfigurePauseButtonLayout(GameObject buttonObject)
    {
        if (buttonObject == null)
            return;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = pauseButtonSize;
        rect.anchoredPosition = pauseButtonOffset;

        if (pauseButtonBringToFront)
            rect.SetAsLastSibling();
    }

    private GameObject CreateRuntimeLevelButton(Transform parent, string objectName, string label, string iconLabel, Vector2 anchoredPos, UnityAction onClick, Color buttonColor, Vector2 size)
    {
        GameObject buttonGo = new GameObject(objectName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button), typeof(Outline), typeof(Shadow));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;

        Image img = buttonGo.GetComponent<Image>();
        img.color = buttonColor;

        Outline outline = buttonGo.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        outline.effectDistance = new Vector2(1f, -1f);

        Shadow shadow = buttonGo.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        shadow.effectDistance = new Vector2(0f, -2f);

        runtimeLevelCards.Add(buttonGo.GetComponent<CanvasGroup>());

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        GameObject iconBgGo = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
        iconBgGo.transform.SetParent(buttonGo.transform, false);
        RectTransform iconBgRect = iconBgGo.GetComponent<RectTransform>();
        iconBgRect.anchorMin = new Vector2(0f, 0.5f);
        iconBgRect.anchorMax = new Vector2(0f, 0.5f);
        iconBgRect.sizeDelta = new Vector2(36f, 36f);
        iconBgRect.anchoredPosition = new Vector2(26f, 0f);
        Image iconBg = iconBgGo.GetComponent<Image>();
        iconBg.color = new Color(1f, 1f, 1f, 0.2f);

        GameObject iconTextGo = new GameObject("IconText", typeof(RectTransform), typeof(TextMeshProUGUI));
        iconTextGo.transform.SetParent(iconBgGo.transform, false);
        RectTransform iconTextRect = iconTextGo.GetComponent<RectTransform>();
        iconTextRect.anchorMin = Vector2.zero;
        iconTextRect.anchorMax = Vector2.one;
        iconTextRect.offsetMin = Vector2.zero;
        iconTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI iconText = iconTextGo.GetComponent<TextMeshProUGUI>();
        iconText.text = iconLabel;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = Color.white;
        iconText.fontSize = 24f;
        iconText.fontStyle = FontStyles.Bold;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(56f, 0f);
        textRect.offsetMax = new Vector2(-14f, 0f);

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.fontSize = 32f;
        text.fontStyle = FontStyles.Bold;

        return buttonGo;
    }

    private static void CreateRuntimeTitle(Transform parent, string title, Vector2 anchoredPos)
    {
        GameObject titleGo = new GameObject("LevelSelectTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(parent, false);
        RectTransform rect = titleGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(680f, 70f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;

        TextMeshProUGUI text = titleGo.GetComponent<TextMeshProUGUI>();
        text.text = title;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.92f, 0.98f, 1f, 1f);
        text.fontSize = 56f;
        text.fontStyle = FontStyles.Bold;
    }

    private static TextMeshProUGUI CreateRuntimeInfoText(Transform parent, string objectName, Vector2 anchoredPos)
    {
        GameObject infoGo = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        infoGo.transform.SetParent(parent, false);
        RectTransform rect = infoGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(460f, 32f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;

        TextMeshProUGUI text = infoGo.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.84f, 0.92f, 0.98f, 0.95f);
        text.fontSize = 22f;
        return text;
    }

    private IEnumerator AnimateRuntimeLevelSelectIn()
    {
        if (runtimeLevelSelectGroup == null)
            yield break;

        float panelTime = 0f;
        const float panelDuration = 0.22f;
        while (panelTime < panelDuration)
        {
            panelTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(panelTime / panelDuration);
            runtimeLevelSelectGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        runtimeLevelSelectGroup.alpha = 1f;

        for (int i = 0; i < runtimeLevelCards.Count; i++)
        {
            CanvasGroup card = runtimeLevelCards[i];
            if (card == null) continue;
            RectTransform rect = card.transform as RectTransform;
            if (rect == null) continue;

            Vector2 targetPos = rect.anchoredPosition;
            Vector2 startPos = targetPos + new Vector2(0f, 12f);
            float elapsed = 0f;
            const float duration = 0.16f;

            card.alpha = 0f;
            rect.anchoredPosition = startPos;
            rect.localScale = new Vector3(0.96f, 0.96f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                card.alpha = eased;
                rect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, eased);
                rect.localScale = Vector3.LerpUnclamped(new Vector3(0.96f, 0.96f, 1f), Vector3.one, eased);
                yield return null;
            }

            card.alpha = 1f;
            rect.anchoredPosition = targetPos;
            rect.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.03f);
        }
    }

    private void ApplyLevelButtonState(int levelIndex, GameObject buttonObject, TextMeshProUGUI infoText)
    {
        if (buttonObject == null) return;

        bool unlocked = GameManager.Instance != null && GameManager.Instance.IsLevelUnlocked(levelIndex);
        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
            button.interactable = unlocked;

        int best = GameManager.Instance != null ? GameManager.Instance.GetLevelBestScore(levelIndex) : 0;
        int required = GameManager.Instance != null ? GameManager.Instance.GetRequiredCoinsForLevel(levelIndex) : 0;

        if (infoText != null)
            infoText.text = unlocked
                ? $"Best {best:D6} | Need Anter {required:D2}"
                : "LOCKED";
    }

    private IEnumerator ScorePopRoutine()
    {
        if (scoreText == null)
        {
            scorePopRoutine = null;
            yield break;
        }
        RectTransform scoreRect = scoreText.rectTransform;
        if (!scoreBaseScaleInitialized)
        {
            scoreBaseScale = scoreRect.localScale;
            scoreBaseScaleInitialized = true;
        }

        Vector3 baseScale = scoreBaseScale;
        Vector3 peakScale = baseScale * 1.2f;

        yield return ScaleTransform(scoreRect, baseScale, peakScale, 0.08f);
        yield return ScaleTransform(scoreRect, peakScale, baseScale, 0.12f);

        scoreRect.localScale = baseScale;
        scorePopRoutine = null;
    }

    private static IEnumerator ScaleTransform(RectTransform target, Vector3 startScale, Vector3 endScale, float duration)
    {
        if (target == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            yield return null;
        }
        if (target != null)
            target.localScale = endScale;
    }

    private static void SetGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        if (group.GetComponent<RectTransform>() == null) return;
        group.gameObject.SetActive(true);
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void ApplyHudVisualStyle()
    {
        if (!autoStyleHudText)
            return;

        if (scoreText != null)
        {
            scoreText.fontSize = hudScoreFontSize;
            scoreText.color = hudAccentTextColor;
            scoreText.fontStyle = FontStyles.Normal;
        }

        if (livesText != null)
        {
            livesText.fontSize = hudSmallFontSize;
            livesText.color = hudPrimaryTextColor;
            livesText.fontStyle = FontStyles.Bold;
        }

        if (coinText != null)
        {
            coinText.fontSize = hudSmallFontSize;
            coinText.color = hudPrimaryTextColor;
            coinText.fontStyle = FontStyles.Bold;
        }

        if (timerText != null)
        {
            timerText.fontSize = hudSmallFontSize;
            timerText.fontStyle = FontStyles.Bold;
        }
    }

    private void ResolveSceneReferences()
    {
        if (mainMenuGroup == null) mainMenuGroup = FindGroupByName("MainMenuPanel");
        if (levelSelectGroup == null) levelSelectGroup = FindGroupByName("LevelSelectPanel");
        if (settingsGroup == null) settingsGroup = FindGroupByName("SettingsPanel");
        if (hudGroup == null) hudGroup = FindGroupByName("HUDPanel");
        if (gameOverGroup == null) gameOverGroup = FindGroupByName("GameOverPanel");
        if (levelCompleteGroup == null) levelCompleteGroup = FindGroupByName("LevelCompletePanel");
        if (winGroup == null) winGroup = FindGroupByName("WinPanel");
        if (fadeGroup == null) fadeGroup = FindGroupByName("FadePanel");
        if (pauseGroup == null) pauseGroup = FindGroupByName("PausePanel");
        if (scoreText == null) scoreText = FindTextByName("ScoreText");
        if (mainMenuDeveloperText == null) mainMenuDeveloperText = FindTextByName("MainMenuDeveloperText");
        if (settingsDeveloperText == null) settingsDeveloperText = FindTextByName("SettingsDeveloperText");
        if (hudDeveloperText == null) hudDeveloperText = FindTextByName("HudDeveloperText");
        if (levelCompleteText == null) levelCompleteText = FindTextByName("LevelCompleteText");
        if (livesText == null) livesText = FindTextByName("LivesText");
        if (coinText == null) coinText = FindTextByName("CoinText");
        if (timerText == null) timerText = FindTextByName("TimerText");
        if (pauseButtonObject == null)
        {
            Transform pauseButton = FindTransformByName("PauseButton");
            pauseButtonObject = pauseButton != null ? pauseButton.gameObject : null;
        }
        if (gameOverText == null) gameOverText = FindTextByName("GameOverText");
        if (winScoreText == null) winScoreText = FindTextByName("WinScoreText");
        if (winCoinsText == null) winCoinsText = FindTextByName("WinCoinsText");

        if (floatingTextRoot == null)
        {
            Canvas rootCanvas = Object.FindAnyObjectByType<Canvas>();
            if (rootCanvas != null)
                floatingTextRoot = rootCanvas.transform as RectTransform;
        }

        if (nextLevelButtonObject == null)
        {
            Transform nextButton = FindTransformByName("NextLevelButton");
            nextLevelButtonObject = nextButton != null ? nextButton.gameObject : null;
        }

        if (mainMenuButtonObject == null)
        {
            Transform menuButton = FindTransformByName("MainMenuButton");
            mainMenuButtonObject = menuButton != null ? menuButton.gameObject : null;
        }

        if (retryButtonObject == null)
        {
            Transform retryButton = FindTransformByName("RetryButton");
            retryButtonObject = retryButton != null ? retryButton.gameObject : null;
        }

        if (musicVolumeSlider == null)
        {
            Transform t = FindTransformByName("MusicVolumeSlider");
            if (t != null) musicVolumeSlider = t.GetComponent<Slider>();
        }
        if (sfxVolumeSlider == null)
        {
            Transform t = FindTransformByName("SfxVolumeSlider");
            if (t != null) sfxVolumeSlider = t.GetComponent<Slider>();
        }

        if (gameOverMenuButtonObject == null)
        {
            Transform goMenuButton = FindTransformByName("GameOverMenuButton");
            gameOverMenuButtonObject = goMenuButton != null ? goMenuButton.gameObject : null;
        }

        if (level1ButtonObject == null)
        {
            Transform t = FindTransformByName("Level1Button");
            level1ButtonObject = t != null ? t.gameObject : null;
        }

        if (level2ButtonObject == null)
        {
            Transform t = FindTransformByName("Level2Button");
            level2ButtonObject = t != null ? t.gameObject : null;
        }

        if (level3ButtonObject == null)
        {
            Transform t = FindTransformByName("Level3Button");
            level3ButtonObject = t != null ? t.gameObject : null;
        }

        if (level1InfoText == null) level1InfoText = FindTextByName("Level1InfoText");
        if (level2InfoText == null) level2InfoText = FindTextByName("Level2InfoText");
        if (level3InfoText == null) level3InfoText = FindTextByName("Level3InfoText");
    }

    private static CanvasGroup FindGroupByName(string objectName)
    {
        Transform target = FindTransformByName(objectName);
        if (target == null) return null;

        RectTransform rt = target as RectTransform;
        if (rt == null)
        {
            rt = target.GetComponent<RectTransform>();
            if (rt == null) return null;
        }

        Canvas parentCanvas = target.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return null;

        return target.GetComponent<CanvasGroup>();
    }

    private static TextMeshProUGUI FindTextByName(string objectName)
    {
        Transform target = FindTransformByName(objectName);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Transform FindTransformByName(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded) return null;

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindInChildrenRecursive(roots[i].transform, objectName);
            if (found != null) return found;
        }

        return null;
    }

    private static Transform FindInChildrenRecursive(Transform current, string targetName)
    {
        if (current.name == targetName) return current;
        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindInChildrenRecursive(current.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }
}
