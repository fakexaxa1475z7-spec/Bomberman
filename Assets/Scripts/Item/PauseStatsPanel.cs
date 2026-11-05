using System.Text;
using TMPro;
using UnityEngine;

// Put this on a TextMeshPro text that sits inside the pause panel.
public class PauseStatsPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsText;

    [Tooltip("Where the value column starts, as a percentage of the text width.")]
    [Range(30, 90)]
    [SerializeField] private int valueColumnPercent = 62;

    [SerializeField] private Color yesColor = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color noColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.2f);

    void Awake()
    {
        if (statsText == null)
            statsText = GetComponent<TextMeshProUGUI>();
    }

    // Runs every time the pause panel opens
    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (statsText == null) return;

        var player = FindFirstObjectByType<PlayerGridMovement>();
        string speed = player != null
            ? player.TotalSpeed.ToString("0.0")
            : "LV " + PlayerUpgrades.SpeedLevel;

        var sb = new StringBuilder();

        AppendRow(sb, "BOMBS", $"{PlayerUpgrades.Bombs} / {PlayerUpgrades.MaxBombs}");
        AppendRow(sb, "FIRE RANGE", $"{PlayerUpgrades.Radius} / {PlayerUpgrades.MaxRadius}");
        AppendRow(sb, "SPEED", speed);
        AppendRow(sb, "BOMB TYPE", PlayerUpgrades.HasRemote ? Colored("REMOTE (F)", activeColor) : "NORMAL");
        AppendRow(sb, "WALL PASS", YesNo(PlayerUpgrades.HasWallPass));
        AppendRow(sb, "BOMB PASS", YesNo(PlayerUpgrades.HasBombPass));
        AppendRow(sb, "FLAME PASS", YesNo(PlayerUpgrades.HasFlamePass));

        string suit = PlayerUpgrades.IsInvincible
            ? Colored(Mathf.CeilToInt(PlayerUpgrades.InvincibleRemaining) + "s", activeColor)
            : Colored("--", noColor);
        AppendRow(sb, "INVINCIBLE", suit, last: true);

        statsText.text = sb.ToString();
    }

    void AppendRow(StringBuilder sb, string label, string value, bool last = false)
    {
        sb.Append(label)
          .Append("<pos=").Append(valueColumnPercent).Append("%>")
          .Append(value);

        if (!last) sb.Append('\n');
    }

    string YesNo(bool value)
    {
        return value ? Colored("YES", yesColor) : Colored("NO", noColor);
    }

    string Colored(string text, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
    }
}