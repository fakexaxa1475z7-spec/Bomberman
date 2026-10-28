using System;
using UnityEngine;

public static class TopScore
{
    const string Key = "BOMBERMAN_TOP_SCORE";

    public static event Action<int> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Changed = null; }

    public static int Get() => PlayerPrefs.GetInt(Key, 0);

    // Call with the current score. Returns true if it beat the old record.
    public static bool Submit(int score)
    {
        if (score <= Get()) return false;

        PlayerPrefs.SetInt(Key, score);
        PlayerPrefs.Save();
        Changed?.Invoke(score);
        return true;
    }

    // For testing
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
        Changed?.Invoke(0);
    }
}