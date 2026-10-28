using UnityEngine;
using TMPro;

// Put this on a TextMeshPro text in the main menu.
public class TopScoreText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private string format = "TOP {0}";
    [Tooltip("Pad the number with zeros to this many digits. 0 = no padding.")]
    [SerializeField] private int minDigits = 0;

    void Awake()
    {
        if (label == null)
            label = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        TopScore.Changed += OnChanged;
        Refresh();
    }

    void OnDisable()
    {
        TopScore.Changed -= OnChanged;
    }

    void OnChanged(int _) => Refresh();

    public void Refresh()
    {
        if (label == null) return;

        int value = TopScore.Get();
        string number = minDigits > 0 ? value.ToString("D" + minDigits) : value.ToString();
        label.text = string.Format(format, number);
    }

    [ContextMenu("Reset top score")]
    public void ResetTopScore()
    {
        TopScore.Clear();
        Refresh();
    }
}