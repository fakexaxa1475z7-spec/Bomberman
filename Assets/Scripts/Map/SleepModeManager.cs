using UnityEngine;
using UnityEngine.SceneManagement;

public class SleepModeManager : MonoBehaviour
{
    [SerializeField] private GameObject sleepPanel;
    [SerializeField] private GameObject menuRootPanel; // the normal main menu UI, hidden while asleep
    [SerializeField] private float idleTimeout = 30f;
    [SerializeField] private int mainMenuBuildIndex = 0;

    private float lastInputTime;
    private bool isAsleep = false;
    private Vector3 lastMousePosition;

    void Awake()
    {
        if (sleepPanel != null)
            sleepPanel.SetActive(false);

        lastInputTime = Time.unscaledTime;
        lastMousePosition = Input.mousePosition;
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
        // Only attract-mode on the Main Menu — reset state cleanly whenever we arrive/leave it
        WakeUp();
        lastInputTime = Time.unscaledTime;
    }

    public void Sleep()
    {
        GoToSleep();
    }

    public void Wake()
    {
        WakeUp();
    }

    void GoToSleep()
    {
        isAsleep = true;

        if (menuRootPanel != null) menuRootPanel.SetActive(false);
        if (sleepPanel != null) sleepPanel.SetActive(true);
    }

    void WakeUp()
    {
        isAsleep = false;

        if (sleepPanel != null) sleepPanel.SetActive(false);
        if (menuRootPanel != null) menuRootPanel.SetActive(true);
    }
}