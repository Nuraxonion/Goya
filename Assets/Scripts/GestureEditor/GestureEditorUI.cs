using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GestureEditorUI : MonoBehaviour
{
    [Header("Systems")]
    public GestureStorageManager storage;
    public GestureDrawingManager drawing;
    public GestureRecognizerManager recognizer;

    [Header("UI")]
    public TMP_InputField nameInput;
    public TMP_Text resultText;

    [Header("Gesture List")]
    [Tooltip("Parent (e.g. a Vertical Layout Group) the list buttons are spawned under.")]
    public Transform listContainer;
    [Tooltip("Button prefab with a TMP_Text child used as a list row.")]
    public Button listItemPrefab;
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.6f, 1f);

    private GestureEntry selectedGesture;
    private readonly List<Button> spawnedItems = new();

    void Start()
    {
        RefreshList();
    }

    public void SaveGesture()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;

        var points = drawing.GetPoints();

        if (points.Count < 5)
        {
            Debug.Log("FAILED: not enough points");
            return;
        }

        var gesture = storage.GetOrCreate(nameInput.text);

        gesture.samples.Add(new GestureSample
        {
            points = points
        });

        storage.Save();
        recognizer.Reload();
        RefreshList();
    }

    public void TestGesture()
    {
        var input = drawing.GetPoints();

        if (input.Count < 5)
        {
            resultText.text = "Draw a gesture first.";
            return;
        }

        var result = recognizer.Recognize(input);

        resultText.text =
            $"Recognized: {result.GestureClass}\nConfidence: {result.Score:F2}";
    }

    public void DeleteSelected()
    {
        if (selectedGesture == null) return;

        storage.database.gestures.Remove(selectedGesture);
        storage.Save();
        recognizer.Reload();

        selectedGesture = null;
        drawing.Clear();
        RefreshList();
    }

    // Rebuilds the list of saved gestures, one row per gesture name.
    public void RefreshList()
    {
        foreach (var item in spawnedItems)
            if (item != null) Destroy(item.gameObject);
        spawnedItems.Clear();

        if (listContainer == null || listItemPrefab == null) return;

        foreach (var gesture in storage.database.gestures)
        {
            GestureEntry captured = gesture; // capture for the closure

            Button item = Instantiate(listItemPrefab, listContainer);
            item.gameObject.SetActive(true);

            var label = item.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"{captured.name} ({captured.samples.Count})";

            item.onClick.AddListener(() => SelectGesture(captured));
            spawnedItems.Add(item);
        }

        UpdateHighlight();
    }

    // Loads the most recent sample of the chosen gesture back into the draw area.
    private void SelectGesture(GestureEntry gesture)
    {
        selectedGesture = gesture;
        nameInput.text = gesture.name;

        if (gesture.samples.Count > 0)
            drawing.LoadPoints(gesture.samples[^1].points);
        else
            drawing.Clear();

        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            var img = spawnedItems[i].GetComponent<Image>();
            if (img == null) continue;

            bool isSelected = selectedGesture != null
                && i < storage.database.gestures.Count
                && storage.database.gestures[i] == selectedGesture;

            img.color = isSelected ? selectedColor : normalColor;
        }
    }
}
