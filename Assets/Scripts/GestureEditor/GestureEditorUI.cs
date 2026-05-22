using TMPro;
using UnityEngine;

public class GestureEditorUI : MonoBehaviour
{
    public GestureStorageManager storage;
    public GestureDrawingManager drawing;
    public GestureRecognizerManager recognizer;

    public TMP_InputField nameInput;
    public TMP_Text resultText;

    private GestureEntry selectedGesture;

    public void SaveGesture()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;
        if (drawing.GetPoints().Count < 5) return;

        var gesture = storage.GetOrCreate(nameInput.text);

        gesture.samples.Add(new GestureSample
        {
            points = drawing.GetPoints()
        });

        storage.Save();
        recognizer.Reload();
    }

    public void TestGesture()
    {
        var result = recognizer.Recognize(drawing.GetPoints());

        resultText.text =
            $"Recognized: {result.GestureClass}\nConfidence: {result.Score:F2}";
    }

    public void DeleteSelected()
    {
        if (selectedGesture == null) return;

        storage.database.gestures.Remove(selectedGesture);
        storage.Save();
        recognizer.Reload();
    }
}