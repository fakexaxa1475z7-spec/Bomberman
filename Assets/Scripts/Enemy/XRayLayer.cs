using UnityEngine;

// Put this on the ghost's VISUAL child (the model), not on the root with the movement script.
// It moves the model onto the "XRay" layer so the renderer feature can draw a silhouette
// whenever a brick or wall is hiding it.
public class XRayLayer : MonoBehaviour
{
    [SerializeField] private string layerName = "XRay";

    void Awake()
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer < 0)
        {
            Debug.LogWarning($"XRayLayer: layer '{layerName}' doesn't exist. Add it in Project Settings > Tags and Layers.");
            return;
        }

        SetLayerRecursively(transform, layer);
    }

    static void SetLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform child in t)
            SetLayerRecursively(child, layer);
    }
}