using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuNavigationHelper : MonoBehaviour
{
    [SerializeField] private Button firstSelectedButton;

    void OnEnable()
    {
        Debug.Log($"MenuNavigationHelper.OnEnable on '{gameObject.name}', firstSelectedButton = {(firstSelectedButton != null ? firstSelectedButton.name : "NULL")}");

        StartCoroutine(SelectNextFrame());
    }

    System.Collections.IEnumerator SelectNextFrame()
    {
        yield return null;

        Debug.Log($"SelectNextFrame resuming on '{gameObject.name}', EventSystem.current = {(EventSystem.current != null ? EventSystem.current.name : "NULL")}, firstSelectedButton = {(firstSelectedButton != null ? firstSelectedButton.name : "NULL")}");

        if (EventSystem.current == null || firstSelectedButton == null)
        {
            Debug.LogWarning($"MenuNavigationHelper on '{gameObject.name}': missing EventSystem or firstSelectedButton — skipping.");
            yield break;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
    }
}