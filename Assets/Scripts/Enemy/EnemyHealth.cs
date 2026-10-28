using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private float hitCooldown = 0.6f; // longer than one explosion, so a blast counts once
    [SerializeField] private int scoreValue = 100;

    [Header("Score Popup")]
    [SerializeField] private bool showScorePopup = true;
    [SerializeField] private Color popupColor = Color.yellow;
    [SerializeField] private float popupSize = 5f;        // bigger number = bigger text
    [SerializeField] private float popupRise = 0f;      // how far it floats, in tiles
    [SerializeField] private float popupDuration = 0.8f;    // seconds
    [Tooltip("Y keeps it above bricks and walls, Z nudges it toward the top of the screen.")]
    [SerializeField] private Vector3 popupOffset = new Vector3(0f, 3f, 0.2f);

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

        if (showScorePopup && scoreValue > 0)
        {
            ScorePopup.Spawn(
                transform.position + popupOffset,
                scoreValue.ToString(),
                popupColor,
                popupSize,
                popupRise,
                popupDuration);
        }

        Destroy(gameObject);
    }
}