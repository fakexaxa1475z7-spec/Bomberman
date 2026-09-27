using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;
    private EnemyMovement movement;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
    }

    void Update()
    {
        var players = FindObjectsByType<PlayerGridMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.CurrentGridPos == movement.CurrentGridPos)
            {
                player.GetComponent<PlayerHealth>()?.TakeDamage(damageAmount);
            }
        }
    }
}