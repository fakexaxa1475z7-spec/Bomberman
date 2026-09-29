using UnityEngine;
using TMPro;
using System.Collections;

public class FadeController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private float fadeDuration = 0.5f;

    void Awake()
    {
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        if (stageText != null)
            stageText.gameObject.SetActive(false);
    }

    public void SetStageText(string text)
    {
        if (stageText == null) return;

        stageText.text = text;
        stageText.gameObject.SetActive(true);
    }

    public void HideStageText()
    {
        if (stageText != null)
            stageText.gameObject.SetActive(false);
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(0f, 1f);
    }

    public IEnumerator FadeIn()
    {
        yield return Fade(1f, 0f);
        HideStageText();
    }

    IEnumerator Fade(float from, float to)
    {
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = (to > 0.99f);
    }
}