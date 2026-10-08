using UnityEngine;

[System.Serializable]
public class SaveSlotData
{
    public int stageIndex;
    public int health;
    public int score;
    public int gridX;
    public int gridY;
    public float timeSeconds;
    public string blockGrid;   // "XXYYBB" tokens, one line per row
    public bool hasExit;
    public int exitX;
    public int exitY;
}

public static class SaveSystem
{
    const string KeyPrefix = "BOMBERMAN_SAVE_";

    public static string GenerateUniqueCode()
    {
        string code;
        do { code = RandomCode(6); }
        while (PlayerPrefs.HasKey(KeyPrefix + code));
        return code;
    }

    static string RandomCode(int length)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var c = new char[length];
        for (int i = 0; i < length; i++)
            c[i] = chars[Random.Range(0, chars.Length)];
        return new string(c);
    }

    public static void Save(string code, SaveSlotData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KeyPrefix + code.Trim().ToUpperInvariant(), json);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(string code, out SaveSlotData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(code)) return false;

        string key = KeyPrefix + code.Trim().ToUpperInvariant();
        if (!PlayerPrefs.HasKey(key)) return false;

        data = JsonUtility.FromJson<SaveSlotData>(PlayerPrefs.GetString(key));
        return data != null;
    }
}