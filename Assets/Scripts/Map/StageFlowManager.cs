using UnityEngine;
using UnityEngine.SceneManagement;

public class StageFlowManager : MonoBehaviour
{
    public static StageFlowManager Instance { get; private set; }

    [SerializeField] private float delayBeforeLoad = 1.5f; // gives time for a "Stage Clear!" moment

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // survives scene loads, so it can keep managing stage flow
    }

    void Start()
    {
        // Subscribe fresh each time a new stage's StageManager exists
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageComplete += HandleStageComplete;
    }

    void HandleStageComplete()
    {
        Debug.Log("StageFlowManager: HandleStageComplete() received"); // add this
        Invoke(nameof(LoadNextStage), delayBeforeLoad);
    }

    void LoadNextStage()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("All stages complete — no more stages in Build Settings.");
            // Placeholder — load a "You Win" / credits scene here later
        }
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
    }

    System.Collections.IEnumerator SubscribeWhenReady()
    {
        // Wait until the new scene's StageManager has initialized
        yield return new WaitUntil(() => StageManager.Instance != null);
        StageManager.Instance.OnStageComplete += HandleStageComplete;
    }
}