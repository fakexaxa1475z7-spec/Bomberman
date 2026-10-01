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

    [Header("Scene")]
    [SerializeField] private int mainMenuBuildIndex = 0;

    private int score = 0;
    private float stageTimer = 0f;
    private bool timerRunning = false;
    private PlayerHealth trackedPlayerHealth;

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

        bool isMainMenu = SceneManager.GetActiveScene().buildIndex == mainMenuBuildIndex;
        SetHUDVisible(!isMainMenu);
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
        bool isMainMenu = scene.buildIndex == mainMenuBuildIndex;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        isPaused = false;
        gameOverActive = false;
        Time.timeScale = 1f; // safety net — never carry a frozen timescale into a new scene

        if (!isMainMenu)
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

        if (trackedPlayerHealth != null)
        {
            trackedPlayerHealth.OnHealthChanged += UpdateLifeText;
            trackedPlayerHealth.OnDeath += HandlePlayerDeath;
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

        StageFlowManager.Instance?.ReturnToMainMenu();
    }

    void LateUpdate()
    {
        HandlePauseInput();
    }

    void HandlePauseInput()
    {
        bool isMainMenu = SceneManager.GetActiveScene().buildIndex == mainMenuBuildIndex;
        if (isMainMenu || gameOverActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;

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

    void UpdateLifeText(int current, int max)
    {
        if (lifeText == null) return;
        lifeText.text = $"LEFT {current}";
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    public void ResetScore()
    {
        score = 0;
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        if (scoreText == null) return;
        scoreText.text = $"{score}";
    }
}