using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject stageSelectPanel; // optional sub-panel, can leave unassigned

    public void PlayGame()
    {
        if (StageFlowManager.Instance == null)
        {
            Debug.LogError("MainMenuManager: StageFlowManager.Instance is null — is StageFlowManager present in this scene (or an earlier persistent one)?");
            return;
        }

        HUDManager.Instance?.ResetScore();
        StageFlowManager.Instance.LoadSceneWithFade(1, "Stage 1");
    }

    public void LoadStage(int stageIndex)
    {
        StageFlowManager.Instance.LoadSceneWithFade(stageIndex, $"Stage {stageIndex}");
    }

    public void OpenStageSelect()
    {
        if (stageSelectPanel != null)
            stageSelectPanel.SetActive(true);
    }

    public void CloseStageSelect()
    {
        if (stageSelectPanel != null)
            stageSelectPanel.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void Update()
    {
        var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        Debug.Log($"Currently selected: {(selected != null ? selected.name : "NOTHING")}");
    }
    public void OnReturnToMenuPressed()
    {
        StageFlowManager.Instance?.ReturnToMainMenu();
    }
}