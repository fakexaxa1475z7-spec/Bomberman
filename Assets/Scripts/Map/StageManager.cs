using UnityEngine;
using System;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [SerializeField] private GameObject exitItemPrefab;

    private bool exitActive = false;
    private Vector2Int exitGridPos;
    private bool stageComplete = false;
    private GameObject spawnedExitItem;

    public event Action OnStageComplete;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GridManager.Instance.OnExitRevealed += HandleExitRevealed;
    }

    void HandleExitRevealed(Vector2Int pos)
    {
        exitGridPos = pos;
        exitActive = true;

        Vector3 worldPos = GridManager.Instance.GridToWorld(pos);

        if (exitItemPrefab != null)
        {
            spawnedExitItem = Instantiate(exitItemPrefab, worldPos + Vector3.up * 0.5f, Quaternion.identity);
        }

        GridManager.Instance.SetTile(pos, TileType.Exit);
    }

    void Update()
    {
        if (!exitActive || stageComplete) return;

        var players = FindObjectsByType<PlayerGridMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.CurrentGridPos == exitGridPos)
            {
                CompleteStage();
                break;
            }
        }
    }

    void CompleteStage()
    {
        stageComplete = true;
        Debug.Log("StageManager: CompleteStage() called"); // add this

        if (spawnedExitItem != null)
            Destroy(spawnedExitItem);

        Debug.Log("Stage Complete!");
        OnStageComplete?.Invoke();
    }

    void OnDestroy()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.OnExitRevealed -= HandleExitRevealed;
    }
}