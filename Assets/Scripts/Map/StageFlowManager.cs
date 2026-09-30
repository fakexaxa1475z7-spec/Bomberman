using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StageFlowManager : MonoBehaviour
{
    public static StageFlowManager Instance { get; private set; }

    [SerializeField] private float delayBeforeLoad = 1.5f;
    [SerializeField] private float delayAfterLoad = 0.5f;
    [SerializeField] private FadeController fadeController;
    [SerializeField] private int mainMenuBuildIndex = 0;

    private bool pendingFadeIn = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageComplete += HandleStageComplete;
    }

    void HandleStageComplete()
    {
        Invoke(nameof(BeginTransition), delayBeforeLoad);
    }

    void BeginTransition()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        LoadSceneWithFade(nextIndex, $"STAGE {nextIndex}");
    }

    public void LoadSceneWithFade(int sceneIndex, string label)
    {
        StartCoroutine(TransitionRoutine(sceneIndex, label));
    }

    IEnumerator TransitionRoutine(int sceneIndex, string label)
    {
        if (sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"StageFlowManager: scene index {sceneIndex} out of range.");
            yield break;
        }

        HUDManager.Instance?.HideHUD();

        if (fadeController != null)
        {
            if (!string.IsNullOrEmpty(label))
                fadeController.SetStageText(label);
            else
                fadeController.HideStageText();

            yield return fadeController.FadeOut();
        }

        pendingFadeIn = true;
        SceneManager.LoadScene(sceneIndex);
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
        StartCoroutine(SubscribeWhenReady());

        if (pendingFadeIn)
        {
            pendingFadeIn = false;
            StartCoroutine(FadeInAfterLoad(scene.buildIndex));
        }
    }

    IEnumerator SubscribeWhenReady()
    {
        yield return new WaitUntil(() => StageManager.Instance != null);
        StageManager.Instance.OnStageComplete += HandleStageComplete;
    }

    IEnumerator FadeInAfterLoad(int loadedSceneIndex)
    {
        if (fadeController == null) yield break;

        yield return null;
        yield return new WaitForSeconds(delayAfterLoad);
        yield return fadeController.FadeIn();

        // Only show the HUD if we actually entered a real stage, not the main menu
        bool isMainMenu = loadedSceneIndex == mainMenuBuildIndex;
        if (!isMainMenu)
            HUDManager.Instance?.ShowHUD();
        else
            HUDManager.Instance?.HideHUD();
    }
    public void ReturnToMainMenu()
    {
        HUDManager.Instance?.ResetScore();
        LoadSceneWithFade(mainMenuBuildIndex, null);
    }
}