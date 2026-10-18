using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip titleScreen;
    [SerializeField] private AudioClip startLevel;   // plays once when a stage begins
    [SerializeField] private AudioClip stageTheme;   // loops after startLevel finishes

    [Header("Death / Game Over")]
    [SerializeField] private AudioClip playerDied;
    [SerializeField] private AudioClip gameOver;

    [Tooltip("Optional: a different theme per stage. Index 0 = Stage 1. Empty slots fall back to Stage Theme.")]
    [SerializeField] private AudioClip[] perStageThemes;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float pausedMusicMultiplier = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    [Header("Scenes")]
    [SerializeField] private int mainMenuBuildIndex = 0;
    [SerializeField] private int winSceneBuildIndex = 5;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private Coroutine musicRoutine;
    private bool ducked = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // DontDestroyOnLoad only works on root objects; if this sits under the persistent root, that root already survives
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D sound

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // sceneLoaded doesn't fire for the scene the game launched into, so handle it once here
        HandleScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene.buildIndex);
    }

    void HandleScene(int buildIndex)
    {
        if (musicRoutine != null)
        {
            StopCoroutine(musicRoutine);
            musicRoutine = null;
        }

        if (buildIndex == mainMenuBuildIndex)
            musicRoutine = StartCoroutine(SwitchTo(titleScreen, null, true));
        else if (buildIndex == winSceneBuildIndex)
            musicRoutine = StartCoroutine(SwitchTo(null, null, false));
        else
            musicRoutine = StartCoroutine(SwitchTo(startLevel, GetThemeForStage(buildIndex), false));
    }

    AudioClip GetThemeForStage(int buildIndex)
    {
        int i = buildIndex - 1; // Stage 1 = build index 1
        if (perStageThemes != null && i >= 0 && i < perStageThemes.Length && perStageThemes[i] != null)
            return perStageThemes[i];
        return stageTheme;
    }

    // first: plays first. loopFirst: whether it loops. then: (optional) looping clip that follows once 'first' ends.
    IEnumerator SwitchTo(AudioClip first, AudioClip then, bool loopFirst)
    {
        yield return FadeOutCurrent();

        if (first == null)
            yield break;

        musicSource.clip = first;
        musicSource.loop = loopFirst;
        musicSource.volume = CurrentMusicVolume();
        musicSource.Play();

        if (!loopFirst && then != null)
        {
            // Realtime wait so pausing the game (timeScale 0) can't stall the hand-off
            yield return new WaitForSecondsRealtime(first.length);

            musicSource.clip = then;
            musicSource.loop = true;
            musicSource.volume = CurrentMusicVolume();
            musicSource.Play();
        }
    }

    IEnumerator FadeOutCurrent()
    {
        if (!musicSource.isPlaying)
            yield break;

        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutDuration);
            yield return null;
        }

        musicSource.Stop();
    }

    float CurrentMusicVolume() => ducked ? musicVolume * pausedMusicMultiplier : musicVolume;

    // ---------- Public API ----------

    // Lower the music while the pause menu is open
    public void SetDucked(bool value)
    {
        ducked = value;
        if (musicSource != null && musicSource.isPlaying)
            musicSource.volume = CurrentMusicVolume();
    }

    // For sound effects later: AudioManager.Instance?.PlaySfx(clip);
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    public void StopMusic()
    {
        if (musicRoutine != null)
            StopCoroutine(musicRoutine);

        musicRoutine = StartCoroutine(SwitchTo(null, null, false));
    }
    public float PlayerDiedLength => playerDied != null ? playerDied.length : 0f;

    public void PlayPlayerDied()
    {
        StopMusic();            // stage theme stops right away
        PlaySfx(playerDied);    // "died" sound plays on top of the fade
    }

    public void PlayGameOver()
    {
        if (musicRoutine != null)
            StopCoroutine(musicRoutine);

        musicRoutine = StartCoroutine(SwitchTo(gameOver, null, false)); // plays once, no loop
    }
}