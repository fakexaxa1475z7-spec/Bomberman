using UnityEngine;

// Lives for the whole play session, so upgrades carry from stage to stage
public static class PlayerUpgrades
{
    public const int MaxBombs = 8;
    public const int MaxRadius = 8;
    public const int MaxSpeedLevel = 6;

    public static int Bombs = 1;
    public static int Radius = 1;
    public static int SpeedLevel = 0;
    public static bool HasRemote = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        Bombs = 1;
        Radius = 1;
        SpeedLevel = 0;
        HasRemote = false;
    }

    // Each returns false if it was already at the maximum
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
}