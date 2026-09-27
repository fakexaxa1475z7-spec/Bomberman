using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerGridMovement movement;
    [SerializeField] private PlayerHealth health;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int PlaceBombParam = Animator.StringToHash("PlaceBomb");
    private static readonly int HitParam = Animator.StringToHash("Hit");
    private static readonly int DieParam = Animator.StringToHash("Die");

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (movement == null) movement = GetComponent<PlayerGridMovement>();
        if (health == null) health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (health != null && health.IsDead) return; // stop driving the animator entirely once dead

        animator.SetFloat(SpeedParam, movement.CurrentSpeed > 0.01f ? 1f : 0f);
    }

    public void PlayPlaceBomb() => animator.SetTrigger(PlaceBombParam);
    public void PlayHit() => animator.SetTrigger(HitParam);
    public void PlayDie()
    {
        animator.SetFloat(SpeedParam, 0f);
        animator.SetTrigger(DieParam);
    }
}