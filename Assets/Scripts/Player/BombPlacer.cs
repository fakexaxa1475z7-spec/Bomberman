using System.Collections.Generic;
using UnityEngine;

public class BombPlacer : MonoBehaviour
{
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private KeyCode placeBombKey = KeyCode.Space;
    [SerializeField] private KeyCode detonateKey = KeyCode.F;

    [Header("Starting stats (upgrades add on top)")]
    [SerializeField] private int startBombs = 1;
    [SerializeField] private int startRadius = 1;

    [Header("Remote control")]
    [Tooltip("Off = F detonates every bomb you placed. On = F detonates the oldest one only.")]
    [SerializeField] private bool detonateOneAtATime = false;

    [SerializeField] private float clearBuffer = 0.05f;

    private readonly List<Bomb> activeBombs = new List<Bomb>();
    private int activeBombCount = 0;
    private PlayerGridMovement movement;

    private Vector2Int? standingOnBombTile = null;
    private Vector3 standingOnBombWorldPos;

    public int MaxBombs => PlayerUpgrades.Bombs;
    public int BlastRadius => PlayerUpgrades.Radius;

    void Awake()
    {
        movement = GetComponent<PlayerGridMovement>();

        // The inspector values act as a floor, so you can still start with more for testing
        PlayerUpgrades.Bombs = Mathf.Clamp(Mathf.Max(PlayerUpgrades.Bombs, startBombs), 1, PlayerUpgrades.MaxBombs);
        PlayerUpgrades.Radius = Mathf.Clamp(Mathf.Max(PlayerUpgrades.Radius, startRadius), 1, PlayerUpgrades.MaxRadius);
    }

    void Update()
    {
        if (Input.GetKeyDown(placeBombKey))
            TryPlaceBomb();

        if (PlayerUpgrades.HasRemote && Input.GetKeyDown(detonateKey))
            DetonateBombs();

        // Once the player has walked off their own bomb, it becomes solid like any other
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
        if (activeBombCount >= MaxBombs) return;

        Vector2Int gridPos = movement.CurrentGridPos;

        if (!GridManager.Instance.CanPlaceBomb(gridPos)) return;

        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
        GameObject bombObj = Instantiate(bombPrefab, worldPos, Quaternion.identity);

        Bomb bomb = bombObj.GetComponent<Bomb>();
        bomb.Initialize(gridPos, BlastRadius, this, PlayerUpgrades.HasRemote);

        activeBombs.Add(bomb);
        activeBombCount++;

        standingOnBombTile = gridPos;
        standingOnBombWorldPos = worldPos;

        GetComponent<PlayerAnimationController>()?.PlayPlaceBomb();
    }

    void DetonateBombs()
    {
        if (activeBombs.Count == 0) return;

        // Copy first: bombs remove themselves from the list as they explode
        var snapshot = new List<Bomb>(activeBombs);

        if (detonateOneAtATime)
        {
            if (snapshot[0] != null) snapshot[0].DetonateNow(); // oldest first
            return;
        }

        foreach (var bomb in snapshot)
            if (bomb != null) bomb.DetonateNow();
    }

    public void OnBombExploded(Bomb bomb)
    {
        activeBombCount = Mathf.Max(0, activeBombCount - 1);
        activeBombs.Remove(bomb);
    }

    public Vector2Int? GetExemptTile() => standingOnBombTile;
    public bool IsExemptTile(Vector2Int pos) => standingOnBombTile.HasValue && standingOnBombTile.Value == pos;
}