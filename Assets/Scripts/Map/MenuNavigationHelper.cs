using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuNavigationHelper : MonoBehaviour
{
    [SerializeField] private Button firstSelectedButton;

    void OnEnable()
    {
        if (firstSelectedButton == null) return;
        StartCoroutine(SelectNextFrame());
    }

    System.Collections.IEnumerator SelectNextFrame()
    {
        yield return null;

        if (EventSystem.current == null || firstSelectedButton == null)
            yield break;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
    }
}