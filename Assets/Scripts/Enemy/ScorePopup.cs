using UnityEngine;
using TMPro;

public class ScorePopup : MonoBehaviour
{
    private TextMeshPro text;
    private Vector3 startPos;
    private Vector3 riseDir;
    private Color baseColor;
    private float riseDistance;
    private float duration;
    private float elapsed;

    private const float PopTime = 0.15f;     // time to grow to the overshoot size, and the same again to settle
    private const float PopOvershoot = 1.25f;
    private const float VanishStart = 0.75f; // fraction of the lifetime when it starts to disappear

    // Creates a popup text at a world position. riseDistance 0 = stays in place.
    public static void Spawn(Vector3 worldPos, string message, Color color, float fontSize, float riseDistance, float duration)
    {
        var go = new GameObject("ScorePopup");
        go.transform.position = worldPos;

        // Face the camera so the text reads upright from the top-down view
        Camera cam = Camera.main;
        go.transform.rotation = cam != null ? cam.transform.rotation : Quaternion.Euler(90f, 0f, 0f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(8f, 2f);
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        tmp.sortingOrder = 100;

        var popup = go.AddComponent<ScorePopup>();
        popup.text = tmp;
        popup.startPos = worldPos;
        popup.riseDir = go.transform.up; // "up" on screen
        popup.baseColor = color;
        popup.riseDistance = riseDistance;
        popup.duration = Mathf.Max(0.3f, duration);

        go.transform.localScale = Vector3.zero; // starts invisible, then pops in
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Optional drift (0 by default = stays where the enemy died)
        if (riseDistance != 0f)
        {
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = startPos + riseDir * (riseDistance * eased);
        }

        // Pop in: 0 -> overshoot -> normal size
        float scale;
        if (elapsed < PopTime)
            scale = Mathf.Lerp(0f, PopOvershoot, elapsed / PopTime);
        else if (elapsed < PopTime * 2f)
            scale = Mathf.Lerp(PopOvershoot, 1f, (elapsed - PopTime) / PopTime);
        else
            scale = 1f;

        // Disappear: shrink and fade over the last part of its life
        float vanish = Mathf.Clamp01((t - VanishStart) / (1f - VanishStart));
        scale *= 1f - vanish;

        Color c = baseColor;
        c.a = 1f - vanish;
        text.color = c;

        transform.localScale = Vector3.one * scale;

        if (t >= 1f)
            Destroy(gameObject);
    }
}