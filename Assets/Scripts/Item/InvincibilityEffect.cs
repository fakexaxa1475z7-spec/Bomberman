using System.Collections.Generic;
using UnityEngine;

// Makes the player model blink while the Invincible Suit is active (faster in the last seconds).
// The countdown itself is handled by HUDManager.
public class InvincibilityEffect : MonoBehaviour
{
    [SerializeField] private float flickerInterval = 0.12f;
    [SerializeField] private float warningTime = 5f;
    [SerializeField] private float warningFlickerInterval = 0.05f;

    private readonly List<Renderer> renderers = new List<Renderer>();
    private float flickerTimer = 0f;
    private bool visible = true;
    private bool wasInvincible = false;

    void Awake()
    {
        // Only the renderers that are visible now, so hidden parts never get switched on
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r.enabled)
                renderers.Add(r);
    }

    void Update()
    {
        bool active = PlayerUpgrades.IsInvincible;

        if (active)
        {
            float interval = PlayerUpgrades.InvincibleRemaining <= warningTime
                ? warningFlickerInterval
                : flickerInterval;

            flickerTimer += Time.deltaTime;
            if (flickerTimer >= interval)
            {
                flickerTimer = 0f;
                visible = !visible;
                SetVisible(visible);
            }
        }
        else if (wasInvincible)
        {
            visible = true;
            SetVisible(true); // suit ran out: make sure the model is shown again
        }

        wasInvincible = active;
    }

    void SetVisible(bool value)
    {
        foreach (var r in renderers)
            if (r != null) r.enabled = value;
    }

    void OnDisable()
    {
        SetVisible(true);
    }
}