using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuNavigationHelper : MonoBehaviour
{
    [SerializeField] private Button firstSelectedButton;

    void OnEnable()
    {
        // Delay one frame — EventSystem sometimes isn't ready the exact frame a panel activates
        StartCoroutine(SelectNextFrame());
    }

    System.Collections.IEnumerator SelectNextFrame()
    {
        yield return null;

        EventSystem.current.SetSelectedGameObject(null); // clear first, avoids a stale-highlight bug
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
    }
}