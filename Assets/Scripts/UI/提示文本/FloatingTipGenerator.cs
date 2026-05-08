using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // 使用TextMeshPro实现更精美的文本效果（推荐）

public class FloatingTipGenerator : MonoBehaviour
{
    public static FloatingTipGenerator Instance;
    [Header("文本样式设置")]
    [SerializeField] private Font font;                     // 普通Text字体（若不使用TMP）
    [SerializeField] private TMP_FontAsset tmpFont;         // TMP字体
    [SerializeField] private int fontSize = 36;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private FontStyle fontStyle = FontStyle.Bold;

    [Header("动画时间参数")]
    [Tooltip("淡入阶段持续时间（慢速上升+淡入）")]
    [SerializeField] private float fadeInDuration = 0.8f;

    [Tooltip("悬浮阶段持续时间（更慢的上升速度，几乎停滞）")]
    [SerializeField] private float hoverDuration = 1.2f;

    [Tooltip("淡出阶段持续时间")]
    [SerializeField] private float fadeOutDuration = 0.6f;

    [Header("位移参数")]
    [Tooltip("淡入阶段向上移动的总距离（世界单位或UI局部坐标，取决于Canvas类型）")]
    [SerializeField] private float fadeInUpDistance = 60f;

    [Tooltip("悬浮阶段向上移动的距离（非常缓慢，体现悬浮感）")]
    [SerializeField] private float hoverUpDistance = 20f;

    [Tooltip("淡出阶段向上移动的距离")]
    [SerializeField] private float fadeOutUpDistance = 40f;

    [Header("位置偏移")]
    [Tooltip("相对于物体位置的屏幕偏移量（像素）")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 50);

    [Header("其他设置")]
    [Tooltip("提示文本预制体（如果不指定，则自动创建）")]
    [SerializeField] private GameObject tipPrefab;
    [SerializeField] private Transform pos;

    [Tooltip("父级Canvas（若为null则自动查找场景中第一个Canvas）")]
    [SerializeField] private RectTransform parentCanvasRect;

    // 可选：使用普通Text还是TextMeshPro
    public enum TextType { TextMeshPro, LegacyText }
    [SerializeField] private TextType textType = TextType.TextMeshPro;

    // 缓存Canvas，用于世界坐标转换等
    private Canvas rootCanvas;

    private void Awake()
    {
        Instance = this;
        // 自动查找Canvas
        if (parentCanvasRect == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                parentCanvasRect = canvas.GetComponent<RectTransform>();
                rootCanvas = canvas;
            }
            else
            {
                Debug.LogError("场景中没有Canvas！请创建一个Canvas或将脚本挂载到Canvas下的物体上，并指定parentCanvasRect。");
            }
        }
        else
        {
            rootCanvas = parentCanvasRect.GetComponent<Canvas>();
        }
    }
    public void ShowDefaultTip(string message) 
    {
        ShowTipAtObject(pos, message);
    }
    public void ShowTipAtObject(Transform targetTransform, string message)
    {
        if (targetTransform == null)
        {
            Debug.LogError("目标Transform为空，无法生成提示文本。");
            return;
        }

        Vector2 screenPosition = GetScreenPosition(targetTransform.gameObject);
        ShowTipAt(screenPosition, message);
    }

    public void ShowTip(string message)
    {
        // 计算屏幕中上位置：水平中央，垂直在屏幕高度 1/3 处（可自定义）
        Vector2 screenCenterTop = new Vector2(Screen.width / 2f, Screen.height * 0.35f);
        ShowTipAt(screenCenterTop, message);
    }

    public void ShowTipAt(Vector2 screenPosition, string message)
    {
        if (parentCanvasRect == null)
        {
            Debug.LogError("父Canvas RectTransform未设置，无法生成提示文本。");
            return;
        }

        // 创建提示文本对象
        GameObject tipObj = CreateTipObject(message);
        RectTransform rectTransform = tipObj.GetComponent<RectTransform>();

        // 设置父级为Canvas
        tipObj.transform.SetParent(parentCanvasRect, false);

        // 应用偏移量
        Vector2 finalScreenPos = screenPosition + screenOffset;

        // 将屏幕坐标转换为UI局部坐标（以Canvas为基准）
        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, finalScreenPos, rootCanvas.worldCamera, out uiPos);
        rectTransform.anchoredPosition = uiPos;

        // 启动动画协程
        StartCoroutine(AnimateTip(tipObj));
    }

    private Vector2 GetScreenPosition(GameObject targetObject)
    {
        // 判断物体是否有RectTransform（UI元素）
        RectTransform rectTransform = targetObject.GetComponent<RectTransform>();

        if (rectTransform != null && rootCanvas != null)
        {
            // 如果是UI元素，将UI局部坐标转换为屏幕坐标
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, rectTransform.position);
            return screenPos;
        }
        else
        {
            // 如果是3D世界物体，使用世界坐标转屏幕坐标
            Vector3 worldPos = targetObject.transform.position;
            Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : Vector3.zero;

            if (Camera.main == null)
            {
                Debug.LogWarning("场景中没有主摄像机，无法将世界坐标转换为屏幕坐标。将使用屏幕中心作为备选。");
                return new Vector2(Screen.width / 2, Screen.height / 2);
            }

            // 确保物体在摄像机前方
            if (screenPos.z < 0)
            {
                Debug.LogWarning($"物体 {targetObject.name} 在摄像机后方，屏幕坐标可能不准确。");
            }

            return new Vector2(screenPos.x, screenPos.y);
        }
    }

    private GameObject CreateTipObject(string message)
    {
        GameObject tipObj;

        if (tipPrefab != null)
        {
            tipObj = Instantiate(tipPrefab);
            // 如果预制体已经有文本组件，尝试获取并设置内容
            if (textType == TextType.TextMeshPro)
            {
                TextMeshProUGUI tmp = tipObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = message;
                else Debug.LogWarning("预制体上没有TextMeshProUGUI组件，将自动添加默认组件。");
            }
            else
            {
                Text legacy = tipObj.GetComponent<Text>();
                if (legacy != null) legacy.text = message;
                else Debug.LogWarning("预制体上没有Text组件，将自动添加默认组件。");
            }
        }
        else
        {
            // 动态创建UI对象
            tipObj = new GameObject("FloatingTip");
            tipObj.layer = LayerMask.NameToLayer("UI");

            RectTransform rect = tipObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 80); // 默认大小

            if (textType == TextType.TextMeshPro)
            {
                TextMeshProUGUI tmp = tipObj.AddComponent<TextMeshProUGUI>();
                tmp.text = message;
                tmp.fontSize = fontSize;
                tmp.color = textColor;
                tmp.font = tmpFont;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                // 让文本根据内容自适应宽度
                tmp.rectTransform.sizeDelta = new Vector2(600, 100);
            }
            else
            {
                Text legacyText = tipObj.AddComponent<Text>();
                legacyText.text = message;
                legacyText.fontSize = fontSize;
                legacyText.color = textColor;
                legacyText.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
                legacyText.fontStyle = fontStyle;
                legacyText.alignment = TextAnchor.MiddleCenter;
                legacyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                legacyText.verticalOverflow = VerticalWrapMode.Truncate;
                // 调整rect
                legacyText.rectTransform.sizeDelta = new Vector2(500, 100);
            }

            // 添加CanvasGroup控制淡入淡出
            CanvasGroup cg = tipObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        // 确保有CanvasGroup组件（动画必备）
        CanvasGroup canvasGroup = tipObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = tipObj.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f; // 初始完全透明

        return tipObj;
    }

    private IEnumerator AnimateTip(GameObject tipObj)
    {
        if (tipObj == null) yield break;

        RectTransform rect = tipObj.GetComponent<RectTransform>();
        CanvasGroup cg = tipObj.GetComponent<CanvasGroup>();
        if (rect == null || cg == null) yield break;

        // 记录起始位置（锚点位置为生成时的屏幕位置对应的anchoredPosition）
        Vector2 startPos = rect.anchoredPosition;

        // ----------------- 阶段1：淡入 + 慢速向上移动 -----------------
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            if (tipObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            // 使用平滑缓动曲线，让淡入和移动更自然
            float easeT = Mathf.SmoothStep(0f, 1f, t);

            // 透明度从0到1
            cg.alpha = Mathf.Lerp(0f, 1f, easeT);

            // 位置：从起始点向上移动 fadeInUpDistance 距离
            Vector2 newPos = startPos + Vector2.up * (fadeInUpDistance * easeT);
            rect.anchoredPosition = newPos;

            yield return null;
        }

        // 确保最终完全可见且位置准确
        if (tipObj != null)
        {
            cg.alpha = 1f;
            rect.anchoredPosition = startPos + Vector2.up * fadeInUpDistance;
        }

        // ----------------- 阶段2：更慢速度悬浮移动（轻微向上漂移，视觉上仿佛停滞悬浮） -----------------
        Vector2 hoverStartPos = rect.anchoredPosition;
        float hoverElapsed = 0f;
        while (hoverElapsed < hoverDuration)
        {
            if (tipObj == null) yield break;
            hoverElapsed += Time.deltaTime;
            float t = hoverElapsed / hoverDuration;
            // 使用非常平滑的曲线，使移动极其缓慢，先快后慢的缓出效果更像悬浮
            float easeOutQuad = 1f - (1f - t) * (1f - t);  // 缓出二次曲线，开始略快，后来慢，但整体位移量小
            // 由于总位移只有hoverUpDistance，所以整体看起来非常缓慢地上漂
            Vector2 newPos = hoverStartPos + Vector2.up * (hoverUpDistance * easeOutQuad);
            rect.anchoredPosition = newPos;

            // 保持完全不透明，悬浮阶段不淡入淡出
            cg.alpha = 1f;
            yield return null;
        }

        if (tipObj != null)
        {
            rect.anchoredPosition = hoverStartPos + Vector2.up * hoverUpDistance;
        }

        // ----------------- 阶段3：淡出 + 继续向上移动（更快淡出） -----------------
        Vector2 fadeOutStartPos = rect.anchoredPosition;
        float fadeOutElapsed = 0f;
        while (fadeOutElapsed < fadeOutDuration)
        {
            if (tipObj == null) yield break;
            fadeOutElapsed += Time.deltaTime;
            float t = fadeOutElapsed / fadeOutDuration;
            // 淡出时透明度1→0，使用平滑曲线
            float easeT = Mathf.SmoothStep(0f, 1f, t);
            cg.alpha = Mathf.Lerp(1f, 0f, easeT);

            // 继续向上移动
            Vector2 newPos = fadeOutStartPos + Vector2.up * (fadeOutUpDistance * easeT);
            rect.anchoredPosition = newPos;
            yield return null;
        }

        // 动画结束，销毁文本对象
        if (tipObj != null)
        {
            Destroy(tipObj);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            string[] testMessages = { "✨ 任务完成！", "❤️ 生命值 +10", "⚡ 闪电一击", "🍃 微风轻拂", "🌟 获得成就", "🎉 庆典开始", "💎 宝石收集", "🔔 提醒事项" };
            string randomMsg = testMessages[Random.Range(0, testMessages.Length)];
            ShowTip(randomMsg);
        }

        // 按Y键测试在鼠标位置生成提示文本
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ShowDefaultTip("✨ 鼠标悬浮提示 ✨");
        }

        // 按U键测试在摄像机位置生成提示（演示物体跟随）
        if (Input.GetKeyDown(KeyCode.U))
        {
            // 示例：在主摄像机位置生成提示
            if (Camera.main != null)
            {
                ShowTipAtObject(Camera.main.gameObject.transform, "📷 摄像机位置提示");
            }
        }
    }
}