using UnityEngine;
using UnityEngine.EventSystems;

public class MenuSelectionArrow : MonoBehaviour
{
    [SerializeField] private RectTransform arrowTransform;
    [SerializeField] private float horizontalOffset = -30f;

    private RectTransform lastSelected;

    void Awake()
    {
        arrowTransform.SetAsLastSibling(); // ensures it draws above all buttons, regardless of hierarchy order
    }

    void Update()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
        {
            arrowTransform.gameObject.SetActive(false);
            return;
        }

        RectTransform selectedRect = selected.GetComponent<RectTransform>();
        if (selectedRect == null) return;

        if (selectedRect != lastSelected)
        {
            arrowTransform.gameObject.SetActive(true);
            arrowTransform.SetAsLastSibling(); // re-assert in case something else got added above it since
            lastSelected = selectedRect;
        }

        PositionArrow(selectedRect);
    }

    void PositionArrow(RectTransform target)
    {
        Vector3 targetPos = target.position;
        arrowTransform.position = new Vector3(
            targetPos.x + horizontalOffset,
            targetPos.y,
            targetPos.z
        );
    }
}