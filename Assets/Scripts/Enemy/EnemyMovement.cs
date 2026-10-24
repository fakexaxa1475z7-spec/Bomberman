using System.Collections.Generic;
using UnityEngine;

public enum EnemyType { Normal, Fast, Pacer, Smart, Ghost, ErraticGhost, Hunter, TimeOut }

public enum ChaseMode { None, LineOfSight, Pathfind }

[System.Serializable]
public struct EnemyStats
{
    [Header("Speed (tiles per second)")]
    public float speed;
    public bool pacing;           // speed pulses between min and max
    public float paceMinSpeed;
    public float paceMaxSpeed;
    public float pacePeriod;      // seconds for one slow -> fast -> slow cycle

    [Header("Movement")]
    public bool passBlocks;       // walks through bricks
    public bool passWalls;        // walks through inner walls (the outer border always stops everyone)
    public bool directionalBias;  // keeps patrolling one direction, reverses instead of turning
    [Range(0f, 1f)] public float erraticChance; // chance per tile to pick a new random direction

    [Header("Chasing")]
    public ChaseMode chase;
    public int chaseRange;        // starts chasing within this many tiles
    public int loseRange;         // gives up beyond this many tiles

    [Header("Survival")]
    public bool dodgeBombs;
}

public static class EnemyPresets
{
    public static EnemyStats Get(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Fast:
                return new EnemyStats { speed = 2.6f, chase = ChaseMode.LineOfSight, chaseRange = 6, loseRange = 6 };

            case EnemyType.Pacer:
                return new EnemyStats { pacing = true, paceMinSpeed = 1.4f, paceMaxSpeed = 3.6f, pacePeriod = 3f, directionalBias = true };

            case EnemyType.Smart:
                return new EnemyStats { speed = 3.6f, chase = ChaseMode.Pathfind, chaseRange = 12, loseRange = 16, dodgeBombs = true };

            case EnemyType.Ghost:
                return new EnemyStats { speed = 1.5f, passBlocks = true };

            case EnemyType.ErraticGhost:
                return new EnemyStats { speed = 1.7f, passBlocks = true, erraticChance = 0.4f };

            case EnemyType.Hunter:
                return new EnemyStats { speed = 3.8f, chase = ChaseMode.Pathfind, chaseRange = 6, loseRange = 99 };

            case EnemyType.TimeOut:
                return new EnemyStats { speed = 4.6f, passBlocks = true, passWalls = true, chase = ChaseMode.Pathfind, chaseRange = 99, loseRange = 99 };

            default: // Normal
                return new EnemyStats { speed = 1.6f };
        }
    }
}

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyType enemyType = EnemyType.Normal;

    [Tooltip("Tick to use the numbers below instead of the built-in preset for this type.")]
    [SerializeField] private bool useCustomStats = false;
    [SerializeField] private EnemyStats customStats;

    private EnemyStats stats;
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector2Int currentDirection;
    private bool isMoving = false;
    private bool isChasing = false;
    private float paceOffset;
    private float nextDecisionTime = 0f;

    private static readonly Vector2Int[] Directions =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public Vector2Int CurrentGridPos => currentGridPos;
    public EnemyType Type => enemyType;

    [ContextMenu("Copy preset into custom stats")]
    void CopyPresetToCustom()
    {
        customStats = EnemyPresets.Get(enemyType);
        useCustomStats = true;
    }

    void Start()
    {
        stats = useCustomStats ? customStats : EnemyPresets.Get(enemyType);
        paceOffset = Random.Range(0f, 10f);

        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        targetGridPos = currentGridPos;
        transform.position = GridManager.Instance.GridToWorld(currentGridPos);
        currentDirection = Vector2Int.zero;
    }

    void Update()
    {
        if (!isMoving)
            TryStartMove();
        else
            MoveTowardsTarget();
    }

    // ---------------- Movement ----------------

    void TryStartMove()
    {
        if (Time.time < nextDecisionTime) return;

        Vector2Int dir = ChooseDirection();

        if (dir == Vector2Int.zero)
        {
            nextDecisionTime = Time.time + 0.2f; // boxed in, look again shortly
            return;
        }

        currentDirection = dir;
        targetGridPos = currentGridPos + dir;
        isMoving = true;
        FaceDirection(dir);
    }

    void MoveTowardsTarget()
    {
        Vector3 targetWorldPos = GridManager.Instance.GridToWorld(targetGridPos);
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, CurrentSpeed() * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
        {
            transform.position = targetWorldPos;
            currentGridPos = targetGridPos;
            isMoving = false;
        }
    }

    float CurrentSpeed()
    {
        if (!stats.pacing) return stats.speed;

        float cycle = Mathf.PI * 2f / Mathf.Max(0.1f, stats.pacePeriod);
        float t = (Mathf.Sin((Time.time + paceOffset) * cycle) + 1f) * 0.5f;
        return Mathf.Lerp(stats.paceMinSpeed, stats.paceMaxSpeed, t);
    }

    void FaceDirection(Vector2Int dir)
    {
        Vector3 worldDir = GridManager.Instance.GridDirectionToWorld(dir);
        if (worldDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(worldDir);
    }

    // ---------------- Decision making ----------------

    Vector2Int ChooseDirection()
    {
        HashSet<Vector2Int> danger = stats.dodgeBombs ? CollectDanger() : null;

        // 1. Standing in a blast zone: run for the nearest safe tile
        if (danger != null && IsDangerous(currentGridPos, danger))
        {
            Vector2Int escape = BfsFirstStep(
                p => !IsDangerous(p, danger),
                p => CanEnter(p),
                8);

            if (escape != Vector2Int.zero) return escape;
        }

        // 2. Chase the player
        if (stats.chase != ChaseMode.None)
        {
            Vector2Int chaseDir = ChooseChaseDirection(danger);
            if (chaseDir != Vector2Int.zero)
            {
                isChasing = true;
                return chaseDir;
            }
            isChasing = false;
        }

        // 3. Wander
        return Wander(danger);
    }

    Vector2Int ChooseChaseDirection(HashSet<Vector2Int> danger)
    {
        Vector2Int? playerTile = FindNearestPlayerTile();
        if (!playerTile.HasValue) return Vector2Int.zero;

        Vector2Int target = playerTile.Value;

        if (stats.chase == ChaseMode.LineOfSight)
        {
            // Must share a row or column
            if (currentGridPos.x != target.x && currentGridPos.y != target.y) return Vector2Int.zero;

            int dist = Mathf.Abs(target.x - currentGridPos.x) + Mathf.Abs(target.y - currentGridPos.y);
            if (dist == 0 || dist > stats.chaseRange) return Vector2Int.zero;

            Vector2Int dir = new Vector2Int(
                Mathf.Clamp(target.x - currentGridPos.x, -1, 1),
                Mathf.Clamp(target.y - currentGridPos.y, -1, 1));

            // Everything between us and the player must be open
            for (Vector2Int p = currentGridPos + dir; p != target; p += dir)
                if (!CanEnter(p) || IsDangerous(p, danger)) return Vector2Int.zero;

            if (!CanEnter(currentGridPos + dir) || IsDangerous(currentGridPos + dir, danger)) return Vector2Int.zero;

            return dir;
        }

        // Pathfind: shortest route, with a bigger range once already chasing
        int range = isChasing ? stats.loseRange : stats.chaseRange;
        return BfsFirstStep(
            p => p == target,
            p => CanEnter(p) && !IsDangerous(p, danger),
            range);
    }

    Vector2Int Wander(HashSet<Vector2Int> danger)
    {
        var options = new List<Vector2Int>(4);

        foreach (var dir in Directions)
        {
            Vector2Int next = currentGridPos + dir;
            if (CanEnter(next) && !IsDangerous(next, danger))
                options.Add(dir);
        }

        if (options.Count == 0) return Vector2Int.zero;

        bool forwardOpen = currentDirection != Vector2Int.zero && options.Contains(currentDirection);
        Vector2Int reverse = -currentDirection;

        if (stats.directionalBias)
        {
            // Patrol: keep going, turn around at the end, only turn sideways if both ends are blocked
            if (forwardOpen) return currentDirection;
            if (options.Contains(reverse)) return reverse;
            return options[Random.Range(0, options.Count)];
        }

        // Keep going straight unless the erratic roll says otherwise
        if (forwardOpen && Random.value >= stats.erraticChance)
            return currentDirection;

        if (options.Count > 1)
            options.Remove(reverse); // avoid immediately backtracking unless it's the only way

        return options[Random.Range(0, options.Count)];
    }

    // ---------------- Helpers ----------------

    bool CanEnter(Vector2Int pos)
    {
        return GridManager.Instance.CanEnemyEnter(pos, stats.passBlocks, stats.passWalls);
    }

    bool IsDangerous(Vector2Int pos, HashSet<Vector2Int> danger)
    {
        if (danger == null) return false; // only bomb-dodgers care
        return danger.Contains(pos) || ExplosionHazard.IsHazard(pos);
    }

    HashSet<Vector2Int> CollectDanger()
    {
        var set = new HashSet<Vector2Int>();

        foreach (var bomb in Bomb.Active)
        {
            if (bomb == null || bomb.HasExploded) continue;
            foreach (var tile in bomb.PredictBlastTiles())
                set.Add(tile);
        }

        return set;
    }

    Vector2Int? FindNearestPlayerTile()
    {
        var players = FindObjectsByType<PlayerGridMovement>(FindObjectsSortMode.None);

        Vector2Int? best = null;
        int bestDist = int.MaxValue;

        foreach (var player in players)
        {
            if (player.TryGetComponent(out PlayerHealth health) && health.IsDead) continue;

            Vector2Int tile = player.CurrentGridPos;
            int dist = Mathf.Abs(tile.x - currentGridPos.x) + Mathf.Abs(tile.y - currentGridPos.y);

            if (dist < bestDist)
            {
                bestDist = dist;
                best = tile;
            }
        }

        return best;
    }

    // Breadth-first search from our tile. Returns the FIRST step of the shortest path to a goal tile.
    Vector2Int BfsFirstStep(System.Func<Vector2Int, bool> isGoal, System.Func<Vector2Int, bool> canStep, int maxDepth)
    {
        int w = GridManager.Instance.Width;
        int h = GridManager.Instance.Height;

        var visited = new bool[w * h];
        var depth = new int[w * h];
        var firstStep = new Vector2Int[w * h];
        var queue = new Queue<Vector2Int>();

        int Index(Vector2Int p) => p.x + p.y * w;

        visited[Index(currentGridPos)] = true;
        queue.Enqueue(currentGridPos);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            int curIndex = Index(cur);

            if (depth[curIndex] >= maxDepth) continue;

            foreach (var dir in Directions)
            {
                Vector2Int next = cur + dir;

                if (next.x < 0 || next.y < 0 || next.x >= w || next.y >= h) continue;

                int nextIndex = Index(next);
                if (visited[nextIndex]) continue;
                if (!canStep(next)) continue;

                visited[nextIndex] = true;
                depth[nextIndex] = depth[curIndex] + 1;
                firstStep[nextIndex] = (cur == currentGridPos) ? dir : firstStep[curIndex];

                if (isGoal(next))
                    return firstStep[nextIndex];

                queue.Enqueue(next);
            }
        }

        return Vector2Int.zero;
    }
}