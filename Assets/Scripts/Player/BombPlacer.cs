using UnityEngine;

public class BombPlacer : MonoBehaviour
{
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private KeyCode placeBombKey = KeyCode.Space;
    [SerializeField] private int maxBombs = 1;
    [SerializeField] private int blastRadius = 1;
    [SerializeField] private float clearBuffer = 0.05f; // small tolerance so it doesn't flicker right at the edge

    private int activeBombCount = 0;
    private PlayerGridMovement movement;

    private Vector2Int? standingOnBombTile = null;
    private Vector3 standingOnBombWorldPos;

    void Awake()
    {
        movement = GetComponent<PlayerGridMovement>();
    }

    void Update()
    {
        if (Input.GetKeyDown(placeBombKey))
        {
            TryPlaceBomb();
        }

        if (standingOnBombTile.HasValue)
        {
            float tile = GridManager.Instance.TileSize;
            float clearDistance = (tile * 0.5f) + movement.PlayerRadius + clearBuffer;

            float dist = Vector3.Distance(transform.position, standingOnBombWorldPos);
            if (dist > clearDistance)
                standingOnBombTile = null;
        }
    }

    void TryPlaceBomb()
    {
        if (activeBombCount >= maxBombs) return;

        Vector2Int gridPos = movement.CurrentGridPos;

        if (!GridManager.Instance.CanPlaceBomb(gridPos)) return;

        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
        GameObject bombObj = Instantiate(bombPrefab, worldPos, Quaternion.identity);

        Bomb bomb = bombObj.GetComponent<Bomb>();
        bomb.Initialize(gridPos, blastRadius, this);

        activeBombCount++;
        standingOnBombTile = gridPos;
        standingOnBombWorldPos = worldPos;

        GetComponent<PlayerAnimationController>()?.PlayPlaceBomb();
    }

    public void OnBombExploded()
    {
        activeBombCount--;
    }

    public Vector2Int? GetExemptTile() => standingOnBombTile;
}