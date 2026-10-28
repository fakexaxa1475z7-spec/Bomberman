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

    [Header("Time Limit")]
    [Tooltip("Seconds per stage. 0 = no limit (timer counts up and no Time Out enemy).")]
    [SerializeField] private float stageTimeLimit = 200f;

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

    // Raised once per stage when the countdown reaches 0
    public event System.Action OnTimeOut;

    public int CurrentScore => score;
    public bool IsTimeOut => timeOutFired;

    private int score = 0;
    private float stageTimer = 0f;   // seconds elapsed in this stage (this is what gets saved)
    private bool timerRunning = false;
    private bool timeOutFired = false;

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
        UpdateTimeText();

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
            timerRunning = true; // starts with the stage; Update() waits for any fade to finish before ticking
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
        }
        else
        {
            Debug.LogWarning("HUDManager: no PlayerHealth found in this stage within 3 seconds.");
        }
    }

    void HandlePlayerDeath()
    {
        timerRunning = false;
        gameOverActive = true;

        AudioManager.Instance?.PlayPlayerDied();

        // Wait at least as long as the died sound, so the game over music doesn't cut it off
        float wait = delayBeforeGameOver;
        if (AudioManager.Instance != null)
            wait = Mathf.Max(wait, AudioManager.Instance.PlayerDiedLength);

        Invoke(nameof(ShowGameOverPanel), wait);
    }

    void ShowGameOverPanel()
    {
        SetHUDVisible(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        AudioManager.Instance?.PlayGameOver();
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

        AudioManager.Instance?.SetDucked(true);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (saveCodePanel != null) saveCodePanel.SetActive(false);

        AudioManager.Instance?.SetDucked(false);

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
            timeSeconds = stageTimer, // elapsed seconds; the countdown is derived from this
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

        // Don't tick behind the black fade screen (stage intro / outro)
        if (StageFlowManager.Instance != null && StageFlowManager.Instance.IsTransitioning) return;

        stageTimer += Time.deltaTime;
        UpdateTimeText();

        if (!timeOutFired && stageTimeLimit > 0f && stageTimer >= stageTimeLimit)
        {
            timeOutFired = true;
            OnTimeOut?.Invoke(); // EnemySpawner spawns the Time Out enemy
        }
    }

    void ResetTimer()
    {
        stageTimer = 0f;
        timerRunning = false;
        timeOutFired = false;
        UpdateTimeText();
    }

    void UpdateTimeText()
    {
        if (timeText == null) return;

        // Countdown when there's a time limit, otherwise count up
        int shown = stageTimeLimit > 0f
            ? Mathf.CeilToInt(Mathf.Max(0f, stageTimeLimit - stageTimer))
            : Mathf.FloorToInt(stageTimer);

        timeText.text = $"TIME {shown}";
    }

    [ContextMenu("Test: force time out")]
    void ForceTimeOut()
    {
        if (timeOutFired) return;

        timeOutFired = true;
        stageTimer = Mathf.Max(stageTimer, stageTimeLimit);
        UpdateTimeText();
        OnTimeOut?.Invoke();
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

        TopScore.Submit(score); // saves automatically when this run passes the record
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