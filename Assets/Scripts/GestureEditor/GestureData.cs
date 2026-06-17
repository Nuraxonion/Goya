using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GestureSample
{
    public List<Vector2> points = new();
}

[System.Serializable]
public class GestureEntry
{
    public string name;
    public List<GestureSample> samples = new();
}

[System.Serializable]
public class GestureDatabase
{
    public List<GestureEntry> gestures = new();
}