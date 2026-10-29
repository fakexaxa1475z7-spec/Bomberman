using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bomb : MonoBehaviour
{
    public static readonly List<Bomb> Active = new List<Bomb>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active.Clear(); }

    [Header("Fuse / Explosion")]
    [SerializeField] private float fuseTime = 3f;
    [Tooltip("Remote-control bombs wait for the detonate key. Above 0 = they also explode by themselves after this many seconds.")]
    [SerializeField] private float remoteFailsafeTime = 0f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionVisualDuration = 0.5f;
    [SerializeField] private float explodeAnimDuration = 0.4f;

    [Header("Remote Control Model (optional)")]
    [Tooltip("Model shown on normal timed bombs. Assign BOTH models to enable swapping.")]
    [SerializeField] private GameObject normalModel;
    [Tooltip("Model shown instead once the player has the remote control.")]
    [SerializeField] private GameObject remoteModel;
    [Tooltip("Apply the red/black blink colour to the remote model too. Leave off if it has its own textures.")]
    [SerializeField] private bool flashRemoteModel = false;

    [Header("Wink Effect")]
    [SerializeField] private Renderer bombRenderer;
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color winkColor = Color.red;
    [SerializeField] private float scalePulseAmount = 0.15f;
    [SerializeField] private AnimationCurve winkSpeedCurve = AnimationCurve.Linear(0f, 1.5f, 1f, 6f);

    [Header("Animator")]
    [SerializeField] private Animator animator;

    private static readonly int ExplodeParam = Animator.StringToHash("Explode");

    private Vector2Int gridPos;
    private int blastRadius;
    private BombPlacer owner;
    private bool isRemote = false;
    private bool hasExploded = false;
    private Coroutine winkRoutine;

    private Vector3 baseScale;
    private MaterialPropertyBlock propBlock;

    public bool HasExploded => hasExploded;

    void Awake()
    {
        baseScale = transform.localScale;
        propBlock = new MaterialPropertyBlock();

        if (bombRenderer == null)
            bombRenderer = GetComponent<Renderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void Initialize(Vector2Int position, int radius, BombPlacer bombOwner, bool remote)
    {
        Active.Add(this);

        gridPos = position;
        blastRadius = radius;
        owner = bombOwner;
        isRemote = remote;

        ApplyModel();

        GridManager.Instance.PlaceBomb(gridPos, gameObject);
        StartCoroutine(FuseCountdown());
        winkRoutine = StartCoroutine(WinkEffect());
    }

    // Shows the normal or the remote model, and points the renderer/animator at the visible one
    void ApplyModel()
    {
        if (normalModel == null || remoteModel == null) return; // nothing to swap

        GameObject shown = isRemote ? remoteModel : normalModel;
        GameObject hidden = isRemote ? normalModel : remoteModel;

        shown.SetActive(true);
        hidden.SetActive(false);

        // Don't keep using a renderer / animator that lives on the hidden model
        if (bombRenderer != null && bombRenderer.transform.IsChildOf(hidden.transform))
            bombRenderer = null;

        if (animator != null && animator.transform.IsChildOf(hidden.transform))
            animator = null;

        Renderer shownRenderer = shown.GetComponentInChildren<Renderer>(true);
        if (bombRenderer == null && shownRenderer != null)
            bombRenderer = shownRenderer;

        Animator shownAnimator = shown.GetComponentInChildren<Animator>(true);
        if (shownAnimator != null)
            animator = shownAnimator;
    }

    IEnumerator FuseCountdown()
    {
        if (isRemote)
        {
            if (remoteFailsafeTime <= 0f) yield break; // waits for DetonateNow()
            yield return new WaitForSeconds(remoteFailsafeTime);
        }
        else
        {
            yield return new WaitForSeconds(fuseTime);
        }

        Explode();
    }

    IEnumerator WinkEffect()
    {
        float elapsed = 0f;

        while (!hasExploded && (isRemote || elapsed < fuseTime))
        {
            // Remote bombs keep a steady, calm blink since they have no countdown
            float progress = isRemote ? 0.2f : elapsed / fuseTime;
            float pulseSpeed = winkSpeedCurve.Evaluate(progress);
            float pulse = (Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f) + 1f) / 2f;

            transform.localScale = baseScale * (1f + pulse * scalePulseAmount);

            if (bombRenderer != null && propBlock != null && (!isRemote || flashRemoteModel))
            {
                Color currentColor = Color.Lerp(normalColor, winkColor, pulse);
                bombRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", currentColor);
                propBlock.SetColor("_Color", currentColor);
                bombRenderer.SetPropertyBlock(propBlock);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Called by the remote control (F key)
    public void DetonateNow()
    {
        TriggerEarly();
    }

    // Also used for chain reactions from another bomb's blast
    public void TriggerEarly()
    {
        if (!hasExploded)
        {
            StopAllCoroutines();
            Explode();
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (owner != null)
            owner.OnBombExploded(this);

        List<Vector2Int> affectedTiles = new List<Vector2Int> { gridPos };
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            for (int step = 1; step <= blastRadius; step++)
            {
                Vector2Int checkPos = gridPos + dir * step;

                if (checkPos.x < 0 || checkPos.x >= GridManager.Instance.Width ||
                    checkPos.y < 0 || checkPos.y >= GridManager.Instance.Height)
                    break;

                TileType tile = GridManager.Instance.GetTile(checkPos);

                if (tile == TileType.Wall)
                    break;

                affectedTiles.Add(checkPos);

                if (tile == TileType.Block)
                    break;

                if (tile == TileType.Bomb)
                {
                    GameObject bombObj = GridManager.Instance.GetTileObject(checkPos);
                    if (bombObj != null && bombObj.TryGetComponent<Bomb>(out var chainedBomb))
                        chainedBomb.TriggerEarly();
                    break;
                }
            }
        }

        ResolveExplosion(affectedTiles);
        PlayExplosionAnimationThenDestroy();
    }

    void ResolveExplosion(List<Vector2Int> tiles)
    {
        foreach (var pos in tiles)
        {
            GridManager.Instance.ClearTile(pos);
            SpawnExplosionVisual(pos);
        }
    }

    void SpawnExplosionVisual(Vector2Int pos)
    {
        if (explosionPrefab == null) return;

        Vector3 worldPos = GridManager.Instance.GridToWorld(pos);
        GameObject vfx = Instantiate(explosionPrefab, worldPos + Vector3.up * 0.5f, Quaternion.identity);

        ExplosionHazard hazard = vfx.GetComponent<ExplosionHazard>();
        if (hazard != null)
            hazard.Initialize(pos, damageAmount, explosionVisualDuration);
        else
            Destroy(vfx, explosionVisualDuration);
    }

    void PlayExplosionAnimationThenDestroy()
    {
        if (winkRoutine != null)
            StopCoroutine(winkRoutine);

        if (animator != null)
            animator.SetTrigger(ExplodeParam);

        Destroy(gameObject, explodeAnimDuration);
    }

    // What enemies use to dodge: the tiles this bomb would burn, without exploding anything
    public List<Vector2Int> PredictBlastTiles()
    {
        var tiles = new List<Vector2Int> { gridPos };
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            for (int step = 1; step <= blastRadius; step++)
            {
                Vector2Int p = gridPos + dir * step;

                if (p.x < 0 || p.x >= GridManager.Instance.Width || p.y < 0 || p.y >= GridManager.Instance.Height)
                    break;

                TileType t = GridManager.Instance.GetTile(p);
                if (t == TileType.Wall) break;

                tiles.Add(p);
                if (t == TileType.Block || t == TileType.Bomb) break;
            }
        }

        return tiles;
    }

    void OnDestroy()
    {
        Active.Remove(this);

        if (!hasExploded && GridManager.Instance != null)
            GridManager.Instance.ClearTile(gridPos);
    }
}