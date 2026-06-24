using System.IO;
using UnityEngine;

public class GestureStorageManager : MonoBehaviour
{
    public GestureDatabase database = new GestureDatabase();
    private string path;

    void Awake()
    {
        path = GestureFiles.SavePath(GestureFiles.GestureDatabaseFile);
        Load();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(database, true));

#if UNITY_EDITOR
        // Import the freshly written file so it appears in the Project window and
        // gets a .meta for version control.
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    public void Load()
    {
        database = LoadDatabase();
    }

    // Loads the gesture database from disk (persistent copy first, shipped seed as
    // fallback). Static so gameplay systems can load gestures without needing a
    // GestureStorageManager instance wired into their scene.
    public static GestureDatabase LoadDatabase()
    {
        string text = GestureFiles.ReadText(GestureFiles.GestureDatabaseFile);
        if (string.IsNullOrEmpty(text)) return new GestureDatabase();
        return JsonUtility.FromJson<GestureDatabase>(text) ?? new GestureDatabase();
    }

    public GestureEntry GetOrCreate(string name)
    {
        var g = database.gestures.Find(x => x.name == name);
        if (g != null) return g;

        g = new GestureEntry { name = name };
        database.gestures.Add(g);
        return g;
    }
}