using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float delayBeforeShow = 1f; // brief pause after death animation before panel appears

    private PlayerHealth trackedPlayer;

    void Start()
    {
        gameOverPanel.SetActive(false);
        StartCoroutine(FindAndTrackPlayer());
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

        if (player == null)
        {
            Debug.LogWarning("GameOverManager: no PlayerHealth found in scene within timeout.");
            yield break;
        }

        trackedPlayer = player;
        trackedPlayer.OnDeath += HandlePlayerDeath;
    }

    void HandlePlayerDeath()
    {
        Invoke(nameof(ShowGameOverPanel), delayBeforeShow);
    }

    void ShowGameOverPanel()
    {
        gameOverPanel.SetActive(true);
    }

    public void OnRetryPressed()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        StageFlowManager.Instance?.LoadSceneWithFade(currentIndex, null);
    }

    public void OnReturnToMenuPressed()
    {
        StageFlowManager.Instance?.ReturnToMainMenu();
    }

    void OnDestroy()
    {
        if (trackedPlayer != null)
            trackedPlayer.OnDeath -= HandlePlayerDeath;
    }
}