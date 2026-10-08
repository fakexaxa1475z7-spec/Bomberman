using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

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
    [SerializeField] private Canvas winCanvas;

    [Header("Save Code")]
    [SerializeField] private GameObject saveCodePanel;
    [SerializeField] private TextMeshProUGUI saveCodeText;

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

    private int persistedHealth = -1;
    private bool hasPendingSpawnPos = false;
    private Vector2Int pendingSpawnPos;
    private float pendingTimeSeconds = -1f;

    // Saved map waiting to be applied by GridManager when the stage loads
    private SaveSlotData pendingGridData;
    public bool HasPendingGridData => pendingGridData != null;
    public SaveSlotData PeekPendingGridData() => pendingGridData;
    public void ClearPendingGridData() => pendingGridData = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        UpdateScoreText();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (saveCodePanel != null) saveCodePanel.SetActive(false);

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

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (saveCodePanel != null) saveCodePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(isWinScene);

        if (isWinScene && finalScoreText != null)
            finalScoreText.text = $"Final Score: {score}";

        if (isWinScene && winCanvas != null)
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera != null)
                winCanvas.worldCamera = sceneCamera;
        }

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

            if (persistedHealth >= 0)
                trackedPlayerHealth.SetHealth(persistedHealth);

            if (hasPendingSpawnPos && trackedMovement != null)
            {
                hasPendingSpawnPos = false;

                Vector2Int target = pendingSpawnPos;
                if (!GridManager.Instance.IsWalkable(target))
                    target = GridManager.Instance.SpawnPoints[0]; // saved tile is blocked, use the spawn corner

                trackedMovement.transform.position = GridManager.Instance.GridToWorld(target);
            }

            if (pendingTimeSeconds >= 0f)
            {
                stageTimer = pendingTimeSeconds;
                pendingTimeSeconds = -1f;
                UpdateTimeText();
            }

            UpdateLifeText(trackedPlayerHealth.CurrentHealth, trackedPlayerHealth.MaxHealth);
            timerRunning = true;
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
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void OnRetryPressed()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        persistedHealth = -1;
        pendingTimeSeconds = -1f;
        pendingGridData = null;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        StageFlowManager.Instance?.LoadSceneWithFade(currentIndex, null);
    }

    public void OnReturnToMenuPressed()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        StageFlowManager.Instance?.ReturnToMainMenu();
    }

    public void OnPlayAgainPressed()
    {
        if (winPanel != null) winPanel.SetActive(false);
        persistedHealth = -1;
        pendingTimeSeconds = -1f;
        pendingGridData = null;

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
                ResumeGame();
            else if (!gameOverActive && !isTransitioning)
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;
        SetPlayerControlsEnabled(false);

        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (saveCodePanel != null) saveCodePanel.SetActive(false);

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

    public void OnResumePressed() => ResumeGame();

    public void OnPauseRetryPressed()
    {
        Time.timeScale = 1f;
        ResumeGame();
        persistedHealth = -1;
        pendingTimeSeconds = -1f;
        pendingGridData = null;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        StageFlowManager.Instance?.LoadSceneWithFade(currentIndex, null);
    }

    public void OnPauseReturnToMenuPressed()
    {
        Time.timeScale = 1f;
        ResumeGame();

        StageFlowManager.Instance?.ReturnToMainMenu();
    }

    // ---------- Save / Load ----------

    public void OnSavePressed()
    {
        if (trackedPlayerHealth == null || trackedMovement == null || GridManager.Instance == null)
        {
            Debug.LogWarning("HUDManager: cannot save, player or grid not currently tracked.");
            return;
        }

        var exitPos = GridManager.Instance.ExitTilePos;

        var data = new SaveSlotData
        {
            stageIndex = SceneManager.GetActiveScene().buildIndex,
            health = trackedPlayerHealth.CurrentHealth,
            score = score,
            gridX = trackedMovement.CurrentGridPos.x,
            gridY = trackedMovement.CurrentGridPos.y,
            timeSeconds = stageTimer,
            blockGrid = GridManager.Instance.GetBlockGridString(),
            hasExit = exitPos.HasValue,
            exitX = exitPos?.x ?? 0,
            exitY = exitPos?.y ?? 0
        };

        string code = SaveSystem.GenerateUniqueCode();
        SaveSystem.Save(code, data);

        if (saveCodeText != null)
            saveCodeText.text = code;

        if (saveCodePanel != null)
            saveCodePanel.SetActive(true);
    }

    public void OnCopyCodePressed()
    {
        if (saveCodeText != null)
            GUIUtility.systemCopyBuffer = saveCodeText.text;
    }

    public void OnCloseSaveCodePressed()
    {
        if (saveCodePanel != null)
            saveCodePanel.SetActive(false);
    }

    public bool TryContinueFromCode(string code, out string errorMessage)
    {
        errorMessage = null;

        if (!SaveSystem.TryLoad(code, out var data))
        {
            errorMessage = "Save not found.";
            return false;
        }

        if (data.stageIndex <= mainMenuBuildIndex ||
            data.stageIndex >= SceneManager.sceneCountInBuildSettings ||
            data.stageIndex == winSceneBuildIndex)
        {
            errorMessage = "Invalid save.";
            return false;
        }

        persistedHealth = data.health;
        score = data.score;
        UpdateScoreText();

        hasPendingSpawnPos = true;
        pendingSpawnPos = new Vector2Int(data.gridX, data.gridY);

        pendingTimeSeconds = data.timeSeconds;
        pendingGridData = data;

        StageFlowManager.Instance?.LoadSceneWithFade(data.stageIndex, $"Stage {data.stageIndex}");
        return true;
    }

    // ---------- HUD visibility ----------

    void SetHUDVisible(bool visible)
    {
        if (hudCanvasGroup == null) return;
        hudCanvasGroup.alpha = visible ? 1f : 0f;
        hudCanvasGroup.blocksRaycasts = visible;
    }

    public void ShowHUD() => SetHUDVisible(true);
    public void HideHUD() => SetHUDVisible(false);

    // ---------- Timer / Score / Life text ----------

    void Update()
    {
        if (!timerRunning) return;

        stageTimer += Time.deltaTime;
        UpdateTimeText();
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

    void UpdateLifeText(int current, int max)
    {
        if (lifeText == null) return;
        lifeText.text = $"LEFT {current}";

        persistedHealth = current;
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    public void ResetScore()
    {
        score = 0;
        persistedHealth = -1;
        pendingTimeSeconds = -1f;
        pendingGridData = null;
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        if (scoreText == null) return;
        scoreText.text = $"{score}";
    }
}