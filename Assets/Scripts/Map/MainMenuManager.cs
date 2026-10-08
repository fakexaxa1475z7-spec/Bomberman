using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private GameObject continuePanel;
    [SerializeField] private TMP_InputField continueCodeInput;
    [SerializeField] private TextMeshProUGUI continueErrorText;

    [SerializeField] private Button mainMenuFirstButton;
    [SerializeField] private Button continueFirstButton;

    [SerializeField] private GameObject mainMenuRootPanel;


    public void PlayGame()
    {
        if (StageFlowManager.Instance == null)
        {
            Debug.LogError("MainMenuManager: StageFlowManager.Instance is null.");
            return;
        }

        HUDManager.Instance?.ResetScore();   // new game: clears score, health, and any pending saved map
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
    }
    public void OnReturnToMenuPressed()
    {
        StageFlowManager.Instance?.ReturnToMainMenu();
    }

    public void OpenContinuePanel()
    {
        if (mainMenuRootPanel != null) mainMenuRootPanel.SetActive(false);
        if (continuePanel != null) continuePanel.SetActive(true);
        if (continueErrorText != null) continueErrorText.text = "";
        if (continueCodeInput != null) continueCodeInput.text = "";

        StartCoroutine(SelectContinuePanelButton());
    }

    System.Collections.IEnumerator SelectContinuePanelButton()
    {
        yield return null;

        if (UnityEngine.EventSystems.EventSystem.current != null && continueFirstButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(continueFirstButton.gameObject);
        }
    }

    public void CloseContinuePanel()
    {
        if (continuePanel != null) continuePanel.SetActive(false);
        if (mainMenuRootPanel != null) mainMenuRootPanel.SetActive(true);

        StartCoroutine(ReselectMainMenu());
    }

    System.Collections.IEnumerator ReselectMainMenu()
    {
        yield return null;

        if (UnityEngine.EventSystems.EventSystem.current != null && mainMenuFirstButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(mainMenuFirstButton.gameObject);
        }
    }

    public void OnContinueConfirmPressed()
    {
        if (continueCodeInput == null) return;

        if (HUDManager.Instance == null)
        {
            Debug.LogError("MainMenuManager: HUDManager.Instance is null.");
            return;
        }

        bool success = HUDManager.Instance.TryContinueFromCode(continueCodeInput.text, out string error);

        if (!success && continueErrorText != null)
            continueErrorText.text = error;
    }
}