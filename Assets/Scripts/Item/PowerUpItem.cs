using UnityEngine;

public enum PowerUpType { BombUp, FireUp, SpeedUp, RemoteControl }

public class PowerUpItem : MonoBehaviour
{
    [SerializeField] private PowerUpType type = PowerUpType.BombUp;
    [SerializeField] private int scoreValue = 0;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private Color popupColor = Color.white;

    private Vector2Int gridPos;
    private float nextCheck = 0f;

    void Start()
    {
        gridPos = GridManager.Instance.WorldToGrid(transform.position);
    }

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.05f;

        var players = FindObjectsByType<PlayerGridMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.CurrentGridPos != gridPos) continue;
            if (player.TryGetComponent(out PlayerHealth health) && health.IsDead) continue;

            Collect();
            return;
        }
    }

    void Collect()
    {
        bool applied;
        string label;

        switch (type)
        {
            case PowerUpType.BombUp:
                applied = PlayerUpgrades.AddBomb();
                label = applied ? "BOMB UP" : "BOMB MAX";
                break;

            case PowerUpType.FireUp:
                applied = PlayerUpgrades.AddRadius();
                label = applied ? "FIRE UP" : "FIRE MAX";
                break;

            case PowerUpType.SpeedUp:
                applied = PlayerUpgrades.AddSpeed();
                label = applied ? "SPEED UP" : "SPEED MAX";
                break;

            default: // RemoteControl
                applied = PlayerUpgrades.GiveRemote();
                label = applied ? "REMOTE" : "REMOTE";
                break;
        }

        if (scoreValue > 0)
            HUDManager.Instance?.AddScore(scoreValue);

        AudioManager.Instance?.PlaySfx(pickupSfx);

        ScorePopup.Spawn(transform.position + new Vector3(0f, 3f, 0.2f), label, popupColor, 3.5f, 0f, 1f);

        Destroy(gameObject);
    }
}