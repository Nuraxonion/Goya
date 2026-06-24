using System.IO;
using UnityEngine;

// Centralizes where gesture data files live and how they are read.
//
// Gesture data is read with a two-tier fallback:
//   1. Application.persistentDataPath  -> the user-writable copy the gesture
//      editor saves to (and the place designers can tweak after a build).
//   2. Application.streamingAssetsPath -> the read-only seed shipped with the
//      game so a fresh install still has the four gestures and their mappings.
//
// Note: on platforms where StreamingAssets is not a regular file path
// (e.g. Android), the streaming fallback would need UnityWebRequest. This
// project targets standalone Windows, where direct file access is valid.
public static class GestureFiles
{
    public const string GestureDatabaseFile = "gestures.json";
    public const string AttackMapFile = "gesture_attack_map.json";

    public static string PersistentPath(string fileName) =>
        Path.Combine(Application.persistentDataPath, fileName);

    public static string StreamingPath(string fileName) =>
        Path.Combine(Application.streamingAssetsPath, fileName);

    // Returns the file contents, preferring the persistent copy and falling back
    // to the shipped seed. Returns null when neither exists.
    public static string ReadText(string fileName)
    {
        string persistent = PersistentPath(fileName);
        if (File.Exists(persistent))
            return File.ReadAllText(persistent);

        string streaming = StreamingPath(fileName);
        if (File.Exists(streaming))
            return File.ReadAllText(streaming);

        return null;
    }
}
