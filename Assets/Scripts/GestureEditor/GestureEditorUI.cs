using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GestureEditorUI : MonoBehaviour
{
    public GestureDatabaseManager database;
    public GestureRecorder recorder;

    public TMP_InputField nameInput;
    public Transform listParent;
    public GameObject buttonPrefab;

    private int selectedIndex = -1;

    void Start()
    {
        RefreshList();
    }

    public void OnAddGesture()
    {
        recorder.StartRecording();
    }

    public void OnStopAndSave()
    {
        var points = recorder.StopRecording();

        if (string.IsNullOrEmpty(nameInput.text)) return;

        database.AddGesture(nameInput.text, points);

        RefreshList();
    }

    public void OnDelete()
    {
        if (selectedIndex == -1) return;

        database.DeleteGesture(selectedIndex);

        selectedIndex = -1;
        RefreshList();
    }

    public void RefreshList()
    {
        foreach (Transform child in listParent)
            Destroy(child.gameObject);

        for (int i = 0; i < database.gestures.Count; i++)
        {
            int index = i;

            GameObject btn = Instantiate(buttonPrefab, listParent);

            btn.GetComponentInChildren<TMP_Text>().text =
                database.gestures[i].name;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedIndex = index;
            });
        }
    }
}