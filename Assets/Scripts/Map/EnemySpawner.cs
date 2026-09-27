using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject normalEnemyPrefab;
    [SerializeField] private GameObject fastEnemyPrefab;

    [Header("Spawn Counts")]
    [SerializeField] private int normalEnemyCount = 4;
    [SerializeField] private int fastEnemyCount = 2;

    [Header("Spawn Rules")]
    [SerializeField] private int minDistanceFromPlayerSpawns = 4;
    [SerializeField] private int minDistanceBetweenEnemies = 2;
    [SerializeField] private int minOpenAreaSize = 4; // candidate tile must have at least this many connected walkable tiles nearby

    private List<Vector2Int> usedPositions = new List<Vector2Int>();

    void Start()
    {
        SpawnEnemies(normalEnemyPrefab, normalEnemyCount);
        SpawnEnemies(fastEnemyPrefab, fastEnemyCount);
    }

    void SpawnEnemies(GameObject prefab, int count)
    {
        if (prefab == null)
        {
            Debug.LogWarning("EnemySpawner: prefab not assigned, skipping.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2Int? spawnPos = FindValidSpawnPosition();

            if (!spawnPos.HasValue)
            {
                Debug.LogWarning($"EnemySpawner: couldn't find valid spawn position for {prefab.name} (arena too crowded?).");
                continue;
            }

            Vector3 worldPos = GridManager.Instance.GridToWorld(spawnPos.Value);
            Instantiate(prefab, worldPos, Quaternion.identity);

            usedPositions.Add(spawnPos.Value);
        }
    }

    Vector2Int? FindValidSpawnPosition()
    {
        List<Vector2Int> candidates = GridManager.Instance.GetAllWalkableTiles();

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        foreach (var candidate in candidates)
        {
            if (GridManager.Instance.IsNearSpawnPoint(candidate, minDistanceFromPlayerSpawns))
                continue;

            if (IsTooCloseToUsedPosition(candidate))
                continue;

            if (!HasMinimumOpenArea(candidate, minOpenAreaSize))
                continue;

            return candidate;
        }

        return null;
    }

    bool IsTooCloseToUsedPosition(Vector2Int candidate)
    {
        foreach (var used in usedPositions)
        {
            int dist = Mathf.Abs(candidate.x - used.x) + Mathf.Abs(candidate.y - used.y);
            if (dist < minDistanceBetweenEnemies)
                return true;
        }
        return false;
    }

    // BFS flood fill — counts connected walkable tiles reachable from 'start',
    // stopping early once it confirms at least 'minSize' tiles (cheap, no need to fully explore large open areas)
    bool HasMinimumOpenArea(Vector2Int start, int minSize)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { start };
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(start);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (frontier.Count > 0)
        {
            if (visited.Count >= minSize)
                return true;

            Vector2Int current = frontier.Dequeue();

            foreach (var dir in dirs)
            {
                Vector2Int neighbor = current + dir;

                if (visited.Contains(neighbor))
                    continue;

                if (!GridManager.Instance.IsWalkable(neighbor))
                    continue;

                visited.Add(neighbor);
                frontier.Enqueue(neighbor);
            }
        }

        return visited.Count >= minSize;
    }
}