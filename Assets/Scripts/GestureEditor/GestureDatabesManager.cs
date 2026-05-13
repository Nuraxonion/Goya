using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GestureDatabaseManager : MonoBehaviour
{
    public List<SavedGesture> gestures = new List<SavedGesture>();

    private string path;

    void Awake()
    {
        path = Application.persistentDataPath + "/gestures.json";
        Load();
    }

    public void AddGesture(string name, List<Vector2> points)
    {
        gestures.Add(new SavedGesture
        {
            name = name,
            points = new List<Vector2>(points)
        });

        Save();
    }

    public void DeleteGesture(int index)
    {
        if (index < 0 || index >= gestures.Count) return;

        gestures.RemoveAt(index);
        Save();
    }

    public void Save()
    {
        GestureDatabaseData data = new GestureDatabaseData
        {
            gestures = gestures
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public void Load()
    {
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        GestureDatabaseData data = JsonUtility.FromJson<GestureDatabaseData>(json);

        gestures = data.gestures ?? new List<SavedGesture>();
    }
}