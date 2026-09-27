using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public enum TileType { Empty, Wall, Block, Bomb, Powerup, Exit }

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugShowExitBlock = true; // turn off before a real playtest/build
    [SerializeField] private Color debugExitColor = Color.yellow;

    [Header("Grid Size")]
    [SerializeField] private int width = 31;
    [SerializeField] private int height = 13;
    [SerializeField] private float tileSize = 1f;

    [Header("Prefab Variants")]
    [SerializeField] private GameObject[] floorPrefabs;
    [SerializeField] private GameObject[] wallPrefabs;
    [SerializeField] private GameObject[] blockPrefabs;

    [Header("Generation")]
    [Range(0f, 1f)]
    [SerializeField] private float blockDensity = 0.4f;
    [SerializeField] private int spawnClearRadius = 2;

    [Header("Exit")]
    [SerializeField] private GameObject exitItemPrefab;
    [Range(0f, 1f)]
    [SerializeField] private float exitZoneWidthFraction = 0.25f;

    private TileType[,] grid;
    private GameObject[,] spawnedTiles;
    private List<Vector2Int> spawnPoints;

    public int Width => width;
    public int Height => height;
    public float TileSize => tileSize;
    public List<Vector2Int> SpawnPoints => spawnPoints;

    void Awake()
    {
        Instance = this;
        grid = new TileType[width, height];
        spawnedTiles = new GameObject[width, height];

        DefineSpawnPoints();
        GenerateGrid();
        PlaceHiddenExit();
    }

    void DefineSpawnPoints()
    {
        spawnPoints = new List<Vector2Int>
        {
            new Vector2Int(1, 1),
            new Vector2Int(width - 2, 1),
            new Vector2Int(1, height - 2),
            new Vector2Int(width - 2, height - 2)
        };
    }

    void GenerateGrid()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileType type = DetermineTileType(x, y);
                grid[x, y] = type;
                SpawnTileVisual(x, y, type);
            }
        }
    }

    TileType DetermineTileType(int x, int y)
    {
        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            return TileType.Wall;

        if (x % 2 == 0 && y % 2 == 0)
            return TileType.Wall;

        if (IsNearAnySpawn(x, y))
            return TileType.Empty;

        return Random.value < blockDensity ? TileType.Block : TileType.Empty;
    }

    bool IsNearAnySpawn(int x, int y)
    {
        foreach (var spawn in spawnPoints)
        {
            int dist = Mathf.Abs(x - spawn.x) + Mathf.Abs(y - spawn.y);
            if (dist <= spawnClearRadius)
                return true;
        }
        return false;
    }

    void SpawnTileVisual(int x, int y, TileType type)
    {
        Vector3 worldPos = GridToWorld(new Vector2Int(x, y));

        // Floor always spawns underneath, regardless of tile type above it
        GameObject floorPrefab = GetRandomPrefab(floorPrefabs);
        if (floorPrefab != null)
            Instantiate(floorPrefab, worldPos, Quaternion.identity, transform);

        GameObject[] sourceArray = type switch
        {
            TileType.Wall => wallPrefabs,
            TileType.Block => blockPrefabs,
            _ => null
        };

        if (sourceArray != null)
        {
            GameObject prefab = GetRandomPrefab(sourceArray);
            if (prefab != null)
            {
                GameObject obj = Instantiate(prefab, worldPos + Vector3.up * 0.5f, Quaternion.identity, transform);
                spawnedTiles[x, y] = obj;
            }
        }
    }

    GameObject GetRandomPrefab(GameObject[] prefabArray)
    {
        if (prefabArray == null || prefabArray.Length == 0)
        {
            Debug.LogWarning("Prefab array is empty — check GridManager Inspector assignments.");
            return null;
        }
        return prefabArray[Random.Range(0, prefabArray.Length)];
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float worldX = gridPos.x * tileSize;
        float worldZ = (height - 1 - gridPos.y) * tileSize;
        return new Vector3(worldX, 0, worldZ);
    }

    public Vector3 GridDirectionToWorld(Vector2Int gridDir)
    {
        // Must mirror the Y-flip used in GridToWorld: increasing grid Y = decreasing world Z
        return new Vector3(gridDir.x, 0, -gridDir.y);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / tileSize);
        int y = height - 1 - Mathf.RoundToInt(worldPos.z / tileSize);
        return new Vector2Int(x, y);
    }

    public bool IsWalkable(Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= width || gridPos.y < 0 || gridPos.y >= height)
            return false;

        TileType type = grid[gridPos.x, gridPos.y];
        return type == TileType.Empty || type == TileType.Powerup || type == TileType.Exit;
    }

    public TileType GetTile(Vector2Int gridPos) => grid[gridPos.x, gridPos.y];
    public void SetTile(Vector2Int gridPos, TileType type) => grid[gridPos.x, gridPos.y] = type;

    public bool CanPlaceBomb(Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= width || gridPos.y < 0 || gridPos.y >= height)
            return false;

        return grid[gridPos.x, gridPos.y] == TileType.Empty;
    }

    public void PlaceBomb(Vector2Int gridPos, GameObject bombObject)
    {
        grid[gridPos.x, gridPos.y] = TileType.Bomb;
        spawnedTiles[gridPos.x, gridPos.y] = bombObject;
    }

    public void ClearTile(Vector2Int gridPos)
    {
        bool wasExitTile = exitTilePos.HasValue && exitTilePos.Value == gridPos;
        TileType previousType = grid[gridPos.x, gridPos.y];

        grid[gridPos.x, gridPos.y] = TileType.Empty;

        GameObject obj = spawnedTiles[gridPos.x, gridPos.y];
        if (obj != null)
        {
            // Bombs manage their own destruction timing (to let explosion animation play) — don't double-destroy
            if (!obj.TryGetComponent<Bomb>(out _))
                Destroy(obj);

            spawnedTiles[gridPos.x, gridPos.y] = null;
        }

        if (wasExitTile && previousType == TileType.Block)
        {
            OnExitRevealed?.Invoke(gridPos);
        }
    }

    public GameObject GetTileObject(Vector2Int gridPos) => spawnedTiles[gridPos.x, gridPos.y];

    public List<Vector2Int> GetAllWalkableTiles()
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsWalkable(pos))
                    result.Add(pos);
            }
        }
        return result;
    }

    public bool IsNearSpawnPoint(Vector2Int pos, int radius)
    {
        foreach (var spawn in spawnPoints)
        {
            int dist = Mathf.Abs(pos.x - spawn.x) + Mathf.Abs(pos.y - spawn.y);
            if (dist <= radius)
                return true;
        }
        return false;
    }

    public bool IsWorldPositionFree(Vector3 worldPos, float radius, Vector2Int? exemptTile = null)
    {
        Vector3[] corners =
        {
        worldPos + new Vector3(-radius, 0, -radius),
        worldPos + new Vector3( radius, 0, -radius),
        worldPos + new Vector3(-radius, 0,  radius),
        worldPos + new Vector3( radius, 0,  radius)
    };

        foreach (var corner in corners)
        {
            Vector2Int cornerGrid = WorldToGrid(corner);

            if (exemptTile.HasValue && cornerGrid == exemptTile.Value)
                continue;

            if (!IsWalkable(cornerGrid))
                return false;
        }
        return true;
    }

    private Vector2Int? exitTilePos;
    public event Action<Vector2Int> OnExitRevealed;

    void PlaceHiddenExit()
    {
        List<Vector2Int> blockTiles = new List<Vector2Int>();
        int rightZoneStart = Mathf.FloorToInt(width * (1f - exitZoneWidthFraction));

        for (int x = rightZoneStart; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == TileType.Block)
                    blockTiles.Add(new Vector2Int(x, y));
            }
        }

        if (blockTiles.Count > 0)
        {
            exitTilePos = blockTiles[UnityEngine.Random.Range(0, blockTiles.Count)];

            if (debugShowExitBlock)
                HighlightExitBlock(exitTilePos.Value);
        }
        else
        {
            Debug.LogWarning("GridManager: no block tiles in the right-side exit zone — check exitZoneWidthFraction or block density.");
        }
    }

    void HighlightExitBlock(Vector2Int pos)
    {
        GameObject blockObj = spawnedTiles[pos.x, pos.y];
        if (blockObj == null) return;

        Renderer rend = blockObj.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        rend.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", debugExitColor); // URP/HDRP
        propBlock.SetColor("_Color", debugExitColor);     // Built-in RP fallback
        rend.SetPropertyBlock(propBlock);
    }
}