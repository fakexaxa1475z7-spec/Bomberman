using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private float hitCooldown = 0.6f; // longer than one explosion, so a blast counts once
    [SerializeField] private int scoreValue = 100;

    private int currentHealth;
    private bool isDead = false;
    private float nextHitTime = 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (Time.time < nextHitTime) return;

        nextHitTime = Time.time + hitCooldown;
        currentHealth -= amount;

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        HUDManager.Instance?.AddScore(scoreValue);
        Destroy(gameObject);
    }
}