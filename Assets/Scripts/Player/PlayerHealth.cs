using UnityEngine;
using System.Collections;
using System;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]

    [SerializeField] private int startingHealth = 2;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityDuration = 1.5f;

    private int currentHealth;
    private bool isInvincible = false;
    private bool isDead = false;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int StartingHealth => startingHealth;
    public bool IsDead => isDead;

    void Awake()
    {
        currentHealth = startingHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        OnHealthChanged?.Invoke(currentHealth, startingHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            GetComponent<PlayerAnimationController>()?.PlayHit();
            StartCoroutine(InvincibilityFrames());
        }
    }

    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();

        GetComponent<PlayerAnimationController>()?.PlayDie();

        OnDeath?.Invoke();

        GetComponent<PlayerGridMovement>().enabled = false;
        GetComponent<BombPlacer>().enabled = false;
    }

    public void ResetHealth()
    {
        currentHealth = startingHealth;
        isDead = false;
        isInvincible = false;
        StopAllCoroutines();

        OnHealthChanged?.Invoke(currentHealth, startingHealth);
    }
    public void SetHealth(int newHealth)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }
}