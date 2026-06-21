#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 生成 SettingsPanel.prefab。Unity 打开项目时若 Prefab 不存在会自动创建。
/// </summary>
public static class SettingsPanelPrefabFactory
{
    private const string PrefabPath = "Assets/Resources/Prefabs/SettingsPanel.prefab";
    private const string FontAssetPath = "Assets/Resources/font/哥特.asset";
    private const string ButtonSpritePath = "Assets/Resources/Art/UIs/按钮/buttons_0002s_0001_图层-1.png";
    private static readonly Color s_panelTextColor = Color.black;

    [InitializeOnLoadMethod]
    private static void EnsurePrefabOnLoad()
    {
        EditorApplication.delayCall += TryCreatePrefabIfMissing;
    }

    [MenuItem("Tools/UI/构建设置 Panel Prefab")]
    public static void BuildPrefabMenu()
    {
        CreateOrUpdatePrefab();
        Debug.Log($"[SettingsPanelPrefabFactory] Prefab 已生成：{PrefabPath}");
    }

    private static void TryCreatePrefabIfMissing()
    {
        if (File.Exists(PrefabPath))
        {
            return;
        }

        CreateOrUpdatePrefab();
        Debug.Log($"[SettingsPanelPrefabFactory] 已自动生成 Prefab：{PrefabPath}");
    }

    public static GameObject CreateOrUpdatePrefab()
    {
        EnsureDirectory("Assets/Resources/Prefabs");

        GameObject host = BuildHierarchy();
        SettingsPanelView view = host.GetComponent<SettingsPanelView>();
        WirePersistentEvents(view);

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(host, PrefabPath);
        Object.DestroyImmediate(host);
        AssetDatabase.SaveAssets();
        return prefabAsset;
    }

    private static GameObject BuildHierarchy()
    {
        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art/背景.png");
        TMP_FontAsset panelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
        var resources = new DefaultControls.Resources();

        if (panelFont == null)
        {
            Debug.LogWarning($"[SettingsPanelPrefabFactory] 未找到字体：{FontAssetPath}，中文可能显示为方框。");
            panelFont = TMP_Settings.defaultFontAsset;
        }

        if (buttonSprite == null)
        {
            Debug.LogWarning($"[SettingsPanelPrefabFactory] 未找到按钮图：{ButtonSpritePath}");
        }

        // Host 始终激活，通过 CanvasGroup alpha 控制显隐；嵌套 Canvas 保证渲染在最上层
        GameObject host = new GameObject(
            "SettingsPanel",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(GraphicRaycaster),
            typeof(SettingsPanelView));
        StretchFull(host.GetComponent<RectTransform>());
        Canvas hostCanvas = host.GetComponent<Canvas>();
        hostCanvas.overrideSorting = true;
        hostCanvas.sortingOrder = 100;

        GameObject panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
        panelRoot.transform.SetParent(host.transform, false);
        StretchFull(panelRoot.GetComponent<RectTransform>());

        GameObject dim = CreateImage("DimOverlay", panelRoot.transform, new Color(0f, 0f, 0f, 0.65f));
        StretchFull(dim.GetComponent<RectTransform>());

        GameObject panelContainer = new GameObject("PanelContainer", typeof(RectTransform));
        panelContainer.transform.SetParent(panelRoot.transform, false);
        RectTransform panelRect = panelContainer.GetComponent<RectTransform>();
        // 按 Canvas 百分比留边，适配不同分辨率
        StretchAnchored(panelRect, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);

        GameObject background = CreateImage("Background", panelContainer.transform, Color.white);
        StretchFull(background.GetComponent<RectTransform>());
        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.preserveAspect = false;
            backgroundImage.type = Image.Type.Simple;
        }

        TMP_Text title = CreateTMP("Title", panelContainer.transform, "设置", 48, FontStyles.Normal, panelFont, s_panelTextColor);
        RectTransform titleRect = title.rectTransform;
        StretchAnchored(titleRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -88f), new Vector2(-24f, -16f));
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = true;
        title.fontSizeMin = 28f;
        title.fontSizeMax = 48f;

        GameObject volumeRow = new GameObject("VolumeRow", typeof(RectTransform));
        volumeRow.transform.SetParent(panelContainer.transform, false);
        RectTransform volumeRowRect = volumeRow.GetComponent<RectTransform>();
        StretchAnchored(volumeRowRect, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.58f), new Vector2(0f, -32f), new Vector2(0f, 32f));

        TMP_Text volumeLabel = CreateTMP("VolumeLabel", volumeRow.transform, "音量", 32, FontStyles.Normal, panelFont, s_panelTextColor);
        RectTransform volumeLabelRect = volumeLabel.rectTransform;
        StretchAnchored(volumeLabelRect, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(96f, 0f));
        volumeLabel.alignment = TextAlignmentOptions.MidlineLeft;
        volumeLabel.enableAutoSizing = true;
        volumeLabel.fontSizeMin = 20f;
        volumeLabel.fontSizeMax = 32f;

        GameObject sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.name = "VolumeSlider";
        sliderObject.transform.SetParent(volumeRow.transform, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        StretchAnchored(sliderRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(104f, -18f), new Vector2(-88f, 18f));
        Slider volumeSlider = sliderObject.GetComponent<Slider>();
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = 1f;

        TMP_Text volumeValueText = CreateTMP("VolumeValueText", volumeRow.transform, "100%", 28, FontStyles.Normal, panelFont, s_panelTextColor);
        RectTransform volumeValueRect = volumeValueText.rectTransform;
        StretchAnchored(volumeValueRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-80f, 0f), Vector2.zero);
        volumeValueText.alignment = TextAlignmentOptions.MidlineRight;
        volumeValueText.enableAutoSizing = true;
        volumeValueText.fontSizeMin = 18f;
        volumeValueText.fontSizeMax = 28f;

        Button returnButton = CreateButton(panelContainer.transform, "ReturnButton", "返回游戏", buttonSprite, panelFont);
        StretchAnchored(
            returnButton.GetComponent<RectTransform>(),
            new Vector2(0.16f, 0.38f),
            new Vector2(0.84f, 0.38f),
            new Vector2(0f, -34f),
            new Vector2(0f, 34f));

        Button quitButton = CreateButton(panelContainer.transform, "QuitButton", "退出游戏", buttonSprite, panelFont);
        StretchAnchored(
            quitButton.GetComponent<RectTransform>(),
            new Vector2(0.16f, 0.18f),
            new Vector2(0.84f, 0.18f),
            new Vector2(0f, -34f),
            new Vector2(0f, 34f));

        SettingsPanelView view = host.GetComponent<SettingsPanelView>();
        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("m_panelRoot").objectReferenceValue = panelRoot;
        serializedView.FindProperty("m_volumeSlider").objectReferenceValue = volumeSlider;
        serializedView.FindProperty("m_volumeValueText").objectReferenceValue = volumeValueText;
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        SetLayerRecursively(host, 5);
        return host;
    }

    private static void WirePersistentEvents(SettingsPanelView view)
    {
        Transform host = view.transform;
        Button returnButton = host.Find("PanelRoot/PanelContainer/ReturnButton")?.GetComponent<Button>();
        Button quitButton = host.Find("PanelRoot/PanelContainer/QuitButton")?.GetComponent<Button>();
        Slider volumeSlider = host.Find("PanelRoot/PanelContainer/VolumeRow/VolumeSlider")?.GetComponent<Slider>();

        if (returnButton != null)
        {
            ClearPersistentListeners(returnButton.onClick);
            UnityEventTools.AddPersistentListener(returnButton.onClick, view.Close);
        }

        if (quitButton != null)
        {
            ClearPersistentListeners(quitButton.onClick);
            UnityEventTools.AddPersistentListener(quitButton.onClick, view.QuitGame);
        }

        if (volumeSlider != null)
        {
            ClearPersistentListeners(volumeSlider.onValueChanged);
            UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, view.OnVolumeChanged);
        }
    }

    private static Button CreateButton(Transform parent, string objectName, string label, Sprite sprite, TMP_FontAsset font)
    {
        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObject.name = objectName;
        buttonObject.transform.SetParent(parent, false);

        Text legacyText = buttonObject.GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            Object.DestroyImmediate(legacyText);
        }

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        StretchFull(textObject.GetComponent<RectTransform>());
        TMP_Text tmpText = textObject.GetComponent<TextMeshProUGUI>();
        ApplyTypography(tmpText, font, label, 36f, FontStyles.Normal, s_panelTextColor);
        tmpText.enableAutoSizing = true;
        tmpText.fontSizeMin = 24f;
        tmpText.fontSizeMax = 40f;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.verticalAlignment = VerticalAlignmentOptions.Middle;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.55f);
        button.colors = colors;
        return button;
    }

    private static GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = DefaultControls.CreateImage(new DefaultControls.Resources());
        imageObject.name = objectName;
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return imageObject;
    }

    private static TMP_Text CreateTMP(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle,
        TMP_FontAsset font,
        Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text tmpText = textObject.GetComponent<TextMeshProUGUI>();
        ApplyTypography(tmpText, font, text, fontSize, fontStyle, color);
        tmpText.raycastTarget = false;
        return tmpText;
    }

    private static void ApplyTypography(
        TMP_Text tmpText,
        TMP_FontAsset font,
        string text,
        float fontSize,
        FontStyles fontStyle,
        Color color)
    {
        tmpText.font = font;
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.fontStyle = fontStyle;
        tmpText.color = color;
        tmpText.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        Transform transform = root.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }
    }

    private static void ClearPersistentListeners(UnityEngine.Events.UnityEventBase unityEvent)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(unityEvent, i);
        }
    }

    private static void StretchFull(RectTransform rectTransform)
    {
        StretchAnchored(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    /// <summary>锚点矩形 + offsetMin/offsetMax，随父级 Canvas 等比缩放</summary>
    private static void StretchAnchored(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureDirectory(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
