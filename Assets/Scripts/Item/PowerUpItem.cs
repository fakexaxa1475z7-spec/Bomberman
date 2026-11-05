using UnityEngine;

public enum PowerUpType { BombUp, FireUp, SpeedUp, RemoteControl, WallPass, BombPass, FlamePass, InvincibleSuit }

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
        string label;

        switch (type)
        {
            case PowerUpType.BombUp:
                label = PlayerUpgrades.AddBomb() ? "BOMB UP" : "BOMB MAX";
                break;

            case PowerUpType.FireUp:
                label = PlayerUpgrades.AddRadius() ? "FIRE UP" : "FIRE MAX";
                break;

            case PowerUpType.SpeedUp:
                label = PlayerUpgrades.AddSpeed() ? "SPEED UP" : "SPEED MAX";
                break;

            case PowerUpType.RemoteControl:
                PlayerUpgrades.GiveRemote();
                label = "REMOTE";
                break;

            case PowerUpType.WallPass:
                PlayerUpgrades.GiveWallPass();
                label = "WALL PASS";
                break;

            case PowerUpType.BombPass:
                PlayerUpgrades.GiveBombPass();
                label = "BOMB PASS";
                break;

            case PowerUpType.FlamePass:
                PlayerUpgrades.GiveFlamePass();
                label = "FLAME PASS";
                break;

            case PowerUpType.InvincibleSuit:
                PlayerUpgrades.StartInvincible();
                label = "INVINCIBLE";
                break;

            default:
                label = "";
                break;
        }

        if (scoreValue > 0)
            HUDManager.Instance?.AddScore(scoreValue);

        AudioManager.Instance?.PlaySfx(pickupSfx);

        if (label.Length > 0)
            ScorePopup.Spawn(transform.position + new Vector3(0f, 3f, 0.2f), label, popupColor, 3.5f, 0f, 1f);

        Destroy(gameObject);
    }
}