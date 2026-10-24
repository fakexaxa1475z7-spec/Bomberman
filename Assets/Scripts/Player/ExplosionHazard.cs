using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ExplosionHazard : MonoBehaviour
{
    private static readonly Dictionary<Vector2Int, int> activeTiles = new Dictionary<Vector2Int, int>();

    public static bool IsHazard(Vector2Int pos) => activeTiles.ContainsKey(pos);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { activeTiles.Clear(); }

    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float damageCheckInterval = 0.05f;

    private Vector2Int gridPos;
    private bool registered = false;

    public void Initialize(Vector2Int position, int damage, float lifetime)
    {
        gridPos = position;
        damageAmount = damage;
        duration = lifetime;

        activeTiles[gridPos] = (activeTiles.TryGetValue(gridPos, out int count) ? count : 0) + 1;
        registered = true;

        StartCoroutine(HazardLifetime());
    }

    void OnDestroy()
    {
        if (!registered) return;

        if (activeTiles.TryGetValue(gridPos, out int count))
        {
            if (count <= 1) activeTiles.Remove(gridPos);
            else activeTiles[gridPos] = count - 1;
        }
    }

    IEnumerator HazardLifetime()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            CheckDamage();
            yield return new WaitForSeconds(damageCheckInterval);
            elapsed += damageCheckInterval;
        }

        Destroy(gameObject);
    }

    void CheckDamage()
    {
        var players = FindObjectsByType<PlayerGridMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.CurrentGridPos == gridPos)
                player.GetComponent<IDamageable>()?.TakeDamage(damageAmount);
        }

        var enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.CurrentGridPos == gridPos)
                enemy.GetComponent<IDamageable>()?.TakeDamage(damageAmount);
        }
    }
}