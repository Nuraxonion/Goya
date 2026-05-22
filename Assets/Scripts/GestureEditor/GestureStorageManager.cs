using System.IO;
using UnityEngine;

public class GestureStorageManager : MonoBehaviour
{
    public GestureDatabase database = new GestureDatabase();
    private string path;

    void Awake()
    {
        path = Application.persistentDataPath + "/gestures.json";
        Load();
    }

    public void Save()
    {
        File.WriteAllText(path, JsonUtility.ToJson(database, true));
    }

    public void Load()
    {
        if (!File.Exists(path)) return;
        database = JsonUtility.FromJson<GestureDatabase>(File.ReadAllText(path));
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