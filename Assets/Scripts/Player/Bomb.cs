using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface IDamageable
{
    void TakeDamage(int amount);
}

public class Bomb : MonoBehaviour
{
    [Header("Fuse / Explosion")]
    [SerializeField] private float fuseTime = 3f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionVisualDuration = 0.5f;
    [SerializeField] private float explodeAnimDuration = 0.4f; // match your explode clip's length

    [Header("Wink Effect (procedural, optional alongside animator)")]
    [SerializeField] private Renderer bombRenderer;
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color winkColor = Color.red;
    [SerializeField] private float scalePulseAmount = 0.15f;
    [SerializeField] private AnimationCurve winkSpeedCurve = AnimationCurve.Linear(0f, 1.5f, 1f, 6f);

    [Header("Animator")]
    [SerializeField] private Animator animator; // assign the bomb model's Animator here (or leave blank to auto-find)

    private static readonly int ExplodeParam = Animator.StringToHash("Explode");

    private Vector2Int gridPos;
    private int blastRadius;
    private BombPlacer owner;
    private bool hasExploded = false;

    private Vector3 baseScale;
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        baseScale = transform.localScale;
        propBlock = new MaterialPropertyBlock();

        if (bombRenderer == null)
            bombRenderer = GetComponent<Renderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void Initialize(Vector2Int position, int radius, BombPlacer bombOwner)
    {
        gridPos = position;
        blastRadius = radius;
        owner = bombOwner;

        GridManager.Instance.PlaceBomb(gridPos, gameObject);
        StartCoroutine(FuseCountdown());
        StartCoroutine(WinkEffect());
    }

    IEnumerator FuseCountdown()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    IEnumerator WinkEffect()
    {
        float elapsed = 0f;

        while (!hasExploded && elapsed < fuseTime)
        {
            float progress = elapsed / fuseTime;
            float pulseSpeed = winkSpeedCurve.Evaluate(progress);
            float pulse = (Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f) + 1f) / 2f;

            float scaleMultiplier = 1f + (pulse * scalePulseAmount);
            transform.localScale = baseScale * scaleMultiplier;

            if (bombRenderer != null && propBlock != null)
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

        owner.OnBombExploded();

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
        StopCoroutine(nameof(WinkEffect));

        if (animator != null)
            animator.SetTrigger(ExplodeParam);

        Destroy(gameObject, explodeAnimDuration);
    }

    void OnDestroy()
    {
        if (!hasExploded && GridManager.Instance != null)
            GridManager.Instance.ClearTile(gridPos);
    }
}