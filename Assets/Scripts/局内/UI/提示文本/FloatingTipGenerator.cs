using System.Collections;
using System.Collections.Generic;
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

    [Header("持续对话框设置")]
    [Tooltip("持续对话框的字体大小")]
    [SerializeField] private int persistentFontSize = 32;
    [Tooltip("持续对话框的颜色")]
    [SerializeField] private Color persistentTextColor = new Color(1f, 0.9f, 0.5f, 1f); // 金色
    [Tooltip("持续对话框在屏幕中的Y位置比例（0-1）")]
    [SerializeField, Range(0f, 1f)] private float persistentDialogYRatio = 0.3f;

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

    // 持续对话框管理
    private readonly Dictionary<string, GameObject> m_persistentDialogs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, Coroutine> m_persistentDialogCoroutines = new Dictionary<string, Coroutine>();
    private readonly Queue<string> m_dialogQueue = new Queue<string>();
    private Coroutine m_dialogQueueCoroutine;
    private bool m_isShowingQueuedDialog;

    public void SetPersistentDialogYRatio(float ratio)
    {
        persistentDialogYRatio = Mathf.Clamp01(ratio);
    }

    public float GetPersistentDialogYRatio()
    {
        return persistentDialogYRatio;
    }

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
            // parentCanvasRect 可能不是 Canvas 自身的 RectTransform（比如是子物体的），
            // 需要向上查找或通过 GetComponentInParent 获取 Canvas
            rootCanvas = parentCanvasRect.GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = parentCanvasRect.GetComponentInParent<Canvas>();
            }
            if (rootCanvas == null)
            {
                Debug.LogError("[FloatingTipGenerator] parentCanvasRect 上及其父级都找不到 Canvas 组件！");
            }
        }
    }

    /// <summary>
    /// 获取 Canvas 坐标转换所需的 Camera。
    /// Screen Space - Overlay 模式下必须传 null，否则 ScreenPointToLocalPointInRectangle 会返回 false。
    /// </summary>
    private Camera GetCanvasCamera()
    {
        if (rootCanvas == null) return null;
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return rootCanvas.worldCamera;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowCenterDialog($"【测试对话】persistentDialogYRatio={persistentDialogYRatio:F2}, screenCenter=({Screen.width / 2f}, {Screen.height * persistentDialogYRatio:F0})", 4f);
        }
    }
#endif

    public void ShowDefaultTip(string message) 
    {
        if (pos == null)
        {
            Debug.LogError("[FloatingTipGenerator] pos 未设置，无法显示默认提示。");
            return;
        }
        ShowTipAtUIPosition(pos, message);
    }

    /// <summary>
    /// 直接在指定 UI Transform 的位置显示提示文本（跳过屏幕坐标转换，避免二次转换导致坐标错位）
    /// </summary>
    private void ShowTipAtUIPosition(Transform uiTarget, string message)
    {
        if (parentCanvasRect == null)
        {
            Debug.LogError("父Canvas RectTransform未设置，无法生成提示文本。");
            return;
        }

        GameObject tipObj = CreateTipObject(message);
        RectTransform tipRect = tipObj.GetComponent<RectTransform>();
        tipObj.transform.SetParent(parentCanvasRect, false);

        RectTransform targetRect = uiTarget.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            // 将目标 UI 元素的 anchoredPosition 转换到 parentCanvasRect 坐标系
            Vector2 targetAnchored = targetRect.anchoredPosition;
            // 如果目标的父级就是 parentCanvasRect，直接使用
            if (targetRect.parent == parentCanvasRect)
            {
                tipRect.anchoredPosition = targetAnchored + screenOffset;
            }
            else
            {
                // 目标在不同层级，转换坐标
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), targetRect.position);
                Camera cam = GetCanvasCamera();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, screenPos, cam, out Vector2 uiPos);
                tipRect.anchoredPosition = uiPos + screenOffset;
            }
        }
        else
        {
            tipRect.anchoredPosition = screenOffset;
        }

        StartCoroutine(AnimateTip(tipObj));
    }
    public void ShowTipAtObject(Transform targetTransform, string message,bool ifUse=false)
    {
        if(!ifUse)
        return;
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
        Camera cam = GetCanvasCamera();
        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, finalScreenPos, cam, out uiPos);
        if (!converted)
        {
            // fallback：直接计算（仅对 Screen Space - Overlay 有效）
            uiPos = finalScreenPos - new Vector2(Screen.width / 2f, Screen.height / 2f);
        }
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

    #region 持续对话框系统

    /// <summary>
    /// 开始一个持续对话框，对话框从底部淡入并停留在屏幕中央偏上位置
    /// </summary>
    /// <param name="dialogId">唯一ID，用于后续关闭</param>
    /// <param name="message">显示的文本</param>
    /// <param name="autoDismissDelay">自动消失延迟（秒），0表示不自动消失</param>
    public void StartPersistentDialog(string dialogId, string message, float autoDismissDelay = 0f)
    {
        if (string.IsNullOrEmpty(dialogId))
        {
            Debug.LogError("[FloatingTipGenerator] StartPersistentDialog: dialogId 不能为空");
            return;
        }

        // 如果已有同ID的对话框，先关闭旧的
        if (m_persistentDialogs.ContainsKey(dialogId))
        {
            StopPersistentDialog(dialogId);
        }

        // 加入队列，按顺序显示
        m_dialogQueue.Enqueue(dialogId);
        // 存储消息以便后续使用
        m_pendingDialogMessages[dialogId] = new DialogEntry { Message = message, AutoDismissDelay = autoDismissDelay };

        if (m_dialogQueueCoroutine == null)
        {
            m_dialogQueueCoroutine = StartCoroutine(ProcessDialogQueue());
        }
    }

    private readonly Dictionary<string, DialogEntry> m_pendingDialogMessages = new Dictionary<string, DialogEntry>();

    private struct DialogEntry
    {
        public string Message;
        public float AutoDismissDelay;
    }

    private IEnumerator ProcessDialogQueue()
    {
        while (m_dialogQueue.Count > 0)
        {
            string dialogId = m_dialogQueue.Dequeue();
            if (!m_pendingDialogMessages.TryGetValue(dialogId, out DialogEntry entry))
            {
                continue;
            }

            m_pendingDialogMessages.Remove(dialogId);

            // 等待上一个对话框显示完成（淡入+短暂停留）
            if (m_isShowingQueuedDialog)
            {
                yield return new WaitForSeconds(0.4f);
            }

            m_isShowingQueuedDialog = true;
            yield return StartCoroutine(ShowPersistentDialogCoroutine(dialogId, entry.Message, entry.AutoDismissDelay));
            m_isShowingQueuedDialog = false;
        }

        m_dialogQueueCoroutine = null;
    }

    private IEnumerator ShowPersistentDialogCoroutine(string dialogId, string message, float autoDismissDelay)
    {
        if (parentCanvasRect == null)
        {
            Debug.LogError("[FloatingTipGenerator] 父Canvas RectTransform未设置");
            yield break;
        }

        // 创建对话框对象
        GameObject dialogObj = CreatePersistentDialogObject(message);
        RectTransform rect = dialogObj.GetComponent<RectTransform>();
        CanvasGroup cg = dialogObj.GetComponent<CanvasGroup>();

        dialogObj.transform.SetParent(parentCanvasRect, false);

        // 定位到屏幕中央偏上
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height * persistentDialogYRatio);
        Vector2 uiPos;
        Camera cam = GetCanvasCamera();
        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvasRect, screenCenter, cam, out uiPos);
        if (!converted)
        {
            Debug.LogError($"[FloatingTipGenerator] ScreenPointToLocalPointInRectangle 转换失败！" +
                $"screenCenter=({screenCenter.x},{screenCenter.y}), " +
                $"parentCanvasRect={(parentCanvasRect != null ? parentCanvasRect.name : "null")}, " +
                $"rootCanvas={(rootCanvas != null ? rootCanvas.name : "null")}, " +
                $"camera={(cam != null ? cam.name : "null")}, " +
                $"Canvas renderMode={(rootCanvas != null ? rootCanvas.renderMode.ToString() : "N/A")}");
            // fallback：直接使用屏幕中心作为 anchoredPosition（仅对 Screen Space - Overlay 有效）
            uiPos = screenCenter - new Vector2(Screen.width / 2f, Screen.height / 2f);
        }
        rect.anchoredPosition = uiPos;

        m_persistentDialogs[dialogId] = dialogObj;

        // 淡入动画
        float elapsed = 0f;
        float duration = fadeInDuration * 0.6f; // 持续对话框淡入稍快
        Vector2 startPos = rect.anchoredPosition;
        Vector2 targetPos = startPos + Vector2.up * (fadeInUpDistance * 0.5f);

        while (elapsed < duration)
        {
            if (dialogObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (dialogObj != null)
        {
            cg.alpha = 1f;
            rect.anchoredPosition = targetPos;
        }

        // 如果有自动消失时间，等待后自动关闭
        if (autoDismissDelay > 0f)
        {
            yield return new WaitForSeconds(autoDismissDelay);
            StopPersistentDialog(dialogId);
        }
    }

    /// <summary>
    /// 停止一个持续对话框（带淡出动画）
    /// </summary>
    /// <param name="dialogId">开始对话框时传入的ID</param>
    public void StopPersistentDialog(string dialogId)
    {
        if (string.IsNullOrEmpty(dialogId) || !m_persistentDialogs.TryGetValue(dialogId, out GameObject dialogObj))
        {
            return;
        }

        m_persistentDialogs.Remove(dialogId);

        if (m_persistentDialogCoroutines.TryGetValue(dialogId, out Coroutine coroutine))
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            m_persistentDialogCoroutines.Remove(dialogId);
        }

        if (dialogObj != null)
        {
            StartCoroutine(DismissPersistentDialogCoroutine(dialogObj));
        }
    }

    /// <summary>
    /// 更新持续对话框的文本内容
    /// </summary>
    public void UpdatePersistentDialogText(string dialogId, string newMessage)
    {
        if (string.IsNullOrEmpty(dialogId) || !m_persistentDialogs.TryGetValue(dialogId, out GameObject dialogObj))
        {
            return;
        }

        TextMeshProUGUI tmp = dialogObj.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = newMessage;
        }
    }

    /// <summary>
    /// 检查指定ID的持续对话框是否存在
    /// </summary>
    public bool HasPersistentDialog(string dialogId)
    {
        return !string.IsNullOrEmpty(dialogId) && m_persistentDialogs.ContainsKey(dialogId);
    }

    /// <summary>
    /// 立即清除所有持续对话框
    /// </summary>
    public void ClearAllPersistentDialogs()
    {
        foreach (var kvp in m_persistentDialogs)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        m_persistentDialogs.Clear();
        m_persistentDialogCoroutines.Clear();
        m_dialogQueue.Clear();
        m_pendingDialogMessages.Clear();
        if (m_dialogQueueCoroutine != null)
        {
            StopCoroutine(m_dialogQueueCoroutine);
            m_dialogQueueCoroutine = null;
        }
        m_isShowingQueuedDialog = false;
    }

    private IEnumerator DismissPersistentDialogCoroutine(GameObject dialogObj)
    {
        if (dialogObj == null) yield break;

        CanvasGroup cg = dialogObj.GetComponent<CanvasGroup>();
        RectTransform rect = dialogObj.GetComponent<RectTransform>();
        if (cg == null || rect == null)
        {
            Destroy(dialogObj);
            yield break;
        }

        Vector2 startPos = rect.anchoredPosition;
        float elapsed = 0f;
        float duration = fadeOutDuration * 0.5f; // 淡出稍快

        while (elapsed < duration)
        {
            if (dialogObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            rect.anchoredPosition = startPos + Vector2.up * (fadeOutUpDistance * 0.5f * t);
            yield return null;
        }

        if (dialogObj != null)
        {
            Destroy(dialogObj);
        }
    }

    private GameObject CreatePersistentDialogObject(string message)
    {
        GameObject dialogObj = new GameObject("PersistentDialog");
        dialogObj.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = dialogObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700, 100);

        TextMeshProUGUI tmp = dialogObj.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = persistentFontSize;
        tmp.color = persistentTextColor;
        tmp.font = tmpFont;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.rectTransform.sizeDelta = new Vector2(700, 120);
        tmp.enableWordWrapping = true;

        // 添加描边效果
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.7f);

        CanvasGroup cg = dialogObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        return dialogObj;
    }

    /// <summary>
    /// 在屏幕中央显示一个短期对话框（非持续，自动消失）
    /// </summary>
    public void ShowCenterDialog(string message, float duration = 2.5f)
    {
        string dialogId = System.Guid.NewGuid().ToString();
        StartPersistentDialog(dialogId, message, duration);
    }

    /// <summary>等待中央对话框队列播放完毕（含淡出）</summary>
    public IEnumerator WaitForDialogQueueIdle()
    {
        while (m_dialogQueueCoroutine != null || m_dialogQueue.Count > 0 || m_isShowingQueuedDialog)
        {
            yield return null;
        }

        yield return null;
    }

    #endregion
}