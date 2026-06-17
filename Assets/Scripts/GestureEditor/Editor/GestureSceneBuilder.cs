#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor utility that builds the Free Drawing and Gesture Editor scenes from scratch,
/// wiring up every component reference so the systems are runnable with no manual setup.
/// Menu: Tools > Gesture System.
/// </summary>
public static class GestureSceneBuilder
{
    const string PrefabDir = "Assets/Prefabs/GestureSystem";
    const string SceneDir = "Assets/Scenes";
    const string FreeScenePath = SceneDir + "/FreeDrawing.unity";
    const string EditorScenePath = SceneDir + "/GestureTrainer.unity";

    // ----------------------------------------------------------------------------------
    // Menu entries
    // ----------------------------------------------------------------------------------

    [MenuItem("Tools/Gesture System/Build Free Drawing Scene")]
    public static void BuildFreeDrawingScene()
    {
        if (!ConfirmOverwrite(FreeScenePath)) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolders();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LineRenderer strokePrefab = CreateStrokePrefab(withFade: true);

        CreateCamera2D(new Color(0.08f, 0.08f, 0.1f));

        var root = new GameObject("FreeDrawingManager");
        var manager = root.AddComponent<FreeDrawingManager>();
        manager.strokePrefab = strokePrefab;
        manager.brushColor = Color.magenta;
        manager.lineWidth = 0.1f;
        manager.opacity = 1f;

        EditorSceneManager.SaveScene(scene, FreeScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GestureSceneBuilder] Built Free Drawing scene at {FreeScenePath}");
        EditorUtility.DisplayDialog("Gesture System",
            "Free Drawing scene built:\n" + FreeScenePath, "OK");
    }

    [MenuItem("Tools/Gesture System/Build Gesture Editor Scene")]
    public static void BuildGestureEditorScene()
    {
        if (!ConfirmOverwrite(EditorScenePath)) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolders();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LineRenderer linePrefab = CreateStrokePrefab(withFade: false);
        Button itemPrefab = CreateListItemPrefab();

        Camera cam = CreateCamera2D(new Color(0.10f, 0.10f, 0.12f));

        // EventSystem (new Input System module)
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Canvas in Screen Space - Camera so world-space strokes can render in front of the panel.
        const float planeDistance = 10f;
        var canvasGO = new GameObject("Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = planeDistance;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Draw area panel (right side). Semi-transparent so strokes are visible regardless of sort order.
        var drawAreaGO = new GameObject("DrawArea", typeof(RectTransform), typeof(Image));
        drawAreaGO.transform.SetParent(canvasGO.transform, false);
        var drawAreaRect = (RectTransform)drawAreaGO.transform;
        SetRect(drawAreaRect, new Vector2(350, 0), new Vector2(820, 820));
        var drawAreaImg = drawAreaGO.GetComponent<Image>();
        drawAreaImg.color = new Color(0f, 0f, 0f, 0.25f);

        // Strokes container + drawing manager (world-space strokes parented here).
        var drawingGO = new GameObject("DrawingManager");
        var drawing = drawingGO.AddComponent<GestureDrawingManager>();
        drawing.drawArea = drawAreaRect;
        drawing.uiCamera = cam;
        drawing.drawCamera = cam;
        drawing.linePrefab = linePrefab;
        drawing.drawDepth = planeDistance - 1f;   // 1 unit in front of the canvas plane
        drawing.lineColor = Color.cyan;
        drawing.lineWidth = 0.08f;
        drawing.minDistance = 5f;

        // Systems (storage, recognizer, UI controller)
        var systemsGO = new GameObject("GestureSystem");
        var storage = systemsGO.AddComponent<GestureStorageManager>();
        var recognizer = systemsGO.AddComponent<GestureRecognizerManager>();
        recognizer.storage = storage;
        var ui = systemsGO.AddComponent<GestureEditorUI>();
        ui.storage = storage;
        ui.drawing = drawing;
        ui.recognizer = recognizer;

        // ---- Left-hand control column ----
        TMP_InputField nameInput = CreateInputField(canvasGO.transform, "NameInput",
            new Vector2(-650, 380), new Vector2(360, 56), "Gesture name...");

        Button saveBtn = CreateButton(canvasGO.transform, "SaveButton", "Save",
            new Vector2(-740, 305), new Vector2(170, 56));
        Button testBtn = CreateButton(canvasGO.transform, "TestButton", "Test",
            new Vector2(-560, 305), new Vector2(170, 56));
        Button deleteBtn = CreateButton(canvasGO.transform, "DeleteButton", "Delete Selected",
            new Vector2(-650, 235), new Vector2(360, 56));

        TMP_Text resultText = CreateText(canvasGO.transform, "ResultText",
            "Draw a gesture and press Test.", new Vector2(-650, 150), new Vector2(360, 90));
        resultText.alignment = TextAlignmentOptions.TopLeft;
        resultText.fontSize = 22;
        resultText.color = Color.white;

        TMP_Text listTitle = CreateText(canvasGO.transform, "ListTitle",
            "Saved gestures:", new Vector2(-650, 80), new Vector2(360, 36));
        listTitle.fontSize = 22;
        listTitle.color = Color.white;

        // Scroll view holding the gesture list.
        RectTransform listContent = CreateScrollView(canvasGO.transform, "GestureList",
            new Vector2(-650, -200), new Vector2(360, 520));

        // Help text under the draw area.
        TMP_Text help = CreateText(canvasGO.transform, "Help",
            "Left-click drag = draw   •   Right-click = clear",
            new Vector2(350, -440), new Vector2(820, 36));
        help.alignment = TextAlignmentOptions.Center;
        help.fontSize = 20;
        help.color = new Color(1, 1, 1, 0.7f);

        // Wire UI references + button clicks (persistent so they survive the scene save).
        ui.nameInput = nameInput;
        ui.resultText = resultText;
        ui.listContainer = listContent;
        ui.listItemPrefab = itemPrefab;
        ui.normalColor = Color.white;
        ui.selectedColor = new Color(1f, 0.6f, 1f);

        UnityEventTools.AddPersistentListener(saveBtn.onClick, new UnityAction(ui.SaveGesture));
        UnityEventTools.AddPersistentListener(testBtn.onClick, new UnityAction(ui.TestGesture));
        UnityEventTools.AddPersistentListener(deleteBtn.onClick, new UnityAction(ui.DeleteSelected));

        EditorSceneManager.SaveScene(scene, EditorScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GestureSceneBuilder] Built Gesture Editor scene at {EditorScenePath}");
        EditorUtility.DisplayDialog("Gesture System",
            "Gesture Editor scene built:\n" + EditorScenePath, "OK");
    }

    // ----------------------------------------------------------------------------------
    // Prefab + material creation
    // ----------------------------------------------------------------------------------

    static LineRenderer CreateStrokePrefab(bool withFade)
    {
        Material mat = LoadOrCreateLineMaterial();

        string name = withFade ? "FadeStrokeLine" : "GestureStrokeLine";
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.useWorldSpace = true;
        lr.positionCount = 0;
        lr.numCapVertices = 6;
        lr.numCornerVertices = 6;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.startColor = withFade ? Color.magenta : Color.cyan;
        lr.endColor = lr.startColor;

        if (withFade)
            go.AddComponent<FadeStrokeController>();

        string path = $"{PrefabDir}/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<LineRenderer>();
    }

    static Material LoadOrCreateLineMaterial()
    {
        string path = $"{PrefabDir}/GestureLine.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var mat = new Material(Shader.Find("Sprites/Default"));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Button CreateListItemPrefab()
    {
        GameObject go = TMP_DefaultControls.CreateButton(DefaultRes());
        go.name = "GestureListItem";
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(340, 40);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 40;
        le.preferredHeight = 40;

        var label = go.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = "Gesture";
            label.fontSize = 22;
            label.color = new Color(0.1f, 0.1f, 0.1f);
        }

        string path = $"{PrefabDir}/GestureListItem.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<Button>();
    }

    // ----------------------------------------------------------------------------------
    // UI element helpers
    // ----------------------------------------------------------------------------------

    static TMP_InputField CreateInputField(Transform parent, string name, Vector2 pos, Vector2 size, string placeholder)
    {
        GameObject go = TMP_DefaultControls.CreateInputField(DefaultRes());
        go.name = name;
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, pos, size);

        var field = go.GetComponent<TMP_InputField>();
        if (field.placeholder is TMP_Text ph) ph.text = placeholder;
        field.pointSize = 24;
        if (field.textComponent != null) field.textComponent.fontSize = 24;
        return field;
    }

    static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        GameObject go = TMP_DefaultControls.CreateButton(DefaultRes());
        go.name = name;
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, pos, size);

        var label = go.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = text;
            label.fontSize = 22;
            label.color = new Color(0.1f, 0.1f, 0.1f);
        }
        return go.GetComponent<Button>();
    }

    static TMP_Text CreateText(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        GameObject go = TMP_DefaultControls.CreateText(DefaultRes());
        go.name = name;
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, pos, size);

        var t = go.GetComponent<TMP_Text>();
        t.text = text;
        return t;
    }

    static RectTransform CreateScrollView(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = DefaultControls.CreateScrollView(DefaultUGUIRes());
        go.name = name;
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, pos, size);

        // Give the scroll view a faint background.
        var img = go.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 1f, 1f, 0.06f);

        var content = (RectTransform)go.transform.Find("Viewport/Content");
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return content;
    }

    // ----------------------------------------------------------------------------------
    // Scene / object helpers
    // ----------------------------------------------------------------------------------

    static Camera CreateCamera2D(Color background)
    {
        var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        camGO.transform.position = new Vector3(0, 0, -10);
        return cam;
    }

    static void SetRect(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
    }

    static bool ConfirmOverwrite(string path)
    {
        if (!File.Exists(path)) return true;
        return EditorUtility.DisplayDialog("Overwrite scene?",
            $"{path} already exists.\n\nRebuild and overwrite it?", "Overwrite", "Cancel");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "GestureSystem");
        if (!AssetDatabase.IsValidFolder(SceneDir))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    static Sprite Builtin(string fileName) =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>($"UI/Skin/{fileName}.psd");

    static TMP_DefaultControls.Resources DefaultRes() => new TMP_DefaultControls.Resources
    {
        standard = Builtin("UISprite"),
        background = Builtin("Background"),
        inputField = Builtin("InputFieldBackground"),
        knob = Builtin("Knob"),
        checkmark = Builtin("Checkmark"),
        dropdown = Builtin("DropdownArrow"),
        mask = Builtin("UIMask"),
    };

    static DefaultControls.Resources DefaultUGUIRes() => new DefaultControls.Resources
    {
        standard = Builtin("UISprite"),
        background = Builtin("Background"),
        inputField = Builtin("InputFieldBackground"),
        knob = Builtin("Knob"),
        checkmark = Builtin("Checkmark"),
        dropdown = Builtin("DropdownArrow"),
        mask = Builtin("UIMask"),
    };
}
#endif
