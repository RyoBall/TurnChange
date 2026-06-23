using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 开始界面开发者浮层：Tab 显隐，提供浓缩模式开关。
/// 仅在 Start 场景自动创建，不追求美观。
/// </summary>
[DisallowMultipleComponent]
public class StartScreenDevOverlay : MonoBehaviour
{
    private const string StartSceneName = "Start";

    private GameObject m_root;
    private Toggle m_condensedModeToggle;
    private Text m_statusText;
    private bool m_isVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != StartSceneName)
        {
            return;
        }

        if (Object.FindAnyObjectByType<StartScreenDevOverlay>() != null)
        {
            return;
        }

        var host = new GameObject(nameof(StartScreenDevOverlay));
        host.AddComponent<StartScreenDevOverlay>();
    }

    private void Awake()
    {
        BuildUi();
        SetVisible(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab))
        {
            return;
        }

        SetVisible(!m_isVisible);
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("DevOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        m_root = new GameObject("DevPanel", typeof(RectTransform), typeof(Image));
        m_root.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = m_root.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(420f, 120f);

        Image panelImage = m_root.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        Text titleText = CreateText(m_root.transform, "Title", "[Dev] Tab 显隐", 18, FontStyle.Bold);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(12f, -36f);
        titleRect.offsetMax = new Vector2(-12f, -8f);
        titleText.alignment = TextAnchor.UpperLeft;

        m_condensedModeToggle = CreateToggle(m_root.transform, "CondensedModeToggle");
        RectTransform toggleRect = m_condensedModeToggle.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 0.5f);
        toggleRect.anchorMax = new Vector2(0f, 0.5f);
        toggleRect.pivot = new Vector2(0f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(12f, -8f);
        toggleRect.sizeDelta = new Vector2(28f, 28f);
        m_condensedModeToggle.isOn = CondensedModePreference.IsEnabled;
        m_condensedModeToggle.onValueChanged.AddListener(OnCondensedModeChanged);

        Text toggleLabel = CreateText(m_root.transform, "CondensedModeLabel", "浓缩模式", 16, FontStyle.Normal);
        RectTransform labelRect = toggleLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(1f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.offsetMin = new Vector2(48f, -18f);
        labelRect.offsetMax = new Vector2(-12f, 18f);
        toggleLabel.alignment = TextAnchor.MiddleLeft;

        m_statusText = CreateText(m_root.transform, "StatusText", BuildStatusText(), 14, FontStyle.Normal);
        RectTransform statusRect = m_statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.offsetMin = new Vector2(12f, 8f);
        statusRect.offsetMax = new Vector2(-12f, 32f);
        m_statusText.alignment = TextAnchor.LowerLeft;
    }

    private void OnCondensedModeChanged(bool isOn)
    {
        CondensedModePreference.SetEnabled(isOn);
        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        if (m_statusText != null)
        {
            m_statusText.text = BuildStatusText();
        }
    }

    private static string BuildStatusText()
    {
        bool enabled = CondensedModePreference.IsEnabled;
        return enabled
            ? "已开启：浓缩关卡 + 战斗等级=关卡等级"
            : "已关闭：常规关卡 + 战斗等级=战队等级";
    }

    private void SetVisible(bool visible)
    {
        m_isVisible = visible;
        if (m_root != null)
        {
            m_root.SetActive(visible);
        }

        if (visible)
        {
            SyncToggleState();
        }
    }

    private void SyncToggleState()
    {
        if (m_condensedModeToggle == null)
        {
            return;
        }

        m_condensedModeToggle.SetIsOnWithoutNotify(CondensedModePreference.IsEnabled);
        RefreshStatusText();
    }

    private static Text CreateText(Transform parent, string objectName, string content, int fontSize, FontStyle fontStyle)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Toggle CreateToggle(Transform parent, string objectName)
    {
        GameObject toggleObject = DefaultControls.CreateToggle(new DefaultControls.Resources());
        toggleObject.name = objectName;
        toggleObject.transform.SetParent(parent, false);

        Text legacyLabel = toggleObject.GetComponentInChildren<Text>();
        if (legacyLabel != null)
        {
            legacyLabel.gameObject.SetActive(false);
        }

        return toggleObject.GetComponent<Toggle>();
    }
}
