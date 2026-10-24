using UnityEngine;

// Put this on the VISUAL child (the model), not on the root that has the movement script.
public class FaceCamera : MonoBehaviour
{
    [Tooltip("The object whose facing direction we read (the root with the movement script). Defaults to the parent.")]
    [SerializeField] private Transform directionSource;

    [Tooltip("World-space offset from the root. Raise Y so the model draws above blocks and walls; shift Z so the body is centered on its tile.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, -0.5f);

    [Tooltip("Show front/side/back depending on the walking direction. Turn off to always show the front.")]
    [SerializeField] private bool turnWithMovement = true;

    // Front toward the camera (+Y), head toward the top of the screen (+Z)
    private static readonly Quaternion FacingCamera = Quaternion.LookRotation(Vector3.up, Vector3.forward);

    void Awake()
    {
        if (directionSource == null)
            directionSource = transform.parent;
    }

    void LateUpdate()
    {
        if (directionSource == null) return;

        // Keep the model in a fixed spot relative to the root, regardless of how the root rotates
        transform.position = directionSource.position + worldOffset;

        float yaw = 0f;

        if (turnWithMovement)
        {
            Vector3 dir = directionSource.forward;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                // Down on screen -> front, left/right -> side, up -> back
                yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            }
        }

        transform.rotation = FacingCamera * Quaternion.Euler(0f, yaw, 0f);
    }
}