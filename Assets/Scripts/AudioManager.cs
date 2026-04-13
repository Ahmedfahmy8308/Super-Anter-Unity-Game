using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer (Optional)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string sfxVolumeParameter = "SfxVolume";
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private GameData gameData;

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip starMusic;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.75f;

    [Header("SFX - Core")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip deathClip;

    [Header("SFX - Combat")]
    [SerializeField] private AudioClip stompClip;
    [SerializeField] private AudioClip kickClip;

    [Header("SFX - Power-Ups")]
    [SerializeField] private AudioClip powerUpClip;
    [SerializeField] private AudioClip powerDownClip;
    [SerializeField] private AudioClip oneUpClip;

    [Header("SFX - Blocks")]
    [SerializeField] private AudioClip blockBumpClip;
    [SerializeField] private AudioClip blockBreakClip;

    [Header("SFX - Environment")]
    [SerializeField] private AudioClip pipeClip;
    [SerializeField] private AudioClip levelCompleteClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip flagpoleClip;
    [SerializeField] private AudioClip levelCompleteFollowupClip;
    [SerializeField] private AudioClip gameOverFollowupClip;
    [Min(0f)]
    [SerializeField] private float levelCompleteFollowupDelay = 0.75f;
    [Min(0f)]
    [SerializeField] private float gameOverFollowupDelay = 0.75f;

    [Header("SFX Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Pitch Variation")]
    [SerializeField] private Vector2 jumpPitchRange = new Vector2(0.94f, 1.06f);
    [SerializeField] private Vector2 coinPitchRange = new Vector2(0.92f, 1.08f);
    [SerializeField] private Vector2 buttonPitchRange = new Vector2(0.98f, 1.02f);
    [SerializeField] private Vector2 deathPitchRange = new Vector2(0.98f, 1.02f);
    [SerializeField] private float globalPitchMultiplier = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    /// <summary>Long UI stingers (game over, level clear) — not affected by death slow-mo pitch on <see cref="sfxSource"/>.</summary>
    private AudioSource stingerSource;
    private AudioClip normalMusic;
    private bool playingStarMusic;
    private Coroutine followupRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        stingerSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        stingerSource.playOnAwake = false;
        stingerSource.loop = false;
        stingerSource.priority = 0;

        if (musicMixerGroup != null) musicSource.outputAudioMixerGroup = musicMixerGroup;
        if (sfxMixerGroup != null) sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        if (sfxMixerGroup != null) stingerSource.outputAudioMixerGroup = sfxMixerGroup;

        if (gameData != null)
        {
            jumpPitchRange = new Vector2(gameData.jumpPitchMin, gameData.jumpPitchMax);
            musicVolume = gameData.musicVolume;
            sfxVolume = gameData.sfxVolume;
        }

        ApplyVolumes();
        ApplyGlobalPitch();

        normalMusic = backgroundMusic;
        if (backgroundMusic != null)
            PlayMusic(backgroundMusic);
    }

    private void Start()
    {
        EnsureAudioListenerExists();
        // Streaming / background-loaded clips may not start in Awake; retry once loaded.
        if (backgroundMusic != null && musicSource != null && !musicSource.isPlaying)
        {
            PlayMusic(backgroundMusic);
            normalMusic = backgroundMusic;
        }
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioListenerExists();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlayJump() => PlaySfx(jumpClip, 1f, jumpPitchRange);
    public void PlayCoin() => PlaySfx(coinClip, 1f, coinPitchRange);
    public void PlayButtonClick() => PlaySfx(buttonClickClip, 1f, buttonPitchRange);
    public void PlayDeath() => PlaySfx(deathClip, 1f, deathPitchRange);

    public void PlayEnemyStomp() => PlaySfx(stompClip, 1f, new Vector2(0.95f, 1.05f));
    public void PlayKick() => PlaySfx(kickClip, 1f, new Vector2(0.95f, 1.05f));
    public void PlayPowerUp() => PlaySfx(powerUpClip, 1f, new Vector2(1f, 1f));
    public void PlayPowerDown() => PlaySfx(powerDownClip, 1f, new Vector2(1f, 1f));
    public void Play1UP() => PlaySfx(oneUpClip, 1f, new Vector2(1f, 1f));
    public void PlayBlockBump() => PlaySfx(blockBumpClip, 1f, new Vector2(0.95f, 1.05f));
    public void PlayBlockBreak() => PlaySfx(blockBreakClip, 1f, new Vector2(0.9f, 1.1f));
    public void PlayPipeSound() => PlaySfx(pipeClip, 1f, new Vector2(1f, 1f));
    public void PlayFlagpole() => PlaySfx(flagpoleClip, 1f, new Vector2(1f, 1f));

    public void PlayLevelComplete()
    {
        musicSource.Stop();
        playingStarMusic = false;
        SetGlobalPitchMultiplier(1f);
        PlayStinger(levelCompleteClip);
        ScheduleStingerFollowup(levelCompleteFollowupClip != null ? levelCompleteFollowupClip : flagpoleClip, levelCompleteFollowupDelay);
    }

    public void PlayGameOver()
    {
        musicSource.Stop();
        playingStarMusic = false;
        SetGlobalPitchMultiplier(1f);
        PlayStinger(gameOverClip);
        ScheduleStingerFollowup(gameOverFollowupClip != null ? gameOverFollowupClip : deathClip, gameOverFollowupDelay);
    }

    private void PlayStinger(AudioClip clip)
    {
        if (clip == null || stingerSource == null) return;
        stingerSource.pitch = 1f;
        stingerSource.PlayOneShot(clip, sfxVolume);
    }

    private void ScheduleStingerFollowup(AudioClip clip, float delay)
    {
        if (followupRoutine != null)
            StopCoroutine(followupRoutine);

        if (clip == null || stingerSource == null)
        {
            followupRoutine = null;
            return;
        }

        followupRoutine = StartCoroutine(PlayFollowupAfterDelay(clip, Mathf.Max(0f, delay)));
    }

    private IEnumerator PlayFollowupAfterDelay(AudioClip clip, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        PlayStinger(clip);
        followupRoutine = null;
    }

    public void PlayStarMusic()
    {
        if (starMusic == null) return;
        normalMusic = musicSource.clip;
        playingStarMusic = true;
        musicSource.clip = starMusic;
        musicSource.Play();
    }

    public void RestoreNormalMusic()
    {
        if (!playingStarMusic) return;
        playingStarMusic = false;
        if (normalMusic != null)
        {
            musicSource.clip = normalMusic;
            musicSource.Play();
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetGlobalPitchMultiplier(float multiplier)
    {
        globalPitchMultiplier = Mathf.Max(0.01f, multiplier);
        ApplyGlobalPitch();
    }

    public void ApplyGameData(GameData data)
    {
        gameData = data;
        if (gameData == null) return;
        musicVolume = gameData.musicVolume;
        sfxVolume = gameData.sfxVolume;
        jumpPitchRange = new Vector2(gameData.jumpPitchMin, gameData.jumpPitchMax);
        ApplyVolumes();
    }

    public void ApplyRuntimeData(GameDataRuntimeData data)
    {
        if (data == null || data.audio == null) return;
        musicVolume = data.audio.musicVolume;
        sfxVolume = data.audio.sfxVolume;
        jumpPitchRange = new Vector2(data.audio.jumpPitchMin, data.audio.jumpPitchMax);
        ApplyVolumes();
    }

    public void PlaySfxAtPitch(AudioClip clip, float volumeScale, float pitch)
    {
        if (clip == null || sfxSource == null) return;
        float finalPitch = Mathf.Max(0.01f, pitch * globalPitchMultiplier);
        sfxSource.pitch = finalPitch;
        sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
        sfxSource.pitch = globalPitchMultiplier;
    }

    private void PlaySfx(AudioClip clip, float volumeScale, Vector2 pitchRange)
    {
        if (clip == null || sfxSource == null) return;
        float pitch = Random.Range(pitchRange.x, pitchRange.y) * globalPitchMultiplier;
        sfxSource.pitch = Mathf.Max(0.01f, pitch);
        sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
        sfxSource.pitch = globalPitchMultiplier;
    }

    private void ApplyVolumes()
    {
        if (mixer != null)
        {
            if (!string.IsNullOrEmpty(musicVolumeParameter))
                mixer.SetFloat(musicVolumeParameter, LinearToDecibels(musicVolume));
            if (!string.IsNullOrEmpty(sfxVolumeParameter))
                mixer.SetFloat(sfxVolumeParameter, LinearToDecibels(sfxVolume));
            musicSource.volume = 1f;
            sfxSource.volume = 1f;
            if (stingerSource != null) stingerSource.volume = 1f;
        }
        else
        {
            musicSource.volume = musicVolume;
            sfxSource.volume = 1f;
            if (stingerSource != null) stingerSource.volume = 1f;
        }
    }

    private void ApplyGlobalPitch()
    {
        if (musicSource != null) musicSource.pitch = globalPitchMultiplier;
        if (sfxSource != null) sfxSource.pitch = globalPitchMultiplier;
        if (stingerSource != null) stingerSource.pitch = 1f;
    }

    private static float LinearToDecibels(float volume)
    {
        if (volume <= 0.0001f) return -80f;
        return Mathf.Log10(volume) * 20f;
    }

    private static void EnsureAudioListenerExists()
    {
        if (Object.FindAnyObjectByType<AudioListener>(FindObjectsInactive.Exclude) != null)
            return;
        Camera cam = Camera.main;
        if (cam == null)
            cam = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
        if (cam != null && cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();
    }
}
