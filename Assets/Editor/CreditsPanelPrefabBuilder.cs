using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 创建制作人名单预制体，并挂载到通用物体 Prefab（随 DontDestroyOnLoad 跨场景保留）。
/// </summary>
public static class CreditsPanelPrefabBuilder
{
    private const string PrefabPath = "Assets/Resources/Prefabs/CreditsPanel.prefab";
    private const string SharedPrefabPath = "Assets/Resources/Prefabs/通用物体/通用物体.prefab";
    private const string FontPath = "Assets/Resources/font/哥特.asset";
    private const string StartScenePath = "Assets/Scenes/Start.unity";
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/Create Credits Panel Prefab")]
    public static void CreateCreditsPanelPrefab()
    {
        GameObject root = BuildCreditsPanelHierarchy();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreditsPanelPrefabBuilder] 已创建预制体: {PrefabPath}");
    }

    [MenuItem("Tools/Setup Credits Panel In Scenes")]
    public static void SetupCreditsPanelInScenes()
    {
        CreateCreditsPanelPrefab();
        SetupCreditsPanelInSharedPrefab();
        RemoveCreditsPanelFromScenes();
        SetupStartScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[CreditsPanelPrefabBuilder] 通用物体 / Start 场景制作人名单配置完成。");
    }

    public static void SetupCreditsPanelInSharedPrefab()
    {
        CreateCreditsPanelPrefab();

        GameObject sharedPrefabRoot = PrefabUtility.LoadPrefabContents(SharedPrefabPath);
        if (sharedPrefabRoot == null)
        {
            Debug.LogError($"[CreditsPanelPrefabBuilder] 未找到通用物体 Prefab：{SharedPrefabPath}");
            return;
        }

        try
        {
            CreditsPanelView existing = sharedPrefabRoot.GetComponentInChildren<CreditsPanelView>(true);
            if (existing != null)
            {
                if (!existing.gameObject.activeSelf)
                {
                    existing.gameObject.SetActive(true);
                }

                PrefabUtility.SaveAsPrefabAsset(sharedPrefabRoot, SharedPrefabPath);
                return;
            }

            GameObject creditsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (creditsPrefab == null)
            {
                Debug.LogError("[CreditsPanelPrefabBuilder] 未找到 CreditsPanel 预制体。");
                return;
            }

            Transform canvasRoot = sharedPrefabRoot.transform.Find("不摧毁的Canvas");
            Transform parent = canvasRoot != null ? canvasRoot : sharedPrefabRoot.transform;
            GameObject instance = PrefabUtility.InstantiatePrefab(creditsPrefab, parent) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[CreditsPanelPrefabBuilder] 无法将 CreditsPanel 实例化到通用物体 Prefab。");
                return;
            }

            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            PrefabUtility.SaveAsPrefabAsset(sharedPrefabRoot, SharedPrefabPath);
            Debug.Log("[CreditsPanelPrefabBuilder] 已将 CreditsPanel 写入通用物体 Prefab。");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(sharedPrefabRoot);
        }
    }

    private static void RemoveCreditsPanelFromScenes()
    {
        RemoveCreditsPanelFromScene(StartScenePath);
        RemoveCreditsPanelFromScene(MainScenePath);
    }

    private static void RemoveCreditsPanelFromScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        CreditsPanelView[] panels = Object.FindObjectsOfType<CreditsPanelView>(true);
        bool removedAny = false;

        for (int i = 0; i < panels.Length; i++)
        {
            CreditsPanelView panel = panels[i];
            if (panel == null)
            {
                continue;
            }

            GameObject nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(panel.gameObject);
            if (nearestRoot != null && nearestRoot != panel.gameObject)
            {
                continue;
            }

            Object.DestroyImmediate(panel.gameObject);
            removedAny = true;
        }

        if (removedAny)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[CreditsPanelPrefabBuilder] 已从场景移除独立 CreditsPanel：{scenePath}");
        }
    }

    private static void SetupStartScene()
    {
        Scene scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        EnsureStartCreditsButton();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureStartCreditsButton()
    {
        CreditsPanelButton existingButton = Object.FindObjectOfType<CreditsPanelButton>(true);
        if (existingButton != null)
        {
            WireStartCreditsButton(existingButton.gameObject);
            return;
        }

        Transform mainRoot = GameObject.Find("Canvas/Main")?.transform;
        if (mainRoot == null)
        {
            Debug.LogWarning("[CreditsPanelPrefabBuilder] 未找到 Canvas/Main，跳过开始场景按钮创建。");
            return;
        }

        GameObject buttonGo = new GameObject("制作名单", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CreditsPanelButton));
        buttonGo.transform.SetParent(mainRoot, false);

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(220f, -207f);
        rect.sizeDelta = new Vector2(220f, 80f);
        rect.localScale = new Vector3(0.6f, 0.6f, 1f);

        Image image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 0.75f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text label = textGo.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null)
        {
            label.font = font;
        }

        label.text = "制作名单";
        label.fontSize = 30f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        WireStartCreditsButton(buttonGo);
    }

    private static void WireStartCreditsButton(GameObject buttonGo)
    {
        CreditsPanelButton opener = buttonGo.GetComponent<CreditsPanelButton>();
        CreditsPanelView panel = Object.FindObjectOfType<CreditsPanelView>(true);
        Transform mainRoot = GameObject.Find("Canvas/Main")?.transform;
        Transform background = GameObject.Find("Canvas/Main/BackgroundLayer")?.transform;
        Image backgroundImage = background != null ? background.GetComponent<Image>() : null;

        SerializedObject serializedOpener = new SerializedObject(opener);
        serializedOpener.FindProperty("m_creditsPanel").objectReferenceValue = panel;
        serializedOpener.FindProperty("m_button").objectReferenceValue = buttonGo.GetComponent<Button>();
        serializedOpener.FindProperty("m_backgroundTransform").objectReferenceValue = background as RectTransform;
        serializedOpener.FindProperty("m_backgroundImage").objectReferenceValue = backgroundImage;
        serializedOpener.FindProperty("m_startSceneUiRoot").objectReferenceValue = mainRoot;
        serializedOpener.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject BuildCreditsPanelHierarchy()
    {
        GameObject root = new GameObject("CreditsPanel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CreditsPanelView));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200;

        GameObject dimGo = new GameObject("DimOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimGo.transform.SetParent(root.transform, false);
        RectTransform dimRect = dimGo.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        Image dimImage = dimGo.GetComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0f);
        dimImage.raycastTarget = true;

        GameObject contentGo = new GameObject("ContentPanel", typeof(RectTransform), typeof(CanvasGroup));
        contentGo.transform.SetParent(root.transform, false);
        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(760f, 520f);
        contentRect.anchoredPosition = Vector2.zero;

        GameObject creditsTextGo = new GameObject("CreditsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        creditsTextGo.transform.SetParent(contentGo.transform, false);
        RectTransform creditsRect = creditsTextGo.GetComponent<RectTransform>();
        creditsRect.anchorMin = new Vector2(0.5f, 0.55f);
        creditsRect.anchorMax = new Vector2(0.5f, 0.55f);
        creditsRect.pivot = new Vector2(0.5f, 0.5f);
        creditsRect.sizeDelta = new Vector2(700f, 360f);
        creditsRect.anchoredPosition = Vector2.zero;

        TMP_Text creditsText = creditsTextGo.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null)
        {
            creditsText.font = font;
        }

        creditsText.text = "策划：容克Kaiser,雨大神\n程序：张良\n美术：Akane,111\n特效：吟月R";
        creditsText.fontSize = 34f;
        creditsText.lineSpacing = 18f;
        creditsText.alignment = TextAlignmentOptions.Center;
        creditsText.color = Color.white;

        GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(contentGo.transform, false);
        RectTransform closeRect = closeGo.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.12f);
        closeRect.anchorMax = new Vector2(0.5f, 0.12f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(220f, 64f);
        closeRect.anchoredPosition = Vector2.zero;

        Image closeImage = closeGo.GetComponent<Image>();
        closeImage.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);

        GameObject closeTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        closeTextGo.transform.SetParent(closeGo.transform, false);
        RectTransform closeTextRect = closeTextGo.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        TMP_Text closeLabel = closeTextGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            closeLabel.font = font;
        }

        closeLabel.text = "关闭";
        closeLabel.fontSize = 28f;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = Color.white;

        CreditsPanelView view = root.GetComponent<CreditsPanelView>();
        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("m_panelRoot").objectReferenceValue = root;
        serializedView.FindProperty("m_dimOverlay").objectReferenceValue = dimImage;
        serializedView.FindProperty("m_contentCanvasGroup").objectReferenceValue = contentGo.GetComponent<CanvasGroup>();
        serializedView.FindProperty("m_creditsText").objectReferenceValue = creditsText;
        serializedView.FindProperty("m_closeButton").objectReferenceValue = closeGo.GetComponent<Button>();
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }
}
