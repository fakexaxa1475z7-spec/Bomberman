using UnityEngine;

public class ExitItemVisual : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float spinSpeed = 90f; // degrees/sec

    private Vector3 baseLocalPos;

    void Start()
    {
        baseLocalPos = transform.localPosition;
    }

    void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = baseLocalPos + Vector3.up * bob;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}