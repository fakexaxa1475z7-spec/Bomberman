using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas mainCanvas;


    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI lifeText;
    [SerializeField] private CanvasGroup hudCanvasGroup;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float delayBeforeGameOver = 1f;

    [Header("Pause")]
    [SerializeField] private GameObject pausePanel;

    [Header("Win Screen")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Scene")]
    [SerializeField] private int mainMenuBuildIndex = 0;
    [SerializeField] private int winSceneBuildIndex = 5;

    public int CurrentScore => score;

    private int score = 0;
    private float stageTimer = 0f;
    private bool timerRunning = false;
    private PlayerHealth trackedPlayerHealth;
    private PlayerGridMovement trackedMovement;
    private BombPlacer trackedBombPlacer;

    private bool isPaused = false;
    private bool gameOverActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        UpdateScoreText();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        int activeIndex = SceneManager.GetActiveScene().buildIndex;

        if (winPanel != null)
            winPanel.SetActive(activeIndex == winSceneBuildIndex);

        SetHUDVisible(!IsNonGameplayScene(activeIndex));
    }

    bool IsNonGameplayScene(int buildIndex)
    {
        return buildIndex == mainMenuBuildIndex || buildIndex == winSceneBuildIndex;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isWinScene = scene.buildIndex == winSceneBuildIndex;
        bool isNonGameplay = IsNonGameplayScene(scene.buildIndex);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(isWinScene);

        if (isWinScene && finalScoreText != null)
            finalScoreText.text = $"Final Score: {score}";

        isPaused = false;
        gameOverActive = false;
        Time.timeScale = 1f;

        if (!isNonGameplay)
        {
            ResetTimer();
            StartCoroutine(FindAndTrackPlayer());
        }
        else
        {
            timerRunning = false;
            SetHUDVisible(false);
        }

        if (mainCanvas != null)
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera != null)
                mainCanvas.worldCamera = sceneCamera;
            else
                Debug.LogWarning($"HUDManager: no Camera.main found in scene '{scene.name}' to assign to Canvas.");
        }
    }

    IEnumerator FindAndTrackPlayer()
    {
        PlayerHealth player = null;
        float timeout = 3f;
        float elapsed = 0f;

        while (player == null && elapsed < timeout)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            if (player == null)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (trackedPlayerHealth != null)
        {
            trackedPlayerHealth.OnHealthChanged -= UpdateLifeText;
            trackedPlayerHealth.OnDeath -= HandlePlayerDeath;
        }

        trackedPlayerHealth = player;
        trackedMovement = player != null ? player.GetComponent<PlayerGridMovement>() : null;
        trackedBombPlacer = player != null ? player.GetComponent<BombPlacer>() : null;

        if (trackedPlayerHealth != null)
        {
            trackedPlayerHealth.OnHealthChanged += UpdateLifeText;
            trackedPlayerHealth.OnDeath += HandlePlayerDeath;

            // Carry health over from the previous stage, instead of letting the new player default to full
            if (persistedHealth >= 0)
                trackedPlayerHealth.SetHealth(persistedHealth);

            UpdateLifeText(trackedPlayerHealth.CurrentHealth, trackedPlayerHealth.MaxHealth);
            timerRunning = true;
        }
        else
        {
            Debug.LogWarning("HUDManager: no PlayerHealth found in scene within timeout.");
        }
    }

    void HandlePlayerDeath()
    {
        timerRunning = false;
        gameOverActive = true;
        Invoke(nameof(ShowGameOverPanel), delayBeforeGameOver);
    }

    void ShowGameOverPanel()
    {
        SetHUDVisible(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void OnRetryPressed()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        StageFlowManager.Instance?.LoadSceneWithFade(currentIndex, null);
    }

    public void OnReturnToMenuPressed()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        StageFlowManager.Instance?.ReturnToMainMenu();
    }

    public void OnPlayAgainPressed()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        StageFlowManager.Instance?.LoadSceneWithFade(1, "Stage 1");
    }

    void LateUpdate()
    {
        HandlePauseInput();
    }

    void HandlePauseInput()
    {
        bool isNonGameplay = IsNonGameplayScene(SceneManager.GetActiveScene().buildIndex);
        if (isNonGameplay) return;

        bool isTransitioning = StageFlowManager.Instance != null && StageFlowManager.Instance.IsTransitioning;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else if (!gameOverActive && !isTransitioning)
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;

        SetPlayerControlsEnabled(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        StartCoroutine(ReenableControlsNextFrame());
    }

    IEnumerator ReenableControlsNextFrame()
    {
        yield return null;
        SetPlayerControlsEnabled(true);
    }

    void SetPlayerControlsEnabled(bool enabled)
    {
        if (trackedMovement != null) trackedMovement.enabled = enabled;
        if (trackedBombPlacer != null) trackedBombPlacer.enabled = enabled;
    }

    public void OnResumePressed()
    {
        ResumeGame();
    }

    public void OnPauseRetryPressed()
    {
        Time.timeScale = 1f;
        ResumeGame();

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        StageFlowManager.Instance?.LoadSceneWithFade(currentIndex, null);
    }

    public void OnPauseReturnToMenuPressed()
    {
        Time.timeScale = 1f;
        ResumeGame();

        StageFlowManager.Instance?.ReturnToMainMenu();
    }

    void SetHUDVisible(bool visible)
    {
        if (hudCanvasGroup == null) return;
        hudCanvasGroup.alpha = visible ? 1f : 0f;
        hudCanvasGroup.blocksRaycasts = visible;
    }

    public void ShowHUD() => SetHUDVisible(true);
    public void HideHUD() => SetHUDVisible(false);

    void Update()
    {
        if (!timerRunning) return;

        stageTimer += Time.deltaTime;
        //UpdateTimeText();
    }

    void ResetTimer()
    {
        stageTimer = 0f;
        timerRunning = false;
        UpdateTimeText();
    }

    void UpdateTimeText()
    {
        if (timeText == null) return;

        int minutes = Mathf.FloorToInt(stageTimer / 60f);
        int seconds = Mathf.FloorToInt(stageTimer % 60f);
        timeText.text = $"TIME {seconds:0}";
    }

    private int persistedHealth = -1; // -1 = no persisted value yet, use the player's own default max health

    void UpdateLifeText(int current, int max)
    {
        if (lifeText == null) return;
        lifeText.text = $"LEFT {current}";

        persistedHealth = current; // remember this for the next stage's player
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    public void ResetScore()
    {
        score = 0;
        persistedHealth = -1; // fresh run — next spawn uses full default health
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        if (scoreText == null) return;
        scoreText.text = $"{score}";
    }

}