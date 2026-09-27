using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ExplosionHazard : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float damageCheckInterval = 0.05f; // how often to re-check overlap while active

    private Vector2Int gridPos;

    public void Initialize(Vector2Int position, int damage, float lifetime)
    {
        gridPos = position;
        damageAmount = damage;
        duration = lifetime;

        StartCoroutine(HazardLifetime());
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