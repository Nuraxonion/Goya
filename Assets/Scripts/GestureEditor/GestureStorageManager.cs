using System.IO;
using UnityEngine;

public class GestureStorageManager : MonoBehaviour
{
    public GestureDatabase database = new GestureDatabase();
    private string path;

    void Awake()
    {
        path = GestureFiles.PersistentPath(GestureFiles.GestureDatabaseFile);
        Load();
    }

    public void Save()
    {
        File.WriteAllText(path, JsonUtility.ToJson(database, true));
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