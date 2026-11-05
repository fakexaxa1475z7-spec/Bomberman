using UnityEngine;

// Lives for the whole play session, so upgrades carry from stage to stage
public static class PlayerUpgrades
{
    public const int MaxBombs = 8;
    public const int MaxRadius = 8;
    public const int MaxSpeedLevel = 6;
    public const float InvincibleDuration = 30f;

    public static int Bombs = 1;
    public static int Radius = 1;
    public static int SpeedLevel = 0;
    public static bool HasRemote = false;

    public static bool HasWallPass = false;   // walk through breakable blocks
    public static bool HasBombPass = false;   // walk through bombs
    public static bool HasFlamePass = false;  // no damage from explosions

    public static float InvincibleRemaining = 0f; // seconds left
    public static bool IsInvincible => InvincibleRemaining > 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        Bombs = 1;
        Radius = 1;
        SpeedLevel = 0;
        HasRemote = false;
        HasWallPass = false;
        HasBombPass = false;
        HasFlamePass = false;
        InvincibleRemaining = 0f;
    }

    // Each returns false if it was already at the maximum / already owned
    public static bool AddBomb()
    {
        if (Bombs >= MaxBombs) return false;
        Bombs++;
        return true;
    }

    public static bool AddRadius()
    {
        if (Radius >= MaxRadius) return false;
        Radius++;
        return true;
    }

    public static bool AddSpeed()
    {
        if (SpeedLevel >= MaxSpeedLevel) return false;
        SpeedLevel++;
        return true;
    }

    public static bool GiveRemote()
    {
        if (HasRemote) return false;
        HasRemote = true;
        return true;
    }

    public static bool GiveWallPass()
    {
        if (HasWallPass) return false;
        HasWallPass = true;
        return true;
    }

    public static bool GiveBombPass()
    {
        if (HasBombPass) return false;
        HasBombPass = true;
        return true;
    }

    public static bool GiveFlamePass()
    {
        if (HasFlamePass) return false;
        HasFlamePass = true;
        return true;
    }

    // Picking up another one while invincible refreshes the timer back to 30 seconds
    public static void StartInvincible()
    {
        InvincibleRemaining = InvincibleDuration;
    }

    public static void TickInvincible(float deltaTime)
    {
        InvincibleRemaining = Mathf.Max(0f, InvincibleRemaining - deltaTime);
    }
}