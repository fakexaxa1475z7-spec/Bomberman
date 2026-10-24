using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    public string label = "Enemy";
    public GameObject prefab;
    [Min(0)] public int count = 1;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemies at stage start")]
    [SerializeField] private EnemySpawnEntry[] enemies;

    [Header("Time Out enemy (type 8)")]
    [SerializeField] private GameObject timeOutEnemyPrefab;
    [SerializeField] private int timeOutEnemyCount = 1;
    [SerializeField] private int timeOutMinDistanceFromPlayer = 8;

    [Header("Spawn Rules")]
    [SerializeField] private int minDistanceFromPlayerSpawns = 4;
    [SerializeField] private int minDistanceBetweenEnemies = 2;
    [SerializeField] private int minOpenAreaSize = 4;

    private readonly List<Vector2Int> usedPositions = new List<Vector2Int>();
    private bool timeOutSpawned = false;

    void Start()
    {
        if (enemies != null)
            foreach (var entry in enemies)
                SpawnEnemies(entry.prefab, entry.count);

        if (HUDManager.Instance != null)
            HUDManager.Instance.OnTimeOut += HandleTimeOut;
    }

    void OnDestroy()
    {
        if (HUDManager.Instance != null)
            HUDManager.Instance.OnTimeOut -= HandleTimeOut;
    }

    // ---------------- Stage start ----------------

    void SpawnEnemies(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            Vector2Int? spawnPos = FindValidSpawnPosition();

            if (!spawnPos.HasValue)
            {
                Debug.LogWarning($"EnemySpawner: couldn't find a valid spawn position for {prefab.name} (arena too crowded?).");
                continue;
            }

            Instantiate(prefab, GridManager.Instance.GridToWorld(spawnPos.Value), Quaternion.identity);
            usedPositions.Add(spawnPos.Value);
        }
    }

    Vector2Int? FindValidSpawnPosition()
    {
        List<Vector2Int> candidates = GridManager.Instance.GetAllWalkableTiles();
        Shuffle(candidates);

        foreach (var candidate in candidates)
        {
            if (GridManager.Instance.IsNearSpawnPoint(candidate, minDistanceFromPlayerSpawns)) continue;
            if (IsTooCloseToUsedPosition(candidate)) continue;
            if (!HasMinimumOpenArea(candidate, minOpenAreaSize)) continue;

            return candidate;
        }

        return null;
    }

    bool IsTooCloseToUsedPosition(Vector2Int candidate)
    {
        foreach (var used in usedPositions)
        {
            int dist = Mathf.Abs(candidate.x - used.x) + Mathf.Abs(candidate.y - used.y);
            if (dist < minDistanceBetweenEnemies) return true;
        }
        return false;
    }

    bool HasMinimumOpenArea(Vector2Int start, int minSize)
    {
        var visited = new HashSet<Vector2Int> { start };
        var frontier = new Queue<Vector2Int>();
        frontier.Enqueue(start);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (frontier.Count > 0)
        {
            if (visited.Count >= minSize) return true;

            Vector2Int current = frontier.Dequeue();

            foreach (var dir in dirs)
            {
                Vector2Int neighbor = current + dir;

                if (visited.Contains(neighbor)) continue;
                if (!GridManager.Instance.IsWalkable(neighbor)) continue;

                visited.Add(neighbor);
                frontier.Enqueue(neighbor);
            }
        }

        return visited.Count >= minSize;
    }

    // ---------------- Time Out ----------------

    void HandleTimeOut()
    {
        if (timeOutSpawned) return;
        timeOutSpawned = true;

        if (timeOutEnemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: time ran out but no Time Out Enemy Prefab is assigned.");
            return;
        }

        for (int i = 0; i < timeOutEnemyCount; i++)
        {
            Vector2Int? pos = FindTimeOutPosition();
            if (!pos.HasValue) continue;

            Instantiate(timeOutEnemyPrefab, GridManager.Instance.GridToWorld(pos.Value), Quaternion.identity);
        }
    }

    Vector2Int? FindTimeOutPosition()
    {
        List<Vector2Int> candidates = GridManager.Instance.GetAllWalkableTiles();
        if (candidates.Count == 0) return null;

        Shuffle(candidates);

        var players = FindObjectsByType<PlayerGridMovement>(FindObjectsSortMode.None);

        Vector2Int best = candidates[0];
        int bestDistance = -1;

        foreach (var candidate in candidates)
        {
            int nearest = int.MaxValue;
            foreach (var player in players)
            {
                Vector2Int p = player.CurrentGridPos;
                nearest = Mathf.Min(nearest, Mathf.Abs(candidate.x - p.x) + Mathf.Abs(candidate.y - p.y));
            }

            if (players.Length == 0) nearest = int.MaxValue;

            if (nearest >= timeOutMinDistanceFromPlayer)
                return candidate; // random tile that is far enough away

            if (nearest > bestDistance)
            {
                bestDistance = nearest;
                best = candidate;
            }
        }

        return best; // nothing far enough: use the farthest one we found
    }

    void Shuffle(List<Vector2Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}