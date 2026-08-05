using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GestureAttackEntry
{
    public string gesture;   // gesture name, must match a name defined in the gesture editor
    public string attack;    // attack id PainterAttack knows how to spawn ("" = reserved / no attack yet)
}

[System.Serializable]
public class GestureAttackMapData
{
    public List<GestureAttackEntry> mappings = new();
}

// Loads the gesture-name -> attack-id mapping from an external data file so the
// gameplay loop no longer hardcodes which gesture triggers which attack.
// New attacks (e.g. spiral / butterfly) can be wired up purely by editing the
// data file plus adding a spawn handler in PainterAttack.
public static class GestureAttackMap
{
    public static Dictionary<string, string> Load()
    {
        var result = new Dictionary<string, string>();

        string text = GestureFiles.ReadText(GestureFiles.AttackMapFile);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning(
                $"[GestureAttackMap] '{GestureFiles.AttackMapFile}' not found in persistentData or StreamingAssets; " +
                "falling back to built-in defaults.");
            return Defaults();
        }

        var data = JsonUtility.FromJson<GestureAttackMapData>(text);
        if (data?.mappings == null)
        {
            Debug.LogWarning($"[GestureAttackMap] '{GestureFiles.AttackMapFile}' could not be parsed; using defaults.");
            return Defaults();
        }

        foreach (var entry in data.mappings)
            if (!string.IsNullOrEmpty(entry.gesture))
                result[entry.gesture] = entry.attack;

        return result;
    }

    // Mirrors the originally hardcoded mapping so the game still functions if the
    // external file is missing or corrupt.
    static Dictionary<string, string> Defaults() => new()
    {
        { "check", AttackIds.Fireball },
        { "circle", AttackIds.Wave },
        { "spiral", AttackIds.Spiral },
    };
}
