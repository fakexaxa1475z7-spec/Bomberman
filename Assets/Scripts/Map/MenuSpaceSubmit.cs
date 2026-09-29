using UnityEngine;
using UnityEngine.EventSystems;

public class MenuSpaceSubmit : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return;

            ExecuteEvents.Execute(
                selected,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler
            );
        }
    }
}