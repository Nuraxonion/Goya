using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SavedGesture
{
    public string name;
    public List<Vector2> points;
}

[Serializable]
public class GestureDatabaseData
{
    public List<SavedGesture> gestures = new List<SavedGesture>();
}