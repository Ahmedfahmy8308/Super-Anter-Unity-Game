using System.IO;
using UnityEngine;

public static class GameDataLoader
{
    public static bool TryLoadFromText(string json, out GameDataRuntimeData data)
    {
        data = default;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<GameDataRuntimeData>(json);
            return data != null;
        }
        catch
        {
            data = default;
            return false;
        }
    }

    public static bool TryLoadFromFile(string filePath, out GameDataRuntimeData data)
    {
        data = default;

        if (!File.Exists(filePath))
        {
            return false;
        }

        return TryLoadFromText(File.ReadAllText(filePath), out data);
    }
}
